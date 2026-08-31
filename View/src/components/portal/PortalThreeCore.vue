<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import * as THREE from 'three'
import { mechaAudio } from '../../utils/portalAudio'

const containerRef = ref(null)
let scene, camera, renderer, animationFrameId
let coreGroup, innerIcosa, outerRing1, outerRing2, particles, centerSphere
let mouseX = 0, mouseY = 0
let targetRotX = 0, targetRotY = 0
let isHovered = false
let isDragging = false
let prevMouseX = 0, prevMouseY = 0
let manualRotX = 0, manualRotY = 0

function initThree() {
  const container = containerRef.value
  if (!container) return

  const width = container.clientWidth || 360
  const height = container.clientHeight || 360

  // 1. Scene & Camera
  scene = new THREE.Scene()
  camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 1000)
  camera.position.z = 7.5

  // 2. Renderer with safe WebGL detection
  try {
    renderer = new THREE.WebGLRenderer({ alpha: true, antialias: true, powerPreference: 'high-performance' })
    renderer.setSize(width, height)
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2))
    container.appendChild(renderer.domElement)
  } catch (_) {
    return
  }

  coreGroup = new THREE.Group()
  scene.add(coreGroup)

  // 3. Central Wireframe Icosahedron (The Original Beloved Core)
  const icoGeo = new THREE.IcosahedronGeometry(1.6, 1)
  const icoMat = new THREE.MeshBasicMaterial({
    color: 0xffcc00,
    wireframe: true,
    transparent: true,
    opacity: 0.85
  })
  innerIcosa = new THREE.Mesh(icoGeo, icoMat)
  coreGroup.add(innerIcosa)

  // 4. Center Glowing Core Sphere
  const sphereGeo = new THREE.SphereGeometry(0.8, 16, 16)
  const sphereMat = new THREE.MeshBasicMaterial({
    color: 0xff5500,
    wireframe: true,
    transparent: true,
    opacity: 0.55
  })
  centerSphere = new THREE.Mesh(sphereGeo, sphereMat)
  coreGroup.add(centerSphere)

  // 5. Dual Outer Shield Orbit Rings
  const ringGeo1 = new THREE.TorusGeometry(2.4, 0.03, 16, 64)
  const ringMat1 = new THREE.MeshBasicMaterial({
    color: 0xffcc00,
    transparent: true,
    opacity: 0.65
  })
  outerRing1 = new THREE.Mesh(ringGeo1, ringMat1)
  outerRing1.rotation.x = Math.PI / 3
  coreGroup.add(outerRing1)

  const ringGeo2 = new THREE.TorusGeometry(2.8, 0.025, 16, 64)
  const ringMat2 = new THREE.MeshBasicMaterial({
    color: 0x00f0ff,
    transparent: true,
    opacity: 0.45
  })
  outerRing2 = new THREE.Mesh(ringGeo2, ringMat2)
  outerRing2.rotation.y = Math.PI / 4
  coreGroup.add(outerRing2)

  // 6. Floating Particle Nebula
  const particleCount = 200
  const pPositions = new Float32Array(particleCount * 3)
  for (let i = 0; i < particleCount * 3; i += 3) {
    pPositions[i] = (Math.random() - 0.5) * 9
    pPositions[i + 1] = (Math.random() - 0.5) * 9
    pPositions[i + 2] = (Math.random() - 0.5) * 9
  }
  const particleGeo = new THREE.BufferGeometry()
  particleGeo.setAttribute('position', new THREE.BufferAttribute(pPositions, 3))
  const particleMat = new THREE.PointsMaterial({
    color: 0xffcc00,
    size: 0.06,
    transparent: true,
    opacity: 0.75
  })
  particles = new THREE.Points(particleGeo, particleMat)
  scene.add(particles)

  // 7. Event Listeners (360 Drag + Parallax + Hover Speed)
  window.addEventListener('mousemove', onMouseMove, { passive: true })
  window.addEventListener('resize', onResize)

  container.addEventListener('mousedown', (e) => {
    isDragging = true
    prevMouseX = e.clientX
    prevMouseY = e.clientY
    mechaAudio.playClick()
  })

  window.addEventListener('mouseup', () => {
    isDragging = false
  })

  container.addEventListener('mouseenter', () => {
    isHovered = true
    mechaAudio.playHover()
  })
  container.addEventListener('mouseleave', () => {
    isHovered = false
    isDragging = false
  })

  animate()
}

function onMouseMove(e) {
  if (isDragging) {
    const deltaX = e.clientX - prevMouseX
    const deltaY = e.clientY - prevMouseY
    manualRotY += deltaX * 0.015
    manualRotX += deltaY * 0.015
    prevMouseX = e.clientX
    prevMouseY = e.clientY
  }

  if (!containerRef.value) return
  const rect = containerRef.value.getBoundingClientRect()
  const cx = rect.left + rect.width / 2
  const cy = rect.top + rect.height / 2
  mouseX = (e.clientX - cx) / (rect.width / 2)
  mouseY = (e.clientY - cy) / (rect.height / 2)
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

  const speedMult = isHovered ? 2.8 : 1.0

  targetRotY += 0.008 * speedMult
  coreGroup.rotation.y = targetRotY + manualRotY + mouseX * 0.4
  coreGroup.rotation.x = manualRotX + mouseY * 0.3

  if (innerIcosa) {
    innerIcosa.rotation.x += 0.006 * speedMult
    innerIcosa.rotation.z += 0.004 * speedMult
  }
  if (centerSphere) {
    centerSphere.rotation.y -= 0.008 * speedMult
  }
  if (outerRing1) {
    outerRing1.rotation.z -= 0.012 * speedMult
  }
  if (outerRing2) {
    outerRing2.rotation.x += 0.01 * speedMult
  }
  if (particles) {
    particles.rotation.y -= 0.003 * speedMult
  }

  if (renderer && scene && camera) {
    renderer.render(scene, camera)
  }
}

onMounted(() => {
  initThree()
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
    <!-- Three.js Canvas Container with Original Icosahedron & Orbit Rings -->
    <div
      ref="containerRef"
      class="h-[300px] w-[300px] sm:h-[350px] sm:w-[350px] cursor-grab active:cursor-grabbing filter drop-shadow-[0_0_40px_rgba(255,204,0,0.5)] transition-transform duration-300 hover:scale-105"
      title="Kéo chuột để xoay 3D 360 độ tự do"
    ></div>

    <!-- Central Overlay Badge -->
    <div class="pointer-events-none absolute bottom-1 flex items-center gap-2 font-mono text-[9px] font-black text-amber-400/90 bg-[#07080b]/90 px-2.5 py-0.5 border border-amber-500/40 mecha-cut-tr shadow-[0_0_20px_rgba(255,204,0,0.4)]">
      <span class="h-1.5 w-1.5 bg-amber-400 animate-ping"></span>
      <span>V-SHIELD 3D // QUANTUM MATRIX CORE</span>
    </div>
  </div>
</template>
