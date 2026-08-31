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
  <div class="relative min-h-screen w-full bg-slate-950 text-slate-100 font-sans selection:bg-cyan-500 selection:text-slate-950">
    <!-- Ambient Particle Canvas Background -->
    <PortalParticlesCanvas />

    <!-- Cyber Background Radial Glows & Grid -->
    <div class="pointer-events-none fixed inset-0 z-0 bg-[radial-gradient(ellipse_80%_80%_at_50%_-20%,rgba(0,240,255,0.15),rgba(255,255,255,0))]"></div>
    <div class="pointer-events-none fixed inset-0 z-0 bg-[radial-gradient(ellipse_60%_60%_at_80%_90%,rgba(255,42,133,0.1),rgba(255,255,255,0))]"></div>
    <div class="pointer-events-none fixed inset-0 z-0 bg-[linear-gradient(to_right,#00f0ff05_1px,transparent_1px),linear-gradient(to_bottom,#00f0ff05_1px,transparent_1px)] bg-[size:4rem_4rem]"></div>

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
          <transition name="portal-page" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </main>

      <!-- Footer -->
      <PortalFooter />
    </div>

    <!-- Google SSO / Community Auth Modal -->
    <PortalAuthModal
      v-if="showAuthModal"
      @close="showAuthModal = false"
      @login-success="handleLoginSuccess"
    />
  </div>
</template>

<style>
.portal-page-enter-active,
.portal-page-leave-active {
  transition: opacity 0.24s cubic-bezier(0.4, 0, 0.2, 1), transform 0.24s cubic-bezier(0.4, 0, 0.2, 1);
}

.portal-page-enter-from {
  opacity: 0;
  transform: translateY(12px) scale(0.99);
}

.portal-page-leave-to {
  opacity: 0;
  transform: translateY(-8px) scale(0.99);
}
</style>
