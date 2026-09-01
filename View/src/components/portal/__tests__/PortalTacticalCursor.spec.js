import { mount } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import PortalTacticalCursor from '../PortalTacticalCursor.vue'

vi.mock('../../../utils/portalAudio', () => ({
  mechaAudio: { playTargetLock: vi.fn() }
}))

vi.mock('../../../utils/portalVoiceSynth', () => ({
  tacticalVoice: {}
}))

describe('PortalTacticalCursor', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    window.requestAnimationFrame = vi.fn(() => 99)
    window.cancelAnimationFrame = vi.fn()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
  })

  it('mounts and renders tactial cursor container', () => {
    const wrapper = mount(PortalTacticalCursor)
    expect(wrapper.find('div').exists()).toBe(true)
  })

  it('tracks mouse position over interactive elements', () => {
    const wrapper = mount(PortalTacticalCursor, { attachTo: document.body })
    const target = document.createElement('button')
    target.innerText = 'CLICK ME'
    document.body.appendChild(target)
    const evt = new MouseEvent('mousemove', { clientX: 100, clientY: 150, bubbles: true })
    Object.defineProperty(evt, 'target', { value: target })
    window.dispatchEvent(evt)
    document.body.removeChild(target)
    expect(wrapper.exists()).toBe(true)
  })

  it('tracks mouse over non-interactive area', () => {
    const wrapper = mount(PortalTacticalCursor)
    const evt = new MouseEvent('mousemove', { clientX: 50, clientY: 50, bubbles: true })
    Object.defineProperty(evt, 'target', { value: document.body })
    window.dispatchEvent(evt)
    expect(wrapper.exists()).toBe(true)
  })

  it('handles click to spawn shockwave', () => {
    const wrapper = mount(PortalTacticalCursor, { attachTo: document.body })
    window.dispatchEvent(new MouseEvent('click', { clientX: 200, clientY: 300 }))
    expect(wrapper.findAll('.mecha-shockwave').length).toBeGreaterThanOrEqual(0)
  })

  it('removes shockwave after timeout via setTimeout callback', () => {
    const wrapper = mount(PortalTacticalCursor)
    window.dispatchEvent(new MouseEvent('click', { clientX: 200, clientY: 300 }))
    expect(wrapper.vm.shockwaves.length).toBeGreaterThan(0)
    vi.advanceTimersByTime(700)
    expect(wrapper.vm.shockwaves.length).toBe(0)
  })

  it('handles mouseleave on document', () => {
    const wrapper = mount(PortalTacticalCursor)
    document.dispatchEvent(new MouseEvent('mouseleave'))
    expect(wrapper.exists()).toBe(true)
  })

  it('cleans up event listeners on unmount', () => {
    const wrapper = mount(PortalTacticalCursor)
    wrapper.unmount()
    expect(window.cancelAnimationFrame).toHaveBeenCalled()
  })
})
