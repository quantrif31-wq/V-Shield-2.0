import { mount } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import PortalThreeCore from '../PortalThreeCore.vue'

let throwRenderer = false
let rafCallbacks

vi.mock('three', () => {
  class MockScene { add() {} }
  class MockCamera {
    constructor() {
      this.position = { set: vi.fn() }
      this.aspect = 1
    }
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
  class MockGroup {
    constructor() { this.rotation = { x: 0, y: 0, z: 0 } }
    add() {}
  }
  class MockGeometry {}
  class MockMaterial {}
  class MockMesh {
    constructor() { this.rotation = { x: 0, y: 0, z: 0 } }
  }
  return {
    Scene: MockScene,
    PerspectiveCamera: MockCamera,
    WebGLRenderer: MockRenderer,
    Group: MockGroup,
    IcosahedronGeometry: MockGeometry,
    SphereGeometry: MockGeometry,
    TorusGeometry: MockGeometry,
    MeshBasicMaterial: MockMaterial,
    Mesh: MockMesh,
    BufferGeometry: class { setAttribute() {} },
    BufferAttribute: class {},
    PointsMaterial: MockMaterial,
    Points: MockMesh
  }
})

vi.mock('../../../utils/portalAudio', () => ({
  mechaAudio: { playClick: vi.fn(), playHover: vi.fn() }
}))

describe('PortalThreeCore', () => {
  beforeEach(() => {
    throwRenderer = false
    rafCallbacks = []
    window.requestAnimationFrame = vi.fn((cb) => { rafCallbacks.push(cb); return 1 })
    window.cancelAnimationFrame = vi.fn()
    Object.defineProperty(window, 'devicePixelRatio', { value: 2, writable: true, configurable: true })
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  function runRaf(count) {
    for (let i = 0; i < count; i++) {
      const cb = rafCallbacks.shift()
      if (cb) cb()
    }
  }

  it('mounts and initializes three.js', () => {
    const wrapper = mount(PortalThreeCore)
    expect(wrapper.find('div').exists()).toBe(true)
    expect(window.requestAnimationFrame).toHaveBeenCalled()
  })

  it('runs animation loop', () => {
    mount(PortalThreeCore)
    runRaf(3)
    expect(rafCallbacks.length).toBeGreaterThan(0)
  })

  it('handles mousemove and resize events', () => {
    const wrapper = mount(PortalThreeCore)
    window.dispatchEvent(new MouseEvent('mousemove', { clientX: 100, clientY: 100 }))
    window.dispatchEvent(new Event('resize'))
    wrapper.unmount()
    expect(window.cancelAnimationFrame).toHaveBeenCalled()
  })

  it('handles drag interaction, hover and resize on container', () => {
    const wrapper = mount(PortalThreeCore)
    const container = wrapper.find('div[title="Kéo chuột để xoay 3D 360 độ tự do"]')
    container.trigger('mousedown', { clientX: 50, clientY: 50 })
    window.dispatchEvent(new MouseEvent('mousemove', { clientX: 60, clientY: 55 }))
    window.dispatchEvent(new MouseEvent('mouseup'))
    container.trigger('mouseenter')
    container.trigger('mouseleave')
    window.dispatchEvent(new Event('resize'))
    runRaf(2)
    wrapper.unmount()
    expect(window.cancelAnimationFrame).toHaveBeenCalled()
  })

  it('handles renderer constructor failure gracefully', () => {
    throwRenderer = true
    const wrapper = mount(PortalThreeCore)
    wrapper.unmount()
    expect(wrapper).toBeTruthy()
  })
})
