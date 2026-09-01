import { mount } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import PortalHoloRadar from '../PortalHoloRadar.vue'

let mockCtx

describe('PortalHoloRadar', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    mockCtx = {
      clearRect: vi.fn(),
      beginPath: vi.fn(),
      arc: vi.fn(),
      fill: vi.fn(),
      stroke: vi.fn(),
      moveTo: vi.fn(),
      lineTo: vi.fn(),
      save: vi.fn(),
      restore: vi.fn(),
      translate: vi.fn(),
      setLineDash: vi.fn(),
      set fillStyle(_v) {},
      set strokeStyle(_v) {},
      set lineWidth(_v) {},
      set shadowColor(_v) {},
      set shadowBlur(_v) {},
    }
    HTMLCanvasElement.prototype.getContext = vi.fn(() => mockCtx)
    window.requestAnimationFrame = vi.fn(() => 99)
    window.cancelAnimationFrame = vi.fn()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
  })

  it('mounts and renders radar canvas', () => {
    const wrapper = mount(PortalHoloRadar)
    expect(wrapper.find('canvas').exists()).toBe(true)
    expect(wrapper.text()).toContain('3D DEFENSE MATRIX')
  })

  it('starts render loop on mount', () => {
    mount(PortalHoloRadar)
    expect(window.requestAnimationFrame).toHaveBeenCalled()
  })

  it('handles mousemove', () => {
    const wrapper = mount(PortalHoloRadar, { attachTo: document.body })
    const canvas = wrapper.find('canvas').element
    Object.defineProperty(canvas, 'getBoundingClientRect', {
      value: vi.fn(() => ({ left: 0, top: 0, width: 360, height: 360 }))
    })
    window.dispatchEvent(new MouseEvent('mousemove', { clientX: 100, clientY: 100 }))
    expect(mockCtx.clearRect).toHaveBeenCalled()
  })

  it('cleans up on unmount', () => {
    const wrapper = mount(PortalHoloRadar)
    wrapper.unmount()
    expect(window.cancelAnimationFrame).toHaveBeenCalled()
  })
})
