package com.clipboardsync

import android.content.ClipData
import android.content.ClipboardManager
import android.content.Context
import android.content.SharedPreferences
import android.util.Base64
import org.json.JSONObject
import java.nio.charset.StandardCharsets
import java.util.UUID

data class InvitePayload(
    val workspaceKey: String,
    val deviceId: String,
    val deviceName: String,
    val platform: String
)

data class RelayMessage(
    val messageId: String,
    val workspaceKey: String,
    val fromDeviceId: String,
    val toDeviceId: String,
    val text: String,
    val sentAt: String,
    val client: String
)

data class SyncSettingsSnapshot(
    val webDavEnabled: Boolean,
    val webDavBaseUrl: String,
    val webDavUsername: String,
    val webDavPassword: String,
    val publicRelayEnabled: Boolean,
    val publicRelayBaseUrl: String,
    val publicRelayBucket: String
)

data class SyncNowResult(
    val sentCount: Int,
    val receivedText: String?,
    val receivedFrom: String?,
    val errorMessage: String?
)

object SyncCoordinator {
    private const val RELAY_LAST_APPLIED_KEY = "relay_last_applied_by_sender_json"
    private const val RELAY_LAST_UPLOADED_KEY = "relay_last_uploaded_by_target_json"
    private val deviceIdRegex = Regex("^[A-Za-z0-9][A-Za-z0-9._-]{4,62}[A-Za-z0-9]$")

    fun ensureIdentity(secureStore: AndroidKeystoreStore): Pair<String, String> {
        var workspaceKey = secureStore.get("workspace_key")
        if (workspaceKey.isNullOrBlank()) {
            workspaceKey = "wsk-" + UUID.randomUUID().toString().replace("-", "")
            secureStore.set("workspace_key", workspaceKey)
        }

        var deviceId = secureStore.get("device_id")
        if (deviceId.isNullOrBlank()) {
            deviceId = "and-" + UUID.randomUUID().toString().replace("-", "").take(12)
            secureStore.set("device_id", deviceId)
        }

        return workspaceKey to deviceId
    }

    fun createInviteCode(workspaceKey: String, deviceId: String, deviceName: String, platform: String): String {
        val payload = JSONObject()
            .put("WorkspaceKey", workspaceKey)
            .put("DeviceId", deviceId)
            .put("DeviceName", deviceName)
            .put("Platform", platform)
        val raw = payload.toString().toByteArray(StandardCharsets.UTF_8)
        return Base64.encodeToString(raw, Base64.NO_WRAP or Base64.URL_SAFE).trimEnd('=')
    }

    fun isValidDeviceId(deviceId: String): Boolean {
        val normalized = deviceId.trim()
        if (normalized.length < 6 || normalized.length > 64) {
            return false
        }
        val dashIndex = normalized.indexOf('-')
        if (dashIndex <= 0 || dashIndex >= normalized.length - 1) {
            return false
        }
        return deviceIdRegex.matches(normalized)
    }

    fun tryParseInviteCode(inviteCode: String): InvitePayload? {
        if (inviteCode.isBlank()) return null
        return try {
            val normalized = inviteCode.trim()
            val padded = when (normalized.length % 4) {
                2 -> "$normalized=="
                3 -> "$normalized="
                else -> normalized
            }
            val raw = Base64.decode(padded, Base64.URL_SAFE)
            val json = JSONObject(String(raw, StandardCharsets.UTF_8))
            val workspaceKey = json.optString("WorkspaceKey")
            val deviceId = json.optString("DeviceId")
            val deviceName = json.optString("DeviceName")
            val platform = json.optString("Platform", "android")
            if (workspaceKey.isBlank() || deviceId.isBlank() || deviceName.isBlank()) {
                null
            } else {
                InvitePayload(workspaceKey, deviceId, deviceName, platform)
            }
        } catch (_: Exception) {
            null
        }
    }

    fun readClipboardText(context: Context): String? {
        return try {
            val manager = context.getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager
            if (!manager.hasPrimaryClip()) return null
            val item = manager.primaryClip?.getItemAt(0) ?: return null
            item.coerceToText(context)?.toString()
        } catch (_: Exception) {
            null
        }
    }

    fun writeClipboardText(context: Context, text: String) {
        try {
            val manager = context.getSystemService(Context.CLIPBOARD_SERVICE) as ClipboardManager
            manager.setPrimaryClip(ClipData.newPlainText("clipboard-sync", text))
        } catch (_: Exception) {
            // Ignore clipboard contention and permission timing issues.
        }
    }

    fun decodeTrustedDevices(raw: String?): List<TrustedDeviceUi> {
        if (raw.isNullOrBlank()) return emptyList()
        return try {
            val array = org.json.JSONArray(raw)
            List(array.length()) { index ->
                val o = array.getJSONObject(index)
                TrustedDeviceUi(
                    deviceId = o.optString("deviceId"),
                    displayName = o.optString("displayName"),
                    lastSeen = o.optString("lastSeen")
                )
            }
        } catch (_: Exception) {
            emptyList()
        }
    }

    fun syncNow(
        context: Context,
        secureStore: AndroidKeystoreStore,
        prefs: SharedPreferences,
        settings: SyncSettingsSnapshot,
        trustedDevices: List<TrustedDeviceUi>
    ): SyncNowResult {
        val (workspaceKey, deviceId) = ensureIdentity(secureStore)
        val localText = readClipboardText(context).orEmpty()

        if (settings.publicRelayEnabled && settings.publicRelayBucket.isNotBlank()) {
            return syncViaPublicRelay(
                context = context,
                prefs = prefs,
                workspaceKey = workspaceKey,
                deviceId = deviceId,
                localText = localText,
                settings = settings,
                trustedDevices = trustedDevices
            )
        }

        if (settings.webDavEnabled && settings.webDavBaseUrl.isNotBlank()) {
            val config = WebDavConfig(
                enabled = true,
                baseUrl = settings.webDavBaseUrl,
                username = settings.webDavUsername,
                password = settings.webDavPassword
            )
            val sentCount = if (localText.isNotBlank() && WebDavClient.uploadText(config, localText)) 1 else 0
            val remote = WebDavClient.downloadText(config)
            if (!remote.isNullOrBlank() && remote != localText) {
                writeClipboardText(context, remote)
                return SyncNowResult(sentCount, remote, "webdav", null)
            }
            return SyncNowResult(sentCount, null, null, null)
        }

        return SyncNowResult(0, null, null, "No sync channel configured")
    }

    private fun syncViaPublicRelay(
        context: Context,
        prefs: SharedPreferences,
        workspaceKey: String,
        deviceId: String,
        localText: String,
        settings: SyncSettingsSnapshot,
        trustedDevices: List<TrustedDeviceUi>
    ): SyncNowResult {
        val config = PublicRelayConfig(
            enabled = true,
            baseUrl = settings.publicRelayBaseUrl.ifBlank { "https://kvdb.io" },
            bucket = settings.publicRelayBucket
        )

        val peers = trustedDevices.filter { !it.deviceId.equals(deviceId, ignoreCase = true) }
        val uploadedByTarget = loadMap(prefs, RELAY_LAST_UPLOADED_KEY)
        var sentCount = 0
        var uploadError: String? = null

        if (localText.isNotBlank()) {
            if (peers.isEmpty()) {
                val legacySent = if (uploadedByTarget["__legacy__"] != localText) {
                    PublicRelayClient.uploadLegacyText(config, localText)
                } else {
                    true
                }
                if (legacySent) {
                    uploadedByTarget["__legacy__"] = localText
                    sentCount += 1
                } else {
                    uploadError = "Public relay legacy upload failed"
                }
            } else {
                for (peer in peers) {
                    if (uploadedByTarget[peer.deviceId] == localText) {
                        continue
                    }
                    val message = RelayMessage(
                        messageId = UUID.randomUUID().toString().replace("-", ""),
                        workspaceKey = workspaceKey,
                        fromDeviceId = deviceId,
                        toDeviceId = peer.deviceId,
                        text = localText,
                        sentAt = java.time.Instant.now().toString(),
                        client = "android"
                    )
                    val payload = JSONObject()
                        .put("MessageId", message.messageId)
                        .put("WorkspaceKey", message.workspaceKey)
                        .put("FromDeviceId", message.fromDeviceId)
                        .put("ToDeviceId", message.toDeviceId)
                        .put("Text", message.text)
                        .put("SentAt", message.sentAt)
                        .put("Client", message.client)
                        .toString()
                    val ok = PublicRelayClient.uploadRoutedMessage(config, peer.deviceId, deviceId, payload)
                    if (ok) {
                        uploadedByTarget[peer.deviceId] = localText
                        sentCount += 1
                    } else {
                        uploadError = "Public relay upload failed"
                    }
                }
            }
        }

        saveMap(prefs, RELAY_LAST_UPLOADED_KEY, uploadedByTarget)

        val lastAppliedBySender = loadMap(prefs, RELAY_LAST_APPLIED_KEY)
        var receivedText: String? = null
        var receivedFrom: String? = null

        for (peer in peers) {
            val payload = PublicRelayClient.downloadRoutedMessage(config, deviceId, peer.deviceId) ?: continue
            val message = tryParseRelayMessage(payload)
            if (message == null) {
                if (payload.isNotBlank() && payload != localText) {
                    writeClipboardText(context, payload)
                    receivedText = payload
                    receivedFrom = peer.deviceId
                    break
                }
                continue
            }

            if (!message.toDeviceId.equals(deviceId, ignoreCase = true)
                || message.fromDeviceId.equals(deviceId, ignoreCase = true)
                || (message.workspaceKey.isNotBlank() && message.workspaceKey != workspaceKey)
            ) {
                continue
            }

            if (lastAppliedBySender[message.fromDeviceId] == message.messageId) {
                continue
            }

            lastAppliedBySender[message.fromDeviceId] = message.messageId
            if (message.text.isNotBlank() && message.text != localText) {
                writeClipboardText(context, message.text)
                receivedText = message.text
                receivedFrom = message.fromDeviceId
                break
            }
        }

        if (receivedText == null) {
            val legacy = PublicRelayClient.downloadLegacyText(config)
            if (!legacy.isNullOrBlank() && legacy != localText) {
                writeClipboardText(context, legacy)
                receivedText = legacy
                receivedFrom = "legacy"
            }
        }

        saveMap(prefs, RELAY_LAST_APPLIED_KEY, lastAppliedBySender)
        return SyncNowResult(sentCount, receivedText, receivedFrom, uploadError)
    }

    private fun tryParseRelayMessage(raw: String): RelayMessage? {
        return try {
            val o = JSONObject(raw)
            val messageId = o.optString("MessageId")
            val fromDeviceId = o.optString("FromDeviceId")
            val toDeviceId = o.optString("ToDeviceId")
            if (messageId.isBlank() || fromDeviceId.isBlank() || toDeviceId.isBlank()) {
                null
            } else {
                RelayMessage(
                    messageId = messageId,
                    workspaceKey = o.optString("WorkspaceKey"),
                    fromDeviceId = fromDeviceId,
                    toDeviceId = toDeviceId,
                    text = o.optString("Text"),
                    sentAt = o.optString("SentAt"),
                    client = o.optString("Client")
                )
            }
        } catch (_: Exception) {
            null
        }
    }

    private fun loadMap(prefs: SharedPreferences, key: String): MutableMap<String, String> {
        val result = linkedMapOf<String, String>()
        val raw = prefs.getString(key, "{}") ?: "{}"
        return try {
            val o = JSONObject(raw)
            o.keys().forEach { k ->
                val value = o.optString(k)
                if (k.isNotBlank() && value.isNotBlank()) {
                    result[k] = value
                }
            }
            result
        } catch (_: Exception) {
            result
        }
    }

    private fun saveMap(prefs: SharedPreferences, key: String, map: Map<String, String>) {
        val o = JSONObject()
        map.forEach { (k, v) ->
            if (k.isNotBlank() && v.isNotBlank()) {
                o.put(k, v)
            }
        }
        prefs.edit().putString(key, o.toString()).apply()
    }
}
