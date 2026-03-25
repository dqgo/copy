import assert from "node:assert/strict";
import { generateDeviceIdentity, generateWorkspaceKey, workspaceIdHash } from "../../crypto-spec/dist/index";
import { InMemoryDedupWindow, SimulatorSyncEngine } from "./engine";

class FlakyPublisher {
  private count = 0;

  async publish<T>(value: T): Promise<T> {
    this.count += 1;
    if (this.count === 1) {
      throw new Error("simulated-network-drop");
    }
    return value;
  }
}

async function run(): Promise<void> {
  const wsKey = generateWorkspaceKey();
  const wsHash = workspaceIdHash(wsKey);
  const senderIdentity = generateDeviceIdentity("win-flaky-a");
  const receiverIdentity = generateDeviceIdentity("and-flaky-b");

  const sender = new SimulatorSyncEngine(
    senderIdentity.deviceId,
    senderIdentity.privateKeyPem,
    senderIdentity.publicKeyPem,
    wsKey,
    wsHash,
    1,
    {
      maxAttempts: 3,
      baseDelayMs: 5,
      maxDelayMs: 20,
      jitterRatio: 0
    },
    new InMemoryDedupWindow(60_000),
    {
      allowedClockSkewMs: 90_000,
      nonceTtlMs: 60_000
    },
    {
      enabled: true,
      allowManualOverride: true
    }
  );

  const receiver = new SimulatorSyncEngine(
    receiverIdentity.deviceId,
    receiverIdentity.privateKeyPem,
    receiverIdentity.publicKeyPem,
    wsKey,
    wsHash,
    1,
    {
      maxAttempts: 3,
      baseDelayMs: 5,
      maxDelayMs: 20,
      jitterRatio: 0
    },
    new InMemoryDedupWindow(60_000),
    {
      allowedClockSkewMs: 90_000,
      nonceTtlMs: 60_000
    },
    {
      enabled: true,
      allowManualOverride: true
    }
  );

  const flaky = new FlakyPublisher();
  const envelope = sender.createEnvelope("weak network payload");
  sender.enqueue(envelope);

  await sender.flush(async (msg) => {
    const accepted = await flaky.publish(msg);
    const result = receiver.receive(accepted, wsKey, sender.getSenderPublicKeyPem());
    assert.equal(result.result, "applied");
  });

  const events = sender.getEvents();
  const failed = events.find((e) => e.type === "failed");
  const published = events.find((e) => e.type === "published");
  assert.ok(failed);
  assert.ok(published);

  console.log("network recovery test passed");
}

run().catch((err) => {
  console.error("network recovery test failed", err);
  process.exitCode = 1;
});
