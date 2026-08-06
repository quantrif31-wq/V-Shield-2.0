# V-Shield 2.0 UI RC2 release report

Updated: 2026-08-06

## Outcome

RC2 fixes the release-pipeline gap by adding 120 reviewed Linux snapshots
rendered in the pinned Playwright environment. No Windows image was copied or
renamed, no threshold was weakened, and the release workflow never updates
snapshots.

GitHub Actions run `31036497935` passed on release commit
`3a357e084344f2fabdc7fb7c3f1f11d92d203a9b`. The protected UAT job was skipped
because the `v-shield-uat` environment and protected inputs still require a
repository administrator.

## Visual root cause and resolution

```text
Existing baseline naming: <case>-<project>-win32.png
Expected CI naming: <case>-<project>-linux.png
Playwright projects: desktop-1920, desktop-1440, tablet-768, tablet-1024, mobile-390
Browser: Chromium 151.0.7922.34
CI OS: Ubuntu Linux
Snapshot template: Playwright default OS-specific suffix
Root cause: repository had Windows baselines but no rendered Linux baselines
Resolution: render and review 120 Linux baselines in the pinned Linux container
```

Canonical rendering used
`mcr.microsoft.com/playwright:v1.62.1-noble` at digest
`sha256:dcc5531e97840b9b5e794f2814476b21571c5124a3fca2267d73041f56e7580e`,
Node 22.23.1, npm 10.9.8, Playwright 1.62.1, locale `vi-VN`, timezone
`Asia/Ho_Chi_Minh`, reduced motion and one worker. Review found no missing or
unexpected baseline and no content clipping regression.

## Gate summary

| Gate | Result |
|---|---|
| Linux functional | PASS — 75/75 |
| Linux visual | PASS — 120/120 |
| Accessibility | PASS — 50/50 |
| Unit | PASS — 26/26 |
| Build | PASS — 458 modules |
| Production/sensitive scans | PASS — 196/198 files |
| npm audit | PASS — 0 vulnerabilities |
| Artifact reproducibility | PASS — two identical ZIP digests |

The remaining non-blocking build observation is the approximately 1.05 MB
minified MapLibre vendor chunk. UAT network and route telemetry must be reviewed
before production approval.

## Open blockers

| Severity | Area | Status | Required owner/action |
|---|---|---|---|
| Blocker | GitHub Environment | Not created | Repo admin creates and protects `v-shield-uat` |
| Blocker | Protected inputs | Not configured | Repo admin/IT configures names in the admin request |
| Blocker | UAT deployment | Not started | IT deploys the immutable RC2 digest |
| Blocker | Real UAT | Not run | QA runs authentication, roles, mutations, SignalR, performance and smoke |
| Blocker | Rollback | Not rehearsed | IT supplies prior digest and rollback owner |
| Blocker | Sign-off | Not provided | IT, QA and Business approve evidence |

## Final status

**RC2 PACKAGED — UAT ADMIN BLOCKED — PRODUCTION NOT READY**
