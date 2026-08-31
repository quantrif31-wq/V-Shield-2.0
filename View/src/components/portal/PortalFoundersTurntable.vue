<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { mechaAudio } from '../../utils/portalAudio'
import { tacticalVoice } from '../../utils/portalVoiceSynth'
import PortalMechaWarrior3DStage from './PortalMechaWarrior3DStage.vue'

const activeIndex = ref(0) // Default to Phạm Văn Thành (Pilot 0)
const isAutoRotating = ref(true)
let autoRotateTimer = null

const champions = [
  {
    id: 0,
    name: 'Phạm Văn Thành',
    role: 'Trưởng Nhóm & Kiến Trúc Sư Trưởng Backend Core',
    classId: 'K65-ATTT',
    codename: 'SHADOW AEGIS // PRIME SOVEREIGN',
    weaponName: 'EX-01 QUANTUM BROADSWORD (LƯỠI ĐẠI KIẾM LƯỢNG TỬ)',
    weaponType: 'Chỉ huy Tác chiến / Điều phối Hệ thống Phân tán',
    weaponDesc: 'Lưỡi kiếm phát quang tích hợp bộ xử lý .NET 8 Clean Architecture & giao thức đồng bộ lai Hybrid Sync, giảm xung đột dữ liệu CRDT với độ trễ dưới 30ms.',
    duties: 'Thiết kế kiến trúc hệ thống phân tán, mã hóa mật mã TOTP HMAC-SHA256, phân quyền đa tầng RBAC và đảm bảo an ninh toàn diện.',
    color: '#c084fc', // Menacing Shadow Purple / Violet
    stats: { code: 99, defense: 97, sync: 99, power: 98 },
    borderColor: 'border-purple-400',
    textColor: 'text-purple-400',
    weaponIcon: '⚔️'
  },
  {
    id: 1,
    name: 'Hà Mạnh Hùng',
    role: 'Kỹ Sư AI & Thị Giác Máy Tính (Optical Vision)',
    classId: 'K65-CNTT',
    codename: 'PHANTOM FALCON // OPTICAL RADAR',
    weaponName: 'EX-02 HYPER-VELOCITY PLASMA RAILGUN (SÚNG TRƯỜNG PLASMA)',
    weaponType: 'Trinh sát Tầm xa / Khóa Mục tiêu Sinh trắc học',
    weaponDesc: 'Phát xạ chùm tia năng lượng quang học 60 FPS YOLOv11 + ArcFace, tự động khóa mục tiêu khuôn mặt trong phạm vi ±45° và đối soát biển số ANPR OCR tức thì.',
    duties: 'Tối ưu hóa pipeline suy luận AI, chống giả mạo sinh trắc học 3D Depth Anti-Spoofing và tích hợp camera giám sát luồng RTSP.',
    color: '#ff5500',
    stats: { code: 95, defense: 91, sync: 93, power: 99 },
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
    weaponName: 'EX-03 TITAN HEAVY PARTICLE CANNON (ĐẠI BÁC HẠT NẶNG TITAN)',
    weaponType: 'Phòng vệ Không gian mạng / Hạ tầng Container',
    weaponDesc: 'Tạo trường lực bảo vệ không gian mạng Docker Compose & Caddy TLS đa lớp, nén băng thông Protobuf và duy trì thời gian hoạt động Uptime 99.99%.',
    duties: 'Triển khai hạ tầng VPS, thiết lập pipeline CI/CD tự động, bảo mật mạng nội bộ và giám sát tài nguyên phần cứng thời gian thực.',
    color: '#00f0ff',
    stats: { code: 93, defense: 99, sync: 96, power: 95 },
    borderColor: 'border-cyan-400',
    textColor: 'text-cyan-400',
    weaponIcon: '🛡️'
  },
  {
    id: 3,
    name: 'Vũ Tiến Đạt',
    role: 'Kỹ Sư Frontend UI/UX & Realtime WebRTC HUD',
    classId: 'K65-CNTT',
    codename: 'SPECTRE STRIKER // CYBER HUD',
    weaponName: 'EX-04 DUAL HOLOGRAPHIC ENERGY DAGGERS (CẶP DAO GĂM HOLOGRAM)',
    weaponType: 'Tác chiến Nhanh / Giao diện Realtime & WebRTC',
    weaponDesc: 'Cặp dao găm laser độ trễ dưới 30ms kết hợp luồng đàm thoại video WebRTC VoIP và giao diện tương tác Mecha Tactical chuẩn game AAA.',
    duties: 'Phát triển toàn bộ hệ thống giao diện Vue 3 Mecha, tích hợp WebGL Three.js và tối ưu hóa trải nghiệm người dùng trên mọi thiết bị.',
    color: '#ffaa00',
    stats: { code: 96, defense: 89, sync: 94, power: 96 },
    borderColor: 'border-amber-400',
    textColor: 'text-amber-400',
    weaponIcon: '🗡️'
  },
  {
    id: 4,
    name: 'Nguyễn Quốc Việt',
    role: 'Kỹ Sư Lập Trình Mobile & Thiết Bị IoT Barie',
    classId: 'K65-ATTT',
    codename: 'TEMPEST JUGGERNAUT // GATE GUARDIAN',
    weaponName: 'EX-05 THUNDERSTRIKE POWER HALBERD (KÍCH XUNG KÍCH SẤM SÉT)',
    weaponType: 'Kiểm soát Rào chắn / Thiết bị Ngoại vi Relay',
    weaponDesc: 'Mũi kích phóng xung điện điều khiển đóng mở rào chắn Barie trong 0.6 giây, đồng bộ ứng dụng di động Android APK và mã TOTP thẻ thông hành.',
    duties: 'Lập trình ứng dụng di động Android, giao tiếp phần cứng Relay/RS485 và kiểm thử độ bền bỉ của trạm kiểm soát an ninh tại cổng.',
    color: '#10b981',
    stats: { code: 92, defense: 95, sync: 92, power: 98 },
    borderColor: 'border-emerald-400',
    textColor: 'text-emerald-400',
    weaponIcon: '⚡'
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
  const next = (activeIndex.value + 1) % champions.length
  selectChampion(next)
}

function prevChampion() {
  const prev = (activeIndex.value - 1 + champions.length) % champions.length
  selectChampion(prev)
}

function toggleAutoRotate() {
  isAutoRotating.value = !isAutoRotating.value
  mechaAudio.playClick()
  if (isAutoRotating.value) {
    startAutoRotate()
  } else {
    stopAutoRotate()
  }
}

function startAutoRotate() {
  stopAutoRotate()
  autoRotateTimer = setInterval(() => {
    activeIndex.value = (activeIndex.value + 1) % champions.length
  }, 5000)
}

function stopAutoRotate() {
  if (autoRotateTimer) {
    clearInterval(autoRotateTimer)
    autoRotateTimer = null
  }
}

onMounted(() => {
  if (isAutoRotating.value) {
    startAutoRotate()
  }
})

onUnmounted(() => {
  stopAutoRotate()
})
</script>

<template>
  <div class="relative space-y-6 font-mono">
    
    <!-- Top Command Telemetry Bar -->
    <div class="flex flex-wrap items-center justify-between gap-4 border-b border-amber-500/20 pb-3">
      <div class="flex items-center gap-3">
        <span class="h-2.5 w-2.5 rounded-full animate-ping" :style="{ backgroundColor: currentChampion.color }"></span>
        <div class="text-xs font-black uppercase tracking-wider text-slate-300">
          BUỒNG LÁI TÁC CHIẾN MECHA 2.0 // <span :style="{ color: currentChampion.color }">{{ currentChampion.codename }}</span>
        </div>
      </div>

      <!-- Controls -->
      <div class="flex items-center gap-2">
        <button
          type="button"
          @click="prevChampion"
          class="px-2.5 py-1 text-xs font-bold border border-slate-700 bg-[#07090e] hover:border-amber-400 text-slate-300 hover:text-amber-400 transition-all mecha-cut-tr"
        >
          ◀ TRƯỚC
        </button>

        <button
          type="button"
          @click="nextChampion"
          class="px-2.5 py-1 text-xs font-bold border border-slate-700 bg-[#07090e] hover:border-amber-400 text-slate-300 hover:text-amber-400 transition-all mecha-cut-tr"
        >
          TIẾP ▶
        </button>

        <button
          type="button"
          @click="toggleAutoRotate"
          class="px-2.5 py-1 text-[11px] font-bold border transition-all mecha-cut-tr"
          :class="[
            isAutoRotating
              ? 'border-emerald-500 bg-emerald-950/60 text-emerald-400 shadow-[0_0_12px_rgba(16,185,129,0.3)]'
              : 'border-slate-800 bg-[#07090e] text-slate-500 hover:text-slate-300'
          ]"
        >
          <span>{{ isAutoRotating ? '🔄 TỰ XOAY: BẬT' : '⏸️ TỰ XOAY: TẮT' }}</span>
        </button>
      </div>
    </div>

    <!-- ── SPLIT SCREEN WITH FULL SCREEN-CROSSING CALLOUT DASHED LEADER LINES ── -->
    <div class="relative grid grid-cols-1 lg:grid-cols-12 gap-6 items-stretch">
      
      <!-- ── AAA GAME CALLOUT DASHED LEADER LINES (CROSSING LEFT TO RIGHT ON DESKTOP) ── -->
      <svg
        class="hidden lg:block pointer-events-none absolute inset-0 w-full h-full z-30 overflow-visible"
        viewBox="0 0 1200 600"
        preserveAspectRatio="none"
      >
        <defs>
          <linearGradient id="lineGlow" x1="0%" y1="0%" x2="100%" y2="0%">
            <stop offset="0%" :stop-color="currentChampion.color" stop-opacity="0.9" />
            <stop offset="100%" :stop-color="currentChampion.color" stop-opacity="0.4" />
          </linearGradient>
        </defs>

        <!-- ── CALLOUT 1: FROM PILOT CYBER MASK (X:340, Y:130) -> RIGHT HEADER CARD (X:680, Y:80) ── -->
        <g class="transition-all duration-500">
          <polyline
            points="340,130 520,80 680,80"
            fill="none"
            :stroke="currentChampion.color"
            stroke-width="2"
            stroke-dasharray="6,4"
            class="animate-dash"
            opacity="0.9"
          />
          <!-- Target Reticle at Origin on Mask -->
          <circle cx="340" cy="130" r="5" :stroke="currentChampion.color" stroke-width="1.5" fill="none" class="animate-ping" />
          <circle cx="340" cy="130" r="3" :fill="currentChampion.color" />
          <!-- Pin Box Label at Angle Kink -->
          <rect x="500" y="70" width="40" height="18" fill="#07090e" :stroke="currentChampion.color" stroke-width="1" rx="2" />
          <text x="520" y="83" font-size="9" font-family="monospace" :fill="currentChampion.color" font-weight="bold" text-anchor="middle">HUD-1</text>
          <!-- Terminal Arrow Pin at Right Box -->
          <polygon points="680,76 688,80 680,84" :fill="currentChampion.color" />
        </g>

        <!-- ── CALLOUT 2: FROM PILOT CHEST CORE (X:340, Y:220) -> RIGHT DUTIES BOX (X:680, Y:190) ── -->
        <g class="transition-all duration-500">
          <polyline
            points="340,220 500,190 680,190"
            fill="none"
            :stroke="currentChampion.color"
            stroke-width="2"
            stroke-dasharray="6,4"
            class="animate-dash"
            opacity="0.85"
          />
          <circle cx="340" cy="220" r="5" :stroke="currentChampion.color" stroke-width="1.5" fill="none" />
          <circle cx="340" cy="220" r="3" :fill="currentChampion.color" />
          <rect x="480" y="180" width="40" height="18" fill="#07090e" :stroke="currentChampion.color" stroke-width="1" rx="2" />
          <text x="500" y="193" font-size="9" font-family="monospace" :fill="currentChampion.color" font-weight="bold" text-anchor="middle">CORE</text>
          <polygon points="680,186 688,190 680,194" :fill="currentChampion.color" />
        </g>

        <!-- ── CALLOUT 3: FROM PILOT ARM WEAPON CONDUIT (X:280, Y:340) -> RIGHT WEAPON SPECS (X:680, Y:310) ── -->
        <g class="transition-all duration-500">
          <polyline
            points="280,340 510,310 680,310"
            fill="none"
            :stroke="currentChampion.color"
            stroke-width="2"
            stroke-dasharray="6,4"
            class="animate-dash"
            opacity="0.85"
          />
          <circle cx="280" cy="340" r="5" :stroke="currentChampion.color" stroke-width="1.5" fill="none" />
          <circle cx="280" cy="340" r="3" :fill="currentChampion.color" />
          <rect x="490" y="300" width="40" height="18" fill="#07090e" :stroke="currentChampion.color" stroke-width="1" rx="2" />
          <text x="510" y="313" font-size="9" font-family="monospace" :fill="currentChampion.color" font-weight="bold" text-anchor="middle">WEAP</text>
          <polygon points="680,306 688,310 680,314" :fill="currentChampion.color" />
        </g>

        <!-- ── CALLOUT 4: FROM PEDESTAL / BASE THRUSTER (X:340, Y:450) -> RIGHT STATS RADAR (X:680, Y:430) ── -->
        <g class="transition-all duration-500">
          <polyline
            points="340,450 530,430 680,430"
            fill="none"
            :stroke="currentChampion.color"
            stroke-width="2"
            stroke-dasharray="6,4"
            class="animate-dash"
            opacity="0.8"
          />
          <circle cx="340" cy="450" r="5" :stroke="currentChampion.color" stroke-width="1.5" fill="none" />
          <circle cx="340" cy="450" r="3" :fill="currentChampion.color" />
          <rect x="510" y="420" width="40" height="18" fill="#07090e" :stroke="currentChampion.color" stroke-width="1" rx="2" />
          <text x="530" y="433" font-size="9" font-family="monospace" :fill="currentChampion.color" font-weight="bold" text-anchor="middle">SYNC</text>
          <polygon points="680,426 688,430 680,434" :fill="currentChampion.color" />
        </g>
      </svg>

      <!-- ── LEFT COLUMN: COCKPIT & 3D ROTATING PILOT STAGE (7 COLS) ── -->
      <div class="lg:col-span-7 relative flex flex-col z-10">
        <PortalMechaWarrior3DStage
          :active-index="activeIndex"
          :is-auto-rotating="isAutoRotating"
          @select-pilot="selectChampion"
        />
      </div>

      <!-- ── RIGHT COLUMN: TACTICAL INFORMATION DOSSIER (5 COLS) ── -->
      <div class="lg:col-span-5 relative flex flex-col justify-between z-20">
        <div
          class="h-full flex flex-col justify-between border-2 bg-[#080b12]/95 p-6 sm:p-7 mecha-cut-corners shadow-[0_0_40px_rgba(0,0,0,0.9)] transition-all duration-500 relative"
          :style="{ borderColor: currentChampion.color, boxShadow: `0 0 35px ${currentChampion.color}30` }"
        >
          <!-- Corner Accents -->
          <div
            class="pointer-events-none absolute -top-1 -right-1 h-6 w-6 border-t-2 border-r-2"
            :style="{ borderColor: currentChampion.color }"
          ></div>
          <div
            class="pointer-events-none absolute -bottom-1 -left-1 h-6 w-6 border-b-2 border-l-2"
            :style="{ borderColor: currentChampion.color }"
          ></div>

          <div class="space-y-4">
            <!-- Node 1: Header Callsign & Pilot Name (Connected to Mask HUD) -->
            <div class="border-b border-slate-800/90 pb-3.5 relative">
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-1.5 text-xs font-black uppercase" :style="{ color: currentChampion.color }">
                  <span>{{ currentChampion.weaponIcon }}</span>
                  <span>{{ currentChampion.codename }}</span>
                </div>
                <div class="text-[10px] font-bold text-slate-400 bg-slate-900/90 px-2 py-0.5 border border-slate-800 mecha-cut-tr">
                  {{ currentChampion.classId }}
                </div>
              </div>

              <h2 class="text-2xl sm:text-3xl font-black text-white mt-1">
                {{ currentChampion.name }}
              </h2>
              <div class="text-xs font-bold text-slate-300 pt-0.5">
                {{ currentChampion.role }}
              </div>
            </div>

            <!-- Node 2: Thesis Responsibilities (Connected to Core) -->
            <div class="space-y-1.5 font-sans relative">
              <div class="font-mono text-[11px] font-black uppercase text-amber-400 flex items-center gap-1.5">
                <span class="inline-block h-2 w-2 rounded-full" :style="{ backgroundColor: currentChampion.color }"></span>
                <span>// NHIỆM VỤ KỸ THUẬT & ĐỒ ÁN:</span>
              </div>
              <p class="text-xs text-slate-300 leading-relaxed">
                {{ currentChampion.duties }}
              </p>
            </div>

            <!-- Node 3: Specialized Cyber Weapon (Connected to Arm) -->
            <div class="space-y-1 border-t border-slate-800/80 pt-3 font-sans relative">
              <div class="font-mono text-[11px] font-black uppercase flex items-center gap-1.5" :style="{ color: currentChampion.color }">
                <span>⚡</span>
                <span>VŨ KHÍ: {{ currentChampion.weaponName.split('(')[0] }}</span>
              </div>
              <p class="text-[11.5px] text-slate-400 leading-relaxed">
                {{ currentChampion.weaponDesc }}
              </p>
            </div>

            <!-- Node 4: Combat Radar Stats (Connected to Base) -->
            <div class="space-y-2.5 border-t border-slate-800/80 pt-3 relative">
              <div class="flex justify-between items-center text-[10.5px] font-black text-slate-400">
                <span>CHỈ SỐ NĂNG LỰC TÁC CHIẾN</span>
                <span :style="{ color: currentChampion.color }">ĐỒNG BỘ: {{ currentChampion.stats.sync }}%</span>
              </div>

              <div class="grid grid-cols-2 gap-2 text-[10px] font-bold">
                <div>
                  <div class="flex justify-between text-slate-400 mb-0.5">
                    <span>BACKEND:</span>
                    <span :style="{ color: currentChampion.color }">{{ currentChampion.stats.code }}%</span>
                  </div>
                  <div class="h-1.5 w-full bg-slate-900 rounded-sm overflow-hidden">
                    <div class="h-full transition-all duration-500" :style="{ width: `${currentChampion.stats.code}%`, backgroundColor: currentChampion.color }"></div>
                  </div>
                </div>

                <div>
                  <div class="flex justify-between text-slate-400 mb-0.5">
                    <span>DEFENSE:</span>
                    <span :style="{ color: currentChampion.color }">{{ currentChampion.stats.defense }}%</span>
                  </div>
                  <div class="h-1.5 w-full bg-slate-900 rounded-sm overflow-hidden">
                    <div class="h-full transition-all duration-500" :style="{ width: `${currentChampion.stats.defense}%`, backgroundColor: currentChampion.color }"></div>
                  </div>
                </div>

                <div>
                  <div class="flex justify-between text-slate-400 mb-0.5">
                    <span>SYNC HUD:</span>
                    <span :style="{ color: currentChampion.color }">{{ currentChampion.stats.sync }}%</span>
                  </div>
                  <div class="h-1.5 w-full bg-slate-900 rounded-sm overflow-hidden">
                    <div class="h-full transition-all duration-500" :style="{ width: `${currentChampion.stats.sync}%`, backgroundColor: currentChampion.color }"></div>
                  </div>
                </div>

                <div>
                  <div class="flex justify-between text-slate-400 mb-0.5">
                    <span>AI POWER:</span>
                    <span :style="{ color: currentChampion.color }">{{ currentChampion.stats.power }}%</span>
                  </div>
                  <div class="h-1.5 w-full bg-slate-900 rounded-sm overflow-hidden">
                    <div class="h-full transition-all duration-500" :style="{ width: `${currentChampion.stats.power}%`, backgroundColor: currentChampion.color }"></div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Bottom 5 Pilot Quick Selection Buttons -->
          <div class="pt-3 border-t border-slate-800/80 mt-3">
            <div class="grid grid-cols-5 gap-1.5">
              <button
                v-for="(c, idx) in champions"
                :key="c.id"
                type="button"
                @click="selectChampion(idx)"
                class="p-1.5 text-center transition-all mecha-cut-tr flex flex-col items-center justify-center gap-0.5"
                :class="[
                  idx === activeIndex
                    ? 'border-2 text-white font-black scale-105 shadow-[0_0_15px_rgba(0,0,0,0.9)]'
                    : 'border border-slate-800 bg-[#0a0d14] text-slate-400 hover:text-slate-200'
                ]"
                :style="{
                  borderColor: idx === activeIndex ? c.color : undefined,
                  backgroundColor: idx === activeIndex ? '#0f1422' : undefined
                }"
              >
                <span class="text-xs">{{ c.weaponIcon }}</span>
                <span class="text-[8.5px] font-mono leading-none truncate max-w-full">
                  {{ c.name.split(' ').pop() }}
                </span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
@keyframes dash {
  to {
    stroke-dashoffset: -20;
  }
}
.animate-dash {
  animation: dash 1.2s linear infinite;
}
</style>
