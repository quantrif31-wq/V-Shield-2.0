# Batch T - Hard-cut Stage 3 (Legacy Router Redirect Removal)

Date: 2026-05-22

## Scope
Executed final hard-cut stage: removed legacy route redirects from frontend router.

## Updated file
- `View/src/router/index.js`

## Verification
- Legacy route/path scan (active code): clean
- Frontend build: PASS (`npm run build`)
- Backend build: PASS (`dotnet build API.sln`)

## Risk
- Runtime risk: Low for internal navigation (all active links use canonical routes).
- External bookmarked old URLs: now intentionally unsupported after hard-cut.
