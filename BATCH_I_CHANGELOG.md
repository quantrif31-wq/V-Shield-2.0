# Batch I - Sidebar Label Standardization (UI-only)

Date: 2026-05-22

## Scope
Standardized remaining abbreviated sidebar labels to clear English display names. No API contract, route path, or data model changes.

## Updated file
- `View/src/components/Layout/Sidebar.vue`

## Label updates
- `thonghanh` -> `Gate Transit Monitor`
- `tao_qr_d` -> `Dynamic QR Generator`
- `scan_qr_d` -> `Dynamic QR Scanner`

## Safety
- UI text-only changes.
- No change to route path values, backend endpoints, payload keys, or storage keys.

## Verification
- Frontend: `npm run build` succeeded.
- Backend: `dotnet build API.sln` succeeded.

## Risk
- Runtime risk: Very Low.
- Refactor safety: Improved readability and naming consistency in navigation layer.
