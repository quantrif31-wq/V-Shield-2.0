package com.vshield.mobile.viewmodel

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.vshield.mobile.data.RetrofitClient
import com.vshield.mobile.data.model.CreateLeaveRequest
import com.vshield.mobile.data.model.LeaveRequestInfo
import com.vshield.mobile.data.model.LeaveType
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

data class LeaveUiState(
    val isLoading: Boolean = false,
    val leaveRequests: List<LeaveRequestInfo> = emptyList(),
    val leaveTypes: List<LeaveType> = emptyList(),
    val error: String? = null,
    val successMessage: String? = null
)

class LeaveViewModel(application: Application) : AndroidViewModel(application) {

    private val _uiState = MutableStateFlow(LeaveUiState())
    val uiState: StateFlow<LeaveUiState> = _uiState

    fun loadData() {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, error = null)
            try {
                val requestsResp = RetrofitClient.apiService.getMyLeaveRequests()
                val typesResp = RetrofitClient.apiService.getLeaveTypes()

                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    leaveRequests = requestsResp.body()?.data ?: emptyList(),
                    leaveTypes = typesResp.body()?.data ?: emptyList()
                )
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    error = "Lỗi tải dữ liệu: ${e.message}"
                )
            }
        }
    }

    fun createLeaveRequest(leaveTypeId: Int, startDate: String, endDate: String, reason: String) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, error = null)
            try {
                val resp = RetrofitClient.apiService.createLeaveRequest(
                    CreateLeaveRequest(leaveTypeId, startDate, endDate, reason)
                )
                if (resp.isSuccessful && resp.body()?.success == true) {
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        successMessage = "Gửi đơn nghỉ phép thành công"
                    )
                    loadData()
                } else {
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        error = resp.body()?.message ?: "Gửi đơn thất bại"
                    )
                }
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    error = "Lỗi: ${e.message}"
                )
            }
        }
    }

    fun clearMessages() {
        _uiState.value = _uiState.value.copy(error = null, successMessage = null)
    }
}
