<script setup>
import { ref, onMounted, onUnmounted } from 'vue'

const fps = ref('60.0')
const mem = ref('28.4 MB')
let lastTime = performance.now()
let frameCount = 0
let rafId = null

function measureFps(now) {
  frameCount++
  if (now - lastTime >= 1000) {
    fps.value = ((frameCount * 1000) / (now - lastTime)).toFixed(1)
    frameCount = 0
    lastTime = now

    // Memory footprint (if supported)
    if (window.performance && window.performance.memory) {
      mem.value = (window.performance.memory.usedJSHeapSize / (1024 * 1024)).toFixed(1) + ' MB'
    } else {
      mem.value = (24 + Math.random() * 6).toFixed(1) + ' MB'
    }
  }
  rafId = requestAnimationFrame(measureFps)
}

onMounted(() => {
  rafId = requestAnimationFrame(measureFps)
})

onUnmounted(() => {
  if (rafId) cancelAnimationFrame(rafId)
})
</script>

<template>
  <div class="pointer-events-none fixed inset-0 z-30 hidden lg:block font-mono select-none">
    <!-- Top-Left Corner Cockpit HUD -->
    <div class="absolute top-2.5 left-4 flex items-center gap-3 text-[9.5px] font-bold text-amber-400/80 bg-[#07080b]/85 px-3 py-1 border border-amber-500/30 mecha-cut-tr shadow-[0_0_15px_rgba(255,204,0,0.2)]">
      <span class="h-1.5 w-1.5 bg-emerald-400 animate-ping"></span>
      <span>HUD: {{ fps }} FPS</span>
      <span class="text-slate-500">|</span>
      <span>RAM: {{ mem }}</span>
      <span class="text-slate-500">|</span>
      <span class="text-orange-400">OVERDRIVE: ACTIVE</span>
    </div>

    <!-- Top-Right Corner Cockpit HUD -->
    <div class="absolute top-2.5 right-4 flex items-center gap-2 text-[9.5px] font-bold text-amber-400/80 bg-[#07080b]/85 px-3 py-1 border border-amber-500/30 mecha-cut-tr shadow-[0_0_15px_rgba(255,204,0,0.2)]">
      <span class="text-cyan-400">SEC-GRID // 100% NOMINAL</span>
      <span class="h-1.5 w-1.5 bg-cyan-400 animate-pulse"></span>
    </div>

    <!-- Bottom-Left Tactical Compass Bracket -->
    <div class="absolute bottom-3 left-4 flex items-center gap-2 text-[9px] font-bold text-slate-500 bg-[#07080b]/85 px-2.5 py-1 border border-slate-800 mecha-cut-tr">
      <span>SECTOR: 01-A</span>
      <span>•</span>
      <span>GRID: 42°N 105°E</span>
      <span>•</span>
      <span class="text-amber-400/80">TAC-LINK: STABLE</span>
    </div>

    <!-- 4 Viewport Corner Optical Markers -->
    <!-- Top-Left Bracket -->
    <div class="absolute top-0 left-0 h-6 w-6 border-t-2 border-l-2 border-amber-400/60"></div>
    <!-- Top-Right Bracket -->
    <div class="absolute top-0 right-0 h-6 w-6 border-t-2 border-r-2 border-amber-400/60"></div>
    <!-- Bottom-Left Bracket -->
    <div class="absolute bottom-0 left-0 h-6 w-6 border-b-2 border-l-2 border-amber-400/60"></div>
    <!-- Bottom-Right Bracket -->
    <div class="absolute bottom-0 right-0 h-6 w-6 border-b-2 border-r-2 border-amber-400/60"></div>
  </div>
</template>
