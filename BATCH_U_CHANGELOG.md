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

## Risk
- Runtime risk: Low for migrated frontend (already targets canonical English routes).
- External clients still calling removed legacy backend aliases may need endpoint update.
