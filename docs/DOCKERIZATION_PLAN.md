# Ke hoach dong goi Docker an toan cho V-Shield 2.0

## Muc tieu
- He thong chay duoc theo 2 cach:
  - Clone ve va chay local khong Docker (giu nguyen hanh vi hien tai).
  - Clone ve va chay bang Docker Compose.
- Chuan hoa toan bo ket noi theo moi truong (`local`, `docker`) de tranh hardcode.
- Dam bao tinh nang giu nguyen nhu cu.

## Nguyen tac an toan
1. Khong doi logic nghiep vu neu khong can thiet.
2. Tach cau hinh khoi code (env/appsettings) theo moi truong.
3. Moi giai doan deu co tieu chi xac minh.
4. Luon giu tuong thich nguoc voi cach chay local hien tai.

## Pham vi
- Frontend: `View` (Vue/Vite).
- Backend: `API/API/API` (.NET).
- Runtime AI/Python: `AI_Runtime` (QR/camera services).
- Database va migration.
- Script van hanh/start-stop.

## Giai doan thuc hien

### Giai doan 1 - Khao sat va lap ban do phu thuoc
- Kiem ke tat ca process/dich vu can de he thong hoat dong.
- Quet toan bo code tim endpoint hardcode (`localhost`, `127.0.0.1`, port, RTSP, ...).
- Lap bang ket noi service-to-service.
- Tao danh sach bien moi truong can chuan hoa.

**Dau ra**
- `docs/DOCKER_DEPENDENCY_MAP.md`
- `docs/DOCKER_ENV_MATRIX.md`

### Giai doan 2 - Chuan hoa cau hinh ket noi
- Frontend:
  - Chuan hoa API base URL qua `import.meta.env`.
  - Loai bo hardcode endpoint trong components/services.
- Backend:
  - Chuan hoa `appsettings.*` + env override.
  - Chuan hoa CORS va URL goi runtime Python.
- Python runtime:
  - Doc host/port/base URL tu env.
- Bo sung co che chuyen mode `local`/`docker`.

**Dau ra**
- Chay local van on, khong vo tinh nang.

### Giai doan 3 - Tao Dockerfile cho tung thanh phan
- `View`: build + Nginx serve.
- `API`: .NET multi-stage.
- `AI_Runtime`: Python image + healthcheck.
- Bo sung `.dockerignore` cho tung module.

### Giai doan 4 - Docker Compose full stack
- Tao `docker-compose.yml` cho:
  - frontend
  - api
  - ai-runtime (hoac nhieu service neu dang tach)
  - db
- Cau hinh volume, network, healthcheck, restart policy.
- Tao file env mau cho docker.

### Giai doan 5 - Migration + bootstrap
- Chot cach run migration an toan.
- Dam bao cac bang moi (vd: `SystemAuditLogs`) duoc tao dung.
- Kiem tra seed/co du lieu test toi thieu.

### Giai doan 6 - Kiem thu hoi quy
- Login/logout, role, CRUD nhan su/khach, pre-registration.
- Access permission, gate monitor, QR scan/verify, audit logs.
- Kiem tra chuoi ket noi frontend-api-runtime-db.

### Giai doan 7 - Tai lieu van hanh
- Tao huong dan:
  - chay local
  - chay docker
  - debug su co thuong gap

## Tieu chi hoan thanh
1. `docker compose up -d` len full stack.
2. Tinh nang cot loi hoat dong giong local.
3. Chuyen qua lai local/docker bang cau hinh, khong sua tay code.
4. Team moi clone repo co the tu chay theo huong dan.

## Tien do
- [x] Tao ke hoach
- [x] Giai doan 1 - Khao sat
- [x] Giai doan 2 - Chuan hoa ket noi
- [x] Giai doan 3 - Dockerfile
- [x] Giai doan 4 - Compose
- [x] Giai doan 5 - Migration
- [ ] Giai doan 6 - Kiem thu
- [ ] Giai doan 7 - Tai lieu

## Moc da xac thuc (2026-05-29)
- Docker core stack (`db`, `api`, `frontend`) da build + run on dinh.
- API health check OK: `/health`.
- Migration tu dong khi startup API da chay thanh cong.
- Login API test thanh cong voi tai khoan seed admin.
- Profile `ai` (QR runtime) da chay trong Docker sau khi them che do `QR_HEADLESS=1`.
- Profile `ai-heavy` (plate runtime) da chuyen sang `requirements.docker.txt` (ban CPU) va bo sung che do `LPR_HEADLESS=1` de tiep tuc on dinh runtime trong Docker.
