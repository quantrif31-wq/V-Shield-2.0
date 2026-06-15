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
   - `BaoVe`: cho qua theo quy tắc thường, cho qua có chịu trách nhiệm trong phạm vi hạn chế, gửi yêu cầu quản lý, kích hoạt vận hành thủ công
   - `QuanLy`: phê duyệt yêu cầu từ bảo vệ, cấp temporary grant nhiều hơn, duyệt ngoại lệ vượt phạm vi
   - `Admin`: toàn quyền và thao tác khẩn cấp hệ thống

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

## Phase A - Chuẩn hóa khung tích hợp

1. Tạo inventory mapping:
   - endpoint -> service method -> page -> role -> current status
2. Tạo shared composables/components:
   - step-up modal
   - decision drawer
   - exception case timeline
   - privileged action reason form
   - audit receipt toast

## Phase B - Ưu tiên tuyến đầu

1. Nâng `GateTransitMonitor`
2. Nâng `QrAccessMonitor`
3. Nâng `DynamicQrScanner`
4. Nâng `BarrierPanel`
5. Kết nối `LaneDashboard`

Tiêu chí xong phase:

- bảo vệ xử lý được ca thực tế ngay tại lane;
- không cần nhảy trang ngoại lệ cho các ca thường gặp;
- có đường escalte lên quản lý rõ ràng.

## Phase C - Ngoại lệ và policy control

1. Tái thiết `Exceptions.vue` thành manager case queue
2. Hoàn thiện `PolicyEngine.vue`
3. Tích hợp temporary grant / anti-passback reset / duress / emergency override

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

## 9. Kết luận

Nếu làm đúng kế hoạch này, frontend sẽ chuyển từ trạng thái “có nhiều màn hình và có nhiều API” sang trạng thái “mỗi vai trò có đúng bàn điều khiển để hoàn thành công việc”.

Giá trị lớn nhất không nằm ở việc gọi thêm endpoint, mà nằm ở việc:

- đưa quyền quyết định đúng người;
- đưa hành động đúng chỗ;
- đưa cảnh báo đúng lúc;
- đưa luồng chịu trách nhiệm vào ngay trong thao tác vận hành.
