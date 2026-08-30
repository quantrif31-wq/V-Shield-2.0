package com.vshield.mobile.ui.screen

import androidx.compose.animation.core.EaseInOutCubic
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Chat
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material.icons.filled.DirectionsCar
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.QrCodeScanner
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.graphicsLayer
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.vshield.mobile.ui.component.ErrorDialog
import com.vshield.mobile.ui.component.LoadingIndicator
import com.vshield.mobile.ui.component.QrCodeView
import com.vshield.mobile.ui.theme.Blue700
import com.vshield.mobile.ui.theme.Gray600
import com.vshield.mobile.ui.theme.Green600
import com.vshield.mobile.ui.theme.Red600
import com.vshield.mobile.ui.theme.SurfaceLight
import com.vshield.mobile.viewmodel.QrViewModel

private data class HomeShortcut(
    val title: String,
    val subtitle: String,
    val icon: androidx.compose.ui.graphics.vector.ImageVector,
    val tint: Color,
    val onClick: () -> Unit
)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun HomeScreen(
    onSessionExpired: () -> Unit,
    onOpenChat: () -> Unit,
    onOpenNotifications: () -> Unit,
    onOpenLeave: () -> Unit,
    onOpenTransfer: () -> Unit,
    onOpenProfile: () -> Unit,
    qrViewModel: QrViewModel = viewModel()
) {
    val uiState by qrViewModel.uiState.collectAsState()

    LaunchedEffect(Unit) {
        qrViewModel.startQrRefresh()
    }

    DisposableEffect(Unit) {
        onDispose { qrViewModel.stopQrRefresh() }
    }

    if (uiState.error != null) {
        ErrorDialog(
            title = "Lỗi mã QR",
            message = uiState.error!!,
            onDismiss = { qrViewModel.clearError() }
        )
    }

    val pulseAnim = rememberInfiniteTransition(label = "pulse")
    val pulseScale by pulseAnim.animateFloat(
        initialValue = 1f,
        targetValue = if (uiState.remainingSeconds <= 10) 1.04f else 1f,
        animationSpec = infiniteRepeatable(
            animation = tween(800, easing = EaseInOutCubic),
            repeatMode = RepeatMode.Reverse
        ),
        label = "pulse"
    )

    val shortcuts = remember(onOpenChat, onOpenNotifications, onOpenLeave, onOpenTransfer, onOpenProfile) {
        listOf(
            HomeShortcut("Trò chuyện", "Trò chuyện nội bộ", Icons.AutoMirrored.Filled.Chat, Color(0xFF2E7D32), onOpenChat),
            HomeShortcut("Thông báo", "Cảnh báo & Cập nhật", Icons.Filled.Notifications, Color(0xFFC62828), onOpenNotifications),
            HomeShortcut("Lịch & Nghỉ", "Đơn xin nghỉ phép", Icons.Filled.CalendarMonth, Color(0xFF0288D1), onOpenLeave),
            HomeShortcut("Bàn giao xe", "Ủy quyền phương tiện", Icons.Filled.DirectionsCar, Color(0xFFEF6C00), onOpenTransfer),
            HomeShortcut("Cá nhân", "Hồ sơ & Lịch làm việc", Icons.Filled.Person, Color(0xFF6A1B9A), onOpenProfile)
        )
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(SurfaceLight)
    ) {
        Surface(
            modifier = Modifier.fillMaxWidth(),
            color = Blue700,
            shadowElevation = 4.dp
        ) {
            Column(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(24.dp)
            ) {
                Text(
                    text = "Bảng điều khiển cá nhân",
                    style = MaterialTheme.typography.bodyLarge,
                    color = MaterialTheme.colorScheme.onPrimary.copy(alpha = 0.82f)
                )
                Text(
                    text = uiState.employeeName.ifEmpty { "Đang tải..." },
                    style = MaterialTheme.typography.headlineMedium,
                    color = MaterialTheme.colorScheme.onPrimary,
                    fontWeight = FontWeight.Bold
                )
                Spacer(modifier = Modifier.height(10.dp))
                Text(
                    text = if (uiState.isOfflineMode) {
                        "Ứng dụng đang giữ mã QR dự phòng trên máy. Khi kết nối máy chủ ổn định, hệ thống sẽ tự động đồng bộ trực tuyến."
                    } else {
                        "Từ đây bạn có thể mở mã QR, trò chuyện, nhận thông báo, xin nghỉ phép, bàn giao xe và quản lý hồ sơ."
                    },
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onPrimary.copy(alpha = 0.9f)
                )
            }
        }

        if (uiState.isLoading && uiState.qrData == null) {
            LoadingIndicator()
        } else {
            LazyVerticalGrid(
                columns = GridCells.Fixed(2),
                modifier = Modifier
                    .fillMaxSize()
                    .padding(16.dp),
                horizontalArrangement = Arrangement.spacedBy(12.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                item(span = { androidx.compose.foundation.lazy.grid.GridItemSpan(2) }) {
                    Card(
                        shape = RoundedCornerShape(24.dp),
                        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
                    ) {
                        Column(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(18.dp),
                            horizontalAlignment = Alignment.CenterHorizontally
                        ) {
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                horizontalArrangement = Arrangement.SpaceBetween,
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Column(modifier = Modifier.weight(1f)) {
                                    Text(
                                        text = if (uiState.isOfflineMode) "Mã QR ngoại tuyến" else "Mã QR động",
                                        style = MaterialTheme.typography.titleLarge,
                                        fontWeight = FontWeight.Bold
                                    )
                                    Text(
                                        text = if (uiState.isOfflineMode) {
                                            "Đang tự sinh mã QR trên máy từ cấu hình đã được cấp trước đó"
                                        } else {
                                            "Mã ra vào dành cho kiểm soát cổng an ninh"
                                        },
                                        style = MaterialTheme.typography.bodyMedium,
                                        color = Gray600
                                    )
                                }
                                Surface(
                                    shape = RoundedCornerShape(999.dp),
                                    color = when {
                                        uiState.isOfflineMode -> Color(0xFFFFF3E0)
                                        uiState.remainingSeconds <= 10 -> Color(0xFFFFEBEE)
                                        else -> Color(0xFFE8F5E9)
                                    }
                                ) {
                                    Text(
                                        text = if (uiState.isOfflineMode) {
                                            "Ngoại tuyến: ${uiState.remainingSeconds}s"
                                        } else {
                                            "Còn ${uiState.remainingSeconds}s"
                                        },
                                        modifier = Modifier.padding(horizontal = 12.dp, vertical = 8.dp),
                                        color = when {
                                            uiState.isOfflineMode -> Color(0xFFEF6C00)
                                            uiState.remainingSeconds <= 10 -> Red600
                                            else -> Green600
                                        },
                                        fontWeight = FontWeight.SemiBold
                                    )
                                }
                            }

                            uiState.statusMessage?.let { status ->
                                Spacer(modifier = Modifier.height(12.dp))
                                Text(
                                    text = status,
                                    style = MaterialTheme.typography.bodySmall,
                                    color = if (uiState.isOfflineMode) Color(0xFFEF6C00) else Gray600,
                                    modifier = Modifier.fillMaxWidth()
                                )
                            }

                            Spacer(modifier = Modifier.height(14.dp))

                            uiState.qrData?.let { qrData ->
                                QrCodeView(
                                    payload = qrData.qrPayload,
                                    modifier = Modifier
                                        .fillMaxWidth()
                                        .padding(horizontal = 8.dp)
                                        .graphicsLayer {
                                             scaleX = pulseScale
                                            scaleY = pulseScale
                                        }
                                )
                            }

                            Spacer(modifier = Modifier.height(12.dp))

                            OutlinedButton(
                                onClick = { qrViewModel.refreshNow() },
                                shape = RoundedCornerShape(12.dp)
                            ) {
                                Icon(Icons.Filled.QrCodeScanner, contentDescription = null)
                                Spacer(modifier = Modifier.width(8.dp))
                                Text(if (uiState.isOfflineMode) "Thử đồng bộ lại" else "Làm mới mã QR")
                            }
                        }
                    }
                }

                items(shortcuts) { shortcut ->
                    ShortcutCard(shortcut)
                }

                item(span = { androidx.compose.foundation.lazy.grid.GridItemSpan(2) }) {
                    Card(
                        shape = RoundedCornerShape(24.dp),
                        colors = CardDefaults.cardColors(containerColor = Color(0xFFF4F7FB))
                    ) {
                        Column(modifier = Modifier.padding(18.dp)) {
                            Text(
                                text = "Trạng thái ứng dụng",
                                style = MaterialTheme.typography.titleMedium,
                                fontWeight = FontWeight.Bold
                            )
                            Spacer(modifier = Modifier.height(8.dp))
                            Text(
                                text = if (uiState.isOfflineMode) {
                                    "Ứng dụng đang chạy ở chế độ ngoại tuyến. Bạn vẫn có thể mở ứng dụng và xuất mã QR bình thường."
                                } else {
                                    "Ứng dụng đang kết nối trực tuyến bình thường. Mã QR và dữ liệu đang được đồng bộ trực tiếp."
                                },
                                style = MaterialTheme.typography.bodyMedium,
                                color = Gray600
                            )
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun ShortcutCard(shortcut: HomeShortcut) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .height(140.dp)
            .clickable(onClick = shortcut.onClick),
        shape = RoundedCornerShape(22.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(16.dp),
            verticalArrangement = Arrangement.SpaceBetween
        ) {
            Surface(
                shape = RoundedCornerShape(16.dp),
                color = shortcut.tint.copy(alpha = 0.14f)
            ) {
                Icon(
                    imageVector = shortcut.icon,
                    contentDescription = null,
                    modifier = Modifier.padding(12.dp),
                    tint = shortcut.tint
                )
            }

            Column {
                Text(
                    text = shortcut.title,
                    style = MaterialTheme.typography.titleMedium,
                    fontWeight = FontWeight.Bold
                )
                Spacer(modifier = Modifier.height(4.dp))
                Text(
                    text = shortcut.subtitle,
                    style = MaterialTheme.typography.bodySmall,
                    color = Gray600,
                    textAlign = TextAlign.Start
                )
            }
        }
    }
}
