import mqtt, { MqttClient } from "mqtt";
import { SyncEnvelope } from "../../protocol-spec/dist/types";
import { MqttClientAdapter } from "./mqtt";

export interface RealMqttConfig {
  url: string;
  username?: string;
  password?: string;
  clientId: string;
}

export class RealMqttClientAdapter implements MqttClientAdapter {
  private client: MqttClient | null = null;

  constructor(private readonly config: RealMqttConfig) {}

  async connect(): Promise<void> {
    if (this.client) {
      return;
    }

    this.client = mqtt.connect(this.config.url, {
      username: this.config.username,
      password: this.config.password,
      clientId: this.config.clientId,
      reconnectPeriod: 1000,
      clean: true
    });

    await new Promise<void>((resolve, reject) => {
      const onConnect = () => {
        cleanup();
        resolve();
      };
      const onError = (err: Error) => {
        cleanup();
        reject(err);
      };
      const cleanup = () => {
        this.client?.off("connect", onConnect);
        this.client?.off("error", onError);
      };

      this.client?.once("connect", onConnect);
      this.client?.once("error", onError);
    });
  }

  async disconnect(): Promise<void> {
    if (!this.client) {
      return;
    }

    await new Promise<void>((resolve, reject) => {
      this.client?.end(false, {}, (err?: Error) => {
        if (err !== undefined && err !== null) {
          reject(err);
          return;
        }
        resolve();
      });
    });

    this.client = null;
  }

  async subscribe(topic: string, handler: (msg: SyncEnvelope) => void): Promise<void> {
    this.requireClient();

    await new Promise<void>((resolve, reject) => {
      this.client?.subscribe(topic, { qos: 1 }, (err?: Error | null) => {
        if (err !== undefined && err !== null) {
          reject(err);
          return;
        }
        resolve();
      });
    });

    this.client?.on("message", (incomingTopic, payload) => {
      if (incomingTopic !== topic) {
        return;
      }
      const parsed = JSON.parse(payload.toString("utf8")) as SyncEnvelope;
      handler(parsed);
    });
  }

  async publish(topic: string, msg: SyncEnvelope): Promise<void> {
    this.requireClient();
    const payload = JSON.stringify(msg);

    await new Promise<void>((resolve, reject) => {
      this.client?.publish(topic, payload, { qos: 1 }, (err?: Error) => {
        if (err !== undefined && err !== null) {
          reject(err);
          return;
        }
        resolve();
      });
    });
  }

  private requireClient(): void {
    if (!this.client) {
      throw new Error("mqtt client not connected");
    }
  }
}
