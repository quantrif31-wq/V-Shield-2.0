<script setup>
const roadmapPhases = [
  {
    phase: 'PHASE 01 // KHỞI TẠO NỀN TẢNG',
    time: 'THÁNG 03/2026',
    title: 'KIẾN TRÚC LÕI & QUẢN TRỊ DANH TÍNH CƠ BẢN',
    status: 'COMPLETED',
    desc: 'Thiết kế cơ sở dữ liệu phân cấp SQL Server, xây dựng ASP.NET Core Web API, phân quyền đa cấp JWT và quản lý hồ sơ nhân viên, lịch trực bảo vệ.',
    deliverables: [
      'Clean Architecture .NET 8 chuẩn mực',
      'Ma trận quản lý ca trực & chấm công chuẩn xác',
      'Phân quyền Admin / Quản Lý / Bảo Vệ / Lễ Tân / Nhân Sự'
    ]
  },
  {
    phase: 'PHASE 02 // TỰ ĐỘNG HÓA EDGE AI',
    time: 'THÁNG 05/2026',
    title: 'TÍCH HỢP AI FACE ID & ĐIỀU KHIỂN RÀO CHẮN TỰ ĐỘNG',
    status: 'COMPLETED',
    desc: 'Huấn luyện mô hình YOLOv11 + OCR nhận dạng biển số ANPR, kết nối luồng camera WebRTC 60 FPS sub-100ms và điều khiển tự động barie mở cổng an ninh.',
    deliverables: [
      'Nhận diện biển số xe ANPR thời gian thực',
      'Luồng Video RTSP/WebRTC sub-100ms độ trễ thấp',
      'Mô phỏng trạm kiểm soát phần hardware Virtual Barrier'
    ]
  },
  {
    phase: 'PHASE 03 // ĐỒNG BỘ LAI & QR ĐỘNG (HIỆN TẠI)',
    time: 'THÁNG 08/2026',
    title: 'HỆ SINH THÁI ĐA NỀN TẢNG & MÃ LƯỢNG TỬ TOTP',
    status: 'ACTIVE',
    desc: 'Phát hành ứng dụng Mobile Android APK, mã QR xoay vòng 30s chống chụp trộm, giao thức đồng bộ lai Hybrid Sync (Offline-First) và kênh thoại VoIP WebRTC.',
    deliverables: [
      'Giao thức Central-Area Node Hybrid Sync (<30ms)',
      'Mã QR TOTP HMAC-SHA256 kháng sao chép',
      'Đàm thoại Video Call hai chiều bảo vệ - nhân viên',
      'Mô hình phát hiện hành vi bất thường UEBA & SOC Console'
    ]
  },
  {
    phase: 'PHASE 04 // AI COPILOT & 3D DIGITAL TWIN',
    time: 'QUÝ 4/2026',
    title: 'TRỢ LÝ AI ĐIỀU HÀNH & BẢN ĐỒ SỐ 3D KHUÔN VIÊN',
    status: 'PLANNED',
    desc: 'Tích hợp trợ lý AI đàm thoại tự nhiên xử lý tình huống khẩn cấp, bản đồ số 3D Digital Twin không gian khuôn viên và điều phối an ninh tự hành.',
    deliverables: [
      'Trợ lý an ninh AI Security Copilot (LLM Multi-modal)',
      'Mô hình 3D Digital Twin không gian khuôn viên',
      'Tự động hóa cảnh báo tác chiến Telegram/SMS Gateway'
    ]
  }
]
</script>

<template>
  <div class="py-10 lg:py-16 font-mono">
    <div class="mx-auto max-w-5xl px-4 sm:px-6 lg:px-8 space-y-12">
      <!-- Header -->
      <div class="text-center space-y-3">
        <div class="inline-flex items-center gap-2 border border-amber-500/40 bg-[#121620] px-3.5 py-1 text-xs font-black text-amber-400 mecha-cut-tr">
          <span>// DEVELOPMENT TIMELINE & MILESTONES</span>
        </div>
        <h1 class="text-3xl sm:text-5xl font-black uppercase text-slate-100">
          LỊCH SỬ PHÁT TRIỂN & LỘ TRÌNH
        </h1>
        <p class="mx-auto max-w-2xl font-sans text-xs sm:text-sm text-slate-400 leading-relaxed">
          Quá trình nghiên cứu, xây dựng và thử nghiệm đề tài đồ án tốt nghiệp V-Shield 2.0 qua từng giai đoạn hoàn thiện hệ thống.
        </p>
      </div>

      <!-- Timeline Tree -->
      <div class="relative border-l-2 border-amber-500/40 pl-6 sm:pl-10 ml-4 sm:ml-8 space-y-12">
        <div
          v-for="(item, idx) in roadmapPhases"
          :key="idx"
          class="relative space-y-3"
        >
          <!-- Timeline Node Marker -->
          <div
            class="absolute -left-[31px] sm:-left-[47px] top-1 flex h-6 w-6 items-center justify-center border-2 bg-[#07080b]"
            :class="[
              item.status === 'ACTIVE'
                ? 'border-amber-400 shadow-[0_0_15px_#ffcc00]'
                : item.status === 'COMPLETED'
                  ? 'border-emerald-400 shadow-[0_0_15px_#10b981]'
                  : 'border-slate-600'
            ]"
          >
            <span
              class="h-2 w-2"
              :class="[
                item.status === 'ACTIVE'
                  ? 'bg-amber-400 animate-ping'
                  : item.status === 'COMPLETED'
                    ? 'bg-emerald-400'
                    : 'bg-slate-600'
              ]"
            ></span>
          </div>

          <!-- Phase Header Tag -->
          <div class="flex flex-wrap items-center gap-3">
            <span class="text-xs font-black uppercase text-amber-400">
              {{ item.phase }}
            </span>
            <span class="bg-[#121620] px-2 py-0.5 text-[10px] text-slate-400 border border-slate-700">
              {{ item.time }}
            </span>
            <span
              class="px-2 py-0.5 text-[10px] font-black uppercase tracking-wider"
              :class="[
                item.status === 'ACTIVE'
                  ? 'bg-amber-950 text-amber-300 border border-amber-500/40'
                  : item.status === 'COMPLETED'
                    ? 'bg-emerald-950 text-emerald-300 border border-emerald-500/40'
                    : 'bg-slate-900 text-slate-400 border border-slate-700'
              ]"
            >
              {{ item.status === 'ACTIVE' ? 'ĐANG TRIỂN KHAI (ACTIVE)' : item.status === 'COMPLETED' ? 'HOÀN THÀNH' : 'KẾ HOẠCH' }}
            </span>
          </div>

          <!-- Phase Content Card -->
          <div class="mecha-hud-bracket border border-amber-500/30 bg-[#0d1017] p-6 mecha-cut-tr transition hover:border-amber-400 space-y-3">
            <h3 class="text-base sm:text-lg font-black text-slate-100 uppercase">
              {{ item.title }}
            </h3>
            <p class="font-sans text-xs text-slate-300 leading-relaxed">
              {{ item.desc }}
            </p>

            <div class="space-y-1.5 border-t border-slate-800 pt-3">
              <div class="text-[10px] font-bold text-amber-400 uppercase tracking-wider">
                KẾT QUẢ ĐẠT ĐƯỢC:
              </div>
              <ul class="space-y-1 text-xs text-slate-300">
                <li v-for="(del, dIdx) in item.deliverables" :key="dIdx" class="flex items-center gap-2">
                  <span class="text-amber-400 font-bold">»</span>
                  <span class="font-sans">{{ del }}</span>
                </li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
