<script setup>
const squadPilots = [
  {
    name: 'Phạm Văn Thành',
    callsign: 'LEAD ARCHITECT // CALLSIGN: VANGUARD-01',
    role: 'Team Lead & Backend Architect',
    avatar: 'https://api.dicebear.com/7.x/adventurer/svg?seed=ThanhLead',
    classId: 'K65-ATTT',
    duties: 'Kiến trúc lõi .NET 8, thuật toán Hybrid Sync đa trạm, giải mã mã lượng tử TOTP và phân quyền JWT.',
    stats: { code: 98, defense: 95, sync: 96 }
  },
  {
    name: 'Hà Mạnh Hùng',
    callsign: 'AI VISION SPECIALIST // CALLSIGN: STRIKER-02',
    role: 'AI & Computer Vision Engineer',
    avatar: 'https://api.dicebear.com/7.x/adventurer/svg?seed=HungAI',
    classId: 'K65-CNTT',
    duties: 'Huấn luyện mạng nơ-ron YOLOv11 + ArcFace nhận diện khuôn mặt 60 FPS và OCR biển số xe quân sự ANPR.',
    stats: { code: 94, defense: 90, sync: 92 }
  },
  {
    name: 'Phạm Ngọc Hoài Anh',
    callsign: 'DEVOPS OPS // CALLSIGN: CYBER-FORTRESS-03',
    role: 'DevOps & Cloud Infrastructure',
    avatar: 'https://api.dicebear.com/7.x/adventurer/svg?seed=HoaiAnh',
    classId: 'K65-KTPM',
    duties: 'Đóng gói Docker Compose vi mô, Reverse Proxy Caddy SSL tự hành, tối ưu hóa bộ nhớ VPS và CI/CD.',
    stats: { code: 92, defense: 96, sync: 95 }
  },
  {
    name: 'Vũ Tiến Đạt',
    callsign: 'HUD INTERFACE // CALLSIGN: NEON-SPECTRE-04',
    role: 'Frontend UI/UX & Realtime WebRTC',
    avatar: 'https://api.dicebear.com/7.x/adventurer/svg?seed=DatFrontend',
    classId: 'K65-CNTT',
    duties: 'Xây dựng giao diện Vue 3 Mecha Tactical, luồng Video Call WebRTC VoIP quân sự và MapLibre 3D.',
    stats: { code: 95, defense: 88, sync: 91 }
  },
  {
    name: 'Nguyễn Quốc Việt',
    callsign: 'EDGE HARDWARE // CALLSIGN: TITAN-ARMOR-05',
    role: 'Mobile Android & Edge Hardware IoT',
    avatar: 'https://api.dicebear.com/7.x/adventurer/svg?seed=VietMobile',
    classId: 'K65-ATTT',
    duties: 'Phát triển ứng dụng Mobile Field App APK, giao tiếp rào chắn Barie động lực và kiểm thử an ninh UEBA.',
    stats: { code: 91, defense: 94, sync: 90 }
  }
]

const techArsenal = [
  { name: 'ASP.NET Core 8.0', type: 'Backend Core Engine', desc: 'REST API, SignalR Hub, EF Core' },
  { name: 'Vue.js 3 + Vite', type: 'Cockpit HUD SPA', desc: 'Composition API, Tailwind CSS, Pinia' },
  { name: 'Python + OpenCV + YOLO', type: 'Optical AI Radar', desc: 'Face ID & ANPR License Plate' },
  { name: 'SQL Server 2022', type: 'Defense Database', desc: 'Relational Database & Audit Logs' },
  { name: 'Docker & Compose', type: 'Armor Containerization', desc: 'Isolated Microservices & Multi-stage' },
  { name: 'Caddy & Nginx', type: 'Tactical Gateway', desc: 'Auto TLS, Load Balancing, Caching' },
  { name: 'WebRTC & Go2RTC', type: 'Comm Link Stream', desc: 'Low-latency Video & VoIP Intercom' }
]
</script>

<template>
  <div class="py-10 lg:py-16 font-mono">
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 space-y-16">
      <!-- Header -->
      <div class="text-center space-y-3">
        <div class="inline-flex items-center gap-2 border border-amber-500/40 bg-[#121620] px-3.5 py-1 text-xs font-black text-amber-400 mecha-cut-tr">
          <span>// SQUAD PILOTS & COMMAND HIERARCHY</span>
        </div>
        <h1 class="text-3xl sm:text-5xl font-black uppercase text-slate-100">
          ĐỘI NGŨ TÁC CHIẾN & CHỈ HUY
        </h1>
        <p class="mx-auto max-w-2xl font-sans text-xs sm:text-sm text-slate-400 leading-relaxed">
          Đề án tốt nghiệp Kỹ sư: <strong>"Hệ thống kiểm soát an ninh thông minh đa nền tảng kết hợp AI sinh trắc học và giao thức đồng bộ lai V-Shield MK-II"</strong>.
        </p>
      </div>

      <!-- Chief Commander Advisor Card -->
      <div class="mecha-hud-bracket mx-auto max-w-3xl border-2 border-amber-500/40 bg-[#0c0f15] p-8 mecha-cut-corners shadow-[0_0_40px_rgba(255,204,0,0.2)]">
        <div class="flex flex-col sm:flex-row items-center gap-6 text-center sm:text-left">
          <div class="relative flex h-24 w-24 shrink-0 items-center justify-center border-2 border-amber-400 bg-[#07080b] mecha-cut-tr shadow-[0_0_25px_rgba(255,204,0,0.5)]">
            <span class="text-4xl">🎖️</span>
          </div>
          <div class="space-y-1.5">
            <div class="text-xs font-black uppercase text-amber-400">
              CHIEF COMMANDER // GIẢNG VIÊN HƯỚNG DẪN
            </div>
            <h3 class="text-2xl font-black text-slate-100">
              ThS. Phan Hoàng Khải
            </h3>
            <p class="font-sans text-xs text-slate-300 leading-relaxed pt-1">
              Khoa Công Nghệ Thông Tin & An Toàn Thông Tin. Chỉ huy định hướng kiến trúc hệ thống phân tán, kiểm toán an ninh bảo mật và phương pháp nghiên cứu khoa học cho đề tài.
            </p>
          </div>
        </div>
      </div>

      <!-- 5 Squad Pilot Cards Grid -->
      <div class="space-y-6">
        <h2 class="text-2xl font-black uppercase text-slate-100 text-center">
          5 KỸ SƯ PHÁT TRIỂN HỆ THỐNG
        </h2>

        <div class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
          <div
            v-for="pilot in squadPilots"
            :key="pilot.name"
            class="mecha-hud-bracket border-2 border-slate-800 bg-[#0c0f15] p-6 mecha-cut-tr transition-all duration-300 hover:border-amber-400 hover:shadow-[0_0_30px_rgba(255,204,0,0.25)] space-y-4"
          >
            <div class="flex items-center gap-3.5 border-b border-slate-800 pb-3">
              <img :src="pilot.avatar" :alt="pilot.name" class="h-14 w-14 border-2 border-amber-400 bg-slate-900 shadow-[0_0_15px_rgba(255,204,0,0.2)]" />
              <div>
                <h4 class="text-sm font-black text-slate-100 uppercase">{{ pilot.name }}</h4>
                <div class="text-[10px] font-bold text-amber-400">{{ pilot.callsign }}</div>
                <div class="text-[9px] text-slate-500 font-bold">{{ pilot.classId }}</div>
              </div>
            </div>

            <p class="font-sans text-xs text-slate-300 leading-relaxed">
              {{ pilot.duties }}
            </p>

            <!-- Combat Stat Bars -->
            <div class="space-y-1.5 pt-2 text-[10px]">
              <div class="flex justify-between text-slate-400">
                <span>SYSTEM CODING</span>
                <span class="text-amber-400 font-bold">{{ pilot.stats.code }}%</span>
              </div>
              <div class="h-1.5 w-full bg-slate-900">
                <div class="h-full bg-gradient-to-r from-amber-400 to-orange-500" :style="{ width: `${pilot.stats.code}%` }"></div>
              </div>

              <div class="flex justify-between text-slate-400 pt-1">
                <span>DEFENSE MATRIX</span>
                <span class="text-orange-400 font-bold">{{ pilot.stats.defense }}%</span>
              </div>
              <div class="h-1.5 w-full bg-slate-900">
                <div class="h-full bg-gradient-to-r from-orange-500 to-red-500" :style="{ width: `${pilot.stats.defense}%` }"></div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Tech Stack Matrix -->
      <div class="space-y-6">
        <h2 class="text-2xl font-black uppercase text-slate-100 text-center">
          KHO VŨ KHÍ CÔNG NGHỆ (TECH ARSENAL)
        </h2>

        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <div
            v-for="(tech, tIdx) in techArsenal"
            :key="tIdx"
            class="mecha-hud-bracket border border-slate-800 bg-[#0c0f15] p-4 space-y-1 mecha-cut-tr"
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
