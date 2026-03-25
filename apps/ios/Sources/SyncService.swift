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
}
