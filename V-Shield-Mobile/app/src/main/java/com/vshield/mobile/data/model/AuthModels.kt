package com.vshield.mobile.data.model

import com.google.gson.annotations.SerializedName

data class LoginRequest(
    @SerializedName("username") val username: String,
    @SerializedName("password") val password: String,
    @SerializedName("mfaCode") val mfaCode: String? = null
)

data class LoginData(
    @SerializedName("token") val token: String = "",
    @SerializedName("refreshToken") val refreshToken: String?,
    @SerializedName("expiresAt") val expiresAt: String?,
    @SerializedName("refreshTokenExpiresAt") val refreshTokenExpiresAt: String?,
    @SerializedName("employeeId") val employeeId: Int?,
    @SerializedName("role") val role: String?,
    @SerializedName("username") val username: String?,
    @SerializedName("fullName") val fullName: String?,
    @SerializedName("requiresMfa") val requiresMfa: Boolean = false,
    @SerializedName("requiresMfaSetup") val requiresMfaSetup: Boolean = false,
    @SerializedName("mfaSetupSecret") val mfaSetupSecret: String?,
    @SerializedName("mfaSetupUri") val mfaSetupUri: String?,
    @SerializedName("message") val message: String?,
    @SerializedName("hasOperationalScopeAssignments") val hasOperationalScopeAssignments: Boolean = false,
    @SerializedName("operationalTaskKeys") val operationalTaskKeys: List<String> = emptyList()
)

typealias LoginResponse = LoginData

data class RefreshTokenRequest(
    @SerializedName("refreshToken") val refreshToken: String
)

data class ApiResponse<T>(
    @SerializedName("success") val success: Boolean,
    @SerializedName("message") val message: String?,
    @SerializedName("data") val data: T?
)

data class EmptyData(
    @SerializedName("id") val id: Int? = null
)
