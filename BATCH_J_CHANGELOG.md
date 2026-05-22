# Batch J - Internal Variable Naming Cleanup (Low-risk)

Date: 2026-05-22

## Scope
Standardized internal variable/helper naming in frontend router and sidebar logic.
No API contract, route path contract, payload key, or backend code change.

## Updated files
- `View/src/router/index.js`
- `View/src/components/Layout/Sidebar.vue`

## Router internal naming updates
- `currentUserRole` -> `currentRole` (local variables in redirect/guard branches)
- `routeRecord` -> `matchedRoute` (guard callbacks)

## Sidebar internal naming updates
- `canAccessNavItem` -> `canAccessNavigationItem`
- `quickSearchTimeout` -> `quickSearchDebounceTimer`
- `debouncedQuickSearch` -> `debouncedSearch`
- Updated all internal/template references accordingly.

## Verification
- Frontend: `npm run build` succeeded.
- Backend: `dotnet build API.sln` succeeded.

## Risk
- Runtime risk: Very Low (internal naming only, references updated together).
- Refactor safety: Improved readability and consistency for future large-scale rename steps.
