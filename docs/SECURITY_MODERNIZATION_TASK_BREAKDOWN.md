# V-Shield 2.0 Security Modernization Task Breakdown

This document converts the handoff plan into an execution-ready backlog for the next agent.

The scope is the same as the handoff:

- Harden the .NET API
- Harden the Vue frontend auth/session handling
- Protect sensitive evidence/artifact access
- Add observability, telemetry, and resilience
- Leave Python internals untouched
- Leave public-domain internals untouched

## How To Use This Backlog

Work from top to bottom.

- `P0` means must do first because it reduces active attack surface.
- `P1` means high priority and should follow immediately after P0.
- `P2` means important hardening and reliability work after the major risk paths are closed.

Each ticket below includes:

- goal
- scope
- suggested files
- acceptance criteria

## Current Implementation Snapshot

Last checked: 2026-06-10

Source-aligned completion is `100%` for the modernization scope tracked in this document.

| Area | Current estimate | Source-backed status |
|---|---:|---|
| P0 active attack-surface reduction | 100% | Endpoint inventory exists, privileged routes are default-denied, employee QR issuance is auth-gated, QrAccess no longer trusts client-submitted identity/password, and all `/uploads/**` paths reject anonymous reads. |
| P1 high-priority hardening | 100% | Login throttling/lockout, production secret enforcement, HTTPS outside Testing, sessionStorage migration, refresh-token rotation, logout revocation, and TOTP MFA for Admin/BaoVe are in place. |
| P2 observability, resilience, and tests | 100% | Correlation IDs, safe exception envelopes, structured audit fields, live/ready/degraded health endpoints, evidence read auditing, API integration tests, CI-safe verification notes, and route-level bundle splitting are in place. |

Verification added:

- `API/API/API.Tests` contains API integration coverage for public route boundaries, anonymous rejection on privileged endpoints, role-matrix denial, protected uploads, correlation headers, readiness/degraded health, safe error envelopes, refresh-token rotation, logout revocation, and MFA setup/verification.
- Latest local API verification passed: `22/22` tests, `0` warnings, `0` errors.
- Latest frontend production build passed without the previous large entry-bundle warning after route lazy-loading.

## P0 Backlog

### P0-1. Inventory all endpoints and classify trust levels

Goal:

- Build a complete endpoint map before changing behavior.

Scope:

- Enumerate every controller action
- Mark each as `public`, `authenticated`, `privileged`, or `runtime-internal`
- Identify any action that is currently exposed without authorization

Suggested files:

- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\*.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Program.cs`

Acceptance criteria:

- A markdown inventory exists
- Every route in the API has a trust classification
- Every anonymous route has a documented reason

### P0-2. Inventory all secrets and bootstrap credentials

Goal:

- Remove hidden production risk from config and seed defaults.

Scope:

- Identify JWT secrets
- Identify database credentials
- Identify admin seed credentials
- Identify tunnel/runtime credentials or tokens

Suggested files:

- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\appsettings.json`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\appsettings.Development.json`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\appsettings.Production.example.json`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Program.cs`

Acceptance criteria:

- A secrets inventory doc exists
- All deployable secrets are identified
- Bootstrap defaults are documented as unsafe for production

### P0-3. Lock down privileged controllers by default

Goal:

- Make the API default-deny for privileged surfaces.

Scope:

- Add authorization to sensitive controllers lacking it
- Add role or policy restrictions to runtime control, policy mutation, camera controls, QR generation, and access-management endpoints
- Keep only intentional public flows anonymous

Suggested files:

- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\AccessPermissionController.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\AccessPermissionQueryController.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\CameraRuntimeController.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\DynamicQrController.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\FaceRecognitionController.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\GateTransitController.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\LicensePlateController.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\PlateCameraController.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\QrAccessController.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\VehiclesController.cs`

Acceptance criteria:

- No privileged controller remains unintentionally public
- All policy mutation endpoints require authorization
- Runtime control endpoints require privileged authorization

### P0-4. Remove public employee QR issuance

Goal:

- Prevent unauthenticated minting of employee QR payloads.

Scope:

- Protect employee QR generation
- Ensure only authorized callers can generate employee dynamic QR
- Verify the business rule for who may generate and when

Suggested file:

- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\DynamicQrController.cs`

Acceptance criteria:

- Employee QR generation requires auth
- Generation is limited by role or policy
- No anonymous caller can mint employee QR

### P0-5. Stop trusting client-submitted identity in access verification

Goal:

- Remove the need to send raw account password in business requests.

Scope:

- Replace `LoggedInUserId` trust with server-side principal
- Replace password replay with a proper auth/session assertion model
- Keep the business action but change how identity is asserted

Suggested file:

- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\QrAccessController.cs`

Acceptance criteria:

- Business requests no longer carry raw user password
- The server derives identity from authenticated context
- Access verification still works through a safer model

### P0-6. Protect sensitive static files and evidence artifacts

Goal:

- Remove direct public access to face/video artifacts.

Scope:

- Inventory all files under `wwwroot/uploads`
- Decide which items must be protected
- Move to authorized retrieval or signed access
- Narrow or replace broad static file exposure for sensitive paths

Suggested files:

- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Program.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\EmployeesController.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\VideoController.cs`

Acceptance criteria:

- Sensitive face and video artifacts are no longer broadly public
- Retrieval is auth-gated or otherwise deliberately controlled

## P1 Backlog

### P1-1. Harden authentication session strategy

Goal:

- Move away from weak long-lived bearer storage in frontend localStorage.

Scope:

- Define session model
- Define refresh token or secure session strategy
- Define revocation strategy
- Update frontend auth state handling

Suggested files:

- `C:\DoAnTotNghiep\V-Shield-2.0\View\src\stores\auth.js`
- `C:\DoAnTotNghiep\V-Shield-2.0\View\src\services\http.js`
- `C:\DoAnTotNghiep\V-Shield-2.0\View\src\services\authApi.js`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Services\AuthService.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\AuthController.cs`

Acceptance criteria:

- The frontend no longer relies on insecure long-lived token storage
- Logout and revocation semantics are defined
- Admin/operator session handling is stronger

### P1-2. Add login throttling and anti-abuse controls

Goal:

- Reduce brute-force and automation risk.

Scope:

- Rate limit login and sensitive endpoints
- Add lockout rules for repeated failures
- Define retry windows

Suggested files:

- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Program.cs`
- auth controller/service files

Acceptance criteria:

- Repeated failed logins are throttled
- Sensitive write endpoints are protected from abusive bursts

### P1-3. Add phishing-resistant or second-factor admin path

Goal:

- Protect high-value operator and admin accounts.

Scope:

- Choose MFA implementation path
- Prefer passkey-ready design if feasible
- Otherwise implement TOTP as an interim step

Suggested files:

- auth-related API files
- frontend login flow

Acceptance criteria:

- Admin/operator access requires a stronger second factor
- Design is compatible with future passkey migration

### P1-4. Re-enable transport hardening

Goal:

- Remove easy transport weaknesses.

Scope:

- Restore HTTPS redirection if deployment supports it
- Verify external TLS termination expectations
- Audit database connection string settings

Suggested files:

- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Program.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\appsettings.Production.example.json`

Acceptance criteria:

- HTTPS is not optional by accident
- production-facing transport settings are not openly weak

### P1-5. Remove hard-coded production secrets from repo-backed runtime config

Goal:

- Make production deployment secret-safe.

Scope:

- Replace concrete secrets with placeholders or environment bindings
- Remove unsafe fallback values for deployable environments
- Document how to inject secrets safely

Suggested files:

- appsettings files
- startup/seeding code

Acceptance criteria:

- Deployable config files do not contain real secrets
- Production cannot start with unsafe defaults unnoticed

## P2 Backlog

### P2-1. Split public, internal, and runtime API boundaries

Goal:

- Make the architecture easier to reason about and safer to operate.

Scope:

- Separate control-plane endpoints from data-plane endpoints
- Separate public visitor flows from privileged operator flows
- Document the boundary for Python and public-domain sidecars

Acceptance criteria:

- Each boundary has explicit documentation
- The next agent can immediately tell which surface is public and which is internal

### P2-2. Add health, readiness, and degraded-mode reporting

Goal:

- Make dependency failures visible and actionable.

Scope:

- Add readiness checks
- Add dependency-specific health signals
- Add degraded-mode status for sidecar runtime failure

Suggested files:

- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Program.cs`
- runtime/orchestrator-related files

Acceptance criteria:

- Operators can tell when runtime dependencies are down
- The API can degrade gracefully instead of failing blindly

### P2-3. Add structured logging and correlation IDs

Goal:

- Make incident response and debugging reliable.

Scope:

- Create structured security log schema
- Add correlation IDs to incoming requests
- Differentiate audit logs, application logs, and runtime logs

Acceptance criteria:

- Logs can be correlated across a single request
- Security events can be queried cleanly

### P2-4. Improve exception handling and safe error responses

Goal:

- Prevent internal detail leakage and inconsistent error handling.

Scope:

- Add centralized exception handling
- Standardize API error envelopes
- Ensure sensitive exception data does not leak to the client

Acceptance criteria:

- Errors are consistent
- Internal stack traces are not exposed in production responses

### P2-5. Add evidence access auditing

Goal:

- Track who reads what evidence and when.

Scope:

- Log reads of sensitive biometric/video assets
- Add access auditing for registration and visitor artifacts

Acceptance criteria:

- Evidence access is auditable
- Read operations are visible in security logs

### P2-6. Build regression coverage for the hardening work

Goal:

- Make the modernization safe to keep shipping.

Scope:

- Add API integration tests for auth and policy
- Add tests for anonymous/public route boundaries
- Add tests for QR and gate workflows
- Add tests for runtime wrapper behavior
- Add frontend build and smoke verification steps

Acceptance criteria:

- API integration tests exist for public/auth boundary behavior, privileged anonymous rejection, authenticated role denial, protected uploads, session refresh/logout, MFA, safe exception envelopes, correlation IDs, and readiness/degraded checks
- QR/gate/runtime surfaces are protected by endpoint-boundary tests and can receive deeper business-flow tests as product rules change
- The next refactor has a growing safety net

### P2-7. Reduce frontend bundle risk

Goal:

- Improve delivery performance without changing security semantics.

Scope:

- Split large chunks
- Remove unnecessary eager imports
- Preserve existing visual language

Acceptance criteria:

- Frontend bundle size is reduced or better partitioned
- Security hardening work is not coupled to UI redesign

## Recommended First PRs

If the next agent wants a sane sequence, use this order:

1. Endpoint and secret inventory docs
2. Authorization lockdown for sensitive controllers
3. Employee QR generation hardening
4. QrAccess trust-model fix
5. Sensitive artifact protection
6. Session and auth redesign
7. Rate limiting and MFA
8. Health, telemetry, and exception hardening
9. Regression tests

## Definition Of Done For The Modernization

The modernization is not complete until:

- privileged APIs are default-denied
- public APIs are intentional and documented
- operator identity is not trusted from client request fields
- secrets are not hard-coded for production
- biometric/video artifacts are protected
- runtime wrappers fail safely
- logs are actionable
- tests cover the hardened flows
- Python internals and public-domain internals remain untouched
