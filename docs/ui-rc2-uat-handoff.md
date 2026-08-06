# V-Shield 2.0 UI RC2 UAT handoff

## Immutable candidate

```text
Branch: release/v-shield-2.0-ui-rc1
Commit: 3a357e084344f2fabdc7fb7c3f1f11d92d203a9b
Tag: v-shield-2.0-ui-rc2
Artifact: v-shield-2.0-ui-rc2.zip
Artifact SHA-256: 627b62e7a69e60337d65d8db7443f414498264605700af727ac49d38424fb5c9
Expected version: 2.0.0-rc2
GitHub Actions run: 31036497935
```

Do not deploy by a mutable branch reference. Resolve the tag, verify it equals
the commit above, verify the artifact digest before deployment, and retain the
previous stable artifact by immutable digest.

## Administrator prerequisite

Follow `docs/ui-rc2-repo-admin-request.md`. The current operator has push access
but lacks repository admin/maintain permission; GitHub returned HTTP 403 when
creating `v-shield-uat`. Do not retry with the same authority and do not place
secret values in tickets, chat, documentation or logs.

## Entry gate

UAT may start only when all items below are evidenced:

- `v-shield-uat` exists with required reviewers and approved release refs.
- Protected secret and variable names are configured.
- RC2 is deployed and its frontend marker and artifact digest match this file.
- Five isolated UAT role accounts and the IT-approved MFA mechanism work.
- Tenant/site boundaries and forbidden production targets are confirmed.
- Mutation, role-matrix and performance manifests are reviewed and approved.
- Cleanup owner, previous artifact digest, rollback owner and rollback method
  are recorded.

## Execution order

1. Run protected preflight without mutation and retain sanitized evidence.
2. Verify authentication, MFA and direct-route/API authorization for all five
   roles.
3. Run approved integration mutations with mandatory cleanup in `finally`.
4. Exercise SignalR reconnect/stale/disconnected behavior and API fallback.
5. Measure route, API, map/camera and large-chunk behavior on the UAT network.
6. Run smoke against the deployed RC2 marker and digest.
7. Rehearse rollback to the recorded previous digest and rerun smoke.
8. Collect IT, QA and Business sign-off.

Any preflight, cleanup, security scan, permission, digest or rollback failure is
a release blocker. Never redirect a failed UAT case to production data.

## Current readiness

```text
Artifact deployed: NO
Protected preflight: NOT RUN
Five-role accounts: NOT VERIFIED
MFA: NOT VERIFIED
Mutation manifest: NOT APPROVED
Previous artifact digest: NOT PROVIDED
Rollback rehearsal: NOT RUN
IT/QA/Business sign-off: NOT PROVIDED

RC2 PACKAGED — UAT ADMIN BLOCKED — PRODUCTION NOT READY
```
