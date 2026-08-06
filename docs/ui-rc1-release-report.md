# V-Shield 2.0 UI RC1 release report

Updated: 2026-08-05

## Release status

```text
Build: PASS — Vite production build, 458 modules, exit 0
Unit: PASS — 26/26
Playwright: PASS — 195/195 across five viewports
Visual: PASS — 120/120 RC-scope baselines; snapshots were not updated
Accessibility: PASS — 50/50 axe baselines plus keyboard/focus flow; no critical/serious finding
Security artifact scan: PASS — 196 production / 198 available combined files

RC commit: PASS
RC tag: PASS
Artifact digest: PASS

UAT authentication: BLOCKED
UAT integration: BLOCKED
Role matrix: HARNESS READY
Mutation: NOT STARTED
SignalR: UNIT PASS / UAT BLOCKED
Performance: HARNESS READY
Smoke: BLOCKED
Rollback: PROCEDURE READY / REHEARSAL BLOCKED

IT sign-off: NOT PROVIDED
QA sign-off: NOT PROVIDED
Business sign-off: NOT PROVIDED

Production readiness: NOT READY
```

The visual matrix excludes five legacy `/dashboard` baselines because that page contains uncommitted VIP feature work outside this RC. The migrated `/operations-dashboard` and all other included modules remain covered at every configured viewport. npm audit reports 0 vulnerabilities.

## Implemented RC hardening

- Restored safe post-login deep-link navigation and explicit Employee Face URL clearing.
- Added redacted, transport-pluggable frontend observability for global/Vue/promise/API/auth/permission/import-export/route chunk/SignalR/map/camera failures.
- Added LCP, INP, CLS, TTFB, page/route/API/map/camera duration metrics and in-memory p50/p75/p95 summaries. Threshold breaches create warning/error events but do not fail an individual production session.
- Hardened chat/notification SignalR connection reuse, dynamic token retrieval, reconnect lifecycle, handler deduplication and cleanup. Chat exposes Live/Reconnecting/Stale/Disconnected and last updated while retaining API fallback/data.
- Added strict production artifact scanning, explicit visual-update script, protected real-UAT/role/smoke suites and CI report retention. Release CI never calls `--update-snapshots`.
- Suppressed only Rollup `INVALID_ANNOTATION` warnings whose module ID is inside `@microsoft/signalr`; all other warnings remain visible. The vendor annotation placement has no observed runtime or tree-shaking impact and `node_modules` is not patched.

The remaining build warning is the already separated `maplibre-vendor` chunk (approximately 1.05 MB minified). It is non-blocking for correctness but must be measured on the UAT network using the new route/dynamic-import/map p75 and p95 telemetry before production sign-off.

## Open UAT issues

| Severity | Module | Role | Steps / expected / actual | Evidence | Root cause | Fix status |
|---|---|---|---|---|---|---|
| Blocker | Release qualification | All five | Run protected UAT; expected all acceptance gates; actual not run | No UAT secrets/deployment supplied | External test prerequisites unavailable | Open |
| Blocker | Rollback | IT | Repoint UAT to prior digest and smoke; expected recovery; actual not rehearsed | No deployment record | Requires IT-controlled environment | Open |
| Blocker | Sign-off | IT/QA/Business | Review evidence; expected three approvals; actual none attached | Sign-off record absent | UAT not completed | Open |

## Production readiness

**NOT READY**. Source hardening and mocked/local gates are necessary but cannot prove real authentication, MFA, backend permissions, mutations, SignalR recovery, deployment smoke or rollback. Readiness may change only after every acceptance gate passes on the immutable UAT build and IT, QA and business sign off.
