# Ke Hoach Demo Control Center Tach Roi Cho V-Shield 2.0

Muc tieu: xay dung mot bang dieu khien demo/tinh huong hoan toan tach roi khoi ung dung chinh, co the nap du lieu mau 3 thang cua mot cong ty cap trung/lon, dieu khien app chinh va database mot cach hop phap, mo phong tu dong cac luong nghiep vu ma ma nguon hien tai co the chong chiu duoc, phuc vu bao ve do an tot nghiep.

Trang thai bat buoc: **khong sua ma nguon hien tai cua app chinh nua**. Agent thuc hien chi duoc tao cong cu/tai lieu/script tach roi, khong sua `API/**`, `View/**`, `AI_Runtime/**`, `runtime/**` hoac public-domain scripts hien co.

## 1. Nguyen Tac Bat Buoc

### 1.1 Khong cham app chinh

Khong duoc sua cac vung:

- `API/**`
- `View/**`
- `AI_Runtime/**`
- `runtime/**`
- `scripts/setup-public-domain.ps1`
- `scripts/uninstall-public-domain.ps1`
- `scripts/reset-public-domain-appsettings.ps1`
- `scripts/read-public-domain-appsettings.ps1`
- `scripts/update-public-domain-appsettings.ps1`
- `setup-public-domain.bat`
- `uninstall-public-domain.bat`

Duoc phep tao moi ngoai app chinh:

- `tools/DemoControlCenter/**`
- `demo-data/**`
- `docs/demo-control-center-*.md`
- `scripts/demo-control-center/**` neu can, nhung khong dung ten/de len script public-domain hien co.

### 1.2 Can thiep hop phap

Bang dieu khien co quyen cao, nhung khong duoc pha co che bao mat cua he thong.

Bat buoc:

- Dang nhap app chinh bang tai khoan hop le: `Admin`, `BaoVe`, `Manager`, `Host`.
- Goi API chinh truoc neu API da co.
- Chi ghi DB truc tiep trong cac vung duoc whitelist va cho seed/simulation.
- Moi thao tac DB truc tiep phai co audit rieng trong Demo Control Center.
- Khong sua token/MFA secret/refresh token/secret/config production bang DB truc tiep.
- Khong tat auth, khong bypass middleware, khong sua role policy.
- Khong can thiep vao `AI_Runtime` hoac `runtime`; neu can mo phong camera/plate/QR thi day event vao API/DB nhu event dau vao hop le.

### 1.3 Chi chay local/demo

Cong cu chi duoc phep chay khi:

- May local/dev.
- URL API la `http://127.0.0.1:*` hoac `http://localhost:*`.
- Database name co hau to/hau to ro rang: `VShieldDemo`, `VShield_Dev`, hoac duoc user xac nhan bang chuoi confirmation.
- Bien moi truong `VSHIELD_DEMO_CONTROL_ENABLED=true`.

Neu phat hien production-like config thi tu dong khoa tat ca nut destructive.

## 2. Kien Truc De Xuat

### 2.1 Thanh phan

Tao cong cu tach roi tai:

```text
tools/DemoControlCenter/
  DemoControlCenter.Api/
  DemoControlCenter.Web/
  DemoControlCenter.Core/
  DemoControlCenter.Data/
  DemoControlCenter.Tests/
  README.md
demo-data/
  company-profiles/
  scenarios/
  seed-manifests/
  snapshots/
```

Khuyen nghi stack:

- Backend control: .NET 8 Minimal API hoac Worker + API.
- DB access: Dapper hoac ADO.NET de tranh phu thuoc EF cua app chinh.
- Frontend control: Vue/Vite rieng hoac Razor Pages don gian.
- Browser automation tuy chon: Playwright, chi de click/view demo UI app chinh.
- Scenario format: JSON/YAML de agent co the sua kich ban khong can build lai.

Ly do chon .NET: cung runtime voi app chinh, de doc connection string SQL Server, de goi API, de viet transaction/backup/restore an toan.

### 2.2 Luong dieu khien

```text
Demo Control Center UI
        |
        |-- Demo Orchestrator
        |      |-- API Client: login, call business endpoints
        |      |-- DB Seed Engine: seed/reset whitelist tables
        |      |-- Scenario Clock: virtual time, speed, pause/resume
        |      |-- Event Injector: QR/plate/access/device/SOC/evidence events
        |      |-- Browser Driver: optional, opens app and navigates views
        |
        |-- App chinh API
        |-- App chinh Web UI
        |-- SQL Server demo database
```

### 2.3 API-first, DB-second

Thu tu thao tac:

1. Goi API chinh neu API co san.
2. Neu API bi chan vi thieu business state, tao business state bang DB seed theo manifest.
3. Neu mo phong scan/event can toc do cao, ghi vao bang log/event duoc whitelist bang transaction.
4. Sau moi batch, goi API/readiness hoac query de xac minh app nhan duoc state.

## 3. Cau Hinh An Toan

File `.env.local` cua tool, khong commit secret:

```text
VSHIELD_DEMO_CONTROL_ENABLED=true
VSHIELD_API_BASE=http://127.0.0.1:5107
VSHIELD_WEB_BASE=http://127.0.0.1:5173
VSHIELD_DB_CONNECTION=Server=.;Database=VShieldDemo;Trusted_Connection=True;TrustServerCertificate=True
VSHIELD_DEMO_ADMIN_USERNAME=admin
VSHIELD_DEMO_ADMIN_PASSWORD=...
VSHIELD_DEMO_GUARD_USERNAME=guard.demo
VSHIELD_DEMO_GUARD_PASSWORD=...
VSHIELD_DEMO_REQUIRE_CONFIRMATION=true
```

DB safety guard:

- Khong cho chay neu database name chua `prod`, `production`, `real`, `master`.
- Yeu cau user nhap `RESET VSHIELD DEMO` truoc khi reset.
- Tao backup/snapshot truoc khi destructive reset.
- Moi run co `RunId`, `CorrelationPrefix`, `StartedBy`, `StartedAt`.

## 4. Du Lieu Mau 3 Thang

### 4.1 Ho so cong ty demo

Ten cong ty mau: **Cong ty Co phan Cong nghe va San xuat Vinh An**

Mo hinh: cong ty cap trung/lon, 4 dia diem, 24/7, co van phong, nha may, kho, R&D, trung tam du lieu nho, nha khach, bai xe, cong bao ve.

Quy mo seed mac dinh:

| Nhom | So luong |
|---|---:|
| Sites | 4 |
| Buildings | 11 |
| Floors/Zones | 58 |
| Access points/gates/doors/lanes | 42 |
| Cameras | 96 |
| Security devices/controllers/sensors | 140 |
| Employees | 850 |
| Departments | 32 |
| Shifts | 9 |
| App users | 90 |
| Vehicles | 620 |
| Visitors trong 90 ngay | 3,200 visits |
| Contractors | 180 |
| Access logs | 180,000 - 260,000 |
| QR scan events | 12,000 - 18,000 |
| Plate/lane events | 45,000 - 70,000 |
| SOC alarms | 1,000 - 1,600 |
| Incidents/cases | 120 - 180 |
| Evidence items | 550 - 900 |
| Device health snapshots | 70,000 - 100,000 |
| UEBA/attendance anomalies | 700 - 1,200 |
| AI recommendations | 400 - 900 |

Agent co the tao 2 profile:

- `medium-850`: 850 nhan su, dung cho demo may ca nhan.
- `large-2500`: 2,500 nhan su, chi dung neu may/DB chiu duoc.

### 4.2 Cau truc sites

Site A - Tru so Ha Noi:

- 350 nhan su.
- Khu van phong, SOC room, server room, R&D lab, executive floor.
- Cong A1 nhan su, A2 khach, A3 bai xe.

Site B - Nha may Bac Ninh:

- 320 nhan su.
- Xuong san xuat, kho vat tu, phong QA, khu doc hai/nhay cam.
- Cong B1 xe may/nhan su, B2 container, B3 nha thau.

Site C - Kho Long Bien:

- 120 nhan su.
- Kho thanh pham, khu xuat hang, khu loading dock, bai xe tai.
- Nhieu lane event/plate event.

Site D - Van phong HCM:

- 60 nhan su.
- Van phong kinh doanh, phong hop khach, khu tiep tan.

### 4.3 Nhan su mau

Can tao du lieu that nhat co the:

- Ten Viet Nam co dau/khong dau neu app ho tro.
- Ma NV: `VA0001` - `VA0850`.
- Email: `firstname.lastname@vinhan.example`.
- Phone theo format Viet Nam.
- Department: Bao ve, IT, HR, Finance, Operations, Production, Warehouse, QA, R&D, Sales, Legal, Facilities.
- Role van hanh:
  - `Admin`: 3 nguoi.
  - `BaoVe`: 24 nguoi.
  - `Manager`: 45 nguoi.
  - `Employee/Staff`: con lai.
  - `Host`: nhan su co quyen moi khach.
- Lich lam:
  - Office: 08:00-17:30.
  - Factory morning: 06:00-14:00.
  - Factory afternoon: 14:00-22:00.
  - Factory night: 22:00-06:00.
  - Security rotating shifts.

Phai co nhan su "cau chuyen demo":

- `Nguyen Minh Quan` - IT Admin, can step-up khi thao tac release/evidence.
- `Tran Thi Lan` - HR Manager, host visitor.
- `Le Van Bao` - BaoVe ca sang, xu ly alarm.
- `Pham Duc Khoa` - Warehouse employee, sinh UEBA risk vi vao kho ngoai gio.
- `Doan Nhat Nam` - Contractor, access tam thoi.
- `Hoang Minh Tu` - nhan su bi offboard trong thang thu 2.

### 4.4 Vehicle/plate mau

Bien so dung format Viet Nam thuc te:

- O to: `30F-123.45`, `29A-886.12`, `51G-672.90`, `99C-234.56`
- Xe may: `29B1-456.78`, `30K1-112.23`
- Xe tai/container: `29H-789.01`, `15C-908.77`

Phai seed cac case:

- Xe nhan vien hop le.
- Xe khach co permit trong ngay.
- Xe nha thau het han permit.
- Bien so OCR low confidence: `30F-123.45` bi doc thanh `30F-I23.4S`.
- Duplicate/similar plate: `29A-886.12` va `29A-886.72`.
- Watchlist vehicle.
- Lane event vao/ra khong khop.

### 4.5 Visitor/contractor mau

Moi visit co:

- Ho ten, cong ty, phone, email.
- Host employee.
- Purpose: phong van, bao tri, giao hang, hop voi Sales, audit QA, tham quan nha may.
- NDA/safety form status.
- QR credential issue/expire.
- Check-in/check-out.
- Escort required.

Tinh huong can co:

- Khach pre-register hop le.
- Khach den som/den muon.
- Khach overstay.
- Khach watchlist/manual review.
- Khach thieu NDA/safety form.
- Contractor vao khu cam khi chua co escort.

### 4.6 90 ngay su kien van hanh

Phan bo thoi gian:

- Ngay thuong: peak 07:00-09:00, 11:30-13:30, 17:00-19:00.
- Cuoi tuan: it traffic, chi co bao ve/nha may/kho.
- Dem: chi factory night shift, security, IT maintenance.
- Co it nhat 3 ngay cao diem: audit, event cong ty, giao hang lon.
- Co 2 ngay incident nang: device outage va visitor/security breach simulation.

Ty le du lieu:

- 92-95% su kien binh thuong.
- 3-5% warning.
- 1-2% high severity.
- 0.1-0.3% critical.

## 5. Cac Nut Dieu Khien Can Co

### 5.1 System controls

- `Connect`: kiem tra API, Web, DB.
- `Login service accounts`: dang nhap Admin/BaoVe/Host bang API.
- `Health check`: goi `/health`, `/health/ready`, runtime health neu co.
- `Open App`: mo app chinh trong browser.
- `Open Dashboard`, `Open Enterprise Security`, `Open UEBA`, `Open Audit Logs`.
- `Backup Demo DB`.
- `Restore Last Snapshot`.
- `Reset Demo Data`.
- `Seed 3-Month Medium Company`.
- `Seed 3-Month Large Company`.
- `Dry Run`: hien se lam gi, khong ghi DB/API.

### 5.2 Simulation clock

- `Set Simulated Now`.
- `Pause`.
- `Resume`.
- `Speed 1x`, `10x`, `60x`, `300x`.
- `Jump to Morning Rush`.
- `Jump to Night Shift`.
- `Jump to Incident Day`.
- `Replay Last Scenario`.

Moi su kien duoc tao phai co:

- `OccurredAtUtc`.
- `SimulatedLocalTime`.
- `ScenarioRunId`.
- `CorrelationId`.
- `Source = DemoControlCenter`.

### 5.3 QR controls

- `Generate Valid Employee QR`.
- `Generate Valid Visitor QR`.
- `Inject QR Scan Success`.
- `Inject QR Expired`.
- `Inject QR Replay Attack`.
- `Inject QR Wrong Gate`.
- `Inject QR Outside Schedule`.
- `Open QR Monitor View`.

Cach can thiep:

- Neu app co API verify QR: goi API verify/scan.
- Neu API yeu cau camera/scanner runtime: ghi event dau vao hop le vao bang scan/log tuong ung theo whitelist.
- Moi QR replay/expired phai khop thoi gian gia lap, khong chi set text ket qua.

### 5.4 License plate controls

- `Inject Plate Recognized`.
- `Inject Plate Low Confidence`.
- `Inject Similar Plate Manual Review`.
- `Inject Unknown Plate`.
- `Inject Watchlist Plate`.
- `Inject Entry Without Exit`.
- `Inject Exit Without Entry`.
- `Open License Plate View`.

Cach can thiep:

- Uu tien goi plate/lane API neu co.
- Neu khong co endpoint day event, insert lane/access/plate log theo whitelist bang transaction.
- Anh/preview neu can thi dung metadata/file mau trong `demo-data/assets`, khong sua runtime.

### 5.5 Access/person controls

- `Employee Normal Entry`.
- `Employee Denied By Policy`.
- `Employee Out-of-Hours Entry`.
- `Employee Unusual Gate`.
- `Employee Bypass`.
- `Employee Tailgating Metadata`.
- `Build UEBA Risk`.
- `Open UEBA Risk Explanation`.

### 5.6 Visitor controls

- `Create Pre-Registered Visitor`.
- `Host Approves Visitor`.
- `Visitor Check-In`.
- `Visitor Missing NDA`.
- `Visitor Escort Required`.
- `Visitor Overstay`.
- `Visitor Check-Out`.
- `Visitor Watchlist Review`.

### 5.7 SOC/evidence controls

- `Create Low Alarm`.
- `Create Critical Alarm`.
- `Acknowledge Alarm`.
- `Assign Alarm`.
- `Run AI Incident Briefing`.
- `Create Incident Timeline`.
- `Attach Evidence`.
- `Run Evidence Assistant`.
- `Request Evidence Export`.
- `Approve Export With Step-Up`.
- `Close Incident With SOP`.

### 5.8 Device/runtime controls

- `Device Online`.
- `Device Degraded`.
- `Device Offline`.
- `Camera Stale`.
- `Barrier Command Success`.
- `Barrier Command Failure`.
- `Runtime Health Degraded`.
- `Run Device AI Diagnosis`.

### 5.9 Policy/release controls

- `Create Draft Policy`.
- `Simulate Policy`.
- `Explain Policy`.
- `Submit Approval`.
- `Approve With Step-Up`.
- `Activate Policy`.
- `Rollback Policy`.
- `Release Gate Pass`.
- `Release Gate Fail`.

### 5.10 Emergency controls

- `Start Lockdown Drill`.
- `Start Evacuation Drill`.
- `Create Muster Snapshot`.
- `Fire Override Event`.
- `Manual Override Event`.
- `End Emergency Mode`.

Tat ca emergency controls chi duoc mo phong neu app chinh da co endpoint/state tuong ung. Neu chua co, chi tao SOC/evidence/timeline demo, khong gia lap la da dieu khien phan cung that.

## 6. Thu Vien Kich Ban Tu Hanh

Tao folder:

```text
demo-data/scenarios/
  00-reset-and-seed.json
  01-normal-business-day.json
  02-morning-rush-qr-plate.json
  03-visitor-full-lifecycle.json
  04-vehicle-low-confidence-review.json
  05-ueba-insider-risk.json
  06-device-stale-camera.json
  07-soc-critical-incident-evidence.json
  08-policy-simulation-approval.json
  09-emergency-lockdown-drill.json
  10-graduation-defense-auto-demo.json
```

Moi scenario gom:

```json
{
  "id": "03-visitor-full-lifecycle",
  "name": "Visitor full lifecycle",
  "durationMinutes": 12,
  "preconditions": ["seed-medium-company", "api-ready", "admin-login"],
  "steps": [
    {
      "at": "09:00:00",
      "action": "createVisitor",
      "mode": "api-first",
      "payloadRef": "visitors/audit-guest-001.json"
    },
    {
      "at": "09:05:00",
      "action": "hostApproveVisitor"
    },
    {
      "at": "09:20:00",
      "action": "injectQrScan",
      "expected": "success"
    }
  ],
  "expectedOutcomes": [
    "visitor checked in",
    "access log created",
    "audit trail created",
    "UI visitor detail shows active pass"
  ],
  "rollback": "delete-run-data"
}
```

## 7. Mapping Voi App Chinh

Agent phai tao mot file mapping:

```text
demo-data/seed-manifests/vshield-table-map.json
```

Noi dung:

- Ten bang app chinh.
- Khoa chinh.
- Cot timestamp.
- Cot status.
- Cot correlation/source neu co.
- Cho phep seed/reset hay khong.
- Co API thay the hay khong.

Quy tac:

- Bang auth/security nhay cam: API-only neu co the.
- Bang event/log/demo: co the DB-direct neu whitelist.
- Bang audit/evidence hash: khong duoc fake hash sai; neu seed thi tinh hash hop le.
- Bang migration/system config: cam ghi.

## 8. Du Lieu Mau Can That Nhu The Nao

### 8.1 Quy tac tao du lieu

- Ten nguoi Viet Nam co tinh da dang vung mien.
- Department/position khop nhau.
- Shift khop site/department.
- Access logs khop policy, gate, schedule.
- Visitor khop host/site/purpose.
- Vehicle khop employee/visitor/permit.
- Plate event co confidence, raw OCR, normalized plate.
- Device health co pattern: online -> degraded -> stale -> offline -> recovered.
- Incident phai co alarm, timeline, evidence, SOP, audit.
- AI recommendation phai co source evidence va confidence.

### 8.2 Cac bat thuong bat buoc

- MFA/step-up required action.
- Login failure burst.
- Expired QR.
- QR replay.
- Wrong gate access.
- Out-of-hours access.
- Weekend access.
- Bypass/tailgate.
- Unknown plate.
- Low confidence plate.
- Duplicate/similar plate.
- Visitor overstay.
- Missing NDA/safety form.
- Watchlist visitor/vehicle.
- Device stale/offline.
- Barrier command failure.
- Evidence hash mismatch simulation chi trong demo-safe record.
- Legal hold blocks purge/export warning.
- Policy conflict/affected users.
- Alarm SLA overdue.
- Incident cannot close before SOP complete.

## 9. UI Bang Dieu Khien

Trang chinh can co:

1. **Connections**
   - API status, Web status, DB status.
   - Current environment guard.
   - Logged-in service accounts.

2. **Data Packs**
   - Medium company 3 months.
   - Large company 3 months.
   - Reset/backup/restore.
   - Seed progress.

3. **Scenario Runner**
   - List scenario.
   - Start/pause/resume/stop.
   - Timeline progress.
   - Speed control.
   - Expected outcomes checklist.

4. **Live Event Console**
   - Event stream.
   - API calls.
   - DB writes.
   - Errors/retries.
   - Correlation IDs.

5. **Business Controls**
   - QR.
   - Plate.
   - Access.
   - Visitor.
   - SOC.
   - Evidence.
   - Device.
   - Policy.
   - Emergency.

6. **App View Automation**
   - Open app.
   - Login as Admin/BaoVe/Host.
   - Navigate to page.
   - Refresh view.
   - Optional screenshot capture.

7. **Audit & Rollback**
   - Scenario run history.
   - Data created by run.
   - Rollback selected run.
   - Export run report.

## 10. Co Che Rollback/Ngat De Dang

Moi du lieu Demo Control Center tao ra phai duoc gan:

- `ScenarioRunId` neu bang co cot mo rong hoac bang demo mapping.
- `CorrelationId` co prefix `DCC-`.
- `CreatedBy`/`Source` neu bang co.
- Neu bang khong co cot gan nhan, luu primary keys vao `demo.DemoSeedManifest`.

Tao schema rieng trong DB:

```sql
demo.DemoScenarioRun
demo.DemoSeedManifest
demo.DemoControlAudit
demo.DemoEventQueue
demo.DemoSnapshot
```

Day la schema cua tool, khong can EF app chinh biet. Neu khong duoc tao schema trong DB app, tool luu manifest o SQLite local, nhung van phai biet primary keys da tao de rollback.

Rollback modes:

- `Rollback last scenario`: xoa/undo du lieu do scenario moi nhat tao.
- `Rollback by run id`.
- `Reset to clean seed`.
- `Restore DB backup`.

Khong rollback bang cach xoa toan bo database neu chua co confirmation.

## 11. App View Tu Hanh

Muc tieu: demo co the chay nhu phim co nguoi dieu khien, nhung van dung app chinh.

Luon co 2 che do:

- `Manual assist`: bang dieu khien tao data, user tu bam app chinh.
- `Guided auto demo`: tool mo browser, login, chuyen trang, kich hoat scenario, refresh view, chup screenshot.

Playwright flow mau:

1. Mo `VSHIELD_WEB_BASE`.
2. Login Admin.
3. Vao dashboard.
4. Chay scenario morning rush.
5. Vao Enterprise Security.
6. Chay SOC incident briefing.
7. Vao UEBA.
8. Chay risk explanation.
9. Vao Audit Logs.
10. Chup report.

Neu login/MFA can thao tac nguoi dung, tool phai dung va hien prompt "Nhap MFA roi bam Continue", khong bypass MFA.

## 12. Testing Cho Demo Control Center

Bat buoc co tests rieng, khong phu thuoc test app chinh:

- Connection guard tests.
- DB production-name block tests.
- Seed idempotency tests.
- Rollback tests.
- Scenario parser tests.
- API-first fallback tests.
- QR event injection tests.
- Plate event injection tests.
- Virtual clock tests.
- Manifest tracking tests.
- Dry-run no-write tests.

Smoke test:

1. Start app chinh.
2. Start Demo Control Center.
3. Connect API/Web/DB.
4. Backup DB.
5. Seed medium 3 months.
6. Run `10-graduation-defense-auto-demo`.
7. Open app dashboard.
8. Confirm logs/incidents/evidence/UEBA/device insights visible.
9. Rollback scenario.
10. Restore snapshot.

## 13. Nghiem Thu Hoan Thanh

Agent khac chi duoc bao xong khi dat:

- Khong co diff trong `API/**`, `View/**`, `AI_Runtime/**`, `runtime/**`.
- Tool nam rieng trong `tools/DemoControlCenter/**`.
- Co `README.md` cach chay ro rang.
- Co `.env.example`, khong commit secret.
- Co medium dataset 3 thang.
- Co large dataset profile hoac generator scale.
- Co it nhat 10 scenario JSON.
- Co UI nut dieu khien theo cac nhom da liet ke.
- Co backup/restore/reset/rollback.
- Co API-first va DB-direct whitelist.
- Co guard chan production database.
- Co service-account login hop phap, khong bypass auth.
- Co QR injection va plate injection theo thoi gian gia lap.
- Co guided auto demo mo app chinh va dieu huong view.
- Co test rieng cho Demo Control Center.
- Co run report sau moi scenario.

## 14. Thu Tu Trien Khai Cho Agent

1. Tao folder `tools/DemoControlCenter`.
2. Tao backend .NET Minimal API.
3. Tao config/env guard.
4. Tao DB connector + safety checks.
5. Tao API client dang nhap app chinh.
6. Tao schema/SQLite manifest cho demo run.
7. Tao seed generator medium company 3 thang.
8. Tao reset/backup/restore.
9. Tao scenario engine + virtual clock.
10. Tao QR injector.
11. Tao plate/lane injector.
12. Tao access/visitor/device/SOC/evidence/policy injectors.
13. Tao UI dashboard.
14. Tao browser automation optional.
15. Tao 10 scenario JSON.
16. Tao tests.
17. Chay smoke end-to-end.
18. Viet README va runbook bao ve.

## 15. Ranh Gioi Trung Thuc Khi Bao Ve

Duoc noi:

- "Day la bang dieu khien demo tach roi, dung de tao du lieu va kich ban nghiem thu."
- "Cong cu su dung tai khoan hop le va API hop le truoc, chi seed DB trong moi truong demo."
- "Muc tieu la tai hien luong van hanh thuc te trong 3 thang."
- "Khong bypass bao mat cua app chinh."

Khong nen noi:

- "Cong cu co quyen tuyet doi tren production."
- "He thong da nghiem thu phan cung that neu chi moi inject event."
- "AI tu quyet dinh mo cong/khoa cua."
- "Demo data la du lieu that cua cong ty that."

## 16. Kich Ban Bao Ve Tot Nghiep De Xuat

Dung scenario `10-graduation-defense-auto-demo`:

1. Reset ve clean seed.
2. Nap 3 thang du lieu cong ty Vinh An.
3. Mo dashboard: thay tong quan nhan su, traffic, warning.
4. Chay morning rush: QR va plate events vao lien tuc.
5. Tao visitor hop le: host approve, QR check-in.
6. Tao visitor overstay: SOC alarm.
7. Tao employee out-of-hours access: UEBA risk tang.
8. Tao plate low-confidence: manual review.
9. Tao camera stale: device health degraded.
10. Tao critical incident: alarm, SOP, evidence.
11. Chay AI incident briefing va evidence assistant.
12. Chay policy simulation truoc khi activate.
13. Mo audit log: chung minh moi thao tac co ghi vet.
14. Rollback scenario: dua DB ve trang thai demo an toan.

Day la cau chuyen day du: nguoi, khach, xe, QR, bien so, thiet bi, SOC, evidence, policy, AI, audit.

