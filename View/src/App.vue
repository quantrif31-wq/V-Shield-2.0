<template>
  <transition name="boot-fade">
    <div v-if="!booted" class="boot-splash" aria-hidden="true">
      <div class="boot-grid"></div>
      <div class="boot-mark">
        <svg viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M12 2L3 7V17L12 22L21 17V7L12 2Z" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round" />
          <path d="M12 8L8 10.5V15.5L12 18L16 15.5V10.5L12 8Z" fill="currentColor" opacity="0.28" />
          <path d="M12 8L8 10.5V15.5L12 18L16 15.5V10.5L12 8Z" stroke="currentColor" stroke-width="1.2" stroke-linejoin="round" />
        </svg>
      </div>
      <div class="boot-copy">
        <strong>V-Shield</strong>
        <span>Trung tâm điều phối</span>
      </div>
      <div class="boot-bar"><span></span></div>
    </div>
  </transition>

  <router-view />
  <RouteProgress />
  <ToastProvider />
</template>

<script setup>
import { onMounted, ref } from 'vue'
import router from './router'
import ToastProvider from './components/ui/ToastProvider.vue'
import RouteProgress from './components/ui/RouteProgress.vue'

const booted = ref(false)

onMounted(async () => {
  try {
    await router.isReady()
  } catch {
    // router đã xử lý lỗi điều hướng riêng; vẫn cho phép vào app
  }
  window.setTimeout(() => {
    booted.value = true
  }, 260)
})
</script>

<style>
.boot-splash {
  position: fixed;
  inset: 0;
  z-index: 1600;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 18px;
  background: radial-gradient(circle at 50% 30%, #14304d, #0c1b2a 72%);
  color: #eefbfc;
}

.boot-grid {
  position: absolute;
  inset: 0;
  background-image:
    linear-gradient(rgba(84, 196, 211, 0.06) 1px, transparent 1px),
    linear-gradient(90deg, rgba(84, 196, 211, 0.06) 1px, transparent 1px);
  background-size: 44px 44px;
  mask-image: radial-gradient(circle at center, rgba(0, 0, 0, 0.7), transparent 74%);
}

.boot-mark {
  position: relative;
  display: grid;
  place-items: center;
  width: 74px;
  height: 74px;
  border-radius: 22px;
  color: #b8f7ff;
  background: linear-gradient(145deg, #0f7c82, #163f5f);
  border: 1px solid rgba(84, 196, 211, 0.35);
  box-shadow: 0 0 0 10px rgba(84, 196, 211, 0.08), 0 24px 60px rgba(0, 0, 0, 0.45);
  animation: boot-pulse 1.6s ease-in-out infinite;
}

.boot-mark svg {
  width: 40px;
  height: 40px;
}

.boot-copy {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
}

.boot-copy strong {
  font-family: 'Segoe UI Variable Display', 'Segoe UI', Arial, sans-serif;
  font-size: 1.5rem;
  font-weight: 800;
  letter-spacing: -0.02em;
  background: linear-gradient(120deg, #b8f7ff, #eefbfc 60%);
  -webkit-background-clip: text;
  background-clip: text;
  color: transparent;
}

.boot-copy span {
  color: rgba(188, 209, 218, 0.78);
  font-size: 0.82rem;
  font-weight: 600;
  letter-spacing: 0.16em;
  text-transform: uppercase;
}

.boot-bar {
  position: relative;
  width: 172px;
  height: 3px;
  margin-top: 4px;
  border-radius: 999px;
  background: rgba(84, 196, 211, 0.16);
  overflow: hidden;
}

.boot-bar span {
  position: absolute;
  inset: 0 auto 0 0;
  width: 46%;
  border-radius: 999px;
  background: linear-gradient(90deg, var(--teal-500), #b8f7ff);
  animation: boot-sweep 1.1s cubic-bezier(0.45, 0, 0.55, 1) infinite;
}

@keyframes boot-pulse {
  0%,
  100% {
    box-shadow: 0 0 0 0 rgba(84, 196, 211, 0.16), 0 24px 60px rgba(0, 0, 0, 0.45);
  }
  50% {
    box-shadow: 0 0 0 14px rgba(84, 196, 211, 0.04), 0 24px 60px rgba(0, 0, 0, 0.45);
  }
}

@keyframes boot-sweep {
  0% {
    transform: translateX(-110%);
  }
  100% {
    transform: translateX(320%);
  }
}

.boot-fade-leave-active {
  transition: opacity 0.34s ease;
  pointer-events: none;
}

.boot-fade-leave-to {
  opacity: 0;
}

@media (prefers-reduced-motion: reduce) {
  .boot-mark,
  .boot-bar span {
    animation: none;
  }
}
</style>