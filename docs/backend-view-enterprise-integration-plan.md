# Kế Hoạch Tích Hợp Backend -> View Cho V-Shield (QR động, không đụng vùng cấm)

Cập nhật: 2026-06-15

## 1. Phạm vi và ràng buộc

Tài liệu này chỉ lập kế hoạch tích hợp những năng lực **đã tồn tại ở backend** nhưng:

- chưa có màn hình sử dụng;
- đã có màn hình nhưng đặt sai chỗ;
- đã gọi API nhưng trải nghiệm vận hành chưa đúng vai trò;
- đã có route frontend nhưng không tạo được luồng thao tác doanh nghiệp hoàn chỉnh.

Ràng buộc bắt buộc:

- Không sửa `AI_Runtime/**`
- Không sửa `runtime/**`
- Không sửa public-domain scripts và batch theo [docs/no-touch-boundaries.md](C:/DoAnTotNghiep/V-Shield-2.0/docs/no-touch-boundaries.md:1)
- Bỏ qua mọi hạng mục Face ID; hệ thống hiện lấy QR động làm luồng nhận dạng chính

## 2. Kết luận rà soát mã nguồn

### 2.1. Mẫu lệch lớn nhất hiện tại

1. Backend đã có nhiều năng lực doanh nghiệp hơn frontend đang dùng.
2. Frontend đang thiên về:
   - màn hình CRUD riêng lẻ;
   - dashboard tổng quan;
   - thao tác kỹ thuật rời rạc;
   - chưa đủ luồng quyết định tại điểm kiểm soát.
3. Các năng lực quan trọng như:
   - temporary grant,
   - emergency override,
   - anti-passback reset,
   - duress,
   - evaluate/shadow compare,
   - lane event logging,
   - parking area/permit governance,
   - evidence collections/legal hold/export request,
   - restore drill/security check/health summary,
   - device detail readers/relays/sensors/history,
   - site maps
   
   đã có ở backend hoặc trong `enterpriseSecurityApi.js`, nhưng chưa được dùng đúng chỗ ở view.

### 2.2. Lệch vai trò quyền hiện tại

Có các route frontend đang mở cho `BaoVe`, nhưng backend thực tế chặn một phần hoặc toàn bộ dữ liệu nền:

- `IdentityManagement`
- `SiteHierarchy`
- một phần `EnterpriseSecurityOperations`

Điều này dẫn đến UX kiểu “vào được trang nhưng hành động thật thì bị 403”, không đạt chuẩn doanh nghiệp.

### 2.2.1. Ma trận quyền backend thực tế cần lấy làm chuẩn

Ma trận dưới đây phản ánh quyền hiện có trong controller/backend hiện tại. Mọi kế hoạch tích hợp view phải bám ma trận này trước, chỉ được mở rộng khi có hạng mục sửa authorization backend riêng.

| Năng lực | Admin | QuanLy | BaoVe | Ghi chú triển khai |
|---|---|---:|---:|---|
| Dashboard, access logs, attendance/report tổng quan | Toàn quyền | Có | Có | `QuanLy` có dữ liệu thực ở các controller tổng quan thường |
| QR động: phát sinh, quét, xác minh | Có | Không | Có | `Staff` cũng có ở một số luồng QR, nhưng không nằm trong tài liệu này |
| Gate transit / barrier / lane vận hành | Có | Không trực tiếp | Có | `BaoVe` là vai trò tuyến đầu thực tế |
| Enterprise visitor-vehicle overview, visits, lanes, watchlist đọc/xử lý cơ bản | Có | Không | Có | mutation governance nhiều điểm chỉ `Admin` |
| Identity enterprise | Có | Không | Không | frontend không được hứa quyền cho `BaoVe` hoặc `QuanLy` ở luồng này |
| Foundation / site hierarchy | Có | Có | Không | route/view phải phản ánh đúng |
| Enterprise access policy | Có | Không | Không | mọi quyền policy hiện là `Admin`; nếu muốn `QuanLy` duyệt thì cần sửa backend riêng |
| Enterprise devices / topology / health / runtime wrapper đọc-vận hành | Có | Không | Có | mutation cấu hình nhiều điểm cần step-up và quyền cao |
| SOC / situational awareness / video / AI review đọc-xử lý | Có | Không | Có | mutation sâu hơn thường chỉ `Admin` |
| Evidence repository đọc cơ bản | Có | Không | Có | governance như legal hold, purge, approval chủ yếu `Admin` |
| Operations / outbox / backup / restore / config health | Có | Không | Có đọc một phần | mutation sâu đa số `Admin` |
| Release readiness | Có | Không | Có đọc một phần | approve/release thực tế là `Admin` + step-up |

Kết luận bắt buộc:

- `QuanLy` hiện là vai trò có thực trong nhiều controller nghiệp vụ nền tảng và báo cáo, nhưng **không phải** enterprise operator toàn diện.
- `BaoVe` hiện là operator mạnh ở tuyến đầu và runtime wrapper, nhưng **không được phép** dùng view hứa hẹn các action policy/identity/foundation không có backend tương ứng.
- Mọi mục trong tài liệu nói `QuanLy` duyệt hay `BaoVe` cấp quyền chỉ được hiểu là:
  - triển khai bằng luồng request/escalation nếu backend chưa cho trực tiếp;
  - không dựng UI “bấm là được” khi backend hiện tại không cho.

### 2.2.2. Mô hình quyền mục tiêu cần chỉnh lại cho hợp lý

Để agent khác triển khai không bị hiểu sai, dùng mô hình quyền mục tiêu sau:

| Vai trò | Trách nhiệm mục tiêu | Được quyết tại chỗ | Không được quyết tại chỗ |
|---|---|---|---|
| `BaoVe` | vận hành cổng, quét QR, điều phối làn, xử lý thủ công bước đầu, ghi nhận sự cố | cho qua khi rule hợp lệ; từ chối; manual mode; duress; barrier vận hành thường | temporary grant; anti-passback reset; emergency state; policy override; evidence governance |
| `QuanLy` | giám sát vận hành, xử lý metadata/hierarchy/report, hậu kiểm quản trị | metadata nền tảng đúng quyền backend; review báo cáo; xử lý case ở phạm vi dữ liệu thật sự xem được | policy mutation lõi; emergency state; temporary grant; evidence governance; release approval |
| `Admin` | toàn quyền hệ thống, policy, emergency, evidence, release, device cấu hình sâu | toàn bộ action đặc quyền sau step-up | không áp dụng |

Quyết định sửa kế hoạch:

- Không giao `QuanLy` làm enterprise approver cho những endpoint backend hiện không hỗ trợ.
- `QuanLy` trong giai đoạn này là vai trò quản trị nghiệp vụ và hậu kiểm, không phải operator policy.
- `BaoVe` không có temporary grant trực tiếp. Khi ca vượt rule, hoặc chuyển `Admin` xử lý, hoặc dùng đường thủ công đã được phép tại lane nếu không đụng policy mutation.

### 2.2.3. Luồng escalation cũ không còn coi là mặc định

Tài liệu này không còn giả định có sẵn một hệ request/escalation backend hoàn chỉnh.

Từ đây phân tách:

1. **Pha 1 - không thêm backend mới**
   - chỉ dùng quyền backend hiện có;
   - UI phải trung thực: nếu vai trò không được mutate thì không hiện nút thực thi.
2. **Pha 2 - nếu muốn có request/escalation thật**
   - phải coi là workstream full-stack riêng;
   - cần định nghĩa bảng dữ liệu, API, queue, thông báo, SLA xử lý.

Trong phạm vi agent tiếp theo, ưu tiên hoàn thành Pha 1 trước.

### 2.3. Nút thắt vận hành tuyến đầu

Luồng quan trọng nhất đang thiếu là luồng tại **điểm quét**:

- quét QR động;
- kiểm soát xe;
- vào khu vực hạn chế;
- xác nhận cho qua có chịu trách nhiệm;
- xin phép quản lý;
- cấp quyền khẩn cấp ngay;
- vận hành thủ công khi QR/biển số đọc lỗi;
- ghi nhận bị ép/duress.

Hiện các năng lực nền liên quan nằm rải rác ở backend hoặc trang policy, nhưng chưa được đặt ngay tại nơi bảo vệ thao tác.

### 2.4. Nguyên tắc không vượt quyền khi tích hợp view

Khi agent khác triển khai, phải phân loại từng hạng mục theo một trong ba nhóm:

1. **Dùng ngay trên backend hiện có**
   - chỉ cần tích hợp UI/UX.
2. **Dùng gián tiếp bằng request/escalation**
   - chỉ áp dụng nếu có hạng mục full-stack riêng định nghĩa request model, queue và quyền xử lý.
3. **Chặn khỏi phạm vi triển khai hiện tại**
   - nếu yêu cầu đó đòi mở rộng authorization backend hoặc đổi domain rule lớn.

Không được trộn lẫn ba nhóm này trong cùng một action label.

Nếu chưa có backend cho escalation thì UI label phải nói thật:

- `Chuyển Admin xử lý`
- `Không đủ quyền`
- `Thao tác thủ công theo quy trình`

## 3. Tồn kho backend có sẵn nhưng chưa được kéo lên view đúng mức

## 3.1. Access policy và kiểm soát thông hành

Đã có ở backend/service:

- `createAccessLevel`
- `createAccessGroup`
- `createSchedule`
- `evaluateAccess`
- `shadowCompare`
- `createTemporaryGrant`
- `createEmergencyState`
- `getActiveEmergencies`
- `recordOccupancy`
- `resetAntiPassback`
- `getDuressEvents`
- `recordDuressEvent`
- `acknowledgeDuressEvent`

Hiện trạng view:

- `PolicyEngine.vue` mới dùng một phần lifecycle policy và emergency.
- Chưa có UI dùng thật cho temporary grant, anti-passback reset, evaluate thực chiến, shadow compare, duress tại điểm quét.

## 3.2. Visitor / Vehicle / Lane / Parking

Đã có ở backend/service:

- `getVisitDetail`
- `getFormTemplates`
- `createFormTemplate`
- `getParkingAreas`
- `createParkingArea`
- `createParkingPermit`
- `recordLaneEvent`

Hiện trạng view:

- Có visit list/check-in/check-out/credential ở một vài trang.
- Chưa có mô hình “hồ sơ thông hành đầy đủ” gắn visit + permit + lane event + exception decision.
- Parking area governance chưa có UI đúng mức.

## 3.3. Thiết bị và runtime control plane

Đã có ở backend/service:

- `getDevice`
- `getDeviceReaders`
- `getDeviceRelays`
- `getDeviceSensors`
- `getDeviceHealthHistory`
- `getConnectorStatus`
- `createDevice`
- `registerController`
- `recordHealth`
- `getHealthSummary`

Hiện trạng view:

- `DeviceTopology`, `DeviceHealth`, `ProvisioningWizard`, `SimulatorPanel` mới dùng một phần.
- Chưa có “device detail workspace” đúng nghĩa cho doanh nghiệp.

## 3.4. Situational awareness

Đã có ở backend/service:

- `getEvent`
- `deleteEvent`
- `createEvent`
- `createAiAdjudication`
- `recordAiMetric`
- `getSiteMaps`
- `getMapPlacements`
- `createSiteMap`
- `addMapPlacement`

Hiện trạng view:

- Có `EventTimeline`, `VideoSearch`, `AiReviewQueue`, `CorrelationView`.
- Chưa có site map vận hành cho luồng SOC.
- AI adjudication và metric mới chỉ chạm mức tối thiểu.

## 3.5. Evidence và compliance

Đã có ở backend/service:

- `createEvidenceItem`
- `getEvidenceItem`
- `verifyEvidenceHash`
- `getEvidenceAccessLogs`
- `getEvidenceCollections`
- `getEvidenceCollectionDetail`
- `createEvidenceCollection`
- `addEvidenceCollectionItem`
- `closeEvidenceCollection`
- `addCustodyEntry`
- `createExportRequest`
- `createRedactionRequest`
- `createLegalHold`
- `releaseLegalHold`
- `getRetentionPolicy`
- `createRetentionPolicy`

Hiện trạng view:

- Có repository / approval / redaction / retention / compliance.
- Nhưng vẫn thiếu các thao tác lập hồ sơ chứng cứ hoàn chỉnh, legal hold lifecycle, export request từ nhiều điểm vào.

## 3.6. Operations / backup / restore / observability

Đã có ở backend/service:

- `startRestore`
- `recordSecurityCheck`

Ngoài ra còn có:

- `getOutboxEvents`
- `getWebhookSubscriptions`
- `getWebhookDeliveries`
- `getSiemExports`
- `getBackupRuns`
- `getRestoreDrills`
- `getSecurityChecks`

Hiện trạng view:

- Mới có các view rời cho một vài phần.
- Chưa có trung tâm vận hành có ưu tiên, cảnh báo, runbook rõ ràng.

## 4. Hạng mục UI/UX cần tích hợp theo đúng chỗ

## 4.0. Bản đồ màn hình điều hành chính và màn hình phụ

Đây là phần bắt buộc để tránh nhân đôi logic ở nhiều page.

| Luồng nghiệp vụ | Màn hình điều hành chính | Màn hình phụ / hỗ trợ | Không được làm |
|---|---|---|---|
| Kiểm soát người/xe tại làn | `GateTransitMonitor.vue` | `LaneDashboard`, `BarrierPanel`, `QrAccessMonitor`, `DynamicQrScanner` | Không copy toàn bộ override flow sang cả 4 nơi |
| Xác minh QR độc lập | `QrAccessMonitor.vue` | `DynamicQrScanner` | Không tự biến thành SOC/lane console thứ hai |
| Ngoại lệ hậu kiểm | `Exceptions.vue` | `AccessLogs`, `EventTimeline`, evidence detail | Không bắt bảo vệ chuyển sang đây để quyết định ca đang đứng trước cổng |
| Policy / emergency / temporary grant control | `PolicyEngine.vue` | `EnterpriseSecurityOperations` summary cards | Không để `EnterpriseSecurityOperations` trở thành nơi thao tác policy chính |
| Visitor / reception / parking | `ReceptionDashboard.vue` | `GuestProfiles`, `HostVisitorPage`, `ContractorManagement`, `WatchlistQueue` | Không phân tán visit detail ở nhiều page khác nhau với dữ liệu khác nhau |
| Thiết bị / topology / health | `DeviceTopology.vue` | `DeviceHealth`, `ProvisioningWizard`, `SimulatorPanel` | Không để provisioning và detail thiết bị tách rời ngữ cảnh |
| SOC / alarm / incident | `SocAlarmConsole.vue` | `EventTimeline`, `CorrelationView`, `VideoSearch`, `AiReviewQueue` | Không tạo một “SOC mini” trong từng trang con |
| Evidence lifecycle | `EvidenceRepository.vue` | `ExportApprovalQueue`, `RedactionQueue`, `RetentionDashboard`, `ComplianceReports` | Không tách legal hold/export/redaction thành luồng độc lập mất ngữ cảnh evidence |
| Operations / restore / security check | `EnterpriseSecurityOperations.vue` | các workspace con operations/release | Không giữ dạng siêu trang nhồi action ngang nhau |

Quy tắc thiết kế:

- chỉ một màn hình được quyền giữ **action bar chính** của mỗi luồng;
- các màn hình phụ chỉ được:
  - dẫn link sâu;
  - mở drawer/modal;
  - hiển thị trạng thái;
  - tạo shortcut có ngữ cảnh.

## 4.1. Luồng 1: Điểm kiểm soát tuyến đầu

Màn hình đích:

- [View/src/components/GateTransitMonitor.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/components/GateTransitMonitor.vue:1)
- [View/src/components/QrAccessMonitor.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/components/QrAccessMonitor.vue:1)
- [View/src/components/DynamicQrScanner.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/components/DynamicQrScanner.vue:1)
- [View/src/pages/BarrierPanel.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/BarrierPanel.vue:1)
- [View/src/pages/LaneDashboard.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/LaneDashboard.vue:1)

Phải bổ sung:

1. **Decision drawer tại chỗ**
   - hiện trạng đối tượng;
   - kết quả QR;
   - biển số đọc được;
   - trạng thái quyền vào;
   - cảnh báo watchlist / anti-passback / holiday / occupancy.

2. **Nhóm quyết định thao tác**
   - Cho qua đúng chuẩn
   - Từ chối
   - Cho qua có chịu trách nhiệm
   - Xin phép quản lý
   - Cấp quyền khẩn cấp ngay
   - Chuyển vận hành thủ công

3. **API phải kéo vào đây**
   - `evaluateAccess`
   - `createTemporaryGrant`
   - `resetAntiPassback`
   - `recordDuressEvent`
   - `recordLaneEvent`
   - `recordBarrierCommand`

4. **UX doanh nghiệp**
   - không bắt bảo vệ nhảy sang trang ngoại lệ để xử lý ca đang đứng trước cổng;
   - action nằm cạnh lane hiện hành;
   - có step-up modal cho hành động rủi ro;
   - có reason bắt buộc;
   - có receipt kết quả sau khi xác nhận.

5. **Quy tắc vai trò**
   - `BaoVe`: cho qua theo quy tắc thường, thao tác vận hành thủ công, ghi nhận duress, dùng các action runtime/vận hành thường đang được backend cho phép
   - `QuanLy`: xem queue quản trị và hậu kiểm trong phạm vi dữ liệu backend thực sự cho phép; không có enterprise policy mutation trong giai đoạn này
   - `Admin`: toàn quyền và thao tác khẩn cấp hệ thống

6. **Ràng buộc triển khai theo backend thật**
   - `createTemporaryGrant`, `resetAntiPassback`, `createEmergencyState` hiện là backend `Admin`
   - vì vậy tại lane, `BaoVe` chỉ được:
     - hoặc dùng action local nằm trong các endpoint guard/runtime hiện cho phép;
     - hoặc chuyển `Admin` xử lý theo quy trình vận hành;
     - không được có nút mutation trực tiếp nếu chưa có lớp backend mới
   - `QuanLy` không được coi là approver cho temporary grant trong kế hoạch này
   - nếu muốn `QuanLy` hoặc `BaoVe` phê duyệt trực tiếp temporary grant, phải tách thành hạng mục mở rộng authorization backend riêng

## 4.2. Luồng 2: Trung tâm ngoại lệ cho quản lý

Màn hình đích:

- [View/src/pages/Exceptions.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/Exceptions.vue:1)

Chuyển mục tiêu trang này thành:

- hàng đợi case cần hậu kiểm;
- không phải nơi bảo vệ buộc phải mở mỗi lần quét lỗi;
- quản lý dùng để:
  - duyệt yêu cầu treo;
  - sửa dữ liệu nền;
  - xác minh sự cố database lệch;
  - đóng case sau khi đã xử lý tại hiện trường.

Cần tích hợp thêm:

- `getVisitDetail`
- `getLaneEvents`
- `getBarrierCommands`
- `getDuressEvents`
- `getEvidenceItems`
- `getCorrelations`

UX cần có:

- case timeline hợp nhất;
- phân loại `data mismatch`, `manual override`, `device degraded`, `emergency pass`, `duress`, `pending manager approval`;
- hàng đợi ưu tiên theo độ nghiêm trọng và thời gian treo.

## 4.3. Luồng 3: Chính sách và cấp quyền khẩn cấp

Màn hình đích:

- [View/src/pages/PolicyEngine.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/PolicyEngine.vue:1)

Phải nâng từ “policy admin page” thành “policy control center”:

1. thêm UI cho:
   - `createAccessLevel`
   - `createAccessGroup`
   - `createSchedule`
   - `shadowCompare`
   - `createTemporaryGrant`
   - `resetAntiPassback`

2. tách 3 lớp rõ ràng:
   - Thiết kế policy
   - Điều hành access bất thường
   - Tình trạng khẩn cấp

3. emergency override không chỉ nằm ở đây:
   - trang này là nơi cấu hình và hậu kiểm;
   - điểm quét mới là nơi phát sinh thao tác thực chiến.

4. **Phân tách rõ action thật và action yêu cầu**
   - Action thật: chỉ `Admin` dùng với các mutation policy backend hiện có
   - Action yêu cầu: chỉ thêm khi có workstream backend riêng cho escalation
   - Trước khi có backend đó, UI chỉ deep-link hoặc hiển thị hướng dẫn chuyển xử lý
   - Không dùng chung một nút cho cả action thật và action yêu cầu

## 4.4. Luồng 4: Visitor / contractor / parking lifecycle

Màn hình đích:

- [View/src/pages/ReceptionDashboard.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/ReceptionDashboard.vue:1)
- [View/src/pages/GuestProfiles.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/GuestProfiles.vue:1)
- [View/src/pages/WatchlistQueue.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/WatchlistQueue.vue:1)
- [View/src/pages/ContractorManagement.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/ContractorManagement.vue:1)
- [View/src/pages/HostVisitorPage.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/HostVisitorPage.vue:1)

Phải kéo thêm:

- `getVisitDetail`
- `getFormTemplates`
- `createFormTemplate`
- `getParkingAreas`
- `createParkingArea`
- `createParkingPermit`

Thiết kế đúng:

- visit detail phải là hồ sơ trung tâm;
- form NDA / safety / policy acceptance phải gắn vào visit;
- permit đỗ xe phải gắn trực tiếp với visit hoặc contractor;
- check-in/reception phải nhìn được permit, watchlist, credential, host, escort requirement trên cùng một detail pane.

## 4.5. Luồng 5: Device operations workspace

Màn hình đích:

- [View/src/pages/DeviceTopology.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/DeviceTopology.vue:1)
- [View/src/pages/DeviceHealth.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/DeviceHealth.vue:1)
- [View/src/pages/ProvisioningWizard.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/ProvisioningWizard.vue:1)
- [View/src/pages/SimulatorPanel.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/SimulatorPanel.vue:1)

Phải kéo thêm:

- `getDevice`
- `getDeviceReaders`
- `getDeviceRelays`
- `getDeviceSensors`
- `getDeviceHealthHistory`
- `getConnectorStatus`
- `createDevice`
- `registerController`
- `recordHealth`
- `getHealthSummary`

Thiết kế đúng:

- topology là bản đồ tổng quan;
- click một node mở ra detail drawer đầy đủ;
- provisioning không tách khỏi topology bằng cảm giác “mất ngữ cảnh”;
- security/guard chỉ thấy thao tác vận hành cần thiết;
- admin mới thấy mutation cấp hạ tầng.

## 4.6. Luồng 6: SOC + situational awareness

Màn hình đích:

- [View/src/pages/SocAlarmConsole.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/SocAlarmConsole.vue:1)
- [View/src/pages/EventTimeline.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/EventTimeline.vue:1)
- [View/src/pages/VideoSearch.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/VideoSearch.vue:1)
- [View/src/pages/AiReviewQueue.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/AiReviewQueue.vue:1)
- [View/src/pages/CorrelationView.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/CorrelationView.vue:1)

Phải kéo thêm:

- `predictEscalationRisk`
- `getEvent`
- `deleteEvent`
- `createEvent`
- `createAiAdjudication`
- `recordAiMetric`
- `getSiteMaps`
- `getMapPlacements`
- `createSiteMap`
- `addMapPlacement`

Thiết kế đúng:

- SOC console là nơi xử lý;
- event timeline là dòng diễn biến;
- correlation là nơi điều tra;
- site map là lớp nhận thức hiện trường;
- không ép người dùng đi qua quá nhiều màn hình con để khép một incident.

## 4.7. Luồng 7: Evidence và compliance lifecycle

Màn hình đích:

- [View/src/pages/EvidenceRepository.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/EvidenceRepository.vue:1)
- [View/src/pages/ExportApprovalQueue.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/ExportApprovalQueue.vue:1)
- [View/src/pages/RedactionQueue.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/RedactionQueue.vue:1)
- [View/src/pages/RetentionDashboard.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/RetentionDashboard.vue:1)
- [View/src/pages/ComplianceReports.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/ComplianceReports.vue:1)

Phải kéo thêm:

- `createEvidenceItem`
- `getEvidenceItem`
- `verifyEvidenceHash`
- `getEvidenceAccessLogs`
- `getEvidenceCollections`
- `getEvidenceCollectionDetail`
- `createEvidenceCollection`
- `addEvidenceCollectionItem`
- `closeEvidenceCollection`
- `addCustodyEntry`
- `createExportRequest`
- `createRedactionRequest`
- `createLegalHold`
- `releaseLegalHold`
- `getRetentionPolicy`
- `createRetentionPolicy`

Thiết kế đúng:

- repository không chỉ là list evidence;
- phải có detail drawer + custody timeline + related collection;
- export/redaction/legal hold phải đi được từ cùng một detail pane;
- compliance report phải drill-down được tới evidence và policy nguồn.

## 4.8. Luồng 8: Operations / release / restore

Màn hình đích:

- [View/src/pages/EnterpriseSecurityOperations.vue](C:/DoAnTotNghiep/V-Shield-2.0/View/src/pages/EnterpriseSecurityOperations.vue:1)
- các workspace operations/release hiện hữu

Phải kéo thêm:

- `startRestore`
- `recordSecurityCheck`

Thiết kế đúng:

- phần này không nên tiếp tục là “siêu trang nhồi nhiều card”;
- tách thành workspace:
  - Health
  - Backup / Restore
  - Security Checks
  - Outbox / Webhook / SIEM
  - Release Readiness

## 5. Các route/page hiện nên sửa về phạm vi quyền

## 5.1. Cần điều chỉnh router hoặc UI shell

Không nên để `BaoVe` thấy đầy đủ các view backend không cấp dữ liệu tương ứng.

Ưu tiên xử lý:

- `IdentityManagement`
- `SiteHierarchy`
- một phần `EnterpriseSecurityOperations`

Phương án đúng:

- hoặc giảm quyền route;
- hoặc giữ route nhưng chuyển thành read-only shell đúng với các endpoint guard thực sự được phép;
- không để UX rơi vào 403 hàng loạt.

## 5.2. Cơ chế step-up chung

Nhiều màn hình đang có mutation đặc quyền nhưng chưa thống nhất trải nghiệm.

Cần tạo `step-up action shell` dùng chung:

- mở modal xác thực;
- gắn `X-Step-Up-Session-Id`;
- hiển thị lý do;
- log kết quả;
- dùng lại ở:
  - barrier command rủi ro,
  - emergency state,
  - export approval,
  - redaction approval,
  - release approval,
  - manual override vượt quyền bảo vệ.

## 5.3. Ma trận hành động theo vai trò cho UI

Ma trận này là chuẩn để agent gắn nút, drawer và CTA.

| Hành động | Admin | QuanLy | BaoVe | Cách thể hiện trên UI |
|---|---|---:|---:|---|
| Cho qua bình thường khi rule đã hợp lệ | Có | Không | Có | Nút chính tại lane |
| Từ chối | Có | Không | Có | Nút chính tại lane |
| Ghi chú thủ công / chuyển manual mode | Có | Không | Có | Nút phụ tại lane |
| Ghi nhận duress / ép buộc | Có | Không | Có | Nút cảnh báo riêng tại lane |
| Mở barrier theo vận hành thường | Có | Không | Có | Có reason bắt buộc |
| Temporary grant trực tiếp | Có | Không | Không | Chỉ admin; chưa có escalation backend thì không dựng CTA ảo |
| Reset anti-passback trực tiếp | Có | Không | Không | Chỉ admin; chưa có escalation backend thì không dựng CTA ảo |
| Emergency state / lockdown / override policy | Có | Không | Không | Chỉ admin + step-up |
| Phê duyệt export/redaction/legal hold release | Có | Không | Không | Queue admin |
| Xem case ngoại lệ và hậu kiểm | Có | Có trong phạm vi dữ liệu backend cho phép | Có đọc hạn chế | `BaoVe` xem case do mình tạo; `QuanLy` chỉ xử lý phần queue có dữ liệu thật; `Admin` toàn quyền |
| Sửa metadata nền tảng, danh mục, hierarchy | Có | Có một phần | Không | Chỉ hiện nơi backend có quyền |

Quy tắc:

- Nếu backend không cho một vai trò mutate, UI phải hiện:
  - `Chuyển Admin xử lý`
  - hoặc `Không đủ quyền`
  - chỉ dùng `Yêu cầu phê duyệt` khi đã có backend escalation thật
  - không hiện `Thực thi ngay`

## 6. Thiết kế UX/UI cấp doanh nghiệp cần tuân thủ

## 6.1. Nguyên tắc chính

1. Mỗi nghiệp vụ có **một màn hình điều hành chính** và các drawer/modal phụ, không bắt người dùng nhảy trang liên tục.
2. Action ưu tiên phải nằm gần thực thể đang xử lý:
   - đang quét lane thì quyết định nằm ngay trong lane panel;
   - đang xem evidence thì export/legal hold nằm ngay trong evidence detail.
3. Trạng thái phải rõ:
   - loading,
   - degraded,
   - denied,
   - pending approval,
   - completed,
   - requires step-up.
4. Cảnh báo quan trọng phải nổi chủ động:
   - toast ưu tiên cao,
   - banner sticky,
   - âm báo hoặc visual pulse nếu đã có hệ thống cảnh báo;
   - không bắt người dùng “tự nhớ vào trang nào đó để kiểm tra”.

## 6.2. Mẫu layout nên dùng

Áp dụng nhất quán cho các workspace lớn:

- cột trái: queue/list
- giữa: detail/context
- phải: action drawer hoặc timeline

Riêng điểm kiểm soát lane:

- lane tiles ở trái
- lane decision panel ở giữa
- override / approval / manual entry drawer ở phải

## 6.3. Mẫu action hierarchy

Ưu tiên hiển thị:

1. hành động an toàn mặc định;
2. hành động cần xác nhận;
3. hành động rủi ro cao cần step-up.

Không đặt ngang hàng về trọng lượng thị giác giữa:

- “Cho qua bình thường”
- “Cấp quyền khẩn cấp”
- “Lockdown”

## 7. Kế hoạch triển khai đề xuất cho agent khác

## Phase A - Chỉnh lại quyền view trước khi làm UX sâu

1. Sửa router và sidebar theo quyền backend thực tế
2. Gỡ hoặc đổi thành read-only shell các page đang gây 403 hàng loạt cho `BaoVe`
3. Gắn capability flags ở frontend:
   - `canOperateLane`
   - `canManagePolicy`
   - `canManageEvidenceGovernance`
   - `canManageFoundation`
4. Ẩn toàn bộ CTA mutate không có quyền backend tương ứng

Tiêu chí xong phase:

- không còn route “vào được nhưng backend 403 hàng loạt” cho 3 role chính;
- các CTA nguy hiểm được hiển thị đúng người.

## Phase A1 - Chuẩn hóa khung tích hợp dùng chung

1. Tạo inventory mapping:
   - endpoint -> service method -> page -> role -> current status
2. Tạo shared composables/components:
   - step-up modal
   - decision drawer
   - exception case timeline
   - privileged action reason form
   - audit receipt toast

## Phase B - Ưu tiên tuyến đầu

1. Nâng `GateTransitMonitor` thành màn hình điều hành chính
2. Giữ `QrAccessMonitor` là màn hình xác minh QR độc lập và deep-link sang lane case khi cần
3. Giữ `DynamicQrScanner` là công cụ phụ trợ quét, không nhân đôi decision engine
4. Kết nối `BarrierPanel` như control phụ có reason + audit
5. Kết nối `LaneDashboard` như màn hình giám sát và điều hướng vào case
6. Không triển khai CTA escalation ảo nếu chưa có backend thật

Tiêu chí xong phase:

- bảo vệ xử lý được ca thực tế ngay tại lane;
- không cần nhảy trang ngoại lệ cho các ca thường gặp;
- có đường escalte lên quản lý rõ ràng.

## Phase C - Ngoại lệ và policy control

1. Tái thiết `Exceptions.vue` thành manager case queue
2. Hoàn thiện `PolicyEngine.vue`
3. Tích hợp temporary grant / anti-passback reset / duress / emergency override theo đúng role thật:
   - `Admin`: thao tác thật
   - `BaoVe`/`QuanLy`: chỉ thấy trạng thái hoặc deep-link chuyển xử lý nếu chưa có backend phù hợp

## Phase D - Visitor/parking/contractor lifecycle

1. Reception + visit detail unified drawer
2. Form templates + permit + escort + watchlist
3. Contractor + parking governance

## Phase E - Device operations

1. Device detail workspace
2. Connector status + health history + provisioning handoff
3. Health summary và manual health record

## Phase F - SOC / evidence / operations

1. SOC correlation + map
2. Evidence collection/legal hold/export entry points
3. Restore/security checks/release operations

## Phase G - Workstream tùy chọn nếu muốn escalation thật

Phase này nằm ngoài phạm vi “chỉ tích hợp backend đã có”.

Chỉ làm khi được phép mở rộng full-stack:

1. Thiết kế bảng `OperationalInterventionRequests`
2. Thêm API tạo request từ `BaoVe`
3. Thêm queue xử lý cho `Admin`
4. Thêm trạng thái:
   - `Pending`
   - `Accepted`
   - `Rejected`
   - `Executed`
   - `Expired`
5. Thêm audit và notification

Nếu không có phase này, mọi chỗ trong UI phải dùng ngôn ngữ chuyển xử lý thủ công, không gọi là request flow tự động.

## 8. Checklist nghiệm thu

Chỉ xem là hoàn chỉnh khi đạt đủ:

- không có thay đổi vào vùng cấm;
- không thêm phụ thuộc vào Face ID flow;
- mọi action đặc quyền có step-up hoặc explicit responsibility capture;
- mọi API kéo lên view đều có:
  - loading;
  - empty;
  - denied;
  - degraded;
  - success;
  - retry path hợp lý;
- `BaoVe`, `QuanLy`, `Admin` nhìn thấy đúng action theo phạm vi trách nhiệm;
- không còn view “vào được nhưng backend 403 hàng loạt”;
- tuyến đầu có đủ:
  - cho qua bình thường,
  - cho qua có chịu trách nhiệm,
  - xin phép quản lý,
  - cấp quyền khẩn cấp,
  - vận hành thủ công,
  - ghi nhận duress,
  - log lane/barrier đầy đủ;
- evidence / operations / SOC có thể đóng luồng end-to-end mà không cần chắp vá qua nhiều trang rời.

## 8.1. Test matrix bắt buộc theo vai trò

### A. Admin

- vào được:
  - `PolicyEngine`
  - `IdentityManagement`
  - `SiteHierarchy`
  - `EvidenceRepository`
  - `EnterpriseSecurityOperations`
- thực hiện được:
  - emergency state
  - temporary grant
  - anti-passback reset
  - release approval
  - export approval
  - legal hold / release
- mọi action đặc quyền đều có:
  - step-up
  - reason
  - audit receipt

### B. BaoVe

- vào được:
  - `GateTransitMonitor`
  - `QrAccessMonitor`
  - `BarrierPanel`
  - `LaneDashboard`
  - `SocAlarmConsole`
  - các màn hình visitor/vehicle được backend cho phép
- không được thấy hoặc không được thực thi trực tiếp:
  - `PolicyEngine` mutation
  - `IdentityManagement`
  - `SiteHierarchy` mutation
  - evidence governance actions
- phải làm được tại lane:
  - cho qua bình thường
  - từ chối
  - chuyển manual mode
  - ghi nhận duress
  - gửi request can thiệp khi vượt quyền

### C. QuanLy

- vào được các màn hình nền tảng/report đúng backend hiện có
- không được hứa hẹn enterprise mutation mà backend chưa hỗ trợ
- phải xử lý được:
  - queue ngoại lệ quản trị trong phạm vi dữ liệu backend cho phép
  - metadata/catalog phù hợp quyền thực tế
  - review case và chuyển admin nếu cần mutation cao hơn

## 8.2. Test matrix bắt buộc theo luồng

### Luồng 1: QR hợp lệ

- quét tại `GateTransitMonitor`
- hiện subject, trạng thái quyền, lane context
- cho qua thành công
- ghi lane event
- nếu có barrier thì có log command hoặc event liên quan

### Luồng 2: QR hợp lệ nhưng database lệch

- `BaoVe` vẫn xem được dữ liệu đọc thực tế
- hệ thống không crash
- có lựa chọn:
  - từ chối
  - manual mode
  - gửi request can thiệp
- tạo case hậu kiểm ở `Exceptions`

### Luồng 3: QR/biển số đọc lỗi hoặc không đọc được

- có đường nhập tay / manual mode ngay tại màn hình tuyến đầu
- không bắt nhảy sang trang khác để tiếp tục quyết định
- case được log với lý do rõ ràng

### Luồng 4: Khẩn cấp cần cho qua ngay

- `BaoVe` không được gọi mutation policy trực tiếp nếu backend chưa cho
- nếu chưa có backend escalation:
  - UI chỉ được hiển thị chuyển xử lý sang admin hoặc quy trình thủ công
- nếu đã có backend escalation:
  - phải đi qua queue riêng của Phase G
- khi admin thực thi, phải có:
  - step-up
  - reason bắt buộc
  - audit receipt
  - hiển thị cảnh báo tương ứng

### Luồng 5: Duress

- `BaoVe` ghi nhận được duress ngay tại điểm thao tác
- case xuất hiện ở queue phù hợp
- `Admin` xử lý hậu kiểm/acknowledge được

### Luồng 6: Evidence end-to-end

- từ event hoặc case có thể đi tới evidence detail
- từ evidence detail có thể:
  - verify hash
  - xem custody
  - tạo export/redaction/legal hold nếu quyền cho phép

### Luồng 7: Operations

- health / backup / restore / security checks có workspace riêng
- `BaoVe` chỉ thấy phần vận hành được phép
- `Admin` thấy action mutation sâu

## 8.3. Tiêu chí “xong” cho agent khác

Chỉ được báo hoàn thành nếu nộp kèm:

1. bảng mapping endpoint -> page -> role -> UI state
2. danh sách route đã chỉnh quyền hoặc shell read-only
3. video/screenshot chứng minh 3 role chính
4. smoke test cho từng luồng bắt buộc ở mục 8.2
5. xác nhận không chạm vùng cấm

## 8.4. Điều kiện để đóng kế hoạch là hợp lý

Chỉ được kết luận kế hoạch hợp lý khi đồng thời đúng 4 điểm:

1. Không hứa quyền mà backend hiện tại không có.
2. Không dựng CTA ảo khiến người dùng tưởng có escalation tự động nhưng thực chất không có.
3. `BaoVe` thao tác được ca thường gặp ngay tại lane mà không phải học thêm nhiều màn hình phụ.
4. `QuanLy` chỉ nhận trách nhiệm tương xứng với dữ liệu và mutation backend thực sự hỗ trợ.

## 9. Kết luận

Nếu làm đúng kế hoạch này, frontend sẽ chuyển từ trạng thái “có nhiều màn hình và có nhiều API” sang trạng thái “mỗi vai trò có đúng bàn điều khiển để hoàn thành công việc”.

Giá trị lớn nhất không nằm ở việc gọi thêm endpoint, mà nằm ở việc:

- đưa quyền quyết định đúng người;
- đưa hành động đúng chỗ;
- đưa cảnh báo đúng lúc;
- đưa luồng chịu trách nhiệm vào ngay trong thao tác vận hành.
