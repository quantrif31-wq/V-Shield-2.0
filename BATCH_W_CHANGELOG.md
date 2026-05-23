# Batch W - Frontend Canonical Endpoint Enforcement

Date: 2026-05-23

## Scope
Removed legacy endpoint fallbacks in active frontend service layer and enforced canonical API paths only.

## Updated files
- `View/src/services/cameraRuntimeApi.js`
- `View/src/services/dynamicQrApi.js`
- `View/src/services/dynamicQrVerifyApi.js`
- `View/src/services/gateTransitApi.js`
- `View/src/services/plateRecognitionApi.js`

## Changes
1. Removed fallback wrappers (`getWithFallback`, `postWithFallback`, `putWithFallback`, `deleteWithFallback`) where legacy paths were used.
2. Kept exported function names unchanged to avoid breaking call sites.
3. Switched requests to canonical-only backend routes:
- `camera-runtime/*`
- `dynamic-qr/*`
- `gate-transit/*`
- `license-plates/*`

## Verification
- Legacy endpoint token scan in `View/src/services`: clean for deprecated fallback paths (`SetCam`, `QR_Dong`, `/Gate/`, `BienSo`) except runtime filename text in QR scanner message.
- Frontend build: PASS (`npm run build`)
- Backend build: PASS (`dotnet build API.sln`)

## Risk
- Runtime risk: Low for migrated backend surface.
- External environments still depending on removed alias routes will require backend Batch X completion coordination.

