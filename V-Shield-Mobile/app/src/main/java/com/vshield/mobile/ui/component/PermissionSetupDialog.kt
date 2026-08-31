package com.vshield.mobile.ui.component

import android.app.Activity
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.Dialog
import androidx.compose.ui.window.DialogProperties
import com.vshield.mobile.security.PermissionManager
import com.vshield.mobile.security.PermissionStatus
import com.vshield.mobile.ui.theme.*

@Composable
fun PermissionSetupDialog(
    status: PermissionStatus,
    onRefreshStatus: () -> Unit,
    onDismiss: () -> Unit
) {
    val context = LocalContext.current
    val activity = context as? Activity
    val scrollState = rememberScrollState()

    val runtimeLauncher = rememberLauncherForActivityResult(
        contract = ActivityResultContracts.RequestMultiplePermissions()
    ) {
        onRefreshStatus()
    }

    Dialog(
        onDismissRequest = onDismiss,
        properties = DialogProperties(usePlatformDefaultWidth = false)
    ) {
        Card(
            modifier = Modifier
                .fillMaxWidth(0.92f)
                .fillMaxHeight(0.85f),
            shape = RoundedCornerShape(20.dp),
            colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
        ) {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(20.dp),
                verticalArrangement = Arrangement.SpaceBetween
            ) {
                // Header
                Column(modifier = Modifier.fillMaxWidth()) {
                    Row(
                        verticalAlignment = Alignment.CenterVertically,
                        horizontalArrangement = Arrangement.SpaceBetween,
                        modifier = Modifier.fillMaxWidth()
                    ) {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Surface(
                                shape = RoundedCornerShape(10.dp),
                                color = if (status.isAllGranted) Green50 else Amber50
                            ) {
                                Icon(
                                    imageVector = if (status.isAllGranted) Icons.Filled.CheckCircle else Icons.Filled.Security,
                                    contentDescription = null,
                                    tint = if (status.isAllGranted) Green600 else Amber600,
                                    modifier = Modifier.padding(8.dp)
                                )
                            }
                            Spacer(modifier = Modifier.width(12.dp))
                            Column {
                                Text(
                                    text = "Cấp quyền ứng dụng V-Shield",
                                    style = MaterialTheme.typography.titleMedium,
                                    fontWeight = FontWeight.Bold
                                )
                                Text(
                                    text = if (status.isAllGranted) "Tất cả quyền đã sẵn sàng 100%" else "Còn thiếu ${status.missingCount} quyền cần kích hoạt",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = if (status.isAllGranted) Green600 else Gray600
                                )
                            }
                        }
                        IconButton(onClick = onDismiss) {
                            Icon(Icons.Filled.Close, contentDescription = "Đóng")
                        }
                    }

                    Spacer(modifier = Modifier.height(8.dp))
                    Text(
                        text = "Để đàm thoại Video Call, nhận cuộc gọi khi tắt màn hình và quét mã thông hành mượt mà, vui lòng cấp đủ các quyền bên dưới:",
                        style = MaterialTheme.typography.bodySmall,
                        color = Gray600
                    )
                }

                Spacer(modifier = Modifier.height(12.dp))

                // Scrollable Permission Checklist
                Column(
                    modifier = Modifier
                        .weight(1f)
                        .verticalScroll(scrollState),
                    verticalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    // 1. Camera & Micro
                    PermissionItemRow(
                        icon = Icons.Filled.CameraAlt,
                        title = "Camera & Microphone",
                        desc = "Quét mã QR, Face ID & gọi đàm thoại Video VoIP",
                        isGranted = status.hasCamera && status.hasAudio,
                        onGrant = {
                            runtimeLauncher.launch(PermissionManager.getRequiredRuntimePermissions().toTypedArray())
                        }
                    )

                    // 2. Notifications
                    PermissionItemRow(
                        icon = Icons.Filled.NotificationsActive,
                        title = "Thông báo & Chuông cuộc gọi",
                        desc = "Đổ chuông khi có cuộc gọi và cảnh báo khẩn cấp",
                        isGranted = status.hasNotifications,
                        onGrant = {
                            runtimeLauncher.launch(PermissionManager.getRequiredRuntimePermissions().toTypedArray())
                        }
                    )

                    // 3. Location
                    PermissionItemRow(
                        icon = Icons.Filled.LocationOn,
                        title = "Vị trí thiết bị (GPS)",
                        desc = "Định vị vị trí trạm an ninh & bản đồ khuôn viên",
                        isGranted = status.hasLocation,
                        onGrant = {
                            runtimeLauncher.launch(PermissionManager.getRequiredRuntimePermissions().toTypedArray())
                        }
                    )

                    // 4. Draw Over Other Apps (Overlay)
                    PermissionItemRow(
                        icon = Icons.Filled.PictureInPicture,
                        title = "Hiển thị trên ứng dụng khác",
                        desc = "Bật màn hình nhận cuộc gọi ngay cả khi đang dùng app khác",
                        isGranted = status.hasOverlay,
                        onGrant = {
                            PermissionManager.requestOverlayPermission(context)
                        }
                    )

                    // 5. Battery Optimization Exemption
                    PermissionItemRow(
                        icon = Icons.Filled.BatteryChargingFull,
                        title = "Không giới hạn Pin (Chạy ngầm)",
                        desc = "Ngăn hệ điều hành ngắt kết nối WebSocket/SignalR",
                        isGranted = status.isIgnoringBattery,
                        onGrant = {
                            activity?.let { PermissionManager.requestBatteryOptimization(it) }
                        }
                    )

                    // 6. OEM Autostart (Xiaomi, Samsung, Oppo...)
                    Card(
                        modifier = Modifier.fillMaxWidth(),
                        shape = RoundedCornerShape(12.dp),
                        colors = CardDefaults.cardColors(containerColor = Blue50)
                    ) {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .padding(12.dp),
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.SpaceBetween
                        ) {
                            Column(modifier = Modifier.weight(1f)) {
                                Text(
                                    text = "Cài đặt Tự khởi chạy máy (Xiaomi / Oppo / Samsung)",
                                    style = MaterialTheme.typography.bodyMedium,
                                    fontWeight = FontWeight.SemiBold,
                                    color = Blue900
                                )
                                Text(
                                    text = "Mở cài đặt của hãng để bật tính năng tự khởi chạy ngầm",
                                    style = MaterialTheme.typography.bodySmall,
                                    color = Blue700
                                )
                            }
                            OutlinedButton(
                                onClick = { PermissionManager.openAutostartSettings(context) },
                                colors = ButtonDefaults.outlinedButtonColors(contentColor = Blue700)
                            ) {
                                Text("Mở")
                            }
                        }
                    }
                }

                Spacer(modifier = Modifier.height(16.dp))

                // Bottom Action Buttons
                Column(
                    modifier = Modifier.fillMaxWidth(),
                    verticalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    Button(
                        onClick = {
                            runtimeLauncher.launch(PermissionManager.getRequiredRuntimePermissions().toTypedArray())
                            if (!status.hasOverlay) {
                                PermissionManager.requestOverlayPermission(context)
                            } else if (!status.isIgnoringBattery && activity != null) {
                                PermissionManager.requestBatteryOptimization(activity)
                            }
                        },
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(48.dp),
                        shape = RoundedCornerShape(12.dp),
                        colors = ButtonDefaults.buttonColors(
                            containerColor = if (status.isAllGranted) Green600 else Blue700
                        )
                    ) {
                        Icon(
                            imageVector = if (status.isAllGranted) Icons.Filled.Check else Icons.Filled.VerifiedUser,
                            contentDescription = null,
                            modifier = Modifier.size(18.dp)
                        )
                        Spacer(modifier = Modifier.width(8.dp))
                        Text(if (status.isAllGranted) "Hoàn tất cấu hình" else "CẤP TẤT CẢ QUYỀN TỰ ĐỘNG")
                    }

                    if (status.isAllGranted) {
                        OutlinedButton(
                            onClick = onDismiss,
                            modifier = Modifier
                                .fillMaxWidth()
                                .height(44.dp),
                            shape = RoundedCornerShape(12.dp)
                        ) {
                            Text("Bắt đầu sử dụng")
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun PermissionItemRow(
    icon: ImageVector,
    title: String,
    desc: String,
    isGranted: Boolean,
    onGrant: () -> Unit
) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        shape = RoundedCornerShape(12.dp),
        colors = CardDefaults.cardColors(
            containerColor = if (isGranted) Green50.copy(alpha = 0.5f) else MaterialTheme.colorScheme.surfaceVariant
        )
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(12.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.SpaceBetween
        ) {
            Surface(
                shape = CircleShape,
                color = if (isGranted) Green500 else Gray400,
                modifier = Modifier.size(36.dp)
            ) {
                Icon(
                    imageVector = icon,
                    contentDescription = null,
                    tint = Color.White,
                    modifier = Modifier.padding(8.dp)
                )
            }

            Spacer(modifier = Modifier.width(12.dp))

            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = title,
                    style = MaterialTheme.typography.bodyMedium,
                    fontWeight = FontWeight.SemiBold
                )
                Text(
                    text = desc,
                    style = MaterialTheme.typography.bodySmall,
                    color = Gray600
                )
            }

            Spacer(modifier = Modifier.width(8.dp))

            if (isGranted) {
                Icon(
                    imageVector = Icons.Filled.CheckCircle,
                    contentDescription = "Đã cấp",
                    tint = Green600,
                    modifier = Modifier.size(24.dp)
                )
            } else {
                Button(
                    onClick = onGrant,
                    contentPadding = PaddingValues(horizontal = 12.dp, vertical = 6.dp),
                    shape = RoundedCornerShape(8.dp),
                    colors = ButtonDefaults.buttonColors(containerColor = Blue700)
                ) {
                    Text("Cấp", style = MaterialTheme.typography.labelMedium)
                }
            }
        }
    }
}
