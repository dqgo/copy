import SwiftUI

// i18n translations
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
            "language": "语言",
            "darkMode": "深色模式",
            "syncMode": "同步模式",
            "space": "工作空间",
            "pairingPolicy": "配对策略",
            "webdev": "WebDev 同步",
            "server": "本地服务模式",
            "manualSync": "手动同步",
            "noDevices": "暂无设备",
            "noPairingRequests": "暂无配对请求",
            "noHistory": "暂无历史记录",
            "lastError": "最近错误",
            "confirrm": "确认",
            "confirm": "确认",
            "cancel": "取消"
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
            "language": "Language",
            "darkMode": "Dark Mode",
            "syncMode": "Sync Mode",
            "space": "Space",
            "pairingPolicy": "Pairing Policy",
            "webdev": "WebDev Sync",
            "server": "Local Server Mode",
            "manualSync": "Manual Sync",
            "noDevices": "No devices",
            "noPairingRequests": "No pairing requests",
            "noHistory": "No history",
            "lastError": "Last Error",
            "confirm": "Confirm",
            "cancel": "Cancel"
        ]
    ]
    
    static func get(_ language: String, _ key: String) -> String {
        return translations[language]?[key] ?? translations["en-US"]?[key] ?? key
    }
}

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
    @Environment(\.colorScheme) var systemColorScheme
    
    @State private var status = StatusViewModel(
        connectionState: .connected,
        syncedOutCount: 12,
        syncedInCount: 11,
        rejectedEventCount: 1,
        trustedDeviceCount: 3,
        pendingPairingCount: 1,
        lastErrorMessage: "Revoked device: old-ipad"
    )
    
    @State private var errorMessage: String? = nil
    @State private var isLoading: Bool = false

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
            // Error message display
            if let error = errorMessage {
                HStack {
                    Text(error)
                        .font(.caption)
                        .foregroundStyle(.red)
                    Spacer()
                    Button("✕") {
                        errorMessage = nil
                    }
                    .buttonStyle(.plain)
                }
                .padding(8)
                .background(Color.red.opacity(0.15))
                .cornerRadius(6)
            }
            
            Text("Clipboard Sync Status")
                .font(.headline)
                Label(status.connectionState.rawValue, systemImage: status.connectionState == .connected ? "checkmark.circle" : "xmark.circle")
                    .animation(.easeInOut(duration: 0.2), value: errorMessage)
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

            Toggle(MacL10n.get(settings.language, "darkMode"), isOn: $settings.darkMode)
            Toggle(MacL10n.get(settings.language, "webdev"), isOn: $settings.webDevEnabled)
            Toggle(MacL10n.get(settings.language, "server"), isOn: $settings.localServerEnabled)
            Text("\(MacL10n.get(settings.language, "pairingPolicy")): \(settings.pairingPolicy)")
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
        .preferredColorScheme(getEffectiveColorScheme())
        .onAppear {
            // Auto detect system dark mode on first launch
            if systemColorScheme == .dark {
                settings.darkMode = true
            }
        }
    }
    
    private func getEffectiveColorScheme() -> ColorScheme? {
        if systemColorScheme == .dark && !settings.darkMode {
            return .dark
        } else if settings.darkMode {
            return .dark
        } else {
            return .light
        }
    }
}

struct MacTrustedDevicesWindowView: View {
    @State private var devices: [MacTrustedDevice] = [
        MacTrustedDevice(id: "mac-office", name: "macOS Laptop", lastSeen: "just now"),
        MacTrustedDevice(id: "win-local", name: "Windows Desktop", lastSeen: "6 min ago"),
        MacTrustedDevice(id: "android-main", name: "Android Phone", lastSeen: "14 min ago")
    ]
    @State private var showConfirmDialog: Bool = false
    @State private var confirmDialogTitle: String = ""
    @State private var confirmDialogMessage: String = ""
    @State private var pendingDeleteIndices: IndexSet? = nil
    @State private var confirmDialogOnConfirm: (() -> Void)? = nil

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
                    let removed = indexSet.map { devices[$0] }
                    let deviceNames = removed.map { $0.name }.joined(separator: ", ")
                    pendingDeleteIndices = indexSet
                    showConfirmDialog = true
                    confirmDialogTitle = "Revoke Device"
                    confirmDialogMessage = "Are you sure you want to revoke \(deviceNames)?"
                    confirmDialogOnConfirm = {
                        devices.remove(atOffsets: indexSet)
                    }
                }
            }
        }
        .padding(16)
        .frame(minWidth: 500, minHeight: 360)
        .alert(confirmDialogTitle, isPresented: $showConfirmDialog) {
            Button("Cancel", role: .cancel) { }
        }
        .padding(16)
        .frame(minWidth: 520, minHeight: 360)
        .alert(confirmDialogTitle, isPresented: $showConfirmDialog) {
            Button("Cancel", role: .cancel) { }
            Button("Reject", role: .destructive) {
                confirmDialogOnConfirm?()
            }
        } message: {
            Text(confirmDialogMessage)
        }
    }
}
            Button("Revoke", role: .destructive) {
                confirmDialogOnConfirm?()
            }
        } message: {
            Text(confirmDialogMessage)
        }
    }
}

struct MacPairingRequestsWindowView: View {
    @State private var requests: [PairingRequestItem] = [
        PairingRequestItem(id: "req-mac-001", deviceName: "Pixel 9", platform: "android", at: "10:12"),
        PairingRequestItem(id: "req-mac-002", deviceName: "iPad Air", platform: "ios", at: "10:14")
    ]
    @State private var showConfirmDialog: Bool = false
    @State private var confirmDialogTitle: String = ""
    @State private var confirmDialogMessage: String = ""
    @State private var pendingRejectRequestId: String? = nil
    @State private var confirmDialogOnConfirm: (() -> Void)? = nil

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
                                pendingRejectRequestId = req.id
                                showConfirmDialog = true
                                confirmDialogTitle = "Reject Request"
                                confirmDialogMessage = "Are you sure you want to reject \(req.deviceName)?"
                                confirmDialogOnConfirm = {
                                    requests.removeAll { $0.id == req.id }
                                }
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
