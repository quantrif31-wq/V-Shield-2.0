<script setup>
import { ref, onMounted, onUnmounted, watch } from 'vue'
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
let scene, camera, renderer, animationFrameId
let fixedStageGroup, revolvingGroup, spotLight
const mechaRobots = []
let targetAngle = 0
let currentAngle = 0
let isDragging = false
let prevMouseX = 0, prevMouseY = 0
let mouseX = 0, mouseY = 0
let manualRotY = 0, manualRotX = 0

// Colors for the 5 Champions
const championColors = [
  { primary: 0xffcc00, accent: 0xff5500, glow: 0xffcc00 }, // 0: Thành (Amber Gold)
  { primary: 0xff5500, accent: 0xff0055, glow: 0xff3300 }, // 1: Hùng (Flame Red-Orange)
  { primary: 0x00f0ff, accent: 0x0088ff, glow: 0x00ffff }, // 2: Hoài Anh (Electric Cyan)
  { primary: 0xa855f7, accent: 0xff00aa, glow: 0xcc44ff }, // 3: Đạt (Cyber Purple)
  { primary: 0x10b981, accent: 0x06b6d4, glow: 0x00ff88 }  // 4: Việt (Emerald Matrix)
]

// ── BUILD ULTRA-BADASS AAA MECHA WARFRAME ──
function buildBadassMecha(index, colorScheme) {
  const mecha = new THREE.Group()

  // Materials
  const gunmetalMat = new THREE.MeshStandardMaterial({
    color: 0x141822,
    metalness: 0.92,
    roughness: 0.22
  })
  const carbonMat = new THREE.MeshStandardMaterial({
    color: 0x090c12,
    metalness: 0.85,
    roughness: 0.4
  })
  const armorMat = new THREE.MeshStandardMaterial({
    color: colorScheme.primary,
    metalness: 0.88,
    roughness: 0.18
  })
  const accentMat = new THREE.MeshStandardMaterial({
    color: colorScheme.accent,
    metalness: 0.9,
    roughness: 0.2
  })
  const glowMat = new THREE.MeshBasicMaterial({
    color: colorScheme.glow
  })
  const visorGlowMat = new THREE.MeshBasicMaterial({
    color: 0x00f0ff
  })
  const wireframeMat = new THREE.MeshBasicMaterial({
    color: 0x222c3c,
    wireframe: true,
    transparent: true,
    opacity: 0.22
  })

  // 1. HEAD & PREDATOR VISOR (Sharp Gundam / Armored Core helmet)
  const headGroup = new THREE.Group()
  headGroup.position.y = 2.45

  // Main Helmet
  const helmGeo = new THREE.BoxGeometry(0.36, 0.36, 0.42)
  const helm = new THREE.Mesh(helmGeo, gunmetalMat)
  headGroup.add(helm)

  // Gundam V-Fin / Antenna Crest on forehead
  const vFinGeoL = new THREE.ConeGeometry(0.04, 0.45, 4)
  const vFinL = new THREE.Mesh(vFinGeoL, armorMat)
  vFinL.position.set(-0.16, 0.28, 0.08)
  vFinL.rotation.z = -Math.PI / 4
  vFinL.rotation.x = -Math.PI / 12
  headGroup.add(vFinL)

  const vFinR = new THREE.Mesh(vFinGeoL, armorMat)
  vFinR.position.set(0.16, 0.28, 0.08)
  vFinR.rotation.z = Math.PI / 4
  vFinR.rotation.x = -Math.PI / 12
  headGroup.add(vFinR)

  // Narrow Predator Visor Slit (Laser Eye)
  const visorGeo = new THREE.BoxGeometry(0.28, 0.07, 0.08)
  const visor = new THREE.Mesh(visorGeo, visorGlowMat)
  visor.position.set(0, 0.03, 0.21)
  headGroup.add(visor)

  // Chin Guard / Face Plate
  const chinGeo = new THREE.ConeGeometry(0.1, 0.18, 4)
  const chin = new THREE.Mesh(chinGeo, armorMat)
  chin.position.set(0, -0.16, 0.16)
  chin.rotation.x = Math.PI
  headGroup.add(chin)

  mecha.add(headGroup)

  // 2. TORSO & REACTOR CORE (Chiseled V-Taper Armor Plates)
  const torsoGroup = new THREE.Group()
  torsoGroup.position.y = 1.6

  // Main Heavy Chest Armor (Upper)
  const chestUpperGeo = new THREE.BoxGeometry(0.85, 0.55, 0.55)
  const chestUpper = new THREE.Mesh(chestUpperGeo, armorMat)
  chestUpper.position.y = 0.28
  torsoGroup.add(chestUpper)

  // Center Chest Glowing Reactor Arc
  const reactorGeo = new THREE.CylinderGeometry(0.16, 0.16, 0.1, 16)
  const reactor = new THREE.Mesh(reactorGeo, glowMat)
  reactor.position.set(0, 0.28, 0.28)
  reactor.rotation.x = Math.PI / 2
  torsoGroup.add(reactor)

  // Side Heat Exhaust Vents
  const ventGeo = new THREE.BoxGeometry(0.12, 0.35, 0.1)
  const ventL = new THREE.Mesh(ventGeo, carbonMat)
  ventL.position.set(-0.35, 0.28, 0.24)
  torsoGroup.add(ventL)

  const ventR = new THREE.Mesh(ventGeo, carbonMat)
  ventR.position.set(0.35, 0.28, 0.24)
  torsoGroup.add(ventR)

  // Lower Torso & Abdominal Hydraulics
  const absGeo = new THREE.BoxGeometry(0.55, 0.45, 0.42)
  const absMesh = new THREE.Mesh(absGeo, gunmetalMat)
  absMesh.position.y = -0.22
  torsoGroup.add(absMesh)

  // Heavy Belt / Tasset Armor
  const beltGeo = new THREE.BoxGeometry(0.65, 0.15, 0.46)
  const belt = new THREE.Mesh(beltGeo, accentMat)
  belt.position.y = -0.48
  torsoGroup.add(belt)

  mecha.add(torsoGroup)

  // 3. HEAVY MULTI-TIERED SHOULDER PAULDRONS & ARMS
  // Left Shoulder (with missile tubes)
  const lShoulderGroup = new THREE.Group()
  lShoulderGroup.position.set(-0.68, 2.05, 0)

  const pldGeo = new THREE.BoxGeometry(0.42, 0.38, 0.5)
  const pldL = new THREE.Mesh(pldGeo, armorMat)
  lShoulderGroup.add(pldL)

  // Missile Pod Tubes
  for (let r = 0; r < 2; r++) {
    for (let c = 0; c < 2; c++) {
      const tubeGeo = new THREE.CylinderGeometry(0.04, 0.04, 0.1, 8)
      const tube = new THREE.Mesh(tubeGeo, carbonMat)
      tube.position.set((r - 0.5) * 0.15, 0.12, (c - 0.5) * 0.15 + 0.2)
      tube.rotation.x = Math.PI / 2
      lShoulderGroup.add(tube)
    }
  }
  mecha.add(lShoulderGroup)

  // Right Shoulder
  const rShoulderGroup = new THREE.Group()
  rShoulderGroup.position.set(0.68, 2.05, 0)
  const pldR = new THREE.Mesh(pldGeo, armorMat)
  rShoulderGroup.add(pldR)
  for (let r = 0; r < 2; r++) {
    for (let c = 0; c < 2; c++) {
      const tubeGeo = new THREE.CylinderGeometry(0.04, 0.04, 0.1, 8)
      const tube = new THREE.Mesh(tubeGeo, carbonMat)
      tube.position.set((r - 0.5) * 0.15, 0.12, (c - 0.5) * 0.15 + 0.2)
      tube.rotation.x = Math.PI / 2
      rShoulderGroup.add(tube)
    }
  }
  mecha.add(rShoulderGroup)

  // Forearms & Hands
  const armUpperGeo = new THREE.CylinderGeometry(0.1, 0.12, 0.6, 8)
  const armLowerGeo = new THREE.BoxGeometry(0.24, 0.65, 0.24)

  // Left Arm
  const lArmUpper = new THREE.Mesh(armUpperGeo, gunmetalMat)
  lArmUpper.position.set(-0.65, 1.55, 0.05)
  mecha.add(lArmUpper)

  const lArmLower = new THREE.Mesh(armLowerGeo, armorMat)
  lArmLower.position.set(-0.65, 1.05, 0.2)
  lArmLower.rotation.x = Math.PI / 5
  mecha.add(lArmLower)

  // Right Arm
  const rArmUpper = new THREE.Mesh(armUpperGeo, gunmetalMat)
  rArmUpper.position.set(0.65, 1.55, 0.05)
  mecha.add(rArmUpper)

  const rArmLower = new THREE.Mesh(armLowerGeo, armorMat)
  rArmLower.position.set(0.65, 1.05, 0.2)
  rArmLower.rotation.x = -Math.PI / 6
  mecha.add(rArmLower)

  // 4. TWIN JETPACK THRUSTER WINGS (Backpack)
  const jetpackGroup = new THREE.Group()
  jetpackGroup.position.set(0, 1.7, -0.32)

  const packMainGeo = new THREE.BoxGeometry(0.6, 0.7, 0.25)
  const packMain = new THREE.Mesh(packMainGeo, carbonMat)
  jetpackGroup.add(packMain)

  // Wing Stabilizers
  const wingGeo = new THREE.BoxGeometry(0.12, 1.1, 0.28)
  const wingL = new THREE.Mesh(wingGeo, armorMat)
  wingL.position.set(-0.45, 0.4, -0.05)
  wingL.rotation.z = -Math.PI / 4
  jetpackGroup.add(wingL)

  const wingR = new THREE.Mesh(wingGeo, armorMat)
  wingR.position.set(0.45, 0.4, -0.05)
  wingR.rotation.z = Math.PI / 4
  jetpackGroup.add(wingR)

  // Glowing Plasma Thruster Nozzles
  const nozzleGeo = new THREE.CylinderGeometry(0.1, 0.16, 0.35, 12)
  const nozzleMat = accentMat
  const flameGeo = new THREE.ConeGeometry(0.12, 0.45, 12)

  const nozL = new THREE.Mesh(nozzleGeo, nozzleMat)
  nozL.position.set(-0.25, -0.38, 0)
  jetpackGroup.add(nozL)
  const flameL = new THREE.Mesh(flameGeo, visorGlowMat)
  flameL.position.set(-0.25, -0.65, 0)
  flameL.rotation.x = Math.PI
  jetpackGroup.add(flameL)

  const nozR = new THREE.Mesh(nozzleGeo, nozzleMat)
  nozR.position.set(0.25, -0.38, 0)
  jetpackGroup.add(nozR)
  const flameR = new THREE.Mesh(flameGeo, visorGlowMat)
  flameR.position.set(0.25, -0.65, 0)
  flameR.rotation.x = Math.PI
  jetpackGroup.add(flameR)

  mecha.add(jetpackGroup)

  // 5. LEGS & COMBAT ARMORED BOOTS
  const legUpperGeo = new THREE.BoxGeometry(0.26, 0.75, 0.3)
  const legLowerGeo = new THREE.BoxGeometry(0.28, 0.85, 0.32)
  const kneeGeo = new THREE.ConeGeometry(0.12, 0.22, 4)
  const bootGeo = new THREE.BoxGeometry(0.34, 0.22, 0.55)

  // Left Leg
  const lThigh = new THREE.Mesh(legUpperGeo, gunmetalMat)
  lThigh.position.set(-0.3, 0.75, 0)
  mecha.add(lThigh)

  const lKnee = new THREE.Mesh(kneeGeo, accentMat)
  lKnee.position.set(-0.3, 0.45, 0.18)
  lKnee.rotation.x = Math.PI / 2
  mecha.add(lKnee)

  const lShin = new THREE.Mesh(legLowerGeo, armorMat)
  lShin.position.set(-0.3, 0.05, 0)
  mecha.add(lShin)

  const lBoot = new THREE.Mesh(bootGeo, carbonMat)
  lBoot.position.set(-0.3, -0.38, 0.08)
  mecha.add(lBoot)

  // Right Leg
  const rThigh = new THREE.Mesh(legUpperGeo, gunmetalMat)
  rThigh.position.set(0.3, 0.75, 0)
  mecha.add(rThigh)

  const rKnee = new THREE.Mesh(kneeGeo, accentMat)
  rKnee.position.set(0.3, 0.45, 0.18)
  rKnee.rotation.x = Math.PI / 2
  mecha.add(rKnee)

  const rShin = new THREE.Mesh(legLowerGeo, armorMat)
  rShin.position.set(0.3, 0.05, 0)
  mecha.add(rShin)

  const rBoot = new THREE.Mesh(bootGeo, carbonMat)
  rBoot.position.set(0.3, -0.38, 0.08)
  mecha.add(rBoot)

  // 6. DISTINCT 3D CINEMATIC WEAPONS
  const weaponGroup = new THREE.Group()

  if (index === 0) {
    // Thành: Massive Quantum Broadsword
    const bGeo = new THREE.BoxGeometry(0.24, 2.4, 0.06)
    const blade = new THREE.Mesh(bGeo, glowMat)
    blade.position.set(0.9, 2.2, 0.4)
    blade.rotation.z = Math.PI / 10
    weaponGroup.add(blade)

    const hiltGeo = new THREE.CylinderGeometry(0.06, 0.06, 0.7, 8)
    const hilt = new THREE.Mesh(hiltGeo, carbonMat)
    hilt.position.set(0.68, 1.1, 0.3)
    hilt.rotation.z = Math.PI / 10
    weaponGroup.add(hilt)
  } else if (index === 1) {
    // Hùng: Heavy Plasma Railgun with dual rails
    const rail1Geo = new THREE.BoxGeometry(0.08, 0.1, 2.2)
    const rail1 = new THREE.Mesh(rail1Geo, gunmetalMat)
    rail1.position.set(0.85, 1.6, 0.9)
    weaponGroup.add(rail1)

    const rail2Geo = new THREE.BoxGeometry(0.08, 0.1, 2.2)
    const rail2 = new THREE.Mesh(rail2Geo, gunmetalMat)
    rail2.position.set(0.85, 1.45, 0.9)
    weaponGroup.add(rail2)

    const beamGeo = new THREE.CylinderGeometry(0.04, 0.04, 2.0, 8)
    const beam = new THREE.Mesh(beamGeo, glowMat)
    beam.position.set(0.85, 1.52, 0.9)
    beam.rotation.x = Math.PI / 2
    weaponGroup.add(beam)
  } else if (index === 2) {
    // Hoài Anh: Massive Titan Heavy Shoulder Cannon
    const cannonGeo = new THREE.CylinderGeometry(0.22, 0.28, 2.0, 16)
    const cannon = new THREE.Mesh(cannonGeo, gunmetalMat)
    cannon.position.set(-0.72, 2.45, 0.5)
    cannon.rotation.x = Math.PI / 2
    weaponGroup.add(cannon)

    const muzzleGeo = new THREE.TorusGeometry(0.25, 0.05, 8, 16)
    const muzzle = new THREE.Mesh(muzzleGeo, glowMat)
    muzzle.position.set(-0.72, 2.45, 1.5)
    weaponGroup.add(muzzle)
  } else if (index === 3) {
    // Đạt: Dual High-Frequency Cyber Daggers
    const dGeo = new THREE.ConeGeometry(0.12, 1.3, 4)
    const d1 = new THREE.Mesh(dGeo, glowMat)
    d1.position.set(-0.85, 1.2, 0.6)
    d1.rotation.x = Math.PI / 2.8
    weaponGroup.add(d1)

    const d2 = new THREE.Mesh(dGeo, glowMat)
    d2.position.set(0.85, 1.2, 0.6)
    d2.rotation.x = -Math.PI / 2.8
    weaponGroup.add(d2)
  } else if (index === 4) {
    // Việt: Heavy Thunderstrike Halberd
    const poleGeo = new THREE.CylinderGeometry(0.06, 0.06, 2.8, 8)
    const pole = new THREE.Mesh(poleGeo, carbonMat)
    pole.position.set(0.9, 2.0, 0.35)
    weaponGroup.add(pole)

    const spearGeo = new THREE.ConeGeometry(0.25, 0.9, 4)
    const spear = new THREE.Mesh(spearGeo, glowMat)
    spear.position.set(0.9, 3.45, 0.35)
    weaponGroup.add(spear)
  }

  mecha.add(weaponGroup)

  return {
    group: mecha,
    reactor,
    visor,
    weapon: weaponGroup,
    armorMat,
    accentMat,
    gunmetalMat,
    carbonMat,
    glowMat,
    wireframeMat
  }
}

function initStage() {
  const container = containerRef.value
  if (!container) return

  const width = container.clientWidth || 480
  const height = container.clientHeight || 420

  // 1. Scene & Camera
  scene = new THREE.Scene()
  camera = new THREE.PerspectiveCamera(38, width / height, 0.1, 1000)
  camera.position.set(0, 3.4, 9.8)
  camera.lookAt(0, 1.5, 0)

  // 2. Lights
  const ambientLight = new THREE.AmbientLight(0xffffff, 0.9)
  scene.add(ambientLight)

  const dirLight = new THREE.DirectionalLight(0xffffff, 1.8)
  dirLight.position.set(5, 10, 7)
  scene.add(dirLight)

  spotLight = new THREE.PointLight(0xffcc00, 2.5, 18)
  spotLight.position.set(0, 2.5, 3)
  scene.add(spotLight)

  // 3. WebGL Renderer with safe try-catch
  try {
    renderer = new THREE.WebGLRenderer({ alpha: true, antialias: true, powerPreference: 'high-performance' })
    renderer.setSize(width, height)
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2))
    container.appendChild(renderer.domElement)
  } catch (_) {
    return
  }

  // ── 4. COMPLETELY FIXED BASE PEDESTAL (ĐẾ CỐ ĐỊNH HOÀN TOÀN, KHÔNG QUAY) ──
  fixedStageGroup = new THREE.Group()
  scene.add(fixedStageGroup)

  // Heavy Octagon Platform Base
  const baseGeo = new THREE.CylinderGeometry(3.6, 3.8, 0.35, 8)
  const baseMat = new THREE.MeshStandardMaterial({
    color: 0x090c14,
    metalness: 0.95,
    roughness: 0.2
  })
  const base = new THREE.Mesh(baseGeo, baseMat)
  base.position.y = -0.18
  fixedStageGroup.add(base)

  // Glowing Outer Laser Perimeter Ring (Fixed)
  const ringGeo = new THREE.TorusGeometry(3.7, 0.04, 16, 64)
  const ringMat = new THREE.MeshBasicMaterial({ color: 0xffcc00 })
  const ring = new THREE.Mesh(ringGeo, ringMat)
  ring.rotation.x = Math.PI / 2
  fixedStageGroup.add(ring)

  // Fixed Tactical Crossbars
  const barGeo = new THREE.BoxGeometry(7.4, 0.02, 0.08)
  const barMat = new THREE.MeshBasicMaterial({ color: 0xff5500, transparent: true, opacity: 0.4 })
  const bar1 = new THREE.Mesh(barGeo, barMat)
  bar1.position.y = 0.01
  fixedStageGroup.add(bar1)

  const bar2 = new THREE.Mesh(barGeo, barMat)
  bar2.position.y = 0.01
  bar2.rotation.y = Math.PI / 2
  fixedStageGroup.add(bar2)

  // ── 5. REVOLVING CAROUSEL RING OF 5 AAA MECHAS ON TOP ──
  revolvingGroup = new THREE.Group()
  scene.add(revolvingGroup)

  const radius = 2.45
  for (let i = 0; i < 5; i++) {
    const angle = (i / 5) * Math.PI * 2
    const mechaObj = buildBadassMecha(i, championColors[i])

    mechaObj.group.position.set(Math.sin(angle) * radius, 0, Math.cos(angle) * radius)
    mechaObj.group.rotation.y = angle

    revolvingGroup.add(mechaObj.group)
    mechaRobots.push(mechaObj)
  }

  updateActiveMaterials(props.activeIndex)

  // 6. Mouse Orbit & Drag
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

  animate()
}

function updateActiveMaterials(activeIdx) {
  mechaRobots.forEach((robot, i) => {
    const isActive = i === activeIdx
    if (isActive) {
      robot.group.scale.set(1.18, 1.18, 1.18)
      robot.group.traverse((child) => {
        if (child.isMesh && child.material) {
          if (child === robot.reactor || child === robot.visor || child.parent === robot.weapon) {
            child.material = robot.glowMat
          } else {
            child.material.wireframe = false
            child.material.opacity = 1.0
            child.material.transparent = false
          }
        }
      })
      if (spotLight) spotLight.color.setHex(championColors[activeIdx].glow)
    } else {
      robot.group.scale.set(0.85, 0.85, 0.85)
      robot.group.traverse((child) => {
        if (child.isMesh && child.material) {
          child.material = robot.wireframeMat
        }
      })
    }
  })
}

function onMouseMove(e) {
  if (isDragging) {
    const deltaX = e.clientX - prevMouseX
    const deltaY = e.clientY - prevMouseY
    manualRotY += deltaX * 0.01
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

  // Smoothly rotate the carousel ring to bring active mecha to front (0 radians)
  targetAngle = -(props.activeIndex / 5) * Math.PI * 2
  currentAngle += (targetAngle - currentAngle) * 0.08

  if (revolvingGroup) {
    revolvingGroup.rotation.y = currentAngle + manualRotY + mouseX * 0.2
  }

  // Active Mecha Combat Idle Pulse
  const activeMecha = mechaRobots[props.activeIndex]
  if (activeMecha) {
    const pulse = 1.0 + Math.sin(time * 4) * 0.18
    if (activeMecha.reactor) activeMecha.reactor.scale.set(pulse, pulse, pulse)

    if (activeMecha.weapon) {
      activeMecha.weapon.position.y = Math.sin(time * 2.5) * 0.05
      activeMecha.weapon.rotation.z = Math.sin(time * 2) * 0.03
    }
  }

  // Camera parallax subtly tracks cursor
  if (camera) {
    camera.position.x = mouseX * 0.4
    camera.position.y = 3.4 - mouseY * 0.3
    camera.lookAt(0, 1.5, 0)
  }

  if (renderer && scene && camera) {
    renderer.render(scene, camera)
  }
}

watch(() => props.activeIndex, (newVal) => {
  updateActiveMaterials(newVal)
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
  <div class="relative flex items-center justify-center select-none">
    <!-- 3D Three.js WebGL Stage Canvas Container with Fixed Pedestal & Badass Mechas -->
    <div
      ref="containerRef"
      class="h-[390px] w-full max-w-[500px] cursor-grab active:cursor-grabbing filter drop-shadow-[0_0_50px_rgba(255,204,0,0.5)]"
      title="Đế bệ cố định • Nhấp giữ chuột để xoay góc nhìn 3D"
    ></div>

    <!-- HUD Central Indicator -->
    <div class="pointer-events-none absolute bottom-1 flex items-center gap-2 font-mono text-[9px] font-black text-amber-400/90 bg-[#07080b]/90 px-3 py-1 border border-amber-500/40 mecha-cut-tr shadow-[0_0_20px_rgba(255,204,0,0.3)]">
      <span class="h-2 w-2 bg-amber-400 animate-ping"></span>
      <span>FIXED PEDESTAL • AAA MECHA WARFRAME STAGE</span>
    </div>
  </div>
</template>
