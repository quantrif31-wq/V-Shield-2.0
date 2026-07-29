# Face credential bindings

`AccessCredential` xác định enterprise credential thuộc một nhân viên.
`EmployeeFaceCredentialBinding` là phê duyệt riêng, có audit, cho phép kết quả
nhận diện khuôn mặt của nhân viên sử dụng credential `FaceBiometric` đó làm
enterprise policy context.

Binding nằm ở cấp `Employee`, không gắn với một phiên bản model. Thay model
enrollment không tự tạo credential hoặc binding mới. Cùng `EmployeeId`, tên
file model, hay việc chỉ có một candidate không bao giờ được dùng để suy luận
binding.

## Lifecycle và effective time

Binding có trạng thái `Pending`, `Active`, hoặc `Revoked`. Mỗi nhân viên và mỗi
credential chỉ có tối đa một binding `Active`. Credential phải thuộc đúng nhân
viên, có type `FaceBiometric`, và effective `Active` khi activate.

Policy comparison resolve binding theo `FaceRecognitionEvent.OccurredAtUtc`:

```text
ActivatedAtUtc <= OccurredAtUtc
and (RevokedAtUtc is null or OccurredAtUtc < RevokedAtUtc)
```

Credential lifecycle cũng được đánh giá tại chính thời điểm event. Binding tạo
sau event không áp dụng hồi tố; revoke không làm thay đổi comparison cũ.

Tất cả FK của binding dùng `NO ACTION`; rowversion bảo vệ mutation đồng thời.
Không có hard delete. Revoke binding không revoke credential hoặc model.

## Administration and approval

API đọc yêu cầu quyền `identity-mgmt`; create/revoke yêu cầu quyền manage.
Response chỉ trả masked identifier và không trả identifier hash, secret, model
path/checksum, ảnh, video hoặc encoding.

CLI tạo template:

```text
dotnet run --project API/API/API -- face-credentials generate-binding-template
```

Template được ghi vào
`runtime/face-data/manifests/face-credential-bindings.json`, mặc định chưa phê
duyệt và không điền credential ID. Validation là dry-run. Apply cần đồng thời
`approved=true`, người/thời điểm phê duyệt UTC, `--apply` và
`--confirm-bindings`. Hệ thống không tự chọn candidate và không tự bind.

Binding không phải permission. Commit này không tạo/sửa rule, permission,
`AccessDecision`, không mở cổng, không ghi chấm công và không chọn policy
canonical. Camera RTSP vật lý vẫn chưa được xác nhận.

## Audit action identifiers

`SystemAuditLogs.ActionType` uses the canonical limit of 64 characters
(`nvarchar(64) NOT NULL`). Action identifiers are never shortened or silently
truncated. Binding creation and its audit records remain in the same database
transaction, so an audit failure rolls back the complete manifest apply.

The development manifest approved on `2026-07-28T09:43:15Z` created five active
bindings for Employee/Credential mappings `1→1` through `5→5`. A subsequent
dry-run validation classified all five mappings as `AlreadyBound`; binding
timestamps, row versions, and `FaceCredentialBindingCreated` audit counts did
not change. Historical comparisons were not re-evaluated, no `AccessDecision`
was created, and no gate or attendance command was issued. Physical RTSP camera
verification remains pending.

Future binding creation persists the binding first to obtain its SQL identity,
then constructs the business audit with the real binding ID and explicitly
resolved actor, and only then commits the shared transaction. An audit failure
therefore rolls back the binding as well.

Five legacy creation audits that predated this ordering were not updated or
deleted. They are linked to append-only
`FaceCredentialBindingAuditReconciled` records. Each correction references the
original audit ID and authoritative binding ID, preserves `Phạm Ngọc Hoài Anh`
as the original business approver, and records User ID 1 (`admin`, `Phạm Văn
Thành`) only as the reconciliation execution actor. Re-running reconciliation
is a dry-run and reports all five records as `AlreadyReconciled`.

The Docker development frontend is served by Nginx on container port 80 and is
published as `http://localhost:5173/`; both the internal and configured host
endpoints return HTTP 200. No access decision, gate command, or attendance
record is produced by binding or audit reconciliation. Physical RTSP camera
verification remains pending.
