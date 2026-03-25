import assert from "node:assert/strict";
import { generateDeviceIdentity, generateWorkspaceKey, workspaceIdHash } from "../../crypto-spec/dist/index";
import { buildWorkspaceTopic, InMemoryBroker, InMemoryMqttClient } from "../../sync-simulator/dist/mqtt";
import { ClipboardSyncAppCore } from "./appCore";
import { ClipboardWriter } from "./contracts";

class MemoryClipboard implements ClipboardWriter {
  private value = "";

  async writeText(text: string): Promise<void> {
    this.value = text;
  }

  read(): string {
    return this.value;
  }
}

async function main(): Promise<void> {
  const workspaceKey = generateWorkspaceKey();
  const wsHash = workspaceIdHash(workspaceKey);
  const topic = buildWorkspaceTopic(wsHash, "coreabcd");

  const a = generateDeviceIdentity("win-core-a");
  const b = generateDeviceIdentity("and-core-b");

  const broker = new InMemoryBroker();
  const aTransport = new InMemoryMqttClient(broker);
  const bTransport = new InMemoryMqttClient(broker);

  const clipA = new MemoryClipboard();
  const clipB = new MemoryClipboard();

  const common = {
    workspaceKey,
    workspaceIdHash: wsHash,
    keyVersion: 1,
    topic,
    retryPolicy: {
      maxAttempts: 3,
      baseDelayMs: 20,
      maxDelayMs: 60,
      jitterRatio: 0
    },
    replayGuard: {
      allowedClockSkewMs: 90_000,
      nonceTtlMs: 60_000
    },
    sensitivePolicy: {
      enabled: true,
      allowManualOverride: true
    },
    webDev: {
      enabled: true,
      mode: "embedded" as const,
      assetBaseUrl: "http://localhost:5173"
    }
  } as const;

  const appA = new ClipboardSyncAppCore({ ...common, identity: a }, aTransport, clipA);
  const appB = new ClipboardSyncAppCore({ ...common, identity: b }, bTransport, clipB);

  appA.updateSettings({ pairingPolicy: "manual-approve", autoSyncEnabled: true });
  appB.updateSettings({ autoSyncEnabled: true });

  const invite = appA.issueOneTimeInvite({ ttlMs: 60_000, spaceName: "demo-space" });
  const request = appA.requestPairingByInvite({
    inviteCode: invite.inviteCode,
    deviceId: b.deviceId,
    publicKeyPem: b.publicKeyPem,
    displayName: "Android Demo Device",
    platform: "android"
  });
  assert.equal(request.status, "pending");
  const approved = appA.approvePairingRequest(request.requestId);
  assert.equal(approved.workspaceIdHash, wsHash);

  appB.registerTrustedDevice(a.deviceId, a.publicKeyPem);

  await appA.start();
  await appB.start();

  await appA.sendText("from app core", true);
  await new Promise((resolve) => setTimeout(resolve, 10));

  assert.equal(clipB.read(), "from app core");

  let blocked = false;
  try {
    await appA.sendText("验证码 123456");
  } catch {
    blocked = true;
  }
  assert.equal(blocked, true);

  await appA.stop();
  await appB.stop();

  console.log("app-core demo passed");
}

main().catch((err) => {
  console.error("app-core demo failed", err);
  process.exitCode = 1;
});
