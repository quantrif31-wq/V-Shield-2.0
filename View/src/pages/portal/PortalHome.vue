<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { portalApi } from '../../services/portalApi'

const router = useRouter()
const overview = ref({
  systemName: 'V-SHIELD 2.0',
  tagline: 'Hệ thống kiểm soát an ninh thông minh đa nền tảng & AI Realtime',
  version: '2.0.0',
  averageRating: 4.95,
  totalReviews: 1280,
  totalComments: 3450,
  serverStatus: 'Online'
})

const mascotQuotes = [
  'Xin chào Operator! Hệ thống phòng thủ V-Shield 2.0 đã trực tuyến.',
  'Đồng bộ dữ liệu Hybrid Sync đang hoạt động với độ trễ dưới 30ms!',
  'Mô hình AI Face ID đã sẵn sàng quét nhận diện sinh trắc học đa góc độ.',
  'Cổng liên lạc Video Call VoIP được mã hóa end-to-end sẵn sàng kết nối.'
]
const currentQuoteIndex = ref(0)

function nextQuote() {
  currentQuoteIndex.value = (currentQuoteIndex.value + 1) % mascotQuotes.length
}

function triggerSfx() {
  window.dispatchEvent(new CustomEvent('portal-click-sfx'))
}

function navigateTo(path) {
  triggerSfx()
  router.push(path)
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

onMounted(async () => {
  try {
    const data = await portalApi.getOverview()
    if (data) overview.value = { ...overview.value, ...data }
  } catch {}
  setInterval(nextQuote, 7000)
})
</script>

<template>
  <div class="relative overflow-hidden py-12 lg:py-20">
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
      <!-- ── SECTION 1: HERO SHOWCASE ── -->
      <div class="grid grid-cols-1 items-center gap-12 lg:grid-cols-12 lg:gap-8">
        <!-- Left: Slogan & CTAs (7 cols) -->
        <div class="space-y-6 lg:col-span-7">
          <!-- Cyber Badge -->
          <div class="inline-flex items-center gap-2 rounded-full border border-cyan-500/40 bg-cyan-950/40 px-3.5 py-1.5 text-xs font-bold text-cyan-300 shadow-[0_0_20px_rgba(0,240,255,0.3)] backdrop-blur-md">
            <span class="flex h-2 w-2 rounded-full bg-cyan-400 animate-ping"></span>
            <span class="font-mono tracking-wider">HỆ THỐNG AN NINH THÔNG MINH THẾ HỆ MỚI</span>
          </div>

          <!-- Main Title -->
          <h1 class="text-4xl font-extrabold tracking-tight sm:text-6xl sm:leading-[1.15]">
            Kiểm Soát An Ninh <br />
            <span class="text-transparent bg-clip-text bg-gradient-to-r from-cyan-400 via-teal-200 to-pink-500 drop-shadow-[0_0_35px_rgba(0,240,255,0.5)] font-mono">
              Đa Nền Tảng & AI Realtime
            </span>
          </h1>

          <p class="max-w-2xl text-base leading-relaxed text-slate-300 sm:text-lg">
            Giải pháp tích hợp nhận diện khuôn mặt sinh trắc học <strong class="text-cyan-300">60 FPS</strong>, rào chắn thông minh, mã QR động TOTP và đồng bộ đa trạm <strong class="text-pink-400">Offline-First</strong> tối tân.
          </p>

          <!-- Interactive Mascot Dialogue Box -->
          <div
            @click="nextQuote"
            class="group relative flex cursor-pointer items-center gap-3.5 rounded-2xl border border-cyan-500/30 bg-slate-900/80 p-4 shadow-[0_0_25px_rgba(0,240,255,0.15)] backdrop-blur-md transition hover:border-cyan-400 hover:shadow-[0_0_35px_rgba(0,240,255,0.3)]"
          >
            <div class="relative flex h-12 w-12 shrink-0 items-center justify-center rounded-xl border border-pink-500/50 bg-gradient-to-br from-pink-900/60 to-purple-900/60 shadow-[0_0_15px_rgba(255,42,133,0.4)]">
              <span class="text-xl">🤖</span>
              <span class="absolute -bottom-1 -right-1 flex h-3.5 w-3.5 items-center justify-center rounded-full bg-cyan-400 text-[8px] font-black text-slate-950">
                AI
              </span>
            </div>
            <div class="flex-1 space-y-0.5">
              <div class="flex items-center justify-between text-[11px] font-bold uppercase tracking-wider text-pink-400 font-mono">
                <span>V-Shield AI Core</span>
                <span class="text-[10px] text-cyan-400/80 group-hover:underline">Click đổi câu thoại ↻</span>
              </div>
              <p class="text-xs font-medium text-slate-200 transition-all duration-300">
                "{{ mascotQuotes[currentQuoteIndex] }}"
              </p>
            </div>
          </div>

          <!-- Hero Action Buttons -->
          <div class="flex flex-wrap items-center gap-4 pt-2">
            <button
              type="button"
              @click="navigateTo('/features')"
              class="group relative inline-flex items-center gap-2 overflow-hidden rounded-xl bg-gradient-to-r from-cyan-500 via-teal-400 to-pink-500 px-6 py-3 text-sm font-bold uppercase tracking-wider text-slate-950 shadow-[0_0_30px_rgba(0,240,255,0.4)] transition-all hover:scale-105 hover:shadow-[0_0_40px_rgba(0,240,255,0.7)]"
            >
              <span class="relative z-10 flex items-center gap-2">
                <span>Khám Phá Tính Năng</span>
                <svg class="h-4 w-4 transition-transform group-hover:translate-x-1" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                  <line x1="5" y1="12" x2="19" y2="12"></line>
                  <polyline points="12 5 19 12 12 19"></polyline>
                </svg>
              </span>
              <div class="absolute inset-0 -translate-x-full bg-gradient-to-r from-transparent via-white/40 to-transparent transition-transform duration-700 group-hover:translate-x-full"></div>
            </button>

            <button
              type="button"
              @click="navigateTo('/download')"
              class="inline-flex items-center gap-2 rounded-xl border border-cyan-500/40 bg-slate-900/80 px-6 py-3 text-sm font-bold uppercase tracking-wider text-cyan-300 shadow-[0_0_20px_rgba(0,240,255,0.2)] backdrop-blur-md transition-all hover:border-cyan-300 hover:bg-slate-800/90 hover:text-cyan-200 hover:shadow-[0_0_30px_rgba(0,240,255,0.4)]"
            >
              <svg class="h-4 w-4 text-cyan-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
                <polyline points="7 10 12 15 17 10"></polyline>
                <line x1="12" y1="15" x2="12" y2="3"></line>
              </svg>
              <span>Tải App Mobile</span>
            </button>

            <router-link
              to="/login"
              @click="triggerSfx"
              class="inline-flex items-center gap-2 rounded-xl border border-slate-700 bg-slate-900/60 px-5 py-3 text-sm font-semibold text-slate-300 transition hover:border-pink-500/50 hover:text-pink-300"
            >
              <span>Vào Đăng Nhập</span>
              <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4M10 17l5-5-5-5M15 12H3"/>
              </svg>
            </router-link>
          </div>
        </div>

        <!-- Right: Holographic Telemetry Terminal (5 cols) -->
        <div class="lg:col-span-5">
          <div class="relative mx-auto max-w-md rounded-3xl border border-cyan-500/30 bg-slate-900/80 p-6 shadow-[0_0_50px_rgba(0,240,255,0.25)] backdrop-blur-2xl">
            <!-- Neon Edge Accent -->
            <div class="absolute -top-px left-8 right-8 h-px bg-gradient-to-r from-transparent via-cyan-400 to-transparent"></div>

            <!-- Terminal Header -->
            <div class="flex items-center justify-between border-b border-slate-800 pb-4">
              <div class="flex items-center gap-2">
                <span class="h-3 w-3 rounded-full bg-rose-500/80"></span>
                <span class="h-3 w-3 rounded-full bg-amber-500/80"></span>
                <span class="h-3 w-3 rounded-full bg-emerald-500/80"></span>
                <span class="ml-2 font-mono text-xs font-bold text-cyan-300">TELEMETRY_NODE_01</span>
              </div>
              <span class="rounded bg-cyan-950 px-2 py-0.5 font-mono text-[10px] text-cyan-400">
                ACTIVE
              </span>
            </div>

            <!-- Key Metrics Grid -->
            <div class="mt-5 grid grid-cols-2 gap-3.5">
              <div class="rounded-xl border border-cyan-500/20 bg-slate-950/60 p-3.5">
                <div class="text-[11px] font-semibold text-slate-400">Độ Chính Xác Face ID</div>
                <div class="mt-1 font-mono text-2xl font-black text-cyan-300">99.98%</div>
                <div class="mt-0.5 text-[10px] text-emerald-400">YOLOv11 Biometrics</div>
              </div>

              <div class="rounded-xl border border-pink-500/20 bg-slate-950/60 p-3.5">
                <div class="text-[11px] font-semibold text-slate-400">Độ Trễ Đồng Bộ</div>
                <div class="mt-1 font-mono text-2xl font-black text-pink-400">&lt;30ms</div>
                <div class="mt-0.5 text-[10px] text-pink-300/80">Hybrid Sync Protocol</div>
              </div>

              <div class="rounded-xl border border-teal-500/20 bg-slate-950/60 p-3.5">
                <div class="text-[11px] font-semibold text-slate-400">Nền Tảng Triển Khai</div>
                <div class="mt-1 font-mono text-2xl font-black text-teal-300">3 Platform</div>
                <div class="mt-0.5 text-[10px] text-slate-400">Cloud / Local / Mobile</div>
              </div>

              <div class="rounded-xl border border-purple-500/20 bg-slate-950/60 p-3.5">
                <div class="text-[11px] font-semibold text-slate-400">Đánh Giá Cộng Đồng</div>
                <div class="mt-1 font-mono text-2xl font-black text-amber-400">{{ overview.averageRating }} ★</div>
                <div class="mt-0.5 text-[10px] text-slate-400">{{ overview.totalReviews }}+ reviews</div>
              </div>
            </div>

            <!-- Live Status Logs Stream -->
            <div class="mt-4 rounded-xl border border-slate-800 bg-slate-950/90 p-3 font-mono text-[11px] text-slate-300">
              <div class="flex items-center gap-2 text-cyan-400">
                <span class="h-1.5 w-1.5 rounded-full bg-cyan-400 animate-ping"></span>
                <span>[SYSTEM_LOG] CentralSync connected (v2.0.0)</span>
              </div>
              <div class="text-slate-500 mt-1">[BARRIER] Lane 01 Gate AUTO-ARMED</div>
              <div class="text-emerald-400/90 mt-1">[ENROLLMENT] 3D Vector indexing ready</div>
            </div>
          </div>
        </div>
      </div>

      <!-- ── SECTION 2: QUICK FEATURE HIGHLIGHTS ── -->
      <div class="mt-24 space-y-10">
        <div class="text-center space-y-3">
          <h2 class="text-xs font-black uppercase tracking-widest text-cyan-400 font-mono">
            HỆ THỐNG PHÒNG THỦ TOÀN DIỆN
          </h2>
          <p class="text-3xl font-extrabold tracking-tight sm:text-4xl text-slate-100 font-mono">
            Khám Phá Công Nghệ Cốt Lõi V-Shield 2.0
          </p>
        </div>

        <div class="grid grid-cols-1 gap-6 md:grid-cols-3">
          <!-- Card 1 -->
          <div
            @click="navigateTo('/features')"
            class="group cursor-pointer rounded-2xl border border-cyan-500/30 bg-slate-900/60 p-6 shadow-[0_0_20px_rgba(0,240,255,0.1)] transition-all duration-300 hover:-translate-y-1.5 hover:border-cyan-400 hover:shadow-[0_0_35px_rgba(0,240,255,0.3)]"
          >
            <div class="flex h-12 w-12 items-center justify-center rounded-xl bg-cyan-500/20 text-cyan-300 border border-cyan-400/30 group-hover:scale-110 transition">
              <svg class="h-6 w-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M12 2a5 5 0 0 0-5 5v3a5 5 0 0 0 10 0V7a5 5 0 0 0-5-5z"></path>
                <path d="M17 11.5a5 5 0 0 1-10 0"></path>
              </svg>
            </div>
            <h3 class="mt-4 text-lg font-bold text-slate-100 group-hover:text-cyan-300 transition">AI Face ID & Virtual Barrier</h3>
            <p class="mt-2 text-xs leading-relaxed text-slate-400">
              Nhận diện khuôn mặt 60 FPS đa góc kết hợp điều khiển barie tự động và đối soát biển số xe thông minh.
            </p>
            <div class="mt-4 flex items-center gap-1.5 text-xs font-bold text-cyan-400 group-hover:underline">
              <span>Xem chi tiết tính năng</span>
              <span>→</span>
            </div>
          </div>

          <!-- Card 2 -->
          <div
            @click="navigateTo('/features')"
            class="group cursor-pointer rounded-2xl border border-pink-500/30 bg-slate-900/60 p-6 shadow-[0_0_20px_rgba(255,42,133,0.1)] transition-all duration-300 hover:-translate-y-1.5 hover:border-pink-400 hover:shadow-[0_0_35px_rgba(255,42,133,0.3)]"
          >
            <div class="flex h-12 w-12 items-center justify-center rounded-xl bg-pink-500/20 text-pink-300 border border-pink-400/30 group-hover:scale-110 transition">
              <svg class="h-6 w-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
                <circle cx="8.5" cy="8.5" r="1.5"></circle>
                <polyline points="21 15 16 10 5 21"></polyline>
              </svg>
            </div>
            <h3 class="mt-4 text-lg font-bold text-slate-100 group-hover:text-pink-300 transition">QR Động TOTP & VoIP Call</h3>
            <p class="mt-2 text-xs leading-relaxed text-slate-400">
              Mã QR xoay vòng liên tục chống chụp lén và đàm thoại video call WebRTC trực tiếp tới phòng an ninh.
            </p>
            <div class="mt-4 flex items-center gap-1.5 text-xs font-bold text-pink-400 group-hover:underline">
              <span>Xem chi tiết tính năng</span>
              <span>→</span>
            </div>
          </div>

          <!-- Card 3 -->
          <div
            @click="navigateTo('/features')"
            class="group cursor-pointer rounded-2xl border border-teal-500/30 bg-slate-900/60 p-6 shadow-[0_0_20px_rgba(20,184,166,0.1)] transition-all duration-300 hover:-translate-y-1.5 hover:border-teal-400 hover:shadow-[0_0_35px_rgba(20,184,166,0.3)]"
          >
            <div class="flex h-12 w-12 items-center justify-center rounded-xl bg-teal-500/20 text-teal-300 border border-teal-400/30 group-hover:scale-110 transition">
              <svg class="h-6 w-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <polyline points="23 4 23 10 17 10"></polyline>
                <path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"></path>
              </svg>
            </div>
            <h3 class="mt-4 text-lg font-bold text-slate-100 group-hover:text-teal-300 transition">Hybrid Sync & UEBA</h3>
            <p class="mt-2 text-xs leading-relaxed text-slate-400">
              Đồng bộ dữ liệu hai chiều Cloud-Local không gián đoạn và AI phân tích phát hiện hành vi bất thường.
            </p>
            <div class="mt-4 flex items-center gap-1.5 text-xs font-bold text-teal-400 group-hover:underline">
              <span>Xem chi tiết tính năng</span>
              <span>→</span>
            </div>
          </div>
        </div>
      </div>

      <!-- ── SECTION 3: BOTTOM CALL TO ACTION BANNER ── -->
      <div class="mt-24 rounded-3xl border border-cyan-500/30 bg-gradient-to-r from-slate-900 via-cyan-950/40 to-slate-900 p-8 sm:p-12 shadow-[0_0_50px_rgba(0,240,255,0.2)] text-center space-y-6">
        <h2 class="text-2xl sm:text-4xl font-extrabold text-slate-100 font-mono">
          Sẵn Sàng Trải Nghiệm Hệ Thống An Ninh V-Shield 2.0?
        </h2>
        <p class="mx-auto max-w-xl text-xs sm:text-sm text-slate-300 leading-relaxed">
          Tải ứng dụng Mobile APK hoặc truy cập Cổng Quản Trị Hệ Thống ngay để khám phá toàn bộ tính năng kiểm soát ra vào thời gian thực.
        </p>
        <div class="flex flex-wrap items-center justify-center gap-4 pt-2">
          <button
            type="button"
            @click="navigateTo('/download')"
            class="rounded-xl bg-gradient-to-r from-cyan-500 to-pink-500 px-6 py-3 text-xs font-bold uppercase tracking-wider text-slate-950 shadow-[0_0_25px_rgba(0,240,255,0.5)] hover:scale-105 transition"
          >
            📥 Tải Ngay APK Android
          </button>
          <button
            type="button"
            @click="navigateTo('/community')"
            class="rounded-xl border border-cyan-500/40 bg-slate-900/80 px-6 py-3 text-xs font-bold uppercase tracking-wider text-cyan-300 hover:border-cyan-300 transition"
          >
            💬 Tham Gia Diễn Đàn
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
