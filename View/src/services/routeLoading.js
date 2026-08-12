import { reactive } from 'vue'

const state = reactive({
    active: false,
    progress: 0,
})

let frameTimer = null
let hideTimer = null

function tick() {
    const increment = Math.max(3, Math.round((92 - state.progress) * 0.14))
    state.progress = Math.min(92, state.progress + increment)
}

function finish() {
    if (frameTimer) {
        window.clearInterval(frameTimer)
        frameTimer = null
    }
    state.progress = 100
    hideTimer = window.setTimeout(() => {
        state.active = false
        state.progress = 0
    }, 420)
}

export function installRouteProgress(router) {
    router.beforeEach(() => {
        window.clearTimeout(hideTimer)
        if (frameTimer) {
            window.clearInterval(frameTimer)
        }
        state.active = true
        state.progress = 6
        frameTimer = window.setInterval(tick, 90)
    })
    router.afterEach(() => finish())
    router.onError(() => finish())
}

export const routeProgress = state