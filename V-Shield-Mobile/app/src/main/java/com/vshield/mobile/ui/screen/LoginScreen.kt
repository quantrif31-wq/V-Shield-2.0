package com.vshield.mobile.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Fingerprint
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Security
import androidx.compose.material.icons.filled.Visibility
import androidx.compose.material.icons.filled.VisibilityOff
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
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
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.unit.dp
import androidx.fragment.app.FragmentActivity
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.LifecycleEventObserver
import androidx.lifecycle.compose.LocalLifecycleOwner
import androidx.lifecycle.viewmodel.compose.viewModel
import com.vshield.mobile.security.BiometricType
import com.vshield.mobile.security.toDisplayText
import com.vshield.mobile.ui.component.ErrorDialog
import com.vshield.mobile.ui.component.LoadingIndicator
import com.vshield.mobile.ui.theme.Blue700
import com.vshield.mobile.viewmodel.AuthViewModel
import kotlinx.coroutines.delay

@Composable
fun LoginScreen(
    onLoginSuccess: () -> Unit,
    authViewModel: AuthViewModel = viewModel()
) {
    val uiState by authViewModel.uiState.collectAsState()
    val activity = LocalContext.current as? FragmentActivity
    val lifecycleOwner = LocalLifecycleOwner.current

    var username by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var mfaCode by remember { mutableStateOf("") }
    var passwordVisible by remember { mutableStateOf(false) }
    var biometricSelection by remember(uiState.showBiometricSetupDialog, uiState.biometricCapabilities) {
        mutableStateOf(
            uiState.enabledBiometricTypes.ifEmpty { uiState.biometricCapabilities.map { it.type }.toSet() }
        )
    }

    LaunchedEffect(uiState.isLoggedIn) {
        if (uiState.isLoggedIn) {
            onLoginSuccess()
        }
    }

    LaunchedEffect(uiState.shouldAutoPromptBiometricLogin, activity) {
        if (uiState.shouldAutoPromptBiometricLogin && activity != null) {
            delay(250)
            authViewModel.consumeAutoBiometricLogin(activity)
        }
    }

    DisposableEffect(lifecycleOwner, activity, uiState.awaitingBiometricEnrollment) {
        val observer = LifecycleEventObserver { _, event ->
            if (event == Lifecycle.Event.ON_RESUME && uiState.awaitingBiometricEnrollment) {
                authViewModel.onBiometricEnrollmentSettingsReturned(activity)
            }
        }
        lifecycleOwner.lifecycle.addObserver(observer)
        onDispose {
            lifecycleOwner.lifecycle.removeObserver(observer)
        }
    }

    if (uiState.error != null) {
        ErrorDialog(
            title = "Lỗi đăng nhập",
            message = uiState.error!!,
            onDismiss = { authViewModel.clearError() }
        )
    }

    if (uiState.showBiometricSetupDialog) {
        AlertDialog(
            onDismissRequest = { authViewModel.dismissBiometricSetupDialog() },
            title = {
                Text("Bật đăng nhập nhanh")
            },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(12.dp)) {
                    Text(
                        "Chọn cách đăng nhập nhanh bạn muốn dùng trên điện thoại này. Hệ thống sẽ sử dụng sinh trắc học đã được kích hoạt trên thiết bị."
                    )

                    uiState.biometricCapabilities.forEach { capability ->
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
                                Text(
                                    text = capability.label,
                                    modifier = Modifier.weight(1f)
                                )
                                if (selected) {
                                    Icon(
                                        imageVector = Icons.Filled.CheckCircle,
                                        contentDescription = null
                                    )
                                }
                            }
                        }
                    }

                    Text(
                        text = if (uiState.awaitingBiometricEnrollment) {
                            "Điện thoại chưa bật sinh trắc học. Bạn hãy bật trong cài đặt máy, sau đó quay lại ứng dụng để tiếp tục kích hoạt."
                        } else {
                            "Nếu máy chưa bật vân tay hoặc khuôn mặt, ứng dụng sẽ mở màn hình cài đặt của điện thoại."
                        },
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            },
            confirmButton = {
                Button(
                    onClick = { authViewModel.submitBiometricSelection(activity, biometricSelection) },
                    enabled = biometricSelection.isNotEmpty() && !uiState.isBiometricPromptActive
                ) {
                    Text(
                        if (uiState.awaitingBiometricEnrollment) "Kiểm tra lại"
                        else "Bật đăng nhập nhanh"
                    )
                }
            },
            dismissButton = {
                OutlinedButton(onClick = { authViewModel.skipBiometricSetup() }) {
                    Text("Nhập tay sau")
                }
            }
        )
    }

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Blue700)
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(24.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center
        ) {
            Spacer(modifier = Modifier.height(60.dp))

            Icon(
                imageVector = Icons.Filled.Lock,
                contentDescription = null,
                modifier = Modifier.size(72.dp),
                tint = MaterialTheme.colorScheme.surface
            )

            Spacer(modifier = Modifier.height(16.dp))

            Text(
                text = "V-Shield",
                style = MaterialTheme.typography.displayLarge,
                color = MaterialTheme.colorScheme.surface,
                fontWeight = FontWeight.Bold
            )

            Text(
                text = "Đăng nhập an toàn cho thiết bị di động",
                style = MaterialTheme.typography.bodyLarge,
                color = MaterialTheme.colorScheme.surface.copy(alpha = 0.82f)
            )

            Spacer(modifier = Modifier.height(40.dp))

            Card(
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(20.dp),
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface)
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(24.dp)
                ) {
                    Text(
                        text = "Đăng nhập",
                        style = MaterialTheme.typography.headlineMedium,
                        color = MaterialTheme.colorScheme.onSurface,
                        fontWeight = FontWeight.Bold
                    )

                    Spacer(modifier = Modifier.height(10.dp))

                    Text(
                        text = "Vui lòng nhập tên tài khoản, mật khẩu và mã xác thực 2 lớp (nếu có). Sau khi đăng nhập thành công, bạn có thể bật mở khóa nhanh bằng sinh trắc học.",
                        style = MaterialTheme.typography.bodyMedium,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )

                    if (uiState.hasBiometricSession) {
                        Spacer(modifier = Modifier.height(12.dp))
                        Text(
                            text = "Đăng nhập nhanh đã bật cho ${uiState.enabledBiometricTypes.toDisplayText()}. Mỗi lần mở ứng dụng, hệ thống sẽ ưu tiên xác thực sinh trắc học trước.",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }

                    Spacer(modifier = Modifier.height(24.dp))

                    OutlinedTextField(
                        value = username,
                        onValueChange = { username = it },
                        label = { Text("Tên đăng nhập") },
                        leadingIcon = { Icon(Icons.Filled.Person, contentDescription = null) },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true,
                        keyboardOptions = KeyboardOptions(imeAction = ImeAction.Next),
                        shape = RoundedCornerShape(12.dp)
                    )

                    Spacer(modifier = Modifier.height(16.dp))

                    OutlinedTextField(
                        value = password,
                        onValueChange = { password = it },
                        label = { Text("Mật khẩu") },
                        leadingIcon = { Icon(Icons.Filled.Lock, contentDescription = null) },
                        trailingIcon = {
                            IconButton(onClick = { passwordVisible = !passwordVisible }) {
                                Icon(
                                    imageVector = if (passwordVisible) Icons.Filled.VisibilityOff else Icons.Filled.Visibility,
                                    contentDescription = null
                                )
                            }
                        },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true,
                        visualTransformation = if (passwordVisible) VisualTransformation.None else PasswordVisualTransformation(),
                        keyboardOptions = KeyboardOptions(
                            keyboardType = KeyboardType.Password,
                            imeAction = ImeAction.Done
                        ),
                        shape = RoundedCornerShape(12.dp)
                    )

                    Spacer(modifier = Modifier.height(16.dp))

                    OutlinedTextField(
                        value = mfaCode,
                        onValueChange = { mfaCode = it.filter(Char::isDigit).take(6) },
                        label = { Text("Mã xác thực 2 lớp (MFA)") },
                        leadingIcon = { Icon(Icons.Filled.Lock, contentDescription = null) },
                        supportingText = {
                            Text("Nhập 6 chữ số từ ứng dụng Authenticator nếu tài khoản đang bật MFA.")
                        },
                        modifier = Modifier.fillMaxWidth(),
                        singleLine = true,
                        keyboardOptions = KeyboardOptions(
                            keyboardType = KeyboardType.NumberPassword,
                            imeAction = ImeAction.Done
                        ),
                        shape = RoundedCornerShape(12.dp)
                    )

                    if (uiState.hasBiometricHardware) {
                        Spacer(modifier = Modifier.height(10.dp))
                        Text(
                            text = "Thiết bị này có thể dùng ${uiState.biometricCapabilities.map { it.type }.toSet().toDisplayText()} để mở khóa nhanh.",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }

                    Spacer(modifier = Modifier.height(20.dp))

                    Button(
                        onClick = { authViewModel.login(username, password, mfaCode) },
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(50.dp),
                        enabled = !uiState.isLoading && username.isNotBlank() && password.isNotBlank(),
                        shape = RoundedCornerShape(12.dp)
                    ) {
                        if (uiState.isLoading) {
                            CircularProgressIndicator(
                                modifier = Modifier.size(24.dp),
                                color = MaterialTheme.colorScheme.onPrimary,
                                strokeWidth = 2.dp
                            )
                        } else {
                            Text("Đăng nhập")
                        }
                    }

                    if (uiState.hasBiometricSession) {
                        Spacer(modifier = Modifier.height(16.dp))

                        OutlinedButton(
                            onClick = {
                                if (activity != null) {
                                    authViewModel.loginWithBiometric(activity)
                                } else {
                                    authViewModel.showError("Không thể mở xác thực sinh trắc học trên thiết bị này.")
                                }
                            },
                            modifier = Modifier
                                .fillMaxWidth()
                                .height(50.dp),
                            enabled = !uiState.isBiometricPromptActive,
                            shape = RoundedCornerShape(12.dp)
                        ) {
                            Icon(Icons.Filled.Fingerprint, contentDescription = null, modifier = Modifier.size(20.dp))
                            Spacer(modifier = Modifier.width(8.dp))
                            Text(
                                if (uiState.lastUsername.isNullOrBlank()) "Mở bằng sinh trắc học"
                                else "Mở nhanh cho ${uiState.lastUsername}"
                            )
                        }
                    }

                    if (uiState.canEnterOffline) {
                        Spacer(modifier = Modifier.height(12.dp))

                        OutlinedButton(
                            onClick = { authViewModel.enterOfflineMode() },
                            modifier = Modifier
                                .fillMaxWidth()
                                .height(50.dp),
                            shape = RoundedCornerShape(12.dp)
                        ) {
                            Text(
                                if (uiState.offlineDisplayName.isNullOrBlank()) {
                                    "Vào chế độ ngoại tuyến"
                                } else {
                                    "Vào ngoại tuyến cho ${uiState.offlineDisplayName}"
                                }
                            )
                        }

                        Spacer(modifier = Modifier.height(8.dp))

                        Text(
                            text = "Nếu máy chủ tạm thời gián đoạn, ứng dụng vẫn có thể truy cập bằng dữ liệu đã lưu từ lần đăng nhập thành công trước.",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
            }

            Spacer(modifier = Modifier.height(60.dp))
        }

        if (uiState.isLoading) {
            LoadingIndicator()
        }
    }
}
