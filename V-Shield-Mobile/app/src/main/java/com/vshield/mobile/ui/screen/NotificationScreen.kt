package com.vshield.mobile.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Circle
import androidx.compose.material.icons.filled.DoneAll
import androidx.compose.material.icons.filled.LocationOn
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.vshield.mobile.ui.component.ErrorDialog
import com.vshield.mobile.viewmodel.NotificationFeedItem
import com.vshield.mobile.viewmodel.NotificationViewModel
import java.text.SimpleDateFormat
import java.util.Locale
import java.util.TimeZone

@OptIn(ExperimentalMaterial3Api::class)
@Composable
@Suppress("UNUSED_PARAMETER")
fun NotificationScreen(
    onSessionExpired: () -> Unit,
    onViewMap: ((Double, Double, String?) -> Unit)? = null,
    notificationViewModel: NotificationViewModel = viewModel()
) {
    val uiState by notificationViewModel.uiState.collectAsState()

    LaunchedEffect(Unit) {
        notificationViewModel.initialize()
    }

    if (uiState.error != null) {
        ErrorDialog(
            title = "Lỗi",
            message = uiState.error!!,
            onDismiss = { notificationViewModel.clearError() }
        )
    }

    Column(modifier = Modifier.fillMaxSize()) {
        TopAppBar(
            title = { Text("Thông báo", fontWeight = FontWeight.Bold) },
            actions = {
                if (uiState.unreadNotificationCount > 0) {
                    TextButton(onClick = { notificationViewModel.markAllRead() }) {
                        Icon(Icons.Filled.DoneAll, contentDescription = null, modifier = Modifier.size(18.dp))
                        Spacer(Modifier.width(4.dp))
                        Text("Đọc tất cả", fontSize = 13.sp)
                    }
                }
            },
            colors = TopAppBarDefaults.topAppBarColors(
                containerColor = MaterialTheme.colorScheme.surface
            )
        )

        SummaryRow(
            pendingCount = uiState.unreadCount,
            unreadNotificationCount = uiState.unreadNotificationCount,
            securityAlertCount = uiState.securityAlertCount,
            isConnected = uiState.isConnected
        )

        if (uiState.isLoading && uiState.notifications.isEmpty()) {
            Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                CircularProgressIndicator()
            }
        } else if (uiState.notifications.isEmpty()) {
            Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    Icon(
                        Icons.Filled.Notifications,
                        contentDescription = null,
                        modifier = Modifier.size(64.dp),
                        tint = MaterialTheme.colorScheme.onSurfaceVariant.copy(alpha = 0.4f)
                    )
                    Spacer(Modifier.height(12.dp))
                    Text(
                        "Chưa có mục nào cần xử lý",
                        color = MaterialTheme.colorScheme.onSurfaceVariant.copy(alpha = 0.6f)
                    )
                }
            }
        } else {
            LazyColumn(
                modifier = Modifier.fillMaxSize(),
                contentPadding = PaddingValues(horizontal = 12.dp, vertical = 8.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                items(uiState.notifications, key = { it.id }) { item ->
                    NotificationCard(
                        notification = item,
                        onMarkRead = {
                            item.notificationId?.let(notificationViewModel::markRead)
                        },
                        onAcknowledge = {
                            notificationViewModel.acknowledgeSecurityItem(item.ackKind, item.referenceId)
                        },
                        onViewMap = if (item.latitude != null && item.longitude != null) {
                            { onViewMap?.invoke(item.latitude, item.longitude, item.locationLabel) }
                        } else {
                            null
                        }
                    )
                }
            }
        }
    }
}

@Composable
private fun SummaryRow(
    pendingCount: Int,
    unreadNotificationCount: Int,
    securityAlertCount: Int,
    isConnected: Boolean
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 12.dp, vertical = 8.dp),
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        SummaryChip(
            label = "$pendingCount cần xử lý",
            color = MaterialTheme.colorScheme.primaryContainer,
            textColor = MaterialTheme.colorScheme.onPrimaryContainer,
            modifier = Modifier.weight(1f)
        )
        SummaryChip(
            label = "$unreadNotificationCount chưa đọc",
            color = Color(0xFFE3F2FD),
            textColor = Color(0xFF1565C0)
        )
        SummaryChip(
            label = "$securityAlertCount cảnh báo",
            color = Color(0xFFFFF3E0),
            textColor = Color(0xFFEF6C00)
        )
        SummaryChip(
            label = if (isConnected) "Trực tuyến" else "Mất kết nối",
            color = if (isConnected) Color(0xFFE8F5E9) else Color(0xFFFFEBEE),
            textColor = if (isConnected) Color(0xFF2E7D32) else Color(0xFFC62828)
        )
    }
}

@Composable
private fun SummaryChip(
    label: String,
    color: Color,
    textColor: Color,
    modifier: Modifier = Modifier
) {
    Surface(
        modifier = modifier,
        shape = RoundedCornerShape(999.dp),
        color = color
    ) {
        Text(
            text = label,
            color = textColor,
            fontSize = 12.sp,
            fontWeight = FontWeight.SemiBold,
            modifier = Modifier.padding(horizontal = 10.dp, vertical = 8.dp)
        )
    }
}

@Composable
private fun NotificationCard(
    notification: NotificationFeedItem,
    onMarkRead: () -> Unit,
    onAcknowledge: () -> Unit,
    onViewMap: (() -> Unit)? = null
) {
    val severityColor = severityColor(notification.severity)
    val backgroundColor = if (notification.isRead && notification.source == "notification") {
        MaterialTheme.colorScheme.surface
    } else {
        severityColor.copy(alpha = 0.08f)
    }

    Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(18.dp),
        colors = CardDefaults.cardColors(containerColor = backgroundColor),
        elevation = CardDefaults.cardElevation(defaultElevation = 1.dp)
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .clickable(enabled = notification.source == "notification" && !notification.isRead) { onMarkRead() }
        ) {
            Box(
                modifier = Modifier
                    .width(6.dp)
                    .background(severityColor)
                    .height(150.dp)
            )

            Column(modifier = Modifier.padding(14.dp)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    if (!notification.isRead && notification.source == "notification") {
                        Icon(
                            Icons.Filled.Circle,
                            contentDescription = null,
                            modifier = Modifier
                                .size(10.dp)
                                .offset(y = 1.dp),
                            tint = severityColor
                        )
                        Spacer(Modifier.width(8.dp))
                    }

                    SeverityPill(
                        label = severityLabel(notification.severity),
                        background = severityColor.copy(alpha = 0.15f),
                        textColor = severityColor
                    )
                    Spacer(Modifier.width(8.dp))
                    SeverityPill(
                        label = notification.sourceLabel,
                        background = MaterialTheme.colorScheme.surfaceVariant,
                        textColor = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }

                Spacer(Modifier.height(10.dp))

                Text(
                    text = notification.title,
                    fontWeight = FontWeight.Bold,
                    fontSize = 15.sp
                )

                if (notification.message.isNotBlank()) {
                    Spacer(Modifier.height(4.dp))
                    Text(
                        text = notification.message,
                        fontSize = 13.sp,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }

                if (!notification.locationLabel.isNullOrBlank()) {
                    Spacer(Modifier.height(8.dp))
                    Text(
                        text = notification.locationLabel,
                        fontSize = 12.sp,
                        color = severityColor,
                        fontWeight = FontWeight.Medium
                    )
                }

                Spacer(Modifier.height(10.dp))

                Row(
                    modifier = Modifier.fillMaxWidth(),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = formatTime(notification.createdAt),
                        fontSize = 11.sp,
                        color = MaterialTheme.colorScheme.onSurfaceVariant.copy(alpha = 0.7f),
                        modifier = Modifier.weight(1f)
                    )

                    if (onViewMap != null) {
                        IconButton(
                            onClick = onViewMap,
                            modifier = Modifier.size(28.dp)
                        ) {
                            Icon(
                                Icons.Filled.LocationOn,
                                contentDescription = "Xem bản đồ",
                                tint = severityColor,
                                modifier = Modifier.size(18.dp)
                            )
                        }
                    }
                }

                Spacer(Modifier.height(10.dp))

                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    if (notification.requiresAck) {
                        Button(
                            onClick = onAcknowledge,
                            colors = ButtonDefaults.buttonColors(containerColor = severityColor),
                            shape = RoundedCornerShape(12.dp)
                        ) {
                            Text("Xác nhận xử lý")
                        }
                    } else if (notification.source == "notification" && !notification.isRead) {
                        OutlinedButton(
                            onClick = onMarkRead,
                            shape = RoundedCornerShape(12.dp)
                        ) {
                            Text("Đánh dấu đã đọc")
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun SeverityPill(
    label: String,
    background: Color,
    textColor: Color
) {
    Surface(
        shape = RoundedCornerShape(999.dp),
        color = background
    ) {
        Text(
            text = label,
            color = textColor,
            fontWeight = FontWeight.SemiBold,
            fontSize = 11.sp,
            modifier = Modifier.padding(horizontal = 10.dp, vertical = 6.dp)
        )
    }
}

private fun severityColor(severity: String): Color = when (severity) {
    "success" -> Color(0xFF2E7D32)
    "info" -> Color(0xFF0288D1)
    "caution" -> Color(0xFFF9A825)
    "warning" -> Color(0xFFEF6C00)
    "critical" -> Color(0xFFC62828)
    else -> Color(0xFF455A64)
}

private fun severityLabel(severity: String): String = when (severity) {
    "success" -> "Thành công"
    "info" -> "Thông tin"
    "caution" -> "Lưu ý"
    "warning" -> "Cảnh báo"
    "critical" -> "Khẩn cấp"
    else -> "Thông tin"
}

private fun formatTime(dateStr: String?): String {
    if (dateStr == null) return ""
    return try {
        val fmt = SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", Locale.US)
        fmt.timeZone = TimeZone.getTimeZone("UTC")
        val parsed = fmt.parse(dateStr.take(19)) ?: return dateStr.take(10)
        val diff = System.currentTimeMillis() - parsed.time
        val mins = diff / 60000
        when {
            mins < 1 -> "Vừa xong"
            mins < 60 -> "${mins} phút trước"
            mins < 1440 -> "${mins / 60} giờ trước"
            else -> "${mins / 1440} ngày trước"
        }
    } catch (_: Exception) {
        dateStr.take(10)
    }
}
