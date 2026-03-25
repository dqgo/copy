import { createServer, IncomingMessage, ServerResponse } from "node:http";

interface ServerState {
  serviceRunning: boolean;
}

const state: ServerState = {
  serviceRunning: false
};

const port = Number(process.env.CUSTOM_SYNC_SERVER_PORT ?? 8787);

function writeJson(res: ServerResponse, statusCode: number, body: unknown): void {
  res.writeHead(statusCode, {
    "Content-Type": "application/json; charset=utf-8"
  });
  res.end(JSON.stringify(body));
}

async function readBody(req: IncomingMessage): Promise<string> {
  const chunks: Buffer[] = [];
  for await (const chunk of req) {
    chunks.push(Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk));
  }
  return Buffer.concat(chunks).toString("utf8");
}

const server = createServer(async (req, res) => {
  const method = req.method ?? "GET";
  const url = req.url ?? "/";

  if (method === "GET" && url === "/health") {
    writeJson(res, 200, {
      ok: true,
      serviceRunning: state.serviceRunning
    });
    return;
  }

  if (method === "POST" && url === "/start-service") {
    state.serviceRunning = true;
    writeJson(res, 200, {
      ok: true,
      serviceRunning: state.serviceRunning
    });
    return;
  }

  if (method === "POST" && url === "/stop-service") {
    state.serviceRunning = false;
    writeJson(res, 200, {
      ok: true,
      serviceRunning: state.serviceRunning
    });
    return;
  }

  if (method === "POST" && url === "/echo") {
    if (!state.serviceRunning) {
      writeJson(res, 503, {
        ok: false,
        error: "service-not-running"
      });
      return;
    }

    const raw = await readBody(req);
    writeJson(res, 200, {
      ok: true,
      data: raw
    });
    return;
  }

  writeJson(res, 404, {
    ok: false,
    error: "not-found"
  });
});

server.listen(port, () => {
  process.stdout.write(`custom-sync-server listening on ${port}\n`);
});
