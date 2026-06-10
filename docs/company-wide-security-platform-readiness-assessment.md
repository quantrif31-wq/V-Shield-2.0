# V-Shield 2.0 - Company-Wide Security Platform Readiness Assessment

Date: 2026-06-10

## Executive Conclusion

When evaluated as a custom company security application for gates, employees, visitors, QR access, face recognition, license plates, audit logs, and attendance support, V-Shield 2.0 is now in a strong MVP+/pilot state.

When evaluated against commercial medium/large company-wide physical security platforms such as Genetec Security Center, Johnson Controls C-CURE, and Avigilon Alta, the current source is approximately:

**45% ready for enterprise company-wide security control.**

This score is intentionally strict. The codebase now has a much safer application-security base than before, but a commercial-grade company security platform is broader than API security and gate scanning. It must cover multi-site physical access control, door/panel hardware, visitor lifecycle, SOC alarm workflows, incident command, evidence governance, high availability, integrations, and operational resilience.

## External Benchmark

The benchmark used for this assessment combines commercial product capabilities and public security standards:

- Genetec Security Center positions itself as a unified security platform combining access control, video surveillance, ALPR, communications, and related security operations in one solution: https://www.genetec.com/products/unified-security/security-center
- Genetec documentation describes unified control of video, access control, and ALPR edge devices, plus monitoring, reporting, event/alarm management, video search, playback, and third-party ecosystem integration: https://techdocs.genetec.com/r/en-US/Security-Center-Administrator-Guide-5.14/About-Security-Center
- Johnson Controls C-CURE 9000 is described as centralized access control and monitoring for businesses of varying sizes, with enterprise architecture and external integrations: https://www.johnsoncontrols.com/security/access-control/access-control-software
- Johnson Controls C-CURE 9000 v3.0 enterprise material describes master/satellite application server architecture and large-scale reader/credential capacity: https://docs.johnsoncontrols.com/softwarehouse/api/khub/documents/HfzfGzrJBLUsKYQ5h5nOXA/content
- Avigilon Alta Visitor includes visitor pre-registration, QR/PIN/badge credentials, ID validation, blocklists, host notifications, audit logs, linked video, and multi-site management: https://www.avigilon.com/alta-visitor
- Avigilon Alta Access highlights cloud management of mobile credentials, guest passes, emergency lockdowns, occupancy, remote unlock, and browser/mobile/tablet operations: https://www.avigilon.com/access-control/cloud
- NIST Cybersecurity Framework 2.0 uses Govern, Identify, Protect, Detect, Respond, and Recover as core cybersecurity outcomes: https://nvlpubs.nist.gov/nistpubs/CSWP/NIST.CSWP.29.pdf
- OWASP ASVS provides a basis for testing web application technical security controls and secure development requirements: https://owasp.org/www-project-application-security-verification-standard/
- SIA OSDP is an access control communications standard for interoperability among access control/security products and is published as IEC 60839-11-5: https://www.securityindustry.org/industry-standards/open-supervised-device-protocol/
- ONVIF Profile C covers door control and event/alarm management for electronic access control systems: https://www.onvif.org/profiles/profile-c/
- ONVIF Profile A covers access rules, credentials, schedules, status, and standardized access-control events: https://www.onvif.org/profiles/profile-a/

## Local Source Evidence

The current source contains real functional coverage in these areas:

- Authentication and authorization hardening in `API/API/API/Program.cs`: fallback authenticated policy, role policies, JWT token version validation, upload protection middleware, correlation ID, safe exception handling, rate limiter, and health endpoints.
- Role-protected operational APIs across access logs, employees, users, vehicles, biometrics, device management, face recognition, license plates, gate transit, QR access, runtime services, and video artifacts under `API/API/API/Controllers`.
- Domain models and database sets for employees, departments, positions, gates, cameras, vehicles, visitors, pre-registration, guest profiles, access permissions, dynamic QR, face artifacts, access logs, audit logs, refresh tokens, and attendance modules in `API/API/API/Data/ApplicationDbContext.cs` and `ApplicationDbContext.Attendance.cs`.
- Frontend routes for dashboard, monitoring, access logs, system audit, pre-registration, guest profiles, face ID, license plate, face video, gate transit, QR generator/scanner, QR monitor, access permission management, biometrics, employees, vehicles, device management, catalogs, users, settings, and attendance workflows in `View/src/router/index.js`.
- Docker deployment surfaces for SQL Server, API, frontend, go2rtc, optional tunnel, QR runtime, and license plate runtime in `docker-compose.yml`.
- Security boundary tests in `API/API/API.Tests/SecurityBoundaryTests.cs`, including public endpoint boundaries, anonymous rejection for privileged endpoints, protected uploads, correlation ID, readiness/degraded health, safe exception envelope, login/refresh/logout, refresh-token rotation, role-based forbidden access, and TOTP setup/login.

Verification run on 2026-06-10:

- `dotnet test API\API\API\API.sln --no-restore --verbosity minimal`: passed, 22/22 tests.
- `npm run build` in `View`: passed.

## Scorecard

| Area | Weight | Current Score | Weighted Result | Assessment |
|---|---:|---:|---:|---|
| Identity, authentication, authorization, session security | 10 | 75 | 7.5 | Strong for current app: MFA, refresh rotation, hashed refresh tokens, token invalidation, role policies, default-deny fallback. Missing SSO/IdP, SCIM, recovery codes, privileged access workflow. |
| People, organization lifecycle, HR integration, multi-site hierarchy | 8 | 35 | 2.8 | Employees, departments, positions exist. Missing site/building/floor/zone hierarchy, HR lifecycle sync, onboarding/offboarding automation, contractors, recertification. |
| Physical access control policy model | 12 | 45 | 5.4 | Employee/visitor permissions and gates exist. Missing door/panel model, schedules/holidays, access levels, anti-passback, occupancy rules, duress, emergency lockdown policy, approval workflow. |
| Visitor and contractor lifecycle | 8 | 60 | 4.8 | Pre-registration, guest profiles, visitor pass, QR flows are solid for a pilot. Missing kiosk mode, ID scan, blocklists, host self-service, escort rules, overstay escalation, NDA/safety forms, multi-site visitor governance. |
| Vehicle, parking, gate, and ALPR operations | 7 | 55 | 3.9 | Vehicles, gates, license plate runtime, and transit monitoring exist. Missing parking policy, barrier controller integration, exception adjudication workflow, vehicle watchlists, lane device health, multi-lane scale rules. |
| Video, AI, sensor fusion, and situational awareness | 10 | 45 | 4.5 | Face/video/license-plate modules and go2rtc exist. Missing VMS-grade playback/search, camera maps, alarm correlation, camera health SLA, model drift tracking, false-positive review, event-to-video case linking at enterprise depth. |
| SOC workflows, incident response, lockdown, guard operations | 12 | 20 | 2.4 | Monitoring pages exist, but there is no full alarm queue, severity model, SOP checklist, dispatch, escalation, incident case, guard tour, evacuation/muster, or command center workflow. |
| Evidence, audit, privacy, compliance | 9 | 55 | 5.0 | System audit logs, correlation IDs, protected artifacts, and access logs are now meaningful. Missing retention policies, legal hold, chain of custody, export signing, privacy masking/redaction, immutable audit storage, compliance reporting. |
| Device/protocol integration and offline resilience | 10 | 25 | 2.5 | Camera/runtime service wrappers exist. Missing OSDP, ONVIF Profile A/C, access panels/readers/controllers, offline local decisions, tamper events, reader supervision, secure device provisioning, firmware/config lifecycle. |
| Operations, HA/DR, observability, cyber hardening | 9 | 45 | 4.1 | Docker, health checks, safe exceptions, rate limits, secret requirements are present. Missing HA API/DB design, backup/restore procedure, RTO/RPO, SIEM integration, full metrics/tracing, key rotation, vulnerability/dependency process. |
| QA, load/stress, deployment maturity | 5 | 45 | 2.3 | Security integration tests and frontend build pass. Missing E2E browser tests, hardware simulation tests, load/stress tests, chaos/failover drills, migration rollback tests, release gates. |

Total weighted result: **45.2 / 100**, rounded to **45%**.

## What Is Already Good

The strongest improvement is the application security layer. The current backend is no longer an open demo-style API. Default authentication, role checks, protected uploads, safe errors, audit context, refresh-token rotation, MFA for sensitive roles, and automated security tests are real progress.

The second strong area is core gate workflow coverage. The source already understands employees, visitors, vehicles, gates, cameras, QR flows, face recognition, plate recognition, logs, exceptions, and attendance. That is enough to demonstrate a credible security-control pilot for one organization or one site.

The frontend also maps to real operations: monitoring, logs, pre-registration, guest profiles, device management, biometrics, face/video, license plate, gate transit, QR access, users, and settings. It is not just a passive admin CRUD shell.

## High-Risk Gaps Found In Source

1. `API/API/API/Controllers/FaceCameraController.cs` has no explicit role authorization. Because fallback authentication exists, anonymous users are blocked, but any authenticated user can reach camera proxy controls unless blocked elsewhere. Camera on/off/reset/status/result/locked-images should be restricted to `Admin,BaoVe` or a runtime operator policy.

2. `API/API/API/Controllers/WeatherForecastController.cs` is still present as a template/demo endpoint. It is protected by fallback authentication, but it should be removed from production code to reduce accidental surface area.

3. Access permission is still too simple for medium/large company-wide use. It does not yet model full access levels, door groups, schedules, holidays, temporary exceptions, approval chains, anti-passback, occupancy limits, duress, lockdown, or emergency override.

4. Device integration is still wrapper-based, not a true physical access control system. There is no OSDP/ONVIF Profile A/C controller integration, no reader supervision, no offline panel decisioning, no device certificate/provisioning lifecycle, and no tamper/fault workflow.

5. Security operations are missing a proper SOC/incident layer. There is no event queue with severity, alarm acknowledgement, SOP checklist, escalation, dispatch, case timeline, evidence bundle, or after-action review.

6. Evidence governance is not enterprise-grade yet. Audit exists, but retention, immutability, chain of custody, legal hold, watermarking, export approval, redaction, and privacy reporting are not implemented.

7. Resilience and scale are not yet proven. The system has Docker and health checks, but no high-availability architecture, backup/restore drills, load/stress results, failover tests, disaster recovery targets, or queue/outbox design.

## Business Workflow Coverage

### Employee Lifecycle

Current coverage:

- Employee records.
- Departments and positions.
- User accounts and roles.
- Face/QR/vehicle linkage.
- Attendance records, schedules, leave requests, and reports.

Missing for enterprise:

- HR system integration.
- Automated onboarding/offboarding.
- Contractor lifecycle.
- Temporary assignments.
- Access recertification.
- Separation/termination instant revocation.
- Manager/security approval chain.
- Site/building/floor/zone assignment.

Estimated readiness: **35-45%**.

### Physical Access Control

Current coverage:

- Gates.
- Employee/visitor access permissions.
- QR access validation.
- Face/plate assisted recognition.
- Access logs and exceptions.

Missing for enterprise:

- Door/panel/controller/reader model.
- OSDP secure reader communication.
- ONVIF Profile A/C interoperability.
- Time schedules, holidays, and access levels.
- Anti-passback and occupancy.
- Duress and emergency unlock/lockdown.
- Offline decisions at panels.
- Reader tamper and device fault alarms.

Estimated readiness: **35-45%**.

### Visitor Management

Current coverage:

- Registration links.
- Pre-registration.
- Guest profiles.
- Visitor pass.
- QR-based visitor access.
- Host relationship through employee data.

Missing for enterprise:

- Self-service lobby kiosk.
- ID/passport scan and validation.
- Blocklist/watchlist screening.
- Host mobile notification.
- Escort-required rules.
- NDA/safety/compliance forms.
- Overstay alerts.
- Visitor evacuation/muster.
- Multi-site visitor governance.

Estimated readiness: **55-65%**.

### SOC And Incident Operations

Current coverage:

- Monitoring screens.
- Access logs.
- Exception reasons.
- Audit logs.
- Face/plate/gate runtime views.

Missing for enterprise:

- Central alarm queue.
- Severity and priority.
- Acknowledge/assign/close workflow.
- SOP checklist per alarm type.
- Guard dispatch.
- Escalation and SLA timers.
- Incident case timeline.
- Evidence bundle.
- Shift handover.
- Post-incident review.

Estimated readiness: **20-30%**.

### Cybersecurity And Application Assurance

Current coverage:

- Default-deny authentication fallback.
- Role policies.
- MFA for sensitive roles.
- Refresh-token rotation and hashed storage.
- Logout/token invalidation.
- Rate limiting.
- Safe exception envelope.
- Correlation ID.
- Protected uploads.
- Health endpoints.
- Automated security boundary tests.

Missing for enterprise:

- SSO/IdP integration.
- Recovery codes and step-up MFA policy.
- Privileged access management.
- Full ASVS test coverage.
- Secrets rotation and key management.
- SIEM export.
- Vulnerability/dependency management workflow.
- Penetration test record.

Estimated readiness: **65-75%**.

## Roadmap To 100%

### Phase 1 - Close Immediate Security Gaps

Target lift: 45% to 50-52%.

- Add explicit `Admin,BaoVe` authorization to `FaceCameraController`.
- Remove `WeatherForecastController`.
- Add tests proving Staff cannot operate face camera controls.
- Add route inventory test to flag controllers without explicit `[Authorize]` or `[AllowAnonymous]`.
- Add production startup guard for required HTTPS/proxy/security headers.

### Phase 2 - Enterprise Access-Control Model

Target lift: 50% to 62-66%.

- Add site/building/floor/zone/door/access-point hierarchy.
- Add access levels, schedules, holidays, temporary exceptions, and policy versioning.
- Add anti-passback, occupancy, duress, emergency lockdown/unlock, and emergency override audit.
- Add approval workflows for sensitive permission changes.
- Add HR lifecycle import/sync hooks.

### Phase 3 - Real Device And Protocol Layer

Target lift: 62% to 72-78%.

- Add access controller, reader, relay, sensor, tamper, and device-health models.
- Add OSDP-compatible abstraction or connector.
- Add ONVIF Profile A/C compatible abstraction or connector.
- Add offline panel decisioning strategy.
- Add secure device provisioning, credentials, certificate/secret rotation, and config audit.
- Add simulated hardware test harness.

### Phase 4 - SOC/Incident Command

Target lift: 72% to 84-88%.

- Add central alarm queue.
- Add event correlation across access, video, face, plate, device health, and visitor data.
- Add SOP templates and checklist execution.
- Add assignment, escalation, SLA timers, guard dispatch, shift handover.
- Add incident case timeline and evidence bundle.
- Add emergency/muster dashboards.

### Phase 5 - Evidence, Compliance, And Enterprise Operations

Target lift: 84% to 94-97%.

- Add retention policies by artifact type.
- Add immutable audit/evidence option.
- Add legal hold, export approval, watermark/signature, and redaction.
- Add SIEM/webhook integrations.
- Add backup/restore runbooks and tests.
- Add HA deployment pattern and disaster-recovery targets.
- Add metrics, tracing, alerting, and operational dashboards.

### Phase 6 - Commercial Hardening

Target lift: 94% to 100%.

- Complete ASVS-oriented verification matrix.
- Add E2E browser tests for high-risk flows.
- Add load/stress/soak/chaos tests.
- Add migration rollback testing.
- Add privacy impact assessment and data retention review.
- Add deployment release gates and operator/admin manuals.

## Final Judgment

The current source is no longer at the fragile prototype stage. It is a credible controlled-access pilot with a much stronger security foundation.

However, for the stated ambition, "kiem soat an ninh toan cong ty" at medium/large scale, the missing work is mostly not about login security anymore. The missing work is enterprise physical-security depth: full access-control policy, real device/protocol integration, SOC incident workflow, evidence governance, resilience, and scale validation.

Current result: **45% enterprise-ready**.

Recommended next target: finish Phase 1 and Phase 2 first, because those will convert the system from a secured pilot into a company-wide access-control product foundation.
