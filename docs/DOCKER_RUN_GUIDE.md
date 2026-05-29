# Docker Run Guide (Safe Rollout)

## 1) Chuan bi
1. Copy file env:
   - Tu `.env.docker.example` thanh `.env`
2. Chinh sua mat khau SQL va cac URL neu can.

## 2) Chay stack co ban (an toan)
Stack co ban gom:
- `db`
- `api`
- `frontend`

Lenh:
```bash
docker compose up -d --build
```

Kiem tra:
- Frontend: `http://localhost:5173`
- API health: `http://localhost:5107/health`

## 3) Bat QR runtime (optional)
```bash
docker compose --profile ai up -d --build
```

Se them service `qr-runtime` (port `8001`).
Service nay da duoc dat `QR_HEADLESS=1` trong Docker de tranh loi GUI/display.

## 4) Bat Plate runtime (heavy)
```bash
docker compose --profile ai-heavy up -d --build
```

Luu y:
- `plate-runtime` dung dependencies nang (paddle/torch), build co the lau va can tai nguyen lon.
- Docker image da chuyen qua `requirements.docker.txt` (CPU-compatible) de giam xung dot dependency.
- Service duoc bat `LPR_HEADLESS=1` de tranh loi `qt.qpa.xcb` khi khong co man hinh GUI.

## 5) Kiem tra nhanh sau khi len stack
Chay lan luot:
```bash
curl http://localhost:5107/health
curl http://localhost:8001/qr/result
curl http://localhost:5002/api/camera/status
```

Ket qua mong doi:
- API: `{"status":"ok","service":"v-shield-api"}`
- QR runtime: JSON co cac truong `running`, `scan_enabled`, `locked`, ...
- Plate runtime: JSON co `success=true`, `camera_enabled=false` (neu chua mo cam)

## 6) Tat stack
```bash
docker compose down
```

## 7) Reset ca volume DB
```bash
docker compose down -v
```

## 8) Giai thich Runtime mode
- API da bo sung `Runtime__Mode`.
- Trong docker compose, mode dat la `docker`:
  - API KHONG tu spawn process local (PowerShell/go2rtc.exe/cloudflared).
  - Runtime duoc quan ly boi docker compose.
- Ngoai docker:
  - Dung `Runtime:Mode=local` de giu hanh vi cu.

## 9) Xu ly su co thuong gap
1. `SystemAuditLogs chua san sang`:
   - Kiem tra API da restart sau khi pull code moi.
   - Xem log `vshield-api` de xac nhan migration da chay xong.
2. QR/LPR timeout:
   - Kiem tra container runtime co dang `Up` khong: `docker compose ps`
   - Xem log: `docker logs vshield-qr-runtime`, `docker logs vshield-plate-runtime`
3. Loi CORS tren frontend:
   - Kiem tra `APP_FRONTEND_URL`, `VITE_API_BASE_URL` trong `.env`
   - Rebuild frontend: `docker compose up -d --build frontend`
