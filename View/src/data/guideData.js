/**
 * guideData.js — Nội dung hướng dẫn sử dụng V-Shield
 * 100% tiếng Việt, step-by-step, giải thích đơn giản cho người ngoài ngành
 */

export const pageData = [
  // ====================================================================
  // NHÓM 1: TỔNG QUAN & GIÁM SÁT
  // ====================================================================
  {
    path: '/dashboard',
    label: 'Dashboard - Trang tổng quan',
    icon: '🏠',
    roles: ['Admin', 'Bảo vệ', 'Quản lý'],
    group: 'Tổng quan & Giám sát',
    groupIcon: '📊',
    mucDich: 'Đây là trang đầu tiên bạn thấy sau khi đăng nhập. Nó cho bạn biết tình hình chung: hôm nay có bao nhiêu xe ra vào, bao nhiêu khách đã đăng ký, có ngoại lệ nào cần xử lý không, và các hoạt động gần đây nhất.',
    steps: [
      {
        title: 'Xem các chỉ số tổng quan',
        moTa: 'Ở đầu trang, các ô số liệu lớn cho bạn biết: số xe đang trong bãi, số khách đã hẹn hôm nay, số ngoại lệ cần xử lý, số nhân viên đã chấm công.',
        nhapGi: null, bamGi: null,
        ketQua: 'Bạn nắm được tình hình chung ngay lập tức.'
      },
      {
        title: 'Xem biểu đồ và AI phân tích',
        moTa: 'Phần giữa trang có biểu đồ cột (lượng xe ra vào theo ngày) và phần "AI Intelligence" - máy tự động nhận xét tình hình an ninh.',
        nhapGi: null, bamGi: null,
        ketQua: 'Bạn biết xu hướng: hôm nào đông, có bất thường gì không.'
      },
      {
        title: 'Xem hoạt động mới nhất',
        moTa: 'Cuối trang là bảng liệt kê các sự kiện gần đây: ai ra vào, qua cổng nào, kết quả thế nào.',
        nhapGi: null, bamGi: 'Bấm vào một dòng để xem chi tiết.',
        ketQua: 'Cửa sổ nhỏ hiện ra với thông tin đầy đủ.'
      }
    ],
    thanhPhan: [
      { ten: 'Các ô số liệu (metric)', yNghia: 'Hiển thị số quan trọng nhất trong ngày', ghiChu: '4-6 ô, mỗi ô màu khác nhau' },
      { ten: 'Biểu đồ cột traffic', yNghia: 'Hiển thị lượt ra vào theo ngày', ghiChu: 'Rê chuột vào cột để xem số' },
      { ten: 'Bảng hoạt động mới nhất', yNghia: 'Các sự kiện gần đây', ghiChu: 'Mỗi dòng: thời gian, người, cổng, kết quả' }
    ]
  },
  {
    path: '/monitoring',
    label: 'Giám sát trực tiếp - Xem camera',
    icon: '📹',
    roles: ['Admin', 'Bảo vệ', 'Quản lý'],
    group: 'Tổng quan & Giám sát',
    groupIcon: '📊',
    mucDich: 'Trang này cho xem hình ảnh từ camera trực tiếp. Bạn có thể xem tối đa 4 camera cùng lúc. Thường bảo vệ dùng để theo dõi ra vào.',
    steps: [
      {
        title: 'Nhập địa chỉ camera',
        moTa: 'Ở phía trên có ô nhập "Camera URL". Gõ địa chỉ camera (thường dạng: rtsp://địa.chỉ.ip:port/...) vào ô này.',
        nhapGi: 'Nhập URL camera vào ô "Camera URL"', bamGi: null,
        ketQua: 'Địa chỉ camera được điền vào.'
      },
      {
        title: 'Bật xem camera',
        moTa: 'Bấm nút "Bật" (có biểu tượng play ▶) để bắt đầu xem.',
        nhapGi: null, bamGi: 'Bấm nút "Bật" (biểu tượng ▶)',
        ketQua: 'Camera hiển thị hình ảnh trực tiếp. Nếu không thấy, kiểm tra lại URL.'
      },
      {
        title: 'Xem nhiều camera',
        moTa: 'Thêm URL camera thứ 2, 3, 4. Màn hình tự động chia thành lưới 2x2.',
        nhapGi: 'Nhập thêm URL các camera khác', bamGi: null,
        ketQua: 'Màn hình chia 4 phần, mỗi phần là một camera.'
      }
    ],
    thanhPhan: [
      { ten: 'Ô "Camera URL"', yNghia: 'Nhập địa chỉ camera', ghiChu: 'Hỏi Admin nếu không biết địa chỉ' },
      { ten: 'Nút Bật/Tắt', yNghia: 'Bấm để bắt đầu/dừng xem camera', ghiChu: null },
      { ten: 'Khung hình camera', yNghia: 'Hiển thị hình ảnh từ camera', ghiChu: 'Tối đa 4 khung' }
    ]
  },
  {
    path: '/access-logs',
    label: 'Tra cứu vào/ra - Lịch sử ra vào',
    icon: '📋',
    roles: ['Admin', 'Bảo vệ', 'Quản lý'],
    group: 'Tổng quan & Giám sát',
    groupIcon: '📊',
    mucDich: 'Như một cuốn sổ ghi chép điện tử. Ghi lại tất cả lần ra vào: ai vào lúc nào, qua cổng nào, bằng phương thức gì (QR, khuôn mặt, biển số xe) và kết quả thế nào.',
    steps: [
      {
        title: 'Nhập từ khóa tìm kiếm',
        moTa: 'Gõ tên người, biển số xe, hoặc thông tin cần tìm vào ô "Từ khóa".',
        nhapGi: 'Nhập từ khóa', bamGi: null,
        ketQua: 'Hệ thống tự động lọc danh sách.'
      },
      {
        title: 'Chọn điều kiện lọc',
        moTa: 'Chọn chiều (Vào/Ra), chọn cổng, chọn khoảng ngày.',
        nhapGi: 'Chọn ngày ở ô "Từ ngày" và "Đến ngày"', bamGi: null,
        ketQua: 'Các điều kiện lọc đã được chọn.'
      },
      {
        title: 'Áp dụng bộ lọc',
        moTa: 'Bấm nút "Áp dụng lọc" để xem kết quả. Muốn xóa hết lọc thì bấm "Đặt lại".',
        nhapGi: null, bamGi: 'Bấm "Áp dụng lọc"',
        ketQua: 'Bảng cập nhật, chỉ hiển thị bản ghi phù hợp.'
      }
    ],
    thanhPhan: [
      { ten: 'Ô "Từ khóa"', yNghia: 'Tìm kiếm nhanh', ghiChu: 'Có thể gõ một phần từ' },
      { ten: 'Chọn chiều/ cổng', yNghia: 'Lọc theo hướng vào/ra hoặc cổng', ghiChu: null },
      { ten: 'Ô "Từ ngày" - "Đến ngày"', yNghia: 'Chọn khoảng thời gian', ghiChu: 'Bấm vào ô để chọn ngày' },
      { ten: 'Bảng danh sách', yNghia: 'Kết quả tra cứu', ghiChu: 'Thời gian, người, cổng, biển số, kết quả' }
    ]
  },
  {
    path: '/employees',
    label: 'Quản lý nhân viên',
    icon: '👥',
    roles: ['Admin'],
    group: 'Tổng quan & Giám sát',
    groupIcon: '📊',
    mucDich: 'Dùng để quản lý danh sách nhân viên: thêm mới, sửa thông tin, xóa, hoặc nhập từ file Excel. Mỗi nhân viên có tài khoản để đăng nhập hệ thống.',
    steps: [
      {
        title: 'Xem danh sách', moTa: 'Bảng hiển thị tất cả nhân viên: ảnh, họ tên, SĐT, email, phòng ban, chức vụ, trạng thái.',
        nhapGi: null, bamGi: null,
        ketQua: 'Thấy toàn bộ nhân viên.'
      },
      {
        title: 'Thêm nhân viên mới', moTa: 'Bấm "Thêm nhân viên" → cửa sổ hiện ra → điền thông tin → bấm "Lưu".',
        nhapGi: 'Điền họ tên, SĐT, Email, phòng ban, chức vụ', bamGi: 'Bấm "Thêm nhân viên" → "Lưu"',
        ketQua: 'Nhân viên mới được thêm vào danh sách.'
      },
      {
        title: 'Import từ Excel', moTa: 'Có thể import danh sách từ file Excel/CSV hàng loạt.',
        nhapGi: null, bamGi: 'Bấm "Import" → chọn file → "Mở"',
        ketQua: 'Tất cả nhân viên trong file được thêm vào.'
      }
    ],
    thanhPhan: [
      { ten: 'Nút "Thêm nhân viên"', yNghia: 'Thêm người mới', ghiChu: 'Góc phải, màu xanh' },
      { ten: 'Nút Import/Export', yNghia: 'Nhập/xuất dữ liệu từ file', ghiChu: null },
      { ten: 'Bảng danh sách', yNghia: 'Hiển thị nhân viên', ghiChu: 'Có thể sửa/xóa từng dòng' }
    ]
  },
  {
    path: '/settings',
    label: 'Cài đặt hệ thống',
    icon: '⚙️',
    roles: ['Admin'],
    group: 'Tổng quan & Giám sát',
    groupIcon: '📊',
    mucDich: 'Dành cho Admin để cấu hình hệ thống: thông tin công ty, camera, AI, cảnh báo.',
    steps: [
      {
        title: 'Cài đặt chung', moTa: 'Sửa tên công ty, giờ mở/đóng cổng, ngôn ngữ, múi giờ.',
        nhapGi: 'Sửa thông tin trong các ô', bamGi: 'Bấm "Lưu"',
        ketQua: 'Thông tin được cập nhật.'
      },
      {
        title: 'Thêm camera', moTa: 'Tab "Mạng lưới Camera" → "Thêm camera" → nhập tên + URL.',
        nhapGi: 'Nhập tên và URL camera', bamGi: 'Bấm "Lưu"',
        ketQua: 'Camera mới có thể dùng ở trang Giám sát.'
      },
      {
        title: 'Cấu hình AI', moTa: 'Tab "Hệ thống AI": bật/tắt Face ID, Anti-spoofing, LPR, điều chỉnh độ nhạy.',
        nhapGi: 'Chỉnh các nút gạt on/off', bamGi: 'Bấm "Lưu cài đặt AI"',
        ketQua: 'Thay đổi được áp dụng ngay.'
      }
    ],
    thanhPhan: [
      { ten: '4 tab: Chung/Camera/AI/Cảnh báo', yNghia: 'Chuyển giữa các nhóm cài đặt', ghiChu: null },
      { ten: 'Nút "Lưu"', yNghia: 'Lưu thay đổi', ghiChu: 'Nhớ lưu trước khi chuyển tab!' }
    ]
  },
  {
    path: '/login',
    label: 'Đăng nhập',
    icon: '🔑',
    roles: ['Tất cả'],
    group: 'Tổng quan & Giám sát',
    groupIcon: '📊',
    mucDich: 'Trang đầu tiên khi truy cập hệ thống. Cần đăng nhập để sử dụng V-Shield.',
    steps: [
      {
        title: 'Nhập tên đăng nhập', moTa: 'Gõ tên tài khoản được Admin cấp vào ô "Tên đăng nhập".',
        nhapGi: 'Gõ tên đăng nhập', bamGi: null,
        ketQua: 'Tên hiện trong ô.'
      },
      {
        title: 'Nhập mật khẩu', moTa: 'Gõ mật khẩu. Các dấu ● hiện ra để bảo mật.',
        nhapGi: 'Gõ mật khẩu', bamGi: null,
        ketQua: 'Mật khẩu đã nhập.'
      },
      {
        title: 'Bấm Đăng nhập', moTa: 'Bấm nút "Đăng nhập" màu xanh.',
        nhapGi: null, bamGi: 'Bấm "Đăng nhập"',
        ketQua: 'Nếu đúng: vào trang chính. Sai: thông báo lỗi, thử lại.'
      },
      {
        title: 'MFA (nếu có)', moTa: 'Một số tài khoản yêu cầu mã xác thực từ điện thoại. Mở app Authenticator, lấy mã 6 số và nhập.',
        nhapGi: 'Nhập mã 6 số', bamGi: 'Bấm "Xác nhận"',
        ketQua: 'Đăng nhập thành công.'
      }
    ],
    thanhPhan: [
      { ten: 'Ô tên đăng nhập', yNghia: 'Nhập tài khoản', ghiChu: 'Liên hệ Admin nếu chưa có' },
      { ten: 'Ô mật khẩu', yNghia: 'Nhập mật khẩu', ghiChu: 'Hiển thị ● để bảo mật' },
      { ten: 'Nút "Đăng nhập SSO"', yNghia: 'Đăng nhập bằng tài khoản Office 365/Google', ghiChu: 'Chỉ xuất hiện nếu công ty cấu hình' }
    ]
  },

  // ====================================================================
  // NHÓM 2: SOC & ENTERPRISE
  // ====================================================================
  {
    path: '/soc-console',
    label: 'SOC Alarm Console - Xử lý cảnh báo an ninh',
    icon: '🚨',
    roles: ['Admin', 'Bảo vệ'],
    group: 'SOC & Enterprise',
    groupIcon: '🏢',
    mucDich: 'Trung tâm chỉ huy xử lý cảnh báo an ninh. Khi có sự cố (đột nhập, cổng bị phá...), hệ thống tạo "alarm". Bạn xem và xử lý tại đây.',
    steps: [
      {
        title: 'Xem tổng quan', moTa: '4 ô số liệu: số cảnh báo chưa xử lý, quy trình đang chạy, sự cố đang mở, cảnh báo lâu nhất.',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết tình hình an ninh hiện tại.'
      },
      {
        title: 'Xem danh sách Alarm', moTa: 'Bấm tab "Alarms". Mỗi dòng là một cảnh báo: mã, loại, mức độ (severity), tóm tắt.',
        nhapGi: null, bamGi: 'Bấm tab "Alarms"',
        ketQua: 'Danh sách cảnh báo hiện ra.'
      },
      {
        title: 'Xem chi tiết và xử lý', moTa: 'Bấm vào một cảnh báo → cửa sổ chi tiết hiện ra. Có các nút: "Acknowledge" (xác nhận), "Assign" (gán cho người xử lý), "Close" (đóng).',
        nhapGi: null, bamGi: 'Bấm "Acknowledge" → "Assign" (nhập ID) → "Close" (nhập lý do)',
        ketQua: 'Trạng thái cảnh báo thay đổi: New → Acknowledged → Assigned → Closed.'
      },
      {
        title: 'Tạo Incident', moTa: 'Nếu sự cố nghiêm trọng, bấm tab "Incidents" → "New Incident" → nhập tên + mức độ.',
        nhapGi: 'Nhập tên sự cố, chọn mức độ', bamGi: 'Bấm "Create"',
        ketQua: 'Sự cố mới được tạo.'
      }
    ],
    thanhPhan: [
      { ten: '4 ô số liệu đầu trang', yNghia: 'Tình hình tổng quan', ghiChu: null },
      { ten: 'Các tab: Alarms/Incidents/SOPs...', yNghia: 'Chuyển giữa danh sách', ghiChu: null },
      { ten: 'Danh sách Alarm', yNghia: 'Mỗi dòng là cảnh báo', ghiChu: 'Màu đỏ = nghiêm trọng' },
      { ten: 'Nút Acknowledge/Assign/Close', yNghia: 'Xử lý cảnh báo', ghiChu: 'Close phải nhập lý do' }
    ]
  },
  {
    path: '/ueba',
    label: 'UEBA - Phân tích hành vi bất thường',
    icon: '🔍',
    roles: ['Admin', 'Bảo vệ', 'Quản lý'],
    group: 'SOC & Enterprise',
    groupIcon: '🏢',
    mucDich: 'Phát hiện hành vi bất thường. Ví dụ: nhân viên thường vào lúc 8h sáng, bỗng vào lúc 2h đêm → hệ thống cảnh báo.',
    steps: [
      {
        title: 'Xem tổng quan', moTa: '4 số liệu: bất thường chưa xử lý, đã xử lý hôm nay, rủi ro cao, tổng hồ sơ.',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết tình hình chung.'
      },
      {
        title: 'Xem bất thường và xử lý', moTa: 'Bấm tab "Bất thường". Mỗi dòng: loại, mức độ, mô tả. Có nút "Xử lý" hoặc "FP" (báo nhầm).',
        nhapGi: null, bamGi: 'Bấm "Xử lý" hoặc "FP"',
        ketQua: 'Bất thường được xử lý hoặc đánh dấu nhầm.'
      },
      {
        title: 'Xem AI đánh giá', moTa: 'AI tự động phân tích rủi ro nhân viên. Bạn có thể "Duyệt" hoặc "Từ chối".',
        nhapGi: null, bamGi: 'Bấm "Duyệt" hoặc "Từ chối"',
        ketQua: 'Đánh giá AI được xác nhận hoặc từ chối.'
      }
    ],
    thanhPhan: [
      { ten: 'Tab Tổng quan/Profile/Bất thường', yNghia: 'Chuyển giữa các danh sách', ghiChu: null },
      { ten: 'Điểm rủi ro (Risk Score)', yNghia: '0-100, càng cao càng rủi ro', ghiChu: '>60 là cao' },
      { ten: 'Nút "Xử lý"', yNghia: 'Đánh dấu đã xử lý', ghiChu: null },
      { ten: 'Nút "FP"', yNghia: 'Báo là hệ thống nhầm', ghiChu: 'FP = False Positive' }
    ]
  },
  {
    path: '/policy-engine',
    label: 'Policy Engine - Máy tạo chính sách ra vào',
    icon: '📜',
    roles: ['Admin'],
    group: 'SOC & Enterprise',
    groupIcon: '🏢',
    mucDich: 'Tạo "bộ luật" cho hệ thống: ai được vào, vào bằng cách nào, vào lúc nào. VD: "Nhân viên vào cổng sau bằng QR" hoặc "Cấm vào kho sau 6h".',
    steps: [
      {
        title: 'Tạo phiên bản chính sách', moTa: 'Tab "Policy Versions" → "New Version" → nhập tên + mô tả → "Create".',
        nhapGi: 'Nhập tên, mô tả', bamGi: 'Bấm "New Version" → "Create"',
        ketQua: 'Phiên bản mới (trạng thái Draft - bản nháp).'
      },
      {
        title: 'Thêm quy tắc', moTa: 'Tab "Rules" → "Add Rule". Đặt: ai, được phép hay không, loại giấy tờ, ở đâu.',
        nhapGi: 'Chọn đối tượng, loại giấy tờ, phạm vi', bamGi: 'Bấm "Create"',
        ketQua: 'Quy tắc được thêm.'
      },
      {
        title: 'Kích hoạt', moTa: 'Bấm "Submit" → "Approve" → "Activate" để chính sách có hiệu lực.',
        nhapGi: null, bamGi: 'Bấm "Submit" → "Approve" → "Activate"',
        ketQua: 'Chính sách áp dụng thực tế tại cổng.'
      },
      {
        title: 'Mô phỏng thử', moTa: 'Tab "Simulator": nhập thông tin giả định, bấm "Simulate" xem Allow/Deny.',
        nhapGi: 'Nhập ID người, cổng, loại giấy tờ', bamGi: 'Bấm "Simulate"',
        ketQua: 'Hệ thống trả lời Allow (cho phép) hoặc Deny (từ chối).'
      },
      {
        title: 'Xử lý khẩn cấp', moTa: 'Tab "Emergency" → "New Emergency" → chọn FullLockdown/PartialLockdown → nhập lý do → "Activate".',
        nhapGi: 'Nhập lý do', bamGi: 'Bấm "Activate"',
        ketQua: 'Cảnh báo đỏ "LOCKDOWN ACTIVE" xuất hiện trên đầu trang.'
      }
    ],
    thanhPhan: [
      { ten: 'Tab Policy Versions', yNghia: 'Quản lý phiên bản', ghiChu: 'Draft → Submit → Approve → Activate' },
      { ten: 'Tab Simulator', yNghia: 'Mô phỏng thử trước', ghiChu: 'Rất hữu ích để kiểm tra' },
      { ten: 'Tab Emergency', yNghia: 'Xử lý khẩn cấp', ghiChu: 'Cảnh báo đỏ khi active' }
    ]
  },
  {
    path: '/enterprise-security',
    label: 'Enterprise Console - Điều khiển trung tâm',
    icon: '🏢',
    roles: ['Admin', 'Bảo vệ'],
    group: 'SOC & Enterprise',
    groupIcon: '🏢',
    mucDich: 'Bảng điều khiển doanh nghiệp tích hợp nhiều workspace: SOC, Reception, Gate, Auditor. Cũng có AI phân tích, truy vấn ngôn ngữ tự nhiên, và xác thực bước-up (Step-up) cho các hành động nhạy cảm.',
    steps: [
      {
        title: 'Chọn workspace', moTa: 'Bấm vào các nút workspace: Admin, SOC, Reception, Gate, Auditor, Ops để chuyển chế độ.',
        nhapGi: null, bamGi: 'Bấm vào tên workspace',
        ketQua: 'Giao diện chuyển sang chế độ tương ứng.'
      },
      {
        title: 'Xác thực bước-up', moTa: 'Để làm hành động nhạy cảm, cần xác thực thêm: nhập mật khẩu + mã MFA. Bấm "Verify".',
        nhapGi: 'Nhập mật khẩu, mã MFA', bamGi: 'Bấm "Verify"',
        ketQua: 'Phiên làm việc được nâng quyền.'
      },
      {
        title: 'Hỏi AI bằng tiếng Việt', moTa: 'Phần "Natural Language Query": gõ câu hỏi bằng tiếng Việt (VD: "Ai vào sau 22h 7 ngày qua?") và bấm "Truy vấn".',
        nhapGi: 'Gõ câu hỏi tiếng Việt', bamGi: 'Bấm "Truy vấn"',
        ketQua: 'AI trả lời kết quả.'
      }
    ],
    thanhPhan: [
      { ten: 'Workspace tabs', yNghia: 'Chuyển chế độ làm việc', ghiChu: '6 chế độ: Admin, SOC, Reception...' },
      { ten: 'Step-up verification', yNghia: 'Xác thực thêm cho hành động nhạy cảm', ghiChu: 'Cần mật khẩu + MFA' },
      { ten: 'Natural Language Query', yNghia: 'Hỏi AI bằng ngôn ngữ tự nhiên', ghiChu: 'Có thể hỏi tiếng Việt' }
    ]
  },
  {
    path: '/identity-management',
    label: 'Identity Management - Quản lý danh tính',
    icon: '🪪',
    roles: ['Admin', 'Bảo vệ'],
    group: 'SOC & Enterprise',
    groupIcon: '🏢',
    mucDich: 'Quản lý danh tính người dùng từ các hệ thống bên ngoài (Office 365, Google, LDAP). Import người dùng hàng loạt và vô hiệu hóa tài khoản khi nhân viên nghỉ việc.',
    steps: [
      {
        title: 'Thêm Identity Provider', moTa: 'Tab "Identity Providers" → "Add Provider". Chọn OIDC/SAML/LDAP, nhập tên, authority, client ID/secret.',
        nhapGi: 'Nhập thông tin provider', bamGi: 'Bấm "Add Provider" → "Create"',
        ketQua: 'Provider mới được thêm.'
      },
      {
        title: 'Import người dùng', moTa: 'Tab "Import Users / Groups". Dán danh sách user (mỗi dòng: subject,username,name,email,role) và bấm "Import Users".',
        nhapGi: 'Dán danh sách user vào ô text', bamGi: 'Bấm "Import Users"',
        ketQua: 'Người dùng được import vào hệ thống.'
      },
      {
        title: 'Offboard nhân viên', moTa: 'Bấm "Offboard Employee". Nhập ID nhân viên và lý do. Bấm "Confirm Offboard".',
        nhapGi: 'Nhập ID nhân viên, lý do', bamGi: 'Bấm "Confirm Offboard"',
        ketQua: 'Tài khoản bị vô hiệu hóa, không thể đăng nhập.'
      }
    ],
    thanhPhan: [
      { ten: 'Tab Identity Providers', yNghia: 'Thêm/sửa provider SSO', ghiChu: 'Hỗ trợ OIDC, SAML, LDAP' },
      { ten: 'Tab Import Users', yNghia: 'Import người dùng hàng loạt', ghiChu: 'Format CSV: subject,username,name,email,role' },
      { ten: 'Nút Offboard', yNghia: 'Vô hiệu hóa tài khoản nhân viên nghỉ việc', ghiChu: 'Có thể hủy nếu cần' }
    ]
  },
  {
    path: '/site-hierarchy',
    label: 'Site Hierarchy - Cây phân cấp công ty',
    icon: '🏗️',
    roles: ['Admin', 'Bảo vệ'],
    group: 'SOC & Enterprise',
    groupIcon: '🏢',
    mucDich: 'Quản lý cấu trúc công ty dạng cây: Công ty → Địa điểm → Tòa nhà → Tầng → Vùng → Cổng. Cũng có thể backfill (tự động tạo) dữ liệu từ legacy.',
    steps: [
      {
        title: 'Xem cây phân cấp', moTa: 'Tab "Hierarchy Tree". Bấm vào các node để mở rộng/xem chi tiết.',
        nhapGi: null, bamGi: 'Bấm vào node trên cây',
        ketQua: 'Thông tin chi tiết của node hiện ra bên phải.'
      },
      {
        title: 'Thêm node con', moTa: 'Chọn node cha → bấm "Add Child" → chọn loại (Site/Building/Floor/Zone) → nhập tên → "Create".',
        nhapGi: 'Nhập tên, code', bamGi: 'Bấm "Add Child" → "Create"',
        ketQua: 'Node mới được thêm vào cây.'
      },
      {
        title: 'Backfill dữ liệu', moTa: 'Tab "Backfill". Nhập company code, site code, bấm "Run Safe Backfill".',
        nhapGi: 'Nhập company code, site code', bamGi: 'Bấm "Run Safe Backfill"',
        ketQua: 'Hệ thống tự động tạo cấu trúc công ty và map dữ liệu.'
      }
    ],
    thanhPhan: [
      { ten: 'Cây phân cấp', yNghia: 'Hiển thị cấu trúc công ty', ghiChu: 'Company → Site → Building → Floor/Zone → Access Point' },
      { ten: 'Nút Add Child', yNghia: 'Thêm node con', ghiChu: null },
      { ten: 'Tab Backfill', yNghia: 'Tự động tạo dữ liệu từ legacy', ghiChu: 'Chạy 1 lần khi mới setup' }
    ]
  },

  // ====================================================================
  // NHÓM 3: QUẢN LÝ KHÁCH THĂM
  // ====================================================================
  {
    path: '/pre-registrations',
    label: 'Đăng ký trước - Duyệt đơn khách',
    icon: '📋',
    roles: ['Admin'],
    group: 'Quản lý Khách thăm',
    groupIcon: '👤',
    mucDich: 'Xem và duyệt đơn đăng ký của khách trước khi đến. Khi khách đăng ký qua link, đơn hiện ở đây để Admin xử lý.',
    steps: [
      {
        title: 'Xem thống kê', moTa: '4 ô đầu: Tổng đơn, Chờ duyệt (cam), Đã duyệt (xanh), Từ chối (đỏ).',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết có bao nhiêu đơn cần xử lý.'
      },
      {
        title: 'Lọc và duyệt đơn', moTa: 'Chọn trạng thái "Chờ duyệt". Bấm ✓ (xanh) để duyệt, ✕ (đỏ) để từ chối.',
        nhapGi: null, bamGi: 'Bấm ✓ hoặc ✕ ở cột Hành động',
        ketQua: 'Đơn chuyển trạng thái. Khách nhận thông báo.'
      },
      {
        title: 'Tạo link đăng ký mới', moTa: 'Bấm "Tạo link đăng ký" → chọn host → chọn thời gian hết hạn → "Khởi tạo Link". Copy link gửi khách.',
        nhapGi: 'Chọn host, giờ hết hạn', bamGi: 'Bấm "Khởi tạo Link"',
        ketQua: 'Link tạo thành công, có thể copy gửi cho khách.'
      }
    ],
    thanhPhan: [
      { ten: '4 ô thống kê', yNghia: 'Tổng/Chờ duyệt/Đã duyệt/Từ chối', ghiChu: null },
      { ten: 'Nút ✓ / ✕', yNghia: 'Duyệt hoặc từ chối đơn', ghiChu: 'Xanh = duyệt, Đỏ = từ chối' },
      { ten: 'Nút "Tạo link đăng ký"', yNghia: 'Tạo link mời khách', ghiChu: 'Góc phải trên' }
    ]
  },
  {
    path: '/reception',
    label: 'Reception - Lễ tân',
    icon: '🛎️',
    roles: ['Admin', 'Bảo vệ'],
    group: 'Quản lý Khách thăm',
    groupIcon: '👤',
    mucDich: 'Lễ tân hoặc bảo vệ sảnh. Check-in khách, walk-in, xem overstay, review watchlist.',
    steps: [
      {
        title: 'Check-in khách', moTa: 'Tab "Today Visits". Tìm khách → bấm "Check-in".',
        nhapGi: null, bamGi: 'Bấm "Check-in" trên dòng khách',
        ketQua: 'Trạng thái chuyển "Checked In".'
      },
      {
        title: 'Walk-in (khách vãng lai)', moTa: 'Bấm "Walk-in Check-in" → nhập tên, SĐT, host, giờ → "Check-in".',
        nhapGi: 'Nhập tên, SĐT, host, giờ đến/đi', bamGi: 'Bấm "Check-in"',
        ketQua: 'Khách vãng lai được check-in.'
      },
      {
        title: 'Xử lý overstay', moTa: 'Tab "Overstays" - danh sách khách quá giờ. Liên hệ nhắc họ check-out.',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết ai đang ở quá giờ.'
      }
    ],
    thanhPhan: [
      { ten: 'Nút Walk-in Check-in', yNghia: 'Check-in nhanh khách vãng lai', ghiChu: 'Màu xanh' },
      { ten: 'Tab Overstays', yNghia: 'Khách quá giờ', ghiChu: null },
      { ten: 'Tab Watchlist Matches', yNghia: 'Kết quả trùng khớp watchlist', ghiChu: null }
    ]
  },
  {
    path: '/host-visitor',
    label: 'Mời khách - Host Portal',
    icon: '✉️',
    roles: ['Admin', 'Nhân viên', 'Bảo vệ'],
    group: 'Quản lý Khách thăm',
    groupIcon: '👤',
    mucDich: 'Nhân viên mời khách đến công ty. Tạo lời mời, cấp QR cho khách.',
    steps: [
      {
        title: 'Tạo lời mời', moTa: 'Bấm "New Invitation". Nhập tên khách, SĐT, email, giờ đến/đi. Bấm "Send Invitation".',
        nhapGi: 'Nhập tên, SĐT, email, giờ', bamGi: 'Bấm "Send Invitation"',
        ketQua: 'Lời mời được gửi.'
      },
      {
        title: 'Cấp QR cho khách', moTa: 'Khi trạng thái "Approved", bấm "Issue QR" → chọn thời gian hiệu lực → "Issue".',
        nhapGi: 'Chọn thời gian hiệu lực', bamGi: 'Bấm "Issue QR" → "Issue"',
        ketQua: 'Khách có mã QR quét tại cổng.'
      }
    ],
    thanhPhan: [
      { ten: 'Nút New Invitation', yNghia: 'Tạo lời mời mới', ghiChu: 'Màu xanh' },
      { ten: 'Nút Issue QR', yNghia: 'Cấp mã QR cho khách', ghiChu: 'Chỉ khi trạng thái Approved' }
    ]
  },
  {
    path: '/registration-links',
    label: 'Link đăng ký tự động',
    icon: '🔗',
    roles: ['Admin'],
    group: 'Quản lý Khách thăm',
    groupIcon: '👤',
    mucDich: 'Tạo và quản lý các link mời khách đăng ký. Mỗi link có token riêng, thời gian hết hạn.',
    steps: [
      {
        title: 'Tạo link mới', moTa: 'Bấm "Tạo link mới". Chọn host (gõ tên để tìm), chọn thời hạn (12-72h). Bấm "Tạo link".',
        nhapGi: 'Tìm và chọn host, chọn giờ hết hạn', bamGi: 'Bấm "Tạo link mới" → "Tạo link"',
        ketQua: 'Link được tạo. Có thể copy gửi khách.'
      },
      {
        title: 'Quản lý link', moTa: 'Bảng danh sách link: host, token, trạng thái (còn hiệu lực/đã dùng/hết hạn).',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết link nào còn dùng được.'
      }
    ],
    thanhPhan: [
      { ten: 'Bảng danh sách link', yNghia: 'Host, token, trạng thái', ghiChu: 'Xanh = hiệu lực, Vàng = đã dùng, Đỏ = hết hạn' },
      { ten: 'Nút Copy link', yNghia: 'Sao chép URL để gửi khách', ghiChu: null }
    ]
  },
  {
    path: '/guest-profiles',
    label: 'Hồ sơ khách',
    icon: '📇',
    roles: ['Admin', 'Bảo vệ'],
    group: 'Quản lý Khách thăm',
    groupIcon: '👤',
    mucDich: 'Danh bạ khách quen. Lưu thông tin khách đã từng đến để tái sử dụng nhanh.',
    steps: [
      {
        title: 'Tìm khách', moTa: 'Gõ tên, CCCD hoặc ID vào ô tìm kiếm combobox. Chọn kết quả.',
        nhapGi: 'Gõ tên khách hoặc CCCD', bamGi: 'Chọn từ kết quả dropdown',
        ketQua: 'Khách được chọn.'
      },
      {
        title: 'Sửa/Xóa khách', moTa: 'Bấm "Sửa" để cập nhật thông tin. Bấm "Xóa" để xóa khách (có xác nhận).',
        nhapGi: null, bamGi: 'Bấm "Sửa" hoặc "Xóa"',
        ketQua: 'Thông tin được cập nhật hoặc xóa.'
      },
      {
        title: 'Xem lịch sử ra vào', moTa: 'Bấm "Lịch sử" để xem các lần ra vào của khách đó.',
        nhapGi: null, bamGi: 'Bấm "Lịch sử"',
        ketQua: 'Bảng lịch sử ra vào hiện ra.'
      }
    ],
    thanhPhan: [
      { ten: 'Combobox tìm khách', yNghia: 'Tìm nhanh theo tên/CCCD/ID', ghiChu: null },
      { ten: 'Nút Sửa/Xóa/Lịch sử', yNghia: 'Thao tác với khách', ghiChu: null }
    ]
  },
  {
    path: '/kiosk',
    label: 'Kiosk Check-in - Tự check-in',
    icon: '💻',
    roles: ['Admin', 'Bảo vệ'],
    group: 'Quản lý Khách thăm',
    groupIcon: '👤',
    mucDich: 'Màn hình cảm ứng đặt ở sảnh để khách tự check-in. Khách gõ tên hoặc SĐT, xác nhận, và tự check-in.',
    steps: [
      {
        title: 'Tìm kiếm (Lookup)', moTa: 'Bước 1: Gõ tên hoặc SĐT vào ô tìm. Chọn tên mình từ kết quả.',
        nhapGi: 'Gõ tên hoặc SĐT', bamGi: 'Bấm vào tên mình',
        ketQua: 'Chuyển sang bước xác nhận.'
      },
      {
        title: 'Xác nhận (Confirm)', moTa: 'Bước 2: Kiểm tra thông tin. Nếu có NDA thì tích vào ô chấp nhận. Nhập loại giấy tờ (nếu cần). Bấm "Check in".',
        nhapGi: 'Tích NDA (nếu có), nhập giấy tờ', bamGi: 'Bấm "Check in"',
        ketQua: 'Check-in thành công! Màn hình chúc mừng.'
      }
    ],
    thanhPhan: [
      { ten: 'Ô tìm kiếm', yNghia: 'Gõ tên/SĐT', ghiChu: 'Bước 1' },
      { ten: 'Nút Check in', yNghia: 'Xác nhận check-in', ghiChu: 'Bước 2' }
    ]
  },
  {
    path: '/watchlist',
    label: 'Watchlist - Danh sách theo dõi',
    icon: '👁️',
    roles: ['Admin', 'Bảo vệ'],
    group: 'Quản lý Khách thăm',
    groupIcon: '👤',
    mucDich: 'Danh sách người/xe cần theo dõi đặc biệt. Khi có người trùng khớp, hệ thống báo để Bảo vệ/Admin xử lý.',
    steps: [
      {
        title: 'Thêm người vào watchlist', moTa: 'Bấm "Add Entry". Nhập tên, loại (Person/Vehicle), identifier (SĐT/biển số), mức độ (Low/Medium/High/Critical), lý do.',
        nhapGi: 'Nhập tên, loại, identifier, mức độ, lý do', bamGi: 'Bấm "Add Entry" → "Add"',
        ketQua: 'Entry được thêm vào danh sách theo dõi.'
      },
      {
        title: 'Review kết quả trùng khớp', moTa: 'Tab "Matches". Khi có trùng khớp, bấm "Review". Chọn: Confirmed (xác nhận), FalsePositive (nhầm), Escalate (báo cáo), Closed (đóng).',
        nhapGi: 'Chọn quyết định + nhập ghi chú', bamGi: 'Bấm "Review" → "Submit Review"',
        ketQua: 'Match được xử lý.'
      }
    ],
    thanhPhan: [
      { ten: 'Tab Matches', yNghia: 'Kết quả trùng khớp', ghiChu: null },
      { ten: 'Tab Entries', yNghia: 'Danh sách theo dõi', ghiChu: null },
      { ten: 'Nút Review', yNghia: 'Xem và xử lý kết quả', ghiChu: null }
    ]
  },

  // ====================================================================
  // NHÓM 4: AI & THIẾT BỊ
  // ====================================================================
  {
    path: '/face-id-security',
    label: 'Face ID - Nhận diện khuôn mặt',
    icon: '😀',
    roles: ['Admin', 'Bảo vệ'],
    group: 'AI & Thiết bị',
    groupIcon: '🤖',
    mucDich: 'Nhận diện khuôn mặt realtime. Camera sẽ quét khuôn mặt người đến, so sánh với cơ sở dữ liệu và cho phép/từ chối ra vào.',
    steps: [
      {
        title: 'Nhập địa chỉ camera', moTa: 'Nhập URL camera vào ô "Camera URL".',
        nhapGi: 'Nhập URL camera', bamGi: null,
        ketQua: 'Địa chỉ đã nhập.'
      },
      {
        title: 'Bật preview', moTa: 'Bấm "Bật preview" để xem trước hình ảnh từ camera.',
        nhapGi: null, bamGi: 'Bấm "Bật preview"',
        ketQua: 'Hình ảnh camera hiện ra.'
      },
      {
        title: 'Bắt đầu nhận diện', moTa: 'Bấm "Khởi tạo phiên" để bắt đầu nhận diện khuôn mặt.',
        nhapGi: null, bamGi: 'Bấm "Khởi tạo phiên"',
        ketQua: 'Hệ thống bắt đầu quét và nhận diện. Kết quả hiện ra: tên, ID, độ chính xác.'
      },
      {
        title: 'Kết thúc', moTa: 'Bấm "Tắt camera" để kết thúc.',
        nhapGi: null, bamGi: 'Bấm "Tắt camera"',
        ketQua: 'Phiên nhận diện kết thúc.'
      }
    ],
    thanhPhan: [
      { ten: 'Ô "Camera URL"', yNghia: 'Nhập địa chỉ camera', ghiChu: null },
      { ten: 'Nút "Bật preview"', yNghia: 'Xem trước camera', ghiChu: null },
      { ten: 'Nút "Khởi tạo phiên"', yNghia: 'Bắt đầu nhận diện', ghiChu: null },
      { ten: 'Kết quả nhận diện', yNghia: 'Tên, ID, độ chính xác', ghiChu: null }
    ]
  },
  {
    path: '/license-plate-security',
    label: 'Nhận diện biển số xe (ANPR)',
    icon: '🚗',
    roles: ['Admin', 'Bảo vệ'],
    group: 'AI & Thiết bị',
    groupIcon: '🤖',
    mucDich: 'Nhận diện biển số xe tự động. Camera chụp biển số, máy tính đọc số và kiểm tra trong danh sách xe đã đăng ký.',
    steps: [
      {
        title: 'Kết nối camera', moTa: 'Nhập URL camera, bấm "Kết nối camera".',
        nhapGi: 'Nhập URL camera', bamGi: 'Bấm "Kết nối camera"',
        ketQua: 'Camera kết nối, hiển thị hình ảnh.'
      },
      {
        title: 'Nhận diện biển số', moTa: 'Bấm "Chụp và nhận dạng". Hệ thống chụp ảnh, OCR đọc biển số.',
        nhapGi: null, bamGi: 'Bấm "Chụp và nhận dạng"',
        ketQua: 'Biển số được nhận diện, hiển thị trên màn hình kèm độ chính xác.'
      }
    ],
    thanhPhan: [
      { ten: 'Nút "Kết nối camera"', yNghia: 'Kết nối camera stream', ghiChu: null },
      { ten: 'Nút "Chụp và nhận dạng"', yNghia: 'Chụp ảnh và đọc biển số', ghiChu: null },
      { ten: 'Kết quả ANPR', yNghia: 'Biển số, ảnh chụp, độ chính xác', ghiChu: null }
    ]
  },
  {
    path: '/dynamic-qr-generator',
    label: 'Tạo QR động',
    icon: '📱',
    roles: ['Admin', 'Nhân viên', 'Bảo vệ'],
    group: 'AI & Thiết bị',
    groupIcon: '🤖',
    mucDich: 'Tạo mã QR thay đổi liên tục (động) để quét tại cổng. Mỗi mã QR chỉ có thời gian sống ngắn, tăng tính bảo mật.',
    steps: [
      {
        title: 'Nhân viên: QR tự động', moTa: 'Nếu bạn là nhân viên, sau khi đăng nhập, QR của bạn sẽ tự động hiện ra. Chỉ cần giữ màn hình trước camera quét QR tại cổng.',
        nhapGi: null, bamGi: null,
        ketQua: 'Mã QR hiển thị, tự động làm mới. Có countdown thời gian sống.'
      },
      {
        title: 'Admin: cấp QR cho người khác', moTa: 'Nhập Employee ID của người cần cấp, bấm "Phát QR realtime".',
        nhapGi: 'Nhập Employee ID', bamGi: 'Bấm "Phát QR realtime"',
        ketQua: 'QR của người đó hiện ra. Có thể tạm dừng hoặc làm mới thủ công.'
      }
    ],
    thanhPhan: [
      { ten: 'Mã QR', yNghia: 'Mã quét để vào cổng', ghiChu: 'Tự động làm mới theo chu kỳ' },
      { ten: 'Nút "Phát QR realtime"', yNghia: 'Bắt đầu tạo QR', ghiChu: null },
      { ten: 'Countdown', yNghia: 'Thời gian còn lại trước khi QR đổi', ghiChu: null }
    ]
  },
  {
    path: '/gate-transit-monitor',
    label: 'Gate Transit - Điều phối thông hành',
    icon: '🚪',
    roles: ['Admin', 'Bảo vệ'],
    group: 'AI & Thiết bị',
    groupIcon: '🤖',
    mucDich: 'Màn hình tổng hợp tại cổng: hiển thị đồng thời Face ID + Biển số + QR. Bảo vệ xem tất cả thông tin xác thực trên một màn hình duy nhất.',
    steps: [
      {
        title: 'Chọn làn/cổng', moTa: 'Chọn làn muốn theo dõi từ ô chọn.',
        nhapGi: null, bamGi: 'Chọn làn từ danh sách',
        ketQua: 'Màn hình hiển thị thông tin của làn đó.'
      },
      {
        title: 'Xem thông tin xác thực', moTa: 'Màn hình hiển thị đồng thời: camera khuôn mặt, biển số xe, mã QR. Thông tin đối tượng (tên, biển số, trạng thái) hiện bên cạnh.',
        nhapGi: null, bamGi: null,
        ketQua: 'Nắm được toàn bộ thông tin xác thực của người/xe đang qua cổng.'
      }
    ],
    thanhPhan: [
      { ten: 'Chọn làn/cổng', yNghia: 'Chọn làn để theo dõi', ghiChu: null },
      { ten: 'Camera combo', yNghia: 'Face + Biển số + QR cùng lúc', ghiChu: null },
      { ten: 'Thông tin đối tượng', yNghia: 'Tên, biển số, trạng thái', ghiChu: null }
    ]
  },

  // ====================================================================
  // NHÓM 5: GIAO THÔNG & BÃI ĐỖ
  // ====================================================================
  {
    path: '/barrier-panel',
    label: 'Barrier Control - Điều khiển barrier',
    icon: '🚧',
    roles: ['Admin', 'Bảo vệ'],
    group: 'Giao thông & Bãi đỗ',
    groupIcon: '🚗',
    mucDich: 'Điều khiển barrier (thanh chắn) tại các làn. Có thể mở, đóng, khóa, hoặc xem lịch sử điều khiển.',
    steps: [
      {
        title: 'Chọn barrier', moTa: 'Tab "Barriers". Danh sách barrier: tên, làn, trạng thái.',
        nhapGi: null, bamGi: null,
        ketQua: 'Thấy danh sách barrier.'
      },
      {
        title: 'Điều khiển', moTa: 'Bấm "Open" (mở), "Close" (đóng), "Hold" (giữ mở), "Lock" (khóa). Một cửa sổ hiện ra yêu cầu nhập lý do (bắt buộc). Bấm "Send".',
        nhapGi: 'Nhập lý do', bamGi: 'Bấm "Open" → nhập lý do → "Send"',
        ketQua: 'Barrier thực hiện lệnh. Lịch sử được ghi lại.'
      },
      {
        title: 'Xem lịch sử', moTa: 'Bấm "History" để xem ai đã điều khiển, làm gì, lúc nào.',
        nhapGi: null, bamGi: 'Bấm "History"',
        ketQua: 'Bảng lịch sử hiện ra.'
      }
    ],
    thanhPhan: [
      { ten: 'Nút Open/Close/Hold/Lock', yNghia: 'Điều khiển barrier', ghiChu: 'Phải nhập lý do' },
      { ten: 'Nút Simulate', yNghia: 'Mô phỏng (không ảnh hưởng thật)', ghiChu: null },
      { ten: 'Nút History', yNghia: 'Xem lịch sử điều khiển', ghiChu: null },
      { ten: 'Tab Parking', yNghia: 'Quản lý vé đỗ xe', ghiChu: null }
    ]
  },
  // ====================================================================
  // NHÓM 6: EVIDENCE & COMPLIANCE
  // ====================================================================
  {
    path: '/evidence-repository',
    label: 'Evidence Repository - Kho vật chứng',
    icon: '📁',
    roles: ['Admin'],
    group: 'Evidence & Compliance',
    groupIcon: '📁',
    mucDich: 'Lưu trữ tất cả bằng chứng (ảnh, video, log) liên quan đến an ninh. Mỗi vật chứng có hash để đảm bảo không bị sửa đổi, và có lịch sử "chain of custody" (ai đã xem/sử dụng).',
    steps: [
      {
        title: 'Lọc danh sách', moTa: 'Dùng các ô chọn: loại evidence (Document/Image/Video/Log), mức độ bảo mật (Privacy Label), legal hold.',
        nhapGi: 'Chọn các điều kiện lọc', bamGi: null,
        ketQua: 'Danh sách các vật chứng phù hợp.'
      },
      {
        title: 'Xem chi tiết', moTa: 'Bấm "Detail". Cửa sổ hiện: loại, nguồn, hash, privacy label, và "Custody Timeline" - ai đã xem, khi nào.',
        nhapGi: null, bamGi: 'Bấm "Detail"',
        ketQua: 'Thông tin đầy đủ vật chứng và lịch sử custody.'
      }
    ],
    thanhPhan: [
      { ten: 'Các ô lọc', yNghia: 'Lọc theo loại, mức bảo mật, legal hold', ghiChu: null },
      { ten: 'Hash SHA256', yNghia: 'Dấu vân tay số, đảm bảo không bị sửa', ghiChu: null },
      { ten: 'Custody Timeline', yNghia: 'Ai đã xem/sử dụng vật chứng', ghiChu: 'Quan trọng cho kiểm toán' }
    ]
  },
  {
    path: '/export-approval-queue',
    label: 'Export Approval - Duyệt xuất evidence',
    icon: '📤',
    roles: ['Admin'],
    group: 'Evidence & Compliance',
    groupIcon: '📁',
    mucDich: 'Khi ai đó yêu cầu xuất vật chứng (evidence), yêu cầu sẽ hiện ở đây để Admin duyệt hoặc từ chối.',
    steps: [
      {
        title: 'Xem yêu cầu', moTa: 'Bảng danh sách: evidence ID, người yêu cầu, thời gian, lý do, trạng thái.',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết ai đang yêu cầu xuất gì.'
      },
      {
        title: 'Duyệt hoặc từ chối', moTa: 'Bấm "Approve" để cho phép xuất, "Reject" để từ chối.',
        nhapGi: null, bamGi: 'Bấm "Approve" hoặc "Reject"',
        ketQua: 'Yêu cầu được xử lý. Người yêu cầu nhận thông báo.'
      }
    ],
    thanhPhan: [
      { ten: 'Nút Approve/Reject', yNghia: 'Duyệt hoặc từ chối xuất evidence', ghiChu: null }
    ]
  },
  {
    path: '/redaction-queue',
    label: 'Redaction Queue - Xóa thông tin nhạy cảm',
    icon: '🖍️',
    roles: ['Admin'],
    group: 'Evidence & Compliance',
    groupIcon: '📁',
    mucDich: 'Khi cần che/xóa thông tin nhạy cảm (khuôn mặt, biển số, CCCD) trong evidence trước khi công bố. Ví dụ: che mặt người trong ảnh trước khi gửi cho cơ quan điều tra.',
    steps: [
      {
        title: 'Xem danh sách cần redact', moTa: 'Bảng: ID evidence, loại, trạng thái, người yêu cầu.',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết evidence nào cần xử lý.'
      },
      {
        title: 'Thực hiện redact', moTa: 'Bấm "Perform redaction" để che thông tin. Sau đó bấm "Verify" để kiểm tra kết quả.',
        nhapGi: null, bamGi: 'Bấm "Perform redaction" → "Verify"',
        ketQua: 'Thông tin nhạy cảm được che. Evidence sẵn sàng để công bố.'
      }
    ],
    thanhPhan: [
      { ten: 'Nút Perform redaction', yNghia: 'Thực hiện che thông tin', ghiChu: null },
      { ten: 'Nút Verify', yNghia: 'Kiểm tra kết quả redaction', ghiChu: null }
    ]
  },
  {
    path: '/retention-dashboard',
    label: 'Retention & Legal Hold - Lưu giữ & niêm phong',
    icon: '🔒',
    roles: ['Admin'],
    group: 'Evidence & Compliance',
    groupIcon: '📁',
    mucDich: 'Quản lý chính sách lưu giữ dữ liệu. Mỗi loại evidence có thời gian lưu khác nhau. Legal Hold: niêm phong evidence không được xóa khi có yêu cầu pháp lý.',
    steps: [
      {
        title: 'Xem chính sách lưu giữ', moTa: 'Bảng: loại evidence, thời gian lưu, số lượng, ngày xóa tiếp theo.',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết evidence nào sắp đến hạn xóa.'
      },
      {
        title: 'Quản lý Legal Hold', moTa: 'Danh sách legal hold đang hoạt động. Evidence trong legal hold sẽ không bị xóa dù hết hạn lưu.',
        nhapGi: null, bamGi: null,
        ketQua: 'Đảm bảo evidence quan trọng không bị xóa.'
      }
    ],
    thanhPhan: [
      { ten: 'Retention policies', yNghia: 'Chính sách lưu giữ', ghiChu: null },
      { ten: 'Legal holds', yNghia: 'Niêm phong pháp lý', ghiChu: 'Không cho xóa dù hết hạn' }
    ]
  },
  {
    path: '/compliance-reports',
    label: 'Compliance Reports - Báo cáo tuân thủ',
    icon: '📊',
    roles: ['Admin'],
    group: 'Evidence & Compliance',
    groupIcon: '📁',
    mucDich: 'Tạo và xem các báo cáo về tình hình tuân thủ quy định an ninh. Dùng cho kiểm toán nội bộ hoặc báo cáo cấp trên.',
    steps: [
      {
        title: 'Tạo báo cáo mới', moTa: 'Bấm "Generate report". Chọn loại báo cáo, hệ thống tự động tổng hợp.',
        nhapGi: null, bamGi: 'Bấm "Generate report"',
        ketQua: 'Báo cáo được tạo, hiển thị trong danh sách.'
      },
      {
        title: 'Tải báo cáo', moTa: 'Trong bảng danh sách, bấm tải xuống để lưu báo cáo về máy.',
        nhapGi: null, bamGi: 'Bấm vào báo cáo để tải',
        ketQua: 'Báo cáo được tải về máy.'
      }
    ],
    thanhPhan: [
      { ten: 'Bảng danh sách báo cáo', yNghia: 'Các báo cáo đã tạo', ghiChu: null }
    ]
  },

  // ====================================================================
  // NHÓM 7: VIDEO & AI REVIEW
  // ====================================================================
  {
    path: '/video-search',
    label: 'Video Search - Tìm kiếm video',
    icon: '🎬',
    roles: ['Admin', 'Bảo vệ'],
    group: 'Video & AI Review',
    groupIcon: '🎥',
    mucDich: 'Tìm kiếm và đánh dấu (bookmark) các đoạn video quan trọng. Tạo yêu cầu clip (clip request) để xuất video ra.',
    steps: [
      {
        title: 'Tạo Bookmark', moTa: 'Bấm "+ Bookmark". Nhập event ID, camera ID, thời gian bắt đầu/kết thúc, ghi chú. Bấm "Save".',
        nhapGi: 'Nhập event ID, camera, thời gian', bamGi: 'Bấm "+ Bookmark" → "Save"',
        ketQua: 'Bookmark được tạo. Có thể xem lại sau.'
      },
      {
        title: 'Yêu cầu xuất clip', moTa: 'Bấm "+ Request Clip". Nhập camera, thời gian, chọn retention category. Bấm "Request".',
        nhapGi: 'Nhập thông tin clip', bamGi: 'Bấm "+ Request Clip" → "Request"',
        ketQua: 'Yêu cầu clip được tạo, chờ duyệt.'
      }
    ],
    thanhPhan: [
      { ten: 'Nút + Bookmark', yNghia: 'Đánh dấu video quan trọng', ghiChu: null },
      { ten: 'Nút + Request Clip', yNghia: 'Yêu cầu xuất clip', ghiChu: null },
      { ten: 'Nút Approve/Export', yNghia: 'Duyệt và xuất clip', ghiChu: null }
    ]
  },

  // ====================================================================
  // NHÓM 8: THIẾT BỊ & HẠ TẦNG
  // ====================================================================
  {
    path: '/device-management',
    label: 'Quản lý Camera & Cổng',
    icon: '📡',
    roles: ['Admin', 'Bảo vệ'],
    group: 'Thiết bị & Hạ tầng',
    groupIcon: '🔧',
    mucDich: 'Quản lý tất cả thiết bị: camera, controller, cổng. Thêm mới, cấu hình, kiểm tra kết nối.',
    steps: [
      {
        title: 'Xem danh sách thiết bị', moTa: 'Bảng danh sách: tên, loại, trạng thái, tình trạng sức khỏe.',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết thiết bị nào đang hoạt động, thiết bị nào lỗi.'
      },
      {
        title: 'Thêm thiết bị mới', moTa: 'Bấm "Add Device". Nhập thông tin: tên, loại, địa chỉ IP. Bấm "Save".',
        nhapGi: 'Nhập tên, loại, địa chỉ', bamGi: 'Bấm "Add Device" → "Save"',
        ketQua: 'Thiết bị mới được thêm. Có thể dùng ngay.'
      }
    ],
    thanhPhan: [
      { ten: 'Bảng thiết bị', yNghia: 'Danh sách camera, controller', ghiChu: null },
      { ten: 'Nút Test connection', yNghia: 'Kiểm tra kết nối thiết bị', ghiChu: null }
    ]
  },
  {
    path: '/device-health',
    label: 'Device Health - Sức khỏe thiết bị',
    icon: '❤️',
    roles: ['Admin', 'Bảo vệ'],
    group: 'Thiết bị & Hạ tầng',
    groupIcon: '🔧',
    mucDich: 'Xem tình trạng sức khỏe của từng thiết bị: CPU, memory, uptime, kết nối. AI cũng chẩn đoán và dự đoán thiết bị nào sắp hỏng.',
    steps: [
      {
        title: 'Xem metrics sức khỏe', moTa: 'Mỗi thiết bị hiển thị: CPU, memory, thời gian hoạt động (uptime), trạng thái kết nối.',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết thiết bị nào đang ổn, thiết bị nào cần bảo trì.'
      },
      {
        title: 'Xem AI Diagnosis', moTa: 'AI phân tích và dự đoán: thiết bị nào có nguy cơ sắp hỏng, kèm giải thích.',
        nhapGi: null, bamGi: null,
        ketQua: 'Cảnh báo sớm trước khi thiết bị hỏng thực sự.'
      }
    ],
    thanhPhan: [
      { ten: 'Health metrics', yNghia: 'CPU, Memory, Uptime', ghiChu: null },
      { ten: 'AI Diagnosis', yNghia: 'Chẩn đoán AI: predicted status, insight', ghiChu: null }
    ]
  },
  {
    path: '/simulator-panel',
    label: 'Simulator - Mô phỏng thiết bị',
    icon: '🎮',
    roles: ['Admin', 'Bảo vệ'],
    group: 'Thiết bị & Hạ tầng',
    groupIcon: '🔧',
    mucDich: 'Tạo thiết bị ảo để kiểm thử. Có thể tạo controller giả lập và tiêm lỗi (fault injection) để xem hệ thống phản ứng thế nào.',
    steps: [
      {
        title: 'Tạo simulator', moTa: 'Nhập tên controller, chọn protocol (OSDP-Sim/ONVIF-Access-Sim), nhập max credentials. Bấm "Create simulator".',
        nhapGi: 'Nhập tên, chọn protocol, nhập max credentials', bamGi: 'Bấm "Create simulator"',
        ketQua: 'Thiết bị ảo được tạo.'
      },
      {
        title: 'Tiêm lỗi', moTa: 'Nhập device ID, chọn loại lỗi (Tamper/Offline/RelayFailure/BarrierStuck), chọn mức độ. Bấm "Inject fault".',
        nhapGi: 'Nhập device ID, chọn lỗi', bamGi: 'Bấm "Inject fault"',
        ketQua: 'Lỗi được tiêm vào thiết bị ảo. Hệ thống sẽ phản ứng như lỗi thật.'
      }
    ],
    thanhPhan: [
      { ten: 'Nút Create simulator', yNghia: 'Tạo thiết bị ảo', ghiChu: 'Dùng để test' },
      { ten: 'Nút Inject fault', yNghia: 'Tiêm lỗi vào thiết bị', ghiChu: 'Kiểm tra phản ứng hệ thống' }
    ]
  },
  {
    path: '/device-topology',
    label: 'Device Topology - Sơ đồ thiết bị',
    icon: '🔌',
    roles: ['Admin', 'Bảo vệ'],
    group: 'Thiết bị & Hạ tầng',
    groupIcon: '🔧',
    mucDich: 'Sơ đồ mạng thiết bị dạng đồ thị (graph). Hiển thị controller nào kết nối với camera/cổng nào, trạng thái kết nối ra sao.',
    steps: [
      {
        title: 'Xem sơ đồ', moTa: 'Sơ đồ dạng graph: các node là thiết bị, đường nối là kết nối. Màu xanh = kết nối tốt, đỏ = mất kết nối.',
        nhapGi: null, bamGi: null,
        ketQua: 'Hình dung toàn bộ mạng lưới thiết bị.'
      }
    ],
    thanhPhan: [
      { ten: 'Topology graph', yNghia: 'Sơ đồ mạng thiết bị', ghiChu: null },
      { ten: 'Trạng thái kết nối', yNghia: 'Xanh = tốt, đỏ = mất kết nối', ghiChu: null }
    ]
  },
  {
    path: '/biometrics',
    label: 'Biometrics - Dữ liệu nhận diện',
    icon: '🫵',
    roles: ['Admin'],
    group: 'Thiết bị & Hạ tầng',
    groupIcon: '🔧',
    mucDich: 'Quản lý dữ liệu nhận diện sinh trắc học (khuôn mặt). Xem độ phủ AI, train model Face ID.',
    steps: [
      {
        title: 'Xem dữ liệu', moTa: 'Bảng danh sách: nhân viên, model khuôn mặt, độ phủ (coverage).',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết nhân viên nào đã có dữ liệu khuôn mặt, ai chưa.'
      },
      {
        title: 'Train model', moTa: 'Bấm "Train model" để huấn luyện AI với dữ liệu mới, cải thiện độ chính xác.',
        nhapGi: null, bamGi: 'Bấm "Train model"',
        ketQua: 'Model được huấn luyện lại.'
      }
    ],
    thanhPhan: [
      { ten: 'Bảng biometric data', yNghia: 'Nhân viên, face model, coverage', ghiChu: null },
      { ten: 'Nút Train model', yNghia: 'Huấn luyện AI', ghiChu: null }
    ]
  },

  // ====================================================================
  // NHÓM 9: KHÁC
  // ====================================================================
  {
    path: '/vehicles',
    label: 'Quản lý phương tiện',
    icon: '🚙',
    roles: ['Admin', 'Quản lý'],
    group: 'Thiết bị & Hạ tầng',
    groupIcon: '🔧',
    mucDich: 'Đăng ký và quản lý xe ra vào công ty. Mỗi xe có biển số, loại xe, chủ sở hữu.',
    steps: [
      {
        title: 'Đăng ký xe mới', moTa: 'Bấm "Đăng ký phương tiện". Nhập biển số, chọn loại xe, chọn chủ sở hữu (nhân viên). Bấm "Lưu".',
        nhapGi: 'Nhập biển số, chọn loại xe, chọn chủ', bamGi: 'Bấm "Đăng ký phương tiện" → "Lưu"',
        ketQua: 'Xe được đăng ký, có thể ra vào tự động qua ANPR.'
      },
      {
        title: 'Tìm xe', moTa: 'Gõ biển số vào ô tìm để tra cứu.',
        nhapGi: 'Gõ biển số', bamGi: null,
        ketQua: 'Thông tin xe hiện ra.'
      }
    ],
    thanhPhan: [
      { ten: 'Nút "Đăng ký phương tiện"', yNghia: 'Thêm xe mới', ghiChu: null },
      { ten: 'Ô tìm biển số', yNghia: 'Tra cứu xe đã đăng ký', ghiChu: null }
    ]
  },
  {
    path: '/exceptions',
    label: 'Xử lý ngoại lệ',
    icon: '⚠️',
    roles: ['Admin', 'Bảo vệ', 'Quản lý'],
    group: 'Tổng quan & Giám sát',
    groupIcon: '📊',
    mucDich: 'Xử lý các trường hợp ngoại lệ: khi hệ thống không nhận diện được (Face ID lỗi, QR hết hạn, biển số không rõ...), bảo vệ có thể xem và xử lý thủ công.',
    steps: [
      {
        title: 'Lọc ngoại lệ', moTa: 'Nhập từ khóa (tên, biển số), chọn lý do ngoại lệ, chọn khoảng ngày. Bấm "Áp dụng lọc".',
        nhapGi: 'Nhập từ khóa, chọn lý do, chọn ngày', bamGi: 'Bấm "Áp dụng lọc"',
        ketQua: 'Danh sách ngoại lệ phù hợp hiện ra.'
      },
      {
        title: 'Xem chi tiết ngoại lệ', moTa: 'Bảng: thời gian, đối tượng, cổng, biển số, lý do, trạng thái, ghi chú.',
        nhapGi: null, bamGi: null,
        ketQua: 'Thông tin đầy đủ ngoại lệ.'
      },
      {
        title: 'Đặt lại', moTa: 'Bấm "Đặt lại" để xóa bộ lọc và xem toàn bộ ngoại lệ.',
        nhapGi: null, bamGi: 'Bấm "Đặt lại"',
        ketQua: 'Tất cả ngoại lệ hiện ra (không lọc).'
      }
    ],
    thanhPhan: [
      { ten: 'Các ô lọc', yNghia: 'Từ khóa, lý do, khoảng ngày', ghiChu: 'Tự động đổi ngày nếu sai thứ tự' },
      { ten: 'Filter summary tags', yNghia: 'Các tag hiển thị bộ lọc đang áp dụng', ghiChu: null },
      { ten: 'Bảng ngoại lệ', yNghia: 'Thời gian, người, cổng, biển số, lý do, trạng thái', ghiChu: null }
    ]
  },
  {
    path: '/campus-map',
    label: 'Bản đồ khuôn viên',
    icon: '🗺️',
    roles: ['Admin', 'Nhân viên', 'Bảo vệ'],
    group: 'Tổng quan & Giám sát',
    groupIcon: '📊',
    mucDich: 'Bản đồ tương tác của khuôn viên. Xem vị trí các tòa nhà, cổng, camera trên bản đồ. Trạng thái thiết bị hiển thị realtime.',
    steps: [
      {
        title: 'Xem bản đồ', moTa: 'Canvas tương tác hiển thị sơ đồ. Có thể zoom, pan (kéo) bằng chuột.',
        nhapGi: null, bamGi: 'Dùng chuột kéo và lăn để zoom',
        ketQua: 'Xem được toàn cảnh khuôn viên.'
      },
      {
        title: 'Xem trạng thái realtime', moTa: 'Các thiết bị trên bản đồ hiển thị trạng thái: xanh = online, đỏ = offline.',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết thiết bị nào đang hoạt động tốt.'
      }
    ],
    thanhPhan: [
      { ten: 'Canvas bản đồ', yNghia: 'Sơ đồ khuôn viên tương tác', ghiChu: null },
      { ten: 'Toolbar', yNghia: 'Zoom, pan, chọn layer', ghiChu: null }
    ]
  },
  {
    path: '/provisioning-wizard',
    label: 'Provisioning - Cấp phát thiết bị',
    icon: '📦',
    roles: ['Admin'],
    group: 'Thiết bị & Hạ tầng',
    groupIcon: '🔧',
    mucDich: 'Cấp phát thiết bị mới hàng loạt. Làm theo từng bước (wizard): chọn loại thiết bị, nhập thông tin, xác nhận. Có thể bulk registration từ file.',
    steps: [
      {
        title: 'Làm theo wizard', moTa: 'Bước 1: chọn loại thiết bị. Bước 2: nhập thông tin. Bước 3: xác nhận. Bấm "Next" để sang bước tiếp.',
        nhapGi: 'Nhập thông tin theo từng bước', bamGi: 'Bấm "Next" để tiếp tục',
        ketQua: 'Thiết bị được cấp phát thành công.'
      },
      {
        title: 'Bulk registration', moTa: 'Có thể import danh sách thiết bị từ file để đăng ký hàng loạt.',
        nhapGi: null, bamGi: 'Bấm "Bulk registration" → chọn file',
        ketQua: 'Nhiều thiết bị được thêm cùng lúc.'
      }
    ],
    thanhPhan: [
      { ten: 'Nút Next/Back', yNghia: 'Chuyển bước trong wizard', ghiChu: null },
      { ten: 'Nút Bulk registration', yNghia: 'Đăng ký hàng loạt từ file', ghiChu: null }
    ]
  },
  {
    path: '/outbox-viewer',
    label: 'Outbox Viewer - Hàng đợi gửi',
    icon: '📨',
    roles: ['Admin'],
    group: 'SOC & Enterprise',
    groupIcon: '🏢',
    mucDich: 'Xem các sự kiện đang chờ gửi đi (webhook, email, SIEM). Nếu gửi thất bại, có thể gửi lại (retry).',
    steps: [
      {
        title: 'Xem danh sách', moTa: 'Bảng: event type, target, status (Pending/Sent/Failed), retry count. Lọc theo status.',
        nhapGi: 'Chọn status để lọc', bamGi: null,
        ketQua: 'Biết sự kiện nào đang chờ, lỗi, hay đã gửi.'
      },
      {
        title: 'Gửi lại', moTa: 'Bấm "Retry" để gửi lại sự kiện bị lỗi. Bấm "Dispatch" để gửi ngay.',
        nhapGi: null, bamGi: 'Bấm "Retry" hoặc "Dispatch"',
        ketQua: 'Sự kiện được gửi lại.'
      }
    ],
    thanhPhan: [
      { ten: 'Bảng Outbox Events', yNghia: 'Các sự kiện chờ gửi', ghiChu: null },
      { ten: 'Nút Retry/Dispatch', yNghia: 'Gửi lại hoặc gửi ngay', ghiChu: null }
    ]
  },

  // ====================================================================
  // NHÓM 7: VIDEO & AI REVIEW (tiếp)
  // ====================================================================
  {
    path: '/ai-review-queue',
    label: 'AI Review Queue - Đánh giá kết quả AI',
    icon: '🤖',
    roles: ['Admin', 'Bảo vệ'],
    group: 'Video & AI Review',
    groupIcon: '🎥',
    mucDich: 'Danh sách các kết quả mà AI tự động phân tích (nhận diện khuôn mặt, biển số, phát hiện bất thường). Con người cần kiểm tra lại để xác nhận hoặc bác bỏ, giúp AI học hỏi và cải thiện.',
    steps: [
      {
        title: 'Xem danh sách cần review', moTa: 'Bảng hiển thị: ID, nguồn AI, loại (Face/Plate/Behavior), độ tin cậy, trạng thái.',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết có bao nhiêu kết quả AI cần kiểm tra.'
      },
      {
        title: 'Review từng kết quả', moTa: 'Bấm "Review". Xem thông tin AI đưa ra. Chọn: Confirm (đúng), Reject (sai), hoặc Flag (cần kiểm tra thêm). Nhập ghi chú nếu cần.',
        nhapGi: 'Nhập ghi chú (nếu có)', bamGi: 'Bấm "Review" → chọn Confirm/Reject/Flag',
        ketQua: 'Kết quả được xử lý. AI sẽ học từ phản hồi của bạn.'
      }
    ],
    thanhPhan: [
      { ten: 'Bảng kết quả AI', yNghia: 'Danh sách cần review', ghiChu: 'Có filter theo loại và trạng thái' },
      { ten: 'Nút Review', yNghia: 'Mở cửa sổ đánh giá', ghiChu: null },
      { ten: 'Nút Confirm/Reject/Flag', yNghia: 'Xác nhận đúng, sai hoặc cần kiểm tra', ghiChu: null }
    ]
  },
  {
    path: '/correlation-view',
    label: 'Correlation View - Tương quan tín hiệu',
    icon: '🔗',
    roles: ['Admin', 'Bảo vệ'],
    group: 'Video & AI Review',
    groupIcon: '🎥',
    mucDich: 'Hiển thị đồng thời nhiều nguồn thông tin về cùng một sự kiện: khuôn mặt + biển số + sự kiện. Giúp điều tra nhanh: "xe này ai lái?", "người này vào lúc nào?".',
    steps: [
      {
        title: 'Xem tương quan', moTa: 'Nhập ID sự kiện hoặc biển số hoặc tên người. Hệ thống hiển thị đồng thời: ảnh khuôn mặt, biển số xe, thời gian, sự kiện liên quan.',
        nhapGi: 'Nhập ID/biển số/tên', bamGi: null,
        ketQua: 'Màn hình split hiển thị tất cả thông tin liên quan đến đối tượng.'
      }
    ],
    thanhPhan: [
      { ten: 'Ô tìm kiếm', yNghia: 'Nhập ID/biển số/tên', ghiChu: null },
      { ten: 'Khung Face + Plate + Event', yNghia: 'Hiển thị đồng thời 3 luồng thông tin', ghiChu: null }
    ]
  },

  // ====================================================================
  // NHÓM 8: THIẾT BỊ & HẠ TẦNG (tiếp)
  // ====================================================================
  {
    path: '/offline-packages',
    label: 'Offline Packages - Gói dữ liệu ngoại tuyến',
    icon: '📦',
    roles: ['Admin'],
    group: 'Thiết bị & Hạ tầng',
    groupIcon: '🔧',
    mucDich: 'Quản lý các gói dữ liệu để đồng bộ với thiết bị offline (khi mất mạng). Tạo gói → gửi xuống thiết bị → thiết bị tự cập nhật khi có mạng.',
    steps: [
      {
        title: 'Xem danh sách gói', moTa: 'Bảng: tên gói, kích thước, trạng thái (Pending/Synced/Failed), ngày tạo.',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết gói nào đã đồng bộ, gói nào còn chờ.'
      },
      {
        title: 'Đồng bộ gói', moTa: 'Bấm "Sync" để gửi gói xuống thiết bị. Nếu lỗi, bấm "Retry" để gửi lại.',
        nhapGi: null, bamGi: 'Bấm "Sync" hoặc "Retry"',
        ketQua: 'Gói được đồng bộ xuống thiết bị.'
      }
    ],
    thanhPhan: [
      { ten: 'Bảng danh sách gói offline', yNghia: 'Tên, kích thước, trạng thái, ngày tạo', ghiChu: null },
      { ten: 'Nút Sync/Retry', yNghia: 'Đồng bộ hoặc gửi lại', ghiChu: null }
    ]
  },

  // ====================================================================
  // NHÓM 9: KHÁC — ATTENDANCE & NHÂN SỰ
  // ====================================================================
  {
    path: '/attendance/records',
    label: 'Bảng chấm công',
    icon: '📅',
    roles: ['Admin', 'Nhân viên', 'Bảo vệ'],
    group: 'Tổng quan & Giám sát',
    groupIcon: '📊',
    mucDich: 'Xem bảng chấm công của bạn hoặc của nhân viên (Admin). Hiển thị giờ vào, giờ ra, tổng giờ làm, trạng thái (đi đúng giờ/trễ/vắng).',
    steps: [
      {
        title: 'Xem chấm công của tôi', moTa: 'Nếu là Nhân viên: mở trang này sẽ thấy bảng chấm công của chính bạn trong tháng. Các ô màu xanh = đi đúng giờ, vàng = trễ, đỏ = vắng.',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết tình trạng đi làm của bạn.'
      },
      {
        title: 'Xem chấm công nhân viên (Admin)', moTa: 'Chọn tên nhân viên từ danh sách. Bảng hiển thị chấm công của người đó.',
        nhapGi: 'Chọn nhân viên', bamGi: null,
        ketQua: 'Thấy chấm công của nhân viên đã chọn.'
      },
      {
        title: 'Xuất báo cáo', moTa: 'Bấm "Xuất Excel" để tải bảng chấm công về máy.',
        nhapGi: null, bamGi: 'Bấm "Xuất Excel"',
        ketQua: 'File Excel được tải về.'
      }
    ],
    thanhPhan: [
      { ten: 'Bảng chấm công', yNghia: 'Hiển thị ngày, giờ vào, giờ ra, tổng giờ', ghiChu: null },
      { ten: 'Chọn tháng', yNghia: 'Chọn tháng muốn xem', ghiChu: null },
      { ten: 'Nút Xuất Excel', yNghia: 'Tải về file Excel', ghiChu: null }
    ]
  },
  {
    path: '/attendance/reports',
    label: 'Báo cáo chấm công',
    icon: '📊',
    roles: ['Admin', 'Quản lý'],
    group: 'Tổng quan & Giám sát',
    groupIcon: '📊',
    mucDich: 'Báo cáo tổng hợp tình hình chấm công của toàn công ty. Xem tỷ lệ đi đúng giờ, số người vắng, số người trễ trong tháng.',
    steps: [
      {
        title: 'Chọn kỳ báo cáo', moTa: 'Chọn tháng và năm muốn xem báo cáo.',
        nhapGi: 'Chọn tháng/năm', bamGi: null,
        ketQua: 'Báo cáo hiển thị số liệu.'
      },
      {
        title: 'Xem biểu đồ', moTa: 'Biểu đồ tròn hiển thị tỷ lệ: Đúng giờ / Trễ / Vắng / Nghỉ phép. Biểu đồ cột hiển thị xu hướng theo ngày.',
        nhapGi: null, bamGi: null,
        ketQua: 'Nắm được tình hình chấm công tổng quan.'
      },
      {
        title: 'Xem chi tiết', moTa: 'Bảng danh sách: từng nhân viên, số ngày đi làm, số ngày trễ, số ngày vắng.',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết ai có vấn đề về chấm công.'
      }
    ],
    thanhPhan: [
      { ten: 'Biểu đồ tròn, cột', yNghia: 'Hiển thị tỷ lệ và xu hướng', ghiChu: null },
      { ten: 'Bảng chi tiết', yNghia: 'Từng nhân viên và chỉ số', ghiChu: null }
    ]
  },
  {
    path: '/attendance/leave-requests',
    label: 'Đơn xin nghỉ phép',
    icon: '📝',
    roles: ['Admin', 'Nhân viên'],
    group: 'Tổng quan & Giám sát',
    groupIcon: '📊',
    mucDich: 'Tạo đơn xin nghỉ phép. Nhân viên gửi đơn, Quản lý/Admin duyệt hoặc từ chối. Xem số ngày phép còn lại.',
    steps: [
      {
        title: 'Tạo đơn mới', moTa: 'Bấm "Tạo đơn mới". Chọn loại nghỉ: Nghỉ phép năm, Nghỉ bệnh, Nghỉ việc riêng. Chọn ngày bắt đầu và kết thúc. Nhập lý do. Bấm "Gửi đơn".',
        nhapGi: 'Chọn loại nghỉ, ngày, nhập lý do', bamGi: 'Bấm "Tạo đơn mới" → "Gửi đơn"',
        ketQua: 'Đơn được gửi, trạng thái "Chờ duyệt".'
      },
      {
        title: 'Xem trạng thái đơn', moTa: 'Bảng danh sách: loại nghỉ, ngày, số ngày, lý do, trạng thái (Chờ duyệt/Đã duyệt/Từ chối).',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết đơn nào đã được duyệt.'
      },
      {
        title: 'Hủy đơn', moTa: 'Nếu đơn chưa được duyệt, có thể bấm "Hủy" để xóa.',
        nhapGi: null, bamGi: 'Bấm "Hủy"',
        ketQua: 'Đơn bị hủy.'
      }
    ],
    thanhPhan: [
      { ten: 'Nút "Tạo đơn mới"', yNghia: 'Tạo đơn xin nghỉ', ghiChu: null },
      { ten: 'Bảng danh sách đơn', yNghia: 'Loại, ngày, số ngày, lý do, trạng thái', ghiChu: null },
      { ten: 'Số ngày phép còn lại', yNghia: 'Hiển thị số ngày phép bạn còn', ghiChu: null }
    ]
  },
  {
    path: '/attendance/work-schedules',
    label: 'Lịch làm việc',
    icon: '🗓️',
    roles: ['Admin', 'Nhân viên', 'Bảo vệ'],
    group: 'Tổng quan & Giám sát',
    groupIcon: '📊',
    mucDich: 'Xem lịch làm việc theo ca. Biết hôm nay ai làm ca nào, giờ nào vào, giờ nào ra.',
    steps: [
      {
        title: 'Xem lịch', moTa: 'Lịch hiển thị dạng bảng: ngày, ca sáng/chiều/tối. Màu sắc phân biệt từng ca.',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết lịch làm việc trong tháng.'
      },
      {
        title: 'Đăng ký ca (nếu có)', moTa: 'Nếu công ty cho phép, có thể bấm vào ô trống để đăng ký ca làm.',
        nhapGi: null, bamGi: 'Bấm vào ô trống trên lịch',
        ketQua: 'Đăng ký ca thành công.'
      }
    ],
    thanhPhan: [
      { ten: 'Bảng lịch làm việc', yNghia: 'Ngày, ca sáng/chiều/tối', ghiChu: null }
    ]
  },
  {
    path: '/attendance/leave-approvals',
    label: 'Duyệt đơn xin nghỉ',
    icon: '✅',
    roles: ['Admin', 'Quản lý'],
    group: 'Tổng quan & Giám sát',
    groupIcon: '📊',
    mucDich: 'Quản lý duyệt hoặc từ chối đơn xin nghỉ của nhân viên. Xem lý do, kiểm tra số ngày phép còn lại, và quyết định.',
    steps: [
      {
        title: 'Xem danh sách đơn chờ duyệt', moTa: 'Bảng: nhân viên, loại nghỉ, ngày, số ngày, lý do, trạng thái.',
        nhapGi: null, bamGi: 'Chọn tab "Chờ duyệt"',
        ketQua: 'Danh sách đơn cần xử lý.'
      },
      {
        title: 'Duyệt hoặc từ chối', moTa: 'Bấm "Duyệt" (xanh) để đồng ý, "Từ chối" (đỏ) để không đồng ý. Có thể nhập ghi chú kèm theo.',
        nhapGi: 'Nhập ghi chú (nếu có)', bamGi: 'Bấm "Duyệt" hoặc "Từ chối"',
        ketQua: 'Đơn được xử lý. Nhân viên nhận thông báo.'
      }
    ],
    thanhPhan: [
      { ten: 'Tab: Chờ duyệt / Đã duyệt / Từ chối', yNghia: 'Lọc đơn theo trạng thái', ghiChu: null },
      { ten: 'Nút Duyệt / Từ chối', yNghia: 'Xử lý đơn xin nghỉ', ghiChu: null },
      { ten: 'Số ngày phép còn lại', yNghia: 'Hiển thị để tham khảo khi duyệt', ghiChu: null }
    ]
  },

  // ====================================================================
  // NHÓM 9: KHÁC — VẬN HÀNH & KIỂM TOÁN
  // ====================================================================
  {
    path: '/operations-dashboard',
    label: 'Operations Dashboard - Vận hành hệ thống',
    icon: '⚡',
    roles: ['Admin'],
    group: 'SOC & Enterprise',
    groupIcon: '🏢',
    mucDich: 'Bảng tổng quan tình hình vận hành hệ thống: Outbox (hàng đợi gửi), Backup (sao lưu), Security Checks (kiểm tra an ninh). Một nơi để quản trị viên nắm toàn bộ tình trạng hệ thống.',
    steps: [
      {
        title: 'Xem trạng thái Outbox', moTa: 'Phần "Outbox Queue": số sự kiện đang chờ gửi, số lỗi. Bấm "View" để vào Outbox Viewer.',
        nhapGi: null, bamGi: 'Bấm "View" ở mỗi phần',
        ketQua: 'Biết tình trạng hàng đợi.'
      },
      {
        title: 'Xem trạng thái Backup', moTa: 'Phần "Backup Status": lần sao lưu cuối, trạng thái (Success/Failed), dung lượng.',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết lần gần nhất hệ thống được sao lưu.'
      },
      {
        title: 'Xem Security Checks', moTa: 'Phần "Security Checks": kiểm tra port mở, cổng chưa cập nhật, phát hiện xâm nhập. Màu xanh = an toàn, đỏ = có vấn đề.',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết tình hình an ninh mạng của hệ thống.'
      }
    ],
    thanhPhan: [
      { ten: 'Phần Outbox Queue', yNghia: 'Sự kiện chờ gửi', ghiChu: null },
      { ten: 'Phần Backup Status', yNghia: 'Tình trạng sao lưu', ghiChu: null },
      { ten: 'Phần Security Checks', yNghia: 'Kiểm tra an ninh', ghiChu: 'Xanh = OK, Đỏ = có vấn đề' }
    ]
  },
  {
    path: '/siem-export-status',
    label: 'SIEM Export - Xuất dữ liệu an ninh',
    icon: '📤',
    roles: ['Admin'],
    group: 'SOC & Enterprise',
    groupIcon: '🏢',
    mucDich: 'Xuất dữ liệu sự kiện an ninh sang hệ thống SIEM bên ngoài (ví dụ: Splunk, ELK) để phân tích tập trung. Tại đây bạn xem trạng thái các lần xuất.',
    steps: [
      {
        title: 'Xem danh sách xuất', moTa: 'Bảng: ID lần xuất, thời gian, target (đích đến), trạng thái (InProgress/Success/Failed).',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết lần xuất nào thành công, lần nào lỗi.'
      },
      {
        title: 'Xuất mới', moTa: 'Bấm "Trigger Export". Chọn loại dữ liệu, khoảng thời gian. Bấm "Export".',
        nhapGi: 'Chọn loại dữ liệu, thời gian', bamGi: 'Bấm "Trigger Export" → "Export"',
        ketQua: 'Yêu cầu xuất được tạo, chờ xử lý.'
      }
    ],
    thanhPhan: [
      { ten: 'Bảng danh sách export', yNghia: 'ID, thời gian, target, trạng thái', ghiChu: null },
      { ten: 'Nút Trigger Export', yNghia: 'Tạo yêu cầu xuất mới', ghiChu: null }
    ]
  },
  {
    path: '/backup-restore-drill',
    label: 'Backup & Restore - Sao lưu và phục hồi',
    icon: '💾',
    roles: ['Admin'],
    group: 'SOC & Enterprise',
    groupIcon: '🏢',
    mucDich: 'Quản lý sao lưu hệ thống. Lên lịch backup, chạy backup thủ công, xem lịch sử backup, và thực hành phục hồi (drill) để đảm bảo dữ liệu luôn an toàn.',
    steps: [
      {
        title: 'Xem lịch sử backup', moTa: 'Bảng: thời gian, loại (Full/Incremental), dung lượng, trạng thái.',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết các lần backup gần đây.'
      },
      {
        title: 'Chạy backup thủ công', moTa: 'Bấm "Run Backup". Chọn loại backup. Bấm "Start".',
        nhapGi: null, bamGi: 'Bấm "Run Backup" → "Start"',
        ketQua: 'Quá trình backup bắt đầu. Hoàn thành sau vài phút.'
      },
      {
        title: 'Thực hành phục hồi (Drill)', moTa: 'Bấm "Start Drill". Hệ thống mô phỏng quá trình phục hồi để kiểm tra dữ liệu backup có hoạt động không.',
        nhapGi: null, bamGi: 'Bấm "Start Drill"',
        ketQua: 'Kết quả: Pass (dữ liệu OK) hoặc Fail (cần kiểm tra lại).'
      }
    ],
    thanhPhan: [
      { ten: 'Bảng backup history', yNghia: 'Lịch sử các lần backup', ghiChu: null },
      { ten: 'Nút Run Backup', yNghia: 'Chạy backup thủ công', ghiChu: null },
      { ten: 'Nút Start Drill', yNghia: 'Thực hành phục hồi thử', ghiChu: 'Quan trọng: kiểm tra backup có hoạt động không' }
    ]
  },
  {
    path: '/system-audit-logs',
    label: 'Nhật ký kiểm toán hệ thống',
    icon: '📜',
    roles: ['Admin'],
    group: 'SOC & Enterprise',
    groupIcon: '🏢',
    mucDich: 'Ghi lại tất cả hành động của người dùng trong hệ thống: ai đã làm gì, lúc nào, ở trang nào. Dùng để kiểm tra khi có sự cố hoặc kiểm toán.',
    steps: [
      {
        title: 'Xem nhật ký', moTa: 'Bảng: thời gian, người dùng, hành động, chi tiết, địa chỉ IP.',
        nhapGi: null, bamGi: null,
        ketQua: 'Xem tất cả hoạt động trong hệ thống.'
      },
      {
        title: 'Lọc nhật ký', moTa: 'Lọc theo người dùng, hành động, khoảng thời gian.',
        nhapGi: 'Chọn người dùng, hành động, ngày', bamGi: null,
        ketQua: 'Chỉ hiển thị nhật ký phù hợp.'
      }
    ],
    thanhPhan: [
      { ten: 'Bảng audit log', yNghia: 'Thời gian, người, hành động, IP', ghiChu: 'Không thể xóa hoặc sửa' }
    ]
  },
  {
    path: '/webhook-delivery-viewer',
    label: 'Webhook Delivery - Xem webhook đã gửi',
    icon: '🔗',
    roles: ['Admin'],
    group: 'SOC & Enterprise',
    groupIcon: '🏢',
    mucDich: 'Xem trạng thái các webhook đã gửi đi. Webhook là cách hệ thống gửi thông báo sự kiện ra ngoài (ví dụ: gửi thông báo ra vào qua webhook đến hệ thống khác).',
    steps: [
      {
        title: 'Xem danh sách webhook', moTa: 'Bảng: ID webhook, URL đích, sự kiện, trạng thái (Success/Failed/Pending), thời gian, số lần thử.',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết webhook nào đã gửi thành công, webhook nào lỗi.'
      },
      {
        title: 'Xem chi tiết lỗi', moTa: 'Bấm vào webhook bị lỗi để xem chi tiết: mã lỗi HTTP, response body.',
        nhapGi: null, bamGi: 'Bấm vào webhook lỗi',
        ketQua: 'Thông tin lỗi chi tiết giúp debug.'
      },
      {
        title: 'Gửi lại', moTa: 'Bấm "Retry" để gửi lại webhook bị lỗi.',
        nhapGi: null, bamGi: 'Bấm "Retry"',
        ketQua: 'Webhook được gửi lại.'
      }
    ],
    thanhPhan: [
      { ten: 'Bảng webhook delivery', yNghia: 'URL, sự kiện, trạng thái, số lần thử', ghiChu: null },
      { ten: 'Nút Retry', yNghia: 'Gửi lại webhook lỗi', ghiChu: null }
    ]
  },
  {
    path: '/visitor-pass',
    label: 'Visitor Pass - Thẻ thăm viếng',
    icon: '🪪',
    roles: ['Admin', 'Bảo vệ'],
    group: 'Quản lý Khách thăm',
    groupIcon: '👤',
    mucDich: 'Cấp thẻ tạm cho khách. In thẻ giấy hoặc cấp QR tạm thời. Thẻ có thời hạn và cổng được phép vào.',
    steps: [
      {
        title: 'Tạo thẻ mới', moTa: 'Bấm "Cấp thẻ mới". Nhập tên khách, loại thẻ (Giấy/QR), thời hạn, cổng được vào.',
        nhapGi: 'Nhập tên, loại thẻ, thời hạn, cổng', bamGi: 'Bấm "Cấp thẻ mới" → "In thẻ"',
        ketQua: 'Thẻ được tạo. In ra hoặc gửi QR cho khách.'
      },
      {
        title: 'Quản lý thẻ đã cấp', moTa: 'Bảng danh sách: tên khách, loại thẻ, thời hạn, trạng thái (Còn hiệu lực/Hết hạn/Đã thu hồi).',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết thẻ nào đang còn hiệu lực.'
      },
      {
        title: 'Thu hồi thẻ', moTa: 'Bấm "Thu hồi" để vô hiệu hóa thẻ trước thời hạn.',
        nhapGi: null, bamGi: 'Bấm "Thu hồi"',
        ketQua: 'Thẻ bị vô hiệu hóa, không thể dùng để ra vào.'
      }
    ],
    thanhPhan: [
      { ten: 'Nút "Cấp thẻ mới"', yNghia: 'Tạo thẻ tạm cho khách', ghiChu: null },
      { ten: 'Bảng danh sách thẻ', yNghia: 'Tên, loại, thời hạn, trạng thái', ghiChu: null },
      { ten: 'Nút Thu hồi', yNghia: 'Vô hiệu hóa thẻ', ghiChu: null }
    ]
  },
  {
    path: '/contractors',
    label: 'Quản lý nhà thầu (Contractor)',
    icon: '👷',
    roles: ['Admin'],
    group: 'Quản lý Khách thăm',
    groupIcon: '👤',
    mucDich: 'Quản lý nhà thầu/nhân công tạm thời làm việc tại công ty. Khác với khách thăm, nhà thầu ở lại nhiều ngày và cần quyền truy cập đặc biệt.',
    steps: [
      {
        title: 'Đăng ký nhà thầu mới', moTa: 'Bấm "Add Contractor". Nhập tên công ty, đại diện, số lượng nhân công, thời gian làm việc, khu vực được phép vào.',
        nhapGi: 'Nhập thông tin nhà thầu', bamGi: 'Bấm "Add Contractor" → "Save"',
        ketQua: 'Nhà thầu được đăng ký. Có thể cấp thẻ cho từng nhân công.'
      },
      {
        title: 'Cấp thẻ cho nhân công', moTa: 'Trong hồ sơ nhà thầu, bấm "Issue Badges". Nhập danh sách nhân công (tên, CCCD, ảnh). Bấm "Issue".',
        nhapGi: 'Nhập danh sách nhân công', bamGi: 'Bấm "Issue Badges" → "Issue"',
        ketQua: 'Mỗi nhân công có thẻ riêng để quét ra vào.'
      },
      {
        title: 'Kết thúc hợp đồng', moTa: 'Khi nhà thầu hết việc, bấm "Contract End". Hệ thống tự động thu hồi tất cả thẻ.',
        nhapGi: null, bamGi: 'Bấm "Contract End"',
        ketQua: 'Tất cả thẻ bị vô hiệu hóa. Nhà thầu không thể vào công ty.'
      }
    ],
    thanhPhan: [
      { ten: 'Nút Add Contractor', yNghia: 'Đăng ký nhà thầu mới', ghiChu: null },
      { ten: 'Nút Issue Badges', yNghia: 'Cấp thẻ cho nhân công', ghiChu: null },
      { ten: 'Nút Contract End', yNghia: 'Kết thúc hợp đồng, thu hồi thẻ', ghiChu: null }
    ]
  },
  {
    path: '/vulnerability-release-gate',
    label: 'Vulnerability Gate - Cổng kiểm tra bảo mật',
    icon: '🛡️',
    roles: ['Admin'],
    group: 'SOC & Enterprise',
    groupIcon: '🏢',
    mucDich: 'Kiểm tra an ninh trước khi phát hành phần mềm hoặc cập nhật. Quét lỗ hổng, kiểm tra phụ thuộc, đảm bảo không có lỗi bảo mật trước khi triển khai.',
    steps: [
      {
        title: 'Xem kết quả quét', moTa: 'Bảng: gói phụ thuộc (dependency), phiên bản hiện tại, phiên bản mới nhất, mức độ lỗ hổng (Critical/High/Medium/Low).',
        nhapGi: null, bamGi: null,
        ketQua: 'Biết gói nào có lỗ hổng bảo mật.'
      },
      {
        title: 'Xử lý lỗ hổng', moTa: 'Bấm "Cập nhật" để nâng cấp gói lên phiên bản an toàn. Hoặc bấm "Bỏ qua" nếu lỗ hổng không ảnh hưởng.',
        nhapGi: null, bamGi: 'Bấm "Cập nhật" hoặc "Bỏ qua"',
        ketQua: 'Lỗ hổng được xử lý hoặc ghi nhận.'
      }
    ],
    thanhPhan: [
      { ten: 'Bảng vulnerability scan', yNghia: 'Gói, phiên bản, mức độ, trạng thái', ghiChu: null },
      { ten: 'Nút Cập nhật / Bỏ qua', yNghia: 'Xử lý từng lỗ hổng', ghiChu: null },
      { ten: 'Score tổng thể', yNghia: 'Điểm an ninh tổng quát', ghiChu: null }
    ]
  },
  {
    path: '/system-catalog',
    label: 'System Catalog - Danh mục hệ thống',
    icon: '📂',
    roles: ['Admin', 'Quản lý'],
    group: 'Tổng quan & Giám sát',
    groupIcon: '📊',
    mucDich: 'Quản lý danh mục dùng chung: phòng ban, chức vụ, loại xe, loại giấy tờ. Các danh mục này được dùng ở nhiều trang khác (nhân viên, xe, khách).',
    steps: [
      {
        title: 'Chọn danh mục', moTa: 'Bấm vào tab: Phòng ban, Chức vụ, Loại xe, Loại giấy tờ.',
        nhapGi: null, bamGi: 'Bấm tab tương ứng',
        ketQua: 'Hiển thị danh sách mục trong danh mục đó.'
      },
      {
        title: 'Thêm mục mới', moTa: 'Bấm "Thêm mới". Nhập tên, mã (nếu có). Bấm "Lưu".',
        nhapGi: 'Nhập tên, mã', bamGi: 'Bấm "Thêm mới" → "Lưu"',
        ketQua: 'Mục mới được thêm vào danh mục.'
      },
      {
        title: 'Sửa/Xóa', moTa: 'Bấm "Sửa" để đổi tên. Bấm "Xóa" để xóa (không xóa được nếu đang có dữ liệu liên quan).',
        nhapGi: null, bamGi: 'Bấm "Sửa" hoặc "Xóa"',
        ketQua: 'Danh mục được cập nhật.'
      }
    ],
    thanhPhan: [
      { ten: 'Các tab: Phòng ban, Chức vụ, Loại xe...', yNghia: 'Chọn loại danh mục', ghiChu: null },
      { ten: 'Nút Thêm mới', yNghia: 'Thêm mục vào danh mục', ghiChu: null }
    ]
  },
]

// ====================================================================
// HELPERS
// ====================================================================

export const groups = [
  { id: 'toan-quan', label: 'Tổng quan & Giám sát', icon: '📊', color: '#3b82f6' },
  { id: 'soc-enterprise', label: 'SOC & Enterprise', icon: '🏢', color: '#8b5cf6' },
  { id: 'khach-tham', label: 'Quản lý Khách thăm', icon: '👤', color: '#f59e0b' },
  { id: 'ai-thiet-bi', label: 'AI & Thiết bị', icon: '🤖', color: '#10b981' },
  { id: 'giao-thong', label: 'Giao thông & Bãi đỗ', icon: '🚗', color: '#ec4899' },
  { id: 'evidence', label: 'Evidence & Compliance', icon: '📁', color: '#06b6d4' },
  { id: 'video-ai', label: 'Video & AI Review', icon: '🎥', color: '#f43f5e' },
  { id: 'thiet-bi', label: 'Thiết bị & Hạ tầng', icon: '🔧', color: '#6b7280' },
]

export const roles = [
  { id: 'all', label: 'Tất cả' },
  { id: 'Admin', label: 'Admin' },
  { id: 'Bảo vệ', label: 'Bảo vệ' },
  { id: 'Quản lý', label: 'Quản lý' },
  { id: 'Nhân viên', label: 'Nhân viên' },
]

export const quickGuides = [
  {
    role: 'Bảo vệ',
    tasks: [
      { label: 'Xem camera trực tiếp', path: '/monitoring', icon: '📹' },
      { label: 'Check-in khách tại lễ tân', path: '/reception', icon: '🛎️' },
      { label: 'Xử lý ngoại lệ ra vào', path: '/exceptions', icon: '⚠️' },
      { label: 'Xem lịch sử ra vào', path: '/access-logs', icon: '📋' },
      { label: 'Theo dõi cổng ra vào', path: '/gate-transit-monitor', icon: '🛣️' },
    ]
  },
  {
    role: 'Nhân viên',
    tasks: [
      { label: 'Tạo QR ra vào', path: '/dynamic-qr-generator', icon: '📱' },
      { label: 'Xem bảng chấm công', path: '/attendance/records', icon: '📅' },
      { label: 'Gửi đơn xin nghỉ phép', path: '/attendance/leave-requests', icon: '📝' },
      { label: 'Mời khách đến công ty', path: '/host-visitor', icon: '✉️' },
      { label: 'Xem bản đồ khuôn viên', path: '/campus-map', icon: '🗺️' },
    ]
  },
  {
    role: 'Quản lý',
    tasks: [
      { label: 'Xem Dashboard tổng quan', path: '/dashboard', icon: '🏠' },
      { label: 'Xem báo cáo chấm công', path: '/attendance/reports', icon: '📊' },
      { label: 'Duyệt đơn xin nghỉ', path: '/attendance/leave-approvals', icon: '✅' },
      { label: 'Xem UEBA bất thường', path: '/ueba', icon: '🔍' },
    ]
  },
  {
    role: 'Admin',
    tasks: [
      { label: 'Cấu hình hệ thống', path: '/settings', icon: '⚙️' },
      { label: 'Quản lý nhân viên', path: '/employees', icon: '👥' },
      { label: 'Tạo & duyệt chính sách', path: '/policy-engine', icon: '📜' },
      { label: 'Xử lý cảnh báo SOC', path: '/soc-console', icon: '🚨' },
      { label: 'Xem nhật ký kiểm toán', path: '/system-audit-logs', icon: '📜' },
      { label: 'Quản lý thiết bị', path: '/device-management', icon: '📡' },
    ]
  },
]

export const faqs = [
  {
    q: 'Tôi quên mật khẩu đăng nhập thì làm thế nào?',
    a: 'Bạn không thể tự đặt lại mật khẩu. Hãy liên hệ với Quản trị viên (Admin) của hệ thống để được cấp lại mật khẩu mới.'
  },
  {
    q: 'Tại sao tôi không thấy hình ảnh từ camera?',
    a: 'Có thể do: 1) Camera chưa được cấu hình trong Cài đặt. 2) URL stream sai. 3) Dịch vụ go2rtc chưa chạy. 4) Trình duyệt chặn nội dung không an toàn (mixed content).'
  },
  {
    q: 'Làm thế nào để tạo mã QR ra vào?',
    a: 'Nhân viên: sau khi đăng nhập, QR tự động hiện ra. Admin: vào Tạo QR động, nhập Employee ID, bấm "Phát QR realtime".'
  },
  {
    q: 'Sự khác nhau giữa các vai trò?',
    a: 'Admin: toàn quyền. Bảo vệ: giám sát camera, check-in khách, xử lý ngoại lệ. Quản lý: báo cáo, duyệt đơn. Nhân viên: QR, chấm công, mời khách.'
  },
  {
    q: 'Làm thế nào để thêm nhân viên mới?',
    a: 'Vào Quản lý nhân viên (chỉ Admin). Bấm "Thêm nhân viên". Điền họ tên, SĐT, email, phòng ban, chức vụ. Có thể upload ảnh khuôn mặt cho Face ID.'
  },
  {
    q: 'Tôi thấy người lạ vào công ty, phải làm sao?',
    a: 'Vào Reception kiểm tra hoặc check-in. Vào Watchlist kiểm tra xem có trong danh sách theo dõi không. Nếu cần, Escalate hoặc tạo alarm trong SOC Console.'
  },
  {
    q: 'Làm thế nào để xử lý barrier bị kẹt?',
    a: 'Vào Barrier Control. Bấm "Open" để mở khẩn cấp (phải nhập lý do). Nếu không xử lý được, báo Admin.'
  },
  {
    q: 'Làm thế nào để xem ai đã ra vào hôm qua?',
    a: 'Vào Tra cứu vào/ra. Chọn "Từ ngày" và "Đến ngày" (hôm qua). Có thể lọc thêm theo cổng. Bấm "Áp dụng lọc".'
  }
]
