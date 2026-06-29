package com.vshield.mobile.data.model

import com.google.gson.annotations.SerializedName

data class LoginRequest(
    @SerializedName("username") val username: String,
    @SerializedName("password") val password: String
)

data class LoginResponse(
    @SerializedName("success") val success: Boolean,
    @SerializedName("message") val message: String?,
    @SerializedName("data") val data: LoginData?
)

data class LoginData(
    @SerializedName("token") val token: String,
    @SerializedName("refreshToken") val refreshToken: String?,
    @SerializedName("expiresAt") val expiresAt: String?,
    @SerializedName("employeeId") val employeeId: Int?,
    @SerializedName("roles") val roles: List<String>?
)

data class ApiResponse<T>(
    @SerializedName("success") val success: Boolean,
    @SerializedName("message") val message: String?,
    @SerializedName("data") val data: T?
)

data class EmptyData(
    @SerializedName("id") val id: Int? = null
)
