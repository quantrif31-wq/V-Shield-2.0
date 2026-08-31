<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { portalApi } from '../../services/portalApi'
import PortalThreeCore from '../../components/portal/PortalThreeCore.vue'
import { TextScramble } from '../../utils/cyberTextScramble'
import { mechaAudio } from '../../utils/portalAudio'

const router = useRouter()
const titleScrambleRef = ref(null)
const subtitleScrambleRef = ref(null)

const overview = ref({
  systemName: 'V-SHIELD 2.0',
  tagline: 'Hệ thống kiểm soát an ninh thông minh đa nền tảng & AI Realtime',
  version: '2.0.0',
  averageRating: 4.95,
  totalReviews: 1280,
  totalComments: 3450,
  serverStatus: 'Online'
})

const systemUpdates = [
  'HỆ THỐNG TRỰC TUYẾN: Rào chắn thông minh đã được nạp thuật toán đối soát ANPR OCR.',
  'ĐỒNG BỘ HYBRID SYNC: Cụm trạm duy trì trạng thái Offline-First với độ trễ dưới 30ms.',
  'RADAR SINH TRẮC HỌC: Mô hình AI Face ID 60FPS sẵn sàng nhận diện đa góc độ.',
  'KÊNH THOẠI KHẨN CẤP: Video Call VoIP mã hóa DTLS/SRTP kết nối trực tiếp phòng an ninh.'
]
const currentUpdateIndex = ref(0)

function nextUpdate() {
  mechaAudio.playHover()
  currentUpdateIndex.value = (currentUpdateIndex.value + 1) % systemUpdates.length
}

function triggerSfx() {
  mechaAudio.playClick()
}

function navigateTo(path) {
  triggerSfx()
  router.push(path)
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

function scrambleHeadline() {
  if (subtitleScrambleRef.value) {
    const fx = new TextScramble(subtitleScrambleRef.value)
    fx.setText('ĐA NỀN TẢNG & AI REALTIME')
  }
}

onMounted(async () => {
  try {
    const data = await portalApi.getOverview()
    if (data) overview.value = { ...overview.value, ...data }
  } catch {}

  scrambleHeadline()
  setInterval(nextUpdate, 7000)
})
</script>

<template>
  <div class="relative overflow-hidden py-10 lg:py-16">
    <div class="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 space-y-16">
      <!-- ── SECTION 1: HERO SECTION WITH THREE.JS 3D HOLOGRAM CORE ── -->
      <div class="grid grid-cols-1 items-center gap-10 lg:grid-cols-12">
        <!-- Left: Headline & Actions (7 cols) -->
        <div class="space-y-6 lg:col-span-7 font-mono">
          <!-- Protocol Badge with Laser Border Tracer -->
          <div class="mecha-laser-border inline-flex items-center gap-2 border border-amber-500/50 bg-[#121620] px-3.5 py-1.5 text-xs font-black text-amber-400 mecha-cut-tr shadow-[0_0_20px_rgba(255,204,0,0.25)]">
            <span class="h-2 w-2 bg-amber-400 animate-ping"></span>
            <span class="tracking-widest">ENTERPRISE DEFENSE SYSTEM // V-SHIELD 2.0</span>
          </div>

          <!-- Stencil Heavy Headline with Cyber Scramble -->
          <h1
            @mouseenter="scrambleHeadline"
            class="mecha-glitch-hover cursor-pointer text-4xl font-black uppercase tracking-tight sm:text-6xl sm:leading-[1.1] text-slate-100 transition-all select-none"
          >
            KIỂM SOÁT AN NINH <br />
            <span
              ref="subtitleScrambleRef"
              class="text-transparent bg-clip-text bg-gradient-to-r from-amber-400 via-orange-400 to-amber-200 drop-shadow-[0_0_35px_rgba(255,204,0,0.6)]"
            >
              ĐA NỀN TẢNG & AI REALTIME
            </span>
          </h1>

          <p class="max-w-2xl text-xs sm:text-sm font-sans leading-relaxed text-slate-300">
            Giải pháp an ninh thế hệ mới tích hợp nhận diện khuôn mặt sinh trắc học <strong class="text-amber-400 font-mono">60 FPS</strong>, kiểm soát rào chắn tự động, mã QR động <strong class="text-amber-400 font-mono">TOTP 30s</strong> và giao thức đồng bộ lai <strong class="text-orange-400 font-mono">Offline-First</strong> độc lập.
          </p>

          <!-- Status Intelligence Stream -->
          <div
            @click="nextUpdate"
            class="mecha-hud-bracket mecha-sheen cursor-pointer border border-amber-500/30 bg-[#0d1017] p-4 mecha-cut-tr transition-all hover:border-amber-400 hover:shadow-[0_0_30px_rgba(255,204,0,0.25)]"
          >
            <div class="flex items-center justify-between border-b border-slate-800 pb-2 text-[10px] font-bold text-amber-400">
              <div class="flex items-center gap-2">
                <span class="h-1.5 w-1.5 bg-amber-400"></span>
                <span>V-SHIELD CORE // TRẠNG THÁI HỆ THỐNG</span>
              </div>
              <span class="text-slate-500 hover:text-amber-300">[ ĐỔI THÔNG BÁO » ]</span>
            </div>
            <p class="mt-2.5 font-sans text-xs font-medium text-slate-200">
              "{{ systemUpdates[currentUpdateIndex] }}"
            </p>
          </div>

          <!-- Action CTAs -->
          <div class="flex flex-wrap items-center gap-4 pt-2">
            <button
              type="button"
              @click="navigateTo('/features')"
              class="mecha-btn-hazard px-6 py-3 text-xs font-black uppercase mecha-cut-btn inline-flex items-center gap-2"
            >
              <span>GIẢI PHÁP CÔNG NGHỆ</span>
              <span class="text-slate-950 font-bold">»</span>
            </button>

            <button
              type="button"
              @click="navigateTo('/download')"
              class="mecha-btn-tactical px-6 py-3 text-xs font-black uppercase mecha-cut-tr inline-flex items-center gap-2"
            >
              <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
                <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
                <polyline points="7 10 12 15 17 10"></polyline>
                <line x1="12" y1="15" x2="12" y2="3"></line>
              </svg>
              <span>TẢI ỨNG DỤNG MOBILE</span>
            </button>

            <router-link
              to="/login"
              @click="triggerSfx"
              class="border border-slate-700 bg-[#0e1117] px-5 py-3 text-xs font-bold text-slate-400 hover:border-amber-400 hover:text-amber-300 transition mecha-cut-tr"
            >
              CỔNG QUẢN TRỊ »
            </router-link>
          </div>
        </div>

        <!-- Right: THREE.JS 3D WebGL Hologram Core & Telemetry (5 cols) -->
        <div class="lg:col-span-5 font-mono">
          <div class="relative mx-auto max-w-md border-2 border-amber-500/40 bg-[#0a0d14] p-5 mecha-cut-corners shadow-[0_0_50px_rgba(255,204,0,0.2)] mecha-laser-border">
            <!-- Telemetry Header -->
            <div class="flex items-center justify-between border-b border-slate-800 pb-3 text-xs font-black text-amber-400">
              <div class="flex items-center gap-2">
                <span class="h-2 w-2 bg-amber-400"></span>
                <span>HỆ THỐNG GIÁM SÁT 3D // THREE.JS WEBGL</span>
              </div>
              <span class="bg-amber-950 px-2 py-0.5 text-[10px] text-amber-300 border border-amber-500/30">
                ACTIVE
              </span>
            </div>

            <!-- Three.js 3D WebGL Hologram Core -->
            <div class="py-2 flex justify-center">
              <PortalThreeCore />
            </div>

            <!-- Gauges Grid -->
            <div class="mt-2 grid grid-cols-2 gap-2.5">
              <div class="border border-amber-500/30 bg-[#121620] p-2.5 mecha-cut-tr">
                <div class="text-[9.5px] text-slate-400 font-bold">ĐỘ CHÍNH XÁC FACE ID</div>
                <div class="mt-0.5 text-xl font-black text-amber-400">99.98%</div>
                <div class="text-[8.5px] text-emerald-400">YOLOv11 60FPS</div>
              </div>

              <div class="border border-orange-500/30 bg-[#121620] p-2.5 mecha-cut-tr">
                <div class="text-[9.5px] text-slate-400 font-bold">ĐỘ TRỄ ĐỒNG BỘ</div>
                <div class="mt-0.5 text-xl font-black text-orange-400">&lt;30ms</div>
                <div class="text-[8.5px] text-amber-300">HYBRID LINK</div>
              </div>
            </div>

            <!-- Live Status Grid Stream -->
            <div class="mt-3 border border-slate-800 bg-[#080a0f] p-2.5 text-[10.5px] text-slate-300 space-y-1">
              <div class="flex items-center justify-between text-amber-400">
                <span>[TIÊU CHUẨN AN TOÀN]</span>
                <span class="font-bold">100% NOMINAL</span>
              </div>
              <div class="h-1.5 w-full bg-slate-900 overflow-hidden">
                <div class="h-full bg-gradient-to-r from-amber-400 to-orange-500 w-full animate-pulse"></div>
              </div>
              <div class="text-slate-500 text-[9.5px] pt-0.5">
                // TRẠNG THÁI: CLOUD & TRẠM CỤC BỘ HOẠT ĐỘNG ỔN ĐỊNH
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- ── SECTION 2: 3 CORE SOLUTION BLOCKS (WITH 3D TILT & SHEEN) ── -->
      <div class="space-y-8 font-mono">
        <div class="text-center space-y-2">
          <div class="inline-flex items-center gap-2 border border-amber-500/30 bg-[#121620] px-3 py-1 text-[10px] font-black text-amber-400 mecha-cut-tr">
            <span>CORE ARCHITECTURE</span>
          </div>
          <h2 class="text-2xl sm:text-4xl font-black uppercase text-slate-100">
            3 TRỤ CỘT CÔNG NGHỆ CỐT LÕI
          </h2>
        </div>

        <div class="grid grid-cols-1 gap-6 md:grid-cols-3">
          <!-- Block 01 -->
          <div
            @click="navigateTo('/features')"
            class="mecha-hud-bracket mecha-card-3d mecha-sheen cursor-pointer border border-amber-500/30 bg-[#0e1117] p-6 mecha-cut-tr transition-all hover:border-amber-400 hover:shadow-[0_0_35px_rgba(255,204,0,0.35)] space-y-3"
          >
            <div class="flex items-center justify-between">
              <span class="bg-amber-950 px-2 py-0.5 text-[9px] font-bold text-amber-400 border border-amber-500/30">MODULE // 01</span>
              <span class="text-xs text-slate-500 font-bold">60 FPS</span>
            </div>
            <h3 class="text-base font-black text-slate-100">AI FACE ID & BARRIER</h3>
            <p class="font-sans text-xs text-slate-400 leading-relaxed">
              Nhận diện khuôn mặt đa góc độ kết hợp đóng mở barie tự động và đối soát biển số ANPR OCR thời gian thực.
            </p>
            <div class="text-[11px] font-bold text-amber-400 pt-2 flex items-center justify-between">
              <span>XEM CHI TIẾT TÍNH NĂNG</span>
              <span>»</span>
            </div>
          </div>

          <!-- Block 02 -->
          <div
            @click="navigateTo('/features')"
            class="mecha-hud-bracket mecha-card-3d mecha-sheen cursor-pointer border border-orange-500/30 bg-[#0e1117] p-6 mecha-cut-tr transition-all hover:border-orange-400 hover:shadow-[0_0_35px_rgba(255,85,0,0.35)] space-y-3"
          >
            <div class="flex items-center justify-between">
              <span class="bg-orange-950 px-2 py-0.5 text-[9px] font-bold text-orange-400 border border-orange-500/30">MODULE // 02</span>
              <span class="text-xs text-slate-500 font-bold">TOTP-256</span>
            </div>
            <h3 class="text-base font-black text-slate-100">QR ĐỘNG & VIDEO CALL</h3>
            <p class="font-sans text-xs text-slate-400 leading-relaxed">
              Mã QR xoay vòng liên tục 30 giây chống chụp màn hình và tổng đài đàm thoại video WebRTC trực tiếp.
            </p>
            <div class="text-[11px] font-bold text-orange-400 pt-2 flex items-center justify-between">
              <span>XEM CHI TIẾT TÍNH NĂNG</span>
              <span>»</span>
            </div>
          </div>

          <!-- Block 03 -->
          <div
            @click="navigateTo('/features')"
            class="mecha-hud-bracket mecha-card-3d mecha-sheen cursor-pointer border border-cyan-500/30 bg-[#0e1117] p-6 mecha-cut-tr transition-all hover:border-cyan-400 hover:shadow-[0_0_35px_rgba(0,240,255,0.35)] space-y-3"
          >
            <div class="flex items-center justify-between">
              <span class="bg-cyan-950 px-2 py-0.5 text-[9px] font-bold text-cyan-400 border border-cyan-500/30">MODULE // 03</span>
              <span class="text-xs text-slate-500 font-bold">SUB-30MS</span>
            </div>
            <h3 class="text-base font-black text-slate-100">HYBRID SYNC & UEBA</h3>
            <p class="font-sans text-xs text-slate-400 leading-relaxed">
              Đồng bộ dữ liệu hai chiều Cloud-Local Offline-First và AI phát hiện hành vi bất thường tự động.
            </p>
            <div class="text-[11px] font-bold text-cyan-400 pt-2 flex items-center justify-between">
              <span>XEM CHI TIẾT TÍNH NĂNG</span>
              <span>»</span>
            </div>
          </div>
        </div>
      </div>

      <!-- ── SECTION 3: BOTTOM CTA ── -->
      <div class="mecha-laser-border border-2 border-amber-500/40 bg-[#0a0d14] p-8 sm:p-12 mecha-cut-corners shadow-[0_0_50px_rgba(255,204,0,0.15)] text-center space-y-6 font-mono">
        <h2 class="text-2xl sm:text-4xl font-black uppercase text-slate-100">
          SẴN SÀNG TRẢI NGHIỆM V-SHIELD 2.0?
        </h2>
        <p class="mx-auto max-w-xl font-sans text-xs sm:text-sm text-slate-300 leading-relaxed">
          Tải ứng dụng Mobile APK hoặc truy cập Cổng Quản Trị Hệ Thống ngay để khám phá toàn bộ tính năng an ninh thời gian thực.
        </p>
        <div class="flex flex-wrap items-center justify-center gap-4 pt-2">
          <button
            type="button"
            @click="navigateTo('/download')"
            class="mecha-btn-hazard px-6 py-3.5 text-xs font-black uppercase mecha-cut-btn"
          >
            📥 TẢI APP MOBILE (APK)
          </button>
          <button
            type="button"
            @click="navigateTo('/community')"
            class="mecha-btn-tactical px-6 py-3.5 text-xs font-black uppercase mecha-cut-tr"
          >
            💬 XEM ĐÁNH GIÁ & GÓP Ý
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
