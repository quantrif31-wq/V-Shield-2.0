# Bao cao kiem tra UI/UX va muc do hoat dong thuc te cua V-Shield

Ngay lap: 2026-06-18

Pham vi: ra soat ma nguon hien tai cua `View`, API backend lien quan den cac luong an ninh doanh nghiep, tinh trang route/navigation, muc do view goi API, va kha nang chay co ban. Bao cao nay phuc vu giai doan quy hoach lam sach du an, chua tien hanh sua UI hay logic.

Ranh gioi: khong cham `AI_Runtime/**`, `runtime/**`, cac script public-domain, va khong tinh Face ID la dinh huong san pham chinh vi du an da chuyen sang QR dong.

## Ket luan nhanh

Du an da co nen tang kha day: login, health, dashboard, QR dong, gate transit, barrier, exception/intervention, evidence, device health, SOC va cac module enterprise deu da co ma nguon. Tuy nhien, ve trai nghiem san pham, app chua sach: dieu huong qua day, nhieu man co muc uu tien ngang nhau, con man Face ID cu, bang du lieu o nhieu noi qua trong suot, va mot phan backend/API moi chi duoc noi mong vao view.

Cham theo goc nhin "san pham demo tot nghiep co tham vong doanh nghiep":

| Hang muc | Diem hien tai | Nhan xet |
|---|---:|---|
| Dieu huong va sap xep tinh nang | 58% | Co nhom menu, nhung qua nhieu module ngang cap; luong bao ve, quan ly, admin con bi tron. |
| UI visual consistency | 52% | Nhieu table/card dung nen trong suot/rgba, doc kem; style bang bi lap lai tung trang. |
| Luong nghiep vu thuc chien | 60% | QR/gate/exception da co huong dung, nhung nhieu luong moi o muc thao tac tung man. |
| Ket noi backend-view | 62% | Nhieu API da duoc goi, nhung 18 ham enterprise chua duoc view nao dung va 101 ham chi goi rat mong. |
| Demo readiness | 70% | API va Vue dang tra 200; co the demo cac luong chinh neu dung tai khoan/du lieu dung. |
| Do sach de tien len giai doan san pham | 55% | Can quy hoach lai navigation, UI table system, role workspace, va cat bo/legacy cac man lech dinh huong. |

Uoc tinh chuc nang theo muc do hoat dong:

| Loai | Ty le uoc tinh | Noi dung |
|---|---:|---|
| Hoat dong thuc te du de demo | 45-55% | Login, dashboard, logs, QR dong, gate/barrier co action, vehicles/employees co CRUD, health, mot phan evidence/device/SOC. |
| Da co backend va view nhung chua thanh workflow muot | 25-30% | Policy lifecycle, intervention escalation, visitor/contractor, evidence governance, device topology, SOC incident. |
| Con la view, backend roi rac, hoac module can cleanup | 20-30% | 6 page chua vao router, Face ID cu, mot so API enterprise chua duoc view dung, nhieu page dang la bang CRUD/console rieng le. |

## Bang chung cu tu ma nguon

| Chi so | Gia tri |
|---|---:|
| Tong page Vue trong `View/src/pages` | 63 |
| Page da duoc router import | 57 |
| Page ton tai nhung chua duoc router dung | 6 |
| File co table/list dang bang | 50 |
| File co style trong suot/rgba/backdrop-filter dang ke | 36 |
| Ham trong `enterpriseSecurityApi.js` | 185 |
| Ham enterprise chua duoc view goi | 18 |
| Ham enterprise chi duoc goi mong | 101 |
| Health API hien tai | `200` |
| Vue dev server hien tai | `200` |

6 page ton tai nhung chua duoc router dung:

- `BackupRestoreDrillDashboard.vue`
- `OperationsDashboard.vue`
- `OutboxViewer.vue`
- `SIEMExportStatus.vue`
- `VulnerabilityReleaseGateStatus.vue`
- `WebhookDeliveryViewer.vue`

18 ham enterprise chua duoc view nao goi:

- `predictEscalationRisk`
- `createCompany`
- `createDoor`
- `createLane`
- `evaluateAccess`
- `getContractorDetail`
- `createParkingArea`
- `getDevice`
- `getEvent`
- `createAiAdjudication`
- `recordAiMetric`
- `getEvidenceItem`
- `getRetentionPolicy`
- `createRetentionPolicy`
- `getConfigHealth`
- `getInterventionOverview`
- `getInterventionRequestDetail`
- `expireInterventionRequests`

## Kiem tra UI/UX tong the

### 1. Dieu huong hien tai

`Sidebar.vue` da chia nhom lon: Tong quan, Giam sat, SOC & Enterprise, Khach tham, Nha thau, Giao thong, Video & AI, Evidence, Quan tri, Cham cong, Ban do, AI & Thiet bi, Cai dat. Ngoai ra con nhom rieng "Dieu phoi thong hanh".

Van de:

- Qua nhieu page nam ngang cap, nguoi dung moi kho biet "vao dau de lam viec".
- Cung mot nghiep vu bi tach thanh nhieu diem vao: gate transit, QR scanner, QR access monitor, barrier panel, lane dashboard, plate review.
- Man van hanh hang ngay cua BaoVe nam cung vung voi console enterprise/admin/simulator.
- QuanLy duoc nhin mot so man bao cao, nhung workflow duyet/canh bao can thiet ke lai de uu tien viec can xu ly truoc.
- Face ID va Video khuon mat van con trong route/sidebar/header/login/chatbot/guide, gay lech san pham vi he thong hien theo QR dong.

Khuyen nghi quy hoach:

| Workspace moi | Doi tuong chinh | Gom cac man |
|---|---|---|
| Trung tam dieu phoi | BaoVe, Admin | Gate Transit, QR Scanner, Barrier, Lane Dashboard, canh bao truc tiep |
| Xu ly ngoai le | BaoVe, QuanLy, Admin | Can thiệp, duyet yeu cau, duress, emergency pass, audit timeline |
| Nhan su & phuong tien | Admin, QuanLy | Employees, Vehicles, access permission, metadata |
| Khach & nha thau | BaoVe, Admin | Reception, pre-registration, guest profiles, contractor, watchlist |
| SOC & thiet bi | BaoVe, Admin | Alarm console, device health, topology, offline packages |
| Chung cu & tuan thu | Admin, QuanLy neu can | Evidence, export approval, redaction, retention, reports |
| Quan tri he thong | Admin | Users, catalog, settings, identity, policy, simulator, release/drill |

### 2. Chat luong hien thi bang

Nhan xet cua ban ve bang trong suot la dung. Nhieu page dang dung `rgba`, `backdrop-filter`, nen bang hoac card tren nen sang/hoa tiet bi nhat. Cac vi du ro:

- `UserManagement.vue` va `Vehicles.vue` dung `.sleek-table th` voi `background: rgba(0,0,0,0.1)`, nen header co cam giac toi mo va khong sang trong.
- `PreRegistration.vue` lap lai cung pattern `.sleek-table`.
- Nhieu page dung `.data-table` rieng, nhung khong co component/table system chung.
- `SystemAuditLogs.vue` dang tot hon: nen trang, header ro, phan tach hang de doc hon.

Chuan can tien toi:

- Tat ca bang du lieu dung nen solid: container `#ffffff`, header `#f8fafc` hoac `#eef3f8`, row hover ro nhung nhe.
- Text chinh khong duoc dung muted qua nhieu; cot quan trong phai dam.
- Header sticky voi cac bang dai.
- Status chip co nen/chu du tuong phan, khong dung opacity qua thap.
- Bang tren cac man van hanh khan cap khong nen nam trong glass/card trong suot.
- Tao mot component `EnterpriseDataTable` hoac CSS token dung chung, khong lap style table moi o tung page.

### 3. Login va dinh vi san pham

Login da co MFA setup QR, day la diem dung. Tuy nhien login van hien metric `Face ID - Nhan dien khuon mat`, trong khi dinh huong san pham da chuyen sang QR dong. Can doi thanh:

- `QR dong`
- `Bien so xe`
- `Giam sat cong`
- `Canh bao ngoai le`

Day la loi UX/san pham nho nhung rat de gay mat niem tin khi demo.

### 4. Header, chatbot, guide

Header va chatbot van nhac Face ID, video khuon mat, du lieu nhan dien. Neu khong dung nua thi nen:

- An route Face ID khoi BaoVe/Admin trong demo chinh.
- Doi guide/chatbot sang QR dong va bien so.
- Neu can giu code cu, dua vao nhan `Legacy/Disabled` trong admin, khong xuat hien o luong van hanh.

## Danh gia tung module

### Auth, session, startup

Trang thai: hoat dong co ban.

Da co:

- API health/readiness tra 200.
- Vue dev server tra 200.
- Login co MFA setup QR.
- CORS va login da duoc sua o cac dot truoc.

Can lam sach:

- Loi hien thi login can noi dung ro hon theo ma loi backend.
- Demo admin co MFA can tai lieu/thiet lap ro de tranh nhap sai bi khoa rate-limit.
- Cac thong diep loi nen phan biet: sai mat khau, thieu MFA, qua nhieu lan thu, server loi, can reset MFA.

### Dashboard, monitoring, access logs

Trang thai: hoat dong thuc te kha on.

Da co:

- Dashboard, monitoring, access logs, UEBA, system audit logs co route va du lieu.
- Phu hop cho demo tong quan.

Can lam sach:

- Dashboard van con noi dung lien quan model khuon mat.
- Can uu tien card theo vai tro: BaoVe can canh bao/lane/exception; QuanLy can bao cao va duyet; Admin can health/risk/config.

### QR dong, thong hanh, gui xe, barrier

Trang thai: co kha nang demo thuc te, nhung UX can tinh gon.

Da co:

- `GateTransitMonitor.vue` co decision drawer, step-up modal, audit toast.
- BaoVe co action `allow`, `deny`, `manual`, `override`, `duress`, `escalate`.
- Co ghi lane event, duress event, intervention request.
- `BarrierPanel.vue` co command voi ly do/step-up/audit toast.
- Dynamic QR generator/scanner va QR access monitor co route rieng.

Van de:

- Nguoi van hanh phai biet vao dung man nao trong 4-5 man lien quan cung mot cong.
- Chua ro mot "man lam viec chinh" cho BaoVe: quet QR, quet bien so, cho qua, tu choi, nhap tay, bao loi camera, xin duyet, emergency pass nen o cung mot flow.
- `GateTransitMonitor.vue` qua lon va gom nhieu hanh vi; can tach thanh component nho de de test.
- Mot so noi van dung ngon ngu face trong header/old component.

Ket luan: day la cum chuc nang co gia tri that nhat, nen duoc chon lam "luong demo chinh" va dau tu UI/UX dau tien.

### Xu ly ngoai le va can thiep

Trang thai: da co backend + view, nhung can nghiem thu ky.

Da co:

- `EnterpriseInterventionController.cs` co endpoint tao, xem, accept, reject, execute intervention.
- `Exceptions.vue` co hang doi intervention, timeline, form tao request, action accept/reject/execute.
- `GateTransitMonitor.vue` co the tao intervention request tu luong quet.
- Co demo data cho manual override, duress, emergency pass.

Van de:

- Mot phan migration/model intervention hien dang la thay doi chua commit trong worktree.
- `getInterventionOverview`, `getInterventionRequestDetail`, `expireInterventionRequests` chua duoc view dung.
- Action duyet hien con dung `alert()` o mot so cho, chua dat UX enterprise.
- Role can tinh lai: BaoVe duoc tao/override trong gioi han; QuanLy/Admin duyet cac ca can phe duyet; Admin quan tri cau hinh va policy.

Ket luan: khong con chi la ke hoach, nhung chua du "san pham muot". Can thiet ke lai thanh hai luong: can thiep ngay tai diem quet va hang doi duyet cua QuanLy/Admin.

### Policy, emergency, temporary grant

Trang thai: backend va UI co, nhung admin/technical-heavy.

Da co:

- Policy overview/version/rule/lifecycle.
- Temporary grant.
- Anti-passback reset.
- Emergency states, duress events.
- Step-up cho action rui ro o backend.

Van de:

- `evaluateAccess` chua duoc view goi, trong khi day la ham cot loi de debug vi sao cho vao/tu choi.
- UI PolicyEngine thien ve ky thuat; phu hop Admin hon BaoVe/QuanLy.
- Can co "decision explanation" de BaoVe/QuanLy hieu ly do tu choi/cho phep nhanh.

### Visitor, contractor, watchlist

Trang thai: partial workflow.

Da co:

- Pre-registration, registration links, guest profiles, reception, kiosk, host visitor, watchlist, contractor.
- Cac man co table, form, list.

Van de:

- Contractor detail API chua duoc view dung.
- Luong visitor lifecycle chua doc thanh mot timeline duy nhat: pre-register -> approve -> check-in -> credential -> escort -> check-out -> expire.
- Cac bang dung style rieng, doc chua dong nhat.

### Vehicle, parking, plate review

Trang thai: CRUD va van hanh co mot phan, can noi voi exception.

Da co:

- Vehicles page.
- Lane dashboard, plate review, barrier panel.
- Lane event recording.

Van de:

- `createParkingArea` chua duoc view dung.
- Cac ca database lech trang thai "dang gui xe" can mot drawer xu ly tai diem quet, khong bat BaoVe nhay qua trang khac.
- Can them lich su xe dang do, owner verification, manual correction reason, audit receipt ro rang.

### Device, runtime health, simulator

Trang thai: partial, phu hop demo giam sat hon la van hanh day du.

Da co:

- Device management, topology, health, provisioning, offline packages, simulator.
- Device readers/relays/sensors/history/configuration co mot so noi goi API.

Van de:

- `getDevice` chua duoc view goi, nghia la detail workspace chua dung dung API detail.
- Simulator nam trong dieu huong BaoVe/Admin, nen trong san pham that phai dua vao Admin/DevOps/Demo mode.
- Can canh bao device stale/offline hien len dung cho BaoVe ma khong bat mo nhieu trang.

### SOC, incident, AI review

Trang thai: co console, partial operation.

Da co:

- SOC Alarm Console, AI review queue, correlation, event timeline.
- Alarm lifecycle va status chip.

Van de:

- `predictEscalationRisk`, `createAiAdjudication`, `recordAiMetric`, `getEvent` chua duoc view dung.
- Nhieu man dang tach thanh dashboard rieng; can gom thanh SOC workspace co left queue + detail + SOP + evidence links.
- Mau nen dark/transparent o SOC co the hop nganh SOC, nhung can tang contrast cho text/status.

### Evidence va compliance

Trang thai: broad coverage, partial depth.

Da co:

- Evidence repository, export approval, redaction queue, retention/legal hold, compliance reports.
- Nhieu API duoc noi: list evidence, create item, hash verify, custody, collection, export, redaction, retention.

Van de:

- `getEvidenceItem`, `getRetentionPolicy`, `createRetentionPolicy` chua duoc view dung.
- Detail drawer/evidence chain can ro hon: hash, custody, legal hold, export approval, watermark/signature.
- Admin-only la hop ly, nhung QuanLy co the can quyen xem bao cao compliance read-only.

### Operations/release readiness

Trang thai: backend/page co dau hieu ton tai nhung chua duoc expose dung.

6 page operations/release dang ton tai nhung chua route:

- `OperationsDashboard`
- `OutboxViewer`
- `SIEMExportStatus`
- `WebhookDeliveryViewer`
- `BackupRestoreDrillDashboard`
- `VulnerabilityReleaseGateStatus`

Ket luan: day la vung backend/view roi rac. Can quyet dinh: dua vao workspace `Quan tri he thong > Van hanh`, hoac xoa neu khong dung cho do an.

## Phan loai tinh nang theo trang thai

### Co the xem la hoat dong thuc te

- Health/readiness/startup co ban.
- Login co MFA setup QR.
- Dashboard, access logs, monitoring, audit logs.
- Employees, vehicles, users, catalog o muc CRUD.
- Dynamic QR generator/scanner.
- Gate transit action drawer: allow/deny/manual/override/duress/escalate.
- Barrier command co ly do/step-up.
- Exception/intervention queue o muc tao/duyet/thuc thi.
- Device health/topology o muc giam sat.
- Evidence repository/export/redaction/retention o muc broad demo.

### Hoat dong mot phan, can noi thanh workflow

- Visitor lifecycle.
- Contractor lifecycle.
- Parking/vehicle exception correction.
- Access policy lifecycle va decision explanation.
- SOC alarm + SOP + incident + evidence.
- Device provisioning/offline package/replay/stale alarm.
- Evidence chain/custody/legal hold/export approval.
- Operations/release readiness.

### Con la view/backend roi rac hoac can legacy

- Face ID, face video, biometrics trong navigation va guide.
- 6 page operations chua route.
- 18 ham enterprise chua co view dung.
- 101 ham enterprise chi goi mong, can nghiem thu tung flow truoc khi noi la hoan chinh.
- `EnterpriseSecurityOperations.vue` dang la console gom qua nhieu thu, nen nen tach role/workspace thay vi lam man chinh.

## Rui ro UX lon nhat

1. BaoVe khong co mot man lam viec duy nhat cho ca quet QR, bien so, camera loi, nhap tay, cho qua, tu choi, xin phe duyet, emergency pass.
2. QuanLy khong co hang doi duyet uu tien ro: critical truoc, sap het SLA, duress/emergency, override can review.
3. Admin co qua nhieu man cau hinh, chua co ban do phu thuoc de biet cau hinh nao anh huong den cong nao.
4. Bang du lieu kho doc lam giam cam giac "enterprise".
5. Face ID cu xuat hien nhieu noi, gay lech thuyet minh do an.
6. Cac page operations/release ton tai nhung khong route lam repo co cam giac chua don dep.

## Ke hoach quy hoach lam sach de xuat

### Phase 1: Dong bang inventory va vai tro

- Lap danh sach route theo vai tro Admin/QuanLy/BaoVe/Staff.
- Danh dau moi page: `Core demo`, `Enterprise support`, `Admin only`, `Legacy`, `Remove/Hide`.
- An hoac danh dau Legacy tat ca Face ID/face video/biometrics khoi luong demo QR dong.
- Dua 6 page orphan vao quyet dinh: route trong Operations hoac xoa neu khong dung.

### Phase 2: Lam lai navigation theo workspace

- Tao sidebar theo workspace, khong theo danh sach ky thuat.
- BaoVe mac dinh vao `Trung tam dieu phoi`.
- QuanLy mac dinh vao `Hang doi duyet & Bao cao`.
- Admin mac dinh vao `Suc khoe he thong & Cau hinh`.
- Simulator/release/drill chi hien khi bat Demo/Admin mode.

### Phase 3: Chuan hoa UI table va surface

- Tao mot table system dung chung cho 50 file co table.
- Thay `.sleek-table`, `.data-table`, table custom bang component/style chung.
- Loai bo nen trong suot o cac bang nghiep vu.
- Them loading/error/empty/permission denied state dong nhat.
- Them sticky header, row density, row action menu, search/filter summary.

### Phase 4: Dong goi luong van hanh chinh

- Gate/parking/limited area chi dung mot workspace:
  - scan QR/bien so
  - manual input khi khong doc duoc
  - show decision reason
  - allow/deny
  - override co trach nhiem
  - escalation request
  - emergency pass
  - audit receipt
- Exception page chuyen thanh hang doi cua QuanLy/Admin, khong bat BaoVe nhay qua khi dang quet.

### Phase 5: Ket noi cac API bi roi

Uu tien noi cac API chua dung neu con gia tri:

- `evaluateAccess`: dua vao drawer giai thich quyet dinh.
- `getInterventionOverview`, `getInterventionRequestDetail`, `expireInterventionRequests`: dua vao Exceptions.
- `getEvidenceItem`: dua vao Evidence detail drawer.
- `getDevice`: dua vao Device detail drawer.
- `predictEscalationRisk`: dua vao SOC queue neu AI gateway hoat dong.
- `createAiAdjudication`, `recordAiMetric`: chi noi neu co luong AI review that.
- `getRetentionPolicy`, `createRetentionPolicy`: dua vao Retention admin.

API khong dung cho do an nen xoa khoi service hoac danh dau future, tranh gay cam giac "co nhung khong chay".

### Phase 6: Nghiem thu theo luong demo

Can test bang tai khoan that:

- Admin: login, policy, evidence, users, settings, release/admin.
- QuanLy: xem dashboard/report, duyet exception/intervention, xem compliance read-only neu cho phep.
- BaoVe 1: gate QR pass hop le.
- BaoVe 2: xe bi lech database, override co trach nhiem.
- BaoVe 3: camera/QR loi, nhap tay va tao request.
- Emergency: cap quyen khan cap co ly do, toan he thong thay canh bao.
- Evidence: export request -> approve -> audit receipt.
- Device: thiet bi offline/stale -> alarm -> ack/resolve.

## Viec nen lam ngay truoc khi sua lon

1. Commit hoac tach rieng cac thay doi intervention migration hien dang dirty de tranh mat viec.
2. Tao file inventory route/page/API voi trang thai `keep/refactor/legacy/remove`.
3. Lam sach Face ID text khoi login/sidebar/header/chatbot/guide neu demo QR dong.
4. Chuan hoa bang o 5 man dau tien: Vehicles, UserManagement, PreRegistration, AccessLogs, Exceptions.
5. Chon `GateTransitMonitor` lam man van hanh chinh va tach component de de test.
6. Dua `Exceptions` thanh man hang doi duyet cua QuanLy/Admin va history/audit cho BaoVe.
7. Dua cac page orphan vao Operations workspace hoac xoa khoi repo neu khong dung.

## Ket luan

V-Shield hien khong phai chi la giao dien mau: nhieu luong thuc te da co API va view. Nhung de buoc sang giai doan "san pham sach", can ngung them man moi va bat dau quy hoach lai. Trong tam nen la: mot workspace van hanh cho BaoVe, mot hang doi duyet cho QuanLy/Admin, mot UI table system doc ro, va loai bo/legacy cac dau vet Face ID khong con dung.

Diem tong quan hien tai cho giai doan quy hoach lam sach: 58/100.

Neu lam dung cac phase tren, du an co the len khoang 78-82/100 cho demo tot nghiep enterprise-style ma khong can cham vung cam.
