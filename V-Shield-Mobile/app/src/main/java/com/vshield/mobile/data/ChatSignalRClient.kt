package com.vshield.mobile.data

import com.google.gson.Gson
import com.google.gson.JsonParser
import com.vshield.mobile.data.model.*
import kotlinx.coroutines.*
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import okhttp3.*
import java.util.concurrent.TimeUnit

class ChatSignalRClient(
    private val baseUrl: String,
    private val accessToken: String,
    private val scope: CoroutineScope
) {
    private val gson = Gson()
    private val rs = "\u001E"

    private var webSocket: WebSocket? = null
    private var okHttpClient: OkHttpClient? = null
    private var connectionToken: String? = null
    private var reconnectAttempt = 0
    private var shouldReconnect = true
    private var isNegotiated = false

    private val _connectionState = MutableStateFlow(ConnectionState.DISCONNECTED)
    val connectionState: StateFlow<ConnectionState> = _connectionState

    var onMessageReceived: ((SignalRReceiveMessage) -> Unit)? = null
    var onMessagesRead: ((SignalRReadReceipt) -> Unit)? = null
    var onUserTyping: ((SignalRTypingInfo) -> Unit)? = null
    var onIncomingCall: ((SignalRCallInfo) -> Unit)? = null
    var onCallResponse: ((SignalRCallResponse) -> Unit)? = null
    var onCallEnded: ((SignalRCallEnded) -> Unit)? = null

    enum class ConnectionState {
        DISCONNECTED, CONNECTING, CONNECTED, RECONNECTING
    }

    fun connect() {
        if (_connectionState.value == ConnectionState.CONNECTED ||
            _connectionState.value == ConnectionState.CONNECTING
        ) return

        shouldReconnect = true
        _connectionState.value = ConnectionState.CONNECTING
        scope.launch(Dispatchers.IO) {
            doNegotiateAndConnect()
        }
    }

    private fun doNegotiateAndConnect() {
        try {
            val negotiateUrl = baseUrl.trimEnd('/') + "/hubs/chat/negotiate?negotiateVersion=1"
            val client = OkHttpClient.Builder()
                .connectTimeout(10, TimeUnit.SECONDS)
                .readTimeout(10, TimeUnit.SECONDS)
                .build()

            val request = Request.Builder()
                .url(negotiateUrl)
                .addHeader("Authorization", "Bearer $accessToken")
                .post(RequestBody.create(null, ""))
                .build()

            val response = client.newCall(request).execute()
            if (!response.isSuccessful) {
                _connectionState.value = ConnectionState.DISCONNECTED
                return
            }

            val body = response.body?.string() ?: run {
                _connectionState.value = ConnectionState.DISCONNECTED
                return
            }

            val json = com.google.gson.JsonParser.parseString(body).asJsonObject
            connectionToken = json.get("connectionToken")?.asString
                ?: json.get("connectionId")?.asString

            if (connectionToken == null) {
                _connectionState.value = ConnectionState.DISCONNECTED
                return
            }

            connectWebSocket()
        } catch (e: Exception) {
            _connectionState.value = ConnectionState.DISCONNECTED
            scheduleReconnect()
        }
    }

    private fun connectWebSocket() {
        val wsUrl = baseUrl
            .replace("http://", "ws://")
            .replace("https://", "wss://")
            .trimEnd('/') + "/hubs/chat?id=$connectionToken&access_token=$accessToken"

        val client = OkHttpClient.Builder()
            .readTimeout(0, TimeUnit.MILLISECONDS)
            .build()
        okHttpClient = client

        val request = Request.Builder()
            .url(wsUrl)
            .build()

        webSocket = client.newWebSocket(request, object : WebSocketListener() {
            override fun onOpen(webSocket: WebSocket, response: Response) {
                isNegotiated = false
                reconnectAttempt = 0
                webSocket.send("{\"protocol\":\"json\",\"version\":1}$rs")
            }

            override fun onMessage(webSocket: WebSocket, text: String) {
                handleSignalRMessage(text)
            }

            override fun onClosing(webSocket: WebSocket, code: Int, reason: String) {
                webSocket.close(1000, null)
            }

            override fun onClosed(webSocket: WebSocket, code: Int, reason: String) {
                if (_connectionState.value == ConnectionState.CONNECTED) {
                    _connectionState.value = ConnectionState.DISCONNECTED
                    if (shouldReconnect) scheduleReconnect()
                }
            }

            override fun onFailure(webSocket: WebSocket, t: Throwable, response: Response?) {
                if (_connectionState.value != ConnectionState.DISCONNECTED) {
                    _connectionState.value = ConnectionState.DISCONNECTED
                    if (shouldReconnect) scheduleReconnect()
                }
            }
        })
    }

    private fun handleSignalRMessage(text: String) {
        val messages = text.split(rs).filter { it.isNotBlank() }
        for (msg in messages) {
            try {
                if (!isNegotiated) {
                    isNegotiated = true
                    _connectionState.value = ConnectionState.CONNECTED
                    return
                }

                val json = com.google.gson.JsonParser.parseString(msg).asJsonObject
                val type = json.get("type")?.asInt ?: continue

                when (type) {
                    1 -> handleInvocation(json)
                    6 -> {}
                    7 -> {}
                }
            } catch (_: Exception) {}
        }
    }

    private fun handleInvocation(json: com.google.gson.JsonObject) {
        val target = json.get("target")?.asString ?: return
        val args = json.getAsJsonArray("arguments") ?: return
        if (args.size() == 0) return

        when (target) {
            "ReceiveMessage" -> {
                val data = gson.fromJson(args[0], SignalRReceiveMessage::class.java)
                scope.launch { onMessageReceived?.invoke(data) }
            }
            "MessagesRead" -> {
                val data = gson.fromJson(args[0], SignalRReadReceipt::class.java)
                scope.launch { onMessagesRead?.invoke(data) }
            }
            "UserTyping" -> {
                val data = gson.fromJson(args[0], SignalRTypingInfo::class.java)
                scope.launch { onUserTyping?.invoke(data) }
            }
            "IncomingCall" -> {
                val data = gson.fromJson(args[0], SignalRCallInfo::class.java)
                scope.launch { onIncomingCall?.invoke(data) }
            }
            "CallResponse" -> {
                val data = gson.fromJson(args[0], SignalRCallResponse::class.java)
                scope.launch { onCallResponse?.invoke(data) }
            }
            "CallEnded" -> {
                val data = gson.fromJson(args[0], SignalRCallEnded::class.java)
                scope.launch { onCallEnded?.invoke(data) }
            }
        }
    }

    fun sendMessage(conversationId: Int, content: String, messageType: String = "Text", signalingData: String? = null) {
        val args = gson.toJsonTree(listOf(conversationId, content, messageType, signalingData)).asJsonArray
        sendHubMessage("SendMessage", args)
    }

    fun markRead(conversationId: Int) {
        val args = gson.toJsonTree(listOf(conversationId)).asJsonArray
        sendHubMessage("MarkRead", args)
    }

    fun typing(conversationId: Int) {
        val args = gson.toJsonTree(listOf(conversationId)).asJsonArray
        sendHubMessage("Typing", args)
    }

    fun callUser(targetEmployeeId: Int, signalingType: String, signalingData: String, conversationId: Int?) {
        val args = gson.toJsonTree(listOf(targetEmployeeId, signalingType, signalingData, conversationId)).asJsonArray
        sendHubMessage("CallUser", args)
    }

    fun callResponse(targetEmployeeId: Int, signalingType: String, signalingData: String) {
        val args = gson.toJsonTree(listOf(targetEmployeeId, signalingType, signalingData)).asJsonArray
        sendHubMessage("CallResponse", args)
    }

    fun endCall(targetEmployeeId: Int, conversationId: Int?) {
        val args = gson.toJsonTree(listOf(targetEmployeeId, conversationId)).asJsonArray
        sendHubMessage("EndCall", args)
    }

    private fun sendHubMessage(target: String, arguments: com.google.gson.JsonArray) {
        val payload = gson.toJson(mapOf(
            "type" to 1,
            "target" to target,
            "arguments" to arguments
        )) + rs
        webSocket?.send(payload)
    }

    private fun scheduleReconnect() {
        if (!shouldReconnect) return
        val delays = listOf(0L, 2000L, 5000L, 10000L, 30000L)
        val delay = delays.getOrElse(reconnectAttempt) { 30000L }
        reconnectAttempt++

        scope.launch {
            delay(delay)
            if (shouldReconnect) {
                _connectionState.value = ConnectionState.RECONNECTING
                doNegotiateAndConnect()
            }
        }
    }

    fun disconnect() {
        shouldReconnect = false
        webSocket?.close(1000, "Client disconnecting")
        webSocket = null
        okHttpClient?.dispatcher?.executorService?.shutdown()
        _connectionState.value = ConnectionState.DISCONNECTED
    }
}
