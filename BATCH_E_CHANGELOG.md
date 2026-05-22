# Batch E - Frontend Component File Naming Standardization

Date: 2026-05-22

## Scope
Standardized Vietnamese/abbreviated Vue component filenames to English names without changing route contracts.

## File renames
- `View/src/components/BienSoSecurity.vue` -> `View/src/components/LicensePlateSecurity.vue`
- `View/src/components/ThongHanhQR.vue` -> `View/src/components/GateTransitMonitor.vue`
- `View/src/components/Tao_QR_D.vue` -> `View/src/components/DynamicQrGenerator.vue`
- `View/src/components/Scan_QR_D.vue` -> `View/src/components/DynamicQrScanner.vue`

## Reference updates
- Router imports updated in `View/src/router/index.js`.
- Component internal name updated:
  - `BienSoSecurity` -> `LicensePlateSecurity` in `View/src/components/LicensePlateSecurity.vue`.

## Compatibility
- Route `path` and `name` values were intentionally preserved to avoid any navigation/runtime regressions.

## Verification
- Frontend: `npm run build` succeeded.
- Backend: `dotnet build API.sln` succeeded.

## Risk
- Runtime risk: Low (import-path-only refactor).
- Refactor safety: Improved (file names now semantically aligned, easier global rename mapping).
