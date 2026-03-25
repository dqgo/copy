namespace ClipboardSync.Windows;

using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System;

internal sealed class MemoryClipboardReader : IClipboardReader
{
    private readonly string? _value;

    public MemoryClipboardReader(string? value)
    {
        _value = value;
    }

    public string? ReadText() => _value;
}

internal sealed class MemoryClipboardWriter : IClipboardWriter
{
    public string LastValue { get; private set; } = string.Empty;

    public void WriteText(string text)
    {
        LastValue = text;
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
            ["retry"] = "重试"
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
            ["retry"] = "Retry"
        }
    };

    [STAThread]
    private static int Main()
    {
        ApplicationConfiguration.Initialize();

        var reader = new MemoryClipboardReader("hello");
        var writer = new MemoryClipboardWriter();
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
            ConnectionState = SyncConnectionState.Connected,
            SyncedOutCount = 1,
            SyncedInCount = 1,
            RejectedEventCount = 0,
            TrustedDeviceCount = 3,
            PendingPairingCount = 2,
            LastErrorMessage = string.Empty
        };

        var trustedDevices = new List<DeviceItem>
        {
            new DeviceItem("win-local", "Windows Desktop", "just now"),
            new DeviceItem("android-main", "Android Phone", "2 min ago"),
            new DeviceItem("ios-handset", "iPhone", "6 min ago")
        };

        var history = new ListBox { Dock = DockStyle.Fill };
        history.Items.Add("[out] text/plain · hello");
        history.Items.Add("[in] text/plain · reply from mobile");
        history.Items.Add($"[secure-store] workspace key loaded · {workspaceKey[..Math.Min(12, workspaceKey.Length)]}...");

        var pairingRequests = new List<PairingRequestItem>
        {
            new PairingRequestItem("req-win-001", "iPad Air", "ios", "10:24"),
            new PairingRequestItem("req-win-002", "Pixel 9", "android", "10:26")
        };

        var locale = "zh-CN";

        var form = new Form
        {
            Text = T(locale, "title"),
            Width = 980,
            Height = 640,
            StartPosition = FormStartPosition.CenterScreen
        };

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
            Height = 36,
            Dock = DockStyle.Top
        };

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
            status.SyncedOutCount += 1;
            sentValue.Text = status.SyncedOutCount.ToString();
            history.Items.Insert(0, "[out] text/html · <b>Clipboard Sync</b>");
        };

        var sendImageRef = new Button { Text = T(locale, "sendImage"), Width = 120, Height = 32 };
        sendImageRef.Click += (_, _) =>
        {
            status.SyncedOutCount += 1;
            sentValue.Text = status.SyncedOutCount.ToString();
            history.Items.Insert(0, "[out] image/png · screenshot.png (ref)");
        };

        var sendFileRef = new Button { Text = T(locale, "sendFile"), Width = 120, Height = 32 };
        sendFileRef.Click += (_, _) =>
        {
            status.SyncedOutCount += 1;
            sentValue.Text = status.SyncedOutCount.ToString();
            history.Items.Insert(0, "[out] application/x-clipboard-file-ref · C:/tmp/demo.txt");
        };

        quickActions.Controls.Add(sendHtml);
        quickActions.Controls.Add(sendImageRef);
        quickActions.Controls.Add(sendFileRef);

        var manualSync = new Button { Text = T(locale, "manualSync"), Width = 180, Height = 34 };
        manualSync.Click += (_, _) =>
        {
            var text = service.CaptureClipboard() ?? "empty";
            service.ApplyRemoteText($"manual-sync:{text}");
            status.SyncedOutCount += 1;
            status.SyncedInCount += 1;
            sentValue.Text = status.SyncedOutCount.ToString();
            recvValue.Text = status.SyncedInCount.ToString();
            history.Items.Insert(0, $"[out] text/plain · {text}");
            errValue.Text = "None";
        };
        statusGrid.Controls.Add(quickActions, 0, 5);
        statusGrid.Controls.Add(quickActions, 0, 7);
        statusGrid.SetColumnSpan(quickActions, 2);
        statusGrid.Controls.Add(manualSync, 0, 8);
        statusGrid.SetColumnSpan(manualSync, 2);
        statusTab.Controls.Add(statusGrid);

        var deviceGrid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(16) };
        deviceGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        deviceGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var deviceList = new ListBox { Dock = DockStyle.Fill };
        foreach (var d in trustedDevices)
        {
            deviceList.Items.Add(d);
        }
        var revoke = new Button { Text = T(locale, "revoke"), Height = 34, Dock = DockStyle.Top };
        revoke.Click += (_, _) =>
        {
            if (deviceList.SelectedItem is DeviceItem selected)
            {
                deviceList.Items.Remove(selected);
                status.RejectedEventCount += 1;
                rejValue.Text = status.RejectedEventCount.ToString();
                status.LastErrorMessage = $"Revoked device: {selected.DeviceId}";
                errValue.Text = status.LastErrorMessage;
                history.Items.Insert(0, $"[event] revoke · {selected.DeviceId}");
            }
        };
        deviceGrid.Controls.Add(deviceList, 0, 0);
        deviceGrid.Controls.Add(revoke, 0, 1);
        devicesTab.Controls.Add(deviceGrid);

        var pairingGrid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(16) };
        pairingGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        pairingGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var pairingList = new ListBox { Dock = DockStyle.Fill };
        foreach (var req in pairingRequests)
        {
            pairingList.Items.Add(req);
        }

        var pairingActions = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Top };
        var approve = new Button { Text = T(locale, "approve"), Width = 130, Height = 34 };
        var reject = new Button { Text = T(locale, "reject"), Width = 130, Height = 34 };

        approve.Click += (_, _) =>
        {
            if (pairingList.SelectedItem is PairingRequestItem selected)
            {
                pairingList.Items.Remove(selected);
                status.PendingPairingCount = Math.Max(0, status.PendingPairingCount - 1);
                status.TrustedDeviceCount += 1;
                pendingValue.Text = status.PendingPairingCount.ToString();
                trustedValue.Text = status.TrustedDeviceCount.ToString();
                history.Items.Insert(0, $"[event] pairing · approved {selected.DeviceName}");
            }
        };

        reject.Click += (_, _) =>
        {
            if (pairingList.SelectedItem is PairingRequestItem selected)
            {
                pairingList.Items.Remove(selected);
                status.PendingPairingCount = Math.Max(0, status.PendingPairingCount - 1);
                pendingValue.Text = status.PendingPairingCount.ToString();
                history.Items.Insert(0, $"[event] pairing · rejected {selected.DeviceName}");
            }
        };

        pairingActions.Controls.Add(approve);
        pairingActions.Controls.Add(reject);
        pairingGrid.Controls.Add(pairingList, 0, 0);
        pairingGrid.Controls.Add(pairingActions, 0, 1);
        pairingTab.Controls.Add(pairingGrid);

        historyTab.Controls.Add(history);

        var settingsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(16)
        };
        settingsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        settingsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var langCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        langCombo.Items.AddRange(new object[] { "zh-CN", "en-US" });
        langCombo.SelectedItem = locale;

        var themeCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        themeCombo.Items.AddRange(new object[] { "system", "light", "dark" });
        themeCombo.SelectedItem = "system";

        var modeCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        modeCombo.Items.AddRange(new object[] { "manual", "auto" });
        modeCombo.SelectedItem = "manual";

        var spaceCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        spaceCombo.Items.AddRange(new object[] { "default", "work", "lab" });
        spaceCombo.SelectedItem = "default";

        var pairingPolicyCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        pairingPolicyCombo.Items.AddRange(new object[] { "manual-approve", "auto-approve-invite" });
        pairingPolicyCombo.SelectedItem = "manual-approve";

        var webdevCheck = new CheckBox { Text = T(locale, "webdev"), AutoSize = true };
        var serverCheck = new CheckBox { Text = T(locale, "server"), AutoSize = true };

        langCombo.SelectedIndexChanged += (_, _) =>
        {
            if (langCombo.SelectedItem is string lang)
            {
                locale = lang;
                ApplyLocale(form, tabs, statusTab, devicesTab, historyTab, pairingTab, settingsTab, manualSync, revoke, approve, reject, webdevCheck, serverCheck, locale);
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
        settingsGrid.Controls.Add(serverCheck, 1, 6);
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
                ApplyLocale(form, tabs, statusTab, devicesTab, historyTab, pairingTab, settingsTab, manualSync, revoke, approve, reject, webdevCheck, serverCheck, locale);
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
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }
        };

        Application.Run(form);
        return 0;
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
            ApplyThemeRecursive(child, b