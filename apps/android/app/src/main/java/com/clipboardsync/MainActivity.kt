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
    val pairingRequests: List<PairingRequestUi>
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
                    darkMode = false,
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
                    TabRow(selectedTabIndex = selectedTab) {
                        Tab(selected = selectedTab == 0, onClick = { selectedTab = 0 }, text = { Text("Status") })
                        Tab(selected = selectedTab == 1, onClick = { selectedTab = 1 }, text = { Text("Devices") })
                        Tab(selected = selectedTab == 2, onClick = { selectedTab = 2 }, text = { Text("Pairing") })
                        Tab(selected = selectedTab == 3, onClick = { selectedTab = 3 }, text = { Text("History") })
                        Tab(selected = selectedTab == 4, onClick = { selectedTab = 4 }, text = { Text("Settings") })
                    }
                    Spacer(modifier = Modifier.height(12.dp))

                    when (selectedTab) {
                        0 -> StatusTab(
                            status = state.status,
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

                        1 -> DevicesTab(state.devices) { deviceId ->
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

                        2 -> PairingTab(state.pairingRequests, onApprove = { requestId ->
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
                                pairingRequests = state.pairingRequests.filterNot { it.requestId == requestId },
                                status = state.status.copy(
                                    pendingPairingCount = (state.status.pendingPairingCount - 1).coerceAtLeast(0)
                                ),
                                history = if (request == null) state.history else listOf(
                                    HistoryUi("event", "pairing", "rejected ${request.displayName}", "now")
                                ) + state.history
                            )
                        })

                        3 -> HistoryTab(state.history)
                        else -> SettingsTab(state.settings) { updated -> state = state.copy(settings = updated) }
                    }
                }
            }
        }
    }
}

@Composable
private fun StatusTab(
    status: StatusViewModel,
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
            StatusRow("Connection", status.connectionState.name)
            StatusRow("Synced Out", status.syncedOutCount.toString())
            StatusRow("Synced In", status.syncedInCount.toString())
            StatusRow("Rejected", status.rejectedEventCount.toString())
            StatusRow("Trusted", status.trustedDeviceCount.toString())
            StatusRow("Pending Pairing", status.pendingPairingCount.toString())
            StatusRow("Last Error", status.lastErrorMessage ?: "None")
            Divider()
            Button(onClick = onManualSync) { Text("Manual Sync") }
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                Button(onClick = onSendHtml) { Text("Send HTML") }
                Button(onClick = onSendImage) { Text("Send Image") }
                Button(onClick = onSendFileRef) { Text("Send File") }
            }
        }
    }
}

@Composable
private fun DevicesTab(devices: List<TrustedDeviceUi>, onRevoke: (String) -> Unit) {
    LazyColumn(verticalArrangement = Arrangement.spacedBy(10.dp)) {
        items(devices, key = { it.deviceId }) { device ->
            Card(modifier = Modifier.fillMaxWidth()) {
                Column(modifier = Modifier.padding(14.dp)) {
                    Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                        Text(device.displayName, fontWeight = FontWeight.Bold)
                        AssistChip(onClick = {}, label = { Text("trusted") })
                    }
                    Text("ID: ${device.deviceId}")
                    Text("Last seen: ${device.lastSeen}")
                    Button(onClick = { onRevoke(device.deviceId) }) {
                        Text("Revoke")
                    }
                }
            }
        }
    }
}

@Composable
private fun PairingTab(requests: List<PairingRequestUi>, onApprove: (String) -> Unit, onReject: (String) -> Unit) {
    LazyColumn(verticalArrangement = Arrangement.spacedBy(10.dp)) {
        if (requests.isEmpty()) {
            item {
                Card(modifier = Modifier.fillMaxWidth()) {
                    Text("No pending pairing request", modifier = Modifier.padding(16.dp))
                }
            }
        }
        items(requests, key = { it.requestId }) { req ->
            Card(modifier = Modifier.fillMaxWidth()) {
                Column(modifier = Modifier.padding(14.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
                    Text(req.displayName, fontWeight = FontWeight.Bold)
                    Text("Platform: ${req.platform}")
                    Text("Requested: ${req.at}")
                    Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                        Button(onClick = { onApprove(req.requestId) }) { Text("Approve") }
                        Button(onClick = { onReject(req.requestId) }) { Text("Reject") }
                    }
                }
            }
        }
    }
}

@Composable
private fun HistoryTab(history: List<HistoryUi>) {
    LazyColumn(verticalArrangement = Arrangement.spacedBy(10.dp)) {
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
private fun SettingsTab(settings: SettingsUi, onChange: (SettingsUi) -> Unit) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
            StatusRow("Language", settings.language)
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text("Dark Mode")
                Spacer(modifier = Modifier.weight(1f))
                Switch(checked = settings.darkMode, onCheckedChange = { onChange(settings.copy(darkMode = it)) })
            }
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text("WebDev Sync")
                Spacer(modifier = Modifier.weight(1f))
                Checkbox(checked = settings.webDevEnabled, onCheckedChange = { onChange(settings.copy(webDevEnabled = it)) })
            }
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text("Local Server")
                Spacer(modifier = Modifier.weight(1f))
                Checkbox(checked = settings.localServerEnabled, onCheckedChange = { onChange(settings.copy(localServerEnabled = it)) })
            }
            StatusRow("Pairing Policy", settings.pairingPolicy)
            StatusRow("Sync Mode", settings.syncMode)
            StatusRow("Space", settings.spaceId)
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
