export interface CustomServerOptions {
  enabled: boolean;
  baseUrl: string;
  autoStartService: boolean;
  healthPath?: string;
  startServicePath?: string;
  token?: string;
}

interface HealthResponse {
  ok: boolean;
  serviceRunning?: boolean;
}

function joinUrl(base: string, path: string): string {
  if (base.endsWith("/") && path.startsWith("/")) {
    return `${base.slice(0, -1)}${path}`;
  }
  if (!base.endsWith("/") && !path.startsWith("/")) {
    return `${base}/${path}`;
  }
  return `${base}${path}`;
}

export async function ensureCustomServerReady(options?: CustomServerOptions): Promise<void> {
  if (!options || !options.enabled) {
    return;
  }

  const healthPath = options.healthPath ?? "/health";
  const startServicePath = options.startServicePath ?? "/start-service";
  const headers: Record<string, string> = {};

  if (options.token) {
    headers.Authorization = `Bearer ${options.token}`;
  }

  const healthUrl = joinUrl(options.baseUrl, healthPath);
  const res = await fetch(healthUrl, {
    method: "GET",
    headers
  });

  if (!res.ok) {
    throw new Error(`custom-server-health-failed:${res.status}`);
  }

  const json = (await res.json()) as HealthResponse;
  if (json.serviceRunning) {
    return;
  }

  if (!options.autoStartService) {
    throw new Error("custom-server-service-not-running");
  }

  const startUrl = joinUrl(options.baseUrl, startServicePath);
  const startRes = await fetch(startUrl, {
    method: "POST",
    headers
  });

  if (!startRes.ok) {
    throw new Error(`custom-server-start-failed:${startRes.status}`);
  }

  const finalHealth = await fetch(healthUrl, {
    method: "GET",
    headers
  });
  if (!finalHealth.ok) {
    throw new Error(`custom-server-health-after-start-failed:${finalHealth.status}`);
  }
  const finalJson = (await finalHealth.json()) as HealthResponse;
  if (!finalJson.serviceRunning) {
    throw new Error("custom-server-service-still-not-running");
  }
}
