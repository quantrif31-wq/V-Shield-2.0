<script setup>
import { ref, onMounted, onUnmounted } from 'vue'

const canvasRef = ref(null)
let animationId = null
let sparks = []
let width = 0
let height = 0

class LaserSpark {
  constructor(x, y) {
    this.x = x
    this.y = y
    const angle = Math.random() * Math.PI * 2
    const speed = Math.random() * 8 + 2
    this.vx = Math.cos(angle) * speed
    this.vy = Math.sin(angle) * speed
    this.gravity = 0.18
    this.friction = 0.94
    this.size = Math.random() * 2.5 + 1.2
    this.life = 1.0
    this.decay = Math.random() * 0.035 + 0.02
    
    // Electric Overdrive Colors: Amber, Electric Orange, Laser Cyan, Crimson
    const colors = ['#ffcc00', '#ff5500', '#ff0055', '#00f0ff', '#ffffff']
    this.color = colors[Math.floor(Math.random() * colors.length)]
  }

  update() {
    this.vx *= this.friction
    this.vy *= this.friction
    this.vy += this.gravity
    this.x += this.vx
    this.y += this.vy
    this.life -= this.decay
  }

  draw(ctx) {
    if (this.life <= 0) return
    ctx.save()
    ctx.globalAlpha = this.life
    ctx.strokeStyle = this.color
    ctx.lineWidth = this.size
    ctx.shadowColor = this.color
    ctx.shadowBlur = 10

    ctx.beginPath()
    ctx.moveTo(this.x, this.y)
    ctx.lineTo(this.x - this.vx * 2.5, this.y - this.vy * 2.5)
    ctx.stroke()
    ctx.restore()
  }
}

function spawnSparks(x, y, count = 22) {
  for (let i = 0; i < count; i++) {
    sparks.push(new LaserSpark(x, y))
  }
}

function handleClick(e) {
  spawnSparks(e.clientX, e.clientY, 28)
}

function handleMouseMove(e) {
  if (Math.random() < 0.22) {
    spawnSparks(e.clientX, e.clientY, 3)
  }
}

function render() {
  const canvas = canvasRef.value
  if (!canvas) return
  const ctx = canvas.getContext ? canvas.getContext('2d') : null
  if (!ctx) return

  ctx.clearRect(0, 0, width, height)

  for (let i = sparks.length - 1; i >= 0; i--) {
    sparks[i].update()
    sparks[i].draw(ctx)
    if (sparks[i].life <= 0) {
      sparks.splice(i, 1)
    }
  }

  animationId = requestAnimationFrame(render)
}

onMounted(() => {
  const canvas = canvasRef.value
  if (canvas) {
    width = canvas.width = window.innerWidth
    height = canvas.height = window.innerHeight

    window.addEventListener('resize', () => {
      width = canvas.width = window.innerWidth
      height = canvas.height = window.innerHeight
    })
    window.addEventListener('click', handleClick)
    window.addEventListener('mousemove', handleMouseMove, { passive: true })
    render()
  }
})

onUnmounted(() => {
  window.removeEventListener('click', handleClick)
  window.removeEventListener('mousemove', handleMouseMove)
  if (animationId) cancelAnimationFrame(animationId)
})
</script>

<template>
  <canvas
    ref="canvasRef"
    class="pointer-events-none fixed inset-0 z-[9997] h-full w-full"
  ></canvas>
</template>
