# UI RC1 rollback procedure

## Controlled inputs

```text
Current production artifact digest: [IT TO PROVIDE]
Previous artifact digest: [IT TO PROVIDE]
RC artifact digest: 9256a27bf683bc701b2b0e6bb7df5626feb770013a23b445eb3ce7e2f593e609
Deployment pipeline/job: [IT TO PROVIDE]
Rollback pipeline/job: [IT TO PROVIDE]
Cache/CDN invalidation: [IT TO PROVIDE]
Rollback owner: [IT TO PROVIDE]
Smoke command: [IT TO PROVIDE]
Rehearsal timestamp: [NOT STARTED]
Rehearsal result: [BLOCKED]
```

## Release inventory

- Previous frontend: record immutable image tag/artifact digest and deployed timestamp before rollout.
- RC frontend: `release/v-shield-2.0-ui-rc1`; record commit and image/artifact digest after the RC is frozen.
- This UI hardening introduces no database migration, API contract or mandatory backend dependency. Deploy the frontend artifact independently. The current working tree contains unrelated backend/migration work; it must not be included in this frontend-only RC without a separate backend migration review.
- No new feature flag or service worker is introduced. If the hosting platform injects one, record its cache/version key before rollout.

## Rehearsal and execution

1. Before deployment, confirm the prior frontend artifact is still retrievable and passes login smoke against the current UAT backend.
2. Deploy RC by immutable reference; do not overwrite the previous tag.
3. If rollback criteria fire, atomically repoint the web service/static origin to the previous artifact. Do not roll back the database for this frontend-only RC.
4. Purge CDN/edge HTML and `index.html`; retain content-hashed JS/CSS. Invalidate any reverse-proxy HTML cache. If a service worker exists in the target environment, unregister/invalidate it and reload clients.
5. Verify login assets, Admin MFA, dashboard, one permitted and one denied route, Employees/Visitors/Vehicles/Access Logs, SignalR and logout.
6. Record start/end time, old/new digest, operator, reason, cache purge result and smoke evidence.

Rollback triggers: login/MFA failure, authorization bypass, widespread chunk/CSS failure, data-destructive regression, unrecoverable API loop, SignalR connection storm or critical accessibility blocker.

Rollback status is **PROCEDURE READY / REHEARSAL BLOCKED**. It remains untested for RC1 until IT performs steps 1–6 in UAT and signs the record.
