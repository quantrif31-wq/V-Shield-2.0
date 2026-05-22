# Batch K - Frontend Service Naming Standardization with Compatibility Shims

Date: 2026-05-22

## Scope
Standardized legacy/mixed-language frontend service file names to clear English names.
Kept backward compatibility by preserving old filenames as re-export shims.

## Service file renames
- `setcamAPI.js` -> `cameraRuntimeApi.js`
- `thonghanhAPI.js` -> `gateTransitApi.js`
- `biensoApi.js` -> `plateCameraApi.js`
- `qr_dAPI.js` -> `dynamicQrScannerApi.js`
- `videofaceAPI.js` -> `faceVideoApi.js`

## Compatibility strategy
Legacy files were recreated as shims:
- `setcamAPI.js` re-exports from `cameraRuntimeApi.js`
- `thonghanhAPI.js` re-exports from `gateTransitApi.js`
- `biensoApi.js` re-exports from `plateCameraApi.js`
- `qr_dAPI.js` re-exports from `dynamicQrScannerApi.js`
- `videofaceAPI.js` re-exports from `faceVideoApi.js`

This prevents import breakage for old call sites while allowing gradual migration.

## Import migration
Primary imports in active components/pages were moved to new service file names where detected.

## Verification
- Frontend: `npm run build` succeeded.
- Backend: `dotnet build API.sln` succeeded.

## Risk
- Runtime risk: Low (shim-based backward compatibility retained).
- Refactor safety: High (clearer service naming + incremental migration path).
