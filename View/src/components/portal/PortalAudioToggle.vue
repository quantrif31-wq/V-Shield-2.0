<script setup>
import { ref, onMounted, onUnmounted } from 'vue'

const isMuted = ref(true)
let audioCtx = null
let ambientInterval = null

function getAudioContext() {
  if (!audioCtx && typeof window !== 'undefined') {
    const AudioContext = window.AudioContext || window.webkitAudioContext
    if (AudioContext) {
      audioCtx = new AudioContext()
    }
  }
  if (audioCtx && audioCtx.state === 'suspended') {
    audioCtx.resume()
  }
  return audioCtx
}

function playCyberChord() {
  if (isMuted.value) return
  const ctx = getAudioContext()
  if (!ctx) return

  const now = ctx.currentTime
  // Lush cyber-ambient frequencies (F minor 9th: F3, C4, Eb4, G4, Bb4)
  const freqs = [174.61, 261.63, 311.13, 392.00, 466.16]

  freqs.forEach((freq, idx) => {
    const osc = ctx.createOscillator()
    const gain = ctx.createGain()
    const filter = ctx.createBiquadFilter()

    osc.type = idx % 2 === 0 ? 'sine' : 'triangle'
    osc.frequency.setValueAtTime(freq, now)

    filter.type = 'lowpass'
    filter.frequency.setValueAtTime(800 + idx * 200, now)

    gain.gain.setValueAtTime(0.001, now)
    gain.gain.exponentialRampToValueAtTime(0.035, now + 0.8)
    gain.gain.exponentialRampToValueAtTime(0.0001, now + 4.5)

    osc.connect(filter)
    filter.connect(gain)
    gain.connect(ctx.destination)

    osc.start(now)
    osc.stop(now + 4.6)
  })
}

function playClickSfx() {
  if (isMuted.value) return
  const ctx = getAudioContext()
  if (!ctx) return

  const now = ctx.currentTime
  const osc = ctx.createOscillator()
  const gain = ctx.createGain()

  osc.type = 'sine'
  osc.frequency.setValueAtTime(880, now)
  osc.frequency.exponentialRampToValueAtTime(1760, now + 0.12)

  gain.gain.setValueAtTime(0.06, now)
  gain.gain.exponentialRampToValueAtTime(0.001, now + 0.14)

  osc.connect(gain)
  gain.connect(ctx.destination)

  osc.start(now)
  osc.stop(now + 0.15)
}

function toggleAudio() {
  isMuted.value = !isMuted.value
  if (!isMuted.value) {
    getAudioContext()
    playCyberChord()
    if (!ambientInterval) {
      ambientInterval = setInterval(() => {
        if (!isMuted.value) playCyberChord()
      }, 9000)
    }
  } else {
    if (ambientInterval) {
      clearInterval(ambientInterval)
      ambientInterval = null
    }
  }
}

onMounted(() => {
  window.addEventListener('portal-click-sfx', playClickSfx)
})

onUnmounted(() => {
  window.removeEventListener('portal-click-sfx', playClickSfx)
  if (ambientInterval) clearInterval(ambientInterval)
  if (audioCtx) audioCtx.close().catch(() => {})
})
</script>

<template>
  <button
    type="button"
    @click="toggleAudio"
    class="group relative inline-flex items-center gap-2 rounded-full border border-cyan-500/40 bg-slate-900/80 px-3.5 py-1.5 text-xs font-semibold text-cyan-300 shadow-[0_0_15px_rgba(0,240,255,0.2)] backdrop-blur-md transition-all hover:border-cyan-400 hover:bg-slate-800/90 hover:text-cyan-200 hover:shadow-[0_0_25px_rgba(0,240,255,0.4)]"
    :title="isMuted ? 'Bật âm thanh không gian (BGM/SFX)' : 'Tắt âm thanh'"
  >
    <!-- Visualizer bars when active -->
    <span class="flex h-3.5 items-center gap-0.5" aria-hidden="true">
      <span
        class="h-2 w-0.5 rounded-full bg-cyan-400 transition-all duration-300"
        :class="{ 'animate-pulse h-3': !isMuted, 'opacity-40': isMuted }"
      ></span>
      <span
        class="h-3 w-0.5 rounded-full bg-pink-400 transition-all duration-300"
        :class="{ 'animate-bounce h-3.5': !isMuted, 'opacity-40': isMuted }"
        style="animation-delay: 150ms"
      ></span>
      <span
        class="h-1.5 w-0.5 rounded-full bg-cyan-400 transition-all duration-300"
        :class="{ 'animate-pulse h-2.5': !isMuted, 'opacity-40': isMuted }"
        style="animation-delay: 300ms"
      ></span>
    </span>

    <span class="tracking-wide">
      {{ isMuted ? 'BGM: Tắt' : 'BGM: Bật' }}
    </span>

    <!-- Sparkle indicator -->
    <span
      v-if="!isMuted"
      class="absolute -top-1 -right-1 flex h-2 w-2 items-center justify-center"
    >
      <span class="absolute inline-flex h-full w-full animate-ping rounded-full bg-cyan-400 opacity-75"></span>
      <span class="relative inline-flex h-1.5 w-1.5 rounded-full bg-cyan-300"></span>
    </span>
  </button>
</template>
