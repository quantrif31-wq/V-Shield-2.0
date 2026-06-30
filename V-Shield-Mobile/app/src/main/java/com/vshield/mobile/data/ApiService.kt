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
    suspend fun getMyProfile(): Response<ApiResponse<EmployeeInfo>>

    @GET("api/employees/lookup")
    suspend fun lookupEmployees(@Query("q") query: String): Response<EmployeeLookupResponse>

    @POST("api/dynamic-qr/my")
    suspend fun getMyQr(): Response<QrResponse>

    @GET("api/vehicles/my-vehicles")
    suspend fun getMyVehicles(): Response<MyVehiclesResponse>

    @GET("api/vehicle-delegations/outgoing")
    suspend fun getOutgoingDelegations(): Response<DelegationListResponse>

    @GET("api/vehicle-delegations/incoming")
    suspend fun getIncomingDelegations(): Response<DelegationListResponse>

    @POST("api/vehicle-delegations")
    suspend fun createDelegation(@Body request: CreateDelegationRequest): Response<DelegationResponse>

    @PUT("api/vehicle-delegations/{id}/approve")
    suspend fun approveDelegation(@Path("id") id: Int): Response<DelegationResponse>

    @PUT("api/vehicle-delegations/{id}/reject")
    suspend fun rejectDelegation(@Path("id") id: Int): Response<DelegationResponse>

    @GET("api/leave-requests/my")
    suspend fun getMyLeaveRequests(): Response<LeaveRequestResponse>

    @POST("api/leave-requests")
    suspend fun createLeaveRequest(@Body request: CreateLeaveRequest): Response<ApiResponse<EmptyData>>

    @GET("api/leave-requests/leave-types")
    suspend fun getLeaveTypes(): Response<LeaveTypeResponse>

    @GET("api/schedules/my")
    suspend fun getMySchedule(): Response<ScheduleResponse>

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

    @POST("api/chat/conversations/{id}/read")
    suspend fun markConversationRead(@Path("id") conversationId: Int): Response<MarkReadResponse>

    // Notification endpoints
    @GET("api/notifications")
    suspend fun getNotifications(
        @Query("skip") skip: Int = 0,
        @Query("take") take: Int = 50
    ): Response<NotificationsListResponse>

    @GET("api/notifications/unread-count")
    suspend fun getUnreadCount(): Response<ApiResponse<UnreadCountResponse>>

    @POST("api/notifications/{id}/read")
    suspend fun markNotificationRead(@Path("id") id: Int): Response<ApiResponse<EmptyData>>

    @POST("api/notifications/read-all")
    suspend fun markAllNotificationsRead(): Response<ApiResponse<EmptyData>>
}
