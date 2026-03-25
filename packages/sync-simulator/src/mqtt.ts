import { SyncEnvelope } from "../../protocol-spec/dist/types";

export interface MqttClientAdapter {
  connect(): Promise<void>;
  disconnect(): Promise<void>;
  subscribe(topic: string, handler: (msg: SyncEnvelope) => void): Promise<void>;
  publish(topic: string, msg: SyncEnvelope): Promise<void>;
}

export class InMemoryBroker {
  private readonly subscribers = new Map<string, Array<(msg: SyncEnvelope) => void>>();

  subscribe(topic: string, handler: (msg: SyncEnvelope) => void): void {
    const list = this.subscribers.get(topic) ?? [];
    list.push(handler);
    this.subscribers.set(topic, list);
  }

  publish(topic: string, msg: SyncEnvelope): void {
    const list = this.subscribers.get(topic) ?? [];
    for (const handler of list) {
      handler(msg);
    }
  }
}

export class InMemoryMqttClient implements MqttClientAdapter {
  private connected = false;

  constructor(private readonly broker: InMemoryBroker) {}

  async connect(): Promise<void> {
    this.connected = true;
  }

  async disconnect(): Promise<void> {
    this.connected = false;
  }

  async subscribe(topic: string, handler: (msg: SyncEnvelope) => void): Promise<void> {
    this.requireConnected();
    this.broker.subscribe(topic, handler);
  }

  async publish(topic: string, msg: SyncEnvelope): Promise<void> {
    this.requireConnected();
    this.broker.publish(topic, msg);
  }

  private requireConnected(): void {
    if (!this.connected) {
      throw new Error("mqtt client is not connected");
    }
  }
}

export function buildWorkspaceTopic(workspaceIdHash: string, topicSalt: string): string {
  return `ws/${workspaceIdHash}/${topicSalt}/events`;
}
