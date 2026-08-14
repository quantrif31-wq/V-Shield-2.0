package com.vshield.mobile.data.model

import com.google.gson.annotations.SerializedName

data class EmployeeInfo(
    @SerializedName("employeeId") val employeeId: Int,
    @SerializedName("fullName") val fullName: String,
    @SerializedName("email") val email: String? = null,
    @SerializedName("phone") val phone: String? = null,
    @SerializedName("positionId") val positionId: Int? = null,
    @SerializedName("positionName") val positionName: String? = null,
    @SerializedName("departmentId") val departmentId: Int? = null,
    @SerializedName("departmentName") val departmentName: String? = null,
    @SerializedName("status") val status: Boolean? = null,
    @SerializedName("faceImageUrl") val faceImageUrl: String? = null,
    @SerializedName("hasFaceId") val hasFaceId: Boolean? = false
)

data class VehicleInfo(
    @SerializedName("vehicleId") val vehicleId: Int,
    @SerializedName("licensePlate") val licensePlate: String,
    @SerializedName("vehicleTypeId") val vehicleTypeId: Int?,
    @SerializedName("vehicleTypeName") val vehicleTypeName: String?,
    @SerializedName("employeeId") val employeeId: Int?,
    @SerializedName("employeeFullName") val employeeFullName: String?,
    @SerializedName("description") val description: String?
)
