package com.vshield.mobile.data

import com.vshield.mobile.data.model.*

class AuthRepository(
    private val tokenManager: TokenManager
) {
    suspend fun login(username: String, password: String): Result<LoginData> {
        return try {
            val response = RetrofitClient.apiService.login(
                LoginRequest(username, password)
            )
            if (response.isSuccessful && response.body()?.success == true) {
                val data = response.body()!!.data!!
                tokenManager.saveToken(data.token)
                data.refreshToken?.let { tokenManager.saveRefreshToken(it) }
                data.employeeId?.let { tokenManager.saveEmployeeId(it) }
                data.roles?.let { tokenManager.saveRoles(it.toSet()) }
                RetrofitClient.setToken(data.token)
                Result.success(data)
            } else {
                Result.failure(
                    Exception(response.body()?.message ?: "Đăng nhập thất bại")
                )
            }
        } catch (e: Exception) {
            Result.failure(Exception("Kết nối thất bại: ${e.message}"))
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
