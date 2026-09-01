import { mount } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import PortalLaserSparksCanvas from '../PortalLaserSparksCanvas.vue'

let mockCtx
let rafCallbacks

describe('PortalLaserSparksCanvas', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    rafCallbacks = []
    mockCtx = {
      clearRect: vi.fn(),
      beginPath: vi.fn(),
      moveTo: vi.fn(),
      lineTo: vi.fn(),
      stroke: vi.fn(),
      save: vi.fn(),
      restore: vi.fn(),
      set globalAlpha(_v) {},
      set strokeStyle(_v) {},
      set lineWidth(_v) {},
      set shadowColor(_v) {},
      set shadowBlur(_v) {},
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
    const wrapper = mount(PortalLaserSparksCanvas)
    expect(wrapper.find('canvas').exists()).toBe(true)
  })

  it('starts render loop on mount', () => {
    mount(PortalLaserSparksCanvas)
    expect(window.requestAnimationFrame).toHaveBeenCalled()
  })

  it('cleans up on unmount', () => {
    const wrapper = mount(PortalLaserSparksCanvas)
    wrapper.unmount()
    expect(window.cancelAnimationFrame).toHaveBeenCalled()
  })

  it('handles click events to spawn sparks and animates them', () => {
    mount(PortalLaserSparksCanvas)
    window.dispatchEvent(new MouseEvent('click', { clientX: 500, clientY: 500 }))
    runRaf(3)
    expect(mockCtx.clearRect).toHaveBeenCalled()
    expect(mockCtx.beginPath).toHaveBeenCalled()
  })

  it('handles mousemove events', () => {
    const rndSpy = vi.spyOn(Math, 'random').mockReturnValue(0)
    mount(PortalLaserSparksCanvas)
    window.dispatchEvent(new MouseEvent('mousemove', { clientX: 300, clientY: 300 }))
    runRaf(2)
    rndSpy.mockRestore()
  })

  it('removes dead sparks via splice in render loop', () => {
    mount(PortalLaserSparksCanvas)
    window.dispatchEvent(new MouseEvent('click', { clientX: 100, clientY: 100 }))
    runRaf(100)
    expect(mockCtx.stroke).toHaveBeenCalled()
  })

  it('handles resize events', () => {
    mount(PortalLaserSparksCanvas)
    window.dispatchEvent(new Event('resize'))
    expect(mockCtx.clearRect).toHaveBeenCalled()
  })
})
