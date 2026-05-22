# Batch M - Call-site Migration to English Service Aliases (GateTransitMonitor)

Date: 2026-05-22

## Scope
Migrated service function call-sites in `GateTransitMonitor.vue` to English-standard alias names introduced in Batch L.
No behavior change, no endpoint change.

## Updated file
- `View/src/components/GateTransitMonitor.vue`

## Import + usage migration
### cameraRuntimeApi aliases
- `startPythonQr` -> `startPythonQrProcess`
- `stopPythonQr` -> `stopPythonQrProcess`
- `startPythonPlate` -> `startPythonPlateProcess`
- `stopPythonPlate` -> `stopPythonPlateProcess`
- `startPythonCamGiaLap` -> `startPythonSimulatedCameraProcess`
- `stopPythonCamGiaLap` -> `stopPythonSimulatedCameraProcess`
- `statusPython` -> `getPythonProcessStatus`

### dynamicQrScannerApi aliases
- `startQr` -> `startQrScanner`
- `resetQr` -> `resetQrSession`
- `stopQr` -> `stopQrScanner`
- `getQrResult` -> `getQrScanResult`
- `scanQr` -> `scanQrOnce`

## Compatibility
- Old export names still exist in services (from Batch L), so this migration is backward-safe and incremental.

## Verification
- Frontend: `npm run build` succeeded.
- Backend: `dotnet build API.sln` succeeded.

## Risk
- Runtime risk: Low (symbol-level rename to existing aliases only).
- Refactor safety: Improved (main high-traffic monitor component now uses standardized naming).
