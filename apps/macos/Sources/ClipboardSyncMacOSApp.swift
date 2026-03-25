import SwiftUI
import UserNotifications

@main
struct ClipboardSyncMacOSApp: App {
    @Environment(\.scenePhase) private var scenePhase

    init() {
        UNUserNotificationCenter.current().requestAuthorization(options: [.alert, .sound]) { _, _ in }
    }

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
                .onChange(of: scenePhase) { _, newPhase in
                    if newPhase == .background {
                        let content = UNMutableNotificationContent()
                        content.title = "Clipboard Sync"
                        content.body = "App is in menu bar. Open app windows to run full sync operations."
                        let request = UNNotificationRequest(
                            identifier: "clipboardsync.macos.background",
                            content: content,
                            trigger: UNTimeIntervalNotificationTrigger(timeInterval: 1, repeats: false)
                        )
                        UNUserNotificationCenter.current().add(request)
                    }
                }
        }
    }
}
