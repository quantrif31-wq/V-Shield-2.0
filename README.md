# V-Shield

Huong dan moi theo kieu "mot lenh" de chay tren may Windows moi.

## Docker chay nhanh (khuyen dung cho nguoi moi)

### Dieu kien
- Da cai Docker Desktop va dang o trang thai `Engine running`.

### 1) Tao file env cho Docker
Chay tai thu muc goc du an:

```powershell
Copy-Item .env.docker.example .env
```

### 2) Chay core stack
```powershell
docker compose up -d --build
```

Core stack gom:
- `db` (SQL Server)
- `api` (.NET)
- `frontend` (Vue + Nginx)

### 3) Bat them runtime AI (neu can)
QR runtime:
```powershell
docker compose --profile ai up -d --build
```

Plate runtime:
```powershell
docker compose --profile ai-heavy up -d --build
```

### 4) Kiem tra nhanh
```powershell
docker compose ps
curl http://localhost:5107/health
curl http://localhost:8001/qr/result
curl http://localhost:5002/api/camera/status
```

Ket qua mong doi:
- API: `{"status":"ok","service":"v-shield-api"}`
- QR runtime: JSON co cac truong `running`, `scan_enabled`, `locked`
- Plate runtime: JSON co `success=true`

### 5) Truy cap app
- Frontend: `http://localhost:5173`
- API: `http://localhost:5107`

### 6) Dung he thong Docker
```powershell
docker compose down
```

Neu can xoa ca du lieu DB volume:
```powershell
docker compose down -v
```

### 7) Neu gap loi
Lay log nhanh:
```powershell
docker compose ps
docker logs vshield-api --tail 100
docker logs vshield-qr-runtime --tail 100
docker logs vshield-plate-runtime --tail 100
```

Tai lieu day du:
- `docs/DOCKER_RUN_GUIDE.md`
- `docs/DOCKER_REGRESSION_CHECKLIST.md`
- `docs/DOCKER_UI_REGRESSION.md`

## 1) Cai dat va dung he thong

Chay trong thu muc goc `V-Shield`:

```powershell
.\manage.ps1 -Action install
.\manage.ps1 -Action start
```

Hoac dung file bat:

```bat
install.bat
start.bat
```

Trang chinh sau khi start:
- API: `http://localhost:5107`
- Frontend: `http://localhost:5173`
- Health API: `http://localhost:5107/health`

### Start 1 click cho production (khuyen dung khi da cai Windows Services)

```bat
start-prod.bat
```

Script se uu tien `Start-Service` cho cac service `vshield-*`. Neu chua cai service thi se fallback ve `manage.ps1 -Action start`.

## 2) Dung he thong

```powershell
.\manage.ps1 -Action stop
```

Hoac:

```bat
stop.bat
```

Neu dang chay theo service production:

```bat
stop-prod.bat
```

## 3) Go moi truong runtime/dependency

```powershell
.\manage.ps1 -Action uninstall
```

Hoac:

```bat
uninstall.bat
```

Script uninstall se:
- Dung process API/Frontend dang chay
- Xoa `node_modules`, `venv` AI, `.runtime`
- `dotnet clean`

Luu y:
- Script khong tu dong xoa database de tranh mat du lieu ngoai y muon.
- Neu can xoa DB, hay tao script rieng cho tung moi truong.

## 4) Cloudflared + go2rtc config

Cau hinh tai `API/API/API/appsettings*.json`:

- `Cloudflared:TunnelName`
- `Cloudflared:PublicHostname`
- `Cloudflared:TargetService`
- `AppSettings:Go2RtcPublicBaseUrl`

Khong hardcode domain trong code nua.

## 5) Trang thai script

```powershell
.\manage.ps1 -Action status
```
