<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import * as THREE from 'three'
import { mechaAudio } from '../../utils/portalAudio'

const props = defineProps({
  activeIndex: {
    type: Number,
    default: 0
  }
})

const emit = defineEmits(['update:activeIndex', 'championClick'])

const containerRef = ref(null)
const canvasContainerRef = ref(null)
const activeAngleIndex = ref(0)
const isShieldActive = ref(false)
const isOverdriveActive = ref(false)
const isSketchfabViewer = ref(false)
const isLockOnActive = ref(false)
const currentActionText = ref('SẴN SÀNG TÁC CHIẾN (COMBAT READY)')

// 3D Parallax & Gyro Tracking
const mouseX = ref(0)
const mouseY = ref(0)
const rotX = ref(0)
const rotY = ref(0)
const isHovering = ref(false)

// Three.js Background Spark Stage
let scene, camera, renderer, animationFrameId, particles

// High-Res Cinematic Render Angles of Oscar Creativo BOT MECHA Warrior
const mechaAngles = [
  {
    id: 'front',
    label: 'GÓC TIÊN PHONG CHÍNH DIỆN',
    src: '/mecha/mecha_render1.jpg',
    scale: 'scale-100'
  },
  {
    id: 'combat',
    label: 'GÓC TOÀN CẢNH VŨ TRANG',
    src: '/mecha/mecha_render2.jpg',
    scale: 'scale-105'
  },
  {
    id: 'heavy',
    label: 'TƯ THẾ XUẤT KÍCH HEROIC',
    src: '/mecha/mecha_thumb.jpg',
    scale: 'scale-100'
  }
]

// 5 Champions Color Grading & Tactical Specs
const championProfiles = [
  {
    id: 0,
    name: 'Phạm Văn Thành',
    codename: 'V-SHIELD PRIME',
    primaryColor: '#ffcc00',
    glowColor: '#ffaa00',
    filterStyle: 'hue-rotate(0deg) saturate(1.35) contrast(1.1)',
    weapon: 'LƯỠI ĐẠI KIẾM LƯỢNG TỬ // QUANTUM BLADE',
    role: 'CHỈ HUY ĐIỀU HÀNH & KIẾN TRÚC SƯ TRƯỞNG'
  },
  {
    id: 1,
    name: 'Hà Mạnh Hùng',
    codename: 'PHANTOM FALCON',
    primaryColor: '#ff5500',
    glowColor: '#ff2200',
    filterStyle: 'hue-rotate(330deg) saturate(1.8) contrast(1.15)',
    weapon: 'SÚNG TRƯỜNG PLASMA RAILGUN // HYPER-VELOCITY',
    role: 'TRINH SÁT AN NINH & GIÁM SÁT THỰC ĐỊA'
  },
  {
    id: 2,
    name: 'Phạm Ngọc Hoài Anh',
    codename: 'DREADNOUGHT VORTEX',
    primaryColor: '#00f0ff',
    glowColor: '#00c8ff',
    filterStyle: 'hue-rotate(185deg) saturate(1.6) contrast(1.15)',
    weapon: 'ĐẠI BÁC HẠT NẶNG TITAN // HEAVY CANNON',
    role: 'HẠ TẦNG MẠNG & BẢO MẬT HỆ THỐNG'
  },
  {
    id: 3,
    name: 'Vũ Tiến Đạt',
    codename: 'SPECTRE STRIKER',
    primaryColor: '#c084fc',
    glowColor: '#a855f7',
    filterStyle: 'hue-rotate(260deg) saturate(1.6) contrast(1.15)',
    weapon: 'CẶP DAO GĂM SÓNG ÂM // SONIC DAGGERS',
    role: 'THỊ GIÁC MÁY TÍNH & NHẬN DIỆN KHUÔN MẶT'
  },
  {
    id: 4,
    name: 'Nguyễn Quốc Việt',
    codename: 'TEMPEST JUGGERNAUT',
    primaryColor: '#10b981',
    glowColor: '#059669',
    filterStyle: 'hue-rotate(95deg) saturate(1.6) contrast(1.15)',
    weapon: 'KÍCH SẤM SÉT // THUNDERSTRIKE HALBERD',
    role: 'XỬ LÝ DỮ LIỆU & PHÂN TÍCH HÀNH VI UEBA'
  }
]

const currentChampion = computed(() => championProfiles[props.activeIndex] || championProfiles[0])

// ── 3D PARALLAX MOUSE HANDLER ──
function handleMouseMove(e) {
  if (!containerRef.value) return
  const rect = containerRef.value.getBoundingClientRect()
  const x = (e.clientX - (rect.left + rect.width / 2)) / (rect.width / 2)
  const y = (e.clientY - (rect.top + rect.height / 2)) / (rect.height / 2)
  
  mouseX.value = x
  mouseY.value = y
  rotY.value = x * 15
  rotX.value = -y * 12
  isHovering.value = true
}

function handleMouseLeave() {
  isHovering.value = false
  rotX.value = 0
  rotY.value = 0
  mouseX.value = 0
  mouseY.value = 0
}

// ── COMBAT OVERDRIVE ACTIONS ──
function triggerStrike() {
  isOverdriveActive.value = true
  currentActionText.value = '⚔️ KÍCH HOẠT XUNG KÍCH LƯỢNG TỬ (OVERDRIVE STRIKE)'
  mechaAudio.playTargetLock()
  mechaAudio.playHeavyImpactDrop()
  setTimeout(() => {
    isOverdriveActive.value = false
    currentActionText.value = 'SẴN SÀNG TÁC CHIẾN (COMBAT READY)'
  }, 1400)
}

function triggerShield() {
  isShieldActive.value = !isShieldActive.value
  if (isShieldActive.value) {
    currentActionText.value = '🛡️ TRƯỜNG LỰC BẢO VỆ ĐANG HOẠT ĐỘNG (ENERGY SHIELD ACTIVE)'
    mechaAudio.playEngage()
  } else {
    currentActionText.value = 'SẴN SÀNG TÁC CHIẾN (COMBAT READY)'
    mechaAudio.playClick()
  }
}

function triggerBoost() {
  currentActionText.value = '🚀 ĐẨY PHẢN LỰC SIÊU THANH (THRUSTER BOOST)'
  mechaAudio.playEngage()
  rotX.value = -18
  setTimeout(() => {
    rotX.value = 0
    currentActionText.value = 'SẴN SÀNG TÁC CHIẾN (COMBAT READY)'
  }, 1200)
}

function triggerLockOn() {
  isLockOnActive.value = !isLockOnActive.value
  if (isLockOnActive.value) {
    currentActionText.value = '🎯 KHÓA MỤC TIÊU CHIẾN THUẬT (TARGET LOCK-ON)'
    mechaAudio.playTargetLock()
  } else {
    currentActionText.value = 'SẴN SÀNG TÁC CHIẾN (COMBAT READY)'
    mechaAudio.playHover()
  }
}

function cycleAngle() {
  activeAngleIndex.value = (activeAngleIndex.value + 1) % mechaAngles.length
  currentActionText.value = '🔄 ĐỔI GÓC NHÌN: ' + mechaAngles[activeAngleIndex.value].label
  mechaAudio.playClick()
}

// ── LIGHTWEIGHT 3D SPARK NEBULA (THREE.JS) ──
function initSparkStage() {
  const container = canvasContainerRef.value
  if (!container) return

  const width = container.clientWidth || 500
  const height = container.clientHeight || 420

  scene = new THREE.Scene()
  camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 100)
  camera.position.set(0, 0, 8)

  try {
    renderer = new THREE.WebGLRenderer({ alpha: true, antialias: true })
    renderer.setSize(width, height)
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.5))
    container.appendChild(renderer.domElement)
  } catch (_) {
    return
  }

  // 120 Floating Cyber Particles
  const pCount = 120
  const pPositions = new Float32Array(pCount * 3)
  for (let i = 0; i < pCount * 3; i += 3) {
    pPositions[i] = (Math.random() - 0.5) * 10
    pPositions[i + 1] = (Math.random() - 0.5) * 8
    pPositions[i + 2] = (Math.random() - 0.5) * 6
  }
  const pGeo = new THREE.BufferGeometry()
  pGeo.setAttribute('position', new THREE.BufferAttribute(pPositions, 3))
  const pMat = new THREE.PointsMaterial({
    color: new THREE.Color(currentChampion.value.primaryColor),
    size: 0.05,
    transparent: true,
    opacity: 0.85
  })
  particles = new THREE.Points(pGeo, pMat)
  scene.add(particles)

  animateSparks()
}

function animateSparks() {
  animationFrameId = requestAnimationFrame(animateSparks)
  const time = Date.now() * 0.001
  if (particles) {
    particles.rotation.y = time * 0.06 + mouseX.value * 0.2
    particles.rotation.x = time * 0.03 - mouseY.value * 0.15
  }
  if (renderer && scene && camera) {
    renderer.render(scene, camera)
  }
}

function updateSparkColor() {
  if (particles && particles.material) {
    particles.material.color.set(currentChampion.value.primaryColor)
  }
}

onMounted(() => {
  initSparkStage()
})

onUnmounted(() => {
  if (animationFrameId) cancelAnimationFrame(animationFrameId)
  if (renderer && renderer.domElement && renderer.domElement.parentNode) {
    renderer.domElement.parentNode.removeChild(renderer.domElement)
  }
})

watch(() => props.activeIndex, () => {
  updateSparkColor()
})
</script>

<template>
  <div class="relative flex flex-col items-center justify-center select-none w-full">
    
    <!-- ── 3D MECHA BREAK HOLOGRAPHIC STAGE CONTAINER ── -->
    <div
      ref="containerRef"
      @mousemove="handleMouseMove"
      @mouseleave="handleMouseLeave"
      class="relative h-[420px] sm:h-[460px] w-full max-w-[540px] flex items-center justify-center cursor-crosshair overflow-hidden rounded-xl border-2 border-amber-500/40 bg-gradient-to-b from-[#0b0e17]/95 via-[#07090e] to-[#04060a] mecha-cut-corners shadow-[0_0_60px_rgba(255,204,0,0.25)]"
      style="perspective: 1200px;"
    >
      <!-- Background Three.js Particle Sparks Canvas -->
      <div ref="canvasContainerRef" class="pointer-events-none absolute inset-0 z-0"></div>

      <!-- Holographic Grid Floor & Pedestal Lighting -->
      <div
        class="pointer-events-none absolute bottom-0 inset-x-0 h-44 bg-[radial-gradient(ellipse_at_bottom,#ffcc0025,transparent_70%)] opacity-80 z-0"
      ></div>

      <!-- Holographic Outer Laser Ring -->
      <div
        class="pointer-events-none absolute bottom-6 h-28 w-80 rounded-full border-2 border-dashed transition-all duration-700 animate-spin-slow opacity-60 z-0"
        :style="{ borderColor: currentChampion.primaryColor, boxShadow: '0 0 30px ' + currentChampion.glowColor + '40' }"
      ></div>

      <!-- ── PSEUDO-3D PARALLAX MECHA WARFRAME LAYER ── -->
      <div
        v-if="!isSketchfabViewer"
        class="relative flex items-center justify-center w-full h-full z-10 transition-transform duration-150 ease-out"
        :style="{
          transform: 'rotateY(' + rotY + 'deg) rotateX(' + rotX + 'deg) translateZ(' + (isOverdriveActive ? 60 : 30) + 'px) scale(' + (isOverdriveActive ? 1.08 : 1) + ')',
          filter: isOverdriveActive ? 'brightness(1.3) drop-shadow(0 0 35px #ff5500)' : 'none'
        }"
      >
        <!-- High-Res Authentic Oscar Creativo Mecha Image -->
        <img
          :src="mechaAngles[activeAngleIndex].src"
          :alt="mechaAngles[activeAngleIndex].label"
          class="h-[360px] sm:h-[400px] w-auto object-contain transition-all duration-500 select-none pointer-events-none drop-shadow-[0_20px_35px_rgba(0,0,0,0.9)]"
          :class="mechaAngles[activeAngleIndex].scale"
          :style="{ filter: currentChampion.filterStyle }"
        />

        <!-- Dynamic Specular Cursor Glare Layer -->
        <div
          class="pointer-events-none absolute inset-0 transition-opacity duration-300 mix-blend-screen"
          :style="{
            background: isHovering
              ? 'radial-gradient(circle 200px at ' + ((mouseX + 1) * 50) + '% ' + ((mouseY + 1) * 50) + '%, ' + currentChampion.primaryColor + '55, transparent 80%)'
              : 'none'
          }"
        ></div>

        <!-- Glowing Reactor Core Surge Effect -->
        <div
          class="pointer-events-none absolute h-12 w-12 rounded-full blur-md animate-pulse mix-blend-screen transition-all duration-500"
          :style="{
            backgroundColor: currentChampion.primaryColor,
            top: '44%',
            left: '48%',
            transform: 'translate(-50%, -50%)',
            boxShadow: '0 0 40px 15px ' + currentChampion.primaryColor
          }"
        ></div>

        <!-- Hexagonal Energy Shield Forcefield Layer -->
        <div
          v-if="isShieldActive"
          class="pointer-events-none absolute inset-6 rounded-2xl border-2 border-cyan-400/80 bg-cyan-500/10 backdrop-blur-[1px] animate-pulse flex items-center justify-center shadow-[0_0_50px_rgba(0,240,255,0.5)] z-20"
        >
          <div class="font-mono text-xs font-black text-cyan-300 tracking-widest bg-slate-950/80 px-3 py-1 border border-cyan-400 mecha-cut-tr">
            ⚡ QUANTUM FORCEFIELD // ACTIVE
          </div>
        </div>

        <!-- Tactical Lock-on Reticles -->
        <div
          v-if="isLockOnActive"
          class="pointer-events-none absolute inset-0 flex items-center justify-center z-20"
        >
          <div class="h-44 w-44 rounded-full border-2 border-red-500/80 animate-ping opacity-60"></div>
          <div class="absolute h-56 w-56 rounded-full border border-dashed border-red-400/80 animate-spin-slow"></div>
          <div class="absolute font-mono text-[9px] font-black text-red-400 bg-red-950/80 px-2 py-0.5 border border-red-500 -top-2">
            [TARGET ACQUIRED: 100%]
          </div>
        </div>
      </div>

      <!-- ── EMBEDDED SKETCHFAB 3D ENGINE (OPTIONAL ON-DEMAND) ── -->
      <div
        v-else
        class="relative w-full h-full z-10 p-2"
      >
        <iframe
          title="BOT MECHA Warrior 3d by Oscar Creativo"
          class="w-full h-full border-0 rounded-lg"
          src="https://sketchfab.com/models/34850bfe441642788154c4a8a0bd60e4/embed?autostart=1&preload=1&ui_theme=dark&ui_infos=0&ui_watermark=0&ui_stop=0&ui_hint=2&dnt=1"
          allow="autoplay; fullscreen; xr-spatial-tracking"
          xr-spatial-tracking="true"
          allowfullscreen
        ></iframe>
      </div>

      <!-- ── TACTICAL HUD OVERLAYS (MECHA BREAK AESTHETICS) ── -->
      <!-- Top Left Unit Code -->
      <div class="pointer-events-none absolute top-3 left-3 flex items-center gap-2 bg-[#07090e]/90 px-2.5 py-1 border border-amber-500/40 text-[9.5px] font-mono font-black text-amber-400 z-30 mecha-cut-tr">
        <span class="h-2 w-2 rounded-full animate-ping" :style="{ backgroundColor: currentChampion.primaryColor }"></span>
        <span>MECHA: {{ currentChampion.codename }}</span>
      </div>

      <!-- Top Right Weapon Badge -->
      <div class="pointer-events-none absolute top-3 right-3 bg-[#07090e]/90 px-2.5 py-1 border border-cyan-500/40 text-[9px] font-mono font-bold text-cyan-400 z-30 mecha-cut-tr">
        <span>VŨ KHÍ: {{ currentChampion.weapon.split('//')[0] }}</span>
      </div>

      <!-- Bottom Status Readout -->
      <div class="pointer-events-none absolute bottom-3 left-3 right-3 flex items-center justify-between bg-[#07090e]/90 px-3 py-1.5 border border-slate-800 text-[9px] font-mono z-30 mecha-cut-corners">
        <div class="flex items-center gap-1.5 text-slate-300">
          <span class="text-amber-400 font-black">TRẠNG THÁI:</span>
          <span class="text-white font-bold">{{ currentActionText }}</span>
        </div>
        <div class="text-emerald-400 font-bold hidden sm:block">
          GÓC 3D GYRO: X:{{ Math.round(rotX) }}° Y:{{ Math.round(rotY) }}°
        </div>
      </div>
    </div>

    <!-- ── INTERACTIVE COMBAT OVERDRIVE CONTROL BAR ── -->
    <div class="mt-3 flex flex-wrap items-center justify-center gap-1.5 z-20 font-mono text-[10px] font-black max-w-xl">
      <button
        type="button"
        @click="triggerStrike"
        class="px-3 py-1.5 bg-red-950/80 hover:bg-red-600 text-red-300 hover:text-white border border-red-500/60 transition-all mecha-cut-tr shadow-[0_0_15px_rgba(239,68,68,0.4)] flex items-center gap-1.5 active:scale-95"
      >
        <span>⚔️ XUNG KÍCH (STRIKE)</span>
      </button>

      <button
        type="button"
        @click="triggerShield"
        class="px-3 py-1.5 transition-all mecha-cut-tr flex items-center gap-1.5 active:scale-95"
        :class="[
          isShieldActive
            ? 'bg-cyan-500 text-slate-950 shadow-[0_0_20px_#00f0ff] border border-cyan-300'
            : 'bg-cyan-950/80 hover:bg-cyan-600 text-cyan-300 hover:text-white border border-cyan-500/60 shadow-[0_0_12px_rgba(6,182,212,0.3)]'
        ]"
      >
        <span>🛡️ KHIÊN NĂNG LƯỢNG</span>
      </button>

      <button
        type="button"
        @click="triggerBoost"
        class="px-3 py-1.5 bg-amber-950/80 hover:bg-amber-500 text-amber-300 hover:text-slate-950 border border-amber-500/60 transition-all mecha-cut-tr shadow-[0_0_12px_rgba(245,158,11,0.3)] flex items-center gap-1.5 active:scale-95"
      >
        <span>🚀 ĐẨY PHẢN LỰC</span>
      </button>

      <button
        type="button"
        @click="triggerLockOn"
        class="px-3 py-1.5 transition-all mecha-cut-tr flex items-center gap-1.5 active:scale-95"
        :class="[
          isLockOnActive
            ? 'bg-red-600 text-white shadow-[0_0_20px_#ff0055] border border-red-400'
            : 'bg-slate-900 hover:bg-red-950 text-slate-300 hover:text-red-300 border border-slate-700'
        ]"
      >
        <span>🎯 KHÓA MỤC TIÊU</span>
      </button>

      <button
        type="button"
        @click="cycleAngle"
        class="px-3 py-1.5 bg-purple-950/80 hover:bg-purple-600 text-purple-300 hover:text-white border border-purple-500/60 transition-all mecha-cut-tr shadow-[0_0_12px_rgba(168,85,247,0.3)] flex items-center gap-1.5 active:scale-95"
      >
        <span>🔄 ĐỔI GÓC NHÌN ({{ activeAngleIndex + 1 }}/3)</span>
      </button>

      <button
        type="button"
        @click="isSketchfabViewer = !isSketchfabViewer"
        class="px-3 py-1.5 bg-slate-900 hover:bg-slate-700 text-slate-300 border border-slate-700 transition-all mecha-cut-tr flex items-center gap-1.5 active:scale-95"
      >
        <span v-if="!isSketchfabViewer">🌐 MỞ 3D SKETCHFAB</span>
        <span v-else class="text-amber-400 font-bold">⚡ QUAY LẠI 3D PARALLAX</span>
      </button>
    </div>

    <!-- Attribution Footer -->
    <div class="mt-2 text-center font-mono text-[9px] text-slate-500">
      MÔ HÌNH NGUYÊN BẢN: <span class="text-slate-400 font-bold">BOT MECHA WARRIOR 3D BY OSCAR CREATIVO</span> • MECHA BREAK AESTHETIC ENGINE
    </div>
  </div>
</template>

<style scoped>
@keyframes spinSlow {
  from {
    transform: rotate(0deg);
  }
  to {
    transform: rotate(360deg);
  }
}
.animate-spin-slow {
  animation: spinSlow 24s linear infinite;
}
</style>
