package com.vshield.mobile.data.model

import com.google.gson.annotations.SerializedName

data class CreateLeaveRequest(
    @SerializedName("leaveTypeId") val leaveTypeId: Int,
    @SerializedName("startDate") val startDate: String,
    @SerializedName("endDate") val endDate: String,
    @SerializedName("reason") val reason: String
)

data class LeaveRequestResponse(
    @SerializedName("success") val success: Boolean,
    @SerializedName("data") val data: List<LeaveRequestInfo>?,
    @SerializedName("message") val message: String?
)

data class LeaveRequestInfo(
    @SerializedName("leaveRequestId") val leaveRequestId: Int,
    @SerializedName("leaveTypeName") val leaveTypeName: String?,
    @SerializedName("startDate") val startDate: String,
    @SerializedName("endDate") val endDate: String,
    @SerializedName("reason") val reason: String?,
    @SerializedName("status") val status: String,
    @SerializedName("createdAt") val createdAt: String?
)

data class LeaveType(
    @SerializedName("leaveTypeId") val leaveTypeId: Int,
    @SerializedName("typeName") val typeName: String,
    @SerializedName("description") val description: String?
)

data class LeaveTypeResponse(
    @SerializedName("success") val success: Boolean,
    @SerializedName("data") val data: List<LeaveType>?
)
