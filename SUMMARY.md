## Goal
Build a cross-platform internal chat + real-time notification system (alarm + approval alerts) with per-user routing and continuous alarm acknowledgment for all roles on Web and Android.

## Constraints & Preferences
- Follow existing codebase patterns (partial DbContext, model conventions, SignalR hubs, Vue Options/Setup API, Android ChatSignalRClient pattern).
- Vietnamese language for user-facing labels and menus.
- JWT auth with roles: Admin, QuanLy, BaoVe, LeTan, NhanVien, NhanSu.
- Build must succeed with zero errors (backend dotnet, frontend npm, Android assembleDebug).
- Android: Gradle 8.7 + Kotlin DSL, JDK 17 Temurin at `C:\Users\phamv\AppData\Local\Java\jdk-17.0.12\jdk-17.0.12+7`, Android SDK platform 34, build-tools 34.0.0.
- Notification system: single SignalR hub `/hubs/notifications`, rules-based routing, per-user delivery records + SignalR group push.
- All company employees must be findable in chat without needing to add friends; contacts must show department, position, email with filter UI.

## Progress
### Done
- Backend chat: ChatMessage, ChatConversation, ChatParticipant models; ChatHub (SendMessage, MarkRead, Typing, CallUser, CallResponse, EndCall); ChatController (contacts, conversations, messages, mark-read); migration AddChatSystem.
- Frontend Web chat: Chat.vue (sidebar + message thread + typing + unread badges + WebRTC signaling); chatApi.js (SignalR client + REST wrappers); route /chat all roles; sidebar "Liên lạc nội bộ".
- Android chat (7 files): ChatSignalRClient.kt (OkHttp WebSocket + SignalR JSON hub protocol with negotiate + reconnect), ChatViewModel.kt, ChatListScreen.kt (conversation list + contact picker dialog with position badge + department name + colored avatar + multi-field search + department filter chips), ConversationScreen.kt (message bubbles + typing + call button + auto-scroll), CallScreen.kt (incoming dialog + outgoing/connected overlay). BottomNavBar: "Trò chuyện" tab with unread badge. NavGraph: chat + conversation routes.
- **All three builds pass**: `dotnet build`, `npm run build`, `gradlew assembleDebug`.
- **Notification backend**: Notification + NotificationRule models; ApplicationDbContext.Notifications.cs partial; NotificationHub at `/hubs/notifications`; INotificationService + NotificationService (rule lookup → recipient resolution → DB insert → SignalR push via `notif_user_{userId}` group); NotificationsController (list, unread-count, mark-read, read-all); NotificationRulesController (CRUD + GET /suggestions?role=); EF migration `AddNotificationSystem`.
- **Seed default rules** (17 rules in `SeedDefaultNotificationRules`): Duress→BaoVe+Admin, EmergencyPass→BaoVe+Admin, DeviceOffline→BaoVe, VisitorOverstay→BaoVe, LeaveRequest→Admin/QuanLy/NhanSu, Intervention→Admin/QuanLy, LostFound→Admin, Evidence→Admin.
- **Seed demo notifications** (14 sample records across Admin, BaoVe, QuanLy, NhanVien, LeTan, NhanSu): mix of Alarm, Approval, System categories.
- **INotificationService integrated into 7 controllers**: LeaveRequestsController (submit/approve/reject), VehicleDelegationsController (create/approve/reject), EnterpriseInterventionController (create/accept/reject/execute), EnterpriseLostFoundController (claim create/approve/reject/complete/cancel), EnterpriseEvidenceController (export/redaction request/approve/perform/verify), EnterpriseAccessPolicyController (duress/emergency pass), EnterpriseSocController (alarm create).
- **Frontend notification UI**: notificationApi.js (REST + SignalR client), Header.vue (3 hardcoded mock items replaced with live SignalR + REST data), NotificationRuleEditor.vue at `/settings/notification-rules` (Admin/QuanLy only).
- **Android notification + alarm**: NotificationSignalRClient.kt (SignalR WebSocket), NotificationViewModel.kt (state + alarm ack), NotificationScreen.kt (lazy list with read/unread cards), NotificationAlarmService.kt (MediaPlayer looping alarm + continuous vibration), AlarmOverlay.kt (full-screen red dialog, must acknowledge to stop sound/vibration). BottomNavBar: "Thông báo" tab with unread badge.
- **SignalR fix**: Server sends `NewNotification`, frontend + Android now listen for `NewNotification` (was mismatched as ReceiveNotification).

### In Progress
- (none — waiting for next task)

### Blocked
- (none)

## Key Decisions
- Single NotificationHub at `/hubs/notifications` for ALL event types (alarm + approval), not separate hubs.
- Rule-based routing: EventType → lookup NotificationRule → resolve recipients (by userId or role) → create Notification record per user → push via SignalR group `notif_user_{userId}`. Each user only sees their own notifications (SignalR group isolation + REST `WHERE RecipientUserId == userId`).
- Regular notifications (`Approval.*`): one-shot device notification sound + short vibrate.
- Alarm notifications (`Alarm.*`): continuous looping alarm sound + pattern vibration until user presses "Xác nhận đã xử lý" on red fullscreen overlay.
- EventType naming: `"Alarm.Duress"`, `"Approval.LeaveRequest.Submitted"`, etc.
- 17 seed rules with sensible defaults; 14 demo notification records for testing.
- Chat contacts: all active employees returned (no friend system needed), enriched with departmentName + positionName + email.

## Next Steps
- (none)

## Critical Context
- `NotificationHub` at `/hubs/notifications` registered in Program.cs. Users join `notif_user_{userId}` group on connect.
- `NotificationService` resolves rules by EventType match (exact or `"*"` wildcard). Recipients come from userId or role lookup (`AppUser.Role` + IsActive).
- Server-side SignalR method name is `"NewNotification"` (both frontend `notificationApi.js` and Android `NotificationSignalRClient.kt` now listen for this name).
- `NotificationsController` endpoints: `GET /api/notifications?skip=0&take=50`, `GET /api/notifications/unread-count`, `POST /api/notifications/{id}/read`, `POST /api/notifications/read-all`.
- `NotificationRulesController` (Admin/QuanLy): GET, POST, PUT, DELETE + `GET /api/notification-rules/suggestions?role=BaoVe`.
- Seed rules + demo notifications in `DemoDataSeeder.SeedDefaultNotificationRules` and `SeedDemoNotifications`.
- Chat contacts endpoint `GET /api/chat/contacts` returns all active employees with EmployeeId, FullName, Email, phone, positionName, departmentName.
- Chat creates 1-1 conversation on first message (no pre-existing connection required).

## Relevant Files
- `API/API/API/Services/NotificationService.cs`: rule matching → recipient resolution → notification record → SignalR push.
- `API/API/API/Hubs/NotificationHub.cs`: user joins `notif_user_{userId}` group on connect.
- `API/API/API/Controllers/NotificationsController.cs`: per-user notification REST endpoints.
- `API/API/API/Controllers/NotificationRulesController.cs`: rule CRUD + suggestions.
- `API/API/API/Services/DemoDataSeeder.cs`: `SeedDefaultNotificationRules` (17 rules) + `SeedDemoNotifications` (14 records).
- `API/API/API/Controllers/ChatController.cs`: `GET /api/chat/contacts` returns all employees with name/dept/position/email.
- `View/src/pages/Chat.vue`: contacts tab currently shows name + dept only; needs profile enrichment + filters.
- `View/src/services/notificationApi.js`: SignalR client listening for `NewNotification` + REST wrappers.
- `View/src/components/Layout/Header.vue`: bell icon with unread badge, dropdown populated via SignalR + REST.
- `View/src/pages/NotificationRuleEditor.vue`: rule management page at `/settings/notification-rules`.
- `V-Shield-Mobile/app/src/main/java/com/vshield/mobile/data/NotificationSignalRClient.kt`: SignalR WebSocket for `/hubs/notifications`.
- `V-Shield-Mobile/app/src/main/java/com/vshield/mobile/viewmodel/NotificationViewModel.kt`: state management + alarm ack.
- `V-Shield-Mobile/app/src/main/java/com/vshield/mobile/service/NotificationAlarmService.kt`: looping alarm sound + pattern vibration.
- `V-Shield-Mobile/app/src/main/java/com/vshield/mobile/ui/screen/AlarmOverlay.kt`: full-screen critical alarm dialog (must acknowledge).
- `V-Shield-Mobile/app/src/main/java/com/vshield/mobile/ui/screen/NotificationScreen.kt`: notification list with read/unread cards.
- `V-Shield-Mobile/app/src/main/java/com/vshield/mobile/ui/screen/ChatListScreen.kt`: contact picker with position badge + department name + colored avatar + multi-field search + department filter chips.
