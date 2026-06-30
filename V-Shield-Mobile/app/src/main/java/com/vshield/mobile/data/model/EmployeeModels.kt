package com.vshield.mobile.data.model

import com.google.gson.annotations.SerializedName

data class EmployeeInfo(
    @SerializedName("employeeId") val employeeId: Int,
    @SerializedName("fullName") val fullName: String,
    @SerializedName("email") val email: String?,
    @SerializedName("phoneNumber") val phoneNumber: String?,
    @SerializedName("positionName") val positionName: String?,
    @SerializedName("departmentName") val departmentName: String?,
    @SerializedName("status") val status: Boolean?,
    @SerializedName("avatarUrl") val avatarUrl: String?
)

data class MyVehiclesResponse(
    @SerializedName("success") val success: Boolean,
    @SerializedName("data") val data: List<VehicleInfo>?
)

data class VehicleInfo(
    @SerializedName("vehicleId") val vehicleId: Int,
    @SerializedName("licensePlate") val licensePlate: String,
    @SerializedName("vehicleTypeName") val vehicleTypeName: String?,
    @SerializedName("vehicleTypeId") val vehicleTypeId: Int?,
    @SerializedName("color") val color: String?,
    @SerializedName("brand") val brand: String?,
    @SerializedName("status") val status: String?
)
