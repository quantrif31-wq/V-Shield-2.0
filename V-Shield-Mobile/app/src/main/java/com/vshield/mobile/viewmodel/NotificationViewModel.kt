package com.vshield.mobile.viewmodel

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.vshield.mobile.BuildConfig
import com.vshield.mobile.data.NotificationSignalRClient
import com.vshield.mobile.data.RetrofitClient
import com.vshield.mobile.data.TokenManager
import com.vshield.mobile.data.model.NotificationItem
import com.vshield.mobile.service.NotificationAlarmService
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

data class AlarmInfo(
    val notificationId: Int,
    val title: String,
    val message: String,
    val eventType: String
)

data class NotificationUiState(
    val isConnected: Boolean = false,
    val notifications: List<NotificationItem> = emptyList(),
    val isLoading: Boolean = false,
    val unreadCount: Int = 0,
    val activeAlarm: AlarmInfo? = null,
    val error: String? = null
)

class NotificationViewModel(application: Application) : AndroidViewModel(application) {

    private val _uiState = MutableStateFlow(NotificationUiState())
    val uiState: StateFlow<NotificationUiState> = _uiState

    private var signalRClient: NotificationSignalRClient? = null
    private val alarmService = NotificationAlarmService(getApplication())

    fun initialize() {
        val token = RetrofitClient.getToken()
        if (token == null) {
            _uiState.value = _uiState.value.copy(error = "Chưa đăng nhập")
            return
        }

        val baseUrl = BuildConfig.API_BASE_URL
        signalRClient = NotificationSignalRClient(baseUrl, token, viewModelScope)
        setupSignalRCallbacks()
        signalRClient?.connect()
        loadNotifications()
        loadUnreadCount()
    }

    private fun setupSignalRCallbacks() {
        val client = signalRClient ?: return

        client.onNotificationReceived = { notif ->
            val item = NotificationItem(
                notificationId = notif.notificationId,
                eventType = notif.eventType,
                title = notif.title,
                message = notif.message,
                entityType = notif.entityType,
                entityId = notif.entityId,
                actionUrl = notif.actionUrl,
                createdAt = notif.createdAt,
                isRead = false
            )
            _uiState.value = _uiState.value.copy(
                notifications = listOf(item) + _uiState.value.notifications,
                unreadCount = _uiState.value.unreadCount + 1
            )

            val isAlarm = notif.eventType?.startsWith("Alarm") == true
            val title = notif.title ?: if (isAlarm) "Báo động khẩn cấp" else "Thông báo mới"
            val message = notif.message ?: ""

            if (isAlarm) {
                alarmService.startAlarm(title, message)
                _uiState.value = _uiState.value.copy(
                    activeAlarm = AlarmInfo(
                        notificationId = notif.notificationId,
                        title = title,
                        message = message,
                        eventType = notif.eventType ?: ""
                    )
                )
            } else {
                alarmService.playNotificationOnce(title, message)
            }
        }

        client.onUnreadCountUpdated = { count ->
            _uiState.value = _uiState.value.copy(unreadCount = count)
        }
    }

    fun loadNotifications() {
        _uiState.value = _uiState.value.copy(isLoading = true)
        viewModelScope.launch {
            try {
                val resp = RetrofitClient.apiService.getNotifications()
                if (resp.isSuccessful && resp.body()?.success == true) {
                    _uiState.value = _uiState.value.copy(
                        notifications = resp.body()!!.data ?: emptyList(),
                        isLoading = false
                    )
                } else {
                    _uiState.value = _uiState.value.copy(isLoading = false)
                }
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    error = "Lỗi tải thông báo: ${e.message}"
                )
            }
        }
    }

    fun loadUnreadCount() {
        viewModelScope.launch {
            try {
                val resp = RetrofitClient.apiService.getUnreadCount()
                if (resp.isSuccessful && resp.body()?.success == true) {
                    _uiState.value = _uiState.value.copy(
                        unreadCount = resp.body()!!.data?.count ?: 0
                    )
                }
            } catch (_: Exception) {}
        }
    }

    fun markRead(notificationId: Int) {
        viewModelScope.launch {
            try {
                RetrofitClient.apiService.markNotificationRead(notificationId)
                val updated = _uiState.value.notifications.map { n ->
                    if (n.notificationId == notificationId) n.copy(isRead = true) else n
                }
                _uiState.value = _uiState.value.copy(
                    notifications = updated,
                    unreadCount = (updated.count { !it.isRead })
                )
            } catch (_: Exception) {}
        }
    }

    fun markAllRead() {
        viewModelScope.launch {
            try {
                RetrofitClient.apiService.markAllNotificationsRead()
                _uiState.value = _uiState.value.copy(
                    notifications = _uiState.value.notifications.map { it.copy(isRead = true) },
                    unreadCount = 0
                )
            } catch (_: Exception) {}
        }
    }

    fun acknowledgeAlarm() {
        val alarm = _uiState.value.activeAlarm ?: return
        alarmService.stopAlarm()
        markRead(alarm.notificationId)
        _uiState.value = _uiState.value.copy(activeAlarm = null)
    }

    fun clearError() {
        _uiState.value = _uiState.value.copy(error = null)
    }

    override fun onCleared() {
        super.onCleared()
        signalRClient?.disconnect()
        alarmService.stopAll()
    }
}
