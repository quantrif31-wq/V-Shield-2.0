# Access policy comparison

V-Shield currently has two independently active access-policy systems:

- Legacy `EmployeeAccessPermission` matches an employee and gate and uses the
  single `IsAllowed` value. Missing rows deny the current QR flow. Duplicate
  rows are reported as indeterminate by the comparison adapter.
- Enterprise `AccessRule` evaluates employee, access point, active policy
  version, credential type, effective UTC window and `AccessSchedule`.
  Scheduled explicit deny rules precede temporary grants and allow rules.

Neither system is declared canonical by this feature. A
`FaceAccessPolicyComparison` contains the two independent results and one
descriptive comparison status. `AgreeAllow` is not an access decision and must
never be used to operate a gate or record attendance.

Physical mapping follows committed database relationships:

`FaceRecognitionEvent → FaceCameraConfiguration → Lane → Gate + AccessPoint`.

Missing or ambiguous relationships are reported and never repaired
automatically. Face recognition currently has no canonical enterprise
credential mapping, so the background processor records
`EnterpriseMissingCredentialContext` instead of inventing a credential.

The processor uses `FaceRecognitionEvent.OccurredAtUtc`. Enterprise schedules
are evaluated in the configured `Asia/Ho_Chi_Minh` timezone. Each engine input
has a deterministic SHA-256 fingerprint containing only policy identifiers and
values used by that evaluation.

Comparison records are immutable and idempotent per recognition event. The
read-only API provides history, summary and processor health. There are no
mutation, approval, override, gate-command or attendance endpoints.

The existing QR flow remains unchanged. Reports are intended to help the
business select a future canonical policy and precedence. Physical RTSP camera
verification remains pending, and Face Model revoke remains unpublished.
# Credential-aware evaluation foundation

The enterprise evaluator now also accepts a canonical `AccessCredentialContext`. This
path validates employee ownership and effective lifecycle before applying the existing
policy algorithm. The legacy type-only path and the Face comparison processor remain
unchanged: Face events continue to produce `EnterpriseMissingCredentialContext` until an
explicit Face credential binding exists. Existing comparison rows are never re-evaluated.

The comparison-to-recognition-event foreign key uses `NO ACTION`. Comparison
snapshots are historical and cannot be cascade-deleted with recognition events.
The Docker development migration chain is applied and the normal API workers
run without the former Testing fallback. No comparison fixture was introduced.
## Face biometric credential context

Với event `Matched`, enterprise comparison resolve binding đã phê duyệt tại
`OccurredAtUtc`, rồi đánh giá credential `FaceBiometric` tại cùng thời điểm.
Thiếu/revoked binding hoặc credential không hiệu lực trả reason code explicit.
Rule precedence, schedule, legacy evaluator và comparison đã lưu không thay đổi.
Processor không tạo `AccessDecision`.
