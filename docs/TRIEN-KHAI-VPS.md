# HƯỚNG DẪN TRIỂN KHAI VPS + MAIL TỰ HOST + AI AGENT

Tài liệu này ghi lại các bước cần thiết để triển khai V-Shield 2.0 lên VPS,
thiết lập mail công ty tự host, và kích hoạt AI Agent (chat + gửi email thay người dùng).

---

## 1. TRƯỚC KHI DEPLOY: CHUẨN BỊ `.env` TRÊN VPS

`.env` **không** được commit lên git (đã gitignore). Trên VPS phải tự tạo/copy.

Các biến bắt buộc/nên có:

```env
# --- Database ---
MSSQL_SA_PASSWORD=<mật khẩu mạnh>

# --- Admin bootstrap ---
VSHIELD_SEED_ADMIN_USERNAME=admin
VSHIELD_SEED_ADMIN_PASSWORD=<mật khẩu admin mạnh>   # Production BẮT BUỘC override

# --- AI Agent (DeepSeek) ---
VSHIELD_AI_PROVIDER=DeepSeek
VSHIELD_AI_ENDPOINT=https://api.deepseek.com/chat/completions
VSHIELD_AI_API_KEY=<key DeepSeek>
VSHIELD_AI_MODEL=deepseek-v4-flash

# --- Mail công ty (tự host hoặc relay) ---
MAIL_DOMAIN=v-shield.site                 # domain email công ty (đặt 1 lần)
MAIL_HOST=vshield-mailu                   # tên service mail (hoặc SMTP relay bên ngoài)
MAIL_PORT=587
MAIL_USER=noreply@v-shield.site           # tài khoản SMTP dùng để GỬI
MAIL_PASSWORD=<pass>
MAIL_FROM=noreply@v-shield.site
MAIL_FROM_NAME=V-Shield
MAIL_ENABLE_SSL=false                     # port 587 = STARTTLS (false); port 465 = true
MAIL_ALLOW_EXTERNAL=true                  # bật nếu được phép gửi ra email ngoài (vd Gmail)
```

> **Quan trọng (deliverability):** để mail tới Gmail/Outlook **vào Inbox** (không vào Spam),
> domain phải có đủ record DNS (mục 2) và IP VPS phải sạch. Nếu chỉ cần GỬI,
> khuyến nghị dùng **SMTP relay** (Amazon SES / Mailgun / Brevo): đặt `MAIL_HOST/PORT/USER/PASSWORD`
> trỏ vào relay — cấu hình 1 lần, không ai phải nhập mail cá nhân.

---

## 2. DNS (chỉ cần khi tự host mail server — Mailu)

| Loại | Tên | Giá trị |
|---|---|---|
| A | `mail.<domain>` | IP VPS |
| MX | `<domain>` | `mail.<domain>` (ưu tiên 10) |
| TXT (SPF) | `<domain>` | `v=spf1 a mx ip4:<IP_VPS> ~all` |
| TXT (DKIM) | `dkim._domainkey.<domain>` | lấy key khi Mailu khởi động (`docker compose -f docker-compose.mail.yml exec mailu cat /data/dkim/...`) |
| TXT (DMARC) | `_dmarc.<domain>` | `v=DMARC1; p=none; rua=mailto:postmaster@<domain>` |
| PTR | reverse IP | `mail.<domain>` (đặt qua nhà cung cấp VPS) |

Firewall VPS phải mở: `25, 587, 465, 993, 995` (mail) + `5107, 5173` (app) + `80/443` (nếu có reverse proxy).

---

## 3. CÁC BƯỚC DEPLOY

```bash
# 1) Clone/pull code
git clone https://github.com/quantrif31-wq/V-Shield-2.0.git
cd V-Shield-2.0

# 2) Tạo .env (xem mục 1)
nano .env

# 3) Build + chạy toàn bộ stack
docker compose up -d --build

# 4) (Tuỳ chọn) Nếu tự host mail server:
docker compose -f docker-compose.mail.yml up -d
#   - vào http://<IP>:8080 đăng nhập admin -> tạo tài khoản gửi (noreply@domain)
#   - lấy DKIM key -> thêm record DNS

# 5) Kiểm tra
curl http://localhost:5107/health/ready
```

---

## 4. CƠ CHẾ TỰ SINH EMAIL CÔNG TY

- Mỗi **nhân viên** và **khách** có trường `CompanyEmail` **tự sinh, không trùng**:
  dạng slug tên bỏ dấu (`pham.van.thanh@v-shield.site`), xung đột thì thêm hậu số.
- Nguồn domain: `MAIL_DOMAIN` (đặt 1 lần khi setup VPS) → **không ai phải nhập/đăng nhập mail cá nhân**.
- **Thứ tự khi nạp data mẫu** (đã đảm bảo đúng):
  `Migrate → SeedAdmin → DemoDataSeeder (180 NV) → Backfill company email`.
  → Trên DB mới, nhân viên/khách seed ra **đều được tự sinh** email công ty.
- Logic: `API/API/API/Services/Agent/CompanyEmailService.cs` (`EnsureBackfillAsync`, chạy tự động lúc khởi động).

---

## 5. AI AGENT (chat + gửi email)

- Endpoint: `POST /api/ai-chat/stream` (SSE: `tool_start`/`tool_done`/`draft`/`token`/`done`).
- Các skill/tool: `get_me, search_people, get_person, get_org_relation, resolve_greeting, draft_email, save_note, get_note`.
- **Phân quyền**: mỗi lời gọi tool kiểm tra quyền của user đang đăng nhập (`OperationalTaskKeys` + scopes).
- **Gửi email**: chỉ qua `POST /api/ai-chat/send-draft` (user bấm Gửi) → `From` = email công ty người gửi,
  người nhận phải là `@domain` hoặc trong danh bạ (ngoài ra cần `MAIL_ALLOW_EXTERNAL=true`).
- **Bộ nhớ**: thread + tóm tắt (compaction) lưu trong SQL Server (`AgentThreads/AgentMessages/AgentDrafts/AgentAuditLogs`).

### Test nhanh
```bash
# login lấy token
TOKEN=$(curl -s -X POST http://<IP>:5107/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"<pass>"}' | jq -r .token)

# agent soạn email
curl -s -N -X POST http://<IP>:5107/api/ai-chat/stream \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"message":"Soạn email xin nghỉ 1 ngày ngày mai vì việc gia đình gửi cho anh Hùng NV0003."}'
```

---

## 6. AN TOÀN

- Không commit `.env` (chứa key AI + mật khẩu SMTP).
- Đổi ngay mật khẩu admin default trên production (`VSHIELD_SEED_ADMIN_PASSWORD`).
- `MAIL_ALLOW_EXTERNAL` chỉ bật khi thực sự cần; mọi lời gọi tool + gửi mail đều ghi `AgentAuditLogs`.
- Hạn chế mở port 25 cho IP không cần thiết.
