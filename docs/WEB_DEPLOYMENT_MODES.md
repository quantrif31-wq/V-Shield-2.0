# Web deployment modes

Du an hien co 2 cach trien khai tach biet:

## 1. Local full stack

Dung khi can day du toan bo he thong nhu moi truong phat trien tai may noi bo:

- Vue frontend
- ASP.NET API
- SQL Server
- go2rtc
- cloudflared profile
- cac runtime AI profile neu can

Lenh chay:

```bash
docker compose up -d --build
```

## 2. VPS web-only

Dung khi dua len Ubuntu VPS va chi can cac thanh phan web:

- Vue frontend
- ASP.NET API
- SQL Server

Khong keo theo camera public, cloudflared, APK hay AI runtime.

Lenh chay:

```bash
cp .env.vps.example .env.vps
docker compose --env-file .env.vps -f docker-compose.vps.yml up -d --build
```

## Luu y quan trong

- Frontend trong `docker-compose.vps.yml` dung same-origin proxy cho `/api` va `/hubs`.
- File `View/nginx/default.conf.template` da co proxy san cho API va SignalR hub.
- Bo `vps web-only` chi mo cong HTTP cua frontend ra ngoai. API va SQL chi giao tiep noi bo giua cac container.
- API production van bat buoc:
  - `VSHIELD_JWT_SECRET`
  - `VSHIELD_SEED_ADMIN_USERNAME`
  - `VSHIELD_SEED_ADMIN_PASSWORD`
  - `VSHIELD_EVIDENCE_EXPORT_SIGNING_KEY`
  - `APP_FRONTEND_URL`
- Bo `vps web-only` da set san:
  - `RateLimiting__Backend=SqlServer`
  - MFA bat buoc cho `Admin` va `BaoVe`
- `SECURITY_ENABLE_HTTPS_REDIRECTION=false` dang de mac dinh de ban co the test nhanh VPS lan dau qua HTTP.
- Sau khi gan domain HTTPS o reverse proxy/edge, nen doi `SECURITY_ENABLE_HTTPS_REDIRECTION=true`.
- Database se duoc migrate tu dong khi API khoi dong.
- Bo `local full` duoc giu nguyen de tranh pha vo luong van hanh hien tai.
