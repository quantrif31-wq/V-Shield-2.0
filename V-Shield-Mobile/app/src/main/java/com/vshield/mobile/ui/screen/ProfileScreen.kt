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

    DisposableEffect(lifecycleOwner, activity, authState.awaitingBiometricEnrollment) {
        val observer = LifecycleEventObserver { _, event ->
            if (event == Lifecycle.Event.ON_RESUME && authState.awaitingBiometricEnrollment) {
                authViewModel.onBiometricEnrollmentSettingsReturned(activity)
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
            title = { Text("Quan ly dang nhap nhanh") },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                    Text("Ban co the bat mot hoac nhieu kieu sinh trac hoc ma dien thoai ho tro.")

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
                            "Sau khi bat sinh trac hoc trong cai dat may, quay lai day de app kich hoat tiep."
                        } else {
                            "Neu may chua bat van tay hoac khuon mat, app se mo cai dat sinh trac hoc cua dien thoai."
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
                    Text(if (authState.awaitingBiometricEnrollment) "Kiem tra lai" else "Luu cau hinh")
                }
            },
            dismissButton = {
                OutlinedButton(onClick = { authViewModel.dismissBiometricSetupDialog() }) {
                    Text("Dong")
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
                            text = "Thong tin lien he",
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
                                label = "So dien thoai",
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
                            text = "Bao mat ung dung",
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
                                        text = "Dang nhap nhanh bang sinh trac hoc",
                                        style = MaterialTheme.typography.bodyLarge,
                                        fontWeight = FontWeight.SemiBold
                                    )
                                    Text(
                                        text = when {
                                            authState.hasBiometricSession -> "Da bat cho ${authState.enabledBiometricTypes.toDisplayText()}. Tu lan mo app sau, he thong se uu tien quet sinh trac hoc truoc."
                                            authState.hasBiometricHardware -> "Thiet bi nay co the dung ${authState.biometricCapabilities.map { it.type }.toSet().toDisplayText()} de mo nhanh."
                                            else -> "Thiet bi nay hien chua co sinh trac hoc kha dung cho app."
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
                                    if (authState.hasBiometricSession) "Thiet lap lai dang nhap nhanh"
                                    else "Bat dang nhap nhanh ngay"
                                )
                            }

                            Spacer(modifier = Modifier.height(8.dp))

                            OutlinedButton(
                                onClick = { authViewModel.disableBiometric() },
                                modifier = Modifier.fillMaxWidth(),
                                enabled = authState.hasBiometricSession
                            ) {
                                Text("Tat va xoa cau hinh dang nhap nhanh")
                            }
                        }

                        Spacer(modifier = Modifier.height(12.dp))
                        HorizontalDivider(color = Gray200)
                        Spacer(modifier = Modifier.height(12.dp))

                        ProfileInfoRow(
                            icon = Icons.Filled.Security,
                            label = "Tu khoa phien",
                            value = "Dong app la khoa ngay, hoac bo khong 5 phut se quay ve man dang nhap."
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
                            text = "Lich lam viec",
                            style = MaterialTheme.typography.titleMedium,
                            fontWeight = FontWeight.Bold
                        )

                        Spacer(modifier = Modifier.height(12.dp))

                        if (uiState.schedules.isEmpty()) {
                            Text(
                                text = "Chua co lich lam viec",
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
                    Text("Dang xuat")
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
