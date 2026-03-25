import { SyncEnvelope } from "../../protocol-spec/dist/types";

export type ClipboardContentType =
  | "text/plain"
  | "text/html"
  | "image/png"
  | "application/octet-stream"
  | "application/x-clipboard-file-ref";

export type SyncMode = "auto" | "manual";
export type ThemeMode = "system" | "light" | "dark";
export type LanguageCode = "zh-CN" | "en-US";
export type PairingPolicy = "manual-approve" | "auto-approve-invite";

export interface HistoryRecord {
  id: string;
  at: string;
  spaceId: string;
  direction: "in" | "out";
  contentType: ClipboardContentType;
  preview: string;
  success: boolean;
  note?: string;
}

export interface AppSettings {
  syncMode: SyncMode;
  themeMode: ThemeMode;
  language: LanguageCode;
  pairingPolicy: PairingPolicy;
  autoSyncEnabled: boolean;
  sensitiveFilterEnabled: boolean;
  blacklistApps: string[];
  webDevSyncEnabled: boolean;
  localServerEnabled: boolean;
}

export interface SpaceProfile {
  id: string;
  name: string;
  workspaceIdHash: string;
  topic: string;
}

export type DevicePlatform = "windows" | "android" | "ios" | "macos" | "linux" | "web" | "unknown";

export interface TrustedDeviceProfile {
  deviceId: string;
  displayName: string;
  platform: DevicePlatform;
  trusted: boolean;
  publicKeyFingerprint: string;
  pairedAt: string;
  lastSeenAt?: string;
}

export interface PairingRequest {
  requestId: string;
  inviteCode: string;
  deviceId: string;
  displayName: string;
  platform: DevicePlatform;
  publicKeyFingerprint: string;
  requestedAt: string;
  status: "pending" | "approved" | "rejected";
  expiresAt?: string;
  spaceId: string;
}

export interface ClipboardReader {
  readText(): Promise<string | null>;
}

export interface ClipboardWriter {
  writeText(text: string): Promise<void>;
}

export interface SecureStore {
  get(key: string): Promise<string | null>;
  set(key: string, value: string): Promise<void>;
  delete(key: string): Promise<void>;
}

export interface SyncTransport {
  connect(): Promise<void>;
  disconnect(): Promise<void>;
  subscribe(topic: string, handler: (msg: SyncEnvelope) => void): Promise<void>;
  publish(topic: string, msg: SyncEnvelope): Promise<void>;
}

export interface SyncStatusSnapshot {
  connected: boolean;
  syncedOutCount: number;
  syncedInCount: number;
  rejectedEventCount: number;
  trustedDeviceCount: number;
  pendingPairingCount: number;
  webDevEnabled: boolean;
  syncServerMode: "default" | "custom";
  currentSpaceId: string;
  syncMode: SyncMode;
  themeMode: ThemeMode;
  language: LanguageCode;
  lastErrorMessage?: string;
}
