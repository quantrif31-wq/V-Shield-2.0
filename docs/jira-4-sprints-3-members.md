# Kế hoạch Jira V-Shield — 4 Sprint / 3 thành viên

Ngày lập báo cáo: 31/08/2026  
Thời lượng dự án: 2 tháng, từ 01/07/2026 đến 31/08/2026  
Nhịp thực hiện: 4 Sprint x 2 tuần; 26/08–31/08 dành cho nghiệm thu, xử lý dự phòng và ký bàn giao  
Mục tiêu: hoàn thành một phiên bản MVP có thể demo và bàn giao nội bộ.

## 1. Cấu trúc Epic

| Epic | Tên | Phạm vi |
|---|---|---|
| EPIC-1 | Kiến trúc Hệ thống & Tích hợp Thiết bị | Kiến trúc API/UI/DB, camera, go2rtc, cấu hình thiết bị, health check |
| EPIC-2 | Quản lý Ra vào & Luồng Khách | Nhân viên, khách, đăng ký trước, quyền ra/vào, quyết định tại cổng |
| EPIC-3 | Giám sát Thời gian thực & AI | Live monitoring, SignalR, QR, biển số, cảnh báo và bằng chứng |
| EPIC-4 | Quản trị, Báo cáo & Nhật ký | Người dùng, vai trò, audit log, dashboard và báo cáo |
| EPIC-5 | Kiểm thử, Tối ưu & Bàn giao | Test, security, Docker, UAT, tài liệu và bàn giao |

## 2. Phân vai 3 thành viên

| Thành viên | Vai trò chính | Mức tham gia | Trách nhiệm xuyên suốt |
|---|---|---:|---|
| Phạm Văn Thành | Developer 1 — Backend & Integration | 100% | API .NET, database, auth, access decision, camera/runtime và triển khai |
| Phạm Ngọc Hoài Anh | Developer 2 — Frontend & Quality | 100% | Vue UI, QR/LPR UI, dashboard, integration test và hỗ trợ triển khai |
| Hà Quang Sang | Documentation | 20% | Jira, đặc tả, hướng dẫn sử dụng, biên bản Sprint/UAT và hồ sơ bàn giao |

Quy tắc phối hợp:

- Mỗi issue có đúng một Assignee chịu trách nhiệm kết quả.
- Thành và Hoài Anh review chéo code; người viết không tự phê duyệt pull request của mình.
- Hai dev cùng chịu trách nhiệm test phần mình phát triển. Sang chỉ tổng hợp bằng chứng và tài liệu, không chịu trách nhiệm lập trình hay kiểm thử kỹ thuật.
- Capacity khởi điểm: Thành 10 SP, Hoài Anh 10 SP, Sang 2 SP mỗi Sprint; tổng nhóm khoảng 22 SP/Sprint.
- Khối lượng của Sang được giới hạn tối đa 20% thời gian làm việc và chỉ nhận issue có nhãn `documentation`.
- Giữ 15–20% thời gian mỗi Sprint cho review, sửa lỗi và tích hợp.

## 3. Sprint 1 — Nền tảng chạy được

Thời gian: 01/07–14/07/2026  
Sprint Goal: dựng môi trường và luồng đăng nhập–quản trị cơ bản chạy end-to-end.  
Tổng: 22 SP.

| Key | Epic | Công việc | Assignee | SP | Acceptance Criteria |
|---|---|---|---|---:|---|
| VSH-101 | EPIC-1 | Thiết kế kiến trúc API–UI–DB và chuẩn cấu hình môi trường | Phạm Văn Thành | 3 | Có sơ đồ, contract chính và cấu hình dev mẫu không chứa secret |
| VSH-102 | EPIC-1 | Dựng database/migration và health endpoints | Phạm Văn Thành | 3 | DB migrate thành công; health API trả 200 |
| VSH-103 | EPIC-4 | Xây đăng nhập JWT và phân quyền vai trò cơ bản | Phạm Văn Thành | 4 | Login/refresh/logout hoạt động; route/API từ chối đúng vai trò |
| VSH-104 | EPIC-4 | Dựng layout, login và điều hướng theo vai trò | Phạm Ngọc Hoài Anh | 5 | UI responsive; menu thay đổi đúng theo role |
| VSH-105 | EPIC-2 | Dựng màn hình danh mục nhân viên/phòng ban/chức vụ | Phạm Ngọc Hoài Anh | 3 | Xem, tìm kiếm và mở form tạo/sửa được |
| VSH-106 | EPIC-5 | Dựng Docker local, seed data và smoke test nền tảng | Phạm Ngọc Hoài Anh | 2 | Một lệnh dựng được DB/API/UI; smoke test pass |
| VSH-107 | EPIC-5 | Lập đặc tả phạm vi, Definition of Done và biên bản Sprint 1 | Hà Quang Sang | 2 | Tài liệu được lưu, liên kết trên Jira và được hai dev xác nhận |

Kết quả demo cuối Sprint: mở hệ thống từ Docker, đăng nhập bằng các vai trò, xem menu và danh mục tổ chức.

## 4. Sprint 2 — Luồng khách và kiểm soát ra/vào

Thời gian: 15/07–28/07/2026  
Sprint Goal: hoàn thành luồng khách từ đăng ký trước tới quyết định cho phép/từ chối tại cổng.  
Tổng: 22 SP.

| Key | Epic | Công việc | Assignee | SP | Acceptance Criteria |
|---|---|---|---|---:|---|
| VSH-201 | EPIC-2 | Xây API khách và đăng ký trước | Phạm Văn Thành | 4 | CRUD, tìm kiếm, trạng thái và validation hoạt động |
| VSH-202 | EPIC-2 | Xây access permission và access decision service | Phạm Văn Thành | 3 | Quyết định allow/deny có lý do và lưu kết quả |
| VSH-205 | EPIC-1 | Tích hợp camera/go2rtc và cấu hình nguồn camera | Phạm Văn Thành | 3 | API cung cấp stream URL an toàn; camera status kiểm tra được |
| VSH-203 | EPIC-2 | Xây UI đăng ký trước và tiếp nhận khách | Phạm Ngọc Hoài Anh | 4 | Tạo, duyệt, từ chối và tra cứu hồ sơ được |
| VSH-204 | EPIC-2 | Xây màn hình điều phối Thông hành | Phạm Ngọc Hoài Anh | 4 | Hiển thị hồ sơ, ảnh, trạng thái và thao tác allow/deny |
| VSH-206 | EPIC-1 | Xây camera preview và integration test luồng khách | Phạm Ngọc Hoài Anh | 2 | Preview xử lý lỗi/reconnect; happy path và deny path pass |
| VSH-207 | EPIC-5 | Viết hướng dẫn luồng khách và biên bản Sprint 2 | Hà Quang Sang | 2 | Có hướng dẫn theo vai trò, ảnh minh họa và liên kết bằng chứng demo |

Kết quả demo cuối Sprint: lễ tân tạo khách, người có quyền duyệt, bảo vệ xem hồ sơ/camera và quyết định tại cổng.

## 5. Sprint 3 — Giám sát realtime, QR và biển số

Thời gian: 29/07–11/08/2026  
Sprint Goal: tự động hóa nhận dạng đầu vào và cập nhật sự kiện tại cổng theo thời gian thực.  
Tổng: 22 SP.

| Key | Epic | Công việc | Assignee | SP | Acceptance Criteria |
|---|---|---|---|---:|---|
| VSH-301 | EPIC-3 | Xây API phát hành và xác thực QR | Phạm Văn Thành | 4 | QR có hạn dùng, chống dùng lại theo policy và trả quyết định rõ |
| VSH-302 | EPIC-3 | Tích hợp license plate runtime và chuẩn hóa biển số | Phạm Văn Thành | 4 | Nhận kết quả runtime, normalize biển số và xử lý confidence thấp |
| VSH-303 | EPIC-3 | Xây SignalR event pipeline cho sự kiện ra/vào | Phạm Văn Thành | 2 | Client nhận sự kiện đúng user/site và reconnect không nhân đôi |
| VSH-304 | EPIC-3 | Xây màn hình quét QR và phản hồi allow/deny | Phạm Ngọc Hoài Anh | 4 | Quét/nhập tay được; màu và âm báo đúng quyết định |
| VSH-305 | EPIC-3 | Xây live monitor camera + kết quả biển số | Phạm Ngọc Hoài Anh | 4 | Stream và sự kiện đồng bộ; có trạng thái lỗi/fallback |
| VSH-306 | EPIC-4 | Xây event timeline và test QR/LPR/SignalR | Phạm Ngọc Hoài Anh | 2 | Bộ lọc hoạt động; test expired/replay/low-confidence/reconnect pass |
| VSH-307 | EPIC-5 | Soạn hướng dẫn giám sát và báo cáo kết quả Sprint 3 | Hà Quang Sang | 2 | Có hướng dẫn QR/LPR, bảng test evidence và biên bản review |

Kết quả demo cuối Sprint: QR hoặc biển số tạo quyết định, sự kiện và bằng chứng xuất hiện ngay trên màn hình giám sát.

## 6. Sprint 4 — Quản trị, báo cáo và bàn giao

Thời gian: 12/08–25/08/2026  
Sprint Goal: hoàn thiện quản trị/audit/reporting, đóng các lỗi quan trọng và tạo gói bàn giao chạy được.  
Tổng: 22 SP.

| Key | Epic | Công việc | Assignee | SP | Acceptance Criteria |
|---|---|---|---|---:|---|
| VSH-401 | EPIC-4 | Hoàn thiện quản lý người dùng vai trò và trạng thái | Phạm Văn Thành | 3 | Admin tạo/khóa tài khoản và gán vai trò có audit |
| VSH-402 | EPIC-4 | Xây audit log bất biến cho thao tác nhạy cảm | Phạm Văn Thành | 4 | Ghi actor/action/time/result/IP; không sửa/xóa từ UI |
| VSH-403 | EPIC-4 | Xây API thống kê và xuất báo cáo | Phạm Văn Thành | 3 | KPI và dữ liệu xuất khớp access log theo bộ lọc |
| VSH-404 | EPIC-4 | Xây dashboard KPI và báo cáo ra/vào | Phạm Ngọc Hoài Anh | 4 | Có KPI, xu hướng, phân bổ theo cổng và bộ lọc thời gian |
| VSH-405 | EPIC-4 | Hoàn thiện UI audit log và quản trị hệ thống | Phạm Ngọc Hoài Anh | 3 | Tìm kiếm/lọc/xem chi tiết; trạng thái loading/error đầy đủ |
| VSH-406 | EPIC-5 | Chạy security review, regression và UAT kỹ thuật | Phạm Ngọc Hoài Anh | 3 | Luồng E2E pass; secret scan pass; Blocker/Critical = 0 |
| VSH-407 | EPIC-5 | Hoàn thiện hướng dẫn, biên bản UAT và hồ sơ bàn giao | Hà Quang Sang | 2 | Đủ hướng dẫn deploy/sử dụng, biên bản UAT và danh mục artefact |

Kết quả demo cuối Sprint: admin xem dashboard/audit, xuất báo cáo; nhóm triển khai bản release candidate và bàn giao tài liệu.

## 7. Phân bổ tải theo Sprint

| Sprint | Phạm Văn Thành | Phạm Ngọc Hoài Anh | Hà Quang Sang | Tổng |
|---|---:|---:|---:|---:|
| Sprint 1 | 10 | 10 | 2 | 22 |
| Sprint 2 | 10 | 10 | 2 | 22 |
| Sprint 3 | 10 | 10 | 2 | 22 |
| Sprint 4 | 10 | 10 | 2 | 22 |

## 7.1. Lịch dự án 2 tháng

| Giai đoạn | Thời gian | Mốc bàn giao |
|---|---|---|
| Sprint 1 | 01/07–14/07/2026 | Nền tảng chạy được |
| Sprint 2 | 15/07–28/07/2026 | Luồng khách và kiểm soát ra/vào |
| Sprint 3 | 29/07–11/08/2026 | Realtime, QR và biển số |
| Sprint 4 | 12/08–25/08/2026 | Quản trị, báo cáo và release candidate |
| Nghiệm thu dự phòng | 26/08–31/08/2026 | Sửa lỗi nghiệm thu, ký biên bản và bàn giao |

Khoảng nghiệm thu dự phòng không nhận thêm tính năng mới. Chỉ xử lý lỗi thuộc phạm vi 4 Sprint, hoàn thiện tài liệu, triển khai và ký xác nhận.

SP không đồng nghĩa giờ làm. Mức 2 SP/Sprint của Sang biểu thị trần tham gia 20% và chỉ dành cho tài liệu. Hai dev chịu trách nhiệm kỹ thuật, test và triển khai trong phạm vi issue của mình.

## 8. Definition of Ready và Definition of Done

Definition of Ready:

- Có mô tả giá trị nghiệp vụ và Acceptance Criteria kiểm chứng được.
- Có thiết kế/API contract hoặc mockup nếu issue phụ thuộc thành viên khác.
- Đã nêu dependency, dữ liệu test và quyền truy cập cần thiết.
- Issue không lớn hơn 8 SP; issue 8 SP nên được rà để tách nhỏ.

Definition of Done:

- Code đã review và merge; build không lỗi.
- Unit/integration test liên quan pass; dev còn lại review và xác nhận Acceptance Criteria kỹ thuật.
- Không còn bug Blocker/Critical phát sinh từ issue.
- Không commit credential/secret/dữ liệu cá nhân nhạy cảm.
- Có logging/audit phù hợp và tài liệu được cập nhật.
- Product Owner chấp nhận trong Sprint Review.

## 9. Mốc và rủi ro chính

| Rủi ro | Ảnh hưởng | Cách xử lý |
|---|---|---|
| Camera/runtime AI không ổn định | Chặn Sprint 2–3 | Dùng simulator và video mẫu; thống nhất contract từ Sprint 1 |
| Chỉ có 3 thành viên nhưng phạm vi rộng | Trễ bàn giao | Giữ phạm vi MVP; đưa FaceID/mobile/SOC nâng cao sang backlog sau Sprint 4 |
| Backend–frontend chờ nhau | Giảm tốc độ | Chốt OpenAPI/mock response trước khi code UI |
| Thiếu dữ liệu thật cho QR/LPR | Kết quả demo không tin cậy | Chuẩn bị dataset có phiên bản và expected result từ Sprint 2 |
| Security/UAT dồn cuối | Phát hiện lỗi muộn | Hai dev test chéo mỗi Sprint; Sang cập nhật checklist và bằng chứng từ Sprint 1 |

## 10. Các hạng mục không nằm trong MVP 4 Sprint

Đưa vào backlog Phase 2: FaceID nâng cao/multi-face, mobile Android, gọi WebRTC, Campus 3D, SOC/UEBA đầy đủ, AI Agent email, central-area sync, visual regression đa viewport và load/soak test quy mô production.

Điều này giữ kế hoạch 4 Sprint khả thi cho nhóm 3 người và vẫn tạo được một sản phẩm demo end-to-end.
