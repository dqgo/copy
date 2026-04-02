using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using ClipboardSync.Windows;

namespace ClipboardSync_Windows_WinUI.Services;

internal sealed record TrustedDevice(string DeviceId, string Name, string LastSeen);

internal sealed class AppSession
{
    private sealed class TrustedDeviceSnapshot
    {
        public string? DeviceId { get; set; }
        public string? Name { get; set; }
        public string? LastSeen { get; set; }
    }

    private sealed class InvitePayload
    {
        public string WorkspaceKey { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Platform { get; set; } = "windows";
    }

    public static AppSession Instance { get; } = new();

    public SecureStoreAdapter Store { get; }

    public SyncService Sync { get; }

    public string WorkspaceKey { get; private set; }

    public string DeviceId { get; }

    private AppSession()
    {
        Store = new SecureStoreAdapter();
        Sync = new SyncService(new WinUiClipboardReader(), new WinUiClipboardWriter(), Store);

        var workspaceKey = Sync.LoadWorkspaceKey();
        if (string.IsNullOrWhiteSpace(workspaceKey))
        {
            workspaceKey = "wsk-" + Guid.NewGuid().ToString("N");
            Sync.SaveWorkspaceKey(workspaceKey);
        }
        WorkspaceKey = workspaceKey;

        var deviceId = Sync.LoadDeviceId();
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            deviceId = "win-" + Guid.NewGuid().ToString("N").Substring(0, 12);
            Sync.SaveDeviceId(deviceId);
        }
        DeviceId = deviceId;

        var publicRelay = Sync.LoadPublicRelaySettings();
        if (string.IsNullOrWhiteSpace(publicRelay.Bucket))
        {
            Sync.SavePublicRelaySettings(publicRelay.BaseUrl, WorkspaceKey, publicRelay.Enabled);
        }
    }

    public void SetWorkspaceKey(string workspaceKey)
    {
        if (string.IsNullOrWhiteSpace(workspaceKey))
        {
            return;
        }

        WorkspaceKey = workspaceKey.Trim();
        Sync.SaveWorkspaceKey(WorkspaceKey);

        var relay = Sync.LoadPublicRelaySettings();
        if (string.IsNullOrWhiteSpace(relay.Bucket))
        {
            Sync.SavePublicRelaySettings(relay.BaseUrl, WorkspaceKey, relay.Enabled);
        }
    }

    public List<TrustedDevice> LoadTrustedDevices()
    {
        var raw = Store.Get("trusted_devices_json");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<TrustedDevice>();
        }

        try
        {
            var payload = JsonSerializer.Deserialize<List<TrustedDeviceSnapshot>>(raw) ?? new List<TrustedDeviceSnapshot>();
            return payload
                .Where(x => !string.IsNullOrWhiteSpace(x.DeviceId))
                .Select(x => new TrustedDevice(x.DeviceId ?? string.Empty, x.Name ?? string.Empty, x.LastSeen ?? string.Empty))
                .ToList();
        }
        catch
        {
            return new List<TrustedDevice>();
        }
    }

    public void SaveTrustedDevices(List<TrustedDevice> devices)
    {
        var payload = devices.Select(x => new TrustedDeviceSnapshot
        {
            DeviceId = x.DeviceId,
            Name = x.Name,
            LastSeen = x.LastSeen
        }).ToList();

        Store.Set("trusted_devices_json", JsonSerializer.Serialize(payload));
    }

    public string CreateInviteCode(string deviceName)
    {
        var payload = new InvitePayload
        {
            WorkspaceKey = WorkspaceKey,
            DeviceId = DeviceId,
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? Environment.MachineName : deviceName.Trim(),
            Platform = "windows"
        };

        var json = JsonSerializer.Serialize(payload);
        return ToBase64Url(Encoding.UTF8.GetBytes(json));
    }

    public bool TryJoinInvite(string inviteCode, out string message, out TrustedDevice? trustedDevice)
    {
        trustedDevice = null;
        if (!TryParseInvite(inviteCode, out var payload, out message))
        {
            return false;
        }

        if (string.Equals(payload.DeviceId, DeviceId, StringComparison.Ordinal))
        {
            message = "不能与当前设备配对";
            return false;
        }

        SetWorkspaceKey(payload.WorkspaceKey);

        trustedDevice = new TrustedDevice(
            payload.DeviceId,
            payload.DeviceName,
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        );

        message = "配对信息已导入";
        return true;
    }

    public (bool Enabled, string BaseUrl, string Bucket) LoadPublicRelaySettings()
    {
        return Sync.LoadPublicRelaySettings();
    }

    public void SavePublicRelaySettings(string baseUrl, string bucket, bool enabled)
    {
        var resolvedBucket = string.IsNullOrWhiteSpace(bucket) ? WorkspaceKey : bucket.Trim();
        Sync.SavePublicRelaySettings(baseUrl, resolvedBucket, enabled);
    }

    public (bool Enabled, string BaseUrl, string Username, string Password) LoadWebDavSettings()
    {
        return Sync.LoadWebDavSettings();
    }

    public void SaveWebDavSettings(string baseUrl, string username, string password, bool enabled)
    {
        Sync.SaveWebDavSettings(baseUrl, username, password, enabled);
    }

    public async Task<bool> TestPublicRelayAsync()
    {
        return await Sync.TestPublicRelayConnectionAsync().ConfigureAwait(false);
    }

    public async Task<bool> TestWebDavAsync()
    {
        return await Sync.TestWebDavConnectionAsync().ConfigureAwait(false);
    }

    public async Task<(bool Ok, string Message)> SyncOnceAsync()
    {
        var localText = Sync.CaptureClipboard() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(localText))
        {
            localText = "empty";
        }

        var channel = GetActiveChannel();
        if (channel == "public")
        {
            var relay = LoadPublicRelaySettings();
            SavePublicRelaySettings(relay.BaseUrl, relay.Bucket, true);
            if (!await Sync.UploadClipboardToPublicRelayAsync(localText).ConfigureAwait(false))
            {
                return (false, "公共服务器上传失败");
            }

            var remote = await Sync.DownloadClipboardFromPublicRelayAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(remote))
            {
                Sync.ApplyRemoteText(remote);
                return (true, "public: " + remote);
            }

            return (true, "public: no remote payload");
        }

        if (channel == "webdav")
        {
            var webDav = LoadWebDavSettings();
            SaveWebDavSettings(webDav.BaseUrl, webDav.Username, webDav.Password, true);
            if (!await Sync.UploadClipboardToWebDavAsync(localText).ConfigureAwait(false))
            {
                return (false, "WebDAV 上传失败");
            }

            var remote = await Sync.DownloadClipboardFromWebDavAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(remote))
            {
                Sync.ApplyRemoteText(remote);
                return (true, "webdav: " + remote);
            }

            return (true, "webdav: no remote payload");
        }

        Sync.ApplyRemoteText("manual-sync:" + localText);
        return (true, "local: manual-sync:" + localText);
    }

    private string GetActiveChannel()
    {
        var relay = LoadPublicRelaySettings();
        if (relay.Enabled)
        {
            return "public";
        }

        var webDav = LoadWebDavSettings();
        if (webDav.Enabled)
        {
            return "webdav";
        }

        return "local";
    }

    private static string ToBase64Url(byte[] raw)
    {
        return Convert.ToBase64String(raw).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static bool TryParseInvite(string inviteCode, out InvitePayload payload, out string error)
    {
        payload = new InvitePayload();
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(inviteCode))
        {
            error = "邀请码为空";
            return false;
        }

        try
        {
            var normalized = inviteCode.Trim().Replace('-', '+').Replace('_', '/');
            switch (normalized.Length % 4)
            {
                case 2:
                    normalized += "==";
                    break;
                case 3:
                    normalized += "=";
                    break;
            }

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
            var parsed = JsonSerializer.Deserialize<InvitePayload>(json);
            if (parsed is null
                || string.IsNullOrWhiteSpace(parsed.WorkspaceKey)
                || string.IsNullOrWhiteSpace(parsed.DeviceId)
                || string.IsNullOrWhiteSpace(parsed.DeviceName))
            {
                error = "邀请码无效";
                return false;
            }

            payload = parsed;
            return true;
        }
        catch
        {
            error = "邀请码格式错误";
            return false;
        }
    }
}
