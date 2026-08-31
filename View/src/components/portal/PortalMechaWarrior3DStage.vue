<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { mechaAudio } from '../../utils/portalAudio'

const props = defineProps({
  activeIndex: {
    type: Number,
    default: 3
  }
})

const emit = defineEmits(['update:activeIndex', 'selectPilot'])

const isShieldActive = ref(false)
const isOverdriveActive = ref(false)
const isLockOnActive = ref(false)
const mouseX = ref(0)
const mouseY = ref(0)
const isHovering = ref(false)

const pilots = [
  {
    id: 0,
    name: 'Phạm Văn Thành',
    callsign: 'AEGIS PRIME',
    role: 'CHỈ HUY HỆ THỐNG & KIẾN TRÚC SƯ BACKEND',
    avatar: '/pilots/pilot_thanh.jpg',
    cockpit: '/cockpits/cockpit_gold.jpg',
    color: '#ffcc00',
    glow: '#ffaa00',
    cockpitName: 'FLAGSHIP COMMAND BRIDGE // AURORA-01',
    systemStatus: 'ONLINE • CRDT HYBRID SYNC 99.99%',
    weapon: 'EX-01 QUANTUM BROADSWORD',
    quote: 'Bảo vệ toàn vẹn kiến trúc dữ liệu và điều phối hệ thống phân tán với độ trễ dưới 30ms.'
  },
  {
    id: 1,
    name: 'Hà Mạnh Hùng',
    callsign: 'PHANTOM FALCON',
    role: 'KỸ SƯ TRÍ TUỆ NHÂN TẠO & THỊ GIÁC MÁY TÍNH',
    avatar: '/pilots/pilot_hung.jpg',
    cockpit: '/cockpits/cockpit_orange.jpg',
    color: '#ff5500',
    glow: '#ff2200',
    cockpitName: 'SUPERSONIC INTERCEPTOR COCKPIT // FALCON-02',
    systemStatus: 'ONLINE • YOLOv11 + ArcFace 60FPS',
    weapon: 'EX-02 HYPER-VELOCITY PLASMA RAILGUN',
    quote: 'Quét và khóa mục tiêu sinh trắc học quang học trong phạm vi ±45° với độ chính xác tuyệt đối.'
  },
  {
    id: 2,
    name: 'Phạm Ngọc Hoài Anh',
    callsign: 'DREADNOUGHT VORTEX',
    role: 'KỸ SƯ HẠ TẦNG ĐÁM MÂY & DEVOPS BẢO MẬT',
    avatar: '/pilots/pilot_hoaianh.jpg',
    cockpit: '/cockpits/cockpit_cyan.jpg',
    color: '#00f0ff',
    glow: '#00b4d8',
    cockpitName: 'FORTRESS DEFENSE MATRIX // VORTEX-03',
    systemStatus: 'ONLINE • DOCKER & CADDY TLS ACTIVE',
    weapon: 'EX-03 TITAN HEAVY PARTICLE CANNON',
    quote: 'Thiết lập trường lực bảo mật đa tầng, tối ưu băng thông mạng và duy trì Uptime 99.99%.'
  },
  {
    id: 3,
    name: 'Vũ Tiến Đạt',
    callsign: 'SPECTRE STRIKER',
    role: 'KỸ SƯ FRONTEND UI/UX & REALTIME WEBRTC',
    avatar: '/pilots/pilot_dat.jpg',
    cockpit: '/cockpits/cockpit_purple.jpg',
    color: '#a855f7',
    glow: '#c084fc',
    cockpitName: 'STEALTH NEURAL MATRIX COCKPIT // SPECTRE-04',
    systemStatus: 'ONLINE • WEBRTC VOIP & HUD LIVE 60FPS',
    weapon: 'EX-04 DUAL HOLOGRAPHIC ENERGY DAGGERS',
    quote: 'Xây dựng giao diện buồng lái Mecha chiến thuật thời gian thực và đồng bộ thị giác sống động.'
  },
  {
    id: 4,
    name: 'Nguyễn Quốc Việt',
    callsign: 'TEMPEST JUGGERNAUT',
    role: 'KỸ SƯ THIẾT BỊ IOT & ỨNG DỤNG MOBILE',
    avatar: '/pilots/pilot_viet.jpg',
    cockpit: '/cockpits/cockpit_green.jpg',
    color: '#10b981',
    glow: '#059669',
    cockpitName: 'HEAVY SIEGE ARTILLERY COCKPIT // TEMPEST-05',
    systemStatus: 'ONLINE • BARRIER RELAY & TOTP SYNCHRONIZED',
    weapon: 'EX-05 THUNDERSTRIKE POWER HALBERD',
    quote: 'Điều khiển rào chắn cổng tự động trong 0.6 giây và kết nối ứng dụng di động bảo mật cao.'
  }
]

const currentPilot = computed(() => pilots[props.activeIndex] || pilots[0])

// Calculate 3D circular carousel offset position for each pilot
function getPilotTransform(index) {
  const total = pilots.length
  let offset = index - props.activeIndex
  if (offset > total / 2) offset -= total
  if (offset < -total / 2) offset += total

  const isSelected = offset === 0

  // 3D positioning
  const xOffset = offset * 145 // horizontal spacing
  const zOffset = isSelected ? 40 : -Math.abs(offset) * 80 // depth
  const yRot = offset * -22 // rotation angle towards center
  const scale = isSelected ? 1.08 : Math.max(0.72, 1 - Math.abs(offset) * 0.16)
  const opacity = isSelected ? 1 : Math.max(0.35, 0.7 - Math.abs(offset) * 0.2)

  return {
    transform: `translateX(${xOffset}px) translateZ(${zOffset}px) rotateY(${yRot}deg) scale(${scale})`,
    opacity,
    zIndex: isSelected ? 30 : 20 - Math.abs(offset),
    filter: isSelected
      ? 'none'
      : 'grayscale(100%) brightness(0.6) contrast(1.1) drop-shadow(0 0 10px rgba(0,0,0,0.8))'
  }
}

function handleMouseMove(e) {
  const rect = e.currentTarget.getBoundingClientRect()
  mouseX.value = ((e.clientX - rect.left) / rect.width - 0.5) * 2
  mouseY.value = ((e.clientY - rect.top) / rect.height - 0.5) * 2
  isHovering.value = true
}

function handleMouseLeave() {
  mouseX.value = 0
  mouseY.value = 0
  isHovering.value = false
}

function triggerOverdrive() {
  isOverdriveActive.value = true
  mechaAudio.playTargetLock()
  mechaAudio.playHeavyImpactDrop()
  setTimeout(() => {
    isOverdriveActive.value = false
  }, 1600)
}

function triggerShield() {
  isShieldActive.value = !isShieldActive.value
  if (isShieldActive.value) {
    mechaAudio.playEngage()
  } else {
    mechaAudio.playClick()
  }
}

function triggerLockOn() {
  isLockOnActive.value = !isLockOnActive.value
  if (isLockOnActive.value) {
    mechaAudio.playTargetLock()
  } else {
    mechaAudio.playHover()
  }
}
</script>

<template>
  <div
    class="relative w-full overflow-hidden rounded-2xl border-2 border-amber-500/50 bg-[#04060a] shadow-[0_0_60px_rgba(0,0,0,0.9)] select-none"
    @mousemove="handleMouseMove"
    @mouseleave="handleMouseLeave"
    style="perspective: 1400px;"
  >
    <!-- ── 1. DYNAMIC MECHA COCKPIT BACKGROUND LAYER ── -->
    <div class="relative h-[480px] sm:h-[540px] w-full overflow-hidden">
      <!-- Cockpit Wallpapers with crossfade -->
      <transition-group name="cockpit-fade">
        <img
          :key="currentPilot.cockpit"
          :src="currentPilot.cockpit"
          :alt="currentPilot.cockpitName"
          class="absolute inset-0 h-full w-full object-cover object-center scale-105 transition-transform duration-700 ease-out"
          :style="{
            transform: `scale(${isOverdriveActive ? 1.12 : 1.05}) translate(${mouseX * -10}px, ${mouseY * -6}px)`,
            filter: isOverdriveActive ? 'brightness(1.25) contrast(1.2)' : 'brightness(0.85) contrast(1.05)'
          }"
        />
      </transition-group>

      <!-- Dynamic Ambient Cockpit Color Vignette Overlay -->
      <div
        class="pointer-events-none absolute inset-0 transition-colors duration-700 mix-blend-color"
        :style="{ backgroundColor: currentPilot.color, opacity: 0.25 }"
      ></div>

      <!-- Cockpit Glass Canopy Reflection Lines -->
      <div class="pointer-events-none absolute inset-0 bg-gradient-to-tr from-transparent via-white/[0.04] to-transparent opacity-60"></div>
      <div class="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_center,transparent_40%,rgba(0,0,0,0.7)_90%)]"></div>

      <!-- ── 2. DYNAMIC COCKPIT HUD TELEMETRY OVERLAYS ── -->
      <!-- Top Pilot Callsign & Cockpit Bridge Name -->
      <div class="pointer-events-none absolute top-3 inset-x-4 flex items-center justify-between z-40 font-mono">
        <div class="flex items-center gap-2 px-3 py-1 bg-[#07090e]/90 border border-amber-500/50 mecha-cut-tr shadow-[0_0_20px_rgba(0,0,0,0.8)]">
          <span class="h-2 w-2 rounded-full animate-ping" :style="{ backgroundColor: currentPilot.color }"></span>
          <span class="text-xs font-black text-white tracking-wider">{{ currentPilot.cockpitName }}</span>
        </div>

        <div class="hidden sm:flex items-center gap-2 px-3 py-1 bg-[#07090e]/90 border border-slate-700 mecha-cut-tr text-[10px] font-bold text-slate-300">
          <span>HỆ THỐNG: <span class="text-emerald-400 font-black">{{ currentPilot.systemStatus }}</span></span>
        </div>
      </div>

      <!-- Hexagonal Cockpit Quantum Forcefield -->
      <div
        v-if="isShieldActive"
        class="pointer-events-none absolute inset-4 rounded-xl border-2 border-cyan-400/90 bg-cyan-500/10 backdrop-blur-[1px] animate-pulse flex items-center justify-center shadow-[0_0_60px_rgba(0,240,255,0.6)] z-30"
      >
        <div class="font-mono text-xs font-black text-cyan-300 tracking-widest bg-slate-950/90 px-4 py-1.5 border border-cyan-400 mecha-cut-tr shadow-[0_0_25px_#00f0ff]">
          ⚡ COCKPIT FORCEFIELD BARRIER // ACTIVE 100%
        </div>
      </div>

      <!-- Tactical Lock-on Radar HUD -->
      <div
        v-if="isLockOnActive"
        class="pointer-events-none absolute inset-0 flex items-center justify-center z-30"
      >
        <div class="h-56 w-56 rounded-full border-2 border-red-500/80 animate-ping opacity-60"></div>
        <div class="absolute h-72 w-72 rounded-full border border-dashed border-red-400/80 animate-spin-slow"></div>
        <div class="absolute font-mono text-[10px] font-black text-red-400 bg-red-950/90 px-3 py-1 border border-red-500 -top-4 shadow-[0_0_20px_#ef4444]">
          [NEURAL LINK SYNCHRONIZED: 100%]
        </div>
      </div>

      <!-- ── 3. 5 HOLOGRAPHIC WARRIOR 3D ROTATING TURNTABLE ── -->
      <div class="absolute inset-0 flex items-end justify-center pb-6 z-20 pointer-events-none" style="transform-style: preserve-3d;">
        <div
          v-for="(pilot, idx) in pilots"
          :key="pilot.id"
          @click="emit('selectPilot', idx)"
          class="absolute bottom-2 flex flex-col items-center cursor-pointer transition-all duration-700 ease-out pointer-events-auto group"
          :style="getPilotTransform(idx)"
        >
          <!-- Holographic Pilot Silhouette / Full Color Hero Frame -->
          <div class="relative flex items-center justify-center">
            <!-- Glowing Pedestal Ring for active pilot -->
            <div
              v-if="idx === activeIndex"
              class="pointer-events-none absolute -bottom-4 h-16 w-56 rounded-full border-2 border-dashed animate-spin-slow opacity-80"
              :style="{ borderColor: pilot.color, boxShadow: `0 0 35px ${pilot.glow}70` }"
            ></div>

            <!-- Authentic Mecha Pilot Image -->
            <div
              class="relative rounded-2xl overflow-hidden transition-all duration-500"
              :class="[
                idx === activeIndex
                  ? 'border-2 shadow-[0_0_40px_rgba(0,0,0,0.9)]'
                  : 'border border-slate-800/80 opacity-60 hover:opacity-90'
              ]"
              :style="{
                borderColor: idx === activeIndex ? pilot.color : '#1e293b',
                boxShadow: idx === activeIndex ? `0 0 35px ${pilot.glow}60` : 'none'
              }"
            >
              <img
                :src="pilot.avatar"
                :alt="pilot.name"
                class="h-[320px] sm:h-[380px] w-[240px] sm:w-[280px] object-cover object-top select-none"
              />

              <!-- Glowing Neon Edge Frame & Scanning Beam (Active Only) -->
              <div
                v-if="idx === activeIndex"
                class="pointer-events-none absolute inset-0 bg-gradient-to-b from-transparent via-transparent to-black/80"
              ></div>

              <!-- Cyber Scanning Laser Line -->
              <div
                v-if="idx === activeIndex"
                class="pointer-events-none absolute inset-x-0 h-1 bg-gradient-to-r from-transparent via-white to-transparent animate-laser-scan opacity-70"
                :style="{ backgroundColor: pilot.color }"
              ></div>

              <!-- Inactive Holographic Scanlines Overlay -->
              <div
                v-if="idx !== activeIndex"
                class="pointer-events-none absolute inset-0 bg-[repeating-linear-gradient(0deg,transparent,transparent_3px,rgba(0,0,0,0.4)_4px)]"
              ></div>
            </div>

            <!-- Floating Active Aura Glow behind center pilot -->
            <div
              v-if="idx === activeIndex"
              class="pointer-events-none absolute -inset-4 rounded-3xl blur-2xl opacity-40 mix-blend-screen transition-all duration-700 -z-10"
              :style="{ backgroundColor: pilot.color }"
            ></div>
          </div>

          <!-- Pilot Name & Callsign Tag -->
          <div
            class="mt-2 px-3 py-1 rounded-md text-center transition-all duration-500 font-mono"
            :class="[
              idx === activeIndex
                ? 'bg-[#07090e]/95 border-2 text-white font-black scale-105 mecha-cut-tr shadow-[0_0_20px_rgba(0,0,0,0.8)]'
                : 'bg-[#07090e]/60 border border-slate-800 text-slate-400 text-xs hover:text-slate-200'
            ]"
            :style="{ borderColor: idx === activeIndex ? pilot.color : '#334155' }"
          >
            <div class="text-[11px] uppercase tracking-wider" :style="{ color: idx === activeIndex ? pilot.color : '#94a3b8' }">
              // {{ pilot.callsign }}
            </div>
            <div class="text-xs font-black">{{ pilot.name }}</div>
          </div>
        </div>
      </div>

      <!-- ── 4. COCKPIT INTERACTIVE ACTION CONTROLS ── -->
      <div class="absolute bottom-3 inset-x-4 flex flex-wrap items-center justify-between gap-2 z-40 font-mono text-[10px] font-black">
        <!-- Action Overdrive Buttons -->
        <div class="flex items-center gap-1.5">
          <button
            type="button"
            @click="triggerOverdrive"
            class="px-3 py-1.5 bg-red-950/90 hover:bg-red-600 text-red-300 hover:text-white border border-red-500/60 transition-all mecha-cut-tr shadow-[0_0_15px_rgba(239,68,68,0.4)] flex items-center gap-1.5 active:scale-95"
          >
            <span>⚡ QUÁ TẢI (OVERDRIVE)</span>
          </button>

          <button
            type="button"
            @click="triggerShield"
            class="px-3 py-1.5 transition-all mecha-cut-tr flex items-center gap-1.5 active:scale-95"
            :class="[
              isShieldActive
                ? 'bg-cyan-500 text-slate-950 shadow-[0_0_20px_#00f0ff] border border-cyan-300'
                : 'bg-cyan-950/90 hover:bg-cyan-600 text-cyan-300 hover:text-white border border-cyan-500/60 shadow-[0_0_12px_rgba(6,182,212,0.3)]'
            ]"
          >
            <span>🛡️ KHIÊN BUỒNG LÁI</span>
          </button>

          <button
            type="button"
            @click="triggerLockOn"
            class="px-3 py-1.5 transition-all mecha-cut-tr flex items-center gap-1.5 active:scale-95"
            :class="[
              isLockOnActive
                ? 'bg-red-600 text-white shadow-[0_0_20px_#ff0055] border border-red-400'
                : 'bg-slate-900/90 hover:bg-red-950 text-slate-300 hover:text-red-300 border border-slate-700'
            ]"
          >
            <span>🎯 KHÓA MỤC TIÊU</span>
          </button>
        </div>

        <!-- Quick 5 Pilot Carousel Dots -->
        <div class="flex items-center gap-2 bg-[#07090e]/90 px-3 py-1 border border-slate-800 mecha-cut-tr">
          <button
            v-for="(pilot, idx) in pilots"
            :key="pilot.id"
            @click="emit('selectPilot', idx)"
            class="h-3 w-3 rounded-full transition-all"
            :style="{
              backgroundColor: idx === activeIndex ? pilot.color : '#334155',
              transform: idx === activeIndex ? 'scale(1.3)' : 'scale(1)',
              boxShadow: idx === activeIndex ? `0 0 10px ${pilot.color}` : 'none'
            }"
            :title="pilot.name"
          ></button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
@keyframes laserScan {
  0% { top: 0%; opacity: 0; }
  20% { opacity: 0.8; }
  80% { opacity: 0.8; }
  100% { top: 100%; opacity: 0; }
}
.animate-laser-scan {
  animation: laserScan 3s ease-in-out infinite;
}

@keyframes spinSlow {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}
.animate-spin-slow {
  animation: spinSlow 20s linear infinite;
}

.cockpit-fade-enter-active,
.cockpit-fade-leave-active {
  transition: opacity 0.8s ease;
}
.cockpit-fade-enter-from,
.cockpit-fade-leave-to {
  opacity: 0;
}
</style>
