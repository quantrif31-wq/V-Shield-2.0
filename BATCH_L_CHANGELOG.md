# Batch L - Service Function Alias Standardization (Compatibility-first)

Date: 2026-05-22

## Scope
Added English-standard function aliases in frontend services while keeping original exported names intact.
No endpoint/path or payload contract changes.

## Updated files
- `View/src/services/cameraRuntimeApi.js`
- `View/src/services/dynamicQrScannerApi.js`

## cameraRuntimeApi.js changes
- Internal client variable renamed:
  - `setCamApiClient` -> `cameraRuntimeApiClient`
- Added English alias exports:
  - `startPythonQrProcess` -> `startPythonQr`
  - `stopPythonQrProcess` -> `stopPythonQr`
  - `startPythonPlateProcess` -> `startPythonPlate`
  - `stopPythonPlateProcess` -> `stopPythonPlate`
  - `startPythonSimulatedCameraProcess` -> `startPythonCamGiaLap`
  - `stopPythonSimulatedCameraProcess` -> `stopPythonCamGiaLap`
  - `getPythonProcessStatus` -> `statusPython`

## dynamicQrScannerApi.js changes
- Added English alias exports:
  - `startQrScanner` -> `startQr`
  - `scanQrOnce` -> `scanQr`
  - `resetQrSession` -> `resetQr`
  - `stopQrScanner` -> `stopQr`
  - `getQrScanResult` -> `getQrResult`

## Verification
- Frontend: `npm run build` succeeded.
- Backend: `dotnet build API.sln` succeeded.

## Risk
- Runtime risk: Very Low (additive aliases, old names preserved).
- Refactor safety: High (enables gradual consumer migration with zero-break rollout).
