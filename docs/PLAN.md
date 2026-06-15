# Kế Hoạch Triển Khai: AI Chatbot Assistant & Hướng Dẫn Sử Dụng V-Shield

## Tổng Quan

**Mục tiêu:** Thêm một trợ lý AI (chatbot) ở góc dưới màn hình + một tài liệu hướng dẫn sử dụng tương tác (Vue-based) cho toàn bộ hệ thống V-Shield.

**Phân vai người dùng:**
| Vai trò | Mô tả | Trang mặc định |
|---------|-------|----------------|
| **Admin** | Quản trị viên - toàn quyền | Dashboard |
| **BaoVe** | Bảo vệ trực cổng | Dashboard |
| **QuanLy** | Quản lý vận hành | Dashboard |
| **Staff** | Nhân viên văn phòng | Dynamic QR Generator |

**Các pages trong hệ thống:** ~65 pages (xem router/index.js)

---

## Phase A: Xóa Docs Cũ & Dọn Dẹp ✅ (Đã Làm)

- [x] Xóa 16 file kế hoạch cũ trong `docs/` (giữ lại 7 file quan trọng)
- [x] Tạo file `docs/PLAN.md` này

---

## Phase B: Tạo Tài Liệu Hướng Dẫn Sử Dụng (User Guide)

### B.1: Tạo component `GuideViewer.vue`

**File:** `View/src/pages/GuideViewer.vue`

**Thiết kế:**
- Sử dụng design system có sẵn (CSS variables: `--font-heading`, `--bg-card`, `--border-radius`, `--accent-*`, `--shadow-*`, ...)
- Giao diện sách/guide dạng cuộn, có sidebar mục lục bên trái
- Header lớn với title + mô tả
- Mỗi section là một card với icon, tiêu đề, nội dung
- Có tabs/buttons để chuyển đổi giữa các vai trò (Admin, BaoVe, QuanLy, Staff)
- Animation mượt mà (dùng keyframes có sẵn: `fadeIn`, `slideUp`)

**Nội dung cần cover:**
1. **Tổng quan hệ thống** - V-Shield là gì, kiến trúc tổng thể
2. **Hướng dẫn theo vai trò** (4 tab):
   - **Admin:** Toàn bộ quyền, tất cả pages
   - **Bảo vệ (BaoVe):** Monitoring, Access Logs, Guest Profiles, Reception, Kiosk, Lane, Barrier, Device...
   - **Quản lý (QuanLy):** Dashboard, Monitoring, Access Logs, Vehicles, Attendance Reports, System Catalog...
   - **Nhân viên (Staff):** Dynamic QR Generator, Host Visitor, Attendance Records, Campus Map...
3. **Luồng hoạt động theo vai trò:**
   - Admin: Quản trị → Phân quyền → Giám sát → Xử lý ngoại lệ → Báo cáo
   - Bảo vệ: Giám sát camera → Xử lý vào/ra → Check-in khách → Xử lý biển số
   - Quản lý: Dashboard → Báo cáo → Duyệt đơn → Danh mục
   - Nhân viên: Tạo QR → Chấm công → Đơn nghỉ → Mời khách
4. **Mô tả từng page:**
   - Tên, đường dẫn, mục đích
   - Các button, input, table, chức năng chính
   - Ảnh mockup/minh họa (dùng SVG inline hoặc icon từ design system)
5. **FAQ / Mẹo sử dụng:**
   - Các thao tác thường gặp
   - Xử lý lỗi cơ bản
   - Shortcut/phím tắt (nếu có)

### B.2: Thêm route cho Guide

**File:** `View/src/router/index.js`
- Thêm route `/guide` với component `GuideViewer.vue`
- Meta: `{ requiresAuth: true }` (chỉ người dùng đã đăng nhập mới xem được)

---

## Phase C: Tạo AI Chatbot Widget

### C.1: Tạo component `AIChatBot.vue`

**File:** `View/src/components/AIChatBot.vue`

**Thiết kế giao diện:**
- **Floating button** (góc dưới-phải):
  - Icon chat bubble với gradient `--accent-gradient`
  - Hiệu ứng pulse nhẹ khi idle
  - Hover: scale + glow
  - Badge "AI" nhỏ
- **Chat dialog** (khi click vào button):
  - Popup từ góc dưới-phải, animation slide-up + fade
  - Width: 380px, max-height: 560px
  - Border-radius: 24px
  - Background: `--bg-card-strong` với backdrop-filter blur
  - Shadow: `--shadow-xl`
- **Header chat:**
  - Icon robot/shield + "Trợ lý V-Shield"
  - Status dot + "Sẵn sàng hỗ trợ"
  - Nút close (X)
- **Messages area:**
  - Messages được căn trái (AI) và phải (user)
  - AI messages: bubble màu trắng/xám nhạt
  - User messages: bubble màu `--accent-gradient` (chữ trắng)
  - Avatar AI nhỏ bên cạnh message
  - Auto-scroll xuống cuối
- **Suggested questions (chips):**
  - Hiển thị dưới dạng chips/capsule buttons
  - "📖 Hướng dẫn tôi sử dụng phần mềm này"
  - "🔐 Tôi là Admin, tôi có thể làm gì?"
  - "🛡️ Tôi là Bảo vệ, cần làm gì khi có người lạ?"
  - "❓ Các câu hỏi thường gặp"
  - Khi click vào chip → tự động gửi message đó
- **Input area:**
  - Text input với placeholder "Nhập câu hỏi..."
  - Nút gửi (icon send)
  - Disabled state khi đang "xử lý"

**Logic xử lý:**
1. Khi user gửi message (click chip hoặc nhập + gửi):
   - Hiển thị message của user trong chat
   - Hiển thị "đang trả lời..." (typing indicator)
   - Delay 0.5-1s để tạo cảm giác tự nhiên
2. Xử lý intent (dùng keyword matching đơn giản):
   - Nếu message chứa "hướng dẫn" / "cách dùng" / "sử dụng":
     - Trả lời kèm link: `👉 Xem hướng dẫn đầy đủ tại đây: [📖 Mở Hướng dẫn sử dụng]($ROOT/guide)` (có thể click để mở)
   - Nếu message chứa "admin" / "quản trị":
     - Trả lời với quyền hạn của Admin và link đến section tương ứng trong guide
   - Nếu message chứa "bảo vệ" / "baove":
     - Trả lời với quyền hạn của Bảo vệ và link đến section
   - Nếu message chứa "nhân viên" / "staff":
     - Trả lời với quyền hạn của Staff
   - Nếu message chứa "quản lý" / "quanly":
     - Trả lời với quyền hạn của Quản lý
   - Mặc định: trả lời gợi ý các câu hỏi mẫu
3. **Link trong chat:** Dùng `<router-link>` hoặc `<a>` để mở `/guide`

### C.2: Tích hợp vào App

**File:** `View/src/components/layout/MainLayout.vue`
- Import và thêm `<AIChatBot />` vào template (chỉ hiển thị khi đã đăng nhập, tức là trong MainLayout)

**File:** `View/src/App.vue`
- Hoặc thêm `<AIChatBot v-if="isLoggedIn" />` ở App.vue để hiển thị trên mọi page (kể cả login? Không, chỉ khi đã login)

### C.3: State Management

- Sử dụng `reactive` / `ref` local trong component (không cần store riêng)
- Messages array: `{ id, role: 'user'|'ai', text, timestamp }`
- Tracking first-time user để tự động gửi lời chào

---

## Phase D: Tinh Chỉnh & Kiểm Thử

### D.1: Kiểm thử
- Chạy `npm run dev` (hoặc `npm run build`) để kiểm tra lỗi build
- Verify các routes hoạt động
- Test chatbot trên nhiều kích thước màn hình (responsive)

### D.2: Review
- Dùng `code-reviewer-deepseek-flash` review toàn bộ changes
- Fix các vấn đề

### D.3: Commit
- `git add -A && git commit -m "feat: add AI chatbot widget and interactive user guide"`

---

## Timeline Ước Lượng

| Bước | Mô tả | Thời gian |
|------|-------|-----------|
| B.1 | Tạo GuideViewer.vue | 2-3 buổi |
| B.2 | Thêm route | 15 phút |
| C.1 | Tạo AIChatBot.vue | 1-2 buổi |
| C.2 | Tích hợp vào App | 30 phút |
| D.1-D.3 | Kiểm thử, review, commit | 1 buổi |

**Tổng:** Khoảng 4-6 buổi làm việc

---

## Kiến Trúc Component

```
View/src/
├── components/
│   ├── layout/
│   │   ├── MainLayout.vue    ← Thêm <AIChatBot /> ở đây
│   │   ├── Sidebar.vue
│   │   └── Header.vue
│   └── AIChatBot.vue          ← MỚI: Chatbot widget
├── pages/
│   └── GuideViewer.vue        ← MỚI: Hướng dẫn sử dụng
├── router/
│   └── index.js                ← Thêm route /guide
└── stores/
    └── auth.js                 ← Đã có isLoggedIn()
```

---

## Ghi Chú Thiết Kế

- **Màu sắc:** Dùng đúng design system hiện có (CSS variables trong `style.css`)
- **Font:** `--font-heading` (Space Grotesk) cho tiêu đề, `--font-body` (IBM Plex Sans) cho nội dung
- **Component style:** Dùng `<style scoped>` cho mỗi component
- **Responsive:** Chatbot mobile: full-width bottom sheet thay vì popup
- **Accessibility:** focus-visible outline, aria-label cho buttons, semantic HTML
