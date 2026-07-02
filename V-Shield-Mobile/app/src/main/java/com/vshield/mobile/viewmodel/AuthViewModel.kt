package com.vshield.mobile.viewmodel

import android.app.Application
import androidx.fragment.app.FragmentActivity
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.vshield.mobile.data.AuthRepository
import com.vshield.mobile.data.TokenManager
import com.vshield.mobile.security.BiometricAuthManager
import com.vshield.mobile.security.SecureStorage
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

data class AuthUiState(
    val isLoading: Boolean = false,
    val isLoggedIn: Boolean = false,
    val hasBiometric: Boolean = false,
    val hasBiometricSession: Boolean = false,
    val pendingBiometricSetupUsername: String? = null,
    val lastUsername: String? = null,
    val error: String? = null
)

class AuthViewModel(application: Application) : AndroidViewModel(application) {

    private val authRepository = AuthRepository(TokenManager(application))
    private val biometricAuthManager = BiometricAuthManager(application)
    private val secureStorage = SecureStorage(application)
    private var inactivityJob: Job? = null

    private val _uiState = MutableStateFlow(AuthUiState())
    val uiState: StateFlow<AuthUiState> = _uiState

    init {
        checkExistingSession()
    }

    private fun checkExistingSession() {
        val hasBiometric = biometricAuthManager.isAvailable()
        val hasBiometricSession = hasBiometric &&
            secureStorage.isBiometricEnabled() &&
            authRepository.hasRestorableSession()

        val canAutoLoginWithoutPrompt = authRepository.hasActiveAccessToken() && !hasBiometricSession
        if (canAutoLoginWithoutPrompt) {
            authRepository.restoreAccessToken()
        }

        _uiState.value = AuthUiState(
            isLoggedIn = canAutoLoginWithoutPrompt,
            hasBiometric = hasBiometric,
            hasBiometricSession = hasBiometricSession,
            lastUsername = secureStorage.getLastUsername()
        )
    }

    fun login(username: String, password: String, mfaCode: String = "") {
        viewModelScope.launch {
            _uiState.value = _uiState.value.copy(isLoading = true, error = null)

            val result = authRepository.login(
                username = username,
                password = password,
                mfaCode = mfaCode.trim().ifBlank { null }
            )

            result.fold(
                onSuccess = {
                    val shouldPromptBiometric = biometricAuthManager.isAvailable() &&
                        authRepository.hasRestorableSession() &&
                        !secureStorage.isBiometricEnabled()

                    secureStorage.rememberLastUsername(username)

                    if (shouldPromptBiometric) {
                        _uiState.value = _uiState.value.copy(
                            isLoading = false,
                            pendingBiometricSetupUsername = username,
                            lastUsername = secureStorage.getLastUsername()
                        )
                    } else {
                        _uiState.value = _uiState.value.copy(
                            isLoading = false,
                            isLoggedIn = true,
                            hasBiometricSession = secureStorage.isBiometricEnabled() && authRepository.hasRestorableSession(),
                            lastUsername = secureStorage.getLastUsername()
                        )
                        startInactivityTimer()
                    }
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

    fun loginWithBiometric(activity: FragmentActivity) {
        if (!secureStorage.isBiometricEnabled() || !authRepository.hasRestorableSession()) {
            _uiState.value = _uiState.value.copy(
                error = "Không tìm thấy phiên đăng nhập để mở bằng vân tay"
            )
            return
        }

        biometricAuthManager.authenticate(
            activity = activity,
            onSuccess = {
                viewModelScope.launch {
                    _uiState.value = _uiState.value.copy(isLoading = true, error = null)
                    val restored = authRepository.restoreSessionWithStoredTokens()
                    _uiState.value = if (restored) {
                        _uiState.value.copy(
                            isLoading = false,
                            isLoggedIn = true
                        ).also { startInactivityTimer() }
                    } else {
                        secureStorage.disableBiometric()
                        _uiState.value.copy(
                            isLoading = false,
                            hasBiometricSession = false,
                            error = "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại"
                        )
                    }
                }
            },
            onError = { error ->
                _uiState.value = _uiState.value.copy(error = error)
            }
        )
    }

    fun completeBiometricSetup(activity: FragmentActivity?, enable: Boolean) {
        val username = _uiState.value.pendingBiometricSetupUsername

        if (!enable) {
            _uiState.value = _uiState.value.copy(
                isLoggedIn = true,
                hasBiometricSession = secureStorage.isBiometricEnabled() && authRepository.hasRestorableSession(),
                pendingBiometricSetupUsername = null,
                lastUsername = secureStorage.getLastUsername()
            )
            startInactivityTimer()
            return
        }

        if (activity == null) {
            _uiState.value = _uiState.value.copy(
                isLoading = false,
                error = "Thiết bị chưa sẵn sàng để đăng ký vân tay. Vui lòng thử lại."
            )
            return
        }

        if (username.isNullOrBlank() || !biometricAuthManager.isAvailable() || !authRepository.hasRestorableSession()) {
            _uiState.value = _uiState.value.copy(
                isLoading = false,
                isLoggedIn = true,
                pendingBiometricSetupUsername = null,
                hasBiometricSession = false,
                lastUsername = secureStorage.getLastUsername()
            )
            startInactivityTimer()
            return
        }

        _uiState.value = _uiState.value.copy(isLoading = true, error = null)
        biometricAuthManager.confirmEnrollment(
            activity = activity,
            onSuccess = {
                secureStorage.enableBiometricForSession(username)
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    isLoggedIn = true,
                    hasBiometricSession = secureStorage.isBiometricEnabled() && authRepository.hasRestorableSession(),
                    pendingBiometricSetupUsername = null,
                    lastUsername = secureStorage.getLastUsername()
                )
                startInactivityTimer()
            },
            onError = { error ->
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    isLoggedIn = true,
                    pendingBiometricSetupUsername = null,
                    hasBiometricSession = false,
                    lastUsername = secureStorage.getLastUsername(),
                    error = error
                )
                startInactivityTimer()
            }
        )
    }

    fun setBiometricEnabled(activity: FragmentActivity?, enable: Boolean) {
        val username = secureStorage.getLastUsername()

        if (!enable) {
            secureStorage.disableBiometric()
            _uiState.value = _uiState.value.copy(
                hasBiometricSession = false,
                lastUsername = secureStorage.getLastUsername(),
                error = null
            )
            return
        }

        if (activity == null) {
            _uiState.value = _uiState.value.copy(
                error = "Không mở được xác thực vân tay trên thiết bị này."
            )
            return
        }

        if (username.isNullOrBlank()) {
            _uiState.value = _uiState.value.copy(
                error = "Hãy đăng nhập lại một lần trước khi bật mở bằng vân tay."
            )
            return
        }

        if (!biometricAuthManager.isAvailable()) {
            _uiState.value = _uiState.value.copy(
                error = "Thiết bị này chưa hỗ trợ sinh trắc học khả dụng cho ứng dụng."
            )
            return
        }

        if (!authRepository.hasRestorableSession()) {
            _uiState.value = _uiState.value.copy(
                error = "Phiên đăng nhập hiện tại không còn hợp lệ để bật mở bằng vân tay."
            )
            return
        }

        _uiState.value = _uiState.value.copy(isLoading = true, error = null)
        biometricAuthManager.confirmEnrollment(
            activity = activity,
            onSuccess = {
                secureStorage.enableBiometricForSession(username)
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    hasBiometricSession = secureStorage.isBiometricEnabled() && authRepository.hasRestorableSession(),
                    lastUsername = secureStorage.getLastUsername(),
                    error = null
                )
            },
            onError = { error ->
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    hasBiometricSession = secureStorage.isBiometricEnabled() && authRepository.hasRestorableSession(),
                    lastUsername = secureStorage.getLastUsername(),
                    error = error
                )
            }
        )
    }

    fun showError(message: String) {
        _uiState.value = _uiState.value.copy(error = message)
    }

    fun recordUserActivity() {
        if (_uiState.value.isLoggedIn) {
            startInactivityTimer()
        }
    }

    fun lockSessionForInactivity() {
        inactivityJob?.cancel()
        val keepRefreshToken = secureStorage.isBiometricEnabled()
        authRepository.lockSession(keepRefreshToken = keepRefreshToken)
        _uiState.value = _uiState.value.copy(
            isLoggedIn = false,
            hasBiometricSession = secureStorage.isBiometricEnabled() && authRepository.hasRestorableSession(),
            pendingBiometricSetupUsername = null,
            lastUsername = secureStorage.getLastUsername()
        )
    }

    private fun startInactivityTimer() {
        inactivityJob?.cancel()
        inactivityJob = viewModelScope.launch {
            delay(5 * 60 * 1000L)
            lockSessionForInactivity()
        }
    }

    fun logout() {
        inactivityJob?.cancel()
        authRepository.logout()
        secureStorage.disableBiometric()
        _uiState.value = AuthUiState(
            hasBiometric = biometricAuthManager.isAvailable()
        )
    }

    fun clearError() {
        _uiState.value = _uiState.value.copy(error = null)
    }

    override fun onCleared() {
        super.onCleared()
        inactivityJob?.cancel()
    }
}
