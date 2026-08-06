# UI RC1 protected UAT runbook

## Protected inputs

Use the GitHub environment `v-shield-uat` or an equivalent secret manager. Never commit `.env`, OTP/TOTP seeds, passwords, tokens or real storage state.

Required identity and target variables:

```text
VSHIELD_UAT_FRONTEND_URL
VSHIELD_UAT_API_URL
VSHIELD_UAT_API_HEALTH_URL             # optional when /health/ready is derivable
VSHIELD_UAT_SIGNALR_URL
VSHIELD_UAT_ENVIRONMENT=UAT
VSHIELD_UAT_TENANT
VSHIELD_UAT_SITE
VSHIELD_UAT_EXPECTED_VERSION
VSHIELD_UAT_FRONTEND_SHA256             # optional approved HTML digest
VSHIELD_UAT_RC_ARTIFACT_DIGEST
VSHIELD_UAT_PREVIOUS_ARTIFACT_DIGEST
VSHIELD_UAT_<ADMIN|GUARD|RECEPTIONIST|MANAGER|HR>_USERNAME
VSHIELD_UAT_<ADMIN|GUARD|RECEPTIONIST|MANAGER|HR>_PASSWORD
```

Set `VSHIELD_UAT_MFA_MODE` to `totp`, `manual`, `test-api` or `storage-state`, then supply only the corresponding protected inputs. Storage-state files must be outside the repository and are never uploaded. Full UAT additionally requires:

```text
VSHIELD_UAT_ALLOW_NEGATIVE_MFA=true
VSHIELD_UAT_ALLOW_MUTATIONS=true
VSHIELD_UAT_CLEANUP_POLICY=required
VSHIELD_UAT_MUTATION_MANIFEST_JSON (or PATH)
VSHIELD_UAT_ROLE_MATRIX_JSON (or PATH)
VSHIELD_UAT_PERFORMANCE_PLAN_JSON (or PATH)
VSHIELD_UAT_NETWORK_PROFILE=corporate|slow
```

## Fail-closed preflight

`npm run test:uat` first validates all contracts, then checks the deployed login HTML/version/SHA-256, API readiness, SignalR reachability and five credential probes. A token returned by a non-MFA probe is immediately logged out. Any failure prevents Playwright and all mutation cases from starting. The sanitized result is written to `View/uat-results/preflight.json`.

The mutation manifest requires every case to declare `module`, `role`, `action`, `kind`, `testDataPrefix`, `allowedTenant`, `allowedSite`, `expectedStatuses`, `expectedAuditRecord`, `auditCheck`, `cleanup`, `rollback` and `forbiddenTargets`. Prefixes must match `UAT-RC1-{{timestamp}}-<entity>`. Cleanup runs in `finally`, and a cleanup status outside the approved set fails the suite.

The role matrix contains five roles. Each role defines visible/hidden menu labels, allowed/denied direct routes and safe `probeOnly` API checks for read list/detail, create, edit, delete, approve, reject, import, export, upload, evidence, Face ID, device configuration, Watchlist, Redaction and backup. Real mutations belong only in the mutation manifest.

The performance plan requires 20–30 iterations, safe interaction selectors, Map/Camera metrics, explicit approval for click actions, and both `corporate` and `slow` profiles. Run the protected workflow once per profile and combine both reports. A low-spec workstation run remains an IT/QA-controlled execution record.

## Execution

1. Record immutable previous and RC artifact digests.
2. Deploy RC to UAT and select the approved network profile.
3. Run `npm run test:uat`. It executes 18 protected tests covering auth/MFA/session, tenant/site context, nine modules, mutation/audit/cleanup, five-role UI/API matrix, SignalR, performance and deployment smoke.
4. Run controlled manual scenarios that cannot be safely automated without IT authority: backend/hub restart, user disable, live permission change, token expiry timing, sleep/wake, physical network switch, deliberately duplicated/out-of-order hub events and rollback rehearsal.
5. Run `npm run security:artifacts:all` before uploading reports. Trace, screenshot and video are disabled. The scanner checks `dist`, local/HTML test reports, JSON/log results and an optional CI artifact root; findings name only the source/signature, never the secret value.
6. Repeat performance under the second network profile. Attach sanitized percentile and SignalR summaries.
7. Record defects using the required ID/severity/module/role/environment/precondition/steps/expected/actual/evidence/root-cause/fix-commit/retest fields.

## Sign-off

- IT/Infrastructure: deployment, monitoring transport, SignalR disruption, both performance profiles and rollback rehearsal.
- QA: functional, regression, accessibility, visual and artifact scan.
- Business owner: workflow, role/permission and test-data outcomes.

The report may record sign-off only when the named approver has provided it. Local or mocked Playwright evidence cannot substitute for protected UAT.
