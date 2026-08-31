<script setup>
import PortalFoundersTurntable from '../../components/portal/PortalFoundersTurntable.vue'

const techArsenal = [
  { name: 'ASP.NET Core 8.0', type: 'Backend Core Engine', desc: 'REST API, SignalR Hub, EF Core' },
  { name: 'Vue.js 3 + Vite', type: 'Frontend HUD SPA', desc: 'Composition API, Tailwind CSS, Pinia' },
  { name: 'Python + OpenCV + YOLO', type: 'Optical AI Vision', desc: 'Face ID & ANPR License Plate' },
  { name: 'SQL Server 2022', type: 'Enterprise Database', desc: 'Relational Database & Audit Trail' },
  { name: 'Docker & Compose', type: 'Containerization', desc: 'Isolated Microservices & Multi-stage' },
  { name: 'Caddy & Nginx', type: 'Reverse Proxy & TLS', desc: 'Auto TLS, Load Balancing, Caching' },
  { name: 'WebRTC & Go2RTC', type: 'Low-Latency Media', desc: 'Low-latency Video & VoIP Intercom' }
]
</script>

<template>
  <div class="py-10 lg:py-16 font-mono">
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 space-y-16">
      <!-- Header -->
      <div class="text-center space-y-3">
        <div class="mecha-laser-border inline-flex items-center gap-2 border border-amber-500/40 bg-[#121620] px-3.5 py-1 text-xs font-black text-amber-400 mecha-cut-tr">
          <span>// ENGINEERING TEAM & THESIS LAB</span>
        </div>
        <h1 class="mecha-glitch-hover cursor-default text-3xl sm:text-5xl font-black uppercase text-slate-100">
          HỘI SÁNG LẬP & CỐ VẤN CHIẾN LƯỢC
        </h1>
        <p class="mx-auto max-w-2xl font-sans text-xs sm:text-sm text-slate-400 leading-relaxed">
          Đề án tốt nghiệp Kỹ sư: <strong>"Hệ thống kiểm soát an ninh thông minh đa nền tảng kết hợp AI sinh trắc học và giao thức đồng bộ lai V-Shield 2.0"</strong>.
        </p>
      </div>

      <!-- Chief Advisor Card -->
      <div class="mecha-hud-bracket mx-auto max-w-3xl border-2 border-amber-500/40 bg-[#0c0f15] p-8 mecha-cut-corners shadow-[0_0_40px_rgba(255,204,0,0.2)]">
        <div class="flex flex-col sm:flex-row items-center gap-6 text-center sm:text-left">
          <div class="relative flex h-24 w-24 shrink-0 items-center justify-center border-2 border-amber-400 bg-[#07080b] mecha-cut-tr shadow-[0_0_25px_rgba(255,204,0,0.5)]">
            <span class="text-4xl">🎖️</span>
          </div>
          <div class="space-y-1.5">
            <div class="text-xs font-black uppercase text-amber-400">
              GIẢNG VIÊN HƯỚNG DẪN ĐỀ TÀI
            </div>
            <h3 class="text-2xl font-black text-slate-100">
              ThS. Phan Hoàng Khải
            </h3>
            <p class="font-sans text-xs text-slate-300 leading-relaxed pt-1">
              Khoa Công Nghệ Thông Tin & An Toàn Thông Tin. Định hướng nghiên cứu kiến trúc hệ thống phân tán, kiểm toán an ninh bảo mật và phương pháp khoa học cho đề tài.
            </p>
          </div>
        </div>
      </div>

      <!-- ── 3D TURNTABLE OF 5 FOUNDERS CHAMPIONS ── -->
      <PortalFoundersTurntable />

      <!-- Tech Stack Matrix -->
      <div class="space-y-6">
        <h2 class="text-2xl font-black uppercase text-slate-100 text-center">
          KHO VŨ KHÍ CÔNG NGHỆ (TECH STACK)
        </h2>

        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <div
            v-for="(tech, tIdx) in techArsenal"
            :key="tIdx"
            class="mecha-hud-bracket border border-slate-800 bg-[#0c0f15] p-4 space-y-1 mecha-cut-tr transition hover:border-amber-400 hover:shadow-[0_0_20px_rgba(255,204,0,0.2)]"
          >
            <div class="text-xs font-black text-amber-400">{{ tech.name }}</div>
            <div class="text-[10px] font-bold text-orange-400">{{ tech.type }}</div>
            <p class="font-sans text-[11px] text-slate-400">{{ tech.desc }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
