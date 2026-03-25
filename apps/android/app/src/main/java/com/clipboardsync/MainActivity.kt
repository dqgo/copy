package com.clipboardsync

import android.content.Intent
import android.os.Build
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.background
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
import androidx.compose.material3.AssistChip
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Checkbox
import androidx.compose.material3.Divider
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
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
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.animation.AnimatedContent
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.slideInHorizontally
import androidx.compose.animation.slideOutHorizontally
import androidx.compose.animation.togetherWith

// i18n translations
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
            "server" to "本地服务模式",
            "justNow" to "刚刚",
            "minAgo" to "分钟前",
            "hoursAgo" to "小时前",
            "daysAgo" to "天前",
            "manual" to "手动",
            "auto" to "自动",
            "default" to "默认",
            "work" to "工作",
            "lab" to "实验室",
            "manualApprove" to "手动批准",
            "autoApproveInvite" to "邀请自动批准",
            "noDevices" to "暂无设备",
            "noPairingRequests" to "暂无配对请求",
            "noHistory" to "暂无历史记录",
            "confirmRevoke" to "确认撤销此设备吗？",
            "confirmReject" to "确认拒绝此请求吗？",
            "confirm" to "确认",
            "cancel" to "取消"
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
            "server" to "Local Server Mode",
            "justNow" to "just now",
            "minAgo" to "min ago",
            "hoursAgo" to "hours ago",
            "daysAgo" to "days ago",
            "manual" to "Manual",
            "auto" to "Auto",
            "default" to "Default",
            "work" to "Work",
            "lab" to "Lab",
            "manualApprove" to "Manual Approve",
            "autoApproveInvite" to "Auto-Approve Invite",
            "noDevices" to "No devices",
            "noPairingRequests" to "No pairing requests",
            "noHistory" to "No history",
            "confirmRevoke" to "Confirm revoking this device?",
            "confirmReject" to "Confirm rejecting this request?"
            "confirm" to "Confirm",
            "cancel" to "Cancel"
        )
    )

    fun get(language: String, key: String): String {
        return translations[language]?.get(key) ?: translations["en-US"]?.get(key) ?: key
    }
}

data class TrustedDeviceUi(
    val deviceId: String,
    val displayName: String,
    val lastSeen: String
)

data class HistoryUi(
    val direction: String,
    val contentType: String,
    val preview: String,
    val at: String
)

data class SettingsUi(
    val language: String,
    val darkMode: Boolean,
    val syncMode: String,
    val spaceId: String,
    val webDevEnabled: Boolean,
    val localServerEnabled: Boolean,
    val pairingPolicy: String
)

data class PairingRequestUi(
    val requestId: String,
    val displayName: String,
    val platform: String,
    val at: String
)

data class DashboardState(
    val status: StatusViewModel,
    val devices: List<TrustedDeviceUi>,
    val history: List<HistoryUi>,
    val settings: SettingsUi,
    val pairingRequests: List<PairingRequestUi>,
    val isLoading: Boolean = false,
    val errorMessage: String? = null,
    val showConfirmDialog: Boolean = false,
    val confirmDialogTitle: String = "",
    val confirmDialogMessage: String = "",
    val confirmDialogOnConfirm: (() -> Unit)? = null
)

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        val quickServiceIntent = Intent(this, SyncQuickActionService::class.java)
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            startForegroundService(quickServiceIntent)
        } else {
            startService(quickServiceIntent)
        }
        setContent {
            ClipboardSyncAndroidApp()
        }
    }
}

@Composable
@OptIn(ExperimentalMaterial3Api::class)
private fun ClipboardSyncAndroidApp() {
    val systemDarkTheme = isSystemInDarkTheme()
    
    var selectedTab by remember { mutableStateOf(0) }
    var state by remember {
        mutableStateOf(
            DashboardState(
                status = StatusViewModel(
                    connectionState = SyncConnectionState.CONNECTED,
                    syncedOutCount = 3,
                    syncedInCount = 2,
                    rejectedEventCount = 0,
                    trustedDeviceCount = 3,
                    pendingPairingCount = 1,
                    lastErrorMessage = "None"
                ),
                devices = listOf(
                    TrustedDeviceUi("android-main", "Android Phone", "just now"),
                    TrustedDeviceUi("win-local", "Windows Desktop", "2 min ago"),
                    TrustedDeviceUi("ios-handset", "iPhone", "8 min ago")
                ),
                history = listOf(
                    HistoryUi("out", "text/plain", "hello from android", "10:03"),
                    HistoryUi("in", "text/plain", "copied on windows", "09:56")
                ),
                settings = SettingsUi(
                    language = "zh-CN",
                    darkMode = systemDarkTheme,
                    syncMode = "manual",
                    spaceId = "default",
                    webDevEnabled = false,
                    localServerEnabled = false,
                    pairingPolicy = "manual-approve"
                ),
                pairingRequests = listOf(
                    PairingRequestUi("req-and-001", "iPad Air", "ios", "10:22")
                )
            )
        )
    }

    MaterialTheme {
        Box(
            modifier = Modifier
                .fillMaxSize()
                .background(
                    Brush.verticalGradient(
                        colors = listOf(Color(0xFF0A355A), Color(0xFF0C5D56), Color(0xFF0B2233))
                    )
                )
        ) {
            Scaffold(
                topBar = {
                    TopAppBar(title = { Text("Clipboard Sync") })
                },
                containerColor = Color.Transparent
            ) { padding ->
                Column(
                    modifier = Modifier
                        .fillMaxSize()
                        .padding(padding)
                        .padding(14.dp)
                ) {
                    // Error message display
                    if (state.errorMessage != null) {
                        Card(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(bottom = 12.dp),
                            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.errorContainer)
                        ) {
                            Row(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(12.dp),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Text(state.errorMessage!!, color = MaterialTheme.colorScheme.onErrorContainer)
                                Button(
                                    onClick = { state = state.copy(errorMessage = null) },
                                    modifier = Modifier.height(32.dp)
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

                    when (selectedTab) {
                        0 -> StatusTab(
                            status = state.status,
                            language = state.settings.language,
                            onManualSync = {
                                state = state.copy(
                                    status = state.status.copy(
                                        syncedOutCount = state.status.syncedOutCount + 1,
                                        syncedInCount = state.status.syncedInCount + 1,
                                        lastErrorMessage = "None"
                                    ),
                                    history = listOf(
                                        HistoryUi("out", "text/plain", "manual sync", "now")
                                    ) + state.history
                                )
                            },
                            onSendHtml = {

                                                    AnimatedContent(targetState = selectedTab,
                                                        transitionSpec = {
                                                            (slideInHorizontally(initialOffsetX = { 300 }) + fadeIn()).togetherWith(
                                                                slideOutHorizontally(targetOffsetX = { -300 }) + fadeOut()
                                                            )
                                                        }) { tab ->
                                                    when (tab) {
                                state = state.copy(
                                    status = state.status.copy(syncedOutCount = state.status.syncedOutCount + 1),
                                    history = listOf(
                                        HistoryUi("out", "text/html", "<b>Clipboard Sync</b>", "now")
                                    ) + state.history
                                )
                            },
                            onSendImage = {
                                state = state.copy(
                                    status = state.status.copy(syncedOutCount = state.status.syncedOutCount + 1),
                                    history = listOf(
                                        HistoryUi("out", "image/png", "screenshot.png (ref)", "now")
                                    ) + state.history
                                )
                            },
                            onSendFileRef = {
                                state = state.copy(
                                    status = state.status.copy(syncedOutCount = state.status.syncedOutCount + 1),
                                    history = listOf(
                                        HistoryUi("out", "application/x-clipboard-file-ref", "C:/tmp/demo.txt", "now")
                                    ) + state.history
                                )
                            }
                        )

                        1 -> DevicesTab(state.devices, state.settings.language) { deviceId ->
                                val deviceName = state.devices.find { it.deviceId == deviceId }?.displayName ?: deviceId
                                state = state.copy(
                                    showConfirmDialog = true,
                                    confirmDialogTitle = AppStrings.get(state.settings.language, "revoke"),
                                    confirmDialogMessage = "Are you sure you want to revoke $deviceName?",
                                    confirmDialogOnConfirm = {
                                        state = state.copy(
                                            devices = state.devices.filterNot { it.deviceId == deviceId },
                                            status = state.status.copy(
                                                rejectedEventCount = state.status.rejectedEventCount + 1,
                                                trustedDeviceCount = (state.status.trustedDeviceCount - 1).coerceAtLeast(0),
                                                lastErrorMessage = "Revoked device: $deviceId"
                                            ),
                                            history = listOf(
                                                HistoryUi("event", "device", "revoked $deviceId", "now")
                                            ) + state.history
                                        )
                                    }
                                )
                        }

                        2 -> PairingTab(state.pairingRequests, state.settings.language, onApprove = { requestId ->
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
                        }, onReject = { requestId ->
                            val request = state.pairingRequests.find { it.requestId == requestId }
                                state = state.copy(
                                    showConfirmDialog = true,
                                    confirmDialogTitle = AppStrings.get(state.settings.language, "reject"),
                                    confirmDialogMessage = "Are you sure you want to reject ${request?.displayName}?",
                                    confirmDialogOnConfirm = {
                                        state = state.copy(
                                            pairingRequests = state.pairingRequests.filterNot { it.requestId == requestId },
                                            status = state.status.copy(
                                                pendingPairingCount = (state.status.pendingPairingCount - 1).coerceAtLeast(0)
                                            ),
                                            history = if (request == null) state.history else listOf(
                                                HistoryUi("event", "pairing", "rejected ${request.displayName}", "now")
                                            ) + state.history
                                        )
                                    }
                                )
                        })

                        3 -> HistoryTab(state.history, state.settings.language)
                        else -> SettingsTab(state.settings, state.settings.language) { updated -> state = state.copy(settings = updated) }
                    }
                        }
                    
            }

                // Confirmation Dialog
                if (state.showConfirmDialog) {
                    AlertDialog(
                        onDismissRequest = { state = state.copy(showConfirmDialog = false) },
                        title = { Text(state.confirmDialogTitle) },
                        text = { Text(state.confirmDialogMessage) },
                        confirmButton = {
                            Button(onClick = {
                                state.confirmDialogOnConfirm?.invoke()
                                state = state.copy(showConfirmDialog = false)
                            }) {
                                Text(AppStrings.get(state.settings.language, "confirm") ?: "Confirm")
                            }
                        },
                        dismissButton = {
                            Button(onClick = { state = state.copy(showConfirmDialog = false) }) {
                                Text(AppStrings.get(state.settings.language, "cancel") ?: "Cancel")
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
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface.copy(alpha = 0.9f))
    ) {
        Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
            StatusRow(AppStrings.get(language, "connection"), status.connectionState.name)
            StatusRow(AppStrings.get(language, "sent"), status.syncedOutCount.toString())
            StatusRow(AppStrings.get(language, "received"), status.syncedInCount.toString())
            StatusRow(AppStrings.get(language, "rejected"), status.rejectedEventCount.toString())
            StatusRow(AppStrings.get(language, "trustedCount"), status.trustedDeviceCount.toString())
            StatusRow(AppStrings.get(language, "pendingPairing"), status.pendingPairingCount.toString())
            StatusRow("Last Error", status.lastErrorMessage ?: "None")
            Divider()
            Button(onClick = onManualSync) { Text(AppStrings.get(language, "manualSync")) }
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                Button(onClick = onSendHtml) { Text(AppStrings.get(language, "sendHtml")) }
                Button(onClick = onSendImage) { Text(AppStrings.get(language, "sendImage")) }
                Button(onClick = onSendFileRef) { Text(AppStrings.get(language, "sendFile")) }
            }
        }
    }
}

@Composable
private fun DevicesTab(devices: List<TrustedDeviceUi>, language: String, onRevoke: (String) -> Unit) {
    LazyColumn(verticalArrangement = Arrangement.spacedBy(10.dp)) {
        items(devices, key = { it.deviceId }) { device ->
            Card(modifier = Modifier.fillMaxWidth()) {
                Column(modifier = Modifier.padding(14.dp)) {
                    Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                        Text(device.displayName, fontWeight = FontWeight.Bold)
                        AssistChip(onClick = {}, label = { Text(AppStrings.get(language, "trustedCount")) })
                    }
                    Text("ID: ${device.deviceId}")
                    Text("${AppStrings.get(language, "lastError")}: ${device.lastSeen}")
                    Button(onClick = { onRevoke(device.deviceId) }) {
                        Text(AppStrings.get(language, "revoke"))
                    }
                }
            }
        }
    }
}

@Composable
private fun PairingTab(requests: List<PairingRequestUi>, language: String, onApprove: (String) -> Unit, onReject: (String) -> Unit) {
    LazyColumn(verticalArrangement = Arrangement.spacedBy(10.dp)) {
        if (requests.isEmpty()) {
            item {
                Card(modifier = Modifier.fillMaxWidth()) {
                    Text(AppStrings.get(language, "noPairingRequests"), modifier = Modifier.padding(16.dp))
                }
            }
        }
        items(requests, key = { it.requestId }) { req ->
            Card(modifier = Modifier.fillMaxWidth()) {
                Column(modifier = Modifier.padding(14.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
                    Text(req.displayName, fontWeight = FontWeight.Bold)
                    Text("${AppStrings.get(language, "status")}: ${req.platform}")
                    Text("${AppStrings.get(language, "lastError")}: ${req.at}")
                    Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                        Button(onClick = { onApprove(req.requestId) }) { Text(AppStrings.get(language, "approve")) }
                        Button(onClick = { onReject(req.requestId) }) { Text(AppStrings.get(language, "reject")) }
                    }
                }
            }
        }
    }
}

@Composable
private fun HistoryTab(history: List<HistoryUi>, language: String) {
    LazyColumn(verticalArrangement = Arrangement.spacedBy(10.dp)) {
        if (history.isEmpty()) {
            item {
                Card(modifier = Modifier.fillMaxWidth()) {
                    Text(AppStrings.get(language, "noHistory"), modifier = Modifier.padding(16.dp))
                }
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
private fun SettingsTab(settings: SettingsUi, language: String, onChange: (SettingsUi) -> Unit) {
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

@Preview(showBackground = true)
@Composable
private fun PreviewApp() {
    ClipboardSyncAndroidApp()
}
