<script setup>
import { ref, onMounted } from 'vue'
import { mechaAudio } from '../../utils/portalAudio'

const isSfxOn = ref(true)
const isBgmOn = ref(false)

function toggleSfx() {
  isSfxOn.value = !isSfxOn.value
  mechaAudio.sfxEnabled = isSfxOn.value
  localStorage.setItem('vshield_portal_audio', String(isSfxOn.value))
  if (isSfxOn.value) mechaAudio.playClick()
}

function toggleBgm() {
  isBgmOn.value = mechaAudio.toggleBgm()
  if (isSfxOn.value) mechaAudio.playTargetLock()
}

onMounted(() => {
  isSfxOn.value = mechaAudio.sfxEnabled
  isBgmOn.value = mechaAudio.bgmEnabled
})
</script>

<template>
  <div class="flex items-center gap-1.5 font-mono">
    <!-- SFX Button -->
    <button
      type="button"
      @click="toggleSfx"
      class="flex items-center gap-1 border px-2 py-1 text-[10px] font-black uppercase tracking-wider transition-all mecha-cut-tr"
      :class="[
        isSfxOn
          ? 'border-amber-500/50 bg-[#151a24] text-amber-400 shadow-[0_0_12px_rgba(255,204,0,0.3)]'
          : 'border-slate-800 bg-[#0c0f15] text-slate-500 hover:text-slate-400'
      ]"
      title="Bật/Tắt hiệu ứng âm thanh cơ khí SFX"
    >
      <svg class="h-3.5 w-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <polygon points="11 5 6 9 2 9 2 15 6 15 11 19 11 5"></polygon>
        <path v-if="isSfxOn" d="M19.07 4.93a10 10 0 0 1 0 14.14M15.54 8.46a5 5 0 0 1 0 7.07"></path>
        <line v-else x1="23" y1="9" x2="17" y2="15"></line>
        <line v-if="!isSfxOn" x1="17" y1="9" x2="23" y2="15"></line>
      </svg>
      <span>SFX</span>
    </button>

    <!-- Ambient BGM Synth Button -->
    <button
      type="button"
      @click="toggleBgm"
      class="flex items-center gap-1 border px-2 py-1 text-[10px] font-black uppercase tracking-wider transition-all mecha-cut-tr"
      :class="[
        isBgmOn
          ? 'border-orange-500/60 bg-[#1f150e] text-orange-400 shadow-[0_0_15px_rgba(255,85,0,0.4)] animate-pulse'
          : 'border-slate-800 bg-[#0c0f15] text-slate-500 hover:text-slate-400'
      ]"
      title="Bật/Tắt nhạc nền Sci-Fi Synth BGM thời gian thực"
    >
      <svg class="h-3.5 w-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <path d="M9 18V5l12-2v13"></path>
        <circle cx="6" cy="18" r="3"></circle>
        <circle cx="18" cy="16" r="3"></circle>
      </svg>
      <span>BGM</span>
    </button>
  </div>
</template>
