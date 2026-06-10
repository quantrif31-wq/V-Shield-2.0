# V-Shield 2.0 - Company-Wide Security Platform Runbooks

Date: 2026-06-10

## Admin Runbook

Daily:

1. Review `api/enterprise/operations/overview` for failed outbox events, webhook deliveries and degraded dependencies.
2. Review `api/enterprise/soc/overview` for open critical alarms and old alarm age.
3. Review evidence export and redaction queues.
4. Confirm release-readiness gates remain current before deploying any build.

User and access lifecycle:

1. Create or link users through enterprise foundation identity mapping.
2. Set employee lifecycle state.
3. Assign site and manager.
4. Grant access through access levels/groups/rules, not one-off bypasses.
5. On termination or suspension, use lifecycle transition so app user sessions and refresh tokens are revoked.
6. Record recertification decisions periodically.

## Security Operator Runbook

Alarm handling:

1. Open SOC overview.
2. Acknowledge the alarm.
3. Assign owner or guard.
4. Start the matching SOP.
5. Create or update incident case.
6. Add timeline notes, video bookmarks and evidence items.
7. Dispatch guard when physical verification is needed.
8. Close alarm and incident only after outcome is recorded.
9. Include unresolved items in shift handover.

Emergency:

1. Set scoped emergency state in access policy.
2. Record alarm and incident.
3. Dispatch guards.
4. Capture muster snapshot.
5. Keep evidence under legal hold if an investigation is likely.

## Reception And Visitor Desk Runbook

1. Create visit with host, expected time and required forms.
2. Screen visitor against watchlist.
3. Issue credential only for approved time and area.
4. Require NDA/safety form acceptance when configured.
5. Record ID verification metadata.
6. Check visitor in.
7. Check visitor out and confirm pass cannot be reused.
8. Escalate watchlist or overstay events to SOC.

## Vehicle And Gate Runbook

1. Confirm parking permit and lane policy.
2. Review lane event and plate/credential context.
3. Use barrier command only with reason.
4. Record manual overrides as lane events.
5. Escalate watchlist matches to SOC.

## Device Enrollment Runbook

1. Create security device record.
2. Register controller/reader/relay/sensor topology.
3. Record provisioning request.
4. Publish configuration version and offline policy package.
5. Monitor device health snapshots.
6. Keep connector work at API/service boundary; do not edit `AI_Runtime/**` or `runtime/**`.

## Evidence Governance Runbook

1. Register evidence item with storage reference and hash.
2. Add chain-of-custody entry for every transfer.
3. Log read/export purpose.
4. Apply retention policy.
5. Apply legal hold before investigation export or deletion risk.
6. Request export with recipient and purpose.
7. Admin approves export and records watermark/signature reference.
8. Use redaction workflow for privacy-sensitive disclosure.
9. Generate compliance report for audit requests.

## Backup And Restore Runbook

1. Start backup run for target profile.
2. Complete backup with reference, size and verification state.
3. Start restore drill from verified backup.
4. Record measured RPO/RTO and findings.
5. Release gate passes only if measured values meet target.

## Deployment Runbook

1. Confirm no-touch areas are clean.
2. Run API tests.
3. Run frontend build.
4. Review migrations and rollback impact.
5. Confirm security checks: secret rotation, dependency scan, public endpoint inventory.
6. Record QA test runs and release gate checks.
7. Approve release candidate only after required gates pass.
