package com.vshield.mobile.service

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.util.Log
import com.vshield.mobile.data.TokenManager

class BootReceiver : BroadcastReceiver() {
    companion object {
        const val ACTION_RESTART_SERVICE = "com.vshield.mobile.RESTART_BACKGROUND_SERVICE"
    }

    override fun onReceive(context: Context, intent: Intent) {
        val action = intent.action
        Log.i("BootReceiver", "Received broadcast: $action")

        val tokenManager = TokenManager(context)
        val token = tokenManager.getToken()
        if (!token.isNullOrBlank()) {
            Log.i("BootReceiver", "Ensuring VShieldBackgroundService is running (action=$action)")
            VShieldBackgroundService.start(context)
        }
    }
}
