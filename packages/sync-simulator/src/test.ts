import assert from "node:assert/strict";
import { generateDeviceIdentity, generateWorkspaceKey, workspaceIdHash } from "../../crypto-spec/dist/index";
import { InMemoryDedupWindow, SimulatorSyncEngine } from "./engine";
import { PairingManager } from "./pairing";

function createEngine(deviceId: string, privateKeyPem: string, publicKeyPem: string, workspaceKey: Buffer, hash: string): SimulatorSyncEngine {
  return new SimulatorSyncEngine(
    deviceId,
    privateKeyPem,
    publicKeyPem,
    workspaceKey,
    hash,
    1,
    {
      maxAttempts: 2,
      baseDelayMs: 10,
      maxDelayMs: 20,
      jitterRatio: 0
    },
    new InMemoryDedupWindow(60 * 1000),
    {
      allowedClockSkewMs: 90 * 1000,
      nonceTtlMs: 60 * 1000
    },
    {
      enabled: true,
      allowManualOverride: true
    }
  );
}

async function run(): Promise<void> {
  const workspaceKey = generateWorkspaceKey();
  const hash = workspaceIdHash(workspaceKey);

  const a = generateDeviceIdentity("win-a");
  const b = generateDeviceIdentity("and-b");

  const sender = createEngine(a.deviceId, a.privateKeyPem, a.publicKeyPem, workspaceKey, hash);
  const receiver = createEngine(b.deviceId, b.privateKeyPem, b.publicKeyPem, workspaceKey, hash);

  const envelope = sender.createEnvelope("hello");
  const first = receiver.receive(envelope, workspaceKey, sender.getSenderPublicKeyPem());
  assert.equal(first.result, "applied");
  assert.equal(first.text, "hello");

  const replay = receiver.receive(envelope, workspaceKey, sender.getSenderPublicKeyPem());
  assert.equal(replay.result, "rejected");

  let blocked = false;
  try {
    sender.createEnvelope("验证码 123456");
  } catch {
    blocked = true;
  }
  assert.equal(blocked, true);

  const pmA = new PairingManager({
    workspaceIdHash: hash,
    localDeviceId: a.deviceId,
    localPrivateKeyPem: a.privateKeyPem,
    localPublicKeyPem: a.publicKeyPem,
    keyVersion: 1
  });

  const req = pmA.createPairingRequest(b.deviceId, b.publicKeyPem, b.privateKeyPem);
  const approval = pmA.approveRequest(req, true);
  assert.equal(approval.approved, true);

  const revocation = pmA.revokeDevice(b.deviceId, "manual_remove");
  assert.equal(revocation.nextKeyVersion, 2);

  console.log("simulator tests passed");
}

run().catch((err) => {
  console.error("simulator tests failed", err);
  process.exitCode = 1;
});
