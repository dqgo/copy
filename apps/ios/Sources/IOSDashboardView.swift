import SwiftUI
import UIKit

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
            "webdevUrl": "WebDev 地址",
            "webdevUser": "WebDev 用户名",
            "webdevPassword": "WebDev 密码",
            "testWebdev": "测试 WebDev 连接",
            "webdevOk": "WebDev 连接成功",
            "webdevFail": "WebDev 连接失败",
            "server": "本地服务模式",
            "manualSync": "手动同步",
            "noDevices": "暂无设备",
            "noPairingRequests": "暂无配对请求",
            "noHistory": "暂无历史记录",
            "lastError": "最近错误",
            "confirm": "确认",
            "cancel": "取消",
            "searchDevices": "搜索设备",
            "searchHistory": "搜索历史",
            "searchPairing": "搜索配对请求",
            "clearFilter": "清空筛选",
            "emptyDevicesHint": "暂无可信设备，可前往配对页添加",
            "emptyPairingHint": "暂无配对请求，等待新设备发起即可",
            "emptyHistoryHint": "暂无同步历史，执行一次手动同步即可"
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
            "webdevUrl": "WebDev URL",
            "webdevUser": "WebDev Username",
            "webdevPassword": "WebDev Password",
            "testWebdev": "Test WebDev Connection",
            "webdevOk": "WebDev connection succeeded",
            "webdevFail": "WebDev connection failed",
            "server": "Local Server Mode",
            "manualSync": "Manual Sync",
            "noDevices": "No devices",
            "noPairingRequests": "No pairing requests",
            "noHistory": "No history",
            "lastError": "Last Error",
            "confirm": "Confirm",
            "cancel": "Cancel",
            "searchDevices": "Search devices",
            "searchHistory": "Search history",
            "searchPairing": "Search pairing requests",
            "clearFilter": "Clear filter",
            "emptyDevicesHint": "No trusted devices yet. Add one from pairing tab.",
            "emptyPairingHint": "No pairing requests for now.",
            "emptyHistoryHint": "No sync history yet. Run manual sync to create records."
        ]
    ]

    static func get(_ language: String, _ key: String) -> String {
        translations[language]?[key] ?? translations["en-US"]?[key] ?? key
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
    let id = UUID()
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
    var webDevBaseUrl: String
    var webDevUsername: String
    var webDevPassword: String
    var localServerEnabled: Bool
    var pairingPolicy: String
}

struct IOSDashboardView: View {
    @Environment(\.colorScheme) private var systemColorScheme

    @State private var selectedTab = 0
    @State private var showConfirmDialog = false
    @State private var confirmDialogTitle = ""
    @State private var confirmDialogMessage = ""
    @State private var confirmDialogOnConfirm: (() -> Void)? = nil

    @State private var deviceQuery = ""
    @State private var historyQuery = ""
    @State private var pairingQuery = ""

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
        webDevBaseUrl: "",
        webDevUsername: "",
        webDevPassword: "",
        localServerEnabled: false,
        pairingPolicy: "manual-approve"
    )

    private var filteredDevices: [TrustedDevice] {
        let q = deviceQuery.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()
        guard !q.isEmpty else { return trustedDevices }
        return trustedDevices.filter { $0.name.lowercased().contains(q) || $0.id.lowercased().contains(q) }
    }

    private var filteredHistory: [HistoryItem] {
        let q = historyQuery.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()
        guard !q.isEmpty else { return history }
        return history.filter { $0.preview.lowercased().contains(q) || $0.contentType.lowercased().contains(q) }
    }

    private var filteredPairing: [PairingRequest] {
        let q = pairingQuery.trimmingCharacters(in: .whitespacesAndNewlines).lowercased()
        guard !q.isEmpty else { return pairingRequests }
        return pairingRequests.filter { $0.deviceName.lowercased().contains(q) || $0.platform.lowercased().contains(q) }
    }

    var body: some View {
        ZStack {
            LinearGradient(
                colors: [Color(red: 0.06, green: 0.16, blue: 0.35), Color(red: 0.07, green: 0.35, blue: 0.42)],
                startPoint: .topLeading,
                endPoint: .bottomTrailing
            )
            .ignoresSafeArea()

            VStack {
                if let error = errorMessage {
                    HStack {
                        Text(error)
                            .foregroundStyle(.red)
                        Spacer()
                        Button("×") { errorMessage = nil }
                            .accessibilityLabel("Dismiss error")
                    }
                    .padding(12)
                    .background(Color.red.opacity(0.15))
                    .cornerRadius(8)
                    .padding(.horizontal)
                }

                TabView(selection: $selectedTab) {
                    statusView.tag(0).tabItem { Label(L10n.get(settings.language, "status"), systemImage: "chart.bar") }
                    devicesView.tag(1).tabItem { Label(L10n.get(settings.language, "devices"), systemImage: "lock.shield") }
                    pairingView.tag(2).tabItem { Label(L10n.get(settings.language, "pairing"), systemImage: "person.badge.plus") }
                    historyView.tag(3).tabItem { Label(L10n.get(settings.language, "history"), systemImage: "clock") }
                    settingsView.tag(4).tabItem { Label(L10n.get(settings.language, "settings"), systemImage: "gearshape") }
                }
                .animation(.easeInOut(duration: 0.25), value: selectedTab)
            }
        }
        .preferredColorScheme(effectiveColorScheme)
        .onAppear {
            if systemColorScheme == .dark {
                settings.darkMode = true
            }
            let store = SecureStoreAdapter()
            settings.webDevEnabled = store.get("webdav_enabled") == "1"
            settings.webDevBaseUrl = store.get("webdav_base_url") ?? ""
            settings.webDevUsername = store.get("webdav_username") ?? ""
            settings.webDevPassword = store.get("webdav_password") ?? ""
        }
        .alert(confirmDialogTitle, isPresented: $showConfirmDialog) {
            Button(L10n.get(settings.language, "cancel"), role: .cancel) { }
            Button(L10n.get(settings.language, "confirm"), role: .destructive) {
                confirmDialogOnConfirm?()
            }
        } message: {
            Text(confirmDialogMessage)
        }
    }

    private var effectiveColorScheme: ColorScheme? {
        settings.darkMode ? .dark : (systemColorScheme == .dark ? .dark : .light)
    }

    private var statusView: some View {
        NavigationStack {
            ScrollView {
                VStack(spacing: 14) {
                    statusCard
                    Button(L10n.get(settings.language, "manualSync")) {
                        Task {
                            await performManualSync()
                        }
                    }
                    .buttonStyle(.borderedProminent)
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .accessibilityLabel("Manual sync")
                    .accessibilityHint("Synchronizes clipboard with trusted devices")
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
        .accessibilityElement(children: .contain)
    }

    private var devicesView: some View {
        NavigationStack {
            List {
                TextField(L10n.get(settings.language, "searchDevices"), text: $deviceQuery)
                    .textInputAutocapitalization(.never)
                    .accessibilityLabel("Search devices")

                if filteredDevices.isEmpty {
                    emptyStateView(
                        title: L10n.get(settings.language, "noDevices"),
                        message: L10n.get(settings.language, "emptyDevicesHint"),
                        actionTitle: deviceQuery.isEmpty ? nil : L10n.get(settings.language, "clearFilter"),
                        action: deviceQuery.isEmpty ? nil : { deviceQuery = "" }
                    )
                } else {
                    ForEach(filteredDevices) { device in
                        VStack(alignment: .leading, spacing: 4) {
                            Text(device.name).font(.headline)
                            Text("ID: \(device.id)").font(.caption)
                            Text("Last seen: \(device.lastSeen)").font(.caption2)
                        }
                        .swipeActions(edge: .trailing, allowsFullSwipe: false) {
                            Button(role: .destructive) {
                                confirmDialogTitle = L10n.get(settings.language, "revoke")
                                confirmDialogMessage = "Revoke \(device.name)?"
                                confirmDialogOnConfirm = {
                                    trustedDevices.removeAll { $0.id == device.id }
                                    status.rejectedEventCount += 1
                                    status.trustedDeviceCount = max(0, status.trustedDeviceCount - 1)
                                    status.lastErrorMessage = "Revoked device: \(device.id)"
                                }
                                showConfirmDialog = true
                            } label: {
                                Text(L10n.get(settings.language, "revoke"))
                            }
                        }
                        .accessibilityLabel("Device \(device.name)")
                    }
                }
            }
            .navigationTitle(L10n.get(settings.language, "devices"))
        }
    }

    private var pairingView: some View {
        NavigationStack {
            List {
                TextField(L10n.get(settings.language, "searchPairing"), text: $pairingQuery)
                    .textInputAutocapitalization(.never)
                    .accessibilityLabel("Search pairing requests")

                if filteredPairing.isEmpty {
                    emptyStateView(
                        title: L10n.get(settings.language, "noPairingRequests"),
                        message: L10n.get(settings.language, "emptyPairingHint"),
                        actionTitle: pairingQuery.isEmpty ? nil : L10n.get(settings.language, "clearFilter"),
                        action: pairingQuery.isEmpty ? nil : { pairingQuery = "" }
                    )
                } else {
                    ForEach(filteredPairing) { req in
                        VStack(alignment: .leading, spacing: 8) {
                            Text(req.deviceName).font(.headline)
                            Text("\(L10n.get(settings.language, "status")): \(req.platform)").font(.caption)
                            Text("Requested: \(req.requestedAt)").font(.caption2)
                            HStack {
                                Button(L10n.get(settings.language, "approve")) {
                                    pairingRequests.removeAll { $0.id == req.id }
                                    status.pendingPairingCount = max(0, status.pendingPairingCount - 1)
                                    status.trustedDeviceCount += 1
                                }
                                .buttonStyle(.borderedProminent)
                                .accessibilityLabel("Approve \(req.deviceName)")

                                Button(L10n.get(settings.language, "reject"), role: .destructive) {
                                    confirmDialogTitle = L10n.get(settings.language, "reject")
                                    confirmDialogMessage = "Reject \(req.deviceName)?"
                                    confirmDialogOnConfirm = {
                                        pairingRequests.removeAll { $0.id == req.id }
                                        status.pendingPairingCount = max(0, status.pendingPairingCount - 1)
                                    }
                                    showConfirmDialog = true
                                }
                                .buttonStyle(.bordered)
                                .accessibilityLabel("Reject \(req.deviceName)")
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
                TextField(L10n.get(settings.language, "searchHistory"), text: $historyQuery)
                    .textInputAutocapitalization(.never)
                    .accessibilityLabel("Search history")

                if filteredHistory.isEmpty {
                    emptyStateView(
                        title: L10n.get(settings.language, "noHistory"),
                        message: L10n.get(settings.language, "emptyHistoryHint"),
                        actionTitle: historyQuery.isEmpty ? nil : L10n.get(settings.language, "clearFilter"),
                        action: historyQuery.isEmpty ? nil : { historyQuery = "" }
                    )
                } else {
                    ForEach(filteredHistory) { item in
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
                TextField(L10n.get(settings.language, "webdevUrl"), text: $settings.webDevBaseUrl)
                    .textInputAutocapitalization(.never)
                    .autocorrectionDisabled(true)
                TextField(L10n.get(settings.language, "webdevUser"), text: $settings.webDevUsername)
                    .textInputAutocapitalization(.never)
                    .autocorrectionDisabled(true)
                SecureField(L10n.get(settings.language, "webdevPassword"), text: $settings.webDevPassword)
                Button(L10n.get(settings.language, "testWebdev")) {
                    Task {
                        await testWebDavConnection()
                    }
                }
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

    private func performManualSync() async {
        if settings.webDevEnabled && !settings.webDevBaseUrl.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
            let store = SecureStoreAdapter()
            store.set("webdav_enabled", value: settings.webDevEnabled ? "1" : "0")
            store.set("webdav_base_url", value: settings.webDevBaseUrl)
            store.set("webdav_username", value: settings.webDevUsername)
            store.set("webdav_password", value: settings.webDevPassword)

            let localText = UIPasteboard.general.string ?? ""
            let uploadOk = await uploadToWebDav(text: localText)
            if !uploadOk {
                await MainActor.run {
                    status.lastErrorMessage = "WebDev upload failed"
                    errorMessage = "WebDev upload failed"
                }
                return
            }

            let remoteText = await downloadFromWebDav()
            await MainActor.run {
                status.syncedOutCount += 1
                status.syncedInCount += 1
                if let remoteText, !remoteText.isEmpty {
                    UIPasteboard.general.string = remoteText
                    history.insert(HistoryItem(direction: "in", contentType: "text/plain", preview: remoteText, at: "now"), at: 0)
                    status.lastErrorMessage = nil
                    errorMessage = nil
                } else {
                    history.insert(HistoryItem(direction: "out", contentType: "text/plain", preview: localText, at: "now"), at: 0)
                    status.lastErrorMessage = nil
                    errorMessage = nil
                }
            }
            return
        }

        await MainActor.run {
            status.syncedOutCount += 1
            status.syncedInCount += 1
            status.lastErrorMessage = nil
            errorMessage = nil
            history.insert(HistoryItem(direction: "out", contentType: "text/plain", preview: "manual sync", at: "now"), at: 0)
        }
    }

    private func testWebDavConnection() async {
        guard settings.webDevEnabled, !settings.webDevBaseUrl.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty else {
            await MainActor.run {
                status.lastErrorMessage = L10n.get(settings.language, "webdevFail")
                errorMessage = L10n.get(settings.language, "webdevFail")
            }
            return
        }

        let base = settings.webDevBaseUrl.hasSuffix("/") ? settings.webDevBaseUrl : settings.webDevBaseUrl + "/"
        guard let url = URL(string: base) else {
            await MainActor.run {
                status.lastErrorMessage = L10n.get(settings.language, "webdevFail")
                errorMessage = L10n.get(settings.language, "webdevFail")
            }
            return
        }

        var request = URLRequest(url: url)
        request.httpMethod = "HEAD"
        applyBasicAuth(&request)

        do {
            let (_, response) = try await URLSession.shared.data(for: request)
            let ok = (response as? HTTPURLResponse).map { (200...299).contains($0.statusCode) } ?? false
            await MainActor.run {
                if ok {
                    status.lastErrorMessage = nil
                    errorMessage = nil
                } else {
                    status.lastErrorMessage = L10n.get(settings.language, "webdevFail")
                    errorMessage = L10n.get(settings.language, "webdevFail")
                }
            }
        } catch {
            await MainActor.run {
                status.lastErrorMessage = L10n.get(settings.language, "webdevFail")
                errorMessage = L10n.get(settings.language, "webdevFail")
            }
        }
    }

    private func uploadToWebDav(text: String) async -> Bool {
        let base = settings.webDevBaseUrl.hasSuffix("/") ? settings.webDevBaseUrl : settings.webDevBaseUrl + "/"
        guard let url = URL(string: base + "clipboard-sync.txt") else { return false }
        var request = URLRequest(url: url)
        request.httpMethod = "PUT"
        request.httpBody = text.data(using: .utf8)
        request.setValue("text/plain; charset=utf-8", forHTTPHeaderField: "Content-Type")
        applyBasicAuth(&request)

        do {
            let (_, response) = try await URLSession.shared.data(for: request)
            if let http = response as? HTTPURLResponse {
                return (200...299).contains(http.statusCode)
            }
            return false
        } catch {
            return false
        }
    }

    private func downloadFromWebDav() async -> String? {
        let base = settings.webDevBaseUrl.hasSuffix("/") ? settings.webDevBaseUrl : settings.webDevBaseUrl + "/"
        guard let url = URL(string: base + "clipboard-sync.txt") else { return nil }
        var request = URLRequest(url: url)
        request.httpMethod = "GET"
        applyBasicAuth(&request)

        do {
            let (data, response) = try await URLSession.shared.data(for: request)
            guard let http = response as? HTTPURLResponse, (200...299).contains(http.statusCode) else { return nil }
            return String(data: data, encoding: .utf8)
        } catch {
            return nil
        }
    }

    private func applyBasicAuth(_ request: inout URLRequest) {
        guard !settings.webDevUsername.isEmpty else { return }
        let raw = Data("\(settings.webDevUsername):\(settings.webDevPassword)".utf8).base64EncodedString()
        request.setValue("Basic \(raw)", forHTTPHeaderField: "Authorization")
    }

    @ViewBuilder
    private func emptyStateView(title: String, message: String, actionTitle: String?, action: (() -> Void)?) -> some View {
        VStack(alignment: .leading, spacing: 8) {
            Text(title).font(.headline)
            Text(message).foregroundStyle(.secondary)
            if let actionTitle, let action {
                Button(actionTitle, action: action)
                    .buttonStyle(.bordered)
            }
        }
        .padding(.vertical, 8)
    }
}

#Preview {
    IOSDashboardView()
}
