package com.vshield.mobile.viewmodel

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.vshield.mobile.BuildConfig
import com.vshield.mobile.data.ChatSignalRClient
import com.vshield.mobile.data.RetrofitClient
import com.vshield.mobile.data.TokenManager
import com.vshield.mobile.data.model.*
import com.vshield.mobile.webrtc.WebRTCManager
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import org.webrtc.IceCandidate
import org.webrtc.VideoTrack

data class ChatUiState(
    val isConnected: Boolean = false,
    val conversations: List<ConversationInfo> = emptyList(),
    val isLoadingConversations: Boolean = false,
    val currentConversation: ConversationInfo? = null,
    val currentConvId: Int = 0,
    val messages: List<ChatMessageInfo> = emptyList(),
    val isLoadingMessages: Boolean = false,
    val contacts: List<ContactInfo> = emptyList(),
    val typingUser: String? = null,
    val callState: ChatCallState = ChatCallState.Idle,
    val myEmployeeId: Int = 0,
    val localVideoTrack: VideoTrack? = null,
    val remoteVideoTrack: VideoTrack? = null,
    val isMicMuted: Boolean = false,
    val isCameraOff: Boolean = false,
    val isSpeakerOn: Boolean = true,
    val callError: String? = null,
    val error: String? = null
)

class ChatViewModel(application: Application) : AndroidViewModel(application) {

    private val _uiState = MutableStateFlow(ChatUiState())
    val uiState: StateFlow<ChatUiState> = _uiState

    private var signalRClient: ChatSignalRClient? = null
    private var typingJob: Job? = null
    private var webrtcManager: WebRTCManager? = null
    private var pendingIceCandidates = mutableListOf<IceCandidate>()
    private var pendingOfferSdp: String? = null

    private val activeSignalR: ChatSignalRClient?
        get() = com.vshield.mobile.service.VShieldBackgroundService.chatClient ?: signalRClient

    fun initialize() {
        val token = RetrofitClient.getToken()
        if (token == null) {
            _uiState.value = _uiState.value.copy(error = "Chưa đăng nhập")
            return
        }

        val empId = TokenManager(getApplication()).getEmployeeId()
        _uiState.value = _uiState.value.copy(myEmployeeId = empId)

        if (!BuildConfig.DEMO_MODE) {
            com.vshield.mobile.service.VShieldBackgroundService.start(getApplication())

            // Hook into background service listeners
            com.vshield.mobile.service.VShieldBackgroundService.onChatMessageReceived = { msg -> handleIncomingMessage(msg) }
            com.vshield.mobile.service.VShieldBackgroundService.onChatMessagesRead = { read -> handleMessagesRead(read) }
            com.vshield.mobile.service.VShieldBackgroundService.onChatUserTyping = { info -> handleUserTyping(info) }
            com.vshield.mobile.service.VShieldBackgroundService.onChatIncomingCall = { call -> handleIncomingCallSignal(call) }
            com.vshield.mobile.service.VShieldBackgroundService.onChatCallResponse = { resp -> handleCallResponseSignal(resp) }
            com.vshield.mobile.service.VShieldBackgroundService.onChatCallEnded = { ended -> handleCallEndedSignal(ended) }

            val baseUrl = BuildConfig.API_BASE_URL
            signalRClient = ChatSignalRClient(baseUrl, token, viewModelScope)
            setupSignalRCallbacks()
            signalRClient?.connect()
        } else {
            _uiState.value = _uiState.value.copy(isConnected = true)
        }
        loadConversations()
        loadContacts()
    }

    private fun setupSignalRCallbacks() {
        val client = signalRClient ?: return

        client.onMessageReceived = { msg -> handleIncomingMessage(msg) }
        client.onMessagesRead = { read -> handleMessagesRead(read) }
        client.onUserTyping = { info -> handleUserTyping(info) }
        client.onIncomingCall = { call -> handleIncomingCallSignal(call) }
        client.onCallResponse = { resp -> handleCallResponseSignal(resp) }
        client.onCallEnded = { ended -> handleCallEndedSignal(ended) }
    }

    private fun handleIncomingMessage(msg: SignalRReceiveMessage) {
        val current = _uiState.value
        if (msg.conversationId == current.currentConvId) {
            val newMsg = ChatMessageInfo(
                messageId = msg.messageId,
                senderId = msg.senderId,
                senderName = msg.fullName,
                content = msg.content,
                messageType = msg.messageType,
                signalingData = msg.signalingData,
                sentAt = msg.sentAt,
                isRead = msg.isRead,
                readAt = null
            )
            val mergedMessages = if (current.messages.any { it.messageId == newMsg.messageId }) {
                current.messages
            } else {
                current.messages + newMsg
            }
            _uiState.value = current.copy(messages = mergedMessages)
        }
        val updatedConvs = current.conversations.map { conv ->
            if (conv.conversationId == msg.conversationId) {
                val unread = if (msg.senderId != current.myEmployeeId && msg.conversationId != current.currentConvId)
                    conv.unreadCount + 1 else conv.unreadCount
                conv.copy(
                    lastMessage = LastMessageInfo(
                        messageId = msg.messageId,
                        content = msg.content,
                        sentAt = msg.sentAt,
                        messageType = msg.messageType,
                        senderName = msg.fullName,
                        senderId = msg.senderId
                    ),
                    unreadCount = unread
                )
            } else conv
        }
        _uiState.value = _uiState.value.copy(conversations = updatedConvs)
    }

    private fun handleMessagesRead(read: SignalRReadReceipt) {
        val current = _uiState.value
        if (read.conversationId == current.currentConvId) {
            val updatedMessages = current.messages.map { msg ->
                if (msg.senderId == current.myEmployeeId && !msg.isRead) {
                    msg.copy(isRead = true, readAt = read.readAt)
                } else msg
            }
            _uiState.value = current.copy(messages = updatedMessages)
        }
    }

    private fun handleUserTyping(info: SignalRTypingInfo) {
        if (info.employeeId != _uiState.value.myEmployeeId) {
            _uiState.value = _uiState.value.copy(typingUser = info.fullName)
            typingJob?.cancel()
            typingJob = viewModelScope.launch {
                delay(3000)
                _uiState.value = _uiState.value.copy(typingUser = null)
            }
        }
    }

    private fun handleCallEndedSignal(ended: SignalRCallEnded? = null) {
        com.vshield.mobile.service.NotificationHelper.cancelIncomingCallNotification(getApplication())
        val myId = _uiState.value.myEmployeeId
        val state = _uiState.value.callState

        android.util.Log.i("ChatViewModel", "handleCallEndedSignal: ended.fromEmployeeId=${ended?.fromEmployeeId}, myId=$myId, state=$state")

        // 1. If CallEnded was broadcasted because we accepted or rejected for ourselves, DO NOT close active call!
        if (ended != null && ended.fromEmployeeId == myId) {
            android.util.Log.i("ChatViewModel", "CallEnded ignored because it was triggered by myself ($myId)")
            return
        }

        // 2. If in Connected state, only end if remote party ended it
        if (state is ChatCallState.Connected) {
            if (ended != null && ended.fromEmployeeId != 0 && ended.fromEmployeeId != state.withEmployeeId) {
                android.util.Log.i("ChatViewModel", "CallEnded ignored because ended.fromEmployeeId (${ended.fromEmployeeId}) != withEmployeeId (${state.withEmployeeId})")
                return
            }
        }

        // 3. If in Outgoing state, only end if remote party ended it
        if (state is ChatCallState.Outgoing) {
            if (ended != null && ended.fromEmployeeId != 0 && ended.fromEmployeeId != state.toEmployeeId) {
                android.util.Log.i("ChatViewModel", "CallEnded ignored because ended.fromEmployeeId (${ended.fromEmployeeId}) != toEmployeeId (${state.toEmployeeId})")
                return
            }
        }

        // 4. If in Incoming state, only dismiss if caller cancelled
        if (state is ChatCallState.Incoming) {
            if (ended != null && ended.fromEmployeeId != 0 && ended.fromEmployeeId != state.fromEmployeeId) {
                android.util.Log.i("ChatViewModel", "CallEnded ignored because ended.fromEmployeeId (${ended.fromEmployeeId}) != incoming.fromEmployeeId (${state.fromEmployeeId})")
                return
            }
        }

        android.util.Log.i("ChatViewModel", "Ending call and returning to Idle state...")
        _uiState.value = _uiState.value.copy(callState = ChatCallState.Idle)
        closeWebRtc()
    }

    private fun handleIncomingCallSignal(call: SignalRCallInfo) {
        val current = _uiState.value
        when (call.signalingType) {
            "offer" -> {
                if (call.signalingData.isNullOrBlank()) return
                pendingOfferSdp = call.signalingData
                val isVideo = (call.signalingData ?: "").contains("m=video")
                val callType = if (isVideo) "video" else "audio"

                // Only show notification if background service is not running
                if (!com.vshield.mobile.service.VShieldBackgroundService.isRunning) {
                    com.vshield.mobile.service.NotificationHelper.showIncomingCallNotification(
                        context = getApplication(),
                        callType = callType,
                        fromEmployeeId = call.fromEmployeeId,
                        fromFullName = call.fromFullName ?: "Cuộc gọi đến",
                        conversationId = call.conversationId
                    )
                }

                _uiState.value = current.copy(
                    callState = ChatCallState.Incoming(
                        fromEmployeeId = call.fromEmployeeId,
                        fromFullName = call.fromFullName ?: "Cuộc gọi đến",
                        conversationId = call.conversationId,
                        offerSdp = call.signalingData,
                        callType = callType
                    )
                )
            }
            "ice" -> {
                val candidate = parseIceCandidate(call.signalingData)
                if (candidate != null) {
                    if (webrtcManager?.hasPeerConnection() == true) {
                        webrtcManager?.addIceCandidate(candidate)
                    } else {
                        pendingIceCandidates.add(candidate)
                    }
                }
            }
        }
    }

    private fun handleCallResponseSignal(resp: SignalRCallResponse) {
        when (resp.signalingType) {
            "answer" -> {
                resp.signalingData?.let { webrtcManager?.handleRemoteAnswer(it) }
            }
            "ice" -> {
                val candidate = parseIceCandidate(resp.signalingData)
                if (candidate != null) {
                    if (webrtcManager?.hasPeerConnection() == true) {
                        webrtcManager?.addIceCandidate(candidate)
                    } else {
                        pendingIceCandidates.add(candidate)
                    }
                }
            }
            "accepted" -> {
                val current = _uiState.value
                if (current.callState is ChatCallState.Outgoing) {
                    _uiState.value = current.copy(
                        callState = ChatCallState.Connected(
                            withEmployeeId = resp.fromEmployeeId,
                            withFullName = resp.fromFullName ?: current.callState.toFullName,
                            callType = current.callState.callType
                        )
                    )
                }
            }
        }
    }

    private fun parseIceCandidate(json: String?): IceCandidate? {
        if (json.isNullOrBlank()) return null
        return try {
            val element = com.google.gson.JsonParser.parseString(json)
            if (!element.isJsonObject) return null
            val obj = element.asJsonObject
            val sdpMid = if (obj.has("sdpMid") && !obj.get("sdpMid").isJsonNull) obj.get("sdpMid").asString else "0"
            val sdpMLineIndex = if (obj.has("sdpMLineIndex") && !obj.get("sdpMLineIndex").isJsonNull) {
                try { obj.get("sdpMLineIndex").asInt } catch (_: Exception) { 0 }
            } else 0
            val candidate = if (obj.has("candidate") && !obj.get("candidate").isJsonNull) obj.get("candidate").asString else return null
            if (candidate.isBlank()) return null
            IceCandidate(sdpMid, sdpMLineIndex, candidate)
        } catch (_: Throwable) {
            null
        }
    }

    private fun buildIceCandidateJson(candidate: IceCandidate): String {
        val obj = com.google.gson.JsonObject().apply {
            addProperty("sdpMid", candidate.sdpMid ?: "0")
            addProperty("sdpMLineIndex", candidate.sdpMLineIndex)
            addProperty("candidate", candidate.sdp)
        }
        return obj.toString()
    }

    fun loadConversations() {
        _uiState.value = _uiState.value.copy(isLoadingConversations = true)
        viewModelScope.launch {
            try {
                val resp = RetrofitClient.apiService.getConversations()
                if (resp.isSuccessful && resp.body()?.success == true) {
                    _uiState.value = _uiState.value.copy(
                        conversations = resp.body()!!.data ?: emptyList(),
                        isLoadingConversations = false
                    )
                } else {
                    _uiState.value = _uiState.value.copy(isLoadingConversations = false)
                }
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    isLoadingConversations = false,
                    error = "Lỗi tải hội thoại: ${e.message}"
                )
            }
        }
    }

    fun loadMessages(conversationId: Int) {
        _uiState.value = _uiState.value.copy(isLoadingMessages = true, currentConvId = conversationId)
        viewModelScope.launch {
            try {
                val resp = RetrofitClient.apiService.getConversationMessages(conversationId)
                if (resp.isSuccessful && resp.body()?.success == true) {
                    _uiState.value = _uiState.value.copy(
                        messages = resp.body()!!.data ?: emptyList(),
                        isLoadingMessages = false
                    )
                    activeSignalR?.markRead(conversationId)
                } else {
                    _uiState.value = _uiState.value.copy(isLoadingMessages = false)
                }
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    isLoadingMessages = false,
                    error = "Lỗi tải tin nhắn: ${e.message}"
                )
            }
        }
    }

    fun openConversation(conversationId: Int) {
        val conv = _uiState.value.conversations.find { it.conversationId == conversationId }
        _uiState.value = _uiState.value.copy(currentConversation = conv, currentConvId = conversationId)
        com.vshield.mobile.service.VShieldBackgroundService.currentOpenedConversationId = conversationId
        com.vshield.mobile.service.NotificationHelper.cancelMessageNotification(getApplication(), conversationId)
        loadMessages(conversationId)
        clearUnreadForConversation(conversationId)
        if (_uiState.value.conversations.isEmpty()) {
            loadConversations()
        }
    }

    fun setCurrentConversation(conversation: ConversationInfo?) {
        _uiState.value = _uiState.value.copy(currentConversation = conversation, currentConvId = conversation?.conversationId ?: 0)
        com.vshield.mobile.service.VShieldBackgroundService.currentOpenedConversationId = conversation?.conversationId
        if (conversation != null) {
            com.vshield.mobile.service.NotificationHelper.cancelMessageNotification(getApplication(), conversation.conversationId)
            loadMessages(conversation.conversationId)
            clearUnreadForConversation(conversation.conversationId)
        }
    }

    private fun clearUnreadForConversation(conversationId: Int) {
        val updated = _uiState.value.conversations.map { conv ->
            if (conv.conversationId == conversationId) conv.copy(unreadCount = 0) else conv
        }
        _uiState.value = _uiState.value.copy(conversations = updated)
    }

    fun sendMessage(content: String) {
        val convId = _uiState.value.currentConvId
        if (convId <= 0 || content.isBlank()) return
        viewModelScope.launch {
            try {
                val response = RetrofitClient.apiService.sendConversationMessage(
                    convId,
                    SendMessageRequest(content = content.trim())
                )

                if (!response.isSuccessful || response.body()?.success != true) {
                    _uiState.value = _uiState.value.copy(
                        error = response.body()?.message ?: "Không thể gửi tin nhắn"
                    )
                }
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    error = "Lỗi gửi tin nhắn: ${e.message}"
                )
            }
        }
    }

    fun sendTyping() {
        val convId = _uiState.value.currentConvId
        if (convId > 0) activeSignalR?.typing(convId)
    }

    fun loadContacts() {
        viewModelScope.launch {
            try {
                val resp = RetrofitClient.apiService.getChatContacts()
                if (resp.isSuccessful && resp.body()?.success == true) {
                    _uiState.value = _uiState.value.copy(
                        contacts = resp.body()!!.data ?: emptyList()
                    )
                }
            } catch (_: Exception) {}
        }
    }

    fun createConversation(employeeId: Int) {
        viewModelScope.launch {
            try {
                val resp = RetrofitClient.apiService.createConversation(
                    CreateConversationRequest(listOf(employeeId))
                )
                if (resp.isSuccessful && resp.body()?.success == true) {
                    loadConversations()
                } else {
                    _uiState.value = _uiState.value.copy(
                        error = resp.body()?.message ?: "Không thể tạo hội thoại"
                    )
                }
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    error = "Lỗi tạo hội thoại: ${e.message}"
                )
            }
        }
    }

    fun startCall(targetEmployeeId: Int, targetFullName: String, conversationId: Int?, type: String = "audio") {
        viewModelScope.launch {
            try {
                val current = _uiState.value
                _uiState.value = current.copy(
                    callState = ChatCallState.Outgoing(
                        toEmployeeId = targetEmployeeId,
                        toFullName = targetFullName,
                        conversationId = conversationId,
                        callType = type
                    ),
                    callError = null
                )

                if (BuildConfig.DEMO_MODE) {
                    activeSignalR?.callUser(targetEmployeeId, "offer", "demo-offer", conversationId)
                    return@launch
                }

                val isVideo = type == "video"
                val manager = ensureWebRtcManager()
                manager.initialize()
                configureIceServers(manager)
                if (!manager.createPeerConnection()) {
                    _uiState.value = _uiState.value.copy(callState = ChatCallState.Idle, callError = "Không thể khởi tạo kết nối thoại")
                    closeWebRtc()
                    return@launch
                }
                if (!manager.setupLocalMedia(enableVideo = isVideo)) {
                    _uiState.value = _uiState.value.copy(callState = ChatCallState.Idle, callError = "Không thể bật thiết bị âm thanh")
                    closeWebRtc()
                    return@launch
                }
                manager.createOffer(enableVideo = isVideo)
                pendingIceCandidates.clear()
            } catch (e: Throwable) {
                android.util.Log.e("ChatViewModel", "startCall CRASH: ${e.message}", e)
                _uiState.value = _uiState.value.copy(callState = ChatCallState.Idle, callError = "Lỗi cuộc gọi: ${e.message}")
                closeWebRtc()
            }
        }
    }

    fun acceptCall(
        fromEmployeeId: Int = 0,
        fromFullName: String = "",
        conversationId: Int = 0,
        callType: String = ""
    ) {
        viewModelScope.launch {
            try {
                var state = _uiState.value.callState
                if (state !is ChatCallState.Incoming) {
                    val bgCall = com.vshield.mobile.service.VShieldBackgroundService.lastIncomingCall
                    val empId = if (fromEmployeeId > 0) fromEmployeeId else (bgCall?.fromEmployeeId ?: 0)
                    val empName = if (fromFullName.isNotBlank()) fromFullName else (bgCall?.fromFullName ?: "Cuộc gọi đến")
                    val cId = if (conversationId > 0) conversationId else bgCall?.conversationId
                    val sdp = bgCall?.signalingData ?: pendingOfferSdp
                    val cType = if (callType.isNotBlank()) callType else if ((sdp ?: "").contains("m=video")) "video" else "audio"

                    if (empId > 0) {
                        state = ChatCallState.Incoming(
                            fromEmployeeId = empId,
                            fromFullName = empName,
                            conversationId = cId,
                            offerSdp = sdp,
                            callType = cType
                        )
                        _uiState.value = _uiState.value.copy(callState = state)
                    } else {
                        return@launch
                    }
                }
                val isVideo = state.callType == "video"

                // Always dismiss incoming call notification immediately
                com.vshield.mobile.service.NotificationHelper.cancelIncomingCallNotification(getApplication())

                // Immediately switch to Connected state so CallOverlay displays and stays on screen
                _uiState.value = _uiState.value.copy(
                    callState = ChatCallState.Connected(
                        withEmployeeId = state.fromEmployeeId,
                        withFullName = state.fromFullName,
                        callType = state.callType
                    )
                )

                // Acknowledge accepted state to remote caller
                activeSignalR?.callResponse(state.fromEmployeeId, "accepted", "")

                if (BuildConfig.DEMO_MODE) {
                    return@launch
                }

                val manager = ensureWebRtcManager()
                manager.initialize()
                configureIceServers(manager)
                manager.createPeerConnection()
                manager.setupLocalMedia(enableVideo = isVideo)

                pendingIceCandidates.forEach { manager.addIceCandidate(it) }
                pendingIceCandidates.clear()

                val offer = state.offerSdp ?: pendingOfferSdp
                if (offer != null && offer.isNotBlank()) {
                    manager.handleRemoteOffer(offer, enableVideo = isVideo)
                }
            } catch (e: Throwable) {
                com.vshield.mobile.service.NotificationHelper.cancelIncomingCallNotification(getApplication())
                android.util.Log.e("ChatViewModel", "acceptCall error: ${e.message}", e)
            }
        }
    }

    fun restoreIncomingCallState(
        fromEmployeeId: Int,
        fromFullName: String,
        conversationId: Int?,
        offerSdp: String?,
        callType: String
    ) {
        _uiState.value = _uiState.value.copy(
            callState = ChatCallState.Incoming(
                fromEmployeeId = fromEmployeeId,
                fromFullName = fromFullName,
                conversationId = conversationId,
                offerSdp = offerSdp,
                callType = callType
            )
        )
    }

    fun rejectCall() {
        val state = _uiState.value.callState
        if (state !is ChatCallState.Incoming) {
            return
        }
        com.vshield.mobile.service.NotificationHelper.cancelIncomingCallNotification(getApplication())
        activeSignalR?.callResponse(state.fromEmployeeId, "reject", "")
        com.vshield.mobile.service.VShieldBackgroundService.rejectCurrentCall()
        closeWebRtc()
        _uiState.value = _uiState.value.copy(callState = ChatCallState.Idle)
    }

    fun endCall() {
        com.vshield.mobile.service.NotificationHelper.cancelIncomingCallNotification(getApplication())
        val state = _uiState.value.callState
        when (state) {
            is ChatCallState.Incoming -> {
                activeSignalR?.endCall(state.fromEmployeeId, state.conversationId)
            }
            is ChatCallState.Outgoing -> {
                activeSignalR?.endCall(state.toEmployeeId, state.conversationId)
            }
            is ChatCallState.Connected -> {
                activeSignalR?.endCall(state.withEmployeeId, null)
            }
            else -> {}
        }
        closeWebRtc()
        _uiState.value = _uiState.value.copy(callState = ChatCallState.Idle)
    }

    fun toggleMic() {
        val muted = !_uiState.value.isMicMuted
        webrtcManager?.setAudioEnabled(!muted)
        _uiState.value = _uiState.value.copy(isMicMuted = muted)
    }

    fun toggleCamera() {
        val off = !_uiState.value.isCameraOff
        webrtcManager?.setVideoEnabled(!off)
        _uiState.value = _uiState.value.copy(isCameraOff = off)
    }

    fun toggleSpeaker() {
        val on = !_uiState.value.isSpeakerOn
        webrtcManager?.setSpeakerphoneOn(on)
        _uiState.value = _uiState.value.copy(isSpeakerOn = on)
    }

    fun switchCamera() {
        webrtcManager?.switchCamera()
    }

    fun clearCallError() {
        _uiState.value = _uiState.value.copy(callError = null)
    }

    private fun ensureWebRtcManager(): WebRTCManager {
        val existing = webrtcManager
        if (existing != null) return existing

        val manager = WebRTCManager(getApplication())
        webrtcManager = manager
        manager.listener = object : WebRTCManager.Listener {
            override fun onOfferCreated(sdp: String) {
                val current = _uiState.value
                val state = current.callState
                if (state is ChatCallState.Outgoing) {
                    activeSignalR?.callUser(state.toEmployeeId, "offer", sdp, state.conversationId)
                }
            }

            override fun onAnswerCreated(sdp: String) {
                val current = _uiState.value
                val state = current.callState
                val targetId = when (state) {
                    is ChatCallState.Incoming -> state.fromEmployeeId
                    is ChatCallState.Connected -> state.withEmployeeId
                    is ChatCallState.Outgoing -> state.toEmployeeId
                    else -> 0
                }
                if (targetId > 0) {
                    activeSignalR?.callResponse(targetId, "answer", sdp)
                }
            }

            override fun onIceCandidate(candidate: IceCandidate) {
                val current = _uiState.value
                val state = current.callState
                val json = buildIceCandidateJson(candidate)
                when (state) {
                    is ChatCallState.Outgoing -> {
                        activeSignalR?.callUser(state.toEmployeeId, "ice", json, state.conversationId)
                    }
                    is ChatCallState.Incoming -> {
                        activeSignalR?.callResponse(state.fromEmployeeId, "ice", json)
                    }
                    is ChatCallState.Connected -> {
                        activeSignalR?.callResponse(state.withEmployeeId, "ice", json)
                    }
                    else -> {}
                }
            }

            override fun onLocalVideo(videoTrack: VideoTrack) {
                _uiState.value = _uiState.value.copy(localVideoTrack = videoTrack)
            }

            override fun onRemoteVideo(videoTrack: VideoTrack) {
                _uiState.value = _uiState.value.copy(remoteVideoTrack = videoTrack)
            }

            override fun onConnectionStateChanged(state: String) {
                val current = _uiState.value
                if (state == "CONNECTED" || state == "COMPLETED") {
                    when (val cs = current.callState) {
                        is ChatCallState.Outgoing -> {
                            _uiState.value = current.copy(
                                callState = ChatCallState.Connected(
                                    withEmployeeId = cs.toEmployeeId,
                                    withFullName = cs.toFullName
                                )
                            )
                        }
                        is ChatCallState.Incoming -> {
                            _uiState.value = current.copy(
                                callState = ChatCallState.Connected(
                                    withEmployeeId = cs.fromEmployeeId,
                                    withFullName = cs.fromFullName
                                )
                            )
                        }
                        else -> {}
                    }
                }
            }

            override fun onError(message: String) {
                _uiState.value = _uiState.value.copy(callError = message)
            }
        }
        return manager
    }

    private suspend fun configureIceServers(manager: WebRTCManager) {
        try {
            val response = RetrofitClient.apiService.getRealtimeIceConfiguration()
            val iceServers = response.body()?.iceServers.orEmpty()
                .filter { it.urls.isNotEmpty() }
                .map { WebRTCManager.IceServerConfig(it.urls, it.username, it.credential) }
            manager.updateIceServers(iceServers)
        } catch (error: Throwable) {
            // A STUN-only fallback still permits same-network calls when central is unavailable.
            android.util.Log.w("ChatViewModel", "Cannot load TURN configuration: ${error.message}")
        }
    }

    private fun closeWebRtc() {
        try {
            webrtcManager?.close()
        } catch (e: Throwable) {
            android.util.Log.w("ChatViewModel", "closeWebRtc error: ${e.message}")
        }
        webrtcManager = null
        pendingIceCandidates.clear()
        pendingOfferSdp = null
        _uiState.value = _uiState.value.copy(
            localVideoTrack = null,
            remoteVideoTrack = null,
            isMicMuted = false,
            isCameraOff = false,
            isSpeakerOn = true
        )
    }

    fun totalUnreadCount(): Int = _uiState.value.conversations.sumOf { it.unreadCount }

    fun clearError() {
        _uiState.value = _uiState.value.copy(error = null)
    }

    override fun onCleared() {
        super.onCleared()
        com.vshield.mobile.service.VShieldBackgroundService.onChatMessageReceived = null
        com.vshield.mobile.service.VShieldBackgroundService.onChatMessagesRead = null
        com.vshield.mobile.service.VShieldBackgroundService.onChatUserTyping = null
        com.vshield.mobile.service.VShieldBackgroundService.onChatIncomingCall = null
        com.vshield.mobile.service.VShieldBackgroundService.onChatCallResponse = null
        com.vshield.mobile.service.VShieldBackgroundService.onChatCallEnded = null
        signalRClient?.disconnect()
    }
}
