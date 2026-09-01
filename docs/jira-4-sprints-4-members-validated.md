# V-Shield 2.0 — Dữ liệu Jira đã đối chiếu phiên bản mới nhất

Ngày đối chiếu: 31/08/2026  
Phiên bản Git: `8a24707a` trên `main`/`origin/main`  
Kỳ thực hiện: 01/07/2026–31/08/2026  
Loại dữ liệu: báo cáo hồi tố dựa trên commit và artefact hiện có.

## 1. Kết luận kiểm tra

Dữ liệu Jira cũ **không còn hợp lệ để nhập nguyên trạng** vì mô tả việc dựng V-Shield từ đầu trong tháng 7–8, trong khi các chức năng nền tảng như database, đăng nhập, nhân sự, khách và access flow đã tồn tại trước tháng 7.

Phiên bản mới nhất cho thấy công việc thực tế trong kỳ tập trung vào:

- Phân quyền theo operational task/scope, SignalR và alerts.
- QR hai làn, camera recording/archive, Docker và central sync.
- Chuẩn hóa FaceID, model lifecycle, access credential và policy decision.
- UI RC1/RC2, visual regression và nâng cấp giao diện.
- FaceID YuNet/SFace, remote enrollment, multi-face và FaceGate.
- Mobile biometric/offline QR, gọi WebRTC và relay Central–AreaNode.
- Coverage/CI, AI Agent email, mail stack và light/dark mode.
- Notification center, realtime hybrid sync, Docker-only runtime và public 3D portal.

Các file cấu hình/SQL đang sửa nhưng chưa commit không được tính là công việc đã hoàn thành.

## 2. Nhân sự và phân công đã sửa

| Thành viên | Vai trò | Mức tham gia | Phạm vi |
|---|---|---:|---|
| Phạm Văn Thành | Trưởng nhóm — Developer Backend & Integration | 100% | Backend, database, runtime, sync, deployment và review kỹ thuật |
| Phạm Ngọc Hoài Anh | Developer Frontend/Full-stack | 100% | Web UI, mobile UI, realtime client, portal và review kỹ thuật |
| Bùi Nhật Huy | Documentation | 20% | Jira, đặc tả, tài liệu kỹ thuật/người dùng, biên bản Sprint và hồ sơ bàn giao |
| Hà Quang Sang | QA & Testing | 20% | Test plan trọng yếu, regression smoke, tổng hợp bằng chứng test và hỗ trợ UAT kỹ thuật |

Quy ước: việc gán Assignee dưới đây là phân công báo cáo của nhóm, không phải kết luận tác giả dựa trên tên tài khoản Git.

## 3. Cấu trúc Epic

| Epic | Tên | Phạm vi áp dụng trong kỳ |
|---|---|---|
| EPIC-1 | Kiến trúc Hệ thống & Tích hợp Thiết bị | Docker, camera, runtime, central sync, mobile và tích hợp nền tảng |
| EPIC-2 | Quản lý Ra vào & Luồng Khách | Scope permission, credential, policy decision, FaceGate và QR |
| EPIC-3 | Giám sát Thời gian thực & AI | Camera archive, FaceID, monitoring, SignalR, notification và AI Agent |
| EPIC-4 | Quản trị, Báo cáo & Nhật ký | Role permissions, dashboard, audit, notification center và quản trị UI |
| EPIC-5 | Kiểm thử, Tối ưu & Bàn giao | Coverage, CI, RC/UAT, hiệu năng, triển khai, tài liệu và portal trình diễn |

Public portal/3D là phạm vi phát sinh cuối kỳ; tạm xếp vào EPIC-5 vì phục vụ đóng gói/trình diễn. Nếu tiếp tục phát triển sau kỳ học, nên tách thành Epic riêng.

## 4. Lịch 4 Sprint theo mốc bàn giao thực tế

| Sprint | Thời gian | Mốc bàn giao | Tổng SP |
|---|---|---|---:|
| Sprint 1 — Nền tảng vận hành phân tán | 01/07–14/07 | Permission, QR/camera, Docker sync và mobile offline | 24 |
| Sprint 2 — FaceID contract & UI RC | 15/07–07/08 | Face lifecycle/access decision và UI RC2 | 24 |
| Sprint 3 — FaceGate, Quality & AI Agent | 08/08–21/08 | FaceID nâng cao, mobile/WebRTC, coverage và Agent email | 24 |
| Sprint 4 — Realtime, Mobile & Portal | 22/08–31/08 | Notification/sync realtime, mobile ổn định và portal 3D | 24 |

Sprint 2 và Sprint 4 không dài đúng 14 ngày vì đây là dữ liệu hồi tố theo các cụm commit/bàn giao. Không nên dùng các Sprint này để dựng lại burndown lịch sử.

## 5. Sprint 1 — Nền tảng vận hành phân tán

Sprint Goal: ổn định phân quyền, QR/camera, Docker và khả năng hoạt động phân tán/offline.

| Key | Epic | Công việc | Assignee | SP | Trạng thái | Bằng chứng chính |
|---|---|---|---|---:|---|---|
| VSH-101 | EPIC-2 | Triển khai operational scope, role permissions và scope override | Phạm Văn Thành | 5 | DONE | `008b650c`, `bb390685` |
| VSH-102 | EPIC-1 | Ổn định QR hai làn, camera recording/archive và Docker sync | Phạm Văn Thành | 5 | DONE | `3c5c6807`, `7ec57d59`, `1d6575db`, `bfc3b058` |
| VSH-103 | EPIC-4 | Hoàn thiện alerts center, SignalR auth và camera stream mode UI | Phạm Ngọc Hoài Anh | 5 | DONE | `0f473247`, `f453c0b2`, `b19d6814` |
| VSH-104 | EPIC-1 | Hoàn thiện mobile biometric, offline QR và camera preview sync | Phạm Ngọc Hoài Anh | 5 | DONE | `4295cf02`, `c3a5f3b9` |
| VSH-105 | EPIC-5 | Kiểm thử hồi quy trọng yếu cho auth, QR, camera, sync và offline flow | Hà Quang Sang | 2 | DONE | Test suites hiện có trong API/View/Mobile |
| VSH-106 | EPIC-5 | Tổng hợp hướng dẫn cài đặt và biên bản Sprint 1 | Bùi Nhật Huy | 2 | DONE | `README.md`, `docs/DOCKER_RUN_GUIDE.md` và tài liệu Sprint |

## 6. Sprint 2 — FaceID contract & UI RC

Sprint Goal: chuẩn hóa FaceID thành luồng có kiểm soát và đóng gói release candidate giao diện.

| Key | Epic | Công việc | Assignee | SP | Trạng thái | Bằng chứng chính |
|---|---|---|---|---:|---|---|
| VSH-201 | EPIC-1 | Cô lập face runtime sau API xác thực và hỗ trợ multi-camera session | Phạm Văn Thành | 5 | DONE | `04531db2`, `d075bab2`, `adfa5953`, `9bbeaa10` |
| VSH-202 | EPIC-2 | Chuẩn hóa face storage/model lifecycle, credential và access decision | Phạm Văn Thành | 5 | DONE | `ddf080e0`, `27237549`, `ccb999b2`, `8793a86e` |
| VSH-203 | EPIC-3 | Xây authenticated FaceCamera workflow và giao diện nhận diện | Phạm Ngọc Hoài Anh | 5 | DONE | `869a2f50`, `743a9fe8` và `View/src/components/FaceCamera.vue` |
| VSH-204 | EPIC-5 | Nâng cấp UI/UX toàn diện và đóng gói UI RC1/RC2 | Phạm Ngọc Hoài Anh | 5 | DONE | `929c77f2`, `3a357e08`, `ef855da8`, `cc7b39a9` |
| VSH-205 | EPIC-5 | Chạy smoke/regression FaceID và xác minh các release gate trọng yếu | Hà Quang Sang | 2 | DONE | Bằng chứng functional/visual/accessibility trong RC2 report |
| VSH-206 | EPIC-5 | Tổng hợp release report, UAT handoff và tài liệu FaceID | Bùi Nhật Huy | 2 | DONE | `55742cc5`, `docs/ui-rc2-release-report.md`, tài liệu face-* |

## 7. Sprint 3 — FaceGate, Quality & AI Agent

Sprint Goal: nâng FaceID lên luồng cổng thực tế, mở rộng mobile và thiết lập quality gate tự động.

| Key | Epic | Công việc | Assignee | SP | Trạng thái | Bằng chứng chính |
|---|---|---|---|---:|---|---|
| VSH-301 | EPIC-3 | Nâng FaceID lên YuNet/SFace, remote enrollment, multi-face và intruder evidence | Phạm Văn Thành | 5 | DONE | `7edc6b56`, `cd81bd14`, `bccd86c8`, `014d878d` |
| VSH-302 | EPIC-1 | Tích hợp FaceGate, relay/sync, AI Agent email và mail stack VPS | Phạm Văn Thành | 5 | DONE | `1bf38fb0`, `16fbb462`, `a39edfe3`, `9c3c4245` |
| VSH-303 | EPIC-4 | Hoàn thiện dashboard KPI, FaceCamera và gate-transit monitor UI | Phạm Ngọc Hoài Anh | 5 | DONE | `66996a2e`, `3b82609b`, `47cf7fca` |
| VSH-304 | EPIC-1 | Hoàn thiện mobile API thật, biometric login và gọi audio/video WebRTC | Phạm Ngọc Hoài Anh | 5 | DONE | `48bcd9bf`, `148ab5f9`, `ae42b1c2`, `97102763` |
| VSH-305 | EPIC-5 | Chạy regression smoke và tổng hợp kết quả coverage/CI đa nền tảng | Hà Quang Sang | 2 | DONE | `5a422047`, `cdf0b6ab`, `d246aafa`, `8d17551c` |
| VSH-306 | EPIC-5 | Tổng hợp hướng dẫn FaceID/Agent mail và báo cáo Sprint 3 | Bùi Nhật Huy | 2 | DONE | `docs/remote-faceid-enrollment-ops.md`, `docs/AI-AGENT-KE-HOACH-2026.md` |

## 8. Sprint 4 — Realtime, Mobile & Portal

Sprint Goal: hoàn thiện realtime end-to-end, tăng độ ổn định mobile và cung cấp portal trình diễn bản cuối kỳ.

| Key | Epic | Công việc | Assignee | SP | Trạng thái | Bằng chứng chính |
|---|---|---|---|---:|---|---|
| VSH-401 | EPIC-1 | Triển khai realtime hybrid sync, notification integration và Docker-only runtime | Phạm Văn Thành | 5 | DONE | `9ea84498`, `5eed7024`, `8d8066fb`, `c705ae6c` |
| VSH-402 | EPIC-1 | Ổn định mobile/WebRTC, background connection và VPS deployment | Phạm Văn Thành | 5 | DONE | `6e0dcfc4`, `fef6ae3c`, `a242519d`, `4d14402f` |
| VSH-403 | EPIC-4 | Xây notification center và responsive navigation cho web | Phạm Ngọc Hoài Anh | 5 | DONE | `329d390f`, `4802ffb6`, `47296504`, `f6d7b6e7` |
| VSH-404 | EPIC-5 | Xây public portal đa trang và sân khấu 3D/Mecha trình diễn | Phạm Ngọc Hoài Anh | 5 | DONE | `d3126f21`, `040b380c`, `2e63cb2a`, `8a24707a` |
| VSH-405 | EPIC-5 | Chạy regression smoke realtime/mobile/portal và kiểm tra lỗi trọng yếu | Hà Quang Sang | 2 | DONE | `3017fac7`, `f8f9600a`, `70111e1d`, automated call simulation tests |
| VSH-406 | EPIC-5 | Chốt Jira, báo cáo và hồ sơ bàn giao | Bùi Nhật Huy | 2 | DONE | Bộ tài liệu trong `docs/` và biên bản Sprint |

## 9. Phân bổ tải

| Sprint | Phạm Văn Thành | Phạm Ngọc Hoài Anh | Bùi Nhật Huy | Hà Quang Sang | Tổng |
|---|---:|---:|---:|---:|---:|
| Sprint 1 | 10 | 10 | 2 | 2 | 24 |
| Sprint 2 | 10 | 10 | 2 | 2 | 24 |
| Sprint 3 | 10 | 10 | 2 | 2 | 24 |
| Sprint 4 | 10 | 10 | 2 | 2 | 24 |

Sang nhận 2 SP kiểm thử và Huy nhận 2 SP tài liệu so với capacity chuẩn 10 SP/người/Sprint; cả hai đều giới hạn tham gia 20%. Hai dev vẫn phải viết unit/integration test, cập nhật tài liệu kỹ thuật tối thiểu và tự xác minh code của mình.

## 10. Definition of Done dùng cho báo cáo

- Có commit hoặc artefact hiện hữu chứng minh phạm vi công việc.
- Code đã nằm trên `main`; build/test liên quan có bằng chứng đạt.
- Không còn lỗi Blocker/Critical đã biết trong phạm vi issue.
- Không đưa secret/camera credential thật vào Git.
- Sang tổng hợp kết quả kiểm thử trọng yếu; Huy cập nhật tài liệu và liên kết bằng chứng trên Jira.
- Với RC/UAT: phải ghi rõ trạng thái `packaged`, `UAT blocked` hoặc `production ready`; không đánh đồng đóng gói với phê duyệt production.

## 11. Các điểm chưa thể xác nhận chỉ bằng repository

- Tên Git author không đủ để xác nhận chính xác ai thực hiện từng phần; Assignee dựa trên phân công nhóm do người quản lý cung cấp.
- Không có dữ liệu Jira gốc về thời điểm issue chuyển trạng thái, nên không tái tạo burndown/velocity lịch sử.
- RC2 đã đóng gói nhưng báo cáo hiện có từng ghi UAT production còn phụ thuộc môi trường/quyền quản trị.
- Các thay đổi chưa commit trong `.env.docker.example`, SQL, compose và go2rtc không được ghi nhận là DONE.

## 12. Hướng dẫn nhập Jira

1. Tạo 5 Epic đúng tên trong mục 3.
2. Tạo 4 Sprint đúng tên trong mục 4.
3. Đảm bảo bốn tài khoản Jira tồn tại và dùng đúng Display name trước khi import.
4. Import `docs/jira-import-4-sprints-4-members-validated.csv`.
5. Map các cột `Epic Link`, `Sprint`, `Assignee`, `Story Points`, `Status`, `Labels`, `Description`.
6. Nếu Jira không nhận Assignee theo Display name, thay bằng email/accountId của từng thành viên.
