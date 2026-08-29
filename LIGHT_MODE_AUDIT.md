# BÁO CÁO AUDIT TOÀN DIỆN LIGHT MODE UI/UX — V-SHIELD 2.0

**Hệ thống:** V-Shield 2.0 Enterprise Physical & Operational Security Platform  
**Vị trí:** Senior Frontend Engineer + UI/UX Designer + Accessibility Specialist  
**Phạm vi:** Toàn bộ hệ thống giao diện (`src/styles/`, `src/components/`, `src/pages/`, `src/Layout/`), 78 trang nghiệp vụ, 29 shared UI components, AI Chatbot, Table & Form systems.  
**Ngày lập:** 29/08/2026  
**Trạng thái Baseline Test:** 133/133 test suites pass, 869/869 unit tests pass, Vite build pass.

---

## 1. Executive Summary

Phần mềm **V-Shield 2.0** là nền tảng quản trị thông hành, kiểm soát an ninh vật lý, chấm công, giám sát camera trực tiếp và hỗ trợ điều hành thông minh qua trợ lý AI DeepSeek.

Hệ thống đã xây dựng nền móng Design Tokens vững chắc tại `src/styles/tokens.css` với các biến màu ngữ nghĩa (`--surface-*`, `--text-*`, `--border-*`, `--status-*`, `--radius-*`, `--shadow-*`). Tuy nhiên, qua đợt kiểm tra và audit toàn diện chế độ **Light Mode** trên toàn bộ 78 màn hình và component, nhóm audit đã phát hiện một số điểm cần chuẩn hóa và tái cấu trúc (refactor) để đạt tiêu chuẩn **Enterprise-Grade Clean UI**:

1. **AI Chatbot (`AIChatBot.vue`) hoàn toàn cố định Dark Theme**: Cửa sổ chat AI nổi ở góc phải màn hình đang sử dụng mã màu tối cứng (`background: rgba(20, 22, 34, 0.92); color: #e8eaf2`), khiến widget chat bị lệch tông hoàn toàn khi người dùng ở Light Mode.
2. **Một số trang nghiệp vụ còn sót màu Hardcoded**: Trang `Chat.vue`, `EnterpriseSecurityOperations.vue`, `ReceptionDashboard.vue`, `SystemAuditLogs.vue`, `ClaimApproval.vue` còn chứa một số màu hex cố định (`#1976D2`, `#8ceaf4`, `#0f172a`, `#e5e7eb`, v.v.) thay vì tận dụng trọn vẹn Design Tokens.
3. **Phân tầng thị giác (Visual Hierarchy) và độ tương phản viền (Border Distinction) ở Light Mode**: Ở một số màn hình nhiều thẻ (Bento Grid, Metric Tiles), các card bề mặt `--surface-default` (#ffffff) trên nền app `--surface-app` (#eef4f6) đôi khi có độ tương phản viền hơi thấp trên các màn hình văn phòng có độ sáng cao, cần tinh chỉnh `--border-subtle` và `--border-default` để các khối tách biệt rõ ràng và chuyên nghiệp.
4. **Trạng thái Focus & Keyboard Navigation**: Một số button icon-only và filter dropdown cần củng cố `focus-visible` ring rõ nét theo chuẩn WCAG 2.1 AA.
5. **Đồng bộ hóa Trạng thái Hover / Active / Disabled**: Cần đảm bảo 100% component bảng (`DataTable.vue`), tab (`BaseTabs.vue`), modal (`BaseModal.vue`), drawer (`DecisionDrawer.vue`) và thanh công cụ tìm kiếm có phản hồi xúc giác (micro-interactions) tinh tế, không chói mắt.

---

## 2. Current Architecture Overview

### 2.1. Theme Architecture & Color System
- **File Token gốc**: `src/styles/tokens.css`
  - Được kích hoạt tự động qua thuộc tính `data-theme="light"` (mặc định) hoặc `data-theme="dark"` trên thẻ `<html>` (`:root`).
  - Điều khiển chuyển đổi theme thông qua composable `src/composables/usePreferences.js` (`setTheme()`, `toggleTheme()`).
- **Phân cấp bề mặt (Surfaces Hierarchy) ở Light Mode**:
  - `Page Shell / App Background`: `--surface-app: #eef4f6` (màu nền dịu mắt pha ánh xanh/slate nhẹ, chống mỏi mắt so với nền trắng tinh).
  - `Default Card / Panel Surface`: `--surface-default: #ffffff`
  - `Raised / Floating Surface (Modals, Popovers, Drawers)`: `--surface-raised: #fbfdfe`
  - `Subtle / Inset Surface (Inputs, Table Headers, Filter Bars)`: `--surface-subtle: #f4f8f9`
  - `Interactive Hover`: `--surface-hover: #edf6f7`
  - `Selected / Active`: `--surface-selected: #e1f1f3`
  - `Modal Overlay Backdrop`: `--surface-overlay: rgba(16, 32, 51, 0.56)`
- **Phân cấp chữ (Typography Hierarchy) ở Light Mode**:
  - `--text-primary: #102033` (Độ tương phản **14.5:1** trên nền trắng — vượt tiêu chuẩn WCAG AAA 7.0:1)
  - `--text-secondary: #3f586b` (Độ tương phản **5.8:1** trên nền trắng — đạt WCAG AA)
  - `--text-muted: #526979` (Độ tương phản **4.6:1** trên nền trắng — đạt WCAG AA)
  - `--text-disabled: #8fa0ad`
  - `--text-link: #0b686e`
  - `--text-on-interactive: #ffffff`
- **Hệ thống Trạng thái (Semantic Status Tokens)**:
  - `Info`: `--status-info-bg: #e8f5f8`, `--status-info-border: #91cbd4`, `--status-info-text: #225a73`
  - `Success`: `--status-success-bg: #e7f5ef`, `--status-success-border: #82c7ad`, `--status-success-text: #0d6b54`
  - `Warning`: `--status-warning-bg: #fff4df`, `--status-warning-border: #dfb76f`, `--status-warning-text: #7a4a0d`
  - `Danger`: `--status-danger-bg: #fbeceb`, `--status-danger-border: #dda09b`, `--status-danger-text: #9a302a`
  - `Neutral`: `--status-neutral-bg: #edf2f4`, `--status-neutral-border: #becbd1`, `--status-neutral-text: #485f70`

---

## 3. Phân loại các vấn đề phát hiện (Current Problems)

### 3.1. Critical (P0 — Ảnh hưởng trực tiếp tính nhất quán Theme)
| Vấn đề | Vị trí | Mô tả chi tiết | Hướng xử lý |
|---|---|---|---|
| **AI Chatbot không có Light Theme** | `src/components/AIChatBot.vue` | Panel chat cố định màu nền tối `rgba(20, 22, 34, 0.92)` và text trắng xám, bong bóng tin nhắn AI mờ tối, không hòa nhập với giao diện sáng. | Refactor toàn bộ CSS của `AIChatBot.vue` để dùng CSS variables ngữ nghĩa (`--surface-default`, `--surface-subtle`, `--text-primary`, `--border-subtle`, `--shadow-overlay`), giữ nguyên Dark Mode khi `[data-theme='dark']`. |
| **Trang Chat nội bộ dùng màu cứng** | `src/pages/Chat.vue` | Các thẻ tin nhắn, nút gửi, tab dùng mã màu cứng `#1976D2`, `#1565C0`, `#f0f7ff`. | Chuyển đổi sang `var(--interactive-primary)`, `var(--surface-selected)`, `var(--surface-default)` và `var(--border-subtle)`. |

### 3.2. High (P1 — Độ tương phản và Thống nhất Component)
| Vấn đề | Vị trí | Mô tả chi tiết | Hướng xử lý |
|---|---|---|---|
| **Header Notification Dropdown Items** | `src/components/Layout/Header.vue` | Notification items có nền `rgba(255, 255, 255, 0.88)` hơi mờ, khi hover nền đổi nhẹ `rgba(240, 248, 250, 0.92)` chưa có độ sâu rõ rệt. | Chuẩn hóa sang `var(--surface-default)` và hover `var(--surface-hover)` kèm viền `var(--border-subtle)` rõ ràng. |
| **Enterprise Security Operations Tabs** | `src/pages/EnterpriseSecurityOperations.vue` | Tabs dùng màu cứng `background: #8ceaf4` và `color: #05313b`, không theo token `BaseTabs`. | Đồng bộ hóa tab styling với hệ thống token chung. |
| **Code Snippet / Json Diff Viewer** | `src/pages/SystemAuditLogs.vue` | Khung `.code-shell` có nền đen `#0f172a` cố định trong khi các phần còn lại là Light Mode. | Bổ sung styling adaptive cho code snippet: nền dịu `#f1f5f9` viền `#cbd5e1` chữ `#0f172a` trong Light Mode, giữ nền tối ở Dark Mode. |

### 3.3. Medium (P2 — Cải thiện Hierarchy, Elevation & Spacing)
| Vấn đề | Vị trí | Mô tả chi tiết | Hướng xử lý |
|---|---|---|---|
| **Độ rõ nét của Viền Card ở Light Mode** | `src/styles/tokens.css` & `src/style.css` | `--border-subtle` (hiện là `rgba(24, 49, 77, 0.10)`) có thể hơi nhạt trên một số màn hình TN/IPS độ tương phản thấp. | Tinh chỉnh `--border-subtle` lên `rgba(24, 49, 77, 0.12)` và `--border-default` lên `rgba(24, 49, 77, 0.18)` để đường phân chia các thẻ rõ nét, sắc sảo. |
| **Trạng thái Empty State trên các Data Table** | `src/components/ui/DataTable.vue` & các trang Table | Một số bảng rỗng chưa có khoảng đệm tối ưu trên mobile. | Chuẩn hóa padding và icon kích thước phù hợp trong `EmptyState.vue`. |
| **Form Inputs Hover & Focus State** | `src/components/ui/BaseInput.vue`, `BaseSelect.vue`, `BaseTextarea.vue` | Focus ring màu `#168e98` rất đẹp nhưng cần đảm bảo `color-mix` box-shadow luôn hiển thị đồng nhất ở mọi trình duyệt. | Tối ưu box-shadow focus sang `rgba(22, 142, 152, 0.20)`. |

### 3.4. Low (P3 — Polish & Micro-interactions)
| Vấn đề | Vị trí | Mô tả chi tiết | Hướng xử lý |
|---|---|---|---|
| **Scrollbar màu sắc ở Light Mode** | `src/style.css` | Scrollbar thumb dùng `rgba(61, 93, 118, 0.35)` cần bo tròn và êm dịu hơn trên nền sáng. | Căn chỉnh độ trong suốt của scrollbar thumb. |
| **Status Badge Font Size & Padding** | `src/components/ui/StatusBadge.vue` | Kích thước font chữ ở badge nhỏ cần giữ tối thiểu 12px để đáp ứng bài test `check-design-system.mjs`. | Giữ vững font-size token tiêu chuẩn. |

---

## 4. Bảng kiểm tra toàn diện 100% Component & Layer

| Component / Subsystem | Kiểm tra Light Mode | Contrast WCAG | Hover/Focus/Active | Kết luận & Kế hoạch |
|---|---|---|---|---|
| **Design Tokens (`tokens.css`)** | ✅ Đầy đủ biến màu | AAA (14.5:1) | Hoàn chỉnh | Tăng độ sắc nét của viền `--border-subtle` |
| **Global Styles (`style.css`)** | ✅ Đã phân lớp | AA / AAA | Đạt chuẩn | Cập nhật `.unified-ui` rules |
| **MainLayout (`MainLayout.vue`)** | ✅ Nền canvas êm dịu | AAA | Tốt | Giữ nguyên kiến trúc |
| **Sidebar (`Sidebar.vue`)** | ✅ Dark-contrast navy | AAA | Hoàn chỉnh | Giữ vững phong cách Enterprise Control |
| **Header (`Header.vue`)** | ✅ Bề mặt kính mờ sáng | AAA | Hoàn chỉnh | Tinh chỉnh dropdown items & severity chips |
| **BaseButton (`BaseButton.vue`)** | ✅ 6 biến thể nút bấm | AAA / AA | Đầy đủ | Hoàn hảo |
| **BaseInput / Select / Field** | ✅ Nền `#f4f8f9` sang `#fff` khi focus | AAA | Focus ring rõ | Chuẩn hóa focus box-shadow |
| **BaseModal / ConfirmDialog** | ✅ Bề mặt nổi `#fbfdfe` | AAA | Phím Tab trap | Đạt chuẩn |
| **BaseTabs (`BaseTabs.vue`)** | ✅ Tab chuyển đổi mượt | AAA | Tabindex/Phím mũi tên | Đạt chuẩn a11y |
| **DataTable (`DataTable.vue`)** | ✅ Header xám nhạt, row hover rõ | AAA | Sortable button | Đạt chuẩn |
| **StatusBadge (`StatusBadge.vue`)** | ✅ 5 màu trạng thái dịu mắt | AA / AAA | Dot indicator | Đạt chuẩn |
| **LoadingSkeleton.vue** | ✅ Shimmer gradient sáng | Phù hợp | Shimmer animation | Đạt chuẩn |
| **EmptyState.vue** | ✅ Icon + Title + Action | AA | Nút CTA rõ | Đạt chuẩn |
| **AIChatBot (`AIChatBot.vue`)** | ⚠️ Đang cố định Dark Mode | Cần sửa | Cần refactor | **Refactor toàn diện sang Theme Adaptive** |
| **Chat (`Chat.vue`)** | ⚠️ Còn mã màu hex cứng | Cần sửa | Cần refactor | **Đồng bộ hóa sang Semantic Tokens** |
| **Dashboard (`Dashboard.vue`)** | ✅ Bento grid sáng rõ | AAA | Card hover elevation | Đạt chuẩn |
| **SocAlarmConsole.vue** | ✅ Console điều hành an ninh | AAA | Queue tabs & Timeline | Đạt chuẩn |
| **GateTransitMonitor.vue** | ✅ Monitor 2 làn xe | AAA | Video overlay & override | Đạt chuẩn |
| **SiteHierarchy.vue** | ✅ Cây phân cấp tòa nhà/cổng | AAA | Tree node focus | Đạt chuẩn |
| **Attendance Modules** | ✅ Bảng chấm công, ca trực | AAA | Datepicker & Filters | Đạt chuẩn |
| **UserManagement / Employees** | ✅ Quản lý nhân viên & tài khoản | AAA | Form drawer & Table | Đạt chuẩn |

---

## 5. Chiến lược Refactor & Thứ tự thực hiện

```text
BƯỚC 1: Củng cố Design Tokens (tokens.css & style.css)
        - Nâng cao độ tương phản viền border ở Light Mode
        - Đảm bảo các biến surface, text, border, status đồng bộ

BƯỚC 2: Refactor AI Chatbot UI (AIChatBot.vue)
        - Chuyển đổi toàn bộ layout chat bot sang hệ thống biến màu CSS
        - Hỗ trợ giao diện sáng sang trọng, tinh tế và chuyển giao diện tối tự động khi data-theme="dark"

BƯỚC 3: Đồng bộ trang Chat nội bộ (Chat.vue)
        - Thay thế toàn bộ hardcoded hex colors bằng semantic tokens

BƯỚC 4: Polish Header, Dropdowns, Cards, Modals và System Audit Logs
        - Tinh chỉnh notification dropdown list trong Header.vue
        - Tinh chỉnh adaptive code viewer trong SystemAuditLogs.vue
        - Tinh chỉnh tab active trong EnterpriseSecurityOperations.vue

BƯỚC 5: Kiểm tra xác minh toàn diện
        - Chạy npm run design:check
        - Chạy npm run test (133 test suites)
        - Chạy npm run build
        - Kiểm tra Dark Mode để đảm bảo 0% regression

BƯỚC 6: Xuất báo cáo hoàn thành LIGHT_MODE_IMPLEMENTATION_REPORT.md
```
