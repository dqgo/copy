import SwiftUI

// i18n translations
struct L10n {
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
            "confirmRevoke": "确认撤销此设备吗？",
            "confirmReject": "确认拒绝此请求吗?"
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
            "confirmRevoke": "Confirm revoking this device?",
            "confirmReject": "Confirm rejecting this request?"
        ]
    ]
    
    static func get(_ language: String, _ key: String) -> String {
        return translations[language]?[key] ?? translations["en-US"]?[key] ?? key
    }
}

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
            .animation(.easeInOut(duration: 0.3), value: selectedTab)
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
    @Environment(\.colorScheme) var systemColorScheme
    
    @State private var status = StatusViewModel(
        connectionState: .connected,
        syncedOutCount: 2,
        syncedInCount: 2,
        rejectedEventCount: 0,
        trustedDeviceCount: 3,
        pendingPairingCount: 1,
        lastErrorMessage: nil
    )
    
    @State private var errorMessage: String? = nil
    @State private var isLoading: Bool = false
    @State private var showConfirmDialog: Bool = false
    @State private var confirmDialogTitle: String = ""
    @State private var confirmDialogMessage: String = ""
    @State private var confirmDialogOnConfirm: (() -> Void)? = nil
    @State private var pendingDeleteIndices: IndexSet? = nil
    @State private var pendingRejectRequestId: String? = nil

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

            VStack {
                // Error message display
                if let error = errorMessage {
                    HStack {
                        Text(error)
                            .foregroundStyle(.red)
                        Spacer()
                        Button("Dismiss") {
                            errorMessage = nil
                        }
                    }
                    .padding(12)
                    .background(Color.red.opacity(0.15))
                    .cornerRadius(8)
                    .padding()
                }
                
                if isLoading {
                    VStack {
                        ProgressView()
                        Text("Loading...")
                            .foregroundStyle(.secondary)
                    }
                    .frame(maxWidth: .infinity, alignment: .center)
                    .padding()
                }
                
                    .transition(.opacity)@@
                TabView {
                statusView
                    .tabItem { Label(L10n.get(settings.language, "status"), systemImage: "chart.bar") }

                devicesView
                    .tabItem { Label(L10n.get(settings.language, "devices"), systemImage: "lock.shield") }

                pairingView
                    .tabItem { Label(L10n.get(settings.language, "pairing"), systemImage: "person.badge.plus") }

                historyView
                    .tabItem { Label(L10n.get(settings.language, "history"), systemImage: "clock") }

                settingsView
                    .tabItem { Label(L10n.get(settings.language, "settings"), systemImage: "gearshape") }
                }
            }
        }
        .preferredColorScheme(getEffectiveColorScheme())
        .onAppear {
            // Auto detect system dark mode on first launch
            if systemColorScheme == .dark {
                settings.darkMode = true
            }
        }
        .alert(confirmDialogTitle, isPresented: $showConfirmDialog) {
            Button("Cancel", role: .cancel) { }
            Button("Confirm", role: .destructive) {
                confirmDialogOnConfirm?()
            }
        } message: {
            Text(confirmDialogMessage)
        }
    }
    
    private func getEffectiveColorScheme() -> ColorScheme? {
        // If user hasn't explicitly set dark mode, use system setting
        // If dark mode explicit, force dark; if light explicit, force light
        if systemColorScheme == .dark && !settings.darkMode {
            return .dark
        } else if settings.darkMode {
            return .dark
        } else {
            return .light
        }
    }
    }

    private var statusView: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 14) {
                    statusCard

                    Button(L10n.get(settings.language, "manualSync")) {
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
            row(L10n.get(settings.language, "connection"), status.connectionState.rawValue)
            row(L10n.get(settings.language, "sent"), "\(status.syncedOutCount)")
            row(L10n.get(settings.language, "received"), "\(status.syncedInCount)")
            row(L10n.get(settings.language, "rejected"), "\(status.rejectedEventCount)")
            row(L10n.get(settings.language, "trustedCount"), "\(status.trustedDeviceCount)")
            row(L10n.get(settings.language, "pendingPairing"), "\(status.pendingPairingCount)")
            row(L10n.get(settings.language, "lastError"), status.lastErrorMessage ?? "None")
        }
        .padding(16)
        .background(.ultraThinMaterial, in: RoundedRectangle(cornerRadius: 18, style: .continuous))
    }

    private var devicesView: some View {
        NavigationStack {
            List {
                if trustedDevices.isEmpty {
                    Text(L10n.get(settings.language, "noDevices"))
                        .foregroundStyle(.secondary)
                } else {
                    ForEach(trustedDevices) { device in
                        VStack(alignment: .leading, spacing: 4) {
                            Text(device.name).font(.headline)
                            Text("ID: \(device.id)").font(.caption)
                            Text("Last seen: \(device.lastSeen)").font(.caption2)
                        }
                    }
                    .onDelete { indexSet in
                        let removed = indexSet.map { trustedDevices[$0] }
                        let deviceNames = removed.map { $0.name }.joined(separator: ", ")
                        pendingDeleteIndices = indexSet
                        showConfirmDialog = true
                        confirmDialogTitle = "Revoke Device"
                        confirmDialogMessage = "Are you sure you want to revoke \(deviceNames)?"
                        confirmDialogOnConfirm = {
                            trustedDevices.remove(atOffsets: indexSet)
                            status.rejectedEventCount += removed.count
                            status.trustedDeviceCount = max(0, status.trustedDeviceCount - removed.count)
                            status.lastErrorMessage = removed.first.map { "Revoked device: \($0.id)" }
                            if let first = removed.first {
                                history.insert(HistoryItem(direction: "event", contentType: "device", preview: "revoked \(first.id)", at: "now"), at: 0)
                            }
                        }
                    }
                }
            }
            .navigationTitle(L10n.get(settings.language, "devices"))
            .toolbar { EditButton() }
        }
    }

    private var pairingView: some View {
        NavigationStack {
            List {
                if pairingRequests.isEmpty {
                    Text(L10n.get(settings.language, "noPairingRequests"))
                        .foregroundStyle(.secondary)
                } else {
                    ForEach(pairingRequests) { req in
                        VStack(alignment: .leading, spacing: 8) {
                            Text(req.deviceName).font(.headline)
                            Text("\(L10n.get(settings.language, "status")): \(req.platform)").font(.caption)
                            Text("\(L10n.get(settings.language, "lastError")): \(req.requestedAt)").font(.caption2)
                            HStack {
                                Button(L10n.get(settings.language, "approve")) {
                                    pairingRequests.removeAll { $0.id == req.id }
                                    status.pendingPairingCount = max(0, status.pendingPairingCount - 1)
                                    status.trustedDeviceCount += 1
                                    history.insert(HistoryItem(direction: "event", contentType: "pairing", preview: "approved \(req.deviceName)", at: "now"), at: 0)
                                }
                                .buttonStyle(.borderedProminent)

                                Button(L10n.get(settings.language, "reject"), role: .destructive) {
                                    pendingRejectRequestId = req.id
                                    showConfirmDialog = true
                                    confirmDialogTitle = L10n.get(settings.language, "reject")
                                    confirmDialogMessage = "Are you sure you want to reject \(req.deviceName)?"
                                    confirmDialogOnConfirm = {
                                        pairingRequests.removeAll { $0.id == req.id }
                                        status.pendingPairingCount = max(0, status.pendingPairingCount - 1)
                                        history.insert(HistoryItem(direction: "event", contentType: "pairing", preview: "rejected \(req.deviceName)", at: "now"), at: 0)
                                    }
                                }
                                .buttonStyle(.bordered)
                            }
                        }
                    }
                }
            }
            .navigationTitle(L10n.get(settings.language, "pairing"))
        }
    }

    private var historyView: some View {
        NavigationStack {
            List {
                if history.isEmpty {
                    Text(L10n.get(settings.language, "noHistory"))
                        .foregroundStyle(.secondary)
                } else {
                    ForEach(history) { item in
                        VStack(alignment: .leading, spacing: 4) {
                            Text("[\(item.direction)] \(item.contentType)").font(.headline)
                            Text(item.preview)
                            Text(item.at).font(.caption)
                        }
                    }
                }
            }
            .navigationTitle(L10n.get(settings.language, "history"))
        }
    }

    private var settingsView: some View {
        NavigationStack {
            Form {
                row(L10n.get(settings.language, "language"), settings.language)
                row(L10n.get(settings.language, "syncMode"), settings.syncMode)
                row(L10n.get(settings.language, "space"), settings.spaceId)
                row(L10n.get(settings.language, "pairingPolicy"), settings.pairingPolicy)
                Toggle(L10n.get(settings.language, "darkMode"), isOn: $settings.darkMode)
                Toggle(L10n.get(settings.language, "webdev"), isOn: $settings.webDevEnabled)
                Toggle(L10n.get(settings.language, "server"), isOn: $settings.localServerEnabled)
            }
            .navigationTitle(L10n.get(settings.language, "settings"))
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
