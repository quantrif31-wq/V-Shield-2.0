package com.vshield.mobile.service

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.util.Log
import com.vshield.mobile.MainActivity

class CallActionReceiver : BroadcastReceiver() {

    companion object {
        private const val TAG = "CallActionReceiver"
    }

    override fun onReceive(context: Context, intent: Intent) {
        val action = intent.action ?: return
        Log.i(TAG, "Received call action broadcast: $action")

        val fromEmpId = intent.getIntExtra("EXTRA_FROM_EMPLOYEE_ID", 0)
        val convId = intent.getIntExtra(NotificationHelper.EXTRA_CONVERSATION_ID, 0)

        when (action) {
            NotificationHelper.ACTION_REJECT_CALL -> {
                Log.i(TAG, "Handling ACTION_REJECT_CALL for employee $fromEmpId")
                NotificationHelper.cancelIncomingCallNotification(context)
                VShieldBackgroundService.rejectCurrentCall()
            }
            NotificationHelper.ACTION_ACCEPT_CALL -> {
                Log.i(TAG, "Handling ACTION_ACCEPT_CALL for employee $fromEmpId")
                NotificationHelper.cancelIncomingCallNotification(context)

                // Launch MainActivity with flags to bring call screen to front
                val launchIntent = Intent(context, MainActivity::class.java).apply {
                    addFlags(Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_SINGLE_TOP or Intent.FLAG_ACTIVITY_CLEAR_TOP)
                    putExtra(NotificationHelper.EXTRA_CALL_ACTION, NotificationHelper.ACTION_ACCEPT_CALL)
                    putExtra(NotificationHelper.EXTRA_CONVERSATION_ID, convId)
                }
                context.startActivity(launchIntent)
            }
        }
    }
}
