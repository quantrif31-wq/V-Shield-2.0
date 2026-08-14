package com.vshield.mobile.data.model

import com.google.gson.annotations.SerializedName

data class ScheduleItem(
    @SerializedName("scheduleId") val scheduleId: Int?,
    @SerializedName("employeeId") val employeeId: Int?,
    @SerializedName("shiftId") val shiftId: Int?,
    @SerializedName("shiftName") val shiftName: String,
    @SerializedName("shiftStartTime") val startTime: String?,
    @SerializedName("shiftEndTime") val endTime: String?,
    @SerializedName("workDate") val date: String,
    @SerializedName("status") val status: String?,
    @SerializedName("note") val note: String?
)
