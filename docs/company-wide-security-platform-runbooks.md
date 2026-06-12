# V-Shield 2.0 - Company-Wide Security Platform Runbooks

This runbook covers the operational flows added for the company-wide security platform renovation. It intentionally avoids protected runtime and public-domain scripts.

## 1. Admin Runbook

### Production Configuration Gate

1. Open Enterprise Security Command.
2. Review Configuration health.
3. Production must not run with:
   - repository-backed JWT secret,
   - default seed admin credentials,
   - localhost CORS origins,
   - repository-backed public domain values without environment override,
   - missing evidence export signing key,
   - memory-only rate limiting for medium/large rollout.
4. Set deploy-time secrets through environment variables:
   - `VSHIELD_JWT_SECRET`
   - `VSHIELD_SEED_ADMIN_USERNAME`
   - `VSHIELD_SEED_ADMIN_PASSWORD`
   - `VSHIELD_EVIDENCE_EXPORT_SIGNING_KEY`
   - `ConnectionStrings__DefaultConnection`
   - `AppSettings__FrontendUrl`
   - `AppSettings__AllowedOrigins__0`

### Step-Up MFA

1. Start a privileged action session from the Enterprise page or call `/api/Auth/step-up/start`.
2. Verify with password and MFA/recovery code through `/api/Auth/step-up/verify`.
3. Use the returned session ID in `X-Step-Up-Session-Id`.
4. Required for user administration, emergency policy, device configuration, evidence export/purge/redaction/legal-hold release, site hierarchy backfill, and release approval.

### Identity And HR Lifecycle

1. Configure identity provider metadata.
2. Import users/groups through enterprise identity import endpoints.
3. For termination/suspension, run offboarding with step-up.
4. Confirm revocation proof:
   - user disabled,
   - token version incremented,
   - refresh tokens revoked,
   - direct access rules disabled,
   - lifecycle event written.

## 2. Foundation And Asset Mapping

1. Verify no protected path changes.
2. Run default-site backfill only after step-up.
3. Confirm the report includes mapped gates, camera devices, employees, vehicles, log snapshots, and event snapshots.
4. Review asset map:
   - every legacy gate should map to a lane,
   - every legacy camera should map to a camera security device,
   - every vehicle should have a site,
   - access logs should retain site/gate/camera snapshots.

## 3. Access Policy Runbook

### Version Lifecycle

1. Create policy version in Draft.
2. Add rules to the version.
3. Submit for approval.
4. Approve with step-up.
5. Activate with step-up.
6. Previous active versions retire automatically.

### Decision Testing

1. Use `/api/enterprise/access-policy/simulate` before activating risky rules.
2. Simulation must not write an enforced decision.
3. Use `/api/enterprise/access-policy/shadow-compare` to compare legacy gate behavior with policy-engine behavior.
4. Shadow mismatch creates a security event and correlation item.

### Precedence

The evaluator applies:

1. emergency deny,
2. holiday block,
3. explicit deny,
4. temporary grant,
5. allow rule,
6. default deny.

## 4. Reception And Gate Operations

### Visitor

1. Create visit.
2. Screen watchlist.
3. Collect required form acceptance before check-in.
4. Check in with ID verification.
5. Issue visitor credential within approved window.
6. Monitor overstay through operations worker.
7. Check out; credentials must not remain usable beyond the visit window.

### Barrier

1. Barrier commands require reason and step-up.
2. Manual open/hold-open/lock-closed creates barrier audit and security event.
3. Low-confidence or watchlist plate flows must be reviewed instead of silently accepted.

## 5. Device And Offline Resilience

1. Register virtual controller in simulator.
2. Publish offline policy package with step-up.
3. Run offline scan simulator.
4. Inject tamper/offline/relay/barrier/camera faults.
5. Confirm SOC alarm and device health snapshot are created.
6. Runtime health is observed through API wrappers only; do not edit protected runtime folders.

## 6. SOC And Incident Command

1. Create or receive alarm.
2. Acknowledge and assign owner.
3. Start SOP execution.
4. Required SOP steps must be completed before closing SOP.
5. Create incident and timeline.
6. Dispatch guard task when needed.
7. Close dispatch with result.
8. Close alarm with note.
9. Close incident only with outcome note.
10. Shift handover must include unresolved alarms/incidents.

## 7. Evidence And Compliance

1. Register evidence with hash and privacy label.
2. Verify hash before export.
3. Request export with purpose and recipient.
4. Approve export with step-up.
5. Export approval blocks if evidence is purged or hash mismatch exists.
6. Export hash and HMAC signature reference are generated when no external signature is supplied.
7. Legal hold prevents purge.
8. Retention purge requires dry-run/review and step-up.

## 8. Operations, Backup, Release

1. Outbox worker creates webhook deliveries with HMAC signatures.
2. Webhook delivery result and dispatch status must be recorded.
3. Record SIEM export through outbox.
4. Record backup run and restore drill with RPO/RTO evidence.
5. Record security checks for secrets and vulnerability gates.
6. Release candidate approval requires:
   - at least one required gate,
   - each required gate passed,
   - evidence reference on each required gate,
   - step-up session.

## 9. Migration Rollback

1. Migrations are additive in this renovation pass.
2. Before production rollout, back up DB.
3. Apply migrations on disposable copy.
4. Run smoke tests.
5. Rollback rehearsal must prove down migrations can remove new columns/tables without touching protected runtime/public-domain paths.

## 10. QA Evidence

Required before production claim:

- API automated tests pass.
- Frontend build pass.
- EF pending model check is clean.
- No-touch protected-path check is empty.
- Browser E2E for Admin/SOC/Reception/Gate/Auditor/Emergency paths.
- Load/stress/soak/chaos runs with documented profile and result.
- Hardware simulator and real hardware acceptance for controller/reader/barrier/camera failure modes.
- Backup/restore drill with measured RPO/RTO.
- Security scan and container/dependency vulnerability gate.
