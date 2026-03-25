namespace ClipboardSync.Windows;

public enum SyncConnectionState
{
    Connected,
    Degraded,
    Disconnected
}

public sealed class StatusViewModel
{
    public SyncConnectionState ConnectionState { get; set; } = SyncConnectionState.Disconnected;
    public int SyncedOutCount { get; set; }
    public int SyncedInCount { get; set; }
    public int RejectedEventCount { get; set; }
    public int TrustedDeviceCount { get; set; }
    public int PendingPairingCount { get; set; }
    public string LastErrorMessage { get; set; } = string.Empty;
}
