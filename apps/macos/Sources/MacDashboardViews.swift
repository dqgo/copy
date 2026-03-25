import SwiftUI

struct MacL10n {
    static let translations: [String: [String: String]] = [
        "zh-CN": [
            "status": "状态",
            "devices": "可信设备",
            "pairing": "配对请求",
            "history": "历史记录",
            "settings": "设置",
            "connection": "连接状态",
            "sent": "发出",
            "received": "接收",
            "rejected": "拒绝",
            "trustedCount": "可信设备数",
            "pendingPairing": "待批准",
            "approve": "批准",
            "reject": "拒绝",
            "revoke": "撤销",
            "darkMode": "深色模式",
            "webdev": "WebDev 同步",
            "server": "本地服务模式",
            "manualSync": "手动同步",
            "lastError": "最近错误",
            "noDevices": "暂无设备",
            "noPairingRequests": "暂无配对请求",
            "searchDevices": "搜索设备",
            "searchPairing": "搜索配对请求",
            "clearFilter": "清空筛选",
            "emptyDevicesHint": "暂无可信设备，可前往配对窗口添加",
            "emptyPairingHint": "暂无配对请求，等待新设备发起即可"
        ],
        "en-US": [
            "status": "Status",
            "devices": "Trusted Devices",
            "pairing": "Pairing Requests",
            "history": "History",
            "settings": "Settings",
            "connection": "Connection",
            "sent": "Sent",
            "received": "Received",
            "rejected": "Rejected",
            "trustedCount": "Trusted Devices",
            "pendingPairing": "Pending",
            "approve": "Approve",
            "reject": "Reject",
            "revoke": "Revoke",
            "darkMode": "Dark Mode",
            "webdev": "WebDev Sync",
            "server": "Local Server Mode",
            "manualSync": "Manual Sync",
            "lastError": "Last Error",
            "noDevices": "No devices",
            "noPairingRequests": "No pairing requests",
            "searchDevices": "Search devices",
            "searchPairing": "Search pairing requests",
            "clearFilter": "Clear filter",
            "emptyDevicesHint": "No trusted devices yet. Add one from pairing window.",
            "emptyPairingHint": "No pairing requests for now."
        ]
    ]

    static func get(_ language: String, _ key: String) -> String {
        translations[language]?[key] ?? translations["en-US"]?[key] ?? key
    }
}

private struct MacTrustedDevice: Identifiable {
    let id: String
    let name: String
    let lastSeen: String
}

private struct PairingRequestItem: Identifiable {
    let id: String
    let deviceName: String
    let platform: String
    let at: String
}

private struct SyncHistoryItem: Identifiable {
    let id = UUID()
    let direction: String
    let contentType: String
    let summary: String
    let time: String
}

private struct MacSettingsModel {
    var language: String
    var darkMode: Bool
    var webDevEnabled: Bool
    var localServerEnabled: Bool
}

struct MacStatusMenuView: View {
    @Environment(\.colorScheme) private var systemColorScheme

    @State private var settings = MacSettingsModel(language: "zh-CN", darkMode: false, webDevEnabled: false, localServerEnabled: false)
    @State private var status = StatusViewModel(
        connectionState: .connected,
        syncedOutCount: 12,
        syncedInCount: 11,
        rejectedEventCount: 1,
        trustedDeviceCount: 3,
        pendingPairingCount: 1,
        lastErrorMessage: "Revoked device: old-ipad"
    )

    var body: some View {
        VStack(alignment: .leading, spacing: 10) {
            Text("Clipboard Sync Status")
                .font(.headline)
            Label(status.connectionState.rawValue, systemImage: status.connectionState == .connected ? "checkmark.circle" : "xmark.circle")
                .accessibilityLabel("Connection status \(status.connectionState.rawValue)")
            Text("\(MacL10n.get(settings.language, "sent")): \(status.syncedOutCount)")
            Text("\(MacL10n.get(settings.language, "received")): \(status.syncedInCount)")
            Text("\(MacL10n.get(settings.language, "rejected")): \(status.rejectedEventCount)")
            Text("\(MacL10n.get(settings.language, "trustedCount")): \(status.trustedDeviceCount)")
            Text("\(MacL10n.get(settings.language, "pendingPairing")): \(status.pendingPairingCount)")
            Text("\(MacL10n.get(settings.language, "lastError")): \(status.lastErrorMessage ?? "None")")
                .font(.caption)
                .foregroundStyle(.secondary)

            Divider()

            Button(MacL10n.get(settings.language, "manualSync")) {
                status.syncedOutCount += 1
                status.syncedInCount += 1
                status.lastErrorMessage = nil
            }
            .accessibilityLabel("Manual sync")

            Toggle(MacL10n.get(settings.language, "darkMode"), isOn: $settings.darkMode)
            Toggle(MacL10n.get(settings.language, "webdev"), isOn: $settings.webDevEnabled)
            Toggle(MacL10n.get(settings.language, "server"), isOn: $settings.localServerEnabled)
        }
        .padding(12)
        .frame(width: 300)
        .background(LinearGradient(colors: [Color.blue.opacity(0.25), Color.teal.opacity(0.18)], startPoint: .topLeading, endPoint: .bottomTrailing))
        .preferredColorScheme(settings.darkMode ? .dark : (systemColorScheme == .dark ? .dark : .light))
    }
}

struct MacTrustedDevicesWindowView: View {
    @State private var devices: [MacTrustedDevice] = [
        MacTrustedDevice(id: "mac-office", name: "macOS Laptop", lastSeen: "just now"),
        MacTrustedDevice(id: "win-local", name: "Windows Desktop", lastSeen: "6 min ago"),
        MacTrustedDevice(id: "android-main", name: "Android Phone", lastSeen: "14 min ago")
    ]
    @State private var query = ""
    @State private var showConfirmDialog = false
    @State private var pendingDevice: MacTrustedDevice?

    private var filteredDevices: [MacTrustedDevice] {
        let q = query.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()
        guard !q.isEmpty else { return devices }
        return devices.filter { $0.name.lowercased().contains(q) || $0.id.lowercased().contains(q) }
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Trusted Devices").font(.title2)
            TextField(MacL10n.get("en-US", "searchDevices"), text: $query)
                .textFieldStyle(.roundedBorder)
                .accessibilityLabel("Search devices")

            List {
                if filteredDevices.isEmpty {
                    VStack(alignment: .leading, spacing: 8) {
                        Text(MacL10n.get("en-US", "noDevices")).font(.headline)
                        Text(MacL10n.get("en-US", "emptyDevicesHint")).foregroundStyle(.secondary)
                        if !query.isEmpty {
                            Button(MacL10n.get("en-US", "clearFilter")) { query = "" }
                                .buttonStyle(.bordered)
                        }
                    }
                } else {
                    ForEach(filteredDevices) { device in
                        VStack(alignment: .leading) {
                            Text(device.name).font(.headline)
                            Text("ID: \(device.id)").font(.caption)
                            Text("Last seen: \(device.lastSeen)").font(.caption2)
                        }
                        .swipeActions(edge: .trailing, allowsFullSwipe: false) {
                            Button(role: .destructive) {
                                pendingDevice = device
                                showConfirmDialog = true
                            } label: {
                                Text(MacL10n.get("en-US", "revoke"))
                            }
                        }
                        .accessibilityLabel("Device \(device.name)")
                    }
                }
            }
        }
        .padding(16)
        .frame(minWidth: 520, minHeight: 360)
        .alert("Revoke device", isPresented: $showConfirmDialog) {
            Button("Cancel", role: .cancel) { }
            Button("Revoke", role: .destructive) {
                if let pendingDevice {
                    devices.removeAll { $0.id == pendingDevice.id }
                }
            }
        } message: {
            Text("Do you want to revoke this device?")
        }
    }
}

struct MacPairingRequestsWindowView: View {
    @State private var requests: [PairingRequestItem] = [
        PairingRequestItem(id: "req-mac-001", deviceName: "Pixel 9", platform: "android", at: "10:12"),
        PairingRequestItem(id: "req-mac-002", deviceName: "iPad Air", platform: "ios", at: "10:14")
    ]
    @State private var query = ""
    @State private var showConfirmDialog = false
    @State private var pendingReject: PairingRequestItem?

    private var filteredRequests: [PairingRequestItem] {
        let q = query.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()
        guard !q.isEmpty else { return requests }
        return requests.filter { $0.deviceName.lowercased().contains(q) || $0.platform.lowercased().contains(q) }
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Pairing Requests").font(.title2)
            TextField(MacL10n.get("en-US", "searchPairing"), text: $query)
                .textFieldStyle(.roundedBorder)
                .accessibilityLabel("Search pairing requests")

            List {
                if filteredRequests.isEmpty {
                    VStack(alignment: .leading, spacing: 8) {
                        Text(MacL10n.get("en-US", "noPairingRequests")).font(.headline)
                        Text(MacL10n.get("en-US", "emptyPairingHint")).foregroundStyle(.secondary)
                        if !query.isEmpty {
                            Button(MacL10n.get("en-US", "clearFilter")) { query = "" }
                                .buttonStyle(.bordered)
                        }
                    }
                } else {
                    ForEach(filteredRequests) { req in
                        VStack(alignment: .leading, spacing: 6) {
                            Text(req.deviceName).font(.headline)
                            Text("Platform: \(req.platform)").font(.caption)
                            Text("Requested: \(req.at)").font(.caption2)
                            HStack {
                                Button(MacL10n.get("en-US", "approve")) {
                                    requests.removeAll { $0.id == req.id }
                                }
                                .buttonStyle(.borderedProminent)
                                .accessibilityLabel("Approve \(req.deviceName)")

                                Button(MacL10n.get("en-US", "reject"), role: .destructive) {
                                    pendingReject = req
                                    showConfirmDialog = true
                                }
                                .buttonStyle(.bordered)
                                .accessibilityLabel("Reject \(req.deviceName)")
                            }
                        }
                    }
                }
            }
        }
        .padding(16)
        .frame(minWidth: 520, minHeight: 360)
        .alert("Reject request", isPresented: $showConfirmDialog) {
            Button("Cancel", role: .cancel) { }
            Button("Reject", role: .destructive) {
                if let pendingReject {
                    requests.removeAll { $0.id == pendingReject.id }
                }
            }
        } message: {
            Text("Do you want to reject this pairing request?")
        }
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
            Text("Sync History").font(.title2)
            List(history) { item in
                HStack {
                    Text("[\(item.direction)] \(item.contentType) · \(item.summary)")
                    Spacer()
                    Text(item.time).foregroundStyle(.secondary)
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
