# Kế hoạch Jira báo cáo công việc V-Shield 2.0

Ngày lập: 31/08/2026  
Nguồn đối chiếu: lịch sử Git từ 04/03/2026 đến 29/08/2026, README, báo cáo RC1/RC2, báo cáo UI/UX và tài liệu kỹ thuật trong `docs/`.

## 1. Mục đích và cách sử dụng

Kế hoạch này dùng để ghi nhận hồi tố các hạng mục đã hoàn thành của V-Shield 2.0 trên Jira Software. Các Sprint phản ánh mốc bàn giao thực tế trong Git, không dùng để suy diễn ngày công cá nhân.

- Loại board đề xuất: Scrum.
- Chu kỳ chuẩn cho giai đoạn tiếp theo: 2 tuần/Sprint.
- Workflow tối giản: `TO DO -> IN PROGRESS -> REVIEW/TEST -> DONE`.
- Issue hierarchy: `Epic -> Story/Task -> Sub-task/Bug`.
- Story Point trong báo cáo là độ phức tạp tương đối, không phải số ngày làm việc.
- Definition of Done: code đã merge, build đạt, kiểm thử liên quan đạt, không lộ secret, và tài liệu vận hành được cập nhật khi có thay đổi triển khai.

## 2. Epic đề xuất

| Epic key tạm | Epic | Kết quả kinh doanh |
|---|---|---|
| VSH-E01 | Nền tảng & quản trị nhân sự | Có nền tảng .NET/Vue, đăng nhập, tài khoản, nhân viên, phòng ban/chức vụ |
| VSH-E02 | Kiểm soát ra/vào đa phương thức | Vận hành cổng bằng FaceID, biển số, QR và xử lý thủ công |
| VSH-E03 | Khách, phương tiện & đăng ký trước | Quản lý vòng đời khách, giấy phép, QR và phương tiện |
| VSH-E04 | Chấm công & bản đồ vận hành | Chấm công theo vùng/cổng và quan sát campus thời gian thực |
| VSH-E05 | An ninh doanh nghiệp & SOC | Policy, MFA, UEBA, cảnh báo, bằng chứng và can thiệp nghiệp vụ |
| VSH-E06 | Camera, thiết bị & lưu trữ video | Quản lý luồng camera, thiết bị, ghi hình và tra cứu archive |
| VSH-E07 | Giao tiếp, thông báo & Mobile | Chat/SignalR, cảnh báo theo người dùng, Android và gọi WebRTC |
| VSH-E08 | Đồng bộ, triển khai & vận hành | Docker/VPS, central-area sync, cấu hình môi trường và bảo mật secret |
| VSH-E09 | Chất lượng, UI/UX & phát hành | UI responsive/light-dark, coverage, CI, visual regression và release gate |
| VSH-E10 | AI Agent doanh nghiệp | Trợ lý tra cứu người, soạn/gửi email có xác nhận và audit |

## 3. Tổng quan các Sprint đã hoàn thành

| Sprint | Thời gian hồi tố | Mục tiêu | Epic chính | SP | Trạng thái |
|---|---|---|---|---:|---|
| VSH-S01 — Khởi tạo nền tảng | 04/03–17/03/2026 | Dựng backend/frontend, dữ liệu tổ chức, đăng nhập và FaceID/biển số ban đầu | E01, E02 | 42 | DONE |
| VSH-S02 — Thông hành & QR | 18/03–10/04/2026 | Hoàn thiện luồng thông hành, QR động/tĩnh, camera và VPS | E02, E03 | 47 | DONE |
| VSH-S03 — Chuẩn hóa & Docker | 21/05–03/06/2026 | Chuẩn hóa source, audit, khách, chấm công, Docker/go2rtc | E03, E04, E08 | 48 | DONE |
| VSH-S04 — Enterprise Security | 04/06–15/06/2026 | Hardening, MFA, AI nghiệp vụ, SOC, policy, evidence và operations | E05 | 55 | DONE |
| VSH-S05 — Điều hành hợp nhất | 16/06–29/06/2026 | Hoàn thiện tích hợp enterprise, bản đồ 3D, fallback và kiểm thử tải | E04, E05, E06 | 45 | DONE |
| VSH-S06 — Realtime & phân quyền | 30/06–13/07/2026 | Ổn định chat, scope permission, camera recording, sync và mobile offline | E06, E07, E08 | 44 | DONE |
| VSH-S07 — UI Release Candidate | 14/07–07/08/2026 | Nâng cấp UI toàn diện, visual regression và đóng gói RC2 | E09 | 40 | DONE* |
| VSH-S08 — FaceID & Mobile nâng cao | 08/08–16/08/2026 | FaceID GPU/remote/multi-face, gate access, mobile thật và relay WebRTC | E02, E07, E08 | 53 | DONE |
| VSH-S09 — Quality Gate & AI Agent | 17/08–31/08/2026 | Tăng coverage, bổ sung CI, agent email, mail stack và light/dark mode | E09, E10 | 55 | DONE |

`DONE*`: phạm vi phát triển/đóng gói RC2 đã hoàn thành; UAT production vẫn là công việc mở do phụ thuộc quyền quản trị repository và môi trường thật.

## 4. Chi tiết Sprint

### VSH-S01 — Khởi tạo nền tảng

Sprint Goal: tạo bộ khung chạy được và các nghiệp vụ lõi đầu tiên.

1. Khởi tạo API .NET 8, Vue frontend và database/migration — 8 SP.
2. Xây dựng đăng nhập, quản lý tài khoản và liên kết tài khoản–nhân viên — 8 SP.
3. Quản lý nhân viên, ảnh hồ sơ, phòng ban và chức vụ — 8 SP.
4. Tích hợp FaceID phiên bản đầu và kết nối giao diện — 8 SP.
5. Xây dựng nhận diện biển số Việt Nam và kết nối camera — 5 SP.
6. Khởi tạo đăng ký trước, registration link và dashboard thống kê — 5 SP.

Kết quả: hình thành nền tảng quản trị và nhận diện cơ bản; các luồng có thể chạy trên môi trường phát triển.

### VSH-S02 — Thông hành & QR

Sprint Goal: tạo luồng vận hành cổng xuyên suốt từ đăng ký tới quyết định vào/ra.

1. Xây dựng giao diện và API điều phối Thông hành — 8 SP.
2. Tích hợp FaceID và biển số vào phiên thông hành, chống ghi nhận lặp — 8 SP.
3. Xây dựng QR động, QR tĩnh và QR cho khách đăng ký trước — 8 SP.
4. Bổ sung chế độ xác nhận thủ công và cảnh báo tổng tại cổng — 8 SP.
5. Chuẩn hóa camera RTSP/go2rtc, preview và toàn màn hình — 8 SP.
6. Cấu hình HTTPS/VPS, remote AI proxy và phân quyền màn hình — 5 SP.
7. Sửa validation biển số/đăng ký trước và ổn định dữ liệu mẫu — 2 SP.

Kết quả: có bộ chức năng kiểm soát cổng bằng QR, FaceID, biển số và phương án thủ công.

### VSH-S03 — Chuẩn hóa & Docker

Sprint Goal: đưa sản phẩm về cấu trúc dễ triển khai, có audit và nghiệp vụ mở rộng.

1. Chuẩn hóa tên module/runtime và làm sạch repository — 5 SP.
2. Hoàn thiện quản lý khách, giấy phép truy cập và visitor pass QR — 8 SP.
3. Xây dựng nhật ký audit hệ thống kèm IP, thiết bị và lý do — 8 SP.
4. Hoàn thiện attendance, lịch làm việc, nghỉ phép và báo cáo — 8 SP.
5. Thêm bản đồ cổng thời gian thực và dữ liệu campus — 5 SP.
6. Docker hóa SQL/API/frontend/go2rtc/AI runtime — 8 SP.
7. Ổn định camera public-domain, WebRTC và tài liệu rollout — 6 SP.

Kết quả: hệ thống có thể dựng bằng Docker, theo dõi audit và hỗ trợ khách/chấm công.

### VSH-S04 — Enterprise Security

Sprint Goal: bổ sung lớp bảo mật và điều hành an ninh cấp doanh nghiệp.

1. Hardening nền tảng: CSP, secret service, rate limit và security boundary — 8 SP.
2. Step-up MFA, session control và phân quyền nhạy cảm — 8 SP.
3. AI import, chuẩn hóa dữ liệu và import/export đa định dạng — 8 SP.
4. AI dashboard, bất thường chấm công, UEBA và fuzzy biển số — 8 SP.
5. SOC Alarm Console, policy engine và duress/emergency flow — 8 SP.
6. Device topology, simulator, video/VMS và AI review — 5 SP.
7. Evidence, retention, redaction, SIEM, backup/restore và security checks — 5 SP.
8. Hướng dẫn tương tác và nội dung nghiệp vụ tiếng Việt — 5 SP.

Kết quả: mở rộng từ ứng dụng kiểm soát cổng thành nền tảng security operations có kiểm soát và truy vết.

### VSH-S05 — Điều hành hợp nhất

Sprint Goal: kết nối các workstream enterprise thành luồng vận hành thống nhất.

1. Tích hợp permission/shared components/lane decision vào frontend–backend — 8 SP.
2. Operational Intervention Request, escalation và auto-expire — 8 SP.
3. Campus 3D: tòa nhà, cổng, bãi xe, camera và trạng thái realtime — 8 SP.
4. Manual access/parking fallback và evidence capture — 8 SP.
5. Bổ sung seed data đầy đủ cho demo enterprise — 5 SP.
6. Thiết lập load/stress/soak test framework và profile chạy — 5 SP.
7. Ổn định build backend/frontend/Android và migration startup — 3 SP.

Kết quả: các module enterprise hoạt động trên một trải nghiệm điều hành chung; build ba nền tảng đạt.

### VSH-S06 — Realtime & phân quyền

Sprint Goal: ổn định giao tiếp thời gian thực và quyền truy cập theo phạm vi vận hành.

1. Sửa idempotency, duplicate rendering và delivery fallback của chat — 5 SP.
2. Thay role cứng bằng operational task và user operational scope — 8 SP.
3. Xây dựng màn hình Role Permissions và scope override — 8 SP.
4. Sửa SignalR authentication và trung tâm cảnh báo — 5 SP.
5. Ổn định QR hai làn, camera preview và go2rtc — 5 SP.
6. Tự động ghi hình, lưu trữ bền vững và camera archive — 5 SP.
7. Central sync và ổn định Docker runtime — 5 SP.
8. Mobile biometric unlock và offline QR — 3 SP.

Kết quả: quyền được quản lý theo chức năng/phạm vi; realtime và lưu trữ camera ổn định hơn.

### VSH-S07 — UI Release Candidate

Sprint Goal: chuẩn hóa giao diện đa kích thước và tạo bộ bằng chứng phát hành lặp lại được.

1. Audit và nâng cấp toàn diện UI/UX các màn hình chính — 13 SP.
2. Chuẩn hóa responsive cho desktop, tablet và mobile — 8 SP.
3. Thiết lập functional/visual/accessibility test bằng Playwright — 8 SP.
4. Tạo và review 120 Linux visual baselines — 5 SP.
5. Thiết lập release gates, manifest, rollback và UAT handoff — 5 SP.
6. Đóng gói RC2 có artifact digest tái lập — 1 SP.

Kết quả: 75/75 functional, 120/120 visual, 50/50 accessibility và 26/26 unit test của bộ RC2 đạt; production sign-off chưa thực hiện.

### VSH-S08 — FaceID & Mobile nâng cao

Sprint Goal: nâng độ tin cậy của nhận diện khuôn mặt và kết nối mobile với hệ thống thật.

1. Nâng FaceID lên YuNet + SFace GPU và guided enrollment 5 góc — 8 SP.
2. Đăng ký FaceID từ xa qua VPS và quản lý lifecycle model — 8 SP.
3. Multi-face/session tracking, intruder detection và ảnh bằng chứng — 8 SP.
4. Kiểm tra quyền theo cổng và ghi AccessLog/attendance cho FaceID — 8 SP.
5. Cải tiến FaceCamera UI và trạng thái allow/deny/unknown — 5 SP.
6. Kết nối mobile với API thật, biometric login và build release — 5 SP.
7. Gọi audio/video WebRTC và relay Central–AreaNode — 8 SP.
8. Sửa sync identity, payload upload, rotation và route lazy-load — 3 SP.

Kết quả: FaceID hỗ trợ đăng ký từ xa, nhiều khuôn mặt và quyết định tại cổng; mobile gọi thật qua WebRTC.

### VSH-S09 — Quality Gate & AI Agent

Sprint Goal: nâng mức kiểm thử và đưa trợ lý AI/email doanh nghiệp vào hệ thống.

1. Thêm hạ tầng unit test frontend và 336 test P0/P1 — 8 SP.
2. Mở rộng coverage UI từ khoảng 23% lên trên 61% — 13 SP.
3. Đưa API coverage gate lên 55% và bổ sung test các cluster quan trọng — 8 SP.
4. Thêm test Android, Python và workflow CI đa nền tảng — 5 SP.
5. Tạo gate-transit auto monitor và simulation harness — 5 SP.
6. Xây AI Agent tra cứu người, soạn và gửi email có kiểm soát — 8 SP.
7. Triển khai mail stack trên Docker/VPS và sửa cấu hình mạng/SSL — 3 SP.
8. Nâng chất lượng email, xử lý sync FK loop và QR rate limit — 2 SP.
9. Refactor light/dark mode, camera preview và route error recovery — 3 SP.

Kết quả: coverage và CI được nâng đáng kể; AI Agent/email cùng giao diện light-dark đã được tích hợp.

## 5. Backlog/Sprint tiếp theo đề xuất

### VSH-S10 — UAT & Production Readiness (PLANNED)

Sprint Goal: biến RC2 đã đóng gói thành bản đủ điều kiện phê duyệt production.

1. Repo Admin tạo và bảo vệ GitHub Environment `v-shield-uat` — 3 SP.
2. IT cấu hình protected inputs/secrets và deploy immutable digest — 5 SP.
3. QA chạy UAT authentication, role, mutation, SignalR, performance và smoke — 8 SP.
4. Diễn tập rollback với prior digest và xác nhận owner — 5 SP.
5. Chạy load test trên staging với seed/token ổn định — 5 SP.
6. Khắc phục phát hiện UAT mức Blocker/Critical — 8 SP dự phòng.
7. Thu thập sign-off IT, QA, Business và chốt release note — 3 SP.

Điều kiện bắt đầu: có quyền quản trị repository, môi trường UAT, secret và dữ liệu kiểm thử.  
Điều kiện hoàn tất: toàn bộ gate đạt, rollback đã diễn tập, không còn Blocker/Critical, đủ ba chữ ký duyệt.

## 6. Dashboard Jira phục vụ báo cáo

Tạo dashboard `V-Shield 2.0 — Delivery Report` với các gadget:

1. Sprint Health: trạng thái Sprint và phần việc hoàn tất.
2. Created vs Resolved: lượng issue tạo/đóng theo thời gian.
3. Pie Chart theo Epic: phân bổ phạm vi công việc.
4. Two-Dimensional Filter: `Sprint x Issue Type`.
5. Filter Results cho blocker còn mở của UAT.

JQL gợi ý:

```text
project = VSH ORDER BY Sprint ASC, Rank ASC
project = VSH AND statusCategory = Done ORDER BY resolved ASC
project = VSH AND labels = production-readiness AND statusCategory != Done
project = VSH AND priority in (Blocker, Highest) AND statusCategory != Done
```

## 7. Quy trình đưa vào Jira

1. Tạo Scrum project với key `VSH`.
2. Tạo 10 Epic theo bảng ở mục 2.
3. Tạo các Sprint đúng tên `VSH-S01` đến `VSH-S10`; với Sprint hồi tố, điền ngày theo mục 3.
4. Import `docs/jira-import-vshield-2026.csv`; map `Epic Name`, `Epic Link`, `Sprint`, `Story Points`, `Labels`, `Status` theo cấu hình Jira đang dùng.
5. Nếu Jira không cho import trực tiếp Sprint/Status, import vào Backlog trước rồi bulk move và bulk transition.
6. Gắn liên kết commit/PR hoặc tài liệu nghiệm thu vào từng issue trọng yếu để tăng tính kiểm chứng của báo cáo.

## 8. Lưu ý báo cáo

- Lịch sử Git có nhiều commit trong cùng ngày và một số commit checkpoint; không nên quy mỗi commit thành một ticket.
- Không dùng tổng Story Point để quy đổi sang giờ công hoặc đánh giá năng suất cá nhân.
- Sprint S01–S09 là báo cáo hồi tố; burndown tạo sau thời điểm hoàn thành sẽ không phản ánh diễn biến thực tế.
- Các thay đổi chưa commit trong workspace không được tính là công việc hoàn thành trong báo cáo này.
