<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { mechaAudio } from '../../utils/portalAudio'
import { tacticalVoice } from '../../utils/portalVoiceSynth'

const activeIndex = ref(0)
const isAutoRotating = ref(true)
let autoRotateTimer = null

const champions = [
  {
    id: 0,
    name: 'Phạm Văn Thành',
    role: 'Trưởng Nhóm & Kiến Trúc Sư Backend',
    classId: 'K65-ATTT',
    codename: 'V-SHIELD PRIME // AEGIS COMMAND',
    avatar: 'https://api.dicebear.com/7.x/bottts/svg?seed=ThanhLead&colors=amber,orange',
    weaponName: 'EX-01 QUANTUM BROADSWORD (LƯỠI ĐẠI KIẾM LƯỢNG TỬ)',
    weaponType: 'Vũ khí Cận chiến / Điều phối Hệ thống',
    weaponDesc: 'Lưỡi kiếm phát quang tích hợp bộ xử lý .NET 8 Clean Architecture & giao thức đồng bộ lai Hybrid Sync, chém phá các xung đột dữ liệu CRDT với độ trễ dưới 30ms.',
    duties: 'Thiết kế kiến trúc hệ thống phân tán, mã hóa mật mã TOTP HMAC-SHA256, phân quyền đa tầng và đảm bảo tính toàn vẹn cơ sở dữ liệu.',
    color: '#ffcc00',
    stats: { code: 98, defense: 96, sync: 99, power: 97 },
    auraClass: 'from-amber-500/30 via-orange-500/20 to-transparent',
    borderColor: 'border-amber-400',
    textColor: 'text-amber-400',
    weaponIcon: '⚔️'
  },
  {
    id: 1,
    name: 'Hà Mạnh Hùng',
    role: 'Kỹ Sư AI & Thị Giác Máy Tính (Vision)',
    classId: 'K65-CNTT',
    codename: 'PHANTOM FALCON // OPTICAL RADAR',
    avatar: 'https://api.dicebear.com/7.x/bottts/svg?seed=HungAI&colors=red,orange',
    weaponName: 'EX-02 HYPER-VELOCITY PLASMA RAILGUN (SÚNG TRƯỜNG PLASMA)',
    weaponType: 'Vũ khí Tầm xa / Khóa Mục tiêu Quang học',
    weaponDesc: 'Phát xạ chùm tia năng lượng quang học 60 FPS YOLOv11 + ArcFace, tự động khóa mục tiêu khuôn mặt trong phạm vi ±45° và đối soát biển số ANPR OCR tức thì.',
    duties: 'Tối ưu hóa pipeline suy luận AI, chống giả mạo sinh trắc học 3D Depth Anti-Spoofing và tích hợp camera giám sát luồng RTSP.',
    color: '#ff5500',
    stats: { code: 95, defense: 91, sync: 93, power: 99 },
    auraClass: 'from-orange-500/30 via-red-500/20 to-transparent',
    borderColor: 'border-orange-500',
    textColor: 'text-orange-400',
    weaponIcon: '🎯'
  },
  {
    id: 2,
    name: 'Phạm Ngọc Hoài Anh',
    role: 'Kỹ Sư DevOps & Hạ Tầng Đám Mây',
    classId: 'K65-KTPM',
    codename: 'DREADNOUGHT VORTEX // CLOUD CORE',
    avatar: 'https://api.dicebear.com/7.x/bottts/svg?seed=HoaiAnh&colors=cyan,blue',
    weaponName: 'EX-03 TITAN HEAVY PARTICLE CANNON (ĐẠI BÁC HẠT NẶNG TITAN)',
    weaponType: 'Vũ khí Phòng vệ / Hạ tầng Đám mây',
    weaponDesc: 'Tạo trường lực bảo vệ không gian mạng Docker Compose & Caddy TLS đa lớp, nén băng thông Protobuf và duy trì thời gian hoạt động Uptime 99.99%.',
    duties: 'Triển khai hạ tầng VPS, thiết lập pipeline CI/CD tự động, bảo mật mạng nội bộ và giám sát tài nguyên phần cứng thời gian thực.',
    color: '#00f0ff',
    stats: { code: 93, defense: 99, sync: 96, power: 95 },
    auraClass: 'from-cyan-500/30 via-blue-500/20 to-transparent',
    borderColor: 'border-cyan-400',
    textColor: 'text-cyan-400',
    weaponIcon: '🛡️'
  },
  {
    id: 3,
    name: 'Vũ Tiến Đạt',
    role: 'Kỹ Sư Frontend UI/UX & Realtime WebRTC',
    classId: 'K65-CNTT',
    codename: 'SPECTRE STRIKER // CYBER HUD',
    avatar: 'https://api.dicebear.com/7.x/bottts/svg?seed=DatFrontend&colors=purple,amber',
    weaponName: 'EX-04 DUAL HOLOGRAPHIC ENERGY DAGGERS (CẶP DAO GĂM HOLOGRAM)',
    weaponType: 'Vũ khí Tác chiến Nhanh / Giao diện Realtime',
    weaponDesc: 'Cặp dao găm laser độ trễ dưới 30ms kết hợp luồng đàm thoại video WebRTC VoIP và giao diện tương tác Mecha Tactical chuẩn game AAA.',
    duties: 'Phát triển toàn bộ hệ thống giao diện Vue 3 Mecha, tích hợp WebGL Three.js và tối ưu hóa trải nghiệm người dùng trên mọi thiết bị.',
    color: '#a855f7',
    stats: { code: 96, defense: 89, sync: 94, power: 96 },
    auraClass: 'from-purple-500/30 via-amber-500/20 to-transparent',
    borderColor: 'border-purple-400',
    textColor: 'text-purple-400',
    weaponIcon: '⚡'
  },
  {
    id: 4,
    name: 'Nguyễn Quốc Việt',
    role: 'Kỹ Sư Lập Trình Mobile & Thiết Bị IoT',
    classId: 'K65-ATTT',
    codename: 'TEMPEST JUGGERNAUT // GATE GUARDIAN',
    avatar: 'https://api.dicebear.com/7.x/bottts/svg?seed=VietMobile&colors=emerald,yellow',
    weaponName: 'EX-05 THUNDERSTRIKE POWER HALBERD (KÍCH XUNG KÍCH SẤM SÉT)',
    weaponType: 'Vũ khí Kiểm soát Rào chắn / Thiết bị Ngoại vi',
    weaponDesc: 'Mũi kích phóng xung điện điều khiển đóng mở rào chắn Barie trong 0.6 giây, đồng bộ ứng dụng di động Android APK và mã TOTP thẻ thông hành.',
    duties: 'Lập trình ứng dụng di động Android, giao tiếp phần cứng Relay/RS485 và kiểm thử độ bền bỉ của trạm kiểm soát an ninh tại cổng.',
    color: '#10b981',
    stats: { code: 92, defense: 95, sync: 92, power: 98 },
    auraClass: 'from-emerald-500/30 via-teal-500/20 to-transparent',
    borderColor: 'border-emerald-400',
    textColor: 'text-emerald-400',
    weaponIcon: '🔱'
  }
]

const currentChampion = computed(() => champions[activeIndex.value])

function selectChampion(index) {
  activeIndex.value = index
  mechaAudio.playTargetLock()
  mechaAudio.playHeavyImpactDrop()
  tacticalVoice.speakTargetLocked(champions[index].name)
}

function nextChampion() {
  selectChampion((activeIndex.value + 1) % champions.length)
}

function prevChampion() {
  selectChampion((activeIndex.value - 1 + champions.length) % champions.length)
}

function getTurntableStyle(index) {
  const total = champions.length
  // Calculate angular offset relative to activeIndex
  let diff = (index - activeIndex.value + total) % total
  if (diff > total / 2) diff -= total

  // 3D positioning on the circular turntable
  const angle = (diff * (2 * Math.PI / total))
  const radiusX = 260
  const radiusZ = 120

  const x = Math.sin(angle) * radiusX
  const z = Math.cos(angle) * radiusZ - radiusZ
  const scale = diff === 0 ? 1.15 : Math.max(0.65, 1 - Math.abs(diff) * 0.22)
  const opacity = diff === 0 ? 1.0 : 0.3
  const zIndex = diff === 0 ? 30 : 20 - Math.abs(diff) * 5

  return {
    transform: `translate3d(${x}px, 0px, ${z}px) scale(${scale})`,
    opacity,
    zIndex,
    filter: diff === 0 ? 'grayscale(0%)' : 'grayscale(100%) brightness(0.4) contrast(1.2)'
  }
}

onMounted(() => {
  autoRotateTimer = setInterval(() => {
    if (isAutoRotating.value) {
      activeIndex.value = (activeIndex.value + 1) % champions.length
      mechaAudio.playHover()
    }
  }, 6500)
})

onUnmounted(() => {
  if (autoRotateTimer) clearInterval(autoRotateTimer)
})
</script>

<template>
  <div
    class="relative w-full font-mono py-8"
    @mouseenter="isAutoRotating = false"
    @mouseleave="isAutoRotating = true"
  >
    <!-- Section Sub-Header -->
    <div class="text-center space-y-2 mb-8">
      <div class="mecha-laser-border inline-flex items-center gap-2 border border-amber-500/40 bg-[#121620] px-4 py-1 text-xs font-black text-amber-400 mecha-cut-tr">
        <span class="h-2 w-2 bg-amber-400 animate-ping"></span>
        <span>// HỘI ĐỒNG SÁNG LẬP • 5 CHIẾN BINH CÔNG NGHỆ</span>
      </div>
      <h2 class="text-2xl sm:text-4xl font-black uppercase text-slate-100">
        SÂN KHẤU 3D CHIẾN BINH V-SHIELD
      </h2>
      <p class="font-sans text-xs sm:text-sm text-slate-400 max-w-xl mx-auto">
        Xoay góc nhìn 3D chính diện từng vị trí chủ chốt với vũ khí công nghệ và vai trò chuyên trách trong đề tài.
      </p>
    </div>

    <!-- ── 3D TURNTABLE STAGE & CALLOUT DOSSIER GRID ── -->
    <div class="grid grid-cols-1 lg:grid-cols-12 gap-8 items-center max-w-7xl mx-auto px-4">
      
      <!-- LEFT / CENTER: 3D TURNTABLE CAROUSEL (7 cols) -->
      <div class="relative lg:col-span-7 flex flex-col items-center justify-center min-h-[440px] overflow-hidden sm:overflow-visible">
        
        <!-- Turntable Floor Hologram Ring -->
        <div class="absolute bottom-6 h-56 w-80 sm:w-96 rounded-full border-2 border-dashed border-amber-500/30 bg-radial from-amber-500/10 to-transparent transform -rotate-x-60 animate-[spin_20s_linear_infinite]"></div>
        <div class="absolute bottom-12 h-40 w-64 sm:w-80 rounded-full border border-orange-500/30 transform -rotate-x-60"></div>

        <!-- 5 Champions 3D Ring Items -->
        <div class="relative w-full h-[360px] flex items-center justify-center perspective-[1000px]">
          <div
            v-for="(champ, idx) in champions"
            :key="champ.id"
            @click="selectChampion(idx)"
            class="absolute cursor-pointer transition-all duration-700 ease-out flex flex-col items-center text-center"
            :style="getTurntableStyle(idx)"
          >
            <!-- Pedestal Aura Platform -->
            <div
              class="relative flex flex-col items-center justify-center p-4 border-2 rounded-xl transition-all duration-500 mecha-cut-corners"
              :class="[
                idx === activeIndex
                  ? `${champ.borderColor} bg-gradient-to-b ${champ.auraClass} shadow-[0_0_45px_rgba(255,204,0,0.4)] scale-105`
                  : 'border-slate-800 bg-[#090c12]/80'
              ]"
            >
              <!-- Weapon Floating Badge Above Head -->
              <div
                v-if="idx === activeIndex"
                class="absolute -top-4 flex items-center gap-1.5 px-3 py-0.5 text-[9px] font-black uppercase text-slate-950 bg-amber-400 mecha-cut-btn animate-bounce shadow-[0_0_15px_#ffcc00]"
              >
                <span>{{ champ.weaponIcon }}</span>
                <span>{{ champ.weaponName.split('(')[0].trim() }}</span>
              </div>

              <!-- Warrior Holographic Avatar / Mech Avatar -->
              <div class="relative h-44 w-44 sm:h-52 sm:w-52 flex items-center justify-center my-2">
                <!-- Energy Halo behind Avatar -->
                <div
                  v-if="idx === activeIndex"
                  class="absolute inset-0 rounded-full bg-radial from-amber-400/30 to-transparent animate-pulse filter blur-md"
                ></div>
                <img
                  :src="champ.avatar"
                  :alt="champ.name"
                  class="h-full w-full object-contain filter drop-shadow-[0_0_20px_rgba(255,204,0,0.5)] transition-transform duration-500"
                  :class="[idx === activeIndex ? 'scale-110 animate-[pulse_3s_ease-in-out_infinite]' : 'opacity-60']"
                />
              </div>

              <!-- Name & Role Tag on Pedestal -->
              <div class="space-y-0.5 mt-1">
                <div class="text-sm font-black uppercase text-slate-100 tracking-wider">
                  {{ champ.name }}
                </div>
                <div
                  class="text-[10px] font-bold"
                  :class="idx === activeIndex ? champ.textColor : 'text-slate-500'"
                >
                  {{ champ.role }}
                </div>
              </div>

              <!-- Laser Target Lock Reticle (Only Active) -->
              <div v-if="idx === activeIndex" class="pointer-events-none absolute inset-0 border border-amber-400/60 mecha-cut-corners">
                <span class="absolute -top-1 -left-1 h-2 w-2 border-t-2 border-l-2 border-amber-400"></span>
                <span class="absolute -top-1 -right-1 h-2 w-2 border-t-2 border-r-2 border-amber-400"></span>
                <span class="absolute -bottom-1 -left-1 h-2 w-2 border-b-2 border-l-2 border-amber-400"></span>
                <span class="absolute -bottom-1 -right-1 h-2 w-2 border-b-2 border-r-2 border-amber-400"></span>
              </div>
            </div>
          </div>
        </div>

        <!-- Navigation Controls Below Turntable -->
        <div class="flex items-center gap-4 mt-6 z-30">
          <button
            type="button"
            @click="prevChampion"
            class="mecha-btn-tactical px-4 py-2 text-xs font-black uppercase mecha-cut-tr flex items-center gap-1"
          >
            <span>◄ TRƯỚC</span>
          </button>

          <!-- Character Dot Navigators -->
          <div class="flex items-center gap-2">
            <button
              v-for="(champ, cIdx) in champions"
              :key="champ.id"
              type="button"
              @click="selectChampion(cIdx)"
              class="h-3 w-8 transition-all mecha-cut-tr"
              :class="[
                cIdx === activeIndex
                  ? 'bg-amber-400 shadow-[0_0_12px_#ffcc00]'
                  : 'bg-slate-800 hover:bg-slate-700'
              ]"
              :title="champ.name"
            ></button>
          </div>

          <button
            type="button"
            @click="nextChampion"
            class="mecha-btn-hazard px-4 py-2 text-xs font-black uppercase mecha-cut-btn flex items-center gap-1"
          >
            <span>TIẾP THEO ►</span>
          </button>
        </div>
      </div>

      <!-- RIGHT: ANNOTATED HUD TACTICAL DOSSIER WITH CALLOUT LINE (5 cols) -->
      <div class="lg:col-span-5 relative font-mono">
        
        <!-- SVG Animated Dashed Callout Line (Connecting Turntable to Dossier) -->
        <svg
          class="pointer-events-none absolute -left-16 top-16 hidden lg:block h-32 w-16 overflow-visible z-20"
        >
          <!-- Glowing Pulsing Dashed Line -->
          <path
            d="M 0,60 L 35,60 L 64,20"
            fill="none"
            stroke="#ffcc00"
            stroke-width="2"
            stroke-dasharray="6,4"
            class="animate-[dash_1s_linear_infinite]"
          />
          <!-- Connection Circle Node at Left -->
          <circle cx="0" cy="60" r="4" fill="#ffcc00" class="animate-ping" />
          <circle cx="0" cy="60" r="3" fill="#ff5500" />
          <!-- End Connection Node at Right -->
          <circle cx="64" cy="20" r="3" fill="#ffcc00" />
        </svg>

        <!-- Tactical Dossier Panel -->
        <div
          class="mecha-hud-bracket border-2 border-amber-500/50 bg-[#0a0d14]/95 p-6 mecha-cut-corners shadow-[0_0_40px_rgba(255,204,0,0.25)] space-y-5 backdrop-blur-xl"
        >
          <!-- Dossier Header -->
          <div class="flex items-center justify-between border-b border-slate-800 pb-3">
            <div class="space-y-0.5">
              <span class="bg-amber-950 px-2 py-0.5 text-[9px] font-black text-amber-400 border border-amber-500/40">
                CHAMPION DOSSIER // 0{{ currentChampion.id + 1 }}
              </span>
              <div class="text-[10px] text-slate-500 font-bold tracking-widest pt-1">
                CALLSIGN: {{ currentChampion.codename }}
              </div>
            </div>
            <span class="text-xs font-black text-amber-400 bg-[#121620] px-2 py-1 border border-slate-800">
              {{ currentChampion.classId }}
            </span>
          </div>

          <!-- Name & Role -->
          <div class="space-y-1">
            <h3 class="text-2xl font-black uppercase text-slate-100 flex items-center gap-2">
              <span>{{ currentChampion.name }}</span>
              <span class="text-xs text-amber-400 font-normal">🎖️</span>
            </h3>
            <div class="text-xs font-bold text-amber-400">
              {{ currentChampion.role }}
            </div>
          </div>

          <!-- Tactical Weapon Callout Box -->
          <div class="border border-orange-500/40 bg-[#121620] p-3.5 mecha-cut-tr space-y-1.5 shadow-[0_0_15px_rgba(255,85,0,0.15)]">
            <div class="flex items-center justify-between text-[10px] font-black text-orange-400">
              <span class="flex items-center gap-1">
                <span>{{ currentChampion.weaponIcon }}</span>
                <span>VŨ KHÍ CHUYÊN BIỆT (SPECIAL WEAPON)</span>
              </span>
              <span class="text-emerald-400">ARMED [100%]</span>
            </div>
            <div class="text-xs font-black text-slate-100">
              {{ currentChampion.weaponName }}
            </div>
            <p class="font-sans text-[11px] text-slate-300 leading-relaxed">
              {{ currentChampion.weaponDesc }}
            </p>
          </div>

          <!-- Core Duties -->
          <div class="space-y-1 text-xs">
            <div class="text-[10px] text-slate-400 font-bold uppercase">
              // NHIỆM VỤ CỐT LÕI ĐỀ ÁN
            </div>
            <p class="font-sans text-slate-300 leading-relaxed text-xs">
              {{ currentChampion.duties }}
            </p>
          </div>

          <!-- Skill Stats Power Grid -->
          <div class="space-y-2 border-t border-slate-800 pt-4 text-[10px]">
            <div class="flex justify-between text-slate-400">
              <span>HỆ THỐNG CODE (ARCHITECTURE)</span>
              <span class="text-amber-400 font-black">{{ currentChampion.stats.code }}%</span>
            </div>
            <div class="h-1.5 w-full bg-slate-900 overflow-hidden">
              <div
                class="h-full bg-gradient-to-r from-amber-400 to-orange-500 transition-all duration-700"
                :style="{ width: `${currentChampion.stats.code}%` }"
              ></div>
            </div>

            <div class="flex justify-between text-slate-400 pt-1">
              <span>ĐỘ TRỄ ĐỒNG BỘ (SYNC RELIABILITY)</span>
              <span class="text-cyan-400 font-black">{{ currentChampion.stats.sync }}%</span>
            </div>
            <div class="h-1.5 w-full bg-slate-900 overflow-hidden">
              <div
                class="h-full bg-gradient-to-r from-cyan-400 to-blue-500 transition-all duration-700"
                :style="{ width: `${currentChampion.stats.sync}%` }"
              ></div>
            </div>

            <div class="flex justify-between text-slate-400 pt-1">
              <span>NĂNG LƯỢNG VŨ KHÍ (WEAPON CHARGE)</span>
              <span class="text-orange-400 font-black">{{ currentChampion.stats.power }}%</span>
            </div>
            <div class="h-1.5 w-full bg-slate-900 overflow-hidden">
              <div
                class="h-full bg-gradient-to-r from-orange-500 to-red-500 transition-all duration-700"
                :style="{ width: `${currentChampion.stats.power}%` }"
              ></div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style>
@keyframes dash {
  to {
    stroke-dashoffset: -20;
  }
}
</style>
