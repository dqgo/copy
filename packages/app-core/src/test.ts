import assert from "node:assert/strict";
import {
  buildWorkspaceKeyFormats,
  buildWorkspaceQrPayload,
  generateDeviceIdentity,
  generateWorkspaceKey,
  parseInviteCode,
  parseManualKey,
  parseRecoveryPhrase,
  parseWorkspaceQrPayload,
  workspaceIdHash
} from "../../crypto-spec/dist/index";
import { buildWorkspaceTopic, InMemoryBroker, InMemoryMqttClient } from "../../sync-simulator/dist/mqtt";
import { ClipboardSyncAppCore } from "./appCore";
import { ClipboardWriter } from "./contracts";
import { InMemoryLogger } from "./logger";

class MemoryClipboard implements ClipboardWriter {
  private value = "";

  async writeText(text: string): Promise<void> {
    this.value = text;
  }

  read(): string {
    return this.value;
  }
}

async function run(): Promise<void> {
  const workspaceKey = generateWorkspaceKey();
  const wsHash = workspaceIdHash(workspaceKey);
  const topic = buildWorkspaceTopic(wsHash, "coretest");

  const senderId = generateDeviceIdentity("win-test-a");
  const receiverId = generateDeviceIdentity("mac-test-b");

  const broker = new InMemoryBroker();
  const senderTransport = new InMemoryMqttClient(broker);
  const receiverTransport = new InMemoryMqttClient(broker);

  const senderClipboard = new MemoryClipboard();
  const receiverClipboard = new MemoryClipboard();

  const base = {
    workspaceKey,
    workspaceIdHash: wsHash,
    keyVersion: 1,
    topic,
    retryPolicy: {
      maxAttempts: 2,
      baseDelayMs: 10,
      maxDelayMs: 20,
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
      mode: "proxy" as const,
      assetBaseUrl: "http://localhost:5173"
    }
  } as const;

  const sender = new ClipboardSyncAppCore({ ...base, identity: senderId }, senderTransport, senderClipboard);
  const receiver = new ClipboardSyncAppCore({ ...base, identity: receiverId }, receiverTransport, receiverClipboard);

  receiver.registerTrustedDevice(senderId.deviceId, senderId.publicKeyPem);
  sender.registerTrustedDevice(receiverId.deviceId, receiverId.publicKeyPem);

  await sender.start();
  await receiver.start();

  let manualBlocked = false;
  try {
    await sender.sendText("should fail in manual mode");
  } catch {
    manualBlocked = true;
  }
  assert.equal(manualBlocked, true);

  await sender.sendText("core test payload", true);
  await new Promise((resolve) => setTimeout(resolve, 10));
  assert.equal(receiverClipboard.read(), "core test payload");

  const senderStatus = sender.getStatus();
  const receiverStatus = receiver.getStatus();
  assert.equal(senderStatus.syncedOutCount, 1);
  assert.equal(receiverStatus.syncedInCount, 1);
  assert.equal(senderStatus.trustedDeviceCount, 2);
  assert.equal(receiverStatus.trustedDeviceCount, 2);
  assert.equal(senderStatus.webDevEnabled, true);
  assert.equal(senderStatus.syncServerMode, "default");

  const localProfile = sender.getLocalDeviceProfile();
  assert.equal(localProfile?.deviceId, senderId.deviceId);
  assert.equal(localProfile?.trusted, true);

  const trustedDevices = sender.listTrustedDevices();
  const remote = trustedDevices.find((x) => x.deviceId === receiverId.deviceId);
  assert.equal(Boolean(remote), true);
  assert.equal(remote?.platform, "macos");

  const receiverClipboardBeforeRevoke = receiverClipboard.read();
  receiver.revokeTrustedDevice(senderId.deviceId);
  assert.equal(receiver.getStatus().trustedDeviceCount, 1);

  await sender.sendText("blocked after revoke", true);
  await new Promise((resolve) => setTimeout(resolve, 10));
  assert.equal(receiverClipboard.read(), receiverClipboardBeforeRevoke);
  assert.equal(receiver.getStatus().lastErrorMessage, "unknown-sender");

  receiver.registerTrustedDevice(senderId.deviceId, senderId.publicKeyPem);
  assert.equal(receiver.getStatus().trustedDeviceCount, 2);

  await sender.sendText("restored after re-pair", true);
  await new Promise((resolve) => setTimeout(resolve, 10));
  assert.equal(receiverClipboard.read(), "restored after re-pair");

  sender.removeTrustedDevice(receiverId.deviceId);
  assert.equal(sender.getStatus().trustedDeviceCount, 1);
  assert.equal(sender.listTrustedDevices().some((x) => x.deviceId === receiverId.deviceId), false);
  sender.registerTrustedDevice(receiverId.deviceId, receiverId.publicKeyPem);
  assert.equal(sender.getStatus().trustedDeviceCount, 2);

  const reset = await sender.resetCurrentSpace();
  await receiver.resetCurrentSpace({
    workspaceKey: reset.workspaceKey,
    workspaceIdHash: reset.workspaceIdHash,
    keyVersion: reset.keyVersion,
    topic: reset.topic
  });
  await sender.sendText("payload after space reset", true);
  await new Promise((resolve) => setTimeout(resolve, 10));
  assert.equal(receiverClipboard.read(), "payload after space reset");

  sender.updateSettings({
    themeMode: "dark",
    language: "en-US",
    syncMode: "auto",
    autoSyncEnabled: true,
    blacklistApps: ["com.bank.app"],
    webDevSyncEnabled: true
  });
  const updated = sender.getSettings();
  assert.equal(updated.themeMode, "dark");
  assert.equal(updated.language, "en-US");
  assert.equal(updated.syncMode, "auto");

  await sender.sendContent("text/html", "<b>hello</b>");
  const history = sender.getHistory();
  assert.equal(history.length > 0, true);

  let blacklistedBlocked = false;
  try {
    await sender.sendTextFromApp("com.bank.app", "bank token");
  } catch {
    blacklistedBlocked = true;
  }
  assert.equal(blacklistedBlocked, true);

  let sensitiveBlocked = false;
  try {
    await sender.sendText("password=abc123");
  } catch {
    sensitiveBlocked = true;
  }
  assert.equal(sensitiveBlocked, true);

  const logger = new InMemoryLogger();
  logger.log({
    level: "info",
    message: "password=abc123",
    metadata: {
      payload: "plain text should never be logged",
      workspaceKey: "super-secret"
    }
  });

  const logged = logger.all()[0];
  assert.equal(logged.metadata?.payload, "[REDACTED]");
  assert.equal(logged.metadata?.workspaceKey, "[REDACTED]");
  assert.equal(logged.message.includes("abc123"), false);

  const keyFormats = buildWorkspaceKeyFormats(workspaceKey);
  const parsedManual = parseManualKey(keyFormats.manualKey);
  assert.equal(parsedManual.equals(workspaceKey), true);
  const parsedRecovery = parseRecoveryPhrase(keyFormats.recoveryPhrase);
  assert.equal(parsedRecovery.equals(workspaceKey), true);
  const parsedInvite = parseInviteCode(keyFormats.inviteCode);
  assert.equal(parsedInvite.workspaceKey.equals(workspaceKey), true);

  const qrPayload = buildWorkspaceQrPayload(workspaceKey, {
    workspaceIdHash: wsHash,
    keyVersion: 1,
    spaceName: "default"
  });
  const parsedQr = parseWorkspaceQrPayload(qrPayload);
  assert.equal(parsedQr.workspaceKey.equals(workspaceKey), true);
  assert.equal(parsedQr.workspaceIdHash, wsHash);

  const issued = sender.issueOneTimeInvite({ ttlMs: 60_000, spaceName: "default" });
  const consumed = sender.consumeOneTimeInvite(issued.inviteCode);
  assert.equal(consumed.workspaceIdHash, reset.workspaceIdHash);

  let inviteBlockedAfterConsume = false;
  try {
    sender.consumeOneTimeInvite(issued.inviteCode);
  } catch {
    inviteBlockedAfterConsume = true;
  }
  assert.equal(inviteBlockedAfterConsume, true);

  const pendingDevice = generateDeviceIdentity("android-pending-c");
  const pendingInvite = sender.issueOneTimeInvite({ ttlMs: 60_000 });
  const pendingResult = sender.requestPairingByInvite({
    inviteCode: pendingInvite.inviteCode,
    deviceId: pendingDevice.deviceId,
    publicKeyPem: pendingDevice.publicKeyPem,
    displayName: "Pending Device",
    platform: "android"
  });
  assert.equal(pendingResult.status, "pending");
  assert.equal(sender.getStatus().pendingPairingCount, 1);
  assert.equal(sender.getStatus().trustedDeviceCount, 2);

  const approved = sender.approvePairingRequest(pendingResult.requestId);
  assert.equal(approved.workspaceIdHash, reset.workspaceIdHash);
  assert.equal(sender.getStatus().pendingPairingCount, 0);
  assert.equal(sender.getStatus().trustedDeviceCount, 3);
  assert.equal(sender.listTrustedDevices().some((x) => x.deviceId === pendingDevice.deviceId), true);

  const rejectedDevice = generateDeviceIdentity("ios-reject-d");
  const rejectedInvite = sender.issueOneTimeInvite({ ttlMs: 60_000 });
  const rejectedResult = sender.requestPairingByInvite({
    inviteCode: rejectedInvite.inviteCode,
    deviceId: rejectedDevice.deviceId,
    publicKeyPem: rejectedDevice.publicKeyPem,
    platform: "ios"
  });
  assert.equal(rejectedResult.status, "pending");
  sender.rejectPairingRequest(rejectedResult.requestId);
  assert.equal(sender.getStatus().pendingPairingCount, 0);

  sender.updateSettings({ pairingPolicy: "auto-approve-invite" });
  const autoDevice = generateDeviceIdentity("linux-auto-e");
  const autoInvite = sender.issueOneTimeInvite({ ttlMs: 60_000 });
  const autoResult = sender.requestPairingByInvite({
    inviteCode: autoInvite.inviteCode,
    deviceId: autoDevice.deviceId,
    publicKeyPem: autoDevice.publicKeyPem,
    platform: "linux"
  });
  assert.equal(autoResult.status, "approved");
  assert.equal(sender.getStatus().trustedDeviceCount, 4);
  assert.equal(sender.listTrustedDevices().some((x) => x.deviceId === autoDevice.deviceId), true);

  let autoInviteReuseBlocked = false;
  try {
    sender.requestPairingByInvite({
      inviteCode: autoInvite.inviteCode,
      deviceId: "web-reuse-f",
      publicKeyPem: autoDevice.publicKeyPem,
      platform: "web"
    });
  } catch {
    autoInviteReuseBlocked = true;
  }
  assert.equal(autoInviteReuseBlocked, true);

  const originalFetch = globalThis.fetch;
  let startCalled = false;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit): Promise<Response> => {
    const url = typeof input === "string" ? input : input.toString();
    if (url.endsWith("/health")) {
      return new Response(JSON.stringify({ ok: true, serviceRunning: startCalled }), { status: 200 });
    }
    if (url.endsWith("/start-service") && init?.method === "POST") {
      startCalled = true;
      return new Response(JSON.stringify({ ok: true, serviceRunning: true }), { status: 200 });
    }
    return new Response(JSON.stringify({ ok: false }), { status: 404 });
  }) as typeof fetch;

  const customSender = new ClipboardSyncAppCore(
    {
      ...base,
      identity: senderId,
      customServer: {
        enabled: true,
        baseUrl: "http://custom-sync.local",
        autoStartService: true
      }
    },
    senderTransport,
    senderClipboard
  );

  await customSender.start();
  const customStatus = customSender.getStatus();
  assert.equal(customStatus.syncServerMode, "custom");
  assert.equal(startCalled, true);
  await customSender.stop();

  globalThis.fetch = originalFetch;

  await sender.stop();
  await receiver.stop();

  console.log("app-core tests passed");
}

run().catch((err) => {
  console.error("app-core tests failed", err);
  process.exitCode = 1;
});
