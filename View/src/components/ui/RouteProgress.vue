<template>
    <transition name="progress-fade">
        <div v-if="state.active" class="route-progress" role="progressbar" aria-label="Đang tải trang" aria-busy="true">
            <div class="route-progress__fill" :style="{ width: `${state.progress}%` }">
                <span class="route-progress__glow"></span>
            </div>
        </div>
    </transition>
</template>

<script setup>
import { routeProgress as state } from '../../services/routeLoading'
</script>

<style scoped>
.route-progress {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    height: 3px;
    z-index: 1400;
    background: rgba(15, 124, 130, 0.12);
    pointer-events: none;
}

.route-progress__fill {
    position: relative;
    height: 100%;
    background: linear-gradient(90deg, var(--glow-500), var(--teal-500), var(--steel-500));
    background-size: 220% 100%;
    animation: progress-shimmer 1.1s linear infinite;
    box-shadow: 0 0 12px rgba(84, 196, 211, 0.75);
    transition: width 120ms ease;
}

.route-progress__glow {
    position: absolute;
    inset: 0;
    border-radius: 999px;
    filter: blur(6px);
    background: linear-gradient(90deg, transparent, rgba(184, 247, 255, 0.9), transparent);
    transform: translateX(-120%);
    animation: progress-sweep 1.4s ease-in-out infinite;
}

@keyframes progress-shimmer {
    0% {
        background-position: 0% 0;
    }
    100% {
        background-position: -220% 0;
    }
}

@keyframes progress-sweep {
    0%,
    100% {
        transform: translateX(-120%);
    }
    50% {
        transform: translateX(220%);
    }
}

.progress-fade-enter-active {
    transition: opacity 0.12s ease;
}

.progress-fade-leave-active {
    transition: opacity 0.42s ease;
}

.progress-fade-enter-from,
.progress-fade-leave-to {
    opacity: 0;
}
</style>