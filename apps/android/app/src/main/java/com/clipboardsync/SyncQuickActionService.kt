package com.clipboardsync

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.app.Service
import android.content.Intent
import android.os.Build
import android.os.IBinder
import androidx.core.app.NotificationCompat

class SyncQuickActionService : Service() {

    override fun onCreate() {
        super.onCreate()
        createChannel()
        startForeground(1001, buildNotification())
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        return START_STICKY
    }

    override fun onBind(intent: Intent?): IBinder? = null

    private fun buildNotification(): Notification {
        val manualIntent = Intent(this, QuickActionReceiver::class.java).apply {
            action = "com.clipboardsync.ACTION_MANUAL_SYNC"
        }
        val pauseIntent = Intent(this, QuickActionReceiver::class.java).apply {
            action = "com.clipboardsync.ACTION_PAUSE_SYNC"
        }

        val pendingFlags = PendingIntent.FLAG_UPDATE_CURRENT or PendingIntent.FLAG_IMMUTABLE
        val manualPending = PendingIntent.getBroadcast(this, 11, manualIntent, pendingFlags)
        val pausePending = PendingIntent.getBroadcast(this, 12, pauseIntent, pendingFlags)

        return NotificationCompat.Builder(this, "clipboardsync.quick")
            .setSmallIcon(android.R.drawable.stat_notify_sync)
            .setContentTitle("Clipboard Sync")
            .setContentText("Foreground sync service is active")
            .setOngoing(true)
            .addAction(0, "Manual Sync", manualPending)
            .addAction(0, "Pause", pausePending)
            .build()
    }

    private fun createChannel() {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            val channel = NotificationChannel(
                "clipboardsync.quick",
                "Clipboard Sync Quick Actions",
                NotificationManager.IMPORTANCE_LOW
            )
            val manager = getSystemService(NotificationManager::class.java)
            manager.createNotificationChannel(channel)
        }
    }
}
