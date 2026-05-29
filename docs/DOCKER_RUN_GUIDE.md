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

## 5) Tat stack
```bash
docker compose down
```

## 6) Reset ca volume DB
```bash
docker compose down -v
```

## 7) Giai thich Runtime mode
- API da bo sung `Runtime__Mode`.
- Trong docker compose, mode dat la `docker`:
  - API KHONG tu spawn process local (PowerShell/go2rtc.exe/cloudflared).
  - Runtime duoc quan ly boi docker compose.
- Ngoai docker:
  - Dung `Runtime:Mode=local` de giu hanh vi cu.
