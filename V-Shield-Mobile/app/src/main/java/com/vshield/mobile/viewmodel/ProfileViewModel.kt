package com.vshield.mobile.viewmodel

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.vshield.mobile.data.RetrofitClient
import com.vshield.mobile.data.TokenManager
import com.vshield.mobile.data.model.EmployeeInfo
import com.vshield.mobile.data.model.ScheduleItem
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

data class ProfileUiState(
    val isLoading: Boolean = false,
    val profile: EmployeeInfo? = null,
    val schedules: List<ScheduleItem> = emptyList(),
    val error: String? = null
)

class ProfileViewModel(application: Application) : AndroidViewModel(application) {

    private val _uiState = MutableStateFlow(ProfileUiState())
    val uiState: StateFlow<ProfileUiState> = _uiState

    private fun currentEmployeeId(): Int = TokenManager(getApplication()).getEmployeeId()

    fun loadData() {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, error = null)
            try {
                val employeeId = currentEmployeeId()
                val profileResp = RetrofitClient.apiService.getMyProfile()
                val scheduleResp = RetrofitClient.apiService.getMySchedule(employeeId)

                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    profile = profileResp.body(),
                    schedules = scheduleResp.body() ?: emptyList()
                )
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    error = "Lỗi tải dữ liệu: ${e.message}"
                )
            }
        }
    }
}
