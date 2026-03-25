import { generateDeviceIdentity, generateWorkspaceKey, workspaceIdHash } from "../../crypto-spec/dist/index";
import { InMemoryDedupWindow, SimulatorSyncEngine } from "./engine";
import { buildWorkspaceTopic, InMemoryBroker, InMemoryMqttClient } from "./mqtt";

async function main(): Promise<void> {
  const workspaceKey = generateWorkspaceKey();
  const spaceHash = workspaceIdHash(workspaceKey);

  const senderIdentity = generateDeviceIdentity("win-dev-a1b2c3d4");
  const receiverIdentity = generateDeviceIdentity("and-dev-b9c8d7e6");

  const sender = new SimulatorSyncEngine(
    senderIdentity.deviceId,
    senderIdentity.privateKeyPem,
    senderIdentity.publicKeyPem,
    workspaceKey,
    spaceHash,
    1,
    {
      maxAttempts: 3,
      baseDelayMs: 40,
      maxDelayMs: 300,
      jitterRatio: 0.2
    },
    new InMemoryDedupWindow(24 * 60 * 60 * 1000),
    {
      allowedClockSkewMs: 90 * 1000,
      nonceTtlMs: 60 * 60 * 1000
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
    workspaceKey,
    spaceHash,
    1,
    {
      maxAttempts: 3,
      baseDelayMs: 40,
      maxDelayMs: 300,
      jitterRatio: 0.2
    },
    new InMemoryDedupWindow(24 * 60 * 60 * 1000),
    {
      allowedClockSkewMs: 90 * 1000,
      nonceTtlMs: 60 * 60 * 1000
    },
    {
      enabled: true,
      allowManualOverride: true
    }
  );

  const broker = new InMemoryBroker();
  const senderMqtt = new InMemoryMqttClient(broker);
  const receiverMqtt = new InMemoryMqttClient(broker);
  const topic = buildWorkspaceTopic(spaceHash, "ab12cd34");
  await senderMqtt.connect();
  await receiverMqtt.connect();

  const envelope = sender.createEnvelope("hello from windows");
  sender.enqueue(envelope);

  await receiverMqtt.subscribe(topic, (msg) => {
    const result = receiver.receive(msg, workspaceKey, sender.getSenderPublicKeyPem());
    if (result.result !== "applied") {
      throw new Error("receive should be applied");
    }

    const replay = receiver.receive(msg, workspaceKey, sender.getSenderPublicKeyPem());
    if (replay.result !== "rejected") {
      throw new Error("replay should be rejected by nonce guard");
    }
  });

  await sender.flush(async (msg) => {
    await senderMqtt.publish(topic, msg);
  });

  const blocked = "您的验证码是 123456";
  let blockedOk = false;
  try {
    sender.createEnvelope(blocked);
  } catch {
    blockedOk = true;
  }

  if (!blockedOk) {
    throw new Error("sensitive policy should block otp content");
  }

  await senderMqtt.disconnect();
  await receiverMqtt.disconnect();

  console.log("sender events:");
  console.log(sender.getEvents());
  console.log("receiver events:");
  console.log(receiver.getEvents());
  console.log("Simulator demo passed");
}

main().catch((err) => {
  console.error("Simulator demo failed", err);
  process.exitCode = 1;
});
