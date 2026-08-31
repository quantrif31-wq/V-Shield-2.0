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
  { name: 'Trang Chủ', path: '/', icon: 'hero' },
  { name: 'Tính Năng', path: '/features', icon: 'sparkles' },
  { name: 'Lộ Trình', path: '/roadmap', icon: 'timeline' },
  { name: 'Tải Ứng Dụng', path: '/download', icon: 'download' },
  { name: 'Cộng Đồng', path: '/community', icon: 'community' },
  { name: 'Đội Ngũ', path: '/about', icon: 'team' },
  { name: 'Góp Ý & Liên Hệ', path: '/contact', icon: 'contact' }
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
  <header class="sticky top-0 z-50 w-full border-b border-cyan-500/20 bg-slate-950/80 backdrop-blur-xl transition-all">
    <!-- Neon Top Glow Line -->
    <div class="h-0.5 w-full bg-gradient-to-r from-transparent via-cyan-400 via-pink-500 to-transparent opacity-80"></div>

    <div class="mx-auto flex max-w-7xl items-center justify-between px-4 py-3 sm:px-6 lg:px-8">
      <!-- 1. Brand Logo -->
      <router-link
        to="/"
        @click="triggerSfx"
        class="group flex items-center gap-3 decoration-transparent outline-none"
      >
        <div class="relative flex h-10 w-10 items-center justify-center rounded-xl border border-cyan-400/40 bg-gradient-to-br from-cyan-900/60 to-slate-900 shadow-[0_0_20px_rgba(0,240,255,0.35)] transition-all group-hover:scale-105 group-hover:border-cyan-300 group-hover:shadow-[0_0_30px_rgba(0,240,255,0.6)]">
          <svg class="h-6 w-6 text-cyan-300 drop-shadow-[0_0_8px_#00f0ff]" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M12 2L3 7V17L12 22L21 17V7L12 2Z" stroke-linejoin="round" />
            <path d="M12 8L8 10.5V15.5L12 18L16 15.5V10.5L12 8Z" fill="currentColor" opacity="0.35" />
          </svg>
          <span class="absolute -inset-0.5 -z-10 rounded-xl bg-cyan-400/20 blur-sm group-hover:bg-cyan-400/40"></span>
        </div>
        <div class="flex flex-col">
          <div class="flex items-center gap-1.5">
            <span class="font-black tracking-wider text-transparent bg-clip-text bg-gradient-to-r from-cyan-300 via-teal-200 to-pink-400 text-lg font-mono drop-shadow-[0_0_12px_rgba(0,240,255,0.4)]">
              V-SHIELD
            </span>
            <span class="rounded bg-gradient-to-r from-pink-500 to-purple-600 px-1.5 py-0.2 text-[10px] font-black uppercase text-white shadow-[0_0_10px_rgba(255,42,133,0.5)]">
              2.0
            </span>
          </div>
          <span class="text-[10px] tracking-widest text-cyan-400/70 font-semibold uppercase">
            NEURAL SECURITY PORTAL
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
          class="relative px-3 py-1.5 text-xs font-bold uppercase tracking-wider transition-all duration-200"
          :class="[
            isActive(item.path)
              ? 'text-cyan-300 drop-shadow-[0_0_10px_rgba(0,240,255,0.6)]'
              : 'text-slate-300 hover:text-cyan-200 hover:drop-shadow-[0_0_8px_rgba(0,240,255,0.3)]'
          ]"
        >
          {{ item.name }}
          <!-- Active Neon Underline -->
          <span
            v-if="isActive(item.path)"
            class="absolute bottom-0 left-2 right-2 h-0.5 bg-gradient-to-r from-cyan-400 via-pink-400 to-cyan-400 shadow-[0_0_10px_#00f0ff]"
          ></span>
        </button>
      </nav>

      <!-- 3. Right Action Tools -->
      <div class="flex items-center gap-2 sm:gap-3">
        <!-- Live Status Pulse Badge (Desktop) -->
        <div class="hidden items-center gap-1.5 rounded-full border border-emerald-500/30 bg-emerald-950/40 px-2.5 py-1 text-[11px] font-semibold text-emerald-300 shadow-[0_0_12px_rgba(16,185,129,0.2)] md:flex">
          <span class="relative flex h-2 w-2">
            <span class="absolute inline-flex h-full w-full animate-ping rounded-full bg-emerald-400 opacity-75"></span>
            <span class="relative inline-flex h-2 w-2 rounded-full bg-emerald-500"></span>
          </span>
          <span class="font-mono uppercase tracking-wider">ONLINE</span>
        </div>

        <!-- Audio Synthesizer Toggle -->
        <PortalAudioToggle />

        <!-- Community Operator Avatar / Login -->
        <div v-if="communityUser" class="relative flex items-center gap-2">
          <button
            type="button"
            @click="handleAuthClick"
            class="flex items-center gap-2 rounded-full border border-pink-500/40 bg-slate-900/90 py-1 pl-1 pr-2.5 text-xs text-pink-200 shadow-[0_0_15px_rgba(255,42,133,0.3)] transition hover:border-pink-400"
          >
            <img :src="communityUser.avatarUrl" alt="Avatar" class="h-6 w-6 rounded-full border border-pink-400 bg-slate-800" />
            <span class="max-w-[80px] truncate font-semibold sm:max-w-[120px]">{{ communityUser.fullName }}</span>
          </button>
        </div>
        <button
          v-else
          type="button"
          @click="handleAuthClick"
          class="hidden items-center gap-1.5 rounded-full border border-pink-500/30 bg-slate-900/60 px-3 py-1.5 text-xs font-semibold text-pink-300 transition-all hover:border-pink-400 hover:bg-pink-950/40 hover:text-pink-200 sm:flex"
        >
          <svg class="h-3.5 w-3.5 text-pink-400" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
            <circle cx="12" cy="7" r="4"></circle>
          </svg>
          <span>Cộng Đồng</span>
        </button>

        <!-- Main Dashboard / Login CTA Button -->
        <router-link
          v-if="isLoggedIn"
          to="/dashboard"
          @click="triggerSfx"
          class="group relative inline-flex items-center gap-1.5 overflow-hidden rounded-xl border border-cyan-400/50 bg-gradient-to-r from-cyan-600 via-teal-500 to-cyan-500 px-3.5 py-1.5 text-xs font-bold uppercase tracking-wider text-slate-950 shadow-[0_0_20px_rgba(0,240,255,0.4)] transition-all hover:scale-105 hover:shadow-[0_0_30px_rgba(0,240,255,0.7)]"
        >
          <span class="relative z-10 flex items-center gap-1">
            <svg class="h-3.5 w-3.5 fill-current" viewBox="0 0 24 24">
              <path d="M13 2L3 14h9l-1 8 10-12h-9l1-8z"/>
            </svg>
            Dashboard
          </span>
          <div class="absolute inset-0 -translate-x-full bg-gradient-to-r from-transparent via-white/40 to-transparent transition-transform duration-700 group-hover:translate-x-full"></div>
        </router-link>

        <router-link
          v-else
          to="/login"
          @click="triggerSfx"
          class="group relative inline-flex items-center gap-1.5 overflow-hidden rounded-xl border border-cyan-400/60 bg-gradient-to-r from-cyan-500 via-teal-400 to-pink-500 px-3.5 py-1.5 text-xs font-bold uppercase tracking-wider text-slate-950 shadow-[0_0_20px_rgba(0,240,255,0.4)] transition-all hover:scale-105 hover:shadow-[0_0_30px_rgba(0,240,255,0.7)]"
        >
          <span class="relative z-10 flex items-center gap-1">
            <svg class="h-3.5 w-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
              <path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4M10 17l5-5-5-5M15 12H3"/>
            </svg>
            Đăng Nhập
          </span>
          <div class="absolute inset-0 -translate-x-full bg-gradient-to-r from-transparent via-white/40 to-transparent transition-transform duration-700 group-hover:translate-x-full"></div>
        </router-link>

        <!-- Mobile Menu Toggle Button -->
        <button
          type="button"
          @click="mobileMenuOpen = !mobileMenuOpen"
          class="inline-flex items-center justify-center rounded-lg border border-cyan-500/30 p-2 text-cyan-300 transition hover:bg-slate-900 lg:hidden"
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
      class="border-b border-cyan-500/20 bg-slate-950/95 px-4 pt-2 pb-6 backdrop-blur-2xl lg:hidden"
    >
      <div class="flex flex-col space-y-1">
        <button
          v-for="item in navLinks"
          :key="item.path"
          type="button"
          @click="navigateTo(item.path)"
          class="flex items-center justify-between rounded-lg px-3 py-2.5 text-left text-sm font-bold uppercase tracking-wider transition"
          :class="[
            isActive(item.path)
              ? 'bg-cyan-500/10 text-cyan-300 border-l-2 border-cyan-400 pl-4'
              : 'text-slate-300 hover:bg-slate-900 hover:text-cyan-200'
          ]"
        >
          <span>{{ item.name }}</span>
          <svg class="h-4 w-4 opacity-50" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="9 18 15 12 9 6"></polyline>
          </svg>
        </button>

        <div class="pt-3 border-t border-slate-800 flex items-center justify-between">
          <button
            type="button"
            @click="handleAuthClick"
            class="flex items-center gap-2 text-xs font-semibold text-pink-400 hover:text-pink-300"
          >
            <svg class="h-4 w-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
              <circle cx="12" cy="7" r="4"></circle>
            </svg>
            <span>{{ communityUser ? communityUser.fullName : 'Tài Khoản Cộng Đồng' }}</span>
          </button>
          <div class="flex items-center gap-1.5 text-xs text-emerald-400">
            <span class="h-2 w-2 rounded-full bg-emerald-500"></span>
            <span>Server Online</span>
          </div>
        </div>
      </div>
    </div>
  </header>
</template>
