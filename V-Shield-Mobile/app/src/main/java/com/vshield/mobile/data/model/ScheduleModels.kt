package com.vshield.mobile.data.model

import com.google.gson.annotations.SerializedName

data class ScheduleResponse(
    @SerializedName("success") val success: Boolean,
    @SerializedName("data") val data: List<ScheduleItem>?
)

data class ScheduleItem(
    @SerializedName("scheduleId") val scheduleId: Int?,
    @SerializedName("date") val date: String,
    @SerializedName("shiftName") val shiftName: String,
    @SerializedName("startTime") val startTime: String?,
    @SerializedName("endTime") val endTime: String?,
    @SerializedName("location") val location: String?,
    @SerializedName("status") val status: String?,
    @SerializedName("note") val note: String?
)
