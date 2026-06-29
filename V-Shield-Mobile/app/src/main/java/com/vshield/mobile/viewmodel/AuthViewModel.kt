package com.vshield.mobile.viewmodel

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.vshield.mobile.data.AuthRepository
import com.vshield.mobile.data.TokenManager
import com.vshield.mobile.security.BiometricAuthManager
import com.vshield.mobile.security.SecureStorage
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

data class AuthUiState(
    val isLoading: Boolean = false,
    val isLoggedIn: Boolean = false,
    val hasBiometric: Boolean = false,
    val hasStoredCredentials: Boolean = false,
    val error: String? = null
)

class AuthViewModel(application: Application) : AndroidViewModel(application) {

    private val tokenManager = TokenManager(application)
    private val authRepository = AuthRepository(tokenManager)
    private val biometricAuthManager = BiometricAuthManager(application)
    private val secureStorage = SecureStorage(application)

    private val _uiState = MutableStateFlow(AuthUiState())
    val uiState: StateFlow<AuthUiState> = _uiState

    init {
        checkExistingSession()
    }

    private fun checkExistingSession() {
        val hasSession = authRepository.restoreSession()
        _uiState.value = AuthUiState(
            isLoggedIn = hasSession,
            hasBiometric = biometricAuthManager.isAvailable(),
            hasStoredCredentials = secureStorage.hasStoredCredentials()
        )
    }

    fun login(username: String, password: String, saveBiometric: Boolean = false) {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, error = null)

            val result = authRepository.login(username, password)
            result.fold(
                onSuccess = {
                    if (saveBiometric && biometricAuthManager.isAvailable()) {
                        secureStorage.saveCredentials(username, password)
                    }
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        isLoggedIn = true
                    )
                },
                onFailure = {
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        error = it.message
                    )
                }
            )
        }
    }

    fun loginWithBiometric(activity: androidx.fragment.app.FragmentActivity) {
        val credentials = secureStorage.getCredentials()
        if (credentials == null) {
            _uiState.value = _uiState.value.copy(error = "Không tìm thấy thông tin đăng nhập đã lưu")
            return
        }

        biometricAuthManager.authenticate(
            activity = activity,
            onSuccess = {
                login(credentials.first, credentials.second)
            },
            onError = { error ->
                _uiState.value = _uiState.value.copy(error = error)
            }
        )
    }

    fun logout() {
        authRepository.logout()
        _uiState.value = AuthUiState(
            hasBiometric = biometricAuthManager.isAvailable(),
            hasStoredCredentials = false
        )
    }

    fun clearError() {
        _uiState.value = _uiState.value.copy(error = null)
    }
}
