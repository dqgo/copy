import Foundation

final class SecureStoreAdapter: SecureStore {
    private var map: [String: String] = [:]

    func get(_ key: String) -> String? {
        return map[key]
    }

    func set(_ key: String, value: String) {
        map[key] = value
    }

    func delete(_ key: String) {
        map.removeValue(forKey: key)
    }
}
