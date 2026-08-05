# V-Shield UI RC2 — repository administrator request

The current release operator has push permission but no repository admin
permission. GitHub returned `403 Must have admin rights to Repository` when the
operator attempted to create the environment. An administrator must complete
the actions below. No secret value belongs in this document, chat, issue or
workflow log.

## Action required

Create the GitHub Environment:

```text
v-shield-uat
```

## Environment protection

- Add the IT/Security-approved required reviewers and prevent self-review.
- Allow only approved `release/**` branches and immutable release tags.
- Do not expose environment secrets to untrusted pull requests or forks.
- Require deployment approval before any mutation-enabled UAT job.
- Retain GitHub audit history for environment, variable and secret changes.
- Keep sanitized UAT reports for 14 days; block report upload when the
  sensitive-artifact scan fails.
- Confirm the deployment target is isolated from production tenants, sites,
  databases, cameras, gates, watchlists, notifications, backups and external
  integrations.

## Environment secrets

Create these names using GitHub Environment Secrets or the approved external
secret manager. Values must never be printed:

```text
VSHIELD_UAT_FRONTEND_URL
VSHIELD_UAT_API_URL
VSHIELD_UAT_API_HEALTH_URL
VSHIELD_UAT_SIGNALR_URL
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

VSHIELD_UAT_MUTATION_MANIFEST_JSON
VSHIELD_UAT_ROLE_MATRIX_JSON
VSHIELD_UAT_PERFORMANCE_PLAN_JSON
```

Choose exactly one approved MFA mechanism. For the current TOTP workflow,
create:

```text
VSHIELD_UAT_ADMIN_TOTP_SECRET
VSHIELD_UAT_GUARD_TOTP_SECRET
VSHIELD_UAT_RECEPTIONIST_TOTP_SECRET
VSHIELD_UAT_MANAGER_TOTP_SECRET
VSHIELD_UAT_HR_TOTP_SECRET
```

If IT selects manual MFA, a UAT-only test API, or protected storage state,
configure only the corresponding names documented in
`docs/ui-rc1-uat-runbook.md` and update the protected workflow before use. Do
not add a production MFA bypass.

## Environment variables

Configure non-secret GitHub Environment Variables:

```text
VSHIELD_UAT_EXPECTED_VERSION=2.0.0-rc2
VSHIELD_UAT_FRONTEND_SHA256
VSHIELD_UAT_RC_ARTIFACT_SHA256
VSHIELD_UAT_PREVIOUS_ARTIFACT_SHA256
```

The current preflight harness consumes the compatibility names below. Until a
separately reviewed workflow migration removes the aliases, set them to the
same immutable SHA-256 values:

```text
VSHIELD_UAT_RC_ARTIFACT_DIGEST
VSHIELD_UAT_PREVIOUS_ARTIFACT_DIGEST
```

The workflow supplies these policy values itself:

```text
VSHIELD_UAT_ENVIRONMENT=UAT
VSHIELD_UAT_MFA_MODE=totp
VSHIELD_UAT_ALLOW_NEGATIVE_MFA=true
VSHIELD_UAT_ALLOW_MUTATIONS=true
VSHIELD_UAT_CLEANUP_POLICY=required
```

Do not enable mutation until IT, QA and Business have approved the mutation
manifest, forbidden targets, seed records, cleanup owners and rollback methods.

## Deployment and rollback inputs

The administrator/IT owner must record:

```text
Frontend UAT URL:
API UAT URL:
SignalR UAT URL:
API health endpoint:
Deployment target:
Deployment pipeline/job:
Current deployed artifact digest:
Previous stable artifact digest:
RC2 artifact digest:
Cache/CDN invalidation procedure:
Monitoring/logging location:
Rollback owner:
Maintenance window:
Smoke command:
```

The previous artifact must remain retrievable by immutable digest.

## Admin verification

Leave every item pending until verified by the named administrator:

```text
Environment created: PENDING
Required reviewers: PENDING
Allowed branches/tags: PENDING
Secrets configured: PENDING
Variables configured: PENDING
Deployment target: PENDING
Previous artifact digest: PENDING
Rollback owner: PENDING
Approval date/time: PENDING
Evidence reference: PENDING
```

Protected UAT must remain blocked until all items are complete and
`npm run test:uat` reports preflight PASS without starting mutation.
