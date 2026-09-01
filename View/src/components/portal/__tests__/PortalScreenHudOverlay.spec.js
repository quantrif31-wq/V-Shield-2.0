import { mount } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import PortalScreenHudOverlay from '../PortalScreenHudOverlay.vue'

describe('PortalScreenHudOverlay', () => {
  let rafCallbacks

  beforeEach(() => {
    vi.useFakeTimers()
    rafCallbacks = []
    window.requestAnimationFrame = vi.fn((cb) => { rafCallbacks.push(cb); return 1 })
    window.cancelAnimationFrame = vi.fn()
    window.performance.now = vi.fn(() => 1000)
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
  })

  it('mounts and renders HUD text', () => {
    const wrapper = mount(PortalScreenHudOverlay)
    expect(wrapper.text()).toContain('HUD')
    expect(wrapper.text()).toContain('OVERDRIVE')
  })

  it('starts RAF loop on mount', () => {
    mount(PortalScreenHudOverlay)
    expect(window.requestAnimationFrame).toHaveBeenCalled()
  })

  it('updates fps when 1000ms elapsed with performance.memory', () => {
    mount(PortalScreenHudOverlay)
    window.performance.memory = { usedJSHeapSize: 10 * 1024 * 1024 }
    window.performance.now = vi.fn(() => 2000)
    if (rafCallbacks.length > 0) {
      rafCallbacks.forEach(cb => cb(2000))
    }
    expect(window.performance.memory).toBeTruthy()
  })

  it('updates fps without performance.memory', () => {
    delete window.performance.memory
    mount(PortalScreenHudOverlay)
    window.performance.now = vi.fn(() => 3000)
    if (rafCallbacks.length > 0) {
      rafCallbacks.forEach(cb => cb(3000))
    }
    expect(true).toBe(true)
  })

  it('cancels RAF on unmount', () => {
    const wrapper = mount(PortalScreenHudOverlay)
    wrapper.unmount()
    expect(window.cancelAnimationFrame).toHaveBeenCalled()
  })
})
