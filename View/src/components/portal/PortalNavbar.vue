<script setup>
import { ref, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { authState } from '../../stores/auth'
import PortalAudioToggle from './PortalAudioToggle.vue'

const props = defineProps({
  communityUser: {
    type: Object,
    default: null
  }
})

const emit = defineEmits(['openAuth', 'logoutCommunity'])
const router = useRouter()
const route = useRoute()
const mobileMenuOpen = ref(false)

const isLoggedIn = computed(() => !!authState.user)
const currentUser = computed(() => authState.user)

const navLinks = [
  { name: 'TỔNG QUAN', path: '/', code: 'OVERVIEW' },
  { name: 'GIẢI PHÁP AI', path: '/features', code: 'SOLUTIONS' },
  { name: 'LỘ TRÌNH', path: '/roadmap', code: 'ROADMAP' },
  { name: 'TẢI APP', path: '/download', code: 'MOBILE' },
  { name: 'ĐÁNH GIÁ', path: '/community', code: 'FEEDBACK' },
  { name: 'ĐỘI NGŨ', path: '/about', code: 'TEAM' },
  { name: 'LIÊN HỆ', path: '/contact', code: 'SUPPORT' }
]

function isActive(path) {
  if (path === '/') {
    return route.path === '/' || route.path === '/portal' || route.path === '/home'
  }
  return route.path.startsWith(path)
}

function triggerSfx() {
  window.dispatchEvent(new CustomEvent('portal-click-sfx'))
}

function navigateTo(path) {
  triggerSfx()
  mobileMenuOpen.value = false
  router.push(path)
}

function handleAuthClick() {
  triggerSfx()
  emit('openAuth')
}
</script>

<template>
  <header class="sticky top-0 z-50 w-full border-b border-amber-500/30 bg-[#080a0f]/90 backdrop-blur-xl transition-all font-mono">
    <!-- Top Hazard Stripe Accent -->
    <div class="h-1 w-full mecha-hazard-bar opacity-90"></div>

    <div class="mx-auto flex max-w-7xl items-center justify-between px-4 py-2.5 sm:px-6 lg:px-8">
      <!-- 1. Brand Logo -->
      <router-link
        to="/"
        @click="triggerSfx"
        class="group flex items-center gap-3 decoration-transparent outline-none"
      >
        <div class="relative flex h-10 w-10 items-center justify-center border-2 border-amber-400 bg-[#121620] mecha-cut-tr shadow-[0_0_20px_rgba(255,204,0,0.35)] transition-all group-hover:scale-105 group-hover:border-amber-300 group-hover:shadow-[0_0_30px_rgba(255,204,0,0.6)]">
          <svg class="h-5 w-5 text-amber-400 drop-shadow-[0_0_8px_#ffcc00]" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2">
            <path d="M12 2L3 7V17L12 22L21 17V7L12 2Z" stroke-linejoin="round" />
            <path d="M12 8L8 10.5V15.5L12 18L16 15.5V10.5L12 8Z" fill="currentColor" opacity="0.4" />
          </svg>
          <span class="absolute -bottom-0.5 -right-0.5 h-1.5 w-1.5 bg-amber-400"></span>
        </div>
        <div class="flex flex-col">
          <div class="flex items-center gap-1.5">
            <span class="font-black tracking-widest text-slate-100 text-base drop-shadow-[0_0_12px_rgba(255,204,0,0.4)]">
              V-SHIELD
            </span>
            <span class="bg-gradient-to-r from-amber-400 to-orange-500 px-1.5 py-0.2 text-[9px] font-black uppercase text-slate-950 mecha-cut-tr">
              2.0
            </span>
          </div>
          <span class="text-[8.5px] tracking-wider text-amber-400/80 font-bold uppercase">
            // SMART DEFENSE SUITE
          </span>
        </div>
      </router-link>

      <!-- 2. Desktop Navigation Menu -->
      <nav class="hidden items-center gap-1 lg:flex xl:gap-2">
        <button
          v-for="item in navLinks"
          :key="item.path"
          type="button"
          @click="navigateTo(item.path)"
          class="relative px-3 py-1.5 text-xs font-black uppercase tracking-wider transition-all duration-200"
          :class="[
            isActive(item.path)
              ? 'text-amber-400 bg-[#151a24] border-b-2 border-amber-400 shadow-[0_0_15px_rgba(255,204,0,0.25)]'
              : 'text-slate-400 hover:text-amber-300 hover:bg-[#121620]/60'
          ]"
        >
          <div class="flex flex-col items-center">
            <span>{{ item.name }}</span>
            <span class="text-[7.5px] text-slate-500 font-normal tracking-tight">{{ item.code }}</span>
          </div>
          <span
            v-if="isActive(item.path)"
            class="absolute top-0 right-0 h-1 w-1 bg-amber-400"
          ></span>
        </button>
      </nav>

      <!-- 3. Right Action Tools -->
      <div class="flex items-center gap-2 sm:gap-3">
        <!-- Live System Status Badge -->
        <div class="hidden items-center gap-2 border border-emerald-500/40 bg-[#0c1613] px-2.5 py-1 text-[10px] font-bold text-emerald-400 shadow-[0_0_15px_rgba(16,185,129,0.2)] md:flex mecha-cut-tr">
          <span class="relative flex h-2 w-2">
            <span class="absolute inline-flex h-full w-full animate-ping rounded-full bg-emerald-400 opacity-75"></span>
            <span class="relative inline-flex h-2 w-2 rounded-full bg-emerald-500"></span>
          </span>
          <span class="tracking-wider">SYSTEM ONLINE</span>
        </div>

        <!-- Audio Synthesizer Toggle -->
        <PortalAudioToggle />

        <!-- Community Operator Profile / SSO -->
        <div v-if="communityUser" class="relative flex items-center gap-2">
          <button
            type="button"
            @click="handleAuthClick"
            class="flex items-center gap-2 border border-amber-500/40 bg-[#121620] py-1 pl-1 pr-2.5 text-xs text-amber-300 mecha-cut-tr shadow-[0_0_15px_rgba(255,204,0,0.2)] transition hover:border-amber-400"
          >
            <img :src="communityUser.avatarUrl" alt="Avatar" class="h-5 w-5 border border-amber-400 bg-slate-800" />
            <span class="max-w-[80px] truncate font-bold sm:max-w-[120px]">{{ communityUser.fullName }}</span>
          </button>
        </div>
        <button
          v-else
          type="button"
          @click="handleAuthClick"
          class="hidden items-center gap-1.5 border border-slate-700 bg-[#121620] px-3 py-1.5 text-xs font-bold text-slate-300 transition-all hover:border-amber-400 hover:text-amber-300 sm:flex mecha-cut-tr"
        >
          <span>TÀI KHOẢN</span>
        </button>

        <!-- Main Dashboard / Login CTA Button -->
        <router-link
          v-if="isLoggedIn"
          to="/dashboard"
          @click="triggerSfx"
          class="mecha-btn-hazard inline-flex items-center gap-1.5 px-3.5 py-1.5 text-xs font-black uppercase tracking-wider mecha-cut-btn transition-all"
        >
          <svg class="h-3.5 w-3.5 fill-current" viewBox="0 0 24 24">
            <path d="M13 2L3 14h9l-1 8 10-12h-9l1-8z"/>
          </svg>
          <span>DASHBOARD</span>
        </router-link>

        <router-link
          v-else
          to="/login"
          @click="triggerSfx"
          class="mecha-btn-hazard inline-flex items-center gap-1.5 px-3.5 py-1.5 text-xs font-black uppercase tracking-wider mecha-cut-btn transition-all"
        >
          <svg class="h-3.5 w-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
            <path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4M10 17l5-5-5-5M15 12H3"/>
          </svg>
          <span>ĐĂNG NHẬP</span>
        </router-link>

        <!-- Mobile Menu Toggle Button -->
        <button
          type="button"
          @click="mobileMenuOpen = !mobileMenuOpen"
          class="inline-flex items-center justify-center border border-amber-500/40 bg-[#121620] p-1.5 text-amber-400 lg:hidden mecha-cut-tr"
          aria-label="Menu"
        >
          <svg v-if="!mobileMenuOpen" class="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="3" y1="12" x2="21" y2="12"></line>
            <line x1="3" y1="6" x2="21" y2="6"></line>
            <line x1="3" y1="18" x2="21" y2="18"></line>
          </svg>
          <svg v-else class="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="18" y1="6" x2="6" y2="18"></line>
            <line x1="6" y1="6" x2="18" y2="18"></line>
          </svg>
        </button>
      </div>
    </div>

    <!-- Mobile Drawer Menu -->
    <div
      v-if="mobileMenuOpen"
      class="border-b border-amber-500/30 bg-[#080a0f]/95 px-4 pt-2 pb-6 backdrop-blur-2xl lg:hidden"
    >
      <div class="flex flex-col space-y-1">
        <button
          v-for="item in navLinks"
          :key="item.path"
          type="button"
          @click="navigateTo(item.path)"
          class="flex items-center justify-between px-3 py-2.5 text-left text-xs font-black uppercase tracking-wider transition"
          :class="[
            isActive(item.path)
              ? 'bg-[#151a24] text-amber-400 border-l-4 border-amber-400 pl-4'
              : 'text-slate-300 hover:bg-[#121620] hover:text-amber-300'
          ]"
        >
          <span>{{ item.name }} // {{ item.code }}</span>
          <span class="text-amber-400">»</span>
        </button>
      </div>
    </div>
  </header>
</template>
