# Batch H - Face Video Route Standardization

Date: 2026-05-22

## Scope
Standardized remaining non-semantic face video route naming while preserving backward compatibility.

## Router updates
File: `View/src/router/index.js`

- Primary route introduced:
  - `face-video-monitor` (name: `FaceVideoMonitor`)
- Legacy compatibility redirect kept:
  - `facevideo` -> `FaceVideoMonitor`

## Sidebar updates
File: `View/src/components/Layout/Sidebar.vue`

- Updated navigation path:
  - `/facevideo` -> `/face-video-monitor`

## Verification
- Frontend: `npm run build` succeeded.
- Backend: `dotnet build API.sln` succeeded.

## Risk
- Runtime risk: Low (legacy path redirects preserved).
- Refactor safety: Improved (route naming now consistent with kebab-case English style).
