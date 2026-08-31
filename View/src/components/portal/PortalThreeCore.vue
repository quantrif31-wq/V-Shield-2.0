<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import * as THREE from 'three'
import { mechaAudio } from '../../utils/portalAudio'

const containerRef = ref(null)
let scene, camera, renderer, animationFrameId
let coreGroup, innerIcosa, outerRing1, outerRing2, particles, coreLight
let mouseX = 0, mouseY = 0
let targetRotX = 0, targetRotY = 0
let isHovered = false

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

  // 3. Inner Icosahedron (Tactical Core)
  const icoGeo = new THREE.IcosahedronGeometry(1.6, 1)
  const icoMat = new THREE.MeshBasicMaterial({
    color: 0xffcc00,
    wireframe: true,
    transparent: true,
    opacity: 0.85
  })
  innerIcosa = new THREE.Mesh(icoGeo, icoMat)
  coreGroup.add(innerIcosa)

  // 4. Center Glowing Sphere
  const coreSphereGeo = new THREE.SphereGeometry(0.85, 16, 16)
  const coreSphereMat = new THREE.MeshBasicMaterial({
    color: 0xff5500,
    wireframe: true,
    transparent: true,
    opacity: 0.5
  })
  const coreSphere = new THREE.Mesh(coreSphereGeo, coreSphereMat)
  coreGroup.add(coreSphere)

  // 5. Outer Shield Orbit Rings
  const ringGeo1 = new THREE.TorusGeometry(2.4, 0.035, 16, 64)
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
  const particleCount = 180
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

  // 7. Mouse Orbit Listeners
  window.addEventListener('mousemove', onMouseMove, { passive: true })
  window.addEventListener('resize', onResize)

  container.addEventListener('mouseenter', () => {
    isHovered = true
    mechaAudio.playHover()
  })
  container.addEventListener('mouseleave', () => {
    isHovered = false
  })

  animate()
}

function onMouseMove(e) {
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

  // Speed multiplier on hover
  const speedMult = isHovered ? 3.2 : 1.0

  targetRotY += 0.008 * speedMult
  coreGroup.rotation.y = targetRotY + mouseX * 0.4
  coreGroup.rotation.x = mouseY * 0.3

  if (innerIcosa) {
    innerIcosa.rotation.x += 0.006 * speedMult
    innerIcosa.rotation.z += 0.004 * speedMult
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
  <div class="relative flex items-center justify-center">
    <!-- Three.js Canvas Container with Overdrive Halo -->
    <div
      ref="containerRef"
      class="h-[300px] w-[300px] sm:h-[350px] sm:w-[350px] cursor-grab active:cursor-grabbing filter drop-shadow-[0_0_40px_rgba(255,204,0,0.5)] transition-transform duration-300 hover:scale-105"
    ></div>

    <!-- Central Overlay Badge -->
    <div class="pointer-events-none absolute bottom-1 flex items-center gap-2 font-mono text-[9px] font-black text-amber-400/90 bg-[#07080b]/90 px-2.5 py-0.5 border border-amber-500/40 mecha-cut-tr shadow-[0_0_20px_rgba(255,204,0,0.4)]">
      <span class="h-1.5 w-1.5 bg-orange-500 animate-ping"></span>
      <span>3D CORE // OVERDRIVE ENGAGED</span>
    </div>
  </div>
</template>
