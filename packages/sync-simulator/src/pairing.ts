import { createHash } from "node:crypto";
import {
  DeviceRecord,
  DeviceRevocationEvent,
  DeviceTrustState,
  PairingApproval,
  PairingRequest,
  PlatformType
} from "../../protocol-spec/dist/types";
import { signPayload, verifyPayload } from "../../crypto-spec/dist/index";

export interface PairingManagerConfig {
  workspaceIdHash: string;
  localDeviceId: string;
  localPrivateKeyPem: string;
  localPublicKeyPem: string;
  keyVersion: number;
}

export class PairingManager {
  private readonly devices = new Map<string, DeviceRecord>();
  private keyVersion: number;

  constructor(private readonly cfg: PairingManagerConfig) {
    this.keyVersion = cfg.keyVersion;
    this.devices.set(cfg.localDeviceId, {
      deviceId: cfg.localDeviceId,
      deviceName: cfg.localDeviceId,
      platform: inferPlatform(cfg.localDeviceId),
      trusted: true,
      trustState: "trusted",
      keyVersion: cfg.keyVersion,
      lastSeenAt: new Date().toISOString(),
      publicKey: cfg.localPublicKeyPem
    });
  }

  createPairingRequest(targetDeviceId: string, requesterPublicKey: string, requesterPrivateKeyPem: string): PairingRequest {
    const timestamp = new Date().toISOString();
    const nonce = createHash("sha256").update(`${targetDeviceId}:${timestamp}`).digest("hex").slice(0, 32);
    const payload = `${this.cfg.workspaceIdHash}.${targetDeviceId}.${this.cfg.localDeviceId}.${nonce}.${timestamp}`;

    return {
      workspaceIdHash: this.cfg.workspaceIdHash,
      targetDeviceId,
      requesterDeviceId: this.cfg.localDeviceId,
      requesterPublicKey,
      requesterPlatform: inferPlatform(this.cfg.localDeviceId),
      nonce,
      timestamp,
      signature: signPayload(payload, requesterPrivateKeyPem)
    };
  }

  approveRequest(request: PairingRequest, approved: boolean): PairingApproval {
    const payload = `${request.workspaceIdHash}.${request.targetDeviceId}.${request.requesterDeviceId}.${request.nonce}.${request.timestamp}`;
    const valid = verifyPayload(payload, request.signature, request.requesterPublicKey);
    if (!valid) {
      throw new Error("invalid pairing signature");
    }

    if (approved) {
      this.devices.set(request.requesterDeviceId, {
        deviceId: request.requesterDeviceId,
        deviceName: request.requesterDeviceId,
        platform: request.requesterPlatform,
        trusted: true,
        trustState: "trusted",
        keyVersion: this.keyVersion,
        lastSeenAt: new Date().toISOString(),
        publicKey: request.requesterPublicKey
      });
    }

    const ts = new Date().toISOString();
    const approvalPayload = `${this.cfg.workspaceIdHash}.${request.requesterDeviceId}.${approved}.${this.keyVersion}.${ts}`;

    return {
      workspaceIdHash: this.cfg.workspaceIdHash,
      targetDeviceId: request.requesterDeviceId,
      approverDeviceId: this.cfg.localDeviceId,
      approved,
      keyVersion: this.keyVersion,
      timestamp: ts,
      signature: signPayload(approvalPayload, this.cfg.localPrivateKeyPem)
    };
  }

  revokeDevice(revokedDeviceId: string, reason: DeviceRevocationEvent["reason"]): DeviceRevocationEvent {
    this.keyVersion += 1;
    const revoked = this.devices.get(revokedDeviceId);
    if (revoked) {
      revoked.trustState = "revoked";
      revoked.trusted = false;
      revoked.keyVersion = this.keyVersion;
      revoked.lastSeenAt = new Date().toISOString();
    }

    const ts = new Date().toISOString();
    const payload = `${this.cfg.workspaceIdHash}.${revokedDeviceId}.${this.cfg.localDeviceId}.${this.keyVersion}.${reason}.${ts}`;

    return {
      workspaceIdHash: this.cfg.workspaceIdHash,
      revokedDeviceId,
      revokedByDeviceId: this.cfg.localDeviceId,
      nextKeyVersion: this.keyVersion,
      reason,
      timestamp: ts,
      signature: signPayload(payload, this.cfg.localPrivateKeyPem)
    };
  }

  applyRevocation(event: DeviceRevocationEvent, signerPublicKeyPem: string): void {
    const payload = `${event.workspaceIdHash}.${event.revokedDeviceId}.${event.revokedByDeviceId}.${event.nextKeyVersion}.${event.reason}.${event.timestamp}`;
    if (!verifyPayload(payload, event.signature, signerPublicKeyPem)) {
      throw new Error("invalid revocation signature");
    }

    const record = this.devices.get(event.revokedDeviceId);
    if (record) {
      record.trustState = "revoked";
      record.trusted = false;
      record.keyVersion = event.nextKeyVersion;
      record.lastSeenAt = new Date().toISOString();
    }

    this.keyVersion = Math.max(this.keyVersion, event.nextKeyVersion);
  }

  getKeyVersion(): number {
    return this.keyVersion;
  }

  listDevicesByState(state: DeviceTrustState): DeviceRecord[] {
    return [...this.devices.values()].filter((d) => d.trustState === state);
  }

  upsertDevice(record: DeviceRecord): void {
    this.devices.set(record.deviceId, record);
  }

  snapshot(): DeviceRecord[] {
    return [...this.devices.values()];
  }
}

function inferPlatform(deviceId: string): PlatformType {
  if (deviceId.startsWith("win-")) {
    return "windows";
  }
  if (deviceId.startsWith("and-")) {
    return "android";
  }
  if (deviceId.startsWith("ios-")) {
    return "ios";
  }
  return "macos";
}
