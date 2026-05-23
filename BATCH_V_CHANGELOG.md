# Batch V - Baseline Freeze and State Reconciliation

Date: 2026-05-23

## Scope
- Established a fresh technical baseline after syncing to latest `main`.
- Reconciled documentation status with active source reality.
- No runtime behavior changes in application code.

## Baseline snapshot
- Git HEAD: `3eceb96bf6766da09c8cdda01e350afb19f2ece1`
- Branch: `main`
- Timestamp: `2026-05-23 09:45:00 +07:00`

## Verification
- Frontend build: PASS (`npm run build`, after reinstalling dependencies via `npm install`)
- Backend build: PASS (`dotnet build API.sln`)

## Generated baseline artifacts
- `analysis_api_route_attributes_2026-05-23.txt`
- `analysis_legacy_surface_hits_2026-05-23.txt`

## Reconciliation findings (active code)
- Frontend still has legacy endpoint fallbacks in canonical services for:
  - `SetCam`
  - `QR_Dong`
  - `Gate`
  - `BienSo`
- Backend still exposes alias route attributes in:
  - `CameraRuntimeController` (`api/SetCam`)
  - `DynamicQrController` (`api/QR_Dong`)
  - `FaceRecognitionController` (`api/FaceID`)

## Next batch
- Proceed to Batch W: remove frontend fallback usage and enforce canonical-only endpoint calls.

