using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClipboardSync_Windows_WinUI.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace ClipboardSync_Windows_WinUI.Pages;

public sealed partial class HomePage : Page
{
    private readonly AppSession _session = AppSession.Instance;
    private readonly DispatcherTimer _autoSyncTimer = new();
    private readonly List<TrustedDevice> _trustedDevices;
    private bool _syncBusy;

    public HomePage()
    {
        InitializeComponent();

        WorkspaceKeyText.Text = _session.WorkspaceKey;
        DeviceIdText.Text = _session.DeviceId;
        InviteNameText.Text = Environment.MachineName;

        _trustedDevices = _session.LoadTrustedDevices();
        AppendHistory("[info] trusted devices loaded: " + _trustedDevices.Count);

        _autoSyncTimer.Interval = TimeSpan.FromSeconds(3);
        _autoSyncTimer.Tick += AutoSyncTimer_Tick;

        var savedMode = _session.Store.Get("sync_mode") ?? "manual";
        AutoSyncSwitch.IsOn = string.Equals(savedMode, "auto", StringComparison.OrdinalIgnoreCase);
        if (AutoSyncSwitch.IsOn)
        {
            _autoSyncTimer.Start();
        }
    }

    private void CopyWorkspaceButton_Click(object sender, RoutedEventArgs e)
    {
        CopyText(WorkspaceKeyText.Text);
        AppendHistory("[event] workspace id copied");
    }

    private void CopyDeviceIdButton_Click(object sender, RoutedEventArgs e)
    {
        CopyText(DeviceIdText.Text);
        AppendHistory("[event] device id copied");
    }

    private void CreateInviteButton_Click(object sender, RoutedEventArgs e)
    {
        InviteCodeText.Text = _session.CreateInviteCode(InviteNameText.Text);
        CopyText(InviteCodeText.Text);
        SyncStatusText.Text = "状态：邀请码已生成并复制";
        AppendHistory("[pairing] invite generated");
    }

    private void CopyInviteButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(InviteCodeText.Text))
        {
            CopyText(InviteCodeText.Text);
            AppendHistory("[pairing] invite copied");
        }
    }

    private void JoinInviteButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_session.TryJoinInvite(JoinInviteText.Text, out var message, out var trustedDevice))
        {
            SyncStatusText.Text = "状态：" + message;
            AppendHistory("[pairing] failed: " + message);
            return;
        }

        if (trustedDevice is not null)
        {
            if (!_trustedDevices.Any(x => string.Equals(x.DeviceId, trustedDevice.DeviceId, StringComparison.Ordinal)))
            {
                _trustedDevices.Add(trustedDevice);
                _session.SaveTrustedDevices(_trustedDevices);
            }

            AppendHistory("[pairing] trusted: " + trustedDevice.Name + " (" + trustedDevice.DeviceId + ")");
        }

        WorkspaceKeyText.Text = _session.WorkspaceKey;
        SyncStatusText.Text = "状态：" + message;
    }

    private async void ManualSyncButton_Click(object sender, RoutedEventArgs e)
    {
        await RunSyncAsync("manual");
    }

    private void AutoSyncSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        var mode = AutoSyncSwitch.IsOn ? "auto" : "manual";
        _session.Store.Set("sync_mode", mode);
        if (AutoSyncSwitch.IsOn)
        {
            _autoSyncTimer.Start();
            AppendHistory("[event] auto shared clipboard enabled");
        }
        else
        {
            _autoSyncTimer.Stop();
            AppendHistory("[event] auto shared clipboard disabled");
        }
    }

    private async void AutoSyncTimer_Tick(object? sender, object e)
    {
        await RunSyncAsync("auto");
    }

    private async Task RunSyncAsync(string trigger)
    {
        if (_syncBusy)
        {
            return;
        }

        _syncBusy = true;
        try
        {
            var result = await _session.SyncOnceAsync();
            if (result.Ok)
            {
                SyncStatusText.Text = "状态：同步成功";
                AppendHistory("[" + trigger + "] " + result.Message);
            }
            else
            {
                SyncStatusText.Text = "状态：" + result.Message;
                AppendHistory("[" + trigger + "] failed: " + result.Message);
            }
        }
        catch (Exception ex)
        {
            SyncStatusText.Text = "状态：同步异常";
            AppendHistory("[" + trigger + "] error: " + ex.GetType().Name);
        }
        finally
        {
            _syncBusy = false;
        }
    }

    private void AppendHistory(string message)
    {
        HistoryList.Items.Insert(0, DateTime.Now.ToString("HH:mm:ss") + " " + message);
    }

    private static void CopyText(string text)
    {
        try
        {
            var package = new DataPackage();
            package.SetText(text ?? string.Empty);
            Clipboard.SetContent(package);
            Clipboard.Flush();
        }
        catch
        {
            // Clipboard can be unavailable briefly.
        }
    }
}
