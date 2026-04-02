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
                    val secureStore = AndroidKeystoreStore(context)
                    val identity = SyncCoordinator.ensureIdentity(secureStore)
                    val settings = SyncSettingsSnapshot(
                        webDavEnabled = prefs.getBoolean("webdav_enabled", false),
                        webDavBaseUrl = prefs.getString("webdav_base_url", "") ?: "",
                        webDavUsername = prefs.getString("webdav_username", "") ?: "",
                        webDavPassword = prefs.getString("webdav_password", "") ?: "",
                        publicRelayEnabled = prefs.getBoolean("public_relay_enabled", true),
                        publicRelayBaseUrl = prefs.getString("public_relay_base_url", "https://kvdb.io") ?: "https://kvdb.io",
                        publicRelayBucket = prefs.getString("public_relay_bucket", identity.first) ?: identity.first
                    )
                    val trustedDevices = SyncCoordinator.decodeTrustedDevices(prefs.getString("trusted_devices_json", null))
                    val outgoingPreview = SyncCoordinator.readClipboardText(context).orEmpty().take(128)
                    val result = SyncCoordinator.syncNow(
                        context = context,
                        secureStore = secureStore,
                        prefs = prefs,
                        settings = settings,
                        trustedDevices = trustedDevices
                    )

                    if (result.sentCount > 0 && outgoingPreview.isNotBlank()) {
                        appendHistory(prefs, "out", "text/plain", outgoingPreview)
                    }
                    if (!result.receivedText.isNullOrBlank()) {
                        appendHistory(prefs, "in", "text/plain", result.receivedText.take(128))
                    }

                    if (result.errorMessage != null) {
                        postToast(context, result.errorMessage)
                    } else {
                        postToast(context, "Manual sync completed")
                    }

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
