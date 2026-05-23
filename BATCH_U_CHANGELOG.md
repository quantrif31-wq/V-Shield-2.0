# Batch U - Backend Hard-cut Completion (Legacy Alias Routes Removed)

Date: 2026-05-22

## Scope
Removed backend legacy compatibility routes that had been retained during migration.
Kept canonical English routes as the only active surface for those modules.

## Updated controllers
- `API/API/API/Controllers/GateTransitController.cs`
- `API/API/API/Controllers/BienSoController.cs`
- `API/API/API/Controllers/SetCamController.cs`
- `API/API/API/Controllers/QR_DongController.cs`
- `API/API/API/Controllers/AccessPermissionController.cs`

## Hard-cut changes
1. Route alias removal
- Removed legacy route attributes from the above controllers:
  - `api/[controller]` (where English canonical route already exists)
  - `api/Gate` (legacy path)

2. Compatibility type cleanup
- Removed `GateApiResponse` backward-compat alias class from `GateTransitController.cs`.
- Canonical response type remains `GateTransitApiResponse`.

## Verification
- Backend build: PASS (`dotnet build API.sln`)
- Frontend build: PASS (`npm run build`)

## Notes
- Remaining `[Route("api/[controller]")]` in other controllers are still their primary active routes (not dual legacy aliases in this hard-cut scope).
- Nullable warnings in backend remain pre-existing and non-blocking.

## Reconciliation note (2026-05-23)
- Post-sync source audit found residual alias routes still active in:
  - `CameraRuntimeController` (`api/SetCam`)
  - `DynamicQrController` (`api/QR_Dong`)
  - `FaceRecognitionController` (`api/FaceID`)
- These are now tracked as pending removal in Batch X under `RENAME_EXECUTION_PLAN_v2_2026-05-23.md`.

## Risk
- Runtime risk: Low for migrated frontend (already targets canonical English routes).
- External clients still calling removed legacy backend aliases may need endpoint update.
