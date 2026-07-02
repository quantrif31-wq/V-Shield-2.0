package com.vshield.mobile.viewmodel

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.vshield.mobile.BuildConfig
import com.vshield.mobile.data.ChatSignalRClient
import com.vshield.mobile.data.RetrofitClient
import com.vshield.mobile.data.TokenManager
import com.vshield.mobile.data.model.*
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

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
    val error: String? = null
)

class ChatViewModel(application: Application) : AndroidViewModel(application) {

    private val _uiState = MutableStateFlow(ChatUiState())
    val uiState: StateFlow<ChatUiState> = _uiState

    private var signalRClient: ChatSignalRClient? = null
    private var typingJob: Job? = null

    fun initialize() {
        val token = RetrofitClient.getToken()
        if (token == null) {
            _uiState.value = _uiState.value.copy(error = "Chưa đăng nhập")
            return
        }

        val empId = TokenManager(getApplication()).getEmployeeId()
        _uiState.value = _uiState.value.copy(myEmployeeId = empId)

        val baseUrl = BuildConfig.API_BASE_URL
        signalRClient = ChatSignalRClient(baseUrl, token, viewModelScope)
        setupSignalRCallbacks()
        signalRClient?.connect()
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
            if (call.fromFullName != null) {
                _uiState.value = _uiState.value.copy(
                    callState = ChatCallState.Incoming(
                        fromEmployeeId = call.fromEmployeeId,
                        fromFullName = call.fromFullName,
                        conversationId = call.conversationId
                    )
                )
            }
        }

        client.onCallResponse = { resp ->
            val current = _uiState.value
            if (current.callState is ChatCallState.Outgoing) {
                _uiState.value = current.copy(
                    callState = ChatCallState.Connected(
                        withEmployeeId = resp.fromEmployeeId,
                        withFullName = resp.fromFullName ?: ""
                    )
                )
            }
        }

        client.onCallEnded = {
            _uiState.value = _uiState.value.copy(callState = ChatCallState.Idle)
        }
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
        _uiState.value = _uiState.value.copy(
            callState = ChatCallState.Outgoing(
                toEmployeeId = targetEmployeeId,
                toFullName = targetFullName,
                conversationId = conversationId
            )
        )
        signalRClient?.callUser(targetEmployeeId, "offer", "{\"type\":\"offer\"}", conversationId)
    }

    fun acceptCall() {
        val state = _uiState.value.callState
        if (state is ChatCallState.Incoming) {
            _uiState.value = _uiState.value.copy(
                callState = ChatCallState.Connected(
                    withEmployeeId = state.fromEmployeeId,
                    withFullName = state.fromFullName
                )
            )
            signalRClient?.callResponse(state.fromEmployeeId, "answer", "{\"type\":\"answer\"}")
        }
    }

    fun rejectCall() {
        val state = _uiState.value.callState
        if (state is ChatCallState.Incoming) {
            signalRClient?.callResponse(state.fromEmployeeId, "reject", "")
            _uiState.value = _uiState.value.copy(callState = ChatCallState.Idle)
        }
    }

    fun endCall() {
        val state = _uiState.value.callState
        when (state) {
            is ChatCallState.Incoming -> {
                signalRClient?.endCall(state.fromEmployeeId, state.conversationId)
                _uiState.value = _uiState.value.copy(callState = ChatCallState.Idle)
            }
            is ChatCallState.Outgoing -> {
                signalRClient?.endCall(state.toEmployeeId, state.conversationId)
                _uiState.value = _uiState.value.copy(callState = ChatCallState.Idle)
            }
            is ChatCallState.Connected -> {
                signalRClient?.endCall(state.withEmployeeId, null)
                _uiState.value = _uiState.value.copy(callState = ChatCallState.Idle)
            }
            else -> {}
        }
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
