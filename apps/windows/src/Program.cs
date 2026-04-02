namespace ClipboardSync.Windows;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

internal sealed class SystemClipboardReader : IClipboardReader
{
    public string? ReadText()
    {
        try
        {
            return Clipboard.ContainsText() ? Clipboard.GetText() : null;
        }
        catch
        {
            return null;
        }
    }
}

internal sealed class SystemClipboardWriter : IClipboardWriter
{
    public void WriteText(string text)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch
        {
            // Ignore clipboard busy errors in MVP runtime path.
        }
    }
}

internal sealed class DeviceItem
{
    public string DeviceId { get; }
    public string Name { get; }
    public string LastSeen { get; set; }

    public DeviceItem(string deviceId, string name, string lastSeen)
    {
        DeviceId = deviceId;
        Name = name;
        LastSeen = lastSeen;
    }

    public override string ToString() => $"{Name} ({DeviceId}) · {LastSeen}";
}

internal sealed class PairingRequestItem
{
    public string RequestId { get; }
    public string DeviceId { get; }
    public string DeviceName { get; }
    public string Platform { get; }
    public string RequestedAt { get; }

    public PairingRequestItem(string requestId, string deviceId, string deviceName, string platform, string requestedAt)
    {
        RequestId = requestId;
        DeviceId = deviceId;
        DeviceName = deviceName;
        Platform = platform;
        RequestedAt = requestedAt;
    }

    public override string ToString() => $"{DeviceName} ({Platform}) · {RequestedAt}";
}

internal static class Program
{
    private static readonly Dictionary<string, Dictionary<string, string>> I18n = new()
    {
        ["zh-CN"] = new Dictionary<string, string>
        {
            ["title"] = "Clipboard Sync 控制台",
            ["status"] = "状态",
            ["devices"] = "可信设备",
            ["history"] = "最近记录",
            ["settings"] = "设置",
            ["connection"] = "连接状态",
            ["sent"] = "发出",
            ["received"] = "接收",
            ["rejected"] = "拒绝",
            ["lastError"] = "最近错误",
            ["manualSync"] = "手动同步",
            ["revoke"] = "撤销选中设备",
            ["pairing"] = "配对请求",
            ["trustedCount"] = "可信设备数",
            ["pendingPairing"] = "待审批配对",
            ["approve"] = "批准配对",
            ["reject"] = "拒绝配对",
            ["language"] = "语言",
            ["theme"] = "主题",
            ["syncMode"] = "同步模式",
            ["space"] = "工作空间",
            ["pairingPolicy"] = "配对策略",
            ["webdev"] = "启用 WebDev 同步",
            ["webdevUrl"] = "WebDev 地址",
            ["webdevUser"] = "WebDev 用户名",
            ["webdevPassword"] = "WebDev 密码",
            ["testWebdev"] = "测试 WebDev 连接",
            ["publicRelay"] = "启用免费公共服务器",
            ["publicRelayUrl"] = "公共服务器地址",
            ["publicRelayBucket"] = "共享桶 ID",
            ["testPublicRelay"] = "测试公共服务器",
            ["server"] = "启用本地服务模式",
            ["sendHtml"] = "发送 HTML",
            ["sendImage"] = "发送图片",
            ["sendFile"] = "发送文件",
            ["manualSyncBtn"] = "手动同步",
            ["workspaceKey"] = "工作空间 ID",
            ["deviceId"] = "设备唯一 ID",
            ["deviceName"] = "设备名称",
            ["remoteDeviceId"] = "对端设备ID",
            ["remoteDeviceName"] = "对端设备名",
            ["remoteDeviceIdPlaceholder"] = "输入对端设备ID",
            ["remoteDeviceNamePlaceholder"] = "输入备注名（可选）",
            ["copy"] = "复制",
            ["createInvite"] = "生成邀请",
            ["joinByInvite"] = "通过邀请码配对",
            ["pairByDeviceId"] = "通过设备ID配对",
            ["inviteCode"] = "邀请码",
            ["invitePlaceholder"] = "粘贴对端邀请码",
            ["system"] = "跟随系统",
            ["light"] = "浅色",
            ["dark"] = "深色",
            ["manual"] = "手动",
            ["auto"] = "自动",
            ["default"] = "默认",
            ["work"] = "工作",
            ["lab"] = "实验室",
            ["manualApprove"] = "手动批准",
            ["autoApproveInvite"] = "邀请自动批准",
            ["show"] = "显示",
            ["quickManualSync"] = "快速手动同步",
            ["exit"] = "退出",
            ["running"] = "应用仍在托盘中运行",
            ["loadingText"] = "正在加载...",
            ["emptyState"] = "没有项目",
            ["errorLoading"] = "加载失败，请重试",
            ["confirmRevoke"] = "确认撤销此设备的配对吗？此操作无法撤销。",
            ["confirmReject"] = "确认拒绝此配对请求吗？",
            ["retry"] = "重试",
            ["clearFilter"] = "清空筛选",
            ["emptyDevicesHint"] = "暂无可信设备，可前往配对页添加",
            ["emptyPairingHint"] = "暂无配对请求，等待新设备发起即可"
        },
        ["en-US"] = new Dictionary<string, string>
        {
            ["title"] = "Clipboard Sync Console",
            ["status"] = "Status",
            ["devices"] = "Trusted Devices",
            ["history"] = "History",
            ["settings"] = "Settings",
            ["connection"] = "Connection",
            ["sent"] = "Sent",
            ["received"] = "Received",
            ["rejected"] = "Rejected",
            ["lastError"] = "Last Error",
            ["manualSync"] = "Manual Sync",
            ["revoke"] = "Revoke Selected Device",
            ["pairing"] = "Pairing Requests",
            ["trustedCount"] = "Trusted Devices",
            ["pendingPairing"] = "Pending Pairing",
            ["approve"] = "Approve Request",
            ["reject"] = "Reject Request",
            ["language"] = "Language",
            ["theme"] = "Theme",
            ["syncMode"] = "Sync Mode",
            ["space"] = "Space",
            ["pairingPolicy"] = "Pairing Policy",
            ["webdev"] = "Enable WebDev Sync",
            ["webdevUrl"] = "WebDev URL",
            ["webdevUser"] = "WebDev Username",
            ["webdevPassword"] = "WebDev Password",
            ["testWebdev"] = "Test WebDev Connection",
            ["publicRelay"] = "Enable Free Public Relay",
            ["publicRelayUrl"] = "Public Relay URL",
            ["publicRelayBucket"] = "Shared Bucket ID",
            ["testPublicRelay"] = "Test Public Relay",
            ["server"] = "Enable Local Server Mode",
            ["sendHtml"] = "Send HTML",
            ["sendImage"] = "Send Image",
            ["sendFile"] = "Send File",
            ["manualSyncBtn"] = "Manual Sync",
            ["workspaceKey"] = "Workspace ID",
            ["deviceId"] = "Unique Device ID",
            ["deviceName"] = "Device Name",
            ["remoteDeviceId"] = "Remote Device ID",
            ["remoteDeviceName"] = "Remote Device Name",
            ["remoteDeviceIdPlaceholder"] = "Enter remote device ID",
            ["remoteDeviceNamePlaceholder"] = "Optional display name",
            ["copy"] = "Copy",
            ["createInvite"] = "Create Invite",
            ["joinByInvite"] = "Pair By Invite",
            ["pairByDeviceId"] = "Pair By Device ID",
            ["inviteCode"] = "Invite Code",
            ["invitePlaceholder"] = "Paste remote invite code",
            ["system"] = "System",
            ["light"] = "Light",
            ["dark"] = "Dark",
            ["manual"] = "Manual",
            ["auto"] = "Auto",
            ["default"] = "Default",
            ["work"] = "Work",
            ["lab"] = "Lab",
            ["manualApprove"] = "Manual Approve",
            ["autoApproveInvite"] = "Auto-Approve Invite",
            ["show"] = "Show",
            ["quickManualSync"] = "Quick Manual Sync",
            ["exit"] = "Exit",
            ["running"] = "App is still running in system tray",
            ["loadingText"] = "Loading...",
            ["emptyState"] = "No items",
            ["errorLoading"] = "Failed to load, please retry",
            ["confirmRevoke"] = "Confirm revoking this device's pairing? This action cannot be undone.",
            ["confirmReject"] = "Confirm rejecting this pairing request?",
            ["retry"] = "Retry",
            ["clearFilter"] = "Clear Filter",
            ["emptyDevicesHint"] = "No trusted devices yet. Add one from pairing tab.",
            ["emptyPairingHint"] = "No pairing requests for now."
        }
    };

    [STAThread]
    private static int Main()
    {
        ApplicationConfiguration.Initialize();

        var reader = new SystemClipboardReader();
        var writer = new SystemClipboardWriter();
        var store = new SecureStoreAdapter();
        var service = new SyncService(reader, writer, store);
        var workspaceKey = service.LoadWorkspaceKey();
        if (string.IsNullOrWhiteSpace(workspaceKey))
        {
            workspaceKey = $"wsk-{Guid.NewGuid():N}";
            service.SaveWorkspaceKey(workspaceKey);
        }

        var deviceId = service.LoadDeviceId();
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            deviceId = GenerateDeviceId();
            service.SaveDeviceId(deviceId);
        }

        var status = new StatusViewModel
        {
            ConnectionState = SyncConnectionState.Disconnected,
            SyncedOutCount = 0,
            SyncedInCount = 0,
            RejectedEventCount = 0,
            TrustedDeviceCount = 0,
            PendingPairingCount = 0,
            LastErrorMessage = string.Empty
        };

        var trustedDevices = new List<DeviceItem>();
        trustedDevices.AddRange(LoadDevices(store));

        var history = new ListBox { Dock = DockStyle.Fill };
        var savedHistory = LoadHistory(store);
        if (savedHistory.Count == 0)
        {
            history.Items.Add($"[info] workspace key loaded · {workspaceKey}");
            history.Items.Add($"[info] device id ready · {deviceId}");
        }
        else
        {
            foreach (var item in savedHistory)
            {
                history.Items.Add(item);
            }
        }

        var pairingRequests = new List<PairingRequestItem>();
        pairingRequests.AddRange(LoadPairingRequests(store));
        status.TrustedDeviceCount = trustedDevices.Count;
        status.PendingPairingCount = pairingRequests.Count;
        var relayLastAppliedBySender = LoadRelayLastAppliedBySender(store);

        void PersistRuntimeSnapshots() => SaveRuntimeSnapshots(store, trustedDevices, pairingRequests, history);

        var locale = "zh-CN";

        var form = new Form
        {
            Text = T(locale, "title"),
            Width = 980,
            Height = 640,
            StartPosition = FormStartPosition.CenterScreen
        };
        using var appBadge = DrawPlatformBadge(48, 48);
        form.Icon = Icon.FromHandle(appBadge.GetHicon());

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new Label
        {
            Text = "Clipboard Sync · Windows MVP",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            Height = 54,
            Dock = DockStyle.Top
        };
        header.Image = DrawPlatformBadge(36, 36);
        header.ImageAlign = ContentAlignment.MiddleLeft;
        header.Padding = new Padding(8, 0, 0, 0);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var statusTab = new TabPage(T(locale, "status"));
        var devicesTab = new TabPage(T(locale, "devices"));
        var historyTab = new TabPage(T(locale, "history"));
        var pairingTab = new TabPage(T(locale, "pairing"));
        var settingsTab = new TabPage(T(locale, "settings"));
        tabs.TabPages.Add(statusTab);
        tabs.TabPages.Add(devicesTab);
        tabs.TabPages.Add(historyTab);
        tabs.TabPages.Add(pairingTab);
        tabs.TabPages.Add(settingsTab);

        var statusGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 11,
            Padding = new Padding(16)
        };
        statusGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        statusGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var connectionValue = new Label { AutoSize = true, Text = status.ConnectionState.ToString() };
        var sentValue = new Label { AutoSize = true, Text = status.SyncedOutCount.ToString() };
        var recvValue = new Label { AutoSize = true, Text = status.SyncedInCount.ToString() };
        var rejValue = new Label { AutoSize = true, Text = status.RejectedEventCount.ToString() };
        var trustedValue = new Label { AutoSize = true, Text = status.TrustedDeviceCount.ToString() };
        var pendingValue = new Label { AutoSize = true, Text = status.PendingPairingCount.ToString() };
        var errValue = new Label { AutoSize = true, Text = "None" };

        var workspacePanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        var workspaceValue = new TextBox { ReadOnly = true, Width = 360, Text = workspaceKey };
        var copyWorkspace = new Button { Text = T(locale, "copy"), Width = 80, Height = 28 };
        copyWorkspace.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText(workspaceValue.Text);
                history.Items.Insert(0, "[event] workspace key copied");
            }
            catch
            {
                history.Items.Insert(0, "[warn] workspace key copy failed");
            }
        };
        workspacePanel.Controls.Add(workspaceValue);
        workspacePanel.Controls.Add(copyWorkspace);

        var devicePanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        var deviceIdValue = new TextBox { ReadOnly = true, Width = 360, Text = deviceId };
        var copyDeviceId = new Button { Text = T(locale, "copy"), Width = 80, Height = 28 };
        copyDeviceId.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText(deviceIdValue.Text);
                history.Items.Insert(0, "[event] device id copied");
            }
            catch
            {
                history.Items.Insert(0, "[warn] device id copy failed");
            }
        };
        devicePanel.Controls.Add(deviceIdValue);
        devicePanel.Controls.Add(copyDeviceId);

        statusGrid.Controls.Add(new Label { Text = T(locale, "connection"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 0);
        statusGrid.Controls.Add(connectionValue, 1, 0);
        statusGrid.Controls.Add(new Label { Text = T(locale, "sent"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 1);
        statusGrid.Controls.Add(sentValue, 1, 1);
        statusGrid.Controls.Add(new Label { Text = T(locale, "received"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 2);
        statusGrid.Controls.Add(recvValue, 1, 2);
        statusGrid.Controls.Add(new Label { Text = T(locale, "rejected"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 3);
        statusGrid.Controls.Add(rejValue, 1, 3);
        statusGrid.Controls.Add(new Label { Text = T(locale, "trustedCount"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 4);
        statusGrid.Controls.Add(trustedValue, 1, 4);
        statusGrid.Controls.Add(new Label { Text = T(locale, "pendingPairing"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 5);
        statusGrid.Controls.Add(pendingValue, 1, 5);
        statusGrid.Controls.Add(new Label { Text = T(locale, "workspaceKey"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 6);
        statusGrid.Controls.Add(workspacePanel, 1, 6);
        statusGrid.Controls.Add(new Label { Text = T(locale, "deviceId"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 7);
        statusGrid.Controls.Add(devicePanel, 1, 7);
        statusGrid.Controls.Add(new Label { Text = T(locale, "lastError"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 8);
        statusGrid.Controls.Add(errValue, 1, 8);

        var quickActions = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = true,
            Dock = DockStyle.Fill
        };

        var sendHtml = new Button { Text = T(locale, "sendHtml"), Width = 120, Height = 32 };
        sendHtml.Click += (_, _) =>
        {
            var text = service.CaptureClipboard() ?? "(empty)";
            status.SyncedOutCount += 1;
            sentValue.Text = status.SyncedOutCount.ToString();
            history.Items.Insert(0, $"[out] text/plain · {text}");
            PersistRuntimeSnapshots();
        };

        var sendImageRef = new Button { Text = T(locale, "sendImage"), Width = 120, Height = 32 };
        sendImageRef.Click += (_, _) =>
        {
            status.SyncedOutCount += 1;
            sentValue.Text = status.SyncedOutCount.ToString();
            if (Clipboard.ContainsImage())
            {
                history.Items.Insert(0, "[out] image/png · clipboard-image");
            }
            else
            {
                history.Items.Insert(0, "[out] image/png · no-image-in-clipboard");
            }
            PersistRuntimeSnapshots();
        };

        var sendFileRef = new Button { Text = T(locale, "sendFile"), Width = 120, Height = 32 };
        sendFileRef.Click += (_, _) =>
        {
            status.SyncedOutCount += 1;
            sentValue.Text = status.SyncedOutCount.ToString();
            using var dialog = new OpenFileDialog
            {
                Title = "Select file to share reference",
                CheckFileExists = true,
                Multiselect = false
            };
            if (dialog.ShowDialog(form) == DialogResult.OK)
            {
                history.Items.Insert(0, $"[out] application/x-clipboard-file-ref · {dialog.FileName}");
            }
            else
            {
                history.Items.Insert(0, "[out] application/x-clipboard-file-ref · canceled");
            }
            PersistRuntimeSnapshots();
        };

        quickActions.Controls.Add(sendHtml);
        quickActions.Controls.Add(sendImageRef);
        quickActions.Controls.Add(sendFileRef);

        var loadedWebDav = service.LoadWebDavSettings();
        var loadedPublicRelay = service.LoadPublicRelaySettings();
        if (string.IsNullOrWhiteSpace(loadedPublicRelay.Bucket))
        {
            loadedPublicRelay = (loadedPublicRelay.Enabled, loadedPublicRelay.BaseUrl, workspaceKey ?? string.Empty);
        }

        var webdevCheck = new CheckBox { Text = T(locale, "webdev"), AutoSize = true, Checked = loadedWebDav.Enabled };
        var publicRelayCheck = new CheckBox { Text = T(locale, "publicRelay"), AutoSize = true, Checked = loadedPublicRelay.Enabled };
        var serverCheck = new CheckBox { Text = T(locale, "server"), AutoSize = true, Checked = store.Get("local_server_enabled") == "1" };
        var webdavUrlText = new TextBox { Width = 320, Text = loadedWebDav.BaseUrl };
        var webdavUserText = new TextBox { Width = 220, Text = loadedWebDav.Username };
        var webdavPasswordText = new TextBox { Width = 220, Text = loadedWebDav.Password, UseSystemPasswordChar = true };
        var publicRelayUrlText = new TextBox { Width = 320, Text = loadedPublicRelay.BaseUrl };
        var publicRelayBucketText = new TextBox { Width = 320, Text = loadedPublicRelay.Bucket };
        var webdavTestBtn = new Button { Text = T(locale, "testWebdev"), Width = 220, Height = 30 };
        var publicRelayTestBtn = new Button { Text = T(locale, "testPublicRelay"), Width = 220, Height = 30 };

        webdevCheck.CheckedChanged += (_, _) =>
        {
            if (webdevCheck.Checked)
            {
                publicRelayCheck.Checked = false;
            }

            service.SaveWebDavSettings(webdavUrlText.Text, webdavUserText.Text, webdavPasswordText.Text, webdevCheck.Checked);
        };

        publicRelayCheck.CheckedChanged += (_, _) =>
        {
            if (publicRelayCheck.Checked)
            {
                webdevCheck.Checked = false;
            }

            service.SavePublicRelaySettings(publicRelayUrlText.Text, publicRelayBucketText.Text, publicRelayCheck.Checked);
        };

        webdavTestBtn.Click += async (_, _) =>
        {
            service.SaveWebDavSettings(webdavUrlText.Text, webdavUserText.Text, webdavPasswordText.Text, webdevCheck.Checked);
            var ok = await service.TestWebDavConnectionAsync();
            if (ok)
            {
                status.LastErrorMessage = string.Empty;
                errValue.Text = "None";
                history.Items.Insert(0, "[event] webdev · connection ok");
            }
            else
            {
                status.LastErrorMessage = "WebDev connection failed";
                errValue.Text = status.LastErrorMessage;
                history.Items.Insert(0, "[error] webdev · connection failed");
            }
            PersistRuntimeSnapshots();
        };

        publicRelayTestBtn.Click += async (_, _) =>
        {
            service.SavePublicRelaySettings(publicRelayUrlText.Text, publicRelayBucketText.Text, publicRelayCheck.Checked);
            var ok = await service.TestPublicRelayConnectionAsync();
            if (ok)
            {
                status.LastErrorMessage = string.Empty;
                errValue.Text = "None";
                history.Items.Insert(0, "[event] public relay · connection ok");
            }
            else
            {
                status.LastErrorMessage = "Public relay connection failed";
                errValue.Text = status.LastErrorMessage;
                history.Items.Insert(0, "[error] public relay · connection failed");
            }
            PersistRuntimeSnapshots();
        };

        string ActiveSyncChannelName()
        {
            if (publicRelayCheck.Checked)
            {
                return "public";
            }

            if (webdevCheck.Checked)
            {
                return "webdav";
            }

            return "local";
        }

        async Task<bool> UploadClipboardToActiveChannelAsync(string text)
        {
            if (publicRelayCheck.Checked)
            {
                service.SavePublicRelaySettings(publicRelayUrlText.Text, publicRelayBucketText.Text, true);
                return await service.UploadClipboardToPublicRelayAsync(text);
            }

            if (webdevCheck.Checked)
            {
                service.SaveWebDavSettings(webdavUrlText.Text, webdavUserText.Text, webdavPasswordText.Text, true);
                return await service.UploadClipboardToWebDavAsync(text);
            }

            return true;
        }

        async Task<string?> DownloadClipboardFromActiveChannelAsync()
        {
            if (publicRelayCheck.Checked)
            {
                service.SavePublicRelaySettings(publicRelayUrlText.Text, publicRelayBucketText.Text, true);
                return await service.DownloadClipboardFromPublicRelayAsync();
            }

            if (webdevCheck.Checked)
            {
                service.SaveWebDavSettings(webdavUrlText.Text, webdavUserText.Text, webdavPasswordText.Text, true);
                return await service.DownloadClipboardFromWebDavAsync();
            }

            return null;
        }

        var autoSyncTimer = new System.Windows.Forms.Timer { Interval = 2500 };
        var syncInProgress = false;
        var lastUploadedText = string.Empty;
        var lastUploadedTextByTarget = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lastAppliedRemoteText = string.Empty;

        List<DeviceItem> BuildValidTrustedPeers()
        {
            var selfId = deviceId ?? string.Empty;
            return trustedDevices
                .Where(x => !string.Equals(x.DeviceId, selfId, StringComparison.OrdinalIgnoreCase))
                .Where(x => IsValidDeviceId(x.DeviceId))
                .GroupBy(x => x.DeviceId, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
        }

        async Task<bool> UploadClipboardToPublicRelayByTargetsAsync(string text)
        {
            service.SavePublicRelaySettings(publicRelayUrlText.Text, publicRelayBucketText.Text, true);
            var selfId = deviceId ?? string.Empty;
            var peers = BuildValidTrustedPeers();

            if (peers.Count == 0)
            {
                if (string.Equals(lastUploadedText, text, StringComparison.Ordinal))
                {
                    return true;
                }

                var uploaded = await service.UploadClipboardToPublicRelayAsync(text);
                if (uploaded)
                {
                    status.SyncedOutCount += 1;
                    sentValue.Text = status.SyncedOutCount.ToString();
                    lastUploadedText = text;
                    history.Items.Insert(0, "[out] public · legacy · " + text);
                }

                return uploaded;
            }

            var allUploaded = true;
            foreach (var peer in peers)
            {
                if (lastUploadedTextByTarget.TryGetValue(peer.DeviceId, out var lastText)
                    && string.Equals(lastText, text, StringComparison.Ordinal))
                {
                    continue;
                }

                var relayMessage = CreateRelayMessage(
                    workspaceKey ?? string.Empty,
                    selfId,
                    peer.DeviceId,
                    text,
                    "windows");

                var payload = JsonSerializer.Serialize(relayMessage);
                var uploaded = await service.UploadClipboardToPublicRelayForDeviceAsync(payload, peer.DeviceId, selfId);
                if (!uploaded)
                {
                    allUploaded = false;
                    history.Items.Insert(0, "[error] public · upload failed · to " + peer.DeviceId);
                    continue;
                }

                lastUploadedTextByTarget[peer.DeviceId] = text;
                status.SyncedOutCount += 1;
                sentValue.Text = status.SyncedOutCount.ToString();
                history.Items.Insert(0, "[out] public · to " + peer.DeviceId + " · " + text);
            }

            if (!string.Equals(lastUploadedText, text, StringComparison.Ordinal))
            {
                var compatUploaded = await service.UploadClipboardToPublicRelayAsync(text);
                if (compatUploaded)
                {
                    lastUploadedText = text;
                    history.Items.Insert(0, "[out] public · legacy-compat · " + text);
                }
                else
                {
                    allUploaded = false;
                    history.Items.Insert(0, "[warn] public · legacy-compat upload failed");
                }
            }

            return allUploaded;
        }

        async Task DownloadClipboardFromPublicRelayByTargetsAsync(string localText)
        {
            service.SavePublicRelaySettings(publicRelayUrlText.Text, publicRelayBucketText.Text, true);
            var selfId = deviceId ?? string.Empty;
            var peers = BuildValidTrustedPeers();

            foreach (var peer in peers)
            {
                var payload = await service.DownloadClipboardFromPublicRelayForDeviceAsync(selfId, peer.DeviceId);
                if (string.IsNullOrWhiteSpace(payload))
                {
                    continue;
                }

                if (!TryParseRelayMessage(payload, out var relayMessage))
                {
                    if (string.Equals(payload, localText, StringComparison.Ordinal)
                        || string.Equals(payload, lastAppliedRemoteText, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    service.ApplyRemoteText(payload);
                    lastAppliedRemoteText = payload;
                    status.SyncedInCount += 1;
                    recvValue.Text = status.SyncedInCount.ToString();
                    history.Items.Insert(0, "[in] public · from " + peer.DeviceId + " · " + payload);
                    return;
                }

                if (!string.Equals(relayMessage.ToDeviceId, selfId, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(relayMessage.FromDeviceId, selfId, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrWhiteSpace(relayMessage.WorkspaceKey)
                        && !string.Equals(relayMessage.WorkspaceKey, workspaceKey, StringComparison.Ordinal)))
                {
                    continue;
                }

                if (relayLastAppliedBySender.TryGetValue(relayMessage.FromDeviceId, out var appliedMessageId)
                    && string.Equals(appliedMessageId, relayMessage.MessageId, StringComparison.Ordinal))
                {
                    continue;
                }

                relayLastAppliedBySender[relayMessage.FromDeviceId] = relayMessage.MessageId;
                SaveRelayLastAppliedBySender(store, relayLastAppliedBySender);

                if (string.IsNullOrWhiteSpace(relayMessage.Text)
                    || string.Equals(relayMessage.Text, localText, StringComparison.Ordinal)
                    || string.Equals(relayMessage.Text, lastAppliedRemoteText, StringComparison.Ordinal))
                {
                    continue;
                }

                service.ApplyRemoteText(relayMessage.Text);
                lastAppliedRemoteText = relayMessage.Text;
                status.SyncedInCount += 1;
                recvValue.Text = status.SyncedInCount.ToString();
                history.Items.Insert(0, "[in] public · from " + relayMessage.FromDeviceId + " · " + relayMessage.Text);
                return;
            }

            var fallbackText = await service.DownloadClipboardFromPublicRelayAsync();
            if (string.IsNullOrWhiteSpace(fallbackText)
                || string.Equals(fallbackText, localText, StringComparison.Ordinal)
                || string.Equals(fallbackText, lastAppliedRemoteText, StringComparison.Ordinal))
            {
                return;
            }

            service.ApplyRemoteText(fallbackText);
            lastAppliedRemoteText = fallbackText;
            status.SyncedInCount += 1;
            recvValue.Text = status.SyncedInCount.ToString();
            history.Items.Insert(0, "[in] public · legacy · " + fallbackText);
        }

        async Task RunSharedClipboardSyncAsync(bool manualTriggered)
        {
            if (syncInProgress)
            {
                return;
            }

            syncInProgress = true;
            try
            {
                var localText = service.CaptureClipboard() ?? string.Empty;

                if (publicRelayCheck.Checked)
                {
                    var uploadOk = true;
                    if (!string.IsNullOrWhiteSpace(localText))
                    {
                        uploadOk = await UploadClipboardToPublicRelayByTargetsAsync(localText);
                    }

                    await DownloadClipboardFromPublicRelayByTargetsAsync(localText);

                    status.ConnectionState = uploadOk ? SyncConnectionState.Connected : SyncConnectionState.Degraded;
                    connectionValue.Text = status.ConnectionState.ToString();
                    status.LastErrorMessage = uploadOk ? string.Empty : "public relay upload failed";
                    errValue.Text = uploadOk ? "None" : status.LastErrorMessage;
                    PersistRuntimeSnapshots();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(localText) && !string.Equals(localText, lastUploadedText, StringComparison.Ordinal))
                {
                    var uploaded = await UploadClipboardToActiveChannelAsync(localText);
                    if (!uploaded)
                    {
                        status.LastErrorMessage = ActiveSyncChannelName() + " upload failed";
                        errValue.Text = status.LastErrorMessage;
                        history.Items.Insert(0, "[error] " + ActiveSyncChannelName() + " · upload failed");
                        connectionValue.Text = SyncConnectionState.Degraded.ToString();
                        PersistRuntimeSnapshots();
                        return;
                    }

                    status.SyncedOutCount += 1;
                    sentValue.Text = status.SyncedOutCount.ToString();
                    lastUploadedText = localText;
                    history.Items.Insert(0, "[out] " + ActiveSyncChannelName() + " · " + localText);
                }

                var remoteText = await DownloadClipboardFromActiveChannelAsync();
                if (!string.IsNullOrWhiteSpace(remoteText)
                    && !string.Equals(remoteText, lastAppliedRemoteText, StringComparison.Ordinal)
                    && !string.Equals(remoteText, localText, StringComparison.Ordinal))
                {
                    service.ApplyRemoteText(remoteText);
                    lastAppliedRemoteText = remoteText;
                    status.SyncedInCount += 1;
                    recvValue.Text = status.SyncedInCount.ToString();
                    history.Items.Insert(0, "[in] " + ActiveSyncChannelName() + " · " + remoteText);
                }

                if (manualTriggered && ActiveSyncChannelName() == "local")
                {
                    service.ApplyRemoteText("manual-sync:" + localText);
                    status.SyncedInCount += 1;
                    recvValue.Text = status.SyncedInCount.ToString();
                    history.Items.Insert(0, "[in] local · manual-sync:" + localText);
                }

                status.ConnectionState = SyncConnectionState.Connected;
                connectionValue.Text = status.ConnectionState.ToString();
                status.LastErrorMessage = string.Empty;
                errValue.Text = "None";
                PersistRuntimeSnapshots();
            }
            catch (Exception ex)
            {
                status.ConnectionState = SyncConnectionState.Degraded;
                connectionValue.Text = status.ConnectionState.ToString();
                status.LastErrorMessage = ex.Message;
                errValue.Text = status.LastErrorMessage;
                history.Items.Insert(0, "[error] sync · " + ex.GetType().Name);
                PersistRuntimeSnapshots();
            }
            finally
            {
                syncInProgress = false;
            }
        }

        autoSyncTimer.Tick += async (_, _) => await RunSharedClipboardSyncAsync(false);

        var manualSync = new Button { Text = T(locale, "manualSync"), Width = 180, Height = 34 };
        manualSync.Click += async (_, _) => await RunSharedClipboardSyncAsync(true);
        statusGrid.Controls.Add(quickActions, 0, 9);
        statusGrid.SetColumnSpan(quickActions, 2);
        statusGrid.Controls.Add(manualSync, 0, 10);
        statusGrid.SetColumnSpan(manualSync, 2);
        statusTab.Controls.Add(statusGrid);

        var deviceGrid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(16) };
        deviceGrid.RowCount = 3;
        deviceGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        deviceGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        deviceGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var deviceSearch = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Search devices..." };
        deviceSearch.AccessibleName = "Device Search";
        deviceSearch.AccessibleDescription = "Filter trusted devices by name or device id";
        var deviceList = new ListBox { Dock = DockStyle.Fill };
        var deviceEmptyHint = new Label { Dock = DockStyle.Top, AutoSize = true, Text = T(locale, "emptyDevicesHint"), ForeColor = Color.DimGray, Visible = false };
        var deviceClearFilter = new Button { Text = T(locale, "clearFilter"), Height = 30, Dock = DockStyle.Top, Visible = false };
        deviceClearFilter.Click += (_, _) => { deviceSearch.Text = string.Empty; RefreshDeviceList(); };
        deviceList.AccessibleName = "Trusted Device List";
        void RefreshDeviceList()
        {
            var query = deviceSearch.Text?.Trim() ?? string.Empty;
            deviceList.Items.Clear();
            foreach (var d in trustedDevices)
            {
                if (query.Length == 0 || d.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || d.DeviceId.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    deviceList.Items.Add(d);
                }
            }
            var isEmpty = deviceList.Items.Count == 0;
            deviceEmptyHint.Visible = isEmpty;
            deviceClearFilter.Visible = isEmpty && query.Length > 0;
        }
        deviceSearch.TextChanged += (_, _) => RefreshDeviceList();
        RefreshDeviceList();
        var revoke = new Button { Text = T(locale, "revoke"), Height = 34, Dock = DockStyle.Top };
        revoke.AccessibleName = "Revoke Device";
        revoke.AccessibleDescription = "Revoke selected trusted device";
        revoke.Click += (_, _) =>
        {
            if (deviceList.SelectedItem is DeviceItem selected)
            {
                // Show confirmation dialog
                var dialogResult = MessageBox.Show(
                    T(locale, "confirmRevoke"),
                    T(locale, "revoke"),
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question
                );
                
                if (dialogResult == DialogResult.OK)
                {
                    trustedDevices.RemoveAll(d => d.DeviceId == selected.DeviceId);
                    RefreshDeviceList();
                    status.TrustedDeviceCount = trustedDevices.Count;
                    trustedValue.Text = status.TrustedDeviceCount.ToString();
                    status.RejectedEventCount += 1;
                    rejValue.Text = status.RejectedEventCount.ToString();
                    status.LastErrorMessage = $"Revoked device: {selected.DeviceId}";
                    errValue.Text = status.LastErrorMessage;
                    history.Items.Insert(0, $"[event] revoke · {selected.DeviceId}");
                    PersistRuntimeSnapshots();
                }
            }
        };
        deviceGrid.Controls.Add(deviceSearch, 0, 0);
        deviceGrid.Controls.Add(deviceList, 0, 1);
        deviceGrid.Controls.Add(revoke, 0, 2);
        deviceGrid.Controls.Add(deviceEmptyHint, 0, 2);
        deviceGrid.Controls.Add(deviceClearFilter, 0, 2);
        devicesTab.Controls.Add(deviceGrid);

        var pairingGrid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(16) };
        pairingGrid.RowCount = 5;
        pairingGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        pairingGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        pairingGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        pairingGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        pairingGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        void AddOrUpdateTrustedDevice(string remoteDeviceId, string remoteName)
        {
            var existing = trustedDevices.FirstOrDefault(x => string.Equals(x.DeviceId, remoteDeviceId, StringComparison.Ordinal));
            var seen = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (existing is null)
            {
                trustedDevices.Insert(0, new DeviceItem(remoteDeviceId, remoteName, seen));
            }
            else
            {
                existing.LastSeen = seen;
            }

            status.TrustedDeviceCount = trustedDevices.Count;
            trustedValue.Text = status.TrustedDeviceCount.ToString();
            RefreshDeviceList();
        }

        var invitePanel = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Top
        };
        var inviteNameText = new TextBox { Width = 150, Text = Environment.MachineName };
        var createInviteButton = new Button { Text = T(locale, "createInvite"), Width = 120, Height = 30 };
        var inviteCodeOutput = new TextBox { Width = 350, ReadOnly = true };
        var copyInviteButton = new Button { Text = T(locale, "copy"), Width = 70, Height = 30 };
        var inviteCodeInput = new TextBox { Width = 350, PlaceholderText = T(locale, "invitePlaceholder") };
        var joinInviteButton = new Button { Text = T(locale, "joinByInvite"), Width = 130, Height = 30 };
        var directPairPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Top
        };
        var directDeviceIdInput = new TextBox { Width = 320, PlaceholderText = T(locale, "remoteDeviceIdPlaceholder") };
        var directDeviceNameInput = new TextBox { Width = 220, PlaceholderText = T(locale, "remoteDeviceNamePlaceholder") };
        var pairByDeviceIdButton = new Button { Text = T(locale, "pairByDeviceId"), Width = 150, Height = 30 };

        createInviteButton.Click += (_, _) =>
        {
            var deviceName = string.IsNullOrWhiteSpace(inviteNameText.Text) ? Environment.MachineName : inviteNameText.Text.Trim();
            var inviteCode = CreateInviteCode(workspaceKey ?? string.Empty, deviceId ?? string.Empty, deviceName, "windows");
            inviteCodeOutput.Text = inviteCode;
            try
            {
                Clipboard.SetText(inviteCode);
                history.Items.Insert(0, "[event] pairing invite created and copied");
            }
            catch
            {
                history.Items.Insert(0, "[event] pairing invite created");
            }
            PersistRuntimeSnapshots();
        };

        copyInviteButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(inviteCodeOutput.Text))
            {
                return;
            }

            try
            {
                Clipboard.SetText(inviteCodeOutput.Text);
                history.Items.Insert(0, "[event] invite copied");
            }
            catch
            {
                history.Items.Insert(0, "[warn] invite copy failed");
            }
        };

        pairByDeviceIdButton.Click += (_, _) =>
        {
            var remoteDeviceId = directDeviceIdInput.Text?.Trim() ?? string.Empty;
            var remoteDeviceName = directDeviceNameInput.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(remoteDeviceId))
            {
                status.LastErrorMessage = "Remote device ID is empty";
                errValue.Text = status.LastErrorMessage;
                return;
            }

            if (!IsValidDeviceId(remoteDeviceId))
            {
                status.LastErrorMessage = "Invalid remote device ID format";
                errValue.Text = status.LastErrorMessage;
                return;
            }

            if (string.Equals(remoteDeviceId, deviceId, StringComparison.OrdinalIgnoreCase))
            {
                status.LastErrorMessage = "Cannot pair with current device";
                errValue.Text = status.LastErrorMessage;
                return;
            }

            AddOrUpdateTrustedDevice(remoteDeviceId, string.IsNullOrWhiteSpace(remoteDeviceName) ? remoteDeviceId : remoteDeviceName);
            directDeviceIdInput.Text = string.Empty;
            directDeviceNameInput.Text = string.Empty;
            status.LastErrorMessage = string.Empty;
            errValue.Text = "None";
            history.Items.Insert(0, "[event] pairing · direct-id " + remoteDeviceId);
            PersistRuntimeSnapshots();
        };

        invitePanel.Controls.Add(new Label { Text = T(locale, "deviceName"), AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        invitePanel.Controls.Add(inviteNameText);
        invitePanel.Controls.Add(createInviteButton);
        invitePanel.Controls.Add(inviteCodeOutput);
        invitePanel.Controls.Add(copyInviteButton);
        invitePanel.Controls.Add(inviteCodeInput);
        invitePanel.Controls.Add(joinInviteButton);

        directPairPanel.Controls.Add(new Label { Text = T(locale, "remoteDeviceId"), AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        directPairPanel.Controls.Add(directDeviceIdInput);
        directPairPanel.Controls.Add(new Label { Text = T(locale, "remoteDeviceName"), AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        directPairPanel.Controls.Add(directDeviceNameInput);
        directPairPanel.Controls.Add(pairByDeviceIdButton);

        var pairingSearch = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Search requests..." };
        pairingSearch.AccessibleName = "Pairing Search";
        pairingSearch.AccessibleDescription = "Filter pairing requests by device name or platform";
        var pairingList = new ListBox { Dock = DockStyle.Fill };
        var pairingEmptyHint = new Label { Dock = DockStyle.Top, AutoSize = true, Text = T(locale, "emptyPairingHint"), ForeColor = Color.DimGray, Visible = false };
        var pairingClearFilter = new Button { Text = T(locale, "clearFilter"), Height = 30, Dock = DockStyle.Top, Visible = false };
        pairingClearFilter.Click += (_, _) => { pairingSearch.Text = string.Empty; RefreshPairingList(); };
        pairingList.AccessibleName = "Pairing Request List";
        void RefreshPairingList()
        {
            var query = pairingSearch.Text?.Trim() ?? string.Empty;
            pairingList.Items.Clear();
            foreach (var req in pairingRequests)
            {
                if (query.Length == 0 || req.DeviceName.Contains(query, StringComparison.OrdinalIgnoreCase) || req.Platform.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    pairingList.Items.Add(req);
                }
            }
            var isEmpty = pairingList.Items.Count == 0;
            pairingEmptyHint.Visible = isEmpty;
            pairingClearFilter.Visible = isEmpty && query.Length > 0;
        }
        pairingSearch.TextChanged += (_, _) => RefreshPairingList();
        RefreshPairingList();

        joinInviteButton.Click += (_, _) =>
        {
            if (!TryParseInviteCode(inviteCodeInput.Text, out var invitePayload, out var parseError))
            {
                status.LastErrorMessage = parseError;
                errValue.Text = status.LastErrorMessage;
                history.Items.Insert(0, "[error] invite parse failed");
                return;
            }

            if (!IsValidDeviceId(invitePayload.DeviceId))
            {
                status.LastErrorMessage = "Invalid invite device ID";
                errValue.Text = status.LastErrorMessage;
                return;
            }

            if (string.Equals(invitePayload.DeviceId, deviceId, StringComparison.Ordinal))
            {
                status.LastErrorMessage = "Cannot pair with current device";
                errValue.Text = status.LastErrorMessage;
                return;
            }

            if (!string.Equals(invitePayload.WorkspaceKey, workspaceKey, StringComparison.Ordinal))
            {
                workspaceKey = invitePayload.WorkspaceKey;
                service.SaveWorkspaceKey(workspaceKey);
                workspaceValue.Text = workspaceKey;
                if (string.IsNullOrWhiteSpace(publicRelayBucketText.Text))
                {
                    publicRelayBucketText.Text = workspaceKey;
                }
            }

            var autoApproveInvite = string.Equals(store.Get("pairing_policy"), "auto-approve-invite", StringComparison.OrdinalIgnoreCase);
            if (autoApproveInvite)
            {
                AddOrUpdateTrustedDevice(invitePayload.DeviceId, invitePayload.DeviceName);
                history.Items.Insert(0, "[event] pairing · auto-approved " + invitePayload.DeviceName);
            }
            else
            {
                if (pairingRequests.Any(x => string.Equals(x.DeviceId, invitePayload.DeviceId, StringComparison.Ordinal)))
                {
                    status.LastErrorMessage = "Pairing request already exists";
                    errValue.Text = status.LastErrorMessage;
                    return;
                }

                pairingRequests.Insert(0, new PairingRequestItem(
                    "req-" + Guid.NewGuid().ToString("N"),
                    invitePayload.DeviceId,
                    invitePayload.DeviceName,
                    invitePayload.Platform,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                ));
                RefreshPairingList();
                status.PendingPairingCount = pairingRequests.Count;
                pendingValue.Text = status.PendingPairingCount.ToString();
                history.Items.Insert(0, "[event] pairing request added · " + invitePayload.DeviceName);
            }

            status.LastErrorMessage = string.Empty;
            errValue.Text = "None";
            PersistRuntimeSnapshots();
        };

        var pairingActions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Top };
        var approve = new Button { Text = T(locale, "approve"), Width = 130, Height = 34 };
        var reject = new Button { Text = T(locale, "reject"), Width = 130, Height = 34 };
        approve.AccessibleName = "Approve Pairing Request";
        reject.AccessibleName = "Reject Pairing Request";

        approve.Click += (_, _) =>
        {
            if (pairingList.SelectedItem is PairingRequestItem selected)
            {
                pairingRequests.RemoveAll(r => r.RequestId == selected.RequestId);
                AddOrUpdateTrustedDevice(selected.DeviceId, selected.DeviceName);
                RefreshPairingList();
                status.PendingPairingCount = pairingRequests.Count;
                pendingValue.Text = status.PendingPairingCount.ToString();
                history.Items.Insert(0, $"[event] pairing · approved {selected.DeviceName}");
                PersistRuntimeSnapshots();
            }
        };

        reject.Click += (_, _) =>
        {
            if (pairingList.SelectedItem is PairingRequestItem selected)
            {
                // Show confirmation dialog
                var dialogResult = MessageBox.Show(
                    T(locale, "confirmReject"),
                    T(locale, "reject"),
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question
                );
                
                if (dialogResult == DialogResult.OK)
                {
                    pairingRequests.RemoveAll(r => r.RequestId == selected.RequestId);
                    RefreshPairingList();
                    status.PendingPairingCount = pairingRequests.Count;
                    pendingValue.Text = status.PendingPairingCount.ToString();
                    history.Items.Insert(0, $"[event] pairing · rejected {selected.DeviceName}");
                    PersistRuntimeSnapshots();
                }
            }
        };

        pairingActions.Controls.Add(approve);
        pairingActions.Controls.Add(reject);
        pairingGrid.Controls.Add(invitePanel, 0, 0);
        pairingGrid.Controls.Add(directPairPanel, 0, 1);
        pairingGrid.Controls.Add(pairingSearch, 0, 2);
        pairingGrid.Controls.Add(pairingList, 0, 3);
        pairingGrid.Controls.Add(pairingActions, 0, 4);
        pairingGrid.Controls.Add(pairingEmptyHint, 0, 4);
        pairingGrid.Controls.Add(pairingClearFilter, 0, 4);
        pairingTab.Controls.Add(pairingGrid);

        historyTab.Controls.Add(history);

        var settingsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 15,
            Padding = new Padding(16)
        };
        settingsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        settingsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var langCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        langCombo.Items.AddRange(new object[] { "zh-CN", "en-US" });
        langCombo.SelectedItem = locale;

        var themeCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        themeCombo.Items.AddRange(new object[] { "system", "light", "dark" });
        // Auto-detect system theme
        var systemDarkMode = DetectSystemDarkMode();
        themeCombo.SelectedItem = systemDarkMode ? "dark" : "light";

        var modeCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        modeCombo.Items.AddRange(new object[] { "manual", "auto" });
        modeCombo.SelectedItem = store.Get("sync_mode") ?? "manual";

        var spaceCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        spaceCombo.Items.AddRange(new object[] { "default", "work", "lab" });
        spaceCombo.SelectedItem = store.Get("space_id") ?? "default";

        var pairingPolicyCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        pairingPolicyCombo.Items.AddRange(new object[] { "manual-approve", "auto-approve-invite" });
        pairingPolicyCombo.SelectedItem = store.Get("pairing_policy") ?? "manual-approve";

        webdevCheck.Text = T(locale, "webdev");
        publicRelayCheck.Text = T(locale, "publicRelay");
        serverCheck.Text = T(locale, "server");

        langCombo.SelectedIndexChanged += (_, _) =>
        {
            if (langCombo.SelectedItem is string lang)
            {
                locale = lang;
                ApplyLocale(form, tabs, statusTab, devicesTab, historyTab, pairingTab, settingsTab, manualSync, revoke, approve, reject, webdevCheck, serverCheck, webdavTestBtn, locale);
                publicRelayCheck.Text = T(locale, "publicRelay");
                publicRelayTestBtn.Text = T(locale, "testPublicRelay");
                copyWorkspace.Text = T(locale, "copy");
                copyDeviceId.Text = T(locale, "copy");
                createInviteButton.Text = T(locale, "createInvite");
                joinInviteButton.Text = T(locale, "joinByInvite");
                copyInviteButton.Text = T(locale, "copy");
                inviteCodeInput.PlaceholderText = T(locale, "invitePlaceholder");
                pairByDeviceIdButton.Text = T(locale, "pairByDeviceId");
                directDeviceIdInput.PlaceholderText = T(locale, "remoteDeviceIdPlaceholder");
                directDeviceNameInput.PlaceholderText = T(locale, "remoteDeviceNamePlaceholder");
            }
        };

        themeCombo.SelectedIndexChanged += (_, _) =>
        {
            if ((themeCombo.SelectedItem as string) == "dark")
            {
                ApplyTheme(form, true);
            }
            else
            {
                ApplyTheme(form, false);
            }
        };

        modeCombo.SelectedIndexChanged += (_, _) =>
        {
            if (modeCombo.SelectedItem is string value)
            {
                store.Set("sync_mode", value);
                if (string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase))
                {
                    autoSyncTimer.Start();
                    history.Items.Insert(0, "[event] auto shared clipboard enabled");
                }
                else
                {
                    autoSyncTimer.Stop();
                    history.Items.Insert(0, "[event] auto shared clipboard disabled");
                }
            }
        };

        spaceCombo.SelectedIndexChanged += (_, _) =>
        {
            if (spaceCombo.SelectedItem is string value)
            {
                store.Set("space_id", value);
            }
        };

        pairingPolicyCombo.SelectedIndexChanged += (_, _) =>
        {
            if (pairingPolicyCombo.SelectedItem is string value)
            {
                store.Set("pairing_policy", value);
            }
        };

        serverCheck.CheckedChanged += (_, _) =>
        {
            store.Set("local_server_enabled", serverCheck.Checked ? "1" : "0");
        };

        // Apply system-detected theme on startup
        ApplyTheme(form, systemDarkMode);

        if (string.Equals(modeCombo.SelectedItem as string, "auto", StringComparison.OrdinalIgnoreCase))
        {
            autoSyncTimer.Start();
        }

        settingsGrid.Controls.Add(new Label { Text = T(locale, "language"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 0);
        settingsGrid.Controls.Add(langCombo, 1, 0);
        settingsGrid.Controls.Add(new Label { Text = T(locale, "theme"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 1);
        settingsGrid.Controls.Add(themeCombo, 1, 1);
        settingsGrid.Controls.Add(new Label { Text = T(locale, "syncMode"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 2);
        settingsGrid.Controls.Add(modeCombo, 1, 2);
        settingsGrid.Controls.Add(new Label { Text = T(locale, "space"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 3);
        settingsGrid.Controls.Add(spaceCombo, 1, 3);
        settingsGrid.Controls.Add(new Label { Text = T(locale, "pairingPolicy"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 4);
        settingsGrid.Controls.Add(pairingPolicyCombo, 1, 4);
        settingsGrid.Controls.Add(webdevCheck, 1, 5);
        settingsGrid.Controls.Add(new Label { Text = T(locale, "webdevUrl"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 6);
        settingsGrid.Controls.Add(webdavUrlText, 1, 6);
        settingsGrid.Controls.Add(new Label { Text = T(locale, "webdevUser"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 7);
        settingsGrid.Controls.Add(webdavUserText, 1, 7);
        settingsGrid.Controls.Add(new Label { Text = T(locale, "webdevPassword"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 8);
        settingsGrid.Controls.Add(webdavPasswordText, 1, 8);
        settingsGrid.Controls.Add(publicRelayCheck, 1, 9);
        settingsGrid.Controls.Add(new Label { Text = T(locale, "publicRelayUrl"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 10);
        settingsGrid.Controls.Add(publicRelayUrlText, 1, 10);
        settingsGrid.Controls.Add(new Label { Text = T(locale, "publicRelayBucket"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 11);
        settingsGrid.Controls.Add(publicRelayBucketText, 1, 11);
        settingsGrid.Controls.Add(serverCheck, 1, 12);
        settingsGrid.Controls.Add(webdavTestBtn, 1, 13);
        settingsGrid.Controls.Add(publicRelayTestBtn, 1, 14);
        settingsTab.Controls.Add(settingsGrid);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(tabs, 0, 1);
        form.Controls.Add(root);

        var exitRequested = false;
        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Show", null, (_, _) =>
        {
            form.Show();
            form.WindowState = FormWindowState.Normal;
            form.BringToFront();
        });
        trayMenu.Items.Add("Quick Manual Sync", null, (_, _) => manualSync.PerformClick());
        trayMenu.Items.Add("Exit", null, (_, _) =>
        {
            exitRequested = true;
            form.Close();
        });

        var notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Clipboard Sync",
            ContextMenuStrip = trayMenu,
            Visible = true
        };
        notifyIcon.DoubleClick += (_, _) =>
        {
            form.Show();
            form.WindowState = FormWindowState.Normal;
            form.BringToFront();
        };

        form.Resize += (_, _) =>
        {
            if (form.WindowState == FormWindowState.Minimized)
            {
                form.Hide();
                notifyIcon.ShowBalloonTip(1200, "Clipboard Sync", "应用仍在托盘中运行", ToolTipIcon.Info);
                ApplyLocale(form, tabs, statusTab, devicesTab, historyTab, pairingTab, settingsTab, manualSync, revoke, approve, reject, webdevCheck, serverCheck, webdavTestBtn, locale);
                publicRelayCheck.Text = T(locale, "publicRelay");
                publicRelayTestBtn.Text = T(locale, "testPublicRelay");
                createInviteButton.Text = T(locale, "createInvite");
                joinInviteButton.Text = T(locale, "joinByInvite");
                copyInviteButton.Text = T(locale, "copy");
                pairByDeviceIdButton.Text = T(locale, "pairByDeviceId");
                directDeviceIdInput.PlaceholderText = T(locale, "remoteDeviceIdPlaceholder");
                directDeviceNameInput.PlaceholderText = T(locale, "remoteDeviceNamePlaceholder");
                copyWorkspace.Text = T(locale, "copy");
                copyDeviceId.Text = T(locale, "copy");
            }
        };

        form.FormClosing += (_, e) =>
        {
            if (!exitRequested)
            {
                e.Cancel = true;
                form.Hide();
            }
            else
            {
                autoSyncTimer.Stop();
                autoSyncTimer.Dispose();
                SaveRuntimeSnapshots(store, trustedDevices, pairingRequests, history);
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }
        };

        Application.Run(form);
        return 0;
    }

    private static List<DeviceItem> LoadDevices(ISecureStore store)
    {
        var raw = store.Get("trusted_devices_json");
        if (string.IsNullOrWhiteSpace(raw)) return new List<DeviceItem>();
        try
        {
            var payload = JsonSerializer.Deserialize<List<DeviceSnapshot>>(raw) ?? new List<DeviceSnapshot>();
            return payload.Select(x => new DeviceItem(x.DeviceId ?? string.Empty, x.Name ?? string.Empty, x.LastSeen ?? string.Empty)).ToList();
        }
        catch
        {
            return new List<DeviceItem>();
        }
    }

    private static List<PairingRequestItem> LoadPairingRequests(ISecureStore store)
    {
        var raw = store.Get("pairing_requests_json");
        if (string.IsNullOrWhiteSpace(raw)) return new List<PairingRequestItem>();
        try
        {
            var payload = JsonSerializer.Deserialize<List<PairingSnapshot>>(raw) ?? new List<PairingSnapshot>();
            return payload.Select(x => new PairingRequestItem(
                x.RequestId ?? string.Empty,
                x.DeviceId ?? string.Empty,
                x.DeviceName ?? string.Empty,
                x.Platform ?? string.Empty,
                x.RequestedAt ?? string.Empty
            )).ToList();
        }
        catch
        {
            return new List<PairingRequestItem>();
        }
    }

    private static List<string> LoadHistory(ISecureStore store)
    {
        var raw = store.Get("history_items_json");
        if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(raw) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static void SaveRuntimeSnapshots(ISecureStore store, List<DeviceItem> devices, List<PairingRequestItem> pairingRequests, ListBox history)
    {
        var devicesPayload = devices.Select(x => new DeviceSnapshot { DeviceId = x.DeviceId, Name = x.Name, LastSeen = x.LastSeen }).ToList();
        var pairingPayload = pairingRequests.Select(x => new PairingSnapshot { RequestId = x.RequestId, DeviceId = x.DeviceId, DeviceName = x.DeviceName, Platform = x.Platform, RequestedAt = x.RequestedAt }).ToList();
        var historyPayload = history.Items.Cast<object>().Select(x => x?.ToString() ?? string.Empty).Take(300).ToList();

        store.Set("trusted_devices_json", JsonSerializer.Serialize(devicesPayload));
        store.Set("pairing_requests_json", JsonSerializer.Serialize(pairingPayload));
        store.Set("history_items_json", JsonSerializer.Serialize(historyPayload));
    }

    private sealed class DeviceSnapshot
    {
        public string? DeviceId { get; set; }
        public string? Name { get; set; }
        public string? LastSeen { get; set; }
    }

    private sealed class PairingSnapshot
    {
        public string? RequestId { get; set; }
        public string? DeviceId { get; set; }
        public string? DeviceName { get; set; }
        public string? Platform { get; set; }
        public string? RequestedAt { get; set; }
    }

    private sealed class InvitePayload
    {
        public string WorkspaceKey { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Platform { get; set; } = "windows";
    }

    private sealed class RelayMessage
    {
        public string MessageId { get; set; } = string.Empty;
        public string WorkspaceKey { get; set; } = string.Empty;
        public string FromDeviceId { get; set; } = string.Empty;
        public string ToDeviceId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string SentAt { get; set; } = string.Empty;
        public string Client { get; set; } = "windows";
    }

    private static RelayMessage CreateRelayMessage(string workspaceKey, string fromDeviceId, string toDeviceId, string text, string client)
    {
        return new RelayMessage
        {
            MessageId = Guid.NewGuid().ToString("N"),
            WorkspaceKey = workspaceKey,
            FromDeviceId = fromDeviceId,
            ToDeviceId = toDeviceId,
            Text = text,
            SentAt = DateTime.UtcNow.ToString("O"),
            Client = client
        };
    }

    private static bool TryParseRelayMessage(string raw, out RelayMessage message)
    {
        message = new RelayMessage();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<RelayMessage>(raw);
            if (payload is null
                || string.IsNullOrWhiteSpace(payload.MessageId)
                || string.IsNullOrWhiteSpace(payload.FromDeviceId)
                || string.IsNullOrWhiteSpace(payload.ToDeviceId))
            {
                return false;
            }

            message = payload;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Dictionary<string, string> LoadRelayLastAppliedBySender(ISecureStore store)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var raw = store.Get("relay_last_applied_json");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return map;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(raw);
            if (payload is null)
            {
                return map;
            }

            foreach (var pair in payload)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                {
                    map[pair.Key] = pair.Value;
                }
            }
        }
        catch
        {
            return map;
        }

        return map;
    }

    private static void SaveRelayLastAppliedBySender(ISecureStore store, Dictionary<string, string> map)
    {
        store.Set("relay_last_applied_json", JsonSerializer.Serialize(map));
    }

    private static string GenerateDeviceId()
    {
        return "win-" + Guid.NewGuid().ToString("N").Substring(0, 12);
    }

    private static bool IsValidDeviceId(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return false;
        }

        var normalized = deviceId.Trim();
        if (normalized.Length < 6 || normalized.Length > 64)
        {
            return false;
        }

        var dashIndex = normalized.IndexOf('-');
        if (dashIndex <= 0 || dashIndex >= normalized.Length - 1)
        {
            return false;
        }

        foreach (var ch in normalized)
        {
            if (!(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.'))
            {
                return false;
            }
        }

        return true;
    }

    private static string CreateInviteCode(string workspaceKey, string deviceId, string deviceName, string platform)
    {
        var payload = new InvitePayload
        {
            WorkspaceKey = workspaceKey,
            DeviceId = deviceId,
            DeviceName = deviceName,
            Platform = platform
        };

        var json = JsonSerializer.Serialize(payload);
        var raw = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(raw)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static bool TryParseInviteCode(string inviteCode, out InvitePayload payload, out string error)
    {
        payload = new InvitePayload();
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(inviteCode))
        {
            error = "Invite code is empty";
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

            var raw = Convert.FromBase64String(normalized);
            var json = Encoding.UTF8.GetString(raw);
            var parsed = JsonSerializer.Deserialize<InvitePayload>(json);
            if (parsed is null
                || string.IsNullOrWhiteSpace(parsed.WorkspaceKey)
                || string.IsNullOrWhiteSpace(parsed.DeviceId)
                || string.IsNullOrWhiteSpace(parsed.DeviceName))
            {
                error = "Invalid invite payload";
                return false;
            }

            payload = parsed;
            return true;
        }
        catch
        {
            error = "Invite code format error";
            return false;
        }
    }

    private static string T(string locale, string key)
    {
        if (I18n.TryGetValue(locale, out var map) && map.TryGetValue(key, out var value))
        {
            return value;
        }
        return key;
    }

    private static void ApplyLocale(
        Form form,
        TabControl tabs,
        TabPage statusTab,
        TabPage devicesTab,
        TabPage historyTab,
        TabPage pairingTab,
        TabPage settingsTab,
        Button manual,
        Button revoke,
        Button approve,
        Button reject,
        CheckBox webdev,
        CheckBox server,
        Button testWebdev,
        string locale)
    {
        form.Text = T(locale, "title");
        statusTab.Text = T(locale, "status");
        devicesTab.Text = T(locale, "devices");
        historyTab.Text = T(locale, "history");
        pairingTab.Text = T(locale, "pairing");
        settingsTab.Text = T(locale, "settings");
        manual.Text = T(locale, "manualSync");
        revoke.Text = T(locale, "revoke");
        approve.Text = T(locale, "approve");
        reject.Text = T(locale, "reject");
        webdev.Text = T(locale, "webdev");
        server.Text = T(locale, "server");
        testWebdev.Text = T(locale, "testWebdev");
        tabs.Refresh();
    }

    private static void ApplyTheme(Control root, bool dark)
    {
        var background = dark ? Color.FromArgb(17, 24, 39) : Color.FromArgb(240, 245, 252);
        var surface = dark ? Color.FromArgb(30, 41, 59) : Color.White;
        var foreground = dark ? Color.FromArgb(241, 245, 249) : Color.FromArgb(26, 35, 50);
        var accent = dark ? Color.FromArgb(56, 189, 248) : Color.FromArgb(13, 110, 253);
        ApplyThemeRecursive(root, background, surface, foreground, accent, dark);
    }

    private static void ApplyThemeRecursive(Control c, Color background, Color surface, Color foreground, Color accent, bool dark)
    {
        c.ForeColor = foreground;

        switch (c)
        {
            case Form:
                c.BackColor = background;
                break;
            case TabControl:
            case TabPage:
            case ListBox:
            case TableLayoutPanel:
            case FlowLayoutPanel:
                c.BackColor = surface;
                break;
            case Button button:
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 0;
                button.BackColor = accent;
                button.ForeColor = Color.White;
                break;
            case TextBox:
            case ComboBox:
                c.BackColor = dark ? Color.FromArgb(15, 23, 42) : Color.White;
                break;
            case Label:
                c.BackColor = Color.Transparent;
                break;
            default:
                c.BackColor = background;
                break;
        }

        foreach (Control child in c.Controls)
        {
            ApplyThemeRecursive(child, background, surface, foreground, accent, dark);
        }
    }

    private static bool DetectSystemDarkMode()
    {
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                if (key != null)
                {
                    var value = key.GetValue("AppsUseLightTheme", 1);
                    return value != null && (int)value == 0;
                }
            }
        }
        catch
        {
            // If registry access fails, default to light mode
        }
        return false;
    }

    private static Bitmap DrawPlatformBadge(int width, int height)
    {
        var bmp = new Bitmap(width, height);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var bgBrush = new LinearGradientBrush(new Rectangle(0, 0, width, height), Color.FromArgb(21, 82, 139), Color.FromArgb(31, 165, 142), 42f))
        {
            using var bgPath = CreateRoundedRectPath(0, 0, width - 1, height - 1, 10f);
            g.FillPath(bgBrush, bgPath);
        }

        using var panelBrush = new SolidBrush(Color.FromArgb(235, 245, 255));
        using (var panelPath = CreateRoundedRectPath(width / 6f, height / 5f, width * 0.34f, height * 0.56f, 7f))
        {
            g.FillPath(panelBrush, panelPath);
        }

        using var clipPen = new Pen(Color.FromArgb(252, 193, 82), 2.6f);
        using (var clipPath = CreateRoundedRectPath(width * 0.57f, height * 0.23f, width * 0.24f, height * 0.46f, 6f))
        {
            g.DrawPath(clipPen, clipPath);
        }
        using var dotBrush = new SolidBrush(Color.FromArgb(247, 102, 94));
        g.FillEllipse(dotBrush, width * 0.68f, height * 0.63f, width * 0.1f, height * 0.1f);

        return bmp;
    }

    private static GraphicsPath CreateRoundedRectPath(float x, float y, float width, float height, float radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2f;
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + width - d, y, d, d, 270, 90);
        path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
        path.AddArc(x, y + height - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
