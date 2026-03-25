export type ProtocolVersion = "1.0";

export type ContentType =
  | "text/plain"
  | "text/html"
  | "image/png"
  | "application/octet-stream"
  | "application/x-clipboard-file-ref";

export type PlatformType = "windows" | "android" | "ios" | "macos";

export type DeviceTrustState =
  | "uninitialized"
  | "join_pending"
  | "trusted"
  | "syncing"
  | "degraded"
  | "revoked"
  | "recovery_required";

export interface SyncEnvelope {
  protocolVersion: ProtocolVersion;
  messageId: string;
  senderDeviceId: string;
  workspaceIdHash: string;
  timestamp: string;
  sequence: number;
  keyVersion: number;
  contentType: ContentType;
  encryptedPayload: string;
  nonce: string;
  signature: string;
  clientVersion: string;
  flags?: string[];
}

export type ReceiveResult = "applied" | "duplicate" | "rejected";

export interface RetryPolicy {
  maxAttempts: number;
  baseDelayMs: number;
  maxDelayMs: number;
  jitterRatio: number;
}

export interface PairingRequest {
  workspaceIdHash: string;
  targetDeviceId: string;
  requesterDeviceId: string;
  requesterPublicKey: string;
  requesterPlatform: PlatformType;
  nonce: string;
  timestamp: string;
  signature: string;
}

export interface PairingApproval {
  workspaceIdHash: string;
  targetDeviceId: string;
  approverDeviceId: string;
  approved: boolean;
  keyVersion: number;
  timestamp: string;
  signature: string;
}

export interface DeviceRevocationEvent {
  workspaceIdHash: string;
  revokedDeviceId: string;
  revokedByDeviceId: string;
  nextKeyVersion: number;
  reason: "device_lost" | "manual_remove" | "policy_violation";
  timestamp: string;
  signature: string;
}

export interface DeviceRecord {
  deviceId: string;
  deviceName: string;
  platform: PlatformType;
  trusted: boolean;
  trustState: DeviceTrustState;
  keyVersion: number;
  lastSeenAt: string;
  publicKey: string;
}

export interface ReplayGuardConfig {
  allowedClockSkewMs: number;
  nonceTtlMs: number;
}

export const ERROR_CODES = {
  AUTH_INVALID_SIGNATURE: "AUTH_INVALID_SIGNATURE",
  AUTH_REVOKED_DEVICE: "AUTH_REVOKED_DEVICE",
  AUTH_KEY_VERSION_MISMATCH: "AUTH_KEY_VERSION_MISMATCH",
  AUTH_REPLAY_DETECTED: "AUTH_REPLAY_DETECTED",
  SYNC_DUPLICATE_MESSAGE: "SYNC_DUPLICATE_MESSAGE",
  SYNC_DECRYPT_FAILED: "SYNC_DECRYPT_FAILED",
  SYNC_INVALID_SEQUENCE: "SYNC_INVALID_SEQUENCE",
  NET_BROKER_UNAVAILABLE: "NET_BROKER_UNAVAILABLE",
  NET_RETRY_EXHAUSTED: "NET_RETRY_EXHAUSTED",
  POLICY_SENSITIVE_CONTENT_BLOCKED: "POLICY_SENSITIVE_CONTENT_BLOCKED"
} as const;

export type ErrorCode = (typeof ERROR_CODES)[keyof typeof ERROR_CODES];
