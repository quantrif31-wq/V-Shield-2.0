# Docker Run Guide (Safe Rollout)

## 1) Chuan bi
1. Copy file env:
   - Tu `.env.docker.example` thanh `.env`
2. Chinh sua mat khau SQL va cac URL neu can.

## 2) Chay stack day du cho may moi
Neu muc tieu la clone repo sang may khac va dung du local Docker, nen dung lenh day du nay truoc:

```bash
docker compose --profile ai --profile ai-heavy up -d --build
```

Stack day du gom:
- `db`
- `api`
- `frontend`
- `go2rtc`
- `qr-runtime`
- `qr-runtime-lane2`
- `plate-runtime`
- `face-runtime`

## 3) Chay stack co ban (nhanh hon)
Chi dung muc nay neu ban moi can web/API/co so du lieu:

Stack co ban gom:
- `db`
- `api`
- `frontend`
- `go2rtc`

Lenh:
```bash
docker compose up -d --build
```

Kiem tra:
- Frontend: `http://localhost:5173`
- API health: `http://localhost:5107/health`

## 4) Bat QR runtime (optional)
```bash
docker compose --profile ai up -d --build
```

Se them service `qr-runtime` (port `8001`) va `qr-runtime-lane2` (port `8002`).
Service nay da duoc dat `QR_HEADLESS=1` trong Docker de tranh loi GUI/display.

## 5) Bat Plate + Face runtime (heavy)
```bash
docker compose --profile ai-heavy up -d --build
```

Luu y:
- `plate-runtime` dung dependencies nang (paddle/torch), build co the lau va can tai nguyen lon.
- Docker image da chuyen qua `requirements.docker.txt` (CPU-compatible) de giam xung dot dependency.
- Service duoc bat `LPR_HEADLESS=1` de tranh loi `qt.qpa.xcb` khi khong co man hinh GUI.
- Nhom nay cung bat service Face ID duy nhat `face-runtime`.

## 6) Kiem tra nhanh sau khi len stack
Chay lan luot:
```bash
curl http://localhost:5107/health
curl http://localhost:8001/qr/result
curl http://localhost:8002/qr/result
curl http://localhost:5002/api/camera/status
curl http://localhost:1984/
```

Face ID khong publish cong Python ra host. Kiem tra Face Runtime qua endpoint
ASP.NET co xac thuc `/api/FaceCamera/models`; `face-runtime:5001` chi resolve
trong Docker network `vshield-face-backend`. FastAPI `FaceID.py`,
`faceid-runtime` va port `8000` da bi loai bo.

Ket qua mong doi:
- API: `{"status":"ok","service":"v-shield-api"}`
- QR runtime: JSON co cac truong `running`, `scan_enabled`, `locked`, ...
- QR lane 2 runtime: JSON tuong tu lane 1
- Plate runtime: JSON co `success=true`, `camera_enabled=false` (neu chua mo cam)
- Face runtime: co response status
- go2rtc: mo duoc web UI/noi dung tra ve

## 7) Tat stack
```bash
docker compose down
```

## 8) Reset ca volume DB
```bash
docker compose down -v
```

## 9) Giai thich Runtime mode
- API da bo sung `Runtime__Mode`.
- Trong docker compose, mode dat la `docker`:
  - API KHONG tu spawn process local (PowerShell/go2rtc.exe/cloudflared).
  - Runtime duoc quan ly boi docker compose.
  - Muon co du het tinh nang thi phai bat dung profile AI, khong chi moi `api` va `frontend`.
- Ngoai docker:
  - Dung `Runtime:Mode=local` de giu hanh vi cu.

## 10) Xu ly su co thuong gap
1. `SystemAuditLogs chua san sang`:
   - Kiem tra API da restart sau khi pull code moi.
   - Xem log `vshield-api` de xac nhan migration da chay xong.
2. QR/LPR timeout:
   - Kiem tra container runtime co dang `Up` khong: `docker compose ps`
   - Xem log: `docker logs vshield-qr-runtime`, `docker logs vshield-qr-runtime-lane2`, `docker logs vshield-plate-runtime`, `docker logs vshield-face-runtime`
3. Loi CORS tren frontend:
   - Kiem tra `APP_FRONTEND_URL`, `VITE_API_BASE_URL` trong `.env`
   - Rebuild frontend: `docker compose up -d --build frontend`
