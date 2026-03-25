import { randomUUID } from "node:crypto";
import { SyncEnvelope } from "./types";

const allowedContentTypes = new Set([
  "text/plain",
  "text/html",
  "image/png",
  "application/octet-stream",
  "application/x-clipboard-file-ref"
]);

function isIso8601(value: string): boolean {
  const parsed = Date.parse(value);
  return Number.isFinite(parsed);
}

export function validateEnvelope(value: SyncEnvelope): string[] {
  const errors: string[] = [];

  if (value.protocolVersion !== "1.0") {
    errors.push("protocolVersion must be 1.0");
  }

  if (!value.messageId || value.messageId.length < 8) {
    errors.push("messageId is required");
  }

  if (!value.senderDeviceId.startsWith("win-") && !value.senderDeviceId.startsWith("and-") && !value.senderDeviceId.startsWith("ios-") && !value.senderDeviceId.startsWith("mac-")) {
    errors.push("senderDeviceId must include platform prefix");
  }

  if (!/^[a-f0-9]{32,64}$/i.test(value.workspaceIdHash)) {
    errors.push("workspaceIdHash must be hex");
  }

  if (!isIso8601(value.timestamp)) {
    errors.push("timestamp must be ISO8601");
  }

  if (!Number.isInteger(value.sequence) || value.sequence <= 0) {
    errors.push("sequence must be positive integer");
  }

  if (!Number.isInteger(value.keyVersion) || value.keyVersion <= 0) {
    errors.push("keyVersion must be positive integer");
  }

  if (!allowedContentTypes.has(value.contentType)) {
    errors.push("contentType is not supported");
  }

  if (!value.encryptedPayload) {
    errors.push("encryptedPayload is required");
  }

  if (!value.nonce) {
    errors.push("nonce is required");
  }

  if (!value.signature) {
    errors.push("signature is required");
  }

  if (!value.clientVersion) {
    errors.push("clientVersion is required");
  }

  return errors;
}

function createSample(): SyncEnvelope {
  return {
    protocolVersion: "1.0",
    messageId: randomUUID(),
    senderDeviceId: "win-dev-a1b2c3d4",
    workspaceIdHash: "c2d4f58f9a0134deaa0011bb22cc33dd",
    timestamp: new Date().toISOString(),
    sequence: 1,
    keyVersion: 1,
    contentType: "text/plain",
    encryptedPayload: "base64:cipher",
    nonce: "base64:nonce",
    signature: "base64:sig",
    clientVersion: "0.1.0"
  };
}

function main(): void {
  const sample = createSample();
  const errors = validateEnvelope(sample);

  if (errors.length > 0) {
    console.error("Protocol validation failed");
    for (const error of errors) {
      console.error(`- ${error}`);
    }
    process.exitCode = 1;
    return;
  }

  console.log("Protocol validation passed");
}

main();
