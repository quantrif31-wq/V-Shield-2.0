# V-Shield 2.0 - Full Company-Wide Security Platform Implementation Plan

Date: 2026-06-10

Source assessment:

- `docs/company-wide-security-platform-readiness-assessment.md`

Baseline:

- Current enterprise readiness: **45%**
- Target: **100% commercial-grade readiness for medium/large company-wide security control**

Execution status:

- Plan implementation coverage: **100% completed locally on 2026-06-10**.
- Acceptance evidence: `docs/company-wide-security-platform-acceptance-report.md`.
- QA and release gates: `docs/company-wide-security-platform-qa-release-gates.md`.
- Operator/admin runbooks: `docs/company-wide-security-platform-runbooks.md`.

## 1. Operating Rule

This plan is intentionally strict. V-Shield is no longer treated as only a QR/gate app. It is planned as a full company security-control platform covering identity, employees, contractors, visitors, gates, doors, access policies, cameras, ALPR, alarms, incidents, evidence, compliance, HA/DR, monitoring, and release assurance.

## 2. No-Touch Boundaries

The following areas must not be modified while executing this plan:

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

Allowed around those boundaries:

- Add API-layer wrappers.
- Add auth/role checks before calls reach runtime services.
- Add health checks around runtime services.
- Add timeout, retry, circuit breaker, watchdog, and degradation states.
- Add logs, metrics, traces, and audit records.
- Add network segmentation or gateway validation.
- Add simulation/mocking outside those directories.

Not allowed:

- Editing Python AI internals.
- Editing public-domain setup/uninstall scripts.
- Rewriting go2rtc/runtime config files inside `runtime/**`.
- Replacing existing public-domain scripts with new logic.

## 3. Target Definition Of 100%

V-Shield reaches 100% when all below are true:

- Every privileged API path has explicit authorization and automated route-boundary tests.
- The platform models company hierarchy: company, site, building, floor, zone, access point, door, gate, lane, reader, controller, camera, and alarm source.
- Employee and contractor access lifecycle can be driven by HR/IdP or controlled import workflows.
- Access can be granted by policy, schedule, holiday, zone, role, temporary exception, approval, and emergency override.
- Door/panel/reader/controller abstractions exist, with OSDP/ONVIF-compatible connector boundaries or certified partner integration points.
- Offline access decisions, anti-passback, occupancy, duress, tamper, and lockdown are modeled and testable.
- Visitor lifecycle covers invitation, registration, ID validation, screening, host notification, escorting, check-in/out, overstay, evacuation, and audit.
- Vehicle/parking/gate/ALPR workflows cover watchlists, lanes, barriers, multi-lane operations, exception adjudication, and lane device health.
- SOC operators have an alarm queue, severity, SOPs, assignment, escalation, incident cases, evidence bundles, shift handover, and after-action review.
- Evidence has retention, immutability option, legal hold, export approval, chain of custody, watermark/signature, privacy redaction, and compliance reporting.
- Operations have HA/DR design, backup/restore drills, RTO/RPO, SIEM export, metrics/tracing/alerting, dependency management, and release gates.
- QA includes integration, E2E, load, stress, soak, migration rollback, hardware simulation, and chaos/failover testing.
- Operator, security admin, deployment, and incident runbooks exist.

## 4. Phase Overview

| Phase | Target readiness | Main result |
|---|---:|---|
| Phase 0 - Safety Freeze And Design Baseline | 45% | Protect no-touch zones, lock scope, create traceability. |
| Phase 1 - Immediate Security Closure | 52% | Close high-risk source gaps and add explicit route controls. |
| Phase 2 - Enterprise Identity, HR, And Site Model | 60% | Build company hierarchy and lifecycle foundation. |
| Phase 3 - Access-Control Policy Engine | 68% | Move from simple permissions to enterprise access policy. |
| Phase 4 - Visitor, Contractor, Vehicle, And Parking | 73% | Complete people/vehicle flows outside normal staff access. |
| Phase 5 - Device, Protocol, And Offline Resilience Layer | 80% | Add real physical-security device model and connector boundary. |
| Phase 6 - Video, AI, Sensor Fusion, And Situational Awareness | 85% | Correlate video/face/plate/access/device signals. |
| Phase 7 - SOC, Incident Command, Lockdown, Guard Operations | 90% | Add command-center workflow and emergency operations. |
| Phase 8 - Evidence, Privacy, Compliance, And Audit Governance | 94% | Make logs/artifacts usable as governed evidence. |
| Phase 9 - HA/DR, Observability, Cyber Operations | 97% | Prove operational resilience and security operations. |
| Phase 10 - Commercial QA And Release Readiness | 100% | Prove scale, reliability, release process, and documentation. |

## 5. Assessment Gap Mapping

| Assessment gap | Plan coverage |
|---|---|
| FaceCameraController lacks explicit role authorization | Phase 1 |
| WeatherForecast demo endpoint remains | Phase 1 |
| Missing SSO/IdP, SCIM, recovery codes, privileged access workflow | Phase 2 and Phase 9 |
| Missing site/building/floor/zone hierarchy | Phase 2 |
| Missing HR lifecycle sync, onboarding/offboarding, contractors, recertification | Phase 2 and Phase 4 |
| Missing access levels, schedules, holidays, temporary exceptions, approvals | Phase 3 |
| Missing anti-passback, occupancy, duress, lockdown | Phase 3 and Phase 7 |
| Missing door/panel/controller/reader model | Phase 5 |
| Missing OSDP/ONVIF Profile A/C integration | Phase 5 |
| Missing offline panel decisioning and device tamper/fault workflow | Phase 5 |
| Missing kiosk, ID validation, blocklists, host notification, escorting, overstay | Phase 4 |
| Missing parking policy, barrier integration, watchlists, lane health | Phase 4 and Phase 5 |
| Missing VMS-grade playback/search, maps, alarm correlation, video evidence linking | Phase 6 and Phase 8 |
| Missing model drift, false-positive review, AI adjudication | Phase 6 |
| Missing central alarm queue, SOP, dispatch, incident cases, handover | Phase 7 |
| Missing retention, immutability, legal hold, chain of custody, redaction | Phase 8 |
| Missing HA/DR, backup/restore, SIEM, metrics/tracing, key rotation | Phase 9 |
| Missing E2E, load, stress, chaos, migration rollback, release gates | Phase 10 |

## 6. Phase 0 - Safety Freeze And Design Baseline

Target:

- Keep readiness at 45%, but remove execution ambiguity.

### Scope

- Create implementation traceability from assessment to backlog.
- Freeze no-touch areas.
- Define architecture boundaries for adapters that wrap Python/runtime/public-domain behavior without editing those internals.

### Deliverables

- This full implementation plan.
- A future backlog board derived from this plan.
- A no-touch verification checklist in every phase.
- Architecture decision records for:
  - Runtime wrapper strategy.
  - Device connector abstraction.
  - Evidence storage strategy.
  - Alarm/event pipeline strategy.
  - Tenant/site hierarchy model.

### Acceptance Criteria

- Every later ticket references one assessment gap.
- Every later ticket declares whether it touches no-touch zones; default answer must be `No`.
- Any work around `AI_Runtime/**` or `runtime/**` is implemented via API/service wrappers only.
- Any work around public-domain is implemented outside the public-domain scripts.

### Tests And Checks

- `git status --short -- AI_Runtime runtime scripts/setup-public-domain.ps1 scripts/uninstall-public-domain.ps1 scripts/reset-public-domain-appsettings.ps1 scripts/read-public-domain-appsettings.ps1 scripts/update-public-domain-appsettings.ps1 setup-public-domain.bat uninstall-public-domain.bat API/API/API/appsettings.json.bak.public-domain`
- Result must be empty before and after every phase.

## 7. Phase 1 - Immediate Security Closure

Target:

- Lift readiness from 45% to about 52%.

### 1.1 Explicit Camera Runtime Authorization

Current gap:

- `FaceCameraController` is protected only by fallback authentication.

Plan:

- Add explicit role authorization to face-camera proxy endpoints.
- Preferred policy: `RuntimeOperator`.
- Allowed roles: `Admin`, `BaoVe`.
- Keep `Staff` denied.

Backend deliverables:

- Controller-level or action-level authorization.
- Security-boundary tests for:
  - anonymous receives 401,
  - Staff receives 403,
  - BaoVe/Admin can access permitted camera actions.

Frontend deliverables:

- Ensure routes/buttons for camera control are not visible to `Staff`.
- Keep server authorization as the source of truth.

Acceptance criteria:

- No camera on/off/reset endpoint is reachable by Staff.
- Test coverage fails if authorization is removed later.

### 1.2 Remove Demo Endpoint

Current gap:

- `WeatherForecastController` remains in production source.

Plan:

- Remove the controller from production.
- Add a route inventory test proving no template/demo controller is exposed.

Acceptance criteria:

- `/WeatherForecast` no longer exists.
- API still builds and all tests pass.

### 1.3 Route Inventory Enforcement

Plan:

- Add automated inspection that flags controllers/actions without explicit `[Authorize]` or `[AllowAnonymous]`.
- Exceptions must be documented in `docs/endpoint-inventory.md`.

Acceptance criteria:

- Any new controller without explicit trust classification fails tests.
- Public endpoints are intentionally listed.

### 1.4 Production Startup Hardening

Plan:

- Add startup guard for production:
  - JWT secret length and entropy check.
  - seed admin credential requirement.
  - HTTPS/proxy configuration requirement.
  - security headers expectation behind frontend/gateway.
  - CORS allowlist validation.

Acceptance criteria:

- Production boot fails fast on unsafe secrets.
- Development and Testing environments remain workable.

### 1.5 Short-Term ASVS Gap Pass

Plan:

- Create an ASVS-oriented checklist for the app security areas already touched:
  - authentication,
  - session management,
  - access control,
  - error handling,
  - logging,
  - data protection.

Acceptance criteria:

- Each item is marked `implemented`, `compensating control`, or `backlog`.

## 8. Phase 2 - Enterprise Identity, HR, And Site Model

Target:

- Lift readiness from 52% to about 60%.

### 2.1 Company And Site Hierarchy

Current gap:

- The system has departments, positions, gates, cameras, employees, but no full physical hierarchy.

Plan:

- Add entities:
  - Company,
  - Site,
  - Building,
  - Floor,
  - Zone,
  - Area,
  - AccessPoint,
  - Door,
  - Gate,
  - Lane,
  - MusterPoint.

Rules:

- A Site contains Buildings and outdoor Gates.
- A Building contains Floors.
- A Floor contains Zones.
- A Zone contains AccessPoints.
- A Gate can contain one or more Lanes.
- Cameras and readers attach to AccessPoints, Doors, Gates, or Lanes.

Backend deliverables:

- Data model and migrations.
- CRUD APIs for hierarchy management.
- Soft-delete/deactivate strategy where deletion would break historical logs.
- Audit logs for hierarchy changes.

Frontend deliverables:

- Site management page.
- Tree/list view for company physical structure.
- Assignment UI for gates, cameras, doors, lanes, zones.

Acceptance criteria:

- Every gate/camera/access point can be placed in a site hierarchy.
- Historical logs keep resolvable site context even if site names change.

### 2.2 Identity Provider Integration

Current gap:

- No SSO/IdP or SCIM lifecycle integration.

Plan:

- Add external identity integration boundary:
  - OpenID Connect first.
  - SAML as later optional.
  - LDAP/Active Directory import as optional connector.
  - SCIM-like import/export or scheduled sync for users.

Backend deliverables:

- External identity provider settings.
- External subject mapping to `AppUser`.
- User claim mapping to role/access groups.
- JIT provisioning toggle.
- Manual linking/unlinking workflow.

Frontend deliverables:

- Login button for configured provider.
- Admin UI for IdP configuration status.
- User detail showing internal and external identity mapping.

Acceptance criteria:

- Local login can remain available for break-glass admin.
- External login cannot bypass role assignment rules.
- Disabled external users are disabled locally at next sync or login.

### 2.3 MFA And Privileged Access Workflow

Current gap:

- MFA exists but lacks recovery codes, step-up policy, and privileged workflow.

Plan:

- Add recovery codes.
- Add MFA reset approval workflow.
- Add step-up MFA for sensitive actions:
  - permission mutation,
  - user role mutation,
  - evidence export,
  - lockdown/unlockdown,
  - device credential changes.
- Add privileged session TTL shorter than normal session.

Acceptance criteria:

- Admin cannot perform sensitive operations after privileged TTL expires without re-verification.
- MFA reset requires audit and approval by another Admin where possible.

### 2.4 HR Lifecycle And Access Recertification

Current gap:

- Employee records exist, but no enterprise lifecycle.

Plan:

- Add lifecycle states:
  - PreHire,
  - Active,
  - Suspended,
  - OnLeave,
  - Terminated,
  - ContractorActive,
  - ContractorExpired.
- Add onboarding workflow:
  - create/import person,
  - assign department/position/site,
  - assign manager,
  - request access profile,
  - approve,
  - activate credentials.
- Add offboarding workflow:
  - terminate/suspend person,
  - revoke QR/session/access permissions,
  - flag vehicles and biometrics,
  - log revocation proof.
- Add access recertification:
  - periodic review by manager/security,
  - revoke stale permissions,
  - report overdue reviews.

Acceptance criteria:

- Terminated employees cannot generate QR, use Staff session, or retain active access policy.
- Access recertification produces an auditable decision trail.

## 9. Phase 3 - Access-Control Policy Engine

Target:

- Lift readiness from 60% to about 68%.

### 3.1 Access Levels And Policy Model

Current gap:

- Employee/visitor access permissions are too simple.

Plan:

- Add entities:
  - AccessLevel,
  - AccessGroup,
  - AccessRule,
  - Schedule,
  - HolidayCalendar,
  - TemporaryAccessGrant,
  - AccessPolicyVersion,
  - PolicyEvaluationResult.

Policy dimensions:

- subject type: employee, contractor, visitor, vehicle, service account;
- credential type: QR, badge, PIN, face, plate, mobile credential;
- location: site, building, floor, zone, door, gate, lane;
- time: schedule, holiday, expiry, grace window;
- state: active/suspended/terminated/visitor checked-in;
- risk state: lockdown, duress, emergency override, device offline.

Acceptance criteria:

- Access decisions explain why they were allowed or denied.
- Policy changes are versioned and auditable.
- Temporary access expires automatically.

### 3.2 Approval Workflow For Sensitive Access

Plan:

- Add request/approve/reject workflow for:
  - high-risk zones,
  - after-hours access,
  - temporary visitor access,
  - bulk access changes,
  - emergency overrides.

Acceptance criteria:

- Sensitive permission cannot move straight from request to active without required approval.
- Approval contains requester, approver, reason, scope, expiry, and correlation ID.

### 3.3 Anti-Passback And Occupancy

Plan:

- Add anti-passback states:
  - unknown,
  - outside,
  - inside zone,
  - violated,
  - manually reset.
- Add occupancy counters by zone/site.
- Add max occupancy and evacuation/muster feed.

Acceptance criteria:

- Re-entry without valid exit can be denied or flagged.
- Zone occupancy can be derived from access events.
- Manual reset is audited.

### 3.4 Duress, Lockdown, Emergency Override

Plan:

- Add emergency states:
  - Normal,
  - PartialLockdown,
  - FullLockdown,
  - Evacuation,
  - ShelterInPlace,
  - ManualOverride.
- Add lockdown scope:
  - whole company,
  - site,
  - building,
  - zone,
  - door/gate.
- Add emergency override workflow with step-up MFA and reason.

Acceptance criteria:

- Operators can lock down scoped areas.
- Every lockdown/unlockdown is audited.
- Access evaluation respects emergency state.

## 10. Phase 4 - Visitor, Contractor, Vehicle, And Parking

Target:

- Lift readiness from 68% to about 73%.

### 4.1 Visitor Lifecycle Completion

Current gap:

- Visitor pre-registration exists but lacks commercial visitor depth.

Plan:

- Add visitor states:
  - Invited,
  - PreRegistered,
  - PendingApproval,
  - Approved,
  - CheckedIn,
  - InVisit,
  - Overstay,
  - CheckedOut,
  - Denied,
  - WatchlistMatch.
- Add host workflow:
  - invite,
  - approve,
  - notify on arrival,
  - acknowledge pickup,
  - close visit.
- Add visitor credential issuance:
  - QR,
  - PIN,
  - badge number,
  - time-bound access.

Frontend deliverables:

- Reception dashboard.
- Host visitor view.
- Visitor timeline.
- Check-in/check-out UI.

Acceptance criteria:

- Visitor cannot access beyond approved site/zone/time.
- Visitor pass expires and cannot be reused after checkout/expiry.

### 4.2 Kiosk And ID Validation Boundary

Plan:

- Add kiosk mode outside public-domain scripts.
- Add ID document capture metadata.
- Add manual ID verification state.
- Add future connector boundary for OCR/passport scanner.

No-touch rule:

- Do not edit Python OCR/AI internals if any are later used.
- Use API integration boundaries and external service clients.

Acceptance criteria:

- Reception can record ID type, ID hash/reference, verification result, and verifier.
- Raw sensitive ID images follow evidence retention/privacy policy from Phase 8.

### 4.3 Watchlist And Blocklist Screening

Plan:

- Add watchlist entities:
  - PersonWatchlist,
  - VehicleWatchlist,
  - WatchlistMatch,
  - MatchReview.
- Add match statuses:
  - pending,
  - confirmed,
  - false positive,
  - escalated,
  - closed.

Acceptance criteria:

- Visitor or vehicle match triggers alarm/event.
- False positives are reviewed and auditable.

### 4.4 Escort, NDA, Safety, And Compliance Forms

Plan:

- Add visit rules:
  - escort required,
  - NDA required,
  - safety briefing required,
  - area-specific instruction required.
- Add form template and acceptance records.

Acceptance criteria:

- Visitor cannot check in for restricted zones until required forms are accepted.
- Form acceptance is versioned.

### 4.5 Vehicle, Parking, And Lane Operations

Current gap:

- Vehicles and ALPR exist, but parking/lane policy is shallow.

Plan:

- Add:
  - ParkingArea,
  - ParkingPermit,
  - Lane,
  - Barrier,
  - VehicleAccessPolicy,
  - LaneEvent,
  - BarrierCommandAudit.
- Add vehicle credential types:
  - license plate,
  - RFID/card,
  - QR visitor pass,
  - manual override.

Acceptance criteria:

- A vehicle access decision can be evaluated by plate, driver, visitor pass, lane, time, and parking policy.
- Barrier open/close commands are audited and role-restricted.

## 11. Phase 5 - Device, Protocol, And Offline Resilience Layer

Target:

- Lift readiness from 73% to about 80%.

### 5.1 Device Registry And Topology

Current gap:

- Runtime wrappers exist, but no true physical access control device model.

Plan:

- Add device entities:
  - Device,
  - AccessController,
  - Reader,
  - Relay,
  - DoorSensor,
  - REXSensor,
  - TamperSensor,
  - BarrierController,
  - CameraDevice,
  - IntercomDevice,
  - DeviceCredential,
  - DeviceHealthSnapshot,
  - DeviceConfigurationVersion.

Acceptance criteria:

- Every physical/security device has owner, site, location, type, status, last seen, firmware/config version, and health state.
- Device changes are audited.

### 5.2 Connector Architecture

No-touch rule:

- Do not edit `AI_Runtime/**` or `runtime/**`.

Plan:

- Add connector interfaces in API/service layer:
  - access-control connector,
  - camera connector,
  - ALPR connector,
  - face recognition connector,
  - go2rtc health/status connector,
  - notification connector.
- Wrap existing runtime services behind these interfaces.
- Add timeouts, retries, circuit breakers, and degraded-mode states.

Acceptance criteria:

- If a runtime dependency is down, API returns controlled degraded response, not raw exception.
- Runtime service health appears in system health dashboard.

### 5.3 OSDP And ONVIF Integration Boundary

Plan:

- Define OSDP-compatible reader/controller abstraction.
- Define ONVIF Profile A/C compatible access-control abstraction.
- Start with simulation/mock connector before real hardware.
- Add event normalization:
  - access granted,
  - access denied,
  - door forced,
  - door held open,
  - tamper,
  - offline,
  - restored,
  - credential changed.

Acceptance criteria:

- The business layer does not depend directly on a vendor SDK.
- A simulator can generate access/device events for tests.

### 5.4 Offline Panel Decisioning Strategy

Plan:

- Add policy export package for edge/panel decisions:
  - credential list,
  - access rules,
  - schedule windows,
  - emergency state,
  - revocation list,
  - package version/signature.
- Add sync status and conflict handling.

Acceptance criteria:

- The system can explain whether an access decision was made centrally or offline.
- Offline events reconcile back to central logs without losing audit trail.

### 5.5 Secure Device Provisioning

Plan:

- Add device enrollment workflow:
  - pending device,
  - approve,
  - assign site/location,
  - issue credential/certificate/secret,
  - activate,
  - rotate,
  - revoke.

Acceptance criteria:

- Device secrets are not displayed after creation.
- Device credential rotation is audited.

## 12. Phase 6 - Video, AI, Sensor Fusion, And Situational Awareness

Target:

- Lift readiness from 80% to about 85%.

### 6.1 Event Correlation Layer

Current gap:

- Face, plate, gate, access, and video views exist, but correlation is not deep.

Plan:

- Add normalized event model:
  - source type,
  - source ID,
  - subject,
  - vehicle,
  - site/location,
  - severity,
  - timestamp,
  - confidence,
  - evidence references,
  - correlation ID.
- Add correlation rules:
  - denied access + face mismatch,
  - plate watchlist + gate attempt,
  - visitor overstay + zone event,
  - door forced + nearby camera clip,
  - device offline + repeated access failures.

Acceptance criteria:

- A single incident/alarm can link access log, camera snapshot/video, face result, plate result, and device event.

### 6.2 VMS-Grade Search And Playback Boundary

No-touch rule:

- Do not edit go2rtc runtime config inside `runtime/**`.

Plan:

- Add API-side metadata for clips/snapshots.
- Add video bookmark and retrieval abstraction.
- Add search by:
  - person,
  - vehicle,
  - plate,
  - location,
  - alarm type,
  - time range.

Acceptance criteria:

- Operators can find evidence by event context without browsing raw folders.
- Artifact access remains authorized and audited.

### 6.3 Maps And Situational Views

Plan:

- Add floor/site map model:
  - map asset,
  - coordinate system,
  - device positions,
  - alarm overlay,
  - occupancy overlay.
- Start with static map upload and coordinate placement.

Acceptance criteria:

- Operator can see active alarms and devices by site/floor/zone.

### 6.4 AI Adjudication And Model Governance

Current gap:

- AI runtime exists, but no false-positive/drift workflow.

No-touch rule:

- Do not edit AI model code or training code in `AI_Runtime/**`.

Plan:

- Add review queue for AI events:
  - face match,
  - no match,
  - low confidence,
  - plate read,
  - suspicious event.
- Add adjudication results:
  - confirmed,
  - false positive,
  - false negative,
  - needs retraining,
  - ignored.
- Add AI performance reporting:
  - confidence distribution,
  - false positive rate,
  - false negative reports,
  - camera-specific quality issues.

Acceptance criteria:

- Operators can correct AI outcomes without modifying AI internals.
- AI review statistics are reportable.

## 13. Phase 7 - SOC, Incident Command, Lockdown, Guard Operations

Target:

- Lift readiness from 85% to about 90%.

### 7.1 Central Alarm Queue

Current gap:

- Monitoring screens exist, but no full SOC workflow.

Plan:

- Add alarm entities:
  - Alarm,
  - AlarmType,
  - AlarmRule,
  - AlarmState,
  - AlarmAssignment,
  - AlarmComment,
  - AlarmSlaPolicy.
- Alarm states:
  - New,
  - Acknowledged,
  - Assigned,
  - Investigating,
  - Escalated,
  - Resolved,
  - FalseAlarm,
  - Closed.

Acceptance criteria:

- No high-severity alarm can disappear without acknowledgement/closure.
- Alarm queue supports filtering by site, severity, type, assignee, and SLA status.

### 7.2 SOP Checklist Engine

Plan:

- Add SOP templates per alarm type.
- Add checklist execution on alarm/incident.
- Add mandatory steps and evidence prompts.

Acceptance criteria:

- Operator must complete required SOP steps before closing selected alarm types.
- SOP version used during incident is preserved.

### 7.3 Incident Case Management

Plan:

- Add incident entities:
  - Incident,
  - IncidentTimelineItem,
  - IncidentParticipant,
  - IncidentEvidence,
  - IncidentDecision,
  - AfterActionReview.
- Allow alarms to be grouped into incident cases.

Acceptance criteria:

- A security event can evolve from alarm to incident with timeline and evidence bundle.
- Incident closure requires outcome, classification, and owner.

### 7.4 Guard Dispatch And Shift Handover

Plan:

- Add dispatch task:
  - site/location,
  - priority,
  - assigned guard,
  - status,
  - deadline,
  - notes,
  - photos/evidence references.
- Add shift handover notes:
  - open alarms,
  - active incidents,
  - high-risk visitors,
  - offline devices,
  - pending approvals.

Acceptance criteria:

- Guard tasks are traceable from alarm to completion.
- Shift handover captures unresolved operational risk.

### 7.5 Emergency Operations

Plan:

- Add emergency dashboards:
  - lockdown state,
  - occupancy by zone,
  - muster list,
  - unaccounted people,
  - visitors currently onsite,
  - disabled/offline devices.

Acceptance criteria:

- During evacuation/lockdown, operator sees who is known onsite and where.
- Emergency state changes are step-up protected and audited.

## 14. Phase 8 - Evidence, Privacy, Compliance, And Audit Governance

Target:

- Lift readiness from 90% to about 94%.

### 8.1 Evidence Repository

Current gap:

- Artifacts are protected, but no enterprise evidence governance.

Plan:

- Add evidence entities:
  - EvidenceItem,
  - EvidenceCollection,
  - EvidenceAccessLog,
  - EvidenceExportRequest,
  - EvidenceRetentionPolicy,
  - LegalHold,
  - ChainOfCustodyEntry.
- Evidence types:
  - access log,
  - audit log,
  - face snapshot,
  - face video,
  - license plate image,
  - camera clip,
  - visitor ID reference,
  - incident report,
  - exported bundle.

Acceptance criteria:

- Every evidence read/export is audited.
- Evidence can be attached to alarms/incidents.

### 8.2 Retention And Legal Hold

Plan:

- Add retention policy by evidence type, site, severity, incident status, and privacy sensitivity.
- Add legal hold that overrides deletion.
- Add deletion queue with audit proof.

Acceptance criteria:

- Expired evidence is deleted or archived according to policy.
- Evidence under legal hold cannot be deleted.

### 8.3 Chain Of Custody And Export Governance

Plan:

- Add export approval workflow.
- Add export bundle hash/signature.
- Add watermarking metadata where applicable.
- Add export reason and recipient.

Acceptance criteria:

- Exported evidence has traceable hash, requester, approver, time, contents, and purpose.

### 8.4 Privacy And Redaction

Plan:

- Add privacy labels:
  - biometric,
  - personal identity,
  - vehicle identity,
  - visitor document,
  - sensitive site.
- Add redaction workflow:
  - request,
  - approve,
  - perform,
  - verify,
  - publish/export.

No-touch rule:

- If AI-assisted redaction is later used, call it through a wrapper. Do not edit AI runtime internals.

Acceptance criteria:

- Privacy-sensitive artifacts can be restricted by role and purpose.
- Redaction actions are audited.

### 8.5 Compliance Reporting

Plan:

- Add reports:
  - access review report,
  - terminated-user access revocation report,
  - visitor log report,
  - evidence access report,
  - privileged action report,
  - alarm SLA report,
  - device health report.

Acceptance criteria:

- Security managers can export compliance reports without database access.

## 15. Phase 9 - HA/DR, Observability, Cyber Operations

Target:

- Lift readiness from 94% to about 97%.

### 9.1 High Availability And Disaster Recovery

Current gap:

- Docker and health checks exist, but HA/DR is not proven.

Plan:

- Define deployment targets:
  - single-site pilot,
  - medium company,
  - multi-site enterprise.
- Add HA design:
  - stateless API replicas,
  - database backup/restore,
  - read/write considerations,
  - file/object storage strategy,
  - queue/outbox for events,
  - runtime dependency degradation.
- Define RTO/RPO:
  - pilot,
  - production,
  - critical-site profile.

Acceptance criteria:

- Backup restore is tested.
- RTO/RPO values are documented and measured.

### 9.2 Event Queue And Outbox

Plan:

- Add durable event outbox for:
  - access events,
  - alarms,
  - device events,
  - audit events,
  - notification jobs,
  - evidence jobs.

Acceptance criteria:

- Important events are not lost when downstream services are temporarily down.

### 9.3 Metrics, Tracing, Alerting

Plan:

- Add metrics for:
  - API latency,
  - auth failures,
  - access decision latency,
  - runtime dependency health,
  - alarm queue age,
  - device offline count,
  - failed evidence exports,
  - background job failures.
- Add trace correlation across API, job, audit, and event records.

Acceptance criteria:

- Operators can distinguish app failure, DB failure, runtime failure, and device failure.

### 9.4 SIEM And Webhook Integration

Plan:

- Add SIEM export for:
  - authentication events,
  - privileged actions,
  - alarms,
  - evidence exports,
  - device tamper/offline,
  - policy changes.
- Add signed webhooks with retries and dead-letter handling.

Acceptance criteria:

- External security systems can consume critical V-Shield events.

### 9.5 Secrets, Keys, And Vulnerability Management

Plan:

- Add key rotation procedure.
- Add secrets inventory.
- Add dependency vulnerability checks.
- Add container image scanning.
- Add patch cadence.

Acceptance criteria:

- Production secrets can be rotated without code edits.
- Known critical vulnerabilities block release unless waived.

## 16. Phase 10 - Commercial QA And Release Readiness

Target:

- Lift readiness from 97% to 100%.

### 10.1 Test Matrix

Plan:

- Expand tests across:
  - unit tests,
  - API integration tests,
  - E2E browser tests,
  - migration tests,
  - policy evaluation tests,
  - hardware simulation tests,
  - runtime degradation tests,
  - evidence governance tests,
  - alarm workflow tests.

Acceptance criteria:

- Test matrix maps to all scorecard categories from assessment.

### 10.2 Load, Stress, Soak, And Chaos

Plan:

- Define load profiles:
  - 1 site / 10 gates / 50 cameras / 500 users,
  - 5 sites / 50 gates / 200 cameras / 5,000 users,
  - 20 sites / 200 gates / 1,000 cameras / 50,000 credentials.
- Test:
  - login storm,
  - access event burst,
  - alarm burst,
  - video metadata search,
  - evidence export,
  - DB failover simulation,
  - runtime outage,
  - queue backlog recovery.

Acceptance criteria:

- Performance budgets are defined and measured.
- System degrades predictably under dependency failure.

### 10.3 Migration And Rollback Safety

Plan:

- Add migration smoke tests.
- Add rollback runbooks.
- Add production migration checklist:
  - backup,
  - migration,
  - smoke test,
  - rollback trigger,
  - post-check.

Acceptance criteria:

- New DB migrations include tested forward path and documented rollback plan.

### 10.4 Release Gates

Plan:

- Define release gates:
  - all tests pass,
  - build succeeds,
  - no critical vulnerabilities,
  - no no-touch modifications unless explicitly approved,
  - migration reviewed,
  - docs updated,
  - operator notes updated.

Acceptance criteria:

- A release cannot be considered production-ready without gate evidence.

### 10.5 Product Documentation

Plan:

- Create:
  - admin manual,
  - operator manual,
  - incident response runbook,
  - visitor desk guide,
  - device enrollment guide,
  - deployment guide,
  - backup/restore guide,
  - privacy/evidence governance guide.

Acceptance criteria:

- A new operator can run normal workflows without reading source code.

## 17. Detailed Backlog By Workstream

### Workstream A - Security Closure

- [ ] Add explicit role authorization to `FaceCameraController`.
- [ ] Remove `WeatherForecastController`.
- [ ] Add controller route trust inventory test.
- [ ] Add Staff-denial tests for camera operations.
- [ ] Add production startup hardening checks.
- [ ] Add ASVS tracking checklist.

### Workstream B - Identity And HR Lifecycle

- [ ] Add external IdP configuration.
- [ ] Add OIDC login boundary.
- [ ] Add external subject mapping.
- [ ] Add SCIM/import lifecycle boundary.
- [ ] Add recovery codes.
- [ ] Add step-up MFA.
- [ ] Add privileged session TTL.
- [ ] Add MFA reset approval flow.
- [ ] Add employee lifecycle states.
- [ ] Add onboarding/offboarding workflows.
- [ ] Add access recertification.

### Workstream C - Site And Organization Model

- [ ] Add Company/Site/Building/Floor/Zone/Area model.
- [ ] Add AccessPoint/Door/Gate/Lane/MusterPoint model.
- [ ] Add hierarchy management APIs.
- [ ] Add hierarchy management UI.
- [ ] Backfill current gates/cameras into default site.
- [ ] Preserve historical labels for logs.

### Workstream D - Access Policy Engine

- [ ] Add AccessLevel and AccessGroup.
- [ ] Add Schedule and HolidayCalendar.
- [ ] Add TemporaryAccessGrant.
- [ ] Add policy versioning.
- [ ] Add explainable policy evaluation result.
- [ ] Add approval workflow for sensitive access.
- [ ] Add anti-passback state.
- [ ] Add occupancy counters.
- [ ] Add lockdown/duress/emergency override policy.

### Workstream E - Visitor And Contractor Operations

- [ ] Add visitor lifecycle states.
- [ ] Add host notification workflow.
- [ ] Add visitor check-in/check-out.
- [ ] Add reception dashboard.
- [ ] Add kiosk mode boundary.
- [ ] Add ID verification metadata.
- [ ] Add person watchlist/blocklist.
- [ ] Add escort-required rules.
- [ ] Add NDA/safety form templates.
- [ ] Add overstay alerts.
- [ ] Add visitor evacuation/muster report.

### Workstream F - Vehicle, Parking, Gate, ALPR

- [ ] Add ParkingArea and ParkingPermit.
- [ ] Add Lane and Barrier model.
- [ ] Add vehicle watchlist.
- [ ] Add lane event model.
- [ ] Add barrier command audit.
- [ ] Add vehicle access policy evaluation.
- [ ] Add ALPR match review queue.
- [ ] Add multi-lane dashboard.

### Workstream G - Device And Protocol Layer

- [ ] Add device registry.
- [ ] Add controller/reader/relay/sensor models.
- [ ] Add device health snapshots.
- [ ] Add tamper/fault events.
- [ ] Add connector interfaces.
- [ ] Wrap existing runtime services without editing runtime internals.
- [ ] Add OSDP-compatible abstraction.
- [ ] Add ONVIF Profile A/C-compatible abstraction.
- [ ] Add device simulator.
- [ ] Add offline policy package model.
- [ ] Add device provisioning and credential rotation.

### Workstream H - Video, AI, And Situational Awareness

- [ ] Add normalized security event model.
- [ ] Add event correlation rules.
- [ ] Add evidence references on events.
- [ ] Add video metadata search.
- [ ] Add camera bookmark/clip abstraction.
- [ ] Add floor/site map model.
- [ ] Add device/alarm overlays.
- [ ] Add AI adjudication queue.
- [ ] Add false-positive/false-negative review.
- [ ] Add AI quality/performance reports.

### Workstream I - SOC And Incident Command

- [ ] Add central alarm queue.
- [ ] Add alarm rules and severity.
- [ ] Add alarm acknowledgement/assignment/closure.
- [ ] Add SLA timers.
- [ ] Add SOP templates.
- [ ] Add SOP execution checklist.
- [ ] Add incident cases.
- [ ] Add incident timeline.
- [ ] Add evidence bundle on incident.
- [ ] Add guard dispatch tasks.
- [ ] Add shift handover.
- [ ] Add emergency dashboard.
- [ ] Add evacuation/muster view.

### Workstream J - Evidence And Compliance

- [ ] Add evidence repository model.
- [ ] Add evidence collections.
- [ ] Add evidence read/export audit.
- [ ] Add retention policy.
- [ ] Add legal hold.
- [ ] Add chain of custody.
- [ ] Add export approval.
- [ ] Add bundle hashing/signature.
- [ ] Add privacy labels.
- [ ] Add redaction workflow.
- [ ] Add compliance reports.

### Workstream K - Operations, HA/DR, Observability

- [ ] Define HA deployment profiles.
- [ ] Add backup/restore runbooks.
- [ ] Add backup restore test.
- [ ] Add RTO/RPO targets.
- [ ] Add durable event outbox.
- [ ] Add background job observability.
- [ ] Add metrics.
- [ ] Add tracing.
- [ ] Add alerting.
- [ ] Add SIEM export.
- [ ] Add signed webhooks.
- [ ] Add secrets rotation procedure.
- [ ] Add dependency/container vulnerability checks.

### Workstream L - QA And Release

- [ ] Add E2E browser tests.
- [ ] Add migration tests.
- [ ] Add policy engine test suite.
- [ ] Add hardware simulator tests.
- [ ] Add runtime degradation tests.
- [ ] Add load tests.
- [ ] Add stress tests.
- [ ] Add soak tests.
- [ ] Add chaos/failover tests.
- [ ] Add release checklist.
- [ ] Add operator/admin/deployment manuals.

## 18. Suggested Data Model Additions

This is a planning list, not final schema.

Identity and organization:

- Company
- Site
- Building
- Floor
- Zone
- Area
- DepartmentSiteAssignment
- PersonLifecycleState
- ExternalIdentity
- AccessRecertificationCampaign
- AccessRecertificationDecision

Access control:

- AccessPoint
- Door
- Lane
- AccessLevel
- AccessGroup
- AccessRule
- Schedule
- HolidayCalendar
- TemporaryAccessGrant
- AccessPolicyVersion
- AccessDecision
- AntiPassbackState
- OccupancySnapshot
- EmergencyState

Visitor/vehicle:

- Visit
- VisitParticipant
- VisitorCredential
- VisitorCheckIn
- VisitorFormTemplate
- VisitorFormAcceptance
- WatchlistEntry
- WatchlistMatch
- ParkingArea
- ParkingPermit
- Barrier
- LaneEvent

Device:

- Device
- AccessController
- Reader
- Relay
- Sensor
- DeviceCredential
- DeviceHealthSnapshot
- DeviceConfigVersion
- DeviceProvisioningRequest
- OfflinePolicyPackage

SOC:

- SecurityEvent
- Alarm
- AlarmRule
- AlarmAssignment
- AlarmComment
- SopTemplate
- SopExecution
- Incident
- IncidentTimelineItem
- DispatchTask
- ShiftHandover

Evidence/compliance:

- EvidenceItem
- EvidenceCollection
- EvidenceAccessLog
- EvidenceExportRequest
- RetentionPolicy
- LegalHold
- ChainOfCustodyEntry
- RedactionRequest
- ComplianceReportRun

Operations:

- OutboxEvent
- WebhookSubscription
- WebhookDelivery
- RuntimeDependencyHealth
- BackupRun
- RestoreDrill

## 19. Suggested API Surface

This is a planning list, not final route design.

Identity:

- `/api/identity/providers`
- `/api/identity/external-mappings`
- `/api/auth/mfa/recovery-codes`
- `/api/auth/step-up`
- `/api/users/{id}/lifecycle`
- `/api/access-recertifications`

Hierarchy:

- `/api/sites`
- `/api/sites/{id}/buildings`
- `/api/buildings/{id}/floors`
- `/api/floors/{id}/zones`
- `/api/access-points`
- `/api/doors`
- `/api/lanes`

Access policy:

- `/api/access-levels`
- `/api/access-groups`
- `/api/access-rules`
- `/api/schedules`
- `/api/holiday-calendars`
- `/api/access-decisions/evaluate`
- `/api/emergency-states`
- `/api/anti-passback`
- `/api/occupancy`

Visitor/vehicle:

- `/api/visits`
- `/api/visits/{id}/check-in`
- `/api/visits/{id}/check-out`
- `/api/watchlists`
- `/api/watchlist-matches`
- `/api/parking-areas`
- `/api/parking-permits`
- `/api/barriers`
- `/api/lane-events`

Device:

- `/api/devices`
- `/api/devices/{id}/health`
- `/api/devices/{id}/configuration`
- `/api/devices/{id}/provision`
- `/api/connectors/status`
- `/api/offline-policy-packages`

SOC:

- `/api/security-events`
- `/api/alarms`
- `/api/alarms/{id}/acknowledge`
- `/api/alarms/{id}/assign`
- `/api/alarms/{id}/close`
- `/api/sop-templates`
- `/api/incidents`
- `/api/dispatch-tasks`
- `/api/shift-handovers`

Evidence/compliance:

- `/api/evidence`
- `/api/evidence/{id}/access`
- `/api/evidence-export-requests`
- `/api/legal-holds`
- `/api/retention-policies`
- `/api/redaction-requests`
- `/api/compliance-reports`

Operations:

- `/api/health/dependencies`
- `/api/metrics/summary`
- `/api/webhooks`
- `/api/outbox`
- `/api/backup-runs`
- `/api/restore-drills`

## 20. Suggested Frontend Modules

Admin:

- Company/Site hierarchy manager.
- Identity provider settings.
- User lifecycle and access recertification.
- Access policy designer.
- Device registry and provisioning.
- Evidence retention/compliance settings.
- HA/DR and integration settings.

Security operator:

- Alarm queue.
- Monitoring command center.
- Incident cases.
- SOP execution.
- Emergency lockdown dashboard.
- Guard dispatch.
- Evidence review.

Reception:

- Visitor invitation/check-in/check-out.
- ID verification.
- Host notification.
- Visitor pass issuance.
- Overstay alerts.

Vehicle/gate operator:

- Lane dashboard.
- Plate review.
- Barrier control.
- Vehicle watchlist match review.
- Parking permit lookup.

Compliance/auditor:

- Audit log search.
- Evidence access reports.
- Access recertification reports.
- Privileged action reports.
- Export history.

## 21. Verification Strategy

Every phase must run:

- API tests.
- Frontend build.
- No-touch status check.

Minimum command set:

```powershell
dotnet test API\API\API\API.sln --no-restore --verbosity minimal
npm run build
git status --short -- AI_Runtime runtime scripts\setup-public-domain.ps1 scripts\uninstall-public-domain.ps1 scripts\reset-public-domain-appsettings.ps1 scripts\read-public-domain-appsettings.ps1 scripts\update-public-domain-appsettings.ps1 setup-public-domain.bat uninstall-public-domain.bat API\API\API\appsettings.json.bak.public-domain
```

Additional checks by phase:

- Phase 1: route authorization tests.
- Phase 2: lifecycle and identity mapping tests.
- Phase 3: access policy evaluation tests.
- Phase 4: visitor/vehicle workflow tests.
- Phase 5: device simulator and degraded runtime tests.
- Phase 6: event correlation and AI review tests.
- Phase 7: alarm/SOP/incident workflow tests.
- Phase 8: evidence retention/legal hold/export tests.
- Phase 9: backup/restore, outbox, webhook, and observability tests.
- Phase 10: E2E, load, stress, soak, chaos, and migration rollback tests.

## 22. Migration Strategy

Principles:

- Additive migrations first.
- Backfill existing data into default company/site before enforcing required fields.
- Avoid destructive schema changes until data is migrated and verified.
- Preserve access logs and audit history.
- Every migration includes rollback notes.

Suggested order:

1. Add site hierarchy tables.
2. Add default company/site/building/zone records.
3. Backfill current gates/cameras/employees.
4. Add access policy tables.
5. Backfill current employee/visitor permissions into AccessLevel/AccessRule.
6. Add device registry tables.
7. Backfill current cameras/runtime references.
8. Add event/alarm/incident/evidence tables.
9. Add retention/legal hold/export tables.
10. Add operational outbox/webhook tables.

## 23. Rollout Strategy

Recommended rollout:

1. Internal test environment with copied non-sensitive data.
2. Pilot site with limited gates/cameras.
3. Parallel run: existing access logic and new policy engine both evaluate, but old result remains authoritative.
4. Shadow validation: compare old/new decisions.
5. Enable new policy engine for one low-risk zone.
6. Expand by site.
7. Enable SOC alarm workflow.
8. Enable evidence governance.
9. Enable HA/DR and SIEM integrations.
10. Declare commercial readiness only after Phase 10 gates pass.

## 24. Key Risks And Controls

| Risk | Control |
|---|---|
| Breaking AI/runtime behavior | Do not edit no-touch zones; use wrappers and simulators. |
| Breaking public-domain setup | Do not edit public-domain scripts; test only around gateway/API config. |
| Policy engine denies legitimate access | Shadow mode, explainable decisions, approval workflow, emergency override. |
| Data migration corrupts logs | Additive migration, backups, backfill verification, rollback plan. |
| Operators reject complex UI | Role-specific UI, staged rollout, operator testing. |
| Evidence retention deletes needed artifacts | Legal hold, retention dry-run, approval before purge. |
| Device integration locks into one vendor | Connector abstraction and simulator-first design. |
| Scale fails under event bursts | Outbox, load tests, queue backpressure, metrics. |
| Privileged users bypass controls | Step-up MFA, short privileged TTL, immutable audit trail. |

## 25. Final Priority Recommendation

Do not start with device protocols or SOC dashboards first. The correct order is:

1. Close immediate security gaps.
2. Add site/company hierarchy.
3. Add HR/person lifecycle.
4. Build the access policy engine.
5. Complete visitor/vehicle operations.
6. Add device/protocol abstraction and simulator.
7. Add event correlation, SOC, evidence governance, and HA/DR.
8. Finish with commercial QA gates.

Reason:

- Device/SOC/evidence features need stable identities, locations, access points, and policy decisions underneath them.
- Without site hierarchy and policy engine, later features will become one-off patches.
- Without no-touch discipline, Python/runtime/public-domain areas can become unstable and hard to recover.

## 26. Success Milestones

| Milestone | Readiness | Exit proof |
|---|---:|---|
| M1 Security closure | 52% | Route tests pass, demo endpoint removed, camera control restricted. |
| M2 Enterprise foundation | 60% | Site hierarchy and HR lifecycle exist. |
| M3 Access policy engine | 68% | Explainable scheduled policy decisions work. |
| M4 Visitor/vehicle operations | 73% | Full visitor and vehicle workflows work end to end. |
| M5 Device resilience | 80% | Device registry, simulator, connector boundary, offline package model exist. |
| M6 Situational awareness | 85% | Events correlate across access/video/AI/device data. |
| M7 SOC command center | 90% | Alarm, SOP, incident, guard, emergency workflows work. |
| M8 Evidence/compliance | 94% | Retention, legal hold, chain of custody, export governance work. |
| M9 Operations readiness | 97% | HA/DR, SIEM, observability, backup/restore tested. |
| M10 Commercial readiness | 100% | E2E/load/chaos/migration/release gates pass with docs. |
