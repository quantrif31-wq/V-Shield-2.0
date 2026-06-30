package com.vshield.mobile.viewmodel

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.vshield.mobile.data.RetrofitClient
import com.vshield.mobile.data.model.*
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

data class TransferUiState(
    val isLoading: Boolean = false,
    val vehicles: List<VehicleInfo> = emptyList(),
    val outgoingDelegations: List<DelegationInfo> = emptyList(),
    val incomingDelegations: List<DelegationInfo> = emptyList(),
    val employeeLookup: List<EmployeeLookup> = emptyList(),
    val selectedTab: Int = 0,
    val error: String? = null,
    val successMessage: String? = null
)

class TransferViewModel(application: Application) : AndroidViewModel(application) {

    private val _uiState = MutableStateFlow(TransferUiState())
    val uiState: StateFlow<TransferUiState> = _uiState

    fun loadData() {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, error = null)
            try {
                val vehiclesResp = RetrofitClient.apiService.getMyVehicles()
                val outgoingResp = RetrofitClient.apiService.getOutgoingDelegations()
                val incomingResp = RetrofitClient.apiService.getIncomingDelegations()

                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    vehicles = vehiclesResp.body()?.data ?: emptyList(),
                    outgoingDelegations = outgoingResp.body()?.data ?: emptyList(),
                    incomingDelegations = incomingResp.body()?.data ?: emptyList()
                )
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    error = "Lỗi tải dữ liệu: ${e.message}"
                )
            }
        }
    }

    fun lookupEmployee(query: String) {
        viewModelScope.launch {
            try {
                val resp = RetrofitClient.apiService.lookupEmployees(query)
                _uiState.value = _uiState.value.copy(
                    employeeLookup = resp.body()?.data ?: emptyList()
                )
            } catch (_: Exception) { }
        }
    }

    fun createDelegation(vehicleId: Int, toEmployeeId: Int, reason: String) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, error = null)
            try {
                val resp = RetrofitClient.apiService.createDelegation(
                    CreateDelegationRequest(vehicleId, toEmployeeId, reason)
                )
                if (resp.isSuccessful && resp.body()?.success == true) {
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        successMessage = "Ủy quyền xe thành công"
                    )
                    loadData()
                } else {
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        error = resp.body()?.message ?: "Ủy quyền thất bại"
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

    fun approveDelegation(id: Int) {
        viewModelScope.launch {
            try {
                val resp = RetrofitClient.apiService.approveDelegation(id)
                if (resp.isSuccessful && resp.body()?.success == true) {
                    _uiState.value = _uiState.value.copy(successMessage = "Đã duyệt ủy quyền")
                    loadData()
                } else {
                    _uiState.value = _uiState.value.copy(
                        error = resp.body()?.message ?: "Duyệt thất bại"
                    )
                }
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(error = "Lỗi: ${e.message}")
            }
        }
    }

    fun rejectDelegation(id: Int) {
        viewModelScope.launch {
            try {
                val resp = RetrofitClient.apiService.rejectDelegation(id)
                if (resp.isSuccessful && resp.body()?.success == true) {
                    _uiState.value = _uiState.value.copy(successMessage = "Đã từ chối ủy quyền")
                    loadData()
                } else {
                    _uiState.value = _uiState.value.copy(
                        error = resp.body()?.message ?: "Từ chối thất bại"
                    )
                }
            } catch (e: Exception) {
                _uiState.value = _uiState.value.copy(error = "Lỗi: ${e.message}")
            }
        }
    }

    fun setTab(index: Int) {
        _uiState.value = _uiState.value.copy(selectedTab = index)
    }

    fun clearMessages() {
        _uiState.value = _uiState.value.copy(error = null, successMessage = null)
    }
}
