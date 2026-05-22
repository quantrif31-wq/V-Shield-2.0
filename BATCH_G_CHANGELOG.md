# Batch G - PascalCase Route Normalization with Backward Redirects

Date: 2026-05-22

## Scope
Standardized remaining PascalCase routes in frontend router to kebab-case English routes, while keeping old route paths active through redirects.

## Router updates
File: `View/src/router/index.js`

- Primary route migration:
  - `FaceID` -> `face-id-security` (name: `FaceIdSecurity`)
  - `QrAccessMonitor` -> `qr-access-monitor` (name: `QrAccessMonitor`)
  - `AccessPermissionManager` -> `access-permission-manager` (name: `AccessPermissionManager`)

- Legacy compatibility redirects retained:
  - `FaceID` -> `FaceIdSecurity`
  - `QrAccessMonitor` -> `QrAccessMonitor`
  - `AccessPermissionManager` -> `AccessPermissionManager`

## Sidebar updates
File: `View/src/components/Layout/Sidebar.vue`

- Updated navigation paths to standardized routes:
  - `/FaceID` -> `/face-id-security`
  - `/thonghanh` -> `/gate-transit-monitor`
  - `/scan_qr_d` -> `/dynamic-qr-scanner`
  - `/tao_qr_d` -> `/dynamic-qr-generator`

## Verification
- Frontend: `npm run build` succeeded.
- Backend: `dotnet build API.sln` succeeded.

## Risk
- Runtime risk: Low (legacy paths redirected).
- Refactor safety: High (routing surface now more consistent and English-standardized).
