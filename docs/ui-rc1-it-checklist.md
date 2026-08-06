# V-Shield 2.0 UI RC1 — IT checklist

- [ ] Verify the delivered artifact SHA-256 against the handoff.
- [ ] Deploy exactly the immutable RC artifact and record the deployment digest.
- [ ] Configure frontend, API and SignalR URLs.
- [ ] Configure five role accounts in the approved secret manager.
- [ ] Configure the approved MFA mechanism.
- [ ] Confirm UAT tenant and site.
- [ ] Obtain the approved mutation manifest and forbidden targets.
- [ ] Run UAT preflight; a missing protected input must fail closed.
- [ ] Run the full UAT suite.
- [ ] Perform controlled SignalR disruption testing.
- [ ] Run performance sampling for approved network profiles.
- [ ] Run deployment smoke.
- [ ] Roll back to the previous immutable artifact.
- [ ] Smoke the rolled-back version.
- [ ] Redeploy the RC artifact and smoke it again.
- [ ] Record the rehearsal timestamps, operators, digests and evidence.
- [ ] Provide named IT sign-off through the approved channel.

Do not place credentials, MFA seeds, OTPs, tokens or storage state in this checklist.
