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
import androidx.compose.runtime.getValue
import androidx.compose.runtime.isSystemInDarkTheme
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
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
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext

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
            "server" to "本地服务模式",
            "manualSync" to "手动同步",
            "sendHtml" to "发送 HTML",
            "sendImage" to "发送图片",
            "sendFile" to "发送文件",
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
            "server" to "Local Server Mode",
            "manualSync" to "Manual Sync",
            "sendHtml" to "Send HTML",
            "sendImage" to "Send Image",
            "sendFile" to "Send File",
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
    val localServerEnabled: Boolean,
    val pairingPolicy: String
)
data class PairingRequestUi(val requestId: String, val displayName: String, val platform: String, val at: String)

data class DashboardState(
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
        if (secureStore.get("workspace_key") == null) {
            secureStore.set("workspace_key", "wsk-android-${System.currentTimeMillis()}")
        }
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
    val systemDarkTheme = isSystemInDarkTheme()
    var selectedTab by remember { mutableStateOf(0) }
    var deviceQuery by remember { mutableStateOf("") }
    var historyQuery by remember { mutableStateOf("") }
    var pairingQuery by remember { mutableStateOf("") }

    var state by remember {
        mutableStateOf(
            DashboardState(
                status = StatusViewModel(
                    connectionState = SyncConnectionState.DISCONNECTED,
                    syncedOutCount = 0,
                    syncedInCount = 0,
                    rejectedEventCount = 0,
                    trustedDeviceCount = 0,
                    pendingPairingCount = 0,
                    lastErrorMessage = null
                ),
                devices = emptyList(),
                history = emptyList(),
                settings = SettingsUi(
                    language = prefs.getString("language", "zh-CN") ?: "zh-CN",
                    darkMode = prefs.getBoolean("dark_mode", systemDarkTheme),
                    syncMode = prefs.getString("sync_mode", "manual") ?: "manual",
                    spaceId = prefs.getString("space_id", "default") ?: "default",
                    webDevEnabled = prefs.getBoolean("webdav_enabled", false),
                    webDevBaseUrl = prefs.getString("webdav_base_url", "") ?: "",
                    webDevUsername = prefs.getString("webdav_username", "") ?: "",
                    webDevPassword = prefs.getString("webdav_password", "") ?: "",
                    localServerEnabled = prefs.getBoolean("local_server_enabled", false),
                    pairingPolicy = prefs.getString("pairing_policy", "manual-approve") ?: "manual-approve"
                ),
                pairingRequests = emptyList()
            )
        )
    }
    val coroutineScope = rememberCoroutineScope()

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
        q.isEmpty() || it.displayName.lowercase().contains(q) || it.platform.lowercase().contains(q)
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
                                onManualSync = {
                                    val webdev = state.settings.webDevEnabled && state.settings.webDevBaseUrl.isNotBlank()
                                    if (webdev) {
                                        val cfg = WebDavConfig(
                                            enabled = true,
                                            baseUrl = state.settings.webDevBaseUrl,
                                            username = state.settings.webDevUsername,
                                            password = state.settings.webDevPassword
                                        )
                                        coroutineScope.launch(Dispatchers.IO) {
                                            val outText = "manual sync from android @${System.currentTimeMillis()}"
                                            val uploaded = WebDavClient.uploadText(cfg, outText)
                                            val remote = WebDavClient.downloadText(cfg)
                                            withContext(Dispatchers.Main) {
                                                state = state.copy(
                                                    status = state.status.copy(
                                                        syncedOutCount = state.status.syncedOutCount + if (uploaded) 1 else 0,
                                                        syncedInCount = state.status.syncedInCount + if (remote != null) 1 else 0,
                                                        lastErrorMessage = if (uploaded) "None" else "webdev upload failed"
                                                    ),
                                                    history = buildList {
                                                        if (remote != null) add(HistoryUi("in", "text/plain", remote.take(64), "now"))
                                                        if (uploaded) add(HistoryUi("out", "text/plain", outText, "now"))
                                                        addAll(state.history)
                                                    }
                                                )
                                            }
                                        }
                                    } else {
                                        state = state.copy(
                                            status = state.status.copy(
                                                syncedOutCount = state.status.syncedOutCount + 1,
                                                syncedInCount = state.status.syncedInCount + 1,
                                                lastErrorMessage = "None"
                                            ),
                                            history = listOf(HistoryUi("out", "text/plain", "manual sync", "now")) + state.history
                                        )
                                    }
                                },
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
                                onApprove = { requestId ->
                                    val request = state.pairingRequests.find { it.requestId == requestId }
                                    state = state.copy(
                                        pairingRequests = state.pairingRequests.filterNot { it.requestId == requestId },
                                        status = state.status.copy(
                                            trustedDeviceCount = state.status.trustedDeviceCount + 1,
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
    onApprove: (String) -> Unit,
    onReject: (String) -> Unit
) {
    LazyColumn(verticalArrangement = Arrangement.spacedBy(10.dp)) {
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
    onTestWebDav: () -> Unit
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
                Text(AppStrings.get(language, "server"))
                Spacer(modifier = Modifier.weight(1f))
                Checkbox(checked = settings.localServerEnabled, onCheckedChange = { onChange(settings.copy(localServerEnabled = it)) })
            }
            StatusRow(AppStrings.get(language, "pairingPolicy"), settings.pairingPolicy)
            StatusRow(AppStrings.get(language, "syncMode"), settings.syncMode)
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

@Preview(showBackground = true)
@Composable
private fun PreviewApp() {
    ClipboardSyncAndroidApp()
}
