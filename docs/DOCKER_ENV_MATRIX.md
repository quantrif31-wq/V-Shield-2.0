# Docker Environment Matrix - V-Shield 2.0

## 1) Bien moi truong de chuan hoa

### Frontend (`View`)
- `VITE_API_BASE_URL`
  - Local: `http://localhost:5107/api`
  - Docker (trinh duyet user): `http://localhost:5107/api` (neu expose API 5107)
- `VITE_PLATE_API_BASE_URL`
  - Local: `http://localhost:5002/api`
  - Docker: qua API proxy neu can
- `VITE_QR_API_BASE_URL`
  - Local: `http://localhost:8001`
  - Docker: `http://localhost:8001` (neu expose truc tiep) hoac URL proxy qua API
- `VITE_DEV_PROXY_TARGET`
  - Local dev: `http://127.0.0.1:5107`

### Backend (`API`)
- `ASPNETCORE_URLS`
  - Docker de xuat: `http://+:5107`
- `ConnectionStrings__DefaultConnection`
  - Local: SQL local/trusted
  - Docker: `Server=db,1433;Database=AccessControlDB;User Id=sa;Password=...;TrustServerCertificate=True;Encrypt=False;`
- `AiServices__FaceCameraBaseUrl`
  - Local: `http://127.0.0.1:5001/api`
  - Docker/VPS: `http://face-runtime:5001/api` tren network `vshield-face-backend`
- `AiServices__PlateBaseUrl`
  - Local: `http://127.0.0.1:5002/api`
  - Docker: `http://plate-runtime:5002/api`
- `AiServices__FaceIdBaseUrl`
  - Local: `http://127.0.0.1:8000`
  - Docker: `http://faceid-runtime:8000` (neu tach container)
- `AppSettings__FrontendUrl`
  - Local: `http://localhost:5173`
  - Docker: URL frontend that user truy cap (vd `http://localhost:5173` hoac domain)
- `AppSettings__AllowedOrigins__0..n`
  - danh sach origin frontend theo tung moi truong
- `AppSettings__Go2RtcPublicBaseUrl`
  - URL cong khai go2rtc (neu can)
- `Cloudflared__*`
  - Chi dung khi run mode co tunnel
- `RuntimePaths__AiRootFolderName`
  - Local: `AI_Runtime`
  - Docker: co the khong can neu bo process spawning
- `Runtime__Mode` (se bo sung)
  - `local` | `docker`

### AI Runtime (Python)
- `PORT`
  - Plate runtime da ho tro (mac dinh 5002)
- `HOST`
  - Nen bo sung, default `0.0.0.0`
- `QR_HEADLESS`
  - Docker QR runtime: `1` (tat `cv2.imshow`, chay headless)
  - Local debug GUI: de trong hoac `0`
- `CORS_ORIGINS`
  - Nen bo sung de han che theo moi truong
- `LOG_LEVEL` / `DEBUG`
  - tuy chon

### Database
- `MSSQL_SA_PASSWORD`
- `ACCEPT_EULA=Y`

---

## 2) Mapping local vs docker (de xuat)

1. Local khong Docker
- Frontend dev: `localhost:5173`
- API: `localhost:5107`
- QR runtime: `localhost:8001`
- Plate runtime: `localhost:5002`
- DB: local SQL Server

2. Docker Compose
- frontend container: expose `5173` hoac `80`
- api container: expose `5107`
- qr-runtime container: expose `8001`
- plate-runtime container: expose `5002`
- db container: expose noi bo `1433` (co the mo ra host neu can)
- Ket noi noi bo bang service name (khong dung localhost trong noi bo container)

---

## 3) Cong viec can lam tiep theo (Giai doan 2)

1. Loai fallback hardcode localhost trong frontend service files.
2. Chuan hoa fallback API/Python URL trong backend:
   - fallback dev van giu,
   - docker mode bat buoc dung env.
3. Bo sung `Runtime__Mode` va tat `RuntimeOrchestrator` process-spawn khi o docker mode.
4. Chuan hoa CORS theo env de tranh 5173/5107 mismatch.
