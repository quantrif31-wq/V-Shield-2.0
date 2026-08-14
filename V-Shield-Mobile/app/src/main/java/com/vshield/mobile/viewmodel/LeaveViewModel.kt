package com.vshield.mobile.viewmodel

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.vshield.mobile.data.RetrofitClient
import com.vshield.mobile.data.TokenManager
import com.vshield.mobile.data.model.CreateLeaveRequest
import com.vshield.mobile.data.model.LEAVE_TYPE_OPTIONS
import com.vshield.mobile.data.model.LeaveRequestInfo
import com.vshield.mobile.data.model.LeaveType
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

data class LeaveUiState(
    val isLoading: Boolean = false,
    val leaveRequests: List<LeaveRequestInfo> = emptyList(),
    val leaveTypes: List<LeaveType> = LEAVE_TYPE_OPTIONS,
    val error: String? = null,
    val successMessage: String? = null
)

class LeaveViewModel(application: Application) : AndroidViewModel(application) {

    private val _uiState = MutableStateFlow(LeaveUiState())
    val uiState: StateFlow<LeaveUiState> = _uiState

    private fun currentEmployeeId(): Int = TokenManager(getApplication()).getEmployeeId()

    fun loadData() {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, error = null)
            try {
                val requestsResp = RetrofitClient.apiService.getMyLeaveRequests()

                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    leaveRequests = requestsResp.body() ?: emptyList(),
                    leaveTypes = LEAVE_TYPE_OPTIONS
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
                val type = LEAVE_TYPE_OPTIONS.find { it.leaveTypeId == leaveTypeId }
                val resp = RetrofitClient.apiService.createLeaveRequest(
                    CreateLeaveRequest(
                        employeeId = currentEmployeeId().takeIf { it > 0 },
                        leaveType = type?.typeName ?: "Other",
                        startDate = startDate,
                        endDate = endDate,
                        reason = reason
                    )
                )
                if (resp.isSuccessful) {
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        successMessage = "Gửi đơn nghỉ phép thành công"
                    )
                    loadData()
                } else {
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        error = "Gửi đơn thất bại (${resp.code()})"
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
