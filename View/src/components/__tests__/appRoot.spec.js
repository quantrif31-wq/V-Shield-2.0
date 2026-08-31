import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../router', () => ({ default: { isReady: vi.fn().mockResolvedValue() } }))
vi.mock('../../components/ui/ToastProvider.vue', () => ({ default: { template: '<div class="stub-toast" />' } }))
vi.mock('../../components/ui/RouteProgress.vue', () => ({ default: { template: '<div class="stub-progress" />' } }))
vi.mock('../../components/Call/GlobalCallOverlay.vue', () => ({ default: { template: '<div class="stub-call-overlay" />' } }))

const router = (await import('../../router')).default
const App = (await import('../../App.vue')).default

beforeEach(() => {
    vi.clearAllMocks()
    router.isReady.mockResolvedValue()
})

describe('App', () => {
    it('renders router-view, route progress, call overlay and toasts directly', async () => {
        const wrapper = mount(App, { global: { stubs: { RouterView: { template: '<div class="mw-view" />' } } } })
        await flushPromises()
        expect(wrapper.find('.stub-progress').exists()).toBe(true)
        expect(wrapper.find('.stub-toast').exists()).toBe(true)
        expect(wrapper.find('.stub-call-overlay').exists()).toBe(true)
    })
})