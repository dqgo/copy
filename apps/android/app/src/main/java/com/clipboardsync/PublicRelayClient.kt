package com.clipboardsync

import java.io.BufferedReader
import java.io.InputStreamReader
import java.net.HttpURLConnection
import java.net.URL
import java.net.URLEncoder
import java.nio.charset.StandardCharsets
import java.util.UUID

data class PublicRelayConfig(
    val enabled: Boolean,
    val baseUrl: String,
    val bucket: String
)

object PublicRelayClient {
    private const val LEGACY_CLIPBOARD_KEY = "clipboard-sync-text"

    fun test(config: PublicRelayConfig): Boolean {
        if (!config.enabled || config.baseUrl.isBlank() || config.bucket.isBlank()) return false
        val probeKey = "clipboard-sync-probe-android-" + UUID.randomUUID().toString().substring(0, 8)
        val payload = "ok"
        val upload = request(buildObjectUrl(config, probeKey), "PUT", payload)
        if (upload == null) return false
        val remote = request(buildObjectUrl(config, probeKey), "GET", null)
        return remote?.trim() == payload
    }

    fun uploadLegacyText(config: PublicRelayConfig, text: String): Boolean {
        if (!config.enabled || config.baseUrl.isBlank() || config.bucket.isBlank()) return false
        return request(buildObjectUrl(config, LEGACY_CLIPBOARD_KEY), "PUT", text) != null
    }

    fun downloadLegacyText(config: PublicRelayConfig): String? {
        if (!config.enabled || config.baseUrl.isBlank() || config.bucket.isBlank()) return null
        return request(buildObjectUrl(config, LEGACY_CLIPBOARD_KEY), "GET", null)
    }

    fun uploadRoutedMessage(config: PublicRelayConfig, toDeviceId: String, fromDeviceId: String, payload: String): Boolean {
        if (!config.enabled || config.baseUrl.isBlank() || config.bucket.isBlank()) return false
        val key = buildRoutedKey(toDeviceId, fromDeviceId)
        return request(buildObjectUrl(config, key), "PUT", payload) != null
    }

    fun downloadRoutedMessage(config: PublicRelayConfig, toDeviceId: String, fromDeviceId: String): String? {
        if (!config.enabled || config.baseUrl.isBlank() || config.bucket.isBlank()) return null
        val key = buildRoutedKey(toDeviceId, fromDeviceId)
        return request(buildObjectUrl(config, key), "GET", null)
    }

    private fun buildRoutedKey(toDeviceId: String, fromDeviceId: String): String {
        val to = toDeviceId.trim().lowercase()
        val from = fromDeviceId.trim().lowercase()
        return "clipboard-$to-$from"
    }

    private fun buildObjectUrl(config: PublicRelayConfig, key: String): String {
        val base = config.baseUrl.trim().trimEnd('/')
        val encodedBucket = urlEncode(config.bucket.trim())
        val encodedKey = urlEncode(key.trim())
        return "$base/$encodedBucket/$encodedKey"
    }

    private fun urlEncode(value: String): String {
        return URLEncoder.encode(value, StandardCharsets.UTF_8.name()).replace("+", "%20")
    }

    private fun request(url: String, method: String, body: String?): String? {
        return try {
            val conn = URL(url).openConnection() as HttpURLConnection
            conn.requestMethod = method
            conn.connectTimeout = 5000
            conn.readTimeout = 5000
            if (body != null) {
                conn.doOutput = true
                conn.setRequestProperty("Content-Type", "text/plain; charset=utf-8")
                conn.outputStream.use { it.write(body.toByteArray(StandardCharsets.UTF_8)) }
            }
            val code = conn.responseCode
            if (code in 200..299) {
                if (method == "GET") {
                    BufferedReader(InputStreamReader(conn.inputStream, StandardCharsets.UTF_8)).use { it.readText() }
                } else {
                    "ok"
                }
            } else {
                null
            }
        } catch (_: Exception) {
            null
        }
    }
}
