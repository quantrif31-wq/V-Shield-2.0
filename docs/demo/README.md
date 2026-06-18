# V-Shield Demo Runbook

Muc dich: tai lieu nay la kich ban demo chuan cho V-Shield sau giai doan lam sach UI/UX va hoan thien luong enterprise. Agent trien khai phai bam tai lieu nay de thiet ke UI, keo API len view, seed du lieu mau va nghiem thu.

Ghi chu: tai lieu nay mo ta trai nghiem muc tieu. Neu mot buoc hien chua co UI/API, agent phai bo sung theo `docs/enterprise-demo-cleanup-execution-plan.md`.

## 1. Chuan bi truoc demo

### 1.1. Tai khoan

Can co it nhat cac tai khoan:

| Vai tro | Username de xuat | Muc dich demo |
|---|---|---|
| Admin | `admin` | duyet, emergency, SOC, policy, evidence, cau hinh |
| QuanLy | `manager` | xem bao cao, xem/duyet neu backend cho, hau kiem |
| BaoVe 1 | `guard.gate.a` | van hanh cong A |
| BaoVe 2 | `guard.parking` | van hanh bai xe |
| BaoVe 3 | `guard.roving` | xu ly camera/QR loi, duress |

Neu tai khoan thuc te khac, seed/demo README trong app phai hien mapping tuong ung.

### 1.2. Du lieu mau can co

| Nhom du lieu | Can co |
|---|---|
| Nhan vien hop le | 5-10 nguoi, co QR dong, xe gan chu so huu |
| Khach tham | 3 khach: approved, pending, watchlist |
| Xe | 6 xe: hop le, sai chu, dang trong bai, khach, blacklist, can review |
| Cong/lane | Cong A, Cong B, Bai xe, Khu vuc han che |
| Device | 1 online, 1 degraded, 1 offline/stale |
| Alarm | 1 low, 1 critical demo |
| Intervention | 1 pending, 1 accepted, 1 expired |
| Evidence | 2 file/link mau gan voi alarm/exception |

### 1.3. Trang thai he thong

Truoc demo:

1. Chay API va Vue.
2. Mo `http://127.0.0.1:5173/`.
3. Kiem tra `/health/ready` ready.
4. Dang nhap BaoVe va Admin o 2 trinh duyet hoac 2 tab rieng.
5. Neu co nut reset demo, bam `System Admin > Demo Control > Reset scenarios`.

Ket qua mong doi:

- BaoVe vao `Control Room`.
- Admin vao `System Admin` hoac dashboard quan tri.
- Header khong hien Face ID.
- Khong co bang trong suot o cac man demo chinh.

## 2. Demo 1 - Luong vao cong binh thuong bang QR dong va bien so

### Muc tieu

Chung minh luong co ban chay that: quet QR, doc/nhap bien so, he thong cho qua, ghi audit.

### Vai tro

BaoVe.

### Duong dan

`Control Room > Gate Transit`

### Du lieu nhap

- Lane: `Cong A - Lane 1`
- QR payload: chon sample `EMP-VALID-001` hoac bam `Nap mau: Nhan vien hop le`
- Bien so: `51A-12345`
- Ly do: de trong hoac `Vao ca lam viec`

### Cac buoc

1. Mo `Control Room`.
2. Chon `Cong A - Lane 1`.
3. Bam `Nap du lieu mau` neu khong co camera that.
4. Kiem tra panel `Nguoi/xe` hien:
   - ten nhan vien
   - ma nhan vien
   - bien so
   - trang thai QR hop le
5. Bam `Cho qua`.
6. Xem receipt hien ra.
7. Mo timeline lane.

### Ket qua dat

- UI hien `Cho qua thanh cong`.
- Co receipt id.
- Timeline co event `ACCESS_GRANTED`.
- Access logs co ban ghi moi.

### Cach giai thich khi demo

"He thong khong chi mo cong tren giao dien. Moi lan cho qua deu co actor, thoi gian, lane, bien so va audit receipt de truy vet."

## 3. Demo 2 - QR khong doc duoc, BaoVe chuyen sang thao tac thu cong

### Muc tieu

Chung minh khi camera/QR loi, he thong khong dung van hanh.

### Vai tro

BaoVe.

### Duong dan

`Control Room > Gate Transit`

### Du lieu nhap

- Scenario: `QR_UNREADABLE`
- Ma nhan vien nhap tay: `EMP-002`
- Bien so: `51B-22222`
- Ly do: `Camera QR loi, da xac minh the nhan vien va giay to`

### Cac buoc

1. Chon lane.
2. Bam `Mo phong loi QR`.
3. UI phai hien trang thai `Khong doc duoc QR`.
4. Bam `Nhap thu cong`.
5. Nhap ma nhan vien va bien so.
6. Nhap ly do.
7. Bam `Xac minh`.
8. Neu hop le, bam `Cho qua thu cong`.

### Ket qua dat

- UI khong chuyen nguoi dung sang trang khac.
- Co event `MANUAL_REVIEW`.
- Neu cho qua, co event `MANUAL_PASS` hoac `ACCESS_GRANTED_MANUAL`.
- Receipt ghi ro day la thao tac thu cong va BaoVe chiu trach nhiem xac minh.

### Cach giai thich khi demo

"Trong van hanh that camera co the loi. Bao ve khong bi treo quy trinh; he thong cho phep nhap tay co ly do va truy vet."

## 4. Demo 3 - Lech database: xe dang bi ghi la con trong bai nhung nguoi that da ra

### Muc tieu

Chung minh he thong chiu duoc sai lech du lieu, nhung bat BaoVe chiu trach nhiem khi override.

### Vai tro

BaoVe.

### Duong dan

`Control Room > Gate Transit`

### Du lieu nhap

- Scenario: `PARKING_STATE_MISMATCH`
- Bien so: `51C-33333`
- Ly do: `Chu xe xuat trinh the nhan vien va bien lai, DB con trang thai dang gui do lan mat ket noi truoc do`

### Cac buoc

1. Nap scenario `Xe lech trang thai`.
2. UI hien decision `Can xac minh` hoac `Data mismatch`.
3. Mo drawer quyet dinh.
4. Chon `Cho qua co trach nhiem`.
5. Nhap ly do.
6. Tick `Toi da xac minh va chiu trach nhiem`.
7. Bam `Cho qua`.

### Ket qua dat

- Co event `OVERRIDE`.
- Receipt co actor BaoVe.
- Timeline ghi reason.
- Exceptions/Audit co case hau kiem.

### Cach giai thich khi demo

"He thong khong khoa cung den muc gay tac cong. Khi du lieu sai nhung nguoi that hop le, BaoVe co the override co trach nhiem va de lai dau vet."

## 5. Demo 4 - Sai thong tin, BaoVe tu choi

### Muc tieu

Chung minh tu choi cung duoc ghi nhan, khong chi ghi cac ca cho qua.

### Vai tro

BaoVe.

### Duong dan

`Control Room > Gate Transit`

### Du lieu nhap

- Scenario: `PLATE_OWNER_MISMATCH`
- Ly do: `Bien so khong khop voi chu so huu QR`

### Cac buoc

1. Nap scenario `Sai chu xe`.
2. UI hien decision `Tu choi de xuat`.
3. Bam `Tu choi`.
4. Nhap ly do.
5. Xem receipt.

### Ket qua dat

- Co event `ACCESS_DENIED`.
- Timeline ghi reason.
- Neu severity cao, tao exception case de hau kiem.

### Cach giai thich khi demo

"Moi quyet dinh tu choi deu co ly do va co the xem lai, tranh tranh cai giua bao ve va nguoi ra vao."

## 6. Demo 5 - Ca vuot quyen BaoVe, gui yeu cau duyet

### Muc tieu

Chung minh luong escalation that: BaoVe khong bam qua quyen, ma gui yeu cau cho Admin/nguoi co tham quyen.

### Vai tro

BaoVe tao request, Admin duyet.

### Duong dan

- BaoVe: `Control Room > Gate Transit`
- Admin: `Exceptions & Approvals`

### Du lieu nhap

- Scenario: `NEEDS_APPROVAL`
- Ly do BaoVe: `Nha thau can vao khu vuc han che, da co nguoi dai dien xac minh qua dien thoai`
- Priority: `high`

### Cac buoc BaoVe

1. Nap scenario `Can duyet`.
2. Bam `Gui yeu cau can thiep`.
3. Chon loai `temporary_grant` hoac `policy_override`.
4. Nhap ly do.
5. Bam `Gui`.
6. Ghi lai request id.

### Cac buoc Admin

1. Mo `Exceptions & Approvals`.
2. Chon tab `Cho duyet`.
3. Tim request id vua tao.
4. Mo detail drawer.
5. Doc subject, plate, lane, reason, timeline.
6. Bam `Chap nhan`.
7. Bam `Thuc thi`.

### Ket qua dat

- BaoVe thay request `Pending`.
- Admin thay request trong queue.
- Sau khi Admin accept/execute, BaoVe thay trang thai cap nhat.
- Co receipt cho accept va execute.
- Neu execute tao grant/permission that, Control Room cho phep tiep tuc.

### Cach giai thich khi demo

"Bao ve khong duoc vuot quyen. Cac ca nhay cam di qua hang doi duyet, co SLA, co nguoi phe duyet va co lich su."

## 7. Demo 6 - Duress: BaoVe bi ep buoc

### Muc tieu

Chung minh tinh huong bi uy hiep: BaoVe gui tin hieu am tham, SOC va toan he thong thay canh bao.

### Vai tro

BaoVe kich hoat, Admin/SOC xem.

### Duong dan

- BaoVe: `Control Room > Gate Transit`
- Admin/SOC: bat ky trang nao, sau do `SOC & Devices > SOC Alarm Console`

### Du lieu nhap

- Ly do: `Bao ve bi ep buoc tai cong A`

### Cac buoc

1. BaoVe mo decision drawer.
2. Bam `Ghi nhan ep buoc / Duress`.
3. Nhap ly do ngan.
4. Tick xac nhan neu UI yeu cau.
5. Bam `Gui tin hieu duress`.
6. Admin/SOC quan sat global banner/header notification.
7. Mo SOC Alarm Console.
8. Chon alarm `Duress`.
9. Bam `Acknowledge`.
10. Them comment.
11. Close alarm sau khi xu ly.

### Ket qua dat

- BaoVe thay receipt duress.
- Backend co `DuressEvent`.
- SOC co `Alarm` Critical.
- Global banner hien tren toan app.
- Header notification count tang.
- Ack/close cap nhat banner.

### Cach giai thich khi demo

"Duress khong chi la nut mau do. No tao alarm critical, day len SOC va hien canh bao toan he thong de nguoi khac biet ma khong can BaoVe roi khoi man dang lam."

## 8. Demo 7 - Emergency pass: can cap quyen di qua ngay

### Muc tieu

Chung minh ca khan cap nhu cap cuu/canh sat/doi PCCC co the duoc cho qua ngay, co ly do va nguoi chiu trach nhiem.

### Vai tro

Admin truc tiep hoac BaoVe gui emergency request neu khong co quyen.

### Duong dan

`Control Room > Gate Transit`

### Du lieu nhap

- Scenario: `EMERGENCY_RESPONDER`
- Doi tuong: `Ambulance / Police / Fire response`
- Bien so: `80A-00001`
- Ly do: `Cap cuu vao cong ty xu ly tai nan lao dong`
- Thoi han: `2 gio`

### Cac buoc Admin

1. Nap scenario emergency.
2. Bam `Emergency Pass`.
3. Chon doi tuong `Cap cuu`.
4. Nhap bien so/ten don vi.
5. Nhap ly do.
6. Xac thuc step-up neu co.
7. Bam `Cap quyen ngay`.
8. Xem global banner.
9. Xem SOC alarm/evidence timeline.

### Cac buoc BaoVe neu khong co Admin tren may

1. Bam `Gui yeu cau khan cap`.
2. Nhap ly do va bien so.
3. Admin o tab khac duyet/thuc thi.
4. BaoVe nhan trang thai `Approved/Executed`.
5. Cho qua.

### Ket qua dat

- Khong bat doi tuong khan cap qua quy trinh visitor binh thuong.
- Co receipt.
- Co alarm/broadcast toan app.
- Co expiry.
- Co actor chiu trach nhiem.

### Cach giai thich khi demo

"Tinh huong khan cap khong cho phep doi thu tuc. He thong cho phep cap quyen ngay nhung bat ly do, actor va canh bao toan cong ty."

## 9. Demo 8 - Emergency mode: lockdown/evacuation

### Muc tieu

Phan biet Emergency Pass voi Emergency Mode. Emergency Mode anh huong quy tac cong ty, khong phai cho mot xe di qua.

### Vai tro

Admin.

### Duong dan

`System Admin > Policy / Emergency` hoac `Control Room > Emergency`

### Du lieu nhap

- Mode: `FullLockdown`
- Scope: `Site chinh`
- Ly do: `Dien tap an ninh`

### Cac buoc

1. Admin chon `Activate Emergency Mode`.
2. Chon `FullLockdown`.
3. Nhap ly do.
4. Step-up.
5. Kich hoat.
6. BaoVe thu quet mot QR binh thuong.
7. He thong tu choi do emergency state.
8. Admin tat emergency mode.

### Ket qua dat

- Global banner hien `FullLockdown`.
- Access decision deny neu khong co emergency override.
- Tat mode xong access quay lai binh thuong.

### Cach giai thich khi demo

"Emergency Mode la trang thai toan cuc cua khu vuc. No khac Emergency Pass, la quyen di qua cho mot doi tuong cu the."

## 10. Demo 9 - Device offline tao canh bao van hanh

### Muc tieu

Chung minh he thong biet thiet bi loi va day len SOC.

### Vai tro

BaoVe/Admin.

### Duong dan

`SOC & Devices > Device Health`

### Du lieu nhap

- Scenario: `CAMERA_GATE_A_OFFLINE`

### Cac buoc

1. Mo Device Health.
2. Nap scenario camera offline.
3. Xem thiet bi chuyen `offline/stale`.
4. Mo SOC.
5. Tim alarm `DeviceOffline`.
6. Acknowledge va assign.

### Ket qua dat

- Device Health hien trang thai offline ro.
- SOC co alarm.
- Control Room hien warning tren lane lien quan.

### Cach giai thich khi demo

"Khi camera loi, van hanh khong im lang that bai. He thong chuyen sang che do canh bao va cho phep thao tac thu cong co audit."

## 11. Demo 10 - Evidence chain sau mot su co

### Muc tieu

Chung minh sau su co co bang chung va audit, khong chi co log ngan.

### Vai tro

Admin.

### Duong dan

`Evidence & Compliance > Evidence Repository`

### Du lieu nhap

- Chon evidence gan voi scenario duress hoac override.

### Cac buoc

1. Mo Evidence Repository.
2. Tim theo receipt id hoac plate.
3. Mo detail drawer.
4. Xem:
   - hash
   - custody log
   - related alarm/exception
   - export status
5. Tao export request neu demo can.
6. Admin approve export.

### Ket qua dat

- Evidence co lien ket voi su co.
- Co hash/custody.
- Export approval co audit.

### Cach giai thich khi demo

"He thong khong chi xu ly tai cong, ma con giu bang chung va chuoi trach nhiem sau su co."

## 12. Bang tom tat kịch bản va API/UI can co

| Kich ban | UI chinh | API/toan cuc can co | Demo dat khi |
|---|---|---|---|
| Normal access | Control Room | scan gate/guest, record lane event | receipt + access log |
| Manual operation | Control Room drawer | record lane event, optional scan override | nhap tay + receipt |
| Data mismatch override | Control Room drawer | scan override, record lane event | override event + audit |
| Deny | Control Room drawer | record lane event | denied event |
| Approval request | Control Room + Exceptions | create/accept/reject/execute intervention | queue cap nhat |
| Duress | Control Room + SOC + Global banner | record duress, create alarm | banner + SOC alarm |
| Emergency pass | Control Room + Global banner | emergency pass endpoint, alarm, lane event | cap quyen ngay + receipt |
| Emergency mode | Policy/Emergency + Global banner | emergency state, access evaluate | normal access bi deny |
| Device offline | Device Health + SOC | device health/alarm | lane warning + SOC alarm |
| Evidence | Evidence Repository | evidence detail/custody/export | mo duoc chain |

## 13. Checklist truoc khi trinh bay do an

- Dang nhap 3 vai tro OK.
- Demo data da reset.
- Control Room khong loi console.
- Global banner polling hoat dong.
- SOC co alarm khi duress.
- Exceptions co request khi escalate.
- Bang UI doc ro, khong trong suot.
- Face ID khong xuat hien trong luong demo.
- Moi nut demo co ket qua nhin thay trong 1-10 giay.
- Neu API loi, UI hien loi than thien va khong crash.

## 14. Loi thuyet minh ngan cho demo

Co the dung cau chuyen sau:

1. "Binh thuong, nhan vien dung QR dong va xe dung bien so de qua cong."
2. "Khi thiet bi loi, bao ve co quy trinh nhap tay co audit."
3. "Khi du lieu sai, bao ve co the override co trach nhiem, khong lam tac cong."
4. "Khi vuot quyen, bao ve gui yeu cau; admin duyet va thuc thi."
5. "Khi bi ep buoc hoac co khan cap, he thong day canh bao toan cong ty va SOC tiep nhan."
6. "Sau su co, evidence va audit giup truy vet ai lam gi, khi nao, vi sao."
