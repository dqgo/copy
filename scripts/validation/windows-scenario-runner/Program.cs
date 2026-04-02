using System.Net;
using System.Text;
using ClipboardSync.Windows;

internal sealed class FakeReader : IClipboardReader
{
    public string? Value { get; set; } = "seed-text";
    public string? ReadText() => Value;
}

internal sealed class FakeWriter : IClipboardWriter
{
    public string? LastWritten { get; private set; }
    public void WriteText(string text) => LastWritten = text;
}

internal sealed record ScenarioResult(string Name, bool Passed, string Detail);

internal sealed class WebDavMockServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;
    private string _content = string.Empty;

    public WebDavMockServer(string baseUrl)
    {
        var normalized = baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/";
        _listener.Prefixes.Add(normalized);
    }

    public void Start()
    {
        _listener.Start();
        _loop = Task.Run(() => RunLoop(_cts.Token), _cts.Token);
    }

    private void RunLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = _listener.GetContext();
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            try
            {
                var req = ctx.Request;
                var res = ctx.Response;

                if (req.HttpMethod.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
                {
                    res.StatusCode = 200;
                    res.Close();
                    continue;
                }

                if (req.HttpMethod.Equals("PUT", StringComparison.OrdinalIgnoreCase))
                {
                    using var sr = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8);
                    _content = sr.ReadToEnd();
                    res.StatusCode = 201;
                    res.Close();
                    continue;
                }

                if (req.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(_content))
                    {
                        res.StatusCode = 404;
                        res.Close();
                        continue;
                    }

                    var bytes = Encoding.UTF8.GetBytes(_content);
                    res.StatusCode = 200;
                    res.ContentType = "text/plain";
                    res.ContentLength64 = bytes.Length;
                    res.OutputStream.Write(bytes, 0, bytes.Length);
                    res.Close();
                    continue;
                }

                res.StatusCode = 405;
                res.Close();
            }
            catch
            {
                try
                {
                    ctx.Response.StatusCode = 500;
                    ctx.Response.Close();
                }
                catch
                {
                    // ignored
                }
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        if (_listener.IsListening)
        {
            _listener.Stop();
        }

        _listener.Close();
        try
        {
            _loop?.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // ignored
        }

        _cts.Dispose();
    }
}

internal static class Program
{
    private static readonly string[] ManagedKeys =
    {
        "workspace_key",
        "webdav_enabled",
        "webdav_base_url",
        "webdav_username",
        "webdav_password",
        "trusted_devices_json",
        "pairing_requests_json",
        "history_items_json",
        "local_server_enabled",
        "sync_mode",
        "space_id",
        "pairing_policy"
    };

    private static async Task<int> Main(string[] args)
    {
        var results = new List<ScenarioResult>();
        var store = new SecureStoreAdapter();
        var backup = BackupKeys(store);
        var reader = new FakeReader();
        var writer = new FakeWriter();
        var service = new SyncService(reader, writer, store);

        try
        {
            var repoRoot = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
            var exePath = Path.Combine(repoRoot, "artifacts", "windows", "ClipboardSync.Windows.exe");
            results.Add(new ScenarioResult(
                "Install artifact",
                File.Exists(exePath),
                File.Exists(exePath) ? "Executable exists." : "Executable not found."
            ));

            service.SaveWebDavSettings("http://127.0.0.1:19091", "alice", "secret", true);
            var loaded = service.LoadWebDavSettings();
            var cfgOk = loaded.Enabled && loaded.BaseUrl.Contains("127.0.0.1:19091") && loaded.Username == "alice" && loaded.Password == "secret";
            results.Add(new ScenarioResult(
                "Configuration save/load",
                cfgOk,
                cfgOk ? "Settings persisted." : "Settings mismatch after save/load."
            ));

            store.Set("pairing_requests_json", "[{\"RequestId\":\"req-validate\",\"DeviceName\":\"iPhone\",\"Platform\":\"ios\",\"RequestedAt\":\"now\"}]");
            store.Set("trusted_devices_json", "[{\"DeviceId\":\"dev-validate\",\"Name\":\"Desktop\",\"LastSeen\":\"now\"}]");
            var reloadedStore = new SecureStoreAdapter();
            var pairingOk = (reloadedStore.Get("pairing_requests_json") ?? string.Empty).Contains("req-validate", StringComparison.Ordinal);
            var trustedOk = (reloadedStore.Get("trusted_devices_json") ?? string.Empty).Contains("dev-validate", StringComparison.Ordinal);
            results.Add(new ScenarioResult(
                "Pairing data persistence",
                pairingOk && trustedOk,
                pairingOk && trustedOk ? "Pairing/trusted snapshots persisted." : "Pairing or trusted snapshot not persisted."
            ));

            using (var server = new WebDavMockServer("http://127.0.0.1:19091/"))
            {
                server.Start();
                service.SaveWebDavSettings("http://127.0.0.1:19091", "", "", true);

                var testOk = await service.TestWebDavConnectionAsync();
                var uploadOk = await service.UploadClipboardToWebDavAsync("hello-sync");
                var downloaded = await service.DownloadClipboardFromWebDavAsync();
                service.ApplyRemoteText(downloaded ?? string.Empty);

                var syncOk = testOk && uploadOk && downloaded == "hello-sync" && writer.LastWritten == "hello-sync";
                results.Add(new ScenarioResult(
                    "Sync flow (WebDAV)",
                    syncOk,
                    syncOk ? "Connection/upload/download/apply all passed." : "Sync flow failed in one or more steps."
                ));
            }

            service.SaveWorkspaceKey("wk-validate");
            var afterRestart = new SecureStoreAdapter();
            var restartOk = string.Equals(afterRestart.Get("workspace_key"), "wk-validate", StringComparison.Ordinal);
            results.Add(new ScenarioResult(
                "Restart persistence",
                restartOk,
                restartOk ? "Workspace key available after re-open." : "Workspace key missing after re-open."
            ));

            service.SaveWebDavSettings("http://127.0.0.1:1", "", "", true);
            var noThrow = true;
            try
            {
                _ = await service.TestWebDavConnectionAsync();
                _ = await service.UploadClipboardToWebDavAsync("x");
                _ = await service.DownloadClipboardFromWebDavAsync();
            }
            catch (Exception ex)
            {
                noThrow = false;
                results.Add(new ScenarioResult("Exception recovery", false, "Thrown: " + ex.GetType().Name));
            }

            if (noThrow)
            {
                results.Add(new ScenarioResult("Exception recovery", true, "Network failures handled without throw."));
            }
        }
        finally
        {
            RestoreKeys(store, backup);
        }

        Console.WriteLine("Windows scenario validation results:");
        foreach (var result in results)
        {
            var flag = result.Passed ? "PASS" : "FAIL";
            Console.WriteLine($"- [{flag}] {result.Name}: {result.Detail}");
        }

        var failed = results.Where(r => !r.Passed).ToList();
        if (failed.Count > 0)
        {
            Console.WriteLine("Validation failed for " + failed.Count + " scenario(s).");
            return 1;
        }

        Console.WriteLine("All scenarios passed.");
        return 0;
    }

    private static Dictionary<string, string?> BackupKeys(SecureStoreAdapter store)
    {
        var backup = new Dictionary<string, string?>();
        foreach (var key in ManagedKeys)
        {
            backup[key] = store.Get(key);
        }

        return backup;
    }

    private static void RestoreKeys(SecureStoreAdapter store, Dictionary<string, string?> backup)
    {
        foreach (var pair in backup)
        {
            if (pair.Value is null)
            {
                store.Delete(pair.Key);
            }
            else
            {
                store.Set(pair.Key, pair.Value);
            }
        }
    }
}
