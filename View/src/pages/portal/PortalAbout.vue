<script setup>
const teamMembers = [
  {
    name: 'Phạm Văn Thành',
    role: 'Team Lead & Backend Architect',
    avatar: 'https://api.dicebear.com/7.x/adventurer/svg?seed=ThanhLead',
    classId: 'K65-ATTT',
    duties: 'Kiến trúc hệ thống .NET 8, thuật toán Hybrid Sync, bảo mật mã QR TOTP và phân quyền JWT.',
    stats: { code: 98, security: 95, architecture: 96 }
  },
  {
    name: 'Hà Mạnh Hùng',
    role: 'AI & Computer Vision Engineer',
    avatar: 'https://api.dicebear.com/7.x/adventurer/svg?seed=HungAI',
    classId: 'K65-CNTT',
    duties: 'Huấn luyện mô hình YOLOv11 + ArcFace nhận diện khuôn mặt 60 FPS và OCR biển số xe ANPR.',
    stats: { code: 94, security: 90, architecture: 92 }
  },
  {
    name: 'Phạm Ngọc Hoài Anh',
    role: 'DevOps & Cloud Infrastructure',
    avatar: 'https://api.dicebear.com/7.x/adventurer/svg?seed=HoaiAnh',
    classId: 'K65-KTPM',
    duties: 'Đóng gói Docker Compose, Reverse Proxy Caddy SSL, tối ưu hóa bộ nhớ VPS và CI/CD Automation.',
    stats: { code: 92, security: 96, architecture: 95 }
  },
  {
    name: 'Vũ Tiến Đạt',
    role: 'Frontend UI/UX & Realtime WebRTC',
    avatar: 'https://api.dicebear.com/7.x/adventurer/svg?seed=DatFrontend',
    classId: 'K65-CNTT',
    duties: 'Xây dựng giao diện Vue 3 Cyber-Anime, tích hợp luồng Video Call WebRTC VoIP và MapLibre 3D.',
    stats: { code: 95, security: 88, architecture: 91 }
  },
  {
    name: 'Nguyễn Quốc Việt',
    role: 'Mobile Android & Edge Hardware IoT',
    avatar: 'https://api.dicebear.com/7.x/adventurer/svg?seed=VietMobile',
    classId: 'K65-ATTT',
    duties: 'Phát triển ứng dụng Mobile Android APK, giao tiếp rào chắn Barie ảo và kiểm thử an ninh UEBA.',
    stats: { code: 91, security: 94, architecture: 90 }
  }
]

const techStack = [
  { name: 'ASP.NET Core 8.0', type: 'Backend Framework', desc: 'REST API, SignalR Hub, EF Core' },
  { name: 'Vue.js 3 + Vite', type: 'Frontend SPA', desc: 'Composition API, Tailwind CSS, Pinia' },
  { name: 'Python + OpenCV + YOLO', type: 'AI Biometrics', desc: 'Face ID & ANPR License Plate' },
  { name: 'SQL Server 2022', type: 'Database Engine', desc: 'Relational Database & Audit Logs' },
  { name: 'Docker & Compose', type: 'Containerization', desc: 'Isolated Microservices & Multi-stage' },
  { name: 'Caddy & Nginx', type: 'Reverse Proxy', desc: 'Auto TLS, Load Balancing, Caching' },
  { name: 'WebRTC & Go2RTC', type: 'Media Streaming', desc: 'Low-latency Video & VoIP Intercom' }
]
</script>

<template>
  <div class="py-12 lg:py-16">
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 space-y-16">
      <!-- Header -->
      <div class="text-center space-y-3">
        <div class="inline-flex items-center gap-2 rounded-full border border-cyan-500/30 bg-cyan-950/40 px-3 py-1 text-xs font-bold text-cyan-300 font-mono">
          <span>GRADUATION THESIS PROJECT • 2026</span>
        </div>
        <h1 class="text-3xl sm:text-5xl font-extrabold tracking-tight text-slate-100 font-mono">
          Về Dự Án & Đội Ngũ Tác Giả
        </h1>
        <p class="mx-auto max-w-2xl text-xs sm:text-sm text-slate-400 leading-relaxed">
          Đề án tốt nghiệp: <strong>"Hệ thống kiểm soát an ninh thông minh đa nền tảng kết hợp AI sinh trắc học và giao thức đồng bộ lai V-Shield 2.0"</strong>.
        </p>
      </div>

      <!-- Advisor Card -->
      <div class="mx-auto max-w-3xl rounded-3xl border border-pink-500/40 bg-gradient-to-br from-slate-900 via-pink-950/20 to-slate-900 p-8 shadow-[0_0_40px_rgba(255,42,133,0.2)] backdrop-blur-2xl">
        <div class="flex flex-col sm:flex-row items-center gap-6 text-center sm:text-left">
          <div class="relative flex h-24 w-24 shrink-0 items-center justify-center rounded-2xl border border-pink-400 bg-slate-950 shadow-[0_0_25px_rgba(255,42,133,0.5)]">
            <span class="text-4xl">🎓</span>
          </div>
          <div class="space-y-1.5">
            <div class="text-xs font-black uppercase tracking-widest text-pink-400 font-mono">
              GIẢNG VIÊN HƯỚNG DẪN ĐỒ ÁN
            </div>
            <h3 class="text-2xl font-black text-slate-100 font-mono">
              ThS. Phan Hoàng Khải
            </h3>
            <p class="text-xs text-slate-300 leading-relaxed pt-1">
              Khoa Công Nghệ Thông Tin & An Toàn Thông Tin. Cố vấn định hướng kiến trúc hệ thống phân tán, kiểm toán an ninh mạng và phương pháp nghiên cứu khoa học cho đề tài.
            </p>
          </div>
        </div>
      </div>

      <!-- 5 Team Developers Grid -->
      <div class="space-y-6">
        <h2 class="text-2xl font-extrabold text-slate-100 font-mono text-center">
          Nhóm Kỹ Sư Phát Triển Hệ Thống
        </h2>

        <div class="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
          <div
            v-for="member in teamMembers"
            :key="member.name"
            class="group rounded-3xl border border-slate-800 bg-slate-900/70 p-6 backdrop-blur-xl transition-all duration-300 hover:-translate-y-1.5 hover:border-cyan-400 hover:shadow-[0_0_30px_rgba(0,240,255,0.25)] space-y-4"
          >
            <div class="flex items-center gap-3.5">
              <img :src="member.avatar" :alt="member.name" class="h-14 w-14 rounded-2xl border border-cyan-400/40 bg-slate-800 shadow-[0_0_15px_rgba(0,240,255,0.2)] group-hover:scale-105 transition" />
              <div>
                <h4 class="text-sm font-bold text-slate-100 font-mono group-hover:text-cyan-300 transition">{{ member.name }}</h4>
                <div class="text-xs font-semibold text-pink-400">{{ member.role }}</div>
                <div class="text-[10px] text-slate-500 font-mono">{{ member.classId }}</div>
              </div>
            </div>

            <p class="text-xs text-slate-300 leading-relaxed border-t border-slate-800/80 pt-3">
              {{ member.duties }}
            </p>

            <!-- Stat Bars -->
            <div class="space-y-1.5 pt-1 text-[10px] font-mono">
              <div class="flex justify-between text-slate-400">
                <span>Code Mastery</span>
                <span class="text-cyan-300 font-bold">{{ member.stats.code }}%</span>
              </div>
              <div class="h-1.5 w-full bg-slate-800 rounded-full overflow-hidden">
                <div class="h-full bg-gradient-to-r from-cyan-400 to-teal-300 rounded-full" :style="{ width: `${member.stats.code}%` }"></div>
              </div>

              <div class="flex justify-between text-slate-400 pt-1">
                <span>Security Analysis</span>
                <span class="text-pink-400 font-bold">{{ member.stats.security }}%</span>
              </div>
              <div class="h-1.5 w-full bg-slate-800 rounded-full overflow-hidden">
                <div class="h-full bg-gradient-to-r from-pink-500 to-purple-400 rounded-full" :style="{ width: `${member.stats.security}%` }"></div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Tech Stack Matrix -->
      <div class="space-y-6">
        <h2 class="text-2xl font-extrabold text-slate-100 font-mono text-center">
          Hệ Thống Công Nghệ Sử Dụng (Tech Stack)
        </h2>

        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <div
            v-for="(tech, tIdx) in techStack"
            :key="tIdx"
            class="rounded-2xl border border-slate-800 bg-slate-900/60 p-4 space-y-1"
          >
            <div class="text-xs font-bold text-cyan-300 font-mono">{{ tech.name }}</div>
            <div class="text-[10px] font-semibold text-pink-400">{{ tech.type }}</div>
            <p class="text-[11px] text-slate-400">{{ tech.desc }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
