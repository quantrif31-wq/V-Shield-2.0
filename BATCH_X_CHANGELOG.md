# Batch X - Backend Alias Route Removal

Date: 2026-05-23

## Scope
Removed remaining legacy backend route aliases after frontend was migrated to canonical-only endpoint calls (Batch W).

## Updated files
- `API/API/API/Controllers/CameraRuntimeController.cs`
- `API/API/API/Controllers/DynamicQrController.cs`
- `API/API/API/Controllers/FaceRecognitionController.cs`

## Changes
- Removed `[Route("api/SetCam")]` from `CameraRuntimeController`.
- Removed `[Route("api/QR_Dong")]` from `DynamicQrController`.
- Removed `[Route("api/FaceID")]` from `FaceRecognitionController`.
- Kept canonical routes unchanged:
  - `api/camera-runtime`
  - `api/dynamic-qr`
  - `api/face-recognition`

## Verification
- Backend build: PASS (`dotnet build API.sln`)
- Frontend build: PASS (`npm run build`)
- Route alias scan: clean for `Route("api/SetCam")`, `Route("api/QR_Dong")`, `Route("api/FaceID")`.

## Risk
- Runtime risk for current frontend: Low (frontend already canonical-only).
- External clients still calling removed alias routes need endpoint update.

