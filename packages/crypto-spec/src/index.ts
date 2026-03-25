import {
  createCipheriv,
  createDecipheriv,
  createHash,
  generateKeyPairSync,
  hkdfSync,
  randomBytes,
  sign,
  verify
} from "node:crypto";

export interface DeviceIdentity {
  deviceId: string;
  publicKeyPem: string;
  privateKeyPem: string;
}

export interface CipherBundle {
  ciphertextB64: string;
  nonceB64: string;
  authTagB64: string;
}

export interface WorkspaceInvitePayload {
  scheme: "clipboard-sync";
  version: 1;
  workspaceIdHash: string;
  keyVersion: number;
  workspaceKeyB64Url: string;
  expiresAt?: string;
  spaceName?: string;
}

export interface WorkspaceKeyFormats {
  inviteCode: string;
  manualKey: string;
  recoveryPhrase: string;
}

export function generateWorkspaceKey(): Buffer {
  return randomBytes(32);
}

export function workspaceIdHash(workspaceKey: Buffer): string {
  return createHash("sha256").update(workspaceKey).digest("hex").slice(0, 32);
}

export function generateDeviceIdentity(deviceId: string): DeviceIdentity {
  const keys = generateKeyPairSync("ed25519");
  const publicKeyPem = keys.publicKey.export({ type: "spki", format: "pem" }).toString();
  const privateKeyPem = keys.privateKey.export({ type: "pkcs8", format: "pem" }).toString();

  return {
    deviceId,
    publicKeyPem,
    privateKeyPem
  };
}

export function deriveMessageKey(workspaceKey: Buffer, senderDeviceId: string, sequence: number): Buffer {
  const info = Buffer.from(`clipboard/v1/${senderDeviceId}/${sequence}`, "utf8");
  const key = hkdfSync("sha256", workspaceKey, Buffer.alloc(0), info, 32);
  return Buffer.from(key);
}

export function encryptText(plainText: string, messageKey: Buffer): CipherBundle {
  const nonce = randomBytes(12);
  const cipher = createCipheriv("aes-256-gcm", messageKey, nonce);
  const encrypted = Buffer.concat([cipher.update(plainText, "utf8"), cipher.final()]);
  const authTag = cipher.getAuthTag();

  return {
    ciphertextB64: encrypted.toString("base64"),
    nonceB64: nonce.toString("base64"),
    authTagB64: authTag.toString("base64")
  };
}

export function decryptText(bundle: CipherBundle, messageKey: Buffer): string {
  const nonce = Buffer.from(bundle.nonceB64, "base64");
  const ciphertext = Buffer.from(bundle.ciphertextB64, "base64");
  const authTag = Buffer.from(bundle.authTagB64, "base64");
  const decipher = createDecipheriv("aes-256-gcm", messageKey, nonce);

  decipher.setAuthTag(authTag);

  const plain = Buffer.concat([decipher.update(ciphertext), decipher.final()]);
  return plain.toString("utf8");
}

export function signPayload(payload: string, privateKeyPem: string): string {
  const signature = sign(null, Buffer.from(payload, "utf8"), privateKeyPem);
  return signature.toString("base64");
}

export function verifyPayload(payload: string, signatureB64: string, publicKeyPem: string): boolean {
  return verify(null, Buffer.from(payload, "utf8"), publicKeyPem, Buffer.from(signatureB64, "base64"));
}

export function workspaceKeyToManualKey(workspaceKey: Buffer): string {
  const hex = workspaceKey.toString("hex").toUpperCase();
  return hex.match(/.{1,4}/g)?.join("-") ?? hex;
}

export function parseManualKey(manualKey: string): Buffer {
  const normalized = manualKey.replace(/[^0-9A-Fa-f]/g, "").toLowerCase();
  if (normalized.length !== 64) {
    throw new Error("invalid-manual-key-length");
  }
  if (!/^[0-9a-f]{64}$/.test(normalized)) {
    throw new Error("invalid-manual-key-format");
  }
  return Buffer.from(normalized, "hex");
}

export function workspaceKeyToRecoveryPhrase(workspaceKey: Buffer): string {
  const b64url = toBase64Url(workspaceKey);
  return b64url.match(/.{1,4}/g)?.join(" ") ?? b64url;
}

export function parseRecoveryPhrase(recoveryPhrase: string): Buffer {
  const normalized = recoveryPhrase.replace(/\s+/g, "");
  const key = fromBase64Url(normalized);
  if (key.length !== 32) {
    throw new Error("invalid-recovery-phrase");
  }
  return key;
}

export function workspaceKeyToInviteCode(
  workspaceKey: Buffer,
  options?: {
    workspaceIdHash?: string;
    keyVersion?: number;
    expiresAt?: string;
    spaceName?: string;
  }
): string {
  const payload: WorkspaceInvitePayload = {
    scheme: "clipboard-sync",
    version: 1,
    workspaceIdHash: options?.workspaceIdHash ?? workspaceIdHash(workspaceKey),
    keyVersion: options?.keyVersion ?? 1,
    workspaceKeyB64Url: toBase64Url(workspaceKey),
    expiresAt: options?.expiresAt,
    spaceName: options?.spaceName
  };
  return `CS1.${toBase64Url(Buffer.from(JSON.stringify(payload), "utf8"))}`;
}

export function parseInviteCode(inviteCode: string): WorkspaceInvitePayload & { workspaceKey: Buffer } {
  if (!inviteCode.startsWith("CS1.")) {
    throw new Error("invalid-invite-prefix");
  }
  const raw = inviteCode.slice(4);
  const payload = JSON.parse(fromBase64Url(raw).toString("utf8")) as WorkspaceInvitePayload;
  if (payload.scheme !== "clipboard-sync" || payload.version !== 1) {
    throw new Error("invalid-invite-payload");
  }
  const key = fromBase64Url(payload.workspaceKeyB64Url);
  if (key.length !== 32) {
    throw new Error("invalid-invite-key");
  }
  const expectedHash = workspaceIdHash(key);
  if (payload.workspaceIdHash !== expectedHash) {
    throw new Error("workspace-hash-mismatch");
  }
  return {
    ...payload,
    workspaceKey: key
  };
}

export function buildWorkspaceQrPayload(
  workspaceKey: Buffer,
  options?: {
    workspaceIdHash?: string;
    keyVersion?: number;
    expiresAt?: string;
    spaceName?: string;
  }
): string {
  return JSON.stringify({
    scheme: "clipboard-sync",
    version: 1,
    workspaceIdHash: options?.workspaceIdHash ?? workspaceIdHash(workspaceKey),
    keyVersion: options?.keyVersion ?? 1,
    workspaceKeyB64Url: toBase64Url(workspaceKey),
    expiresAt: options?.expiresAt,
    spaceName: options?.spaceName
  } as WorkspaceInvitePayload);
}

export function parseWorkspaceQrPayload(qrPayload: string): WorkspaceInvitePayload & { workspaceKey: Buffer } {
  const payload = JSON.parse(qrPayload) as WorkspaceInvitePayload;
  if (payload.scheme !== "clipboard-sync" || payload.version !== 1) {
    throw new Error("invalid-qr-payload");
  }
  const key = fromBase64Url(payload.workspaceKeyB64Url);
  if (key.length !== 32) {
    throw new Error("invalid-qr-key");
  }
  return {
    ...payload,
    workspaceKey: key
  };
}

export function buildWorkspaceKeyFormats(workspaceKey: Buffer): WorkspaceKeyFormats {
  return {
    inviteCode: workspaceKeyToInviteCode(workspaceKey),
    manualKey: workspaceKeyToManualKey(workspaceKey),
    recoveryPhrase: workspaceKeyToRecoveryPhrase(workspaceKey)
  };
}

function toBase64Url(value: Buffer): string {
  return value
    .toString("base64")
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/g, "");
}

function fromBase64Url(value: string): Buffer {
  const pad = value.length % 4 === 0 ? "" : "=".repeat(4 - (value.length % 4));
  const normalized = value.replace(/-/g, "+").replace(/_/g, "/") + pad;
  return Buffer.from(normalized, "base64");
}
