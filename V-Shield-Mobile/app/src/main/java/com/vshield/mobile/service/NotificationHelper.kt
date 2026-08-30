package com.vshield.mobile.service

import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.media.AudioAttributes
import android.media.RingtoneManager
import android.os.Build
import androidx.core.app.NotificationCompat
import androidx.core.app.NotificationManagerCompat
import com.vshield.mobile.MainActivity

object NotificationHelper {

    const val CHANNEL_CALLS_ID = "vshield_incoming_calls_v2"
    const val CHANNEL_MESSAGES_ID = "vshield_chat_messages"
    const val CHANNEL_ALERTS_ID = "vshield_security_alerts"
    const val CHANNEL_SERVICE_ID = "vshield_background_service"

    const val NOTIFICATION_SERVICE_ID = 9000
    const val NOTIFICATION_CALL_ID = 9001
    const val EXTRA_NAVIGATE_TO = "extra_navigate_to"
    const val EXTRA_CONVERSATION_ID = "extra_conversation_id"
    const val EXTRA_CALL_ACTION = "extra_call_action"
    const val ACTION_ACCEPT_CALL = "action_accept_call"
    const val ACTION_REJECT_CALL = "action_reject_call"
    const val ACTION_OPEN_CHAT = "action_open_chat"

    private var currentRingtone: android.media.Ringtone? = null
    private var ringtoneVibrator: android.os.Vibrator? = null

    fun playCallRingtone(context: Context) {
        try {
            stopCallRingtone()
            val ringtoneUri = RingtoneManager.getDefaultUri(RingtoneManager.TYPE_RINGTONE)
            val ringtone = RingtoneManager.getRingtone(context.applicationContext, ringtoneUri)
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
                ringtone?.isLooping = true
            }
            ringtone?.audioAttributes = AudioAttributes.Builder()
                .setUsage(AudioAttributes.USAGE_NOTIFICATION_RINGTONE)
                .setContentType(AudioAttributes.CONTENT_TYPE_SONIFICATION)
                .build()
            ringtone?.play()
            currentRingtone = ringtone

            val vibrator = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
                val vibratorManager = context.getSystemService(Context.VIBRATOR_MANAGER_SERVICE) as? android.os.VibratorManager
                vibratorManager?.defaultVibrator
            } else {
                @Suppress("DEPRECATION")
                context.getSystemService(Context.VIBRATOR_SERVICE) as? android.os.Vibrator
            }
            val pattern = longArrayOf(0, 1000, 1000)
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                vibrator?.vibrate(android.os.VibrationEffect.createWaveform(pattern, 0))
            } else {
                @Suppress("DEPRECATION")
                vibrator?.vibrate(pattern, 0)
            }
            ringtoneVibrator = vibrator
            android.util.Log.i("NotificationHelper", "Incoming call ringtone & vibration started")
        } catch (e: Exception) {
            android.util.Log.e("NotificationHelper", "Error playing ringtone: ${e.message}")
        }
    }

    fun stopCallRingtone() {
        try {
            currentRingtone?.stop()
            currentRingtone = null
            ringtoneVibrator?.cancel()
            ringtoneVibrator = null
            android.util.Log.i("NotificationHelper", "Incoming call ringtone & vibration stopped")
        } catch (_: Exception) {}
    }

    fun createNotificationChannels(context: Context) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) return

        val notificationManager = context.getSystemService(Context.NOTIFICATION_SERVICE) as? NotificationManager ?: return

        // 0. Channel for Persistent Foreground Service
        val serviceChannel = NotificationChannel(
            CHANNEL_SERVICE_ID,
            "Dịch vụ nền V-Shield",
            NotificationManager.IMPORTANCE_LOW
        ).apply {
            description = "Duy trì kết nối an toàn để nhận cuộc gọi và tin nhắn khi đóng ứng dụng"
            setShowBadge(false)
        }
        notificationManager.createNotificationChannel(serviceChannel)

        // 1. Channel for Incoming Calls (High Priority, Ringtone, Heads-up)
        val ringtoneUri = RingtoneManager.getDefaultUri(RingtoneManager.TYPE_RINGTONE)
        val callAudioAttributes = AudioAttributes.Builder()
            .setUsage(AudioAttributes.USAGE_NOTIFICATION_RINGTONE)
            .setContentType(AudioAttributes.CONTENT_TYPE_SONIFICATION)
            .build()

        val callChannel = NotificationChannel(
            CHANNEL_CALLS_ID,
            "Cuộc gọi đến",
            NotificationManager.IMPORTANCE_HIGH
        ).apply {
            description = "Thông báo khi có cuộc gọi thoại hoặc cuộc gọi video đến"
            setSound(ringtoneUri, callAudioAttributes)
            enableVibration(true)
            vibrationPattern = longArrayOf(0, 1000, 800, 1000, 800, 1000)
            lockscreenVisibility = NotificationCompat.VISIBILITY_PUBLIC
            setShowBadge(true)
        }
        notificationManager.createNotificationChannel(callChannel)

        // 2. Channel for Chat Messages (Heads-up banner, message sound)
        val notifSoundUri = RingtoneManager.getDefaultUri(RingtoneManager.TYPE_NOTIFICATION)
        val notifAudioAttributes = AudioAttributes.Builder()
            .setUsage(AudioAttributes.USAGE_NOTIFICATION_COMMUNICATION_INSTANT)
            .setContentType(AudioAttributes.CONTENT_TYPE_SONIFICATION)
            .build()

        val msgChannel = NotificationChannel(
            CHANNEL_MESSAGES_ID,
            "Tin nhắn trò chuyện",
            NotificationManager.IMPORTANCE_HIGH
        ).apply {
            description = "Thông báo khi có tin nhắn mới từ đồng nghiệp"
            setSound(notifSoundUri, notifAudioAttributes)
            enableVibration(true)
            vibrationPattern = longArrayOf(0, 200, 150, 200)
            lockscreenVisibility = NotificationCompat.VISIBILITY_PRIVATE
            setShowBadge(true)
        }
        notificationManager.createNotificationChannel(msgChannel)

        // 3. Channel for Security Alerts & Alarms
        val alertChannel = NotificationChannel(
            CHANNEL_ALERTS_ID,
            "Cảnh báo & Báo động",
            NotificationManager.IMPORTANCE_HIGH
        ).apply {
            description = "Thông báo sự cố an ninh và khẩn cấp"
            setSound(RingtoneManager.getDefaultUri(RingtoneManager.TYPE_ALARM), callAudioAttributes)
            enableVibration(true)
            vibrationPattern = longArrayOf(0, 500, 300, 500, 300, 500)
            lockscreenVisibility = NotificationCompat.VISIBILITY_PUBLIC
            setShowBadge(true)
        }
        notificationManager.createNotificationChannel(alertChannel)
    }

    fun buildForegroundServiceNotification(context: Context): android.app.Notification {
        val piFlags = PendingIntent.FLAG_UPDATE_CURRENT or (if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) PendingIntent.FLAG_IMMUTABLE else 0)
        val intent = Intent(context, MainActivity::class.java).apply {
            addFlags(Intent.FLAG_ACTIVITY_SINGLE_TOP or Intent.FLAG_ACTIVITY_CLEAR_TOP)
        }
        val pendingIntent = PendingIntent.getActivity(context, 0, intent, piFlags)

        return NotificationCompat.Builder(context, CHANNEL_SERVICE_ID)
            .setSmallIcon(android.R.drawable.stat_sys_phone_call)
            .setContentTitle("V-Shield")
            .setContentText("Đang kết nối an toàn để nhận cuộc gọi & tin nhắn")
            .setPriority(NotificationCompat.PRIORITY_LOW)
            .setCategory(NotificationCompat.CATEGORY_SERVICE)
            .setOngoing(true)
            .setContentIntent(pendingIntent)
            .build()
    }

    fun showIncomingCallNotification(
        context: Context,
        callType: String,
        fromEmployeeId: Int,
        fromFullName: String,
        conversationId: Int?
    ) {
        // Start playing ringtone and vibration immediately
        playCallRingtone(context)

        val piFlags = PendingIntent.FLAG_UPDATE_CURRENT or (if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) PendingIntent.FLAG_IMMUTABLE else 0)

        // Content intent (tap notification body) -> Open app to Call screen
        val contentIntent = Intent(context, MainActivity::class.java).apply {
            addFlags(Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_SINGLE_TOP or Intent.FLAG_ACTIVITY_CLEAR_TOP)
            putExtra(EXTRA_NAVIGATE_TO, "call")
            putExtra(EXTRA_CONVERSATION_ID, conversationId ?: 0)
        }
        val contentPendingIntent = PendingIntent.getActivity(context, 101, contentIntent, piFlags)

        // Accept intent -> Open app directly and answer call
        val acceptIntent = Intent(context, MainActivity::class.java).apply {
            addFlags(Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_SINGLE_TOP or Intent.FLAG_ACTIVITY_CLEAR_TOP)
            putExtra(EXTRA_CALL_ACTION, ACTION_ACCEPT_CALL)
            putExtra("EXTRA_FROM_EMPLOYEE_ID", fromEmployeeId)
            putExtra("EXTRA_FROM_FULL_NAME", fromFullName)
            putExtra("EXTRA_CALL_TYPE", callType)
            putExtra(EXTRA_CONVERSATION_ID, conversationId ?: 0)
        }
        val acceptPendingIntent = PendingIntent.getActivity(context, 102, acceptIntent, piFlags)

        // Reject broadcast intent -> CallActionReceiver (dismisses notif & hangs up in background)
        val rejectIntent = Intent(context, CallActionReceiver::class.java).apply {
            action = ACTION_REJECT_CALL
            putExtra("EXTRA_FROM_EMPLOYEE_ID", fromEmployeeId)
            putExtra(EXTRA_CONVERSATION_ID, conversationId ?: 0)
        }
        val rejectPendingIntent = PendingIntent.getBroadcast(context, 103, rejectIntent, piFlags)

        val isVideo = callType == "video"
        val subtitle = if (isVideo) "Cuộc gọi video đến..." else "Cuộc gọi thoại đến..."
        val ringtoneUri = RingtoneManager.getDefaultUri(RingtoneManager.TYPE_RINGTONE)

        val builder = NotificationCompat.Builder(context, CHANNEL_CALLS_ID)
            .setSmallIcon(android.R.drawable.stat_sys_phone_call)
            .setContentTitle(fromFullName)
            .setContentText(subtitle)
            .setPriority(NotificationCompat.PRIORITY_MAX)
            .setCategory(NotificationCompat.CATEGORY_CALL)
            .setVisibility(NotificationCompat.VISIBILITY_PUBLIC)
            .setAutoCancel(true)
            .setOngoing(true)
            .setSound(ringtoneUri)
            .setContentIntent(contentPendingIntent)
            .setFullScreenIntent(contentPendingIntent, true)
            .addAction(android.R.drawable.ic_menu_close_clear_cancel, "Từ chối", rejectPendingIntent)
            .addAction(android.R.drawable.ic_menu_call, "Trả lời", acceptPendingIntent)

        try {
            NotificationManagerCompat.from(context).notify(NOTIFICATION_CALL_ID, builder.build())
        } catch (_: SecurityException) {}
    }

    fun cancelIncomingCallNotification(context: Context) {
        try {
            stopCallRingtone()
            NotificationManagerCompat.from(context).cancel(NOTIFICATION_CALL_ID)
        } catch (_: Exception) {}
    }

    fun showMessageNotification(
        context: Context,
        conversationId: Int,
        senderId: Int,
        senderName: String,
        messageText: String
    ) {
        val piFlags = PendingIntent.FLAG_UPDATE_CURRENT or (if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) PendingIntent.FLAG_IMMUTABLE else 0)

        val intent = Intent(context, MainActivity::class.java).apply {
            action = "ACTION_OPEN_CHAT_${conversationId}_${System.currentTimeMillis()}"
            addFlags(Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_SINGLE_TOP or Intent.FLAG_ACTIVITY_CLEAR_TOP)
            putExtra(EXTRA_NAVIGATE_TO, ACTION_OPEN_CHAT)
            putExtra(EXTRA_CONVERSATION_ID, conversationId)
        }
        val pendingIntent = PendingIntent.getActivity(context, conversationId, intent, piFlags)

        val builder = NotificationCompat.Builder(context, CHANNEL_MESSAGES_ID)
            .setSmallIcon(android.R.drawable.stat_notify_chat)
            .setContentTitle(senderName)
            .setContentText(messageText)
            .setStyle(NotificationCompat.BigTextStyle().bigText(messageText))
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setCategory(NotificationCompat.CATEGORY_MESSAGE)
            .setVisibility(NotificationCompat.VISIBILITY_PRIVATE)
            .setAutoCancel(true)
            .setContentIntent(pendingIntent)

        try {
            NotificationManagerCompat.from(context).notify(conversationId, builder.build())
        } catch (_: SecurityException) {}
    }

    fun cancelMessageNotification(context: Context, conversationId: Int) {
        try {
            NotificationManagerCompat.from(context).cancel(conversationId)
        } catch (_: Exception) {}
    }

    fun showSecurityAlertNotification(
        context: Context,
        notificationId: Int,
        title: String,
        message: String
    ) {
        val piFlags = PendingIntent.FLAG_UPDATE_CURRENT or (if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) PendingIntent.FLAG_IMMUTABLE else 0)

        val intent = Intent(context, MainActivity::class.java).apply {
            addFlags(Intent.FLAG_ACTIVITY_SINGLE_TOP or Intent.FLAG_ACTIVITY_CLEAR_TOP)
        }
        val pendingIntent = PendingIntent.getActivity(context, notificationId, intent, piFlags)

        val builder = NotificationCompat.Builder(context, CHANNEL_ALERTS_ID)
            .setSmallIcon(android.R.drawable.stat_notify_error)
            .setContentTitle(title)
            .setContentText(message)
            .setStyle(NotificationCompat.BigTextStyle().bigText(message))
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setCategory(NotificationCompat.CATEGORY_ALARM)
            .setAutoCancel(true)
            .setContentIntent(pendingIntent)

        try {
            NotificationManagerCompat.from(context).notify(notificationId, builder.build())
        } catch (_: SecurityException) {}
    }
}
