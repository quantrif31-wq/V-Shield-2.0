# V-Shield

Huong dan cai dat va chay V-Shield theo 3 nhu cau pho bien:

- `Docker local`: danh cho may Windows ca nhan, de dung nhat
- `Docker VPS`: danh cho may chu public
- `Windows non-Docker`: chi dung khi ban muon chay truc tiep bang `manage.ps1`

Neu ban chi muon chay du an nhanh tren may cua minh, hay di theo muc `Docker local`.

## 1. Chon cach cai dat

### Lua chon A: Docker local

Dung khi:

- Ban dang dung Windows
- Ban muon mo app tai `http://localhost:5173`
- Ban muon co san SQL Server + API + frontend + go2rtc trong cung mot stack
- Ban muon may khac clone repo ve co the dung day du cac luong local Docker

### Lua chon B: Docker VPS

Dung khi:

- Ban deploy len VPS
- Ban muon stack gon hon, uu tien web/API
- Ban can secret va bien moi truong rieng cho moi truong public

### Lua chon C: Windows non-Docker

Dung khi:

- Ban muon debug local truc tiep bang `dotnet` + `npm`
- Ban khong muon dung container

Mac dinh cua repo hien tai:

- Docker local frontend: `http://localhost:5173`
- Windows non-Docker frontend: `http://localhost:5174`
- API local Docker: `http://localhost:5107`
- API non-Docker: `http://127.0.0.1:5108`

## 2. Docker local

### 2.1. Dieu kien

Can co:

- Docker Desktop
- Docker Desktop dang o trang thai `Engine running`

Kiem tra nhanh:

```powershell
docker version
docker compose version
```

### 2.2. Chay lan dau

1. Tao file env:

```powershell
Copy-Item .env.docker.example .env
```

2. Khoi dong stack day du cho may moi:

```powershell
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

Neu ban chi muon len web/API co ban, dung lenh nhe hon ben duoi:

```powershell
docker compose up -d --build
```

Stack co ban gom:

- `db`
- `api`
- `frontend`
- `go2rtc`

3. Truy cap:

- Frontend: [http://localhost:5173](http://localhost:5173)
- API health: [http://localhost:5107/health](http://localhost:5107/health)

4. Kiem tra container:

```powershell
docker compose ps
```

### 2.3. Bat them AI runtime khi can

Neu ban da chay stack co ban truoc do, co the bat them tung nhom service nhu sau:

QR runtime:

```powershell
docker compose --profile ai up -d --build
```

Plate + Face runtime:

```powershell
docker compose --profile ai-heavy up -d --build
```

Luu y:

- `--profile ai` bat `qr-runtime` va `qr-runtime-lane2`
- `--profile ai-heavy` bat `plate-runtime` va `face-runtime`
- Neu muon may local dung du het QR, plate, face ngay tu dau thi dung:

```powershell
docker compose --profile ai --profile ai-heavy up -d --build
```

- Neu chi dung `docker compose up -d --build` thi ban chua co cac runtime AI nang

Phan bo tinh nang theo service:

- QR lane 1: `qr-runtime`
- QR lane 2: `qr-runtime-lane2`
- Plate recognition: `plate-runtime`
- Face ID: Flask `nhandienface.py` trong service `face-runtime`
- Streaming camera/WebRTC/MSE: `go2rtc`

Kiem tra nhanh:

```powershell
curl http://localhost:5107/health
curl http://localhost:8001/qr/result
curl http://localhost:8002/qr/result
curl http://localhost:5002/api/camera/status
curl http://localhost:1984/
```

Face ID chi duoc truy cap qua ASP.NET (`/api/FaceCamera/...`). Python Face
Runtime khong publish port `5001` ra host; backend ket noi service duy nhat
`face-runtime` bang `http://face-runtime:5001/api` tren Docker bridge rieng.
FastAPI `FaceID.py`, service `faceid-runtime`, port `8000` va route
`/api/face-recognition/*` da bi loai bo.

### 2.4. Tu lan sau

Chay lai stack:

```powershell
docker compose up -d
```

Neu vua sua code va muon build lai:

```powershell
docker compose up -d --build
```

Neu chi muon build lai frontend va api:

```powershell
docker compose build api frontend
docker compose up -d --force-recreate api frontend
```

### 2.4.1. Luu tru video camera tren Docker local

He thong hien tai tu dong bat ghi hinh cho camera da ket noi va luu metadata ve DB local.

- File video duoc luu duoi thu muc `runtime/uploads/recordings`
- Metadata duoc luu trong bang `Recorded_Segment`
- Trang tra cuu archive co the loc theo camera va moc thoi gian

Trong Docker local, recorder se uu tien nguon noi bo Docker de tranh loi goi nham `localhost:5173` tu trong container API.

### 2.5. Dung va don dep

Dung stack:

```powershell
docker compose down
```

Dung va xoa ca volume database:

```powershell
docker compose down -v
```

Luu y:

- Repo da duoc cau hinh de giu khoa `Data Protection` bang volume rieng
- Vi vay MFA se khong bi mat chi vi ban restart hoac recreate container

### 2.6. Neu gap loi

Xem trang thai:

```powershell
docker compose ps
```

Xem log:

```powershell
docker logs vshield-api --tail 100
docker logs vshield-frontend --tail 100
docker logs vshield-go2rtc --tail 100
docker logs vshield-qr-runtime --tail 100
docker logs vshield-qr-runtime-lane2 --tail 100
docker logs vshield-plate-runtime --tail 100
docker logs vshield-face-runtime --tail 100
```

Neu Docker Desktop co hien tuong "luc duoc luc khong":

```powershell
wsl --shutdown
```

Sau do mo lai Docker Desktop roi chay:

```powershell
docker version
```

Chi khi `docker version` hien ca `Client` va `Server` thi moi nen build lai.

## 3. Docker VPS

Mode nay dung file [`docker-compose.vps.yml`](C:/DoAnTotNghiep/V-Shield-2.0/docker-compose.vps.yml).

### 3.1. Dung cho truong hop nao

Nen dung khi:

- Ban deploy len VPS Ubuntu hoac may chu public
- Ban can frontend + api + sql server
- Ban muon tach bien moi truong san xuat ro rang

Mode nay da co:

- volume giu `Data Protection` cho MFA
- bat buoc secret quan trong phai duoc dien
- frontend proxy same-origin cho `/api`

### 3.2. Chuan bi file env

```powershell
Copy-Item .env.vps.example .env.vps
```

Toi thieu can dien trong `.env.vps`:

- `MSSQL_SA_PASSWORD`
- `VSHIELD_JWT_SECRET`
- `VSHIELD_SEED_ADMIN_USERNAME`
- `VSHIELD_SEED_ADMIN_PASSWORD`
- `VSHIELD_EVIDENCE_EXPORT_SIGNING_KEY`
- `APP_FRONTEND_URL`
- `APP_PUBLIC_HOSTNAME`

Neu deploy that, can kiem tra ky them:

- `JWT_ISSUER`
- `JWT_AUDIENCE`
- `SECURITY_ENABLE_HTTPS_REDIRECTION`
- `SECURITY_GATEWAY_HEADERS_MANAGED_BY_PROXY`

### 3.3. Chay len VPS

```powershell
docker compose --env-file .env.vps -f docker-compose.vps.yml up -d --build
```

Kiem tra:

```powershell
docker compose --env-file .env.vps -f docker-compose.vps.yml ps
```

### 3.4. Cap nhat sau nay

```powershell
docker compose --env-file .env.vps -f docker-compose.vps.yml up -d --build
```

### 3.5. Luu y quan trong cho VPS

- Khong de secret mac dinh khi deploy that
- Neu dung demo data tren Production, phai tu y bat `DEMO_DATA_ALLOW_IN_PRODUCTION=true`
- MFA duoc luu trong DB, secret duoc ma hoa, va key ma hoa duoc giu trong volume `vshield_data_protection`

### 3.6. Dong bo nhieu local khu vuc ve 1 VPS trung tam

Mo hinh nay dung khi:

- Moi ban `Docker local` la 1 diem van hanh trong cong ty
- Diem van hanh do co the la:
  - 1 `node khu vuc`, chi nhan du lieu trong pham vi duoc cap
  - hoac 1 `node full-data`, nhan toan bo du lieu tu VPS
- Khu vuc thuong gan cap `gate`, `lane`, hoac `zone`
- VPS la noi tong hop va la nguon chuan cuoi cung

Cach bat tren VPS:

- Trong `.env.vps`, dat `SYNC_MODE=Central`
- Dien `SYNC_REGISTRATION_KEY` bang 1 khoa manh va giu kin
- Chay lai stack VPS bang file `docker-compose.vps.yml`

Cach bat tren moi local:

- Trong `.env`, dat `SYNC_MODE=AreaNode`
- Dien `SYNC_CENTRAL_BASE_URL` thanh URL API cua VPS, vi du `https://vshield.company.vn`
- Dien `SYNC_REGISTRATION_KEY` giong khoa dang ky tren VPS
- Dien `SYNC_LOCAL_AREA_NODE_ID` duy nhat cho khu vuc, vi du `hn-gate-a-lane-1`
- Dien `SYNC_COMPANY_ID`, `SYNC_SITE_ID`
- Dien `SYNC_ASSIGNED_GATE_IDS`, `SYNC_ASSIGNED_LANE_IDS`, `SYNC_ASSIGNED_ZONE_IDS` neu local chi phu trach 1 khu vuc cu the
- Dat `SYNC_DISPLAY_NAME` de de quan tri, vi du `HN Gate A Lane 1`

Neu 1 local can lam diem van hanh tong hop va phai thay TOAN BO du lieu dong bo tu VPS:

- van de `SYNC_MODE=AreaNode`
- nhung de trong:
  - `SYNC_ASSIGNED_GATE_IDS`
  - `SYNC_ASSIGNED_LANE_IDS`
  - `SYNC_ASSIGNED_ZONE_IDS`

Khi ba truong scope nay de trong, local se duoc xem nhu node `full data` va se keo toan bo du lieu nghiep vu/master data tu VPS thay vi bi gioi han theo khu.

Tom tat de de nho:

- Co scope `Gate/Lane/Zone` => local chi nhan phan du lieu duoc cap
- De trong toan bo scope => local nhan full data tu VPS

Nguyen tac van hanh:

- Local tu day event hien truong len VPS theo lo
- Local keo master data va ban chuan hoa tu VPS moi vai giay
- Mat mang thi local van ghi DB local, co mang lai se tu day bu
- Giai doan hien tai chi dong bo nghiep vu cot loi va metadata, chua day file media lon
- VPS trung tam co co che quet downstream theo nhieu nhom event lien tiep, tranh viec 1 local bi mac ket phia sau event cua khu vuc khac khi so node tang len

Khi local moi vua duoc cap:

1. Chay local voi day du thong tin `SYNC_*`
2. Local tu dang ky node voi VPS va nhan secret noi bo
3. Local bootstrap master data trong pham vi duoc cap
4. Sau do local chuyen sang push/pull delta gan realtime

Neu muon mot local chay doc lap, chi can tra `SYNC_MODE=Standalone`

### 3.7. Trien khai thu nhanh: 1 VPS + 1 local khu vuc

Day la luong nen dung de test lan dau truoc khi nhan ban cho nhieu khu vuc.

Buoc 1. Dung VPS trung tam:

- Copy `.env.vps.example` thanh `.env.vps`
- Dien toi thieu:
  - `MSSQL_SA_PASSWORD`
  - `VSHIELD_JWT_SECRET`
  - `VSHIELD_SEED_ADMIN_USERNAME`
  - `VSHIELD_SEED_ADMIN_PASSWORD`
  - `VSHIELD_EVIDENCE_EXPORT_SIGNING_KEY`
  - `APP_FRONTEND_URL`
  - `APP_PUBLIC_HOSTNAME`
- `SYNC_MODE=Central`
- `SYNC_REGISTRATION_KEY=<mot khoa manh dung chung cho cac local>`
- `SYNC_DOWNSTREAM_SCAN_MULTIPLIER=20` neu du kien co nhieu local khu vuc cung dong bo
- Chay:

```powershell
docker compose --env-file .env.vps -f docker-compose.vps.yml up -d --build
```

Buoc 2. Dung 1 local khu vuc:

- Copy `.env.docker.example` thanh `.env`
- Sua toi thieu:
  - `SYNC_MODE=AreaNode`
  - `SYNC_CENTRAL_BASE_URL=https://<ten-mien-hoac-ip-vps>`
  - `SYNC_REGISTRATION_KEY=<giong VPS>`
  - `SYNC_LOCAL_AREA_NODE_ID=<ma duy nhat cua khu vuc>`
  - `SYNC_COMPANY_ID`
  - `SYNC_SITE_ID`
  - `SYNC_DISPLAY_NAME`
  - `SYNC_ASSIGNED_GATE_IDS`
  - `SYNC_ASSIGNED_LANE_IDS`
  - `SYNC_ASSIGNED_ZONE_IDS` neu co
- Chay:

```powershell
docker compose --profile ai --profile ai-heavy up -d --build
```

Buoc 3. Dau hieu da len dung:

- VPS mo duoc:
  - frontend: `http://<host-vps>:<port>`
  - API health: `http://<host-vps>:5107/health` neu mo truc tiep API
- Local mo duoc:
  - frontend: [http://localhost:5173](http://localhost:5173)
  - API health: [http://localhost:5107/health](http://localhost:5107/health)
- Trong DB local se co:
  - `sync.bootstrap.completed=true`
  - `sync.node.secret` da duoc cap
  - `sync.last-pulled-sequence > 0`
- Tren VPS se co `SyncAreaNode` voi `AreaNodeId` da dang ky

Buoc 4. Test hai chieu:

- Sua 1 gate hoac master data tren VPS, local phai nhan trong vai giay
- Tao 1 manual access hoac scan event tai local, VPS phai thay log trong vai giay
- Gui 1 tin nhan chat moi tai local, VPS phai thay trong vai giay

Neu local khong dong bo sau khi VPS bi reset:

- Ban chi can de local tiep tuc chay
- Ban moi da tu phuc hoi secret node cu, dang ky lai va bootstrap lai tu dong
- Khong can xoa DB local chi vi VPS vua duoc dung lai

### 3.8. Mau quy uoc dat AreaNode

Nen dat `SYNC_LOCAL_AREA_NODE_ID` theo mau on dinh:

- `hn-gate-a-lane-1`
- `hn-gate-a-lane-2`
- `bn-truck-gate-1`
- `hp-warehouse-zone-b`

Nen giu quy tac:

- duy nhat toan he thong
- phan anh ro khu vuc van hanh
- khong doi ten tuy tien sau khi da dua vao van hanh

## 4. Windows non-Docker

Chi dung muc nay neu ban chu dong muon chay local khong qua Docker.

### 4.1. Cai dependency

```powershell
.\manage.ps1 -Action install
```

### 4.2. Chay app

```powershell
.\manage.ps1 -Action start
```

Mac dinh:

- Frontend: [http://127.0.0.1:5174](http://127.0.0.1:5174)
- API: [http://127.0.0.1:5108/health](http://127.0.0.1:5108/health)

Neu cong `5174` dang bi chiem, script se thu fallback sang `5175` hoac `5176`.

### 4.3. Dung app

```powershell
.\manage.ps1 -Action stop
```

### 4.4. Xem trang thai

```powershell
.\manage.ps1 -Action status
```

### 4.5. Don moi truong local

```powershell
.\manage.ps1 -Action uninstall
```

Luu y:

- script se khong xoa DB Docker
- script nay chu yeu don dependency local va `.runtime`

## 5. Cloudflare Tunnel

Repo co san luong cau hinh Cloudflare Tunnel, nhung day khong phai buoc bat buoc de chay local.

Neu can:

- Docker tunnel: xem `scripts/setup-docker-cloudflare-tunnel.ps1`
- Windows non-Docker public domain: xem `setup-public-domain.bat`

Tai lieu bo sung:

- `docs/DOCKER_RUN_GUIDE.md`
- `docs/DOCKER_REGRESSION_CHECKLIST.md`
- `docs/DOCKER_UI_REGRESSION.md`
- `docs/WEB_DEPLOYMENT_MODES.md`

## 6. Tai khoan mau

Khi demo data dang bat, he thong tu seed mot nhom tai khoan mau theo vai tro.

### 6.1. Tai khoan nen thu truoc

Docker local:

- `admin` / `AdminLocal@2026`

Windows non-Docker:

- `admin` / `Admin@123`

Tai khoan demo theo vai tro do backend tao:

- `manager` / `Manager@123`
- `quanly2` / `Manager@123`
- `baove1` / `BaoVe@123`
- `baove2` / `BaoVe@123`
- `letan1` / `LeTan@123`
- `nhansu1` / `HR@123`
- `nhanvien1` / `Staff@123`

Luu y:

- So luong tai khoan `baove*`, `quanly*`, `letan*`, `nhansu*`, `nhanvien*` co the nhieu hon tuy theo bo employee demo duoc seed
- `manager` la tai khoan QuanLy dau tien, cac tai khoan tiep theo se la `quanly2`, `quanly3`...
- `admin` la tai khoan seed rieng cua he thong, khong phai luc nao cung giong mat khau demo cua mode khac

### 6.2. Tai khoan test hien tai cho Docker local

- App local: [http://localhost:5173](http://localhost:5173)
- API local: [http://localhost:5107](http://localhost:5107)
- Tai khoan test: `admin`
- Mat khau test: `AdminLocal@2026`

### 6.3. Neu dang dung MFA

Luu y:

- MFA secret duoc luu trong DB o dang ma hoa
- Khoa giai ma MFA khong duoc phep mat sau moi lan recreate container
- Docker local va VPS trong repo hien da duoc cau hinh de giu khoa nay bang volume rieng

Neu ban da tung recreate API truoc khi co ban fix nay, mot so tai khoan co the se phai setup MFA lai 1 lan cuoi.

## 7. Nap lai demo data

### 7.1. Demo data co duoc bat san khong

Docker local:

- duoc bat san khi chay stack local

Docker VPS:

- phu thuoc bien `.env.vps`
- thuong can:
  - `DEMO_DATA_ENABLED=true`
  - neu dang chay Production va van muon seed demo thi can them `DEMO_DATA_ALLOW_IN_PRODUCTION=true`

### 7.2. Seed demo data khi khoi dong

Demo data duoc seed tu dong khi app khoi dong neu:

- `DemoData:Enabled = true`
- va neu la Production thi `DemoData:AllowInProduction = true`

Neu DB chua co du lieu demo, app se nap bo du lieu mau trong luc startup.

### 7.3. Nap lai kich ban demo van hanh

Repo hien co endpoint reset demo:

- `POST /api/demo-control/reset`

Dieu kien de dung:

- phai dang nhap bang tai khoan `Admin`
- app phai dang chay trong moi truong development demo
- `DemoData:Enabled = true`

Muc dich cua endpoint nay:

- reset lai cac kich ban demo van hanh
- nap lai alarm, intervention, emergency pass va cac tinh huong demo lien quan

### 7.4. Cach reset nhanh tren Docker local

Cach de nhat la dung giao dien admin neu man hinh co nut reset demo. Neu muon goi API truc tiep, co the:

1. Dang nhap bang `admin`
2. Lay token tu phien dang nhap
3. Goi:

```powershell
curl -X POST http://localhost:5107/api/demo-control/reset ^
  -H "Authorization: Bearer <TOKEN>"
```

Neu ban chi muon seed lai tu dau toan bo stack local, cach manh tay hon la:

```powershell
docker compose down -v
docker compose up -d --build
```

Cach nay se xoa DB volume local va cho app seed lai tu dau.

### 7.5. Cach reset demo tren VPS

Neu VPS duoc cau hinh cho phep demo data:

```powershell
docker compose --env-file .env.vps -f docker-compose.vps.yml up -d --build
```

Neu can seed lai tu dau tren VPS:

- can rat than trong
- chi nen lam tren moi truong demo
- neu xoa volume DB thi du lieu hien tai se mat

## 8. Lenh nhanh

### Docker local

```powershell
Copy-Item .env.docker.example .env
docker compose --profile ai --profile ai-heavy up -d --build
```

### Docker local chi len web/API co ban

```powershell
Copy-Item .env.docker.example .env
docker compose up -d --build
```

### Docker VPS

```powershell
Copy-Item .env.vps.example .env.vps
docker compose --env-file .env.vps -f docker-compose.vps.yml up -d --build
```

### Windows non-Docker

```powershell
.\manage.ps1 -Action install
.\manage.ps1 -Action start
```
