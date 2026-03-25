export type LogLevel = "debug" | "info" | "warn" | "error";

export interface LogEntry {
  level: LogLevel;
  message: string;
  metadata?: Record<string, unknown>;
}

export interface Logger {
  log(entry: LogEntry): void;
}

export class InMemoryLogger implements Logger {
  private readonly entries: LogEntry[] = [];

  log(entry: LogEntry): void {
    this.entries.push(redactEntry(entry));
  }

  all(): LogEntry[] {
    return [...this.entries];
  }
}

export function redactEntry(entry: LogEntry): LogEntry {
  const metadata = entry.metadata ? { ...entry.metadata } : undefined;

  if (metadata) {
    for (const key of Object.keys(metadata)) {
      const lower = key.toLowerCase();
      if (
        lower.includes("payload") ||
        lower.includes("plaintext") ||
        lower.includes("workspacekey") ||
        lower.includes("invite") ||
        lower.includes("manualkey") ||
        lower.includes("recoveryphrase") ||
        lower.includes("qrcode")
      ) {
        metadata[key] = "[REDACTED]";
      }
    }
  }

  const message = entry.message.replace(/(验证码|otp|password|passwd|密码)\s*[:=]?\s*[\w-]+/gi, "$1 [REDACTED]");

  return {
    level: entry.level,
    message,
    metadata
  };
}
