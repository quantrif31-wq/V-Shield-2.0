# V-Shield 2.0 UI RC1 release manifest

## Release identity

| Field | Value |
|---|---|
| Product | V-Shield 2.0 |
| Release | UI RC1 |
| Branch | `release/v-shield-2.0-ui-rc1` |
| Commit SHA | Resolve immutable tag `v-shield-2.0-ui-rc1^{commit}`; the literal SHA is recorded in the packaging handoff and release output |
| Tag | `v-shield-2.0-ui-rc1` |
| Build timestamp | `2026-08-05T17:25:04Z` |
| Node | `v24.19.0` |
| npm | `11.17.0` |
| Lockfile SHA-256 | `93121d1320e4bd591308d61dc409558e678f1340330ff5f9e8a926df671d5291` |
| Production artifact | `v-shield-2.0-ui-rc1.zip` |
| Production artifact SHA-256 | `9256a27bf683bc701b2b0e6bb7df5626feb770013a23b445eb3ce7e2f593e609` |

The commit SHA cannot be embedded literally in the same commit whose hash it would change. The annotated tag is the canonical immutable lookup; use `git rev-list -n 1 v-shield-2.0-ui-rc1` and compare it with the packaging handoff.

## Release gates

```text
Build: PASS — Vite 7.3.6, 458 modules, exit 0
Unit: PASS — 26/26
Playwright: PASS — 195/195 across five viewports
Visual: PASS — 120/120; snapshots were not updated
Accessibility: PASS — 50/50 axe and keyboard/focus flow
Design check: PASS
Security artifact scan: PASS — 196 production files / 198 available artifact files
npm audit: PASS — 0 vulnerabilities
```

The RC visual matrix covers the nine migrated operational modules and their shared states. Five `/dashboard` baselines were intentionally excluded because that legacy page contains separate, uncommitted VIP work outside RC1; `/operations-dashboard` remains covered on all five viewports.

The production output contains 196 files and 3,945,178 uncompressed bytes. The deterministic ZIP is 1,219,508 bytes. Two archives created from the same candidate output were byte-for-byte identical.

## Included scope

- Employees.
- Visitors/PreRegistration.
- Vehicles.
- Access Logs.
- Device Management.
- Watchlist Queue.
- AI Review Queue.
- Redaction Queue.
- Operations Dashboard.
- Shared UI/design system.
- Theme/density.
- Login/MFA UX.
- SignalR lifecycle hardening.
- Observability.
- UAT harness.
- Rollback documentation.

## Excluded scope

- Legacy modules not migrated.
- Production backend changes.
- Database schema changes.
- Visitor biometric and VIP calendar feature work.
- UAT environment provisioning.
- Real credentials.
- IT deployment execution.
- Business sign-off.

## Known non-blocking observations

- MapLibre vendor is approximately 1.05 MB minified.
- Legacy CSS remains outside the migrated scope.
- The SignalR Rollup annotation warning is filtered only for the affected dependency.

## Current readiness

```text
RC CODE COMPLETE
UAT BLOCKED
PRODUCTION NOT READY
```
