package com.vshield.mobile.viewmodel

import android.app.Application
import androidx.fragment.app.FragmentActivity
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.vshield.mobile.data.AuthRepository
import com.vshield.mobile.data.RetrofitClient
import com.vshield.mobile.data.TokenManager
import com.vshield.mobile.security.BiometricAuthManager
import com.vshield.mobile.security.BiometricCapability
import com.vshield.mobile.security.BiometricType
import com.vshield.mobile.security.OfflineQrConfig
import com.vshield.mobile.security.SecureStorage
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch

data class AuthUiState(
    val isLoading: Boolean = false,
    val isLoggedIn: Boolean = false,
    val isOfflineMode: Boolean = false,
    val biometricCapabilities: List<BiometricCapability> = emptyList(),
    val enabledBiometricTypes: Set<BiometricType> = emptySet(),
    val hasBiometricSession: Boolean = false,
    val canEnterOffline: Boolean = false,
    val offlineDisplayName: String? = null,
    val pendingBiometricSetupUsername: String? = null,
    val pendingBiometricTypes: Set<BiometricType> = emptySet(),
    val lastUsername: String? = null,
    val showBiometricSetupDialog: Boolean = false,
    val awaitingBiometricEnrollment: Boolean = false,
    val shouldAutoPromptBiometricLogin: Boolean = false,
    val isBiometricPromptActive: Boolean = false,
    val error: String? = null
) {
    val hasBiometricHardware: Boolean
        get() = biometricCapabilities.isNotEmpty()
}

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
        val biometricCapabilities = biometricAuthManager.getSupportedBiometricCapabilities()
        val enabledBiometricTypes = secureStorage.getEnabledBiometricTypes()
        val offlineSession = secureStorage.getOfflineUserSession()
        val hasOfflineSession = offlineSession != null
        val hasBiometricSession = biometricCapabilities.isNotEmpty() &&
            secureStorage.isBiometricEnabled() &&
            (authRepository.hasRestorableSession() || hasOfflineSession)

        val canAutoLoginWithoutPrompt = authRepository.hasActiveAccessToken() && !hasBiometricSession
        if (canAutoLoginWithoutPrompt) {
            authRepository.restoreAccessToken()
        }

        _uiState.value = AuthUiState(
            isLoggedIn = canAutoLoginWithoutPrompt,
            isOfflineMode = false,
            biometricCapabilities = biometricCapabilities,
            enabledBiometricTypes = enabledBiometricTypes,
            hasBiometricSession = hasBiometricSession,
            canEnterOffline = hasOfflineSession,
            offlineDisplayName = offlineSession?.fullName,
            lastUsername = secureStorage.getLastUsername(),
            shouldAutoPromptBiometricLogin = hasBiometricSession
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
                onSuccess = { data ->
                    val capabilities = biometricAuthManager.getSupportedBiometricCapabilities()
                    val hasRestorableSession = authRepository.hasRestorableSession()
                    val displayName = data.fullName?.takeIf { it.isNotBlank() } ?: username
                    val employeeId = data.employeeId ?: 0

                    secureStorage.rememberLastUsername(username)
                    if (employeeId > 0) {
                        secureStorage.saveOfflineUserSession(
                            username = username,
                            fullName = displayName,
                            employeeId = employeeId,
                            role = data.role
                        )
                    }

                    primeOfflineQrCache()

                    if (capabilities.isNotEmpty() &&
                        hasRestorableSession &&
                        !secureStorage.isBiometricEnabled()
                    ) {
                        _uiState.value = _uiState.value.copy(
                            isLoading = false,
                            isLoggedIn = false,
                            isOfflineMode = false,
                            biometricCapabilities = capabilities,
                            enabledBiometricTypes = secureStorage.getEnabledBiometricTypes(),
                            hasBiometricSession = false,
                            canEnterOffline = secureStorage.hasOfflineSession(),
                            offlineDisplayName = secureStorage.getOfflineUserSession()?.fullName,
                            pendingBiometricSetupUsername = username,
                            pendingBiometricTypes = emptySet(),
                            lastUsername = secureStorage.getLastUsername(),
                            showBiometricSetupDialog = true,
                            awaitingBiometricEnrollment = false
                        )
                    } else {
                        _uiState.value = _uiState.value.copy(
                            isLoading = false,
                            isLoggedIn = true,
                            isOfflineMode = false,
                            biometricCapabilities = capabilities,
                            enabledBiometricTypes = secureStorage.getEnabledBiometricTypes(),
                            hasBiometricSession = secureStorage.isBiometricEnabled() && hasRestorableSession,
                            canEnterOffline = secureStorage.hasOfflineSession(),
                            offlineDisplayName = secureStorage.getOfflineUserSession()?.fullName,
                            lastUsername = secureStorage.getLastUsername(),
                            pendingBiometricSetupUsername = null,
                            pendingBiometricTypes = emptySet(),
                            showBiometricSetupDialog = false,
                            awaitingBiometricEnrollment = false,
                            shouldAutoPromptBiometricLogin = false,
                            isBiometricPromptActive = false
                        )
                        startInactivityTimer()
                    }
                },
                onFailure = {
                    val cachedOfflineUser = secureStorage.getOfflineUserSession()
                    _uiState.value = _uiState.value.copy(
                        isLoading = false,
                        canEnterOffline = cachedOfflineUser != null,
                        offlineDisplayName = cachedOfflineUser?.fullName,
                        error = it.message
                    )
                }
            )
        }
    }

    private fun primeOfflineQrCache() {
        viewModelScope.launch {
            runCatching {
                val response = RetrofitClient.apiService.getMyOfflineQrBootstrap()
                val payload = response.body()?.data
                if (response.isSuccessful && response.body()?.success == true && payload != null) {
                    secureStorage.saveOfflineQrConfig(
                        OfflineQrConfig(
                            employeeId = payload.employeeId,
                            employeeName = payload.employeeName,
                            secretKey = payload.secretKey,
                            timeStepSeconds = payload.timeStepSeconds,
                            digits = payload.digits
                        )
                    )
                }
            }
        }
    }

    fun consumeAutoBiometricLogin(activity: FragmentActivity?) {
        if (activity == null ||
            !_uiState.value.shouldAutoPromptBiometricLogin ||
            _uiState.value.isBiometricPromptActive
        ) {
            return
        }

        _uiState.value = _uiState.value.copy(
            shouldAutoPromptBiometricLogin = false,
            isBiometricPromptActive = true,
            error = null
        )

        loginWithBiometric(activity)
    }

    fun loginWithBiometric(activity: FragmentActivity) {
        val hasOfflineSession = secureStorage.hasOfflineSession()
        if (!secureStorage.isBiometricEnabled() || (!authRepository.hasRestorableSession() && !hasOfflineSession)) {
            _uiState.value = _uiState.value.copy(
                isBiometricPromptActive = false,
                hasBiometricSession = false,
                error = "Khong tim thay phien dang nhap de mo bang sinh trac hoc"
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
                            isLoggedIn = true,
                            isOfflineMode = false,
                            isBiometricPromptActive = false
                        ).also { startInactivityTimer() }
                    } else if (hasOfflineSession) {
                        val offline = secureStorage.getOfflineUserSession()
                        _uiState.value.copy(
                            isLoading = false,
                            isLoggedIn = true,
                            isOfflineMode = true,
                            isBiometricPromptActive = false,
                            canEnterOffline = true,
                            offlineDisplayName = offline?.fullName
                        ).also { startInactivityTimer() }
                    } else {
                        secureStorage.disableBiometric()
                        _uiState.value.copy(
                            isLoading = false,
                            isBiometricPromptActive = false,
                            enabledBiometricTypes = emptySet(),
                            hasBiometricSession = false,
                            error = "Phien dang nhap da het han, vui long dang nhap lai"
                        )
                    }
                }
            },
            onError = { error ->
                _uiState.value = _uiState.value.copy(
                    isBiometricPromptActive = false,
                    error = error
                )
            }
        )
    }

    fun enterOfflineMode() {
        val offline = secureStorage.getOfflineUserSession()
        val offlineQr = secureStorage.getOfflineQrConfig()

        if (offline == null || offlineQr == null) {
            _uiState.value = _uiState.value.copy(
                error = "May nay chua co du lieu ngoai tuyen day du. Hay dang nhap thanh cong online it nhat 1 lan."
            )
            return
        }

        _uiState.value = _uiState.value.copy(
            isLoggedIn = true,
            isOfflineMode = true,
            canEnterOffline = true,
            offlineDisplayName = offline.fullName,
            error = null
        )
        startInactivityTimer()
    }

    fun openBiometricSetupDialog() {
        if (_uiState.value.biometricCapabilities.isEmpty()) {
            _uiState.value = _uiState.value.copy(
                error = "Thiet bi nay chua co sinh trac hoc kha dung cho dang nhap nhanh."
            )
            return
        }

        _uiState.value = _uiState.value.copy(
            showBiometricSetupDialog = true,
            error = null
        )
    }

    fun dismissBiometricSetupDialog() {
        _uiState.value = _uiState.value.copy(
            showBiometricSetupDialog = false,
            awaitingBiometricEnrollment = false
        )
    }

    fun submitBiometricSelection(
        activity: FragmentActivity?,
        selectedTypes: Set<BiometricType>
    ) {
        val username = _uiState.value.pendingBiometricSetupUsername ?: secureStorage.getLastUsername()
        val supportedTypes = _uiState.value.biometricCapabilities.map { it.type }.toSet()
        val filteredTypes = selectedTypes.intersect(supportedTypes)

        if (activity == null) {
            _uiState.value = _uiState.value.copy(
                error = "Khong mo duoc cau hinh sinh trac hoc tren thiet bi nay."
            )
            return
        }

        if (filteredTypes.isEmpty()) {
            _uiState.value = _uiState.value.copy(
                error = "Hay chon it nhat mot cach dang nhap nhanh kha dung."
            )
            return
        }

        if (username.isNullOrBlank() || (!authRepository.hasRestorableSession() && !secureStorage.hasOfflineSession())) {
            _uiState.value = _uiState.value.copy(
                error = "Hay dang nhap lai mot lan truoc khi bat dang nhap nhanh."
            )
            return
        }

        _uiState.value = _uiState.value.copy(
            pendingBiometricSetupUsername = username,
            pendingBiometricTypes = filteredTypes,
            error = null
        )

        if (!biometricAuthManager.isBiometricReady()) {
            val opened = biometricAuthManager.openEnrollmentSettings(activity)
            _uiState.value = _uiState.value.copy(
                showBiometricSetupDialog = true,
                awaitingBiometricEnrollment = opened,
                error = if (opened) {
                    "Sau khi bat sinh trac hoc tren may, quay lai app de kich hoat dang nhap nhanh."
                } else {
                    "Khong mo duoc man hinh cai dat sinh trac hoc cua dien thoai."
                }
            )
            return
        }

        confirmBiometricEnrollment(activity, username, filteredTypes)
    }

    fun onBiometricEnrollmentSettingsReturned(activity: FragmentActivity?) {
        if (activity == null || !_uiState.value.awaitingBiometricEnrollment) {
            return
        }

        if (!biometricAuthManager.isBiometricReady()) {
            _uiState.value = _uiState.value.copy(
                awaitingBiometricEnrollment = false,
                showBiometricSetupDialog = true,
                error = "Dien thoai van chua bat sinh trac hoc. Ban co the thu lai hoac bo qua."
            )
            return
        }

        val username = _uiState.value.pendingBiometricSetupUsername ?: secureStorage.getLastUsername()
        val selectedTypes = _uiState.value.pendingBiometricTypes

        if (username.isNullOrBlank() || selectedTypes.isEmpty()) {
            _uiState.value = _uiState.value.copy(
                awaitingBiometricEnrollment = false,
                showBiometricSetupDialog = true,
                error = "Khong tim thay cau hinh dang nhap nhanh dang cho kich hoat."
            )
            return
        }

        confirmBiometricEnrollment(activity, username, selectedTypes)
    }

    private fun confirmBiometricEnrollment(
        activity: FragmentActivity,
        username: String,
        selectedTypes: Set<BiometricType>
    ) {
        _uiState.value = _uiState.value.copy(
            isLoading = true,
            error = null,
            showBiometricSetupDialog = false,
            awaitingBiometricEnrollment = false,
            isBiometricPromptActive = true
        )

        biometricAuthManager.confirmEnrollment(
            activity = activity,
            selectedTypes = selectedTypes,
            onSuccess = {
                secureStorage.enableBiometricForSession(username, selectedTypes)
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    isLoggedIn = true,
                    isOfflineMode = false,
                    enabledBiometricTypes = secureStorage.getEnabledBiometricTypes(),
                    hasBiometricSession = secureStorage.isBiometricEnabled() &&
                        (authRepository.hasRestorableSession() || secureStorage.hasOfflineSession()),
                    canEnterOffline = secureStorage.hasOfflineSession(),
                    offlineDisplayName = secureStorage.getOfflineUserSession()?.fullName,
                    pendingBiometricSetupUsername = null,
                    pendingBiometricTypes = emptySet(),
                    showBiometricSetupDialog = false,
                    awaitingBiometricEnrollment = false,
                    isBiometricPromptActive = false,
                    lastUsername = secureStorage.getLastUsername()
                )
                startInactivityTimer()
            },
            onError = { error ->
                _uiState.value = _uiState.value.copy(
                    isLoading = false,
                    isLoggedIn = true,
                    isOfflineMode = false,
                    canEnterOffline = secureStorage.hasOfflineSession(),
                    offlineDisplayName = secureStorage.getOfflineUserSession()?.fullName,
                    pendingBiometricTypes = selectedTypes,
                    showBiometricSetupDialog = true,
                    awaitingBiometricEnrollment = false,
                    isBiometricPromptActive = false,
                    lastUsername = secureStorage.getLastUsername(),
                    error = error
                )
                startInactivityTimer()
            }
        )
    }

    fun skipBiometricSetup() {
        _uiState.value = _uiState.value.copy(
            isLoggedIn = true,
            isOfflineMode = false,
            enabledBiometricTypes = secureStorage.getEnabledBiometricTypes(),
            hasBiometricSession = secureStorage.isBiometricEnabled() &&
                (authRepository.hasRestorableSession() || secureStorage.hasOfflineSession()),
            canEnterOffline = secureStorage.hasOfflineSession(),
            offlineDisplayName = secureStorage.getOfflineUserSession()?.fullName,
            pendingBiometricSetupUsername = null,
            pendingBiometricTypes = emptySet(),
            showBiometricSetupDialog = false,
            awaitingBiometricEnrollment = false,
            lastUsername = secureStorage.getLastUsername()
        )
        startInactivityTimer()
    }

    fun disableBiometric() {
        secureStorage.disableBiometric()
        _uiState.value = _uiState.value.copy(
            enabledBiometricTypes = emptySet(),
            hasBiometricSession = false,
            shouldAutoPromptBiometricLogin = false,
            showBiometricSetupDialog = false,
            awaitingBiometricEnrollment = false,
            isBiometricPromptActive = false,
            lastUsername = secureStorage.getLastUsername(),
            error = null
        )
    }

    fun setBiometricEnabled(activity: FragmentActivity?, enable: Boolean) {
        if (!enable) {
            disableBiometric()
            return
        }

        if (activity == null) {
            _uiState.value = _uiState.value.copy(
                error = "Khong mo duoc xac thuc sinh trac hoc tren thiet bi nay."
            )
            return
        }

        if (_uiState.value.biometricCapabilities.isEmpty()) {
            _uiState.value = _uiState.value.copy(
                error = "Thiet bi nay chua ho tro sinh trac hoc kha dung cho ung dung."
            )
            return
        }

        if (!authRepository.hasRestorableSession() && !secureStorage.hasOfflineSession()) {
            _uiState.value = _uiState.value.copy(
                error = "Hay dang nhap lai mot lan truoc khi bat dang nhap nhanh."
            )
            return
        }

        openBiometricSetupDialog()
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
        if (_uiState.value.isBiometricPromptActive || _uiState.value.awaitingBiometricEnrollment) {
            return
        }

        inactivityJob?.cancel()
        val keepRefreshToken = secureStorage.isBiometricEnabled()
        authRepository.lockSession(keepRefreshToken = keepRefreshToken)
        _uiState.value = _uiState.value.copy(
            isLoggedIn = false,
            isOfflineMode = false,
            hasBiometricSession = secureStorage.isBiometricEnabled() &&
                (authRepository.hasRestorableSession() || secureStorage.hasOfflineSession()),
            canEnterOffline = secureStorage.hasOfflineSession(),
            offlineDisplayName = secureStorage.getOfflineUserSession()?.fullName,
            pendingBiometricSetupUsername = null,
            pendingBiometricTypes = emptySet(),
            showBiometricSetupDialog = false,
            awaitingBiometricEnrollment = false,
            shouldAutoPromptBiometricLogin = secureStorage.isBiometricEnabled() &&
                (authRepository.hasRestorableSession() || secureStorage.hasOfflineSession()),
            isBiometricPromptActive = false,
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
        secureStorage.clearOfflineSession()
        _uiState.value = AuthUiState(
            biometricCapabilities = biometricAuthManager.getSupportedBiometricCapabilities()
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
