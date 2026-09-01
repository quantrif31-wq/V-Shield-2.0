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

const wheelRadius = 270
const currentAngle = ref(-props.activeIndex * 72)
let lastIndex = props.activeIndex

const isDragging = ref(false)
const startX = ref(0)
const startAngle = ref(0)
const hasDragged = ref(false)

watch(() => props.activeIndex, (newVal) => {
  if (isDragging.value) return
  isRotating.value = true
  if (rotateTimer) clearTimeout(rotateTimer)
  rotateTimer = setTimeout(() => {
    isRotating.value = false
  }, 2000)

  // Calculate shortest continuous rotational step
  let diff = newVal - lastIndex
  if (diff > 2) diff -= 5
  if (diff < -2) diff += 5

  currentAngle.value -= diff * 72
  lastIndex = newVal
})

function handleDragStart(clientX) {
  isDragging.value = true
  hasDragged.value = false
  startX.value = clientX
  startAngle.value = currentAngle.value
}

function handleDragMove(clientX) {
  if (!isDragging.value) return
  const deltaX = clientX - startX.value
  if (Math.abs(deltaX) > 6) {
    hasDragged.value = true
  }
  // Drag rotation sensitivity: 0.35 deg per pixel
  currentAngle.value = startAngle.value + deltaX * 0.35

  // Estimate active index while dragging
  const step = 72
  let estimatedIndex = Math.round(-currentAngle.value / step) % pilots.length
  if (estimatedIndex < 0) estimatedIndex += pilots.length
  if (estimatedIndex !== props.activeIndex) {
    emit('selectPilot', estimatedIndex)
  }
}

function handleDragEnd() {
  if (!isDragging.value) return
  isDragging.value = false

  // Snap smoothly to nearest 72 deg slot
  const step = 72
  const targetSlot = Math.round(currentAngle.value / step)
  currentAngle.value = targetSlot * step

  let finalIndex = (-targetSlot) % pilots.length
  if (finalIndex < 0) finalIndex += pilots.length
  lastIndex = finalIndex

  emit('selectPilot', finalIndex)
  mechaAudio.playTargetLock()
}

function onMouseDown(e) {
  // Prevent dragging on control buttons
  if (e.target.closest('button')) return
  handleDragStart(e.clientX)
  window.addEventListener('mousemove', onMouseMoveWindow)
  window.addEventListener('mouseup', onMouseUpWindow)
}

function onMouseMoveWindow(e) {
  handleDragMove(e.clientX)
}

function onMouseUpWindow() {
  window.removeEventListener('mousemove', onMouseMoveWindow)
  window.removeEventListener('mouseup', onMouseUpWindow)
  handleDragEnd()
}

function onTouchStart(e) {
  if (e.target.closest('button')) return
  if (e.touches.length > 0) {
    handleDragStart(e.touches[0].clientX)
  }
}

function onTouchMove(e) {
  if (e.touches.length > 0) {
    handleDragMove(e.touches[0].clientX)
  }
}

function onTouchEnd() {
  handleDragEnd()
}

onUnmounted(() => {
  if (rotateTimer) clearTimeout(rotateTimer)
  window.removeEventListener('mousemove', onMouseMoveWindow)
  window.removeEventListener('mouseup', onMouseUpWindow)
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

function onCardClick(index) {
  if (hasDragged.value) return
  emit('selectPilot', index)
}

function prevPilot() {
  const prev = (props.activeIndex - 1 + pilots.length) % pilots.length
  emit('selectPilot', prev)
}

function nextPilot() {
  const next = (props.activeIndex + 1) % pilots.length
  emit('selectPilot', next)
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
    class="relative h-[520px] sm:h-[570px] w-full overflow-hidden rounded-2xl border-2 border-amber-500/40 bg-[#04060a] shadow-[0_0_50px_rgba(0,0,0,0.9)] select-none flex flex-col justify-between cursor-grab active:cursor-grabbing"
    @mousemove="handleMouseMove"
    @mouseleave="handleMouseLeave"
    @mousedown="onMouseDown"
    @touchstart="onTouchStart"
    @touchmove="onTouchMove"
    @touchend="onTouchEnd"
  >
    <!-- ── 1. FIRST-PERSON POV COCKPIT BACKGROUNDS (SMOOTH CROSS-FADE TRANSITION) ── -->
    <div class="absolute inset-0 overflow-hidden pointer-events-none">
      <img
        v-for="(pilot, pIdx) in pilots"
        :key="pilot.id"
        :src="pilot.cockpit"
        :alt="pilot.cockpitName"
        class="absolute inset-0 h-full w-full object-cover object-center transition-opacity duration-[2000ms] ease-in-out"
        :class="pIdx === activeIndex ? 'opacity-100' : 'opacity-0'"
        :style="{
          transform: `scale(${pIdx === activeIndex ? (isOverdriveActive ? 1.12 : 1.05) : 1.0}) translate(${mouseX * -8}px, ${mouseY * -5}px)`,
          filter: `${pilot.cockpitFilter} ${isOverdriveActive ? 'brightness(1.3) contrast(1.2)' : 'brightness(0.85) contrast(1.05)'}`
        }"
      />
    </div>

    <!-- Cockpit Ambient Tint Overlay (Smooth Color Shift) -->
    <div
      class="pointer-events-none absolute inset-0 transition-colors duration-[2000ms] ease-in-out mix-blend-color"
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
    </div>

    <!-- ── 3. 3D REVOLVING CYLINDER WHEEL CAROUSEL (PHYSICAL CENTRAL AXIS ROTATION) ── -->
    <div
      class="relative flex-1 flex items-center justify-center pb-8 z-20 pointer-events-none"
      style="perspective: 1200px;"
    >
      <!-- 3D Turntable Perspective Base Platform on Floor -->
      <div
        class="pointer-events-none absolute bottom-4 h-20 w-[92%] max-w-[640px] rounded-full border border-amber-500/30 bg-gradient-to-t from-amber-500/10 to-transparent blur-[0.5px]"
        style="transform: rotateX(72deg);"
      ></div>

      <!-- Quick Floating Stage Navigation Arrows -->
      <button
        type="button"
        @click.stop="prevPilot"
        class="pointer-events-auto absolute left-2 top-1/2 -translate-y-1/2 z-40 p-2 bg-[#07090e]/85 hover:bg-amber-500/20 text-slate-400 hover:text-amber-400 border border-slate-700 hover:border-amber-500/60 rounded-lg transition-all active:scale-90"
        aria-label="Phi công trước"
      >
        <span class="text-sm font-black">◀</span>
      </button>

      <button
        type="button"
        @click.stop="nextPilot"
        class="pointer-events-auto absolute right-2 top-1/2 -translate-y-1/2 z-40 p-2 bg-[#07090e]/85 hover:bg-amber-500/20 text-slate-400 hover:text-amber-400 border border-slate-700 hover:border-amber-500/60 rounded-lg transition-all active:scale-90"
        aria-label="Phi công tiếp theo"
      >
        <span class="text-sm font-black">▶</span>
      </button>

      <!-- ── 3D CYLINDER WHEEL (SPINS CONTINUOUSLY AROUND Y-AXIS) ── -->
      <div
        class="revolving-3d-wheel relative w-[210px] sm:w-[240px] h-[340px] sm:h-[390px]"
        :style="{
          transform: `translateZ(-${wheelRadius}px) rotateY(${currentAngle}deg)`,
          transformStyle: 'preserve-3d',
          WebkitTransformStyle: 'preserve-3d',
          transition: isDragging ? 'none' : 'transform 2.0s cubic-bezier(0.25, 1, 0.35, 1)'
        }"
      >
        <!-- 5 Faces Fixed on the Circumference, Facing Outwards (Radial Tangent) -->
        <div
          v-for="(pilot, idx) in pilots"
          :key="pilot.id"
          @click.stop="onCardClick(idx)"
          class="absolute inset-0 flex flex-col items-center justify-end pb-1 cursor-pointer pointer-events-auto group wheel-card-item"
          :style="{
            transform: `rotateY(${idx * 72}deg) translateZ(${wheelRadius}px)`,
            transformStyle: 'preserve-3d',
            WebkitTransformStyle: 'preserve-3d',
            zIndex: idx === activeIndex ? 50 : 20
          }"
        >
          <!-- 3D Card Body Wrapper (Double-Sided) -->
          <div
            class="relative w-[195px] sm:w-[230px] h-[280px] sm:h-[330px] transition-all duration-[1600ms] ease-in-out"
            :style="{
              transformStyle: 'preserve-3d',
              WebkitTransformStyle: 'preserve-3d',
              transform: idx === activeIndex ? 'scale(1.06)' : 'scale(0.92)'
            }"
          >
            <!-- ── FRONT FACE (Pilot Portrait & HUD Frame) ── -->
            <div
              class="absolute inset-0 rounded-2xl overflow-hidden bg-[#0a0e17] flex flex-col justify-between border-2 transition-all duration-[1600ms]"
              :class="[
                idx === activeIndex
                  ? 'opacity-100 shadow-[0_0_40px_rgba(0,0,0,0.95)]'
                  : 'opacity-65 hover:opacity-90'
              ]"
              :style="{
                borderColor: idx === activeIndex ? pilot.color : '#1e293b',
                boxShadow: idx === activeIndex ? `0 0 35px ${pilot.glow}80` : 'none',
                backfaceVisibility: 'hidden',
                WebkitBackfaceVisibility: 'hidden'
              }"
            >
              <!-- Pilot Photo Container -->
              <div class="relative flex-1 overflow-hidden bg-slate-950">
                <img
                  :src="pilot.avatar"
                  :alt="pilot.name"
                  class="w-full h-full object-cover object-top select-none pointer-events-none transition-all duration-[1600ms]"
                  :style="{
                    filter: idx === activeIndex ? 'none' : 'grayscale(100%) brightness(0.4) contrast(1.15)'
                  }"
                />

                <!-- Active Neon Scanning Laser -->
                <div
                  v-if="idx === activeIndex"
                  class="pointer-events-none absolute inset-x-0 h-1 bg-gradient-to-r from-transparent via-white to-transparent animate-laser-scan opacity-80"
                  :style="{ backgroundColor: pilot.color }"
                ></div>

                <!-- Active Vignette Overlay -->
                <div
                  v-if="idx === activeIndex"
                  class="pointer-events-none absolute inset-0 bg-gradient-to-t from-black/80 via-transparent to-transparent"
                ></div>

                <!-- Inactive Dark Shading -->
                <div
                  v-if="idx !== activeIndex"
                  class="pointer-events-none absolute inset-0 bg-black/45"
                ></div>
              </div>

              <!-- Pilot Nameplate Footer -->
              <div
                class="p-2 text-center font-mono bg-[#07090e] border-t transition-all duration-[1600ms]"
                :style="{ borderColor: idx === activeIndex ? pilot.color + '70' : '#1e293b' }"
              >
                <div class="text-[9.5px] font-black tracking-wider" :style="{ color: idx === activeIndex ? pilot.color : '#64748b' }">
                  {{ pilot.callsign.split('//')[0] }}
                </div>
                <div class="text-[11px] font-bold text-white tracking-wide truncate">{{ pilot.name }}</div>
              </div>
            </div>

            <!-- Active Aura Glow Flare -->
            <div
              v-if="idx === activeIndex"
              class="pointer-events-none absolute -inset-3 rounded-3xl blur-xl opacity-45 mix-blend-screen transition-all duration-[1600ms] -z-10"
              :style="{ backgroundColor: pilot.color }"
            ></div>

            <!-- ── BACK FACE (High-Tech Mecha Armor Plate) ── -->
            <div
              class="absolute inset-0 rounded-2xl overflow-hidden bg-[#06080f] border border-slate-800/90 flex flex-col items-center justify-center p-4 text-center select-none pointer-events-none"
              :style="{
                transform: 'rotateY(180deg)',
                backfaceVisibility: 'hidden',
                WebkitBackfaceVisibility: 'hidden',
                boxShadow: 'inset 0 0 30px rgba(0,0,0,0.95)'
              }"
            >
              <div class="w-12 h-12 rounded-full border border-amber-500/30 flex items-center justify-center mb-2 bg-amber-500/5">
                <span class="text-xl">🛡️</span>
              </div>
              <div class="font-mono text-[9px] font-black text-amber-400 tracking-widest uppercase">V-SHIELD TITAN</div>
              <div class="font-mono text-[8px] text-slate-500 mt-0.5 tracking-wider">DEFENSE CHASSIS // MK-V</div>
              <div class="mt-3 w-16 h-0.5 bg-gradient-to-r from-transparent via-amber-500/40 to-transparent"></div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ── 4. COCKPIT BOTTOM CONTROLS & DOTS ── -->
    <div class="relative pb-2.5 px-3.5 flex items-center justify-between gap-1.5 z-40 font-mono text-[9.5px] font-black bg-gradient-to-t from-[#04060a]/90 to-transparent pt-3">
      <div class="flex items-center gap-1">
        <button
          type="button"
          @click.stop="triggerOverdrive"
          class="px-2 py-1 bg-red-950/90 hover:bg-red-600 text-red-300 hover:text-white border border-red-500/60 transition-all mecha-cut-tr active:scale-95 pointer-events-auto"
        >
          <span>⚡ QUÁ TẢI</span>
        </button>

        <button
          type="button"
          @click.stop="triggerShield"
          class="px-2 py-1 transition-all mecha-cut-tr active:scale-95 pointer-events-auto"
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
          @click.stop="triggerLockOn"
          class="px-2 py-1 transition-all mecha-cut-tr active:scale-95 pointer-events-auto"
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
      <div class="flex items-center gap-1.5 bg-[#07090e]/90 px-2 py-1 border border-slate-800 mecha-cut-tr pointer-events-auto">
        <button
          v-for="(pilot, idx) in pilots"
          :key="pilot.id"
          @click.stop="emit('selectPilot', idx)"
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

</style>
