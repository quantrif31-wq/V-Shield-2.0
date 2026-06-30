package com.vshield.mobile.data

import com.vshield.mobile.data.model.LoginData
import com.vshield.mobile.data.model.LoginRequest

class AuthRepository(
    private val tokenManager: TokenManager
) {
    suspend fun login(username: String, password: String, mfaCode: String? = null): Result<LoginData> {
        return try {
            val response = RetrofitClient.apiService.login(
                LoginRequest(username, password, mfaCode)
            )
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
                            "Tai khoan can thiet lap xac thuc hai lop tren web truoc khi dang nhap mobile."
                        } else {
                            "Tai khoan yeu cau ma xac thuc hai lop. Phien ban mobile hien chua ho tro buoc nay."
                        }
                    )
                )
            } else {
                Result.failure(
                    Exception(data?.message ?: "Dang nhap that bai")
                )
            }
        } catch (e: Exception) {
            Result.failure(Exception("Ket noi that bai: ${e.message}"))
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

    fun isLoggedIn(): Boolean = tokenManager.isLoggedIn()
}
