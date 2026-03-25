import Foundation

enum SyncConnectionState: String {
    case connected = "CONNECTED"
    case degraded = "DEGRADED"
    case disconnected = "DISCONNECTED"
}

struct StatusViewModel {
    var connectionState: SyncConnectionState
    var syncedOutCount: Int
    var syncedInCount: Int
    var rejectedEventCount: Int
    var trustedDeviceCount: Int
    var pendingPairingCount: Int
    var lastErrorMessage: String?
}
