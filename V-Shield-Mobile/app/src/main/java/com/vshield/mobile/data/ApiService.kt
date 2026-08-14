package com.vshield.mobile.data

import com.vshield.mobile.data.model.*
import retrofit2.Response
import retrofit2.http.*

interface ApiService {
    @POST("api/auth/login")
    suspend fun login(@Body request: LoginRequest): Response<LoginResponse>

    @POST("api/auth/refresh")
    suspend fun refresh(@Body request: RefreshTokenRequest): Response<LoginResponse>

    @GET("api/employees/me")
    suspend fun getMyProfile(): Response<EmployeeInfo>

    @GET("api/employees")
    suspend fun lookupEmployees(@Query("search") query: String): Response<List<EmployeeInfo>>

    @POST("api/dynamic-qr/my")
    suspend fun getMyQr(): Response<QrResponse>

    @GET("api/dynamic-qr/mobile-bootstrap")
    suspend fun getMyOfflineQrBootstrap(): Response<OfflineQrBootstrapResponse>

    @GET("api/vehicles/employee/{employeeId}")
    suspend fun getMyVehicles(@Path("employeeId") employeeId: Int): Response<List<VehicleInfo>>

    @GET("api/vehicle-delegations/outgoing")
    suspend fun getOutgoingDelegations(): Response<List<DelegationInfo>>

    @GET("api/vehicle-delegations/incoming")
    suspend fun getIncomingDelegations(): Response<List<DelegationInfo>>

    @POST("api/vehicle-delegations")
    suspend fun createDelegation(@Body request: CreateDelegationRequest): Response<DelegationInfo>

    @PATCH("api/vehicle-delegations/{id}/approve")
    suspend fun approveDelegation(@Path("id") id: Int): Response<DelegationActionResponse>

    @PATCH("api/vehicle-delegations/{id}/reject")
    suspend fun rejectDelegation(@Path("id") id: Int): Response<DelegationActionResponse>

    @GET("api/leave-requests/my")
    suspend fun getMyLeaveRequests(): Response<List<LeaveRequestInfo>>

    @POST("api/leave-requests")
    suspend fun createLeaveRequest(@Body request: CreateLeaveRequest): Response<LeaveRequestInfo>

    @GET("api/work-schedules/employee/{employeeId}")
    suspend fun getMySchedule(@Path("employeeId") employeeId: Int): Response<List<ScheduleItem>>

    @GET("api/chat/contacts")
    suspend fun getChatContacts(): Response<ContactsResponse>

    @GET("api/chat/conversations")
    suspend fun getConversations(): Response<ConversationsResponse>

    @POST("api/chat/conversations")
    suspend fun createConversation(@Body request: CreateConversationRequest): Response<ApiResponse<CreateConversationResponse>>

    @GET("api/chat/conversations/{id}/messages")
    suspend fun getConversationMessages(
        @Path("id") conversationId: Int,
        @Query("skip") skip: Int = 0,
        @Query("take") take: Int = 50
    ): Response<MessagesResponse>

    @POST("api/chat/conversations/{id}/messages")
    suspend fun sendConversationMessage(
        @Path("id") conversationId: Int,
        @Body request: SendMessageRequest
    ): Response<ApiChatResponse<ChatMessageInfo>>

    @POST("api/chat/conversations/{id}/read")
    suspend fun markConversationRead(@Path("id") conversationId: Int): Response<MarkReadResponse>

    // Notification endpoints
    @GET("api/notifications")
    suspend fun getNotifications(
        @Query("skip") skip: Int = 0,
        @Query("take") take: Int = 50
    ): Response<NotificationsListResponse>

    @GET("api/notifications/unread-count")
    suspend fun getUnreadCount(): Response<UnreadCountEnvelope>

    @POST("api/notifications/{id}/read")
    suspend fun markNotificationRead(@Path("id") id: Long): Response<ApiResponse<EmptyData>>

    @POST("api/notifications/read-all")
    suspend fun markAllNotificationsRead(): Response<ApiResponse<EmptyData>>

    @GET("api/security-alerts/active")
    suspend fun getActiveSecurityAlerts(): Response<SecurityAlertsResponse>

    @PATCH("api/enterprise/soc/alarms/{id}/acknowledge")
    suspend fun acknowledgeAlarm(@Path("id") id: Long): Response<Unit>

    @POST("api/enterprise/access-policy/duress-events/{id}/acknowledge")
    suspend fun acknowledgeDuressEvent(@Path("id") id: Long): Response<Unit>
}
