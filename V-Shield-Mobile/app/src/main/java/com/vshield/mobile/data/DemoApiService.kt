package com.vshield.mobile.data

import com.vshield.mobile.data.model.*
import com.vshield.mobile.security.OfflineQrConfig
import com.vshield.mobile.security.OfflineQrGenerator
import retrofit2.Response
import java.time.Instant
import java.time.LocalDate
import java.time.format.DateTimeFormatter

object DemoApiService : ApiService {

    const val DEMO_USERNAME = "admin"
    const val DEMO_EMPLOYEE_ID = 5734
    const val DEMO_SECRET = "GR4HS5IT6JU7KVALWBMXCNYDOZEP2FQ3"

    private val isoFormatter = DateTimeFormatter.ISO_INSTANT

    private var nextDelegationId = 100
    private var nextLeaveId = 200
    private var nextMessageId = 1000
    private var nextConversationId = 30

    private val delegations = mutableListOf(
        DelegationInfo(
            delegationId = 365,
            vehicleId = 3126,
            licensePlate = "29A-10000",
            fromEmployeeId = 5734,
            fromEmployeeName = "Tran Thi Binh 001",
            toEmployeeId = 5737,
            toEmployeeName = "Hoang Duc Giang 004",
            reason = "On-call support coverage for late shift",
            status = "Pending",
            createdAt = "2026-08-10T03:58:15.2014041Z",
            approvedAt = null
        ),
        DelegationInfo(
            delegationId = 374,
            vehicleId = 3135,
            licensePlate = "29A-10333",
            fromEmployeeId = 5743,
            fromEmployeeName = "Do Van Mai 010",
            toEmployeeId = 5734,
            toEmployeeName = "Tran Thi Binh 001",
            reason = "On-call support coverage for late shift",
            status = "Approved",
            createdAt = "2026-08-01T03:58:15.2014041Z",
            approvedAt = "2026-08-01T05:00:00.0000000Z"
        ),
        DelegationInfo(
            delegationId = 371,
            vehicleId = 3132,
            licensePlate = "29A-10222",
            fromEmployeeId = 5740,
            fromEmployeeName = "Vo Gia Lan 007",
            toEmployeeId = 5734,
            toEmployeeName = "Tran Thi Binh 001",
            reason = "On-call support coverage for late shift",
            status = "Rejected",
            createdAt = "2026-08-04T03:58:15.2014041Z",
            approvedAt = null
        ),
        DelegationInfo(
            delegationId = 368,
            vehicleId = 3129,
            licensePlate = "29A-10111",
            fromEmployeeId = 5737,
            fromEmployeeName = "Hoang Duc Giang 004",
            toEmployeeId = 5734,
            toEmployeeName = "Tran Thi Binh 001",
            reason = "On-call support coverage for late shift",
            status = "Approved",
            createdAt = "2026-08-07T03:58:15.2014041Z",
            approvedAt = "2026-08-07T05:00:00.0000000Z"
        ),
        DelegationInfo(
            delegationId = 377,
            vehicleId = 3138,
            licensePlate = "29A-10444",
            fromEmployeeId = 5746,
            fromEmployeeName = "Tran Quang Quan 013",
            toEmployeeId = 5734,
            toEmployeeName = "Tran Thi Binh 001",
            reason = "On-call support coverage for late shift",
            status = "Revoked",
            createdAt = "2026-07-29T03:58:15.2014041Z",
            approvedAt = null
        )
    )

    private val leaveRequests = mutableListOf(
        LeaveRequestInfo(
            leaveRequestId = 487,
            leaveTypeName = "AnnualLeave",
            startDate = "2026-08-13",
            endDate = "2026-08-13",
            reason = "Nghỉ phép cá nhân",
            status = "Pending",
            createdAt = "2026-08-11T06:28:31.1419263Z"
        ),
        LeaveRequestInfo(
            leaveRequestId = 486,
            leaveTypeName = "SickLeave",
            startDate = "2026-07-30",
            endDate = "2026-07-31",
            reason = "Nghỉ ốm",
            status = "Approved",
            createdAt = "2026-07-28T02:00:00.0000000Z"
        ),
        LeaveRequestInfo(
            leaveRequestId = 485,
            leaveTypeName = "PersonalLeave",
            startDate = "2026-07-20",
            endDate = "2026-07-21",
            reason = "Việc gia đình",
            status = "Rejected",
            createdAt = "2026-07-18T09:15:00.0000000Z"
        )
    )

    private val leaveTypes = listOf(
        LeaveType(1, "AnnualLeave", "Nghỉ phép năm"),
        LeaveType(2, "SickLeave", "Nghỉ ốm"),
        LeaveType(3, "PersonalLeave", "Nghỉ phép cá nhân"),
        LeaveType(4, "UnpaidLeave", "Nghỉ không lương")
    )

    private val conversations = mutableListOf(
        ConversationInfo(
            conversationId = 27,
            createdAt = "2026-08-10T21:58:15.2014041Z",
            title = "SOC Shift Coordination",
            lastMessage = LastMessageInfo(
                messageId = 81,
                content = "Manifest mismatch vehicle is still queued at HP gate. Export package requested.",
                sentAt = "2026-08-11T02:16:15.2014041Z",
                messageType = "Text",
                senderName = "Pham Quang Dung 003",
                senderId = 5736
            ),
            participants = listOf(
                ParticipantInfo(5734, "Tran Thi Binh 001"),
                ParticipantInfo(5735, "Le Minh Chau 002"),
                ParticipantInfo(5736, "Pham Quang Dung 003"),
                ParticipantInfo(5737, "Hoang Duc Giang 004")
            ),
            unreadCount = 1,
            lastReadAt = "2026-08-11T03:43:15.2014041Z"
        ),
        ConversationInfo(
            conversationId = 28,
            createdAt = "2026-08-11T06:28:55.1215111Z",
            title = null,
            lastMessage = LastMessageInfo(
                messageId = 83,
                content = "hihi",
                sentAt = "2026-08-11T06:29:00.2718954Z",
                messageType = "Text",
                senderName = "Tran Thi Binh 001",
                senderId = 5734
            ),
            participants = listOf(
                ParticipantInfo(5734, "Tran Thi Binh 001"),
                ParticipantInfo(5790, "Bui Gia Dung 057")
            ),
            unreadCount = 0,
            lastReadAt = "2026-08-11T06:28:55.1991135Z"
        )
    )

    private val messagesByConversation = mutableMapOf<Int, MutableList<ChatMessageInfo>>(
        27 to mutableListOf(
            ChatMessageInfo(
                messageId = 79,
                senderId = 5734,
                senderName = "Tran Thi Binh 001",
                content = "Tailgating clip is uploaded. Need one guard to verify badge owner near turnstile A.",
                messageType = "Text",
                signalingData = null,
                sentAt = "2026-08-10T23:40:15.2014041Z",
                isRead = true,
                readAt = "2026-08-11T03:43:15.2014041Z"
            ),
            ChatMessageInfo(
                messageId = 80,
                senderId = 5735,
                senderName = "Le Minh Chau 002",
                content = "Dispatch acknowledged. Approaching location now and will report back in 5 minutes.",
                messageType = "Text",
                signalingData = null,
                sentAt = "2026-08-10T23:33:15.2014041Z",
                isRead = true,
                readAt = "2026-08-11T03:43:15.2014041Z"
            ),
            ChatMessageInfo(
                messageId = 81,
                senderId = 5736,
                senderName = "Pham Quang Dung 003",
                content = "Manifest mismatch vehicle is still queued at HP gate. Export package requested.",
                messageType = "Text",
                signalingData = null,
                sentAt = "2026-08-11T02:16:15.2014041Z",
                isRead = false,
                readAt = null
            )
        ),
        28 to mutableListOf(
            ChatMessageInfo(
                messageId = 82,
                senderId = 5734,
                senderName = "Tran Thi Binh 001",
                content = "hjfjy",
                messageType = "Text",
                signalingData = null,
                sentAt = "2026-08-11T06:28:57.5915626Z",
                isRead = false,
                readAt = null
            ),
            ChatMessageInfo(
                messageId = 83,
                senderId = 5734,
                senderName = "Tran Thi Binh 001",
                content = "hihi",
                messageType = "Text",
                signalingData = null,
                sentAt = "2026-08-11T06:29:00.2718954Z",
                isRead = false,
                readAt = null
            )
        )
    )

    private val notifications = mutableListOf(
        NotificationItem(
            id = 406,
            title = "Đơn nghỉ phép mới từ Tran Thi Binh 001",
            body = "Tran Thi Binh 001 xin nghỉ AnnualLeave từ 13/08 đến 13/08: Nghỉ phép cá nhân",
            category = "Approval",
            severity = "caution",
            referenceType = "LeaveRequest",
            referenceId = "487",
            actionUrl = "/attendance/leave-approvals",
            latitude = null,
            longitude = null,
            locationLabel = null,
            createdAt = "2026-08-11T06:28:31.1955186Z",
            readAt = null,
            isRead = false
        ),
        NotificationItem(
            id = 391,
            title = "Báo động uy hiếp",
            body = "Phát hiện uy hiếp tại Access Point #12 — Nhân viên Nguyễn Văn An",
            category = "Alarm",
            severity = null,
            referenceType = "Alarm",
            referenceId = null,
            actionUrl = null,
            latitude = 21.0285,
            longitude = 105.8048,
            locationLabel = "Tòa nhà HN Admin - Tầng 1",
            createdAt = "2026-08-11T03:53:15.2014041Z",
            readAt = null,
            isRead = false
        ),
        NotificationItem(
            id = 392,
            title = "Yêu cầu xuất bằng chứng mới",
            body = "Nhân viên Trần Thị Bình yêu cầu xuất video camera #HN-CAM-03",
            category = "Approval",
            severity = null,
            referenceType = "Evidence",
            referenceId = null,
            actionUrl = "/evidence/export",
            latitude = null,
            longitude = null,
            locationLabel = null,
            createdAt = "2026-08-11T03:28:15.2014041Z",
            readAt = null,
            isRead = false
        ),
        NotificationItem(
            id = 393,
            title = "Đơn nghỉ phép mới",
            body = "Nhân viên Lê Văn Cường xin nghỉ ốm từ 30/06 đến 01/07",
            category = "Approval",
            severity = null,
            referenceType = "LeaveRequest",
            referenceId = null,
            actionUrl = "/attendance/leave-approvals",
            latitude = null,
            longitude = null,
            locationLabel = null,
            createdAt = "2026-08-11T01:58:15.2014041Z",
            readAt = null,
            isRead = false
        ),
        NotificationItem(
            id = 394,
            title = "Yêu cầu nhận đồ thất lạc",
            body = "Có yêu cầu nhận lại điện thoại iPhone từ tủ đồ #L3",
            category = "Approval",
            severity = null,
            referenceType = "LostFound",
            referenceId = null,
            actionUrl = null,
            latitude = null,
            longitude = null,
            locationLabel = null,
            createdAt = "2026-08-10T23:58:15.2014041Z",
            readAt = "2026-08-11T00:05:00.0000000Z",
            isRead = true
        ),
        NotificationItem(
            id = 395,
            title = "Hệ thống đồng bộ thành công",
            body = "Đồng bộ danh sách hẹn trước và dashboard hoàn tất.",
            category = "System",
            severity = null,
            referenceType = null,
            referenceId = null,
            actionUrl = null,
            latitude = null,
            longitude = null,
            locationLabel = null,
            createdAt = "2026-08-10T19:58:15.2014041Z",
            readAt = "2026-08-10T20:00:00.0000000Z",
            isRead = true
        )
    )

    private val securityAlerts = mutableListOf(
        SecurityAlertItem(
            id = "duress-51",
            kind = "duress",
            severity = "critical",
            title = "Báo động duress tại cổng chính",
            message = "Nhân viên kích hoạt báo động duress tại Access Point #12 — cần kiểm tra ngay lập tức.",
            occurredAtUtc = "2026-08-11T03:53:15.2014041Z",
            route = "/security/incident/51",
            locationLabel = "Tòa nhà HN Admin - Tầng 1",
            zoneName = "Khu vực sản xuất"
        ),
        SecurityAlertItem(
            id = "alarm-184",
            kind = "alarm",
            severity = "high",
            title = "Thiết bị offline",
            message = "Security device Gate A QR Controller is offline.",
            occurredAtUtc = "2026-08-11T04:03:04.0511277Z",
            route = "/security/devices/184",
            locationLabel = null,
            zoneName = null
        ),
        SecurityAlertItem(
            id = "alarm-179",
            kind = "alarm",
            severity = "critical",
            title = "Reader khu vực hạn chế offline",
            message = "Restricted Zone Reader has been offline for two hours.",
            occurredAtUtc = "2026-08-11T03:38:15.2014041Z",
            route = "/security/devices/179",
            locationLabel = null,
            zoneName = null
        ),
        SecurityAlertItem(
            id = "alarm-182",
            kind = "alarm",
            severity = "medium",
            title = "Khách vượt thời gian",
            message = "Visit 2000 visitor Hoang Quang An exceeded approved visit window.",
            occurredAtUtc = "2026-08-11T03:59:04.1288703Z",
            route = "/visits/2000",
            locationLabel = null,
            zoneName = null
        )
    )

    private val employees = listOf(
        EmployeeInfo(
            employeeId = 5734,
            fullName = "Tran Thi Binh 001",
            email = "employee001@vshield-demo.vn",
            phoneNumber = "0985458942",
            positionName = "Manager",
            departmentName = "Security Operations",
            status = true,
            avatarUrl = null
        )
    )

    private val contacts = listOf(
        ContactInfo(5735, "Le Minh Chau 002", "employee002@vshield-demo.vn", "0994126299", "Supervisor", "Human Resources"),
        ContactInfo(5736, "Pham Quang Dung 003", "employee003@vshield-demo.vn", "0914899566", "Security Officer", "Production"),
        ContactInfo(5737, "Hoang Duc Giang 004", "employee004@vshield-demo.vn", "0933213203", "Engineer", "Quality Assurance"),
        ContactInfo(5738, "Phan Thanh Hanh 005", "employee005@vshield-demo.vn", "0924964843", "Technician", "Warehouse"),
        ContactInfo(5739, "Vu Anh Khanh 006", "employee006@vshield-demo.vn", "0946343282", "Operator", "Maintenance"),
        ContactInfo(5740, "Vo Gia Lan 007", "employee007@vshield-demo.vn", "0954327088", "HR Specialist", "Information Technology"),
        ContactInfo(5741, "Dang Bao Linh 008", "employee008@vshield-demo.vn", "0915800574", "Accountant", "Finance"),
        ContactInfo(5742, "Bui Hoai Long 009", "employee009@vshield-demo.vn", "0937321774", "Warehouse Coordinator", "Sales"),
        ContactInfo(5743, "Do Van Mai 010", "employee010@vshield-demo.vn", "0936886204", "Director", "Executive Office"),
        ContactInfo(5744, "Ngo Thi Nam 011", "employee011@vshield-demo.vn", "0943383441", "Manager", "Security Operations"),
        ContactInfo(5745, "Nguyen Minh Phuc 012", "employee012@vshield-demo.vn", "0919160258", "Supervisor", "Human Resources"),
        ContactInfo(5746, "Tran Quang Quan 013", "employee013@vshield-demo.vn", "0957920195", "Security Officer", "Production"),
        ContactInfo(5747, "Le Duc Son 014", "employee014@vshield-demo.vn", "0922267233", "Engineer", "Quality Assurance"),
        ContactInfo(5748, "Pham Thanh Trang 015", "employee015@vshield-demo.vn", "0924791732", "Technician", "Warehouse"),
        ContactInfo(5749, "Hoang Anh Tuan 016", "employee016@vshield-demo.vn", "0958476553", "Operator", "Maintenance"),
        ContactInfo(5750, "Phan Gia Vy 017", "employee017@vshield-demo.vn", "0942334213", "HR Specialist", "Information Technology"),
        ContactInfo(5751, "Vu Bao An 018", "employee018@vshield-demo.vn", "0964246208", "Accountant", "Finance"),
        ContactInfo(5752, "Vo Hoai Binh 019", "employee019@vshield-demo.vn", "0977633797", "Warehouse Coordinator", "Sales"),
        ContactInfo(5753, "Dang Van Chau 020", "employee020@vshield-demo.vn", "0999640491", "Director", "Executive Office"),
        ContactInfo(5754, "Bui Thi Dung 021", "employee021@vshield-demo.vn", "0996416454", "Manager", "Security Operations")
    )

    private val myVehicles = listOf(
        VehicleInfo(vehicleId = 3126, licensePlate = "29A-10000", vehicleTypeName = "Truck", vehicleTypeId = 4, color = null, brand = null, status = "Active"),
        VehicleInfo(vehicleId = 3210, licensePlate = "29B12345", vehicleTypeName = "Car", vehicleTypeId = 2, color = "White", brand = "Toyota", status = "Active")
    )

    private fun demoLoginData(username: String): LoginData = LoginData(
        token = "demo-token-$DEMO_EMPLOYEE_ID",
        refreshToken = "demo-refresh-$DEMO_EMPLOYEE_ID",
        expiresAt = isoFormatter.format(Instant.now().plusSeconds(3600)),
        refreshTokenExpiresAt = isoFormatter.format(Instant.now().plusSeconds(86400)),
        employeeId = DEMO_EMPLOYEE_ID,
        role = "Admin",
        username = username,
        fullName = "Tran Thi Binh 001",
        requiresMfa = false,
        requiresMfaSetup = false,
        mfaSetupSecret = null,
        mfaSetupUri = null,
        message = null,
        hasOperationalScopeAssignments = false,
        operationalTaskKeys = emptyList()
    )

    private fun demoQrData(): QrData = OfflineQrGenerator.generate(
        OfflineQrConfig(
            employeeId = DEMO_EMPLOYEE_ID,
            employeeName = "Tran Thi Binh 001",
            secretKey = DEMO_SECRET,
            timeStepSeconds = 30,
            digits = 6
        )
    )

    private fun demoSchedule(): List<ScheduleItem> {
        val today = LocalDate.now()
        val shifts = listOf(
            Triple("Factory Morning", "06:00", "14:00"),
            Triple("Office", "08:00", "17:00"),
            Triple("Factory Afternoon", "14:00", "22:00")
        )
        return (0..13).map { offset ->
            val date = today.plusDays(offset.toLong())
            val shift = shifts[offset % shifts.size]
            ScheduleItem(
                scheduleId = 1000 + offset,
                date = date.toString(),
                shiftName = shift.first,
                startTime = shift.second,
                endTime = shift.third,
                location = if (shift.first.startsWith("Factory")) "Nhà máy 1 - Khu sản xuất" else "Tòa nhà văn phòng",
                status = "Scheduled",
                note = null
            )
        }
    }

    override suspend fun login(request: LoginRequest): Response<LoginData> {
        if (request.username.isBlank() || request.password.isBlank()) {
            return Response.error(400, okhttp3.ResponseBody.create(null, "Tên đăng nhập và mật khẩu không được để trống"))
        }
        return Response.success(demoLoginData(request.username))
    }

    override suspend fun refresh(request: RefreshTokenRequest): Response<LoginData> =
        Response.success(demoLoginData(DEMO_USERNAME))

    override suspend fun getMyProfile(): Response<ApiResponse<EmployeeInfo>> =
        Response.success(ApiResponse(true, null, employees.first()))

    override suspend fun lookupEmployees(query: String): Response<EmployeeLookupResponse> {
        val q = query.trim().lowercase()
        val result = contacts
            .filter { q.isBlank() || it.fullName.lowercase().contains(q) || it.departmentName?.lowercase()?.contains(q) == true }
            .map { EmployeeLookup(it.employeeId, it.fullName, it.departmentName) }
        return Response.success(EmployeeLookupResponse(true, result))
    }

    override suspend fun getMyQr(): Response<QrResponse> =
        Response.success(QrResponse(true, null, demoQrData()))

    override suspend fun getMyOfflineQrBootstrap(): Response<OfflineQrBootstrapResponse> =
        Response.success(
            OfflineQrBootstrapResponse(
                true,
                null,
                OfflineQrBootstrapData(
                    employeeId = DEMO_EMPLOYEE_ID,
                    employeeName = "Tran Thi Binh 001",
                    secretKey = DEMO_SECRET,
                    timeStepSeconds = 30,
                    digits = 6,
                    issuedAtUtc = isoFormatter.format(Instant.now())
                )
            )
        )

    override suspend fun getMyVehicles(): Response<MyVehiclesResponse> =
        Response.success(MyVehiclesResponse(true, myVehicles))

    override suspend fun getOutgoingDelegations(): Response<DelegationListResponse> =
        Response.success(DelegationListResponse(true, delegations.filter { it.fromEmployeeId == DEMO_EMPLOYEE_ID }, null))

    override suspend fun getIncomingDelegations(): Response<DelegationListResponse> =
        Response.success(DelegationListResponse(true, delegations.filter { it.toEmployeeId == DEMO_EMPLOYEE_ID }, null))

    override suspend fun createDelegation(request: CreateDelegationRequest): Response<DelegationResponse> {
        val vehicle = myVehicles.find { it.vehicleId == request.vehicleId }
        val target = contacts.find { it.employeeId == request.toEmployeeId }
        delegations.add(
            0,
            DelegationInfo(
                delegationId = nextDelegationId++,
                vehicleId = request.vehicleId,
                licensePlate = vehicle?.licensePlate,
                fromEmployeeId = DEMO_EMPLOYEE_ID,
                fromEmployeeName = "Tran Thi Binh 001",
                toEmployeeId = request.toEmployeeId,
                toEmployeeName = target?.fullName ?: "NV#${request.toEmployeeId}",
                reason = request.reason,
                status = "Pending",
                createdAt = isoFormatter.format(Instant.now()),
                approvedAt = null
            )
        )
        return Response.success(DelegationResponse(true, "Tạo ủy quyền thành công (demo)", null))
    }

    override suspend fun approveDelegation(id: Int): Response<DelegationResponse> {
        val item = delegations.find { it.delegationId == id }
        if (item == null) {
            return Response.success(DelegationResponse(false, "Không tìm thấy yêu cầu ủy quyền", null))
        }
        val index = delegations.indexOf(item)
        delegations[index] = item.copy(
            status = "Approved",
            approvedAt = isoFormatter.format(Instant.now())
        )
        return Response.success(DelegationResponse(true, "Đã duyệt ủy quyền (demo)", delegations[index]))
    }

    override suspend fun rejectDelegation(id: Int): Response<DelegationResponse> {
        val item = delegations.find { it.delegationId == id }
        if (item == null) {
            return Response.success(DelegationResponse(false, "Không tìm thấy yêu cầu ủy quyền", null))
        }
        val index = delegations.indexOf(item)
        delegations[index] = item.copy(status = "Rejected")
        return Response.success(DelegationResponse(true, "Đã từ chối ủy quyền (demo)", delegations[index]))
    }

    override suspend fun getMyLeaveRequests(): Response<LeaveRequestResponse> =
        Response.success(LeaveRequestResponse(true, leaveRequests, null))

    override suspend fun createLeaveRequest(request: CreateLeaveRequest): Response<ApiResponse<EmptyData>> {
        val type = leaveTypes.find { it.leaveTypeId == request.leaveTypeId }
        leaveRequests.add(
            0,
            LeaveRequestInfo(
                leaveRequestId = nextLeaveId++,
                leaveTypeName = type?.typeName ?: "LeaveType#${request.leaveTypeId}",
                startDate = request.startDate,
                endDate = request.endDate,
                reason = request.reason,
                status = "Pending",
                createdAt = isoFormatter.format(Instant.now())
            )
        )
        return Response.success(ApiResponse(true, null, null))
    }

    override suspend fun getLeaveTypes(): Response<LeaveTypeResponse> =
        Response.success(LeaveTypeResponse(true, leaveTypes))

    override suspend fun getMySchedule(): Response<ScheduleResponse> =
        Response.success(ScheduleResponse(true, demoSchedule()))

    override suspend fun getChatContacts(): Response<ContactsResponse> =
        Response.success(ContactsResponse(true, contacts))

    override suspend fun getConversations(): Response<ConversationsResponse> =
        Response.success(ConversationsResponse(true, conversations))

    override suspend fun createConversation(request: CreateConversationRequest): Response<ApiChatResponse<CreateConversationResponse>> {
        val newParticipants = (listOf(5734) + request.employeeIds).distinct().mapNotNull { id ->
            contacts.find { it.employeeId == id }?.let { ParticipantInfo(it.employeeId, it.fullName) }
                ?: employees.find { it.employeeId == id }?.let { ParticipantInfo(it.employeeId, it.fullName) }
        }
        if (newParticipants.isEmpty()) {
            return Response.success(ApiChatResponse(false, "Không tìm thấy liên hệ", null))
        }
        val convId = nextConversationId++
        conversations.add(
            0,
            ConversationInfo(
                conversationId = convId,
                createdAt = isoFormatter.format(Instant.now()),
                title = request.title,
                lastMessage = null,
                participants = newParticipants,
                unreadCount = 0,
                lastReadAt = isoFormatter.format(Instant.now())
            )
        )
        messagesByConversation[convId] = mutableListOf()
        return Response.success(ApiChatResponse(true, null, CreateConversationResponse(convId, false)))
    }

    override suspend fun getConversationMessages(
        conversationId: Int,
        skip: Int,
        take: Int
    ): Response<MessagesResponse> =
        Response.success(MessagesResponse(true, messagesByConversation[conversationId].orEmpty()))

    override suspend fun sendConversationMessage(
        conversationId: Int,
        request: SendMessageRequest
    ): Response<ApiChatResponse<ChatMessageInfo>> {
        val list = messagesByConversation.getOrPut(conversationId) { mutableListOf() }
        val message = ChatMessageInfo(
            messageId = nextMessageId++,
            senderId = DEMO_EMPLOYEE_ID,
            senderName = "Tran Thi Binh 001",
            content = request.content,
            messageType = request.messageType,
            signalingData = request.signalingData,
            sentAt = isoFormatter.format(Instant.now()),
            isRead = false,
            readAt = null
        )
        list.add(message)
        val convIndex = conversations.indexOfFirst { it.conversationId == conversationId }
        if (convIndex >= 0) {
            val conv = conversations[convIndex]
            conversations[convIndex] = conv.copy(
                lastMessage = LastMessageInfo(
                    messageId = message.messageId,
                    content = message.content,
                    sentAt = message.sentAt,
                    messageType = message.messageType,
                    senderName = message.senderName,
                    senderId = message.senderId
                ),
                unreadCount = 0
            )
        }
        return Response.success(ApiChatResponse(true, null, message))
    }

    override suspend fun markConversationRead(conversationId: Int): Response<MarkReadResponse> {
        val list = messagesByConversation[conversationId].orEmpty()
        val readAt = isoFormatter.format(Instant.now())
        list.forEachIndexed { index, msg ->
            if (msg.senderId != DEMO_EMPLOYEE_ID && !msg.isRead) {
                list[index] = msg.copy(isRead = true, readAt = readAt)
            }
        }
        val convIndex = conversations.indexOfFirst { it.conversationId == conversationId }
        if (convIndex >= 0) {
            conversations[convIndex] = conversations[convIndex].copy(
                unreadCount = 0,
                lastReadAt = isoFormatter.format(Instant.now())
            )
        }
        return Response.success(MarkReadResponse(true, 1))
    }

    override suspend fun getNotifications(skip: Int, take: Int): Response<NotificationsListResponse> =
        Response.success(NotificationsListResponse(true, notifications))

    override suspend fun getUnreadCount(): Response<UnreadCountEnvelope> {
        val unread = notifications.count { !it.isRead }
        return Response.success(UnreadCountEnvelope(true, unread))
    }

    override suspend fun markNotificationRead(id: Long): Response<ApiResponse<EmptyData>> {
        val index = notifications.indexOfFirst { it.id == id }
        if (index >= 0) {
            notifications[index] = notifications[index].copy(isRead = true, readAt = isoFormatter.format(Instant.now()))
        }
        return Response.success(ApiResponse(true, null, null))
    }

    override suspend fun markAllNotificationsRead(): Response<ApiResponse<EmptyData>> {
        val now = isoFormatter.format(Instant.now())
        notifications.forEachIndexed { index, item ->
            if (!item.isRead) {
                notifications[index] = item.copy(isRead = true, readAt = now)
            }
        }
        return Response.success(ApiResponse(true, null, null))
    }

    override suspend fun getActiveSecurityAlerts(): Response<SecurityAlertsResponse> =
        Response.success(
            SecurityAlertsResponse(
                generatedAtUtc = isoFormatter.format(Instant.now()),
                criticalCount = securityAlerts.count { it.severity == "critical" },
                items = securityAlerts
            )
        )

    override suspend fun acknowledgeAlarm(id: Long): Response<Unit> {
        securityAlerts.removeAll { it.id == "alarm-$id" }
        return Response.success(Unit)
    }

    override suspend fun acknowledgeDuressEvent(id: Long): Response<Unit> {
        securityAlerts.removeAll { it.id == "duress-$id" }
        return Response.success(Unit)
    }
}
