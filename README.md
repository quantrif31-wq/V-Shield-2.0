# V-Shield

Hướng dẫn mới theo kiểu "một lệnh" để chạy trên máy Windows mới.

## Docker chạy nhanh (khuyên dùng cho người mới)

### Điều kiện
- Đã cài Docker Desktop và đang ở trạng thái `Engine running`.

### Lần đầu (setup)
1) Tạo file env cho Docker:
Chạy tại thư mục gốc dự án:

```powershell
Copy-Item .env.docker.example .env
```

2) Khởi động core stack:
```powershell
docker compose up -d --build
```

Core stack gồm:
- `db` (SQL Server)
- `api` (.NET)
- `frontend` (Vue + Nginx)

3) Bật thêm runtime AI (nếu cần):
QR runtime:
```powershell
docker compose --profile ai up -d --build
```

Plate runtime:
```powershell
docker compose --profile ai-heavy up -d --build
```

4) Kiểm tra nhanh:
```powershell
docker compose ps
curl http://localhost:5107/health
curl http://localhost:8001/qr/result
curl http://localhost:5002/api/camera/status
```

Kết quả mong đợi:
- API: `{"status":"ok","service":"v-shield-api"}`
- QR runtime: JSON có các trường `running`, `scan_enabled`, `locked`
- Plate runtime: JSON có `success=true`

### Từ lần thứ 2 trở đi
```powershell
docker compose up -d
```

Nếu cần rebuild sau khi đổi code:
```powershell
docker compose up -d --build
```

### Truy cập app
- Frontend: `http://localhost:5173`
- API: `http://localhost:5107`

### Dừng hệ thống Docker
```powershell
docker compose down
```

Nếu cần xóa cả dữ liệu DB volume:
```powershell
docker compose down -v
```

### Nếu gặp lỗi
Lấy log nhanh:
```powershell
docker compose ps
docker logs vshield-api --tail 100
docker logs vshield-qr-runtime --tail 100
docker logs vshield-plate-runtime --tail 100
```

## Docker + Cloudflare Tunnel (kh?ng VPS)

### B??c 1: L?y token tunnel tr?n m?y host
```bat
get-cloudflare-token.bat
```

Script s?:
- login cloudflare (c? m? browser c?p quy?n)
- t?o/b?o ??m tunnel t?n t?i
- t?o/b?o ??m DNS route
- in ra token ?? copy

### B??c 2: Setup Docker tunnel
```powershell
.\scripts\setup-docker-cloudflare-tunnel.ps1
```

Script s?:
- c?p nh?t `.env` (token, domain, go2rtc base)
- patch `appsettings.json` cho public domain
- ch?y `db -> go2rtc -> api -> frontend -> cloudflared`
- g?i reload go2rtc

M?c ??nh gi? logic c?:
- `GO2RTC_STREAM_MODE=webrtc`
- `GO2RTC_WEBRTC_CANDIDATES=` (?? tr?ng, kh?ng ?p candidate)
- stream URL public theo dang `https://<domain>/go2rtc/stream.html?...`

T?i li?u ??y ??:
- `docs/DOCKER_RUN_GUIDE.md`
- `docs/DOCKER_REGRESSION_CHECKLIST.md`
- `docs/DOCKER_UI_REGRESSION.md`

## 1) C?i ??t v? d?ng h? th?ng

Ch?y trong th? m?c g?c `V-Shield`:

```powershell
.\manage.ps1 -Action install
.\manage.ps1 -Action start
```

Ho?c d?ng file bat:

```bat
install.bat
start.bat
```

Trang ch?nh sau khi start:
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

### Start 1 click cho production (khuy?n d?ng khi ?? c?i Windows Services)

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

## 5) Tr?ng th?i script

```powershell
.\manage.ps1 -Action status
```
## Deployment modes

Project hien co 2 mode trien khai chinh:

### Local full

Dung cho may local/noi bo khi can day du thanh phan:

- Vue frontend
- ASP.NET API
- SQL Server
- go2rtc
- Cloudflared tunnel neu can
- AI runtime theo profile

Lenh chay:

```powershell
docker compose up -d --build
docker compose --profile ai up -d --build
docker compose --profile ai-heavy up -d --build
```

### VPS web-only

Dung cho VPS Ubuntu khi chi can stack web:

- Vue frontend
- ASP.NET API
- SQL Server

Khong keo theo go2rtc, camera public, Cloudflared, AI runtime hay APK/mobile.

Lenh chay:

```powershell
Copy-Item .env.vps.example .env.vps
docker compose --env-file .env.vps -f docker-compose.vps.yml up -d --build
```

Luu y:

- Frontend VPS proxy same-origin cho `/api` va `/hubs`
- API va SQL chi mo noi bo container trong mode VPS
- Compose VPS khong chay service go2rtc hay AI
- Dien day du secret trong `.env.vps` truoc khi deploy

Tai lieu bo sung:

- `docs/WEB_DEPLOYMENT_MODES.md`
