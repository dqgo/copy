import SwiftUI

private struct MacTrustedDevice: Identifiable {
    let id: String
    let name: String
    let lastSeen: String
}

private struct SyncHistoryItem: Identifiable {
    let id: UUID = UUID()
    let direction: String
    let contentType: String
    let summary: String
    let time: String
}

private struct SettingsModel {
    var language: String
    var darkMode: Bool
    var syncMode: String
    var spaceId: String
    var webDevEnabled: Bool
    var localServerEnabled: Bool
    var pairingPolicy: String
}

private struct PairingRequestItem: Identifiable {
    let id: String
    let deviceName: String
    let platform: String
    let at: String
}

struct MacStatusMenuView: View {
    @State private var status = StatusViewModel(
        connectionState: .connected,
        syncedOutCount: 12,
        syncedInCount: 11,
        rejectedEventCount: 1,
        trustedDeviceCount: 3,
        pendingPairingCount: 1,
        lastErrorMessage: "Revoked device: old-ipad"
    )

    @State private var settings = SettingsModel(
        language: "zh-CN",
        darkMode: false,
        syncMode: "manual",
        spaceId: "default",
        webDevEnabled: false,
        localServerEnabled: false,
        pairingPolicy: "manual-approve"
    )

    var body: some View {
        VStack(alignment: .leading, spacing: 10) {
            Text("Clipboard Sync Status")
                .font(.headline)
            Label(status.connectionState.rawValue, systemImage: status.connectionState == .connected ? "checkmark.circle" : "xmark.circle")
            Text("Sent: \(status.syncedOutCount)")
            Text("Received: \(status.syncedInCount)")
            Text("Rejected: \(status.rejectedEventCount)")
            Text("Trusted: \(status.trustedDeviceCount)")
            Text("Pending Pairing: \(status.pendingPairingCount)")
            Text("Last error: \(status.lastErrorMessage ?? "None")")
                .font(.caption)
                .foregroundStyle(.secondary)

            Divider()

            Button("Manual Sync") {
                status.syncedOutCount += 1
                status.syncedInCount += 1
                status.lastErrorMessage = nil
            }

            Toggle("Dark Mode", isOn: $settings.darkMode)
            Toggle("WebDev Sync", isOn: $settings.webDevEnabled)
            Toggle("Local Server", isOn: $settings.localServerEnabled)
            Text("Pairing Policy: \(settings.pairingPolicy)")
                .font(.caption)
                .foregroundStyle(.secondary)

            Text("Use menu bar for background listener visibility.")
                .font(.caption2)
                .foregroundStyle(.secondary)
        }
        .padding(12)
        .frame(width: 280)
        .background(
            LinearGradient(colors: [Color.blue.opacity(0.25), Color.teal.opacity(0.18)], startPoint: .topLeading, endPoint: .bottomTrailing)
        )
        .preferredColorScheme(settings.darkMode ? .dark : .light)
    }
}

struct MacTrustedDevicesWindowView: View {
    @State private var devices: [MacTrustedDevice] = [
        MacTrustedDevice(id: "mac-office", name: "macOS Laptop", lastSeen: "just now"),
        MacTrustedDevice(id: "win-local", name: "Windows Desktop", lastSeen: "6 min ago"),
        MacTrustedDevice(id: "android-main", name: "Android Phone", lastSeen: "14 min ago")
    ]

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Trusted Devices")
                .font(.title2)
            List {
                ForEach(devices) { device in
                    VStack(alignment: .leading) {
                        Text(device.name).font(.headline)
                        Text("ID: \(device.id)").font(.caption)
                        Text("Last seen: \(device.lastSeen)").font(.caption2)
                    }
                }
                .onDelete { indexSet in
                    devices.remove(atOffsets: indexSet)
                }
            }
        }
        .padding(16)
        .frame(minWidth: 500, minHeight: 360)
    }
}

struct MacPairingRequestsWindowView: View {
    @State private var requests: [PairingRequestItem] = [
        PairingRequestItem(id: "req-mac-001", deviceName: "Pixel 9", platform: "android", at: "10:12"),
        PairingRequestItem(id: "req-mac-002", deviceName: "iPad Air", platform: "ios", at: "10:14")
    ]

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Pairing Requests")
                .font(.title2)
            List {
                ForEach(requests) { req in
                    VStack(alignment: .leading, spacing: 6) {
                        Text(req.deviceName).font(.headline)
                        Text("Platform: \(req.platform)").font(.caption)
                        Text("Requested: \(req.at)").font(.caption2)
                        HStack {
                            Button("Approve") {
                                requests.removeAll { $0.id == req.id }
                            }
                            .buttonStyle(.borderedProminent)

                            Button("Reject", role: .destructive) {
                                requests.removeAll { $0.id == req.id }
                            }
                            .buttonStyle(.bordered)
                        }
                    }
                }
            }
        }
        .padding(16)
        .frame(minWidth: 520, minHeight: 360)
    }
}

struct MacHistoryWindowView: View {
    private let history: [SyncHistoryItem] = [
        SyncHistoryItem(direction: "in", contentType: "text/plain", summary: "Received clipboard from iPhone", time: "09:42"),
        SyncHistoryItem(direction: "out", contentType: "text/plain", summary: "Manual sync to Windows", time: "09:30"),
        SyncHistoryItem(direction: "event", contentType: "device", summary: "Rejected replay packet", time: "09:11")
    ]

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Sync History")
                .font(.title2)
            List(history) { item in
                HStack {
                    Text("[\(item.direction)] \(item.contentType) · \(item.summary)")
                    Spacer()
                    Text(item.time)
                        .foregroundStyle(.secondary)
                }
            }
        }
        .padding(16)
        .frame(minWidth: 520, minHeight: 360)
    }
}

#Preview {
    MacStatusMenuView()
}
