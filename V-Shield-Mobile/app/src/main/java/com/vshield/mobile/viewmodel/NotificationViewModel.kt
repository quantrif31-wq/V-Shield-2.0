package com.vshield.mobile.viewmodel

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.vshield.mobile.BuildConfig
import com.vshield.mobile.data.NotificationSignalRClient
import com.vshield.mobile.data.RetrofitClient
import com.vshield.mobile.data.model.NotificationItem
import com.vshield.mobile.data.model.SecurityAlertItem
import com.vshield.mobile.data.model.SignalRNotification
import com.vshield.mobile.service.NotificationAlarmService
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import java.time.Instant

data class AlarmInfo(
    val id: String,
    val title: String,
    val message: String,
    val kindLabel: String,
    val requiresAck: Boolean,
    val ackKind: String? = null,
    val referenceId: Long? = null,
    val latitude: Double? = null,
    val longitude: Double? = null,
    val locationLabel: String? = null
)

data class NotificationFeedItem(
    val id: String,
    val source: String,
    val notificationId: Long? = null,
    val title: String,
    val message: String,
    val severity: String,
    val severityRank: Int,
    val sourceLabel: String,
    val createdAt: String?,
    val isRead: Boolean,
    val isActive: Boolean,
    val latitude: Double? = null,
    val longitude: Double? = null,
    val locationLabel: String? = null,
    val actionUrl: String? = null,
    val requiresAck: Boolean = false,
    val ackKind: String? = null,
    val referenceId: Long? = null
)

data class NotificationUiState(
    val isConnected: Boolean = false,
    val notifications: List<NotificationFeedItem> = emptyList(),
    val isLoading: Boolean = false,
    val unreadCount: Int = 0,
    val unreadNotificationCount: Int = 0,
    val securityAlertCount: Int = 0,
    val activeAlarm: AlarmInfo? = null,
    val error: String? = null
)

class NotificationViewModel(application: Application) : AndroidViewModel(application) {

    private val _uiState = MutableStateFlow(NotificationUiState())
    val uiState: StateFlow<NotificationUiState> = _uiState

    private var signalRClient: NotificationSignalRClient? = null
    private val alarmService = NotificationAlarmService(getApplication())
    private var securityAlertPollingJob: Job? = null

    private var initialized = false
    private var rawNotifications: List<NotificationItem> = emptyList()
    private var rawSecurityAlerts: List<SecurityAlertItem> = emptyList()

    fun initialize() {
        if (initialized) return

        val token = RetrofitClient.getToken()
        if (token == null) {
            _uiState.value = _uiState.value.copy(error = "Chưa đăng nhập")
            return
        }

        initialized = true
        if (!BuildConfig.DEMO_MODE) {
            com.vshield.mobile.service.VShieldBackgroundService.start(getApplication())

            com.vshield.mobile.service.VShieldBackgroundService.onNewNotification = { notif ->
                handleIncomingNotification(notif)
            }
            com.vshield.mobile.service.VShieldBackgroundService.onUnreadCountUpdated = { count ->
                _uiState.value = _uiState.value.copy(
                    unreadNotificationCount = count,
                    unreadCount = count + rawSecurityAlerts.size
                )
            }
            com.vshield.mobile.service.VShieldBackgroundService.onSyncEventApplied = {
                loadNotifications()
                loadUnreadCount()
            }
            com.vshield.mobile.service.VShieldBackgroundService.onNotificationConnectionChanged = { connected ->
                _uiState.value = _uiState.value.copy(isConnected = connected)
            }

            signalRClient = NotificationSignalRClient(BuildConfig.API_BASE_URL, token, viewModelScope)
            setupSignalRCallbacks()
            signalRClient?.connect()
        }

        loadNotifications()
        loadUnreadCount()
        startSecurityAlertPolling()
    }

    private fun setupSignalRCallbacks() {
        val client = signalRClient ?: return

        client.onConnectionChanged = { connected ->
            _uiState.value = _uiState.value.copy(isConnected = connected)
        }

        client.onSyncEventApplied = {
            loadNotifications()
            loadUnreadCount()
        }

        client.onNotificationReceived = { notif ->
            handleIncomingNotification(notif)
        }

        client.onUnreadCountUpdated = { count ->
            _uiState.value = _uiState.value.copy(
                unreadNotificationCount = count,
                unreadCount = count + rawSecurityAlerts.size
            )
        }
    }

    private fun handleIncomingNotification(notif: SignalRNotification) {
        val item = notif.toNotificationItem()
        rawNotifications = listOf(item) + rawNotifications.filterNot { it.id == item.id }
        recomputeState(preferIncomingAlert = notif.toAlarmInfo())

        alarmService.playNotificationOnce(item.title, item.body)
    }

    fun loadNotifications() {
        _uiState.value = _uiState.value.copy(isLoading = true)
        viewModelScope.launch {
            try {
                val resp = RetrofitClient.apiService.getNotifications()
                if (resp.isSuccessful && resp.body()?.success == true) {
                    rawNotifications = resp.body()?.data.orEmpty()
                    recomputeState()
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
                    val unread = resp.body()?.count ?: 0
                    _uiState.value = _uiState.value.copy(
                        unreadNotificationCount = unread,
                        unreadCount = unread + rawSecurityAlerts.size
                    )
                }
            } catch (_: Exception) {
            }
        }
    }

    fun markRead(notificationId: Long) {
        viewModelScope.launch {
            try {
                RetrofitClient.apiService.markNotificationRead(notificationId)
                rawNotifications = rawNotifications.map { item ->
                    if (item.id == notificationId) item.copy(isRead = true) else item
                }
                recomputeState()
            } catch (_: Exception) {
            }
        }
    }

    fun markAllRead() {
        viewModelScope.launch {
            try {
                RetrofitClient.apiService.markAllNotificationsRead()
                rawNotifications = rawNotifications.map { it.copy(isRead = true) }
                recomputeState()
            } catch (_: Exception) {
            }
        }
    }

    fun acknowledgeAlarm() {
        val alarm = _uiState.value.activeAlarm ?: return
        if (!alarm.requiresAck) {
            _uiState.value = _uiState.value.copy(activeAlarm = null)
            return
        }

        acknowledgeSecurityItem(alarm.ackKind, alarm.referenceId)
    }

    fun acknowledgeSecurityItem(ackKind: String?, referenceId: Long?) {
        if (ackKind.isNullOrBlank() || referenceId == null) return

        viewModelScope.launch {
            try {
                when (ackKind) {
                    "alarm" -> RetrofitClient.apiService.acknowledgeAlarm(referenceId)
                    "duress" -> RetrofitClient.apiService.acknowledgeDuressEvent(referenceId)
                }
                alarmService.stopAlarm()
                refreshSecurityAlerts()
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    error = "Không thể xác nhận xử lý: ${e.message}"
                )
            }
        }
    }

    fun clearError() {
        _uiState.value = _uiState.value.copy(error = null)
    }

    private fun startSecurityAlertPolling() {
        securityAlertPollingJob?.cancel()
        securityAlertPollingJob = viewModelScope.launch {
            while (true) {
                refreshSecurityAlerts()
                delay(10_000)
            }
        }
    }

    private suspend fun refreshSecurityAlerts() {
        try {
            val resp = RetrofitClient.apiService.getActiveSecurityAlerts()
            if (resp.isSuccessful) {
                rawSecurityAlerts = resp.body()?.items.orEmpty()
                recomputeState()
            } else if (resp.code() == 403 || resp.code() == 404) {
                rawSecurityAlerts = emptyList()
                recomputeState()
            }
        } catch (_: Exception) {
            rawSecurityAlerts = emptyList()
            recomputeState()
        }
    }

    private fun recomputeState(preferIncomingAlert: AlarmInfo? = null) {
        val feed = buildMergedFeed()
        val unreadNotificationCount = rawNotifications.count { !it.isRead }
        val securityAlertCount = rawSecurityAlerts.size
        val activeAlarm = preferIncomingAlert ?: feed.firstOrNull {
            it.severity == "critical" && it.isActive
        }?.toAlarmInfo()

        if (activeAlarm == null) {
            alarmService.stopAlarm()
        }

        _uiState.value = _uiState.value.copy(
            notifications = feed,
            isLoading = false,
            unreadNotificationCount = unreadNotificationCount,
            securityAlertCount = securityAlertCount,
            unreadCount = unreadNotificationCount + securityAlertCount,
            activeAlarm = activeAlarm
        )
    }

    private fun buildMergedFeed(): List<NotificationFeedItem> {
        val notificationItems = rawNotifications.map { it.toFeedItem() }
        val securityItems = rawSecurityAlerts.map { it.toFeedItem() }

        return (securityItems + notificationItems).sortedWith(
            compareByDescending<NotificationFeedItem> { it.severityRank }
                .thenByDescending { it.isActive }
                .thenBy { it.isRead }
                .thenByDescending { parseInstant(it.createdAt) }
        )
    }

    override fun onCleared() {
        super.onCleared()
        signalRClient?.disconnect()
        securityAlertPollingJob?.cancel()
        alarmService.stopAll()
    }
}

private fun SignalRNotification.toNotificationItem(): NotificationItem =
    NotificationItem(
        id = id,
        title = title,
        body = body,
        category = category,
        severity = severity,
        referenceType = referenceType,
        referenceId = referenceId,
        actionUrl = actionUrl,
        latitude = latitude,
        longitude = longitude,
        locationLabel = locationLabel,
        createdAt = createdAt,
        readAt = null,
        isRead = isRead
    )

private fun NotificationItem.toFeedItem(): NotificationFeedItem {
    val severityKey = normalizeNotificationSeverity(severity, category, referenceType, title, body)
    return NotificationFeedItem(
        id = "notification-$id",
        source = "notification",
        notificationId = id,
        title = title ?: "Thông báo",
        message = body ?: "",
        severity = severityKey,
        severityRank = severityKey.toSeverityRank(),
        sourceLabel = category.toSourceLabel(severityKey),
        createdAt = createdAt,
        isRead = isRead,
        isActive = !isRead,
        latitude = latitude,
        longitude = longitude,
        locationLabel = locationLabel,
        actionUrl = actionUrl
    )
}

private fun SecurityAlertItem.toFeedItem(): NotificationFeedItem {
    val parsedId = parsePrefixedId(id)
    val severityKey = mapSecuritySeverity(severity, kind)
    val ackKind = when {
        id.startsWith("alarm-") -> "alarm"
        id.startsWith("duress-") -> "duress"
        else -> null
    }

    return NotificationFeedItem(
        id = "security-$id",
        source = "security-alert",
        title = title ?: "Cảnh báo an ninh",
        message = message ?: "Hệ thống đang cần xử lý một tình huống an ninh.",
        severity = severityKey,
        severityRank = severityKey.toSeverityRank(),
        sourceLabel = when (ackKind) {
            "alarm" -> "Báo động SOC"
            "duress" -> "Báo động khẩn cấp"
            else -> "Điều phối an ninh"
        },
        createdAt = occurredAtUtc,
        isRead = false,
        isActive = true,
        locationLabel = locationLabel ?: zoneName,
        actionUrl = route,
        requiresAck = ackKind != null,
        ackKind = ackKind,
        referenceId = parsedId
    )
}

private fun NotificationFeedItem.toAlarmInfo(): AlarmInfo =
    AlarmInfo(
        id = id,
        title = title,
        message = message,
        kindLabel = sourceLabel,
        requiresAck = requiresAck,
        ackKind = ackKind,
        referenceId = referenceId,
        latitude = latitude,
        longitude = longitude,
        locationLabel = locationLabel
    )

private fun SignalRNotification.toAlarmInfo(): AlarmInfo? {
    val severityKey = normalizeNotificationSeverity(severity, category, referenceType, title, body)
    if (severityKey != "critical") return null

    return AlarmInfo(
        id = "notification-$id",
        title = title ?: "Cảnh báo khẩn cấp",
        message = body ?: "",
        kindLabel = category.toSourceLabel(severityKey),
        requiresAck = false,
        latitude = latitude,
        longitude = longitude,
        locationLabel = locationLabel
    )
}

private fun normalizeNotificationSeverity(
    severity: String?,
    category: String?,
    referenceType: String?,
    title: String?,
    body: String?
): String {
    val normalizedSeverity = severity?.lowercase()
    if (normalizedSeverity in setOf("success", "info", "caution", "warning", "critical")) {
        return normalizedSeverity!!
    }

    val categoryKey = category.orEmpty().lowercase()
    val referenceKey = referenceType.orEmpty().lowercase()
    val combinedText = "${title.orEmpty()} ${body.orEmpty()}".lowercase()

    if (categoryKey == "chat") return "success"
    if (categoryKey == "approval") return "caution"
    if (categoryKey == "alarm") {
        if (
            referenceKey == "alarm" &&
            (combinedText.contains("khẩn cấp") ||
                combinedText.contains("uy hiếp") ||
                combinedText.contains("đột nhập") ||
                combinedText.contains("duress") ||
                combinedText.contains("intrusion"))
        ) {
            return "critical"
        }
        return "warning"
    }

    return "info"
}

private fun mapSecuritySeverity(severity: String?, kind: String?): String {
    val severityKey = severity.orEmpty().lowercase()
    val kindKey = kind.orEmpty().lowercase()

    return when {
        severityKey == "critical" || kindKey.contains("duress") || kindKey.contains("emergency") || kindKey.contains("intrusion") -> "critical"
        severityKey == "high" -> "warning"
        severityKey == "medium" -> "caution"
        severityKey == "low" -> "info"
        severityKey == "warning" -> "warning"
        else -> "info"
    }
}

private fun String?.toSourceLabel(severity: String): String {
    val categoryKey = this.orEmpty().lowercase()
    return when {
        categoryKey == "chat" || severity == "success" -> "Trò chuyện"
        categoryKey == "approval" || severity == "caution" -> "Phê duyệt"
        categoryKey == "alarm" -> "Cảnh báo"
        else -> "Thông báo"
    }
}

private fun String.toSeverityRank(): Int = when (this.lowercase()) {
    "success" -> 1
    "info" -> 2
    "caution" -> 3
    "warning" -> 4
    "critical" -> 5
    else -> 2
}

private fun parsePrefixedId(value: String): Long? {
    val raw = value.substringAfter('-', "").ifBlank { return null }
    return raw.toLongOrNull()
}

private fun parseInstant(value: String?): Instant =
    try {
        value?.let { Instant.parse(it) } ?: Instant.EPOCH
    } catch (_: Exception) {
        Instant.EPOCH
    }
