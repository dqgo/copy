package com.clipboardsync

enum class SyncConnectionState {
    CONNECTED,
    DEGRADED,
    DISCONNECTED
}

data class StatusViewModel(
    val connectionState: SyncConnectionState,
    val syncedOutCount: Int,
    val syncedInCount: Int,
    val rejectedEventCount: Int,
    val trustedDeviceCount: Int,
    val pendingPairingCount: Int,
    val lastErrorMessage: String?
)
