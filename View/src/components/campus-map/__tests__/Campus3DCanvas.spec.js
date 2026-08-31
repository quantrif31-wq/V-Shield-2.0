import { flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import Campus3DCanvas from '../Campus3DCanvas.vue'

const t = vi.hoisted(() => {
  const registry = { emptyBox: false, zeroDirection: false }

  function Vector2(x = 0, y = 0) {
    this.x = x
    this.y = y
  }
  Vector2.prototype.set = function (x, y) {
    this.x = x
    this.y = y
    return this
  }

  function Vector3(x = 0, y = 0, z = 0) {
    this.x = x
    this.y = y
    this.z = z
  }
  Vector3.prototype.set = function (x, y, z) {
    this.x = x
    this.y = y
    this.z = z
    return this
  }
  Vector3.prototype.copy = function (v) {
    this.x = v.x
    this.y = v.y
    this.z = v.z
    return this
  }
  Vector3.prototype.add = function (v) {
    this.x += v.x
    this.y += v.y
    this.z += v.z
    return this
  }
  Vector3.prototype.sub = function (v) {
    this.x -= v.x
    this.y -= v.y
    this.z -= v.z
    return this
  }
  Vector3.prototype.lerp = function (v, lev) {
    this.x += (v.x - this.x) * lev
    this.y += (v.y - this.y) * lev
    this.z += (v.z - this.z) * lev
    return this
  }
  Vector3.prototype.normalize = function () {
    return this
  }
  Vector3.prototype.multiplyScalar = function (s) {
    this.x *= s
    this.y *= s
    this.z *= s
    return this
  }
  Vector3.prototype.crossVectors = function (a, b) {
    this.x = a.y * b.z - a.z * b.y
    this.y = a.z * b.x - a.x * b.z
    this.z = a.x * b.y - a.y * b.x
    return this
  }
  Vector3.prototype.lengthSq = function () {
    return this.x * this.x + this.y * this.y + this.z * this.z
  }
  Vector3.prototype.setScalar = function (s) {
    this.x = s
    this.y = s
    this.z = s
    return this
  }
  Vector3.prototype.clone = function () {
    return new Vector3(this.x, this.y, this.z)
  }

  function makeColor(hex = 0xffffff) {
    const c = {
      hex,
      setHex(h) {
        c.hex = h
        return c
      },
      getHex() {
        return c.hex
      },
      setHSL() {
        return c
      },
    }
    return c
  }

  function makeMaterial(props) {
    const p = props || {}
    return {
      color: makeColor(typeof p.color === 'number' ? p.color : 0xffffff),
      emissive: makeColor(0),
      emissiveIntensity: 0,
      opacity: p.transparent ? (Number.isFinite(p.opacity) ? p.opacity : 1) : 1,
      transparent: !!p.transparent,
      map: null,
      visible: true,
      dispose() {},
      clone() {
        return makeMaterial(props)
      },
    }
  }

  function makeObject3D() {
    const o = {
      type: 'Object3D',
      children: [],
      userData: {},
      parent: null,
      position: new Vector3(),
      rotation: new Vector3(),
      scale: new Vector3(1, 1, 1),
      visible: true,
      isMesh: false,
      castShadow: false,
      receiveShadow: false,
      geometry: null,
      material: null,
      add(child) {
        if (child) {
          child.parent = o
          o.children.push(child)
        }
        return o
      },
      remove(child) {
        const i = o.children.indexOf(child)
        if (i >= 0) o.children.splice(i, 1)
        return o
      },
      clear() {
        o.children.length = 0
      },
      traverse(cb) {
        const stack = [o]
        while (stack.length) {
          const node = stack.pop()
          cb(node)
          for (let i = node.children.length - 1; i >= 0; i -= 1) stack.push(node.children[i])
        }
      },
      getWorldDirection(target) {
        if (target) target.set(0, 0, registry.zeroDirection ? 0 : -1)
        return target
      },
      clone() {
        return o
      },
    }
    return o
  }

  function makeGeometry(type) {
    return {
      type,
      dispose() {},
      setFromPoints() {
        return this
      },
      attributes: {},
    }
  }

  function Mesh(geometry, material) {
    const o = makeObject3D()
    o.isMesh = true
    o.geometry = geometry || makeGeometry('MeshGeometry')
    o.material = material || makeMaterial()
    return o
  }

  function Line(geometry, material) {
    const o = makeObject3D()
    o.geometry = geometry || makeGeometry('LineGeometry')
    o.material = material || makeMaterial()
    return o
  }

  function LineLoop(geometry, material) {
    const o = makeObject3D()
    o.geometry = geometry || makeGeometry('LineLoopGeometry')
    o.material = material || makeMaterial()
    return o
  }

  function Sprite(material) {
    const o = makeObject3D()
    o.material = material || makeMaterial()
    return o
  }

  function Scene() {
    const o = makeObject3D()
    o.type = 'Scene'
    o.background = null
    o.fog = null
    return o
  }

  function Group() {
    const o = makeObject3D()
    o.type = 'Group'
    return o
  }

  function PerspectiveCamera(fov, aspect, near, far) {
    const o = makeObject3D()
    o.fov = fov
    o.aspect = aspect
    o.near = near
    o.far = far
    o.updateProjectionMatrix = function () {}
    return o
  }

  function Raycaster() {
    const r = {
      hits: [],
      setFromCamera() {},
      intersectObjects() {
        return r.hits
      },
    }
    return r
  }

  function Box3() {
    const b = {
      isEmpty() {
        return registry.emptyBox
      },
      getSize(out) {
        if (out) out.set(100, 120, 100)
        return out
      },
      getCenter(out) {
        if (out) out.set(0, 0, 0)
        return out
      },
      setFromObject() {
        return b
      },
    }
    return b
  }

  function WebGLRenderer() {
    const r = {
      domElement: {
        style: {},
        addEventListener() {},
        removeEventListener() {},
        getBoundingClientRect() {
          return { left: 0, top: 0, width: 800, height: 600 }
        },
        remove() {},
      },
      shadowMap: {},
      toneMapping: 0,
      toneMappingExposure: 1,
      outputColorSpace: 0,
      setSize() {},
      setPixelRatio() {},
      render() {},
      dispose() {},
    }
    return r
  }

  function OrbitControls() {
    const o = {
      target: new Vector3(),
      enableDamping: false,
      dampingFactor: 0,
      enablePan: false,
      screenSpacePanning: false,
      panSpeed: 0,
      zoomSpeed: 0,
      mouseButtons: {},
      touches: {},
      maxPolarAngle: 0,
      minDistance: 0,
      maxDistance: 0,
      update() {},
    }
    return o
  }

  function AmbientLight() {
    return makeObject3D()
  }
  function HemisphereLight() {
    return makeObject3D()
  }
  function DirectionalLight(color, intensity) {
    const o = makeObject3D()
    o.intensity = intensity
    o.castShadow = false
    o.shadow = { mapSize: {}, camera: { left: 0, right: 0, top: 0, bottom: 0, near: 0, far: 0 } }
    return o
  }
  function PointLight() {
    return makeObject3D()
  }

  function BufferGeometry() {
    return makeGeometry('BufferGeometry')
  }
  function BoxGeometry() {
    return makeGeometry('BoxGeometry')
  }
  function SphereGeometry() {
    return makeGeometry('SphereGeometry')
  }
  function PlaneGeometry() {
    return makeGeometry('PlaneGeometry')
  }
  function RingGeometry() {
    return makeGeometry('RingGeometry')
  }
  function CircleGeometry() {
    return makeGeometry('CircleGeometry')
  }
  function CylinderGeometry() {
    return makeGeometry('CylinderGeometry')
  }
  function GridHelper() {
    return makeObject3D()
  }
  function AxesHelper() {
    return makeObject3D()
  }

  function MeshBasicMaterial(p) {
    return makeMaterial(p)
  }
  function MeshStandardMaterial(p) {
    return makeMaterial(p)
  }
  function MeshPhysicalMaterial(p) {
    return makeMaterial(p)
  }
  function LineBasicMaterial(p) {
    return makeMaterial(p)
  }
  function SpriteMaterial(p) {
    return makeMaterial(p)
  }

  function CanvasTexture(image) {
    return { image, needsUpdate: false, dispose() {} }
  }
  function FogExp2(color, density) {
    return { color, density }
  }
  function Color(hex) {
    return makeColor(hex)
  }

  return {
    __registry: registry,
    Vector2,
    Vector3,
    Color,
    Box3,
    Raycaster,
    PerspectiveCamera,
    WebGLRenderer,
    Scene,
    Group,
    Mesh,
    Line,
    LineLoop,
    Sprite,
    AmbientLight,
    HemisphereLight,
    DirectionalLight,
    PointLight,
    BufferGeometry,
    BoxGeometry,
    SphereGeometry,
    PlaneGeometry,
    RingGeometry,
    CircleGeometry,
    CylinderGeometry,
    GridHelper,
    AxesHelper,
    MeshBasicMaterial,
    MeshStandardMaterial,
    MeshPhysicalMaterial,
    LineBasicMaterial,
    SpriteMaterial,
    CanvasTexture,
    FogExp2,
    MOUSE: { ROTATE: 0, DOLLY: 1, PAN: 2 },
    TOUCH: { ROTATE: 0, DOLLY_PAN: 1 },
    PCFShadowMap: 0,
    ACESFilmicToneMapping: 0,
    SRGBColorSpace: 0,
    BackSide: 0,
    DoubleSide: 1,
    OrbitControls,
  }
})

vi.mock('three', () => t)
vi.mock('three/addons/controls/OrbitControls.js', () => ({
  default: t.OrbitControls,
  OrbitControls: t.OrbitControls,
}))

let rafQueue = []

function vmctx(wrapper) {
  return wrapper.vm.$.ctx
}

async function settle() {
  await nextTick()
  await flushPromises()
  await new Promise((resolve) => setTimeout(resolve, 0))
}

function pumpFrames(n) {
  for (let i = 0; i < n; i += 1) {
    const cb = rafQueue.shift()
    if (typeof cb === 'function') cb()
  }
}

function makeProps(overrides = {}) {
  return {
    sites: [
      {
        siteId: 1,
        name: 'Khu A',
        code: 'A',
        objects: [
          { id: 'B1', label: 'Toa A1', type: 'Building', floors: 4, posX: 0, posZ: 0, width: 20, length: 16, height: 14, rotation: 0, properties: JSON.stringify({ level: 'Critical', zone: 'Trong nha' }) },
          { id: 'GATE-1', label: 'Cong 1 GATE-1', type: 'GateMarker', posX: 14, posZ: 10, width: 6, length: 2, height: 3.6, properties: JSON.stringify({}) },
          { id: 'P1', label: 'Bai xe A', type: 'ParkingArea', posX: -8, posZ: 8, width: 20, length: 14, properties: { level: 'Normal' } },
          { id: 'PATH-1', label: 'Tuyen A', type: 'Path', posX: 0, posZ: -12, width: 8, length: 30, properties: null },
          { id: 'LAND-1', label: 'Moc A', type: 'Landmark', posX: -14, posZ: -10, properties: {} },
          { id: 'B2', label: 'Toa A2', type: 'Building', floors: 1, posX: 6, posZ: 4, width: 12, length: 10, height: 8, properties: '{bad json' },
        ],
      },
      {
        siteId: 2,
        name: 'Khu B',
        code: 'B',
        objects: [
          { id: 'B3', label: 'Toa B1', type: 'Building', floors: 2, posX: 4, posZ: 2, width: 18, length: 14, height: 10, properties: { level: 'Restricted', zone: 'Ngoai vi' } },
          { id: 'B4', label: 'Toa B2', type: 'Building', floors: 3, posX: -6, posZ: 4, width: 16, length: 12, height: 12, properties: {} },
          { id: 'GATE-2', label: 'Cong 2 GATE-2', type: 'GateMarker', posX: -16, posZ: -8, width: 6, height: 3.2, properties: {} },
        ],
      },
      {
        siteId: 3,
        name: 'Khu C',
        code: 'C',
        objects: [
          { id: 'GATE-3', label: 'Cong 3 GATE-3', type: 'GateMarker', posX: 0, posZ: 0, width: 6, height: 3, properties: {} },
        ],
      },
      {
        siteId: 4,
        name: 'Khu D',
        code: 'D',
        objects: [
          { id: 'GATE-4', label: 'Cong 4 GATE-4', type: 'GateMarker', posX: 0, posZ: 0, width: 5, height: 3, properties: {} },
          { id: 'GATE-5', label: 'Cong 5', type: 'GateMarker', posX: 10, posZ: 0, width: 5, height: 3, properties: {} },
        ],
      },
      {
        siteId: 5,
        name: 'Khu E',
        code: 'E',
        objects: [],
      },
    ],
    gates: [
      { gateId: 101, gateName: 'GATE-1', status: 'Active', cameraCount: 3, offlineCameraCount: 1, recentAccessCount: 12, lastAccessAt: '2026-08-30T10:05:00Z' },
      { gateId: 102, gateName: 'GATE-2', status: 'Warning', cameraCount: 2, offlineCameraCount: 0, recentAccessCount: 5, lastAccessAt: null },
      { gateId: 103, gateName: 'GATE-3', status: 'Offline', cameraCount: 4, offlineCameraCount: 2, recentAccessCount: 0 },
      { gateId: 104, gateName: 'GATE-4', status: 'Alarm', cameraCount: 1, offlineCameraCount: 0, recentAccessCount: 10 },
    ],
    recentEvents: [
      { gateName: 'GATE-1', direction: 'Vao' },
      { gateName: 'GATE-1' },
      { gateName: 'GATE-2' },
      { noGateName: true },
      { gateName: 'NONE' },
    ],
    selectedGateId: null,
    ...overrides,
  }
}

function findMesh(group, predicate) {
  let found = null
  group.traverse((child) => {
    if (found === null && predicate(child)) found = child
  })
  return found
}

beforeEach(() => {
  rafQueue = []
  window.requestAnimationFrame = (cb) => {
    rafQueue.push(cb)
    return rafQueue.length
  }
  window.cancelAnimationFrame = vi.fn()
})

afterEach(() => {
  vi.clearAllMocks()
})

describe('initialization and lifecycle', () => {
  it('initializes the scene, hides the loading label and starts the render loop', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    expect(wrapper.text()).toContain('Đang khởi tạo 3D...')
    expect(wrapper.vm.$.data.initialized).toBe(false)

    await settle()

    const vm = vmctx(wrapper)
    expect(vm.initialized).toBe(true)
    expect(wrapper.text()).not.toContain('Đang khởi tạo')
    expect(vm.scene).toBeTruthy()
    expect(vm.camera).toBeTruthy()
    expect(vm.renderer).toBeTruthy()
    expect(vm.controls).toBeTruthy()
    expect(vm.controls.enableDamping).toBe(true)
    expect(vm.siteMeshes.size).toBe(5)
    expect(vm.gateMeshes.size).toBe(5)
    expect(vm.siteCards).toHaveLength(5)
    expect(rafQueue.length).toBe(1)

    pumpFrames(1)
    expect(rafQueue.length).toBe(1)
    wrapper.unmount()
  })

  it('guards every interaction before the renderer is ready', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    const vm = vmctx(wrapper)
    const ev = { clientX: 10, clientY: 10 }
    vm.onPointerMove(ev)
    vm.onClick(ev)
    vm.onMouseEnter()
    vm.onMouseLeave()
    vm.onResize()
    vm.updateKeyboardMove()
    vm.rebuildWorld()
    await settle()
    wrapper.unmount()
  })

  it('disposes cleanly when unmounted before initialization completes', () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    const vm = vmctx(wrapper)
    vm.clearHoverState()
    wrapper.unmount()
    vm.initScene()
    vm.onResize()
    vm.onMouseEnter()
    vm.onPointerMove({ clientX: 1, clientY: 1 })
  })

  it('disposes renderer resources on unmount after a full init', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    await settle()
    const vm = vmctx(wrapper)
    const disposeSpy = vi.fn()
    vm.renderer.dispose = disposeSpy
    vm.renderer.domElement.remove = vi.fn()
    wrapper.unmount()
    expect(disposeSpy).toHaveBeenCalledTimes(1)
    expect(vm.renderer.domElement.remove).toHaveBeenCalledTimes(1)
    expect(window.cancelAnimationFrame).toHaveBeenCalled()
  })
})

describe('site cards', () => {
  it('computes site cards and renders chips with warning/critical classes', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    await settle()

    expect(wrapper.findAll('.c3d-site-chip')).toHaveLength(5)
    const vm = vmctx(wrapper)
    const card = vm.siteCards[0]
    expect(card.buildingCount).toBe(2)
    expect(card.gateCount).toBe(1)
    expect(card.criticalCount).toBe(1)
    expect(card.warningGateCount).toBe(0)

    expect(vm.siteCards[1].warningGateCount).toBe(1)
    expect(vm.siteCards[4].buildingCount).toBe(0)

    expect(wrapper.findAll('.c3d-site-chip')[0].find('.site-chip-dot').classes()).toContain('critical')
    expect(wrapper.findAll('.c3d-site-chip')[1].find('.site-chip-dot').classes()).toContain('warning')

    await wrapper.findAll('.c3d-site-chip')[0].trigger('click')
    expect(wrapper.emitted('inspect-object').at(-1)[0].objectType).toBe('Site')
    expect(vm.selectedSiteId).toBe(1)
    expect(wrapper.findAll('.c3d-site-chip')[0].classes()).toContain('active')
  })
})

describe('hover, pointer and tooltip interactions', () => {
  it('shows a tooltip on pointer move over an object and clears on empty space', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    await settle()
    const vm = vmctx(wrapper)

    const siteGroup = vm.siteMeshes.get(2)
    vm.raycaster.hits = [{ object: siteGroup }]
    vm.onPointerMove({ clientX: 200, clientY: 150 })

    expect(vm.tooltip.visible).toBe(true)
    expect(vm.tooltip.glyphClass).toBe('glyph-site')
    expect(vm.tooltip.meta).toHaveLength(1)
    expect(vm.tooltip.signals).toHaveLength(2)
    expect(vm.tooltipStyle.top).toBe('162px')
    expect(vm.renderer.domElement.style.cursor).toBe('pointer')
    expect(vm.hoveredObject).toBe(siteGroup)

    const hover = wrapper.emitted('hover-object')
    expect(hover.at(-1)[0].objectType).toBe('Site')
    expect(hover.at(-1)[0].metrics).toBeTruthy()

    await nextTick()
    expect(wrapper.find('.c3d-tooltip').exists()).toBe(true)
    expect(wrapper.find('.c3d-tooltip').classes()).toContain('tone-neutral')
    expect(wrapper.find('.c3d-signal-pill').exists()).toBe(true)
    expect(wrapper.find('.c3d-meta').exists()).toBe(true)

    vm.applyHoverState(siteGroup)
    vm.applyHoverState(siteGroup)
    vm.clearHoverState()

    vm.raycaster.hits = []
    vm.onPointerMove({ clientX: 200, clientY: 150 })
    expect(vm.tooltip.visible).toBe(false)
    expect(wrapper.emitted('hover-object').at(-1)[0]).toBe(null)
    expect(vm.renderer.domElement.style.cursor).toBe('default')
  })

  it('renders a full gate tooltip with status, signals and meta', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    await settle()
    const vm = vmctx(wrapper)

    const gateGroup = vm.gateMeshes.get('GATE-1')
    vm.raycaster.hits = [{ object: findMesh(gateGroup, (c) => c.isMesh) }]
    vm.onPointerMove({ clientX: 100, clientY: 100 })

    expect(vm.tooltip.visible).toBe(true)
    expect(vm.tooltip.tone).toBe('tone-active')
    expect(vm.tooltip.status).toBe('Active')
    expect(vm.tooltip.signals).toHaveLength(3)
    expect(vm.tooltip.meta).toHaveLength(2)
    expect(vm.tooltip.waveLevel).toBe(3)

    await nextTick()
    expect(wrapper.findAll('.c3d-signal-pill')).toHaveLength(3)
    expect(wrapper.findAll('.c3d-meta')).toHaveLength(2)
    expect(wrapper.findAll('.wave-bar.active')).toHaveLength(3)
    expect(wrapper.find('.c3d-status-badge').text()).toContain('Active')
    expect(wrapper.find('.c3d-tooltip').classes()).toContain('tone-active')
  })

  it('builds tooltip payloads for every object type and gate status', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    await settle()
    const vm = vmctx(wrapper)

    const site = vm.buildTooltipPayload({ objectType: 'Site', label: 'Khu', siteCode: 'A', siteName: 'Khu A', metrics: { buildings: 2, gates: 5 } })
    expect(site.glyphClass).toBe('glyph-site')
    expect(site.waveLevel).toBe(5)
    expect(site.meta).toEqual(['Cụm vận hành / zone tổng hợp'])

    const buildingCritical = vm.buildTooltipPayload({ objectType: 'Building', label: 'Toa', floors: 4, siteCode: 'A', siteName: 'Khu A', dimensions: { width: 20, length: 16, height: 14 }, properties: { level: 'Critical', zone: 'Trong nha' } })
    expect(buildingCritical.tone).toBe('tone-danger')
    expect(buildingCritical.waveLevel).toBe(5)
    expect(buildingCritical.signals).toHaveLength(3)
    expect(buildingCritical.meta).toHaveLength(2)

    const buildingRestricted = vm.buildTooltipPayload({ objectType: 'Building', label: 'Toa', floors: 2, dimensions: { width: 18, length: 14 }, properties: { level: 'Restricted' } })
    expect(buildingRestricted.tone).toBe('tone-warn')
    expect(buildingRestricted.waveLevel).toBe(4)

    const buildingNormal = vm.buildTooltipPayload({ objectType: 'Building', label: 'Toa', dimensions: { width: 16, length: 12 }, properties: {} })
    expect(buildingNormal.tone).toBe('tone-calm')
    expect(buildingNormal.waveLevel).toBe(3)

    const gateActive = vm.buildTooltipPayload({ objectType: 'GateMarker', label: 'Cong 1 GATE-1', floors: 1, siteCode: 'A', siteName: 'Khu A' })
    expect(gateActive.tone).toBe('tone-active')
    expect(gateActive.status).toBe('Active')
    expect(gateActive.waveLevel).toBe(3)
    expect(gateActive.meta).toHaveLength(2)
    expect(gateActive.signals).toHaveLength(3)

    const gateWarning = vm.buildTooltipPayload({ objectType: 'GateMarker', label: 'Cong 2 GATE-2' })
    expect(gateWarning.tone).toBe('tone-warn')
    expect(gateWarning.waveLevel).toBe(4)

    const gateOffline = vm.buildTooltipPayload({ objectType: 'GateMarker', label: 'Cong 3 GATE-3' })
    expect(gateOffline.tone).toBe('tone-danger')
    expect(gateOffline.waveLevel).toBe(5)

    const gateAlarm = vm.buildTooltipPayload({ objectType: 'GateMarker', label: 'Cong 4 GATE-4' })
    expect(gateAlarm.tone).toBe('tone-danger')
    expect(gateAlarm.waveLevel).toBe(5)

    const gateNoMatch = vm.buildTooltipPayload({ objectType: 'GateMarker', label: 'Cong 5' })
    expect(gateNoMatch.tone).toBe('tone-calm')
    expect(gateNoMatch.waveLevel).toBe(2)
    expect(gateNoMatch.status).toBe('')
    expect(gateNoMatch.signals).toHaveLength(0)

    const parking = vm.buildTooltipPayload({ objectType: 'ParkingArea', label: 'Bai xe', dimensions: { width: 20, length: 14 } })
    expect(parking.glyphClass).toBe('glyph-parking')
    expect(parking.signals).toHaveLength(2)
    expect(parking.tone).toBe('tone-neutral')

    const path = vm.buildTooltipPayload({ objectType: 'Path', label: 'Tuyen' })
    expect(path.glyphClass).toBe('glyph-path')
    expect(path.tone).toBe('tone-calm')
    expect(path.waveLevel).toBe(3)

    const landmark = vm.buildTooltipPayload({ objectType: 'Landmark', label: 'Moc' })
    expect(landmark.glyphClass).toBe('glyph-landmark')
    expect(landmark.waveLevel).toBe(1)

    const fallback = vm.buildTooltipPayload({ objectType: 'RecCenter', label: '' })
    expect(fallback.detail).toBe('RecCenter')
    expect(fallback.tone).toBe('tone-neutral')
    expect(fallback.signals).toHaveLength(0)
  })

  it('keeps empty geometry from emitting hover info', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    await settle()
    const vm = vmctx(wrapper)
    vm.emitHover(null)
    expect(wrapper.emitted('hover-object').at(-1)[0]).toBe(null)
    expect(vm.findObjectFromHit(null)).toBe(null)
    expect(vm.findObjectFromHit({ userData: { objectType: 'Building' } })).toMatchObject({ userData: { objectType: 'Building' } })
  })

  it('detaches and reattaches pointer listeners via mouse enter/leave', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    await settle()
    const vm = vmctx(wrapper)
    const addSpy = vi.fn()
    const removeSpy = vi.fn()
    vm.renderer.domElement.addEventListener = addSpy
    vm.renderer.domElement.removeEventListener = removeSpy

    await wrapper.trigger('mouseenter')
    expect(addSpy).toHaveBeenCalledWith('pointermove', vm.onPointerMove)
    expect(addSpy).toHaveBeenCalledWith('click', vm.onClick)

    vm.applyHoverState(vm.objectRecords[0])

    await wrapper.trigger('mouseleave')
    expect(removeSpy).toHaveBeenCalledWith('pointermove', vm.onPointerMove)
    expect(removeSpy).toHaveBeenCalledWith('click', vm.onClick)
    expect(vm.tooltip.visible).toBe(false)
    expect(vm.hoveredObject).toBe(null)
    expect(wrapper.emitted('hover-object').at(-1)[0]).toBe(null)
  })
})

describe('raycast click behaviours', () => {
  it('focuses sites, selects gates, frames generic objects and ignores empty hits', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    await settle()
    const vm = vmctx(wrapper)
    const ev = { clientX: 30, clientY: 40 }

    vm.raycaster.hits = [{ object: vm.siteMeshes.get(1) }]
    vm.onClick(ev)
    expect(vm.selectedSiteId).toBe(1)
    expect(wrapper.emitted('inspect-object').at(-1)[0].objectType).toBe('Site')

    vm.raycaster.hits = [{ object: vm.gateMeshes.get('GATE-2') }]
    vm.onClick(ev)
    expect(wrapper.emitted('select-gate').at(-1)[0]).toBe(102)
    expect(wrapper.emitted('inspect-object').at(-1)[0]).toMatchObject({ objectType: 'GateMarker', gate: { gateId: 102 } })

    vm.raycaster.hits = [{ object: vm.gateMeshes.get('GATE-5') }]
    vm.onClick(ev)
    expect(wrapper.emitted('select-gate')).toHaveLength(1)

    const building = vm.objectRecords.find((r) => r.userData.objectType === 'Building')
    vm.raycaster.hits = [{ object: findMesh(building, (c) => c.isMesh) }]
    vm.onClick(ev)
    expect(wrapper.emitted('inspect-object').at(-1)[0].objectType).toBe('Building')

    vm.raycaster.hits = []
    vm.onClick(ev)
    expect(wrapper.emitted('inspect-object')).toHaveLength(4)
  })
})

describe('watchers', () => {
  it('runs updateGateStatus when gates change', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    await settle()
    const vm = vmctx(wrapper)
    await wrapper.setProps({ gates: makeProps().gates.map((g) => ({ ...g })) })

    const glow1 = findMesh(vm.gateMeshes.get('GATE-1'), (c) => c.userData.isGateGlow)
    expect(glow1.material.opacity).toBe(0.54)
    expect(glow1.material.color.hex).toBe(0x38bdf8)

    const glow2 = findMesh(vm.gateMeshes.get('GATE-2'), (c) => c.userData.isGateGlow)
    expect(glow2.material.opacity).toBe(0.48)

    const glow3 = findMesh(vm.gateMeshes.get('GATE-3'), (c) => c.userData.isGateGlow)
    expect(glow3.material.opacity).toBe(0.22)

    const glow4 = findMesh(vm.gateMeshes.get('GATE-4'), (c) => c.userData.isGateGlow)
    expect(glow4.material.opacity).toBe(0.36)

    const glow5 = findMesh(vm.gateMeshes.get('GATE-5'), (c) => c.userData.isGateGlow)
    expect(glow5.material.color.hex).toBe(0x22c55e)
    expect(glow5.material.opacity).toBe(0.36)

    const barrier1 = findMesh(vm.gateMeshes.get('GATE-1'), (c) => c.userData.isGateBarrier)
    expect(barrier1.rotation.z).toBe(-Math.PI / 3.5)

    const barrier2 = findMesh(vm.gateMeshes.get('GATE-2'), (c) => c.userData.isGateBarrier)
    expect(barrier2.rotation.z).toBe(-Math.PI / 10)
  })

  it('highlights and focuses the gate from selectedGateId watch', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    await settle()
    const vm = vmctx(wrapper)

    await wrapper.setProps({ selectedGateId: 102 })
    expect(vm.selectedSiteId).toBe(2)
    const glow = findMesh(vm.gateMeshes.get('GATE-2'), (c) => c.userData.isGateGlow)
    expect(glow.material.emissive.hex).toBe(0x1d4ed8)
    expect(glow.material.emissiveIntensity).toBe(0.35)

    await wrapper.setProps({ selectedGateId: 999 })
    await wrapper.setProps({ selectedGateId: null })
    const glow1 = findMesh(vm.gateMeshes.get('GATE-2'), (c) => c.userData.isGateGlow)
    expect(glow1.material.emissive.hex).toBe(0x000000)
  })

  it('refreshes event signals when recentEvents change', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    await settle()
    const vm = vmctx(wrapper)

    const signal1 = findMesh(vm.gateMeshes.get('GATE-1'), (c) => c.userData.isEventSignal)
    expect(signal1.visible).toBe(true)
    expect(signal1.userData.eventWeight).toBe(2)

    await wrapper.setProps({ recentEvents: [{ gateName: 'GATE-2' }, { gateName: 'GATE-2' }, { gateName: 'GATE-2' }] })
    expect(findMesh(vm.gateMeshes.get('GATE-1'), (c) => c.userData.isEventSignal).visible).toBe(false)
    const signal2 = findMesh(vm.gateMeshes.get('GATE-2'), (c) => c.userData.isEventSignal)
    expect(signal2.visible).toBe(true)
    expect(signal2.userData.eventWeight).toBe(3)

    await wrapper.setProps({ recentEvents: [] })
    expect(findMesh(vm.gateMeshes.get('GATE-2'), (c) => c.userData.isEventSignal).visible).toBe(false)
  })

  it('rebuilds the world when sites change after init and bails on empty bounds', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    await settle()
    const vm = vmctx(wrapper)

    await wrapper.setProps({ sites: makeProps().sites.map((s) => ({ ...s, name: `${s.name} renamed` })) })
    await settle()
    expect(vm.worldGroup.children.length).toBeGreaterThan(0)

    const registry = (await import('three')).__registry
    registry.emptyBox = true
    await wrapper.setProps({ sites: makeProps().sites })
    await settle()
    registry.emptyBox = false

    vm.focusSite(1, false)
    vm.fitToContent()
    expect(vm.scene).toBeTruthy()
  })

  it('skips the rebuild when sites change before initialization finishes', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    const vm = vmctx(wrapper)
    vm.initialized = false
    await wrapper.setProps({ sites: makeProps().sites })
    await settle()
    wrapper.unmount()
  })
})

describe('exposed focus and frame helpers', () => {
  it('frames site bounds, gate bounds and content bounds', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    await settle()
    const vm = vmctx(wrapper)

    vm.focusSite(1)
    expect(vm.selectedSiteId).toBe(1)

    vm.focusSite(999)
    expect(vm.selectedSiteId).toBe(1)

    vm.focusGate(101)
    expect(vm.selectedSiteId).toBe(1)
    vm.focusGate(999)
    expect(vm.selectedSiteId).toBe(1)

    vm.fitToContent()
    expect(vm.selectedSiteId).toBe(null)

    const fakeBounds = {
      isEmpty: () => false,
      getSize: (o) => o.set(50, 60, 50),
      getCenter: (o) => o.set(0, 0, 0),
    }
    vm.frameBounds(fakeBounds, false, 1.2)
    vm.frameBounds(fakeBounds, true, 1.2)
    vm.frameBounds(fakeBounds)
  })
})

describe('keyboard movement and animation', () => {
  it('tracks pressed keys and moves the camera through pumped frames', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    await settle()
    const vm = vmctx(wrapper)

    const keys = [
      ['w', 'forward', true],
      ['s', 'backward', true],
      ['a', 'left', true],
      ['d', 'right', true],
      ['ArrowUp', 'forward', true],
      ['ArrowDown', 'backward', true],
      ['ArrowLeft', 'left', true],
      ['ArrowRight', 'right', true],
    ]
    for (const [key, state, value] of keys) {
      window.dispatchEvent(new KeyboardEvent('keydown', { key }))
      expect(vm.moveState[state]).toBe(value)
      window.dispatchEvent(new KeyboardEvent('keyup', { key }))
      expect(vm.moveState[state]).toBe(false)
    }
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'x' }))
    expect(vm.moveState.forward).toBe(false)

    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'w' }))
    const zBefore = vm.camera.position.z
    pumpFrames(1)
    expect(vm.camera.position.z).not.toBe(zBefore)
    window.dispatchEvent(new KeyboardEvent('keyup', { key: 'w' }))

    const posOf = (p) => `${p.x},${p.y},${p.z}`
    for (const key of ['w', 's', 'a', 'd']) {
      window.dispatchEvent(new KeyboardEvent('keydown', { key }))
      const before = posOf(vm.camera.position)
      pumpFrames(1)
      expect(posOf(vm.camera.position)).not.toBe(before)
      window.dispatchEvent(new KeyboardEvent('keyup', { key }))
    }

    pumpFrames(1)
    wrapper.unmount()
  })

  it('animates stars, beacons, pulses, gate glow and event signals', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    await settle()
    pumpFrames(2)
    wrapper.unmount()
  })

  it('bails out of keyboard movement when the forward direction is a zero vector', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    await settle()
    const registry = (await import('three')).__registry
    registry.zeroDirection = true
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'w' }))
    pumpFrames(1)
    registry.zeroDirection = false
    wrapper.unmount()
  })
})

describe('helpers and disposal', () => {
  it('parses properties, formats dates and computes status colors', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    await settle()
    const vm = vmctx(wrapper)

    expect(vm.parseProperties(null)).toEqual({})
    expect(vm.parseProperties({ a: 1 })).toEqual({ a: 1 })
    expect(vm.parseProperties('{"b":2}')).toEqual({ b: 2 })
    expect(vm.parseProperties('not json')).toEqual({})

    expect(vm.statusColorHex('Warning')).toBe('#f59e0b')
    expect(vm.statusColorHex('Active')).toBe('#38bdf8')
    expect(vm.statusColorHex('Offline')).toBe('#64748b')
    expect(vm.statusColorHex('Alarm')).toBe('#ef4444')
    expect(vm.statusColorHex('Bogus')).toBe('#22c55e')

    expect(vm.formatDateTime(null)).toBe('--')
    expect(vm.formatDateTime('2026-08-30T10:00:00Z')).not.toBe('--')
  })

  it('disposes materials, arrays and groups without throwing', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    await settle()
    const vm = vmctx(wrapper)

    const mapDispose = vi.fn()
    const dispose = vi.fn()
    const withMap = { map: { dispose: mapDispose }, dispose }
    vm.disposeMaterial(null)
    vm.disposeMaterial(withMap)
    expect(mapDispose).toHaveBeenCalledTimes(1)
    expect(dispose).toHaveBeenCalledTimes(1)

    vm.disposeMaterial({ map: null, dispose: vi.fn() })
    vm.disposeMaterial([withMap, null, { dispose: vi.fn() }, { map: null }])

    vm.disposeGroup(vm.worldGroup)
    expect(vm.worldGroup.children).toHaveLength(0)
  })

  it('marks a single mesh as a label during highlight and hover traversal', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    await settle()
    const vm = vmctx(wrapper)

    const gate = vm.gateMeshes.get('GATE-1')
    findMesh(gate, (c) => c.userData.isGateBarrier).userData.isLabel = true

    vm.highlightGate(101)
    vm.highlightGate(null)
    vm.applyHoverState(gate)
    vm.clearHoverState()
  })

  it('emits inspect payloads with and without gate info', async () => {
    const wrapper = mount(Campus3DCanvas, { props: makeProps() })
    await settle()
    const vm = vmctx(wrapper)

    vm.emitInspection(vm.gateMeshes.get('GATE-2'))
    expect(wrapper.emitted('inspect-object').at(-1)[0]).toMatchObject({ objectType: 'GateMarker', gate: { gateId: 102 } })

    vm.emitInspection(vm.objectRecords.find((r) => r.userData.objectType === 'Path'))
    expect(wrapper.emitted('inspect-object').at(-1)[0]).toMatchObject({ objectType: 'Path', gate: null })

    vm.emitHover(vm.objectRecords.find((r) => r.userData.objectType === 'Path'))
    expect(wrapper.emitted('hover-object').at(-1)[0]).toMatchObject({ objectType: 'Path', gate: null })
  })

  it('covers prop defaults and defensive branches', async () => {
    const wrapper = mount(Campus3DCanvas)
    const vm = vmctx(wrapper)

    expect(wrapper.vm.$.props.sites).toEqual([])
    expect(wrapper.vm.$.props.gates).toEqual([])
    expect(wrapper.vm.$.props.recentEvents).toEqual([])
    expect(wrapper.vm.$.props.selectedGateId).toBeNull()

    vm.fitToContent()
    vm.addSiteConnections(null)
    vm.addSiteConnections([])
    vm.addSiteConnections([{ centerX: 0, centerZ: 0 }])

    await settle()

    vm.onResize()
    vm.applyHoverState(vm.worldGroup)
    vm.applyHoverState(null)
    expect(vm.hoveredObject).toBe(null)
    wrapper.unmount()
  })
})