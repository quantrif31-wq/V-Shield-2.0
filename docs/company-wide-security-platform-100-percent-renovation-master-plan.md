# V-Shield 2.0 - 100% Company-Wide Security Platform Renovation Master Plan

Date: 2026-06-10

Purpose:

- Plan a safe, complete renovation from the current source state to a commercial-grade company-wide security platform for medium and large organizations.
- Treat the current source as approximately **46% ready** against commercial physical-security platforms and security standards.
- Fix every gap identified in the fresh review without touching protected runtime/public-domain areas.

## 1. Executive Position

The current codebase is a strong backend foundation and pilot-grade access/security application. It has real strengths:

- Auth, JWT, refresh-token rotation, MFA for sensitive roles.
- Default authenticated API posture and route-boundary tests.
- Employees, visitors, vehicles, gates, cameras, QR, face, ALPR, audit and attendance.
- New enterprise backend models/APIs for site hierarchy, access policy, visitor/vehicle, devices, SOC, evidence, operations and release readiness.
- API tests and frontend build pass.

The current codebase is **not yet commercial-grade** for harsh real-world medium/large company security because many high-end capabilities are still data/API scaffolds rather than complete operator workflows, integrations and proven runtime behavior:

- Enterprise modules have little or no frontend UI.
- No real SSO/SCIM/HR sync.
- No real OSDP/ONVIF controller/reader integration.
- No worker-driven outbox/webhook/SIEM/backup pipeline.
- No hardware simulator or hardware-in-the-loop test harness.
- No immutable evidence storage or real export/redaction pipeline.
- No SOC command-center UI with realtime alarm handling.
- No load/stress/soak/chaos/failover evidence.

Target after this renovation:

- **100% source and local acceptance readiness** for a commercial-grade product implementation.
- **Production go-live readiness** only after environment-specific hardware, network, load, security and operator drills pass.

## 2. Absolute No-Touch Boundaries

These paths are protected. Do not edit, move, delete, reformat or regenerate them:

- `AI_Runtime/**`
- `runtime/**`
- `scripts/setup-public-domain.ps1`
- `scripts/uninstall-public-domain.ps1`
- `scripts/reset-public-domain-appsettings.ps1`
- `scripts/read-public-domain-appsettings.ps1`
- `scripts/update-public-domain-appsettings.ps1`
- `setup-public-domain.bat`
- `uninstall-public-domain.bat`
- `API/API/API/appsettings.json.bak.public-domain`

Allowed work around those areas:

- API-layer adapters/wrappers outside the protected folders.
- Health checks around runtime services.
- Timeout/retry/circuit-breaker policies in .NET services.
- Logs, metrics, traces and audit records.
- Simulation/mocking outside the protected folders.
- Config readers that consume existing runtime/public-domain behavior without rewriting it.
- Documentation that explains the boundary.

Not allowed:

- Editing Python AI internals.
- Editing go2rtc/runtime files inside `runtime/**`.
- Editing public-domain setup/uninstall/read/reset/update scripts.
- Replacing public-domain scripts with new logic.
- Running automated formatters or migration tools over protected paths.

Mandatory no-touch check before and after every phase:

```powershell
git status --short -- AI_Runtime runtime scripts/setup-public-domain.ps1 scripts/uninstall-public-domain.ps1 scripts/reset-public-domain-appsettings.ps1 scripts/read-public-domain-appsettings.ps1 scripts/update-public-domain-appsettings.ps1 setup-public-domain.bat uninstall-public-domain.bat API/API/API/appsettings.json.bak.public-domain
```

Expected output:

- Empty.

## 3. Definition Of 100%

The renovation reaches 100% only when all conditions below are true.

### 3.1 Product Capability

- Complete role-specific frontend workflows exist for Admin, Security Operator, Reception, Gate Operator, Auditor and Deployment Operator.
- Company/site/building/floor/zone/access-point/door/gate/lane/device hierarchy is manageable from UI and API.
- Employee, contractor and external identity lifecycle is driven by HR/IdP import or connector.
- SSO/OIDC is implemented, with SAML/LDAP boundaries documented or available through connector interfaces.
- SCIM-like provisioning or scheduled identity sync exists.
- Access decisions support schedules, holidays, zones, roles, access levels, groups, temporary exceptions, approvals, emergency states, anti-passback, occupancy and explainable denial.
- Visitors have invitation, pre-registration, kiosk/reception check-in, ID verification metadata, forms, host notification, escorting, watchlist screening, credential issuance, overstay and checkout.
- Vehicles have parking areas, permits, lane policy, barriers, plate watchlists, ALPR review queue and barrier command audit.
- Devices have registry, controller/reader/relay/sensor topology, OSDP/ONVIF-compatible adapters, simulator, health, tamper, provisioning and offline policy package delivery.
- SOC has alarm queue, severity, SLA, acknowledgement, assignment, SOP execution, escalation, incident cases, dispatch, handover, emergency lockdown and muster.
- Evidence has repository, immutable storage adapter, retention, legal hold, chain of custody, export approval, watermark/signature, redaction and compliance reports.
- Operations have durable outbox, background workers, signed webhooks, SIEM export, metrics, traces, alerts, backup, restore drill, RTO/RPO and release gates.

### 3.2 Security And Compliance

- Every privileged route has explicit role/policy authorization and tests.
- Every sensitive action has audit trail with actor, reason, correlation ID and before/after or decision context.
- Step-up MFA protects high-risk operations: role changes, access policy activation, emergency lockdown/unlockdown, evidence export, legal hold release, device credential/config changes, release approval.
- Secrets are environment/secret-store driven; repo files contain no production secrets or public-domain-specific operational values.
- OWASP ASVS-style checklist is mapped to implemented tests or documented compensating controls.
- NIST CSF-style Govern, Identify, Protect, Detect, Respond, Recover coverage exists in docs and release gates.

### 3.3 Reliability

- API tests pass.
- Frontend build passes.
- E2E browser tests pass for critical workflows.
- Migration tests pass on fresh and upgraded databases.
- Hardware simulator tests pass.
- Runtime degradation tests pass without editing protected runtime paths.
- Outbox retry/dead-letter tests pass.
- Load, stress, soak and chaos tests have recorded evidence.
- Backup/restore drill passes target RPO/RTO.
- No-touch zones remain clean.

## 4. Operating Model For Safe One-Pass Renovation

### 4.1 Branching And Checkpoints

Use this cadence even if working continuously:

1. Start from clean `main`.
2. Create working branch: `codex/enterprise-security-100-renovation`.
3. Commit after each phase passes its local gate.
4. Never leave a phase half-integrated with failing build/tests.
5. Push after each phase commit if remote is available.
6. Only merge to `main` after final release gate passes.

If the user explicitly requests direct `main`, still keep phase commits so rollback is possible.

### 4.2 Phase Gate Template

Every phase must include:

- Scope check: no protected paths.
- Backend compile.
- API tests.
- Frontend build if UI changed.
- Migration review if schema changed.
- Route-boundary test if controllers changed.
- Documentation update if workflow changed.
- Manual checklist update in release-readiness docs.

Minimum commands:

```powershell
dotnet test API\API\API\API.sln --no-restore --verbosity minimal
npm run build
git diff --check
git status --short -- AI_Runtime runtime scripts/setup-public-domain.ps1 scripts/uninstall-public-domain.ps1 scripts/reset-public-domain-appsettings.ps1 scripts/read-public-domain-appsettings.ps1 scripts/update-public-domain-appsettings.ps1 setup-public-domain.bat uninstall-public-domain.bat API/API/API/appsettings.json.bak.public-domain
```

### 4.3 Migration Safety

Rules:

- Additive migrations first.
- No destructive schema changes until backfill and rollback plan exist.
- Every new table gets indexes for common operational queries.
- Every business-critical entity gets `CreatedAtUtc`, actor fields where relevant, status fields and audit coverage.
- Every migration has rollback notes.
- Test `dotnet ef migrations list`.
- Test fresh database creation in CI or local disposable DB before production.

### 4.4 Feature Flags And Shadow Mode

High-risk logic must ship in shadow mode first:

- Access policy engine evaluates alongside old gate logic.
- Device protocol adapters run in simulator mode before live hardware.
- Outbox workers can run dry-run before external webhook/SIEM delivery.
- Evidence deletion/purge starts as dry-run.
- Lockdown workflow requires simulator/tabletop confirmation before live activation.

## 5. Target Workstreams

## Workstream A - Production Security And Configuration Hardening

Target readiness lift: 46% to 52%

Gaps:

- Repo config still contains local dev secret placeholders and public domain values.
- Step-up MFA is not enforced for sensitive enterprise actions.
- No production secret inventory enforcement in CI.
- Rate limiting is local memory only.
- Security headers are API-level baseline, not full gateway CSP/HSTS strategy.

Backend deliverables:

- Add `PrivilegedActionSession` model and service.
- Add step-up MFA endpoint:
  - `POST /api/auth/step-up/start`
  - `POST /api/auth/step-up/verify`
  - `GET /api/auth/step-up/status`
- Add `[RequireStepUp]` filter/attribute for:
  - role mutation,
  - access policy activation,
  - emergency state changes,
  - evidence export approval,
  - legal hold release,
  - device configuration changes,
  - release candidate approval.
- Add secret/config validation service:
  - fail production when repo-backed defaults are used,
  - warn development only.
- Move public-domain defaults out of general `appsettings.json` into documented env placeholders without editing public-domain scripts.
- Add distributed rate limit design option, initially backed by SQL or Redis-compatible abstraction.

Frontend deliverables:

- Step-up MFA modal.
- Privileged action expiry indicator.
- Admin config health page showing safe/unsafe production settings without exposing secrets.

Tests:

- Admin cannot approve evidence export without step-up.
- Step-up expires after configured TTL.
- Production config test rejects unsafe defaults.
- Staff cannot access privileged surfaces.

Acceptance criteria:

- Every high-risk action has role + step-up + audit.
- Production cannot boot with unsafe default secret or missing env seed admin.
- Public endpoint inventory is current.

No-touch status:

- No protected path changes.

## Workstream B - Enterprise Identity, SSO, SCIM And HR Lifecycle

Target readiness lift: 52% to 60%

Gaps:

- No OIDC/SAML/LDAP login.
- External identity mapping is a data scaffold, not a working connector.
- No SCIM-like provisioning.
- No HR import/offboarding workflow with approval and revocation proof.
- No MFA recovery codes and reset approval.

Backend deliverables:

- Identity provider configuration:
  - OIDC provider metadata URL,
  - client ID,
  - secret reference,
  - scopes,
  - claim mapping,
  - role mapping,
  - JIT provisioning toggle,
  - break-glass local admin toggle.
- OIDC login flow:
  - `GET /api/auth/external/{provider}/challenge`
  - `GET /api/auth/external/{provider}/callback`
  - external subject linked to `AppUser`.
- SCIM/import boundary:
  - `POST /api/identity/import/users`
  - `POST /api/identity/import/groups`
  - scheduled sync job.
- HR lifecycle import:
  - employee create/update,
  - contractor start/end,
  - manager assignment,
  - department/site assignment,
  - termination/suspension.
- Revocation proof:
  - app user disabled,
  - token version increment,
  - refresh tokens revoked,
  - access rules revoked or marked inactive,
  - credentials disabled,
  - evidence/audit record written.
- MFA recovery codes:
  - generate,
  - consume once,
  - rotate,
  - audit.
- MFA reset approval:
  - request by support/admin,
  - approval by second admin where possible,
  - forced re-enrollment.

Frontend deliverables:

- Identity providers page.
- External mapping page.
- HR import review page.
- Employee lifecycle dashboard.
- Offboarding checklist page.
- Access recertification campaign UI.
- MFA recovery/reset UI.

Tests:

- Disabled external user disables local user at sync/login.
- Terminated employee cannot generate QR, pass policy evaluation, or keep active refresh token.
- MFA recovery code is one-time.
- MFA reset requires approval and revokes sessions.

Acceptance criteria:

- A user can be provisioned from external identity import.
- Offboarding produces auditable revocation proof.
- Local admin remains available as break-glass.

No-touch status:

- No protected path changes.

## Workstream C - Enterprise UI Shell And Role-Specific Workplaces

Target readiness lift: 60% to 65%

Gaps:

- Enterprise APIs exist but frontend has no pages for most of them.
- Operators cannot use new SOC/evidence/policy/release modules from UI.
- Existing navigation is operational but not enough for medium/large SOC/reception/admin workflows.

Frontend deliverables:

- Admin workspace:
  - enterprise foundation,
  - identity providers,
  - access policy designer,
  - device registry,
  - evidence governance settings,
  - release gates.
- Security operator workspace:
  - alarm queue,
  - event timeline,
  - incident cases,
  - SOP execution,
  - dispatch board,
  - emergency dashboard.
- Reception workspace:
  - today's visits,
  - walk-in check-in,
  - ID verification,
  - forms,
  - host notification,
  - badge/QR credential.
- Gate operator workspace:
  - lane dashboard,
  - plate review,
  - barrier command,
  - exception adjudication.
- Auditor workspace:
  - evidence search,
  - access logs,
  - export request review,
  - compliance reports.

Backend deliverables:

- Add read APIs needed by UI:
  - search/filter/pagination for enterprise modules,
  - dashboard summaries by role,
  - timeline endpoints,
  - exportable reports.

Tests:

- E2E browser tests for each workspace smoke path.
- Role-based route visibility tests.
- API pagination/filter tests.

Acceptance criteria:

- A non-developer can run daily workflows without calling raw API.
- Staff cannot see Admin/SOC/evidence controls.
- Admin/BaoVe route split matches server authorization.

No-touch status:

- No protected path changes.

## Workstream D - Site Hierarchy, Asset Mapping And Backfill

Target readiness lift: 65% to 69%

Gaps:

- New hierarchy exists, but current gates/cameras/employees are not deeply backfilled into it.
- No frontend tree/map manager.
- No historical label strategy visible in UI.

Backend deliverables:

- Default company/site bootstrap migration or admin setup wizard.
- Backfill service:
  - gates to site/access point/lane,
  - cameras to access point/lane,
  - employees to primary site,
  - vehicles to owner/site.
- Historical label snapshot fields for logs/events:
  - site name,
  - zone name,
  - access point name,
  - lane name.
- Site hierarchy search API.

Frontend deliverables:

- Site tree editor.
- Asset placement UI.
- Map upload/reference UI.
- Gate/camera/door assignment UI.

Tests:

- Backfill creates default site and maps legacy assets.
- Renaming site does not break historical log display.
- Site tree CRUD preserves constraints.

Acceptance criteria:

- Every operational camera/gate can be traced to site/zone/access point/lane.
- Logs/events retain resolvable context after asset rename.

No-touch status:

- No protected path changes.

## Workstream E - Access Policy Engine 2.0

Target readiness lift: 69% to 76%

Gaps:

- Current policy engine is useful but too simple.
- No approval workflow for sensitive access.
- No policy version activation lifecycle.
- Anti-passback and occupancy are manually recorded, not driven by access events.
- No conflict/rule precedence model.

Backend deliverables:

- Policy version lifecycle:
  - Draft,
  - PendingApproval,
  - Approved,
  - Active,
  - Retired.
- Rule precedence:
  - emergency deny,
  - explicit deny,
  - temporary emergency override,
  - temporary grant,
  - active allow,
  - default deny.
- Approval workflow:
  - sensitive zone access,
  - after-hours access,
  - bulk changes,
  - emergency override.
- Policy simulation endpoint:
  - evaluate person/credential/time/location without committing a decision.
- Policy shadow mode:
  - compare legacy gate decision vs new policy decision.
- Event-driven anti-passback:
  - entry/exit updates state,
  - violation flag,
  - audited manual reset.
- Occupancy:
  - derived from access events,
  - max occupancy alert,
  - evacuation feed.
- Duress:
  - duress credential/PIN flag,
  - silent alarm.
- Lockdown:
  - scoped company/site/building/zone/door/gate/lane,
  - step-up MFA,
  - reason required,
  - unlock workflow.

Frontend deliverables:

- Policy designer.
- Rule conflict viewer.
- Policy simulator.
- Approval queue.
- Emergency lockdown panel.
- Occupancy dashboard.

Tests:

- Rule precedence tests.
- Holiday/schedule/overnight schedule tests.
- Policy version cannot activate without approval.
- Lockdown denies normal credential and allows emergency override only with proper workflow.
- Anti-passback violation is detected from event sequence.

Acceptance criteria:

- Every access decision explains allow/deny with version, rule and reason.
- Operators can test policy changes before activation.
- Emergency state has safe activation/unlock audit.

No-touch status:

- No protected path changes.

## Workstream F - Visitor, Contractor, Reception And Compliance Forms

Target readiness lift: 76% to 80%

Gaps:

- Visitor backend exists but UI and full lifecycle are incomplete.
- No kiosk/reception mode.
- No ID verification workflow beyond metadata.
- No overstay/escalation worker.
- Contractor lifecycle is not distinct enough.

Backend deliverables:

- Visitor invitation workflow:
  - host invite,
  - security approval,
  - visitor pre-registration,
  - QR/PIN/badge credential.
- Reception check-in:
  - walk-in visit,
  - ID type/reference/hash,
  - manual verification,
  - photo reference,
  - host notification status,
  - escort assignment.
- Forms:
  - NDA,
  - safety briefing,
  - area-specific rules,
  - versioned acceptance.
- Watchlist:
  - person and vehicle watchlists,
  - fuzzy matching hooks,
  - pending/confirmed/false positive/escalated/closed.
- Overstay job:
  - mark overstay,
  - create alarm,
  - notify host/security.
- Contractor model:
  - company,
  - contract period,
  - required training,
  - access review,
  - expiry revocation.

Frontend deliverables:

- Reception dashboard.
- Kiosk mode.
- Host visitor page.
- Watchlist review queue.
- Contractor management page.
- Overstay panel.

Tests:

- Visitor cannot check in before required forms.
- Visitor credential expires after checkout/time.
- Watchlist match creates review/alarm.
- Overstay job creates SOC alarm.
- Contractor expiry revokes access.

Acceptance criteria:

- Reception can process planned and walk-in visitors end to end.
- Host/security notifications are recorded.
- Visitor/contractor access cannot persist beyond approval window.

No-touch status:

- OCR/AI-assisted ID parsing, if added later, must call external wrapper only. Do not edit AI runtime.

## Workstream G - Vehicle, Parking, Barrier And ALPR Operations

Target readiness lift: 80% to 83%

Gaps:

- Vehicle model and ALPR exist, but no complete parking/lane/barrier command center.
- Barrier commands are records, not integrated with controller.
- No plate adjudication queue.
- No lane health and multi-lane scale workflow.

Backend deliverables:

- Vehicle policy evaluation:
  - plate,
  - driver,
  - visitor pass,
  - parking permit,
  - lane,
  - time,
  - emergency state.
- Parking occupancy.
- Plate review queue:
  - low confidence,
  - mismatch,
  - watchlist match,
  - manual correction.
- Barrier adapter interface:
  - open,
  - close,
  - hold open,
  - lock closed,
  - status.
- Barrier simulator outside protected paths.
- Lane health:
  - camera,
  - barrier,
  - reader,
  - last event age,
  - degraded state.
- Guard manual override workflow with reason and step-up when high risk.

Frontend deliverables:

- Lane dashboard.
- Plate review UI.
- Parking permit lookup.
- Barrier command panel.
- Watchlist vehicle review.

Tests:

- Plate watchlist creates alarm.
- Barrier command requires role and reason.
- Low-confidence plate requires review.
- Parking capacity breach creates event.
- Lane health degraded appears in dashboard.

Acceptance criteria:

- Vehicle entry can be decided by policy and audited.
- Manual barrier actions are traceable.
- ALPR exceptions are reviewable, not silently accepted.

No-touch status:

- ALPR runtime remains untouched; API consumes wrapper responses only.

## Workstream H - Device, Protocol, Edge And Offline Resilience

Target readiness lift: 83% to 88%

Gaps:

- Device registry exists but no real OSDP/ONVIF adapters.
- No controller/reader command delivery.
- No offline edge decision sync.
- No tamper/supervision handling.
- No simulator harness.

Backend deliverables:

- Device adapter abstractions:
  - `IAccessControllerAdapter`,
  - `IReaderAdapter`,
  - `IRelayAdapter`,
  - `IBarrierAdapter`,
  - `ICameraAccessAdapter`,
  - `IDeviceSimulator`.
- OSDP-compatible adapter boundary:
  - capabilities,
  - secure channel status,
  - reader status,
  - tamper events,
  - credential download.
- ONVIF Profile A/C-compatible adapter boundary:
  - site information,
  - access rules,
  - credentials,
  - schedules,
  - door access control,
  - events/alarms.
- Offline policy package publisher:
  - package build,
  - sign/hash,
  - enqueue delivery,
  - delivery status,
  - rollback package.
- Device heartbeat worker.
- Tamper/fault event normalization into SOC alarm.
- Simulator service outside protected paths:
  - virtual controller,
  - virtual reader,
  - virtual barrier,
  - virtual door sensor,
  - fault injection.

Frontend deliverables:

- Device topology map.
- Provisioning wizard.
- Offline package publishing page.
- Device health dashboard.
- Simulator control panel for test environment.

Tests:

- Simulator grants/denies by offline package.
- Tamper event creates SOC alarm.
- Offline package hash/signature is validated.
- Device heartbeat degradation changes status.
- Adapter failures do not crash API.

Acceptance criteria:

- Device layer can run in simulator with deterministic tests.
- Real protocol adapters can be added without changing domain logic.
- Offline operation has publish, verify, rollback and audit.

No-touch status:

- No `AI_Runtime/**` or `runtime/**` edits. Simulator and adapters live in API/test folders or new non-protected directories.

## Workstream I - Video, VMS, AI Review And Situational Awareness

Target readiness lift: 88% to 91%

Gaps:

- Face/plate/video runtime wrappers exist, but no VMS-grade search/playback/evidence workflow.
- No camera map overlay in frontend.
- AI false-positive/false-negative review is not tied to model quality process.
- Event correlation is simple.

Backend deliverables:

- Video metadata index:
  - camera,
  - time range,
  - event type,
  - subject,
  - plate,
  - confidence,
  - evidence reference.
- Video bookmark and clip request workflow:
  - create,
  - approve/export,
  - retention category.
- AI adjudication lifecycle:
  - pending,
  - confirmed,
  - false positive,
  - false negative,
  - training candidate,
  - closed.
- AI model quality metrics:
  - precision/recall proxy,
  - review counts,
  - drift alerts,
  - source/runtime version.
- Multi-signal correlation:
  - access denied + plate mismatch + face mismatch,
  - forced door + camera motion,
  - visitor overstay + no checkout,
  - tailgating pattern.
- Map overlay APIs:
  - site map,
  - devices,
  - alarms,
  - camera view links.

Frontend deliverables:

- Event timeline.
- Video search page.
- Clip/bookmark page.
- Map overlay.
- AI review queue.
- Correlation view.

Tests:

- High-severity event creates correlation/alarm.
- AI review updates metric counters.
- Video bookmark can be linked to evidence.
- Runtime unavailable creates degraded event, not crash.

Acceptance criteria:

- Operator can investigate event with linked video/evidence.
- AI mistakes become reviewable operational items.
- Camera/runtime health affects situational awareness.

No-touch status:

- AI runtime remains untouched; all review/metrics live in API/database/frontend.

## Workstream J - SOC, Incident Command, Emergency And Guard Operations

Target readiness lift: 91% to 94%

Gaps:

- SOC APIs exist, but no full realtime command-center workflow.
- No SLA/escalation.
- No guard dispatch mobile workflow.
- No emergency tabletop flow.

Backend deliverables:

- Alarm queue:
  - New,
  - Acknowledged,
  - Assigned,
  - Escalated,
  - Suppressed,
  - Closed.
- Alarm rules:
  - condition builder,
  - severity,
  - SLA,
  - suppression window,
  - notification route.
- SLA/escalation worker.
- SOP templates with checklist steps:
  - required,
  - optional,
  - evidence-required,
  - dispatch-required.
- Incident case:
  - timeline,
  - linked alarms,
  - linked evidence,
  - decisions,
  - after-action review.
- Guard dispatch:
  - task,
  - accept,
  - arrive,
  - complete,
  - attach photo/note,
  - escalation if overdue.
- Emergency:
  - lockdown,
  - evacuation,
  - shelter-in-place,
  - muster,
  - all-clear.
- SignalR realtime updates for alarms/incidents.

Frontend deliverables:

- SOC alarm console.
- Incident workspace.
- SOP checklist panel.
- Dispatch board.
- Emergency command dashboard.
- Shift handover page.

Tests:

- Alarm SLA breach escalates.
- SOP cannot complete if required steps missing.
- Incident close requires outcome.
- Lockdown requires step-up and reason.
- Shift handover includes open alarms/incidents.
- Muster snapshot computes unaccounted people.

Acceptance criteria:

- Security operator can run full alarm-to-incident-to-close workflow from UI.
- Emergency state is controlled, auditable and reversible.
- Guard tasks are accountable.

No-touch status:

- No protected path changes.

## Workstream K - Evidence, Privacy, Retention And Compliance

Target readiness lift: 94% to 96%

Gaps:

- Evidence model exists, but storage/export/redaction are not real pipelines.
- No immutable object storage adapter.
- No cryptographic signing.
- No retention execution.
- No privacy access purpose enforcement.

Backend deliverables:

- Evidence storage adapter:
  - local dev storage,
  - object storage interface,
  - immutable/WORM-capable provider boundary.
- Hashing:
  - SHA-256 on upload/import,
  - verify on access/export.
- Chain-of-custody:
  - every transfer/export/read/redaction.
- Retention worker:
  - dry-run,
  - review,
  - approved purge,
  - legal hold bypass prevention.
- Legal hold workflow:
  - apply,
  - review,
  - release with step-up and reason.
- Export workflow:
  - request,
  - approve,
  - package,
  - watermark,
  - sign,
  - deliver,
  - access log.
- Redaction workflow:
  - request,
  - approve,
  - perform,
  - verify,
  - publish/export.
- Privacy labels:
  - biometric,
  - personal identity,
  - vehicle identity,
  - visitor document,
  - sensitive site.
- Compliance reports:
  - access review,
  - terminated user revocation,
  - visitor log,
  - evidence access,
  - privileged action,
  - alarm SLA,
  - device health.

Frontend deliverables:

- Evidence repository.
- Evidence detail/custody timeline.
- Export approval queue.
- Redaction queue.
- Retention/legal hold dashboard.
- Compliance report page.

Tests:

- Evidence export requires approval and step-up.
- Legal hold prevents purge.
- Hash mismatch blocks export.
- Redaction cannot verify before perform.
- Compliance report includes expected records.

Acceptance criteria:

- Auditor can trace evidence from event to export.
- Sensitive evidence access always has purpose and actor.
- Retention cannot delete legal-hold evidence.

No-touch status:

- AI redaction, if used later, must be adapter-only. Do not edit AI runtime.

## Workstream L - Operations, HA/DR, Observability, SIEM And Cyber Operations

Target readiness lift: 96% to 98%

Gaps:

- Operations APIs exist, but workers and actual delivery are missing.
- No metrics/tracing stack.
- No real backup/restore automation.
- No vulnerability gate.
- No HA topology proof.

Backend deliverables:

- Background worker framework:
  - outbox dispatcher,
  - webhook retry,
  - SIEM exporter,
  - dependency health poller,
  - evidence retention,
  - alarm SLA escalation,
  - overstay detection,
  - backup verification scheduler.
- Outbox:
  - retry policy,
  - exponential backoff,
  - dead-letter,
  - replay.
- Webhooks:
  - HMAC signature,
  - timestamp,
  - idempotency key,
  - retry,
  - dead-letter.
- SIEM:
  - normalized event schema,
  - auth events,
  - privileged actions,
  - policy changes,
  - device tamper/offline,
  - evidence export,
  - alarm/incident.
- Observability:
  - structured logs,
  - correlation IDs,
  - metrics endpoint or OpenTelemetry,
  - traces for API/job/audit/event.
- HA/DR docs and scripts:
  - API stateless replicas,
  - DB backup/restore,
  - object storage,
  - queue/outbox recovery,
  - runtime degradation.
- Security operations:
  - secret rotation runbook,
  - dependency vulnerability scan gate,
  - container image scan gate,
  - patch cadence.

Frontend deliverables:

- Operations dashboard.
- Outbox/dead-letter viewer.
- Webhook delivery viewer.
- SIEM export status.
- Backup/restore drill dashboard.
- Vulnerability/release gate status.

Tests:

- Outbox retries then dead-letters.
- Webhook HMAC validates.
- SIEM export payload matches schema.
- Dependency outage changes health and alert state.
- Backup/restore drill records RPO/RTO.
- Release approval blocked by failed security check.

Acceptance criteria:

- Operators can distinguish API, DB, runtime, device and external integration failure.
- Important events are not lost during downstream outage.
- Release cannot pass with failed critical vulnerability gate.

No-touch status:

- Runtime health observed through wrappers only.

## Workstream M - QA, Load, Stress, Soak, Chaos And Hardware Validation

Target readiness lift: 98% to 100%

Gaps:

- Current tests are useful but not broad enough for commercial claims.
- No E2E browser suite.
- No load/stress/soak/chaos evidence.
- No hardware simulator tests.
- No migration rollback rehearsal.

Deliverables:

- API test expansion:
  - authorization matrix for all controllers,
  - policy rule matrix,
  - visitor lifecycle,
  - vehicle/lane/barrier,
  - device simulator,
  - SOC workflows,
  - evidence governance,
  - outbox/webhook/SIEM.
- E2E browser tests:
  - Admin policy setup,
  - Reception visitor check-in,
  - Guard alarm handling,
  - Gate vehicle/plate review,
  - Auditor evidence export,
  - Emergency lockdown drill.
- Migration tests:
  - fresh DB,
  - upgrade from current main,
  - rollback rehearsal on disposable DB.
- Load profiles:
  - Pilot: 500 users, 10 gates, 50 cameras.
  - Medium: 5,000 users, 50 gates, 200 cameras.
  - Large: 50,000 credentials, 200 gates, 1,000 cameras.
- Stress scenarios:
  - login storm,
  - access event burst,
  - alarm burst,
  - plate recognition burst,
  - evidence export burst,
  - webhook downstream outage.
- Soak:
  - 24-hour API/job/device simulator run.
- Chaos:
  - DB restart,
  - runtime outage,
  - webhook outage,
  - high latency,
  - disk/object storage unavailable.
- Hardware simulator:
  - controller offline,
  - reader tamper,
  - relay failure,
  - barrier stuck,
  - camera unavailable.

Acceptance criteria:

- All automated tests pass.
- Load profile meets documented latency/error budgets.
- Chaos tests show controlled degradation.
- Migration rollback plan is verified.
- Release-readiness API records every required gate.

No-touch status:

- Simulators live outside protected folders.

## 6. Critical End-To-End Business Workflows

## 6.1 Employee Onboarding

Target flow:

1. HR/IdP import creates employee.
2. Manager and site assignment are mapped.
3. Access request is generated from role/site.
4. Security reviews sensitive access.
5. Policy version/rule is approved.
6. Credential is issued.
7. Offline package is updated for relevant controllers.
8. Audit trail records every step.
9. Employee can access only approved zones/time windows.

Acceptance:

- No direct unapproved sensitive access.
- Every credential maps to a person, status and access policy.

## 6.2 Employee Offboarding

Target flow:

1. HR/IdP sends termination/suspension.
2. Local user is disabled.
3. Token version increments.
4. Refresh tokens revoked.
5. Access rules/credentials revoked.
6. Offline packages updated.
7. Vehicle/visitor/biometric references flagged.
8. Revocation proof report generated.

Acceptance:

- Terminated employee cannot login, generate QR, pass access policy or remain in offline package.

## 6.3 Visitor Visit

Target flow:

1. Host creates invitation.
2. Visitor pre-registers.
3. Watchlist screening runs.
4. Required forms accepted.
5. Reception verifies ID.
6. Host notified.
7. Credential issued for approved scope/time.
8. Visitor checks in.
9. Overstay monitored.
10. Visitor checks out.
11. Credential disabled.

Acceptance:

- Visitor cannot enter restricted areas without required approval/forms.
- Pass cannot be reused after checkout/expiry.

## 6.4 Vehicle Entry

Target flow:

1. Plate read arrives.
2. Vehicle/driver/visitor permit is looked up.
3. Parking/lane policy evaluates.
4. Watchlist and confidence checks run.
5. Barrier command is issued or review queue opens.
6. Event, decision, video bookmark and audit are linked.

Acceptance:

- Barrier action has policy decision or manual override reason.

## 6.5 Alarm To Incident

Target flow:

1. Security event creates alarm.
2. Alarm is prioritized.
3. Operator acknowledges.
4. SOP starts.
5. Guard dispatch created if needed.
6. Evidence/video linked.
7. Incident case opened.
8. Timeline records actions.
9. SLA/escalation monitored.
10. Case closes with outcome and after-action review.

Acceptance:

- Critical alarms cannot disappear without owner, outcome and audit.

## 6.6 Evidence Export

Target flow:

1. Evidence item registered with hash.
2. Evidence added to collection/case.
3. Export requested with recipient/purpose.
4. Legal hold/retention checked.
5. Step-up MFA required.
6. Admin approves.
7. Package is built, watermarked and signed.
8. Access/export log written.
9. Compliance report can reproduce the chain.

Acceptance:

- Exported evidence is traceable from source event to recipient.

## 6.7 Emergency Lockdown

Target flow:

1. Operator selects scope.
2. Step-up MFA required.
3. Reason required.
4. Policy engine activates emergency state.
5. Offline packages update controllers.
6. SOC alarm/incident created.
7. Muster snapshot starts.
8. Unlock requires step-up and reason.
9. After-action report generated.

Acceptance:

- Lockdown is scoped, auditable, testable and reversible.

## 7. Recommended Execution Sequence

Do not build everything randomly. Follow this exact order:

1. Safety baseline and branch setup.
2. Production security and step-up MFA.
3. Enterprise UI shell and API client structure.
4. Identity/HR/SSO/SCIM.
5. Site hierarchy backfill and asset mapping.
6. Access policy engine 2.0.
7. Visitor/contractor/reception.
8. Vehicle/parking/barrier/ALPR review.
9. Device protocol abstraction, simulator and offline packages.
10. Video/AI/situational awareness.
11. SOC/incident/emergency.
12. Evidence/privacy/compliance.
13. Operations workers/HA/DR/observability/SIEM.
14. QA/load/stress/soak/chaos/hardware validation.
15. Final release gate and documentation.

Reason:

- Identity and site hierarchy must stabilize before access policy.
- Policy must stabilize before visitor/vehicle/device decisions.
- Device/offline must stabilize before lockdown claims.
- SOC and evidence depend on events from policy/device/video.
- QA/load/chaos only has meaning after real workflows exist.

## 8. Phase Milestones And Target Score

| Milestone | Target score | Exit proof |
|---|---:|---|
| M0 Clean baseline | 46% | Repo clean, no-touch clean, current tests/build pass. |
| M1 Security hardening | 52% | Step-up MFA, production config guard, secret checks. |
| M2 UI shell | 58% | Role-specific enterprise pages exist. |
| M3 Identity/HR | 64% | OIDC/import/offboarding/recovery codes work. |
| M4 Site/backfill | 69% | Assets mapped to hierarchy. |
| M5 Policy 2.0 | 76% | Approval/version/simulation/lockdown/anti-passback work. |
| M6 Visitor/vehicle | 83% | Reception and lane workflows work end to end. |
| M7 Device/offline | 88% | Simulator, adapter boundaries, offline package delivery pass. |
| M8 SOC/video/evidence | 94% | SOC incident + video/evidence workflow works. |
| M9 Ops resilience | 98% | Workers, outbox, SIEM, backup/restore, alerts work. |
| M10 Commercial QA | 100% | E2E/load/chaos/migration/hardware-sim gates pass. |

## 9. Risk Register

| Risk | Severity | Control |
|---|---|---|
| Accidentally editing protected runtime/public-domain files | Critical | No-touch check before/after every phase; do not run broad formatters over repo. |
| Policy engine denies legitimate access | Critical | Shadow mode, simulator, approval workflow, emergency override, rollback. |
| Lockdown misconfiguration | Critical | Step-up MFA, scope preview, tabletop drill, simulator before live. |
| Migration damages existing data | Critical | Additive migrations, backups, disposable DB rehearsal, rollback notes. |
| Device integration blocks vendor flexibility | High | Adapter interfaces and simulator-first design. |
| Evidence export violates privacy | High | Purpose, role, step-up, redaction, legal hold and chain of custody. |
| UI exposes action server denies or hides needed operator action | High | Shared authorization metadata, E2E role tests. |
| Outbox/webhook loses events | High | Durable outbox, retry, idempotency, dead-letter and replay. |
| Production secrets leak | High | Secret inventory, env-only production values, scan gate. |
| Load fails during shift change or emergency | High | Load/stress/soak tests and backpressure metrics. |

## 10. Required Documentation Outputs

Update or create:

- Admin manual.
- Security operator manual.
- Reception manual.
- Gate operator manual.
- Auditor/evidence manual.
- Device enrollment guide.
- Access policy guide.
- Incident response runbook.
- Emergency lockdown/evacuation runbook.
- Backup/restore guide.
- Deployment guide.
- Migration rollback guide.
- Security hardening guide.
- QA evidence report.
- Release gate report.

## 11. Final Acceptance Checklist

The renovation is not complete until every item is checked:

- [ ] No-touch status empty.
- [ ] API tests pass.
- [ ] Frontend build pass.
- [ ] E2E browser tests pass.
- [ ] Migration fresh DB test pass.
- [ ] Migration upgrade test pass.
- [ ] Migration rollback rehearsal documented.
- [ ] Load test pass for target profile.
- [ ] Stress test pass.
- [ ] Soak test pass.
- [ ] Chaos/failover test pass.
- [ ] Hardware simulator tests pass.
- [ ] Runtime degradation tests pass without protected edits.
- [ ] OIDC/identity import test pass.
- [ ] Offboarding revocation proof generated.
- [ ] Access policy shadow-mode comparison recorded.
- [ ] Visitor lifecycle E2E pass.
- [ ] Vehicle/lane/barrier E2E pass.
- [ ] SOC alarm-to-incident E2E pass.
- [ ] Evidence export/redaction/legal hold E2E pass.
- [ ] Outbox/webhook/SIEM retry/dead-letter pass.
- [ ] Backup/restore drill pass.
- [ ] Secrets/vulnerability/container scan gates pass or have approved waiver.
- [ ] Operator/admin/reception/auditor runbooks updated.
- [ ] Release candidate approved only after required gates pass.

## 12. Practical Rule For Implementation

Do not chase the number by adding empty tables or placeholder pages. A feature counts toward 100% only when it has:

- domain model,
- migration,
- backend service,
- secured API,
- frontend workflow when humans operate it,
- audit trail,
- tests,
- failure/degraded behavior,
- documentation,
- release-gate evidence.

Anything less is counted as partial.

