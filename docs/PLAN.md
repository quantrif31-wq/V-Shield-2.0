# Kế Hoạch Cải Tạo GuideViewer — Hướng Dẫn Trực Quan, Tiếng Việt, Step-by-Step

## 🎯 Mục Tiêu

Tạo lại trang `/guide` từ **gốc** để đáp ứng 3 yêu cầu:

| Yêu cầu | Hiện tại | Mục tiêu |
|----------|----------|----------|
| **Ngôn ngữ** | Pha tiếng Anh + thuật ngữ kỹ thuật (confidence, severity, chain of custody...) | **100% tiếng Việt**, giải thích cho người ngoài ngành hiểu |
| **Hướng dẫn** | Danh sách nút/input khô khan, không có luồng | **Step-by-step**: Mục đích → Các bước làm → Giải thích từng thứ |
| **Trực quan** | Phải scroll xuống chọn dropdown mới thấy chi tiết | **Hiển thị ngay tại chỗ**: card mở rộng, icon, màu sắc, tabs |

---

## 🔴 Vấn Đề Cốt Lõi Của Giao Diện Hiện Tại

### 1. Section "Chi tiết chức năng" (features)
- Dùng `<select>` dropdown để chọn trang → phải scroll xuống, click mở, tìm kiếm
- Sau khi chọn, hiển thị dạng list dài → khó đọc, thiếu cấu trúc
- Mỗi mục chỉ có: `Nút / Ô nhập / Bảng` + tên + mô tả ngắn — không giải thích "tại sao phải làm cái này trước"
- Dùng nhiều từ tiếng Anh: `Metric tiles`, `Event type filter`, `Outbox Events`...

### 2. Section "Danh mục trang" (pages)
- Chỉ hiển thị card nhỏ với tên + icon + vai trò
- Thiếu thông tin: không biết trang đó để làm gì, khi nào cần dùng

### 3. Section "Luồng hoạt động" (workflow)
- Chỉ có workflow tổng quan cho 4 vai trò, chưa có hướng dẫn chi tiết từng thao tác

---

## ✅ Kế Hoạch Cải Tạo (Viết Lại Hoàn Toàn GuideViewer.vue)

### Phase 1: Cấu Trúc Giao Diện Mới

**Layout:**
```
┌──────────────────────────────────────────────────┐
│ HERO: "Hướng dẫn sử dụng V-Shield"              │
│ 4 chip vai trò: Admin | Bảo vệ | Quản lý | NV   │
├──────────────────────────────────────────────────┤
│ [Tổng quan] [Nhóm trang] [Chi tiết] [FAQ]       │ ← Tabs lớn đầu trang
├──────────────────────────────────────────────────┤
│                                                  │
│  NỘI DUNG CHÍNH (không cần sidebar)              │
│  - Hiển thị full-width, dễ đọc                   │
│  - Mỗi trang là 1 card lớn, có thể mở rộng       │
│  - Click vào trang → xổ xuống hướng dẫn step     │
│                                                  │
└──────────────────────────────────────────────────┘
```

**Thay đổi cụ thể:**
1. **Bỏ sidebar navigation** — chuyển thành tabs ngang ở đầu trang
2. **Bỏ section "Danh mục trang" riêng** — gộp vào section "Chi tiết" luôn
3. **Mỗi trang là 1 card accordion** — click để mở rộng, hiển thị hướng dẫn step-by-step
4. **Không cần dropdown chọn trang** — tất cả pages hiển thị dạng lưới card

### Phase 2: Nội Dung Mới (100% Tiếng Việt, Step-by-Step)

**Format mới cho mỗi trang:**
```markdown
## [Tên trang] 🏠

### 📌 Trang này dùng để làm gì?
Giải thích bằng tiếng Việt đơn giản, không thuật ngữ.
Ví dụ: "Trang Dashboard là trang đầu tiên bạn thấy sau khi đăng nhập. 
Nó cho bạn biết tổng quan tình hình: có bao nhiêu xe trong bãi, 
bao nhiêu khách đang đến, có ngoại lệ nào không..."

### 📋 Các bước thực hiện

**Bước 1: [Tên bước]**
→ Mô tả: "Nhìn vào góc trên bên trái màn hình, bạn sẽ thấy..."
→ Nhập gì: "Ô nhập [tên] - bạn gõ [giá trị] vào đây"
→ Nhấn gì: "Sau đó bấm nút [tên] (màu xanh, có chữ [nhãn])"
→ Kết quả: "Một cửa sổ mới hiện ra, bên trong có..."

**Bước 2: ...**
**Bước 3: ...**

### 🔍 Giải thích các thành phần trên trang

| Thành phần | Ý nghĩa | Ghi chú |
|------------|---------|---------|
| Ô nhập "Tên" | Gõ họ tên người dùng vào đây | Bắt buộc |
| Nút "Tìm kiếm" (xanh) | Bấm để lọc danh sách | Phải nhập từ khóa trước |
| Bảng danh sách | Hiển thị kết quả | Có thể bấm vào dòng để xem chi tiết |
```
(Nội dung này sẽ được lưu trong file `GuideViewerData.js` riêng — không nhét vào .vue)

### Phase 3: File Structure Mới

```
View/src/
├── pages/
│   └── GuideViewer.vue          ← VIẾT LẠI (chỉ template + logic)
├── components/
│   └── guide/
│       ├── GuideHero.vue        ← Header hero + chọn vai trò
│       ├── GuideTabs.vue        ← tabs điều hướng
│       ├── GuidePageCard.vue    ← Card từng trang (accordion)
│       ├── GuideStepList.vue    ← Danh sách bước step-by-step
│       └── GuideTableExplain.vue ← Bảng giải thích thành phần
├── data/
│   └── guideData.js             ← TOÀN BỘ nội dung tiếng Việt
```

**Lợi ích:** Tách nội dung ra khỏi code → dễ chỉnh sửa, dễ mở rộng, dễ đọc.

### Phase 4: Nội Dung Mỗi Trang (45+ trang, 100% tiếng Việt)

**Danh sách các trang cần viết hướng dẫn:**

#### Nhóm 1: Tổng quan & Giám sát
| Trang | Nội dung chính |
|-------|----------------|
| **Dashboard** | Tổng quan: xe trong bãi, khách hẹn, ngoại lệ. Các nút tắt: "Giám sát", "Tra cứu" |
| **Giám sát trực tiếp** | Xem camera. Nhập URL → Bật → Xem 4 luồng |
| **Tra cứu vào/ra** | Lọc lịch sử. Nhập từ khóa → chọn ngày → chọn cổng → bấm Áp dụng |
| **UEBA** | Phân tích bất thường. Xem tổng quan → Lọc bất thường → Xử lý |

#### Nhóm 2: SOC & Enterprise
| Trang | Nội dung chính |
|-------|----------------|
| **SOC Alarm Console** | Xử lý cảnh báo an ninh. Chọn tab → Xem alarm → Acknowledge/Assign/Close |
| **Identity Management** | Quản lý tài khoản từ xa. Thêm provider OIDC → Import user → Offboard |
| **Site Hierarchy** | Cây phân cấp công ty. Chọn node → Thêm node con → Backfill dữ liệu |
| **Policy Engine** | Tạo chính sách ra vào. Tạo version → Submit → Approve → Activate |
| **Enterprise Console** | Điều khiển trung tâm. Workspace tabs → Step-up verify → NL Query |

#### Nhóm 3: Quản lý Khách
| Trang | Nội dung chính |
|-------|----------------|
| **Đăng ký trước** | Duyệt đơn khách. Lọc trạng thái → Click duyệt/từ chối → Xem QR |
| **Link đăng ký** | Tạo link mời. Chọn host → Chọn giờ → Tạo → Copy link gửi |
| **Hồ sơ khách** | Danh bạ khách quen. Tìm kiếm → Sửa/Xem lịch sử/Xóa |
| **Reception** | Lễ tân. Walk-in check-in → Xem overstays → Review watchlist |
| **Kiosk Check-in** | Khách tự check-in. Gõ tên → Xác nhận → Check-in |
| **Mời khách** | Host mời khách. Điền tên khách → Chọn giờ → Bấm Gửi |
| **Watchlist** | Danh sách theo dõi. Add entry → Review match → Confirm/FP |

#### Nhóm 4: AI & Thiết bị
| Trang | Nội dung chính |
|-------|----------------|
| **Face ID** | Nhận diện khuôn mặt. Nhập URL camera → Bật preview → Nhận diện |
| **Video khuôn mặt** | Xem camera face. Bật camera → Chụp ảnh → Xem log |
| **Nhận diện biển số** | ANPR. Kết nối camera → Chụp → Xem biển số |
| **Gate Transit** | Điều phối thông hành. Chọn làn → Xem 3 luồng (Face + Plate + QR) |
| **Tạo QR động** | Tạo QR realtime. Nhập Employee ID → Phát QR → Làm mới |
| **Quét QR động** | Quét QR tại cổng. Bật camera → Đưa QR → Xác thực |

#### Nhóm 5: Giao thông & Bãi đỗ
| Trang | Nội dung chính |
|-------|----------------|
| **Lane Dashboard** | Trạng thái làn đường. Xem health → Xem sự kiện gần nhất |
| **Barrier Control** | Điều khiển barrier. Chọn barrier → Open/Close → Nhập lý do |
| **Plate Review** | Duyệt biển số AI. Lọc status → Review → Confirm/FP |

#### Nhóm 6: Evidence & Compliance
| Trang | Nội dung chính |
|-------|----------------|
| **Evidence Repository** | Kho vật chứng. Lọc type/privacy → Xem detail → Custody timeline |
| **Export Approval** | Duyệt xuất evidence. Xem yêu cầu → Approve/Reject |
| **Redaction Queue** | Xóa thông tin nhạy cảm. Xem danh sách → Redact → Verify |
| **Retention Dashboard** | Chính sách lưu giữ. Xem policy → Legal hold → Purge |
| **Compliance Reports** | Báo cáo tuân thủ. Generate → Download |

#### Nhóm 7: Video & AI Review
| Trang | Nội dung chính |
|-------|----------------|
| **Event Timeline** | Dòng thời gian sự kiện. Lọc type/severity → Click event → Xem detail |
| **Video Search** | Tìm kiếm video. Bookmark → Clip request → Approve/Export |
| **AI Review Queue** | Đánh giá AI. Review → Confirm/Reject |
| **Correlation View** | Tương quan tín hiệu. Xem face + plate + event cùng lúc |

#### Nhóm 8: Thiết bị & Hạ tầng
| Trang | Nội dung chính |
|-------|----------------|
| **Camera & cổng** | Quản lý thiết bị. Thêm → Cấu hình → Test kết nối |
| **Device Topology** | Sơ đồ mạng. Xem graph → Trạng thái kết nối |
| **Device Health** | Sức khỏe thiết bị. Xem metrics → AI diagnosis |
| **Provisioning** | Cấp phát thiết bị. Wizard step → Bulk registration |
| **Offline Packages** | Gói offline. Xem danh sách → Sync |
| **Simulator** | Mô phỏng thiết bị. Tạo controller → Inject fault |
| **Biometrics** | Dữ liệu nhận diện. Xem coverage → Train model |

#### Nhóm 9: Khác
| Trang | Nội dung chính |
|-------|----------------|
| **Đăng nhập** | Nhập user/pass → Bấm Đăng nhập → MFA nếu cần |
| **Cài đặt** | Cấu hình hệ thống. 4 tab: Chung → Camera → AI → Cảnh báo |
| **Ngoại lệ** | Xử lý lỗi ra vào. Lọc lý do → Xem danh sách → Ghi chú |
| **Nhân viên** | Quản lý nhân sự. Thêm → Import → Export → Sửa/Xóa |
| **Phương tiện** | Quản lý xe. Đăng ký → Tìm biển số → Gán chủ |
| **Bản đồ khuôn viên** | Xem bản đồ tương tác. Zoom/Pan → Trạng thái realtime |
| **Outbox** | Hàng đợi gửi. Filter → Retry/Dispatch |
| **Operations Dashboard** | Vận hành. Outbox → Backup → Security checks |
| **SIEM Export** | Xuất SIEM. Trigger export → Xem trạng thái |

### Phase 5: Nâng Cao UX

**Các tính năng mới:**
1. **Accordion cards** — Mỗi trang là 1 card, click để mở rộng
2. **Filter nhanh** — 2 chế độ: lọc theo nhóm trang (dropdown) hoặc search text
3. **Role filter** — Chỉ hiển thị trang cho vai trò đang chọn, trang không có quyền sẽ mờ đi
4. **"Tôi cần làm gì?"** — Phần mềm gợi ý: "Nếu bạn muốn tạo link mời khách → click vào đây"
5. **Responsive** — Trên mobile, accordion full-width, tabs thành scroll ngang
6. **Icons màu sắc** — Mỗi trang có icon + màu riêng theo nhóm
7. **Trạng thái "đã đọc"** — Trang nào đã mở sẽ đánh dấu

### Phase 6: Timeline

| Phase | Nội dung | Kích thước | Thời gian dự kiến |
|-------|----------|-----------|-------------------|
| **1** | Tạo `guideData.js` với nội dung tiếng Việt cho 10 trang đầu | ~500 dòng | 1 buổi |
| **2** | Viết `GuidePageCard.vue` + `GuideStepList.vue` (accordion) | ~300 dòng | 1 buổi |
| **3** | Viết lại `GuideViewer.vue` với layout tabs mới | ~400 dòng | 1 buổi |
| **4** | Viết nội dung 35 trang còn lại trong `guideData.js` | ~1500 dòng | 2 buổi |
| **5** | CSS responsive, animation, polish | ~200 dòng | 1 buổi |
| **6** | Build, review, fix, commit | - | 1 buổi |

**Tổng:** ~7-8 buổi

---

## 📐 Mẫu Giao Diện Mới (Text Mockup)

```
┌──────────────────────────────────────────────────────────────────┐
│ 📖 HƯỚNG DẪN SỬ DỤNG V-SHIELD                                 │
│ Dành cho: [Tất cả] [Admin] [Bảo vệ] [Quản lý] [Nhân viên]     │
├──────────────────────────────────────────────────────────────────┤
│ [Tổng quan]  [Tất cả trang]  [FAQ]                              │ ← Tabs
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│ 🔍 [Tìm trang...                                     ]          │
│ 📂 Tất cả nhóm [▼]                                                │
│                                                                  │
│ ┌─ TỔNG QUAN & GIÁM SÁT ──────────────────────────────────────┐ │
│ │                                                              │ │
│ │ [🏠] Dashboard                           Admin Bảo vệ QLý  │ │
│ │      Xem tổng quan: xe, khách, ngoại lệ, biểu đồ           │ │
│ │      ▶ [Click để xem hướng dẫn]                             │ │
│ │                                                              │ │
│ │ [📹] Giám sát trực tiếp                   Admin Bảo vệ QLý  │ │
│ │      Xem camera, tối đa 4 luồng cùng lúc                    │ │
│ │      ▶ [Click để xem hướng dẫn]                             │ │
│ │                                                              │ │
│ └──────────────────────────────────────────────────────────────┘ │
│                                                                  │
│ ┌─ QUẢN LÝ KHÁCH ─────────────────────────────────────────────┐ │
│ │                                                              │ │
│ │ [📋] Đăng ký trước                       Admin               │ │
│ │      ▶ [Click để xem hướng dẫn]                             │ │
│ │   KHI MỞ RỘNG:                                              │ │
│ │   ┌────────────────────────────────────────────────────┐     │ │
│ │   │ 📌 Trang này dùng để làm gì?                       │     │ │
│ │   │ Dùng để duyệt hoặc từ chối đơn đăng ký trước của   │     │ │
│ │   │ khách. Khi khách đăng ký qua link mời, đơn sẽ hiện  │     │ │
│ │   │ ở đây để Admin xử lý.                              │     │ │
│ │   │                                                    │     │ │
│ │   │ 📋 Các bước thực hiện                              │     │ │
│ │   │                                                    │     │ │
│ │   │ Bước 1: Xem danh sách đơn                          │     │ │
│ │   │   → Trên màn hình bạn thấy 4 ô thống kê:            │     │ │
│ │   │     [Tổng đơn] [Chờ duyệt] [Đã duyệt] [Từ chối]   │     │ │
│ │   │   → Bên dưới là bảng danh sách các đơn              │     │ │
│ │   │                                                    │     │ │
│ │   │ Bước 2: Lọc đơn cần xử lý                          │     │ │
│ │   │   → Ô nhập "Tìm khách": gõ tên hoặc SĐT khách      │     │ │
│ │   │   → Chọn "Chờ duyệt" trong ô "Lọc trạng thái"      │     │ │
│ │   │                                                    │     │ │
│ │   │ Bước 3: Xem chi tiết đơn                           │     │ │
│ │   │   → Bấm vào icon 👁️ (con mắt) ở cột "Hành động"    │     │ │
│ │   │   → Một cửa sổ hiện ra với thông tin đầy đủ:        │     │ │
│ │   │     Tên khách, Host, Thời gian, Đoàn đi cùng       │     │ │
│ │   │                                                    │     │ │
│ │   │ Bước 4: Duyệt hoặc từ chối                         │     │ │
│ │   │   → Nếu đồng ý: bấm nút ✓ (xanh) ở cột Hành động   │     │ │
│ │   │   → Nếu từ chối: bấm nút ✕ (đỏ)                    │     │ │
│ │   │                                                    │     │ │
│ │   │ 🔍 Giải thích các thành phần                        │     │ │
│ │   │                                                    │     │ │
│ │   │ | Thành phần | Ý nghĩa |                            │     │ │
│ │   │ |------------|---------|                            │     │ │
│ │   │ | "Tìm khách" | Gõ tên hoặc SĐT để lọc |           │     │ │
│ │   │ | "Tạo link đăng ký" | Mở cửa sổ tạo link |        │     │ │
│ │   │ | Bảng danh sách | Mỗi dòng là 1 đơn đăng ký |     │     │ │
│ │   │                                                     │     │ │
│ │   └────────────────────────────────────────────────────┘     │ │
│ │                                                              │ │
│ └──────────────────────────────────────────────────────────────┘ │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

---

## ✅ Check List Implement

- [ ] Phase 1: Tạo `guideData.js` — nội dung tiếng Việt 10 trang đầu (mẫu)
- [ ] Phase 2: Tạo `GuidePageCard.vue` — accordion card component
- [ ] Phase 3: Tạo `GuideStepList.vue` — step-by-step component
- [ ] Phase 4: Viết lại `GuideViewer.vue` — layout tabs mới
- [ ] Phase 5: Viết nội dung 45+ trang trong `guideData.js`
- [ ] Phase 6: CSS responsive + animation
- [ ] Phase 7: Build, review, fix, commit
