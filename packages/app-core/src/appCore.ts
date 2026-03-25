import {
  DeviceIdentity,
  generateWorkspaceKey,
  parseInviteCode,
  workspaceIdHash,
  workspaceKeyToInviteCode
} from "../../crypto-spec/dist/index";
import { createHash } from "node:crypto";
import { ReplayGuardConfig, RetryPolicy, SyncEnvelope } from "../../protocol-spec/dist/types";
import { InMemoryDedupWindow, SimulatorSyncEngine } from "../../sync-simulator/dist/engine";
import { SensitivePolicy } from "../../sync-simulator/dist/policy";
import {
  AppSettings,
  ClipboardContentType,
  ClipboardWriter,
  DevicePlatform,
  HistoryRecord,
  PairingRequest,
  SpaceProfile,
  SyncStatusSnapshot,
  SyncTransport,
  TrustedDeviceProfile
} from "./contracts";
import { CustomServerOptions, ensureCustomServerReady } from "./customServer";

interface SpaceRuntime {
  profile: SpaceProfile;
  workspaceKey: Buffer;
  keyVersion: number;
  engine: SimulatorSyncEngine;
}

export interface WebDevOptions {
  enabled: boolean;
  mode: "off" | "proxy" | "embedded";
  assetBaseUrl?: string;
}

export interface AppCoreOptions {
  identity: DeviceIdentity;
  retryPolicy: RetryPolicy;
  replayGuard: ReplayGuardConfig;
  sensitivePolicy: SensitivePolicy;
  webDev?: WebDevOptions;
  customServer?: CustomServerOptions;
  dedupTtlMs?: number;
  settings?: Partial<AppSettings>;
  maxHistoryItems?: number;
  spaces?: Array<{
    id: string;
    name: string;
    workspaceKey: Buffer;
    workspaceIdHash: string;
    keyVersion: number;
    topic: string;
  }>;
  currentSpaceId?: string;
  workspaceKey?: Buffer;
  workspaceIdHash?: string;
  keyVersion?: number;
  topic?: string;
}

export class ClipboardSyncAppCore {
  private readonly trustedKeys = new Map<string, string>();
  private readonly trustedDevices = new Map<string, TrustedDeviceProfile>();
  private readonly oneTimeInvites = new Map<string, { expiresAt: number; consumed: boolean; spaceId: string }>();
  private readonly pairingRequests = new Map<string, PairingRequest & { publicKeyPem: string }>();
  private readonly spaces = new Map<string, SpaceRuntime>();
  private readonly history: HistoryRecord[] = [];
  private readonly maxHistoryItems: number;
  private settings: AppSettings;
  private activeSpaceId: string;
  private status: SyncStatusSnapshot = {
    connected: false,
    syncedOutCount: 0,
    syncedInCount: 0,
    rejectedEventCount: 0,
    trustedDeviceCount: 0,
    pendingPairingCount: 0,
    webDevEnabled: false,
    syncServerMode: "default",
    currentSpaceId: "default",
    syncMode: "manual",
    themeMode: "system",
    language: "zh-CN"
  };

  constructor(
    private readonly options: AppCoreOptions,
    private readonly transport: SyncTransport,
    private readonly clipboardWriter: ClipboardWriter
  ) {
    this.settings = {
      syncMode: "manual",
      themeMode: "system",
      language: "zh-CN",
      pairingPolicy: "manual-approve",
      autoSyncEnabled: false,
      sensitiveFilterEnabled: true,
      blacklistApps: [],
      webDevSyncEnabled: options.webDev?.enabled ?? false,
      localServerEnabled: options.customServer?.enabled ?? false,
      ...options.settings
    };

    this.maxHistoryItems = options.maxHistoryItems ?? 100;

    const inputSpaces = options.spaces ?? this.createCompatDefaultSpaces(options);
    if (inputSpaces.length === 0) {
      throw new Error("at least one space is required");
    }

    for (const s of inputSpaces) {
      const profile: SpaceProfile = {
        id: s.id,
        name: s.name,
        workspaceIdHash: s.workspaceIdHash,
        topic: s.topic
      };

      this.spaces.set(s.id, {
        profile,
        workspaceKey: s.workspaceKey,
        keyVersion: s.keyVersion,
        engine: this.createEngineForSpace(s.workspaceKey, s.workspaceIdHash, s.keyVersion)
      });
    }

    this.activeSpaceId = options.currentSpaceId && this.spaces.has(options.currentSpaceId) ? options.currentSpaceId : inputSpaces[0].id;

    this.registerTrustedDevice(options.identity.deviceId, options.identity.publicKeyPem, {
      displayName: options.identity.deviceId,
      platform: inferPlatformFromDeviceId(options.identity.deviceId)
    });
    this.status.currentSpaceId = this.activeSpaceId;
    this.status.syncMode = this.settings.syncMode;
    this.status.themeMode = this.settings.themeMode;
    this.status.language = this.settings.language;
  }

  registerTrustedDevice(
    deviceId: string,
    publicKeyPem: string,
    options?: {
      displayName?: string;
      platform?: DevicePlatform;
    }
  ): void {
    this.trustedKeys.set(deviceId, publicKeyPem);
    const previous = this.trustedDevices.get(deviceId);
    const now = new Date().toISOString();
    this.trustedDevices.set(deviceId, {
      deviceId,
      displayName: options?.displayName ?? previous?.displayName ?? deviceId,
      platform: options?.platform ?? previous?.platform ?? inferPlatformFromDeviceId(deviceId),
      trusted: true,
      publicKeyFingerprint: fingerprintPublicKey(publicKeyPem),
      pairedAt: previous?.pairedAt ?? now,
      lastSeenAt: previous?.lastSeenAt
    });
    this.status.trustedDeviceCount = this.countTrustedDevices();
  }

  listTrustedDevices(): TrustedDeviceProfile[] {
    return [...this.trustedDevices.values()].map((x) => ({ ...x }));
  }

  issueOneTimeInvite(options?: {
    ttlMs?: number;
    spaceId?: string;
    spaceName?: string;
  }): { inviteCode: string; expiresAt: string; spaceId: string } {
    const spaceId = options?.spaceId ?? this.activeSpaceId;
    const runtime = this.spaces.get(spaceId);
    if (!runtime) {
      throw new Error("space-not-found");
    }

    const ttlMs = options?.ttlMs ?? 5 * 60 * 1000;
    const expiresAtTs = Date.now() + Math.max(1_000, ttlMs);
    const expiresAt = new Date(expiresAtTs).toISOString();
    const inviteCode = workspaceKeyToInviteCode(runtime.workspaceKey, {
      workspaceIdHash: runtime.profile.workspaceIdHash,
      keyVersion: runtime.keyVersion,
      expiresAt,
      spaceName: options?.spaceName ?? runtime.profile.name
    });

    this.oneTimeInvites.set(inviteCode, {
      expiresAt: expiresAtTs,
      consumed: false,
      spaceId: runtime.profile.id
    });

    return {
      inviteCode,
      expiresAt,
      spaceId: runtime.profile.id
    };
  }

  consumeOneTimeInvite(inviteCode: string): {
    workspaceKey: Buffer;
    workspaceIdHash: string;
    keyVersion: number;
    spaceName?: string;
    expiresAt?: string;
  } {
    const state = this.oneTimeInvites.get(inviteCode);
    if (!state) {
      throw new Error("invite-not-issued");
    }
    if (state.consumed) {
      throw new Error("invite-already-consumed");
    }
    if (Date.now() > state.expiresAt) {
      throw new Error("invite-expired");
    }

    const parsed = parseInviteCode(inviteCode);
    state.consumed = true;
    this.oneTimeInvites.set(inviteCode, state);

    return {
      workspaceKey: parsed.workspaceKey,
      workspaceIdHash: parsed.workspaceIdHash,
      keyVersion: parsed.keyVersion,
      spaceName: parsed.spaceName,
      expiresAt: parsed.expiresAt
    };
  }

  requestPairingByInvite(input: {
    inviteCode: string;
    deviceId: string;
    publicKeyPem: string;
    displayName?: string;
    platform?: DevicePlatform;
  }):
    | {
        status: "pending";
        requestId: string;
      }
    | {
        status: "approved";
        requestId: string;
        workspaceKey: Buffer;
        workspaceIdHash: string;
        keyVersion: number;
      } {
    const inviteState = this.oneTimeInvites.get(input.inviteCode);
    if (!inviteState) {
      throw new Error("invite-not-issued");
    }
    if (inviteState.consumed) {
      throw new Error("invite-already-consumed");
    }
    if (Date.now() > inviteState.expiresAt) {
      throw new Error("invite-expired");
    }

    const parsed = parseInviteCode(input.inviteCode);
    const requestId = createHash("sha256")
      .update(`${input.deviceId}:${Date.now()}:${Math.random()}`)
      .digest("hex")
      .slice(0, 24);

    if (this.settings.pairingPolicy === "auto-approve-invite") {
      inviteState.consumed = true;
      this.oneTimeInvites.set(input.inviteCode, inviteState);
      this.registerTrustedDevice(input.deviceId, input.publicKeyPem, {
        displayName: input.displayName,
        platform: input.platform
      });
      return {
        status: "approved",
        requestId,
        workspaceKey: parsed.workspaceKey,
        workspaceIdHash: parsed.workspaceIdHash,
        keyVersion: parsed.keyVersion
      };
    }

    this.pairingRequests.set(requestId, {
      requestId,
      inviteCode: input.inviteCode,
      deviceId: input.deviceId,
      displayName: input.displayName ?? input.deviceId,
      platform: input.platform ?? inferPlatformFromDeviceId(input.deviceId),
      publicKeyFingerprint: fingerprintPublicKey(input.publicKeyPem),
      requestedAt: new Date().toISOString(),
      status: "pending",
      expiresAt: parsed.expiresAt,
      spaceId: inviteState.spaceId,
      publicKeyPem: input.publicKeyPem
    });
    this.status.pendingPairingCount = this.countPendingPairingRequests();
    return {
      status: "pending",
      requestId
    };
  }

  listPairingRequests(): PairingRequest[] {
    return [...this.pairingRequests.values()].map(({ publicKeyPem: _unused, ...item }) => ({ ...item }));
  }

  approvePairingRequest(requestId: string): {
    requestId: string;
    workspaceKey: Buffer;
    workspaceIdHash: string;
    keyVersion: number;
  } {
    const req = this.pairingRequests.get(requestId);
    if (!req) {
      throw new Error("pairing-request-not-found");
    }
    if (req.status !== "pending") {
      throw new Error("pairing-request-not-pending");
    }

    const inviteState = this.oneTimeInvites.get(req.inviteCode);
    if (!inviteState) {
      throw new Error("invite-not-issued");
    }
    if (inviteState.consumed) {
      throw new Error("invite-already-consumed");
    }
    if (Date.now() > inviteState.expiresAt) {
      throw new Error("invite-expired");
    }

    const parsed = parseInviteCode(req.inviteCode);
    inviteState.consumed = true;
    this.oneTimeInvites.set(req.inviteCode, inviteState);

    req.status = "approved";
    this.pairingRequests.set(requestId, req);
    this.status.pendingPairingCount = this.countPendingPairingRequests();

    this.registerTrustedDevice(req.deviceId, req.publicKeyPem, {
      displayName: req.displayName,
      platform: req.platform
    });

    return {
      requestId,
      workspaceKey: parsed.workspaceKey,
      workspaceIdHash: parsed.workspaceIdHash,
      keyVersion: parsed.keyVersion
    };
  }

  rejectPairingRequest(requestId: string): void {
    const req = this.pairingRequests.get(requestId);
    if (!req) {
      throw new Error("pairing-request-not-found");
    }
    if (req.status !== "pending") {
      return;
    }
    req.status = "rejected";
    this.pairingRequests.set(requestId, req);
    this.status.pendingPairingCount = this.countPendingPairingRequests();
  }

  revokeTrustedDevice(deviceId: string): void {
    if (deviceId === this.options.identity.deviceId) {
      throw new Error("cannot-revoke-local-device");
    }
    const current = this.trustedDevices.get(deviceId);
    if (!current) {
      return;
    }

    this.trustedKeys.delete(deviceId);
    this.trustedDevices.set(deviceId, {
      ...current,
      trusted: false
    });
    this.status.trustedDeviceCount = this.countTrustedDevices();
  }

  removeTrustedDevice(deviceId: string): void {
    if (deviceId === this.options.identity.deviceId) {
      throw new Error("cannot-remove-local-device");
    }
    this.trustedKeys.delete(deviceId);
    this.trustedDevices.delete(deviceId);
    this.status.trustedDeviceCount = this.countTrustedDevices();
  }

  getLocalDeviceProfile(): TrustedDeviceProfile | undefined {
    const profile = this.trustedDevices.get(this.options.identity.deviceId);
    return profile ? { ...profile } : undefined;
  }

  async start(): Promise<void> {
    await ensureCustomServerReady(this.options.customServer);
    await this.transport.connect();
    await this.transport.subscribe(this.currentSpace().profile.topic, async (envelope) => {
      await this.handleIncoming(envelope);
    });
    this.status.connected = true;
    this.status.webDevEnabled = this.options.webDev?.enabled ?? false;
    this.status.syncServerMode = this.options.customServer?.enabled ? "custom" : "default";
  }

  async stop(): Promise<void> {
    await this.transport.disconnect();
    this.status.connected = false;
  }

  async sendText(text: string, manualOverride = false): Promise<void> {
    await this.sendContent("text/plain", text, manualOverride);
  }

  async sendTextFromApp(sourceAppId: string, text: string, manualOverride = false): Promise<void> {
    if (this.isBlacklistedApp(sourceAppId)) {
      this.status.lastErrorMessage = `blacklisted-app:${sourceAppId}`;
      this.addHistory({
        direction: "out",
        contentType: "text/plain",
        success: false,
        preview: this.previewFor("text/plain", text),
        note: this.status.lastErrorMessage
      });
      throw new Error(this.status.lastErrorMessage);
    }

    await this.sendContent("text/plain", text, manualOverride);
  }

  async sendContent(contentType: ClipboardContentType, payload: string, manualOverride = false): Promise<void> {
    if (!this.settings.autoSyncEnabled && !manualOverride) {
      this.status.lastErrorMessage = "manual-mode-required";
      this.addHistory({
        direction: "out",
        contentType,
        success: false,
        preview: this.previewFor(contentType, payload),
        note: this.status.lastErrorMessage
      });
      throw new Error(this.status.lastErrorMessage);
    }

    if (!this.settings.sensitiveFilterEnabled) {
      // Keep engine policy in sync with settings at runtime.
      this.options.sensitivePolicy.enabled = false;
    } else {
      this.options.sensitivePolicy.enabled = true;
    }

    try {
      const runtime = this.currentSpace();
      const runtimeEngine = runtime.engine as any;
      const envelope = runtimeEngine.createEnvelopeWithContent(contentType, payload, manualOverride) as SyncEnvelope;
      runtime.engine.enqueue(envelope);
      await runtime.engine.flush(async (msg) => {
        await this.transport.publish(runtime.profile.topic, msg);
      });
      this.status.syncedOutCount += 1;
      this.status.lastErrorMessage = undefined;
      this.addHistory({
        direction: "out",
        contentType,
        success: true,
        preview: this.previewFor(contentType, payload),
        note: this.settings.syncMode === "manual" ? "manual" : "auto"
      });
    } catch (err) {
      this.status.lastErrorMessage = err instanceof Error ? err.message : "send-failed";
      this.addHistory({
        direction: "out",
        contentType,
        success: false,
        preview: this.previewFor(contentType, payload),
        note: this.status.lastErrorMessage
      });
      throw err;
    }
  }

  getSettings(): AppSettings {
    return { ...this.settings, blacklistApps: [...this.settings.blacklistApps] };
  }

  updateSettings(partial: Partial<AppSettings>): AppSettings {
    this.settings = {
      ...this.settings,
      ...partial,
      blacklistApps: partial.blacklistApps ? [...partial.blacklistApps] : this.settings.blacklistApps
    };
    this.status.syncMode = this.settings.syncMode;
    this.status.themeMode = this.settings.themeMode;
    this.status.language = this.settings.language;
    this.status.webDevEnabled = this.settings.webDevSyncEnabled;
    return this.getSettings();
  }

  getHistory(): HistoryRecord[] {
    return [...this.history];
  }

  listSpaces(): SpaceProfile[] {
    return [...this.spaces.values()].map((s) => ({ ...s.profile }));
  }

  async switchSpace(spaceId: string): Promise<void> {
    if (!this.spaces.has(spaceId)) {
      throw new Error("space-not-found");
    }
    this.activeSpaceId = spaceId;
    this.status.currentSpaceId = spaceId;
    if (this.status.connected) {
      await this.transport.subscribe(this.currentSpace().profile.topic, async (envelope) => {
        await this.handleIncoming(envelope);
      });
    }
  }

  async resetCurrentSpace(options?: {
    workspaceKey?: Buffer;
    workspaceIdHash?: string;
    keyVersion?: number;
    topic?: string;
    keepConnected?: boolean;
  }): Promise<{
    spaceId: string;
    workspaceIdHash: string;
    keyVersion: number;
    topic: string;
    workspaceKey: Buffer;
  }> {
    const runtime = this.currentSpace();
    const nextWorkspaceKey = options?.workspaceKey ?? generateWorkspaceKey();
    const nextWorkspaceIdHash = options?.workspaceIdHash ?? workspaceIdHash(nextWorkspaceKey);
    const nextKeyVersion = options?.keyVersion ?? runtime.keyVersion + 1;
    const nextTopic = options?.topic ?? `${runtime.profile.topic}.k${nextKeyVersion}`;

    runtime.workspaceKey = nextWorkspaceKey;
    runtime.keyVersion = nextKeyVersion;
    runtime.profile.workspaceIdHash = nextWorkspaceIdHash;
    runtime.profile.topic = nextTopic;
    runtime.engine = this.createEngineForSpace(nextWorkspaceKey, nextWorkspaceIdHash, nextKeyVersion);

    if (this.status.connected && (options?.keepConnected ?? true)) {
      await this.transport.subscribe(nextTopic, async (envelope) => {
        await this.handleIncoming(envelope);
      });
    }

    return {
      spaceId: runtime.profile.id,
      workspaceIdHash: nextWorkspaceIdHash,
      keyVersion: nextKeyVersion,
      topic: nextTopic,
      workspaceKey: nextWorkspaceKey
    };
  }

  getStatus(): SyncStatusSnapshot {
    return { ...this.status };
  }

  private async handleIncoming(envelope: SyncEnvelope): Promise<void> {
    const runtime = this.currentSpace();
    if (envelope.workspaceIdHash !== runtime.profile.workspaceIdHash) {
      this.status.rejectedEventCount += 1;
      this.status.lastErrorMessage = "workspace-mismatch";
      return;
    }

    const sender = this.trustedDevices.get(envelope.senderDeviceId);
    const senderKey = this.trustedKeys.get(envelope.senderDeviceId);
    if (!sender || !sender.trusted || !senderKey) {
      this.status.rejectedEventCount += 1;
      this.status.lastErrorMessage = "unknown-sender";
      return;
    }

    const result = runtime.engine.receive(envelope, runtime.workspaceKey, senderKey) as any;
    if (result.result === "applied" && result.payload !== undefined) {
      this.touchTrustedDevice(envelope.senderDeviceId);
      if (result.contentType === "text/plain") {
        await this.clipboardWriter.writeText(result.payload);
      }
      this.status.syncedInCount += 1;
      this.status.lastErrorMessage = undefined;
      this.addHistory({
        direction: "in",
        contentType: (result.contentType ?? "text/plain") as ClipboardContentType,
        success: true,
        preview: this.previewFor((result.contentType ?? "text/plain") as ClipboardContentType, result.payload)
      });
      return;
    }

    if (result.result === "rejected") {
      this.status.rejectedEventCount += 1;
      this.status.lastErrorMessage = "incoming-rejected";
      this.addHistory({
        direction: "in",
        contentType: (result.contentType ?? "text/plain") as ClipboardContentType,
        success: false,
        preview: "rejected",
        note: "incoming-rejected"
      });
    }
  }

  private createEngineForSpace(workspaceKey: Buffer, workspaceIdHash: string, keyVersion: number): SimulatorSyncEngine {
    return new SimulatorSyncEngine(
      this.options.identity.deviceId,
      this.options.identity.privateKeyPem,
      this.options.identity.publicKeyPem,
      workspaceKey,
      workspaceIdHash,
      keyVersion,
      this.options.retryPolicy,
      new InMemoryDedupWindow(this.options.dedupTtlMs ?? 24 * 60 * 60 * 1000),
      this.options.replayGuard,
      this.options.sensitivePolicy
    );
  }

  private isBlacklistedApp(sourceAppId: string): boolean {
    return this.settings.blacklistApps.some((x) => x.toLowerCase() === sourceAppId.toLowerCase());
  }

  private currentSpace(): SpaceRuntime {
    const runtime = this.spaces.get(this.activeSpaceId);
    if (!runtime) {
      throw new Error("active-space-missing");
    }
    return runtime;
  }

  private addHistory(entry: Omit<HistoryRecord, "id" | "at" | "spaceId">): void {
    this.history.unshift({
      id: `${Date.now()}-${Math.random().toString(16).slice(2)}`,
      at: new Date().toISOString(),
      spaceId: this.activeSpaceId,
      ...entry
    });
    if (this.history.length > this.maxHistoryItems) {
      this.history.length = this.maxHistoryItems;
    }
  }

  private touchTrustedDevice(deviceId: string): void {
    const current = this.trustedDevices.get(deviceId);
    if (!current) {
      return;
    }
    this.trustedDevices.set(deviceId, {
      ...current,
      lastSeenAt: new Date().toISOString()
    });
  }

  private previewFor(contentType: ClipboardContentType, payload: string): string {
    if (contentType === "text/plain" || contentType === "text/html") {
      return payload.slice(0, 64);
    }
    return `${contentType} (${payload.length} bytes)`;
  }

  private countTrustedDevices(): number {
    return [...this.trustedDevices.values()].filter((x) => x.trusted).length;
  }

  private countPendingPairingRequests(): number {
    return [...this.pairingRequests.values()].filter((x) => x.status === "pending").length;
  }

  private createCompatDefaultSpaces(options: AppCoreOptions): Array<{
    id: string;
    name: string;
    workspaceKey: Buffer;
    workspaceIdHash: string;
    keyVersion: number;
    topic: string;
  }> {
    if (!options.workspaceKey || !options.workspaceIdHash || !options.keyVersion || !options.topic) {
      return [];
    }
    return [
      {
        id: "default",
        name: "Default Workspace",
        workspaceKey: options.workspaceKey,
        workspaceIdHash: options.workspaceIdHash,
        keyVersion: options.keyVersion,
        topic: options.topic
      }
    ];
  }
}

function inferPlatformFromDeviceId(deviceId: string): DevicePlatform {
  const id = deviceId.toLowerCase();
  if (id.includes("win")) {
    return "windows";
  }
  if (id.includes("android")) {
    return "android";
  }
  if (id.includes("ios") || id.includes("iphone") || id.includes("ipad")) {
    return "ios";
  }
  if (id.includes("mac")) {
    return "macos";
  }
  if (id.includes("linux") || id.includes("ubuntu")) {
    return "linux";
  }
  if (id.includes("web") || id.includes("browser")) {
    return "web";
  }
  return "unknown";
}

function fingerprintPublicKey(publicKeyPem: string): string {
  const digest = createHash("sha256").update(publicKeyPem, "utf8").digest("hex");
  return digest.slice(0, 16);
}
