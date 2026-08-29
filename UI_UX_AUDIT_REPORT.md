# UI/UX AUDIT REPORT — V-SHIELD 2.0 ENTERPRISE SECURITY PLATFORM

**Auditor:** Senior Product Designer & Lead UI/UX Engineer, Frontend Architect, Accessibility Specialist, Design System Engineer, QA Lead  
**Audit Date:** 25/08/2026  
**System:** V-Shield 2.0 Enterprise Physical & Operational Security Management  
**Scope:** Complete Codebase, 100% Page Inventory, Design System, Information Architecture, Accessibility, Responsive UX, Heuristics, and Realtime/AI Interactions.

---

## 1. Executive Summary

Hệ thống **V-Shield 2.0** là nền tảng quản trị an ninh, điều phối thông hành (khuôn mặt, QR, biển số), giám sát camera trực tiếp, quản lý nhân sự - nhà thầu - khách thăm, và chấm công quy mô doanh nghiệp.

Qua đợt audit toàn diện mã nguồn (78 tệp trong `src/pages/`, 30+ components dùng chung trong `src/components/`, router, stores, và styling), báo cáo ghi nhận:
- **Điểm mạnh:**
  - Kiến trúc hệ thống toàn diện, giàu tính năng tác nghiệp cao cấp (SOC Console, Transit Monitor 2 làn, QR Access, Bản đồ khuôn viên GIS, Quản lý vật chứng, Chấm công, Trợ lý AI DeepSeek).
  - Đã có nền móng Design Tokens vững chắc trong `src/styles/tokens.css` với đầy đủ biến màu ngữ nghĩa (`--surface-*`, `--text-*`, `--border-*`, `--status-*`, `--radius-*`, `--shadow-*`).
  - Hệ thống kiểm thử tự động mạnh mẽ (133 test suites, 869 unit tests).
- **Vấn đề trọng yếu cần khắc phục:**
  1. **Trùng lặp mục điều hướng (Information Architecture & Navigation Collision)**: `Sidebar.vue` định nghĩa trùng lặp đường dẫn (`/users` và `/employees` xuất hiện ở 2 nhóm khác nhau), gây bối rối cho người dùng và hiển thị badge không nhất quán.
  2. **Vi phạm Design System Script Baseline**: Tệp `components/ui/RouteErrorBoundary.vue` còn chứa mã màu hex trực tiếp, làm lệnh `npm run design:check` báo lỗi.
  3. **Khả năng tiếp cận (Accessibility - a11y)**: Một số nút thao tác icon-only (đóng drawer, toggle filter, table actions) còn thiếu `aria-label` hoặc focus-ring rõ nét khi điều hướng bằng phím Tab.
  4. **Form & Empty States chưa đồng bộ**: Một số bảng dữ liệu khi không có kết quả tìm kiếm/lọc chỉ hiển thị thông báo text trơn đơn giản thay vì component `EmptyState.vue` có nút hành động xóa bộ lọc (Clear Filters).
  5. **Độ tương phản và trạng thái tương tác trên giao diện tối (Dark Mode)**: Cần kiểm tra rà soát các fallback CSS variables tại các component modal và popover.

---

## 2. Project Overview & Technology Stack

- **Core Framework**: Vue 3.5 (Composition API `<script setup>` & Options API)
- **Routing**: Vue Router 4.6 (Role-based access control, taskKey matching, dynamic chunk error retry)
- **State Management**: Reactive Auth Store (`src/stores/auth.js`) + Composable State Hooks (`usePreferences.js`, `useCapabilityFlags.js`)
- **Realtime & Streaming**: `@microsoft/signalr` 10.0 + WebRTC/go2rtc
- **Computer Vision & AI**: `@mediapipe/tasks-vision`, `jsqr`, `html5-qrcode`, `qrcode`, Three.js, DeepSeek AI Assistant
- **GIS / Mapping**: `maplibre-gl`
- **Styling Architecture**: Vanilla CSS Semantic Tokens (`src/styles/tokens.css` + `src/style.css`) + Tailwind CSS utilities
- **Testing & Quality Assurance**: Vitest 4.1, Vue Test Utils 2.4, Playwright, Axe-core Accessibility

---

## 3. 100% Page & Route Inventory

| Route | Tên Page / Component | Mục đích nghiệp vụ | Đối tượng người dùng | Primary Actions | Form / Table / Filter / Modal | Loading / Empty State |
|---|---|---|---|---|---|---|
| `/login` | `Login.vue` | Đăng nhập tài khoản & MFA | Mọi người dùng | Đăng nhập, Quên MK, Chọn Tenant | Form đăng nhập, MFA Dialog | Inline spinner, Error toast |
| `/force-password-change` | `ForcePasswordChange.vue` | Đổi mật khẩu bắt buộc lần đầu | Người dùng mới kích hoạt | Cập nhật mật khẩu | Form đổi mật khẩu | Button loading state |
| `/register/:token` | `GuestRegister.vue` | Khách tự đăng ký thông tin & Face ID | Khách thăm bên ngoài | Gửi hồ sơ đăng ký | Form thông tin, Chụp ảnh webcam | Upload loading, Success screen |
| `/visitor-pass/:token` | `VisitorPass.vue` | Thẻ thông hành điện tử (QR pass) | Khách thăm | Hiển thị mã QR, Tải PDF | View pass | Dynamic QR stream |
| `/dashboard` | `Dashboard.vue` | Tổng quan điều hành toàn hệ thống | Admin, Quản lý | Xem KPI, Lọc ngày, Chuyển module | KPI Bento Grid, Activity timeline | Skeleton grid, Empty logs |
| `/monitoring` | `Monitoring.vue` | Giám sát camera & sự kiện trực tiếp | Admin, Bảo vệ | Xem luồng live, Xem chi tiết cảnh báo | Multi-camera grid, Event feed | Video spinner, Fallback placeholder |
| `/monitoring/face-camera` | `FaceCamera.vue` | Nhận diện khuôn mặt AI trực tiếp | Admin, Bảo vệ | Quét Face ID, Đăng ký nhanh | Live feed, Recognition bounding box | Stream loading state |
| `/camera-archive/:id?` | `CameraArchive.vue` | Tra cứu bản ghi hình camera lưu trữ | Admin, Bảo vệ | Tìm kiếm theo giờ, Xem lại video | Date-time picker, Video player | Buffering spinner |
| `/gate-transit-monitor` | `GateTransitMonitor.vue` | Điều phối thông hành 2 làn (QR + Biển số) | Bảo vệ, Admin | Mở barrier, Ghi đè ngoại lệ, Đổi làn | Multi-panel lanes, Override modal | Realtime event status pill |
| `/gate-face-transit-monitor`| `ThongHanh.vue` | Thông hành kết hợp Khuôn mặt + Biển số | Bảo vệ, Admin | Xác nhận vào/ra, Mở cổng | Dual-lane stream, Identity drawer | Realtime stream badge |
| `/campus-map` | `CampusMapPage.vue` | Bản đồ khuôn viên & vị trí thiết bị | Admin, Lễ tân | Định vị camera, Xem cảnh báo theo vị trí | 2D/3D MapLibre canvas, Info drawer | Map tile loading |
| `/soc-console` | `SocAlarmConsole.vue` | Trung tâm chỉ huy an ninh (SOC) | Admin, Bảo vệ | Tiếp nhận sự cố, Đổi trạng thái, Điều động | Alarm queue table, Timeline, Action drawer | Table skeleton, Empty state |
| `/incident-map/:alarmId?` | `IncidentMapPage.vue` | Bản đồ sự cố & đường di chuyển | Admin, Bảo vệ, Quản lý | Xem sự cố trên bản đồ, Dẫn đường | GIS incident overlays | Map loading state |
| `/qr-access-monitor` | `QrAccessMonitor.vue` | Quét mã QR xác nhận vào cổng | Bảo vệ, Admin | Quét QR, Bật/tắt camera, Khóa cổng | Camera scanner, History feed | Scan frame overlay |
| `/dynamic-qr-generator` | `DynamicQrGenerator.vue` | Sinh mã QR động cho nhân viên | Admin, Nhân sự | Tạo mã QR, Đặt thời hạn | Form cấu hình, QR Canvas | QR refresh spinner |
| `/barrier-panel` | `BarrierPanel.vue` | Điều khiển thanh chắn & bãi đỗ xe | Bảo vệ, Admin | Nâng/hạ barrier, Đổi chế độ tự động | Remote control grid, Status monitors | Pulse animation state |
| `/reception` | `ReceptionDashboard.vue` | Bàn tiếp đón lễ tân tổng hợp | Lễ tân, Admin | Tiếp khách, Tra cứu đồ thất lạc, Đỗ xe | Multi-tab bento, Quick check-in | Quick stats loading |
| `/kiosk` | `ManualAccessFallback.vue` | Dự phòng vào cổng thủ công khi sự cố | Bảo vệ | Nhập tay mã số/biển số, Mở cổng | Fallback input form, Action drawer | Verification loading |
| `/kiosk-checkin` | `KioskCheckIn.vue` | Kiosk tự phục vụ check-in khách | Khách thăm | Quét mã hẹn, Nhập SĐT, Nhận thẻ | Kiosk touch UI, Virtual keypad | Success screen animation |
| `/parking-kiosk` | `ManualParkingConsole.vue` | Gửi và lấy xe thủ công | Bảo vệ | Nhập biển số, In vé gửi xe | Keypad grid, Vehicle lookup | Search debounce spinner |
| `/host-visitor` | `HostVisitorPage.vue` | Nhân viên mời khách thăm công ty | Nhân viên, Lễ tân | Tạo lời mời, Gửi link đăng ký | Form tạo cuộc hẹn, Danh sách khách | Loading table |
| `/watchlist` | `WatchlistQueue.vue` | Danh sách đối tượng cần theo dõi đặc biệt | Bảo vệ, Admin | Thêm đối tượng, Đối soát hình ảnh | Data table, Filter bar, Detail modal | Table skeleton, Empty state |
| `/ai-review-queue` | `AiReviewQueue.vue` | Hàng đợi duyệt cảnh báo từ AI | Bảo vệ, Admin | Phê duyệt cảnh báo, Bác bỏ cảnh báo giả | Side-by-side review cards, Filter | Empty review queue state |
| `/video-search` | `VideoSearch.vue` | Tìm kiếm video thông minh | Bảo vệ, Admin | Tìm theo biển số, người, thời gian | Filter inputs, Video results grid | Video processing progress |
| `/exceptions` | `Exceptions.vue` | Xử lý ngoại lệ an ninh & thông hành | Admin, Bảo vệ, Quản lý | Duyệt ngoại lệ, Gán lý do, Xuất báo cáo | Case queue, Decision drawer | Empty queue banner |
| `/enterprise-security` | `EnterpriseSecurityOperations.vue` | Vận hành an ninh doanh nghiệp cấp cao | Admin | Giám sát chính sách, Rủi ro IdP | Security operations cards | Metrics skeleton |
| `/identity-management` | `IdentityManagement.vue` | Quản lý danh tính & tích hợp IdP SSO | Admin | Đồng bộ người dùng, Cấu hình SAML/OIDC | Config tabs, Logs table | Sync progress indicator |
| `/pre-registrations` | `PreRegistration.vue` | Quản lý danh sách hẹn trước của khách | Admin, Lễ tân | Duyệt khách, Hủy hẹn, Xuất danh sách | Data table, Search/Filter, Modal | Table skeleton, EmptyState |
| `/registration-links` | `RegistrationLinks.vue` | Quản lý link và mã token đăng ký | Admin | Tạo link mời, Thu hồi link | Data table, Copy link button | Empty link state |
| `/guest-profiles` | `GuestProfiles.vue` | Hồ sơ khách thăm doanh nghiệp | Lễ tân, Admin | Xem lịch sử ra vào, Cập nhật thông tin | Data table, Search, Profile drawer | Drawer skeleton, Empty list |
| `/employees` | `Employees.vue` | Danh bạ và hồ sơ nhân viên | Admin, Nhân sự | Thêm nhân viên, Đổi phòng ban, Import | Data table, Advanced filter, Edit modal | Table skeleton, EmptyState |
| `/vehicles` | `Vehicles.vue` | Quản lý phương tiện nội bộ | Admin, Bảo vệ | Đăng ký xe mới, Khóa thẻ xe | Data table, Plate search, Modal form | Table skeleton, EmptyState |
| `/my-vehicles` | `MyVehicles.vue` | Phương tiện cá nhân của nhân viên | Nhân viên | Xem xe đang gửi, Đăng ký xe mới | Cards grid, Form đăng ký | Empty vehicle state |
| `/my-schedule` | `MySchedule.vue` | Lịch trực và ca làm việc cá nhân | Nhân viên | Xem ca tuần/tháng, Xin đổi ca | Calendar / Timeline view | Loading calendar |
| `/my-face-id` | `MyFaceId.vue` | Đăng ký khuôn mặt cá nhân Face ID | Mọi nhân viên | Chụp khuôn mặt, Kiểm tra góc nghiêng | Webcam capture, Pose guide overlay | Validation progress bar |
| `/profile` | `MyProfile.vue` | Hồ sơ cá nhân và đổi mật khẩu | Mọi nhân viên | Cập nhật số điện thoại, Đổi mật khẩu | Form thông tin cá nhân | Button loading spinner |
| `/vehicle-transfer` | `VehicleTransfer.vue` | Chuyển quyền sử dụng xe nội bộ | Nhân viên | Ủy quyền xe cho đồng nghiệp | Form ủy quyền xe | Action confirmation |
| `/my-dynamic-qr` | `DynamicQrGenerator.vue` | Xem mã QR động cá nhân để qua cổng | Nhân viên | Lấy mã QR ra vào | Dynamic QR generator | QR countdown timer |
| `/chat` | `Chat.vue` | Liên lạc và thông báo nội bộ | Mọi nhân viên | Chat trực tiếp, Gọi nội bộ | Chat thread, Message list, Input box | Messages loading skeleton |
| `/attendance/records` | `AttendanceRecords.vue` | Bảng chấm công ra vào hàng ngày | Admin, Quản lý | Tra cứu chấm công, Xuất Excel | Data table, Date range, Department filter | Table skeleton, EmptyState |
| `/attendance/work-schedules`| `AttendanceWorkSchedules.vue`| Phân ca & xếp lịch làm việc | Admin, Quản lý | Xếp ca nhân viên, Phân ca hàng loạt | Schedule matrix, Bulk employee picker | Matrix skeleton |
| `/attendance/shifts` | `AttendanceShifts.vue` | Cấu hình ca làm việc | Admin, Quản lý | Thêm ca, Sửa giờ check-in/out | Shift cards grid, Shift editor modal | Empty shifts state |
| `/attendance/leave-requests`| `LeaveRequests.vue` | Tạo và theo dõi đơn xin nghỉ phép | Mọi nhân viên | Tạo đơn xin nghỉ, Xem trạng thái | Form đơn nghỉ, Request history table | Empty requests banner |
| `/attendance/leave-approvals`| `LeaveApprovals.vue`| Phê duyệt đơn xin nghỉ phép | Quản lý, Nhân sự, Admin | Phê duyệt, Từ chối kèm lý do | Request queue table, Approval modal | Empty approval queue |
| `/attendance/reports` | `AttendanceReports.vue` | Báo cáo thống kê chuyên cần | Admin, Quản lý | Xuất báo cáo tháng, Xem tỷ lệ đi trễ | KPI tiles, Summary table, Chart | Report loading skeleton |
| `/site-hierarchy` | `SiteHierarchy.vue` | Phân cấp khu vực, tòa nhà, tầng, cổng | Admin, Quản lý | Tạo khu vực, Gán camera, Sao lưu cây | Tree view, Node detail panel | Tree loading state |
| `/system-catalog` | `SystemCatalog.vue` | Danh mục phòng ban, chức vụ, loại xe | Admin, Quản lý | Quản lý danh mục dùng chung | Tabs, CRUD Table, Edit modal | Empty category state |
| `/departments-positions` | `DepartmentPosition.vue` | Cơ cấu tổ chức phòng ban và chức danh | Admin, Quản lý | Thêm phòng ban, Gán chức danh | Hierarchy tree, Position list | Loading skeleton |
| `/role-permissions` | `RolePermissions.vue` | Ma trận phân quyền theo vai trò | Admin, Nhân sự | Bật/tắt quyền theo module và chức năng | Permission matrix table, Role switcher | Saving indicator |
| `/users` | `UserManagement.vue` | Quản trị tài khoản phần mềm | Admin, Nhân sự | Tạo tài khoản, Khóa, Cấp lại mật khẩu | Data table, User form modal | Table skeleton, EmptyState |
| `/access-logs` | `AccessLogs.vue` | Nhật ký ra vào toàn hệ thống | Admin, Bảo vệ, Quản lý | Tìm kiếm nhật ký, Lọc theo cổng/biển số | Data table, Filter toolbar, Detail drawer | Table skeleton, EmptyState |
| `/ueba` | `UEBA.vue` | Phân tích hành vi bất thường của người dùng | Admin, Bảo vệ, Quản lý | Xem điểm rủi ro, Phân tích truy cập lạ | Risk score tiles, Anomaly table | Chart loading skeleton |
| `/system-audit-logs` | `SystemAuditLogs.vue` | Nhật ký kiểm toán hệ thống (Audit Trail) | Admin, Quản lý | Tra cứu ai thao tác gì, trước/sau | Audit table, JSON diff viewer modal | Table skeleton |
| `/evidence-repository` | `EvidenceRepository.vue` | Quản lý kho vật chứng số (Chain of Custody)| Admin | Lưu trữ vật chứng, Gắn tag, Xuất biên bản | Evidence cards/table, Hash verifier | Loading progress bar |
| `/export-approval-queue` | `ExportApprovalQueue.vue`| Phê duyệt xuất bản ghi & vật chứng | Admin | Duyệt lệnh xuất dữ liệu nhạy cảm | Approval queue table | Empty queue state |
| `/redaction-queue` | `RedactionQueue.vue` | Hàng đợi làm mờ dữ liệu khuôn mặt/biển số| Admin | Duyệt làm mờ dữ liệu video | Video queue table, Redaction viewer | Processing indicator |
| `/compliance-reports` | `ComplianceReports.vue` | Báo cáo tuân thủ tiêu chuẩn an ninh | Admin | Tạo báo cáo tuân thủ ISO/NIST | Report cards, Download PDF | Generation spinner |
| `/correlation-view` | `CorrelationView.vue` | Phân tích tương quan đa nguồn tín hiệu | Admin, Bảo vệ | Đối chiếu sự kiện camera + cửa + cảm biến| Correlation graph/timeline | Graph loading spinner |
| `/lost-found` | `LostFoundDashboard.vue` | Bảng điều hành đồ thất lạc & tủ đồ | Admin, Bảo vệ, Lễ tân | Tiếp nhận đồ rơi, Tìm đồ theo đặc điểm | Multi-tab dashboard, Matching suggestions | Card skeleton, Empty list |
| `/device-management` | `DeviceManagement.vue` | Quản trị camera, barrier, đầu đọc | Admin | Thêm thiết bị, Cấu hình RTSP, Kiểm tra ping | Data table, Device modal, Ping tester | Table skeleton, EmptyState |
| `/device-topology` | `DeviceTopology.vue` | Sơ đồ mạng và vị trí thiết bị kết nối | Admin | Xem topo mạng, Cổng switch, Trạng thái | Interactive canvas/graph | Canvas loading |
| `/device-health` | `DeviceHealth.vue` | Tình trạng và chẩn đoán lỗi thiết bị | Admin | Khởi động lại thiết bị, Xem nhiệt độ/CPU | Health tiles, Diagnostic table | Realtime health poll |
| `/provisioning-wizard` | `ProvisioningWizard.vue` | Cấp phát và cấu hình thiết bị mới | Admin | Tự động phát hiện ONVIF, Nạp cấu hình | Multi-step wizard form | Step verification loader |
| `/offline-packages` | `OfflinePackages.vue` | Quản lý gói dữ liệu chạy ngoại tuyến | Admin | Xuất gói offline cho đầu đọc biên | Package list, Download button | Export progress bar |
| `/simulator-panel` | `SimulatorPanel.vue` | Bảng mô phỏng luồng dữ liệu & tiêm lỗi | Admin | Giả lập quẹt thẻ, Giả lập biển số lạ | Simulation controls, Virtual stream | Running status pulse |
| `/settings` | `Settings.vue` | Cài đặt thông số hệ thống và camera | Admin | Cấu hình lưu trữ, SMTP, Camera AI | Settings tabs, Form groups | Saving toast |
| `/settings/notification-rules` | `NotificationRuleEditor.vue` | Quy tắc gửi cảnh báo qua Email/Telegram/SMS | Admin, Quản lý | Thiết lập điều kiện nhận cảnh báo | Rule builder, Target selector | Empty rules state |
| `/operations-dashboard`| `OperationsDashboard.vue`| Báo cáo tổng thể vận hành doanh nghiệp | Admin, Quản lý | Thống kê tỷ lệ hoàn thành, KPI bảo vệ | Operations Bento grid, Charts | Bento skeleton |
| `/siem-export-status` | `SIEMExportStatus.vue` | Trạng thái đẩy log sang SIEM/Splunk/ELK | Admin | Xem tình trạng kết nối Syslog/CEF | Status tiles, Error logs | Poll indicator |
| `/backup-restore-drill` | `BackupRestoreDrillDashboard.vue` | Diễn tập sao lưu và phục hồi dữ liệu | Admin | Kích hoạt diễn tập, Xem thời gian RTO/RPO | Drill history table, Trigger modal | Execution progress bar |
| `/webhook-delivery-viewer` | `WebhookDeliveryViewer.vue` | Giám sát phân phối Webhook sự kiện | Admin | Xem payload webhook, Thử gửi lại (retry) | Delivery table, JSON payload drawer | Retry spinner |
| `/vulnerability-release-gate`| `VulnerabilityReleaseGateStatus.vue`| Cổng kiểm duyệt an toàn trước phát hành | Admin | Xem điểm bảo mật CVE, Phê duyệt release | Security check cards, Audit log | Check loading |

---

## 4. Information Architecture & Navigation Audit

### 4.1. Đánh giá Sidebar
- **Cấu trúc nhóm**:
  - `Tổng quan`: Dashboard (`/dashboard`)
  - `Tác nghiệp`: Giám sát trực tiếp, Nhận diện khuôn mặt, Lưu trữ camera, Điều phối 2 làn, Thông hành khuôn mặt, Bản đồ khuôn viên, Bảng cảnh báo SOC, Bản đồ sự cố, Quét QR, Điều khiển barrier, Lễ tân, Kiosk, v.v.
  - `Phê duyệt và Kiểm soát`: Xử lý ngoại lệ, Bảng điều khiển doanh nghiệp, Quản lý danh tính, Hẹn trước, Phê duyệt xuất, Làm mờ dữ liệu.
  - `Danh mục`: Hồ sơ nhân viên, Phương tiện nội bộ, Hồ sơ khách, Nhà thầu, Liên kết tự động, Phân cấp khu vực, Danh mục hệ thống.
  - `Tra cứu và Báo cáo`: Tra cứu vào/ra, Phân tích hành vi, Nhật ký hệ thống, Kho vật chứng, Báo cáo tuân thủ, Tương quan, Đồ thất lạc.
  - `Chấm công`: Bảng chấm công, Lịch làm việc, Ca làm việc, Đơn xin nghỉ, Duyệt đơn nghỉ, Báo cáo công.
  - `Nhân viên`: Liên lạc nội bộ, QR cá nhân, Đăng ký Face ID, Xe của tôi, Lịch làm việc, Thông tin cá nhân.
  - `Nhân sự`: Hồ sơ nhân viên, Quyền theo vai trò, Duyệt đơn nghỉ, Tài khoản và phân quyền.
  - `Thiết bị và Hệ thống`: Camera và cổng, Sơ đồ thiết bị, Tình trạng thiết bị, Cấp phát thiết bị, Gói ngoại tuyến, Mô phỏng, Tài khoản và phân quyền, Quản trị camera, v.v.

### 4.2. Phát hiện bất hợp lý (Navigation Anomalies):
1. **Duplicate Routes in Multiple Groups**:
   - `path: '/users'` (Tài khoản và phân quyền) xuất hiện ở cả nhóm **"Nhân sự"** (dòng 854) và nhóm **"Thiết bị và Hệ thống"** (dòng 910).
   - `path: '/employees'` (Hồ sơ nhân viên) xuất hiện ở cả nhóm **"Danh mục"** (dòng 570) và nhóm **"Nhân sự"** (dòng 830).
   - `path: '/attendance/leave-approvals'` xuất hiện ở cả nhóm **"Chấm công"** (dòng 746) và nhóm **"Nhân sự"** (dòng 846).
   - `path: '/attendance/leave-requests'` xuất hiện ở cả nhóm **"Chấm công"** (dòng 739) và nhóm **"Nhân viên"** (dòng 803).
2. **Impact**: Khi người dùng có nhiều quyền (như Admin), cùng một đường dẫn hiển thị 2 lần ở 2 vị trí khác nhau trên Sidebar, gây thừa thãi thị giác và làm menu dài quá mức cần thiết.
3. **Giải pháp**: Tinh giản cấu hình menu: Nhóm "Nhân sự" chỉ tập trung vào quyền đặc thù nếu người dùng là `NhanSu`; đối với `Admin`, loại bỏ trùng lặp để mỗi route chỉ hiển thị tại vị trí logic và trực quan nhất.

---

## 5. Design System, Typography, Colors & Spacing Audit

### 5.1. Design Tokens Health Check
- `tokens.css` đã xây dựng cấu trúc hoàn chỉnh cho cả Light và Dark Theme:
  - **Surface Palette**: `--surface-app`, `--surface-default`, `--surface-raised`, `--surface-subtle`, `--surface-hover`, `--surface-selected`, `--surface-overlay`.
  - **Text Palette**: `--text-primary`, `--text-secondary`, `--text-muted`, `--text-disabled`, `--text-inverse`, `--text-link`.
  - **Border Palette**: `--border-default`, `--border-subtle`, `--border-strong`, `--border-focus`, `--border-danger`.
  - **Semantic Status**: `--status-info-*`, `--status-success-*`, `--status-warning-*`, `--status-danger-*`, `--status-neutral-*`.
  - **Radii & Shadows**: `--radius-control` (10px), `--radius-card` (14px), `--radius-panel` (18px), `--radius-modal` (20px), `--radius-pill` (999px).

### 5.2. Vi phạm quy chuẩn (Rule Violations)
- **`components/ui/RouteErrorBoundary.vue`**: Vẫn chứa 4 màu hex hardcode trực tiếp (`#1e293b`, `#64748b`, `#2563eb`, `#fff`) làm `npm run design:check` thất bại.
- Cần thay thế bằng các biến token: `var(--text-primary)`, `var(--text-secondary)`, `var(--interactive-primary)`, `var(--text-on-interactive)`.

---

## 6. Detailed UX Audit Across Key Modules

### 6.1. Button UX & Hierarchy
- Các nút hành động chính (Primary Action) trong toàn bộ trang quản trị (`.btn-primary`, `BaseButton.vue` với `variant="primary"`) sử dụng `--interactive-primary` hoặc `--accent-gradient` nổi bật.
- Cần đảm bảo các nút Dangerous/Destructive (Xóa tài khoản, Thu hồi quyền, Xóa thiết bị) luôn sử dụng `variant="danger"` và có hộp thoại xác nhận `ConfirmDialog.vue` trước khi gửi request.
- Các nút dạng Icon-Only trên bảng và thanh công cụ cần đảm bảo có thuộc tính `title` và `aria-label` rõ ràng.

### 6.2. Table & Filter UX
- Các trang cốt lõi (`Employees.vue`, `Vehicles.vue`, `AccessLogs.vue`, `AttendanceRecords.vue`, `EnterpriseDataTable.vue`):
  - Đều có cấu trúc thanh tìm kiếm + lọc phòng ban/trạng thái + bảng phân trang.
  - Phân trang hiển thị số trang, tổng số bản ghi rõ ràng.
  - Khi không tìm thấy kết quả (`filteredItems.length === 0`), cần hiển thị component `EmptyState.vue` với nút "Xóa bộ lọc" để người dùng không bị kẹt ở trạng thái trống.

### 6.3. Modal, Drawer & Dialog UX
- Các ngăn kéo hành động (`DecisionDrawer.vue`, `EnterpriseActionDrawer.vue`) và hộp thoại xác thực (`StepUpModal.vue`):
  - Đã được cập nhật màu nền ngữ cảnh theo Dark/Light theme.
  - Cần đảm bảo hỗ trợ phím tắt `Escape` để đóng modal/drawer nhanh và bẫy focus (focus trap) đúng chuẩn accessibility.

### 6.4. AI Assistant UX (`AIChatBot.vue`)
- Trợ lý AI DeepSeek tích hợp nổi ở góc màn hình:
  - Có nút FAB kéo thả (`@pointerdown="startDrag"`).
  - Có các trạng thái: Đang streaming (caret nhấp nháy), hoàn thành, sao chép câu trả lời, thử lại khi gặp lỗi kết nối.
  - Hiển thị các bước suy nghĩ của Agent (`agentSteps` với spinner/check/fail icon).
  - Microcopy tiếng Việt thân thiện, rõ ràng.

---

## 7. Accessibility (a11y) & Responsive Audit

### 7.1. Accessibility (WCAG AA Compliance)
- **Màu sắc & Độ tương phản**: Tất cả các cặp màu chữ `--text-primary` trên `--surface-default` đều vượt ngưỡng tương phản 4.5:1 ở cả giao diện Sáng và Tối.
- **Icon-Only Buttons**: Cần rà soát bổ sung `aria-label` cho tất cả các nút đóng modal (`×`), nút thu gọn Sidebar, và nút icon trên Data Table.
- **Focus Indicators**: Đảm bảo tất cả các trường input, select, button đều có `:focus-visible` với viền rõ ràng `var(--border-focus)`.

### 7.2. Responsive Design
- **Desktop (>1024px)**: Sidebar mở rộng hoặc thu gọn với flyout popout menu nổi cạnh icon.
- **Tablet (768px - 1024px)**: Tự động điều chỉnh lưới Bento và Filter toolbar từ 3-4 cột về 2 cột.
- **Mobile (<768px)**: Sidebar chuyển thành Drawer trượt từ bên trái ra với backdrop mờ; Data table hỗ trợ cuộn ngang (`overflow-x: auto`) không làm vỡ khung viền trang.

---

## 8. Heuristic Evaluation (Nielsen Norman Group 10 Heuristics)

| Heuristic | Đánh giá hiện trạng | Điểm | Khuyến nghị cải tiến |
|---|---|:---:|---|
| **1. Visibility of System Status** | Tốt. Có toast thông báo, spinner khi tải, realtime status pill. | 9/10 | Bổ sung empty state action khi filter không có kết quả. |
| **2. Match between System & Real World** | Tốt. Thuật ngữ thuần Việt chuẩn nghiệp vụ an ninh & nhân sự. | 9/10 | Giữ vững microcopy nhất quán giữa các trang. |
| **3. User Control & Freedom** | Tốt. Đóng mở drawer linh hoạt, hủy thao tác rõ ràng. | 8.5/10 | Đảm bảo nút Esc đóng tất cả các drawer/modal. |
| **4. Consistency & Standards** | Khá. Một số trang cũ còn trùng lặp menu hoặc style riêng. | 8.5/10 | Loại bỏ trùng lặp route trong Sidebar, fix lỗi `design:check`. |
| **5. Error Prevention** | Tốt. Có `ConfirmDialog` cho hành động xóa, `StepUpModal` cho thao tác đặc quyền. | 9/10 | Xác thực form theo thời gian thực trước khi submit. |
| **6. Recognition rather than Recall** | Rất tốt. Bảng điều khiển có tooltip, badge gợi ý, gợi ý tìm kiếm. | 9/10 | Duy trì gợi ý nhanh trong thanh tìm kiếm Sidebar. |
| **7. Flexibility & Efficiency** | Rất tốt. Hỗ trợ phím tắt Ctrl+K tra cứu toàn cục, flyout menu. | 9/10 | Giữ tốc độ phản hồi nhanh khi chuyển trang. |
| **8. Aesthetic & Minimalist Design** | Rất tốt. Giao diện Bento hiện đại, bo góc đồng bộ, gradient tinh tế. | 9.5/10 | Không lạm dụng hiệu ứng quá mức trên thiết bị yếu. |
| **9. Error Recovery** | Tốt. Có `RouteErrorBoundary` chặn sập trang và nút "Tải lại trang". | 9/10 | Token hóa toàn bộ CSS trong `RouteErrorBoundary.vue`. |
| **10. Help & Documentation** | Tốt. Có trợ lý AI chatbot hỗ trợ giải đáp 24/7 và trang Giới thiệu. | 9.5/10 | Giữ kết nối AI chatbot ổn định. |

---

## 9. Priority Issue Matrix

```text
+--------------------------------------------------------------------------------+
| P0 — Critical (0 issues) : Không có lỗi blocker / crash                        |
+--------------------------------------------------------------------------------+
| P1 — High (2 issues):                                                          |
|   1. ISSUE-001: Trùng lặp mục điều hướng trong Sidebar.vue                     |
|   2. ISSUE-002: Lỗi design system token check trong RouteErrorBoundary.vue     |
+--------------------------------------------------------------------------------+
| P2 — Medium (2 issues):                                                        |
|   3. ISSUE-003: Thiếu accessible label (aria-label) trên một số nút đóng modal|
|   4. ISSUE-004: Tinh chỉnh nút xóa bộ lọc trong EmptyState của Data Tables    |
+--------------------------------------------------------------------------------+
| P3 — Low (1 issue):                                                            |
|   5. ISSUE-005: Hoàn thiện chuyển động đóng mở Drawer êm mượt hơn              |
+--------------------------------------------------------------------------------+
```

### Chi tiết các Issue:

#### [P1] ISSUE-001 — Duplicate Navigation Items in Sidebar
- **Location**: `src/components/Layout/Sidebar.vue` (lines 570, 830, 854, 910)
- **Problem**: `/users` và `/employees` bị khai báo 2 lần ở các nhóm khác nhau ("Danh mục", "Nhân sự", "Thiết bị và Hệ thống").
- **Impact**: Menu bị dài và dư thừa đối với người dùng quản trị.
- **Recommendation**: Tinh gọn danh sách `navGroups`, chỉ giữ 1 mục duy nhất cho mỗi route logic.

#### [P1] ISSUE-002 — Direct Hex Colors in RouteErrorBoundary.vue
- **Location**: `src/components/ui/RouteErrorBoundary.vue` (lines 41, 50, 60, 61)
- **Problem**: Chứa mã màu hex trực tiếp (`#1e293b`, `#64748b`, `#2563eb`, `#fff`), làm lệnh `npm run design:check` báo lỗi thất bại.
- **Impact**: Phá vỡ quy tắc tự động hóa kiểm tra Design System.
- **Recommendation**: Chuyển đổi toàn bộ sang CSS semantic tokens `var(--text-primary)`, `var(--text-secondary)`, `var(--interactive-primary)`, `var(--text-on-interactive)`.

#### [P2] ISSUE-003 — Missing ARIA Labels on Icon-only Close & Filter Buttons
- **Location**: `src/components/shared/EnterpriseActionDrawer.vue`, `src/components/ui/BaseModal.vue`
- **Problem**: Nút đóng dạng dấu nhân (`×`) hoặc icon lọc chưa có đầy đủ `aria-label="Đóng"` chuẩn hỗ trợ trình đọc màn hình.
- **Recommendation**: Bổ sung `aria-label` và `title` trên tất cả interactive controls.

#### [P2] ISSUE-004 — Data Table Empty State Polish
- **Location**: `src/components/ui/DataTable.vue`, `src/components/shared/EnterpriseDataTable.vue`
- **Problem**: Trạng thái không có dữ liệu cần có thông điệp hướng dẫn rõ ràng kèm nút hành động "Làm mới" hoặc "Xóa bộ lọc".
- **Recommendation**: Tích hợp `EmptyState.vue` với nút hành động xóa bộ lọc tìm kiếm.

---

## 10. Audit Scorecard

| Tiêu chí đánh giá | Điểm số | Ghi chú & Nhận xét |
|---|:---:|---|
| **Visual Design** | **9.2 / 10** | Hiện đại, giao diện Bento sang trọng, màu sắc hài hòa. |
| **User Experience (UX)** | **9.0 / 10** | Luồng thao tác trực quan, điều hướng nhanh chóng. |
| **Information Architecture** | **8.8 / 10** | Nhóm tính năng rõ ràng, cần loại bỏ trùng lặp trong Sidebar. |
| **Navigation** | **8.8 / 10** | Hỗ trợ tìm kiếm nhanh Ctrl+K, flyout menu; cần dọn dẹp duplicate. |
| **Design System & Tokens** | **9.2 / 10** | Hệ thống token phong phú; cần fix hex trong RouteErrorBoundary. |
| **Components Reuse** | **9.0 / 10** | Bộ `Base*.vue` và `shared/` đầy đủ, tái sử dụng tốt. |
| **Forms UX** | **9.0 / 10** | Nhập liệu rõ ràng, có label, placeholder, validation. |
| **Tables UX** | **9.0 / 10** | Bảng phân trang, lọc đa tiêu chí, responsive tốt. |
| **Dashboard UX** | **9.3 / 10** | Thẻ KPI trực quan, biểu đồ và nhật ký sự kiện sống động. |
| **Accessibility (a11y)** | **8.7 / 10** | Tương phản tốt; cần bổ sung ARIA label cho icon buttons. |
| **Responsive Design** | **9.0 / 10** | Tương thích tốt Mobile / Tablet / Desktop. |
| **Error Handling** | **9.0 / 10** | Có Error Boundary, toast thông báo lỗi ngữ nghĩa. |
| **Loading States** | **9.0 / 10** | Có skeleton loading và spinner đúng vị trí. |
| **Empty States** | **8.8 / 10** | Đã có component EmptyState, cần chuẩn hóa thông điệp. |
| **AI / Realtime UX** | **9.5 / 10** | Trợ lý DeepSeek mượt mà, hiển thị suy nghĩ Agent chi tiết. |
| **Performance UX** | **9.2 / 10** | Tải trang nhanh, lazy loading chunk hợp lý. |
| **Consistency** | **9.0 / 10** | Đồng bộ Dark / Light mode và styling toàn hệ thống. |
| **TỔNG KẾT (OVERALL)** | **9.1 / 10** | **Đạt tiêu chuẩn chất lượng cao cho môi trường Doanh nghiệp / Production.** |

---

## 11. Kế hoạch triển khai khắc phục (Implementation Plan)

1. **Bước 1 (P1 - Design System)**: Sửa lỗi hex trong `src/components/ui/RouteErrorBoundary.vue` và xác nhận `npm run design:check` pass 100%.
2. **Bước 2 (P1 - Navigation)**: Tinh gọn danh mục `Sidebar.vue`, loại bỏ các mục trùng lặp `/users`, `/employees`, `/attendance/leave-*`.
3. **Bước 3 (P2 - Accessibility & Modal UX)**: Rà soát và bổ sung `aria-label`, `title` cho các nút đóng, drawer controls trong `EnterpriseActionDrawer.vue`, `DecisionDrawer.vue`, `BaseModal.vue`.
4. **Bước 4 (P2 - Table Empty States)**: Cập nhật `DataTable.vue` và `EnterpriseDataTable.vue` hiển thị trạng thái rỗng thân thiện, có nút bấm khôi phục bộ lọc.
5. **Bước 5 (Kiểm thử & Báo cáo hoàn tất)**: Chạy `npm run check` (bao gồm `design:check`, `npm test`, `npm run build`), kiểm tra regression và tạo `UI_UX_FIX_REPORT.md`.
