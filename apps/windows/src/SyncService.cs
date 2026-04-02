namespace ClipboardSync.Windows;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

public interface IClipboardReader
{
    string? ReadText();
}

public interface IClipboardWriter
{
    void WriteText(string text);
}

public interface ISecureStore
{
    string? Get(string key);
    void Set(string key, string value);
    void Delete(string key);
}

public sealed class SyncService
{
    private const string DefaultPublicRelayBaseUrl = "https://kvdb.io";
    private const string PublicRelayClipboardKey = "clipboard-sync-text";
    private const string RoutedRelayKeyPrefix = "clipboard";

    private readonly IClipboardReader _reader;
    private readonly IClipboardWriter _writer;
    private readonly ISecureStore _store;

    public SyncService(IClipboardReader reader, IClipboardWriter writer, ISecureStore store)
    {
        _reader = reader;
        _writer = writer;
        _store = store;
    }

    public string? CaptureClipboard()
    {
        return _reader.ReadText();
    }

    public void ApplyRemoteText(string text)
    {
        _writer.WriteText(text);
    }

    public void SaveWorkspaceKey(string key)
    {
        _store.Set("workspace_key", key);
    }

    public string? LoadWorkspaceKey()
    {
        return _store.Get("workspace_key");
    }

    public void ClearWorkspaceKey()
    {
        _store.Delete("workspace_key");
    }

    public void SaveDeviceId(string deviceId)
    {
        _store.Set("device_id", deviceId);
    }

    public string? LoadDeviceId()
    {
        return _store.Get("device_id");
    }

    public void SaveWebDavSettings(string baseUrl, string username, string password, bool enabled)
    {
        _store.Set("webdav_enabled", enabled ? "1" : "0");
        _store.Set("webdav_base_url", baseUrl);
        _store.Set("webdav_username", username);
        _store.Set("webdav_password", password);
    }

    public (bool Enabled, string BaseUrl, string Username, string Password) LoadWebDavSettings()
    {
        var enabled = _store.Get("webdav_enabled") == "1";
        return (
            enabled,
            _store.Get("webdav_base_url") ?? string.Empty,
            _store.Get("webdav_username") ?? string.Empty,
            _store.Get("webdav_password") ?? string.Empty
        );
    }

    public void SavePublicRelaySettings(string baseUrl, string bucket, bool enabled)
    {
        _store.Set("public_relay_enabled", enabled ? "1" : "0");
        _store.Set("public_relay_base_url", NormalizePublicRelayBaseUrl(baseUrl));
        _store.Set("public_relay_bucket", NormalizePublicRelayBucket(bucket));
    }

    public (bool Enabled, string BaseUrl, string Bucket) LoadPublicRelaySettings()
    {
        var enabled = _store.Get("public_relay_enabled") == "1";
        var baseUrl = _store.Get("public_relay_base_url");
        var bucket = _store.Get("public_relay_bucket");
        return (
            enabled,
            NormalizePublicRelayBaseUrl(baseUrl),
            NormalizePublicRelayBucket(bucket)
        );
    }

    public async Task<bool> TestPublicRelayConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cfg = LoadPublicRelaySettings();
            if (!cfg.Enabled || string.IsNullOrWhiteSpace(cfg.Bucket))
            {
                return false;
            }

            using var client = BuildPublicRelayClient(cfg.BaseUrl);
            var probeKey = $"clipboard-sync-probe-{Environment.MachineName.ToLowerInvariant()}";
            using var probeContent = new StringContent("ok", Encoding.UTF8, "text/plain");
            using var putResponse = await client.PutAsync(BuildPublicRelayPath(cfg.Bucket, probeKey), probeContent, cancellationToken).ConfigureAwait(false);
            if (!putResponse.IsSuccessStatusCode)
            {
                return false;
            }

            using var getResponse = await client.GetAsync(BuildPublicRelayPath(cfg.Bucket, probeKey), cancellationToken).ConfigureAwait(false);
            if (!getResponse.IsSuccessStatusCode)
            {
                return false;
            }

            var payload = await getResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return string.Equals(payload.Trim(), "ok", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UploadClipboardToPublicRelayAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var cfg = LoadPublicRelaySettings();
            if (!cfg.Enabled || string.IsNullOrWhiteSpace(cfg.Bucket))
            {
                return false;
            }

            using var client = BuildPublicRelayClient(cfg.BaseUrl);
            using var content = new StringContent(text, Encoding.UTF8, "text/plain");
            using var response = await client.PutAsync(BuildPublicRelayPath(cfg.Bucket, PublicRelayClipboardKey), content, cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UploadClipboardToPublicRelayForDeviceAsync(
        string text,
        string toDeviceId,
        string fromDeviceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cfg = LoadPublicRelaySettings();
            if (!cfg.Enabled || string.IsNullOrWhiteSpace(cfg.Bucket) || string.IsNullOrWhiteSpace(toDeviceId) || string.IsNullOrWhiteSpace(fromDeviceId))
            {
                return false;
            }

            using var client = BuildPublicRelayClient(cfg.BaseUrl);
            using var content = new StringContent(text, Encoding.UTF8, "text/plain");
            using var response = await client.PutAsync(
                BuildPublicRelayPath(cfg.Bucket, BuildRoutedPublicRelayKey(toDeviceId, fromDeviceId)),
                content,
                cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> DownloadClipboardFromPublicRelayAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cfg = LoadPublicRelaySettings();
            if (!cfg.Enabled || string.IsNullOrWhiteSpace(cfg.Bucket))
            {
                return null;
            }

            using var client = BuildPublicRelayClient(cfg.BaseUrl);
            using var response = await client.GetAsync(BuildPublicRelayPath(cfg.Bucket, PublicRelayClipboardKey), cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> DownloadClipboardFromPublicRelayForDeviceAsync(
        string toDeviceId,
        string fromDeviceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cfg = LoadPublicRelaySettings();
            if (!cfg.Enabled || string.IsNullOrWhiteSpace(cfg.Bucket) || string.IsNullOrWhiteSpace(toDeviceId) || string.IsNullOrWhiteSpace(fromDeviceId))
            {
                return null;
            }

            using var client = BuildPublicRelayClient(cfg.BaseUrl);
            using var response = await client.GetAsync(
                BuildPublicRelayPath(cfg.Bucket, BuildRoutedPublicRelayKey(toDeviceId, fromDeviceId)),
                cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> TestWebDavConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cfg = LoadWebDavSettings();
            if (!cfg.Enabled || string.IsNullOrWhiteSpace(cfg.BaseUrl))
            {
                return false;
            }

            using var client = BuildWebDavClient(cfg.BaseUrl, cfg.Username, cfg.Password);
            using var req = new HttpRequestMessage(HttpMethod.Head, cfg.BaseUrl.TrimEnd('/') + "/");
            using var res = await client.SendAsync(req, cancellationToken).ConfigureAwait(false);
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UploadClipboardToWebDavAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var cfg = LoadWebDavSettings();
            if (!cfg.Enabled || string.IsNullOrWhiteSpace(cfg.BaseUrl))
            {
                return false;
            }

            using var client = BuildWebDavClient(cfg.BaseUrl, cfg.Username, cfg.Password);
            var target = cfg.BaseUrl.TrimEnd('/') + "/clipboard-sync.txt";
            using var content = new StringContent(text, Encoding.UTF8, "text/plain");
            using var res = await client.PutAsync(target, content, cancellationToken).ConfigureAwait(false);
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> DownloadClipboardFromWebDavAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cfg = LoadWebDavSettings();
            if (!cfg.Enabled || string.IsNullOrWhiteSpace(cfg.BaseUrl))
            {
                return null;
            }

            using var client = BuildWebDavClient(cfg.BaseUrl, cfg.Username, cfg.Password);
            var target = cfg.BaseUrl.TrimEnd('/') + "/clipboard-sync.txt";
            using var res = await client.GetAsync(target, cancellationToken).ConfigureAwait(false);
            if (!res.IsSuccessStatusCode)
            {
                return null;
            }

            return await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    private static HttpClient BuildWebDavClient(string baseUrl, string username, string password)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/")
        };

        if (!string.IsNullOrWhiteSpace(username))
        {
            var raw = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", raw);
        }
        return client;
    }

    private static HttpClient BuildPublicRelayClient(string baseUrl)
    {
        var normalizedBaseUrl = NormalizePublicRelayBaseUrl(baseUrl);
        if (!normalizedBaseUrl.EndsWith('/'))
        {
            normalizedBaseUrl += "/";
        }

        return new HttpClient
        {
            BaseAddress = new Uri(normalizedBaseUrl)
        };
    }

    private static string BuildPublicRelayPath(string bucket, string key)
    {
        return Uri.EscapeDataString(bucket.Trim()) + "/" + Uri.EscapeDataString(key.Trim());
    }

    private static string BuildRoutedPublicRelayKey(string toDeviceId, string fromDeviceId)
    {
        return RoutedRelayKeyPrefix
            + "-"
            + toDeviceId.Trim().ToLowerInvariant()
            + "-"
            + fromDeviceId.Trim().ToLowerInvariant();
    }

    private static string NormalizePublicRelayBaseUrl(string? input)
    {
        var raw = string.IsNullOrWhiteSpace(input) ? DefaultPublicRelayBaseUrl : input.Trim();
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            return DefaultPublicRelayBaseUrl;
        }

        return uri.GetLeftPart(UriPartial.Authority);
    }

    private static string NormalizePublicRelayBucket(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var raw = input.Trim();
        if (Uri.TryCreate(raw, UriKind.Absolute, out var absoluteUri))
        {
            var segments = absoluteUri.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.Length > 0 ? segments[0].Trim() : string.Empty;
        }

        var relativeSegments = raw.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return relativeSegments.Length > 0 ? relativeSegments[0].Trim() : string.Empty;
    }
}
