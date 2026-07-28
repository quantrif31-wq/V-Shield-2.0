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
