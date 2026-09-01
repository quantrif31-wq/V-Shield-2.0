import { mount } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import PortalGlobalThreeCanvas from '../PortalGlobalThreeCanvas.vue'

let throwRenderer = false

vi.mock('three', () => {
  class MockScene { add() {} }
  class MockCamera {
    constructor() {
      this.position = { set: vi.fn(), x: 0, y: 0, z: 0 }
      this.aspect = 1
    }
    lookAt() {}
    updateProjectionMatrix() {}
  }
  class MockRenderer {
    constructor() {
      if (throwRenderer) throw new Error('no webgl')
      this.domElement = document.createElement('canvas')
    }
    setSize() {}
    setPixelRatio() {}
    render() {}
    dispose() {}
  }
  class MockPlaneGeometry {}
  class MockMeshBasicMaterial {}
  class MockMesh {
    constructor() { this.rotation = { x: 0 }; this.position = { y: 0, z: 0 } }
    add() {}
  }
  class MockBufferGeometry { setAttribute() {} }
  class MockBufferAttribute {}
  class MockPointsMaterial {}
  class MockPoints {
    constructor() { this.rotation = { y: 0, x: 0 } }
    add() {}
  }
  return {
    Scene: MockScene,
    PerspectiveCamera: MockCamera,
    WebGLRenderer: MockRenderer,
    PlaneGeometry: MockPlaneGeometry,
    MeshBasicMaterial: MockMeshBasicMaterial,
    Mesh: MockMesh,
    BufferGeometry: MockBufferGeometry,
    BufferAttribute: MockBufferAttribute,
    PointsMaterial: MockPointsMaterial,
    Points: MockPoints
  }
})

describe('PortalGlobalThreeCanvas', () => {
  let rafCallbacks

  beforeEach(() => {
    throwRenderer = false
    rafCallbacks = []
    window.requestAnimationFrame = vi.fn((cb) => { rafCallbacks.push(cb); return 1 })
    window.cancelAnimationFrame = vi.fn()
    Object.defineProperty(window, 'innerWidth', { value: 1920, writable: true, configurable: true })
    Object.defineProperty(window, 'innerHeight', { value: 1080, writable: true, configurable: true })
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('mounts and renders container div', () => {
    const wrapper = mount(PortalGlobalThreeCanvas)
    expect(wrapper.find('div').exists()).toBe(true)
  })

  it('initializes Three.js on mount', () => {
    mount(PortalGlobalThreeCanvas)
    expect(window.requestAnimationFrame).toHaveBeenCalled()
  })

  it('cleans up on unmount', () => {
    const wrapper = mount(PortalGlobalThreeCanvas)
    wrapper.unmount()
    expect(window.cancelAnimationFrame).toHaveBeenCalled()
  })

  it('handles mousemove events', () => {
    mount(PortalGlobalThreeCanvas)
    window.dispatchEvent(new MouseEvent('mousemove', { clientX: 500, clientY: 400 }))
  })

  it('handles scroll events', () => {
    window.scrollY = 100
    mount(PortalGlobalThreeCanvas)
    window.dispatchEvent(new Event('scroll'))
  })

  it('handles resize events', () => {
    mount(PortalGlobalThreeCanvas)
    window.dispatchEvent(new Event('resize'))
  })

  it('runs animation loop when callbacks are stored', () => {
    mount(PortalGlobalThreeCanvas)
    rafCallbacks.forEach(cb => cb())
  })

  it('handles WebGL renderer constructor failure gracefully', () => {
    throwRenderer = true
    const wrapper = mount(PortalGlobalThreeCanvas)
    wrapper.unmount()
    expect(wrapper).toBeTruthy()
  })
})
