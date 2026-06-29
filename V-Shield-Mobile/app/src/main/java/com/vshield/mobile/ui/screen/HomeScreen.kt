package com.vshield.mobile.ui.screen

import androidx.compose.animation.core.*
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.vshield.mobile.ui.component.ErrorDialog
import com.vshield.mobile.ui.component.LoadingIndicator
import com.vshield.mobile.ui.component.QrCodeView
import com.vshield.mobile.ui.theme.*
import com.vshield.mobile.viewmodel.QrViewModel

@Composable
fun HomeScreen(
    onSessionExpired: () -> Unit,
    qrViewModel: QrViewModel = viewModel()
) {
    val uiState by qrViewModel.uiState.collectAsState()

    LaunchedEffect(Unit) {
        qrViewModel.startQrRefresh()
    }

    DisposableEffect(Unit) {
        onDispose {
            qrViewModel.stopQrRefresh()
        }
    }

    if (uiState.error != null) {
        ErrorDialog(
            title = "Lỗi",
            message = uiState.error!!,
            onDismiss = { qrViewModel.clearError() }
        )
    }

    val pulseAnim = rememberInfiniteTransition(label = "pulse")
    val pulseScale by pulseAnim.animateFloat(
        initialValue = 1f,
        targetValue = if (uiState.remainingSeconds <= 10) 1.05f else 1f,
        animationSpec = infiniteRepeatable(
            animation = tween(800, easing = EaseInOutCubic),
            repeatMode = RepeatMode.Reverse
        ),
        label = "pulse"
    )

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
                    text = "Chào mừng",
                    style = MaterialTheme.typography.bodyLarge,
                    color = MaterialTheme.colorScheme.onPrimary.copy(alpha = 0.8f)
                )
                Text(
                    text = uiState.employeeName.ifEmpty { "..." },
                    style = MaterialTheme.typography.headlineMedium,
                    color = MaterialTheme.colorScheme.onPrimary,
                    fontWeight = FontWeight.Bold
                )
            }
        }

        if (uiState.isLoading && uiState.qrData == null) {
            LoadingIndicator()
        } else {
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(16.dp),
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Spacer(modifier = Modifier.height(8.dp))

                Text(
                    text = "QR Động",
                    style = MaterialTheme.typography.titleLarge,
                    fontWeight = FontWeight.Bold
                )

                Spacer(modifier = Modifier.height(4.dp))

                uiState.qrData?.let { qrData ->
                    Text(
                        text = "Còn ${uiState.remainingSeconds}s",
                        style = MaterialTheme.typography.titleMedium,
                        color = if (uiState.remainingSeconds <= 10) Red600
                        else Green600
                    )

                    Spacer(modifier = Modifier.height(8.dp))

                    QrCodeView(
                        payload = qrData.qrPayload,
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(horizontal = 16.dp)
                    )

                    Spacer(modifier = Modifier.height(12.dp))

                    Text(
                        text = "Quét mã QR tại cổng bảo vệ để xác thực",
                        style = MaterialTheme.typography.bodyMedium,
                        color = Gray600,
                        textAlign = TextAlign.Center
                    )

                    Spacer(modifier = Modifier.height(16.dp))

                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.spacedBy(12.dp)
                    ) {
                        OutlinedButton(
                            onClick = { qrViewModel.refreshNow() },
                            modifier = Modifier.weight(1f),
                            shape = RoundedCornerShape(12.dp)
                        ) {
                            Text("Làm mới")
                        }
                    }
                }
            }
        }
    }
}
