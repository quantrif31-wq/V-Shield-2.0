<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import * as THREE from 'three'

const canvasContainerRef = ref(null)
let scene, camera, renderer, animationFrameId
let gridHelper, particles, planeMesh
let mouseX = 0, mouseY = 0
let scrollY = 0

function initGlobalThree() {
  const container = canvasContainerRef.value
  if (!container) return

  const width = window.innerWidth
  const height = window.innerHeight

  // 1. Scene & Camera
  scene = new THREE.Scene()
  camera = new THREE.PerspectiveCamera(60, width / height, 0.1, 1000)
  camera.position.set(0, 4, 14)
  camera.lookAt(0, 0, 0)

  // 2. WebGL Renderer with safe try-catch
  try {
    renderer = new THREE.WebGLRenderer({ alpha: true, antialias: true, powerPreference: 'high-performance' })
    renderer.setSize(width, height)
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 1.5))
    container.appendChild(renderer.domElement)
  } catch (_) {
    return
  }

  // 3. Cyber Wireframe Ground Plane
  const planeGeo = new THREE.PlaneGeometry(60, 60, 30, 30)
  const planeMat = new THREE.MeshBasicMaterial({
    color: 0xffcc00,
    wireframe: true,
    transparent: true,
    opacity: 0.08
  })
  planeMesh = new THREE.Mesh(planeGeo, planeMat)
  planeMesh.rotation.x = -Math.PI / 2.3
  planeMesh.position.y = -5
  scene.add(planeMesh)

  // 4. Floating 3D Cyber Particle Constellation
  const pCount = 200
  const pPositions = new Float32Array(pCount * 3)
  for (let i = 0; i < pCount * 3; i += 3) {
    pPositions[i] = (Math.random() - 0.5) * 35
    pPositions[i + 1] = (Math.random() - 0.5) * 20
    pPositions[i + 2] = (Math.random() - 0.5) * 30
  }
  const pGeo = new THREE.BufferGeometry()
  pGeo.setAttribute('position', new THREE.BufferAttribute(pPositions, 3))
  const pMat = new THREE.PointsMaterial({
    color: 0xffcc00,
    size: 0.08,
    transparent: true,
    opacity: 0.45
  })
  particles = new THREE.Points(pGeo, pMat)
  scene.add(particles)

  // 5. Event Listeners
  window.addEventListener('mousemove', onMouseMove, { passive: true })
  window.addEventListener('scroll', onScroll, { passive: true })
  window.addEventListener('resize', onResize)

  animate()
}

function onMouseMove(e) {
  mouseX = (e.clientX / window.innerWidth - 0.5) * 2
  mouseY = (e.clientY / window.innerHeight - 0.5) * 2
}

function onScroll() {
  scrollY = window.scrollY || window.pageYOffset
}

function onResize() {
  if (!canvasContainerRef.value || !renderer || !camera) return
  const width = window.innerWidth
  const height = window.innerHeight
  camera.aspect = width / height
  camera.updateProjectionMatrix()
  renderer.setSize(width, height)
}

function animate() {
  animationFrameId = requestAnimationFrame(animate)

  // Subtle Camera Parallax
  camera.position.x += (mouseX * 1.5 - camera.position.x) * 0.04
  camera.position.y += (4 - mouseY * 1.2 - scrollY * 0.002 - camera.position.y) * 0.04
  camera.lookAt(0, -scrollY * 0.002, 0)

  // Rotate & Wave Plane
  if (planeMesh) {
    planeMesh.position.z = (Date.now() * 0.002) % 2 - 5
  }

  // Float Particles
  if (particles) {
    particles.rotation.y += 0.0006
    particles.rotation.x = mouseX * 0.05
  }

  if (renderer && scene && camera) {
    renderer.render(scene, camera)
  }
}

onMounted(() => {
  initGlobalThree()
})

onUnmounted(() => {
  window.removeEventListener('mousemove', onMouseMove)
  window.removeEventListener('scroll', onScroll)
  window.removeEventListener('resize', onResize)
  if (animationFrameId) cancelAnimationFrame(animationFrameId)
  if (renderer) renderer.dispose()
})
</script>

<template>
  <div
    ref="canvasContainerRef"
    class="pointer-events-none fixed inset-0 z-0 h-full w-full opacity-70 filter drop-shadow-[0_0_20px_rgba(255,204,0,0.15)]"
  ></div>
</template>
