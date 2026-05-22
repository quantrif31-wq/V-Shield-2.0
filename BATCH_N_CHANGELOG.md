# Batch N - Service API Naming Inversion (English-first, Backward-compatible)

Date: 2026-05-22

## Scope
Promoted English-standard function names as primary exports inside core frontend services, while retaining legacy names as aliases.
This keeps existing call-sites safe and supports gradual migration.

## Updated files
- `View/src/services/cameraRuntimeApi.js`
- `View/src/services/dynamicQrScannerApi.js`

## cameraRuntimeApi.js
- Primary function declarations renamed to English-standard names:
  - `startPythonQrProcess`
  - `stopPythonQrProcess`
  - `startPythonPlateProcess`
  - `stopPythonPlateProcess`
  - `startPythonSimulatedCameraProcess`
  - `stopPythonSimulatedCameraProcess`
  - `getPythonProcessStatus`
- Backward aliases preserved:
  - `startPythonQr`, `stopPythonQr`, `startPythonPlate`, `stopPythonPlate`,
    `startPythonCamGiaLap`, `stopPythonCamGiaLap`, `statusPython`

## dynamicQrScannerApi.js
- Primary function declarations renamed to English-standard names:
  - `startQrScanner`
  - `scanQrOnce`
  - `resetQrSession`
  - `stopQrScanner`
  - `getQrScanResult`
- Backward aliases preserved:
  - `startQr`, `scanQr`, `resetQr`, `stopQr`, `getQrResult`

## Verification
- Frontend: `npm run build` succeeded.
- Backend: `dotnet build API.sln` succeeded.

## Risk
- Runtime risk: Very Low (legacy names remain exported).
- Refactor safety: High (public service APIs now English-first with compatibility layer).
