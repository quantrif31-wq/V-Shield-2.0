package com.vshield.mobile.data

import com.vshield.mobile.data.model.LoginData
import com.vshield.mobile.data.model.LoginRequest
import com.vshield.mobile.data.model.RefreshTokenRequest

class AuthRepository(
    private val tokenManager: TokenManager
) {
    suspend fun login(username: String, password: String, mfaCode: String? = null): Result<LoginData> {
        return try {
            android.util.Log.d("AuthRepository", "Attempting login for user: $username")
            val response = RetrofitClient.apiService.login(
                LoginRequest(username, password, mfaCode)
            )
            android.util.Log.d("AuthRepository", "Login response code: ${response.code()}")
            val data = response.body()

            if (response.isSuccessful && data != null && data.token.isNotBlank()) {
                tokenManager.saveToken(data.token)
                data.refreshToken?.let { tokenManager.saveRefreshToken(it) }
                data.employeeId?.let { tokenManager.saveEmployeeId(it) }
                data.role?.let { tokenManager.saveRoles(setOf(it)) }
                RetrofitClient.setToken(data.token)
                Result.success(data)
            } else if (response.isSuccessful && data?.requiresMfa == true) {
                Result.failure(
                    Exception(
                        data.message ?: if (data.requiresMfaSetup) {
                            "Tài khoản cần thiết lập xác thực hai lớp trên web trước khi đăng nhập trên mobile."
                        } else {
                            "Tài khoản yêu cầu mã xác thực hai lớp (MFA)."
                        }
                    )
                )
            } else {
                val errorMsg = response.errorBody()?.string()
                android.util.Log.e("AuthRepository", "Login failed errorBody: $errorMsg")
                Result.failure(
                    Exception(data?.message ?: errorMsg ?: "Đăng nhập thất bại")
                )
            }
        } catch (e: Exception) {
            android.util.Log.e("AuthRepository", "Login exception: ${e.message}", e)
            Result.failure(Exception("Kết nối máy chủ thất bại: ${e.message}"))
        }
    }

    fun restoreSession(): Boolean {
        val token = tokenManager.getToken()
        if (token != null) {
            RetrofitClient.setToken(token)
            return true
        }
        return false
    }

    fun logout() {
        RetrofitClient.setToken(null)
        tokenManager.clear()
    }

    fun lockSession(keepRefreshToken: Boolean) {
        RetrofitClient.setToken(null)
        tokenManager.clearToken()
        if (!keepRefreshToken) {
            tokenManager.clearRefreshToken()
        }
    }

    fun hasActiveAccessToken(): Boolean = !tokenManager.getToken().isNullOrBlank()

    fun hasRestorableSession(): Boolean =
        !tokenManager.getToken().isNullOrBlank() || !tokenManager.getRefreshToken().isNullOrBlank()

    fun restoreAccessToken(): Boolean {
        val token = tokenManager.getToken() ?: return false
        RetrofitClient.setToken(token)
        return true
    }

    suspend fun restoreSessionWithStoredTokens(): Boolean {
        if (restoreAccessToken()) return true

        val refreshToken = tokenManager.getRefreshToken() ?: return false
        return try {
            val response = RetrofitClient.apiService.refresh(RefreshTokenRequest(refreshToken))
            val data = response.body()
            if (!response.isSuccessful || data == null || data.token.isBlank()) {
                false
            } else {
                tokenManager.saveToken(data.token)
                data.refreshToken?.let { tokenManager.saveRefreshToken(it) }
                data.employeeId?.let { tokenManager.saveEmployeeId(it) }
                data.role?.let { tokenManager.saveRoles(setOf(it)) }
                RetrofitClient.setToken(data.token)
                true
            }
        } catch (_: Exception) {
            false
        }
    }

    fun isLoggedIn(): Boolean = tokenManager.isLoggedIn()
}
