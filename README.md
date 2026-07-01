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
- Ban muon co san SQL Server + API + frontend trong cung mot stack
- Ban co the bat them AI runtime khi can

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

2. Khoi dong stack chinh:

```powershell
docker compose up -d --build
```

Stack chinh gom:

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

QR runtime:

```powershell
docker compose --profile ai up -d --build
```

Plate + Face runtime:

```powershell
docker compose --profile ai-heavy up -d --build
```

Kiem tra nhanh:

```powershell
curl http://localhost:5107/health
curl http://localhost:8001/qr/result
curl http://localhost:5002/api/camera/status
```

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

### 6.2. Neu dang dung MFA

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
