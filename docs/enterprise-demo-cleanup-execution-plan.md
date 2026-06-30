# Ke hoach lam sach UI/UX va hoan thien demo enterprise cho V-Shield

Ngay lap: 2026-06-18

Muc tieu: bien cac nang luc da co cua V-Shield thanh cac luong demo ro rang, chuyen nghiep, dung vai tro, khong con cam giac "co nut nhung goi nua voi", "co API nhung view khong dung", hoac "co view nhung thao tac that khong chay".

Tai lieu nguon:

- `docs/ui-ux-and-feature-cleanup-assessment.md`
- Kiem tra bo sung ve manual pass, emergency pass, override, intervention approval, duress va company-wide alarm
- `docs/backend-view-enterprise-integration-plan.md`
- `docs/demo/README.md`

## 1. Nguyen tac bat buoc

### 1.1. Vung cam

Agent thuc hien khong duoc sua:

- `AI_Runtime/**`
- `runtime/**`
- public-domain scripts va batch trong `docs/no-touch-boundaries.md`
- cac logic nhan dien khuon mat / Face ID, tru khi muc tieu la an hoac gan nhan legacy tren UI

Moi cai tien lien quan camera/runtime/AI chi duoc lam qua API wrapper, health state, timeout, retry, UI state, demo data, audit va notification o lop ung dung.

### 1.2. Dinh huong san pham

- QR dong la luong nhan dang chinh.
- Bien so xe la du lieu phu tro cho cong/bai xe.
- Face ID la legacy/khong demo.
- BaoVe can mot man van hanh chinh, khong bi day qua nhieu trang.
- QuanLy/Admin can man duyet va giam sat uu tien theo muc do nguy cap.
- Moi action rui ro phai co actor, ly do, ket qua, receipt/audit va cach doc y nghia tren UI.

### 1.3. Dinh nghia "demo dat"

Mot tinh nang chi duoc tinh la demo-ready khi dat tat ca dieu kien:

1. Co nut/hien thi o dung man va dung vai tro.
2. Co du lieu mau de thuc hien ma khong can sua DB bang tay.
3. Khi bam nut co goi API that hoac neu offline fallback thi UI phai noi ro.
4. Co ket qua nhin thay ngay tren man hinh.
5. Co record/audit/alarm/event de xem lai.
6. Loi duoc hien thi than thien, khong dung `alert()`/`prompt()`/`confirm()` cho luong enterprise chinh.
7. Co kich ban trong `docs/demo/README.md`.

## 2. Hien trang can xu ly

### 2.1. UI/UX

- Navigation qua day, nhieu module ngang cap.
- Cung mot luong cong/QR/bien so bi tach thanh nhieu man.
- BaoVe, QuanLy, Admin bi tron trong menu.
- Nhieu bang trong suot, nen mo, chu nhat.
- Style table lap lai o tung page.
- Face ID con hien trong login/sidebar/header/chatbot/guide.
- 6 page ton tai nhung chua route: `OperationsDashboard`, `OutboxViewer`, `SIEMExportStatus`, `WebhookDeliveryViewer`, `BackupRestoreDrillDashboard`, `VulnerabilityReleaseGateStatus`.

### 2.2. Luong ngoai le va khan cap

| Luong | Hien trang | Van de |
|---|---|---|
| Cho qua binh thuong | Co goi API scan va ghi lane event | Demo duoc |
| Override/cho qua co trach nhiem | Co nut, co reason, co ghi event | Can step-up/quyen/audit UI tot hon |
| Manual operation | Moi ghi event va toast | Chua co quy trinh nhap tay/chon ly do/xac minh |
| Duress/bi ep buoc | Backend tao `DuressEvent` va `Alarm Critical` | Chua co broadcast realtime toan he thong |
| Intervention approval | Co request, accept, reject, execute | Execute moi doi trang thai, UI con alert/prompt |
| Emergency pass | Co emergency state backend | Chua dung nghia "cap quyen di qua ngay cho nguoi/xe cu the" |
| Company-wide alarm | Co SOC alarm list, header notification tinh | Chua co realtime notification va banner toan app |

## 3. Kien truc UI muc tieu

### 3.1. Workspace moi

Sidebar phai chuyen tu danh sach page sang workspace:

| Workspace | Vai tro | Man chinh | Man phu |
|---|---|---|---|
| Control Room | BaoVe, Admin | `GateTransitMonitor` moi | QR scanner, barrier, lane status |
| Exceptions & Approvals | QuanLy, Admin, BaoVe read/own | Intervention queue | Exception history, audit timeline |
| People & Vehicles | Admin, QuanLy | Employees, Vehicles | Access permission, metadata |
| Visitors & Contractors | BaoVe, Admin | Reception | Pre-registration, guest profiles, contractor, watchlist |
| SOC & Devices | BaoVe, Admin | Alarm console | Device health, topology, offline packages |
| Evidence & Compliance | Admin, QuanLy read-only neu backend cho | Evidence repository | Export, redaction, retention, reports |
| System Admin | Admin | Users/settings/policy | Simulator/release/drill/orphan pages |

### 3.2. Role landing page

Sau login:

- `BaoVe` vao thang `Control Room`.
- `QuanLy` vao `Exceptions & Approvals` hoac dashboard bao cao neu khong co pending approval.
- `Admin` vao `System Health / Admin Overview`.
- `Staff` neu con dung thi vao `Dynamic QR Generator`.

### 3.3. Navigation rule

- Khong hien nut neu backend chac chan se 403.
- Neu can xin phe duyet thi nut phai ghi ro "Gui yeu cau", khong ghi "Cho qua".
- Legacy Face ID khong xuat hien trong demo menu.
- Simulator chi hien khi bat demo/admin mode.

## 4. He thong UI dung chung can lam dau tien

### 4.1. `EnterpriseDataTable`

Tao component dung chung:

- File de xuat: `View/src/components/shared/EnterpriseDataTable.vue`
- Props:
  - `columns`
  - `rows`
  - `loading`
  - `error`
  - `emptyTitle`
  - `emptyMessage`
  - `rowKey`
  - `density`
  - `stickyHeader`
- Slots:
  - `cell:<columnKey>`
  - `rowActions`
  - `toolbar`
  - `empty`
- Visual:
  - nen bang solid
  - header ro
  - hover ro nhung nhe
  - text contrast cao
  - status chip khong trong suot

Dung thay cho cac bang uu tien:

1. `Vehicles.vue`
2. `UserManagement.vue`
3. `PreRegistration.vue`
4. `AccessLogs.vue`
5. `Exceptions.vue`
6. `BarrierPanel.vue`
7. `SocAlarmConsole.vue`

### 4.2. `EnterpriseActionDrawer`

Dung cho cac action rui ro:

- allow
- deny
- manual pass
- override
- emergency pass
- duress
- intervention request
- approve/reject/execute

Yeu cau:

- khong dung `alert/prompt/confirm`
- co form ly do
- co checkbox "toi chiu trach nhiem" khi can
- co level risk
- co step-up neu backend can
- co preview ket qua truoc khi submit
- co audit receipt sau submit

### 4.3. `GlobalEmergencyBanner`

Component hien tren toan app khi co emergency/duress/critical alarm.

Can co:

- banner o top layout
- mau theo severity
- am thanh nhe/tuy chon neu demo can
- nut "Xem chi tiet"
- khong chiem man hinh qua muc, khong pha cong viec dang lam
- tu dong cap nhat tu API polling neu chua co SignalR

De tranh lam qua lon, phase dau co the polling:

- `/api/enterprise/soc/alarms?state=New&severity=Critical&pageSize=5`
- `/api/enterprise/access-policy/emergency-states?active=true`
- `/api/enterprise/access-policy/duress-events?unacknowledged=true`

Sau do moi nang cap SignalR.

### 4.4. `AuditReceiptToast`

Da co component. Can chuan hoa dung cho:

- allow
- deny
- manual
- override
- duress
- emergency
- intervention approve/reject/execute
- barrier command

Moi receipt hien:

- action
- actor
- thoi gian
- lane/gate
- subject/plate
- ket qua
- ma tham chieu

## 5. Phase trien khai chi tiet

### Phase 0 - Bao ve hien trang va lap inventory

Muc tieu: agent khong bi lam lan va khong cham vung cam.

Viec can lam:

1. Chay `git status` va ghi chu cac file dirty san co.
2. Kiem tra diff vung cam truoc khi sua.
3. Lap file inventory tam neu can: route, page, API, role.
4. Xac nhan app build duoc truoc khi sua lon.

Nghiem thu:

- Khong co diff trong vung cam.
- Co danh sach route/page can keep/refactor/legacy/hide.

### Phase 1 - Lam sach dieu huong va dinh vi san pham

Muc tieu: nguoi demo biet vao dau; nguoi xem thay san pham dung QR dong.

Viec can lam:

1. Sua login metric:
   - bo `Face ID - Nhan dien khuon mat`
   - thay bang `QR dong`, `Bien so xe`, `Canh bao ngoai le`
2. Sua `Sidebar.vue`:
   - gom menu theo workspace o muc 3.1
   - an Face ID, Face Video, Biometrics khoi demo navigation
   - dua simulator/orphan page vao `System Admin > Operations`
3. Sua `Header.vue`:
   - mo ta Gate Transit khong nhac face
   - notification se la shell cho alarm thuc te
4. Sua chatbot/guide neu chung hien Face ID trong demo.
5. Sua router redirect theo role.

Nghiem thu:

- BaoVe login thay Control Room dau tien.
- Admin van vao duoc cac trang quan tri.
- Face ID khong xuat hien tren demo path chinh.
- Menu con toi da 6-8 muc quan trong cho moi vai tro.

### Phase 2 - Chuan hoa visual UI

Muc tieu: het cam giac bang trong suot/nhat/khong enterprise.

Viec can lam:

1. Them `EnterpriseDataTable`.
2. Tao CSS token:
   - `--surface-table`
   - `--surface-table-header`
   - `--text-strong`
   - `--status-*`
3. Refactor 5 man dau:
   - Vehicles
   - UserManagement
   - PreRegistration
   - AccessLogs
   - Exceptions
4. Sau do refactor cac man enterprise co table:
   - BarrierPanel
   - SocAlarmConsole
   - EvidenceRepository
   - DeviceHealth
   - WatchlistQueue
5. Them state chung:
   - loading
   - error
   - empty
   - permission denied

Nghiem thu:

- Khong con header bang `rgba(0,0,0,0.1)` tren cac man demo chinh.
- Bang doc ro tren laptop/projector.
- Action row khong bi day lung tung.

### Phase 3 - Lam Control Room thanh man demo chinh

Muc tieu: moi tinh huong tai cong duoc xu ly ngay trong mot workspace.

Man chinh: `GateTransitMonitor`.

Can tach hoac them component:

- `LaneScannerPanel`
- `SubjectVerificationPanel`
- `AccessDecisionPanel`
- `ManualOperationPanel`
- `DecisionResultReceipt`
- `LaneEventTimeline`

Luong tren man:

1. Quet QR/bien so hop le.
2. Khong doc duoc QR/bien so -> nhap tay.
3. Du lieu lech DB -> BaoVe override co trach nhiem.
4. Sai thong tin/khong xac minh duoc -> deny.
5. Can duyet -> tao intervention request.
6. Bi ep buoc -> duress.
7. Khan cap can di qua ngay -> emergency pass.

API/UI can noi:

- Allow/Deny: giu goi scan hien co + `recordLaneEvent`.
- Manual:
  - them form nhap tay: subject, plate, reason, evidence note
  - ghi `MANUAL_REVIEW` hoac `MANUAL_PASS` lane event
  - neu cho qua that thi goi scan/override path dung rule
- Override:
  - reason bat buoc
  - responsibility checkbox bat buoc
  - step-up neu role/admin policy yeu cau
  - ghi `OVERRIDE` lane event
- Escalate:
  - tao `OperationalInterventionRequest`
  - hien request id va SLA
  - day sang Exceptions queue
- Duress:
  - goi `recordDuressEvent`
  - backend tao `Alarm Critical`
  - hien global banner sau khi polling cap nhat
- Emergency pass:
  - khong chi tao `EmergencyState` chung chung
  - can UI tach 2 loai:
    - `Emergency Mode`: lockdown/evacuation/shelter
    - `Emergency Pass`: cho nguoi/xe cu the di qua ngay
  - Neu backend chua co endpoint Emergency Pass rieng, phai tao endpoint hop phap o API layer, khong sua runtime:
    - `POST /api/enterprise/access-policy/emergency-passes`
    - luu actor, reason, subject, plate, lane, expiresAt
    - tao `Alarm` severity Critical/High tuy loai
    - ghi lane event

Nghiem thu:

- BaoVe co the xu ly ca QR fail, plate fail, data mismatch, deny, escalate, duress tu cung mot drawer.
- Khong dung alert/prompt/confirm.
- Moi action co receipt.
- Exceptions/SOC thay duoc ket qua.

### Phase 4 - Hoan thien Exceptions & Approvals

Muc tieu: duyet cho qua va theo doi ngoai le khong con nua voi.

Can lam:

1. Refactor `Exceptions.vue` thanh 3 tab:
   - `Can xu ly ngay`
   - `Cho duyet`
   - `Da xu ly / audit`
2. Dung `EnterpriseDataTable`.
3. Dung drawer detail thay alert/prompt:
   - timeline
   - subject/plate/lane
   - reason
   - risk
   - evidence links
   - history
4. Noi them API:
   - `getInterventionOverview`
   - `getInterventionRequestDetail`
   - `expireInterventionRequests`
5. Sua execute:
   - neu request la temporary grant thi tao temporary grant hoac ghi ro "approval only"
   - neu request la emergency pass thi goi endpoint emergency pass
   - neu request la device override thi ghi command/audit dung endpoint
6. Cap nhat role:
   - BaoVe tao request va xem request cua minh
   - Admin duyet/thuc thi
   - QuanLy chi hien neu backend duoc mo quyen; neu chua, chi read-only/report

Nghiem thu:

- Tao request tu Control Room xong thay ngay trong queue.
- Admin accept -> trang thai doi.
- Admin execute -> co hieu ung that hoac receipt noi ro ket qua.
- Reject bat buoc ly do.
- Het han request co trang thai Expired.

### Phase 5 - Hoan thien global alarm/notification

Muc tieu: "bao dong toan cong ty" nhin thay that, khong can vao dung trang moi biet.

Can lam:

1. Tao service frontend `securityAlertBus`.
2. Tao `GlobalEmergencyBanner` trong `MainLayout`.
3. Poll 3 nguon:
   - active emergency states
   - unacknowledged duress events
   - critical new alarms
4. Header notification lay du lieu that, khong dung array tinh.
5. Khi co duress/emergency:
   - banner hien mau danger
   - notification count tang
   - click vao mo SOC/Exceptions detail
6. Them ack flow:
   - Admin/BaoVe SOC ack duress/alarm
   - banner bien mat khi khong con active critical/unacknowledged
7. Sau khi polling on dinh, co the nang cap SignalR:
   - them hub `SecurityAlertHub`
   - backend publish khi tao Alarm/Emergency/Duress/Intervention

Nghiem thu:

- Bam duress trong Control Room -> trong 5-10 giay banner hien o moi page.
- Header notification hien alarm moi.
- SOC alarm list co alarm Critical.
- Ack/close xong banner cap nhat.

### Phase 6 - SOC va Evidence ho tro demo

Muc tieu: nguoi xem thay duoc "su co di dau sau khi bao dong".

Can lam:

1. SocAlarmConsole:
   - highlight Critical/New
   - action drawer thay modal/alert
   - nut "Start SOP"
   - comment/audit ro
   - noi `predictEscalationRisk` neu co
2. Evidence:
   - detail drawer dung `getEvidenceItem`
   - lien ket evidence tu exception/alarm
   - hien hash/custody/export state
3. Device:
   - device detail dung `getDevice`
   - stale/offline alarm lien ket SOC

Nghiem thu:

- Duress tao alarm -> SOC thay -> ack -> comment -> close.
- Exception co evidence link -> mo detail -> thay chain.

### Phase 7 - Demo data va reset demo

Muc tieu: demo lap lai duoc, khong phu thuoc thao tac DB thu cong.

Can lam:

1. Kiem tra seed data hien co.
2. Them demo scenario data neu chua co:
   - employees
   - vehicles
   - visitors
   - lanes
   - access permissions
   - sample QR sessions
   - stale device
   - open alarm
   - intervention request pending
3. Them toggle cau hinh:
   - `DemoData:Enabled`
   - mac dinh true trong Development
   - production false hoac can env override
4. Them endpoint admin/dev neu can:
   - `POST /api/demo/reset`
   - `POST /api/demo/scenarios/{scenario}/prepare`
   - chi Development/Admin

Nghiem thu:

- Xoa DB/chay lai van co du lieu demo.
- Doc `docs/demo/README.md` la co the demo het luong.
- Co cach reset ve trang thai ban dau.

### Phase 8 - Test va nghiem thu

Bat buoc chay:

- `dotnet test API\API\API.Tests\API.Tests.csproj --no-restore --verbosity minimal`
- `npm run build` trong `View`
- API health ready
- Vue open OK
- Login Admin/BaoVe/QuanLy/dev accounts OK
- Chay tung kich ban trong `docs/demo/README.md`

Checklist nghiem thu:

- Khong diff vung cam.
- Khong con Face ID trong demo path chinh.
- Bang demo chinh khong trong suot.
- Control Room xu ly duoc 7 tinh huong.
- Exceptions queue co duyet that.
- Duress/emergency co global banner.
- SOC thay alarm va ack/close duoc.
- Moi action rui ro co reason + actor + receipt.

## 6. Thu tu uu tien cho agent thuc hien

Lam theo dung thu tu:

1. Phase 0: inventory va bao ve vung cam.
2. Phase 1: navigation + an Face ID + role landing.
3. Phase 2: table system cho cac man demo chinh.
4. Phase 3: Control Room.
5. Phase 4: Exceptions & Approvals.
6. Phase 5: Global alarm.
7. Phase 7: Demo data/reset.
8. Phase 6: SOC/Evidence polish.
9. Phase 8: test full.

Khong duoc bat dau them tinh nang enterprise moi khi Control Room va demo runbook chua chay muot.

## 7. Dinh nghia hoan thanh

Ke hoach nay chi hoan thanh khi:

- Agent khac co the mo `docs/demo/README.md` va demo tung kich ban khong can hoi them.
- Moi kich ban co UI ro, API that, ket qua that, audit/receipt that.
- Nguoi xem demo hieu ngay:
  - ai thao tac
  - vi sao he thong cho/tu choi
  - khi ngoai le thi ai chiu trach nhiem
  - khi khan cap thi ca cong ty thay canh bao o dau
  - sau su co thi SOC/Evidence xu ly tiep ra sao
