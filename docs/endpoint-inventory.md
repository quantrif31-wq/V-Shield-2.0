# V-Shield 2.0 - Endpoint Inventory

Last updated: 2026-06-10

Classification: `public` | `authenticated` | `privileged` | `runtime-internal`

## Current API Authorization Posture

The API now uses a default-deny posture through the ASP.NET Core authorization fallback policy in:

- `API/API/API/Program.cs`

Any controller action without `[AllowAnonymous]` now requires an authenticated user by default. Role-specific attributes are still preferred for sensitive business surfaces.

## Explicit Public Endpoints

These endpoints are intentionally anonymous.

| Controller / Surface | Route | Reason |
|---|---|---|
| AuthController | `POST /api/Auth/login` | User login |
| AuthController | `POST /api/Auth/refresh` | Refresh-token session renewal and rotation |
| PreRegistrationController | `GET /api/pre-registrations/validate/{token}` | Public visitor registration token validation |
| PreRegistrationController | `POST /api/pre-registrations/submit/{token}` | Public visitor registration submission |
| PreRegistrationController | `GET /api/pre-registrations/visitor-pass/{token}` | Public visitor pass lookup |
| Minimal API | `GET /health` | External health check |
| Minimal API | `GET /health/live` | Liveness check |
| Minimal API | `GET /health/ready` | Readiness check with database connectivity |
| Minimal API | `GET /health/degraded` | Degraded-mode status including runtime service state |

## Privileged Controllers

These controllers have explicit role requirements.

| Controller | Route Prefix | Classification | Required Role(s) | Notes |
|---|---|---|---|---|
| AccessPermissionController | `api/access-permissions` | privileged | `Admin` | Access permission mutation |
| AccessPermissionQueryController | `api/access-permissions` | privileged | `Admin` | Access permission read/delete |
| AccessLogsController | `api/access-logs` | privileged | `Admin,BaoVe`; delete `Admin` | Access event data |
| BiometricsController | `api/biometrics` | privileged | `Admin` | Biometric management |
| CameraRuntimeController | `api/camera-runtime` | runtime-internal | `Admin` | Camera/runtime control plane |
| DashboardController | `api/dashboard` | privileged | `Admin,BaoVe` | Operational visibility |
| DeviceManagementController | `api/device-management` | privileged | `Admin` | Device management |
| DepartmentsController | `api/departments` | privileged | `Admin` | Catalog mutation |
| DynamicQrController | `api/dynamic-qr` | privileged | `Admin,Staff,BaoVe` | Employee QR generation/verification |
| EnterpriseAccessPolicyController | `api/enterprise/access-policy` | privileged | `Admin` | Enterprise access levels, schedules, rules, emergency state, anti-passback, occupancy, explainable decisioning |
| EnterpriseDeviceController | `api/enterprise/devices` | privileged/runtime-internal | `Admin,BaoVe`; mutations mostly `Admin` | Device registry, health, OSDP/ONVIF-compatible boundaries, offline policy packages |
| EnterpriseEvidenceController | `api/enterprise/evidence` | privileged | `Admin,BaoVe`; governance actions `Admin` | Evidence repository, retention, legal hold, export approval, chain of custody, redaction, compliance reports |
| EnterpriseFoundationController | `api/enterprise/foundation` | privileged | `Admin` | Company/site/building/floor/zone/access point/person lifecycle/recertification foundation |
| EnterpriseOperationsController | `api/enterprise/operations` | privileged/runtime-internal | `Admin,BaoVe`; configuration `Admin` | Outbox, webhook/SIEM, dependency health, backup/restore, observability checks |
| EnterpriseReleaseReadinessController | `api/enterprise/release-readiness` | privileged | `Admin,BaoVe`; release mutations `Admin` | QA evidence, release gates, release candidates, runbook acknowledgements |
| EnterpriseSituationalAwarenessController | `api/enterprise/situational-awareness` | privileged | `Admin,BaoVe`; map mutation `Admin` | Security events, correlation, video bookmarks, maps, AI adjudication and metrics |
| EnterpriseSocController | `api/enterprise/soc` | privileged | `Admin,BaoVe`; rule/template mutation `Admin` | Alarm queue, SOP execution, incidents, dispatch, handover, muster snapshots |
| EnterpriseVisitorVehicleController | `api/enterprise/visitor-vehicle` | privileged | `Admin,BaoVe`; governance/catalog actions `Admin` | Visitor lifecycle, forms, watchlist, parking, barrier, lane events |
| FaceCameraController | `api/FaceCamera` | runtime-internal | `Admin,BaoVe` via `RuntimeOperator` policy | Face camera runtime control proxy |
| FaceRecognitionController | `api/face-recognition` | runtime-internal | `Admin,BaoVe` | Face runtime wrapper |
| GateTransitController | `api/gate-transit` | privileged | `Admin,BaoVe` | Gate/vehicle transit decisions |
| GuestProfilesController | `api/guest-profiles` | privileged | `Admin,BaoVe` | Guest profile data |
| LicensePlateController | `api/license-plates` | runtime-internal | `Admin,BaoVe` | Plate camera integration |
| PlateCameraController | `api/platecamera` | runtime-internal | `Admin,BaoVe` | Plate camera control proxy |
| PositionsController | `api/positions` | privileged | `Admin` | Catalog mutation |
| QrAccessController | `api/QrAccess` | privileged | `Admin,BaoVe` | QR scan access workflow |
| RegistrationLinkController | `api/registration-links` | privileged | `Admin` | Registration token issuance |
| RuntimeServicesController | `api/runtime-services` | runtime-internal | `Admin,BaoVe` | Runtime service control |
| UsersController | `api/users` | privileged | `Admin` | User administration |
| VehiclesController | `api/vehicles` | privileged | `Admin` | Vehicle CRUD |
| VideoController | `api/video` | privileged | `Admin,BaoVe`; delete `Admin` | Face video evidence; content reads audited |

## Authenticated Controllers

These controllers are protected by either explicit `[Authorize]` or the default fallback policy.

| Controller | Route Prefix | Classification | Notes |
|---|---|---|---|
| AttendancesController | `api/attendances` | authenticated | Attendance records |
| AuthController | `api/Auth/logout`, `api/Auth/me` | authenticated | Session actions; logout revokes refresh tokens and increments access-token version |
| ExceptionReasonsController | `api/exception-reasons` | authenticated | Mutations require `Admin` |
| LeaveRequestsController | `api/leave-requests` | authenticated | Leave workflow |
| PreRegistrationController | `api/pre-registrations` | authenticated | Non-public management actions |
| ReportsController | `api/reports` | authenticated | Reporting |
| ShiftsController | `api/shifts` | authenticated | Shift management |
| StatisticsController | `api/statistics` | authenticated | Statistics |
| WorkSchedulesController | `api/work-schedules` | authenticated | Work schedule management |
| SignalR EmployeeStatsHub | `/hubs/employee-stats` | authenticated | Uses `.RequireAuthorization()` |

## Error Handling, Correlation, And Health

Current operational hardening:

- Every request receives an `X-Correlation-ID` response header.
- A caller-provided `X-Correlation-ID` is accepted only when it is short and contains safe characters.
- Unexpected API exceptions are converted to a generic `application/problem+json` response with `correlationId`.
- Exception details are logged server-side but not returned to clients.
- Request audit metadata includes `correlationId`.
- Audit rows include structured `EventCategory`, `Severity`, `CorrelationId`, `ClientIp`, and `UserAgent` fields.
- `GET /health/live` reports process liveness.
- `GET /health/ready` checks database connectivity and returns `503` when the API is not ready.
- `GET /health/degraded` reports database status plus enabled/runtime service state.
- API responses include baseline security headers: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, and `Permissions-Policy`.
- Production startup rejects wildcard/invalid CORS origins and requires explicit JWT issuer/audience.

External integration work:

- Add external alert wiring if an operator dashboard or SIEM is introduced.

## Authentication Sessions And MFA

Current auth hardening:

- Access tokens include a `token_version` claim and are rejected when the account version changes.
- `POST /api/Auth/refresh` rotates refresh tokens and stores only token hashes.
- `POST /api/Auth/logout` revokes refresh tokens and invalidates existing access tokens for that user.
- Admin and BaoVe accounts require TOTP MFA by default.
- First valid password login for an MFA-required account returns setup data instead of an access token.
- `POST /api/Users/{id}/mfa/reset` lets Admin reset MFA enrollment and revoke the user's active sessions.

## Regression Coverage

Current API test coverage:

- `API/API/API.Tests` uses an isolated `Testing` environment with an in-memory database.
- Public endpoint tests verify intentional anonymous access for health and visitor token flows.
- Privileged endpoint tests verify anonymous callers are rejected for representative protected surfaces.
- Role-matrix tests verify authenticated Staff users are still denied Admin-only routes.
- Session tests verify refresh-token rotation and logout revocation.
- MFA tests verify Admin TOTP setup and successful TOTP login.
- Protected upload tests verify anonymous `/uploads/**` access is blocked.
- Correlation ID tests verify generated and caller-provided `X-Correlation-ID` response headers.
- Health tests verify readiness and degraded-mode dependency responses.
- Safe exception tests verify generic problem details and no exception-detail leak.
- Route-boundary tests fail when controller actions lack explicit `[Authorize]` or `[AllowAnonymous]`.
- Face camera role tests verify Staff cannot operate camera runtime controls.
- Demo endpoint tests verify `WeatherForecastController` is no longer exposed.
- Enterprise workflow tests verify Staff denial and Admin execution for foundation, access policy, visitor/vehicle, device, situational awareness, SOC, evidence, operations resilience, and release readiness workflows.

Latest local result:

- `dotnet test API\API\API\API.sln --no-restore --verbosity minimal`
- `44/44` tests passed, with `0` warnings and `0` errors.

Future product test work:

- Add deeper QR, gate, and visitor business-flow tests when product rules stabilize.

## Sensitive Static Artifacts

The static file middleware is still enabled, but the API now blocks anonymous reads for:

- `/uploads/**`

Preferred retrieval paths:

- `GET /api/Employees/{id}/face-image` - authorized employee face image retrieval; writes `READ` audit event.
- `GET /api/Video/{id}/content` - authorized face video retrieval; writes `READ` audit event.

The Vue frontend now fetches employee face images and face videos as authenticated blobs instead of embedding static `/uploads/...` paths in the main protected screens.

Public visitor registration no longer returns `HostFaceImageUrl` from `GET /api/pre-registrations/validate/{token}`.

Future cleanup:

- Consider removing direct static serving for upload folders once all legacy callers are confirmed migrated.

## Config Secrets Inventory

| Key | File / Source | Sensitivity | Current State | Deployment Action |
|---|---|---|---|---|
| `JwtSettings:Secret` | `appsettings.json` | CRITICAL | Local-development placeholder only | Do not use for production |
| `VSHIELD_JWT_SECRET` | Environment | CRITICAL | Supported for validation and token issuance | Required for production and Docker compose |
| `SeedAdmin:*` | `appsettings*.json` | CRITICAL | Development defaults remain; production template uses placeholders | Production bootstrap requires env overrides |
| `VSHIELD_SEED_ADMIN_*` | Environment | CRITICAL | Supported for production bootstrap | Required when production DB has no users |
| `ConnectionStrings:DefaultConnection` | appsettings / env / compose | CRITICAL | Production template and compose use encrypted SQL settings | Move real values to environment/secret store |
| `AiServices:*` | appsettings | LOW-MEDIUM | Local service URLs | Keep environment-specific |
| `Cloudflared:*` | appsettings | MEDIUM | Tunnel metadata | Keep tokens/secrets out of repo |

## Future Authorization Product Decisions

- Confirm whether `DynamicQrController` verification should be available to all authenticated guards or narrowed further.
- Confirm whether `VehiclesController` should allow `BaoVe` read-only access instead of `Admin` only.
- Confirm whether future runtime-control endpoints should use `RuntimeOperator` policy by default.
