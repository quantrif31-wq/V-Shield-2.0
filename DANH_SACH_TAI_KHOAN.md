# Danh sách tài khoản V-Shield

Nguồn: cơ sở dữ liệu Docker local, trích xuất lúc 03/09/2026 và đã đối chiếu trực tiếp với VPS. Hai bên cùng có 30 tài khoản dưới đây, với cùng tên đăng nhập, vai trò, trạng thái hoạt động và cấu hình MFA.

| ID | Tên đăng nhập | Mật khẩu mặc định | Họ tên | Vai trò | MFA |
| --- | --- | --- | --- | --- | --- |
| 1 | `admin` | `AdminLocal@2026` (local và VPS) | Phạm Văn Thành | Quản trị viên | Bật |
| 2 | `manager` | `Manager@123` | Hà Mạnh Hùng | Quản lý | Tắt |
| 3–5 | `quanly2`–`quanly4` | `Manager@123` | Theo danh sách | Quản lý | Tắt |
| 6–21 | `baove1`–`baove16` | `BaoVe@123` | Theo danh sách | Bảo vệ | `baove1`: bật; còn lại: tắt |
| 22–23 | `letan1`–`letan2` | `LeTan@123` | Theo danh sách | Lễ tân | Tắt |
| 24–25 | `nhansu1`–`nhansu2` | `HR@123` | Theo danh sách | Nhân sự | Tắt |
| 26–30 | `nhanvien1`–`nhanvien5` | `Staff@123` | Theo danh sách | Nhân viên | Tắt |

## Lưu ý bảo mật

- Docker local và VPS **không dùng chung một database vật lý**: mỗi bên vẫn độc lập để hoạt động khi mất mạng. VPS là máy chủ trung tâm; các thay đổi tài khoản được đồng bộ giữa hai bên khi có Internet.
- `admin` dùng cùng mật khẩu ở local và VPS: `AdminLocal@2026`. VPS đọc mật khẩu từ biến bí mật riêng nhưng API sẽ đồng bộ hash theo giá trị này mỗi khi khởi động. Cơ sở dữ liệu chỉ lưu giá trị băm, nên không thể khôi phục mật khẩu do người dùng từng tự đổi.
- Các mật khẩu nhóm còn lại là mật khẩu mẫu do dữ liệu demo đặt. Nếu đã được quản trị viên đổi thủ công, giá trị hiện tại chỉ có thể được thay bằng thao tác đặt lại mật khẩu, không thể đọc ngược.
- Hai tài khoản đang bật xác thực đa yếu tố (MFA): `admin` và `baove1`; cần có mã MFA để đăng nhập ngoài mật khẩu.
- Hãy đổi toàn bộ mật khẩu mẫu trước khi vận hành thực tế và không gửi file này ra ngoài phạm vi quản trị.
