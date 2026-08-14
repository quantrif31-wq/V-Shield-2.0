package com.vshield.mobile.data.model

import com.google.gson.annotations.SerializedName

data class CreateDelegationRequest(
    @SerializedName("vehicleId") val vehicleId: Int,
    @SerializedName("toEmployeeId") val toEmployeeId: Int,
    @SerializedName("reason") val reason: String?
)

data class DelegationInfo(
    @SerializedName("vehicleDelegationId") val delegationId: Int,
    @SerializedName("vehicleId") val vehicleId: Int,
    @SerializedName("licensePlate") val licensePlate: String?,
    @SerializedName("fromEmployeeId") val fromEmployeeId: Int?,
    @SerializedName("fromEmployeeName") val fromEmployeeName: String?,
    @SerializedName("toEmployeeId") val toEmployeeId: Int?,
    @SerializedName("toEmployeeName") val toEmployeeName: String?,
    @SerializedName("reason") val reason: String?,
    @SerializedName("status") val status: String,
    @SerializedName("requestedAtUtc") val createdAt: String?,
    @SerializedName("respondedAtUtc") val approvedAt: String?
)

data class DelegationActionResponse(
    @SerializedName("message") val message: String?
)
