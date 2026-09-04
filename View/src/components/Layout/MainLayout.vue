<template>
    <div
        class="ops-shell"
        :class="{
            'nav-collapsed': desktopCollapsed,
            'nav-open': mobileSidebarOpen,
            'is-mobile': isMobile,
        }"
    >
        <a class="skip-link" href="#main-content">Bỏ qua điều hướng</a>
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
            <main id="main-content" class="main-content" tabindex="-1">
                <div class="content-shell unified-ui">
                    <RouteErrorBoundary>
                        <router-view v-slot="{ Component, route }">
                            <!--
                              Do not use `mode="out-in"` here.  With lazy
                              routes it removes the current page first and a
                              delayed/failed mount leaves an empty content
                              area.  The normal transition keeps a rendered
                              route available while Vue completes the next
                              navigation.
                            -->
                            <transition name="page-fade">
                                <keep-alive v-if="route.meta.keepAlive">
                                    <component v-if="Component" :is="Component" :key="route.name" />
                                </keep-alive>
                                <component v-else-if="Component" :is="Component" :key="route.fullPath" />
                            </transition>
                        </router-view>
                    </RouteErrorBoundary>
                </div>
            </main>
        </div>
        <AIChatBot />
    </div>
</template>

<script setup>
import { onMounted, onUnmounted, ref, watch } from 'vue'
import Sidebar from './Sidebar.vue'
import Header from './Header.vue'
import AIChatBot from '../AIChatBot.vue'
import RouteErrorBoundary from '../ui/RouteErrorBoundary.vue'
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
    desktopCollapsed.value = localStorage.getItem('vshield-sidebar-collapsed') === 'true'
    syncViewport()
    window.addEventListener('resize', syncViewport)
    startSecurityAlertPolling()
})

watch(desktopCollapsed, (value) => {
    localStorage.setItem('vshield-sidebar-collapsed', String(value))
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

.page-fade-enter-active {
    transition: opacity 0.18s ease, transform 0.18s ease;
}

.page-fade-leave-active {
    transition: opacity 0.12s ease, transform 0.12s ease;
}

.page-fade-enter-from {
    opacity: 0;
    transform: translateY(8px);
}

.page-fade-leave-to {
    opacity: 0;
    transform: translateY(-4px);
}
</style>
