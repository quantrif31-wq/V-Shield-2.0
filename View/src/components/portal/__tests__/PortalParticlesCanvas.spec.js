import { mount } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import PortalParticlesCanvas from '../PortalParticlesCanvas.vue'

let mockCtx
let rafCallbacks

describe('PortalParticlesCanvas', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    rafCallbacks = []
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
      set lineStyle(_v) {},
      set fillStyle(_v) {},
      set lineWidth(_v) {},
      set shadowBlur(_v) {},
      set shadowColor(_v) {},
      set globalAlpha(_v) {},
    }
    HTMLCanvasElement.prototype.getContext = vi.fn(() => mockCtx)
    window.requestAnimationFrame = vi.fn((cb) => { rafCallbacks.push(cb); return rafCallbacks.length })
    window.cancelAnimationFrame = vi.fn()
    Object.defineProperty(window, 'innerWidth', { value: 1920, writable: true, configurable: true })
    Object.defineProperty(window, 'innerHeight', { value: 1080, writable: true, configurable: true })
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
  })

  function runRaf(count) {
    for (let i = 0; i < count; i++) {
      const cb = rafCallbacks.shift()
      if (cb) cb()
    }
  }

  it('mounts and renders canvas', () => {
    const wrapper = mount(PortalParticlesCanvas)
    expect(wrapper.find('canvas').exists()).toBe(true)
    expect(wrapper.find('canvas').classes()).toContain('pointer-events-none')
  })

  it('starts animation on mount', () => {
    mount(PortalParticlesCanvas)
    expect(window.requestAnimationFrame).toHaveBeenCalled()
  })

  it('cancels animation frame on unmount', () => {
    const wrapper = mount(PortalParticlesCanvas)
    wrapper.unmount()
    expect(window.cancelAnimationFrame).toHaveBeenCalled()
  })

  it('handles resize event', () => {
    mount(PortalParticlesCanvas)
    window.dispatchEvent(new Event('resize'))
    runRaf(1)
    expect(mockCtx.clearRect).toHaveBeenCalled()
  })

  it('handles mousemove and mouseleave events with active mouse interaction', () => {
    mount(PortalParticlesCanvas)
    window.dispatchEvent(new MouseEvent('mousemove', { clientX: 500, clientY: 500 }))
    runRaf(2)
    window.dispatchEvent(new MouseEvent('mouseleave'))
    runRaf(1)
    expect(mockCtx.clearRect).toHaveBeenCalled()
    expect(mockCtx.fill).toHaveBeenCalled()
  })
})
