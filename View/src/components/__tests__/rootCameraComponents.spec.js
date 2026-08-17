import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({ route: { query: {} }, router: { push: vi.fn() } }))

vi.mock('vue-router', () => ({ useRoute: () => hoisted.route, useRouter: () => hoisted.router }))
vi.mock('../../services/cameraRuntimeApi', () => ({
  getCameras: vi.fn(),
  ensureCameraRegistered: vi.fn(),
  getPythonProcessStatus: vi.fn(),
  startPythonQrProcess: vi.fn(),
  stopPythonQrProcess: vi.fn(),
  startPythonPlateProcess: vi.fn(),
  stopPythonPlateProcess: vi.fn(),
}))
vi.mock('../../services/cameraRegistryApi', () => ({ getConfiguredCameras: vi.fn() }))
vi.mock('../../services/plateRecognitionApi', () => ({ fuzzyMatchPlate: vi.fn(), getPlateAnomalies: vi.fn() }))
vi.mock('../../services/observability', () => ({ captureError: vi.fn(), recordMetric: vi.fn() }))
vi.mock('../../services/gateTransitApi', () => ({ scanGate: vi.fn(), scanGuest: vi.fn(), getManualGates: vi.fn() }))
vi.mock('../../services/dynamicQrVerifyApi', () => ({ verifyDynamicQr: vi.fn() }))
vi.mock('../../services/runtimeServiceApi', () => ({
  getRuntimeServices: vi.fn(),
  updateRuntimeService: vi.fn(),
  startRuntimeService: vi.fn(),
  stopRuntimeService: vi.fn(),
}))
vi.mock('../../services/dynamicQrScannerApi', () => ({
  startQrScanner: vi.fn(),
  resetQrSession: vi.fn(),
  stopQrScanner: vi.fn(),
  getQrScanResult: vi.fn(),
  scanQrOnce: vi.fn(),
}))
vi.mock('../../services/viewPrefs', () => ({ loadViewPrefs: vi.fn(() => null), saveViewPrefs: vi.fn() }))
vi.mock('../../services/http', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn(), delete: vi.fn(), request: vi.fn() },
}))
vi.mock('jsqr', () => ({ default: vi.fn() }))
vi.mock('axios', () => ({ default: Object.assign(vi.fn(() => Promise.resolve({ data: {} })), { isCancel: vi.fn(() => false) }) }))

const cameraRuntimeApi = await import('../../services/cameraRuntimeApi')
const cameraRegistryApi = await import('../../services/cameraRegistryApi')
const gateTransitApi = await import('../../services/gateTransitApi')
const runtimeServiceApi = await import('../../services/runtimeServiceApi')
const dynamicQrScannerApi = await import('../../services/dynamicQrScannerApi')

const FaceCamera = (await import('../FaceCamera.vue')).default
const LicensePlateSecurity = (await import('../LicensePlateSecurity.vue')).default
const QrAccessMonitor = (await import('../QrAccessMonitor.vue')).default
const ThongHanh = (await import('../ThongHanh.vue')).default
const AIChatBot = (await import('../AIChatBot.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  cameraRuntimeApi.getCameras.mockResolvedValue([])
  cameraRuntimeApi.ensureCameraRegistered.mockResolvedValue({ cameraId: 1 })
  cameraRuntimeApi.getPythonProcessStatus.mockResolvedValue({ data: {} })
  cameraRegistryApi.getConfiguredCameras.mockResolvedValue([])
  gateTransitApi.getManualGates.mockResolvedValue({ data: [] })
  runtimeServiceApi.getRuntimeServices.mockResolvedValue({ data: [] })
  dynamicQrScannerApi.getQrScanResult.mockResolvedValue({})
  dynamicQrScannerApi.startQrScanner.mockResolvedValue({})
})

describe('FaceCamera', () => {
  it('loads cameras on mount', async () => {
    const wrapper = mount(FaceCamera)
    await flushPromises()
    expect(cameraRuntimeApi.getCameras).toHaveBeenCalled()
  })
})

describe('LicensePlateSecurity', () => {
  it('loads configured cameras on mount', async () => {
    const wrapper = mount(LicensePlateSecurity, { global: { stubs: { StreamPreview: true } } })
    await flushPromises()
    expect(cameraRegistryApi.getConfiguredCameras).toHaveBeenCalled()
  })
})

describe('QrAccessMonitor', () => {
  it('loads cameras and gates on mount', async () => {
    const wrapper = mount(QrAccessMonitor)
    await flushPromises()
    expect(cameraRuntimeApi.getCameras).toHaveBeenCalled()
    expect(gateTransitApi.getManualGates).toHaveBeenCalled()
  })
})

describe('ThongHanh', () => {
  it('mounts the face-plate transit monitor', async () => {
    const wrapper = mount(ThongHanh)
    await flushPromises()
    expect(wrapper.exists()).toBe(true)
  })
})

describe('AIChatBot', () => {
  it('renders the chat widget', () => {
    const wrapper = mount(AIChatBot)
    expect(wrapper.exists()).toBe(true)
  })
})
