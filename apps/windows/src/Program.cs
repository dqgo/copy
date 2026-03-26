namespace ClipboardSync.Windows;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System;
using System.Linq;
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
    public string DeviceName { get; }
    public string Platform { get; }
    public string RequestedAt { get; }

    public PairingRequestItem(string requestId, string deviceName, string platform, string requestedAt)
    {
        RequestId = requestId;
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
            ["server"] = "启用本地服务模式",
            ["sendHtml"] = "发送 HTML",
            ["sendImage"] = "发送图片",
            ["sendFile"] = "发送文件",
            ["manualSyncBtn"] = "手动同步",
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
            ["server"] = "Enable Local Server Mode",
            ["sendHtml"] = "Send HTML",
            ["sendImage"] = "Send Image",
            ["sendFile"] = "Send File",
            ["manualSyncBtn"] = "Manual Sync",
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
            history.Items.Add("[info] workspace key loaded");
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
            RowCount = 9,
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
        statusGrid.Controls.Add(new Label { Text = T(locale, "lastError"), AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) }, 0, 6);
        statusGrid.Controls.Add(errValue, 1, 6);

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
        var webdevCheck = new CheckBox { Text = T(locale, "webdev"), AutoSize = true, Checked = loadedWebDav.Enabled };
        var serverCheck = new CheckBox { Text = T(locale, "server"), AutoSize = true, Checked = store.Get("local_server_enabled") == "1" };
        var webdavUrlText = new TextBox { Width = 320, Text = loadedWebDav.BaseUrl };
        var webdavUserText = new TextBox { Width = 220, Text = loadedWebDav.Username };
        var webdavPasswordText = new TextBox { Width = 220, Text = loadedWebDav.Password, UseSystemPasswordChar = true };
        var webdavTestBtn = new Button { Text = T(locale, "testWebdev"), Width = 220, Height = 30 };
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
        var manualSync = new Button { Text = T(locale, "manualSync"), Width = 180, Height = 34 };
        manualSync.Click += async (_, _) =>
        {
            var text = service.CaptureClipboard() ?? "empty";
            if (webdevCheck.Checked)
            {
                service.SaveWebDavSettings(webdavUrlText.Text, webdavUserText.Text, webdavPasswordText.Text, true);
                var uploaded = await service.UploadClipboardToWebDavAsync(text);
                if (!uploaded)
                {
                    status.LastErrorMessage = "WebDev upload failed";
                    errValue.Text = status.LastErrorMessage;
                    history.Items.Insert(0, "[error] webdev · upload failed");
                    PersistRuntimeSnapshots();
                    return;
                }

                var remote = await service.DownloadClipboardFromWebDavAsync();
                if (!string.IsNullOrWhiteSpace(remote))
                {
                    service.ApplyRemoteText(remote);
                    history.Items.Insert(0, $"[in] text/plain · {remote}");
                }
            }
            else
            {
                service.ApplyRemoteText($"manual-sync:{text}");
                history.Items.Insert(0, $"[out] text/plain · {text}");
            }

            status.SyncedOutCount += 1;
            status.SyncedInCount += 1;
            sentValue.Text = status.SyncedOutCount.ToString();
            recvValue.Text = status.SyncedInCount.ToString();
            errValue.Text = "None";
            PersistRuntimeSnapshots();
        };
        statusGrid.Controls.Add(quickActions, 0, 5);
        statusGrid.Controls.Add(quickActions, 0, 7);
        statusGrid.SetColumnSpan(quickActions, 2);
        statusGrid.Controls.Add(manualSync, 0, 8);
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
        pairingGrid.RowCount = 3;
        pairingGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        pairingGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        pairingGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
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
                RefreshPairingList();
                status.PendingPairingCount = Math.Max(0, status.PendingPairingCount - 1);
                status.TrustedDeviceCount += 1;
                pendingValue.Text = status.PendingPairingCount.ToString();
                trustedValue.Text = status.TrustedDeviceCount.ToString();
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
                    status.PendingPairingCount = Math.Max(0, status.PendingPairingCount - 1);
                    pendingValue.Text = status.PendingPairingCount.ToString();
                    history.Items.Insert(0, $"[event] pairing · rejected {selected.DeviceName}");
                    PersistRuntimeSnapshots();
                }
            }
        };

        pairingActions.Controls.Add(approve);
        pairingActions.Controls.Add(reject);
        pairingGrid.Controls.Add(pairingSearch, 0, 0);
        pairingGrid.Controls.Add(pairingList, 0, 1);
        pairingGrid.Controls.Add(pairingActions, 0, 2);
        pairingGrid.Controls.Add(pairingEmptyHint, 0, 2);
        pairingGrid.Controls.Add(pairingClearFilter, 0, 2);
        pairingTab.Controls.Add(pairingGrid);

        historyTab.Controls.Add(history);

        var settingsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 11,
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
        serverCheck.Text = T(locale, "server");

        langCombo.SelectedIndexChanged += (_, _) =>
        {
            if (langCombo.SelectedItem is string lang)
            {
                locale = lang;
                ApplyLocale(form, tabs, statusTab, devicesTab, historyTab, pairingTab, settingsTab, manualSync, revoke, approve, reject, webdevCheck, serverCheck, webdavTestBtn, locale);
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
        settingsGrid.Controls.Add(serverCheck, 1, 9);
        settingsGrid.Controls.Add(webdavTestBtn, 1, 10);
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
            return payload.Select(x => new PairingRequestItem(x.RequestId ?? string.Empty, x.DeviceName ?? string.Empty, x.Platform ?? string.Empty, x.RequestedAt ?? string.Empty)).ToList();
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
        var pairingPayload = pairingRequests.Select(x => new PairingSnapshot { RequestId = x.RequestId, DeviceName = x.DeviceName, Platform = x.Platform, RequestedAt = x.RequestedAt }).ToList();
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
        public string? DeviceName { get; set; }
        public string? Platform { get; set; }
        public string? RequestedAt { get; set; }
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
        var bg = dark ? Color.FromArgb(26, 26, 32) : Color.White;
        var fg = dark ? Color.FromArgb(230, 230, 240) : Color.Black;
        ApplyThemeRecursive(root, bg, fg);
    }

    private static void ApplyThemeRecursive(Control c, Color bg, Color fg)
    {
        c.BackColor = bg;
        c.ForeColor = fg;
        foreach (Control child in c.Controls)
        {
            ApplyThemeRecursive(child, bg, fg);
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
