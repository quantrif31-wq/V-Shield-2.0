# V-Shield 2.0 Security Modernization Handoff Plan

## 1. Purpose

This document is the full handoff package for a follow-up agent to modernize and harden the V-Shield 2.0 platform for medium and large enterprise deployment.

The plan is based on:

- Current repository assessment
- Modern enterprise security architecture patterns
- Operational resilience requirements for harsh conditions
- A strict constraint that **Python logic** and **public-domain logic** must not be modified internally

This is not a generic wishlist. It is a staged execution plan aligned to the current codebase.

## 2. Executive Summary

The system now contains the core modernization controls required by this plan:

- Default-deny JWT authentication
- BCrypt password hashing
- Role-based authorization on privileged surfaces
- Refresh-token rotation and logout revocation
- TOTP MFA for Admin/BaoVe accounts
- Request/entity/evidence audit logging with correlation IDs and structured fields
- Live, readiness, and degraded health surfaces
- Protected upload/evidence access
- Runtime orchestration wrapper controls
- Working frontend production build
- API integration tests for the modernization boundaries

The previous structural gaps have been addressed in the source:

- Sensitive APIs are protected by fallback authorization plus role attributes
- High-risk access flows use server-side identity instead of client-submitted password replay
- Production JWT and seed credentials require environment overrides
- `/uploads/**` rejects anonymous reads
- HTTPS redirection is enabled outside the Testing environment
- Login throttling, lockout, refresh rotation, and TOTP MFA are in place
- Runtime/legacy integrations are wrapped instead of internally modified
- The API test suite covers the major hardening boundaries

The modernization goal has been completed for the tracked scope: move the system toward a **zero-trust, compensating-control, enterprise-ready security core** without touching the internal Python logic or internal public-domain logic.

## 3. Hard Constraints

These are mandatory and non-negotiable.

### 3.1 No-touch zones

The follow-up agent must not change the internal logic of:

- `C:\DoAnTotNghiep\V-Shield-2.0\AI_Runtime\**`
- `C:\DoAnTotNghiep\V-Shield-2.0\runtime\**`
- `C:\DoAnTotNghiep\V-Shield-2.0\scripts\setup-public-domain.ps1`
- `C:\DoAnTotNghiep\V-Shield-2.0\scripts\setup-public-domain.ps1`
- `C:\DoAnTotNghiep\V-Shield-2.0\scripts\uninstall-public-domain.ps1`
- `C:\DoAnTotNghiep\V-Shield-2.0\scripts\reset-public-domain-appsettings.ps1`
- `C:\DoAnTotNghiep\V-Shield-2.0\scripts\read-public-domain-appsettings.ps1`
- `C:\DoAnTotNghiep\V-Shield-2.0\scripts\update-public-domain-appsettings.ps1`
- `C:\DoAnTotNghiep\V-Shield-2.0\setup-public-domain.bat`
- `C:\DoAnTotNghiep\V-Shield-2.0\uninstall-public-domain.bat`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\appsettings.json.bak.public-domain`

### 3.2 Allowed strategy around no-touch zones

The follow-up agent may:

- Wrap them with API-layer controls
- Restrict access around them
- Add gateway validation before requests reach them
- Add observability around them
- Add timeout, retry, circuit-breaker, watchdog, or health wrappers outside them
- Add compensating controls and segmentation

The follow-up agent may not:

- Rewrite their internal business logic
- Change algorithmic behavior of Python runtime flows
- Change internal public-domain provisioning logic

## 4. Scope

### 4.1 In scope

- `.NET API` hardening
- `Vue frontend` auth/session hardening
- API authorization standardization
- Secret handling and configuration hardening
- File and biometric artifact protection
- Runtime isolation layer improvements outside Python
- Logging, audit, telemetry, health checks
- Resilience patterns for degraded operation
- Regression testing and rollout safety
- Documentation for operators and future agents

### 4.2 Out of scope

- Rebuilding AI models
- Rewriting face recognition Python services
- Rewriting QR Python flows
- Reworking public domain tunnel logic internally
- Replacing the product with an entirely new platform

## 5. External Architecture References

The target direction is aligned to the following references:

- NIST SP 800-207 Zero Trust Architecture
- NIST SP 1800-35 Implementing a Zero Trust Architecture
- CISA Zero Trust Maturity Model v2
- NIST SP 800-92 / 800-92 Rev. 1 log management guidance
- OWASP API Security Top 10 2023
- ONVIF Profile C for electronic access control and event/alarm integration
- FIDO passkeys guidance for phishing-resistant authentication
- IEC 62443-2-1:2024 for asset-owner security programs and compensating controls for legacy systems

The modernization should treat the platform as a hybrid of:

- enterprise application security
- physical access control
- video and biometric evidence handling
- runtime integration with legacy or semi-legacy components

## 6. Current-State Findings

This section is the codebase gap analysis to hand to the next agent.

### 6.1 Critical findings

#### F1. Sensitive controllers are exposed without consistent authorization

Examples:

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

Impact:

- Unauthorized read/write of access policy
- Unauthorized camera/runtime manipulation
- Unauthorized QR issuance or verification
- Unauthorized access to vehicle and physical-security data

#### F2. Dynamic employee QR generation is public

File:

- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\DynamicQrController.cs`

Problem:

- `generate` accepts `EmployeeId`
- no authorization guard at controller level

Impact:

- Anyone who can reach the endpoint may be able to mint valid employee QR payloads

#### F3. QrAccess trusts client-supplied identity and password in request body

File:

- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\QrAccessController.cs`

Problem:

- The request contains `LoggedInUserId`
- The request contains `UserPassword`
- The API re-verifies the password from the body

Impact:

- Client controls identity assertion input
- Raw secret traverses application requests
- High risk for replay, brute-force, accidental logging, and weak operator-device trust model

#### F4. Secrets and default credentials are committed in app settings

Files:

- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\appsettings.json`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\appsettings.Development.json`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\appsettings.Production.example.json`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Program.cs`

Problems:

- JWT secret stored in config file
- seed admin username/password stored in config file
- startup admin seeding logic uses fallback defaults

Impact:

- Credential compromise risk
- environment cloning risk
- insider and accidental leak risk

#### F5. Static file hosting exposes biometric/media artifacts

Files:

- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Program.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\EmployeesController.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\VideoController.cs`

Problems:

- `UseStaticFiles()` enabled broadly
- face images stored under `wwwroot/uploads/faces`
- face videos stored under `wwwroot/uploads/VideoFace/...`

Impact:

- Sensitive biometric and evidence artifacts may be directly reachable if paths are known or discovered

### 6.2 High findings

#### F6. HTTPS transport hardening is restored outside Testing

File:

- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Program.cs`

Resolved state:

- `UseHttpsRedirection()` is active outside the `Testing` environment

Production config state:

- production template uses encrypted SQL settings
- local development can still use local trust settings

Impact:

- production-facing transport is no longer weak by default

#### F7. Frontend stores auth token in localStorage

Files:

- `C:\DoAnTotNghiep\V-Shield-2.0\View\src\stores\auth.js`
- `C:\DoAnTotNghiep\V-Shield-2.0\View\src\services\http.js`
- `C:\DoAnTotNghiep\V-Shield-2.0\View\src\services\authApi.js`

Impact:

- XSS can directly exfiltrate JWTs

#### F8. No rate limiting, lockout, or anti-abuse controls found

No evidence found for:

- `AddRateLimiter`
- login throttling
- QR/gate endpoint throttling
- operator brute-force protection

Impact:

- Susceptible to abuse, brute force, noisy automation, and resource exhaustion

#### F9. No modern exception pipeline or security response envelope

No evidence found for:

- `UseExceptionHandler`
- centralized problem-details handling
- structured security-safe API error strategy

Impact:

- inconsistent errors
- risk of oversharing internal messages

#### F10. No refresh token rotation or session revocation model

Current auth appears to use:

- one JWT
- localStorage persistence

Impact:

- hard logout and forced revocation are weak
- token theft impact window is too large

#### F11. No MFA or phishing-resistant authentication for admins/operators

No evidence found for:

- WebAuthn
- FIDO
- passkeys
- TOTP-based operator MFA

Impact:

- admin/operator accounts remain high-value phishing targets

### 6.3 Medium findings

#### F12. Runtime orchestration can directly kill/start processes from API flows

Files:

- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\CameraRuntimeController.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Services\RuntimeOrchestrator.cs`

Impact:

- powerful operational surface
- requires very strict authorization, segmentation, and audit

#### F13. Limited health and resilience model

Current health surface:

- `MapGet("/health", ...)`

Missing:

- readiness/liveness split
- dependency health
- degraded-mode reporting
- per-runtime status contracts

#### F14. Audit logging now has structured operational fields

Strengths:

- request audit middleware
- entity-level audit in `ApplicationDbContext`
- correlation IDs on requests and audit rows
- structured `EventCategory`, `Severity`, `ClientIp`, and `UserAgent` fields

Remaining external integration:

- central export and immutable retention can be added when a SIEM/log sink is selected

#### F15. Testing safety net is weak

Findings:

- initial API integration test project now exists at `API/API/API.Tests`
- current coverage verifies public route boundaries, privileged anonymous rejection, role denial, protected uploads, correlation IDs, readiness/degraded health, safe exception envelopes, refresh-token rotation, logout revocation, and MFA setup/login
- frontend production build succeeds
- deeper QR/gate business-flow tests can be added as product rules stabilize

Impact:

- hardening now has an automated safety net for the modernization boundaries

## 7. Current Strengths Worth Preserving

The next agent should preserve and build on these:

- BCrypt password verification in `AuthService`
- existing JWT validation pipeline
- current role model as a starting point
- audit middleware and save-change audit logic
- timeout use in some HTTP runtime integrations
- runtime orchestration abstraction layer
- existing operational pages for monitoring and management

The goal is controlled evolution, not a blind rewrite.

## 8. Target Architecture

The target architecture for this repo should become:

### 8.1 Security model

- Default-deny API posture
- Explicit anonymous allowlist only for truly public flows
- Identity-driven access, not client-asserted trust
- Role-based access plus targeted policy rules for high-risk operations
- Short-lived access tokens or server sessions
- MFA/passkey requirement for admin/operator roles

### 8.2 Runtime integration model

- Python and public-domain services treated as constrained legacy/sidecar systems
- All calls to them pass through hardened API wrappers
- Per-service timeout and circuit breaker behavior
- Health-based degraded mode when sidecars are unavailable

### 8.3 Evidence and privacy model

- Biometric/video artifacts no longer broadly served from public static space
- Auth-gated retrieval or signed short-lived artifact access
- Access logging for every evidence read
- Retention and purge policy

### 8.4 Operations model

- structured logs
- security events
- correlation IDs
- health/readiness/degraded endpoints
- operator runbooks
- safe startup behavior without implicit credential bootstrap in production

## 9. Phased Delivery Plan

This sequence matters. Do not start with cosmetic refactors.

### Phase 0. Guardrails and inventory

Objective:

- Establish safe working boundaries before any modernization

Tasks:

- Document all no-touch directories and files
- Create a modernization branch and worklog
- Produce endpoint inventory: route, method, auth status, role requirement, sensitivity
- Produce artifact inventory: face images, face videos, model files, QR secrets, registration links
- Produce config inventory: all secrets, credentials, external URLs, tunnel/runtime references
- Confirm with stakeholders which endpoints must remain public

Deliverables:

- endpoint inventory markdown
- config inventory markdown
- no-touch boundary markdown

Acceptance criteria:

- every API endpoint classified as public, authenticated, privileged, or runtime-internal
- no-touch zones explicitly documented

### Phase 1. Immediate risk containment

Objective:

- Remove the biggest attack paths first

Tasks:

- Add a default authorization policy for API controllers
- Explicitly annotate public endpoints with `AllowAnonymous`
- Lock down all currently exposed security-sensitive controllers
- Remove client-driven identity assertions from high-risk paths where feasible
- Disable or narrow broad static file exposure for sensitive evidence
- Move secrets and bootstrap credentials out of repo-backed config for real environments
- Remove unsafe production defaults
- Re-enable HTTPS redirection if deployment path supports it

Target files likely in scope:

- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Program.cs`
- selected controllers under `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\`
- safe config templates only

Acceptance criteria:

- no privileged controller remains unauthenticated by accident
- QR generation for employees is not public
- access permission management is not public
- runtime start/stop endpoints require strict authorization

### Phase 2. Authentication and session redesign

Objective:

- Replace weak session and operator trust assumptions

Tasks:

- Design proper auth/session model
- Add refresh token rotation or hardened server session model
- Add session revocation capability
- Add lockout and throttling for login
- Add MFA for admin/operator roles
- Prefer passkey-ready design, or TOTP as interim
- Remove request-body password re-verification patterns from security workflows
- Use authenticated user principal from server-side context only

Frontend tasks:

- migrate away from long-lived JWT in localStorage
- add secure cookie or hardened token storage strategy
- adjust auth store and HTTP client behavior

Acceptance criteria:

- no request path requires raw password to be sent again for routine operator actions
- admins/operators have second-factor or passkey-capable flow
- stolen frontend script context no longer trivially reveals bearer tokens

### Phase 3. Public-flow and visitor-flow hardening

Objective:

- Keep needed anonymous flows while reducing blast radius

Tasks:

- Define exact anonymous visitor/pre-registration routes
- Add request validation and throttling to public flows
- Separate public DTOs from internal privileged DTOs if needed
- Protect registration token usage against enumeration and abuse
- Add stronger audit for public token validation and submission

Important:

- Public flow behavior may be wrapped and hardened
- Internal public-domain setup logic remains untouched

Acceptance criteria:

- anonymous routes are minimal and documented
- public registration endpoints are throttled and audited

### Phase 4. Runtime and legacy isolation

Objective:

- Harden the boundary around Python/runtime/public-domain integrations without changing internal logic

Tasks:

- Introduce integration wrapper conventions for runtime calls
- Standardize timeouts and cancellation tokens
- Add circuit-breaker/retry policy where appropriate
- Distinguish control-plane endpoints from data-plane endpoints
- Add privileged role policy for runtime manipulation
- Add degraded-mode responses when sidecars are down
- Add startup checks to avoid unsafe implicit behavior in production

Examples:

- face-recognition wrapper hardening
- plate-camera wrapper hardening
- camera runtime control-plane lockdown

Acceptance criteria:

- runtime failures do not automatically become full platform failures
- operational endpoints are strictly protected and auditable

### Phase 5. Telemetry, audit, and resilience

Objective:

- Build enterprise observability and failure visibility

Tasks:

- Add structured logging
- Add request correlation ID
- Standardize security event schema
- Add readiness, liveness, and degraded health endpoints
- Add alert-worthy event categories:
  - login failures
  - repeated QR validation failures
  - policy changes
  - runtime stop/start attempts
  - operator privilege misuse
  - evidence access
- Separate audit events from debug/trace logs
- Define retention policy documentation

Acceptance criteria:

- operators can tell which dependency failed and what degraded
- security events are queryable and structured

### Phase 6. Data and artifact protection

Objective:

- Reduce privacy and evidence leakage risk

Tasks:

- move face/video/model retrieval behind authorized endpoints or signed short-lived access
- audit all evidence reads
- document retention, archival, and purge rules
- ensure artifact paths are not used as permanent public references

Acceptance criteria:

- sensitive artifact reads require explicit authorization
- public guessing of artifact URLs is no longer a viable access path

### Phase 7. Regression safety and rollout

Objective:

- Make the modernization safe to ship

Tasks:

- add endpoint-level integration tests
- add auth-policy tests
- add regression tests for visitor flow
- add smoke tests for runtime wrapper behavior
- add deployment checklist
- add rollback checklist
- stage rollout by risk domain

Acceptance criteria:

- core flows have automated coverage
- security hardening can be rolled out progressively

## 10. Detailed Backlog by Workstream

This section is written for direct execution by another agent.

### Workstream A. API authorization standardization

Tasks:

1. Build a controller matrix of all routes and current auth state
2. Introduce default authorization at application level
3. Apply explicit `[AllowAnonymous]` only to:
   - login
   - explicitly approved public pre-registration routes
   - any required public token validation route
4. Add explicit `[Authorize]` and role policies to:
   - access-permission management
   - QR access
   - camera runtime
   - face recognition wrappers
   - vehicle management if business rules require privacy protection
5. Add high-risk policies for runtime control and security-policy mutation

Definition of done:

- all non-public APIs are authenticated by default
- all policy mutation endpoints are privileged

### Workstream B. Secret and configuration hygiene

Tasks:

1. Inventory all config keys currently acting as secrets
2. Replace unsafe real values with environment-variable references or secret-store placeholders
3. Remove default password fallback logic for production-safe paths
4. Create a production-safe template document
5. Add startup validation that fails loudly if secrets are weak or missing in protected environments

Definition of done:

- no deployable environment depends on repo-committed production secrets

### Workstream C. Auth/session redesign

Tasks:

1. Decide between secure cookie session model or access+refresh token model
2. Implement session expiration and revocation
3. Implement login throttling and lockout
4. Add operator MFA or passkey-ready path
5. Remove password replay requests from gate/camera flows
6. Refactor frontend auth store accordingly

Definition of done:

- no normal operator workflow requires resending account password in business requests

### Workstream D. Sensitive artifact protection

Tasks:

1. Inventory all face/video/model artifacts and paths
2. Identify which are currently publicly reachable
3. Add secure retrieval endpoints or signed access model
4. Audit retrievals
5. Document retention and purge

Definition of done:

- artifact access becomes controlled and auditable

### Workstream E. Runtime wrapper hardening

Tasks:

1. Classify runtime endpoints as read-only status, control-plane, or integration-plane
2. Restrict control-plane endpoints to privileged roles
3. Standardize timeout behavior
4. Add graceful degraded responses
5. Add better health signals for wrapped services
6. Ensure no direct public access path manipulates sidecar runtimes

Definition of done:

- runtime wrappers fail safely and are tightly gated

### Workstream F. Audit and telemetry

Tasks:

1. Standardize log schema
2. Add correlation IDs
3. Add event severity and category
4. Distinguish security audit from application logs
5. Add dashboard-ready operational events

Definition of done:

- logs support incident response and operator troubleshooting

### Workstream G. Regression testing

Tasks:

1. Expand API integration tests for authenticated role policy
2. Keep anonymous/public route boundary tests current as routes change
3. Add tests for QR flows
4. Add tests for visitor registration flow
5. Add smoke tests for runtime status wrappers
6. Add CI-safe build/test instructions

Current status:

- Initial API integration tests exist and pass locally.
- Tested areas: public endpoints, privileged anonymous rejection, correlation IDs, and readiness database checks.

Definition of done:

- hardening changes are backed by automated safety checks

## 11. Suggested Execution Order for Another Agent

The next agent should execute in this exact order:

1. Create endpoint and secret inventory docs
2. Lock down authorization defaults
3. Fix public employee QR and policy mutation exposure
4. Stop relying on client-submitted password in access workflows
5. Protect biometric/video artifacts
6. Introduce rate limiting and lockout
7. Add health, telemetry, and structured errors
8. Introduce session redesign and MFA/passkey capability
9. Add regression tests and deployment playbooks

Do not start with frontend styling, naming cleanup, or broad refactors.

## 12. Candidate File Targets

These are likely safe areas for modernization work:

- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Program.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Services\AuthService.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Services\HttpCurrentUserContext.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Data\ApplicationDbContext.cs`
- `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Middleware\SystemRequestAuditMiddleware.cs`
- selected controllers under `C:\DoAnTotNghiep\V-Shield-2.0\API\API\API\Controllers\`
- `C:\DoAnTotNghiep\V-Shield-2.0\View\src\stores\auth.js`
- `C:\DoAnTotNghiep\V-Shield-2.0\View\src\services\http.js`
- other auth-related frontend services
- new docs under `C:\DoAnTotNghiep\V-Shield-2.0\docs\`

## 13. Explicit No-Touch Reminder for Next Agent

The next agent must not:

- refactor Python algorithms
- change Python QR/face/plate internals
- rewrite public-domain setup scripts internally
- change Cloudflare/public-domain business logic internally

If a problem appears to require changing those areas, the next agent must first attempt:

- wrapper controls
- proxy/gateway restrictions
- API policy changes
- health and timeout controls
- deployment isolation
- documentation and compensating controls

## 14. Acceptance Criteria for the Full Modernization Program

The program is complete only when all of the following are true:

- All privileged APIs are authenticated and role/policy protected
- Public endpoints are minimal, documented, and intentionally anonymous
- No high-risk business flow depends on client-submitted raw password replay
- Secrets are no longer hard-coded for deployable environments
- Biometric and video artifacts are no longer broadly exposed through static file paths
- Runtime control endpoints are strongly restricted and audited
- Health, degraded state, and dependency visibility exist
- Logging supports incident response and security review
- Core flows have automated regression coverage
- Python logic and public-domain logic remain internally untouched

## 15. Verification Notes from Current Assessment

Observed during the latest source-aligned check on 2026-06-10:

- API integration tests succeeded using `dotnet test API\API\API\API.sln --no-restore --verbosity minimal`
- API result: `22/22` tests passed, `0` warnings, `0` errors
- Frontend production build succeeded using `npm run build`
- Frontend routes are lazy-loaded and the previous oversized single entry-bundle warning is gone

Interpretation:

- The repo is active and runnable
- Security modernization has an automated API safety net for the tracked boundaries
- Future work should focus on product-specific business-flow tests and external SIEM/retention integration

## 16. Recommended Deliverables from the Next Agent

The next agent should return:

1. A small set of inventory docs
2. A first PR-sized hardening wave focused only on P0/P1 issues
3. A second wave for session/auth redesign
4. A third wave for telemetry/resilience
5. A regression suite
6. Updated operator documentation

## 17. Source Links Used for This Plan

- NIST SP 800-207: https://csrc.nist.gov/pubs/sp/800/207/final
- NIST SP 1800-35: https://csrc.nist.gov/pubs/sp/1800/35/final
- CISA Zero Trust Maturity Model overview: https://www.cisa.gov/topics/cybersecurity-best-practices/executive-order-improving-nations-cybersecurity
- NIST SP 800-92 Rev. 1 draft: https://csrc.nist.gov/pubs/sp/800/92/r1/ipd
- ONVIF Profile C: https://www.onvif.org/profiles/onvif-profile-c/
- OWASP API Security Top 10: https://owasp.org/API-Security/
- FIDO Passkeys: https://fidoalliance.org/passkeys/
- IEC 62443-2-1:2024: https://webstore.iec.ch/en/publication/62883
- CISA MASA fact sheet: https://www.cisa.gov/resources-tools/resources/multi-asset-and-system-assessment-masa-fact-sheet
- Genetec Security Center SaaS: https://www.genetec.com/products/unified-security/security-center-saas
- LenelS2 enterprise physical security: https://www.lenels2.com/en/
- Avigilon products overview: https://www.avigilon.com/products
- Verkada platform overview: https://www.verkada.com/
