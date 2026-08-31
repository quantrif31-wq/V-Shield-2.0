<script setup>
import { ref } from 'vue'

const roadmapMilestones = [
  {
    version: 'v1.0.0 (Genesis Phase)',
    time: 'Tháng 03/2026',
    title: 'Nền Tảng Kiểm Soát Cơ Bản & Quản Trị Trung Tâm',
    status: 'Completed',
    desc: 'Thiết kế cơ sở dữ liệu SQL Server, triển khai ASP.NET Core Web API, xác thực JWT đa quyền và quản lý nhân viên, lịch làm việc, khách đăng ký trước.',
    deliverables: [
      'Kiến trúc Clean Architecture .NET 8',
      'Hệ thống quản lý chấm công & ca trực',
      'Phân quyền Admin / Quản Lý / Bảo Vệ / Lễ Tân / Nhân Sự'
    ]
  },
  {
    version: 'v1.5.0 (Automation Phase)',
    time: 'Tháng 05/2026',
    title: 'Tích Hợp AI Edge & Rào Chắn Tự Động',
    status: 'Completed',
    desc: 'Tích hợp mô hình AI YOLOv11 + OCR nhận diện biển số xe máy / ô tô, kết nối luồng camera Go2RTC WebRTC 60 FPS và điều khiển tự động barie đóng mở.',
    deliverables: [
      'Nhận diện biển số xe ANPR thời gian thực',
      'Luồng Video RTSP/WebRTC sub-100ms',
      'Mô phỏng trạm kiểm soát phần cứng Virtual Barrier'
    ]
  },
  {
    version: 'v2.0.0 (Neural Realtime Defense - Hiện tại)',
    time: 'Tháng 08/2026',
    title: 'Hệ Sinh Thái Đa Nền Tảng, Hybrid Sync & Mã QR TOTP',
    status: 'Active',
    desc: 'Phát hành ứng dụng Mobile Android (APK), mã QR xoay vòng liên tục chống gian lận, giao thức đồng bộ lai Cloud-Local (Offline-First) và tổng đài VoIP Video Call WebRTC.',
    deliverables: [
      'Giao thức Central-Area Node Hybrid Sync',
      'Mã QR TOTP SHA-256 chống chụp màn hình',
      'Đàm thoại Video Call hai chiều bảo vệ - nhân viên',
      'Mô hình phát hiện hành vi bất thường UEBA & SOC Console'
    ]
  },
  {
    version: 'v2.5.0 (Autonomous AI Era - Kế hoạch tương lai)',
    time: 'Quý 4/2026',
    title: 'Mô Hình Ngôn Ngữ Lớn (LLM Agent) & Bản Đồ Số 3D Twin',
    status: 'Planned',
    desc: 'Tích hợp trợ lý AI đàm thoại tự nhiên xử lý tình huống khẩn cấp, bản đồ số 3D không gian tòa nhà (Digital Twin 3D) và điều phối an ninh tự hành qua máy bay không người lái (Drone Edge).',
    deliverables: [
      'Trợ lý an ninh AI Security Copilot (LLM Multi-modal)',
      'Mô hình 3D Digital Twin không gian khuôn viên',
      'Tự động hóa cảnh báo đa kênh Telegram/SMS/Zalo Gateway'
    ]
  }
]
</script>

<template>
  <div class="py-12 lg:py-16">
    <div class="mx-auto max-w-5xl px-4 sm:px-6 lg:px-8 space-y-12">
      <!-- Header -->
      <div class="text-center space-y-3">
        <div class="inline-flex items-center gap-2 rounded-full border border-pink-500/30 bg-pink-950/40 px-3 py-1 text-xs font-bold text-pink-300 font-mono">
          <span>CYBER TIMELINE & EVOLUTION</span>
        </div>
        <h1 class="text-3xl sm:text-5xl font-extrabold tracking-tight text-slate-100 font-mono">
          Lịch Sử Phát Triển & Lộ Trình
        </h1>
        <p class="mx-auto max-w-2xl text-xs sm:text-sm text-slate-400 leading-relaxed">
          Hành trình nghiên cứu, xây dựng và hoàn thiện đề tài đồ án tốt nghiệp V-Shield 2.0 từ phiên bản khởi nguyên tới hệ sinh thái bảo mật thông minh thế hệ mới.
        </p>
      </div>

      <!-- Timeline Tree -->
      <div class="relative border-l-2 border-cyan-500/30 pl-6 sm:pl-10 ml-4 sm:ml-8 space-y-12">
        <div
          v-for="(item, idx) in roadmapMilestones"
          :key="idx"
          class="relative space-y-3"
        >
          <!-- Timeline Glowing Node Dot -->
          <div
            class="absolute -left-[31px] sm:-left-[47px] top-1.5 flex h-6 w-6 items-center justify-center rounded-full border-2 bg-slate-950"
            :class="[
              item.status === 'Active'
                ? 'border-pink-400 shadow-[0_0_15px_#ff2a85]'
                : item.status === 'Completed'
                  ? 'border-cyan-400 shadow-[0_0_15px_#00f0ff]'
                  : 'border-slate-600'
            ]"
          >
            <span
              class="h-2 w-2 rounded-full"
              :class="[
                item.status === 'Active'
                  ? 'bg-pink-400 animate-ping'
                  : item.status === 'Completed'
                    ? 'bg-cyan-400'
                    : 'bg-slate-600'
              ]"
            ></span>
          </div>

          <!-- Version & Status Tag -->
          <div class="flex flex-wrap items-center gap-3">
            <span class="font-mono text-xs font-black uppercase text-cyan-300">
              {{ item.version }}
            </span>
            <span class="rounded bg-slate-800 px-2 py-0.5 text-[10px] font-mono text-slate-400">
              {{ item.time }}
            </span>
            <span
              class="rounded-full px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider"
              :class="[
                item.status === 'Active'
                  ? 'bg-pink-950 text-pink-300 border border-pink-500/40 shadow-[0_0_10px_rgba(255,42,133,0.3)]'
                  : item.status === 'Completed'
                    ? 'bg-emerald-950 text-emerald-300 border border-emerald-500/40'
                    : 'bg-slate-900 text-slate-400 border border-slate-700'
              ]"
            >
              {{ item.status === 'Active' ? 'Đang Hoạt Động (Current)' : item.status === 'Completed' ? 'Đã Hoàn Thành' : 'Kế Hoạch' }}
            </span>
          </div>

          <!-- Title & Content Card -->
          <div class="rounded-2xl border border-slate-800 bg-slate-900/60 p-6 backdrop-blur-xl transition hover:border-cyan-500/40">
            <h3 class="text-lg font-bold text-slate-100 font-mono">
              {{ item.title }}
            </h3>
            <p class="mt-2 text-xs leading-relaxed text-slate-300">
              {{ item.desc }}
            </p>

            <div class="mt-4 space-y-1.5 border-t border-slate-800/80 pt-3">
              <div class="text-[11px] font-bold text-slate-400 uppercase tracking-wider font-mono">
                Các kết quả đạt được:
              </div>
              <ul class="space-y-1 text-xs text-slate-300">
                <li v-for="(del, dIdx) in item.deliverables" :key="dIdx" class="flex items-center gap-2">
                  <span class="text-cyan-400 font-bold">✓</span>
                  <span>{{ del }}</span>
                </li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
