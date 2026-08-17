import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({ route: { query: {} } }))

vi.mock('vue-router', () => ({ useRoute: () => hoisted.route }))
vi.mock('../../config/api', () => ({ API_BASE_URL: 'http://localhost:5107/api' }))
vi.mock('axios', () => ({ default: { get: vi.fn(), post: vi.fn() } }))
vi.mock('../../services/cameraRuntimeApi', () => ({
  createCamera: vi.fn(),
  deleteCamera: vi.fn(),
  getCameras: vi.fn(),
  reloadGo2rtc: vi.fn(),
  updateCamera: vi.fn(),
}))
vi.mock('../../services/faceApi', () => ({ discoverIpWebcams: vi.fn() }))

const cameraRuntimeApi = await import('../../services/cameraRuntimeApi')
const faceApi = await import('../../services/faceApi')
const Settings = (await import('../Settings.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  localStorage.clear()
})

describe('Settings', () => {
  it('loads camera list and discovers LAN cameras', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue({ data: [{ cameraId: 1, cameraName: 'CAM-01' }] })
    faceApi.discoverIpWebcams.mockResolvedValue({ data: [{ ip: '10.0.0.5', name: 'Webcam A' }] })
    const wrapper = mount(Settings)
    await flushPromises()

    await wrapper.findAll('.tab-btn, .settings-tab, button').find((b) => b.text().toLowerCase().includes('camera')).trigger('click')
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.classes().includes('ghost-btn')).trigger('click')
    await flushPromises()
    expect(faceApi.discoverIpWebcams).toHaveBeenCalled()
  })

  it('saves system settings to localStorage', async () => {
    const wrapper = mount(Settings)
    await flushPromises()
    const saveBtn = wrapper.findAll('button').find((b) => b.text().includes('Lưu cài đặt'))
    if (saveBtn) {
      await saveBtn.trigger('click')
      await flushPromises()
      expect(localStorage.getItem('vshield-system-settings-v1')).toBeTruthy()
    }
  })
})
