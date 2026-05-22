# Batch S - Hard-cut Stage 2 (Legacy Function Alias Removal)

Date: 2026-05-22

## Scope
Executed Stage 2 from hard-cut checklist: removed deprecated legacy function aliases in canonical frontend services.

## Updated files
- `View/src/services/cameraRuntimeApi.js`
- `View/src/services/dynamicQrScannerApi.js`

## Verification
- Legacy alias symbol scan (active code): clean
- Frontend build: PASS (`npm run build`)
- Backend build: PASS (`dotnet build API.sln`)

## Risk
- Runtime risk after Stage 2: Low
- Remaining compatibility layer concentrated in router legacy redirects only.
