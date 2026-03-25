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

    public async Task<bool> TestWebDavConnectionAsync(CancellationToken cancellationToken = default)
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

    public async Task<bool> UploadClipboardToWebDavAsync(string text, CancellationToken cancellationToken = default)
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

    public async Task<string?> DownloadClipboardFromWebDavAsync(CancellationToken cancellationToken = default)
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
}
