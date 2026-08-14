# HƯỚNG DẪN VẬN HÀNH — ĐĂNG KÝ FACE ID TỪ XA

> Bổ trợ cho `remote-faceid-enrollment-plan.md`. Mô tả cách triển khai VPS
> (central) + local node (area node) để khách đăng ký Face ID từ xa.

---

## 1. Vai trò 2 loại node

| Node | SYNC_MODE | Có AI (Face Runtime)? | Nhiệm vụ |
|---|---|---|---|
| **VPS (Central)** | `Central` | Không bắt buộc | Nhận ảnh từ app → job queue → phát template xuống local |
| **Local (AreaNode)** | `AreaNode` | Có (GPU) | Định kỳ gọi VPS: claim job → chạy AI → trả template |

> Bản local hiện tại (chạy `docker compose`) có cả AI và có thể đóng vai trò
> `AreaNode`. Bản VPS chỉ cần API + DB, không cần face-runtime container.

---

## 2. Cấu hình VPS (Central)

Tạo `.env` trên VPS:

```env
SYNC_MODE=Central
SYNC_REGISTRATION_KEY=<khoa-bi-mat-cho-node-dang-ky>
```

Khởi động chỉ các service cần thiết (api + db), không cần profile ai:

```bash
docker compose up -d api db
```

---

## 3. Cấu hình Local node (AreaNode)

Tạo `.env` trên local:

```env
SYNC_MODE=AreaNode
SYNC_CENTRAL_BASE_URL=https://vps.tencongty.com
SYNC_REGISTRATION_KEY=<cung-khoa-bi-mat>
SYNC_LOCAL_AREA_NODE_ID=node-hcm-01
SYNC_COMPANY_ID=1
SYNC_SITE_ID=1
SYNC_DISPLAY_NAME=Chi nhanh HCM
```

Khởi động đầy đủ (có AI):

```bash
docker compose --profile ai --profile ai-heavy up -d
```

Local node sẽ tự đăng ký với VPS (dùng `SYNC_REGISTRATION_KEY`) và định kỳ
push/pull qua `AreaNodeSyncWorker` + xử lý Face ID qua `RemoteFaceEnrollmentWorker`.

---

## 4. Luồng đăng ký từ xa (đã code xong)

```
(1) Khách (webcam) → POST /api/FaceEnrollment/submit-remote → VPS tạo job Pending
(2) Local AreaNode (worker 5s) → POST /api/sync/face-enrollment/claim-next
    → nhận frame ảnh → gọi Face Runtime LiveEnrollAsync (embed)
(3) Local → POST /api/sync/face-enrollment/{jobId}/complete
    (kèm templateContent) → VPS lưu EmployeeFaceModel Active
(4) Các local khác (worker) → GET /api/sync/face-enrollment/templates
    → tải template → ghi model dir → reload Face Runtime
```

---

## 5. HTTPS & camera thiết bị

`getUserMedia` (webcam khách) chỉ chạy trên **HTTPS** hoặc localhost. Khi khách
truy cập từ xa qua VPS, **bắt buộc dùng HTTPS**:

- Đã có cloudflared tunnel (SSL miễn phí): `setup-public-domain.bat`.
- Hoặc đặt nginx + Let's Encrypt.

Nếu không HTTPS, trình duyệt chặn camera → đăng ký không mở được.

---

## 6. Rollback

Các thay đổi đều **thêm mới** (bảng, endpoint, worker mới), không sửa hành vi
cũ. Rollback an toàn:

```bash
# 1. Tắt worker + endpoint mới (về SYNC_MODE cũ)
#    -> đặt SYNC_MODE=Standalone
docker compose down
git checkout <commit-truoc-khi-lam-faceid-remote>
docker compose --profile ai --profile ai-heavy up -d --build
```

- Bảng mới `RemoteFaceEnrollmentJobs`/`RemoteFaceEnrollmentFrames` có thể giữ
  lại (vô hại) hoặc drop nếu cần: `dotnet ef database update <migration-truoc>`.
- Face ID vẫn đăng ký được trực tiếp trên local qua `enroll-self` (GĐ1) — không
  phụ thuộc sync.

---

## 7. Kiểm thử nhanh

Trên VPS (Central):

```bash
# Đăng ký node (local làm tự động)
curl -X POST https://vps/api/sync/nodes/register \
  -H "X-VShield-Registration-Key: <key>" \
  -H "Content-Type: application/json" \
  -d '{"areaNodeId":"node-test","companyId":1,"siteId":1,"displayName":"Test","version":"2.0.0","gateIds":[],"laneIds":[],"zoneIds":[]}'
```

Trên local (AreaNode) — worker tự chạy, xem log:

```bash
docker logs vshield-api | grep RemoteFaceEnrollment
```
