<script setup>
import { ref, onMounted, onUnmounted } from 'vue'

const canvasRef = ref(null)
let animationFrameId = null
let particles = []
let width = 0
let height = 0
let mouse = { x: null, y: null, radius: 160 }

class MechaParticle {
  constructor(w, h) {
    this.x = Math.random() * w
    this.y = Math.random() * h
    this.size = Math.random() * 2.4 + 0.6
    this.baseX = this.x
    this.baseY = this.y
    this.density = Math.random() * 20 + 5
    this.vx = (Math.random() - 0.5) * 0.5
    this.vy = (Math.random() - 0.5) * 0.5
    
    // Mecha BREAK colors: Hazard Gold, Thermal Orange, Laser Cyan, Crimson
    const colors = [
      'rgba(255, 204, 0, ',
      'rgba(255, 85, 0, ',
      'rgba(255, 42, 68, ',
      'rgba(0, 240, 255, ',
      'rgba(255, 220, 100, '
    ]
    this.colorBase = colors[Math.floor(Math.random() * colors.length)]
    this.alpha = Math.random() * 0.6 + 0.15
    this.pulseSpeed = Math.random() * 0.02 + 0.005
  }

  update(w, h) {
    this.x += this.vx
    this.y += this.vy

    if (this.x < 0 || this.x > w) this.vx = -this.vx
    if (this.y < 0 || this.y > h) this.vy = -this.vy

    this.alpha += Math.sin(Date.now() * this.pulseSpeed) * 0.01
    this.alpha = Math.max(0.12, Math.min(0.85, this.alpha))

    // Mouse interaction
    if (mouse.x !== null && mouse.y !== null) {
      const dx = mouse.x - this.x
      const dy = mouse.y - this.y
      const distance = Math.sqrt(dx * dx + dy * dy)
      if (distance < mouse.radius) {
        const forceDirectionX = dx / distance
        const forceDirectionY = dy / distance
        const maxDistance = mouse.radius
        const force = (maxDistance - distance) / maxDistance
        const directionX = forceDirectionX * force * this.density
        const directionY = forceDirectionY * force * this.density
        this.x -= directionX * 0.4
        this.y -= directionY * 0.4
      }
    }
  }

  draw(ctx) {
    ctx.beginPath()
    ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2)
    ctx.fillStyle = this.colorBase + this.alpha + ')'
    ctx.shadowBlur = this.size > 1.8 ? 10 : 4
    ctx.shadowColor = this.colorBase + '0.9)'
    ctx.fill()
    ctx.shadowBlur = 0
  }
}

function initCanvas() {
  const canvas = canvasRef.value
  if (!canvas) return
  const ctx = canvas.getContext('2d')
  
  const handleResize = () => {
    width = window.innerWidth
    height = window.innerHeight
    canvas.width = width
    canvas.height = height
    createParticles()
  }

  const createParticles = () => {
    particles = []
    const count = Math.floor((width * height) / 14000)
    const particleCount = Math.min(Math.max(count, 40), 120)
    for (let i = 0; i < particleCount; i++) {
      particles.push(new MechaParticle(width, height))
    }
  }

  const connectParticles = () => {
    for (let a = 0; a < particles.length; a++) {
      for (let b = a + 1; b < particles.length; b++) {
        const dx = particles[a].x - particles[b].x
        const dy = particles[a].y - particles[b].y
        const dist = Math.sqrt(dx * dx + dy * dy)
        if (dist < 115) {
          const alpha = (1 - dist / 115) * 0.18
          ctx.beginPath()
          ctx.strokeStyle = `rgba(255, 204, 0, ${alpha})`
          ctx.lineWidth = 0.65
          ctx.moveTo(particles[a].x, particles[a].y)
          ctx.lineTo(particles[b].x, particles[b].y)
          ctx.stroke()
        }
      }
    }
  }

  const animate = () => {
    ctx.clearRect(0, 0, width, height)
    
    for (let i = 0; i < particles.length; i++) {
      particles[i].update(width, height)
      particles[i].draw(ctx)
    }
    connectParticles()

    animationFrameId = requestAnimationFrame(animate)
  }

  window.addEventListener('resize', handleResize)
  window.addEventListener('mousemove', (e) => {
    mouse.x = e.clientX
    mouse.y = e.clientY
  })
  window.addEventListener('mouseleave', () => {
    mouse.x = null
    mouse.y = null
  })

  handleResize()
  animate()
}

onMounted(() => {
  initCanvas()
})

onUnmounted(() => {
  if (animationFrameId) {
    cancelAnimationFrame(animationFrameId)
  }
})
</script>

<template>
  <canvas
    ref="canvasRef"
    class="pointer-events-none fixed inset-0 z-0 h-full w-full opacity-60"
  ></canvas>
</template>

<style scoped>
canvas {
  filter: drop-shadow(0 0 10px rgba(255, 204, 0, 0.25));
}
</style>
