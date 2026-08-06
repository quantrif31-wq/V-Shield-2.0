# V-Shield 2.0 UI RC1 UAT handoff

## RC identity

| Field | Value |
|---|---|
| Branch | `release/v-shield-2.0-ui-rc1` |
| Commit SHA | Resolve `v-shield-2.0-ui-rc1^{commit}` and verify against the release output |
| Tag | `v-shield-2.0-ui-rc1` |
| Artifact | `v-shield-2.0-ui-rc1.zip` |
| Artifact SHA-256 | `9256a27bf683bc701b2b0e6bb7df5626feb770013a23b445eb3ce7e2f593e609` |
| Build timestamp | `2026-08-05T17:25:04Z` |
| Build command | `npm ci && npm run build && npm run security:artifact` |

## UAT protected inputs required

Store values only in the approved secret manager. Do not commit or paste them into reports.

```text
VSHIELD_UAT_FRONTEND_URL
VSHIELD_UAT_API_URL
VSHIELD_UAT_SIGNALR_URL
VSHIELD_UAT_ENVIRONMENT
VSHIELD_UAT_TENANT
VSHIELD_UAT_SITE

VSHIELD_UAT_ADMIN_USERNAME
VSHIELD_UAT_ADMIN_PASSWORD
VSHIELD_UAT_GUARD_USERNAME
VSHIELD_UAT_GUARD_PASSWORD
VSHIELD_UAT_RECEPTIONIST_USERNAME
VSHIELD_UAT_RECEPTIONIST_PASSWORD
VSHIELD_UAT_MANAGER_USERNAME
VSHIELD_UAT_MANAGER_PASSWORD
VSHIELD_UAT_HR_USERNAME
VSHIELD_UAT_HR_PASSWORD
```

Supported MFA modes are TOTP, manual approval, UAT test API and protected storage state. Never record a seed, OTP, test API key, secret endpoint or storage-state content here.

## Inputs IT must provide

- UAT frontend, API, SignalR and health URLs.
- Tenant/site and five approved role accounts.
- Approved MFA mechanism.
- Deployed RC digest and previous artifact digest.
- Deploy permission and cache/CDN procedure.
- Permission to perform SignalR restart/disruption testing.
- Named rollback owner.

## Inputs Business and QA must provide

- Mutation manifest and forbidden targets.
- Seed data, expected audit records and cleanup policy.
- Role/permission confirmation.
- Business acceptance criteria.

## Execution

```bash
npm run test:uat
```

Missing protected input must exit 1 before any mutation. The harness must not request credentials outside the approved channel, print secrets or disable artifact scanning.

## Required completion evidence

- UAT authentication and five-role matrix results.
- Mutation, audit and cleanup results.
- SignalR disruption result.
- Performance p50/p75/p95.
- Deployment smoke and rollback rehearsal results.
- IT, QA and Business sign-off from the named approvers.
