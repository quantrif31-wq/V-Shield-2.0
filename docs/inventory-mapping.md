# Inventory Mapping: Endpoint → Service → Page → Role → Status

> Cập nhật: 2026-06-15  
> Phase A.1 theo kế hoạch backend-view-enterprise-integration-plan.md

---

## 1. Access Policy & Kiểm soát thông hành

| Endpoint | Service method | Page(s) | Admin | QuanLy | BaoVe | Trạng thái hiện tại |
|---|---|---|---|---|---|---|
| `POST /enterprise/access-policy/access-levels` | `enterpriseApi.createAccessLevel` | PolicyEngine.vue | ✅ | ❌ | ❌ | Chưa có UI |
| `POST /enterprise/access-policy/access-groups` | `enterpriseApi.createAccessGroup` | PolicyEngine.vue | ✅ | ❌ | ❌ | Chưa có UI |
| `POST /enterprise/access-policy/schedules` | `enterpriseApi.createSchedule` | PolicyEngine.vue | ✅ | ❌ | ❌ | Chưa có UI |
| `POST /enterprise/access-policy/evaluate` | `enterpriseApi.evaluateAccess` | GateTransitMonitor, PolicyEngine | ✅ | ❌ | ✅ | Chưa dùng ở view |
| `POST /enterprise/access-policy/simulate` | `enterpriseApi.simulateAccess` | PolicyEngine.vue | ✅ | ❌ | ❌ | Đã có UI cơ bản |
| `POST /enterprise/access-policy/shadow-compare` | `enterpriseApi.shadowCompare` | PolicyEngine.vue | ✅ | ❌ | ❌ | Chưa có UI |
| `POST /enterprise/access-policy/temporary-grants` | `enterpriseApi.createTemporaryGrant` | PolicyEngine | ✅ | ❌ | ❌ (request) | Chỉ Admin |
| `POST /enterprise/access-policy/emergency-states` | `enterpriseApi.createEmergencyState` | PolicyEngine | ✅ | ❌ | ❌ (request) | Chỉ Admin |
| `GET /enterprise/access-policy/emergency-states?active=true` | `enterpriseApi.getActiveEmergencies` | PolicyEngine | ✅ | ❌ | ✅ (xem) | Có UI cơ bản |
| `POST /enterprise/access-policy/occupancy` | `enterpriseApi.recordOccupancy` | — | ✅ | ❌ | ❌ | Chưa có UI |
| `POST /enterprise/access-policy/anti-passback/reset` | `enterpriseApi.resetAntiPassback` | PolicyEngine | ✅ | ❌ | ❌ (request) | Chỉ Admin |
| `GET /enterprise/access-policy/duress-events` | `enterpriseApi.getDuressEvents` | Exceptions, SocAlarmConsole | ✅ | ✅ | ✅ (case của mình) | Chưa dùng đúng chỗ |
| `POST /enterprise/access-policy/duress-events` | `enterpriseApi.recordDuressEvent` | GateTransitMonitor | ✅ | ❌ | ✅ | Chưa có UI ở lane |

## 2. QR Động

| Endpoint | Service method | Page(s) | Admin | QuanLy | BaoVe | Trạng thái hiện tại |
|---|---|---|---|---|---|---|
| `POST /dynamic-qr/generate` | `dynamicQrApi.generateDynamicQr` | DynamicQrGenerator | ✅ | ❌ | ✅ | Đã có UI |
| `POST /dynamic-qr/verify` | `dynamicQrVerifyApi.verifyDynamicQr` | GateTransitMonitor, QrAccessMonitor, DynamicQrScanner | ✅ | ❌ | ✅ | Đã có UI |
| QR Scanner (Python) | `dynamicQrScannerApi.*` | GateTransitMonitor, QrAccessMonitor, DynamicQrScanner | ✅ | ❌ | ✅ | Đã có UI |

## 3. Gate Transit / Barrier / Lane

| Endpoint | Service method | Page(s) | Admin | QuanLy | BaoVe | Trạng thái hiện tại |
|---|---|---|---|---|---|---|
| `POST /gate-transit/scan` | `gateTransitApi.scanGate` | GateTransitMonitor | ✅ | ❌ | ✅ | Đã có UI cơ bản |
| `POST /gate-transit/scan-guest` | `gateTransitApi.scanGuest` | GateTransitMonitor | ✅ | ❌ | ✅ | Đã có UI cơ bản |
| `GET /enterprise/visitor-vehicle/barriers` | `enterpriseApi.getBarriers` | BarrierPanel, LaneDashboard | ✅ | ❌ | ✅ | Đã có UI |
| `POST /enterprise/visitor-vehicle/barriers` | `enterpriseApi.createBarrier` | BarrierPanel | ✅ | ❌ | ❌ | Đã có UI |
| `POST /enterprise/visitor-vehicle/barriers/{id}/commands` | `enterpriseApi.recordBarrierCommand` | BarrierPanel | ✅ | ❌ | ✅ | Đã có UI (có reason) |
| `GET /enterprise/visitor-vehicle/barriers/{id}/commands` | `enterpriseApi.getBarrierCommands` | BarrierPanel | ✅ | ❌ | ✅ | Đã có UI |
| `GET /enterprise/visitor-vehicle/lane-events` | `enterpriseApi.getLaneEvents` | LaneDashboard, Exceptions | ✅ | ❌ | ✅ | Đã có UI cơ bản |
| `POST /enterprise/visitor-vehicle/lane-events` | `enterpriseApi.recordLaneEvent` | GateTransitMonitor | ✅ | ❌ | ✅ | Chưa dùng |
| `GET /enterprise/visitor-vehicle/lane-health` | `enterpriseApi.getLaneHealth` | LaneDashboard | ✅ | ❌ | ✅ | Đã có UI |

## 4. Visitor / Vehicle / Parking

| Endpoint | Service method | Page(s) | Admin | QuanLy | BaoVe | Trạng thái hiện tại |
|---|---|---|---|---|---|---|
| `GET /enterprise/visitor-vehicle/visits` | `enterpriseApi.getVisits` | ReceptionDashboard, HostVisitorPage | ✅ | ❌ | ✅ | Đã có UI |
| `GET /enterprise/visitor-vehicle/visits/{id}` | `enterpriseApi.getVisitDetail` | ReceptionDashboard | ✅ | ❌ | ✅ | Chưa có full detail |
| `POST /enterprise/visitor-vehicle/visits` | `enterpriseApi.createVisit` | ReceptionDashboard | ✅ | ❌ | ✅ | Đã có UI |
| `POST /enterprise/visitor-vehicle/visits/{id}/check-in` | `enterpriseApi.checkInVisit` | ReceptionDashboard, KioskCheckIn | ✅ | ❌ | ✅ | Đã có UI |
| `POST /enterprise/visitor-vehicle/visits/{id}/check-out` | `enterpriseApi.checkOutVisit` | ReceptionDashboard | ✅ | ❌ | ✅ | Đã có UI |
| `GET /enterprise/visitor-vehicle/parking-areas` | `enterpriseApi.getParkingAreas` | BarrierPanel | ✅ | ❌ | ✅ | Chưa có full UI |
| `POST /enterprise/visitor-vehicle/parking-areas` | `enterpriseApi.createParkingArea` | BarrierPanel | ✅ | ❌ | ❌ | Chưa có UI |
| `GET /enterprise/visitor-vehicle/parking-permits` | `enterpriseApi.getParkingPermits` | BarrierPanel | ✅ | ❌ | ✅ | Đã có UI cơ bản |
| `POST /enterprise/visitor-vehicle/parking-permits` | `enterpriseApi.createParkingPermit` | BarrierPanel | ✅ | ❌ | ❌ | Chưa có UI |
| `GET /enterprise/visitor-vehicle/watchlist-entries` | `enterpriseApi.getWatchlistEntries` | WatchlistQueue | ✅ | ❌ | ✅ | Đã có UI |
| `GET /enterprise/visitor-vehicle/contractors` | `enterpriseApi.getContractors` | ContractorManagement | ✅ | ❌ | ❌ | Đã có UI |
| `GET /enterprise/visitor-vehicle/forms` | `enterpriseApi.getFormTemplates` | ReceptionDashboard | ✅ | ❌ | ✅ | Chưa có UI |

## 5. Device Operations

| Endpoint | Service method | Page(s) | Admin | QuanLy | BaoVe | Trạng thái hiện tại |
|---|---|---|---|---|---|---|
| `GET /enterprise/devices/topology` | `enterpriseApi.getTopology` | DeviceTopology | ✅ | ❌ | ✅ | Đã có UI |
| `GET /enterprise/devices/{id}` | `enterpriseApi.getDevice` | DeviceTopology, DeviceHealth | ✅ | ❌ | ✅ | Chưa có detail drawer |
| `GET /enterprise/devices/{id}/readers` | `enterpriseApi.getDeviceReaders` | DeviceTopology | ✅ | ❌ | ✅ | Chưa có UI |
| `GET /enterprise/devices/{id}/relays` | `enterpriseApi.getDeviceRelays` | DeviceTopology | ✅ | ❌ | ✅ | Chưa có UI |
| `GET /enterprise/devices/{id}/sensors` | `enterpriseApi.getDeviceSensors` | DeviceTopology | ✅ | ❌ | ✅ | Chưa có UI |
| `GET /enterprise/devices/{id}/health` | `enterpriseApi.getDeviceHealthHistory` | DeviceHealth | ✅ | ❌ | ✅ | Đã có UI cơ bản |
| `GET /enterprise/devices/connectors/status` | `enterpriseApi.getConnectorStatus` | DeviceHealth | ✅ | ❌ | ✅ | Chưa có UI |
| `POST /enterprise/devices` | `enterpriseApi.createDevice` | DeviceTopology, ProvisioningWizard | ✅ | ❌ | ❌ | Đã có UI |
| `POST /enterprise/devices/{id}/controllers` | `enterpriseApi.registerController` | ProvisioningWizard | ✅ | ❌ | ❌ | Chưa có UI |
| `POST /enterprise/devices/{id}/health` | `enterpriseApi.recordHealth` | DeviceHealth | ✅ | ❌ | ✅ | Chưa có UI |
| `GET /enterprise/devices/health-insights` | `enterpriseApi.getHealthInsights` | DeviceHealth | ✅ | ❌ | ✅ | Chưa có UI |

## 6. SOC / Situational Awareness

| Endpoint | Service method | Page(s) | Admin | QuanLy | BaoVe | Trạng thái hiện tại |
|---|---|---|---|---|---|---|
| `GET /enterprise/situational-awareness/events` | `enterpriseApi.getEvents` | EventTimeline, SocAlarmConsole | ✅ | ❌ | ✅ | Đã có UI |
| `GET /enterprise/situational-awareness/events/{id}` | `enterpriseApi.getEvent` | EventTimeline | ✅ | ❌ | ✅ | Chưa có detail |
| `POST /enterprise/situational-awareness/events` | `enterpriseApi.createEvent` | SocAlarmConsole | ✅ | ❌ | ✅ | Chưa có UI |
| `DELETE /enterprise/situational-awareness/events/{id}` | `enterpriseApi.deleteEvent` | EventTimeline | ✅ | ❌ | ❌ | Đã có UI |
| `GET /enterprise/situational-awareness/correlations` | `enterpriseApi.getCorrelations` | CorrelationView | ✅ | ❌ | ✅ | Đã có UI |
| `GET /enterprise/situational-awareness/maps` | `enterpriseApi.getSiteMaps` | SocAlarmConsole | ✅ | ❌ | ✅ | Chưa có UI |
| `POST /enterprise/situational-awareness/maps` | `enterpriseApi.createSiteMap` | SocAlarmConsole | ✅ | ❌ | ❌ | Chưa có UI |
| `GET /enterprise/situational-awareness/maps/{id}/placements` | `enterpriseApi.getMapPlacements` | SocAlarmConsole | ✅ | ❌ | ✅ | Chưa có UI |
| `POST /enterprise/situational-awareness/ai-adjudications` | `enterpriseApi.createAiAdjudication` | AiReviewQueue | ✅ | ❌ | ✅ | Chưa có UI |
| `GET /enterprise/situational-awareness/ai-adjudications` | `enterpriseApi.getAiAdjudications` | AiReviewQueue | ✅ | ❌ | ✅ | Đã có UI |

## 7. Evidence & Compliance

| Endpoint | Service method | Page(s) | Admin | QuanLy | BaoVe | Trạng thái hiện tại |
|---|---|---|---|---|---|---|
| `GET /enterprise/evidence/items` | `enterpriseApi.getEvidenceItems` | EvidenceRepository | ✅ | ❌ | ❌ | Đã có UI |
| `POST /enterprise/evidence/items` | `enterpriseApi.createEvidenceItem` | EvidenceRepository | ✅ | ❌ | ❌ | Chưa có UI |
| `GET /enterprise/evidence/items/{id}` | `enterpriseApi.getEvidenceItem` | EvidenceRepository | ✅ | ❌ | ❌ | Chưa có detail drawer |
| `POST /enterprise/evidence/items/{id}/verify-hash` | `enterpriseApi.verifyEvidenceHash` | EvidenceRepository | ✅ | ❌ | ❌ | Chưa có UI |
| `GET /enterprise/evidence/items/{id}/custody` | `enterpriseApi.getChainOfCustody` | EvidenceRepository | ✅ | ❌ | ❌ | Chưa có UI |
| `GET /enterprise/evidence/collections` | `enterpriseApi.getEvidenceCollections` | EvidenceRepository | ✅ | ❌ | ❌ | Chưa có UI |
| `POST /enterprise/evidence/collections` | `enterpriseApi.createEvidenceCollection` | EvidenceRepository | ✅ | ❌ | ❌ | Chưa có UI |
| `POST /enterprise/evidence/export-requests` | `enterpriseApi.createExportRequest` | EvidenceRepository | ✅ | ❌ | ❌ | Chưa có UI |
| `PATCH /enterprise/evidence/export-requests/{id}/approve` | `enterpriseApi.approveExportRequest` | ExportApprovalQueue | ✅ | ❌ | ❌ | Đã có UI |
| `POST /enterprise/evidence/redaction-requests` | `enterpriseApi.createRedactionRequest` | EvidenceRepository | ✅ | ❌ | ❌ | Chưa có UI |
| `POST /enterprise/evidence/legal-holds` | `enterpriseApi.createLegalHold` | EvidenceRepository | ✅ | ❌ | ❌ | Chưa có UI |
| `PATCH /enterprise/evidence/legal-holds/{id}/release` | `enterpriseApi.releaseLegalHold` | EvidenceRepository | ✅ | ❌ | ❌ | Chưa có UI |
| `GET /enterprise/evidence/retention-policies` | `enterpriseApi.getRetentionPolicies` | RetentionDashboard | ✅ | ❌ | ❌ | Đã có UI |

## 8. Operations

| Endpoint | Service method | Page(s) | Admin | QuanLy | BaoVe | Trạng thái hiện tại |
|---|---|---|---|---|---|---|
| `GET /enterprise/operations/outbox-events` | `enterpriseApi.getOutboxEvents` | OutboxViewer | ✅ | ❌ | ✅ (xem) | Đã có UI |
| `GET /enterprise/operations/webhook-subscriptions` | `enterpriseApi.getWebhookSubscriptions` | WebhookDeliveryViewer | ✅ | ❌ | ✅ (xem) | Đã có UI |
| `GET /enterprise/operations/siem-exports` | `enterpriseApi.getSiemExports` | SIEMExportStatus | ✅ | ❌ | ✅ (xem) | Đã có UI |
| `GET /enterprise/operations/backup-runs` | `enterpriseApi.getBackupRuns` | BackupRestoreDrillDashboard | ✅ | ❌ | ✅ (xem) | Đã có UI |
| `GET /enterprise/operations/restore-drills` | `enterpriseApi.getRestoreDrills` | BackupRestoreDrillDashboard | ✅ | ❌ | ✅ (xem) | Đã có UI |
| `POST /enterprise/operations/restore-drills` | `enterpriseApi.startRestore` | BackupRestoreDrillDashboard | ✅ | ❌ | ❌ | Đã có UI |
| `GET /enterprise/operations/security-checks` | `enterpriseApi.getSecurityChecks` | VulnerabilityReleaseGateStatus | ✅ | ❌ | ✅ (xem) | Đã có UI |
| `POST /enterprise/operations/security-checks` | `enterpriseApi.recordSecurityCheck` | VulnerabilityReleaseGateStatus | ✅ | ❌ | ❌ | Đã có UI |
| `GET /enterprise/operations/config-health` | `enterpriseApi.getConfigHealth` | EnterpriseSecurityOperations | ✅ | ❌ | ✅ (xem) | Đã có UI |
| `GET /enterprise/operations/health-summary` | `enterpriseApi.getHealthSummary` | EnterpriseSecurityOperations | ✅ | ❌ | ✅ (xem) | Đã có UI |

---

## 9. Route/Vue cần chỉnh quyền

| Route | Component | allowedRoles hiện tại | Cần sửa thành | Lý do |
|---|---|---|---|---|
| `identity-management` | IdentityManagement.vue | Admin, BaoVe | Admin | Backend không cấp dữ liệu cho BaoVe |
| `site-hierarchy` | SiteHierarchy.vue | Admin, BaoVe | Admin, QuanLy | Backend cho QuanLy nhưng không cho BaoVe |
| `enterprise-security` | EnterpriseSecurityOperations.vue | Admin, BaoVe | Admin, BaoVe (giữ nguyên) | Phân quyền trong component, use read-only shell |
| `reception` | ReceptionDashboard.vue | Admin, BaoVe | Admin, BaoVe (giữ) | OK, backend có dữ liệu |
| `contractors` | ContractorManagement.vue | Admin | Admin (giữ) | OK |

---

## 10. Shared Components cần tạo (Phase A)

| Component | Mục đích | Dùng ở đâu |
|---|---|---|
| `StepUpModal.vue` | Modal xác thực mạnh trước action rủi ro cao | BarrierPanel, PolicyEngine, EvidenceRepository |
| `DecisionDrawer.vue` | Drawer quyết định tại lane (cho qua/từ chối/manual/duress) | GateTransitMonitor |
| `ExceptionCaseTimeline.vue` | Timeline hợp nhất cho case ngoại lệ | Exceptions |
| `PrivilegedActionReasonForm.vue` | Form nhập lý do + responsibility capture | DecisionDrawer, BarrierPanel |
| `AuditReceiptToast.vue` | Toast hiển thị receipt sau khi action hoàn thành | Toàn bộ pages có action đặc quyền |
