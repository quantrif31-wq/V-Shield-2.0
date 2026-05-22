# Batch Critical - Frontend Route Alias Migration (Compatibility First)

Date: 2026-05-22

## Scope
Updated frontend API calls to prefer new English route aliases while retaining fallback to legacy routes.

## Updated files
- View/src/services/plateRecognitionApi.js
- View/src/services/thonghanhAPI.js
- View/src/services/setcamAPI.js
- View/src/services/dynamicQrApi.js
- View/src/services/dynamicQrVerifyApi.js
- View/src/components/AccessPermissionManager.vue

## Route migration strategy
- Primary call: new alias route
- Fallback call: legacy route when HTTP 404

## Mapping applied
- `/BienSo/*` -> `/license-plates/*`
- `/Gate/*` -> `/gate-transit/*`
- `/SetCam/*` -> `/camera-runtime/*`
- `/QR_Dong/*` -> `/dynamic-qr/*`
- `/api/AccessPermission/*` -> `/api/access-permissions/*`

## Verification
- Frontend: `npm run build` succeeded (Vite chunk-size warning only)
- Backend: `dotnet build API.sln` succeeded (0 warnings, 0 errors)

## Risk assessment
- Runtime risk: Low (fallback preserves backward compatibility)
- Refactor safety: High for next phase (legacy route dependency now isolated)
