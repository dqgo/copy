package com.clipboardsync

import java.io.BufferedReader
import java.io.InputStreamReader
import java.net.HttpURLConnection
import java.net.URL
import java.util.Base64

data class WebDavConfig(
    val enabled: Boolean,
    val baseUrl: String,
    val username: String,
    val password: String
)

object WebDavClient {
    fun test(config: WebDavConfig): Boolean {
        if (!config.enabled || config.baseUrl.isBlank()) return false
        return request(config.baseUrl, "HEAD", null, config) != null
    }

    fun uploadText(config: WebDavConfig, text: String): Boolean {
        if (!config.enabled || config.baseUrl.isBlank()) return false
        val target = config.baseUrl.trimEnd('/') + "/clipboard-sync.txt"
        return request(target, "PUT", text, config) != null
    }

    fun downloadText(config: WebDavConfig): String? {
        if (!config.enabled || config.baseUrl.isBlank()) return null
        val target = config.baseUrl.trimEnd('/') + "/clipboard-sync.txt"
        return request(target, "GET", null, config)
    }

    private fun request(url: String, method: String, body: String?, config: WebDavConfig): String? {
        return try {
            val conn = URL(url).openConnection() as HttpURLConnection
            conn.requestMethod = method
            conn.connectTimeout = 5000
            conn.readTimeout = 5000
            if (config.username.isNotBlank()) {
                val token = Base64.getEncoder().encodeToString("${config.username}:${config.password}".toByteArray())
                conn.setRequestProperty("Authorization", "Basic $token")
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
