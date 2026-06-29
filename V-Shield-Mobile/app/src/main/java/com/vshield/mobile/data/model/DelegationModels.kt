package com.vshield.mobile.data.model

import com.google.gson.annotations.SerializedName

data class CreateDelegationRequest(
    @SerializedName("vehicleId") val vehicleId: Int,
    @SerializedName("toEmployeeId") val toEmployeeId: Int,
    @SerializedName("reason") val reason: String
)

data class DelegationResponse(
    @SerializedName("success") val success: Boolean,
    @SerializedName("message") val message: String?,
    @SerializedName("data") val data: DelegationInfo?
)

data class DelegationListResponse(
    @SerializedName("success") val success: Boolean,
    @SerializedName("data") val data: List<DelegationInfo>?,
    @SerializedName("message") val message: String?
)

data class DelegationInfo(
    @SerializedName("delegationId") val delegationId: Int,
    @SerializedName("vehicleId") val vehicleId: Int,
    @SerializedName("licensePlate") val licensePlate: String?,
    @SerializedName("fromEmployeeId") val fromEmployeeId: Int,
    @SerializedName("fromEmployeeName") val fromEmployeeName: String?,
    @SerializedName("toEmployeeId") val toEmployeeId: Int,
    @SerializedName("toEmployeeName") val toEmployeeName: String?,
    @SerializedName("reason") val reason: String?,
    @SerializedName("status") val status: String,
    @SerializedName("createdAt") val createdAt: String?,
    @SerializedName("approvedAt") val approvedAt: String?
)

data class EmployeeLookup(
    @SerializedName("employeeId") val employeeId: Int,
    @SerializedName("fullName") val fullName: String,
    @SerializedName("departmentName") val departmentName: String?
)

data class EmployeeLookupResponse(
    @SerializedName("success") val success: Boolean,
    @SerializedName("data") val data: List<EmployeeLookup>?
)
