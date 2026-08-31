<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { mechaAudio } from '../../utils/portalAudio'

const cursorX = ref(-100)
const cursorY = ref(-100)
const targetX = ref(-100)
const targetY = ref(-100)
const isLocked = ref(false)
const lockLabel = ref('')
const isVisible = ref(false)
let rafId = null

function updatePosition(e) {
  isVisible.value = true
  targetX.value = e.clientX
  targetY.value = e.clientY

  // Check if hovering interactive target
  const target = e.target.closest('button, a, input, select, textarea, .mecha-hud-bracket, .cursor-pointer')
  if (target) {
    if (!isLocked.value) {
      mechaAudio.playTargetLock()
      isLocked.value = true
    }
    const txt = target.innerText || target.getAttribute('aria-label') || 'ENGAGED'
    lockLabel.value = txt.slice(0, 16).trim().toUpperCase()
  } else {
    isLocked.value = false
    lockLabel.value = ''
  }
}

function handleMouseLeave() {
  isVisible.value = false
}

function loop() {
  // Smooth lerp follow
  cursorX.value += (targetX.value - cursorX.value) * 0.28
  cursorY.value += (targetY.value - cursorY.value) * 0.28
  rafId = requestAnimationFrame(loop)
}

onMounted(() => {
  window.addEventListener('mousemove', updatePosition, { passive: true })
  document.addEventListener('mouseleave', handleMouseLeave)
  loop()
})

onUnmounted(() => {
  window.removeEventListener('mousemove', updatePosition)
  document.removeEventListener('mouseleave', handleMouseLeave)
  if (rafId) cancelAnimationFrame(rafId)
})
</script>

<template>
  <div
    v-show="isVisible"
    class="pointer-events-none fixed top-0 left-0 z-[9999] hidden md:block"
    :style="{
      transform: `translate3d(${cursorX}px, ${cursorY}px, 0)`
    }"
  >
    <!-- Center Dot -->
    <div
      class="absolute -top-1 -left-1 h-2 w-2 rounded-full transition-all duration-150"
      :class="[
        isLocked
          ? 'bg-orange-500 shadow-[0_0_12px_#ff5500] scale-125'
          : 'bg-amber-400 shadow-[0_0_8px_#ffcc00]'
      ]"
    ></div>

    <!-- Outer Rotating Targeting Ring -->
    <div
      class="absolute -top-5 -left-5 h-10 w-10 rounded-full border border-dashed transition-all duration-200"
      :class="[
        isLocked
          ? 'border-orange-500 scale-125 animate-spin shadow-[0_0_15px_rgba(255,85,0,0.5)]'
          : 'border-amber-400/40 animate-[spin_8s_linear_infinite]'
      ]"
    ></div>

    <!-- Crosshair Corner Brackets -->
    <div
      class="absolute -top-4 -left-4 h-8 w-8 transition-transform duration-200"
      :class="[isLocked ? 'scale-150 text-orange-400' : 'text-amber-400/70']"
    >
      <!-- Top-Left -->
      <span class="absolute top-0 left-0 h-1.5 w-1.5 border-t-2 border-l-2 border-current"></span>
      <!-- Top-Right -->
      <span class="absolute top-0 right-0 h-1.5 w-1.5 border-t-2 border-r-2 border-current"></span>
      <!-- Bottom-Left -->
      <span class="absolute bottom-0 left-0 h-1.5 w-1.5 border-b-2 border-l-2 border-current"></span>
      <!-- Bottom-Right -->
      <span class="absolute bottom-0 right-0 h-1.5 w-1.5 border-b-2 border-r-2 border-current"></span>
    </div>

    <!-- Coordinate & Lock HUD Readout Tag -->
    <div
      class="absolute top-4 left-4 whitespace-nowrap font-mono text-[9px] font-bold tracking-widest transition-all"
      :class="[
        isLocked
          ? 'text-orange-400 bg-[#0c0f15]/90 px-1.5 py-0.5 border border-orange-500/50 mecha-cut-tr shadow-[0_0_10px_rgba(255,85,0,0.3)]'
          : 'text-amber-400/60'
      ]"
    >
      <div v-if="isLocked" class="flex items-center gap-1">
        <span class="h-1.5 w-1.5 bg-orange-400 animate-ping"></span>
        <span>&lt; LOCK: {{ lockLabel || 'TARGET' }} &gt;</span>
      </div>
      <div v-else>
        X:{{ Math.round(targetX) }} Y:{{ Math.round(targetY) }}
      </div>
    </div>
  </div>
</template>
