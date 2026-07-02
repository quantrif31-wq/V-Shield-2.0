package com.vshield.mobile.data.model

import com.google.gson.annotations.SerializedName

data class SignalRNotification(
    @SerializedName("id") val id: Long,
    @SerializedName("title") val title: String?,
    @SerializedName("body") val body: String?,
    @SerializedName("category") val category: String?,
    @SerializedName("severity") val severity: String?,
    @SerializedName("referenceType") val referenceType: String?,
    @SerializedName("referenceId") val referenceId: String?,
    @SerializedName("actionUrl") val actionUrl: String?,
    @SerializedName("latitude") val latitude: Double?,
    @SerializedName("longitude") val longitude: Double?,
    @SerializedName("locationLabel") val locationLabel: String?,
    @SerializedName("createdAt") val createdAt: String?,
    @SerializedName("isRead") val isRead: Boolean
)

data class NotificationItem(
    @SerializedName("id") val id: Long,
    @SerializedName("title") val title: String?,
    @SerializedName("body") val body: String?,
    @SerializedName("category") val category: String?,
    @SerializedName("severity") val severity: String?,
    @SerializedName("referenceType") val referenceType: String?,
    @SerializedName("referenceId") val referenceId: String?,
    @SerializedName("actionUrl") val actionUrl: String?,
    @SerializedName("latitude") val latitude: Double?,
    @SerializedName("longitude") val longitude: Double?,
    @SerializedName("locationLabel") val locationLabel: String?,
    @SerializedName("createdAt") val createdAt: String?,
    @SerializedName("readAt") val readAt: String?,
    @SerializedName("isRead") val isRead: Boolean
)

data class UnreadCountEnvelope(
    @SerializedName("success") val success: Boolean,
    @SerializedName("count") val count: Int
)

data class NotificationsListResponse(
    @SerializedName("success") val success: Boolean,
    @SerializedName("data") val data: List<NotificationItem>?
)

data class SecurityAlertsResponse(
    @SerializedName("generatedAtUtc") val generatedAtUtc: String?,
    @SerializedName("criticalCount") val criticalCount: Int,
    @SerializedName("items") val items: List<SecurityAlertItem>?
)

data class SecurityAlertItem(
    @SerializedName("id") val id: String,
    @SerializedName("kind") val kind: String?,
    @SerializedName("severity") val severity: String?,
    @SerializedName("title") val title: String?,
    @SerializedName("message") val message: String?,
    @SerializedName("occurredAtUtc") val occurredAtUtc: String?,
    @SerializedName("route") val route: String?,
    @SerializedName("locationLabel") val locationLabel: String? = null,
    @SerializedName("zoneName") val zoneName: String? = null
)
