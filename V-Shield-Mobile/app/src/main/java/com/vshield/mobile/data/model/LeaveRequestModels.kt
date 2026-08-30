package com.vshield.mobile.data.model

import com.google.gson.annotations.SerializedName

data class CreateLeaveRequest(
    @SerializedName("employeeId") val employeeId: Int?,
    @SerializedName("leaveType") val leaveType: String,
    @SerializedName("startDate") val startDate: String,
    @SerializedName("endDate") val endDate: String,
    @SerializedName("reason") val reason: String?
)

data class LeaveRequestInfo(
    @SerializedName("leaveRequestId") val leaveRequestId: Int,
    @SerializedName("employeeId") val employeeId: Int? = null,
    @SerializedName("leaveType") val leaveType: String? = null,
    @SerializedName("startDate") val startDate: String,
    @SerializedName("endDate") val endDate: String,
    @SerializedName("reason") val reason: String? = null,
    @SerializedName("status") val status: String,
    @SerializedName("approverId") val approverId: Int? = null,
    @SerializedName("rejectReason") val rejectReason: String? = null,
    @SerializedName("createdAt") val createdAt: String? = null,
    @SerializedName("approvedAt") val approvedAt: String? = null,
    @SerializedName("updatedAt") val updatedAt: String? = null
)

data class LeaveType(
    @SerializedName("leaveTypeId") val leaveTypeId: Int,
    @SerializedName("typeName") val typeName: String
)

val LEAVE_TYPE_OPTIONS = listOf(
    LeaveType(1, "Nghỉ phép năm"),
    LeaveType(2, "Nghỉ ốm / Bệnh"),
    LeaveType(3, "Nghỉ không lương"),
    LeaveType(4, "Nghỉ việc riêng"),
    LeaveType(5, "Lý do khác")
)
