import SwiftUI

private struct TrustedDevice: Identifiable {
    let id: String
    let name: String
    let lastSeen: String
}

private struct PairingRequest: Identifiable {
    let id: String
    let deviceName: String
    let platform: String
    let requestedAt: String
}

private struct HistoryItem: Identifiable {
    let id: UUID = UUID()
    let direction: String
    let contentType: String
    let preview: String
    let at: String
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

struct IOSDashboardView: View {
    @State private var status = StatusViewModel(
        connectionState: .connected,
        syncedOutCount: 2,
        syncedInCount: 2,
        rejectedEventCount: 0,
        trustedDeviceCount: 3,
        pendingPairingCount: 1,
        lastErrorMessage: nil
    )

    @State private var trustedDevices: [TrustedDevice] = [
        TrustedDevice(id: "ios-handset", name: "iPhone", lastSeen: "just now"),
        TrustedDevice(id: "win-local", name: "Windows Desktop", lastSeen: "3 min ago"),
        TrustedDevice(id: "android-main", name: "Android Phone", lastSeen: "10 min ago")
    ]

    @State private var history: [HistoryItem] = [
        HistoryItem(direction: "out", contentType: "text/plain", preview: "hello from iPhone", at: "10:05"),
        HistoryItem(direction: "in", contentType: "text/plain", preview: "copied on Windows", at: "09:58")
    ]

    @State private var pairingRequests: [PairingRequest] = [
        PairingRequest(id: "req-ios-001", deviceName: "Galaxy S24", platform: "android", requestedAt: "10:08")
    ]

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
        ZStack {
            LinearGradient(
                colors: [Color(red: 0.06, green: 0.16, blue: 0.35), Color(red: 0.07, green: 0.35, blue: 0.42)],
                startPoint: .topLeading,
                endPoint: .bottomTrailing
            )
            .ignoresSafeArea()

            TabView {
                statusView
                    .tabItem { Label("Status", systemImage: "chart.bar") }

                devicesView
                    .tabItem { Label("Devices", systemImage: "lock.shield") }

                pairingView
                    .tabItem { Label("Pairing", systemImage: "person.badge.plus") }

                historyView
                    .tabItem { Label("History", systemImage: "clock") }

                settingsView
                    .tabItem { Label("Settings", systemImage: "gearshape") }
            }
        }
        .preferredColorScheme(settings.darkMode ? .dark : .light)
    }

    private var statusView: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 14) {
                    statusCard

                    Button("Manual Sync") {
                        status.syncedOutCount += 1
                        status.syncedInCount += 1
                        status.lastErrorMessage = nil
                        history.insert(HistoryItem(direction: "out", contentType: "text/plain", preview: "manual sync", at: "now"), at: 0)
                    }
                    .buttonStyle(.borderedProminent)
                    .frame(maxWidth: .infinity, alignment: .leading)
                }
                .padding()
            }
            .navigationTitle("Clipboard Sync")
        }
    }

    private var statusCard: some View {
        VStack(alignment: .leading, spacing: 10) {
            row("Connection", status.connectionState.rawValue)
            row("Synced Out", "\(status.syncedOutCount)")
            row("Synced In", "\(status.syncedInCount)")
            row("Rejected", "\(status.rejectedEventCount)")
            row("Trusted Devices", "\(status.trustedDeviceCount)")
            row("Pending Pairing", "\(status.pendingPairingCount)")
            row("Last Error", status.lastErrorMessage ?? "None")
        }
        .padding(16)
        .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: 18, style: .continuous))
    }

    private var devicesView: some View {
        NavigationStack {
            List {
                ForEach(trustedDevices) { device in
                    VStack(alignment: .leading, spacing: 4) {
                        Text(device.name).font(.headline)
                        Text("ID: \(device.id)").font(.caption)
                        Text("Last seen: \(device.lastSeen)").font(.caption2)
                    }
                }
                .onDelete { indexSet in
                    let removed = indexSet.map { trustedDevices[$0] }
                    trustedDevices.remove(atOffsets: indexSet)
                    status.rejectedEventCount += removed.count
                    status.trustedDeviceCount = max(0, status.trustedDeviceCount - removed.count)
                    status.lastErrorMessage = removed.first.map { "Revoked device: \($0.id)" }
                    if let first = removed.first {
                        history.insert(HistoryItem(direction: "event", contentType: "device", preview: "revoked \(first.id)", at: "now"), at: 0)
                    }
                }
            }
            .navigationTitle("Trusted Devices")
            .toolbar { EditButton() }
        }
    }

    private var pairingView: some View {
        NavigationStack {
            List {
                if pairingRequests.isEmpty {
                    Text("No pending pairing request")
                        .foregroundStyle(.secondary)
                } else {
                    ForEach(pairingRequests) { req in
                        VStack(alignment: .leading, spacing: 8) {
                            Text(req.deviceName).font(.headline)
                            Text("Platform: \(req.platform)").font(.caption)
                            Text("Requested: \(req.requestedAt)").font(.caption2)
                            HStack {
                                Button("Approve") {
                                    pairingRequests.removeAll { $0.id == req.id }
                                    status.pendingPairingCount = max(0, status.pendingPairingCount - 1)
                                    status.trustedDeviceCount += 1
                                    history.insert(HistoryItem(direction: "event", contentType: "pairing", preview: "approved \(req.deviceName)", at: "now"), at: 0)
                                }
                                .buttonStyle(.borderedProminent)

                                Button("Reject", role: .destructive) {
                                    pairingRequests.removeAll { $0.id == req.id }
                                    status.pendingPairingCount = max(0, status.pendingPairingCount - 1)
                                    history.insert(HistoryItem(direction: "event", contentType: "pairing", preview: "rejected \(req.deviceName)", at: "now"), at: 0)
                                }
                                .buttonStyle(.bordered)
                            }
                        }
                    }
                }
            }
            .navigationTitle("Pairing Requests")
        }
    }

    private var historyView: some View {
        NavigationStack {
            List(history) { item in
                VStack(alignment: .leading, spacing: 4) {
                    Text("[\(item.direction)] \(item.contentType)").font(.headline)
                    Text(item.preview)
                    Text(item.at).font(.caption)
                }
            }
            .navigationTitle("History")
        }
    }

    private var settingsView: some View {
        NavigationStack {
            Form {
                row("Language", settings.language)
                row("Sync Mode", settings.syncMode)
                row("Space", settings.spaceId)
                row("Pairing Policy", settings.pairingPolicy)
                Toggle("Dark Mode", isOn: $settings.darkMode)
                Toggle("WebDev Sync", isOn: $settings.webDevEnabled)
                Toggle("Local Server Mode", isOn: $settings.localServerEnabled)
            }
            .navigationTitle("Settings")
        }
    }

    private func row(_ label: String, _ value: String) -> some View {
        HStack {
            Text(label)
            Spacer()
            Text(value)
        }
    }
}

#Preview {
    IOSDashboardView()
}
