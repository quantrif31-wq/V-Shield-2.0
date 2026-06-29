package com.vshield.mobile.viewmodel

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.vshield.mobile.data.RetrofitClient
import com.vshield.mobile.data.model.QrData
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

data class QrUiState(
    val isLoading: Boolean = false,
    val qrData: QrData? = null,
    val remainingSeconds: Int = 0,
    val employeeName: String = "",
    val error: String? = null
)

class QrViewModel(application: Application) : AndroidViewModel(application) {

    private val _uiState = MutableStateFlow(QrUiState())
    val uiState: StateFlow<QrUiState> = _uiState

    private var refreshJob: Job? = null
    private var countdownJob: Job? = null

    fun startQrRefresh() {
        refreshJob?.cancel()
        refreshJob = viewModelScope.launch {
            fetchQr()
            while (true) {
                delay(25000)
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
                    error = null
                )
                startCountdown()
            } else {
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    error = response.body()?.message ?: "Không thể tạo QR"
                )
            }
        } catch (e: Exception) {
            _uiState.value = _uiState.value.copy(
                isLoading = false,
                error = "Lỗi kết nối: ${e.message}"
            )
        }
    }

    private fun startCountdown() {
        countdownJob?.cancel()
        countdownJob = viewModelScope.launch {
            while (_uiState.value.remainingSeconds > 0) {
                delay(1000)
                _uiState.value = _uiState.value.copy(
                    remainingSeconds = _uiState.value.remainingSeconds - 1
                )
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
