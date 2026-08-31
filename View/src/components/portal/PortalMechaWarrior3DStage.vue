<script setup>
import { ref, onMounted, onUnmounted, watch, nextTick } from 'vue'
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
const currentActionName = ref('Thủ Thế (Idle)')
const isHeavySketchfabMode = ref(false)

let scene, camera, renderer, animationFrameId
let fixedStageGroup, mechaGroup, spotLight, ambientLight, dirLight
let reactorMesh, visorMesh, lFlameMesh, rFlameMesh, activeWeaponGroup
let leftArmGroup, rightArmGroup, torsoGroup, headGroup, jetpackGroup

// Interactive orbit and drag
let isDragging = false
let prevMouseX = 0, prevMouseY = 0
let mouseX = 0, mouseY = 0
let manualRotY = 0, manualRotX = 0
let currentAnimState = 'idle'
let animTimer = 0

// 5 Champions Color Profiles & Weapon Specs
const championProfiles = [
  {
    id: 0,
    name: 'Phạm Văn Thành',
    codename: 'V-SHIELD PRIME',
    primary: 0xffcc00,
    accent: 0xff5500,
    glow: 0xffcc00,
    metal: 0x141824,
    weaponType: 'broadsword'
  },
  {
    id: 1,
    name: 'Hà Mạnh Hùng',
    codename: 'PHANTOM FALCON',
    primary: 0xff5500,
    accent: 0xff0055,
    glow: 0xff3300,
    metal: 0x181216,
    weaponType: 'railgun'
  },
  {
    id: 2,
    name: 'Phạm Ngọc Hoài Anh',
    codename: 'DREADNOUGHT VORTEX',
    primary: 0x00f0ff,
    accent: 0x0088ff,
    glow: 0x00ffff,
    metal: 0x0c1622,
    weaponType: 'cannon'
  },
  {
    id: 3,
    name: 'Vũ Tiến Đạt',
    codename: 'SPECTRE STRIKER',
    primary: 0xa855f7,
    accent: 0xff00aa,
    glow: 0xcc44ff,
    metal: 0x161022,
    weaponType: 'daggers'
  },
  {
    id: 4,
    name: 'Nguyễn Quốc Việt',
    codename: 'TEMPEST JUGGERNAUT',
    primary: 0x10b981,
    accent: 0x06b6d4,
    glow: 0x00ff88,
    metal: 0x0f1c18,
    weaponType: 'halberd'
  }
]

// ── ULTRA-LIGHTWEIGHT MATERIALS (INSTANCED & REUSABLE) ──
let armorMat, accentMat, gunmetalMat, carbonMat, glowMat, visorGlowMat

function initSharedMaterials(profile) {
  armorMat = new THREE.MeshStandardMaterial({
    color: profile.primary,
    metalness: 0.9,
    roughness: 0.2
  })
  accentMat = new THREE.MeshStandardMaterial({
    color: profile.accent,
    metalness: 0.92,
    roughness: 0.18
  })
  gunmetalMat = new THREE.MeshStandardMaterial({
    color: profile.metal,
    metalness: 0.95,
    roughness: 0.25
  })
  carbonMat = new THREE.MeshStandardMaterial({
    color: 0x090b10,
    metalness: 0.8,
    roughness: 0.4
  })
  glowMat = new THREE.MeshBasicMaterial({
    color: profile.glow
  })
  visorGlowMat = new THREE.MeshBasicMaterial({
    color: profile.glow
  })
}

// ── ULTRA-LIGHT CHISELED MECHA WARFRAME BUILDER (< 1200 POLYS) ──
function buildUltraLightMecha(profile) {
  const mecha = new THREE.Group()

  // 1. TORSO & CORE (CHISELED V-TAPER CHEST)
  torsoGroup = new THREE.Group()
  torsoGroup.position.y = 1.35

  const chestGeo = new THREE.BoxGeometry(0.78, 0.48, 0.5)
  const chest = new THREE.Mesh(chestGeo, armorMat)
  chest.position.y = 0.25
  torsoGroup.add(chest)

  // Arc Reactor Core (Pulsing Center Glow)
  const reactorGeo = new THREE.CylinderGeometry(0.14, 0.14, 0.08, 12)
  reactorMesh = new THREE.Mesh(reactorGeo, glowMat)
  reactorMesh.position.set(0, 0.25, 0.26)
  reactorMesh.rotation.x = Math.PI / 2
  torsoGroup.add(reactorMesh)

  // Abdominal Armor & Hydraulic Waist
  const absGeo = new THREE.BoxGeometry(0.5, 0.38, 0.38)
  const absMesh = new THREE.Mesh(absGeo, gunmetalMat)
  absMesh.position.y = -0.16
  torsoGroup.add(absMesh)

  const beltGeo = new THREE.BoxGeometry(0.6, 0.12, 0.42)
  const belt = new THREE.Mesh(beltGeo, accentMat)
  belt.position.y = -0.38
  torsoGroup.add(belt)

  mecha.add(torsoGroup)

  // 2. HEAD & PREDATOR GUNDAM VISOR
  headGroup = new THREE.Group()
  headGroup.position.y = 2.1

  const helmGeo = new THREE.BoxGeometry(0.32, 0.32, 0.36)
  const helm = new THREE.Mesh(helmGeo, gunmetalMat)
  headGroup.add(helm)

  // Gundam V-Fin Crest
  const vFinGeo = new THREE.ConeGeometry(0.035, 0.38, 4)
  const vFinL = new THREE.Mesh(vFinGeo, armorMat)
  vFinL.position.set(-0.14, 0.22, 0.08)
  vFinL.rotation.set(-0.2, 0, -0.7)
  headGroup.add(vFinL)

  const vFinR = new THREE.Mesh(vFinGeo, armorMat)
  vFinR.position.set(0.14, 0.22, 0.08)
  vFinR.rotation.set(-0.2, 0, 0.7)
  headGroup.add(vFinR)

  // Glowing Predator Visor Eye
  const visorGeo = new THREE.BoxGeometry(0.25, 0.06, 0.06)
  visorMesh = new THREE.Mesh(visorGeo, visorGlowMat)
  visorMesh.position.set(0, 0.02, 0.19)
  headGroup.add(visorMesh)

  // Chin Plate
  const chinGeo = new THREE.ConeGeometry(0.08, 0.14, 4)
  const chin = new THREE.Mesh(chinGeo, accentMat)
  chin.position.set(0, -0.14, 0.14)
  chin.rotation.x = Math.PI
  headGroup.add(chin)

  mecha.add(headGroup)

  // 3. SHOULDER PAULDRONS & ARMS
  leftArmGroup = new THREE.Group()
  leftArmGroup.position.set(-0.58, 1.75, 0)

  const pldGeo = new THREE.BoxGeometry(0.35, 0.32, 0.42)
  const pldL = new THREE.Mesh(pldGeo, armorMat)
  leftArmGroup.add(pldL)

  const armUpperGeo = new THREE.CylinderGeometry(0.08, 0.09, 0.5, 6)
  const lUpperArm = new THREE.Mesh(armUpperGeo, gunmetalMat)
  lUpperArm.position.set(0, -0.38, 0)
  leftArmGroup.add(lUpperArm)

  const armLowerGeo = new THREE.BoxGeometry(0.2, 0.55, 0.2)
  const lLowerArm = new THREE.Mesh(armLowerGeo, armorMat)
  lLowerArm.position.set(0, -0.75, 0.1)
  lLowerArm.rotation.x = 0.35
  leftArmGroup.add(lLowerArm)

  mecha.add(leftArmGroup)

  rightArmGroup = new THREE.Group()
  rightArmGroup.position.set(0.58, 1.75, 0)

  const pldR = new THREE.Mesh(pldGeo, armorMat)
  rightArmGroup.add(pldR)

  const rUpperArm = new THREE.Mesh(armUpperGeo, gunmetalMat)
  rUpperArm.position.set(0, -0.38, 0)
  rightArmGroup.add(rUpperArm)

  const rLowerArm = new THREE.Mesh(armLowerGeo, armorMat)
  rLowerArm.position.set(0, -0.75, 0.1)
  rLowerArm.rotation.x = -0.2
  rightArmGroup.add(rLowerArm)

  mecha.add(rightArmGroup)

  // 4. JETPACK FLIGHT WINGS & PLASMA NOZZLES
  jetpackGroup = new THREE.Group()
  jetpackGroup.position.set(0, 1.45, -0.28)

  const packGeo = new THREE.BoxGeometry(0.5, 0.6, 0.2)
  const pack = new THREE.Mesh(packGeo, carbonMat)
  jetpackGroup.add(pack)

  const wingGeo = new THREE.BoxGeometry(0.08, 0.9, 0.22)
  const wingL = new THREE.Mesh(wingGeo, armorMat)
  wingL.position.set(-0.38, 0.3, -0.05)
  wingL.rotation.z = -0.7
  jetpackGroup.add(wingL)

  const wingR = new THREE.Mesh(wingGeo, armorMat)
  wingR.position.set(0.38, 0.3, -0.05)
  wingR.rotation.z = 0.7
  jetpackGroup.add(wingR)

  // Plasma Nozzles & Animated Flames
  const nozGeo = new THREE.CylinderGeometry(0.08, 0.12, 0.25, 8)
  const flameGeo = new THREE.ConeGeometry(0.09, 0.35, 8)

  const nozL = new THREE.Mesh(nozGeo, accentMat)
  nozL.position.set(-0.2, -0.32, 0)
  jetpackGroup.add(nozL)

  lFlameMesh = new THREE.Mesh(flameGeo, visorGlowMat)
  lFlameMesh.position.set(-0.2, -0.55, 0)
  lFlameMesh.rotation.x = Math.PI
  jetpackGroup.add(lFlameMesh)

  const nozR = new THREE.Mesh(nozGeo, accentMat)
  nozR.position.set(0.2, -0.32, 0)
  jetpackGroup.add(nozR)

  rFlameMesh = new THREE.Mesh(flameGeo, visorGlowMat)
  rFlameMesh.position.set(0.2, -0.55, 0)
  rFlameMesh.rotation.x = Math.PI
  jetpackGroup.add(rFlameMesh)

  mecha.add(jetpackGroup)

  // 5. COMBAT LEGS & BOOTS
  const legUpperGeo = new THREE.BoxGeometry(0.22, 0.65, 0.24)
  const legLowerGeo = new THREE.BoxGeometry(0.24, 0.75, 0.26)
  const kneeGeo = new THREE.ConeGeometry(0.09, 0.18, 4)
  const bootGeo = new THREE.BoxGeometry(0.28, 0.18, 0.45)

  // Left Leg
  const lThigh = new THREE.Mesh(legUpperGeo, gunmetalMat)
  lThigh.position.set(-0.25, 0.65, 0)
  mecha.add(lThigh)

  const lKnee = new THREE.Mesh(kneeGeo, accentMat)
  lKnee.position.set(-0.25, 0.38, 0.14)
  lKnee.rotation.x = Math.PI / 2
  mecha.add(lKnee)

  const lShin = new THREE.Mesh(legLowerGeo, armorMat)
  lShin.position.set(-0.25, 0.02, 0)
  mecha.add(lShin)

  const lBoot = new THREE.Mesh(bootGeo, carbonMat)
  lBoot.position.set(-0.25, -0.35, 0.06)
  mecha.add(lBoot)

  // Right Leg
  const rThigh = new THREE.Mesh(legUpperGeo, gunmetalMat)
  rThigh.position.set(0.25, 0.65, 0)
  mecha.add(rThigh)

  const rKnee = new THREE.Mesh(kneeGeo, accentMat)
  rKnee.position.set(0.25, 0.38, 0.14)
  rKnee.rotation.x = Math.PI / 2
  mecha.add(rKnee)

  const rShin = new THREE.Mesh(legLowerGeo, armorMat)
  rShin.position.set(0.25, 0.02, 0)
  mecha.add(rShin)

  const rBoot = new THREE.Mesh(bootGeo, carbonMat)
  rBoot.position.set(0.25, -0.35, 0.06)
  mecha.add(rBoot)

  // 6. ATTACH ACTIVE WEAPON
  activeWeaponGroup = buildUltraLightWeapon(profile.weaponType)
  mecha.add(activeWeaponGroup)

  return mecha
}

// ── ULTRA-LIGHT WEAPONS BUILDER ──
function buildUltraLightWeapon(weaponType) {
  const wGroup = new THREE.Group()

  if (weaponType === 'broadsword') {
    const bladeGeo = new THREE.BoxGeometry(0.16, 2.0, 0.03)
    const blade = new THREE.Mesh(bladeGeo, glowMat)
    blade.position.set(0.72, 1.4, 0.25)
    blade.rotation.z = 0.25
    wGroup.add(blade)

    const hiltGeo = new THREE.CylinderGeometry(0.04, 0.04, 0.5, 6)
    const hilt = new THREE.Mesh(hiltGeo, carbonMat)
    hilt.position.set(0.56, 0.5, 0.2)
    hilt.rotation.z = 0.25
    wGroup.add(hilt)
  } else if (weaponType === 'railgun') {
    const railGeo = new THREE.BoxGeometry(0.05, 0.07, 1.6)
    const r1 = new THREE.Mesh(railGeo, gunmetalMat)
    r1.position.set(0.68, 1.05, 0.7)
    wGroup.add(r1)

    const r2 = new THREE.Mesh(railGeo, gunmetalMat)
    r2.position.set(0.68, 0.92, 0.7)
    wGroup.add(r2)

    const coreGeo = new THREE.CylinderGeometry(0.025, 0.025, 1.5, 6)
    const core = new THREE.Mesh(coreGeo, glowMat)
    core.position.set(0.68, 0.98, 0.7)
    core.rotation.x = Math.PI / 2
    wGroup.add(core)
  } else if (weaponType === 'cannon') {
    const barrelGeo = new THREE.CylinderGeometry(0.15, 0.18, 1.5, 12)
    const barrel = new THREE.Mesh(barrelGeo, gunmetalMat)
    barrel.position.set(-0.58, 1.85, 0.35)
    barrel.rotation.x = Math.PI / 2
    wGroup.add(barrel)

    const ringGeo = new THREE.TorusGeometry(0.18, 0.03, 6, 12)
    const ring = new THREE.Mesh(ringGeo, glowMat)
    ring.position.set(-0.58, 1.85, 1.1)
    wGroup.add(ring)
  } else if (weaponType === 'daggers') {
    const dGeo = new THREE.ConeGeometry(0.08, 0.95, 4)
    const d1 = new THREE.Mesh(dGeo, glowMat)
    d1.position.set(-0.65, 0.8, 0.45)
    d1.rotation.x = 0.9
    wGroup.add(d1)

    const d2 = new THREE.Mesh(dGeo, glowMat)
    d2.position.set(0.65, 0.8, 0.45)
    d2.rotation.x = -0.9
    wGroup.add(d2)
  } else if (weaponType === 'halberd') {
    const shaftGeo = new THREE.CylinderGeometry(0.035, 0.035, 2.2, 6)
    const shaft = new THREE.Mesh(shaftGeo, carbonMat)
    shaft.position.set(0.72, 1.3, 0.2)
    wGroup.add(shaft)

    const tipGeo = new THREE.ConeGeometry(0.15, 0.65, 4)
    const tip = new THREE.Mesh(tipGeo, glowMat)
    tip.position.set(0.72, 2.45, 0.2)
    wGroup.add(tip)
  }

  return wGroup
}

function initStage() {
  const container = containerRef.value
  if (!container) return

  disposeThree()

  const width = container.clientWidth || 480
  const height = container.clientHeight || 420

  // 1. Scene & Camera
  scene = new THREE.Scene()
  camera = new THREE.PerspectiveCamera(40, width / height, 0.1, 1000)
  camera.position.set(0, 2.3, 6.0)
  camera.lookAt(0, 1.15, 0)

  // 2. High-Efficiency Lighting
  ambientLight = new THREE.AmbientLight(0xffffff, 1.4)
  scene.add(ambientLight)

  dirLight = new THREE.DirectionalLight(0xffffff, 2.5)
  dirLight.position.set(5, 8, 6)
  scene.add(dirLight)

  const rimLight = new THREE.DirectionalLight(0x00f0ff, 1.8)
  rimLight.position.set(-5, 3, -4)
  scene.add(rimLight)

  const currentProfile = championProfiles[props.activeIndex] || championProfiles[0]
  spotLight = new THREE.PointLight(currentProfile.glow, 3.2, 14)
  spotLight.position.set(0, 3.0, 3.2)
  scene.add(spotLight)

  // 3. WebGL Renderer with performance clamping
  try {
    renderer = new THREE.WebGLRenderer({ alpha: true, antialias: true, powerPreference: 'high-performance' })
    renderer.setSize(width, height)
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.5))
    container.appendChild(renderer.domElement)
  } catch (_) {
    return
  }

  // ── 4. FIXED HOLOGRAPHIC LAUNCH PEDESTAL ──
  fixedStageGroup = new THREE.Group()
  scene.add(fixedStageGroup)

  const baseGeo = new THREE.CylinderGeometry(2.4, 2.6, 0.3, 8)
  const baseMat = new THREE.MeshStandardMaterial({
    color: 0x07090e,
    metalness: 0.95,
    roughness: 0.2
  })
  const baseMesh = new THREE.Mesh(baseGeo, baseMat)
  baseMesh.position.y = -0.15
  fixedStageGroup.add(baseMesh)

  const outerRingGeo = new THREE.TorusGeometry(2.55, 0.035, 8, 32)
  const ringMat = new THREE.MeshBasicMaterial({ color: currentProfile.glow })
  const outerRing = new THREE.Mesh(outerRingGeo, ringMat)
  outerRing.rotation.x = Math.PI / 2
  fixedStageGroup.add(outerRing)

  // 4 Corner Energy Emitters
  for (let i = 0; i < 4; i++) {
    const angle = (i / 4) * Math.PI * 2 + Math.PI / 4
    const pylonGeo = new THREE.BoxGeometry(0.14, 0.4, 0.14)
    const pylon = new THREE.Mesh(pylonGeo, gunmetalMat || baseMat)
    pylon.position.set(Math.cos(angle) * 2.3, 0.08, Math.sin(angle) * 2.3)
    fixedStageGroup.add(pylon)

    const tipGeo = new THREE.SphereGeometry(0.05, 6, 6)
    const tip = new THREE.Mesh(tipGeo, ringMat)
    tip.position.set(Math.cos(angle) * 2.3, 0.32, Math.sin(angle) * 2.3)
    fixedStageGroup.add(tip)
  }

  // 100 Floating Cyber Particles
  const pCount = 100
  const pPositions = new Float32Array(pCount * 3)
  for (let i = 0; i < pCount * 3; i += 3) {
    pPositions[i] = (Math.random() - 0.5) * 4.2
    pPositions[i + 1] = Math.random() * 3.2
    pPositions[i + 2] = (Math.random() - 0.5) * 4.2
  }
  const pGeo = new THREE.BufferGeometry()
  pGeo.setAttribute('position', new THREE.BufferAttribute(pPositions, 3))
  const pMat = new THREE.PointsMaterial({
    color: currentProfile.glow,
    size: 0.04,
    transparent: true,
    opacity: 0.75
  })
  const particles = new THREE.Points(pGeo, pMat)
  fixedStageGroup.add(particles)

  // ── 5. BUILD MECHA WARFRAME INSTANCE ──
  initSharedMaterials(currentProfile)
  mechaGroup = buildUltraLightMecha(currentProfile)
  scene.add(mechaGroup)

  // Listeners
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
    triggerCombatAction('strike')
  })

  animate()
}

function updateChampionSkin(index) {
  const profile = championProfiles[index] || championProfiles[0]

  if (spotLight) spotLight.color.setHex(profile.glow)

  initSharedMaterials(profile)

  if (mechaGroup) {
    scene.remove(mechaGroup)
  }
  mechaGroup = buildUltraLightMecha(profile)
  scene.add(mechaGroup)
}

function triggerCombatAction(actionType) {
  currentAnimState = actionType
  animTimer = 0

  if (actionType === 'strike') {
    currentActionName.value = 'Tấn Công (Strike Combo)'
    mechaAudio.playHeavyImpactDrop()
  } else if (actionType === 'jump') {
    currentActionName.value = 'Bật Nhảy Phản Lực (Thruster Boost)'
    mechaAudio.playEngage()
  } else if (actionType === 'salute') {
    currentActionName.value = 'Chào Tác Chiến (Tactical Salute)'
    mechaAudio.playClick()
  } else if (actionType === 'sprint') {
    currentActionName.value = 'Xung Phong (Overdrive Sprint)'
    mechaAudio.playTargetLock()
  } else if (actionType === 'dance') {
    currentActionName.value = 'Ăn Mừng Thắng Lợi (Victory Pulse)'
    mechaAudio.playTargetLock()
  } else {
    currentActionName.value = 'Thủ Thế (Combat Idle)'
    mechaAudio.playHover()
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

  const time = Date.now() * 0.003
  animTimer += 0.04

  // Smooth Mecha Rotations & Physics Breathing
  if (mechaGroup) {
    mechaGroup.rotation.y = manualRotY + Math.sin(time * 0.5) * 0.12 + mouseX * 0.3
    
    // Procedural Action State Machine
    if (currentAnimState === 'strike') {
      const strikeProg = Math.sin(Math.min(animTimer * 3, Math.PI))
      if (rightArmGroup) rightArmGroup.rotation.x = -strikeProg * 1.5
      if (leftArmGroup) leftArmGroup.rotation.x = strikeProg * 0.8
      if (torsoGroup) torsoGroup.rotation.y = strikeProg * 0.35
      if (animTimer > 1.2) currentAnimState = 'idle'
    } else if (currentAnimState === 'jump') {
      const jumpProg = Math.sin(Math.min(animTimer * 2, Math.PI))
      mechaGroup.position.y = jumpProg * 0.7
      if (lFlameMesh) lFlameMesh.scale.set(1 + jumpProg * 2, 1 + jumpProg * 3, 1 + jumpProg * 2)
      if (rFlameMesh) rFlameMesh.scale.set(1 + jumpProg * 2, 1 + jumpProg * 3, 1 + jumpProg * 2)
      if (animTimer > 1.6) currentAnimState = 'idle'
    } else if (currentAnimState === 'salute') {
      const saluteProg = Math.sin(Math.min(animTimer * 2, Math.PI))
      if (rightArmGroup) rightArmGroup.rotation.z = -saluteProg * 1.6
      if (rightArmGroup) rightArmGroup.rotation.x = saluteProg * 0.4
      if (animTimer > 1.6) currentAnimState = 'idle'
    } else if (currentAnimState === 'sprint') {
      const sprintCycle = Math.sin(animTimer * 8)
      if (leftArmGroup) leftArmGroup.rotation.x = sprintCycle * 0.8
      if (rightArmGroup) rightArmGroup.rotation.x = -sprintCycle * 0.8
      mechaGroup.position.z = Math.sin(animTimer * 4) * 0.15
      mechaGroup.rotation.x = 0.2
      if (animTimer > 2.0) {
        mechaGroup.rotation.x = 0
        mechaGroup.position.z = 0
        currentAnimState = 'idle'
      }
    } else if (currentAnimState === 'dance') {
      const danceCycle = Math.sin(animTimer * 6)
      mechaGroup.position.y = Math.abs(danceCycle) * 0.25
      if (leftArmGroup) leftArmGroup.rotation.z = danceCycle * 0.7
      if (rightArmGroup) rightArmGroup.rotation.z = -danceCycle * 0.7
      if (animTimer > 2.2) currentAnimState = 'idle'
    } else {
      // Idle Breathing
      mechaGroup.position.y = Math.sin(time * 2) * 0.03
      if (torsoGroup) torsoGroup.rotation.y = 0
      if (rightArmGroup) rightArmGroup.rotation.set(0, 0, 0)
      if (leftArmGroup) leftArmGroup.rotation.set(0, 0, 0)
      if (lFlameMesh) lFlameMesh.scale.set(1, 1 + Math.sin(time * 6) * 0.3, 1)
      if (rFlameMesh) rFlameMesh.scale.set(1, 1 + Math.sin(time * 6) * 0.3, 1)
    }

    // Core Reactor Energy Pulse
    if (reactorMesh) {
      const pulse = 1.0 + Math.sin(time * 4) * 0.15
      reactorMesh.scale.set(pulse, pulse, pulse)
    }
  }

  // Floating Cyber Sparks Animation
  if (fixedStageGroup) {
    const particles = fixedStageGroup.children.find(c => c.isPoints)
    if (particles) {
      particles.rotation.y = time * 0.08
    }
  }

  // Camera tracking
  if (camera) {
    camera.position.x = mouseX * 0.25
    camera.position.y = 2.3 - mouseY * 0.2
    camera.lookAt(0, 1.15, 0)
  }

  if (renderer && scene && camera) {
    renderer.render(scene, camera)
  }
}

function disposeThree() {
  if (animationFrameId) cancelAnimationFrame(animationFrameId)
  if (renderer) {
    renderer.dispose()
    if (renderer.domElement && renderer.domElement.parentNode) {
      renderer.domElement.parentNode.removeChild(renderer.domElement)
    }
  }
}

function toggleSketchfab() {
  isHeavySketchfabMode.value = !isHeavySketchfabMode.value
  mechaAudio.playEngage()
  if (!isHeavySketchfabMode.value) {
    nextTick(() => {
      initStage()
    })
  } else {
    disposeThree()
  }
}

watch(() => props.activeIndex, (newVal) => {
  if (!isHeavySketchfabMode.value) {
    updateChampionSkin(newVal)
  }
})

onMounted(() => {
  initStage()
})

onUnmounted(() => {
  window.removeEventListener('mousemove', onMouseMove)
  window.removeEventListener('resize', onResize)
  disposeThree()
})
</script>

<template>
  <div class="relative flex flex-col items-center justify-center select-none w-full">
    
    <!-- ── ULTRA-LIGHTWEIGHT 60FPS THREE.JS WARFRAME STAGE (DEFAULT) ── -->
    <div
      v-if="!isHeavySketchfabMode"
      class="relative flex flex-col items-center justify-center w-full"
    >
      <!-- 3D Three.js WebGL Stage Canvas Container (0s Instant Load, < 15MB VRAM) -->
      <div
        ref="containerRef"
        class="h-[380px] sm:h-[420px] w-full max-w-[520px] cursor-grab active:cursor-grabbing filter drop-shadow-[0_0_50px_rgba(255,204,0,0.4)] relative"
        title="Kéo chuột để xoay 360° • Nhấp chuột để ra đòn tấn công"
      ></div>

      <!-- Combat Animation Control Bar (Instant Interactive Actions) -->
      <div class="mt-2 flex flex-wrap items-center justify-center gap-1.5 z-20 font-mono text-[10px] font-black max-w-lg">
        <button
          type="button"
          @click="triggerCombatAction('strike')"
          class="px-2.5 py-1 bg-red-950/80 hover:bg-red-600 text-red-300 hover:text-white border border-red-500/50 transition-all mecha-cut-tr shadow-[0_0_10px_rgba(239,68,68,0.3)] flex items-center gap-1"
        >
          <span>⚔️ TẤN CÔNG</span>
        </button>

        <button
          type="button"
          @click="triggerCombatAction('jump')"
          class="px-2.5 py-1 bg-cyan-950/80 hover:bg-cyan-600 text-cyan-300 hover:text-white border border-cyan-500/50 transition-all mecha-cut-tr shadow-[0_0_10px_rgba(6,182,212,0.3)] flex items-center gap-1"
        >
          <span>🚀 BẬT NHẢY</span>
        </button>

        <button
          type="button"
          @click="triggerCombatAction('salute')"
          class="px-2.5 py-1 bg-amber-950/80 hover:bg-amber-500 text-amber-300 hover:text-slate-950 border border-amber-500/50 transition-all mecha-cut-tr shadow-[0_0_10px_rgba(245,158,11,0.3)] flex items-center gap-1"
        >
          <span>🫡 CHÀO CHIẾN ĐỘI</span>
        </button>

        <button
          type="button"
          @click="triggerCombatAction('sprint')"
          class="px-2.5 py-1 bg-purple-950/80 hover:bg-purple-600 text-purple-300 hover:text-white border border-purple-500/50 transition-all mecha-cut-tr shadow-[0_0_10px_rgba(168,85,247,0.3)] flex items-center gap-1"
        >
          <span>🏃 XUNG PHONG</span>
        </button>

        <button
          type="button"
          @click="triggerCombatAction('dance')"
          class="px-2.5 py-1 bg-emerald-950/80 hover:bg-emerald-600 text-emerald-300 hover:text-white border border-emerald-500/50 transition-all mecha-cut-tr shadow-[0_0_10px_rgba(16,185,129,0.3)] flex items-center gap-1"
        >
          <span>🏆 ĂN MỪNG</span>
        </button>

        <button
          type="button"
          @click="triggerCombatAction('idle')"
          class="px-2.5 py-1 bg-slate-900 hover:bg-slate-700 text-slate-300 border border-slate-700 transition-all mecha-cut-tr flex items-center gap-1"
        >
          <span>🛡️ THỦ THẾ</span>
        </button>
      </div>

      <!-- HUD Telemetry Footer -->
      <div class="mt-3 flex items-center justify-between w-full max-w-md px-3 py-1 font-mono text-[9px] font-bold text-amber-400/90 bg-[#07090e]/90 border border-amber-500/30 mecha-cut-corners shadow-[0_0_25px_rgba(255,204,0,0.2)]">
        <div class="flex items-center gap-1.5">
          <span class="h-1.5 w-1.5 bg-emerald-400 animate-ping"></span>
          <span>TRẠNG THÁI: <span class="text-white uppercase">{{ currentActionName }}</span></span>
        </div>
        <div class="text-slate-400">
          TỐC ĐỘ: <span class="text-emerald-400 font-black">60 FPS (SIÊU NHẸ)</span>
        </div>
      </div>
    </div>

    <!-- ── OPTIONAL HEAVY SKETCHFAB 3D VIEW (ON-DEMAND) ── -->
    <div
      v-else
      class="relative h-[380px] sm:h-[430px] w-full max-w-[540px] border-2 border-amber-500/50 bg-[#07090e] mecha-cut-corners shadow-[0_0_50px_rgba(255,204,0,0.35)] overflow-hidden"
    >
      <iframe
        title="BOT MECHA Warrior 3d by Oscar Creativo"
        class="w-full h-full border-0"
        src="https://sketchfab.com/models/34850bfe441642788154c4a8a0bd60e4/embed?autostart=1&preload=1&ui_theme=dark&ui_infos=0&ui_watermark=0&ui_stop=0&ui_hint=2&dnt=1"
        allow="autoplay; fullscreen; xr-spatial-tracking"
        xr-spatial-tracking="true"
        allowfullscreen
      ></iframe>

      <div class="pointer-events-none absolute top-2 left-2 flex items-center gap-1.5 bg-[#07080b]/90 px-2 py-0.5 border border-amber-500/40 text-[9px] font-mono font-bold text-amber-400">
        <span class="h-1.5 w-1.5 bg-amber-400 animate-ping"></span>
        <span>SKETCHFAB 3D // OSCAR CREATIVO (50.7K POLYS)</span>
      </div>
    </div>

    <!-- ── OPTIONAL ON-DEMAND TOGGLE BUTTON ── -->
    <div class="mt-2 flex items-center justify-center">
      <button
        type="button"
        @click="toggleSketchfab"
        class="text-[9.5px] font-mono font-bold text-slate-400 hover:text-amber-300 transition-colors flex items-center gap-1.5 py-0.5 px-2 bg-slate-900/60 border border-slate-800 hover:border-amber-500/40 mecha-cut-tr"
      >
        <span v-if="!isHeavySketchfabMode">🌐 Xem bản gốc Sketchfab (Mô hình nặng 50k polys) »</span>
        <span v-else class="text-amber-400">⚡ Quay lại bản Siêu Nhẹ 60FPS (V-Shield Warframe) «</span>
      </button>
    </div>
  </div>
</template>

