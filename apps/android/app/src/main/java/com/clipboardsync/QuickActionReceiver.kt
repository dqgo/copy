package com.clipboardsync

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.widget.Toast

class QuickActionReceiver : BroadcastReceiver() {
    override fun onReceive(context: Context, intent: Intent) {
        when (intent.action) {
            "com.clipboardsync.ACTION_MANUAL_SYNC" -> {
                Toast.makeText(context, "Manual sync triggered", Toast.LENGTH_SHORT).show()
            }
            "com.clipboardsync.ACTION_PAUSE_SYNC" -> {
                Toast.makeText(context, "Sync paused", Toast.LENGTH_SHORT).show()
            }
        }
    }
}
