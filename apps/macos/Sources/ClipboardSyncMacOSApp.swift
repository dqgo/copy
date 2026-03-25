import SwiftUI

@main
struct ClipboardSyncMacOSApp: App {
    var body: some Scene {
        MenuBarExtra("Clipboard Sync", systemImage: "doc.on.clipboard") {
            MacStatusMenuView()
        }

        Window("Trusted Devices", id: "trusted-devices") {
            MacTrustedDevicesWindowView()
        }

        Window("Pairing Requests", id: "pairing-requests") {
            MacPairingRequestsWindowView()
        }

        Window("Sync History", id: "sync-history") {
            MacHistoryWindowView()
        }
    }
}
