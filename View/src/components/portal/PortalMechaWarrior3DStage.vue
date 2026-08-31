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
let stageGroup, platformMesh, floorGrid
const mechaRobots = []
let targetAngle = 0
let currentAngle = 0
let isDragging = false
let prevMouseX = 0, prevMouseY = 0
let mouseX = 0, mouseY = 0
let manualRotY = 0, manualRotX = 0

// Colors for the 5 Champions
const championColors = [
  0xffcc00, // 0: Amber (Thành)
  0xff5500, // 1: Flame Orange (Hùng)
  0x00f0ff, // 2: Cyan (Hoài Anh)
  0xa855f7, // 3: Purple (Đạt)
  0x10b981  // 4: Emerald (Việt)
]

function createMechaRobot(index, primaryColor) {
  const robotGroup = new THREE.Group()

  // 1. Materials
  const armorMat = new THREE.MeshStandardMaterial({
    color: primaryColor,
    metalness: 0.8,
    roughness: 0.2,
    wireframe: false
  })
  const darkMetalMat = new THREE.MeshStandardMaterial({
    color: 0x181e29,
    metalness: 0.9,
    roughness: 0.3
  })
  const glowMat = new THREE.MeshBasicMaterial({
    color: primaryColor,
    wireframe: false
  })
  const wireframeMat = new THREE.MeshBasicMaterial({
    color: 0x334155,
    wireframe: true,
    transparent: true,
    opacity: 0.25
  })

  // 2. Torso (Body Chest)
  const chestGeo = new THREE.BoxGeometry(0.9, 1.1, 0.6)
  const chestMesh = new THREE.Mesh(chestGeo, armorMat)
  chestMesh.position.y = 1.6
  robotGroup.add(chestMesh)

  // 3. Glowing Core Reactor (Center Chest)
  const reactorGeo = new THREE.SphereGeometry(0.22, 16, 16)
  const reactorMesh = new THREE.Mesh(reactorGeo, glowMat)
  reactorMesh.position.set(0, 1.65, 0.32)
  robotGroup.add(reactorMesh)

  // 4. Head & Optical Sensor Visor
  const headGeo = new THREE.BoxGeometry(0.45, 0.45, 0.45)
  const headMesh = new THREE.Mesh(headGeo, darkMetalMat)
  headMesh.position.y = 2.45
  robotGroup.add(headMesh)

  const visorGeo = new THREE.BoxGeometry(0.38, 0.12, 0.1)
  const visorMesh = new THREE.Mesh(visorGeo, glowMat)
  visorMesh.position.set(0, 2.45, 0.22)
  robotGroup.add(visorMesh)

  // 5. Shoulder Armor (Pauldrons)
  const leftShoulderGeo = new THREE.ConeGeometry(0.35, 0.6, 4)
  const leftShoulder = new THREE.Mesh(leftShoulderGeo, armorMat)
  leftShoulder.position.set(-0.75, 2.0, 0)
  leftShoulder.rotation.z = Math.PI / 4
  robotGroup.add(leftShoulder)

  const rightShoulderGeo = new THREE.ConeGeometry(0.35, 0.6, 4)
  const rightShoulder = new THREE.Mesh(rightShoulderGeo, armorMat)
  rightShoulder.position.set(0.75, 2.0, 0)
  rightShoulder.rotation.z = -Math.PI / 4
  robotGroup.add(rightShoulder)

  // 6. Arms & Legs
  const limbMat = darkMetalMat
  // Left Leg
  const lLegGeo = new THREE.CylinderGeometry(0.12, 0.15, 1.3, 8)
  const lLeg = new THREE.Mesh(lLegGeo, limbMat)
  lLeg.position.set(-0.32, 0.65, 0)
  robotGroup.add(lLeg)

  // Right Leg
  const rLeg = new THREE.Mesh(lLegGeo, limbMat)
  rLeg.position.set(0.32, 0.65, 0)
  robotGroup.add(rLeg)

  // Left Arm
  const armGeo = new THREE.CylinderGeometry(0.1, 0.12, 1.0, 8)
  const lArm = new THREE.Mesh(armGeo, limbMat)
  lArm.position.set(-0.65, 1.4, 0.2)
  lArm.rotation.x = Math.PI / 6
  robotGroup.add(lArm)

  // Right Arm (Holding Weapon)
  const rArm = new THREE.Mesh(armGeo, limbMat)
  rArm.position.set(0.65, 1.4, 0.2)
  rArm.rotation.x = -Math.PI / 8
  robotGroup.add(rArm)

  // 7. Back Thruster Booster Wings
  const wingGeo = new THREE.BoxGeometry(0.2, 0.8, 0.08)
  const lWing = new THREE.Mesh(wingGeo, armorMat)
  lWing.position.set(-0.45, 1.8, -0.35)
  lWing.rotation.z = -Math.PI / 6
  robotGroup.add(lWing)

  const rWing = new THREE.Mesh(wingGeo, armorMat)
  rWing.position.set(0.45, 1.8, -0.35)
  rWing.rotation.z = Math.PI / 6
  robotGroup.add(rWing)

  // 8. Signature 3D Weapon (Distinct for each champion)
  let weaponGroup = new THREE.Group()

  if (index === 0) {
    // Phạm Văn Thành: 3D Quantum Broadsword
    const bladeGeo = new THREE.BoxGeometry(0.18, 1.9, 0.06)
    const blade = new THREE.Mesh(bladeGeo, glowMat)
    blade.position.set(0.85, 2.0, 0.4)
    blade.rotation.z = Math.PI / 12
    weaponGroup.add(blade)
  } else if (index === 1) {
    // Hà Mạnh Hùng: 3D Plasma Railgun
    const barrelGeo = new THREE.CylinderGeometry(0.08, 0.12, 1.8, 12)
    const barrel = new THREE.Mesh(barrelGeo, darkMetalMat)
    barrel.position.set(0.8, 1.6, 0.8)
    barrel.rotation.x = Math.PI / 2
    weaponGroup.add(barrel)

    const ringGeo = new THREE.TorusGeometry(0.2, 0.03, 8, 16)
    const ring = new THREE.Mesh(ringGeo, glowMat)
    ring.position.set(0.8, 1.6, 1.2)
    weaponGroup.add(ring)
  } else if (index === 2) {
    // Phạm Ngọc Hoài Anh: 3D Heavy Shoulder Cannon
    const cannonGeo = new THREE.CylinderGeometry(0.18, 0.22, 1.5, 12)
    const cannon = new THREE.Mesh(cannonGeo, darkMetalMat)
    cannon.position.set(-0.6, 2.3, 0.4)
    cannon.rotation.x = Math.PI / 2
    weaponGroup.add(cannon)
  } else if (index === 3) {
    // Vũ Tiến Đạt: 3D Dual Energy Daggers
    const daggerGeo = new THREE.ConeGeometry(0.12, 1.0, 4)
    const d1 = new THREE.Mesh(daggerGeo, glowMat)
    d1.position.set(-0.75, 1.2, 0.5)
    d1.rotation.x = Math.PI / 3
    weaponGroup.add(d1)

    const d2 = new THREE.Mesh(daggerGeo, glowMat)
    d2.position.set(0.75, 1.2, 0.5)
    d2.rotation.x = -Math.PI / 3
    weaponGroup.add(d2)
  } else if (index === 4) {
    // Nguyễn Quốc Việt: 3D Thunder Halberd
    const shaftGeo = new THREE.CylinderGeometry(0.05, 0.05, 2.4, 8)
    const shaft = new THREE.Mesh(shaftGeo, darkMetalMat)
    shaft.position.set(0.85, 1.8, 0.3)
    weaponGroup.add(shaft)

    const tipGeo = new THREE.ConeGeometry(0.22, 0.7, 4)
    const tip = new THREE.Mesh(tipGeo, glowMat)
    tip.position.set(0.85, 3.1, 0.3)
    weaponGroup.add(tip)
  }
  robotGroup.add(weaponGroup)

  return {
    group: robotGroup,
    chest: chestMesh,
    reactor: reactorMesh,
    visor: visorMesh,
    weapon: weaponGroup,
    armorMat,
    darkMetalMat,
    glowMat,
    wireframeMat,
    color: primaryColor
  }
}

function initStage() {
  const container = containerRef.value
  if (!container) return

  const width = container.clientWidth || 480
  const height = container.clientHeight || 420

  // 1. Scene & Camera
  scene = new THREE.Scene()
  camera = new THREE.PerspectiveCamera(40, width / height, 0.1, 1000)
  camera.position.set(0, 3.2, 9.8)
  camera.lookAt(0, 1.4, 0)

  // 2. Lights
  const ambientLight = new THREE.AmbientLight(0xffffff, 0.8)
  scene.add(ambientLight)

  const dirLight = new THREE.DirectionalLight(0xffffff, 1.5)
  dirLight.position.set(5, 10, 7)
  scene.add(dirLight)

  const pointLight = new THREE.PointLight(0xffcc00, 2.0, 15)
  pointLight.position.set(0, 2, 0)
  scene.add(pointLight)

  // 3. Renderer with safe try-catch
  try {
    renderer = new THREE.WebGLRenderer({ alpha: true, antialias: true, powerPreference: 'high-performance' })
    renderer.setSize(width, height)
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2))
    container.appendChild(renderer.domElement)
  } catch (_) {
    return
  }

  stageGroup = new THREE.Group()
  scene.add(stageGroup)

  // 4. Circular 3D Pedestal Stage
  const platGeo = new THREE.CylinderGeometry(3.5, 3.7, 0.35, 32)
  const platMat = new THREE.MeshStandardMaterial({
    color: 0x0a0e17,
    metalness: 0.9,
    roughness: 0.1
  })
  platformMesh = new THREE.Mesh(platGeo, platMat)
  platformMesh.position.y = -0.18
  stageGroup.add(platformMesh)

  // Glowing Outer Rim
  const rimGeo = new THREE.TorusGeometry(3.6, 0.04, 16, 64)
  const rimMat = new THREE.MeshBasicMaterial({ color: 0xffcc00 })
  const rim = new THREE.Mesh(rimGeo, rimMat)
  rim.rotation.x = Math.PI / 2
  stageGroup.add(rim)

  // 5. Build and Position 5 Mecha Champions
  const radius = 2.4
  for (let i = 0; i < 5; i++) {
    const angle = (i / 5) * Math.PI * 2
    const mecha = createMechaRobot(i, championColors[i])

    // Position on circular ring
    mecha.group.position.x = Math.sin(angle) * radius
    mecha.group.position.z = Math.cos(angle) * radius

    // Face outward from center
    mecha.group.rotation.y = angle

    stageGroup.add(mecha.group)
    mechaRobots.push(mecha)
  }

  // 6. Interactive Event Listeners
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

  updateActiveMaterials(props.activeIndex)
  animate()
}

function updateActiveMaterials(activeIdx) {
  mechaRobots.forEach((robot, i) => {
    const isActive = i === activeIdx
    if (isActive) {
      robot.chest.material = robot.armorMat
      robot.reactor.material = robot.glowMat
      robot.visor.material = robot.glowMat
      robot.group.scale.set(1.15, 1.15, 1.15)
    } else {
      robot.chest.material = robot.wireframeMat
      robot.reactor.material = robot.wireframeMat
      robot.visor.material = robot.wireframeMat
      robot.group.scale.set(0.85, 0.85, 0.85)
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

  // Smoothly rotate the stage so active champion faces front (0 radians)
  targetAngle = -(props.activeIndex / 5) * Math.PI * 2
  currentAngle += (targetAngle - currentAngle) * 0.08

  if (stageGroup) {
    stageGroup.rotation.y = currentAngle + manualRotY + mouseX * 0.2
    stageGroup.rotation.x = manualRotX + mouseY * 0.15
  }

  // Active Champion Combat Idle Animations
  mechaRobots.forEach((robot, i) => {
    if (i === props.activeIndex) {
      // Reactor pulsating breath
      const pulse = 1.0 + Math.sin(time * 3) * 0.2
      robot.reactor.scale.set(pulse, pulse, pulse)

      // Weapon oscillation / charge
      if (robot.weapon) {
        robot.weapon.position.y = Math.sin(time * 2) * 0.06
        robot.weapon.rotation.z = Math.sin(time * 1.5) * 0.04
      }
    }
  })

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
    <!-- 3D Three.js WebGL Stage Canvas Container -->
    <div
      ref="containerRef"
      class="h-[380px] w-full max-w-[500px] cursor-grab active:cursor-grabbing filter drop-shadow-[0_0_50px_rgba(255,204,0,0.45)]"
      title="Nhấp giữ chuột để xoay góc nhìn 3D quanh 5 Chiến Binh"
    ></div>

    <!-- HUD Central Indicator -->
    <div class="pointer-events-none absolute bottom-1 flex items-center gap-2 font-mono text-[9px] font-black text-amber-400/90 bg-[#07080b]/90 px-3 py-1 border border-amber-500/40 mecha-cut-tr shadow-[0_0_20px_rgba(255,204,0,0.3)]">
      <span class="h-2 w-2 bg-amber-400 animate-ping"></span>
      <span>3D ANIMATED MECHA CHAMPIONS // ROTATING VIEWPORT</span>
    </div>
  </div>
</template>
