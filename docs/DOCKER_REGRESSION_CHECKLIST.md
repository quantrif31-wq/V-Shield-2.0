# Docker Regression Checklist

Ngay cap nhat: 2026-05-29

## 1) Trang thai stack
- [x] `docker compose up -d`
- [x] `docker compose --profile ai up -d`
- [x] `docker compose --profile ai-heavy up -d`
- [x] `db`, `api`, `frontend`, `qr-runtime`, `plate-runtime` deu `Up`

## 2) Smoke test endpoint
- [x] `GET /health` -> `status=ok`
- [x] `GET qr-runtime /qr/result` -> JSON hop le
- [x] `GET plate-runtime /api/camera/status` -> JSON hop le

## 3) Hoi quy API co xac thuc
- [x] `POST /api/Auth/login` (admin) thanh cong
- [x] `GET /api/Employees` thanh cong
- [x] `GET /api/pre-registrations?page=1&pageSize=5` thanh cong
- [x] `GET /api/access-permissions/employee-matrix?page=1&pageSize=5` thanh cong
- [x] `GET /api/access-permissions/visitor-matrix?page=1&pageSize=5` thanh cong
- [x] `GET /api/access-logs/system-audit?page=1&pageSize=10` thanh cong

## 4) Luu y on dinh runtime
- QR runtime da bat che do headless (`QR_HEADLESS=1`).
- Plate runtime da bat che do headless (`LPR_HEADLESS=1`) va dung `requirements.docker.txt` CPU-compatible.
- Lan dau khoi dong `plate-runtime` co the mat them thoi gian de tai model OCR.

## 5) Muc can test tay UI (con lai)
- [ ] Dang nhap/dang xuat tren giao dien `localhost:5173`
- [ ] Luong pre-registration -> duyet -> link khach
- [ ] Trang `access-permission-manager` (employee + visitor tabs)
- [ ] Trang `system-audit-logs` (bo loc + panel chi tiet)
- [ ] Luong camera thuc te (RTSP that) cho QR/LPR

## 6) Ket luan tam thoi
- Back-end/API va runtime container da dat muc san sang de tiep tuc test nghiep vu tren UI.
- Chua co dau hieu vo ket noi giua frontend-api-runtime trong cau hinh Docker hien tai.
