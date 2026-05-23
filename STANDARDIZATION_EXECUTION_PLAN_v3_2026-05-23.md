# STANDARDIZATION_EXECUTION_PLAN_v3

Date: 2026-05-23
Project: V-Shield-2.0
Base commit: 4e801c7

## Objective
Complete project standardization safely before feature continuation:
- English-first naming for active code surfaces.
- Remove/contain remaining legacy naming debt.
- Prevent recurrence of Vietnamese text corruption (mojibake).

## Safety policy (mandatory)
1. Vietnamese text fixes must be manual, line-by-line only.
2. No automated bulk replace for multilingual text.
3. Every batch must pass build gates before moving on.
4. Contract-sensitive renames (API/DB/runtime keys) must be isolated and documented.

## Batch sequence

### Batch Z1 - Low-risk internal naming cleanup
- Rename internal DTO/type/file symbols with minimal blast radius.
- Keep API route contracts and serialized payload fields unchanged.
- Example scope:
  - `SetCamRequest` -> `CameraRuntimeUpsertRequest`.

### Batch Z2 - Frontend internal naming alignment
- Normalize remaining internal aliases/variables referencing legacy names.
- Keep user-facing behavior unchanged.

### Batch Z3 - Runtime naming reconciliation
- Standardize runtime service labels/keys where safe.
- Keep backward compatibility map when external scripts may depend on old keys.

### Batch Z4 - Contract-sensitive review gate
- Re-assess items that could impact external clients or deployed runtime contracts.
- Execute only with explicit compatibility strategy and rollback notes.

### Batch Z5 - Final audit and closure
- Full repo scan for residual legacy naming patterns.
- Full repo scan for mojibake patterns.
- Publish final checkpoint report.

## Build/verification gate per batch
- Backend: `dotnet build API.sln`
- Frontend: `npm run build`
- Naming scan:
  - legacy token check (`SetCam`, `QR_Dong`, `FaceID`, etc.)
- Mojibake scan:
  - suspicious charset pattern scan in active code

