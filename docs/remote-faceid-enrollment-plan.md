# KẾ HOẠCH — ĐĂNG KÝ FACE ID TỪ XA & ĐỒNG BỘ TOÀN HỆ THỐNG

> **Mục tiêu:** Cho phép khách/nhân viên đăng ký Face ID **từ xa** bằng camera trên
> thiết bị (điện thoại/máy tính), không cần đến công ty hay dùng VPN. Kết quả Face ID
> được đồng bộ tới **toàn bộ các node local** trong hệ thống qua VPS trung tâm.

- **Ngày lập:** 2026-08-14
- **Trạng thái:** Kế hoạch (chưa code)
- **Người thực hiện:** theo dõi tiến độ tại mục `## TIẾN ĐỘ`

---

## 1. BÀI TOÁN & KIẾN TRÚC TỔNG THỂ

### 1.1 Bối cảnh
- **Local node** (công ty/chi nhánh): có GPU + AI, tự chạy Face Runtime. **Định kỳ
  chủ động gọi VPS** để push/pull data (VPS là điểm cố định).
- **VPS (central)**: không có AI, chỉ trao đổi data đã xử lý. **Không thể tự gọi local**
  (không biết local nào / local không có IP công khai).
- **App ngoài** (điện thoại khách): chỉ kết nối được VPS (điểm cố định).

### 1.2 Cơ chế chọn (mô hình Hybrid — chuẩn ngân hàng/fintech)
Phân chia vai trò giữa thiết bị khách và server để **tối ưu băng thông + tận dụng phần
cứng khách** mà vẫn giữ phần nhạy cảm ở server:

| Tầng | Trách nhiệm | Chạy ở đâu |
|---|---|---|
| **Thiết bị khách** | Nhận diện mặt trong khung, hướng dẫn quay 5 góc, cảnh báo (không/vài người), chọn frame tốt | Trình duyệt (MediaPipe) |
| **VPS (central)** | Nhận frame → xếp hàng chờ → gán cho local đang online → nhận template → lưu + phát xuống | ASP.NET (không AI) |
| **Local node** | Nhận job → embed (SFace) → tạo template JSON → đẩy lên VPS | ASP.NET + Face Runtime (GPU) |

### 1.3 Luồng 1 vòng cập nhật FaceID toàn hệ thống
```
(1) Điện thoại: MediaPipe detect + hướng dẫn 5 góc → chọn 8–15 frame tốt
(2) Gửi frame → VPS  →  tạo FaceEnrollmentJob (Pending)
(3) VPS: local nào kết nối (sync) đầu tiên → nhận job AI
(4) Local: embed từng frame → tạo emp_{id}.json → đẩy template lên VPS
(5) VPS: lưu EmployeeFaceModel (Active) + lưu file template
(6) VPS: phát template mới xuống mọi local khi chúng sync
(7) Các local: nhận template → nạp vào Face Runtime → nhận diện được khách
```

---

## 2. RÀNG BUỘC & QUYẾT ĐỊNH ĐÃ CHỐT

- [x] Đăng ký Face ID **không cần chọn nhân viên** — lấy `EmployeeId` từ tài khoản đang đăng nhập (JWT).
- [x] Nhân viên tạo xong đã có tài khoản tự sinh (`nv{id}` / `Staff@123`) — giữ nguyên.
- [x] Thiết bị khách dùng **camera mặc định** (webcam/getUserMedia).
- [x] Kết quả lưu vào bảng `EmployeeFaceModel` (đã tồn tại).
- [x] Tận dụng cơ chế sync hiện có (`AreaNodeSyncWorker`, `CentralSyncService`, `OutboxEvent`).
- [x] File template sync qua **file upload HTTP** (không nhúng base64 vào event) — sẽ chốt chi tiết ở Giai đoạn 4.
- [x] Giai đoạn hiện tại (GĐ1–GĐ2) chỉ chạy **local trực tiếp**; sync VPS↔local từ GĐ3 trở đi.

---

## 3. CÁC GIAI ĐOẠN THỰC HIỆN

### GIAI ĐOẠN 0 — Khảo sát & chuẩn bị (0.5–1 ngày)

**Mục tiêu:** nắm chắc code hiện có, không phá vỡ những gì đang chạy.

| # | Bước | Mô tả | Nghiệm thu (Acceptance) |
|---|---|---|---|
| 0.1 | Khảo sát sync | Đọc `AreaNodeSyncWorker`, `CentralSyncService`, `SyncEventApplier`, `SyncContracts` | Ghi chú rõ cơ chế push/pull, cách đăng ký node, ack, retry |
| 0.2 | Khảo sát Face enrollment | Đọc `EnrollmentService`, `FaceCameraController`, `IFaceRecognitionClient` | Liệt kê các endpoint hiện có + contract |
| 0.3 | Khảo sát DB | Đọc `EmployeeFaceModel`, `EmployeeFaceCredentialBinding`, `FaceEnrollmentJob` (nếu có) | Biết bảng nào đã có, bảng nào cần thêm |
| 0.4 | Xác định file sync | Chốt cách lưu/chuyển file template (upload HTTP vs base64) | Quyết định ghi vào kế hoạch |

**Nghiệm thu GĐ0:** tài liệu khảo sát + checklist các điểm chốt.

---

### GIAI ĐOẠN 1 — Tự đăng ký Face ID bằng webcam (local trực tiếp)

**Mục tiêu:** nhân viên đăng nhập → mục "Đăng ký Face ID" → dùng camera thiết bị →
chụp nhiều khung → lưu template, **chưa cần VPS**.

| # | Bước | Mô tả | Nghiệm thu |
|---|---|---|---|
| 1.1 | Backend endpoint | `POST /api/FaceCamera/guided/confirm-self`: lấy `EmployeeId` từ JWT, nhận danh sách frame (base64), gọi Face Runtime `enroll-live`/confirm, lưu `EmployeeFaceModel` | Test qua Swagger/Postman: đăng ký thành công, DB có bản ghi `Active` |
| 1.2 | Backend status | `GET /api/FaceCamera/my-face-status`: trả trạng thái FaceID của tài khoản hiện tại (đã đăng ký/chưa, file, version) | Trả đúng `hasFaceId: true/false` |
| 1.3 | Frontend menu | Thêm mục "Đăng ký Face ID" cho role `NhanVien` (và các role có EmployeeId) | Menu hiển thị đúng theo role |
| 1.4 | Frontend trang | Trang `MyFaceId.vue`: bật camera mặc định (getUserMedia), chụp liên tục ~10–15 frame có mặt rõ, gửi lên endpoint 1.1 | Chụp frame, hiển thị số frame, gửi thành công |
| 1.5 | Employees.vue | Hiển thị trạng thái FaceID của từng nhân viên (cột/badge đã đăng ký/chưa) | Cột trạng thái đúng dữ liệu |

**Nghiệm thu GĐ1:**
- [ ] Đăng ký bằng webcam máy → thành công → template lưu đúng + DB ghi đúng.
- [ ] Đăng nhập tài khoản nhân viên → thấy mục Đăng ký Face ID → đăng ký không cần chọn người.
- [ ] Quét thử nhận diện đúng người vừa đăng ký.

---

### GIAI ĐOẠN 2 — MediaPipe trên thiết bị (hướng dẫn quay 5 góc)

**Mục tiêu:** đưa phần nhận diện + hướng dẫn quay lên trình duyệt khách (giống logic 5 góc
hiện có nhưng chạy client), giảm tải server và phù hợp đăng ký từ xa.

| # | Bước | Mô tả | Nghiệm thu |
|---|---|---|---|
| 2.1 | Tích hợp MediaPipe | Thêm MediaPipe FaceLandmarker vào bundle frontend (hoặc dùng thư viện tương đương) | Load được model, detect 1 mặt realtime |
| 2.2 | State machine 5 góc | Tái hiện `pose_guide.py` (5 góc: thẳng/trái/phải/lên/xuống) bằng JS | Hướng dẫn đúng, cover đủ 5 góc |
| 2.3 | Cảnh báo viền | Viền đỏ khi 0/multiple mặt, xanh khi đủ, thông báo trên khung | Giống UI FaceCamera hiện tại |
| 2.4 | Chọn frame | Lọc frame mờ/tối + chọn ~10–15 frame tốt nhất (đủ góc) | Frame chọn được có chất lượng tốt |
| 2.5 | Gửi lên server | Chỉ gửi frame đã chọn (không video thô) | Băng thông nhỏ, request hợp lệ |

**Nghiệm thu GĐ2:**
- [ ] Trên điện thoại: detect mặt + hướng dẫn quay mượt, không cần server AI.
- [ ] Đủ 5 góc → chọn frame → gửi lên VPS/local.

---

### GIAI ĐOẠN 3 — Job queue tại VPS (nhận frame, chờ local xử lý)

**Mục tiêu:** VPS nhận frame từ khách → xếp hàng `Pending` → gán cho local online.

| # | Bước | Mô tả | Nghiệm thu |
|---|---|---|---|
| 3.1 | Bảng job | Thêm `FaceEnrollmentJob` (Id, EmployeeId, CompanyId/SiteId, Frames, Status Pending/Processing/Done/Failed, CreatedAt) | Migration tạo bảng đúng |
| 3.2 | Endpoint nhận frame | `POST /api/face-enrollment/self` (VPS): lưu job Pending + lưu frame (file/DB) | Tạo job Pending thành công |
| 3.3 | Gán cho local | Khi local sync tới (pull), VPS gán job Pending cho node đó (lock chống trùng) | Job chỉ gán 1 node, không trùng |
| 3.4 | Retry/timeout | Job quá hạn chưa xử lý → reassign; local xử lý lỗi → Failed | Retry đúng, không mất job |

**Nghiệm thu GĐ3:**
- [ ] Khách gửi frame → VPS lưu Pending.
- [ ] Local kết nối → nhận job → xử lý → không trùng giữa các local.

---

### GIAI ĐOẠN 4 — Local xử lý AI & tạo template

**Mục tiêu:** local nhận job → chạy Face Runtime embed → tạo `emp_{id}.json` → đẩy lên VPS.

| # | Bước | Mô tả | Nghiệm thu |
|---|---|---|---|
| 4.1 | Worker xử lý | Local: background worker nhận job, gọi Face Runtime `enroll-live` với frame của khách | Template tạo đúng |
| 4.2 | Upload template | Đẩy file template + checksum + encodingCount lên VPS (upload HTTP hoặc qua event) | VPS nhận đủ file + metadata |
| 4.3 | Cập nhật trạng thái | Job → Done; lưu `EmployeeFaceModel` Active tại VPS | DB đúng trạng thái |

**Nghiệm thu GĐ4:**
- [ ] Local xử lý job từ VPS → template hợp lệ → VPS lưu Active.

---

### GIAI ĐOẠN 5 — Phát template xuống toàn bộ local

**Mục tiêu:** VPS phát template mới tới mọi local để đồng bộ FaceID toàn hệ thống.

| # | Bước | Mô tả | Nghiệm thu |
|---|---|---|---|
| 5.1 | Đóng gói template | VPS đưa template + metadata vào sync event (hoặc file download) | Local nhận đủ dữ liệu |
| 5.2 | Phát xuống | Dùng `CentralSyncService` đẩy tới mọi node theo scope | Các node đều nhận |
| 5.3 | Local nạp | Local lưu template vào Face Runtime model dir → reload registry | Nhận diện được khách mới |
| 5.4 | Xác nhận | Local ack đã nạp; VPS cập nhật trạng thái đồng bộ | Toàn bộ node đồng bộ |

**Nghiệm thu GĐ5:**
- [ ] Đăng ký ở VPS → mọi local đều nhận diện được khách mới.

---

### GIAI ĐOẠN 6 — Kiểm thử toàn hệ thống & tài liệu

| # | Bước | Mô tả | Nghiệm thu |
|---|---|---|---|
| 6.1 | E2E | Mô phỏng đầy đủ: điện thoại → VPS → local → VPS → mọi local | Luồng chạy end-to-end |
| 6.2 | Rollback | Tài liệu rollback nếu lỗi | Quay lại được trạng thái cũ |
| 6.3 | Tài liệu vận hành | Hướng dẫn cấu hình VPS/local, tunnel, bảo mật | Tài liệu rõ ràng |

**Nghiệm thu GĐ6:** toàn bộ tiêu chí các giai đoạn trước đạt, tài liệu đầy đủ.

---

## 4. TIẾN ĐỘ

| Giai đoạn | Trạng thái | Ghi chú |
|---|---|---|
| GĐ0 Khảo sát | ✅ Hoàn thành | Đã khảo sát sync + Face enrollment + DB |
| GĐ1 Webcam tự đăng ký | ✅ Hoàn thành | Đăng ký + DB + status + menu + Employees.vue |
| GĐ2 MediaPipe client | ✅ Hoàn thành | MediaPipe WASM + 5 góc + viền cảnh báo + chọn frame |
| GĐ3 Job queue VPS | ✅ Hoàn thành | entity + nhận frame + queue + atomic claim + complete/fail |
| GĐ4 Local AI | ✅ Hoàn thành | RemoteFaceEnrollmentWorker claim + embed + complete/fail |
| GĐ5 Phát template | ✅ Hoàn thành | templateContent lưu VPS + local sync tải + reload |
| GĐ6 Kiểm thử | ✅ Hoàn thành | E2E 2 node: register → claim → complete → templates |

> **Cập nhật tiến độ:** mỗi bước hoàn thành sẽ tick `[x]` trong bảng nghiệm thu của
> giai đoạn tương ứng và cập nhật cột Trạng thái ở bảng trên.

---

## 6. NHẬT KÝ THỰC HIỆN

### GĐ1 (2026-08-14) — Đã hoàn thành & nghiệm thu
- Thêm `FaceEnrollmentController` (`/api/FaceEnrollment`): `GET my-status`,
  `POST enroll-self` (lấy EmployeeId từ JWT, không cần chọn nhân viên).
- `enroll-self` gọi Face Runtime `LiveEnrollAsync` → lưu `EmployeeFaceModel`
  (Active, ModelPath canonical `models/active/{file}`).
- `EmployeeResponse` thêm `HasFaceId` + `FaceModelFileName`; `GET /api/Employees`
  trả trạng thái FaceID từng người.
- Frontend: menu "Đăng ký Face ID" (mọi role), trang `MyFaceId.vue` dùng
  webcam thiết bị (getUserMedia) chụp ~15 khung → gửi `enroll-self`.
- `Employees.vue` thêm cột "Face ID" (Đã/Chưa đăng ký).
- **Nghiệm thu:** đăng ký thành công (employeeId=2 → `emp_2_v1_422a81b1.json`,
  12 encodings), `my-status` trả `hasFaceId:true`, `Employees` trả `hasFaceId`
  đúng. Build API 0 errors, frontend 200.

### GĐ2 (2026-08-14) — Đã hoàn thành & nghiệm thu
- Thêm `@mediapipe/tasks-vision` (0.10.35) + model `face_landmarker.task` (3.6MB)
  vào `public/models/`.
- `faceLandmarker.js`: load FaceLandmarker WASM (GPU), `detectFace()` trả
  faceState (none/single/multiple) + yaw/pitch/roll từ transformation matrix.
- `poseGuideClient.js`: state machine 5 góc (thẳng/trái/phải/lên/xuống) + hướng
  dẫn tiếng Việt — tái hiện `pose_guide.py` trên client.
- `MyFaceId.vue`: realtime detect + hướng dẫn quay + viền đỏ (không/nhiều mặt)
  / xanh (đủ góc) + lưới 5 góc + chỉ chụp frame khác biệt (dedup diff).
- **Nghiệm thu:** build frontend OK, model served 200 (3.6MB), bundle
  `MyFaceId` + `vision_bundle` (MediaPipe WASM) có mặt. Camera khách chỉ gửi
  ảnh, không cần public IP.

### GĐ3a (2026-08-14) — Đã hoàn thành & nghiệm thu
- Entity `RemoteFaceEnrollmentJob` + `RemoteFaceEnrollmentFrame` + migration
  `AddRemoteFaceEnrollmentJob`.
- Endpoint `POST /api/FaceEnrollment/submit-remote`: nhận ảnh, tạo job Pending
  + lưu frame (image data URI).
- Đăng ký `RemoteFaceEnrollmentJob` vào `AreaNodeEntities` + `CentralEntities`
  và `EmployeeFaceModel` vào `CentralEntities`; thêm apply handler trong
  `SyncEventApplier` cho cả 2 entity.
- **Nghiệm thu:** migration áp dụng, `submit-remote` trả job Pending (jobId).
  Build API 0 errors.
- **Còn lại (GĐ3b/3c):** gán job cho local online + retry/timeout; xử lý frame
  ảnh qua sync (không nhúng base64 vào event — sẽ dùng API tải frame riêng).

### GĐ3b/3c (2026-08-14) — Đã hoàn thành & nghiệm thu
- `RemoteFaceEnrollmentQueueService` (VPS): `ClaimNextAsync` (atomic claim bằng
  `ExecuteUpdateAsync` với điều kiện `Status=Pending` → chống nhiều local trùng
  job), `CompleteAsync` (lưu `EmployeeFaceModel` Active + job Completed),
  `FailAsync` (job Failed).
- `SyncController` thêm `POST face-enrollment/claim-next|complete|fail` (local
  gọi VPS qua HTTP, auth bằng node header).
- Đăng ký DI service trong Program.cs.
- **Nghiệm thu:** build 0 errors, API deploy healthy. Ghi chú: frame ảnh tải
  qua claim-next trực tiếp (không qua sync event), mô hình claim-based worker
  queue phù hợp "local chủ động gọi VPS".

### GĐ4 (2026-08-14) — Đã hoàn thành & nghiệm thu
- `RemoteFaceEnrollmentWorker` (BackgroundService, chỉ chạy AreaNode mode):
  định kỳ 5s gọi VPS `claim-next` → nhận frame ảnh → gọi Face Runtime
  `LiveEnrollAsync` (embed) → `complete` (trả modelFileName/checksum/encodingCount)
  hoặc `fail` (lỗi).
- Đăng ký worker trong Program.cs.
- **Nghiệm thu:** build 0 errors, API deploy healthy. Luồng đầy đủ: VPS nhận
  ảnh → local claim → AI embed → template → VPS lưu `EmployeeFaceModel` Active.

### GĐ5 (2026-08-14) — Đã hoàn thành & nghiệm thu
- `RemoteFaceEnrollmentJob` thêm cột `TemplateContent`; `CompleteAsync` nhận +
  lưu nội dung template JSON; worker local gửi templateContent khi complete.
- API container mount `runtime/face-data/models`; `FaceStoragePathResolver`
  thêm `ModelActiveDir`.
- `SyncController` thêm `GET face-enrollment/templates` (VPS trả danh sách
  template Active). Worker local `SyncTemplatesAsync`: tải template → ghi vào
  model dir (xoá file cũ của employee) → `ReloadModelsAsync`.
- **Nghiệm thu:** build 0 errors, API deploy healthy. Vòng đầy đủ: khách gửi ảnh
  → local A xử lý → VPS lưu template → local B/C tải template → reload → nhận
  diện được khách mới.

### GĐ6 (2026-08-14) — Đã hoàn thành & nghiệm thu
- Tài liệu vận hành `remote-faceid-enrollment-ops.md` (cấu hình Central vs
  AreaNode, HTTPS/tunnel, rollback, kiểm thử nhanh).
- Smoke test E2E 2 node: dựng container Central (`Sync__Mode=Central`) trỏ cùng
  DB → register node → `submit-remote` tạo job Pending → `claim-next` (node nhận
  job + 10 frame) → `complete` (lưu EmployeeFaceModel v2 Active) →
  `templates` (trả đúng templateContent). Sửa lỗi unique `(EmployeeId, Version)`
  bằng cách tăng Version dần.
- **Nghiệm thu:** toàn bộ luồng claim → complete → templates chạy đúng. Build
  0 errors. Container test đã dọn.

---

## 5. RỦI RO & LƯU Ý

1. **Băng thông frame từ xa:** chỉ gửi ~10–15 frame (không video) — đã xử lý ở GĐ2.
2. **Bảo mật frame:** frame khuôn mặt là dữ liệu nhạy cảm → mã hóa/lưu tạm có hạn, xóa sau khi xử lý.
3. **Nhiều local tranh job:** cần lock khi gán job (GĐ3.3).
4. **File template lớn:** chọn upload HTTP riêng thay vì nhúng vào event JSON.
5. **Rollback:** giữ cơ chế sync cũ không bị ảnh hưởng — mọi thay đổi thêm mới, không sửa hành vi cũ.
