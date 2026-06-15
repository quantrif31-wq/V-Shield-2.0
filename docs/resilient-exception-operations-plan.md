# Ke Hoach Chong Chiu Ngoai Le Va Van Hanh Thu Cong V-Shield

## 1. Muc Tieu

Bien luong xu ly ngoai le tu "trang xem log" thanh mot he thong van hanh thuc te co kha nang chong chiu khi:

- Database bi lech trang thai, vi du xe thuc te da ra nhung DB van ghi `IN`.
- QR khong doc duoc, doc sai, het han hoac replay.
- Bien so khong doc duoc, OCR sai, bien mo, camera lech goc.
- Camera QR, camera bien so, Python worker, go2rtc hoac API adapter bi loi.
- Bao ve can thao tac nhanh tai chot ma khong roi man hinh dang quet.
- Tinh huong khan cap can cap quyen ngay cho cong an, canh sat, cap cuu, PCCC, cuu ho ma khong the doi quy trinh thu tuc.
- Ca rui ro cao phai chuyen quan ly/SOC xu ly, co audit va bang chung.

Muc tieu cuoi: bao ve co the can thiep hop phap bang danh tinh cua minh trong pham vi duoc phep; quan ly xu ly cac ca vuot quyen o trang ngoai le; moi hanh dong deu co audit, ly do, before/after va kha nang truy vet.

## 2. Ranh Gioi An Toan

Tuyet doi khong sua cac vung cam:

- `AI_Runtime/**`
- `runtime/**`
- Public-domain scripts:
  - `scripts/setup-public-domain.ps1`
  - `scripts/uninstall-public-domain.ps1`
  - `scripts/reset-public-domain-appsettings.ps1`
  - `scripts/read-public-domain-appsettings.ps1`
  - `scripts/update-public-domain-appsettings.ps1`
  - `setup-public-domain.bat`
  - `uninstall-public-domain.bat`
- `API/API/API/appsettings.json.bak.public-domain`

Moi cai tao lien quan camera/Python/go2rtc chi duoc lam qua API wrapper, health model, timeout, retry, audit, UI dieu phoi va manual fallback. Khong sua truc tiep runtime bi cam.

### 2.1 Bang Dinh Nghia Va Thuat Ngu Chuan

Muc nay dung de tranh agent khac hieu sai cac tu viet tat, enum va pham vi nghiep vu trong ke hoach.

Neu agent tach viec bang ky hieu `D` va `E`, chuan hoa nhu sau:

- `D` = `Decision/Approval`: nhom luong can quyet dinh, phe duyet, tu choi, xin bo sung, chuyen cap, dong case.
- `E` = `Escalation/Emergency/Evidence`: nhom luong vuot quyen hoac khan cap, gom escalate len quan ly/SOC, emergency mode, alert/broadcast, evidence/legal hold.

Thuat ngu he thong:

- `DB`: database ung dung chinh, noi luu user, vehicle, visitor, access log, case, policy, audit.
- `QR`: ma QR dong hoac QR truy cap duoc ung dung dung de xac minh danh tinh/luot vao.
- `OCR`: nhan dang ky tu bien so tu anh/video. Trong ke hoach nay khong sua logic OCR/Python; chi dung ket qua co san hoac cho nhap/sua thu cong.
- `API`: backend .NET hien tai, noi thuc thi access decision, policy, audit, case, health wrapper.
- `UI`: frontend Vue hien tai.
- `SOC`: bo phan/nguoi dieu phoi an ninh, xu ly alarm, incident, escalation va hau kiem.
- `PCCC`: phong chay chua chay/cuu hoa.
- `HR`: nhan su, nguon trang thai nhan vien: active, suspended, terminated, transfer.
- `MFA`: xac thuc da yeu to.
- `SLA`: thoi han xu ly mot case/alarm/action truoc khi bi nhac hoac escalate.
- `NDA`: cam ket bao mat thong tin cho khach/nha thau/vendor.
- `SOP`: quy trinh thao tac chuan theo tung loai su co.
- `Runbook`: tap hop action tu dong/ban tu dong de dieu phoi mot su co.
- `Playbook`: bo SOP/runbook theo mot kich ban nghiep vu, vi du fire, duress, lockdown, lost QR.
- `Common Operating Picture`: man hinh tong hop trang thai toan cong ty/site/zone/gate/device/case/occupancy.

Thuat ngu van hanh:

- `Access decision`: ket qua backend quyet dinh mot luot vao/ra duoc cho phep, bi tu choi, can guard resolve hay can manager approval.
- `Exception case`: ho so ngoai le duoc tao khi luong binh thuong khong the xu ly an toan.
- `Incident`: su co an ninh/an toan co the gom nhieu case, alarm, evidence va action.
- `Alarm`: canh bao can hanh dong, co severity va SLA.
- `Alert`: thong bao hien cho user/nhan vien; co the la banner, toast, badge, notification.
- `Broadcast`: thong bao phat ra nhieu nguoi hoac toan cong ty theo scope.
- `Acknowledge`: hanh dong xac nhan da nhan/thay mot alert/alarm, khong dong nghia la da xu ly xong.
- `Audit`: nhat ky bat bien ve ai lam gi, luc nao, tai dau, truoc/sau ra sao.
- `Evidence`: bang chung gan voi case, co the la log, snapshot, file, export package, note.
- `Legal hold`: trang thai giu bang chung/log, khong cho xoa/sua/purge khi dang dieu tra/tuan thu.
- `Chain of custody`: chuoi ghi nhan ai xem/tai/export/chuyen giao evidence.
- `Post-incident review`: hau kiem sau su co/dien tap de ket luan, rut kinh nghiem va giao action item.

Thuat ngu phan quyen:

- `Role`: nhom tai khoan thuc te hien co, gom `Admin`, `QuanLy`, `BaoVe`.
- `Permission`: quyen chi tiet theo action, vi du `emergency.activate`, `exception.inline.resolve.medium`.
- `Step-up`: yeu cau xac thuc lai/MFA khi lam hanh dong rui ro cao.
- `Override`: cho phep/tu quyet vuot qua luong binh thuong trong pham vi policy va audit.
- `Hau kiem`: quan ly/SOC xem lai sau khi guard/emergency da xu ly de ket luan hop le hay sai quy trinh.

Thuat ngu trang thai va do uu tien:

- `Severity`: muc do nghiem trong. Chuan dung: `Info`, `Low`, `Medium`, `High`, `Critical`.
- `PriorityScore`: diem uu tien sap xep case, tinh tu severity, blocking, SLA, zone risk, elapsed time, emergency.
- `BlockingOperation`: case dang chan cong/bai xe/khu vuc hoac lam bao ve phai doi quyet.
- `CriticalActive`: su co Critical dang dien ra, chua end/review/acknowledge day du.
- `HighRisk`: nhom rui ro cao nhu watchlist, blacklist, QR replay, identity mismatch, sensitive zone.
- `SlaBreaching`: case sap qua han hoac da qua SLA.
- `Manual mode`: che do bao ve nhap/xac minh thu cong khi thiet bi/API/QR/plate khong du tin cay.
- `Offline mode`: che do van hanh gioi han khi API/DB/network khong san sang.
- `Fast-lane`: chinh sach tam thoi de giam un tac, chi ap dung cho doi tuong rui ro thap va can hau kiem.
- `Duress`: tinh huong nguoi van hanh bi ep buoc; can silent alarm va khong lam lo tren UI tai chot.
- `Drill`: dien tap, phai tach ro voi su co that bang `IsDrill = true`.

Thuat ngu doi tuong va pham vi:

- `Site`: mot co so/cong ty/chi nhanh.
- `Building`: toa nha trong site.
- `Floor`: tang.
- `Zone`: khu vuc an ninh.
- `Gate`: cong vao/ra.
- `Lane`: lan xe/luong xu ly tai cong.
- `Access point`: cua/cong/barrier/diem kiem soat.
- `Host`: nhan vien noi bo chiu trach nhiem tiep khach/nha thau.
- `Escort`: nguoi di kem bat buoc cho khach/nha thau/vendor trong zone rui ro.
- `Visitor`: khach.
- `Contractor`: nha thau lam viec co thoi han/nhiem vu.
- `Vendor`: don vi cung cap/giao hang/dich vu.
- `First responder`: luc luong ung pho khan cap nhu cong an, canh sat, cap cuu, PCCC, cuu ho.

Thuat ngu ky thuat:

- `CorrelationId`: ma truy vet xuyen suot mot luong request/case/log.
- `IdempotencyKey`: ma chong bam lap; request lap lai phai tra ket qua cu, khong tao log/action moi.
- `ScanSessionId`: ma cho mot phien scan/luot xu ly tai lane.
- `BeforeJson`/`AfterJson`: trang thai truoc/sau khi sua/chot quyet dinh.
- `Health wrapper`: lop bao quanh integration de do timeout, retry, circuit breaker, status online/degraded/offline/stale.
- `Degraded`: thiet bi/dich vu con chay nhung ket qua cham/khong on dinh/khong du tin cay.
- `Stale`: du lieu qua cu, khong nen dung de quyet dinh truc tiep.
- `go2rtc`: runtime streaming hien co, thuoc vung khong sua truc tiep.
- `Python worker`: tien trinh Python/AI/OCR hien co, thuoc vung khong sua truc tiep neu nam trong vung cam.

## 3. Nguyen Tac Thiet Ke

He thong phai co 3 tang chong chiu:

1. `AUTO`: QR, bien so, camera, DB deu binh thuong, xu ly tu dong.
2. `ASSISTED` / `MANUAL`: mot phan du lieu bi loi nhung bao ve xac minh duoc, xu ly ngay tai man hinh quet.
3. `ESCALATED`: rui ro cao hoac vuot quyen, tao case cho quan ly/SOC xu ly tai trang ngoai le.
4. `EMERGENCY`: tinh huong khan cap duoc cap quyen tuc thoi, nguoi kich hoat chiu trach nhiem ca nhan, he thong broadcast bao dong va bat buoc hau kiem.

Khong bat bao ve roi `GateTransitMonitor`, `DynamicQrScanner`, `QrAccessMonitor` de sang trang ngoai le trong luc dang xu ly xe/nguoi tai chot.

Trang ngoai le la workspace cho quan ly/SOC, khong phai man hinh van hanh tuc thoi cua bao ve.

## 4. Phan Loai Ngoai Le

### 4.1 Ngoai Le Bao Ve Duoc Xu Ly Inline

Bao ve duoc xu ly tai man hinh quet neu policy cho phep:

- `PARKING_STATE_MISMATCH`: DB ghi xe dang `IN` nhung xe thuc te dang vao, hoac DB ghi `OUT` nhung xe thuc te dang ra.
- `PLATE_UNREADABLE`: khong doc duoc bien so, bao ve nhap tay.
- `PLATE_CORRECTED`: OCR doc sai, bao ve sua bien.
- `QR_UNREADABLE`: khong doc duoc QR, bao ve nhap dinh danh thay the.
- `QR_EXPIRED`: QR het han nhung nguoi dung hop le va policy cho phep xac minh thu cong.
- `DEVICE_DEGRADED`: camera/Python/go2rtc loi, chuyen sang manual operation tam thoi.
- `TEMPORARY_ACCESS_BY_GUARD`: bao ve cap phep tam thoi trong pham vi quyen.
- `EMERGENCY_IMMEDIATE_ACCESS`: canh sat, cap cuu, PCCC, cuu ho, lanh dao ung pho su co can vao ngay lap tuc; nguoi cho phep chiu trach nhiem va bat buoc nhap ly do.

### 4.2 Ngoai Le Bat Buoc Chuyen Quan Ly

Bao ve khong duoc tu quyet:

- `WATCHLIST_MATCH`
- `BLACKLISTED_VEHICLE`
- `QR_REPLAY`
- `QR_IDENTITY_MISMATCH`
- `DUPLICATE_ACTIVE_IDENTITY`
- `RESTRICTED_ZONE_HIGH_RISK`
- `MANAGER_APPROVAL_REQUIRED`

Luu y: `EMERGENCY_IMMEDIATE_ACCESS` khong bi chan de doi phe duyet truoc, nhung sau khi kich hoat phai tu dong tao alarm/case muc `Critical` cho quan ly/SOC hau kiem.

## 5. Luong Van Hanh Tai Chot

### 5.1 Luong Binh Thuong

1. Camera QR doc QR.
2. Camera bien so doc plate.
3. API verify QR, plate, employee/guest, vehicle, gate, zone, policy.
4. Neu hop le, ghi `AccessLog` thanh cong.
5. Cap nhat `Vehicle.ParkingStatus` neu la luong gui xe.
6. Cap nhat zone transit/attendance neu lien quan.
7. UI reset lane, tiep tuc xu ly xe/nguoi tiep theo.

### 5.2 DB Lech Nhung Du Lieu Doc Dung

Vi du: xe `30A-12345` muon vao, QR va bien so dung, nhung DB dang ghi xe `IN`.

1. API khong toggle trang thai mu.
2. API tra response `requiresResolution = true`, `resolutionMode = GUARD_INLINE`.
3. UI hien modal tai lane dang quet:
   - Bien so.
   - Chu xe/nguoi dung.
   - QR hop le hay khong.
   - Trang thai DB hien tai.
   - Huong thao tac dang yeu cau: `IN`/`OUT`.
   - Log gan nhat.
4. Bao ve chon:
   - `Tu choi`
   - `Xac minh DB sai, sua trang thai va cho vao`
   - `Xac minh DB sai, sua trang thai va cho ra`
   - `Chuyen quan ly`
5. Neu bao ve sua:
   - Bat buoc nhap ly do.
   - Bat buoc tick xac nhan trach nhiem.
   - API ghi access log `SUCCESS_WITH_GUARD_OVERRIDE`.
   - `IsBypass = true`.
   - Ghi before/after state.
   - Ghi audit actor la user bao ve.

### 5.3 Khong Doc Duoc QR

1. UI phat hien QR timeout hoac API tra `QR_UNREADABLE`.
2. Bao ve bam `Nhap thu cong`.
3. Form bat buoc:
   - Ma nhan vien, email, phone hoac chon nhan vien.
   - Ly do khong doc QR.
   - Xac nhan da doi chieu nguoi/giay to.
4. API kiem tra nhan vien/khach, permission, watchlist.
5. Neu rui ro thap/trung binh va policy cho phep:
   - Cho qua voi `SUCCESS_WITH_MANUAL_IDENTITY`.
6. Neu rui ro cao:
   - Tu choi hoac escalate.

### 5.4 QR Doc Sai, Het Han Hoac Replay

- `QR_EXPIRED`: co the cho manual fallback neu policy cho phep.
- `QR_UNREADABLE`: co the cho manual fallback.
- `QR_REPLAY`: bat buoc tu choi/escalate.
- `QR_IDENTITY_MISMATCH`: bat buoc tu choi/escalate.

UI phai hien ro ly do va chi hien action hop le theo severity.

### 5.5 Khong Doc Duoc Bien So

1. OCR timeout hoac khong co plate.
2. UI hien `Khong doc duoc bien so`.
3. Bao ve chon `Nhap bien so thu cong`.
4. Form:
   - Bien so nhap tay.
   - Loai xe neu can.
   - Ly do.
   - Xac nhan trach nhiem.
5. API normalize bien so, doi chieu DB.
6. Neu hop le:
   - Ghi `CapturedLicensePlate`.
   - `InputMode = MANUAL`.
   - `FailureMode = PLATE_UNREADABLE`.
   - `IsBypass = true`.

### 5.6 OCR Doc Sai Bien So

1. UI hien raw OCR plate.
2. Bao ve sua bien so truoc khi confirm.
3. API ghi ca:
   - `RawDetectedPlate`
   - `CorrectedPlate`
   - `CorrectionByUserId`
   - `CorrectionReason`
4. Khong ghi de lam mat du lieu OCR goc.

### 5.7 Camera/Python/go2rtc Bi Loi

1. Health wrapper phat hien QR camera/plate camera/Python/go2rtc offline, degraded, stale hoac timeout.
2. UI lane hien `DEVICE DEGRADED`.
3. Bao ve co the bam `Bat che do thu cong`.
4. Manual session bat buoc:
   - Gate/lane.
   - Thiet bi loi.
   - Ly do.
   - Thoi gian toi da.
5. Moi luot xu ly trong manual session deu ghi audit.
6. Neu so luot manual vuot nguong, tu dong tao case cho quan ly.

### 5.8 Cap Quyen Khan Cap Ngay Lap Tuc

Ap dung khi co luc luong/chuc nang can vao ngay:

- Cong an/canh sat.
- Cap cuu/y te.
- PCCC/cuu hoa.
- Cuu ho/cuu nan.
- Doi ung cuu su co khan cap duoc cong ty quy dinh.
- Lanh dao/ban chi huy khan cap cua cong ty.

Nguyen tac:

1. Khong bat doi quy trinh dang ky, phe duyet truoc, QR hay bien so day du.
2. Bao ve/Admin duoc kich hoat `Emergency Immediate Access` ngay tai man hinh quet.
3. Nguoi kich hoat phai dang nhap bang tai khoan ca nhan.
4. Bat buoc nhap mo ta ly do.
5. Neu co the, nhap thong tin toi thieu:
   - Don vi/nhom vao: cong an, cap cuu, PCCC, cuu ho.
   - So nguoi uoc tinh.
   - Bien so/ma xe neu nhin thay.
   - Khu vuc/cua/cam can mo.
6. He thong lap tuc:
   - Ghi access log `EMERGENCY_GRANTED`.
   - Tao exception case `EMERGENCY_IMMEDIATE_ACCESS`.
   - Tao alarm severity `Critical`.
   - Broadcast thong bao cho toan bo nhan vien cong ty.
   - Ghi audit before/after.
7. Sau su kien, quan ly/SOC bat buoc hau kiem va dong case.

UI tai chot:

- Nut rieng mau canh bao: `Cap quyen khan cap`.
- Modal xac nhan ngan gon, uu tien toc do:
  - Loai tinh huong.
  - Khu vuc/cua/cam can mo.
  - Mo ta ly do bat buoc.
  - Checkbox: `Toi xac nhan day la tinh huong khan cap va chiu trach nhiem voi thao tac nay`.
- Sau khi xac nhan, UI khong bat bao ve roi man hinh quet.

Trang quan ly/SOC:

- Case hien trong queue `Critical`.
- Hien banner/bao dong rieng.
- Bat buoc cap quan ly xac nhan sau su kien:
  - Hop le.
  - Can bo sung thong tin.
  - Sai quy trinh/lap bien ban.

Broadcast toan cong ty:

- Banner tren web app.
- Notification trong header.
- Co the bo sung email/SMS/webhook sau.
- Noi dung can co:
  - Loai khan cap.
  - Khu vuc lien quan.
  - Nguoi kich hoat.
  - Thoi gian.
  - Huong dan ngan: tranh khu vuc / ho tro / so tan neu can.

## 6. Trang Ngoai Le Cho Quan Ly

`Exceptions.vue` can duoc nang cap thanh workspace case:

- Tab `Cho xu ly`.
- Tab `Dang escalate`.
- Tab `Bao dong khan cap`.
- Tab `Can bo sung thong tin`.
- Tab `Da duyet`.
- Tab `Da tu choi`.
- Tab `Da dong`.

Quan ly co the:

- Xem timeline case.
- Xem access log lien quan.
- Xem before/after vehicle state.
- Xem QR metadata, plate raw/corrected.
- Xem device health tai thoi diem loi.
- Duyet, tu choi, yeu cau bo sung.
- Dong case.
- Xuat bao cao.
- Xem va dong cac case khan cap sau hau kiem.

Bao ve chi can trang nay de xem lai neu can, khong dung lam man hinh xu ly tuc thoi tai chot.

## 7. Data Model De Xuat

### 7.1 `AccessExceptionCases`

Truong de xuat:

- `ExceptionCaseId`
- `CorrelationId`
- `ScanSessionId`
- `IdempotencyKey`
- `ExceptionCode`
- `Severity`
- `Status`
- `ResolutionMode`: `GUARD_INLINE`, `MANAGER_CASE`
- `EmergencyMode`: `None`, `ImmediateAccess`, `Evacuation`, `Lockdown`, `Shelter`, `FireOverride`
- `InputMode`: `AUTO`, `ASSISTED`, `MANUAL`
- `FailureMode`
- `SubjectType`: `Employee`, `Guest`, `Vehicle`
- `SubjectId`
- `VehicleId`
- `LicensePlate`
- `GateId`
- `CameraId`
- `AccessLogId`
- `CurrentStateJson`
- `RequestedStateJson`
- `DeviceHealthSnapshotJson`
- `RawQrPayload`
- `ManualIdentityInput`
- `RawDetectedPlate`
- `CorrectedPlate`
- `ManualPlateInput`
- `DetectedAtUtc`
- `CreatedByUserId`
- `AssignedToUserId`
- `ResolvedByUserId`
- `ResolvedAtUtc`
- `ResolutionAction`
- `ResolutionNote`
- `RequiresManagerApproval`
- `ManagerDecisionByUserId`
- `ManagerDecisionAtUtc`
- `ManagerDecisionNote`
- `BroadcastRequired`
- `BroadcastSentAtUtc`
- `EmergencyType`
- `EmergencyScopeJson`

### 7.2 `AccessExceptionCaseEvents`

Truong de xuat:

- `CaseEventId`
- `ExceptionCaseId`
- `EventType`: `Created`, `Acknowledged`, `InlineResolved`, `Escalated`, `Approved`, `Denied`, `StateRepaired`, `Closed`
- `EventType` bo sung: `EmergencyActivated`, `EmergencyBroadcastSent`, `EmergencyReviewed`
- `ActorUserId`
- `EventAtUtc`
- `BeforeJson`
- `AfterJson`
- `Note`
- `EvidenceRef`

### 7.3 `ManualOperationSessions`

Truong de xuat:

- `ManualOperationSessionId`
- `GateId`
- `LaneId`
- `StartedByUserId`
- `StartedAtUtc`
- `EndedAtUtc`
- `Reason`
- `DeviceFailureMode`
- `MaxDurationMinutes`
- `Status`
- `TotalManualPasses`

### 7.4 `EmergencyAccessEvents`

Truong de xuat:

- `EmergencyAccessEventId`
- `CorrelationId`
- `ActivatedByUserId`
- `ActivatedAtUtc`
- `EmergencyType`: `Police`, `Ambulance`, `FireDepartment`, `Rescue`, `InternalCommand`, `Other`
- `ScopeType`: `Gate`, `Zone`, `Site`, `AllCompany`
- `ScopeJson`
- `Reason`
- `EstimatedPeopleCount`
- `VehiclePlate`
- `AccessPointId`
- `GateId`
- `SecurityZoneId`
- `Status`: `Active`, `Ended`, `UnderReview`, `Closed`
- `EndedAtUtc`
- `ReviewedByUserId`
- `ReviewedAtUtc`
- `ReviewConclusion`
- `RelatedExceptionCaseId`
- `BroadcastMessage`
- `BroadcastSentAtUtc`

### 7.5 `CompanyWideAlerts`

Truong de xuat:

- `CompanyWideAlertId`
- `AlertType`: `EmergencyAccess`, `Evacuation`, `Lockdown`, `SystemCritical`
- `Severity`: `High`, `Critical`
- `Title`
- `Message`
- `ScopeType`: `Company`, `Site`, `Zone`
- `ScopeId`
- `CreatedByUserId`
- `CreatedAtUtc`
- `ExpiresAtUtc`
- `AcknowledgementRequired`
- `Status`: `Active`, `Expired`, `Closed`
- `RelatedEmergencyAccessEventId`

## 8. API Can Them Hoac Nang Cap

### 8.1 Nang Cap Scan

`POST /api/gate-transit/scan`

Them request fields:

- `IntendedDirection`: `IN`/`OUT`
- `ScanSessionId`
- `IdempotencyKey`
- `CorrelationId`
- `InputMode`
- `RawDetectedPlate`
- `CorrectedPlate`
- `ManualPlateInput`
- `ManualIdentityInput`

Khong duoc tiep tuc toggle `IN/OUT` mu.

### 8.2 Resolve Inline

`POST /api/gate-transit/exceptions/{caseId}/resolve-inline`

Role: `Admin,BaoVe`

Actions:

- `DENY`
- `CONFIRM_AND_ALLOW`
- `REPAIR_STATE_AND_ALLOW_IN`
- `REPAIR_STATE_AND_ALLOW_OUT`
- `ESCALATE`

Bat buoc:

- `ResolutionNote`
- `GuardConfirmed = true`
- `IdempotencyKey`

### 8.3 Manual Operation

Endpoints:

- `POST /api/operations/manual-sessions/start`
- `POST /api/operations/manual-sessions/{id}/end`
- `POST /api/gate-transit/manual-admit`
- `POST /api/gate-transit/manual-exit`
- `POST /api/gate-transit/plate-correction`

### 8.4 Manager Case

Endpoints:

- `GET /api/access-exception-cases`
- `GET /api/access-exception-cases/{id}`
- `POST /api/access-exception-cases/{id}/manager-decision`
- `POST /api/access-exception-cases/{id}/close`

### 8.5 Emergency Immediate Access

Endpoints:

- `POST /api/emergency-access/activate`
- `POST /api/emergency-access/{id}/end`
- `POST /api/emergency-access/{id}/review`
- `GET /api/emergency-access/active`
- `GET /api/company-wide-alerts/active`
- `POST /api/company-wide-alerts/{id}/acknowledge`

`POST /api/emergency-access/activate` bat buoc:

- `EmergencyType`
- `ScopeType`
- `ScopeId` hoac `ScopeJson`
- `Reason`
- `GuardConfirmed = true`
- `CorrelationId`
- `IdempotencyKey`

Response phai tra ngay:

- `EmergencyAccessEventId`
- `RelatedExceptionCaseId`
- `CompanyWideAlertId`
- `BroadcastStatus`
- `ActivatedBy`
- `ActivatedAtUtc`

## 9. UI Can Nang Cap

### 9.1 `GateTransitMonitor.vue`

Them:

- Inline exception modal/drawer.
- Manual mode toggle.
- Manual QR identity input.
- Manual plate input.
- OCR correction field.
- Device degraded banner.
- Lane-level case timeline.
- Action buttons theo policy:
  - `Tu choi`
  - `Cho qua`
  - `Sua DB va cho vao`
  - `Sua DB va cho ra`
  - `Chuyen quan ly`
- Nut rieng `Cap quyen khan cap` luon hien voi role duoc phep, khong phu thuoc camera/QR/plate.

### 9.2 `QrAccessMonitor.vue`

Them:

- QR unreadable fallback.
- Manual identity lookup.
- Restricted-zone override modal.
- Escalate action neu vuot quyen.

### 9.3 `DynamicQrScanner.vue`

Them:

- Hien thi reason khi QR invalid.
- Phan biet `expired`, `unreadable`, `replay`, `identity mismatch`.
- Manual fallback chi hien voi reason duoc policy cho phep.

### 9.4 `Exceptions.vue`

Chuyen thanh case workspace:

- Queue.
- Detail.
- Timeline.
- Decision panel.
- Evidence panel.
- Audit panel.
- Emergency alarm panel.
- Company-wide broadcast status.

Mac dinh khong hien nhu mot bang log. Phai la `Approval Decision Queue` giup quan ly/admin nhin ngay viec nao can quyet truoc.

Thu tu uu tien mac dinh:

1. `CriticalActive`: emergency dang dien ra, duress, forced-open, lockdown, workplace violence.
2. `BlockingOperations`: case dang chan cong, chan bai xe, chan khu vuc hoac bao ve dang doi quyet.
3. `HighRisk`: watchlist, blacklist, QR replay, identity mismatch, sensitive zone.
4. `SlaBreaching`: case sap qua SLA hoac da qua SLA.
5. `MediumOperational`: DB lech, manual mode keo dai, visitor overstay, device degraded.
6. `LowReview`: hau kiem, drill, can bo sung thong tin, report.

Bo cuc de xuat:

- Thanh tong quan tren cung:
  - So `Critical` active.
  - So case dang chan van hanh.
  - So case sap qua SLA.
  - So emergency mode active.
  - So manual/offline session active.
- Danh sach case:
  - Sap xep theo uu tien, khong sap theo thoi gian don thuan.
  - Card moi case hien severity, loai su kien, vi tri, nguoi yeu cau, thoi gian cho, SLA, action chinh.
  - Badge `Blocking`, `Silent`, `Emergency`, `Offline`, `LegalHold`, `NeedsStepUp` neu co.
- Khung chi tiet:
  - Timeline.
  - Du lieu xac minh.
  - Rui ro va policy lien quan.
  - Audit/event lien quan.
  - Evidence/camera snapshot neu co san tu API, khong phu thuoc sua runtime.
  - Cac hanh dong hop le theo role/permission.
- Decision panel:
  - Action ro rang: `Duyet`, `Tu choi`, `Yeu cau bo sung`, `Chuyen cap`, `Dong case`.
  - Hanh dong rui ro cao bat buoc reason va step-up neu policy bat.
  - Khong hien action ma user khong co quyen.

Quy tac UX:

- Quan ly khong bi bat mo tung case de biet cai nao nguy hiem nhat.
- Case dang chan bao ve tai chot phai co countdown/SLA ro.
- Case `Critical` phai pinned tren cung cho den khi acknowledged/ended/reviewed.
- Khong tron case drill voi case that neu khong co label ro.
- Khi user duyet xong, UI phai dua ngay case tiep theo theo uu tien, khong quay ve dau trang.

### 9.5 Header/Layout Notification

Can them co che hien thi bao dong toan cong ty:

- Banner tren dau layout khi co `CompanyWideAlert` active.
- Mau/cap do rieng cho `Critical`.
- Nhan vien nao dang dang nhap cung thay.
- Neu alert yeu cau acknowledge, nhan vien phai bam `Da nhan thong bao`.
- Bao ve/quan ly/admin thay link den emergency case.

Can thiet ke theo mo hinh `Attention Without Unnecessary Interruption`:

- `Info`:
  - Badge/header indicator nhe.
  - Khong toast lap lai.
  - Khong bat acknowledge.
- `Warning`:
  - Banner mong tren dau app.
  - Co the thu gon.
  - Co link xem chi tiet neu user co quyen.
- `High`:
  - Banner ro rang, mau canh bao.
  - Toast mot lan hoac notification trong app.
  - Co nut acknowledge nhanh.
- `Critical`:
  - Banner persistent tren layout.
  - Co am/thong bao lap theo chu ky neu chua acknowledge va policy cho phep.
  - Khong ep chuyen trang tru khi la evacuation/lockdown bat buoc.
  - Noi dung can ngan: chuyen gi, o dau, can lam gi.
- `Evacuation` / `Lockdown`:
  - Overlay ban phan hoac banner lon.
  - Huong dan cuc ngan theo vai tro.
  - Nut `Da nhan thong bao`.
  - Khong hien du lieu nhay cam cho nhan vien thuong.

Noi dung alert theo role:

- Nhan vien thuong: chi thay thong tin can hanh dong, vi tri anh huong va huong dan.
- Bao ve: thay vi tri, gate/lane, action can lam.
- Quan ly/SOC: thay link case, muc rui ro, SLA, nguoi kich hoat.
- Admin: thay them thong tin he thong, policy, integration/device neu co.

Quy tac khong lam do viec:

- Alert khong duoc xoa du lieu dang nhap trong form.
- Alert khong duoc tu dong redirect user dang thao tac, tru evacuation/lockdown bat buoc.
- Acknowledge duoc bam ngay tren banner.
- Neu co nhieu alert, gom nhom theo severity va site/zone.
- Critical van hien persistent nhung khong chiem toan man hinh neu user dang nhap lieu quan trong.

### 9.6 Approval UX Va Escalation Khi Bi Bo Qua

Can co co che dam bao case quan trong khong bi chim:

- Moi case co `SlaDueAtUtc`, `BlockingOperation`, `PriorityScore`.
- PriorityScore tinh tu severity, blocking, zone risk, affected people, elapsed time, emergency mode.
- Neu case sap qua SLA:
  - Tang badge mau.
  - Gui notification cho quan ly truc ca.
- Neu qua SLA:
  - Tu dong escalate len cap cao hon.
  - Ghi event `ApprovalSlaBreached`.
- Neu khong co quan ly online:
  - Chuyen sang backup approver.
  - Neu van khong co, ap dung policy fallback: tu choi mac dinh, cho phep tam thoi, hoac emergency-only tuy loai case.
- Moi approval action phai hien hau qua truoc khi xac nhan:
  - Mo cong/cho vao vung nao.
  - Thoi han bao lau.
  - Ai chiu trach nhiem.
  - Co broadcast/hau kiem/legal hold hay khong.

### 9.7 Guard UX Khong Bi Roi Luong

Bao ve dang scan khong nen phai chuyen sang `Exceptions.vue`.

- Inline drawer/modal phai xu ly duoc Low/Medium.
- Case vuot quyen thi bam `Xin phe duyet` ngay tai man hinh scan.
- Trong luc cho duyet, lane hien trang thai `Dang cho quan ly`.
- Neu quan ly duyet, UI tai chot nhan ket qua va cho phep tiep tuc.
- Neu bi tu choi, UI hien ly do ngan va nut reset lane.
- Bao ve co the tiep tuc xu ly lane khac neu UI ho tro nhieu lane.

### 9.8 Design Token Cho Canh Bao

Can thong nhat ngon ngu mau sac/trang thai:

- `Info`: xanh/neutral, khong gay cang thang.
- `Warning`: vang/cam nhe, can chu y.
- `High`: cam/do dam, can hanh dong.
- `Critical`: do dam, persistent.
- `Silent`: khong noi bat tai chot, chi hien o SOC/quan ly.
- `Drill`: mau rieng va label `Dien tap`, khong de nham voi su co that.

Khong chi dua vao mau sac. Moi alert phai co icon/trang thai text ngan de ho tro kha nang doc va tranh nham lan.

## 10. Phan Quyen

Co che `cho qua thang` khong duoc gan theo role tho kieu "co/khong". Phai tinh theo:

- Muc rui ro cua tinh huong.
- Pham vi gate/site/zone.
- Loai doi tuong: nhan vien, khach, nha thau, vendor, luc luong khan cap.
- Du lieu xac minh duoc tai chot hay khong.
- Co phai khu vuc han che/nhay cam hay khong.
- Can hau kiem hay phe duyet truoc.

### 10.1 Nguyen Tac Phan Cap Trach Nhiem

Muc xu ly:

- `Level 1 - Low`: loi ky thuat nhe, nguoi/xe hop le ro rang, khong vao vung nhay cam.
- `Level 2 - Medium`: du lieu lech nhung bao ve xac minh duoc tai chot, can audit va hau kiem mau.
- `Level 3 - High`: vung han che, watchlist nghi ngo, HR conflict, visitor/vendor rui ro, can quan ly quyet.
- `Level 4 - Critical`: khan cap, duress, blacklist/watchlist xac nhan, forced-open, legal/evidence, can alarm va hau kiem bat buoc.

Khong ai duoc sua/xoa audit log. Neu can sua nghiep vu thi tao ban ghi dieu chinh moi, co before/after va actor.

### 10.2 Bao Ve

Bao ve la nguoi van hanh tai chot. Can co quyen xu ly nhanh, nhung trong pham vi han che.

Duoc phep:

- Xu ly inline cac case `Low`/`Medium` theo policy.
- Nhap thu cong khi QR/bien so khong doc duoc.
- Sua OCR bien so khi nhin thay bien dung.
- Sua trang thai xe khi DB lech nhung du lieu thuc te va danh tinh hop le.
- Bat manual mode ngan han khi camera/API adapter/device loi.
- Cap `Emergency Immediate Access` cho cong an, cap cuu, PCCC, cuu ho neu tinh huong can vao ngay.
- Tao incident nhanh khi co nguoi gay roi, de doa, vendor/cargo bat thuong.
- Kich hoat duress/silent alarm.
- Danh dau tailgating thu cong neu nhin thay.

Khong duoc phep:

- Cho qua `WATCHLIST_MATCH`, `BLACKLISTED_VEHICLE`, `QR_REPLAY`, `QR_IDENTITY_MISMATCH`.
- Tu pha two-person rule, tru emergency override duoc policy cho phep.
- Tu cho vao khu vuc nhay cam cao khi role/department/status conflict.
- Gia han manual mode qua nguong.
- Bat fast-lane dien rong.
- Duyet cargo/asset removal khi thieu approval.
- Dong/xoa case high-risk.
- Export evidence hoac thao tac legal hold.

Kiem soat:

- Moi override bat buoc co ly do.
- Bat buoc tick xac nhan chiu trach nhiem.
- Gioi han so luot override/gio/ca.
- Vuot nguong tu dong tao review case cho quan ly.
- Emergency duoc phep nhanh, nhung luon tao alarm/case va hau kiem.

### 10.3 Quan Ly

Quan ly la cap quyet dinh nghiep vu, hau kiem va phe duyet cac ca vuot quyen bao ve.

Duoc phep:

- Xem tat ca case trong pham vi site/zone duoc giao.
- Duyet/tu choi case escalated.
- Duyet restricted-zone access neu dung tham quyen.
- Hau kiem va dong case khan cap.
- Gia han manual mode.
- Bat/tat fast-lane theo thoi han va pham vi.
- Duyet visitor/contractor/vendor khi host khong phan hoi.
- Xu ly HR conflict trong pham vi rui ro cho phep.
- Ket thuc emergency mode neu co quyen.
- Yeu cau bo sung bang chung, ghi ket luan, dong case.

Khong nen duoc phep mac dinh:

- Vuot watchlist/blacklist da xac nhan neu khong co policy dac biet.
- Xoa/sua audit.
- Tu cau hinh policy he thong lon.
- Export evidence nhay cam neu chua co approval/step-up.

Kiem soat:

- Case `High`/`Critical` can step-up MFA neu he thong bat.
- Moi quyet dinh phai co reason code va ghi chu.
- Dong case phai du dieu kien: timeline, evidence/audit, ket luan.

### 10.4 Admin

Admin la cap cau hinh va override cuoi cung, khong nen dung de van hanh thuong ngay.

Duoc phep:

- Cau hinh role, permission, site, zone, gate, threshold.
- Cau hinh policy ngoai le, reason code, severity, manual limit.
- Cau hinh emergency mode, offline allowlist, fast-lane policy.
- Reset MFA, revoke token, khoa/mo tai khoan.
- Override case dac biet neu co ly do hop le.
- Cau hinh legal hold/evidence retention/export policy.
- Xem audit/report toan he thong.

Bat buoc kiem soat:

- Hanh dong rui ro cao can step-up MFA.
- Hanh dong xoa/purge/export evidence can approval kep neu co the.
- Khong duoc sua/xoa audit log goc.
- Moi cau hinh thay doi phai co before/after.

### 10.5 Ma Tran Quyen Cho Qua Va Override

| Tinh huong | Bao ve | Quan ly | Admin |
|---|---|---|---|
| QR khong doc duoc, danh tinh hop le, vung thuong | Cho qua co ly do | Hau kiem | Cau hinh policy |
| Bien so khong doc duoc, nhap tay | Cho qua co ly do | Hau kiem | Cau hinh policy |
| OCR sai bien so | Sua va cho qua | Hau kiem neu lap lai | Cau hinh nguong |
| DB lech trang thai gui xe, du lieu doc dung | Sua trong pham vi cong/bai | Hau kiem/duyet neu lap lai | Cau hinh policy |
| QR het han nhe do clock/device | Cho qua neu policy cho phep | Duyet neu vung han che | Cau hinh policy |
| QR replay | Khong cho qua | Escalate/xu ly case | Override cuc han neu co ly do phap ly/khan cap |
| QR sai danh tinh | Khong cho qua | Escalate/xac minh manh | Override cuc han, Critical |
| Watchlist/blacklist | Khong cho qua | Khong tu override neu khong co policy dac biet | Override cuc han, Critical |
| User terminated/suspended | Khong cho qua | Co the cap tam vung thap neu HR xac nhan | Xu ly tai khoan/policy |
| Restricted zone cao | Khong tu cho qua | Duyet neu dung tham quyen | Toan quyen co audit |
| Two-person rule | Khong tu pha | Lam nguoi xac nhan/duyet | Override co audit |
| Emergency police/ambulance/fire | Cap ngay, chiu trach nhiem | Cap/ngung/hau kiem | Cap/ngung/cau hinh |
| Duress | Kich hoat silent alarm | Nhan va dieu phoi | Nhan va dieu phoi |
| Manual mode do device/API loi | Bat ngan han | Gia han/duyet | Cau hinh nguong |
| Offline allowlist | Dung allowlist co san | Duyet reconcile | Cau hinh allowlist |
| Fast-lane gio cao diem | Khong tu bat | Bat theo pham vi/thoi han | Cau hinh policy |
| Visitor/vendor/cargo thieu approval | Ghi nhan/giu/escalate | Duyet/tu choi | Cau hinh quy trinh |
| Evidence export/legal hold | Khong | Yeu cau/xem theo quyen | Duyet/cau hinh |

### 10.6 Permission Nen Tach Rieng Trong Ma Nguon

Khong nen chi check role `Admin`, `Manager`, `Guard`. Nen co permission rieng:

- `exception.inline.resolve.low`
- `exception.inline.resolve.medium`
- `exception.escalated.decide`
- `exception.critical.override`
- `emergency.activate`
- `emergency.end`
- `duress.activate`
- `manual.mode.start`
- `manual.mode.extend`
- `fastlane.activate`
- `restricted-zone.approve`
- `two-person.authorize`
- `visitor.vendor.approve`
- `cargo.asset-release.approve`
- `offline.allowlist.use`
- `offline.reconcile.approve`
- `evidence.view`
- `evidence.export`
- `policy.configure`
- `audit.view`

Permission bi cam tuyet doi:

- `audit.modify`
- `audit.delete`

Neu co nhu cau sua sai, dung compensation event thay vi sua log goc.

## 11. Audit Bat Buoc

Moi hanh dong manual/override phai ghi:

- Actor user id.
- Role.
- Gate/lane/camera.
- Input mode.
- Failure mode.
- Exception code.
- Before state.
- After state.
- Reason note.
- Correlation id.
- Idempotency key.
- Thoi diem.
- Ket qua.
- Voi khan cap: emergency type, scope, broadcast message, broadcast sent timestamp, review conclusion.

Khong cho resolve neu thieu ly do.

## 12. Idempotency Va Chong Bam Lap

Moi scan/resolve/manual operation phai co:

- `ScanSessionId`
- `IdempotencyKey`
- `CorrelationId`

Neu request lap lai:

- API tra ket qua cu.
- Khong tao access log moi.
- Khong toggle/sua trang thai lan nua.

## 13. Nguong Canh Bao Va Chinh Sach Chong Lam Dung

Can co policy:

- Mot bao ve chi duoc manual toi da N luot/gio neu khong co phe duyet.
- Mot lane o manual qua M phut thi tao case quan ly.
- Mot camera loi qua T phut thi tao alarm thiet bi.
- Mot bien so bi sua OCR qua nhieu lan thi tao case kiem tra camera.
- Mot xe co qua nhieu state repair thi dua vao danh sach can kiem tra.
- Mot bao ve kich hoat emergency qua nhieu lan trong ca thi tu dong tao review case cho quan ly/Admin.
- Moi emergency active qua thoi gian toi da ma chua end/review thi lien tuc bao dong.

## 14. Cac Luong Chong Chiu Bo Sung Can Co

Phan nay bo sung cac tinh huong doanh nghiep vua/lon thuong gap, tru nhung ca bat buoc phai co camera nhan dien thong minh. Neu sau nay co metadata tu camera/AI thi co the nap vao, nhung ke hoach nay khong yeu cau sua `AI_Runtime/**`, `runtime/**` hay phu thuoc nhan dien camera.

### 14.1 Duress / Bi Ep Buoc Mo Cong

Ap dung khi bao ve, nhan vien hoac quan ly bi de doa, bi ep mo cong/cua/khu vuc.

Exception code can them:

- `DURESS_ACCESS_ATTEMPT`
- `DURESS_GUARD_OVERRIDE`
- `DURESS_QR_OR_PIN`

Nguyen tac:

1. Nguoi dung co the nhap ma duress/keyword duress theo cau hinh.
2. UI tai chot co the hien ket qua trung tinh de khong lam tinh huong nguy hiem hon.
3. He thong am tham tao alarm `Critical`.
4. Tao exception case cho SOC/quan ly.
5. Broadcast chi gui cho nhom co quyen ung pho, khong hien dai tra neu policy danh dau `SilentAlarm = true`.
6. Ghi audit day du: ai bi ep, gate/zone, thoi diem, nguoi/xe lien quan neu co, action da thuc hien.

UI can co:

- Truong nhap duress code/duress keyword trong man hinh quet va man hinh login van hanh neu can.
- Khong hien chu "duress" ro rang tren UI tai chot.
- SOC/quan ly thay case rieng `Duress`.

### 14.2 Tailgating Thu Cong / Nhieu Nguoi Hoac Nhieu Xe Tren Mot Luot Cap Quyen

Khong dung camera nhan dien. Chi bo sung kha nang bao ve danh dau thu cong khi nhin thay mot QR/lenh mo cong bi loi dung cho nhieu nguoi/xe.

Exception code can them:

- `TAILGATING_REPORTED_BY_GUARD`
- `MULTI_PERSON_SINGLE_QR_REPORTED`
- `MULTI_VEHICLE_SINGLE_GRANT_REPORTED`
- `WRONG_DIRECTION_ENTRY_REPORTED`

Luong:

1. Sau mot luot scan/mo cong, bao ve bam `Ghi nhan di bam duoi` neu thay bat thuong.
2. UI hien modal nhanh:
   - Loai su kien.
   - Mo ta ngan.
   - Nguoi/xe/QR lien quan neu co.
   - Muc do rui ro.
3. Neu khu vuc thuong: ghi case warning va audit.
4. Neu khu vuc han che: tao case `High` cho quan ly/SOC.
5. Khong chan luong tai chot neu bao ve dang xu ly dong nguoi/xe, nhung case phai duoc dua vao queue hau kiem.

### 14.3 Door/Barrier/Lock Bat Thuong

Ap dung cho cong, barrier, cua, relay khoa, cam bien trang thai.

Exception code can them:

- `DOOR_FORCED_OPEN`
- `DOOR_HELD_OPEN_TOO_LONG`
- `BARRIER_STUCK_OPEN`
- `BARRIER_STUCK_CLOSED`
- `LOCK_RELAY_FAILED`
- `GATE_SENSOR_CONFLICT`
- `ACCESS_GRANTED_BUT_GATE_NOT_OPENED`
- `GATE_OPENED_WITHOUT_ACCESS_LOG`

Luong:

1. Device wrapper hoac UI nhan tin hieu trang thai bat thuong.
2. API tao alarm theo severity.
3. Bao ve co the:
   - Ghi nhan dang xu ly tai chot.
   - Chuyen sang manual mode.
   - Mo/dong thu cong neu co quyen.
   - Tao yeu cau bao tri.
4. Neu barrier/cua o trang thai unsafe-open qua nguong, tu dong escalate.
5. Neu mo cong khong co access log, bat buoc tao case dieu tra.

Data can them:

- `PhysicalDeviceStateEvents`: device id, state, source, gate/lane, severity, correlation id.
- `MaintenanceTickets`: su co, thiet bi, nguoi tiep nhan, trang thai, SLA.

### 14.4 Che Do Khan Cap Cap Cong Ty

Ngoai `Emergency Immediate Access`, can co cac mode toan cong ty hoac theo site/zone.

Emergency mode can them:

- `LOCKDOWN_MODE`
- `EVACUATION_MODE`
- `FIRE_OVERRIDE_MODE`
- `SHELTER_IN_PLACE_MODE`
- `FIRST_RESPONDER_ENTRY_MODE`

Luong:

1. Nguoi co quyen kich hoat emergency mode.
2. Bat buoc chon pham vi: site, building, floor, zone, gate/lane.
3. Bat buoc nhap ly do va muc do.
4. He thong ap dung policy tam thoi:
   - Lockdown: chan vao/ra cac khu vuc chi dinh, tru nguoi duoc phep ung pho.
   - Evacuation/fire: uu tien loi thoat, tao danh sach muster.
   - Shelter-in-place: han che di chuyen, broadcast huong dan o yen tai vung an toan.
   - First responder: mo luong vao nhanh cho luc luong chuc nang.
5. Tu dong broadcast va tao case `Critical`.
6. Khi ket thuc, bat buoc hau kiem.

### 14.5 Muster / Diem Danh Trong Tinh Huong So Tan

Exception code can them:

- `MUSTER_ACCOUNTING_STARTED`
- `PERSON_MISSING_DURING_EVACUATION`
- `UNKNOWN_OCCUPANCY_STATE`
- `OCCUPANCY_STATE_CORRECTED_BY_GUARD`

Luong:

1. Khi evacuation/fire active, he thong lay danh sach nguoi/khach/nha thau dang `inside`.
2. Tao muster session theo site/zone.
3. Quan ly/bac ve diem danh:
   - Da ra ngoai.
   - Chua tim thay.
   - Khong chac trang thai.
   - Dang ho tro ung pho.
4. Neu DB lech, bao ve/quan ly duoc sua co ly do.
5. Bao cao sau su kien phai co danh sach missing/unknown/resolved.

### 14.6 Visitor/Contractor Overstay, Host Unavailable, Escort Missing

Exception code can them:

- `VISITOR_OVERSTAY`
- `CONTRACTOR_ACCESS_EXPIRED`
- `HOST_UNAVAILABLE`
- `ESCORT_REQUIRED_MISSING`
- `VISITOR_LEFT_WITHOUT_CHECKOUT`
- `VISITOR_AREA_VIOLATION`

Luong:

1. Visitor/contractor co thoi gian, host, zone, escort policy.
2. Qua gio/qua zone thi tao warning/alarm.
3. Neu host khong phan hoi trong N phut:
   - Chuyen backup host neu co.
   - Chuyen quan ly truc ca.
   - Tu choi neu la khu vuc rui ro cao.
4. Neu visitor can escort ma khong co escort, khong duoc cho vao zone han che.
5. Khi check-out thieu, he thong tao case cuoi ngay cho reception/security.

### 14.7 Lost/Stolen Credential, QR Bi Lo, Clock Skew

Ap dung rieng cho QR dong va tai khoan/mobile device.

Exception code can them:

- `QR_DEVICE_REPORTED_LOST`
- `QR_COMPROMISED`
- `DYNAMIC_QR_CLOCK_SKEW`
- `TOKEN_REVOKED_BUT_PRESENTED`
- `MULTI_DEVICE_QR_CONFLICT`
- `IMPOSSIBLE_TRAVEL_BETWEEN_GATES`

Luong:

1. Nhan vien/quan ly bao mat co the bao mat thiet bi.
2. He thong revoke QR/session/token lien quan.
3. Neu QR/token da bi revoke van duoc trinh tai cong, block va tao case.
4. Neu lech gio nhe, UI cho phep manual fallback theo policy.
5. Neu lech gio nang hoac xuat hien dong thoi o hai cong xa nhau, escalate.
6. Audit phai luu server time, client/device time neu co, delta, gate/lane.

### 14.8 HR Lifecycle Va Quyen Bi Lech

Exception code can them:

- `TERMINATED_USER_ACCESS_ATTEMPT`
- `HR_SYNC_DELAY`
- `ROLE_CHANGED_ACCESS_CONFLICT`
- `DEPARTMENT_TRANSFER_PENDING`
- `USER_STATUS_CONFLICT`

Luong:

1. Access decision phai kiem tra trang thai user: active, suspended, terminated, pending transfer.
2. Neu user terminated/suspended nhung van quet QR: block va tao High/Critical case.
3. Neu HR sync tre nhung quan ly xac nhan hop le:
   - Chi cho phep vung rui ro thap/trung binh.
   - Bat buoc ly do va nguoi chiu trach nhiem.
4. Khu vuc nhay cam khong cho guard tu quyet neu role/department conflict.

### 14.9 Two-Person Rule Cho Khu Vuc Nhay Cam

Exception code can them:

- `TWO_PERSON_RULE_REQUIRED`
- `SECOND_AUTHORIZER_MISSING`
- `SENSITIVE_ZONE_SOLO_ENTRY`
- `TWO_PERSON_RULE_EMERGENCY_OVERRIDE`

Luong:

1. Zone co policy `RequireTwoPersonRule = true`.
2. User thu nhat quet hop le, he thong tao pending grant.
3. User thu hai co quyen xac nhan trong thoi gian ngan.
4. Neu qua timeout, grant bi huy.
5. Neu emergency override, cho qua nhung tao Critical case va broadcast theo policy.

### 14.10 Shift Handover / Ban Giao Ca

Exception code can them:

- `SHIFT_HANDOVER_REQUIRED`
- `OPEN_CASES_AT_SHIFT_END`
- `MANUAL_MODE_ACTIVE_AT_SHIFT_END`
- `UNACKNOWLEDGED_ALARMS_AT_SHIFT_END`

Luong:

1. Moi ca truc co start/end va danh sach nhan su.
2. Cuoi ca, he thong bat buoc hien:
   - Case dang mo.
   - Emergency active.
   - Manual session dang chay.
   - Device degraded/offline.
   - Visitor/vehicle chua checkout.
3. Nguoi giao ca nhap ghi chu.
4. Nguoi nhan ca xac nhan.
5. Neu ca ket thuc ma khong ban giao, tao case cho quan ly.

### 14.11 Network/API/Database Offline

Exception code can them:

- `API_UNAVAILABLE`
- `DATABASE_UNAVAILABLE`
- `NETWORK_PARTITION`
- `OFFLINE_ALLOWLIST_MODE`
- `OFFLINE_EVENT_RECONCILIATION`
- `OFFLINE_CONFLICT_REQUIRES_REVIEW`

Luong:

1. UI/API health phat hien mat ket noi.
2. Neu co offline allowlist package da ky, cho phep van hanh gioi han.
3. Chi cho:
   - Emergency.
   - Bao ve/quan ly truc ca.
   - Nhan su thiet yeu.
   - Danh sach allowlist theo policy.
4. Moi event offline vao queue co idempotency key.
5. Khi online lai, API reconcile:
   - Event hop le thi ghi log chinh thuc.
   - Conflict thi tao case.
6. Khong cho offline mode keo dai qua thoi gian cau hinh neu khong co manager approval.

### 14.12 Queue Surge / Un Tac Gio Cao Diem

Exception code can them:

- `HIGH_QUEUE_PRESSURE`
- `TEMPORARY_FAST_LANE_POLICY`
- `BULK_ENTRY_MODE_ACTIVATED`
- `FAST_LANE_REVIEW_REQUIRED`

Luong:

1. Supervisor/manager kich hoat fast-lane theo gate/lane va thoi gian.
2. Fast-lane chi ap dung voi doi tuong rui ro thap.
3. Co the cho phep:
   - QR hop le nhung plate ghi bo sung sau.
   - Guard confirm nhanh voi ly do mac dinh theo ca.
4. Tat ca luot fast-lane can hau kiem mau sau ca.
5. Neu phat hien watchlist/blacklist/replay thi khong duoc fast-lane.

### 14.13 Workplace Violence / Nguoi Gay Roi Tai Cong

Exception code can them:

- `AGGRESSIVE_PERSON_AT_GATE`
- `WORKPLACE_VIOLENCE_THREAT`
- `TRESPASSER_REFUSES_TO_LEAVE`
- `SECURITY_ESCORT_REQUESTED`
- `LAW_ENFORCEMENT_REQUESTED`

Luong:

1. Bao ve co nut tao incident nhanh tai man hinh quet.
2. Khong bat buoc co QR/bien so.
3. Form toi thieu:
   - Vi tri.
   - Muc do.
   - Mo ta ngan.
   - Can ho tro hay khong.
4. Neu severity cao, broadcast cho nhom ung pho hoac toan cong ty theo policy.
5. Case gan lien voi emergency/workplace violence report de hau kiem.

### 14.14 Delivery/Vendor/Cargo/Asset Removal

Exception code can them:

- `DELIVERY_WITHOUT_PRE_REGISTRATION`
- `VENDOR_REQUIRES_ESCORT`
- `CARGO_EXIT_REQUIRES_APPROVAL`
- `ASSET_REMOVAL_WITHOUT_PASS`
- `VENDOR_OVERSTAY`

Luong:

1. Xe giao hang/vendor co loai luong rieng, khong dung chung hoan toan voi nhan vien.
2. Vao cong can ghi:
   - Don vi.
   - Tai xe/nguoi dai dien.
   - Bien so.
   - Muc dich.
   - Khu vuc den.
   - Host/nguoi tiep nhan.
3. Ra cong neu co hang/tai san:
   - Can phieu xuat/approval.
   - Neu thieu, bao ve giu xe va escalate.
4. Vendor can escort ma khong co escort thi khong duoc vao zone han che.

### 14.15 Evidence Retention / Legal Hold / Chain Of Custody

Exception code can them:

- `AUTO_LEGAL_HOLD_ON_CRITICAL_INCIDENT`
- `EVIDENCE_RETENTION_POLICY_APPLIED`
- `EVIDENCE_EXPORT_APPROVAL_REQUIRED`
- `EVIDENCE_CHAIN_OF_CUSTODY_EVENT`

Luong:

1. Moi case `Critical` tu dong bat legal hold.
2. Khong cho xoa/sua evidence/log lien quan neu legal hold active.
3. Moi lan xem/tai/export/chia se bang chung phai ghi custody event.
4. Export can approval voi reason.
5. File export can co watermark/signature/hash neu he thong ho tro.

### 14.16 Drill Mode / Dien Tap Va Hau Kiem Sau Su Co

Exception code can them:

- `DRILL_MODE_STARTED`
- `DRILL_MODE_ENDED`
- `POST_INCIDENT_REVIEW_REQUIRED`
- `LESSON_LEARNED_ACTION_ITEM`

Luong:

1. Quan ly/Admin co the bat drill mode theo site/zone.
2. Drill mode khong lam ban du lieu van hanh that:
   - Case co `IsDrill = true`.
   - Alarm co `IsDrill = true`.
   - Broadcast hien ro la dien tap.
3. Sau su co that hoac dien tap, he thong bat buoc tao post-incident review.
4. Review gom:
   - Timeline.
   - Dieu lam dung.
   - Diem cham/loi.
   - Action item.
   - Owner va deadline.

### 14.17 Common Operating Picture / Ban Do Dieu Phoi Toan Cong Ty

Khong yeu cau camera nhan dien. Day la man hinh tong hop tu du lieu he thong hien co: gate, zone, access log, device health, manual session, emergency mode, case va occupancy.

Can co:

- So do site/building/floor/zone dang quan ly.
- Trang thai gate/lane/barrier/cua: online, degraded, offline, unsafe-open, unsafe-closed.
- So nguoi/xe/khach/vendor dang `inside` theo zone.
- Case `Critical`/`High` gan voi vi tri.
- Emergency mode dang active theo pham vi.
- Manual/offline/fast-lane session dang active.
- Guard/manager dang phu trach ca truc neu co.

Luong:

1. SOC/quan ly mo dashboard dieu phoi.
2. Click vao zone/gate de xem:
   - Alarm/case lien quan.
   - Occupancy.
   - Device health.
   - Access event gan nhat.
   - Action hop le theo quyen.
3. Khi co Critical case, dashboard ghim vi tri va hien huong dan/SOP lien quan.

Gia tri: mot man hinh biet cong ty dang o trang thai nao, thay vi di tim tung trang.

### 14.18 SOP Playbook / Huong Dan Phan Ung Theo Su Co

Exception/code can them:

- `SOP_STARTED`
- `SOP_STEP_COMPLETED`
- `SOP_STEP_SKIPPED`
- `SOP_REQUIRED_STEP_MISSING`
- `SOP_COMPLETED`

Can co:

- Mau SOP theo incident type: fire, lockdown, duress, forced-open, lost QR, visitor overstay, blacklist, evacuation.
- Cac buoc bat buoc va tuy chon.
- Role phu trach moi buoc.
- SLA cho tung buoc neu can.
- Dieu kien de dong case.

Luong:

1. Case duoc tao thi auto gan SOP phu hop.
2. Quan ly/SOC xem checklist phan ung.
3. Moi buoc duoc tick, skip co ly do, hoac escalate.
4. Case khong duoc dong neu thieu buoc bat buoc.
5. Audit ghi actor, thoi gian, ket qua moi buoc.

Gia tri: he thong khong chi bao dong, ma huong nguoi van hanh phan ung dung quy trinh.

### 14.19 Runbook Automation / Macro Nghiep Vu

Khong dung automation cam nhan dien. Chi la macro dieu phoi nghiep vu trong API/UI.

Runbook can co:

- `ActivateLockdownRunbook`
- `ActivateEvacuationRunbook`
- `LostQrDeviceRunbook`
- `DoorHeldOpenRunbook`
- `VisitorOverstayRunbook`
- `ManualModeEscalationRunbook`
- `LegalHoldRunbook`

Luong:

1. Event thoa dieu kien kich hoat runbook.
2. Runbook tao case, broadcast, SOP, audit, task cho nguoi phu trach.
3. Runbook khong thuc hien lenh nguy hiem neu thieu permission/approval.
4. Moi action co correlation id va idempotency.
5. Quan ly/Admin co the xem runbook da lam gi va rollback buoc cau hinh neu hop le.

Gia tri: giam thao tac lap lai, tranh quen buoc trong su co lon.

### 14.20 Mobile Response / Van Hanh Tu Thiet Bi Di Dong

Khong can app mobile rieng neu chua co, nhung web phai responsive cho cac luong chinh.

Can co:

- Man hinh guard mobile: case duoc giao, ghi chu hien truong, incident nhanh, emergency acknowledge.
- Man hinh manager mobile: duyet/tua choi case, xem severity/SLA, step-up, acknowledge alert.
- Chup/attach anh hien truong neu web/app cho phep, luu nhu evidence metadata.
- Handover ca tren mobile.
- Notification/toast than thien voi man hinh nho.

Nguyen tac:

- Nut action chinh to, ro, khong qua nhieu lua chon.
- Critical action van bat reason/confirm.
- Khong hien bang qua rong tren mobile; dung card/list/detail.

### 14.21 Executive Risk Dashboard / Bao Cao Lanh Dao

Can co dashboard cho lanh dao/quan ly cap cao, khong di vao tung log.

Chi so can co:

- Tong incident theo thang/quy.
- Incident theo severity.
- Top gate/zone co rui ro.
- Top ly do override/manual.
- SLA xu ly case.
- Device downtime.
- Manual/offline/fast-lane usage.
- Visitor/vendor compliance.
- Emergency/drill readiness.
- Guard override rate.
- Legal hold/evidence export count.

Gia tri: bien he thong thanh cong cu quan tri rui ro, khong chi cong cu quet QR.

### 14.22 Compliance Pack / Goi Bao Cao Tuan Thu

Report can co:

- Access audit theo nguoi/zone/thoi gian.
- Sensitive-zone access report.
- Visitor/vendor lifecycle report.
- Emergency/drill report.
- Manual override report.
- Role/permission change report.
- User inactive nhung con quyen.
- Evidence/legal hold/custody report.
- Device health/SLA report.

Nguyen tac:

- Report co filter theo site/zone/time/severity.
- Export can ghi audit.
- Report nhay cam can permission rieng.
- Report nen co summary va chi tiet.

### 14.23 Training/Certification Gate Cho Khach, Nha Thau, Vendor

Exception code can them:

- `SAFETY_BRIEFING_REQUIRED`
- `NDA_REQUIRED`
- `CERTIFICATION_EXPIRED`
- `CONTRACTOR_PERMIT_MISSING`

Luong:

1. Visitor/contractor/vendor co profile compliance.
2. Truoc khi cap QR/access:
   - Kiem tra NDA.
   - Kiem tra safety briefing.
   - Kiem tra chung chi/giay phep neu vao zone dac thu.
3. Neu thieu:
   - Chan cap quyen.
   - Cho phep manager override co ly do neu policy cho phep.
4. Khi het han, tu dong thu hoi access lien quan.

Gia tri: giai quyet bai toan nha thau/khach vao cong ty nhung chua du dieu kien an toan.

### 14.24 Permit-To-Work / Giay Phep Lam Viec Rui Ro

Exception code can them:

- `PERMIT_REQUIRED`
- `PERMIT_EXPIRED`
- `PERMIT_SCOPE_MISMATCH`
- `AFTER_HOURS_WORK_REQUIRES_APPROVAL`
- `HOT_WORK_REQUIRES_APPROVAL`
- `SERVER_ROOM_WORK_REQUIRES_APPROVAL`

Ap dung cho:

- Lam viec ngoai gio.
- Thi cong/sua chua trong khu vuc han che.
- Vao phong server/kho/ky thuat.
- Hanh dong co rui ro: han cat, dien, bao tri cong/cua, mang thiet bi vao/ra.

Luong:

1. Tao permit co thoi gian, zone, nguoi phu trach, nha thau/nhan vien, loai viec.
2. Quan ly duyet permit.
3. Khi quet QR/vao zone, access decision kiem tra permit.
4. Permit het han/sai pham vi thi block/escalate.
5. Ket thuc permit phai co check-out/close note.

### 14.25 Asset Custody / Tai San Di Kem Nguoi Va Xe

Exception code can them:

- `ASSET_CHECKIN_REQUIRED`
- `ASSET_CHECKOUT_MISMATCH`
- `ASSET_REMOVAL_WITHOUT_PASS`
- `SENSITIVE_ASSET_REQUIRES_APPROVAL`

Can co:

- Dang ky laptop, thiet bi, cong cu, hang mau, hang hoa.
- Gan asset voi nguoi/xe/visitor/vendor.
- Check-in/check-out asset tai cong.
- Approval cho tai san nhay cam.
- Case khi ra cong thieu phieu hoac sai danh sach.

Gia tri: giam that thoat tai san va lam luong vendor/cargo that hon.

### 14.26 Identity Risk Score / Diem Rui Ro Truy Cap

Khong can AI. Co the la rule-based score.

Diem tang khi:

- Hay bi QR expired/unreadable.
- Hay can guard override.
- Vao ngoai gio.
- Vao zone la/zone nhay cam.
- Visitor overstay.
- Vehicle state mismatch lap lai.
- QR/device tung bao mat.
- Cung identity co su kien bat thuong o nhieu gate.
- HR status conflict.

Luong:

1. Moi access decision tinh `IdentityRiskScore`.
2. Score chi dung de sap xep uu tien/escalate, khong tu dong ket luan gian lan.
3. Score cao thi bat step-up, manager approval hoac hau kiem.
4. UI phai hien ly do score tang de tranh hop den.

### 14.27 Health And Safety Operations

Mo rong tu security sang safety nhung van nam trong van hanh cong ty.

Exception code can them:

- `MEDICAL_EMERGENCY`
- `WORKPLACE_ACCIDENT`
- `SAFETY_VIOLATION`
- `HAZARD_ZONE_ACTIVE`
- `CONTRACTOR_SAFETY_NON_COMPLIANCE`

Luong:

1. Tao incident safety nhanh tai cong/khu vuc.
2. Gan zone nguy hiem va broadcast theo pham vi.
3. Neu zone nguy hiem active, access decision can canh bao hoac chan vao theo policy.
4. Ket noi voi muster/emergency/drill/post-incident review.

### 14.28 Shift Performance Va Accountability Dashboard

Can co bao cao theo ca truc:

- So case xu ly.
- So override/manual entry.
- Thoi gian phan hoi trung binh.
- SLA bi tre.
- Case chua ban giao.
- Device loi trong ca.
- Emergency/duress/workplace violence trong ca.
- Ghi chu truong ca.

Gia tri: quan ly duoc chat luong van hanh va trach nhiem ca truc.

### 14.29 Investigation Workspace / Khong Gian Dieu Tra

Khong can video analytics. Tap trung vao lien ket du lieu san co.

Can co:

- Tim theo nguoi, xe, QR, bien so, zone, gate, case.
- Timeline tong hop access log, visitor log, vehicle log, exception case, evidence, audit.
- Cau hoi mau:
  - Ai vao phong server toi qua?
  - Xe nay da vao/ra nhung luc nao trong 3 thang?
  - QR/token nay da bi revoke nhung co xuat hien nua khong?
  - Bao ve nao da override cho identity nay?
- Export investigation package co audit.

### 14.30 Policy Simulation / Shadow Policy

Can co de tranh cau hinh sai lam tac van hanh.

Luong:

1. Admin tao policy moi o trang thai `Draft`.
2. Chay simulation tren access log/demo data/du lieu lich su.
3. He thong bao:
   - Bao nhieu luot se bi chan them.
   - Bao nhieu luot se duoc cho qua them.
   - Zone/gate bi anh huong.
   - Rui ro/exception moi phat sinh.
4. Quan ly/Admin review ket qua roi moi approve/activate.
5. Sau khi activate, co rollback va audit.

### 14.31 Data Quality Center / Trung Tam Chat Luong Du Lieu

Can co de xu ly goc re cac loi DB lech, QR sai, xe sai trang thai.

Loai van de can phat hien:

- Duplicate identity.
- Duplicate plate.
- Vehicle stuck `IN` qua thoi gian bat thuong.
- Visitor chua checkout.
- Contractor/vendor het han nhung con access.
- User inactive/terminated nhung con quyen.
- QR/token revoked nhung van xuat hien.
- Device hay sinh loi.
- Plate correction lap lai.
- Manual override lap lai theo cung nguoi/xe.

Luong:

1. Job hoac API tong hop data quality issue.
2. Quan ly/Admin xem queue data quality.
3. Moi issue co owner, severity, suggested fix, deadline.
4. Khi fix, tao audit before/after.
5. Nhung issue anh huong access decision phai duoc uu tien cao.

## 15. Thu Tu Trien Khai

1. Them constants/enum exception code, severity, input mode, failure mode.
2. Tao migration cho `AccessExceptionCases`, `AccessExceptionCaseEvents`, `ManualOperationSessions`.
3. Tao `AccessExceptionWorkflowService`.
4. Nang `GateTransitController` bo toggle mu, them `IntendedDirection`.
5. Them idempotency/correlation handling.
6. Them endpoint resolve inline.
7. Them endpoint manual operation.
8. Them endpoint manager case.
9. Them endpoint emergency immediate access va company-wide alerts.
10. Nang `GateTransitMonitor.vue` voi inline modal, manual mode va emergency button.
11. Nang `QrAccessMonitor.vue` voi manual identity fallback va emergency button.
12. Nang `DynamicQrScanner.vue` de phan loai QR error.
13. Nang `Exceptions.vue` thanh case workspace.
14. Them banner alert toan cong ty trong layout/header.
15. Them cac model/service bo sung:
    - `PhysicalDeviceStateEvents`
    - `MaintenanceTickets`
    - `EmergencyModeSessions`
    - `MusterSessions`
    - `ShiftHandoverSessions`
    - `OfflineOperationQueue`
    - `EvidenceCustodyEvents`
    - `PostIncidentReviews`
    - `OperatingPictureSnapshots`
    - `SopPlaybooks`
    - `SopStepEvents`
    - `RunbookExecutions`
    - `RiskDashboardMetrics`
    - `ComplianceReportDefinitions`
    - `TrainingCertificationRecords`
    - `WorkPermits`
    - `AssetCustodyRecords`
    - `IdentityRiskScores`
    - `SafetyIncidents`
    - `InvestigationWorkspaces`
    - `PolicySimulationRuns`
    - `DataQualityIssues`
16. Them API cho duress, emergency mode, muster, shift handover, offline queue, vendor/delivery va evidence custody.
17. Them API cho SOP playbook, runbook automation, common operating picture, risk dashboard va compliance report.
18. Them API cho training/certification, permit-to-work, asset custody, identity risk score, safety incident, investigation, policy simulation va data quality.
19. Them UI nhanh tai man hinh quet cho duress, incident nhanh, device fault, delivery/vendor va fast-lane.
20. Nang `Exceptions.vue` thanh approval decision queue co priority, SLA, blocking indicator va decision panel.
21. Them alert attention model trong layout/header: Info, Warning, High, Critical, Evacuation/Lockdown.
22. Them common operating picture dashboard cho SOC/quan ly.
23. Them workspace quan ly cho muster, emergency mode, shift handover, legal hold, SOP, runbook va post-incident review.
24. Them dashboard/report cho lanh dao, compliance, shift performance, data quality va investigation.
25. Them demo data cho cac case:
    - DB lech.
    - QR unreadable.
    - Plate unreadable.
    - Plate corrected.
    - Camera offline.
    - Watchlist escalate.
    - Emergency police/ambulance/fire access.
    - Duress silent alarm.
    - Door/barrier stuck.
    - Evacuation muster.
    - Visitor overstay.
    - Lost QR device.
    - HR status conflict.
    - Two-person rule.
    - Shift handover.
    - Offline allowlist.
    - Queue surge fast-lane.
    - Workplace violence incident.
    - Vendor cargo exit approval.
    - Legal hold/evidence custody.
    - Drill mode.
    - Approval queue blocking case.
    - Critical company-wide alert.
    - SLA breach escalation.
    - Common operating picture site/zone/device/case.
    - SOP fire/duress/lost QR/visitor overstay.
    - Runbook lockdown/lost QR/legal hold.
    - Executive dashboard metrics.
    - Compliance report pack.
    - Contractor certification expired.
    - Permit-to-work after-hours/server-room.
    - Asset custody mismatch.
    - Identity risk score escalation.
    - Safety incident/hazard zone.
    - Shift performance dashboard.
    - Investigation workspace query.
    - Shadow policy simulation.
    - Data quality issue queue.
26. Them audit va reporting.
27. Chay build/test/smoke.

## 16. Checklist Nghiem Thu

- Xe vao binh thuong pass.
- Xe ra binh thuong pass.
- Xe vao nhung DB dang `IN` khong bi toggle sai.
- Bao ve sua DB va cho vao duoc khi policy cho phep.
- QR khong doc duoc van nhap thu cong duoc.
- Bien so khong doc duoc van nhap thu cong duoc.
- OCR sai, bao ve sua bien va log luu raw/corrected.
- Camera/Python/go2rtc loi, lane vao manual session duoc.
- Manual session het han hoac qua nguong tao case quan ly.
- QR replay bi chan/escalate.
- Watchlist/blacklist bat buoc quan ly/Admin.
- Canh sat/cap cuu/PCCC vao khan cap duoc cap quyen ngay.
- Emergency access bat buoc co ly do va nguoi chiu trach nhiem.
- Emergency access tao alarm `Critical`.
- Toan bo nhan vien dang nhap nhin thay broadcast.
- Quan ly/SOC hau kiem va dong emergency case duoc.
- Bao ve chi override duoc case `Low`/`Medium` trong pham vi policy.
- Bao ve bi chan khi co watchlist/blacklist, QR replay, QR identity mismatch.
- Quan ly duyet duoc case escalated nhung hanh dong `High`/`Critical` can reason va step-up neu policy bat.
- Admin cau hinh duoc policy/permission nhung khong sua/xoa duoc audit log goc.
- Permission duoc check theo action nhu `exception.inline.resolve.medium`, `emergency.activate`, `fastlane.activate`, khong chi dua vao role tho.
- `Exceptions.vue` sap xep case theo `Critical > Blocking > HighRisk > SlaBreaching > Medium > Low`, khong chi theo thoi gian.
- Case dang chan van hanh tai chot co badge `Blocking`, countdown/SLA va action chinh de quyet nhanh.
- Quan ly/Admin khong thay action ma tai khoan khong co quyen.
- Hanh dong approval rui ro cao bat buoc hien hau qua, ly do va step-up neu policy bat.
- Case qua SLA tu dong escalate va ghi event `ApprovalSlaBreached`.
- Company-wide alert hien theo severity, khong ep redirect user tru evacuation/lockdown.
- Nhan vien thuong nhan alert dang banner/notification ngan gon, co acknowledge nhanh, khong lo thong tin nhay cam.
- Critical alert persistent cho den khi acknowledged/ended/reviewed.
- Duress code tao silent critical alarm, khong lo thong tin nguy hiem tren man hinh tai chot.
- Bao ve danh dau tailgating thu cong duoc va case vao queue hau kiem.
- Door/barrier forced-open/stuck/held-open tao alarm dung severity.
- Lockdown/evacuation/fire/shelter/first-responder mode kich hoat, broadcast va ket thuc duoc.
- Evacuation tao muster session va theo doi missing/unknown/resolved duoc.
- Visitor/contractor overstay, host unavailable, escort missing duoc canh bao/escalate.
- Lost/stolen QR device revoke token va chan token cu.
- Clock skew QR duoc hien ly do ro va fallback theo policy.
- HR terminated/suspended user bi chan va tao case.
- Two-person rule yeu cau nguoi xac nhan thu hai truoc khi mo khu vuc nhay cam.
- Shift handover bat buoc ban giao open case, device degraded, manual mode, emergency active.
- Offline allowlist mode hoat dong gioi han va reconcile khi online lai.
- Fast-lane mode chi ap dung cho doi tuong rui ro thap va tao review sau ca.
- Workplace violence incident tao duoc case nhanh khong can QR/bien so.
- Delivery/vendor/cargo exit thieu approval bi giu/escalate.
- Critical incident tu dong legal hold va ghi chain of custody khi xem/export evidence.
- Drill mode khong lam ban du lieu su co that va co post-incident review.
- Common operating picture hien duoc site/zone/gate/device/case/occupancy theo trang thai moi nhat.
- Case Critical/High duoc ghim dung vi tri tren common operating picture.
- SOP playbook tu gan theo incident type va khong cho dong case khi thieu buoc bat buoc.
- Runbook automation tao dung case, broadcast, task, audit va khong lap action khi request trung.
- Mobile/responsive view cho guard/manager dung duoc cac action chinh tren man hinh nho.
- Executive risk dashboard hien duoc incident, SLA, override, device downtime, visitor/vendor compliance.
- Compliance pack export duoc report co audit va permission.
- Training/certification gate chan contractor/vendor khi thieu NDA/safety/chung chi.
- Permit-to-work chan access khi permit het han, sai zone hoac sai thoi gian.
- Asset custody phat hien thieu/sai tai san khi check-out va escalate.
- Identity risk score hien ly do tinh diem va chi dung de uu tien/escalate, khong ket luan gian lan mu.
- Safety incident/hazard zone canh bao hoac chan access theo policy.
- Shift performance dashboard tong hop ca truc, override, SLA, case chua ban giao.
- Investigation workspace gom timeline theo nguoi/xe/QR/zone/case va export co audit.
- Policy simulation cho biet policy draft se chan/cho qua khac bao nhieu luot truoc khi activate.
- Data quality center phat hien duplicate, stuck `IN`, inactive user con quyen, visitor chua checkout, manual override lap lai.
- Trang ngoai le hien case escalated.
- Quan ly duyet/tu choi/dong case duoc.
- Bao ve khong can roi man hinh quet khi xu ly inline.
- Moi override/manual operation co audit.
- Request lap khong tao log trung.
- `dotnet test API/API/API.Tests/API.Tests.csproj --no-restore --verbosity minimal` pass.
- `npm run build` trong `View` pass.
- `/health/ready` ready.
- Khong co diff trong vung cam.

## 17. Ket Luan

Thiet ke moi bien he thong thanh mot nen tang van hanh co kha nang chong chiu:

- Binh thuong thi tu dong.
- Loi nhe/trung binh thi bao ve xu ly ngay tai chot, co trach nhiem ca nhan va audit.
- Rui ro cao thi chuyen quan ly/SOC.
- Khan cap thi cap quyen tuc thoi, broadcast toan cong ty va hau kiem bat buoc.
- Duress, lockdown, evacuation, offline, lost QR, HR conflict, vendor/cargo va workplace violence deu co luong xu ly rieng.
- Trang ngoai le tro thanh noi dieu tra, phe duyet va dong ho so, khong con la trang xem log de trung.

Day la huong phu hop hon voi doanh nghiep vua/lon vi no giu toc do van hanh tai chot nhung van dam bao truy vet, phan quyen va kiem soat rui ro.
