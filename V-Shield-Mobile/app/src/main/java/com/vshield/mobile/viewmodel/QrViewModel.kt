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
            if (response.isSuccessful && response.body()?.success == true) {
                val data = response.body()!!.data!!
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    qrData = data,
                    remainingSeconds = data.remainingSeconds,
                    employeeName = data.employeeName,
                    isOfflineMode = false,
                    statusMessage = "QR dang dong bo truc tiep tu he thong.",
                    error = null
                )
                startCountdown()
            } else {
                fallbackToOfflineQr(response.body()?.message ?: "Khong the tao QR online.")
            }
        } catch (e: Exception) {
            fallbackToOfflineQr("Dang dung QR ngoai tuyen vi API tam thoi khong san sang.")
        }
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
                error = "Chua co cau hinh QR ngoai tuyen. Hay dang nhap online it nhat 1 lan."
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
