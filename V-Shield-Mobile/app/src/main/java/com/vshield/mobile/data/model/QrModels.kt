package com.vshield.mobile.data.model

import com.google.gson.annotations.SerializedName

data class QrResponse(
    @SerializedName("success") val success: Boolean,
    @SerializedName("message") val message: String?,
    @SerializedName("data") val data: QrData?
)

data class QrData(
    @SerializedName("employeeId") val employeeId: Int,
    @SerializedName("employeeName") val employeeName: String,
    @SerializedName("qrPayload") val qrPayload: String,
    @SerializedName("timeStepSeconds") val timeStepSeconds: Int,
    @SerializedName("generatedAtUtc") val generatedAtUtc: String,
    @SerializedName("expiresAtUtc") val expiresAtUtc: String,
    @SerializedName("remainingSeconds") val remainingSeconds: Int
)

data class OfflineQrBootstrapResponse(
    @SerializedName("success") val success: Boolean,
    @SerializedName("message") val message: String?,
    @SerializedName("data") val data: OfflineQrBootstrapData?
)

data class OfflineQrBootstrapData(
    @SerializedName("employeeId") val employeeId: Int,
    @SerializedName("employeeName") val employeeName: String,
    @SerializedName("secretKey") val secretKey: String,
    @SerializedName("timeStepSeconds") val timeStepSeconds: Int,
    @SerializedName("digits") val digits: Int,
    @SerializedName("issuedAtUtc") val issuedAtUtc: String?
)
