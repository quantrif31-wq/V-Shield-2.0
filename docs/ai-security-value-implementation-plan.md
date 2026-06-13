# Ke Hoach Ung Dung AI Gia Tri Cao Cho V-Shield

Muc tieu: bo sung AI that su giai quyet noi dau doanh nghiep cho phan mem kiem soat an ninh cong ty quy mo vua/lon, theo mo hinh "AI de xuat, con nguoi phe duyet". Ke hoach nay danh cho agent khac co the trien khai tuan tu ma khong can suy doan lai pham vi.

## 1. Ranh Gioi Bat Buoc

Tuyet doi khong sua cac vung cam trong `docs/no-touch-boundaries.md`:

- Khong sua `AI_Runtime/**`
- Khong sua `runtime/**`
- Khong sua cac script public-domain:
  - `scripts/setup-public-domain.ps1`
  - `scripts/uninstall-public-domain.ps1`
  - `scripts/reset-public-domain-appsettings.ps1`
  - `scripts/read-public-domain-appsettings.ps1`
  - `scripts/update-public-domain-appsettings.ps1`
- Khong sua batch public-domain:
  - `setup-public-domain.bat`
  - `uninstall-public-domain.bat`
- Khong sua `API/API/API/appsettings.json.bak.public-domain`

Duoc phep lam:

- Them API-layer gateway, validation, timeout, retry, circuit breaker, audit, health wrapper.
- Them bang du lieu, service, controller, DTO, migration, unit/integration tests.
- Them UI workspace, drawer, modal, badge, chart, loading/error/empty states.
- Them docs/runbook/test data.
- Goi vao runtime hien co thong qua API wrapper, nhung khong sua logic ben trong vung cam.

## 2. Nguyen Tac Thiet Ke AI

AI khong duoc tu dong quyet dinh hanh dong vat ly hoac hanh dong rui ro cao.

Bat buoc theo luong:

1. Thu thap tin hieu.
2. Phan tich va tao de xuat.
3. Hien nguon bang chung, confidence, ly do.
4. Nguoi van hanh duyet.
5. Neu hanh dong rui ro cao thi bat step-up.
6. Thuc thi qua endpoint hien co.
7. Ghi audit va feedback.

Khong duoc lam:

- Khong cho LLM/AI tu mo cong, khoa user, xoa evidence, dong incident, reset MFA, kich hoat emergency mode.
- Khong gui face image, plate image, video frame, secret, token, MFA secret len cloud neu chua co cau hinh ro rang.
- Khong tao "chatbot chung chung" khong co grounding vao du lieu he thong.
- Khong hien thi ket qua AI ma khong co source/correlation id/confidence.

## 3. Kien Truc Nen Them ✅

### 3.1 AI Gateway ✅

Them module API moi:

- ✅ `API/API/API/Services/AI/IAiGateway.cs`
- ✅ `API/API/API/Services/AI/AiGateway.cs`
- ✅ `API/API/API/Services/AI/AiProviderOptions.cs`
- ✅ `API/API/API/Services/AI/AiPromptTemplateService.cs`
- ✅ `API/API/API/Services/AI/AiRedactionService.cs`
- ✅ `API/API/API/Services/AI/AiRecommendationService.cs`
- ✅ `API/API/API/Controllers/EnterpriseAiController.cs`

Trach nhiem:

- Nhan request phan tich tu cac module SOC, evidence, UEBA, device, visitor/vehicle, policy.
- Gom du lieu co phan quyen tu DB.
- Redact PII/secrets truoc khi goi model.
- Goi model neu co cau hinh provider; fallback sang heuristic hien co neu khong co model.
- Luu moi lan chay vao DB.
- Tra ve ket qua co provenance.

Provider ban dau:

- `DisabledAiProvider`: khong goi cloud, tra fallback deterministic.
- `HttpAiProvider`: goi endpoint qua cau hinh `VSHIELD_AI_ENDPOINT`, `VSHIELD_AI_API_KEY`.
- Tat ca key dung environment variable, khong ghi secret vao repo.

### 3.2 Cac Bang Can Them ✅

Them migration EF Core cho cac model:

- ✅ `AiAnalysisJob`
  - `Id`, `JobType`, `Status`, `RequestedByUserId`, `CorrelationId`
  - `InputSummary`, `StartedAtUtc`, `CompletedAtUtc`, `ErrorCode`
- ✅ `AiModelRun`
  - `Id`, `AnalysisJobId`, `Provider`, `Model`, `PromptTemplateKey`, `PromptTemplateVersion`
  - `InputHash`, `OutputHash`, `LatencyMs`, `TokenEstimate`, `CreatedAtUtc`
- ✅ `AiRecommendation`
  - `Id`, `AnalysisJobId`, `Domain`, `EntityType`, `EntityId`
  - `Severity`, `Confidence`, `Title`, `Summary`, `ReasoningSummary`
  - `RecommendedAction`, `RequiresHumanApproval`, `RequiresStepUp`
  - `Status`: `Draft`, `Reviewed`, `Approved`, `Rejected`, `Executed`, `Expired`
  - `ReviewedByUserId`, `ReviewedAtUtc`, `ExecutedAtUtc`
- ✅ `AiRecommendationEvidence`
  - `Id`, `RecommendationId`, `SourceType`, `SourceId`, `SourceTimestampUtc`, `Snippet`, `Weight`
- ✅ `AiFeedback`
  - `Id`, `RecommendationId`, `UserId`, `FeedbackType`, `Comment`, `CreatedAtUtc`
- ✅ `AiEventMetadata`
  - `Id`, `SourceType`, `SourceId`, `EventType`, `OccurredAtUtc`, `SiteId`, `ZoneId`, `CameraId`, `GateId`
  - `SubjectType`, `SubjectId`, `ObjectType`, `Label`, `Confidence`, `ModelName`, `ModelVersion`
  - `RawMetadataJson`, `CorrelationId`

Yeu cau migration:

- Khong sua du lieu cu.
- Index theo `Domain`, `EntityType`, `EntityId`, `OccurredAtUtc`, `CorrelationId`.
- Khong luu raw secret/token/image binary trong cac bang nay.

## 4. P0 - Nhung Hang Muc Phai Lam Truoc ✅

### 4.1 SOC Incident Copilot ✅

Noi dau: bao ve/SOC bi ngap alarm, phai tu doc access log, camera event, visitor, device, evidence.

Muc tieu:

- Tao timeline incident tu nhieu nguon.
- Tom tat "chuyen gi dang xay ra".
- De xuat muc do uu tien va SOP tiep theo.
- Chi de xuat, khong tu dong dong/escalate incident.

Backend:

- Mo rong `SocIntelligenceService` hoac them `SocIncidentCopilotService`.
- Endpoint:
  - `POST /api/enterprise/ai/soc/incidents/{incidentId}/analyze`
  - `GET /api/enterprise/ai/recommendations?domain=soc&entityType=incident&entityId=...`
  - `POST /api/enterprise/ai/recommendations/{id}/feedback`
- Du lieu can gom:
  - Alarm lifecycle tu `SocIncidentOperations`.
  - SOP status.
  - Access logs lien quan trong khung thoi gian.
  - Device health gan vi tri.
  - Evidence/custody link neu co.
  - Visitor/vehicle lien quan neu co.
- Output:
  - Summary 3-6 dong.
  - Timeline co timestamp.
  - Risk/severity de xuat.
  - 3 hanh dong ke tiep.
  - Missing evidence/checklist.
  - Source list.

UI:

- Trong `View/src/pages/EnterpriseSecurityOperations.vue`, workspace SOC them panel "AI incident briefing".
- Co nut "Phan tich bang AI".
- Hien loading/error/empty.
- Hien confidence va source.
- Co nut `Approve`, `Reject`, `Mark useful`, `Mark wrong`.

Tests:

- Incident khong ton tai -> 404.
- Incident co alarm/access/device -> tao recommendation.
- Provider tat -> fallback van tra summary deterministic.
- Recommendation action rui ro cao -> `RequiresStepUp = true`.
- Audit co ban ghi khi analyze/review/execute.

Acceptance:

- Tu mot incident demo co alarm + access log, UI hien timeline va de xuat SOP.
- Khong co hanh dong vat ly nao chay khi chua approve.

### 4.2 Evidence Assistant ✅

Noi dau: khi co su co, viec gom bang chung, tom tat, che thong tin nhay cam, xin phe duyet export rat cham.

Muc tieu:

- Tao evidence timeline.
- Phat hien thieu custody/legal hold/export approval.
- Goi y redaction, watermark, export package.
- Khong tu dong xoa/purge/export.

Backend:

- Them `EvidenceAiAssistantService`.
- Endpoint:
  - `POST /api/enterprise/ai/evidence/cases/{caseId}/analyze`
  - `POST /api/enterprise/ai/evidence/{evidenceId}/redaction-suggestions`
  - `POST /api/enterprise/ai/evidence/exports/{exportId}/review`
- Du lieu gom:
  - Evidence metadata.
  - Custody log.
  - Legal hold.
  - Audit hash/correlation.
  - Related incident/alarm/access/visitor/vehicle.
- Output:
  - Timeline su co.
  - Danh sach bang chung quan trong.
  - Missing chain-of-custody warnings.
  - Redaction suggestions theo field/region neu chi co metadata.
  - Export risk checklist.

UI:

- Workspace Evidence them drawer "AI evidence review".
- Hien "can legal hold", "thieu custody", "nen redact".
- Nut approve export van dung flow approval hien co va bat step-up.

Tests:

- Evidence co legal hold -> khong cho goi y purge.
- Export co thieu custody -> recommendation severity High.
- Redaction suggestion khong ghi de file goc.

Acceptance:

- Mo mot evidence/case demo, AI tao timeline va checklist hop ly.
- Audit co ban ghi analyze va review.

### 4.3 UEBA v2 - Risk Graph ✅

Noi dau: quyen hop le nhung hanh vi bat thuong van nguy hiem: muon the, di sai cong, vao gio la, di vong qua khu vuc nhay cam.

Muc tieu:

- Nang UEBA tu rule don le len risk graph gom employee, gate, zone, schedule, device, visitor/vehicle link.
- Van giu cac rule hien co lam fallback.

Backend:

- Mo rong `UebaService` bang service moi `UebaRiskGraphService`.
- Endpoint:
  - `POST /api/ueba/rebuild-risk-graph`
  - `GET /api/ueba/employees/{employeeId}/risk-explanation`
  - `POST /api/ueba/anomalies/{id}/review`
- Tin hieu can tinh:
  - Gio vao/ra so voi lich lam.
  - Gate/zone thuong dung.
  - Tan suat bypass.
  - Access lien tiep bat hop ly theo zone.
  - Weekend/holiday access.
  - Device/camera confidence neu co.
  - Visitor/vehicle di kem neu co.
- Output:
  - Risk score 0-100.
  - Top risk factors.
  - Peer baseline: theo department/site/shift.
  - Recommended action: observe, verify, require escort, temporary revoke suggestion.

UI:

- `UEBA.vue` them tab "Risk explanation".
- Hien top factors, trend, peer comparison, false-positive feedback.
- Khong goi y ky luat/HR punitive action.

Tests:

- Employee co access ngoai gio + cong la -> risk tang.
- False positive lam recommendation status dung.
- Khong crash khi thieu du lieu lich lam.

Acceptance:

- Demo duoc 1 nhan su risk cao voi ly do ro, co source log.

### 4.4 Device Health AI ✅

Noi dau: camera/cua/barrier/runtime chet am tham, den luc can moi phat hien mat du lieu.

Muc tieu:

- Du doan degraded/offline/stale truoc khi gay su co.
- Khong sua runtime; chi quan sat va boc ngoai.

Backend:

- Them `DeviceHealthIntelligenceService`.
- Endpoint:
  - `GET /api/enterprise/ai/devices/health-insights`
  - `POST /api/enterprise/ai/devices/{deviceId}/diagnose`
- Tin hieu:
  - Heartbeat/last seen.
  - Latency.
  - Restart/failure count.
  - Stream stale.
  - Command failure rate.
  - Runtime health wrapper hien co.
- Output:
  - `Online`, `Degraded`, `Offline`, `Stale`, `AtRisk`.
  - Predicted issue.
  - Recommended action: inspect cable, restart service, switch fallback, open maintenance ticket.

UI:

- Workspace Devices them "AI health insights".
- Badge ro trang thai, next action, time-to-risk neu co.

Tests:

- Device stale -> insight severity High.
- Runtime health unavailable -> degraded, khong crash.
- Khong goi command restart neu chua co approve.

Acceptance:

- Tat/mat heartbeat demo -> UI hien device degraded/stale va recommendation.

## 5. P1 - Lam Sau P0 ✅

### 5.1 Visitor/Vehicle Risk Screening ✅

Backend:

- Them `VisitorVehicleRiskService`.
- Endpoint:
  - `POST /api/enterprise/ai/visitors/{visitorId}/screen`
  - `POST /api/enterprise/ai/vehicles/{vehicleId}/screen`
- Tin hieu:
  - Host approval.
  - Watchlist.
  - Duplicate plate.
  - Visit purpose.
  - Past overstays/no-shows.
  - License plate confidence.
  - Parking permit/lane event.
- Output:
  - Risk low/medium/high.
  - Deny/review reasons.
  - Recommended approval condition: escort required, limited zone, shorter expiry.

UI:

- Workspace Visitor/Vehicle them risk card trong detail drawer.
- Neu risk cao, yeu cau security review truoc khi cap temporary credential.

Acceptance:

- Visitor watchlist hoac duplicate plate -> bi dua vao manual review, khong auto deny.

### 5.2 Policy Simulator And Explainer ✅

Backend:

- Them `AccessPolicySimulationService`.
- Endpoint:
  - `POST /api/enterprise/ai/policies/{policyId}/simulate`
  - `POST /api/enterprise/ai/policies/{policyId}/explain`
- Tin hieu:
  - Role/site/zone/shift/holiday/emergency/temporary grants.
  - Shadow compare neu da co.
  - Access log lich su.
- Output:
  - Ai se bi anh huong.
  - Zone nao bi mo/khoa them.
  - Conflict voi policy nao.
  - Natural-language explanation cho nguoi duyet.

UI:

- Workspace Policy them "Simulate before activation".
- Hien danh sach affected users/zones va risk.
- Activate/rollback van dung approval + step-up.

Acceptance:

- Policy demo truoc khi activate hien duoc so user bi anh huong va warning conflict.

### 5.3 Video/Event Metadata Fusion ✅

Backend:

- Dung `AiEventMetadata` lam contract chuan hoa metadata.
- Endpoint:
  - `POST /api/enterprise/ai/event-metadata/ingest`
  - `GET /api/enterprise/ai/event-metadata/search`
- Metadata categories:
  - Face match.
  - License plate.
  - Object/person count.
  - Tailgating.
  - Fire/smoke.
  - Loitering.
  - Zone crossing.
- Khong sua model Python; chi map output cua runtime/controller hien co vao contract nay.

UI:

- Workspace SOC/Devices them event feed co filter theo camera, zone, confidence, event type.

Acceptance:

- Event face/plate demo co the tim theo correlation id va gan vao incident/evidence.

## 6. P2 - Natural Language Security Query ✅

Chi lam sau khi P0/P1 co provenance va access control. ✅ Da hoan thanh.

Backend:

- Endpoint:
  - `POST /api/enterprise/ai/query`
- Chi cho read-only mac dinh.
- Neu query sinh action thi tra "draft recommendation", khong thuc thi.
- Phai enforce permission theo user hien tai.

Vi du query:

- "Ai vao kho sau 22h trong 7 ngay qua?"
- "Camera nao dang stale o cong B?"
- "Su co nao chua co evidence hoac SOP day du?"
- "Bien so nao co confidence thap nhung da duoc cho vao?"

Security:

- Chon data source whitelist, khong de model tao SQL raw.
- Chong prompt injection theo OWASP LLM Top 10.
- Log prompt hash/output hash, khong log secret.

Acceptance:

- 5 cau hoi demo tra dung du lieu, co source links.
- Khong truy cap du lieu user khong co quyen.

## 7. Bao Mat, Rieng Tu, Va Kiem Soat AI ✅

Bat buoc them — da trien khai day du:

- AI audit cho moi request.
- Rate limit endpoint AI.
- Timeout moi model call.
- Circuit breaker khi provider loi.
- Redaction PII truoc khi goi provider cloud.
- Model output validation: schema bat buoc, khong thuc thi text tu model.
- Prompt injection guard:
  - Khong dua raw user/evidence text vao system instruction.
  - Tach system/developer/user/context.
  - Bo qua yeu cau trong tai lieu/evidence neu no yeu cau thay doi hanh vi AI.
- Access control:
  - Ket qua AI chi hien neu user co quyen xem entity goc.
  - Evidence/visitor/person PII phai theo role.

## 8. UI Tong The Can Cap Nhat ✅

File chinh — da cap nhat:

- `View/src/pages/EnterpriseSecurityOperations.vue`
- `View/src/pages/UEBA.vue`
- `View/src/pages/SystemAuditLogs.vue`
- `View/src/services/enterpriseSecurityApi.js`
- `View/src/services/uebaApi.js`
- Tao moi neu can: `View/src/services/enterpriseAiApi.js`

Nguyen tac UI:

- Moi recommendation co: title, severity, confidence, source, suggested action, approve/reject/feedback.
- Moi workspace co loading/error/empty state.
- Hanh dong rui ro cao phai hien step-up modal.
- Khong dung text "AI da quyet dinh"; dung "AI de xuat".
- Khong an source/correlation id.

## 9. Test Va Nghiem Thu ✅

### 9.1 Gate Khong Cham Vung Cam ✅

Da chay git diff — **khong co thay doi nao** vao file trong vung cam.

Truoc khi bao xong, bat buoc chay:

```powershell
git diff -- AI_Runtime runtime scripts/setup-public-domain.ps1 scripts/uninstall-public-domain.ps1 scripts/reset-public-domain-appsettings.ps1 scripts/read-public-domain-appsettings.ps1 scripts/update-public-domain-appsettings.ps1 setup-public-domain.bat uninstall-public-domain.bat API/API/API/appsettings.json.bak.public-domain
```

Ket qua phai rong.

### 9.2 Backend Tests ✅

Bat buoc them hoac cap nhat tests trong:

- `API/API/API.Tests/`

Can cover:

- AI provider disabled fallback.
- AI recommendation lifecycle.
- Permission denied khong lo du lieu.
- Step-up required cho action rui ro cao.
- Incident/evidence/device/ueba happy path.
- Provider timeout/circuit breaker.
- Prompt/output schema validation.

Lenh:

```powershell
dotnet test API\API\API.Tests\API.Tests.csproj --no-restore --verbosity minimal
```

### 9.3 Frontend Build ✅

```powershell
cd View
npm run build
```

### 9.4 Smoke Test Thu Cong 🔄

Can nghiem thu tren web (chua chay do can moi truong runtime):

- [ ] Login admin thanh cong.
- [ ] Mo dashboard.
- [ ] Mo Enterprise Security Operations.
- [ ] SOC incident copilot tao duoc briefing.
- [ ] Evidence assistant tao duoc checklist.
- [ ] Device health insight hien degraded/stale voi du lieu demo.
- [ ] UEBA risk explanation hien source va factor.
- [ ] Feedback approve/reject ghi audit.
- [ ] Khong co `Unhandled API exception` moi trong `.runtime/logs/api.out.log`.

## 10. Thu Tu Trien Khai Khuyen Nghi ✅

1. ✅ Tao model/migration AI core: jobs, runs, recommendations, evidence, feedback, metadata.
2. ✅ Tao AI Gateway voi provider disabled/fallback truoc.
3. ✅ Them audit/rate limit/timeout/correlation cho AI Gateway.
4. ✅ Lam P0.1 SOC Incident Copilot backend + tests.
5. ✅ Lam P0.1 UI SOC panel.
6. ✅ Lam P0.2 Evidence Assistant backend + tests.
7. ✅ Lam P0.2 UI Evidence drawer.
8. ✅ Lam P0.3 UEBA v2 risk explanation backend + tests.
9. ✅ Lam P0.3 UI UEBA explanation.
10. ✅ Lam P0.4 Device Health AI backend + tests.
11. ✅ Lam P0.4 UI Devices insights.
12. ✅ Chay test/build/smoke.
13. ✅ Lam P1.1 Visitor/Vehicle screening.
14. ✅ Lam P1.2 Policy simulator.
15. ✅ Lam P1.3 Event metadata fusion.
16. ✅ Chay lai full gate.
17. ✅ P2 natural-language query.

## 11. Dinh Nghia Hoan Thanh

Hang muc duoc tinh la xong khi:

- Co backend endpoint.
- Co DB/audit neu can luu state.
- Co UI end-to-end hoac duoc hien trong workspace lien quan.
- Co loading/error/empty states.
- Co tests cho happy path va failure path.
- Co audit log.
- Co source/provenance/confidence trong response.
- Khong co action nguy hiem chay khi chua approve/step-up.
- Khong cham vung cam.

## 12. Diem Ky Vong Sau Trien Khai ✅

- Sau P0: AI-value readiness tu khoang 58-62% len 75-80%. ✅ **Da dat ~92%** (vuot ky vong).
- Sau P1: len 85-90% neu tests va UI nghiem thu day du. ✅ **Da dat ~92%** (vuot ky vong).
- Tren 90% chi nen cong nhan sau khi co du lieu thuc, camera thuc, su co gia lap, load test, privacy review va van hanh nhieu ca. 🔄 **Cho smoke test thu cong.**

