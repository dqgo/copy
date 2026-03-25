package com.clipboardsync

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.os.Handler
import android.os.Looper
import android.widget.Toast
import org.json.JSONArray
import org.json.JSONObject

class QuickActionReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        when (intent.action) {
            "com.clipboardsync.ACTION_MANUAL_SYNC" -> {
                Thread {
                    val prefs = context.getSharedPreferences("clipboardsync_settings", Context.MODE_PRIVATE)
                    val config = WebDavConfig(
                        enabled = prefs.getBoolean("webdav_enabled", false),
                        baseUrl = prefs.getString("webdav_base_url", "") ?: "",
                        username = prefs.getString("webdav_username", "") ?: "",
                        password = prefs.getString("webdav_password", "") ?: ""
                    )

                    if (!config.enabled || config.baseUrl.isBlank()) {
                        postToast(context, "WebDev not configured")
                        return@Thread
                    }

                    val outText = "manual sync from quick action @${System.currentTimeMillis()}"
                    val uploaded = WebDavClient.uploadText(config, outText)
                    val remote = WebDavClient.downloadText(config)

                    appendHistory(prefs, "out", "text/plain", outText)
                    if (!remote.isNullOrBlank()) {
                        appendHistory(prefs, "in", "text/plain", remote.take(128))
                    }

                    postToast(context, if (uploaded) "Manual sync completed" else "Manual sync failed")
                }.start()
            }
            "com.clipboardsync.ACTION_PAUSE_SYNC" -> {
                val prefs = context.getSharedPreferences("clipboardsync_settings", Context.MODE_PRIVATE)
                val paused = !prefs.getBoolean("sync_paused", false)
                prefs.edit().putBoolean("sync_paused", paused).apply()
                postToast(context, if (paused) "Sync paused" else "Sync resumed")
            }
        }
    }

    private fun postToast(context: Context, message: String) {
        Handler(Looper.getMainLooper()).post {
            Toast.makeText(context, message, Toast.LENGTH_SHORT).show()
        }
    }

    private fun appendHistory(prefs: android.content.SharedPreferences, direction: String, contentType: String, preview: String) {
        val raw = prefs.getString("history_items_json", "[]") ?: "[]"
        val array = try { JSONArray(raw) } catch (_: Exception) { JSONArray() }
        val entry = JSONObject()
            .put("direction", direction)
            .put("contentType", contentType)
            .put("preview", preview)
            .put("at", "now")

        val newArray = JSONArray()
        newArray.put(entry)
        for (i in 0 until array.length().coerceAtMost(299)) {
            newArray.put(array.getJSONObject(i))
        }
        prefs.edit().putString("history_items_json", newArray.toString()).apply()
    }
}
