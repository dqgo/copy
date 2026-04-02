package com.clipboardsync

import android.content.Intent
import android.content.Context
import android.os.Build
import android.os.Bundle
import android.app.NotificationChannel
import android.app.NotificationManager
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.core.app.NotificationCompat
import androidx.compose.animation.AnimatedContent
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.slideInHorizontally
import androidx.compose.animation.slideOutHorizontally
import androidx.compose.animation.togetherWith
import androidx.compose.foundation.background
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.AssistChip
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Checkbox
import androidx.compose.material3.Divider
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Switch
import androidx.compose.material3.Tab
import androidx.compose.material3.TabRow
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.drawscope.Stroke
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.semantics.contentDescription
import androidx.compose.ui.semantics.semantics
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.json.JSONArray
import org.json.JSONObject

object AppStrings {
    private val translations = mapOf(
        "zh-CN" to mapOf(
            "status" to "状态",
            "devices" to "可信设备",
            "pairing" to "配对请求",
            "history" to "历史记录",
            "settings" to "设置",
            "connection" to "连接状态",
            "sent" to "发出",
            "received" to "接收",
            "rejected" to "拒绝",
            "trustedCount" to "可信设备数",
            "pendingPairing" to "待批准",
            "approve" to "批准",
            "reject" to "拒绝",
            "revoke" to "撤销",
            "language" to "语言",
            "darkMode" to "深色模式",
            "syncMode" to "同步模式",
            "space" to "工作空间",
            "pairingPolicy" to "配对策略",
            "webdev" to "WebDev 同步",
            "webdevUrl" to "WebDev 地址",
            "webdevUser" to "WebDev 用户名",
            "webdevPassword" to "WebDev 密码",
            "testWebdev" to "测试 WebDev 连接",
            "webdevOk" to "WebDev 连接成功",
            "webdevFail" to "WebDev 连接失败",
            "publicRelay" to "启用免费公共服务器",
            "publicRelayUrl" to "公共服务器地址",
            "publicRelayBucket" to "共享桶 ID",
            "testPublicRelay" to "测试公共服务器",
            "publicRelayFail" to "公共服务器连接失败",
            "server" to "本地服务模式",
            "manualSync" to "手动同步",
            "sendHtml" to "发送 HTML",
            "sendImage" to "发送图片",
            "sendFile" to "发送文件",
            "deviceId" to "设备唯一 ID",
            "deviceName" to "设备名称",
            "remoteDeviceId" to "对端设备 ID",
            "remoteDeviceName" to "对端设备名",
            "pairByDeviceId" to "通过设备ID配对",
            "createInvite" to "生成邀请",
            "joinByInvite" to "通过邀请码配对",
            "invitePlaceholder" to "粘贴对端邀请码",
            "copy" to "复制",
            "lastError" to "最近错误",
            "noDevices" to "暂无设备",
            "noPairingRequests" to "暂无配对请求",
            "noHistory" to "暂无历史记录",
            "confirm" to "确认",
            "cancel" to "取消",
            "searchDevices" to "搜索设备",
            "searchHistory" to "搜索历史",
            "searchPairing" to "搜索配对请求",
            "clearFilter" to "清空筛选",
            "emptyDevicesHint" to "当前没有可信设备，可前往配对页添加设备",
            "emptyPairingHint" to "当前没有配对请求，等待新设备发起即可",
            "emptyHistoryHint" to "还没有同步记录，执行一次手动同步即可生成历史"
        ),
        "en-US" to mapOf(
            "status" to "Status",
            "devices" to "Trusted Devices",
            "pairing" to "Pairing Requests",
            "history" to "History",
            "settings" to "Settings",
            "connection" to "Connection",
            "sent" to "Sent",
            "received" to "Received",
            "rejected" to "Rejected",
            "trustedCount" to "Trusted Devices",
            "pendingPairing" to "Pending",
            "approve" to "Approve",
            "reject" to "Reject",
            "revoke" to "Revoke",
            "language" to "Language",
            "darkMode" to "Dark Mode",
            "syncMode" to "Sync Mode",
            "space" to "Space",
            "pairingPolicy" to "Pairing Policy",
            "webdev" to "WebDev Sync",
            "webdevUrl" to "WebDev URL",
            "webdevUser" to "WebDev Username",
            "webdevPassword" to "WebDev Password",
            "testWebdev" to "Test WebDev Connection",
            "webdevOk" to "WebDev connection succeeded",
            "webdevFail" to "WebDev connection failed",
            "publicRelay" to "Enable Free Public Relay",
            "publicRelayUrl" to "Public Relay URL",
            "publicRelayBucket" to "Shared Bucket ID",
            "testPublicRelay" to "Test Public Relay",
            "publicRelayFail" to "Public relay connection failed",
            "server" to "Local Server Mode",
            "manualSync" to "Manual Sync",
            "sendHtml" to "Send HTML",
            "sendImage" to "Send Image",
            "sendFile" to "Send File",
            "deviceId" to "Unique Device ID",
            "deviceName" to "Device Name",
            "remoteDeviceId" to "Remote Device ID",
            "remoteDeviceName" to "Remote Device Name",
            "pairByDeviceId" to "Pair By Device ID",
            "createInvite" to "Create Invite",
            "joinByInvite" to "Pair By Invite",
            "invitePlaceholder" to "Paste remote invite code",
            "copy" to "Copy",
            "lastError" to "Last Error",
            "noDevices" to "No devices",
            "noPairingRequests" to "No pairing requests",
            "noHistory" to "No history",
            "confirm" to "Confirm",
            "cancel" to "Cancel",
            "searchDevices" to "Search devices",
            "searchHistory" to "Search history",
            "searchPairing" to "Search pairing requests",
            "clearFilter" to "Clear filter",
            "emptyDevicesHint" to "No trusted devices yet. Add one from pairing tab.",
            "emptyPairingHint" to "No pairing requests for now.",
            "emptyHistoryHint" to "No sync history yet. Run manual sync to create records."
        )
    )

    fun get(language: String, key: String): String {
        return translations[language]?.get(key) ?: translations["en-US"]?.get(key) ?: key
    }
}

data class TrustedDeviceUi(val deviceId: String, val displayName: String, val lastSeen: String)
data class HistoryUi(val direction: String, val contentType: String, val preview: String, val at: String)
data class SettingsUi(
    val language: String,
    val darkMode: Boolean,
    val syncMode: String,
    val spaceId: String,
    val webDevEnabled: Boolean,
    val webDevBaseUrl: String,
    val webDevUsername: String,
    val webDevPassword: String,
    val publicRelayEnabled: Boolean,
    val publicRelayBaseUrl: String,
    val publicRelayBucket: String,
    val localServerEnabled: Boolean,
    val pairingPolicy: String
)
data class PairingRequestUi(
    val requestId: String,
    val deviceId: String,
    val displayName: String,
    val platform: String,
    val at: String
)

data class DashboardState(
    val workspaceKey: String,
    val deviceId: String,
    val status: StatusViewModel,
    val devices: List<TrustedDeviceUi>,
    val history: List<HistoryUi>,
    val settings: SettingsUi,
    val pairingRequests: List<PairingRequestUi>,
    val errorMessage: String? = null,
    val showConfirmDialog: Boolean = false,
    val confirmDialogTitle: String = "",
    val confirmDialogMessage: String = "",
    val confirmDialogOnConfirm: (() -> Unit)? = null
)

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        val secureStore = AndroidKeystoreStore(this)
        SyncCoordinator.ensureIdentity(secureStore)
        val quickServiceIntent = Intent(this, SyncQuickActionService::class.java)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            startForegroundService(quickServiceIntent)
        } else {
            startService(quickServiceIntent)
        }
        setContent { ClipboardSyncAndroidApp() }
    }

    override fun onStart() {
        super.onStart()
        val manager = getSystemService(NotificationManager::class.java)
        manager.cancel(2002)
    }

    override fun onStop() {
        super.onStop()
        val channelId = "clipboardsync.lifecycle"
        val manager = getSystemService(NotificationManager::class.java)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(channelId, "Clipboard Sync Lifecycle", NotificationManager.IMPORTANCE_LOW)
            manager.createNotificationChannel(channel)
        }
        val notification = NotificationCompat.Builder(this, channelId)
            .setSmallIcon(android.R.drawable.stat_notify_sync)
            .setContentTitle("Clipboard Sync")
            .setContentText("App in background, foreground service still active")
            .setAutoCancel(false)
            .build()
        manager.notify(2002, notification)
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun ClipboardSyncAndroidApp() {
    val context = LocalContext.current
    val prefs = remember { context.getSharedPreferences("clipboardsync_settings", Context.MODE_PRIVATE) }
    val secureStore = remember { AndroidKeystoreStore(context) }
    val identity = remember { SyncCoordinator.ensureIdentity(secureStore) }
    val workspaceKey = identity.first
    val deviceId = identity.second
    val initialDevices = remember { decodeTrustedDevices(prefs.getString("trusted_devices_json", null)) }
    val initialHistory = remember { decodeHistoryItems(prefs.getString("history_items_json", null)) }
    val initialPairing = remember { decodePairingRequests(prefs.getString("pairing_requests_json", null)) }
    val systemDarkTheme = isSystemInDarkTheme()
    var selectedTab by remember { mutableStateOf(0) }
    var deviceQuery by remember { mutableStateOf("") }
    var historyQuery by remember { mutableStateOf("") }
    var pairingQuery by remember { mutableStateOf("") }

    var state by remember {
        mutableStateOf(
            DashboardState(
                workspaceKey = workspaceKey,
                deviceId = deviceId,
                status = StatusViewModel(
                    connectionState = SyncConnectionState.DISCONNECTED,
                    syncedOutCount = 0,
                    syncedInCount = 0,
                    rejectedEventCount = 0,
                    trustedDeviceCount = initialDevices.size,
                    pendingPairingCount = initialPairing.size,
                    lastErrorMessage = null
                ),
                devices = initialDevices,
                history = initialHistory,
                settings = SettingsUi(
                    language = prefs.getString("language", "zh-CN") ?: "zh-CN",
                    darkMode = prefs.getBoolean("dark_mode", systemDarkTheme),
                    syncMode = prefs.getString("sync_mode", "manual") ?: "manual",
                    spaceId = prefs.getString("space_id", "default") ?: "default",
                    webDevEnabled = prefs.getBoolean("webdav_enabled", false),
                    webDevBaseUrl = prefs.getString("webdav_base_url", "") ?: "",
                    webDevUsername = prefs.getString("webdav_username", "") ?: "",
                    webDevPassword = prefs.getString("webdav_password", "") ?: "",
                    publicRelayEnabled = prefs.getBoolean("public_relay_enabled", true),
                    publicRelayBaseUrl = prefs.getString("public_relay_base_url", "https://kvdb.io") ?: "https://kvdb.io",
                    publicRelayBucket = prefs.getString("public_relay_bucket", workspaceKey) ?: workspaceKey,
                    localServerEnabled = prefs.getBoolean("local_server_enabled", false),
                    pairingPolicy = prefs.getString("pairing_policy", "manual-approve") ?: "manual-approve"
                ),
                pairingRequests = initialPairing
            )
        )
    }
    val coroutineScope = rememberCoroutineScope()
    var syncInProgress by remember { mutableStateOf(false) }
    var generatedInviteCode by remember { mutableStateOf("") }

    fun runSync(trigger: String) {
        if (syncInProgress) {
            return
        }
        if (prefs.getBoolean("sync_paused", false)) {
            state = state.copy(
                status = state.status.copy(lastErrorMessage = "sync paused"),
                errorMessage = "Sync is paused. Resume from quick action first."
            )
            return
        }

        val outgoingPreview = SyncCoordinator.readClipboardText(context).orEmpty().take(64)
        coroutineScope.launch {
            syncInProgress = true
            try {
                val snapshot = state
                val result = withContext(Dispatchers.IO) {
                    SyncCoordinator.syncNow(
                        context = context,
                        secureStore = secureStore,
                        prefs = prefs,
                        settings = SyncSettingsSnapshot(
                            webDavEnabled = snapshot.settings.webDevEnabled,
                            webDavBaseUrl = snapshot.settings.webDevBaseUrl,
                            webDavUsername = snapshot.settings.webDevUsername,
                            webDavPassword = snapshot.settings.webDevPassword,
                            publicRelayEnabled = snapshot.settings.publicRelayEnabled,
                            publicRelayBaseUrl = snapshot.settings.publicRelayBaseUrl,
                            publicRelayBucket = snapshot.settings.publicRelayBucket
                        ),
                        trustedDevices = snapshot.devices
                    )
                }

                val current = state
                val mergedHistory = buildList {
                    if (result.receivedText != null) {
                        add(HistoryUi("in", "text/plain", result.receivedText.take(128), "now"))
                    }
                    if (result.sentCount > 0 && outgoingPreview.isNotBlank()) {
                        add(HistoryUi("out", "text/plain", outgoingPreview, "now"))
                    }
                    if (trigger == "auto" && result.sentCount == 0 && result.receivedText == null) {
                        addAll(current.history.take(299))
                    } else {
                        addAll(current.history)
                    }
                }.take(300)

                state = current.copy(
                    status = current.status.copy(
                        connectionState = if (result.errorMessage == null) SyncConnectionState.CONNECTED else SyncConnectionState.DEGRADED,
                        syncedOutCount = current.status.syncedOutCount + result.sentCount,
                        syncedInCount = current.status.syncedInCount + if (result.receivedText != null) 1 else 0,
                        lastErrorMessage = result.errorMessage ?: "None"
                    ),
                    history = mergedHistory,
                    errorMessage = result.errorMessage
                )
            } finally {
                syncInProgress = false
            }
        }
    }

    val filteredDevices = state.devices.filter {
        val q = deviceQuery.trim().lowercase()
        q.isEmpty() || it.displayName.lowercase().contains(q) || it.deviceId.lowercase().contains(q)
    }
    val filteredHistory = state.history.filter {
        val q = historyQuery.trim().lowercase()
        q.isEmpty() || it.preview.lowercase().contains(q) || it.contentType.lowercase().contains(q)
    }
    val filteredPairing = state.pairingRequests.filter {
        val q = pairingQuery.trim().lowercase()
        q.isEmpty()
            || it.displayName.lowercase().contains(q)
            || it.platform.lowercase().contains(q)
            || it.deviceId.lowercase().contains(q)
    }

    LaunchedEffect(state.devices, state.history, state.pairingRequests) {
        prefs.edit()
            .putString("trusted_devices_json", encodeTrustedDevices(state.devices))
            .putString("history_items_json", encodeHistoryItems(state.history))
            .putString("pairing_requests_json", encodePairingRequests(state.pairingRequests))
            .apply()
    }

    LaunchedEffect(
        state.settings.syncMode,
        state.settings.webDevEnabled,
        state.settings.webDevBaseUrl,
        state.settings.publicRelayEnabled,
        state.settings.publicRelayBaseUrl,
        state.settings.publicRelayBucket,
        state.devices,
        state.workspaceKey,
        state.deviceId
    ) {
        if (state.settings.syncMode != "auto") {
            return@LaunchedEffect
        }
        while (isActive && state.settings.syncMode == "auto") {
            if (!syncInProgress && !prefs.getBoolean("sync_paused", false)) {
                runSync("auto")
            }
            delay(2500)
        }
    }

    MaterialTheme {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(Brush.verticalGradient(colors = listOf(Color(0xFF0A355A), Color(0xFF0C5D56), Color(0xFF0B2233))))
        ) {
            Scaffold(
                topBar = { TopAppBar(title = { Text("Clipboard Sync") }) },
                containerColor = Color.Transparent
            ) { padding ->
                Column(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(padding)
                        .padding(14.dp)
                ) {
                    state.errorMessage?.let { msg ->
                        Card(
                            modifier = Modifier.fillMaxWidth().padding(bottom = 12.dp),
                            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.errorContainer)
                        ) {
                            Row(
                                modifier = Modifier.fillMaxWidth().padding(12.dp),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(msg, color = MaterialTheme.colorScheme.onErrorContainer)
                                Button(
                                    modifier = Modifier.semantics { contentDescription = "Dismiss error message" },
                                    onClick = { state = state.copy(errorMessage = null) }
                                ) { Text("×") }
                            }
                        }
                    }

                    TabRow(selectedTabIndex = selectedTab) {
                        Tab(selected = selectedTab == 0, onClick = { selectedTab = 0 }, text = { Text(AppStrings.get(state.settings.language, "status")) })
                        Tab(selected = selectedTab == 1, onClick = { selectedTab = 1 }, text = { Text(AppStrings.get(state.settings.language, "devices")) })
                        Tab(selected = selectedTab == 2, onClick = { selectedTab = 2 }, text = { Text(AppStrings.get(state.settings.language, "pairing")) })
                        Tab(selected = selectedTab == 3, onClick = { selectedTab = 3 }, text = { Text(AppStrings.get(state.settings.language, "history")) })
                        Tab(selected = selectedTab == 4, onClick = { selectedTab = 4 }, text = { Text(AppStrings.get(state.settings.language, "settings")) })
                    }
                    Spacer(modifier = Modifier.height(12.dp))

                    AnimatedContent(
                        targetState = selectedTab,
                        transitionSpec = {
                            (slideInHorizontally(initialOffsetX = { 300 }) + fadeIn()).togetherWith(
                                slideOutHorizontally(targetOffsetX = { -300 }) + fadeOut()
                            )
                        }
                    ) { tab ->
                        when (tab) {
                            0 -> StatusTab(
                                status = state.status,
                                language = state.settings.language,
                                onManualSync = { runSync("manual") },
                                onSendHtml = {
                                    state = state.copy(
                                        status = state.status.copy(syncedOutCount = state.status.syncedOutCount + 1),
                                        history = listOf(HistoryUi("out", "text/html", "<b>Clipboard Sync</b>", "now")) + state.history
                                    )
                                },
                                onSendImage = {
                                    state = state.copy(
                                        status = state.status.copy(syncedOutCount = state.status.syncedOutCount + 1),
                                        history = listOf(HistoryUi("out", "image/png", "screenshot.png (ref)", "now")) + state.history
                                    )
                                },
                                onSendFileRef = {
                                    state = state.copy(
                                        status = state.status.copy(syncedOutCount = state.status.syncedOutCount + 1),
                                        history = listOf(HistoryUi("out", "application/x-clipboard-file-ref", "local-file-ref", "now")) + state.history
                                    )
                                }
                            )

                            1 -> DevicesTab(
                                devices = filteredDevices,
                                language = state.settings.language,
                                query = deviceQuery,
                                onQueryChange = { deviceQuery = it },
                                onRevoke = { deviceId ->
                                    val deviceName = state.devices.find { it.deviceId == deviceId }?.displayName ?: deviceId
                                    state = state.copy(
                                        showConfirmDialog = true,
                                        confirmDialogTitle = AppStrings.get(state.settings.language, "revoke"),
                                        confirmDialogMessage = "Revoke $deviceName?",
                                        confirmDialogOnConfirm = {
                                            state = state.copy(
                                                devices = state.devices.filterNot { it.deviceId == deviceId },
                                                status = state.status.copy(
                                                    rejectedEventCount = state.status.rejectedEventCount + 1,
                                                    trustedDeviceCount = (state.status.trustedDeviceCount - 1).coerceAtLeast(0),
                                                    lastErrorMessage = "Revoked device: $deviceId"
                                                )
                                            )
                                        }
                                    )
                                }
                            )

                            2 -> PairingTab(
                                requests = filteredPairing,
                                language = state.settings.language,
                                query = pairingQuery,
                                onQueryChange = { pairingQuery = it },
                                localDeviceId = state.deviceId,
                                inviteCode = generatedInviteCode,
                                onCopyLocalDeviceId = {
                                    SyncCoordinator.writeClipboardText(context, state.deviceId)
                                    state = state.copy(
                                        history = listOf(HistoryUi("event", "pairing", "device id copied", "now")) + state.history,
                                        errorMessage = null
                                    )
                                },
                                onCreateInvite = { deviceName ->
                                    val safeName = deviceName.ifBlank { Build.MODEL ?: "Android" }
                                    generatedInviteCode = SyncCoordinator.createInviteCode(
                                        workspaceKey = state.workspaceKey,
                                        deviceId = state.deviceId,
                                        deviceName = safeName,
                                        platform = "android"
                                    )
                                    state = state.copy(
                                        history = listOf(HistoryUi("event", "pairing", "invite created", "now")) + state.history,
                                        errorMessage = null
                                    )
                                },
                                onCopyInvite = {
                                    if (generatedInviteCode.isNotBlank()) {
                                        SyncCoordinator.writeClipboardText(context, generatedInviteCode)
                                        state = state.copy(
                                            history = listOf(HistoryUi("event", "pairing", "invite copied", "now")) + state.history,
                                            errorMessage = null
                                        )
                                    }
                                },
                                onJoinInvite = { inviteCodeInput ->
                                    val payload = SyncCoordinator.tryParseInviteCode(inviteCodeInput)
                                    if (payload == null) {
                                        state = state.copy(
                                            status = state.status.copy(lastErrorMessage = "invite parse failed"),
                                            errorMessage = "Invite code format error"
                                        )
                                        return@PairingTab
                                    }

                                    if (!SyncCoordinator.isValidDeviceId(payload.deviceId)) {
                                        state = state.copy(
                                            status = state.status.copy(lastErrorMessage = "Invalid invite device ID"),
                                            errorMessage = "Invalid invite device ID"
                                        )
                                        return@PairingTab
                                    }

                                    if (payload.deviceId.equals(state.deviceId, ignoreCase = true)) {
                                        state = state.copy(
                                            status = state.status.copy(lastErrorMessage = "Cannot pair with current device"),
                                            errorMessage = "Cannot pair with current device"
                                        )
                                        return@PairingTab
                                    }

                                    var next = state
                                    if (payload.workspaceKey.isNotBlank() && payload.workspaceKey != state.workspaceKey) {
                                        secureStore.set("workspace_key", payload.workspaceKey)
                                        val nextBucket = if (
                                            state.settings.publicRelayBucket.isBlank()
                                            || state.settings.publicRelayBucket == state.workspaceKey
                                        ) payload.workspaceKey else state.settings.publicRelayBucket
                                        prefs.edit().putString("public_relay_bucket", nextBucket).apply()
                                        next = next.copy(
                                            workspaceKey = payload.workspaceKey,
                                            settings = next.settings.copy(publicRelayBucket = nextBucket)
                                        )
                                    }

                                    val autoApprove = next.settings.pairingPolicy == "auto-approve-invite"
                                    if (autoApprove) {
                                        val exists = next.devices.any { it.deviceId.equals(payload.deviceId, ignoreCase = true) }
                                        val updatedDevices = if (exists) {
                                            next.devices.map {
                                                if (it.deviceId.equals(payload.deviceId, ignoreCase = true)) {
                                                    it.copy(displayName = payload.deviceName, lastSeen = "now")
                                                } else {
                                                    it
                                                }
                                            }
                                        } else {
                                            listOf(TrustedDeviceUi(payload.deviceId, payload.deviceName, "now")) + next.devices
                                        }
                                        next = next.copy(
                                            devices = updatedDevices,
                                            status = next.status.copy(trustedDeviceCount = updatedDevices.size),
                                            history = listOf(HistoryUi("event", "pairing", "approved ${payload.deviceName}", "now")) + next.history
                                        )
                                    } else {
                                        if (next.pairingRequests.any {
                                                it.deviceId.equals(payload.deviceId, ignoreCase = true)
                                            }) {
                                            state = state.copy(
                                                status = state.status.copy(lastErrorMessage = "Pairing request already exists"),
                                                errorMessage = "Pairing request already exists"
                                            )
                                            return@PairingTab
                                        }
                                        val updatedRequests = listOf(
                                            PairingRequestUi(
                                                "req-${java.util.UUID.randomUUID().toString().replace("-", "")}",
                                                payload.deviceId,
                                                payload.deviceName,
                                                payload.platform,
                                                "now"
                                            )
                                        ) + next.pairingRequests
                                        next = next.copy(
                                            pairingRequests = updatedRequests,
                                            status = next.status.copy(pendingPairingCount = updatedRequests.size),
                                            history = listOf(HistoryUi("event", "pairing", "request from ${payload.deviceName}", "now")) + next.history
                                        )
                                    }

                                    state = next.copy(
                                        status = next.status.copy(lastErrorMessage = "None"),
                                        errorMessage = null
                                    )
                                },
                                onPairByDeviceId = { remoteId, remoteName ->
                                    val safeRemoteId = remoteId.trim()
                                    if (safeRemoteId.isBlank()) {
                                        state = state.copy(
                                            status = state.status.copy(lastErrorMessage = "Remote device ID is empty"),
                                            errorMessage = "Remote device ID is empty"
                                        )
                                        return@PairingTab
                                    }
                                    if (!SyncCoordinator.isValidDeviceId(safeRemoteId)) {
                                        state = state.copy(
                                            status = state.status.copy(lastErrorMessage = "Invalid remote device ID format"),
                                            errorMessage = "Invalid remote device ID format"
                                        )
                                        return@PairingTab
                                    }
                                    if (safeRemoteId.equals(state.deviceId, ignoreCase = true)) {
                                        state = state.copy(
                                            status = state.status.copy(lastErrorMessage = "Cannot pair with current device"),
                                            errorMessage = "Cannot pair with current device"
                                        )
                                        return@PairingTab
                                    }

                                    val safeName = remoteName.trim().ifBlank { safeRemoteId }
                                    val exists = state.devices.any { it.deviceId.equals(safeRemoteId, ignoreCase = true) }
                                    val updatedDevices = if (exists) {
                                        state.devices.map {
                                            if (it.deviceId.equals(safeRemoteId, ignoreCase = true)) {
                                                it.copy(displayName = safeName, lastSeen = "now")
                                            } else {
                                                it
                                            }
                                        }
                                    } else {
                                        listOf(TrustedDeviceUi(safeRemoteId, safeName, "now")) + state.devices
                                    }

                                    state = state.copy(
                                        devices = updatedDevices,
                                        status = state.status.copy(
                                            trustedDeviceCount = updatedDevices.size,
                                            lastErrorMessage = "None"
                                        ),
                                        history = listOf(HistoryUi("event", "pairing", "direct-id $safeRemoteId", "now")) + state.history,
                                        errorMessage = null
                                    )
                                },
                                onApprove = { requestId ->
                                    val request = state.pairingRequests.find { it.requestId == requestId }
                                    val updatedDevices = if (request == null) {
                                        state.devices
                                    } else {
                                        val exists = state.devices.any { it.deviceId.equals(request.deviceId, ignoreCase = true) }
                                        if (exists) {
                                            state.devices.map {
                                                if (it.deviceId.equals(request.deviceId, ignoreCase = true)) {
                                                    it.copy(displayName = request.displayName, lastSeen = "now")
                                                } else {
                                                    it
                                                }
                                            }
                                        } else {
                                            listOf(TrustedDeviceUi(request.deviceId, request.displayName, "now")) + state.devices
                                        }
                                    }
                                    state = state.copy(
                                        devices = updatedDevices,
                                        pairingRequests = state.pairingRequests.filterNot { it.requestId == requestId },
                                        status = state.status.copy(
                                            trustedDeviceCount = updatedDevices.size,
                                            pendingPairingCount = (state.status.pendingPairingCount - 1).coerceAtLeast(0)
                                        ),
                                        history = if (request == null) state.history else listOf(
                                            HistoryUi("event", "pairing", "approved ${request.displayName}", "now")
                                        ) + state.history
                                    )
                                },
                                onReject = { requestId ->
                                    val request = state.pairingRequests.find { it.requestId == requestId }
                                    state = state.copy(
                                        showConfirmDialog = true,
                                        confirmDialogTitle = AppStrings.get(state.settings.language, "reject"),
                                        confirmDialogMessage = "Reject ${request?.displayName ?: requestId}?",
                                        confirmDialogOnConfirm = {
                                            state = state.copy(
                                                pairingRequests = state.pairingRequests.filterNot { it.requestId == requestId },
                                                status = state.status.copy(
                                                    pendingPairingCount = (state.status.pendingPairingCount - 1).coerceAtLeast(0)
                                                )
                                            )
                                        }
                                    )
                                }
                            )

                            3 -> HistoryTab(
                                history = filteredHistory,
                                language = state.settings.language,
                                query = historyQuery,
                                onQueryChange = { historyQuery = it }
                            )

                            else -> SettingsTab(
                                settings = state.settings,
                                language = state.settings.language,
                                onChange = { updated ->
                                    prefs.edit()
                                        .putString("language", updated.language)
                                        .putBoolean("dark_mode", updated.darkMode)
                                        .putString("sync_mode", updated.syncMode)
                                        .putString("space_id", updated.spaceId)
                                        .putBoolean("webdav_enabled", updated.webDevEnabled)
                                        .putString("webdav_base_url", updated.webDevBaseUrl)
                                        .putString("webdav_username", updated.webDevUsername)
                                        .putString("webdav_password", updated.webDevPassword)
                                        .putBoolean("public_relay_enabled", updated.publicRelayEnabled)
                                        .putString("public_relay_base_url", updated.publicRelayBaseUrl)
                                        .putString("public_relay_bucket", updated.publicRelayBucket)
                                        .putBoolean("local_server_enabled", updated.localServerEnabled)
                                        .putString("pairing_policy", updated.pairingPolicy)
                                        .apply()
                                    state = state.copy(settings = updated)
                                },
                                onTestWebDav = {
                                    val cfg = WebDavConfig(
                                        enabled = state.settings.webDevEnabled,
                                        baseUrl = state.settings.webDevBaseUrl,
                                        username = state.settings.webDevUsername,
                                        password = state.settings.webDevPassword
                                    )
                                    coroutineScope.launch(Dispatchers.IO) {
                                        val ok = WebDavClient.test(cfg)
                                        withContext(Dispatchers.Main) {
                                            state = state.copy(
                                                status = state.status.copy(
                                                    lastErrorMessage = if (ok) "None" else AppStrings.get(state.settings.language, "webdevFail")
                                                ),
                                                errorMessage = if (ok) null else AppStrings.get(state.settings.language, "webdevFail")
                                            )
                                        }
                                    }
                                },
                                onTestPublicRelay = {
                                    val cfg = PublicRelayConfig(
                                        enabled = state.settings.publicRelayEnabled,
                                        baseUrl = state.settings.publicRelayBaseUrl,
                                        bucket = state.settings.publicRelayBucket
                                    )
                                    coroutineScope.launch(Dispatchers.IO) {
                                        val ok = PublicRelayClient.test(cfg)
                                        withContext(Dispatchers.Main) {
                                            state = state.copy(
                                                status = state.status.copy(
                                                    lastErrorMessage = if (ok) "None" else AppStrings.get(state.settings.language, "publicRelayFail")
                                                ),
                                                errorMessage = if (ok) null else AppStrings.get(state.settings.language, "publicRelayFail")
                                            )
                                        }
                                    }
                                }
                            )
                        }
                    }
                }
            }

            if (state.showConfirmDialog) {
                AlertDialog(
                    onDismissRequest = { state = state.copy(showConfirmDialog = false) },
                    title = { Text(state.confirmDialogTitle) },
                    text = { Text(state.confirmDialogMessage) },
                    confirmButton = {
                        Button(onClick = {
                            state.confirmDialogOnConfirm?.invoke()
                            state = state.copy(showConfirmDialog = false)
                        }) { Text(AppStrings.get(state.settings.language, "confirm")) }
                    },
                    dismissButton = {
                        Button(onClick = { state = state.copy(showConfirmDialog = false) }) {
                            Text(AppStrings.get(state.settings.language, "cancel"))
                        }
                    }
                )
            }
        }
    }
}

@Composable
private fun StatusTab(
    status: StatusViewModel,
    language: String,
    onManualSync: () -> Unit,
    onSendHtml: () -> Unit,
    onSendImage: () -> Unit,
    onSendFileRef: () -> Unit
) {
    Card(modifier = Modifier.fillMaxWidth(), colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface.copy(alpha = 0.9f))) {
        Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
            PlatformHeroGraphic()
            StatusRow(AppStrings.get(language, "connection"), status.connectionState.name)
            StatusRow(AppStrings.get(language, "sent"), status.syncedOutCount.toString())
            StatusRow(AppStrings.get(language, "received"), status.syncedInCount.toString())
            StatusRow(AppStrings.get(language, "rejected"), status.rejectedEventCount.toString())
            StatusRow(AppStrings.get(language, "trustedCount"), status.trustedDeviceCount.toString())
            StatusRow(AppStrings.get(language, "pendingPairing"), status.pendingPairingCount.toString())
            StatusRow(AppStrings.get(language, "lastError"), status.lastErrorMessage ?: "None")
            Divider()
            Button(modifier = Modifier.semantics { contentDescription = "Manual sync action" }, onClick = onManualSync) {
                Text(AppStrings.get(language, "manualSync"))
            }
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                Button(modifier = Modifier.semantics { contentDescription = "Send HTML content" }, onClick = onSendHtml) {
                    Text(AppStrings.get(language, "sendHtml"))
                }
                Button(modifier = Modifier.semantics { contentDescription = "Send image reference" }, onClick = onSendImage) {
                    Text(AppStrings.get(language, "sendImage"))
                }
                Button(modifier = Modifier.semantics { contentDescription = "Send file reference" }, onClick = onSendFileRef) {
                    Text(AppStrings.get(language, "sendFile"))
                }
            }
        }
    }
}

@Composable
private fun PlatformHeroGraphic() {
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(containerColor = Color(0xFF0B2233).copy(alpha = 0.92f))
    ) {
        Canvas(
            modifier = Modifier
                .fillMaxWidth()
                .height(112.dp)
        ) {
            drawRoundRect(
                color = Color(0xFF1BB5A7),
                topLeft = Offset(16f, 16f),
                size = Size(size.width - 32f, size.height - 32f),
                cornerRadius = androidx.compose.ui.geometry.CornerRadius(24f, 24f)
            )
            drawRoundRect(
                color = Color(0xFF0A355A),
                topLeft = Offset(36f, 26f),
                size = Size(size.width * 0.35f, size.height * 0.56f),
                cornerRadius = androidx.compose.ui.geometry.CornerRadius(20f, 20f)
            )
            drawRoundRect(
                color = Color.White,
                topLeft = Offset(size.width * 0.56f, 34f),
                size = Size(size.width * 0.28f, size.height * 0.42f),
                cornerRadius = androidx.compose.ui.geometry.CornerRadius(14f, 14f),
                style = Stroke(width = 6f)
            )
            drawCircle(color = Color(0xFFFFC857), radius = 11f, center = Offset(size.width * 0.82f, size.height * 0.68f))
            drawCircle(color = Color(0xFFFF6B6B), radius = 8f, center = Offset(size.width * 0.72f, size.height * 0.72f))
        }
    }
}

@Composable
private fun DevicesTab(
    devices: List<TrustedDeviceUi>,
    language: String,
    query: String,
    onQueryChange: (String) -> Unit,
    onRevoke: (String) -> Unit
) {
    LazyColumn(verticalArrangement = Arrangement.spacedBy(10.dp)) {
        item {
            OutlinedTextField(
                value = query,
                onValueChange = onQueryChange,
                label = { Text(AppStrings.get(language, "searchDevices")) },
                modifier = Modifier.fillMaxWidth().semantics { contentDescription = "Device search input" }
            )
        }
        if (devices.isEmpty()) {
            item {
                EmptyStateCard(
                    title = AppStrings.get(language, "noDevices"),
                    message = AppStrings.get(language, "emptyDevicesHint"),
                    actionLabel = if (query.isNotBlank()) AppStrings.get(language, "clearFilter") else null,
                    onAction = if (query.isNotBlank()) ({ onQueryChange("") }) else null
                )
            }
        }
        items(devices, key = { it.deviceId }) { device ->
            Card(modifier = Modifier.fillMaxWidth().semantics { contentDescription = "Device ${device.displayName}" }) {
                Column(modifier = Modifier.padding(14.dp)) {
                    Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                        Text(device.displayName, fontWeight = FontWeight.Bold)
                        AssistChip(onClick = {}, label = { Text(AppStrings.get(language, "trustedCount")) })
                    }
                    Text("ID: ${device.deviceId}")
                    Text("${AppStrings.get(language, "lastError")}: ${device.lastSeen}")
                    Button(
                        modifier = Modifier.semantics { contentDescription = "Revoke ${device.displayName}" },
                        onClick = { onRevoke(device.deviceId) }
                    ) { Text(AppStrings.get(language, "revoke")) }
                }
            }
        }
    }
}

@Composable
private fun PairingTab(
    requests: List<PairingRequestUi>,
    language: String,
    query: String,
    onQueryChange: (String) -> Unit,
    localDeviceId: String,
    inviteCode: String,
    onCopyLocalDeviceId: () -> Unit,
    onCreateInvite: (String) -> Unit,
    onCopyInvite: () -> Unit,
    onJoinInvite: (String) -> Unit,
    onPairByDeviceId: (String, String) -> Unit,
    onApprove: (String) -> Unit,
    onReject: (String) -> Unit
) {
    var inviteName by remember { mutableStateOf(Build.MODEL ?: "Android") }
    var inviteCodeInput by remember { mutableStateOf("") }
    var remoteDeviceIdInput by remember { mutableStateOf("") }
    var remoteDeviceNameInput by remember { mutableStateOf("") }

    LazyColumn(verticalArrangement = Arrangement.spacedBy(10.dp)) {
        item {
            Card(modifier = Modifier.fillMaxWidth()) {
                Column(modifier = Modifier.padding(12.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    Text("${AppStrings.get(language, "deviceId")}: $localDeviceId", fontWeight = FontWeight.SemiBold)
                    Button(onClick = onCopyLocalDeviceId) { Text(AppStrings.get(language, "copy")) }
                }
            }
        }
        item {
            Card(modifier = Modifier.fillMaxWidth()) {
                Column(modifier = Modifier.padding(12.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    OutlinedTextField(
                        value = inviteName,
                        onValueChange = { inviteName = it },
                        label = { Text(AppStrings.get(language, "deviceName")) },
                        modifier = Modifier.fillMaxWidth()
                    )
                    Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                        Button(onClick = { onCreateInvite(inviteName) }) { Text(AppStrings.get(language, "createInvite")) }
                        Button(onClick = onCopyInvite, enabled = inviteCode.isNotBlank()) { Text(AppStrings.get(language, "copy")) }
                    }
                    if (inviteCode.isNotBlank()) {
                        Text(inviteCode)
                    }
                    OutlinedTextField(
                        value = inviteCodeInput,
                        onValueChange = { inviteCodeInput = it },
                        label = { Text(AppStrings.get(language, "invitePlaceholder")) },
                        modifier = Modifier.fillMaxWidth()
                    )
                    Button(onClick = { onJoinInvite(inviteCodeInput) }) { Text(AppStrings.get(language, "joinByInvite")) }
                }
            }
        }
        item {
            Card(modifier = Modifier.fillMaxWidth()) {
                Column(modifier = Modifier.padding(12.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                    OutlinedTextField(
                        value = remoteDeviceIdInput,
                        onValueChange = { remoteDeviceIdInput = it },
                        label = { Text(AppStrings.get(language, "remoteDeviceId")) },
                        modifier = Modifier.fillMaxWidth()
                    )
                    OutlinedTextField(
                        value = remoteDeviceNameInput,
                        onValueChange = { remoteDeviceNameInput = it },
                        label = { Text(AppStrings.get(language, "remoteDeviceName")) },
                        modifier = Modifier.fillMaxWidth()
                    )
                    Button(onClick = {
                        onPairByDeviceId(remoteDeviceIdInput, remoteDeviceNameInput)
                        remoteDeviceIdInput = ""
                        remoteDeviceNameInput = ""
                    }) { Text(AppStrings.get(language, "pairByDeviceId")) }
                }
            }
        }
        item {
            OutlinedTextField(
                value = query,
                onValueChange = onQueryChange,
                label = { Text(AppStrings.get(language, "searchPairing")) },
                modifier = Modifier.fillMaxWidth().semantics { contentDescription = "Pairing request search input" }
            )
        }
        if (requests.isEmpty()) {
            item {
                EmptyStateCard(
                    title = AppStrings.get(language, "noPairingRequests"),
                    message = AppStrings.get(language, "emptyPairingHint"),
                    actionLabel = if (query.isNotBlank()) AppStrings.get(language, "clearFilter") else null,
                    onAction = if (query.isNotBlank()) ({ onQueryChange("") }) else null
                )
            }
        }
        items(requests, key = { it.requestId }) { req ->
            Card(modifier = Modifier.fillMaxWidth()) {
                Column(modifier = Modifier.padding(14.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
                    Text(req.displayName, fontWeight = FontWeight.Bold)
                    Text("ID: ${req.deviceId}")
                    Text("${AppStrings.get(language, "status")}: ${req.platform}")
                    Text("${AppStrings.get(language, "lastError")}: ${req.at}")
                    Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                        Button(
                            modifier = Modifier.semantics { contentDescription = "Approve ${req.displayName}" },
                            onClick = { onApprove(req.requestId) }
                        ) { Text(AppStrings.get(language, "approve")) }
                        Button(
                            modifier = Modifier.semantics { contentDescription = "Reject ${req.displayName}" },
                            onClick = { onReject(req.requestId) }
                        ) { Text(AppStrings.get(language, "reject")) }
                    }
                }
            }
        }
    }
}

@Composable
private fun HistoryTab(history: List<HistoryUi>, language: String, query: String, onQueryChange: (String) -> Unit) {
    LazyColumn(verticalArrangement = Arrangement.spacedBy(10.dp)) {
        item {
            OutlinedTextField(
                value = query,
                onValueChange = onQueryChange,
                label = { Text(AppStrings.get(language, "searchHistory")) },
                modifier = Modifier.fillMaxWidth().semantics { contentDescription = "History search input" }
            )
        }
        if (history.isEmpty()) {
            item {
                EmptyStateCard(
                    title = AppStrings.get(language, "noHistory"),
                    message = AppStrings.get(language, "emptyHistoryHint"),
                    actionLabel = if (query.isNotBlank()) AppStrings.get(language, "clearFilter") else null,
                    onAction = if (query.isNotBlank()) ({ onQueryChange("") }) else null
                )
            }
        }
        items(history) { item ->
            Card(modifier = Modifier.fillMaxWidth()) {
                Column(modifier = Modifier.padding(12.dp)) {
                    Text("[${item.direction}] ${item.contentType}", fontWeight = FontWeight.Bold)
                    Text(item.preview)
                    Text(item.at)
                }
            }
        }
    }
}

@Composable
private fun SettingsTab(
    settings: SettingsUi,
    language: String,
    onChange: (SettingsUi) -> Unit,
    onTestWebDav: () -> Unit,
    onTestPublicRelay: () -> Unit
) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
            StatusRow(AppStrings.get(language, "language"), settings.language)
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(AppStrings.get(language, "darkMode"))
                Spacer(modifier = Modifier.weight(1f))
                Switch(checked = settings.darkMode, onCheckedChange = { onChange(settings.copy(darkMode = it)) })
            }
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(AppStrings.get(language, "syncMode"))
                Spacer(modifier = Modifier.weight(1f))
                Switch(
                    checked = settings.syncMode == "auto",
                    onCheckedChange = { onChange(settings.copy(syncMode = if (it) "auto" else "manual")) }
                )
            }
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(AppStrings.get(language, "webdev"))
                Spacer(modifier = Modifier.weight(1f))
                Checkbox(checked = settings.webDevEnabled, onCheckedChange = { onChange(settings.copy(webDevEnabled = it)) })
            }
            OutlinedTextField(
                value = settings.webDevBaseUrl,
                onValueChange = { onChange(settings.copy(webDevBaseUrl = it)) },
                label = { Text(AppStrings.get(language, "webdevUrl")) },
                modifier = Modifier.fillMaxWidth()
            )
            OutlinedTextField(
                value = settings.webDevUsername,
                onValueChange = { onChange(settings.copy(webDevUsername = it)) },
                label = { Text(AppStrings.get(language, "webdevUser")) },
                modifier = Modifier.fillMaxWidth()
            )
            OutlinedTextField(
                value = settings.webDevPassword,
                onValueChange = { onChange(settings.copy(webDevPassword = it)) },
                label = { Text(AppStrings.get(language, "webdevPassword")) },
                modifier = Modifier.fillMaxWidth()
            )
            Button(onClick = onTestWebDav) {
                Text(AppStrings.get(language, "testWebdev"))
            }
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(AppStrings.get(language, "publicRelay"))
                Spacer(modifier = Modifier.weight(1f))
                Checkbox(checked = settings.publicRelayEnabled, onCheckedChange = { onChange(settings.copy(publicRelayEnabled = it)) })
            }
            OutlinedTextField(
                value = settings.publicRelayBaseUrl,
                onValueChange = { onChange(settings.copy(publicRelayBaseUrl = it)) },
                label = { Text(AppStrings.get(language, "publicRelayUrl")) },
                modifier = Modifier.fillMaxWidth()
            )
            OutlinedTextField(
                value = settings.publicRelayBucket,
                onValueChange = { onChange(settings.copy(publicRelayBucket = it)) },
                label = { Text(AppStrings.get(language, "publicRelayBucket")) },
                modifier = Modifier.fillMaxWidth()
            )
            Button(onClick = onTestPublicRelay) {
                Text(AppStrings.get(language, "testPublicRelay"))
            }
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(AppStrings.get(language, "server"))
                Spacer(modifier = Modifier.weight(1f))
                Checkbox(checked = settings.localServerEnabled, onCheckedChange = { onChange(settings.copy(localServerEnabled = it)) })
            }
            StatusRow(AppStrings.get(language, "pairingPolicy"), settings.pairingPolicy)
            StatusRow(AppStrings.get(language, "space"), settings.spaceId)
        }
    }
}

@Composable
private fun StatusRow(label: String, value: String) {
    Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
        Text(label, fontWeight = FontWeight.SemiBold)
        Text(value)
    }
}

@Composable
private fun EmptyStateCard(title: String, message: String, actionLabel: String? = null, onAction: (() -> Unit)? = null) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
            Text(title, fontWeight = FontWeight.Bold)
            Text(message, color = MaterialTheme.colorScheme.onSurfaceVariant)
            if (actionLabel != null && onAction != null) {
                Button(onClick = onAction) { Text(actionLabel) }
            }
        }
    }
}

private fun encodeTrustedDevices(list: List<TrustedDeviceUi>): String {
    val array = JSONArray()
    list.forEach {
        array.put(JSONObject().put("deviceId", it.deviceId).put("displayName", it.displayName).put("lastSeen", it.lastSeen))
    }
    return array.toString()
}

private fun decodeTrustedDevices(raw: String?): List<TrustedDeviceUi> {
    if (raw.isNullOrBlank()) return emptyList()
    return try {
        val array = JSONArray(raw)
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

private fun encodeHistoryItems(list: List<HistoryUi>): String {
    val array = JSONArray()
    list.forEach {
        array.put(JSONObject().put("direction", it.direction).put("contentType", it.contentType).put("preview", it.preview).put("at", it.at))
    }
    return array.toString()
}

private fun decodeHistoryItems(raw: String?): List<HistoryUi> {
    if (raw.isNullOrBlank()) return emptyList()
    return try {
        val array = JSONArray(raw)
        List(array.length()) { index ->
            val o = array.getJSONObject(index)
            HistoryUi(
                direction = o.optString("direction"),
                contentType = o.optString("contentType"),
                preview = o.optString("preview"),
                at = o.optString("at")
            )
        }
    } catch (_: Exception) {
        emptyList()
    }
}

private fun encodePairingRequests(list: List<PairingRequestUi>): String {
    val array = JSONArray()
    list.forEach {
        array.put(
            JSONObject()
                .put("requestId", it.requestId)
                .put("deviceId", it.deviceId)
                .put("displayName", it.displayName)
                .put("platform", it.platform)
                .put("at", it.at)
        )
    }
    return array.toString()
}

private fun decodePairingRequests(raw: String?): List<PairingRequestUi> {
    if (raw.isNullOrBlank()) return emptyList()
    return try {
        val array = JSONArray(raw)
        List(array.length()) { index ->
            val o = array.getJSONObject(index)
            PairingRequestUi(
                requestId = o.optString("requestId"),
                deviceId = o.optString("deviceId", o.optString("displayName")),
                displayName = o.optString("displayName"),
                platform = o.optString("platform"),
                at = o.optString("at")
            )
        }
    } catch (_: Exception) {
        emptyList()
    }
}

@Preview(showBackground = true)
@Composable
private fun PreviewApp() {
    ClipboardSyncAndroidApp()
}
