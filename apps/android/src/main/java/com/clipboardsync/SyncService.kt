package com.clipboardsync

interface ClipboardReader {
    fun readText(): String?
}

interface ClipboardWriter {
    fun writeText(text: String)
}

interface SecureStore {
    fun get(key: String): String?
    fun set(key: String, value: String)
    fun delete(key: String)
}

class SyncService(
    private val reader: ClipboardReader,
    private val writer: ClipboardWriter,
    private val secureStore: SecureStore
) {
    fun captureClipboard(): String? = reader.readText()

    fun applyRemoteText(text: String) {
        writer.writeText(text)
    }

    fun saveWorkspaceKey(key: String) {
        secureStore.set("workspace_key", key)
    }
}
