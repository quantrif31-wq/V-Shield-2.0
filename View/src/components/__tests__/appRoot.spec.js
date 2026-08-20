import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../router', () => ({ default: { isReady: vi.fn().mockResolvedValue() } }))
vi.mock('../../components/ui/ToastProvider.vue', () => ({ default: { template: '<div class="stub-toast" />' } }))
vi.mock('../../components/ui/RouteProgress.vue', () => ({ default: { template: '<div class="stub-progress" />' } }))

import { mount as _m } from '@vue/test-utils'
const router = (await import('../../router')).default
const App = (await import('../../App.vue')).default

beforeEach(() => {
    vi.clearAllMocks()
    router.isReady.mockResolvedValue()
})

afterEach(() => {
    vi.useRealTimers()
})

describe('App', () => {
    it('shows boot splash initially and hides after ready', async () => {
        vi.useFakeTimers()
        const wrapper = mount(App, { global: { stubs: { RouterView: true, transition: false, Transition: false } } })
        expect(wrapper.find('.boot-splash').exists()).toBe(true)
        await vi.advanceTimersByTimeAsync(300)
        await flushPromises()
        expect(wrapper.find('.boot-splash').exists()).toBe(false)
    })

    it('boots even if router.isReady rejects', async () => {
        vi.useFakeTimers()
        router.isReady.mockRejectedValue(new Error('nav'))
        const wrapper = mount(App, { global: { stubs: { RouterView: true, transition: false, Transition: false } } })
        await vi.advanceTimersByTimeAsync(300)
        await flushPromises()
        expect(wrapper.find('.boot-splash').exists()).toBe(false)
    })

    it('renders router-view, route progress and toasts', async () => {
        const wrapper = mount(App, { global: { stubs: { RouterView: { template: '<div class="mw-view" />' }, transition: false, Transition: false } } })
        await flushPromises()
        expect(wrapper.find('.stub-progress').exists()).toBe(true)
        expect(wrapper.find('.stub-toast').exists()).toBe(true)
    })
})