<script setup>
import { ref, computed, watch, onUnmounted } from 'vue'
import { mechaAudio } from '../../utils/portalAudio'

const props = defineProps({
  activeIndex: {
    type: Number,
    default: 0
  },
  isAutoRotating: {
    type: Boolean,
    default: true
  }
})

const emit = defineEmits(['update:activeIndex', 'selectPilot'])

const isShieldActive = ref(false)
const isOverdriveActive = ref(false)
const isLockOnActive = ref(false)
const isRotating = ref(false)
let rotateTimer = null

watch(() => props.activeIndex, () => {
  isRotating.value = true
  if (rotateTimer) clearTimeout(rotateTimer)
  rotateTimer = setTimeout(() => {
    isRotating.value = false
  }, 650)
})

onUnmounted(() => {
  if (rotateTimer) clearTimeout(rotateTimer)
})

const mouseX = ref(0)
const mouseY = ref(0)

const pilots = [
  {
    id: 0,
    name: 'Phạm Văn Thành',
    callsign: 'SHADOW AEGIS // PRIME SOVEREIGN',
    role: 'TRƯỞNG NHÓM & KIẾN TRÚC SƯ TRƯỞNG BACKEND',
    avatar: '/pilots/pilot_thanh.jpg',
    cockpit: '/cockpits/cockpit_gold.jpg',
    cockpitFilter: 'hue-rotate(85deg) saturate(1.8) contrast(1.15)',
    color: '#a855f7', // Ethereal Purple & Violet Shadow
    glow: '#9333ea',
    cockpitName: 'FLAGSHIP SHADOW BRIDGE // AEGIS-00',
    systemStatus: 'NOMINAL • CRDT HYBRID SYNC 99.99%',
    weapon: 'EX-01 QUANTUM BROADSWORD',
    quote: 'Chỉ huy kiến trúc phân tán với hệ thống giáp bóng đêm ma mị và phòng thủ bất khả xâm phạm.'
  },
  {
    id: 1,
    name: 'Hà Mạnh Hùng',
    callsign: 'PHANTOM FALCON',
    role: 'KỸ SƯ TRÍ TUỆ NHÂN TẠO & THỊ GIÁC MÁY TÍNH',
    avatar: '/pilots/pilot_hung.jpg',
    cockpit: '/cockpits/cockpit_cyan.jpg',
    cockpitFilter: 'none',
    color: '#00f0ff', // Electric Cyan
    glow: '#0284c7',
    cockpitName: 'QUANTUM VISION COCKPIT // FALCON-02',
    systemStatus: 'ONLINE • YOLOv11 + ArcFace 60FPS',
    weapon: 'EX-02 HYPER-VELOCITY PLASMA RAILGUN',
    quote: 'Quét và khóa mục tiêu sinh trắc học quang học trong phạm vi ±45° với độ chính xác tuyệt đối.'
  },
  {
    id: 2,
    name: 'Phạm Ngọc Hoài Anh',
    callsign: 'DREADNOUGHT VORTEX',
    role: 'KỸ SƯ HẠ TẦNG ĐÁM MÂY & DEVOPS BẢO MẬT',
    avatar: '/pilots/pilot_hoaianh_v8.jpg?v=8',
    cockpit: '/cockpits/cockpit_orange.jpg',
    cockpitFilter: 'none',
    color: '#ef4444', // Crimson Red
    glow: '#dc2626',
    cockpitName: 'CRIMSON ARSENAL COCKPIT // VORTEX-03',
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
    cockpitFilter: 'none',
    color: '#eab308', // Amber Gold
    glow: '#ca8a04',
    cockpitName: 'STEALTH NEURAL MATRIX // SPECTRE-04',
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
    cockpitFilter: 'none',
    color: '#10b981', // Matrix Green
    glow: '#059669',
    cockpitName: 'HEAVY SIEGE ARTILLERY // TEMPEST-05',
    systemStatus: 'ONLINE • BARRIER RELAY & TOTP SYNCHRONIZED',
    weapon: 'EX-05 THUNDERSTRIKE POWER HALBERD',
    quote: 'Điều khiển rào chắn cổng tự động trong 0.6 giây và kết nối ứng dụng di động bảo mật cao.'
  }
]

const currentPilot = computed(() => pilots[props.activeIndex] || pilots[0])

function getPilotTransform(index) {
  const total = pilots.length
  let offset = index - props.activeIndex
  while (offset > total / 2) offset -= total
  while (offset < -total / 2) offset += total

  const isSelected = offset === 0

  // 3D Circular Turntable Geometry (72 deg per slot)
  const angleDeg = offset * 72
  const angleRad = (angleDeg * Math.PI) / 180

  const rx = 195 // Horizontal orbit radius
  const rz = 125 // Depth orbit radius

  const x = Math.sin(angleRad) * rx
  const z = (Math.cos(angleRad) - 1) * rz
  const yRot = -offset * 25

  // Depth factor: 1.0 at front center, down to 0.1 at back
  const depthFactor = (Math.cos(angleRad) + 1) / 2
  const scale = isSelected ? 1.08 : 0.65 + depthFactor * 0.22
  const opacity = isSelected ? 1.0 : 0.32 + depthFactor * 0.35

  const filter = isSelected
    ? 'grayscale(0%) brightness(1.05) contrast(1.05) drop-shadow(0 0 25px ' + pilots[index].glow + '80)'
    : 'grayscale(100%) brightness(' + (0.35 + depthFactor * 0.25).toFixed(2) + ') contrast(1.15) drop-shadow(0 0 10px rgba(0,0,0,0.95))'

  return {
    transform: `translateX(${x.toFixed(1)}px) translateZ(${z.toFixed(1)}px) rotateY(${yRot}deg) scale(${scale.toFixed(3)})`,
    opacity,
    zIndex: isSelected ? 40 : Math.round(depthFactor * 25) + 5,
    filter
  }
}

function handleMouseMove(e) {
  const rect = e.currentTarget.getBoundingClientRect()
  mouseX.value = ((e.clientX - rect.left) / rect.width - 0.5) * 2
  mouseY.value = ((e.clientY - rect.top) / rect.height - 0.5) * 2
}

function handleMouseLeave() {
  mouseX.value = 0
  mouseY.value = 0
}

function triggerOverdrive() {
  isOverdriveActive.value = true
  mechaAudio.playTargetLock()
  mechaAudio.playHeavyImpactDrop()
  setTimeout(() => {
    isOverdriveActive.value = false
  }, 1500)
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
    class="relative h-[520px] sm:h-[570px] w-full overflow-hidden rounded-2xl border-2 border-amber-500/40 bg-[#04060a] shadow-[0_0_50px_rgba(0,0,0,0.9)] select-none flex flex-col justify-between"
    @mousemove="handleMouseMove"
    @mouseleave="handleMouseLeave"
    style="perspective: 1200px;"
  >
    <!-- ── 1. FIRST-PERSON POV COCKPIT BACKGROUNDS (SMOOTH CROSS-FADE TRANSITION) ── -->
    <div class="absolute inset-0 overflow-hidden pointer-events-none">
      <img
        v-for="(pilot, pIdx) in pilots"
        :key="pilot.id"
        :src="pilot.cockpit"
        :alt="pilot.cockpitName"
        class="absolute inset-0 h-full w-full object-cover object-center transition-all duration-1000 ease-in-out"
        :class="pIdx === activeIndex ? 'opacity-100 scale-105' : 'opacity-0 scale-100'"
        :style="{
          transform: `scale(${pIdx === activeIndex ? (isOverdriveActive ? 1.12 : 1.05) : 1.0}) translate(${mouseX * -8}px, ${mouseY * -5}px)`,
          filter: `${pilot.cockpitFilter} ${isOverdriveActive ? 'brightness(1.3) contrast(1.2)' : 'brightness(0.85) contrast(1.05)'}`
        }"
      />
    </div>

    <!-- Cockpit Ambient Tint Overlay (Smooth Color Shift) -->
    <div
      class="pointer-events-none absolute inset-0 transition-colors duration-1000 ease-in-out mix-blend-color"
      :style="{ backgroundColor: currentPilot.color, opacity: 0.28 }"
    ></div>

    <div class="pointer-events-none absolute inset-0 bg-gradient-to-tr from-transparent via-white/[0.04] to-transparent opacity-60"></div>
    <div class="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_center,transparent_40%,rgba(0,0,0,0.75)_90%)]"></div>

    <!-- ── 2. TOP HEADER HUD ── -->
    <div class="relative pt-3 px-3.5 flex items-center justify-between z-40 font-mono pointer-events-none">
      <div class="flex items-center gap-2 px-2.5 py-1 bg-[#07090e]/95 border border-amber-500/50 mecha-cut-tr shadow-[0_0_20px_rgba(0,0,0,0.9)]">
        <span class="h-2 w-2 rounded-full animate-ping" :style="{ backgroundColor: currentPilot.color }"></span>
        <span class="text-[11px] font-black text-white tracking-wider truncate max-w-[210px] sm:max-w-none">{{ currentPilot.cockpitName }}</span>
      </div>

      <div class="flex items-center gap-2 px-2 py-0.5 bg-[#07090e]/95 border border-slate-700 mecha-cut-tr text-[9.5px] font-bold text-slate-300">
        <span>TỰ XOAY: <span :class="isAutoRotating ? 'text-emerald-400' : 'text-slate-500'">{{ isAutoRotating ? 'ĐANG BẬT' : 'TẠM DỪNG' }}</span></span>
      </div>
    </div>

    <!-- Shield Overlay -->
    <div
      v-if="isShieldActive"
      class="pointer-events-none absolute inset-3 rounded-xl border-2 border-cyan-400/90 bg-cyan-500/10 backdrop-blur-[1px] animate-pulse flex items-center justify-center shadow-[0_0_50px_rgba(0,240,255,0.6)] z-30"
    >
      <div class="font-mono text-[11px] font-black text-cyan-300 tracking-widest bg-slate-950/90 px-3 py-1 border border-cyan-400 mecha-cut-tr">
        ⚡ COCKPIT FORCEFIELD // ACTIVE 100%
      </div>
    </div>

    <!-- Lock On Radar Reticle -->
    <div
      v-if="isLockOnActive"
      class="pointer-events-none absolute inset-0 flex items-center justify-center z-30"
    >
      <div class="h-44 w-44 rounded-full border-2 border-red-500/80 animate-ping opacity-60"></div>
      <div class="absolute h-60 w-60 rounded-full border border-dashed border-red-400/80 animate-spin-slow"></div>
    </div>

    <!-- ── DYNAMIC HOLO-WARP WAVE & SCANLINE BURST ON ROTATION ── -->
    <transition name="warp-flash">
      <div
        v-if="isRotating"
        class="pointer-events-none absolute inset-0 z-30 flex items-center justify-center mix-blend-screen overflow-hidden"
      >
        <div
          class="h-80 w-80 rounded-full border-2 border-dashed animate-ping opacity-70"
          :style="{ borderColor: currentPilot.color, boxShadow: `0 0 50px ${currentPilot.glow}` }"
        ></div>
        <div
          class="absolute inset-x-0 h-1 bg-gradient-to-r from-transparent via-white to-transparent animate-laser-sweep"
          :style="{ backgroundColor: currentPilot.color, boxShadow: `0 0 25px ${currentPilot.color}` }"
        ></div>
      </div>
    </transition>

    <!-- ── 3. 5 PILOTS 3D ROTATING TURNTABLE CAROUSEL (SMOOTH CIRCULAR ORBIT) ── -->
    <div class="relative flex-1 flex items-end justify-center pb-12 z-20 pointer-events-none" style="transform-style: preserve-3d;">
      <div
        v-for="(pilot, idx) in pilots"
        :key="pilot.id"
        @click="emit('selectPilot', idx)"
        class="absolute bottom-2 flex flex-col items-center cursor-pointer pointer-events-auto group pilot-card-turntable"
        :style="getPilotTransform(idx)"
      >
        <div class="relative flex items-center justify-center">
          <!-- Active Pedestal Base Ring (Only for Active Pilot) -->
          <div
            v-if="idx === activeIndex"
            class="pointer-events-none absolute -bottom-3 h-12 w-48 rounded-full border border-dashed animate-spin-slow opacity-90 transition-opacity duration-700"
            :style="{ borderColor: pilot.color, boxShadow: `0 0 35px ${pilot.glow}` }"
          ></div>

          <!-- Pilot Photo Frame (Sharp & Vivid vs. Grayscale & Hidden) -->
          <div
            class="relative rounded-2xl overflow-hidden transition-all duration-700"
            :class="[
              idx === activeIndex
                ? 'border-2 shadow-[0_0_40px_rgba(0,0,0,0.95)]'
                : 'border border-slate-800/80'
            ]"
            :style="{
              borderColor: idx === activeIndex ? pilot.color : '#1e293b',
              boxShadow: idx === activeIndex ? `0 0 35px ${pilot.glow}70` : 'none'
            }"
          >
            <img
              :src="pilot.avatar"
              :alt="pilot.name"
              class="h-[300px] sm:h-[350px] w-[220px] sm:w-[250px] object-cover object-top select-none"
            />

            <!-- Active Neon Glow Frame & Scanning Laser -->
            <div
              v-if="idx === activeIndex"
              class="pointer-events-none absolute inset-0 bg-gradient-to-b from-transparent via-transparent to-black/85"
            ></div>

            <div
              v-if="idx === activeIndex"
              class="pointer-events-none absolute inset-x-0 h-1 bg-gradient-to-r from-transparent via-white to-transparent animate-laser-scan opacity-80"
              :style="{ backgroundColor: pilot.color }"
            ></div>

            <!-- Inactive Darkening & Scanlines Filter -->
            <div
              v-if="idx !== activeIndex"
              class="pointer-events-none absolute inset-0 bg-black/40 bg-[repeating-linear-gradient(0deg,transparent,transparent,3px,rgba(0,0,0,0.5)_4px)]"
            ></div>
          </div>

          <!-- Active Aura Glow Flare -->
          <div
            v-if="idx === activeIndex"
            class="pointer-events-none absolute -inset-3 rounded-3xl blur-xl opacity-45 mix-blend-screen transition-all duration-700 -z-10"
            :style="{ backgroundColor: pilot.color }"
          ></div>
        </div>

        <!-- Pilot Name Tag -->
        <div
          class="mt-1.5 px-2.5 py-0.5 rounded text-center transition-all duration-700 font-mono"
          :class="[
            idx === activeIndex
              ? 'bg-[#07090e]/95 border text-white font-black scale-105 mecha-cut-tr shadow-[0_0_15px_rgba(0,0,0,0.8)]'
              : 'bg-[#07090e]/60 border border-slate-800 text-slate-500 text-[10px]'
          ]"
          :style="{ borderColor: idx === activeIndex ? pilot.color : '#334155' }"
        >
          <div class="text-[9.5px] font-black" :style="{ color: idx === activeIndex ? pilot.color : '#64748b' }">
            {{ pilot.callsign.split('//')[0] }}
          </div>
          <div class="text-[11px] font-bold">{{ pilot.name }}</div>
        </div>
      </div>
    </div>

    <!-- ── 4. COCKPIT BOTTOM CONTROLS & DOTS ── -->
    <div class="relative pb-2.5 px-3.5 flex items-center justify-between gap-1.5 z-40 font-mono text-[9.5px] font-black bg-gradient-to-t from-[#04060a]/90 to-transparent pt-3">
      <div class="flex items-center gap-1">
        <button
          type="button"
          @click="triggerOverdrive"
          class="px-2 py-1 bg-red-950/90 hover:bg-red-600 text-red-300 hover:text-white border border-red-500/60 transition-all mecha-cut-tr active:scale-95"
        >
          <span>⚡ QUÁ TẢI</span>
        </button>

        <button
          type="button"
          @click="triggerShield"
          class="px-2 py-1 transition-all mecha-cut-tr active:scale-95"
          :class="[
            isShieldActive
              ? 'bg-cyan-500 text-slate-950 border border-cyan-300'
              : 'bg-cyan-950/90 hover:bg-cyan-600 text-cyan-300 border border-cyan-500/60'
          ]"
        >
          <span>🛡️ KHIÊN</span>
        </button>

        <button
          type="button"
          @click="triggerLockOn"
          class="px-2 py-1 transition-all mecha-cut-tr active:scale-95"
          :class="[
            isLockOnActive
              ? 'bg-red-600 text-white border border-red-400'
              : 'bg-slate-900/90 text-slate-300 border border-slate-700'
          ]"
        >
          <span>🎯 KHÓA</span>
        </button>
      </div>

      <!-- Quick 5 Dots -->
      <div class="flex items-center gap-1.5 bg-[#07090e]/90 px-2 py-1 border border-slate-800 mecha-cut-tr">
        <button
          v-for="(pilot, idx) in pilots"
          :key="pilot.id"
          @click="emit('selectPilot', idx)"
          class="h-2.5 w-2.5 rounded-full transition-all"
          :style="{
            backgroundColor: idx === activeIndex ? pilot.color : '#334155',
            transform: idx === activeIndex ? 'scale(1.3)' : 'scale(1)',
            boxShadow: idx === activeIndex ? `0 0 8px ${pilot.color}` : 'none'
          }"
          :title="pilot.name"
        ></button>
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

@keyframes laserSweep {
  0% { top: 0%; opacity: 0; }
  30% { opacity: 0.9; }
  70% { opacity: 0.9; }
  100% { top: 100%; opacity: 0; }
}
.animate-laser-sweep {
  animation: laserSweep 0.65s ease-in-out forwards;
}

.warp-flash-enter-active,
.warp-flash-leave-active {
  transition: opacity 0.3s ease;
}
.warp-flash-enter-from,
.warp-flash-leave-to {
  opacity: 0;
}

.pilot-card-turntable {
  transition: transform 0.85s cubic-bezier(0.22, 1, 0.36, 1),
              opacity 0.85s cubic-bezier(0.22, 1, 0.36, 1),
              filter 0.85s cubic-bezier(0.22, 1, 0.36, 1);
  will-change: transform, opacity, filter;
}
</style>
