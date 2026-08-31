<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import * as THREE from 'three'
import { mechaAudio } from '../../utils/portalAudio'

const containerRef = ref(null)
let scene, camera, renderer, animationFrameId
let coreGroup, torusKnot, innerDodeca, outerRing1, outerRing2, particles
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

  // 3. Central Quantum Torus Knot
  const knotGeo = new THREE.TorusKnotGeometry(1.1, 0.22, 64, 16)
  const knotMat = new THREE.MeshBasicMaterial({
    color: 0xffcc00,
    wireframe: true,
    transparent: true,
    opacity: 0.85
  })
  torusKnot = new THREE.Mesh(knotGeo, knotMat)
  coreGroup.add(torusKnot)

  // 4. Inner Dodecahedron
  const dodecaGeo = new THREE.DodecahedronGeometry(0.7, 0)
  const dodecaMat = new THREE.MeshBasicMaterial({
    color: 0xff5500,
    wireframe: true,
    transparent: true,
    opacity: 0.6
  })
  innerDodeca = new THREE.Mesh(dodecaGeo, dodecaMat)
  coreGroup.add(innerDodeca)

  // 5. Outer Shield Orbit Rings
  const ringGeo1 = new THREE.TorusGeometry(2.5, 0.035, 16, 64)
  const ringMat1 = new THREE.MeshBasicMaterial({
    color: 0xffcc00,
    transparent: true,
    opacity: 0.7
  })
  outerRing1 = new THREE.Mesh(ringGeo1, ringMat1)
  outerRing1.rotation.x = Math.PI / 3
  coreGroup.add(outerRing1)

  const ringGeo2 = new THREE.TorusGeometry(2.9, 0.025, 16, 64)
  const ringMat2 = new THREE.MeshBasicMaterial({
    color: 0x00f0ff,
    transparent: true,
    opacity: 0.5
  })
  outerRing2 = new THREE.Mesh(ringGeo2, ringMat2)
  outerRing2.rotation.y = Math.PI / 4
  coreGroup.add(outerRing2)

  // 6. Floating Quantum Particle Nebula
  const particleCount = 240
  const pPositions = new Float32Array(particleCount * 3)
  for (let i = 0; i < particleCount * 3; i += 3) {
    pPositions[i] = (Math.random() - 0.5) * 10
    pPositions[i + 1] = (Math.random() - 0.5) * 10
    pPositions[i + 2] = (Math.random() - 0.5) * 10
  }
  const particleGeo = new THREE.BufferGeometry()
  particleGeo.setAttribute('position', new THREE.BufferAttribute(pPositions, 3))
  const particleMat = new THREE.PointsMaterial({
    color: 0xffcc00,
    size: 0.07,
    transparent: true,
    opacity: 0.8
  })
  particles = new THREE.Points(particleGeo, particleMat)
  scene.add(particles)

  // 7. Interactive Event Listeners
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

  const speedMult = isHovered ? 3.2 : 1.0

  targetRotY += 0.008 * speedMult
  coreGroup.rotation.y = targetRotY + manualRotY + mouseX * 0.4
  coreGroup.rotation.x = manualRotX + mouseY * 0.3

  if (torusKnot) {
    torusKnot.rotation.x += 0.008 * speedMult
    torusKnot.rotation.y += 0.012 * speedMult
  }
  if (innerDodeca) {
    innerDodeca.rotation.x -= 0.01 * speedMult
    innerDodeca.rotation.z += 0.008 * speedMult
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
    <!-- Three.js Canvas Container with 360° Drag & Glow -->
    <div
      ref="containerRef"
      class="h-[310px] w-[310px] sm:h-[360px] sm:w-[360px] cursor-grab active:cursor-grabbing filter drop-shadow-[0_0_45px_rgba(255,204,0,0.55)] transition-transform duration-300 hover:scale-105"
      title="Kéo chuột để xoay 3D 360 độ tự do"
    ></div>

    <!-- Central Overlay Badge -->
    <div class="pointer-events-none absolute bottom-1 flex items-center gap-2 font-mono text-[9px] font-black text-amber-400/90 bg-[#07080b]/90 px-2.5 py-0.5 border border-amber-500/40 mecha-cut-tr shadow-[0_0_20px_rgba(255,204,0,0.4)]">
      <span class="h-1.5 w-1.5 bg-orange-500 animate-ping"></span>
      <span>QUANTUM DEFENSE CORE // 360° DRAG 3D</span>
    </div>
  </div>
</template>
