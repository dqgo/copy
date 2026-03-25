import SwiftUI
import UserNotifications

@main
struct ClipboardSyncIOSApp: App {
    @Environment(\.scenePhase) private var scenePhase

    init() {
        UNUserNotificationCenter.current().requestAuthorization(options: [.alert, .badge, .sound]) { _, _ in }
    }

    var body: some Scene {
        WindowGroup {
            IOSDashboardView()
                .onChange(of: scenePhase) { _, newPhase in
                    if newPhase == .background {
                        let content = UNMutableNotificationContent()
                        content.title = "Clipboard Sync"
                        content.body = "Background sync monitoring is active."
                        content.sound = .default

                        let request = UNNotificationRequest(
                            identifier: "clipboardsync.background",
                            content: content,
                            trigger: UNTimeIntervalNotificationTrigger(timeInterval: 1, repeats: false)
                        )
                        UNUserNotificationCenter.current().add(request)
                    }
                }
        }
    }
}
