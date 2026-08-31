<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { mechaAudio } from '../../utils/portalAudio'

const isExpanded = ref(false)
const events = ref([])
const activeLatency = ref(24)
let intervalId = null

const sampleLogTemplates = [
  { type: 'SYNC', msg: 'Area Node 01 heartbeat ACK (18ms)', color: 'text-emerald-400' },
  { type: 'AI_RADAR', msg: 'Face Vector ArcFace Hash:0x8E12 match 99.98%', color: 'text-amber-400' },
  { type: 'BARRIER', msg: 'ANPR OCR 29A-888.88 verify match // Pass Granted', color: 'text-cyan-400' },
  { type: 'CRYPTO', msg: 'TOTP Token HMAC-SHA256 verified valid', color: 'text-emerald-400' },
  { type: 'SOC_LOG', msg: 'Security Perimeter Zone A Nominal', color: 'text-slate-400' }
]

function generateEvent() {
  const t = sampleLogTemplates[Math.floor(Math.random() * sampleLogTemplates.length)]
  const timeStr = new Date().toTimeString().split(' ')[0]
  events.value.unshift({
    id: Date.now() + Math.random(),
    time: timeStr,
    type: t.type,
    msg: t.msg,
    color: t.color
  })
  if (events.value.length > 8) events.value.pop()
  activeLatency.value = Math.floor(18 + Math.random() * 12)
}

function toggleDrawer() {
  mechaAudio.playClick()
  isExpanded.value = !isExpanded.value
}

onMounted(() => {
  for (let i = 0; i < 4; i++) generateEvent()
  intervalId = setInterval(generateEvent, 3800)
})

onUnmounted(() => {
  if (intervalId) clearInterval(intervalId)
})
</script>

<template>
  <div class="fixed bottom-3 right-3 z-40 hidden sm:block font-mono">
    <!-- Collapsed Toggle Button -->
    <div
      v-if="!isExpanded"
      @click="toggleDrawer"
      class="mecha-laser-border mecha-hud-bracket flex cursor-pointer items-center gap-2.5 border border-amber-500/40 bg-[#080a0f]/95 px-3.5 py-2 text-xs font-black text-amber-400 shadow-[0_0_20px_rgba(255,204,0,0.3)] transition hover:border-amber-400 mecha-cut-tr"
    >
      <span class="relative flex h-2 w-2">
        <span class="absolute inline-flex h-full w-full animate-ping rounded-full bg-emerald-400 opacity-75"></span>
        <span class="relative inline-flex h-2 w-2 rounded-full bg-emerald-500"></span>
      </span>
      <span>SOC TELEMETRY FEED</span>
      <span class="bg-amber-950 px-1.5 py-0.2 text-[9px] text-amber-300 border border-amber-500/30">
        {{ activeLatency }}ms
      </span>
      <span class="text-slate-500">▲</span>
    </div>

    <!-- Expanded Floating SOC Terminal -->
    <div
      v-else
      class="mecha-hud-bracket w-[380px] border-2 border-amber-500/50 bg-[#07080b]/95 p-4 shadow-[0_0_35px_rgba(255,204,0,0.35)] mecha-cut-corners backdrop-blur-xl"
    >
      <!-- Header -->
      <div class="flex items-center justify-between border-b border-slate-800 pb-2.5">
        <div class="flex items-center gap-2">
          <span class="h-2 w-2 bg-amber-400 animate-pulse"></span>
          <span class="text-xs font-black text-amber-400 tracking-wider">LIVE DEFENSE TELEMETRY</span>
        </div>
        <div class="flex items-center gap-2">
          <span class="text-[9px] text-emerald-400 font-bold">{{ activeLatency }}ms ping</span>
          <button
            type="button"
            @click="toggleDrawer"
            class="text-xs text-slate-500 hover:text-amber-400"
          >
            ✕
          </button>
        </div>
      </div>

      <!-- Live Stream Log Items -->
      <div class="my-3 max-h-[180px] space-y-2 overflow-y-auto pr-1 text-[10.5px]">
        <div
          v-for="ev in events"
          :key="ev.id"
          class="flex items-start gap-2 border-b border-slate-900/60 pb-1.5 font-sans"
        >
          <span class="font-mono text-slate-500 shrink-0 text-[9.5px]">[{{ ev.time }}]</span>
          <span class="font-mono font-bold shrink-0" :class="ev.color">[{{ ev.type }}]</span>
          <span class="text-slate-300 truncate">{{ ev.msg }}</span>
        </div>
      </div>

      <!-- Bottom System Status -->
      <div class="flex items-center justify-between border-t border-slate-800 pt-2 text-[9.5px] text-slate-400">
        <span>CENTRAL CLOUD: <strong class="text-emerald-400">SYNCED</strong></span>
        <span>AREA 01: <strong class="text-emerald-400">ONLINE</strong></span>
        <span>SECURITY: <strong class="text-amber-400">SECURE</strong></span>
      </div>
    </div>
  </div>
</template>
