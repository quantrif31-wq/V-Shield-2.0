# V-Shield 2.0 UI RC1 — Functional parity audit

Audit date: 2026-08-05. Baseline: pre-migration page implementations in repository history versus current shared-component implementations. This is a source-level audit; every item involving persisted data or authorization remains **Needs backend verification** until protected UAT passes.

Status vocabulary: **Preserved**, **Improved**, **Intentionally removed**, **Missing**, **Needs backend verification**. No feature was classified as intentionally removed in this RC.

| Module | Preserved | Improved | Regression found/fixed | Needs backend verification |
|---|---|---|---|---|
| Employees | Name/phone/email/department/position/status/Face ID; search/status filter; pagination; create/edit/delete; import/export; face upload; modal and row actions | Shared fields/table/dialog/toast, protected face image loading, validation and dirty-form guard | URL-mode lacked an explicit clear action; restored **Xóa URL ảnh** | List/detail/create/update/delete, import/export/upload, conflict, role/task permission and persisted Face ID removal |
| Visitors / Pre-registration | Guest identity/contact/host/visit/status fields; status/date/search filters; pagination; detail; approve/reject; create link; QR download | Clearer status/evidence presentation, confirmation and retained form error state | No source-level missing feature found | Backend search semantics, link/QR expiry, approve/reject conflicts, sensitive PII rules and LeTan task assignment |
| Vehicles | Plate/type/owner/status data; search/type filter; pagination; create/edit/delete; import/export; owner selection | Normalization/validation, datalist selection, destructive confirmation | No source-level missing feature found | CRUD/conflict/import/export, duplicate plate, BaoVe parking-task permission |
| Access Logs | Direction/gate/result/date/query filters; page/page size; summary and pagination | Detail/evidence preview, export flow, explicit permission/error/empty states | No source-level missing feature found | List/detail/filter/export, evidence authorization, large-result timeout and 401/403/404/500 recovery |
| Device Management | Camera and gate overview; camera/gate create/edit/delete; separate pagination | Unified summary, retry/permission state, shared modal/table | No source-level missing feature found | CRUD, device conflict, runtime linkage, Admin device-mgmt task and concurrent edit |
| Watchlist Queue | Match/entry tabs; status/severity/search filters; pagination; add entry; review match; visitor detail | Query-backed tab/filter/page state, dirty-form guard, consistent decision dialog | No source-level missing feature found | Match ordering/deduplication, review conflict, sensitive visitor evidence, BaoVe monitoring task |
| AI Review Queue | Pending/reviewed/outcome filters; adjudication review; metrics and summary | Explicit dual loading/error states, query-backed filters, retained review data on error | No source-level missing feature found | Review conflict/idempotency, metric accuracy, ordering and BaoVe permission |
| Redaction Queue | Status filter; approve and perform workflow | Verify step, permission/error states, required storage reference retained after failure | No source-level missing feature found | State-machine conflicts, evidence privacy controls, audit record and Admin evidence-mgmt task |
| Operations Dashboard | Overview/KPIs, config health, trends and backup operations | Consistent loading/error states and enterprise information hierarchy | No source-level missing feature found | KPI correctness, backup operation/timeout, QuanLy dashboard task and backend degradation behavior |

## Cross-cutting parity

| Capability | Result | Evidence / follow-up |
|---|---|---|
| Deep-link before authentication | **Regression fixed** | Login now returns to a safe internal `redirect` path instead of always `/`; protected UAT test covers query preservation. |
| Query parameters | **Preserved/Improved** | Access Logs, Watchlist, AI Review and Redaction retain or normalize relevant filters in route state. Backend filter semantics need UAT. |
| Keyboard behavior | **Preserved/Improved** | Native form controls/shared components and modal focus behavior remain covered by accessibility tests; real MFA paste is in protected UAT. |
| Notifications/errors | **Improved** | Shared toast, inline retry, permission-denied, empty and retained-input states replace ad-hoc failure handling on migrated pages. |
| Realtime | **Improved** | Single connection promise, deduplicated subscriptions, dynamic reconnect token, cleanup, Live/Reconnecting/Stale/Disconnected and last-updated state. Real server restart/sleep/wake needs UAT. |
| Removed features | **None** | No low-use legacy function was intentionally removed by this hardening pass. |

## Release blockers from this audit

- No source-level blocker remains after restoring deep-link return and Face URL clearing.
- Real API mutations, authorization and business-rule parity are not proven locally because no UAT credentials/approved mutation manifest were supplied.
- Business owner must confirm field labels, evidence visibility, Watchlist decisions and Redaction state transitions against operating procedures.
