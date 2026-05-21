# V-Shield

Huong dan moi theo kieu "mot lenh" de chay tren may Windows moi.

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
