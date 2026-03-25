import Foundation

protocol ClipboardReader {
    func readText() -> String?
}

protocol ClipboardWriter {
    func writeText(_ text: String)
}

protocol SecureStore {
    func get(_ key: String) -> String?
    func set(_ key: String, value: String)
    func delete(_ key: String)
}

final class SyncService {
    private let reader: ClipboardReader
    private let writer: ClipboardWriter
    private let store: SecureStore

    init(reader: ClipboardReader, writer: ClipboardWriter, store: SecureStore) {
        self.reader = reader
        self.writer = writer
        self.store = store
    }

    func captureClipboard() -> String? {
        return reader.readText()
    }

    func applyRemoteText(_ text: String) {
        writer.writeText(text)
    }

    func saveWorkspaceKey(_ key: String) {
        store.set("workspace_key", value: key)
    }

    func saveWebDavSettings(baseUrl: String, username: String, password: String, enabled: Bool) {
        store.set("webdav_enabled", value: enabled ? "1" : "0")
        store.set("webdav_base_url", value: baseUrl)
        store.set("webdav_username", value: username)
        store.set("webdav_password", value: password)
    }

    func loadWebDavSettings() -> (enabled: Bool, baseUrl: String, username: String, password: String) {
        (
            store.get("webdav_enabled") == "1",
            store.get("webdav_base_url") ?? "",
            store.get("webdav_username") ?? "",
            store.get("webdav_password") ?? ""
        )
    }

    func testWebDavConnection() async -> Bool {
        let cfg = loadWebDavSettings()
        guard cfg.enabled, let url = URL(string: cfg.baseUrl) else { return false }
        var request = URLRequest(url: url)
        request.httpMethod = "HEAD"
        applyBasicAuth(&request, username: cfg.username, password: cfg.password)

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

    func uploadClipboardToWebDav(_ text: String) async -> Bool {
        let cfg = loadWebDavSettings()
        guard cfg.enabled else { return false }
        let base = cfg.baseUrl.hasSuffix("/") ? cfg.baseUrl : cfg.baseUrl + "/"
        guard let url = URL(string: base + "clipboard-sync.txt") else { return false }

        var request = URLRequest(url: url)
        request.httpMethod = "PUT"
        request.httpBody = text.data(using: .utf8)
        request.setValue("text/plain; charset=utf-8", forHTTPHeaderField: "Content-Type")
        applyBasicAuth(&request, username: cfg.username, password: cfg.password)

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

    func downloadClipboardFromWebDav() async -> String? {
        let cfg = loadWebDavSettings()
        guard cfg.enabled else { return nil }
        let base = cfg.baseUrl.hasSuffix("/") ? cfg.baseUrl : cfg.baseUrl + "/"
        guard let url = URL(string: base + "clipboard-sync.txt") else { return nil }

        var request = URLRequest(url: url)
        request.httpMethod = "GET"
        applyBasicAuth(&request, username: cfg.username, password: cfg.password)

        do {
            let (data, response) = try await URLSession.shared.data(for: request)
            if let http = response as? HTTPURLResponse, (200...299).contains(http.statusCode) {
                return String(data: data, encoding: .utf8)
            }
            return nil
        } catch {
            return nil
        }
    }

    private func applyBasicAuth(_ request: inout URLRequest, username: String, password: String) {
        guard !username.isEmpty else { return }
        let raw = Data("\(username):\(password)".utf8).base64EncodedString()
        request.setValue("Basic \(raw)", forHTTPHeaderField: "Authorization")
    }
}
