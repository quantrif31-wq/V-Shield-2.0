# Docker Dependency Map - V-Shield 2.0

## 1) Thanh phan runtime chinh

1. Frontend (`View`)
- Cong dev: `5173` (Vite dev server)
- Build output: static assets
- Goi API backend qua `VITE_API_BASE_URL` (fallback local theo `api.js`)

2. Backend (`API/API/API`) - .NET
- Cong local theo launch settings: `5107` (http), `7107` (https dev)
- Phu thuoc DB SQL Server qua `ConnectionStrings:DefaultConnection`
- Proxy sang AI services:
  - Face camera service (mac dinh `127.0.0.1:5001/api`)
  - Plate service (mac dinh `127.0.0.1:5002/api`)
  - FaceId service (mac dinh `127.0.0.1:8000`)
- Runtime orchestration (bat/tat process) qua `RuntimeOrchestrator`

3. AI Runtime (`AI_Runtime`)
- `QR_Dong/QR_Dong.py`: FastAPI, bind `0.0.0.0:8001`
- `doc_bien_gpu/docbien.py`: Flask, bind `0.0.0.0:${PORT|5002}`
- `AI_An_Ninh/app.py`: co logic RTSP/xu ly AI, can ra soat them trong giai doan trien khai
- `cam/go2rtc_win64`: go2rtc cho stream/bridge RTSP

4. Streaming/Tunnel tools
- go2rtc duoc backend khoi dong bang process local (duong dan file exe trong `AI_Runtime/cam/go2rtc_win64`)
- cloudflared duoc backend khoi dong process local dua tren `~/.cloudflared/config.yml`

5. Database
- SQL Server (hien mac dinh local `Server=.;Database=AccessControlDB;Trusted_Connection=True`)
- Migration duoc goi khi API startup (`db.Database.Migrate()`)

---

## 2) Luong ket noi service-to-service

1. Browser -> Frontend (Vite/Nginx)
2. Frontend -> API (`/api/...`)
3. API -> SQL Server (EF Core)
4. API -> Python Face camera (`AiServices:FaceCameraBaseUrl`)
5. API -> Python Plate (`AiServices:PlateBaseUrl`)
6. API -> Python FaceId (`AiServices:FaceIdBaseUrl`)
7. Frontend -> QR service truc tiep (mot so tinh nang goi `VITE_QR_API_BASE_URL`, fallback `http://localhost:8001`)
8. API -> go2rtc/cloudflared qua process management (hien dang legacy local process)

---

## 3) Diem hardcode/nhay cam can chuan hoa

1. Frontend
- `View/src/config/api.js`: fallback `localhost:{5107,5001,5002}`
- `View/src/services/dynamicQrScannerApi.js`: fallback `http://localhost:8001`
- `View/src/services/plateCameraApi.js`: fallback `127.0.0.1:5002/api`
- `View/src/components/GateTransitMonitor.vue`: fallback QR URL/plate URL local
- `View/vite.config.js`: proxy target fallback `http://127.0.0.1:5107`

2. API
- `appsettings*.json`: mac dinh localhost/127.0.0.1 cho AI services + origins
- Controller fallback:
  - `FaceCameraController` fallback `http://127.0.0.1:5001/api`
  - `PlateCameraController` fallback `http://127.0.0.1:5002/api`
  - `FaceRecognitionController` fallback `http://127.0.0.1:8000`
- CORS fallback hardcode localhost ports trong `Program.cs`

3. Runtime orchestration
- `RuntimeOrchestrator` dung `powershell.exe`, process `go2rtc.exe`, `cloudflared` local
- Day la logic phu hop local Windows, khong phu hop nguyen ban cho Linux container.

---

## 4) Rủi ro Docker hoa

1. Runtime mode xung dot
- Local mode: API tu start Python/go2rtc/cloudflared.
- Docker mode: nen de Compose quan ly container, API khong duoc tu spawn process.

2. DNS noi bo container
- Trong Docker, `localhost` se tro den chinh container, khong phai container khac.
- Can doi sang service name (`api`, `qr-runtime`, `plate-runtime`, `db`, ...).

3. Camera/RTSP
- Stream RTSP la tai nguyen LAN/edge, can mapping dung mang de container truy cap.
- Co the can host networking hoac route network phu hop.

4. CORS / public URL
- Frontend domain, API domain va go2rtc public URL phai tach ro local vs docker.

---

## 5) Huong xu ly de giu hanh vi cu

1. Them RuntimeMode (`local` | `docker`)
- `local`: giu logic legacy (co the bat process tu API neu can)
- `docker`: tat process orchestration trong API, chi goi service qua URL cau hinh

2. Chuan hoa service URL bang env
- Frontend: khong fallback cung vao localhost khi production/docker
- API: fallback chi danh cho dev; docker se lay URL tu env ro rang

3. Tach AI runtime thanh cac container rieng
- `qr-runtime` (8001)
- `plate-runtime` (5002)
- co the bo sung `face-runtime`/`faceid-runtime` tuy code thuc te dang dung

4. Compose quan ly startup sequence + healthcheck
- API cho db/ai healthy roi moi nhan request.
