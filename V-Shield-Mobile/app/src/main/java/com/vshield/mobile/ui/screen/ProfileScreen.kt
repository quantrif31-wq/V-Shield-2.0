package com.vshield.mobile.ui.screen

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Logout
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Email
import androidx.compose.material.icons.filled.Fingerprint
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Phone
import androidx.compose.material.icons.filled.PowerSettingsNew
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Security
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Surface
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.fragment.app.FragmentActivity
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import androidx.lifecycle.compose.LocalLifecycleOwner
import androidx.lifecycle.viewmodel.compose.viewModel
import com.vshield.mobile.data.model.ScheduleItem
import com.vshield.mobile.security.BiometricType
import com.vshield.mobile.security.toDisplayText
import com.vshield.mobile.service.AutoStartHelper
import com.vshield.mobile.ui.component.LoadingIndicator
import com.vshield.mobile.ui.theme.Blue50
import com.vshield.mobile.ui.theme.Blue700
import com.vshield.mobile.ui.theme.Gray200
import com.vshield.mobile.ui.theme.Gray400
import com.vshield.mobile.ui.theme.Gray600
import com.vshield.mobile.ui.theme.Red600
import com.vshield.mobile.viewmodel.AuthViewModel
import com.vshield.mobile.viewmodel.ProfileViewModel

@Composable
fun ProfileScreen(
    onSessionExpired: () -> Unit,
    profileViewModel: ProfileViewModel = viewModel(),
    authViewModel: AuthViewModel = viewModel()
) {
    val uiState by profileViewModel.uiState.collectAsState()
    val authState by authViewModel.uiState.collectAsState()
    val activity = LocalContext.current as? FragmentActivity
    val lifecycleOwner = LocalLifecycleOwner.current

    var biometricSelection by remember(authState.showBiometricSetupDialog, authState.biometricCapabilities) {
        mutableStateOf(
            authState.enabledBiometricTypes.ifEmpty { authState.biometricCapabilities.map { it.type }.toSet() }
        )
    }

    LaunchedEffect(Unit) {
        profileViewModel.loadData()
    }

    val context = LocalContext.current
    var isAutoStartEnabled by remember { mutableStateOf(AutoStartHelper.isAutoStartEnabled(context)) }
    var isIgnoringBattery by remember { mutableStateOf(AutoStartHelper.isIgnoringBatteryOptimizations(context)) }

    DisposableEffect(lifecycleOwner, activity, authState.awaitingBiometricEnrollment) {
        val observer = LifecycleEventObserver { _, event ->
            if (event == Lifecycle.Event.ON_RESUME) {
                isIgnoringBattery = AutoStartHelper.isIgnoringBatteryOptimizations(context)
                if (authState.awaitingBiometricEnrollment) {
                    authViewModel.onBiometricEnrollmentSettingsReturned(activity)
                }
            }
        }
        lifecycleOwner.lifecycle.addObserver(observer)
        onDispose {
            lifecycleOwner.lifecycle.removeObserver(observer)
        }
    }

    if (authState.showBiometricSetupDialog) {
        AlertDialog(
            onDismissRequest = { authViewModel.dismissBiometricSetupDialog() },
            title = { Text("Quản lý đăng nhập nhanh") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                    Text("Bạn có thể bật một hoặc nhiều kiểu sinh trắc học mà thiết bị hỗ trợ.")

                    authState.biometricCapabilities.forEach { capability ->
                        val selected = biometricSelection.contains(capability.type)
                        OutlinedButton(
                            onClick = {
                                biometricSelection = if (selected) {
                                    biometricSelection - capability.type
                                } else {
                                    biometricSelection + capability.type
                                }
                            },
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            Row(
                                modifier = Modifier.fillMaxWidth(),
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Icon(
                                    imageVector = if (capability.type == BiometricType.FINGERPRINT) {
                                        Icons.Filled.Fingerprint
                                    } else {
                                        Icons.Filled.Security
                                    },
                                    contentDescription = null
                                )
                                Spacer(modifier = Modifier.width(12.dp))
                                Text(capability.label, modifier = Modifier.weight(1f))
                                if (selected) {
                                    Icon(Icons.Filled.CheckCircle, contentDescription = null)
                                }
                            }
                        }
                    }

                    Text(
                        text = if (authState.awaitingBiometricEnrollment) {
                            "Sau khi bật sinh trắc học trong cài đặt máy, hãy quay lại đây để ứng dụng kích hoạt tiếp."
                        } else {
                            "Nếu máy chưa bật vân tay hoặc khuôn mặt, ứng dụng sẽ mở cài đặt sinh trắc học của điện thoại."
                        },
                        style = MaterialTheme.typography.bodySmall,
                        color = Gray600
                    )
                }
            },
            confirmButton = {
                Button(
                    onClick = { authViewModel.submitBiometricSelection(activity, biometricSelection) },
                    enabled = biometricSelection.isNotEmpty() && !authState.isBiometricPromptActive
                ) {
                    Text(if (authState.awaitingBiometricEnrollment) "Kiểm tra lại" else "Lưu cấu hình")
                }
            },
            dismissButton = {
                OutlinedButton(onClick = { authViewModel.dismissBiometricSetupDialog() }) {
                    Text("Đóng")
                }
            }
        )
    }

    if (uiState.isLoading) {
        LoadingIndicator()
    } else {
        LazyColumn(
            modifier = Modifier
                .fillMaxSize()
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(16.dp)
        ) {
            item {
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(16.dp)
                ) {
                    Column(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(24.dp),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Surface(
                            modifier = Modifier.size(80.dp),
                            shape = CircleShape,
                            color = Blue50
                        ) {
                            Icon(
                                imageVector = Icons.Filled.Person,
                                contentDescription = null,
                                modifier = Modifier
                                    .padding(20.dp)
                                    .fillMaxSize(),
                                tint = Blue700
                            )
                        }

                        Spacer(modifier = Modifier.height(12.dp))

                        Text(
                            text = uiState.profile?.fullName ?: "",
                            style = MaterialTheme.typography.headlineMedium,
                            fontWeight = FontWeight.Bold
                        )

                        uiState.profile?.positionName?.let {
                            Text(
                                text = it,
                                style = MaterialTheme.typography.bodyLarge,
                                color = Gray600
                            )
                        }

                        uiState.profile?.departmentName?.let {
                            Text(
                                text = it,
                                style = MaterialTheme.typography.bodyMedium,
                                color = Gray600
                            )
                        }
                    }
                }
            }

            item {
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(16.dp)
                ) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        Text(
                            text = "Thông tin liên hệ",
                            style = MaterialTheme.typography.titleMedium,
                            fontWeight = FontWeight.Bold
                        )

                        Spacer(modifier = Modifier.height(12.dp))

                        uiState.profile?.email?.let {
                            ProfileInfoRow(
                                icon = Icons.Filled.Email,
                                label = "Email",
                                value = it
                            )
                        }

                        uiState.profile?.phone?.let {
                            ProfileInfoRow(
                                icon = Icons.Filled.Phone,
                                label = "Số điện thoại",
                                value = it
                            )
                        }
                    }
                }
            }

            item {
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(16.dp)
                ) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        Text(
                            text = "Bảo mật ứng dụng",
                            style = MaterialTheme.typography.titleMedium,
                            fontWeight = FontWeight.Bold
                        )

                        Spacer(modifier = Modifier.height(12.dp))

                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Row(
                                modifier = Modifier.weight(1f),
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Surface(
                                    shape = RoundedCornerShape(12.dp),
                                    color = Blue50
                                ) {
                                    Icon(
                                        imageVector = Icons.Filled.Security,
                                        contentDescription = null,
                                        tint = Blue700,
                                        modifier = Modifier.padding(10.dp)
                                    )
                                }
                                Spacer(modifier = Modifier.width(12.dp))
                                Column {
                                    Text(
                                        text = "Đăng nhập nhanh bằng sinh trắc học",
                                        style = MaterialTheme.typography.bodyLarge,
                                        fontWeight = FontWeight.SemiBold
                                    )
                                    Text(
                                        text = when {
                                            authState.hasBiometricSession -> "Đã bật cho ${authState.enabledBiometricTypes.toDisplayText()}. Từ lần mở ứng dụng sau, hệ thống sẽ ưu tiên xác thực sinh trắc học trước."
                                            authState.hasBiometricHardware -> "Thiết bị này có thể dùng ${authState.biometricCapabilities.map { it.type }.toSet().toDisplayText()} để mở khóa nhanh."
                                            else -> "Thiết bị này hiện chưa có sinh trắc học khả dụng cho ứng dụng."
                                        },
                                        style = MaterialTheme.typography.bodySmall,
                                        color = Gray600
                                    )
                                }
                            }

                            Switch(
                                checked = authState.hasBiometricSession,
                                onCheckedChange = { authViewModel.setBiometricEnabled(activity, it) },
                                enabled = authState.hasBiometricHardware
                            )
                        }

                        Spacer(modifier = Modifier.height(12.dp))

                        if (authState.hasBiometricHardware) {
                            OutlinedButton(
                                onClick = { authViewModel.openBiometricSetupDialog() },
                                modifier = Modifier.fillMaxWidth(),
                                enabled = !authState.isBiometricPromptActive
                            ) {
                                Text(
                                    if (authState.hasBiometricSession) "Thiết lập lại đăng nhập nhanh"
                                    else "Bật đăng nhập nhanh ngay"
                                )
                            }

                            Spacer(modifier = Modifier.height(8.dp))

                            OutlinedButton(
                                onClick = { authViewModel.disableBiometric() },
                                modifier = Modifier.fillMaxWidth(),
                                enabled = authState.hasBiometricSession
                            ) {
                                Text("Tắt và xóa cấu hình đăng nhập nhanh")
                            }
                        }

                        Spacer(modifier = Modifier.height(12.dp))
                        HorizontalDivider(color = Gray200)
                        Spacer(modifier = Modifier.height(12.dp))

                        ProfileInfoRow(
                            icon = Icons.Filled.Security,
                            label = "Tự khóa phiên",
                            value = "Đóng ứng dụng sẽ khóa ngay, hoặc để yên 5 phút sẽ tự quay về màn hình đăng nhập."
                        )
                    }
                }
            }

            item {
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(16.dp)
                ) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        Text(
                            text = "Khởi chạy cùng thiết bị & Chạy nền",
                            style = MaterialTheme.typography.titleMedium,
                            fontWeight = FontWeight.Bold
                        )

                        Spacer(modifier = Modifier.height(12.dp))

                        Row(
                            modifier = Modifier.fillMaxWidth(),
                            horizontalArrangement = Arrangement.SpaceBetween,
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Row(
                                modifier = Modifier.weight(1f),
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                Surface(
                                    shape = RoundedCornerShape(12.dp),
                                    color = Blue50
                                ) {
                                    Icon(
                                        imageVector = Icons.Filled.PowerSettingsNew,
                                        contentDescription = null,
                                        tint = Blue700,
                                        modifier = Modifier.padding(10.dp)
                                    )
                                }
                                Spacer(modifier = Modifier.width(12.dp))
                                Column {
                                    Text(
                                        text = "Tự khởi chạy khi mở máy",
                                        style = MaterialTheme.typography.bodyLarge,
                                        fontWeight = FontWeight.SemiBold
                                    )
                                    Text(
                                        text = if (isAutoStartEnabled) {
                                            "Tự động bật dịch vụ ngầm nhận cuộc gọi & thông báo khẩn cấp ngay khi điện thoại khởi động lại."
                                        } else {
                                            "Đang tắt: Ứng dụng sẽ không tự chạy khi điện thoại khởi động lại."
                                        },
                                        style = MaterialTheme.typography.bodySmall,
                                        color = Gray600
                                    )
                                }
                            }

                            Switch(
                                checked = isAutoStartEnabled,
                                onCheckedChange = {
                                    isAutoStartEnabled = it
                                    AutoStartHelper.setAutoStartEnabled(context, it)
                                }
                            )
                        }

                        Spacer(modifier = Modifier.height(12.dp))

                        OutlinedButton(
                            onClick = {
                                activity?.let { AutoStartHelper.requestIgnoreBatteryOptimizations(it) }
                            },
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            Row(verticalAlignment = Alignment.CenterVertically) {
                                Icon(
                                    imageVector = if (isIgnoringBattery) Icons.Filled.CheckCircle else Icons.Filled.PlayArrow,
                                    contentDescription = null,
                                    tint = if (isIgnoringBattery) Blue700 else Gray600,
                                    modifier = Modifier.size(18.dp)
                                )
                                Spacer(modifier = Modifier.width(8.dp))
                                Text(
                                    if (isIgnoringBattery) "Đã cấp quyền chạy ngầm không giới hạn Pin"
                                    else "Yêu cầu quyền bỏ qua tối ưu hóa Pin"
                                )
                            }
                        }

                        Spacer(modifier = Modifier.height(8.dp))

                        OutlinedButton(
                            onClick = {
                                AutoStartHelper.openAutoStartPermissionSettings(context)
                            },
                            modifier = Modifier.fillMaxWidth()
                        ) {
                            Row(verticalAlignment = Alignment.CenterVertically) {
                                Icon(
                                    imageVector = Icons.Filled.PowerSettingsNew,
                                    contentDescription = null,
                                    modifier = Modifier.size(18.dp)
                                )
                                Spacer(modifier = Modifier.width(8.dp))
                                Text("Mở cài đặt Tự khởi chạy (Autostart) của máy")
                            }
                        }

                        Spacer(modifier = Modifier.height(8.dp))

                        Text(
                            text = "Gợi ý: Đối với các dòng máy Xiaomi/POCO, Samsung, Oppo, Vivo, bạn nên bật 'Tự khởi chạy' và chọn 'Không giới hạn pin' để luôn nhận được cuộc gọi và cảnh báo.",
                            style = MaterialTheme.typography.bodySmall,
                            color = Gray600
                        )
                    }
                }
            }

            item {
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(16.dp)
                ) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        Text(
                            text = "Lịch làm việc",
                            style = MaterialTheme.typography.titleMedium,
                            fontWeight = FontWeight.Bold
                        )

                        Spacer(modifier = Modifier.height(12.dp))

                        if (uiState.schedules.isEmpty()) {
                            Text(
                                text = "Chưa có lịch làm việc",
                                style = MaterialTheme.typography.bodyMedium,
                                color = Gray600
                            )
                        } else {
                            uiState.schedules.take(5).forEach { schedule ->
                                ScheduleRow(schedule)
                                HorizontalDivider(
                                    modifier = Modifier.padding(vertical = 4.dp),
                                    color = Gray200
                                )
                            }
                        }
                    }
                }
            }

            item {
                Button(
                    onClick = { onSessionExpired() },
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(50.dp),
                    shape = RoundedCornerShape(12.dp),
                    colors = ButtonDefaults.buttonColors(containerColor = Red600)
                ) {
                    Icon(Icons.AutoMirrored.Filled.Logout, contentDescription = null)
                    Spacer(modifier = Modifier.width(8.dp))
                    Text("Đăng xuất")
                }
            }
        }
    }
}

@Composable
private fun ProfileInfoRow(
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    label: String,
    value: String
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 4.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(
            imageVector = icon,
            contentDescription = null,
            tint = Gray600,
            modifier = Modifier.size(20.dp)
        )
        Spacer(modifier = Modifier.width(12.dp))
        Column {
            Text(
                text = label,
                style = MaterialTheme.typography.labelSmall,
                color = Gray400
            )
            Text(
                text = value,
                style = MaterialTheme.typography.bodyMedium
            )
        }
    }
}

@Composable
private fun ScheduleRow(schedule: ScheduleItem) {
    Column(modifier = Modifier.fillMaxWidth()) {
        Text(
            text = schedule.shiftName,
            style = MaterialTheme.typography.bodyLarge,
            fontWeight = FontWeight.SemiBold
        )
        Spacer(modifier = Modifier.height(4.dp))
        Text(
            text = "${schedule.startTime} - ${schedule.endTime}",
            style = MaterialTheme.typography.bodyMedium,
            color = Gray600
        )
    }
}
