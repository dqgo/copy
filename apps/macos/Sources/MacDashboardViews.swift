import SwiftUI
import AppKit

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
            "webdevUrl": "WebDev 地址",
            "webdevUser": "WebDev 用户名",
            "webdevPassword": "WebDev 密码",
            "testWebdev": "测试 WebDev 连接",
            "webdevFail": "WebDev 连接失败",
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
            "webdevUrl": "WebDev URL",
            "webdevUser": "WebDev Username",
            "webdevPassword": "WebDev Password",
            "testWebdev": "Test WebDev Connection",
            "webdevFail": "WebDev connection failed",
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

private struct MacTrustedDevice: Identifiable, Codable, Equatable {
    let id: String
    let name: String
    let lastSeen: String
}

private struct PairingRequestItem: Identifiable, Codable, Equatable {
    let id: String
    let deviceName: String
    let platform: String
    let at: String
}

private struct SyncHistoryItem: Identifiable, Codable, Equatable {
    var id = UUID()
    let direction: String
    let contentType: String
    let summary: String
    let time: String
}

private struct MacSettingsModel {
    var language: String
    var darkMode: Bool
    var webDevEnabled: Bool
    var webDevBaseUrl: String
    var webDevUsername: String
    var webDevPassword: String
    var localServerEnabled: Bool
}

struct MacStatusMenuView: View {
    @Environment(\.colorScheme) private var systemColorScheme

    @State private var settings = MacSettingsModel(
        language: "zh-CN",
        darkMode: false,
        webDevEnabled: false,
        webDevBaseUrl: "",
        webDevUsername: "",
        webDevPassword: "",
        localServerEnabled: false
    )
    @State private var status = StatusViewModel(
        connectionState: .disconnected,
        syncedOutCount: 0,
        syncedInCount: 0,
        rejectedEventCount: 0,
        trustedDeviceCount: 0,
        pendingPairingCount: 0,
        lastErrorMessage: nil
    )

    var body: some View {
        VStack(alignment: .leading, spacing: 10) {
            platformHero
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
                Task {
                    await performManualSync()
                }
            }
            .accessibilityLabel("Manual sync")

            Toggle(MacL10n.get(settings.language, "darkMode"), isOn: $settings.darkMode)
            Toggle(MacL10n.get(settings.language, "webdev"), isOn: $settings.webDevEnabled)
            TextField(MacL10n.get(settings.language, "webdevUrl"), text: $settings.webDevBaseUrl)
            TextField(MacL10n.get(settings.language, "webdevUser"), text: $settings.webDevUsername)
            SecureField(MacL10n.get(settings.language, "webdevPassword"), text: $settings.webDevPassword)
            Button(MacL10n.get(settings.language, "testWebdev")) {
                Task {
                    await testWebDavConnection()
                }
            }
            Toggle(MacL10n.get(settings.language, "server"), isOn: $settings.localServerEnabled)
        }
        .padding(12)
        .frame(width: 300)
        .background(LinearGradient(colors: [Color.blue.opacity(0.25), Color.teal.opacity(0.18)], startPoint: .topLeading, endPoint: .bottomTrailing))
        .preferredColorScheme(settings.darkMode ? .dark : (systemColorScheme == .dark ? .dark : .light))
        .onAppear {
            let store = SecureStoreAdapter()
            settings.webDevEnabled = store.get("webdav_enabled") == "1"
            settings.webDevBaseUrl = store.get("webdav_base_url") ?? ""
            settings.webDevUsername = store.get("webdav_username") ?? ""
            settings.webDevPassword = store.get("webdav_password") ?? ""
            settings.localServerEnabled = store.get("local_server_enabled") == "1"
            status.trustedDeviceCount = decodeList(store.get("trusted_devices_json"), as: [MacTrustedDevice].self).count
            status.pendingPairingCount = decodeList(store.get("pairing_requests_json"), as: [PairingRequestItem].self).count
        }
        .onChange(of: settings.webDevEnabled) { _, newValue in
            let store = SecureStoreAdapter()
            store.set("webdav_enabled", value: newValue ? "1" : "0")
        }
        .onChange(of: settings.webDevBaseUrl) { _, newValue in
            let store = SecureStoreAdapter()
            store.set("webdav_base_url", value: newValue)
        }
        .onChange(of: settings.webDevUsername) { _, newValue in
            let store = SecureStoreAdapter()
            store.set("webdav_username", value: newValue)
        }
        .onChange(of: settings.webDevPassword) { _, newValue in
            let store = SecureStoreAdapter()
            store.set("webdav_password", value: newValue)
        }
        .onChange(of: settings.localServerEnabled) { _, newValue in
            let store = SecureStoreAdapter()
            store.set("local_server_enabled", value: newValue ? "1" : "0")
        }
    }

    private var platformHero: some View {
        Canvas { context, size in
            let bg = Path(roundedRect: CGRect(x: 0, y: 0, width: size.width, height: size.height), cornerSize: CGSize(width: 18, height: 18))
            context.fill(bg, with: .linearGradient(Gradient(colors: [Color(red: 0.07, green: 0.2, blue: 0.35), Color(red: 0.14, green: 0.45, blue: 0.42)]), startPoint: .zero, endPoint: CGPoint(x: size.width, y: size.height)))

            let card = Path(roundedRect: CGRect(x: 16, y: 16, width: size.width * 0.36, height: size.height * 0.6), cornerSize: CGSize(width: 14, height: 14))
            context.fill(card, with: .color(Color.white.opacity(0.9)))

            let board = Path(roundedRect: CGRect(x: size.width * 0.58, y: 18, width: size.width * 0.24, height: size.height * 0.52), cornerSize: CGSize(width: 10, height: 10))
            context.stroke(board, with: .color(Color(red: 0.98, green: 0.76, blue: 0.3)), lineWidth: 3)
            context.fill(Path(ellipseIn: CGRect(x: size.width * 0.68, y: size.height * 0.62, width: 12, height: 12)), with: .color(Color(red: 1, green: 0.43, blue: 0.39)))
        }
        .frame(height: 86)
        .overlay(alignment: .bottomLeading) {
            Text("Clipboard Sync")
                .font(.caption.weight(.semibold))
                .padding(.horizontal, 8)
                .padding(.vertical, 4)
                .background(.regularMaterial, in: Capsule())
                .padding(8)
        }
        .clipShape(RoundedRectangle(cornerRadius: 18, style: .continuous))
    }

    private func performManualSync() async {
        if settings.webDevEnabled && !settings.webDevBaseUrl.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
            let store = SecureStoreAdapter()
            store.set("webdav_enabled", value: settings.webDevEnabled ? "1" : "0")
            store.set("webdav_base_url", value: settings.webDevBaseUrl)
            store.set("webdav_username", value: settings.webDevUsername)
            store.set("webdav_password", value: settings.webDevPassword)

            let localText = NSPasteboard.general.string(forType: .string) ?? ""
            let uploadOk = await uploadToWebDav(text: localText)
            if !uploadOk {
                await MainActor.run {
                    status.lastErrorMessage = "WebDev upload failed"
                }
                return
            }

            let remoteText = await downloadFromWebDav()
            await MainActor.run {
                status.syncedOutCount += 1
                status.syncedInCount += 1
                if let remoteText, !remoteText.isEmpty {
                    NSPasteboard.general.clearContents()
                    NSPasteboard.general.setString(remoteText, forType: .string)
                }
                status.lastErrorMessage = nil
            }
            return
        }

        await MainActor.run {
            status.syncedOutCount += 1
            status.syncedInCount += 1
            status.lastErrorMessage = nil
        }
    }

    private func testWebDavConnection() async {
        guard settings.webDevEnabled, !settings.webDevBaseUrl.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty else {
            await MainActor.run {
                status.lastErrorMessage = MacL10n.get(settings.language, "webdevFail")
            }
            return
        }

        let base = settings.webDevBaseUrl.hasSuffix("/") ? settings.webDevBaseUrl : settings.webDevBaseUrl + "/"
        guard let url = URL(string: base) else {
            await MainActor.run {
                status.lastErrorMessage = MacL10n.get(settings.language, "webdevFail")
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
                status.lastErrorMessage = ok ? nil : MacL10n.get(settings.language, "webdevFail")
            }
        } catch {
            await MainActor.run {
                status.lastErrorMessage = MacL10n.get(settings.language, "webdevFail")
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
}

struct MacTrustedDevicesWindowView: View {
    @State private var devices: [MacTrustedDevice] = []
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
        .onAppear {
            let store = SecureStoreAdapter()
            devices = decodeList(store.get("trusted_devices_json"), as: [MacTrustedDevice].self)
        }
        .onChange(of: devices) { _, newValue in
            persistList("trusted_devices_json", value: newValue)
        }
    }
}

struct MacPairingRequestsWindowView: View {
    @State private var requests: [PairingRequestItem] = []
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
        .onAppear {
            let store = SecureStoreAdapter()
            requests = decodeList(store.get("pairing_requests_json"), as: [PairingRequestItem].self)
        }
        .onChange(of: requests) { _, newValue in
            persistList("pairing_requests_json", value: newValue)
        }
    }
}

struct MacHistoryWindowView: View {
    @State private var history: [SyncHistoryItem] = []

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
        .onAppear {
            let store = SecureStoreAdapter()
            history = decodeList(store.get("history_items_json"), as: [SyncHistoryItem].self)
        }
        .onChange(of: history) { _, newValue in
            persistList("history_items_json", value: newValue)
        }
    }
}

private func persistList<T: Codable>(_ key: String, value: T) {
    let store = SecureStoreAdapter()
    guard let data = try? JSONEncoder().encode(value), let json = String(data: data, encoding: .utf8) else { return }
    store.set(key, value: json)
}

private func decodeList<T: Codable>(_ raw: String?, as type: T.Type) -> T {
    guard let raw, let data = raw.data(using: .utf8), let decoded = try? JSONDecoder().decode(type, from: data) else {
        if type == [MacTrustedDevice].self { return [] as! T }
        if type == [PairingRequestItem].self { return [] as! T }
        if type == [SyncHistoryItem].self { return [] as! T }
        fatalError("Unsupported decode type")
    }
    return decoded
}

#Preview {
    MacStatusMenuView()
}
