package com.vshield.mobile.service

import android.app.AlarmManager
import android.app.PendingIntent
import android.app.Service
import android.content.Context
import android.content.Intent
import android.net.ConnectivityManager
import android.net.Network
import android.net.NetworkCapabilities
import android.net.NetworkRequest
import android.os.Build
import android.os.IBinder
import android.os.PowerManager
import android.os.SystemClock
import android.util.Log
import androidx.core.content.ContextCompat
import com.vshield.mobile.BuildConfig
import com.vshield.mobile.data.ChatSignalRClient
import com.vshield.mobile.data.NotificationSignalRClient
import com.vshield.mobile.data.TokenManager
import com.vshield.mobile.data.model.SignalRCallEnded
import com.vshield.mobile.data.model.SignalRCallInfo
import com.vshield.mobile.data.model.SignalRCallResponse
import com.vshield.mobile.data.model.SignalRNotification
import com.vshield.mobile.data.model.SignalRReadReceipt
import com.vshield.mobile.data.model.SignalRReceiveMessage
import com.vshield.mobile.data.model.SignalRTypingInfo
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.delay
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch

class VShieldBackgroundService : Service() {

    private val serviceScope = CoroutineScope(SupervisorJob() + Dispatchers.IO)
    private var tokenManager: TokenManager? = null
    private var connectivityManager: ConnectivityManager? = null
    private var networkCallback: ConnectivityManager.NetworkCallback? = null
    private var wakeLock: PowerManager.WakeLock? = null

    companion object {
        private const val TAG = "VShieldBgService"

        @Volatile
        var isRunning: Boolean = false
            private set

        @Volatile
        var isAppInForeground: Boolean = false

        @Volatile
        var currentOpenedConversationId: Int? = null

        var chatClient: ChatSignalRClient? = null
            private set

        var notificationClient: NotificationSignalRClient? = null
            private set

        // Listeners for UI when ChatViewModel is active
        var onChatMessageReceived: ((SignalRReceiveMessage) -> Unit)? = null
        var onChatMessagesRead: ((SignalRReadReceipt) -> Unit)? = null
        var onChatUserTyping: ((SignalRTypingInfo) -> Unit)? = null
        var onChatIncomingCall: ((SignalRCallInfo) -> Unit)? = null
        var onChatCallResponse: ((SignalRCallResponse) -> Unit)? = null
        var onChatCallEnded: ((SignalRCallEnded) -> Unit)? = null

        var onNewNotification: ((SignalRNotification) -> Unit)? = null
        var onUnreadCountUpdated: ((Int) -> Unit)? = null
        var onSyncEventApplied: ((com.google.gson.JsonObject) -> Unit)? = null
        var onNotificationConnectionChanged: ((Boolean) -> Unit)? = null

        @Volatile
        var lastIncomingCall: SignalRCallInfo? = null

        fun rejectCurrentCall() {
            val call = lastIncomingCall
            if (call != null) {
                Log.i(TAG, "rejectCurrentCall: sending reject to employee ${call.fromEmployeeId}")
                chatClient?.callResponse(call.fromEmployeeId, "reject", "")
                lastIncomingCall = null
            }
        }

        fun start(context: Context) {
            val intent = Intent(context, VShieldBackgroundService::class.java)
            try {
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                    ContextCompat.startForegroundService(context, intent)
                } else {
                    context.startService(intent)
                }
            } catch (e: Exception) {
                Log.e(TAG, "Failed to start VShieldBackgroundService: ${e.message}", e)
            }
        }

        fun stop(context: Context) {
            val intent = Intent(context, VShieldBackgroundService::class.java)
            context.stopService(intent)
        }
    }

    override fun onCreate() {
        super.onCreate()
        Log.i(TAG, "VShieldBackgroundService created")
        isRunning = true
        tokenManager = TokenManager(this)

        acquireWakeLock()
        NotificationHelper.createNotificationChannels(this)
        val notif = NotificationHelper.buildForegroundServiceNotification(this)
        startForeground(NotificationHelper.NOTIFICATION_SERVICE_ID, notif)

        initSignalRConnections()
        registerNetworkCallback()
        startHeartbeat()
        scheduleWatchdogAlarm()
    }

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        Log.i(TAG, "VShieldBackgroundService onStartCommand")
        acquireWakeLock()
        initSignalRConnections()
        return START_STICKY
    }

    private fun initSignalRConnections() {
        val token = tokenManager?.getToken()
        if (token.isNullOrBlank() || BuildConfig.DEMO_MODE) {
            Log.d(TAG, "No token or in demo mode, skipping background SignalR connect")
            return
        }

        val baseUrl = BuildConfig.API_BASE_URL

        // Initialize and connect Chat SignalR
        if (chatClient == null) {
            val client = ChatSignalRClient(baseUrl, token, serviceScope)
            setupChatCallbacks(client)
            client.connect()
            chatClient = client
        } else {
            chatClient?.connect()
        }

        // Initialize and connect Notification SignalR
        if (notificationClient == null) {
            val notifClient = NotificationSignalRClient(baseUrl, token, serviceScope)
            setupNotificationCallbacks(notifClient)
            notifClient.connect()
            notificationClient = notifClient
        } else {
            notificationClient?.connect()
        }
    }

    private fun setupChatCallbacks(client: ChatSignalRClient) {
        client.onMessageReceived = { msg ->
            val myEmpId = tokenManager?.getEmployeeId() ?: 0
            val isInCurrentConv = isAppInForeground && currentOpenedConversationId == msg.conversationId
            Log.i(TAG, "Chat message received from ${msg.senderId} (myEmpId=$myEmpId, isInCurrentConv=$isInCurrentConv, isForeground=$isAppInForeground): ${msg.content}")

            if (!isInCurrentConv && msg.senderId != myEmpId) {
                // Trigger Android notification banner with sound and vibration
                Log.i(TAG, "Triggering showMessageNotification for conv ${msg.conversationId}")
                NotificationHelper.showMessageNotification(
                    context = this,
                    conversationId = msg.conversationId,
                    senderId = msg.senderId,
                    senderName = msg.fullName ?: "Đồng nghiệp",
                    messageText = msg.content ?: ""
                )
            }

            // Forward to active UI listener
            onChatMessageReceived?.invoke(msg)
        }

        client.onMessagesRead = { read ->
            onChatMessagesRead?.invoke(read)
        }

        client.onUserTyping = { info ->
            onChatUserTyping?.invoke(info)
        }

        client.onIncomingCall = { call ->
            Log.i(TAG, "Incoming call received from ${call.fromEmployeeId}, type=${call.signalingType}")
            if (call.signalingType == "offer" && !call.signalingData.isNullOrBlank()) {
                lastIncomingCall = call
                val isVideo = (call.signalingData ?: "").contains("m=video")
                val callType = if (isVideo) "video" else "audio"

                // Show full-screen / Heads-up Incoming Call notification with ringtone
                NotificationHelper.showIncomingCallNotification(
                    context = this,
                    callType = callType,
                    fromEmployeeId = call.fromEmployeeId,
                    fromFullName = call.fromFullName ?: "Cuộc gọi đến",
                    conversationId = call.conversationId
                )
            }

            onChatIncomingCall?.invoke(call)
        }

        client.onCallResponse = { resp ->
            if (resp.signalingType == "reject") {
                lastIncomingCall = null
                NotificationHelper.cancelIncomingCallNotification(this)
            }
            onChatCallResponse?.invoke(resp)
        }

        client.onCallEnded = { ended ->
            val myId = tokenManager?.getEmployeeId() ?: 0
            if (ended.fromEmployeeId != myId) {
                lastIncomingCall = null
                NotificationHelper.cancelIncomingCallNotification(this)
            }
            onChatCallEnded?.invoke(ended)
        }
    }

    private fun setupNotificationCallbacks(client: NotificationSignalRClient) {
        client.onNotificationReceived = { notif ->
            // Trigger security alert notification if app in background
            if (!isAppInForeground) {
                NotificationHelper.showSecurityAlertNotification(
                    context = this,
                    notificationId = notif.id.toInt(),
                    title = notif.title ?: "Thông báo bảo mật",
                    message = notif.body ?: ""
                )
            }
            onNewNotification?.invoke(notif)
        }

        client.onUnreadCountUpdated = { count ->
            onUnreadCountUpdated?.invoke(count)
        }

        client.onSyncEventApplied = { syncData ->
            onSyncEventApplied?.invoke(syncData)
        }

        client.onConnectionChanged = { connected ->
            onNotificationConnectionChanged?.invoke(connected)
        }
    }

    private fun registerNetworkCallback() {
        try {
            connectivityManager = getSystemService(Context.CONNECTIVITY_SERVICE) as? ConnectivityManager
            val request = NetworkRequest.Builder()
                .addCapability(NetworkCapabilities.NET_CAPABILITY_INTERNET)
                .build()

            networkCallback = object : ConnectivityManager.NetworkCallback() {
                override fun onAvailable(network: Network) {
                    Log.d(TAG, "Network available, reconnecting SignalR...")
                    serviceScope.launch {
                        chatClient?.connect()
                        notificationClient?.connect()
                    }
                }
            }

            connectivityManager?.registerNetworkCallback(request, networkCallback!!)
        } catch (e: Exception) {
            Log.e(TAG, "Error registering network callback: ${e.message}")
        }
    }

    private fun startHeartbeat() {
        serviceScope.launch {
            while (isActive) {
                delay(10000) // Check connection health every 10s
                try {
                    val token = tokenManager?.getToken()
                    if (!token.isNullOrBlank() && !BuildConfig.DEMO_MODE) {
                        if (chatClient == null) {
                            initSignalRConnections()
                        } else if (chatClient?.connectionState?.value != ChatSignalRClient.ConnectionState.CONNECTED &&
                            chatClient?.connectionState?.value != ChatSignalRClient.ConnectionState.CONNECTING) {
                            Log.d(TAG, "Heartbeat: Reconnecting Chat SignalR...")
                            chatClient?.forceReconnect()
                        }

                        if (notificationClient == null) {
                            initSignalRConnections()
                        } else if (notificationClient?.connectionState?.value != NotificationSignalRClient.ConnectionState.CONNECTED &&
                            notificationClient?.connectionState?.value != NotificationSignalRClient.ConnectionState.CONNECTING) {
                            Log.d(TAG, "Heartbeat: Reconnecting Notification SignalR...")
                            notificationClient?.forceReconnect()
                        }
                    }
                } catch (e: Exception) {
                    Log.e(TAG, "Heartbeat error: ${e.message}")
                }
            }
        }
    }

    override fun onTaskRemoved(rootIntent: Intent?) {
        super.onTaskRemoved(rootIntent)
        Log.i(TAG, "onTaskRemoved: App swiped away. Keeping VShieldBackgroundService alive!")
        isAppInForeground = false
        currentOpenedConversationId = null

        // Clear UI listeners to prevent leaking dead Activity/Compose references
        onChatMessageReceived = null
        onChatMessagesRead = null
        onChatUserTyping = null
        onChatIncomingCall = null
        onChatCallResponse = null
        onChatCallEnded = null
        onNewNotification = null
        onUnreadCountUpdated = null
        onNotificationConnectionChanged = null

        acquireWakeLock()

        // Ensure connections are active without interrupting already-connected sockets
        serviceScope.launch {
            try {
                if (chatClient == null) {
                    initSignalRConnections()
                } else if (chatClient?.connectionState?.value != ChatSignalRClient.ConnectionState.CONNECTED &&
                    chatClient?.connectionState?.value != ChatSignalRClient.ConnectionState.CONNECTING) {
                    chatClient?.connect()
                }

                if (notificationClient == null) {
                    initSignalRConnections()
                } else if (notificationClient?.connectionState?.value != NotificationSignalRClient.ConnectionState.CONNECTED &&
                    notificationClient?.connectionState?.value != NotificationSignalRClient.ConnectionState.CONNECTING) {
                    notificationClient?.connect()
                }
            } catch (e: Exception) {
                Log.e(TAG, "Error in onTaskRemoved check: ${e.message}")
            }
        }

        // Reschedule service restart via AlarmManager in case OS attempts to kill the process
        try {
            val restartIntent = Intent(applicationContext, VShieldBackgroundService::class.java).apply {
                setPackage(packageName)
            }
            val piFlags = PendingIntent.FLAG_ONE_SHOT or (if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) PendingIntent.FLAG_IMMUTABLE else 0)
            val pendingIntent = PendingIntent.getService(applicationContext, 999, restartIntent, piFlags)
            val alarmManager = applicationContext.getSystemService(Context.ALARM_SERVICE) as? AlarmManager
            alarmManager?.set(
                AlarmManager.ELAPSED_REALTIME_WAKEUP,
                SystemClock.elapsedRealtime() + 1000,
                pendingIntent
            )
        } catch (e: Exception) {
            Log.e(TAG, "Error scheduling alarm onTaskRemoved: ${e.message}")
        }
    }

    private fun acquireWakeLock() {
        try {
            if (wakeLock == null) {
                val powerManager = getSystemService(Context.POWER_SERVICE) as? PowerManager
                wakeLock = powerManager?.newWakeLock(
                    PowerManager.PARTIAL_WAKE_LOCK,
                    "VShield::BackgroundServiceWakeLock"
                )?.apply {
                    setReferenceCounted(false)
                }
            }
            if (wakeLock?.isHeld == false) {
                wakeLock?.acquire()
                Log.i(TAG, "WakeLock acquired for background WebSocket")
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error acquiring WakeLock: ${e.message}")
        }
    }

    private fun scheduleWatchdogAlarm() {
        try {
            val alarmManager = getSystemService(Context.ALARM_SERVICE) as? AlarmManager
            val intent = Intent(this, BootReceiver::class.java).apply {
                action = BootReceiver.ACTION_RESTART_SERVICE
            }
            val piFlags = PendingIntent.FLAG_UPDATE_CURRENT or (if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) PendingIntent.FLAG_IMMUTABLE else 0)
            val pendingIntent = PendingIntent.getBroadcast(this, 777, intent, piFlags)
            alarmManager?.setInexactRepeating(
                AlarmManager.ELAPSED_REALTIME_WAKEUP,
                SystemClock.elapsedRealtime() + 60000,
                300000, // 5 minutes
                pendingIntent
            )
        } catch (e: Exception) {
            Log.e(TAG, "Error scheduling watchdog alarm: ${e.message}")
        }
    }

    private fun releaseWakeLock() {
        try {
            if (wakeLock?.isHeld == true) {
                wakeLock?.release()
                Log.i(TAG, "WakeLock released")
            }
        } catch (e: Exception) {
            Log.e(TAG, "Error releasing WakeLock: ${e.message}")
        }
    }

    override fun onDestroy() {
        super.onDestroy()
        Log.i(TAG, "VShieldBackgroundService destroyed")
        isRunning = false

        releaseWakeLock()

        try {
            networkCallback?.let { connectivityManager?.unregisterNetworkCallback(it) }
        } catch (_: Exception) {}

        chatClient?.disconnect()
        chatClient = null

        notificationClient?.disconnect()
        notificationClient = null

        serviceScope.cancel()
    }

    override fun onBind(intent: Intent?): IBinder? = null
}
