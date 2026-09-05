package com.vshield.mobile.viewmodel

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.vshield.mobile.data.RetrofitClient
import com.vshield.mobile.data.model.QrData
import com.vshield.mobile.security.OfflineQrGenerator
import com.vshield.mobile.security.SecureStorage
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import java.io.IOException
import java.time.Instant

data class QrUiState(
    val isLoading: Boolean = false,
    val qrData: QrData? = null,
    val remainingSeconds: Int = 0,
    val employeeName: String = "",
    val isOfflineMode: Boolean = false,
    val statusMessage: String? = null,
    val error: String? = null
)

class QrViewModel(application: Application) : AndroidViewModel(application) {

    private val secureStorage = SecureStorage(application)

    private val _uiState = MutableStateFlow(QrUiState())
    val uiState: StateFlow<QrUiState> = _uiState

    private var refreshJob: Job? = null
    private var countdownJob: Job? = null
    private var offlineConfigRefreshedForSession = false

    fun startQrRefresh() {
        refreshJob?.cancel()
        refreshJob = viewModelScope.launch {
            fetchQr()
            while (true) {
                delay(5000)
                fetchQr()
            }
        }
    }

    fun stopQrRefresh() {
        refreshJob?.cancel()
        countdownJob?.cancel()
    }

    private suspend fun fetchQr() {
        try {
            val response = RetrofitClient.apiService.getMyQr()
            val body = response.body()
            if (response.isSuccessful && body?.success == true && body.data != null) {
                val data = body.data
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    qrData = data,
                    remainingSeconds = data.remainingSeconds,
                    employeeName = data.employeeName,
                    isOfflineMode = false,
                    statusMessage = "Mã QR đang đồng bộ trực tiếp từ hệ thống.",
                    error = null
                )
                startCountdown()
                refreshOfflineConfigFromServer()
            } else {
                showOnlineQrFailure(
                    body?.message ?: "Không thể tạo mã QR trực tuyến (HTTP ${response.code()})."
                )
            }
        } catch (e: IOException) {
            fallbackToOfflineQr("Đang dùng mã QR ngoại tuyến vì kết nối máy chủ tạm thời gián đoạn.")
        } catch (e: Exception) {
            showOnlineQrFailure("Không thể đồng bộ mã QR với máy chủ. Vui lòng đăng nhập lại hoặc thử lại.")
        }
    }

    /**
     * QR offline must use the same secret currently held by the central server.
     * Refresh it once after a verified online QR, avoiding a network call on every
     * five-second QR refresh while replacing stale config after a server re-seed.
     */
    private fun refreshOfflineConfigFromServer() {
        if (offlineConfigRefreshedForSession) return
        offlineConfigRefreshedForSession = true

        viewModelScope.launch {
            try {
                val response = RetrofitClient.apiService.getMyOfflineQrBootstrap()
                val payload = response.body()?.data
                if (response.isSuccessful && response.body()?.success == true && payload != null) {
                    secureStorage.saveOfflineQrConfig(
                        com.vshield.mobile.security.OfflineQrConfig(
                            employeeId = payload.employeeId,
                            employeeName = payload.employeeName,
                            secretKey = payload.secretKey,
                            timeStepSeconds = payload.timeStepSeconds,
                            digits = payload.digits
                        )
                    )
                } else {
                    offlineConfigRefreshedForSession = false
                }
            } catch (_: Exception) {
                offlineConfigRefreshedForSession = false
            }
        }
    }

    private fun showOnlineQrFailure(message: String) {
        countdownJob?.cancel()
        _uiState.value = _uiState.value.copy(
            isLoading = false,
            qrData = null,
            remainingSeconds = 0,
            isOfflineMode = false,
            statusMessage = null,
            error = message
        )
    }

    private fun fallbackToOfflineQr(message: String) {
        val offlineConfig = secureStorage.getOfflineQrConfig()
        val offlineUser = secureStorage.getOfflineUserSession()

        if (offlineConfig != null) {
            val qrData = OfflineQrGenerator.generate(offlineConfig, Instant.now().epochSecond)
            _uiState.value = _uiState.value.copy(
                isLoading = false,
                qrData = qrData,
                remainingSeconds = qrData.remainingSeconds,
                employeeName = qrData.employeeName,
                isOfflineMode = true,
                statusMessage = message,
                error = null
            )
            startCountdown()
        } else {
            _uiState.value = _uiState.value.copy(
                isLoading = false,
                employeeName = offlineUser?.fullName.orEmpty(),
                isOfflineMode = true,
                statusMessage = null,
                error = "Chưa có cấu hình mã QR ngoại tuyến. Hãy đăng nhập trực tuyến ít nhất 1 lần."
            )
        }
    }

    private fun startCountdown() {
        countdownJob?.cancel()
        countdownJob = viewModelScope.launch {
            while (true) {
                val state = _uiState.value
                if (state.qrData == null) {
                    break
                }

                if (state.remainingSeconds > 0) {
                    delay(1000)
                    _uiState.value = _uiState.value.copy(
                        remainingSeconds = (_uiState.value.remainingSeconds - 1).coerceAtLeast(0)
                    )
                } else if (state.isOfflineMode) {
                    val offlineConfig = secureStorage.getOfflineQrConfig()
                    if (offlineConfig != null) {
                        val refreshedQr = OfflineQrGenerator.generate(offlineConfig, Instant.now().epochSecond)
                        _uiState.value = _uiState.value.copy(
                            qrData = refreshedQr,
                            remainingSeconds = refreshedQr.remainingSeconds,
                            employeeName = refreshedQr.employeeName
                        )
                    } else {
                        break
                    }
                } else {
                    break
                }
            }
        }
    }

    fun refreshNow() {
        _uiState.value = _uiState.value.copy(isLoading = true)
        viewModelScope.launch { fetchQr() }
    }

    fun clearError() {
        _uiState.value = _uiState.value.copy(error = null)
    }
}
