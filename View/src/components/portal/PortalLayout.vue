<script setup>
import { ref, onMounted, provide } from 'vue'
import PortalNavbar from './PortalNavbar.vue'
import PortalFooter from './PortalFooter.vue'
import PortalParticlesCanvas from './PortalParticlesCanvas.vue'
import PortalAuthModal from './PortalAuthModal.vue'

const showAuthModal = ref(false)
const communityUser = ref(null)

onMounted(() => {
  const savedUser = localStorage.getItem('vshield_community_user')
  if (savedUser) {
    try {
      communityUser.value = JSON.parse(savedUser)
    } catch {}
  }
})

function handleOpenAuth() {
  showAuthModal.value = true
}

function handleLoginSuccess(user) {
  communityUser.value = user
  showAuthModal.value = false
}

function handleLogoutCommunity() {
  communityUser.value = null
  localStorage.removeItem('vshield_community_user')
}

// Provide to all children pages
provide('communityUser', communityUser)
provide('openAuthModal', handleOpenAuth)
</script>

<template>
  <div class="relative min-h-screen w-full bg-[#07080b] text-slate-100 font-sans selection:bg-amber-400 selection:text-slate-950">
    <!-- Ambient Particle Canvas Background (Amber & Laser Sparkles) -->
    <PortalParticlesCanvas />

    <!-- Mecha Carbon Grid & Hexagon Cockpit Glows -->
    <div class="pointer-events-none fixed inset-0 z-0 bg-[radial-gradient(ellipse_80%_80%_at_50%_-20%,rgba(255,204,0,0.08),rgba(0,0,0,0))]"></div>
    <div class="pointer-events-none fixed inset-0 z-0 bg-[radial-gradient(ellipse_60%_60%_at_80%_90%,rgba(255,85,0,0.06),rgba(0,0,0,0))]"></div>
    <div class="pointer-events-none fixed inset-0 z-0 bg-[linear-gradient(to_right,#ffcc0006_1px,transparent_1px),linear-gradient(to_bottom,#ffcc0006_1px,transparent_1px)] bg-[size:3.5rem_3.5rem]"></div>
    <div class="pointer-events-none fixed inset-0 z-0 mecha-scanlines opacity-40"></div>

    <!-- Main Content Container -->
    <div class="relative z-10 flex min-h-screen flex-col justify-between">
      <!-- Navbar -->
      <PortalNavbar
        :community-user="communityUser"
        @open-auth="handleOpenAuth"
        @logout-community="handleLogoutCommunity"
      />

      <!-- Sub-Page Views with Smooth Transitions -->
      <main class="flex-1">
        <router-view v-slot="{ Component }">
          <transition name="mecha-page" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </main>

      <!-- Footer -->
      <PortalFooter />
    </div>

    <!-- Google SSO / Pilot ID Auth Modal -->
    <PortalAuthModal
      v-if="showAuthModal"
      @close="showAuthModal = false"
      @login-success="handleLoginSuccess"
    />
  </div>
</template>

<style>
.mecha-page-enter-active,
.mecha-page-leave-active {
  transition: opacity 0.22s cubic-bezier(0.4, 0, 0.2, 1), transform 0.22s cubic-bezier(0.4, 0, 0.2, 1);
}

.mecha-page-enter-from {
  opacity: 0;
  transform: translateY(8px) scale(0.99);
}

.mecha-page-leave-to {
  opacity: 0;
  transform: translateY(-8px) scale(0.99);
}
</style>
