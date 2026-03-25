package com.clipboardsync

class SecureStoreAdapter : SecureStore {
    private val map = mutableMapOf<String, String>()

    override fun get(key: String): String? = map[key]

    override fun set(key: String, value: String) {
        map[key] = value
    }

    override fun delete(key: String) {
        map.remove(key)
    }
}
