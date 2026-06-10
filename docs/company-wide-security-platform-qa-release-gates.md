# V-Shield 2.0 - QA And Release Gates

Date: 2026-06-10

## Required Gates

Every release candidate must record these gates in `api/enterprise/release-readiness`:

| Gate | Required evidence |
|---|---|
| API tests | `dotnet test API\API\API\API.sln --no-restore --verbosity minimal` passes. |
| Frontend build | `npm run build` passes in `View`. |
| No-touch verification | No changes under AI runtime, runtime and public-domain setup files. |
| Migration reviewed | New migrations are additive or have documented rollback impact. |
| Runbooks updated | Operator/admin/deployment/evidence/backup guides reflect new workflows. |
| Security checks | Secret rotation, dependency scan and public endpoint review recorded. |
| Load/stress/soak/chaos evidence | Test run record exists for target deployment profile. |

## Test Matrix

| Area | Minimum coverage |
|---|---|
| Unit/API integration | Auth, authorization, lifecycle, policy evaluation, visitor/vehicle, SOC, evidence, operations and release-readiness workflows. |
| E2E browser | Login, Admin enterprise workflow, guard alarm workflow, receptionist visitor flow, evidence review. |
| Migration | Fresh database migration, forward migration from current main, rollback rehearsal on disposable backup. |
| Policy engine | Scheduled access, holiday denial, temporary grant, emergency lockdown, anti-passback reset and occupancy update. |
| Hardware simulation | Controller offline, reader tamper, relay stuck, barrier command failure, camera/runtime unavailable. |
| Runtime degradation | AI/runtime wrapper unavailable, degraded health visible, no no-touch file edits required. |
| Evidence governance | Access purpose, legal hold, export approval, chain of custody, redaction and compliance report. |
| Alarm workflow | Alarm creation, ack, assignment, SOP, dispatch, incident close, shift handover and muster snapshot. |

## Load Profiles

| Profile | Users/credentials | Physical assets | Required scenario |
|---|---:|---:|---|
| Pilot | 500 users | 10 gates, 50 cameras | Normal workday plus visitor rush. |
| Medium company | 5,000 users | 50 gates, 200 cameras | Login storm, access burst, alarm burst, evidence export. |
| Large company | 50,000 credentials | 200 gates, 1,000 cameras | Multi-site event bursts, queue backlog recovery, runtime outage and DB failover simulation. |

## Migration Checklist

1. Confirm backup completed and verified.
2. Apply migration to staging copy.
3. Run smoke tests for auth, health, access policy, visitor, SOC and evidence.
4. Apply production migration during approved window.
5. Run post-migration smoke tests.
6. Record gate evidence in release-readiness.

Rollback note:

- `AddEnterpriseSecurityPlatform` and `AddReleaseReadiness` create new enterprise tables and add lifecycle columns to `Employee`.
- Rolling back those migrations drops the new enterprise/release-readiness tables and lifecycle columns. Rollback must only run after exporting or intentionally discarding those new records.

## Release Decision

A release candidate can be approved only when every required gate is `Passed`. The API enforces this through:

- `POST /api/enterprise/release-readiness/release-candidates`
- `POST /api/enterprise/release-readiness/release-candidates/{id}/gate-checks`
- `PATCH /api/enterprise/release-readiness/release-candidates/{id}/approve`
