<script setup>
import { ref, onMounted, onUnmounted } from 'vue'

const canvasRef = ref(null)
let animationId = null
let width = 360
let height = 360
let rotX = 0.4
let rotY = 0
let mouseTiltX = 0
let mouseTiltY = 0

// Generate 3D Spherical/Polyhedral Nodes
const NODES_COUNT = 38
const nodes = []

for (let i = 0; i < NODES_COUNT; i++) {
  const phi = Math.acos(-1 + (2 * i) / NODES_COUNT)
  const theta = Math.sqrt(NODES_COUNT * Math.PI) * phi
  const radius = 120
  nodes.push({
    x: radius * Math.cos(theta) * Math.sin(phi),
    y: radius * Math.sin(theta) * Math.sin(phi),
    z: radius * Math.cos(phi),
    pulse: Math.random() * Math.PI
  })
}

function handleMouseMove(e) {
  const rect = canvasRef.value?.getBoundingClientRect()
  if (!rect) return
  const cx = rect.left + rect.width / 2
  const cy = rect.top + rect.height / 2
  mouseTiltX = (e.clientY - cy) * 0.0006
  mouseTiltY = (e.clientX - cx) * 0.0006
}

function render() {
  const canvas = canvasRef.value
  if (!canvas) return
  const ctx = canvas.getContext ? canvas.getContext('2d') : null
  if (!ctx) return

  ctx.clearRect(0, 0, width, height)

  rotY += 0.012 + mouseTiltY * 0.5
  rotX += mouseTiltX * 0.2

  const cx = width / 2
  const cy = height / 2
  const fov = 300

  // 1. Draw outer orbit rings
  ctx.save()
  ctx.translate(cx, cy)
  ctx.beginPath()
  ctx.arc(0, 0, 138, 0, Math.PI * 2)
  ctx.strokeStyle = 'rgba(255, 204, 0, 0.12)'
  ctx.lineWidth = 1
  ctx.setLineDash([4, 8])
  ctx.stroke()
  ctx.setLineDash([])

  ctx.beginPath()
  ctx.arc(0, 0, 155, 0, Math.PI * 2)
  ctx.strokeStyle = 'rgba(255, 85, 0, 0.08)'
  ctx.lineWidth = 1
  ctx.stroke()
  ctx.restore()

  // 2. Project 3D nodes to 2D
  const projected = nodes.map(n => {
    // Rotation around Y
    const cosY = Math.cos(rotY)
    const sinY = Math.sin(rotY)
    const x1 = n.x * cosY - n.z * sinY
    const z1 = n.z * cosY + n.x * sinY

    // Rotation around X
    const cosX = Math.cos(rotX)
    const sinX = Math.sin(rotX)
    const y2 = n.y * cosX - z1 * sinX
    const z2 = z1 * cosX + n.y * sinX

    const scale = fov / (fov + z2 + 180)
    const px = x1 * scale + cx
    const py = y2 * scale + cy

    return { px, py, z: z2, scale, pulse: n.pulse }
  })

  // Sort by depth
  projected.sort((a, b) => a.z - b.z)

  // 3. Draw connecting laser lines
  for (let i = 0; i < projected.length; i++) {
    for (let j = i + 1; j < projected.length; j++) {
      const dx = projected[i].px - projected[j].px
      const dy = projected[i].py - projected[j].py
      const dist = Math.sqrt(dx * dx + dy * dy)
      if (dist < 64) {
        const alpha = (1 - dist / 64) * 0.35 * Math.min(projected[i].scale, projected[j].scale)
        ctx.beginPath()
        ctx.strokeStyle = `rgba(255, 204, 0, ${alpha})`
        ctx.lineWidth = 0.8
        ctx.moveTo(projected[i].px, projected[i].py)
        ctx.lineTo(projected[j].px, projected[j].py)
        ctx.stroke()
      }
    }
  }

  // 4. Draw glowing nodes
  const now = Date.now() * 0.003
  projected.forEach(p => {
    const size = Math.max(1.2, 2.5 * p.scale)
    const glow = Math.sin(now + p.pulse) * 0.3 + 0.7
    ctx.beginPath()
    ctx.arc(p.px, p.py, size, 0, Math.PI * 2)
    ctx.fillStyle = `rgba(255, 204, 0, ${0.4 * p.scale * glow})`
    ctx.shadowColor = '#ffcc00'
    ctx.shadowBlur = 8
    ctx.fill()
    ctx.shadowBlur = 0
  })

  // 5. Draw center defense core pulse
  ctx.save()
  ctx.translate(cx, cy)
  ctx.beginPath()
  ctx.arc(0, 0, 12, 0, Math.PI * 2)
  ctx.fillStyle = 'rgba(255, 85, 0, 0.35)'
  ctx.shadowColor = '#ff5500'
  ctx.shadowBlur = 15
  ctx.fill()

  ctx.beginPath()
  ctx.arc(0, 0, 5, 0, Math.PI * 2)
  ctx.fillStyle = '#ffcc00'
  ctx.fill()
  ctx.restore()

  animationId = requestAnimationFrame(render)
}

onMounted(() => {
  const canvas = canvasRef.value
  if (canvas) {
    width = canvas.width = 360
    height = canvas.height = 360
    window.addEventListener('mousemove', handleMouseMove, { passive: true })
    render()
  }
})

onUnmounted(() => {
  window.removeEventListener('mousemove', handleMouseMove)
  if (animationId) cancelAnimationFrame(animationId)
})
</script>

<template>
  <div class="relative flex items-center justify-center">
    <canvas
      ref="canvasRef"
      class="h-[320px] w-[320px] sm:h-[360px] sm:w-[360px] cursor-crosshair filter drop-shadow-[0_0_25px_rgba(255,204,0,0.35)]"
    ></canvas>
    
    <!-- Central HUD Overlay Metrics -->
    <div class="pointer-events-none absolute bottom-2 flex items-center gap-3 font-mono text-[9px] font-bold text-amber-400/80 bg-[#07080b]/80 px-2 py-0.5 border border-amber-500/30 mecha-cut-tr">
      <span>3D DEFENSE MATRIX // ACTIVE</span>
      <span class="text-orange-400">● 60 FPS</span>
    </div>
  </div>
</template>
