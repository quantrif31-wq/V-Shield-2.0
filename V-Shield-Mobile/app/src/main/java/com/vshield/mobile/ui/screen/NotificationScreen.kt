package com.vshield.mobile.ui.screen

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Circle
import androidx.compose.material.icons.filled.DoneAll
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.vshield.mobile.data.model.NotificationItem
import com.vshield.mobile.ui.component.ErrorDialog
import com.vshield.mobile.ui.theme.*
import com.vshield.mobile.viewmodel.NotificationViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun NotificationScreen(
    onSessionExpired: () -> Unit,
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
                if (uiState.unreadCount > 0) {
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
                        "Không có thông báo",
                        color = MaterialTheme.colorScheme.onSurfaceVariant.copy(alpha = 0.6f)
                    )
                }
            }
        } else {
            LazyColumn(
                modifier = Modifier.fillMaxSize(),
                contentPadding = PaddingValues(horizontal = 12.dp, vertical = 8.dp),
                verticalArrangement = Arrangement.spacedBy(6.dp)
            ) {
                items(uiState.notifications, key = { it.notificationId }) { item ->
                    NotificationCard(
                        notification = item,
                        onClick = { notificationViewModel.markRead(item.notificationId) }
                    )
                }
            }
        }
    }
}

@Composable
private fun NotificationCard(
    notification: NotificationItem,
    onClick: () -> Unit
) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onClick),
        shape = RoundedCornerShape(14.dp),
        colors = CardDefaults.cardColors(
            containerColor = if (notification.isRead)
                MaterialTheme.colorScheme.surface
            else
                MaterialTheme.colorScheme.primaryContainer.copy(alpha = 0.3f)
        ),
        elevation = CardDefaults.cardElevation(defaultElevation = 1.dp)
    ) {
        Row(
            modifier = Modifier.padding(14.dp),
            verticalAlignment = Alignment.Top
        ) {
            if (!notification.isRead) {
                Icon(
                    Icons.Filled.Circle,
                    contentDescription = null,
                    modifier = Modifier
                        .size(10.dp)
                        .offset(y = 6.dp),
                    tint = MaterialTheme.colorScheme.primary
                )
                Spacer(Modifier.width(10.dp))
            }

            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = notification.title ?: "Thông báo",
                    fontWeight = if (notification.isRead) FontWeight.Normal else FontWeight.Bold,
                    fontSize = 14.sp
                )
                if (notification.message != null) {
                    Spacer(Modifier.height(4.dp))
                    Text(
                        text = notification.message,
                        fontSize = 13.sp,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                        maxLines = 2
                    )
                }
                Spacer(Modifier.height(6.dp))
                Text(
                    text = formatTime(notification.createdAt),
                    fontSize = 11.sp,
                    color = MaterialTheme.colorScheme.onSurfaceVariant.copy(alpha = 0.6f)
                )
            }
        }
    }
}

private fun formatTime(dateStr: String?): String {
    if (dateStr == null) return ""
    return try {
        val fmt = java.text.SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", java.util.Locale.US)
        fmt.timeZone = java.util.TimeZone.getTimeZone("UTC")
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
