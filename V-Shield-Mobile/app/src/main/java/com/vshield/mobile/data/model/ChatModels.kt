package com.vshield.mobile.data.model

import com.google.gson.annotations.SerializedName

data class ApiChatResponse<T>(
    @SerializedName("success") val success: Boolean,
    @SerializedName("message") val message: String?,
    @SerializedName("data") val data: T?
)

data class ContactInfo(
    @SerializedName("employeeId") val employeeId: Int,
    @SerializedName("fullName") val fullName: String,
    @SerializedName("email") val email: String?,
    @SerializedName("phone") val phone: String?,
    @SerializedName("positionName") val positionName: String?,
    @SerializedName("departmentName") val departmentName: String?
)

data class ConversationInfo(
    @SerializedName("conversationId") val conversationId: Int,
    @SerializedName("createdAt") val createdAt: String?,
    @SerializedName("title") val title: String?,
    @SerializedName("lastMessage") val lastMessage: LastMessageInfo?,
    @SerializedName("participants") val participants: List<ParticipantInfo>,
    @SerializedName("unreadCount") val unreadCount: Int,
    @SerializedName("lastReadAt") val lastReadAt: String?
)

data class LastMessageInfo(
    @SerializedName("messageId") val messageId: Int,
    @SerializedName("content") val content: String?,
    @SerializedName("sentAt") val sentAt: String?,
    @SerializedName("messageType") val messageType: String?,
    @SerializedName("senderName") val senderName: String?,
    @SerializedName("senderId") val senderId: Int?
)

data class ParticipantInfo(
    @SerializedName("employeeId") val employeeId: Int,
    @SerializedName("fullName") val fullName: String
)

data class CreateConversationRequest(
    @SerializedName("employeeIds") val employeeIds: List<Int>,
    @SerializedName("title") val title: String? = null
)

data class CreateConversationResponse(
    @SerializedName("conversationId") val conversationId: Int?,
    @SerializedName("isExisting") val isExisting: Boolean?
)

data class ChatMessageInfo(
    @SerializedName("messageId") val messageId: Int,
    @SerializedName("senderId") val senderId: Int,
    @SerializedName("senderName") val senderName: String?,
    @SerializedName("content") val content: String?,
    @SerializedName("messageType") val messageType: String?,
    @SerializedName("signalingData") val signalingData: String?,
    @SerializedName("sentAt") val sentAt: String?,
    @SerializedName("isRead") val isRead: Boolean,
    @SerializedName("readAt") val readAt: String?
)

data class ConversationsResponse(
    @SerializedName("success") val success: Boolean,
    @SerializedName("data") val data: List<ConversationInfo>?
)

data class ContactsResponse(
    @SerializedName("success") val success: Boolean,
    @SerializedName("data") val data: List<ContactInfo>?
)

data class MessagesResponse(
    @SerializedName("success") val success: Boolean,
    @SerializedName("data") val data: List<ChatMessageInfo>?
)

data class MarkReadResponse(
    @SerializedName("success") val success: Boolean,
    @SerializedName("readCount") val readCount: Int?
)

// SignalR hub message types
data class SignalRInvocation(
    val type: Int,
    val target: String?,
    val arguments: List<Any>?
)

data class SignalRReceiveMessage(
    val messageId: Int,
    val conversationId: Int,
    val senderId: Int,
    val fullName: String?,
    val content: String?,
    val messageType: String?,
    val signalingData: String?,
    val sentAt: String?,
    val isRead: Boolean
)

data class SignalRReadReceipt(
    val conversationId: Int,
    val readByEmployeeId: Int,
    val readAt: String?
)

data class SignalRTypingInfo(
    val conversationId: Int,
    val employeeId: Int,
    val fullName: String?
)

data class SignalRCallInfo(
    val fromEmployeeId: Int,
    val fromFullName: String?,
    val signalingType: String?,
    val signalingData: String?,
    val conversationId: Int?
)

data class SignalRCallResponse(
    val fromEmployeeId: Int,
    val fromFullName: String?,
    val signalingType: String?,
    val signalingData: String?
)

data class SignalRCallEnded(
    val fromEmployeeId: Int,
    val conversationId: Int?
)

// Notification models
data class SignalRNotification(
    val notificationId: Int,
    val eventType: String?,
    val title: String?,
    val message: String?,
    val entityType: String?,
    val entityId: String?,
    val actionUrl: String?,
    val latitude: Double?,
    val longitude: Double?,
    val locationLabel: String?,
    val createdAt: String?,
    val isRead: Boolean
)

data class NotificationItem(
    @SerializedName("notificationId") val notificationId: Int,
    @SerializedName("eventType") val eventType: String?,
    @SerializedName("title") val title: String?,
    @SerializedName("message") val message: String?,
    @SerializedName("entityType") val entityType: String?,
    @SerializedName("entityId") val entityId: String?,
    @SerializedName("actionUrl") val actionUrl: String?,
    @SerializedName("latitude") val latitude: Double?,
    @SerializedName("longitude") val longitude: Double?,
    @SerializedName("locationLabel") val locationLabel: String?,
    @SerializedName("createdAt") val createdAt: String?,
    @SerializedName("isRead") val isRead: Boolean
)

data class UnreadCountResponse(
    @SerializedName("count") val count: Int
)

data class NotificationsListResponse(
    @SerializedName("success") val success: Boolean,
    @SerializedName("data") val data: List<NotificationItem>?
)

sealed class ChatCallState {
    data object Idle : ChatCallState()
    data class Incoming(
        val fromEmployeeId: Int,
        val fromFullName: String,
        val conversationId: Int?
    ) : ChatCallState()
    data class Outgoing(
        val toEmployeeId: Int,
        val toFullName: String,
        val conversationId: Int?
    ) : ChatCallState()
    data class Connected(
        val withEmployeeId: Int,
        val withFullName: String
    ) : ChatCallState()
}
