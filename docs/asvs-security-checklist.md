# V-Shield 2.0 - ASVS-Oriented Security Checklist

Date: 2026-06-10

Purpose:

- Track application-security controls against an OWASP ASVS-style checklist.
- Keep this as a living checklist for the company-wide security platform plan.

Status values:

- `Implemented`
- `Partial`
- `Backlog`
- `Compensating control`

## Current Enterprise Status

| Area | Status | Current control | Remaining work |
|---|---|---|---|
| Authentication boundary | Implemented | Login, refresh, logout, token version validation, hashed refresh tokens. | Add SSO/IdP and privileged session TTL in later phases. |
| Session renewal | Implemented | Refresh-token rotation and logout revocation. | Add device/session inventory and admin session revocation UI. |
| MFA | Partial | TOTP required for Admin/BaoVe; sensitive enterprise surfaces are role-restricted and audited. | Add full step-up challenge enforcement before production use of evidence export, lockdown and credential mutation. |
| Authorization default posture | Implemented | Fallback policy requires authenticated users. | Keep route inventory tests mandatory. |
| Explicit route trust boundary | Implemented | Controller action inventory test requires `[Authorize]` or `[AllowAnonymous]`. | Extend with generated endpoint inventory in later QA phase. |
| Runtime control authorization | Implemented | Face camera, plate/camera, camera runtime, runtime services are role/policy restricted. | Standardize all future runtime-control endpoints on `RuntimeOperator`. |
| Sensitive static artifact protection | Implemented | Anonymous `/uploads/**` blocked; face/video artifact retrieval goes through authorized APIs. | Add evidence repository and retention/legal hold controls. |
| Error handling | Implemented | Safe exception middleware returns generic problem details with correlation ID. | Add SIEM/export and alerting for error patterns. |
| Logging and audit | Implemented | System audit logs include category, severity, correlation ID, client IP, user agent; evidence access logs and chain-of-custody entries exist. | Add immutable storage backend in deployment architecture. |
| Rate limiting | Implemented | Auth/public/ops rate limit policies exist. | Add distributed rate limiting if horizontally scaled. |
| Production configuration guard | Partial | Production requires external JWT secret, explicit seed credentials, valid explicit CORS origins, issuer/audience. | Add full deployment gate and secret rotation process. |
| Security headers | Partial | API emits `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`. | Add gateway-level HSTS/CSP policy once deployment topology is fixed. |
| Public endpoint minimization | Implemented | Public endpoints are documented; demo WeatherForecast endpoint removed. | Keep public route review as a release gate. |
| Dependency/runtime failure handling | Implemented | Degraded health reports runtime status; operations module tracks runtime dependency health, outbox, webhook delivery, SIEM export, backup and restore drills. | Wire external alerting in the target deployment. |

## Enterprise Additions Completed

| Area | Status | Current control | Remaining work |
|---|---|---|---|
| Company/site hierarchy | Implemented | Company, site, building, floor, zone, access point, door, lane and muster point model plus Admin APIs. | Backfill live production assets during rollout. |
| HR lifecycle | Implemented | Employee lifecycle states, external identity mapping boundary and access recertification records. | Connect actual HR/IdP feed. |
| Access policy | Implemented | Access levels, groups, schedules, holidays, temporary grants, emergency states, anti-passback, occupancy and explainable decisions. | Run shadow-mode comparison with live gate decisions. |
| Visitor/vehicle | Implemented | Visit lifecycle, credentials, forms, watchlists, parking, barriers and lane events. | Add receptionist/operator UI screens. |
| Device/protocol boundary | Implemented | Device registry, controller/reader/relay/sensor models, health snapshots, provisioning and offline policy packages. | Certify real OSDP/ONVIF partner connectors. |
| Situational awareness | Implemented | Normalized security events, correlation, video bookmarks, maps, AI review queue and AI metrics. | Connect production VMS/video metadata provider. |
| SOC/incident command | Implemented | Alarm queue, alarm rules, comments, SOP templates/executions, incident timeline, dispatch, handover and muster snapshots. | Exercise incident drills with operators. |
| Evidence/privacy/compliance | Implemented | Evidence repository, collections, access logs, retention, legal hold, export approval, chain of custody, redaction and compliance reports. | Attach object storage immutability/legal retention features in production. |
| HA/DR/observability | Implemented | Durable outbox, signed webhook delivery, SIEM export queue, dependency health, backup runs, restore drills and security operations checks. | Execute backup/restore in target infrastructure. |
| QA/release gates | Implemented | QA test run evidence, release candidate gates and runbook acknowledgements. | Keep gates mandatory in CI/CD. |

## Phase 1 Exit Criteria

- `FaceCameraController` requires `RuntimeOperator`.
- `WeatherForecastController` is removed.
- Staff cannot operate face-camera endpoints.
- Public/demo endpoint regression tests pass.
- Controller trust-boundary test passes.
- API tests pass.
- Frontend build passes.
- No-touch zones remain unchanged.

## Later ASVS Work

Phase 2:

- SSO/IdP.
- External identity mapping.
- MFA recovery codes.
- Step-up MFA.
- Privileged access workflow.

Phase 8:

- Evidence access purpose.
- Legal hold.
- Retention.
- Chain of custody.
- Export governance.
- Redaction.

Phase 9:

- Secrets rotation.
- SIEM export.
- Distributed monitoring.
- Backup/restore security checks.
- Vulnerability/dependency gate.

Phase 10:

- Full ASVS verification matrix.
- E2E tests for sensitive workflows.
- Penetration-test readiness checklist.
- Release gate evidence.
