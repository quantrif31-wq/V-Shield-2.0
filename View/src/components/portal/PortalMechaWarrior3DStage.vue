<script setup>
import { ref, onMounted, onUnmounted, watch } from 'vue'
import * as THREE from 'three'
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js'
import { mechaAudio } from '../../utils/portalAudio'

const props = defineProps({
  activeIndex: {
    type: Number,
    default: 0
  }
})

const emit = defineEmits(['update:activeIndex', 'championClick'])

const containerRef = ref(null)
const isLoading = ref(true)
const currentActionName = ref('Idle')

let scene, camera, renderer, animationFrameId, clock
let fixedStageGroup, mechaRootGroup, spotLight, ambientLight, dirLight
let mixer = null
const actions = {}
let activeAction = null
let currentMechaMesh = null
let customWeapons = []

// Mouse orbit and drag interaction
let isDragging = false
let prevMouseX = 0, prevMouseY = 0
let mouseX = 0, mouseY = 0
let manualRotY = 0, manualRotX = 0

// 5 Champions Color Profiles & Weapon Specs
const championProfiles = [
  {
    id: 0,
    name: 'Phạm Văn Thành',
    codename: 'V-SHIELD PRIME',
    primary: 0xffcc00,
    accent: 0xff5500,
    glow: 0xffcc00,
    metalness: 0.88,
    roughness: 0.18,
    defaultAnim: 'Standing',
    weaponType: 'broadsword'
  },
  {
    id: 1,
    name: 'Hà Mạnh Hùng',
    codename: 'PHANTOM FALCON',
    primary: 0xff5500,
    accent: 0xff0055,
    glow: 0xff3300,
    metalness: 0.92,
    roughness: 0.15,
    defaultAnim: 'Punch',
    weaponType: 'railgun'
  },
  {
    id: 2,
    name: 'Phạm Ngọc Hoài Anh',
    codename: 'DREADNOUGHT VORTEX',
    primary: 0x00f0ff,
    accent: 0x0088ff,
    glow: 0x00ffff,
    metalness: 0.95,
    roughness: 0.12,
    defaultAnim: 'Jump',
    weaponType: 'cannon'
  },
  {
    id: 3,
    name: 'Vũ Tiến Đạt',
    codename: 'SPECTRE STRIKER',
    primary: 0xa855f7,
    accent: 0xff00aa,
    glow: 0xcc44ff,
    metalness: 0.9,
    roughness: 0.2,
    defaultAnim: 'Running',
    weaponType: 'daggers'
  },
  {
    id: 4,
    name: 'Nguyễn Quốc Việt',
    codename: 'TEMPEST JUGGERNAUT',
    primary: 0x10b981,
    accent: 0x06b6d4,
    glow: 0x00ff88,
    metalness: 0.85,
    roughness: 0.25,
    defaultAnim: 'Walking',
    weaponType: 'halberd'
  }
]

// ── BUILD 3D HIGH-TECH WEAPONS ──
function buildChampionWeapon(weaponType, colorScheme) {
  const weaponGroup = new THREE.Group()
  const glowMat = new THREE.MeshBasicMaterial({ color: colorScheme.glow })
  const metalMat = new THREE.MeshStandardMaterial({
    color: 0x181e2b,
    metalness: 0.95,
    roughness: 0.18
  })

  if (weaponType === 'broadsword') {
    // Quantum Energy Broadsword
    const bladeGeo = new THREE.BoxGeometry(0.18, 2.2, 0.04)
    const blade = new THREE.Mesh(bladeGeo, glowMat)
    blade.position.set(0.65, 1.4, 0.3)
    blade.rotation.z = Math.PI / 12
    weaponGroup.add(blade)

    const hiltGeo = new THREE.CylinderGeometry(0.05, 0.05, 0.6, 8)
    const hilt = new THREE.Mesh(hiltGeo, metalMat)
    hilt.position.set(0.5, 0.4, 0.25)
    hilt.rotation.z = Math.PI / 12
    weaponGroup.add(hilt)
  } else if (weaponType === 'railgun') {
    // Hyper-Velocity Plasma Railgun
    const rail1Geo = new THREE.BoxGeometry(0.06, 0.08, 1.8)
    const rail1 = new THREE.Mesh(rail1Geo, metalMat)
    rail1.position.set(0.6, 0.9, 0.8)
    weaponGroup.add(rail1)

    const rail2 = new THREE.Mesh(rail1Geo, metalMat)
    rail2.position.set(0.6, 0.76, 0.8)
    weaponGroup.add(rail2)

    const coreBeamGeo = new THREE.CylinderGeometry(0.03, 0.03, 1.6, 8)
    const coreBeam = new THREE.Mesh(coreBeamGeo, glowMat)
    coreBeam.position.set(0.6, 0.83, 0.8)
    coreBeam.rotation.x = Math.PI / 2
    weaponGroup.add(coreBeam)
  } else if (weaponType === 'cannon') {
    // Titan Heavy Shoulder Cannon
    const barrelGeo = new THREE.CylinderGeometry(0.18, 0.22, 1.6, 16)
    const barrel = new THREE.Mesh(barrelGeo, metalMat)
    barrel.position.set(-0.55, 1.65, 0.4)
    barrel.rotation.x = Math.PI / 2
    weaponGroup.add(barrel)

    const ringGeo = new THREE.TorusGeometry(0.2, 0.04, 8, 16)
    const ring = new THREE.Mesh(ringGeo, glowMat)
    ring.position.set(-0.55, 1.65, 1.2)
    weaponGroup.add(ring)
  } else if (weaponType === 'daggers') {
    // Dual Holographic Energy Daggers
    const dGeo = new THREE.ConeGeometry(0.09, 1.1, 4)
    const d1 = new THREE.Mesh(dGeo, glowMat)
    d1.position.set(-0.6, 0.7, 0.5)
    d1.rotation.x = Math.PI / 2.5
    weaponGroup.add(d1)

    const d2 = new THREE.Mesh(dGeo, glowMat)
    d2.position.set(0.6, 0.7, 0.5)
    d2.rotation.x = -Math.PI / 2.5
    weaponGroup.add(d2)
  } else if (weaponType === 'halberd') {
    // Thunderstrike Power Halberd
    const shaftGeo = new THREE.CylinderGeometry(0.04, 0.04, 2.4, 8)
    const shaft = new THREE.Mesh(shaftGeo, metalMat)
    shaft.position.set(0.65, 1.2, 0.2)
    weaponGroup.add(shaft)

    const tipGeo = new THREE.ConeGeometry(0.18, 0.7, 4)
    const tip = new THREE.Mesh(tipGeo, glowMat)
    tip.position.set(0.65, 2.45, 0.2)
    weaponGroup.add(tip)
  }

  return weaponGroup
}

function initStage() {
  const container = containerRef.value
  if (!container) return

  const width = container.clientWidth || 480
  const height = container.clientHeight || 420

  clock = new THREE.Clock()

  // 1. Scene & Camera
  scene = new THREE.Scene()
  camera = new THREE.PerspectiveCamera(42, width / height, 0.1, 1000)
  camera.position.set(0, 2.4, 6.2)
  camera.lookAt(0, 1.1, 0)

  // 2. Cinematic Tactical Lighting
  ambientLight = new THREE.AmbientLight(0xffffff, 1.2)
  scene.add(ambientLight)

  dirLight = new THREE.DirectionalLight(0xffffff, 2.8)
  dirLight.position.set(5, 10, 7)
  scene.add(dirLight)

  const rimLight = new THREE.DirectionalLight(0x00f0ff, 2.0)
  rimLight.position.set(-5, 4, -5)
  scene.add(rimLight)

  spotLight = new THREE.PointLight(0xffcc00, 3.5, 15)
  spotLight.position.set(0, 3.2, 3.5)
  scene.add(spotLight)

  // 3. Renderer with safe WebGL handling
  try {
    renderer = new THREE.WebGLRenderer({ alpha: true, antialias: true, powerPreference: 'high-performance' })
    renderer.setSize(width, height)
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2))
    renderer.shadowMap.enabled = true
    renderer.shadowMap.type = THREE.PCFSoftShadowMap
    renderer.toneMapping = THREE.ACESFilmicToneMapping
    renderer.toneMappingExposure = 1.15
    container.appendChild(renderer.domElement)
  } catch (_) {
    isLoading.value = false
    return
  }

  // ── 4. MECHA BREAK HOLOGRAPHIC LAUNCH PLATFORM (FIXED BASE) ──
  fixedStageGroup = new THREE.Group()
  scene.add(fixedStageGroup)

  // Heavy Octagonal Cyber Platform
  const baseGeo = new THREE.CylinderGeometry(2.6, 2.8, 0.35, 8)
  const baseMat = new THREE.MeshStandardMaterial({
    color: 0x07090e,
    metalness: 0.96,
    roughness: 0.15
  })
  const baseMesh = new THREE.Mesh(baseGeo, baseMat)
  baseMesh.position.y = -0.18
  fixedStageGroup.add(baseMesh)

  // Glowing Outer Energy Barrier Ring
  const outerRingGeo = new THREE.TorusGeometry(2.75, 0.04, 16, 64)
  const ringMat = new THREE.MeshBasicMaterial({ color: 0xffcc00 })
  const outerRing = new THREE.Mesh(outerRingGeo, ringMat)
  outerRing.rotation.x = Math.PI / 2
  fixedStageGroup.add(outerRing)

  // Inner Projector Disc (Pulsing Glow)
  const discGeo = new THREE.CylinderGeometry(1.6, 1.6, 0.02, 32)
  const discMat = new THREE.MeshBasicMaterial({
    color: 0xffcc00,
    transparent: true,
    opacity: 0.35,
    wireframe: true
  })
  const discMesh = new THREE.Mesh(discGeo, discMat)
  discMesh.position.y = 0.01
  fixedStageGroup.add(discMesh)

  // 4 Corner Energy Emitters / Pylons
  for (let i = 0; i < 4; i++) {
    const angle = (i / 4) * Math.PI * 2 + Math.PI / 4
    const pylonGeo = new THREE.BoxGeometry(0.16, 0.45, 0.16)
    const pylonMat = new THREE.MeshStandardMaterial({ color: 0x181e2b, metalness: 0.9, roughness: 0.2 })
    const pylon = new THREE.Mesh(pylonGeo, pylonMat)
    pylon.position.set(Math.cos(angle) * 2.5, 0.1, Math.sin(angle) * 2.5)
    fixedStageGroup.add(pylon)

    const tipGeo = new THREE.SphereGeometry(0.06, 8, 8)
    const tip = new THREE.Mesh(tipGeo, ringMat)
    tip.position.set(Math.cos(angle) * 2.5, 0.36, Math.sin(angle) * 2.5)
    fixedStageGroup.add(tip)
  }

  // Floating Cyber Sparks & Nebula Dust
  const pCount = 180
  const pPositions = new Float32Array(pCount * 3)
  for (let i = 0; i < pCount * 3; i += 3) {
    pPositions[i] = (Math.random() - 0.5) * 4.8
    pPositions[i + 1] = Math.random() * 3.5
    pPositions[i + 2] = (Math.random() - 0.5) * 4.8
  }
  const pGeo = new THREE.BufferGeometry()
  pGeo.setAttribute('position', new THREE.BufferAttribute(pPositions, 3))
  const pMat = new THREE.PointsMaterial({
    color: 0xffcc00,
    size: 0.045,
    transparent: true,
    opacity: 0.8
  })
  const particles = new THREE.Points(pGeo, pMat)
  fixedStageGroup.add(particles)

  // ── 5. MECHA ROOT CONTAINER & GLTF MODEL LOADER ──
  mechaRootGroup = new THREE.Group()
  scene.add(mechaRootGroup)

  loadMechaModel()

  // 6. Interaction Listeners
  window.addEventListener('mousemove', onMouseMove, { passive: true })
  window.addEventListener('resize', onResize)

  container.addEventListener('mousedown', (e) => {
    isDragging = true
    prevMouseX = e.clientX
    prevMouseY = e.clientY
  })

  window.addEventListener('mouseup', () => {
    isDragging = false
  })

  container.addEventListener('click', () => {
    triggerRandomAction()
  })

  animate()
}

function loadMechaModel() {
  const loader = new GLTFLoader()

  loader.load(
    '/models/robot_expressive.glb',
    (gltf) => {
      const model = gltf.scene
      currentMechaMesh = model
      model.scale.set(0.48, 0.48, 0.48)
      model.position.set(0, 0, 0)

      // Initialize Animation Mixer
      mixer = new THREE.AnimationMixer(model)
      const animClips = gltf.animations || []

      animClips.forEach((clip) => {
        const action = mixer.clipAction(clip)
        actions[clip.name] = action
      })

      // Default to Idle Animation
      if (actions['Idle']) {
        activeAction = actions['Idle']
        activeAction.play()
        currentActionName.value = 'Idle'
      } else if (animClips.length > 0) {
        activeAction = mixer.clipAction(animClips[0])
        activeAction.play()
        currentActionName.value = animClips[0].name
      }

      mechaRootGroup.add(model)
      applyChampionSkin(props.activeIndex)
      isLoading.value = false
    },
    undefined,
    (err) => {
      console.warn('Could not load robot_expressive.glb, applying procedural mecha fallback:', err)
      // High-grade procedural fallback
      const fallbackGeo = new THREE.BoxGeometry(0.8, 1.8, 0.6)
      const fallbackMat = new THREE.MeshStandardMaterial({ color: 0xffcc00, metalness: 0.9, roughness: 0.2 })
      const fallbackMesh = new THREE.Mesh(fallbackGeo, fallbackMat)
      fallbackMesh.position.y = 0.9
      mechaRootGroup.add(fallbackMesh)
      currentMechaMesh = fallbackMesh
      applyChampionSkin(props.activeIndex)
      isLoading.value = false
    }
  )
}

function playAnimation(name, duration = 0.4) {
  if (!mixer || !actions[name]) return

  const prevAction = activeAction
  activeAction = actions[name]
  currentActionName.value = name

  if (prevAction !== activeAction) {
    if (prevAction) prevAction.fadeOut(duration)
    activeAction
      .reset()
      .setEffectiveTimeScale(1)
      .setEffectiveWeight(1)
      .fadeIn(duration)
      .play()
  }
}

function applyChampionSkin(index) {
  const profile = championProfiles[index] || championProfiles[0]

  // Update Spotlight & Light Color
  if (spotLight) {
    spotLight.color.setHex(profile.glow)
  }

  // Reskin GLB Materials for Cyber Mecha Aesthetics
  if (currentMechaMesh) {
    currentMechaMesh.traverse((child) => {
      if (child.isMesh && child.material) {
        const mat = child.material
        if (child.name.toLowerCase().includes('head') || child.name.toLowerCase().includes('body') || child.name.toLowerCase().includes('main')) {
          mat.color.setHex(profile.primary)
          mat.metalness = profile.metalness
          mat.roughness = profile.roughness
        } else if (child.name.toLowerCase().includes('eye') || child.name.toLowerCase().includes('visor') || child.name.toLowerCase().includes('glow')) {
          mat.color.setHex(profile.glow)
          if (mat.emissive) mat.emissive.setHex(profile.glow)
        } else {
          mat.metalness = 0.9
          mat.roughness = 0.22
        }
      }
    })
  }

  // Re-attach Dedicated Champion Weapon
  customWeapons.forEach(w => mechaRootGroup.remove(w))
  customWeapons = []

  const newWeapon = buildChampionWeapon(profile.weaponType, profile)
  mechaRootGroup.add(newWeapon)
  customWeapons.push(newWeapon)

  // Play Champion's Signature Animation
  if (profile.defaultAnim && actions[profile.defaultAnim]) {
    playAnimation(profile.defaultAnim, 0.5)
  } else if (actions['Idle']) {
    playAnimation('Idle', 0.5)
  }
}

function triggerRandomAction() {
  const actionList = ['Punch', 'Jump', 'Wave', 'Running', 'Dance', 'Idle']
  const randomAnim = actionList[Math.floor(Math.random() * actionList.length)]
  if (actions[randomAnim]) {
    playAnimation(randomAnim, 0.3)
    mechaAudio.playTargetLock()
    mechaAudio.playHeavyImpactDrop()
  }
}

function onMouseMove(e) {
  if (isDragging) {
    const deltaX = e.clientX - prevMouseX
    const deltaY = e.clientY - prevMouseY
    manualRotY += deltaX * 0.012
    manualRotX += deltaY * 0.008
    prevMouseX = e.clientX
    prevMouseY = e.clientY
  }

  if (!containerRef.value) return
  const rect = containerRef.value.getBoundingClientRect()
  mouseX = (e.clientX - (rect.left + rect.width / 2)) / (rect.width / 2)
  mouseY = (e.clientY - (rect.top + rect.height / 2)) / (rect.height / 2)
}

function onResize() {
  if (!containerRef.value || !renderer || !camera) return
  const width = containerRef.value.clientWidth
  const height = containerRef.value.clientHeight
  camera.aspect = width / height
  camera.updateProjectionMatrix()
  renderer.setSize(width, height)
}

function animate() {
  animationFrameId = requestAnimationFrame(animate)

  const delta = clock ? clock.getDelta() : 0.016
  const time = Date.now() * 0.002

  // Update Skeletal Animation Mixer
  if (mixer) {
    mixer.update(delta)
  }

  // Smooth Mecha Stance Rotation
  if (mechaRootGroup) {
    mechaRootGroup.rotation.y = manualRotY + Math.sin(time * 0.5) * 0.15 + mouseX * 0.35
    mechaRootGroup.position.y = Math.sin(time * 2) * 0.03
  }

  // Floating Cyber Sparks Animation
  if (fixedStageGroup) {
    const particles = fixedStageGroup.children.find(c => c.isPoints)
    if (particles) {
      particles.rotation.y = time * 0.1
    }
  }

  // Camera Parallax
  if (camera) {
    camera.position.x = mouseX * 0.3
    camera.position.y = 2.4 - mouseY * 0.25
    camera.lookAt(0, 1.1, 0)
  }

  if (renderer && scene && camera) {
    renderer.render(scene, camera)
  }
}

watch(() => props.activeIndex, (newVal) => {
  applyChampionSkin(newVal)
})

onMounted(() => {
  initStage()
})

onUnmounted(() => {
  window.removeEventListener('mousemove', onMouseMove)
  window.removeEventListener('resize', onResize)
  if (animationFrameId) cancelAnimationFrame(animationFrameId)
  if (renderer) renderer.dispose()
})
</script>

<template>
  <div class="relative flex flex-col items-center justify-center select-none w-full">
    <!-- 3D Three.js WebGL Stage Canvas Container -->
    <div
      ref="containerRef"
      class="h-[380px] sm:h-[420px] w-full max-w-[520px] cursor-grab active:cursor-grabbing filter drop-shadow-[0_0_50px_rgba(255,204,0,0.45)] relative"
      title="Kéo chuột để xoay 360° • Nhấp chuột để kích hoạt đòn đánh"
    >
      <!-- Loading Overlay -->
      <div
        v-if="isLoading"
        class="absolute inset-0 flex flex-col items-center justify-center bg-[#07090e]/80 backdrop-blur-sm z-30 font-mono text-xs text-amber-400 gap-2"
      >
        <span class="h-4 w-4 border-2 border-amber-400 border-t-transparent animate-spin rounded-full"></span>
        <span>KHỞI ĐỘNG VẬT LÝ 3D WARFRAME...</span>
      </div>
    </div>

    <!-- ── COMBAT ANIMATION CONTROL BAR (MECHA BREAK STYLE) ── -->
    <div class="mt-2 flex flex-wrap items-center justify-center gap-1.5 z-20 font-mono text-[10px] font-black max-w-lg">
      <button
        type="button"
        @click="playAnimation('Punch'); mechaAudio.playHeavyImpactDrop()"
        class="px-2.5 py-1 bg-red-950/80 hover:bg-red-600 text-red-300 hover:text-white border border-red-500/50 transition-all mecha-cut-tr shadow-[0_0_10px_rgba(239,68,68,0.3)] flex items-center gap-1"
      >
        <span>⚔️ TẤN CÔNG</span>
      </button>

      <button
        type="button"
        @click="playAnimation('Jump'); mechaAudio.playEngage()"
        class="px-2.5 py-1 bg-cyan-950/80 hover:bg-cyan-600 text-cyan-300 hover:text-white border border-cyan-500/50 transition-all mecha-cut-tr shadow-[0_0_10px_rgba(6,182,212,0.3)] flex items-center gap-1"
      >
        <span>🚀 BẬT NHẢY</span>
      </button>

      <button
        type="button"
        @click="playAnimation('Wave'); mechaAudio.playClick()"
        class="px-2.5 py-1 bg-amber-950/80 hover:bg-amber-500 text-amber-300 hover:text-slate-950 border border-amber-500/50 transition-all mecha-cut-tr shadow-[0_0_10px_rgba(245,158,11,0.3)] flex items-center gap-1"
      >
        <span>🫡 CHÀO TÁC CHIẾN</span>
      </button>

      <button
        type="button"
        @click="playAnimation('Running'); mechaAudio.playTargetLock()"
        class="px-2.5 py-1 bg-purple-950/80 hover:bg-purple-600 text-purple-300 hover:text-white border border-purple-500/50 transition-all mecha-cut-tr shadow-[0_0_10px_rgba(168,85,247,0.3)] flex items-center gap-1"
      >
        <span>🏃 XUNG PHONG</span>
      </button>

      <button
        type="button"
        @click="playAnimation('Dance'); mechaAudio.playClick()"
        class="px-2.5 py-1 bg-emerald-950/80 hover:bg-emerald-600 text-emerald-300 hover:text-white border border-emerald-500/50 transition-all mecha-cut-tr shadow-[0_0_10px_rgba(168,85,247,0.3)] flex items-center gap-1"
      >
        <span>🏆 ĂN MỪNG</span>
      </button>

      <button
        type="button"
        @click="playAnimation('Idle'); mechaAudio.playHover()"
        class="px-2.5 py-1 bg-slate-900 hover:bg-slate-700 text-slate-300 border border-slate-700 transition-all mecha-cut-tr flex items-center gap-1"
      >
        <span>🛡️ THỦ THẾ</span>
      </button>
    </div>

    <!-- HUD Telemetry Floating Footer Indicator -->
    <div class="mt-3 flex items-center justify-between w-full max-w-md px-3 py-1 font-mono text-[9px] font-bold text-amber-400/90 bg-[#07090e]/90 border border-amber-500/30 mecha-cut-corners shadow-[0_0_25px_rgba(255,204,0,0.2)]">
      <div class="flex items-center gap-1.5">
        <span class="h-1.5 w-1.5 bg-amber-400 animate-ping"></span>
        <span>HOẠT ẢNH: <span class="text-white uppercase">{{ currentActionName }}</span></span>
      </div>
      <div class="text-slate-400">
        XOAY 3D: <span class="text-emerald-400 font-black">CHUỘT TRÁI</span>
      </div>
      <div class="text-amber-300">
        V-SHIELD // AAA WARFRAME
      </div>
    </div>
  </div>
</template>
