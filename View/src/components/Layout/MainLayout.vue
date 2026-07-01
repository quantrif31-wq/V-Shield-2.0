<template>
    <div
        class="ops-shell"
        :class="{
            'nav-collapsed': desktopCollapsed,
            'nav-open': mobileSidebarOpen,
            'is-mobile': isMobile,
        }"
    >
        <div class="shell-background" aria-hidden="true">
            <div class="shell-grid"></div>
        </div>

        <Sidebar
            :collapsed="desktopCollapsed"
            :is-mobile="isMobile"
            :mobile-open="mobileSidebarOpen"
            @toggle="handleSidebarToggle"
            @close-mobile="mobileSidebarOpen = false"
        />

        <button
            v-if="isMobile && mobileSidebarOpen"
            class="shell-scrim"
            type="button"
            aria-label="Đóng điều hướng"
            @click="mobileSidebarOpen = false"
        ></button>

        <div class="shell-main">
            <Header
                :collapsed="desktopCollapsed"
                :is-mobile="isMobile"
                @toggle-sidebar="handleSidebarToggle"
            />
            <main class="main-content">
                <div class="content-shell">
                    <router-view v-slot="{ Component, route }">
                        <keep-alive>
                            <component :is="Component" :key="route.name" v-if="route.meta.keepAlive" />
                        </keep-alive>
                        <component :is="Component" :key="route.fullPath" v-if="!route.meta.keepAlive" />
                    </router-view>
                </div>
            </main>
        </div>
        <AIChatBot />
    </div>
</template>

<script setup>
import { onMounted, onUnmounted, ref } from 'vue'
import Sidebar from './Sidebar.vue'
import Header from './Header.vue'
import AIChatBot from '../AIChatBot.vue'
import { startSecurityAlertPolling, stopSecurityAlertPolling } from '../../services/securityAlertBus'

const isMobile = ref(false)
const desktopCollapsed = ref(false)
const mobileSidebarOpen = ref(false)

function syncViewport() {
    const nextIsMobile = window.innerWidth < 1024
    isMobile.value = nextIsMobile

    if (!nextIsMobile) {
        mobileSidebarOpen.value = false
    }
}

function handleSidebarToggle() {
    if (isMobile.value) {
        mobileSidebarOpen.value = !mobileSidebarOpen.value
        return
    }

    desktopCollapsed.value = !desktopCollapsed.value
}

onMounted(() => {
    syncViewport()
    window.addEventListener('resize', syncViewport)
    startSecurityAlertPolling()
})

onUnmounted(() => {
    window.removeEventListener('resize', syncViewport)
    stopSecurityAlertPolling()
})
</script>

<style scoped>
.ops-shell {
    min-height: 100vh;
    position: relative;
}

.shell-background {
    position: fixed;
    inset: 0;
    pointer-events: none;
    z-index: 0;
}

.shell-grid {
    position: absolute;
    inset: 0;
    background-image:
        linear-gradient(rgba(16, 32, 51, 0.025) 1px, transparent 1px),
        linear-gradient(90deg, rgba(16, 32, 51, 0.025) 1px, transparent 1px);
    background-size: 48px 48px;
    mask-image: linear-gradient(180deg, rgba(0, 0, 0, 0.32), transparent 88%);
}

.shell-main {
    position: relative;
    z-index: 1;
}

.main-content {
    margin-left: var(--sidebar-width);
    padding-top: calc(var(--header-height) + 18px);
    transition: margin-left var(--transition-slow);
}

.nav-collapsed {
    --sidebar-width: 0px;
}

.content-shell {
    min-height: calc(100vh - var(--header-height));
    padding-bottom: 28px;
}

.shell-scrim {
    position: fixed;
    inset: 0;
    z-index: 84;
    background: rgba(16, 32, 51, 0.42);
    backdrop-filter: blur(6px);
}

:global(body.monitoring-immersive) {
    overflow: hidden;
}

@media (max-width: 1023px) {
    .main-content {
        margin-left: 0;
        padding-top: calc(var(--header-height) + 10px);
    }

    .content-shell {
        padding-bottom: 18px;
    }
}
</style>
