import { randomUUID } from "node:crypto";
import { decryptText, deriveMessageKey, encryptText, signPayload, verifyPayload } from "../../crypto-spec/dist/index";
import { ContentType, ReplayGuardConfig, ReceiveResult, RetryPolicy, SyncEnvelope } from "../../protocol-spec/dist/types";
import { shouldBlockByPolicy, SensitivePolicy } from "./policy";

export interface DedupWindow {
  has(key: string, nowMs: number): boolean;
  add(key: string, nowMs: number): void;
}

export interface EngineEvent {
  type: "enqueue" | "published" | "failed" | "received" | "duplicate" | "applied" | "rejected";
  messageId: string;
  detail?: string;
  at: string;
}

export class InMemoryDedupWindow implements DedupWindow {
  private readonly seen = new Map<string, number>();

  constructor(private readonly ttlMs: number) {}

  has(key: string, nowMs: number): boolean {
    this.gc(nowMs);
    return this.seen.has(key);
  }

  add(key: string, nowMs: number): void {
    this.seen.set(key, nowMs + this.ttlMs);
  }

  private gc(nowMs: number): void {
    for (const [key, expiresAt] of this.seen) {
      if (expiresAt <= nowMs) {
        this.seen.delete(key);
      }
    }
  }
}

export class SimulatorSyncEngine {
  private sequence = 0;
  private readonly lastSeenSequence = new Map<string, number>();
  private readonly nonceSeen = new Map<string, number>();
  private readonly outbox: Array<{ envelope: SyncEnvelope; attempts: number }> = [];
  private readonly events: EngineEvent[] = [];

  constructor(
    private readonly senderDeviceId: string,
    private readonly senderPrivateKeyPem: string,
    private readonly senderPublicKeyPem: string,
    private readonly workspaceKey: Buffer,
    private readonly workspaceIdHash: string,
    private readonly keyVersion: number,
    private readonly retryPolicy: RetryPolicy,
    private readonly dedup: DedupWindow,
    private readonly replayGuard: ReplayGuardConfig,
    private readonly sensitivePolicy: SensitivePolicy
  ) {}

  createEnvelope(plainText: string, manualOverride = false): SyncEnvelope {
    return this.createEnvelopeWithContent("text/plain", plainText, manualOverride);
  }

  createEnvelopeWithContent(contentType: ContentType, payloadText: string, manualOverride = false): SyncEnvelope {
    const policySensitiveType = contentType === "text/plain" || contentType === "text/html";
    if (policySensitiveType && shouldBlockByPolicy(payloadText, this.sensitivePolicy, manualOverride)) {
      throw new Error("policy-blocked-sensitive-content");
    }

    this.sequence += 1;
    const messageKey = deriveMessageKey(this.workspaceKey, this.senderDeviceId, this.sequence);
    const encrypted = encryptText(payloadText, messageKey);
    const payload = `${encrypted.ciphertextB64}.${encrypted.nonceB64}.${encrypted.authTagB64}`;
    const signature = signPayload(payload, this.senderPrivateKeyPem);

    return {
      protocolVersion: "1.0",
      messageId: randomUUID(),
      senderDeviceId: this.senderDeviceId,
      workspaceIdHash: this.workspaceIdHash,
      timestamp: new Date().toISOString(),
      sequence: this.sequence,
      keyVersion: this.keyVersion,
      contentType,
      encryptedPayload: payload,
      nonce: encrypted.nonceB64,
      signature,
      clientVersion: "0.1.0"
    };
  }

  enqueue(envelope: SyncEnvelope): void {
    this.outbox.push({ envelope, attempts: 0 });
    this.pushEvent("enqueue", envelope.messageId);
  }

  async flush(publish: (envelope: SyncEnvelope) => Promise<void>): Promise<void> {
    const pending = [...this.outbox];
    this.outbox.length = 0;

    for (const item of pending) {
      let delivered = false;

      while (!delivered && item.attempts < this.retryPolicy.maxAttempts) {
        try {
          item.attempts += 1;
          await publish(item.envelope);
          this.pushEvent("published", item.envelope.messageId, `attempt=${item.attempts}`);
          delivered = true;
        } catch (err) {
          const delay = this.backoffMs(item.attempts);
          this.pushEvent("failed", item.envelope.messageId, `attempt=${item.attempts}`);
          await this.sleep(delay);
          if (item.attempts >= this.retryPolicy.maxAttempts) {
            throw err;
          }
        }
      }
    }
  }

  receive(
    envelope: SyncEnvelope,
    expectedWorkspaceKey: Buffer,
    senderPublicKeyPem: string
  ): { result: ReceiveResult; text?: string; payload?: string; contentType?: ContentType } {
    const dedupKey = `${envelope.senderDeviceId}:${envelope.messageId}`;
    const now = Date.now();
    this.pushEvent("received", envelope.messageId);

    if (envelope.keyVersion !== this.keyVersion) {
      this.pushEvent("rejected", envelope.messageId, "key-version-mismatch");
      return { result: "rejected" };
    }

    if (!this.isTimestampValid(envelope.timestamp, now)) {
      this.pushEvent("rejected", envelope.messageId, "replay-timestamp-window");
      return { result: "rejected" };
    }

    if (this.hasSeenNonce(envelope.nonce, now)) {
      this.pushEvent("rejected", envelope.messageId, "replay-nonce");
      return { result: "rejected" };
    }

    if (!this.isSequenceValid(envelope.senderDeviceId, envelope.sequence)) {
      this.pushEvent("rejected", envelope.messageId, "invalid-sequence");
      return { result: "rejected" };
    }

    if (this.dedup.has(dedupKey, now)) {
      this.pushEvent("duplicate", envelope.messageId);
      return { result: "duplicate" };
    }

    const signatureOk = verifyPayload(envelope.encryptedPayload, envelope.signature, senderPublicKeyPem);
    if (!signatureOk) {
      this.pushEvent("rejected", envelope.messageId, "invalid-signature");
      return { result: "rejected" };
    }

    const parts = envelope.encryptedPayload.split(".");
    if (parts.length !== 3) {
      this.pushEvent("rejected", envelope.messageId, "invalid-payload");
      return { result: "rejected" };
    }

    try {
      const messageKey = deriveMessageKey(expectedWorkspaceKey, envelope.senderDeviceId, envelope.sequence);
      const text = decryptText(
        {
          ciphertextB64: parts[0],
          nonceB64: parts[1],
          authTagB64: parts[2]
        },
        messageKey
      );

      this.dedup.add(dedupKey, now);
      this.markNonceSeen(envelope.nonce, now);
      this.lastSeenSequence.set(envelope.senderDeviceId, envelope.sequence);
      this.pushEvent("applied", envelope.messageId);
      return { result: "applied", text, payload: text, contentType: envelope.contentType };
    } catch {
      this.pushEvent("rejected", envelope.messageId, "decrypt-failed");
      return { result: "rejected" };
    }
  }

  getEvents(): EngineEvent[] {
    return [...this.events];
  }

  getSenderPublicKeyPem(): string {
    return this.senderPublicKeyPem;
  }

  private backoffMs(attempt: number): number {
    const expo = Math.min(this.retryPolicy.maxDelayMs, this.retryPolicy.baseDelayMs * Math.pow(2, attempt - 1));
    const jitter = expo * this.retryPolicy.jitterRatio * Math.random();
    return Math.floor(expo + jitter);
  }

  private pushEvent(type: EngineEvent["type"], messageId: string, detail?: string): void {
    this.events.push({ type, messageId, detail, at: new Date().toISOString() });
  }

  private isTimestampValid(iso: string, nowMs: number): boolean {
    const tsMs = Date.parse(iso);
    if (!Number.isFinite(tsMs)) {
      return false;
    }
    return Math.abs(nowMs - tsMs) <= this.replayGuard.allowedClockSkewMs;
  }

  private hasSeenNonce(nonce: string, nowMs: number): boolean {
    this.gcNonce(nowMs);
    return this.nonceSeen.has(nonce);
  }

  private markNonceSeen(nonce: string, nowMs: number): void {
    this.nonceSeen.set(nonce, nowMs + this.replayGuard.nonceTtlMs);
  }

  private gcNonce(nowMs: number): void {
    for (const [nonce, exp] of this.nonceSeen) {
      if (exp <= nowMs) {
        this.nonceSeen.delete(nonce);
      }
    }
  }

  private isSequenceValid(senderDeviceId: string, incomingSeq: number): boolean {
    const last = this.lastSeenSequence.get(senderDeviceId);
    if (last === undefined) {
      return incomingSeq > 0;
    }
    return incomingSeq > last;
  }

  private async sleep(ms: number): Promise<void> {
    await new Promise((resolve) => setTimeout(resolve, ms));
  }
}
