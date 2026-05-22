# Batch P - Deprecation Tagging Phase

Date: 2026-05-22

## Scope
Marked legacy compatibility surfaces as deprecated to prepare controlled hard-cut in future releases.
No runtime behavior changes.

## Updated files
- `View/src/services/setcamAPI.js`
- `View/src/services/thonghanhAPI.js`
- `View/src/services/biensoApi.js`
- `View/src/services/qr_dAPI.js`
- `View/src/services/videofaceAPI.js`
- `View/src/services/cameraRuntimeApi.js`
- `View/src/services/dynamicQrScannerApi.js`
- `View/src/router/index.js`

## Changes
1. Shim service files
- Added `@deprecated` headers indicating new canonical service files.

2. Service alias exports
- Added `@deprecated` marker comments above backward-compatible alias blocks in:
  - `cameraRuntimeApi.js`
  - `dynamicQrScannerApi.js`

3. Legacy router redirects
- Added `@deprecated` inline comments for legacy route paths kept only for migration window.

## Verification
- Frontend: `npm run build` succeeded.
- Backend: `dotnet build API.sln` succeeded.

## Risk
- Runtime risk: Very Low (comment/annotation-only plus existing compatibility paths unchanged).
- Refactor safety: Increased (clear visibility of migration debt and removal candidates).
