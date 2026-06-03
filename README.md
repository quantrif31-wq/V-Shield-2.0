# V-Shield

Huong dan moi theo kieu "mot lenh" de chay tren may Windows moi.

## Docker chay nhanh (khuyen dung cho nguoi moi)

### Dieu kien
- Da cai Docker Desktop va dang o trang thai `Engine running`.

### Lan dau (setup)
1) Tao file env cho Docker:
Chay tai thu muc goc du an:

```powershell
Copy-Item .env.docker.example .env
```

2) Khoi dong core stack:
```powershell
docker compose up -d --build
```

Core stack gom:
- `db` (SQL Server)
- `api` (.NET)
- `frontend` (Vue + Nginx)

3) Bat them runtime AI (neu can):
QR runtime:
```powershell
docker compose --profile ai up -d --build
```

Plate runtime:
```powershell
docker compose --profile ai-heavy up -d --build
```

4) Kiem tra nhanh:
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

### Tu lan thu 2 tro di
```powershell
docker compose up -d
```

Neu can rebuild sau khi doi code:
```powershell
docker compose up -d --build
```

### Truy cap app
- Frontend: `http://localhost:5173`
- API: `http://localhost:5107`

### Dung he thong Docker
```powershell
docker compose down
```

Neu can xoa ca du lieu DB volume:
```powershell
docker compose down -v
```

### Neu gap loi
Lay log nhanh:
```powershell
docker compose ps
docker logs vshield-api --tail 100
docker logs vshield-qr-runtime --tail 100
docker logs vshield-plate-runtime --tail 100
```

## Docker + Cloudflare Tunnel (khong VPS)

### Buoc 1: Lay token tunnel tren may host
```bat
get-cloudflare-token.bat
```

Script se:
- login cloudflare (co mo browser cap quyen)
- tao/bao dam tunnel ton tai
- tao/bao dam DNS route
- in ra token de copy

### Buoc 2: Setup Docker tunnel
```powershell
.\scripts\setup-docker-cloudflare-tunnel.ps1
```

Script se:
- cap nhat `.env` (token, domain, go2rtc base)
- patch `appsettings.json` cho public domain
- chay `db -> go2rtc -> api -> frontend -> cloudflared`
- goi reload go2rtc

Mac dinh giu logic cam cu:
- `GO2RTC_STREAM_MODE=webrtc`
- `GO2RTC_WEBRTC_CANDIDATES=` (de trong, khong ep candidate)
- stream URL public theo dang `https://<domain>/go2rtc/stream.html?...`

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

## Windows non-Docker: cấu hình domain public + Cloudflare

Dùng phần này khi chạy dự án trực tiếp trên Windows bằng các file `.bat`, không chạy Docker.
Mục tiêu là public camera qua Cloudflare Tunnel để các máy khác xem được stream go2rtc.

### Chạy lần đầu

```bat
setup-public-domain.bat
```

Script sẽ hỏi domain, tên tunnel và chế độ cấu hình. Có 2 chế độ:

- `AUTO`: script tự mở đăng nhập Cloudflare, tạo hoặc dùng lại tunnel, tạo DNS route, lấy token, patch `appsettings.json`, bật `cloudflared`, bật `go2rtc`, rồi reload URL camera.
- `MANUAL_TOKEN`: bạn tự dán `CLOUDFLARED_TUNNEL_TOKEN`; script bỏ qua bước mở web/tạo route, chỉ dùng token để chạy tunnel, patch cấu hình và reload camera.

Nên chọn:

- Chọn `AUTO` nếu máy cài có trình duyệt và muốn script làm gần như toàn bộ.
- Chọn `MANUAL_TOKEN` nếu máy khách không mở được trình duyệt, đăng nhập Cloudflare bị lỗi, hoặc bạn đã có token từ trước.

Sau khi chạy thành công, stream mẫu sẽ có dạng:

```text
https://<domain>/stream.html?src=cam1&mode=webrtc
```

Ví dụ:

```text
https://cam.example.com/stream.html?src=cam1&mode=webrtc
```

### Lấy token thủ công nếu chọn MANUAL_TOKEN

Nếu chưa có token, chạy:

```bat
get-cloudflare-token.bat
```

File này sẽ mở trình duyệt để bạn cấp quyền Cloudflare, đảm bảo tunnel/DNS route tồn tại, rồi in token ra màn hình.
Copy token đó và dán vào `setup-public-domain.bat` khi chọn chế độ `MANUAL_TOKEN`.

### Chạy lại sau khi đã cấu hình

Nếu chỉ cần bật lại hệ thống:

```bat
start.bat
```

Nếu cần chạy lại cấu hình public domain, cứ chạy lại:

```bat
setup-public-domain.bat
```

Script được thiết kế để chạy lại an toàn: tunnel đã có thì dùng lại, cấu hình đã có thì cập nhật lại theo giá trị mới.

### Gỡ cấu hình public domain

```bat
uninstall-public-domain.bat
```

Script gỡ sẽ hỏi trước các thao tác nhạy cảm như xóa tunnel, xóa credential Cloudflare, reset `appsettings.json` hoặc dọn URL camera trong DB.
Nếu chỉ muốn xem script sẽ làm gì mà chưa muốn xóa thật, chạy:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\uninstall-public-domain.ps1 -DryRun
```

### Lỗi thường gặp

- `Token is required`: đang chọn `MANUAL_TOKEN` nhưng chưa dán token.
- `Tunnel token is not valid`: token sai, hết hạn hoặc không thuộc tunnel/domain đang dùng.
- `cloudflared not found`: cài Cloudflare Tunnel bằng `winget install Cloudflare.cloudflared` rồi chạy lại.
- Stream bị đen hoặc `stream not found`: kiểm tra camera trong app, chạy lại `setup-public-domain.bat`, sau đó mở trực tiếp `https://<domain>/stream.html?src=cam1&mode=webrtc` để test.

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
