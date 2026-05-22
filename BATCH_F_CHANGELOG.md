# Batch F - Route Naming Standardization with Legacy Redirects

Date: 2026-05-22

## Scope
Standardized key Vietnamese route names/paths to English while preserving backward compatibility.

## Router changes
File: `View/src/router/index.js`

- Added route-name constant:
  - `ROUTE_NAME_DYNAMIC_QR_GENERATOR = 'DynamicQrGenerator'`

- Introduced English primary routes:
  - `license-plate-security` (name: `LicensePlateSecurity`)
  - `gate-transit-monitor` (name: `GateTransitMonitor`)
  - `dynamic-qr-generator` (name: `DynamicQrGenerator`)
  - `dynamic-qr-scanner` (name: `DynamicQrScanner`)

- Preserved legacy routes via redirect:
  - `bienso` -> `LicensePlateSecurity`
  - `thonghanh` -> `GateTransitMonitor`
  - `tao_qr_d` -> `DynamicQrGenerator`
  - `scan_qr_d` -> `DynamicQrScanner`

- Updated guard/home redirects for Staff role to new route name constant.

## Compatibility impact
- Existing old URLs keep working through redirect.
- Internal navigation now uses standardized English route naming for staff default flow.

## Verification
- Frontend: `npm run build` succeeded.
- Backend: `dotnet build API.sln` succeeded.

## Risk
- Runtime risk: Low (legacy path compatibility retained).
- Refactor safety: High (route naming now normalized with migration-safe redirect layer).
