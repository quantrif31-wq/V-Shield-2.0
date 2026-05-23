# RENAME_EXECUTION_PLAN_v2 (Post-U Reality-Based)

Date: 2026-05-23
Project: V-Shield-2.0
Owner: Refactor/Naming cleanup track

## 1) Current reality snapshot (from active code)

This plan is based on current source state, not only previous reports.

- Frontend still contains legacy endpoint fallbacks:
  - `View/src/services/cameraRuntimeApi.js` (`/SetCam`)
  - `View/src/services/dynamicQrApi.js` (`/QR_Dong`)
  - `View/src/services/dynamicQrVerifyApi.js` (`/QR_Dong`)
  - `View/src/services/gateTransitApi.js` (`/Gate`)
  - `View/src/services/plateRecognitionApi.js` (`/BienSo`)
- Backend still exposes legacy alias routes in some controllers:
  - `API/API/API/Controllers/CameraRuntimeController.cs` (`[Route("api/SetCam")]`)
  - `API/API/API/Controllers/DynamicQrController.cs` (`[Route("api/QR_Dong")]`)
  - `API/API/API/Controllers/FaceRecognitionController.cs` (`[Route("api/FaceID")]`)
- Several docs/changelogs no longer perfectly match current code and need reconciliation.

## 2) Goal

Execute a no-surprise, step-by-step cleanup to:

1. Remove remaining runtime legacy compatibility surfaces safely.
2. Align documentation with actual code state.
3. Keep deploy risk low through explicit validation gates and rollback points.

## 3) Non-goals / protected constraints

- Do not rename project root `V-Shield-2.0`.
- Do not modify protected AI runtime data folders unless explicitly requested.
- Do not change business behavior while doing naming-only hard-cut tasks.

## 4) Phase-by-phase execution (sequential)

### Phase 0 - Baseline Freeze and Safety Net

Objective: freeze reproducible baseline before additional hard-cut.

Checklist:
- Capture `git rev-parse HEAD`.
- Run baseline build/test commands:
  - Frontend: `npm run build` (in `View`)
  - Backend: `dotnet build API.sln` (in `API/API/API`)
- Export active endpoint inventory from frontend services and backend route attributes.
- Create rollback tag/branch for this phase.

Exit criteria:
- Both builds pass.
- Baseline artifacts committed to a dated checkpoint note.

### Phase 1 - Documentation Reconciliation

Objective: make reports trustworthy before further removals.

Work:
- Add a "state reconciliation" note documenting gaps between:
  - Hard-cut reports
  - Actual remaining legacy aliases/fallbacks
- Update `HARD_CUT_READINESS_CHECKLIST.md` status wording from "completed" to "completed for legacy shims/routes, pending for residual endpoint alias fallback removal" if applicable.
- Append dated correction notes to `BATCH_T_CHANGELOG.md` and `BATCH_U_CHANGELOG.md`.

Exit criteria:
- All status docs represent current code truth.
- No functional code change in this phase.

### Phase 2 - Frontend Usage Elimination (switch to canonical-only calls)

Objective: stop using legacy fallback paths in frontend runtime.

Work:
- Refactor these services to canonical-only requests (remove fallback wrappers):
  - `cameraRuntimeApi.js`
  - `dynamicQrApi.js`
  - `dynamicQrVerifyApi.js`
  - `gateTransitApi.js`
  - `plateRecognitionApi.js`
- Keep clear error messaging if canonical endpoints fail.
- Remove now-unneeded `@deprecated` fallback comments when fallback code is removed.

Validation:
- `rg` scan confirms no legacy paths remain in frontend:
  - `/SetCam`, `/QR_Dong`, `/Gate/`, `/BienSo/`, `/FaceID` (except intentional user-facing text labels)
- Frontend build pass.
- Manual smoke for key screens:
  - Settings camera runtime
  - Gate transit monitor
  - Dynamic QR generate/verify
  - License plate monitor

Exit criteria:
- Frontend no longer calls legacy backend aliases.

### Phase 3 - Backend Alias Route Removal (final API hard-cut)

Objective: remove redundant alias route attributes once frontend is canonical-only.

Work:
- Remove alias route attributes:
  - `CameraRuntimeController.cs`: remove `api/SetCam`
  - `DynamicQrController.cs`: remove `api/QR_Dong`
  - `FaceRecognitionController.cs`: remove `api/FaceID` (if frontend and integrations already migrated)
- Verify canonical routes remain unchanged:
  - `api/camera-runtime`
  - `api/dynamic-qr`
  - `api/face-recognition`

Validation:
- Backend build pass.
- Endpoint smoke via frontend and/or API client.
- Route scan shows no removed alias routes.

Exit criteria:
- Backend only exposes canonical route surface for these domains.

### Phase 4 - DTO/Class Naming Consistency Sweep (safe symbol-only pass)

Objective: reduce naming debt inside code without contract breaks.

Candidates:
- `SetCamRequest` naming alignment to canonical camera runtime terminology.
- Remaining mixed-language internal symbols (variables/messages/comments) in controllers and services.

Rules:
- No JSON property or API contract break unless explicitly versioned.
- If rename impacts serialized contracts, defer to separate compatibility batch.

Validation:
- Build pass.
- Symbol reference scan clean.

### Phase 5 - Encoding and Text Hygiene

Objective: resolve mojibake/encoding artifacts and preserve Vietnamese readability.

Work:
- Fix corrupted Vietnamese literals/comments where currently garbled in source files.
- Standardize file encoding approach (UTF-8 without BOM unless toolchain requires otherwise).
- Remove stale backup artifact `View/src/components/ThongHanhQR.vue.bak_mojibake` if no longer needed.

Validation:
- Build pass.
- UI quick-check for affected messages.

### Phase 6 - Runtime Process Naming Consistency (AI orchestrator layer)

Objective: align runtime process identifiers/messages with canonical naming.

Candidates:
- `QR_Dong.py` process label handling in:
  - `API/API/API/Controllers/CameraRuntimeController.cs`
  - `API/API/API/Services/RuntimeOrchestrator.cs`

Rules:
- File-path/process-name changes only after startup scripts and deployment docs are updated in same batch.
- Keep backward-compatible runtime key mapping for one release if external scripts depend on old keys.

Validation:
- Start/stop/status workflow works for all runtime services.

### Phase 7 - Documentation and Operational Closure

Objective: produce final consistent documents for the team.

Deliverables:
- `RENAME_CHECKPOINT_POST_U.md` (new consolidated checkpoint)
- Updated execution changelog for each completed phase
- "Canonical API surface" section in main docs

Exit criteria:
- New contributors can follow one source of truth without reading old contradictory reports.

## 5) Quality gates for every phase

For each phase:
- Build gate:
  - Frontend build pass
  - Backend build pass
- Scan gate:
  - Target legacy tokens reduced as expected (no accidental regressions)
- Runtime gate:
  - Minimum smoke scenario pass for impacted modules
- Rollback gate:
  - Clear rollback commit/tag available

## 6) Suggested batch mapping (new sequence)

- Batch V: Phase 0 + Phase 1 (baseline + doc reconciliation)
- Batch W: Phase 2 (frontend canonical-only calls)
- Batch X: Phase 3 (backend alias route removal)
- Batch Y: Phase 4 + Phase 5 (internal naming + encoding hygiene)
- Batch Z: Phase 6 + Phase 7 (runtime naming consistency + final checkpoint)

## 7) Risk register

- High risk:
  - Removing backend alias routes before frontend canonical migration completes.
- Medium risk:
  - Symbol renames that accidentally impact model binding/serialization.
  - Process-key rename in runtime orchestrator without script alignment.
- Low risk:
  - Documentation reconciliation and deprecation annotation updates.

## 8) Immediate next action (recommended)

Start Batch V now:

1. Generate baseline build + route/call inventories.
2. Publish reconciliation note.
3. Lock checkpoint before code removals in Batch W.

