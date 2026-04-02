using System;
using ClipboardSync_Windows_WinUI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ClipboardSync_Windows_WinUI.Pages;

public sealed partial class SettingsPage : Page
{
    private readonly AppSession _session = AppSession.Instance;

    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        var relay = _session.LoadPublicRelaySettings();
        PublicRelaySwitch.IsOn = relay.Enabled;
        PublicRelayUrlText.Text = relay.BaseUrl;
        PublicRelayBucketText.Text = string.IsNullOrWhiteSpace(relay.Bucket) ? _session.WorkspaceKey : relay.Bucket;

        var webDav = _session.LoadWebDavSettings();
        WebDavSwitch.IsOn = webDav.Enabled;
        WebDavUrlText.Text = webDav.BaseUrl;
        WebDavUserText.Text = webDav.Username;
        WebDavPasswordText.Password = webDav.Password;
    }

    private void SaveSettings()
    {
        _session.SavePublicRelaySettings(PublicRelayUrlText.Text, PublicRelayBucketText.Text, PublicRelaySwitch.IsOn);
        _session.SaveWebDavSettings(WebDavUrlText.Text, WebDavUserText.Text, WebDavPasswordText.Password, WebDavSwitch.IsOn);
    }

    private void PublicRelaySwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (PublicRelaySwitch.IsOn)
        {
            WebDavSwitch.IsOn = false;
        }
    }

    private void WebDavSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        if (WebDavSwitch.IsOn)
        {
            PublicRelaySwitch.IsOn = false;
        }
    }

    private async void TestPublicRelayButton_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        var ok = await _session.TestPublicRelayAsync();
        SettingsStatusText.Text = ok ? "状态：公共服务器连接成功" : "状态：公共服务器连接失败";
    }

    private async void TestWebDavButton_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        var ok = await _session.TestWebDavAsync();
        SettingsStatusText.Text = ok ? "状态：WebDAV 连接成功" : "状态：WebDAV 连接失败";
    }

    private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();
        SettingsStatusText.Text = "状态：设置已保存";
    }
}
