# UI RC1 role/permission matrix

This matrix records the frontend route contract. “Task” means the account must also have the configured operational task assignment where the route declares `taskKey`. UAT must prove both UI visibility/direct-route behavior and a backend 403; hiding a button is not authorization.

| Module / action family | Admin | Bảo vệ | Lễ tân | Quản lý | Nhân sự | Backend proof required |
|---|---:|---:|---:|---:|---:|---|
| Employees: view/create/edit/delete/import/export/Face ID | Task: employee-directory | Deny | Deny | Deny | Task: employee-directory | CRUD, bulk/import/export/upload and PII |
| Vehicles: view/create/edit/delete/import/export | Task: parking | Task: parking | Deny | Deny | Deny | CRUD, bulk/import/export |
| Access Logs: view/detail/export/evidence | Task: access-logs | Task: access-logs | Deny | Task: access-logs | Deny | Detail/evidence/export |
| Visitors: view/detail/create link/approve/reject/QR | Task: guest-support | Deny unless assigned task by policy | Task: guest-support | Deny unless assigned task | Deny | PII, approval and QR download |
| Device Management: camera/gate configuration | Task: device-mgmt | Deny | Deny | Deny | Deny | Every create/edit/delete/config endpoint |
| Watchlist: view sensitive data/add/review | Task: monitoring | Task: monitoring | Deny | Deny | Deny | Sensitive match/visitor evidence and decisions |
| AI Review: view metrics/review | Task: monitoring | Task: monitoring | Deny | Deny | Deny | Review mutation and metrics |
| Redaction: approve/perform/verify | Task: evidence-mgmt | Deny | Deny | Deny | Deny | Every state transition and evidence reference |
| Operations Dashboard: view/backups | Task: dashboard | Deny | Deny | Task: dashboard | Deny | KPI/config/backup action |
| Chat/realtime | Allow | Allow | Allow | Allow | Allow | Hub authorization and message scope |

For each allow/deny cell, record: menu, direct URL, primary/secondary action, row/bulk action, import/export, create/edit/delete, approve/reject, sensitive evidence/PII and device configuration. Attach only sanitized request IDs/correlation IDs—not tokens, MFA, faces, QR secrets or evidence.

Automated protected UAT covers the five real accounts and representative direct routes/API denials. QA must expand the run with the approved `UAT_API_CASES_JSON` manifest to cover every mutation above. Any mismatch between this table and backend policy is a release blocker until Product/Security explicitly resolves the contract.
