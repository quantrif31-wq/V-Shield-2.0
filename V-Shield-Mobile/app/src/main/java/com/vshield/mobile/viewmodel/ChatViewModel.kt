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

    fun initialize() {
        val token = RetrofitClient.getToken()
        if (token == null) {
            _uiState.value = _uiState.value.copy(error = "Chưa đăng nhập")
            return
        }

        val empId = TokenManager(getApplication()).getEmployeeId()
        _uiState.value = _uiState.value.copy(myEmployeeId = empId)

        if (!BuildConfig.DEMO_MODE) {
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

        client.onMessageReceived = { msg ->
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

        client.onMessagesRead = { read ->
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

        client.onUserTyping = { info ->
            if (info.employeeId != _uiState.value.myEmployeeId) {
                _uiState.value = _uiState.value.copy(typingUser = info.fullName)
                typingJob?.cancel()
                typingJob = viewModelScope.launch {
                    delay(3000)
                    _uiState.value = _uiState.value.copy(typingUser = null)
                }
            }
        }

        client.onIncomingCall = { call ->
            handleIncomingCallSignal(call)
        }

        client.onCallResponse = { resp ->
            handleCallResponseSignal(resp)
        }

        client.onCallEnded = {
            _uiState.value = _uiState.value.copy(callState = ChatCallState.Idle)
            closeWebRtc()
        }
    }

    private fun handleIncomingCallSignal(call: SignalRCallInfo) {
        val current = _uiState.value
        when (call.signalingType) {
            "offer" -> {
                pendingOfferSdp = call.signalingData
                _uiState.value = current.copy(
                    callState = ChatCallState.Incoming(
                        fromEmployeeId = call.fromEmployeeId,
                        fromFullName = call.fromFullName ?: "Cuộc gọi đến",
                        conversationId = call.conversationId,
                        offerSdp = call.signalingData
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
                            withFullName = resp.fromFullName ?: current.callState.toFullName
                        )
                    )
                }
            }
        }
    }

    private fun parseIceCandidate(json: String?): IceCandidate? {
        if (json.isNullOrBlank()) return null
        return try {
            val obj = com.google.gson.JsonParser.parseString(json).asJsonObject
            val sdpMid = obj.get("sdpMid")?.asString ?: "0"
            val sdpMLineIndex = obj.get("sdpMLineIndex")?.asInt ?: 0
            val candidate = obj.get("candidate")?.asString ?: return null
            IceCandidate(sdpMid, sdpMLineIndex, candidate)
        } catch (_: Exception) {
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
                    signalRClient?.markRead(conversationId)
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

    fun setCurrentConversation(conversation: ConversationInfo?) {
        _uiState.value = _uiState.value.copy(currentConversation = conversation)
        if (conversation != null) {
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
        if (convId > 0) signalRClient?.typing(convId)
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

    fun startCall(targetEmployeeId: Int, targetFullName: String, conversationId: Int?) {
        val current = _uiState.value
        _uiState.value = current.copy(
            callState = ChatCallState.Outgoing(
                toEmployeeId = targetEmployeeId,
                toFullName = targetFullName,
                conversationId = conversationId
            ),
            callError = null
        )

        if (BuildConfig.DEMO_MODE) {
            signalRClient?.callUser(targetEmployeeId, "offer", "demo-offer", conversationId)
            return
        }

        val manager = ensureWebRtcManager()
        manager.initialize()
        if (!manager.createPeerConnection()) {
            _uiState.value = _uiState.value.copy(callError = "Không thể khởi tạo peer")
            return
        }
        if (!manager.setupLocalMedia()) {
            _uiState.value = _uiState.value.copy(callError = "Không thể bật camera/mic")
            return
        }
        manager.createOffer()
        pendingIceCandidates.clear()
        signalRClient?.callUser(targetEmployeeId, "offer", "", conversationId)
    }

    fun acceptCall() {
        val state = _uiState.value.callState
        if (state !is ChatCallState.Incoming) return

        if (BuildConfig.DEMO_MODE) {
            signalRClient?.callResponse(state.fromEmployeeId, "accepted", "")
            _uiState.value = _uiState.value.copy(
                callState = ChatCallState.Connected(
                    withEmployeeId = state.fromEmployeeId,
                    withFullName = state.fromFullName
                )
            )
            return
        }

        val manager = ensureWebRtcManager()
        manager.initialize()
        if (!manager.createPeerConnection()) {
            _uiState.value = _uiState.value.copy(callError = "Không thể khởi tạo peer")
            return
        }
        if (!manager.setupLocalMedia()) {
            _uiState.value = _uiState.value.copy(callError = "Không thể bật camera/mic")
            return
        }

        pendingIceCandidates.forEach { manager.addIceCandidate(it) }
        pendingIceCandidates.clear()

        val offer = state.offerSdp ?: pendingOfferSdp
        if (offer != null && offer.isNotBlank()) {
            manager.handleRemoteOffer(offer)
            signalRClient?.callResponse(state.fromEmployeeId, "accepted", "")
            _uiState.value = _uiState.value.copy(
                callState = ChatCallState.Connected(
                    withEmployeeId = state.fromEmployeeId,
                    withFullName = state.fromFullName
                )
            )
        } else {
            _uiState.value = _uiState.value.copy(callError = "Không nhận được offer")
        }
    }

    fun rejectCall() {
        val state = _uiState.value.callState
        if (state is ChatCallState.Incoming) {
            signalRClient?.callResponse(state.fromEmployeeId, "reject", "")
        }
        closeWebRtc()
        _uiState.value = _uiState.value.copy(callState = ChatCallState.Idle)
    }

    fun endCall() {
        val state = _uiState.value.callState
        when (state) {
            is ChatCallState.Incoming -> {
                signalRClient?.endCall(state.fromEmployeeId, state.conversationId)
            }
            is ChatCallState.Outgoing -> {
                signalRClient?.endCall(state.toEmployeeId, state.conversationId)
            }
            is ChatCallState.Connected -> {
                signalRClient?.endCall(state.withEmployeeId, null)
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
                    signalRClient?.callUser(state.toEmployeeId, "offer", sdp, state.conversationId)
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
                    signalRClient?.callResponse(targetId, "answer", sdp)
                }
            }

            override fun onIceCandidate(candidate: IceCandidate) {
                val current = _uiState.value
                val state = current.callState
                val json = buildIceCandidateJson(candidate)
                when (state) {
                    is ChatCallState.Outgoing -> {
                        signalRClient?.callUser(state.toEmployeeId, "ice", json, state.conversationId)
                    }
                    is ChatCallState.Incoming -> {
                        signalRClient?.callResponse(state.fromEmployeeId, "ice", json)
                    }
                    is ChatCallState.Connected -> {
                        signalRClient?.callResponse(state.withEmployeeId, "ice", json)
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

    private fun closeWebRtc() {
        webrtcManager?.close()
        webrtcManager = null
        pendingIceCandidates.clear()
        pendingOfferSdp = null
        _uiState.value = _uiState.value.copy(
            localVideoTrack = null,
            remoteVideoTrack = null,
            isMicMuted = false,
            isCameraOff = false
        )
    }

    fun totalUnreadCount(): Int = _uiState.value.conversations.sumOf { it.unreadCount }

    fun clearError() {
        _uiState.value = _uiState.value.copy(error = null)
    }

    override fun onCleared() {
        super.onCleared()
        signalRClient?.disconnect()
    }
}
