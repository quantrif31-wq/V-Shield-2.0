# Batch R - Hard-cut Stage 1 (Shim Removal)

Date: 2026-05-22

## Scope
Executed Stage 1 from hard-cut checklist: remove legacy service shim files.

## Removed files
- `View/src/services/setcamAPI.js`
- `View/src/services/thonghanhAPI.js`
- `View/src/services/biensoApi.js`
- `View/src/services/qr_dAPI.js`
- `View/src/services/videofaceAPI.js`

## Follow-up fix
- Updated residual import in `View/src/services/cameraRegistryApi.js`:
  - `./setcamAPI` -> `./cameraRuntimeApi`

## Verification
- Legacy shim import scan (active code): clean
- Frontend build: PASS (`npm run build`)
- Backend build: PASS (`dotnet build API.sln`)

## Risk
- Runtime risk after Stage 1: Low
- Compatibility retained at function alias and route redirect layers
