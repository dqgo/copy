package com.clipboardsync

import java.io.BufferedReader
import java.io.InputStreamReader
import java.net.HttpURLConnection
import java.net.URL
import java.util.Base64

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

    fun saveWebDavSettings(baseUrl: String, username: String, password: String, enabled: Boolean) {
        secureStore.set("webdav_enabled", if (enabled) "1" else "0")
        secureStore.set("webdav_base_url", baseUrl)
        secureStore.set("webdav_username", username)
        secureStore.set("webdav_password", password)
    }

    fun loadWebDavSettings(): WebDavSettings {
        return WebDavSettings(
            enabled = secureStore.get("webdav_enabled") == "1",
            baseUrl = secureStore.get("webdav_base_url") ?: "",
            username = secureStore.get("webdav_username") ?: "",
            password = secureStore.get("webdav_password") ?: ""
        )
    }

    fun testWebDavConnection(): Boolean {
        val cfg = loadWebDavSettings()
        if (!cfg.enabled || cfg.baseUrl.isBlank()) return false
        return request(cfg.baseUrl, "HEAD", null, cfg) != null
    }

    fun uploadClipboardToWebDav(text: String): Boolean {
        val cfg = loadWebDavSettings()
        if (!cfg.enabled || cfg.baseUrl.isBlank()) return false
        val target = cfg.baseUrl.trimEnd('/') + "/clipboard-sync.txt"
        return request(target, "PUT", text, cfg) != null
    }

    fun downloadClipboardFromWebDav(): String? {
        val cfg = loadWebDavSettings()
        if (!cfg.enabled || cfg.baseUrl.isBlank()) return null
        val target = cfg.baseUrl.trimEnd('/') + "/clipboard-sync.txt"
        return request(target, "GET", null, cfg)
    }

    private fun request(url: String, method: String, body: String?, cfg: WebDavSettings): String? {
        return try {
            val conn = URL(url).openConnection() as HttpURLConnection
            conn.requestMethod = method
            conn.connectTimeout = 5000
            conn.readTimeout = 5000
            if (cfg.username.isNotBlank()) {
                val raw = Base64.getEncoder().encodeToString("${cfg.username}:${cfg.password}".toByteArray())
                conn.setRequestProperty("Authorization", "Basic $raw")
            }
            if (body != null) {
                conn.doOutput = true
                conn.setRequestProperty("Content-Type", "text/plain; charset=utf-8")
                conn.outputStream.use { it.write(body.toByteArray()) }
            }
            val code = conn.responseCode
            if (code in 200..299) {
                if (method == "GET") {
                    BufferedReader(InputStreamReader(conn.inputStream)).use { it.readText() }
                } else {
                    "ok"
                }
            } else null
        } catch (_: Exception) {
            null
        }
    }
}

data class WebDavSettings(
    val enabled: Boolean,
    val baseUrl: String,
    val username: String,
    val password: String
)
