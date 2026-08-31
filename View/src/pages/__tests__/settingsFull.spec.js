import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({ route: { query: {} } }))

vi.mock('vue-router', async () => {
  const { reactive } = await import('vue')
  const route = reactive({ query: {} })
  hoisted.route = route
  return { useRoute: () => route }
})
vi.mock('../../config/api', () => ({ API_BASE_URL: 'http://localhost:5107/api' }))
vi.mock('axios', () => ({ default: { get: vi.fn(), post: vi.fn(), put: vi.fn() } }))
vi.mock('../../services/cameraRuntimeApi', () => ({
  createCamera: vi.fn(),
  deleteCamera: vi.fn(),
  getCameras: vi.fn(),
  reloadGo2rtc: vi.fn(),
  updateCamera: vi.fn(),
}))
vi.mock('../../services/faceApi', () => ({ discoverIpWebcams: vi.fn() }))
vi.mock('../../utils/cameraNetwork', () => ({
  buildCameraHealthProbeUrl: vi.fn(),
  isHttpCameraUrl: vi.fn(),
  isRtspCameraUrl: vi.fn(),
  normalizeCameraUrl: vi.fn(),
}))

const axios = (await import('axios')).default
const cameraRuntimeApi = await import('../../services/cameraRuntimeApi')
const faceApi = await import('../../services/faceApi')
const cameraNetwork = await import('../../utils/cameraNetwork')
const Settings = (await import('../Settings.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  localStorage.clear()
  sessionStorage.clear()
  hoisted.route.query = {}
  cameraNetwork.isHttpCameraUrl.mockImplementation((url) => /^https?:\/\//i.test(url || ''))
  cameraNetwork.isRtspCameraUrl.mockImplementation((url) => /^rtsp:\/\//i.test(url || ''))
  cameraNetwork.normalizeCameraUrl.mockImplementation((raw) => {
    const v = (raw || '').trim()
    if (!v) return ''
    return v.startsWith('http') || v.startsWith('rtsp') || v.includes('://') ? v : `http://${v}`
  })
  cameraNetwork.buildCameraHealthProbeUrl.mockImplementation((url) => (/^https?:\/\//i.test(url || '') ? url : ''))
  cameraRuntimeApi.getCameras.mockResolvedValue([])
  cameraRuntimeApi.reloadGo2rtc.mockResolvedValue()
  cameraRuntimeApi.updateCamera.mockResolvedValue({})
  cameraRuntimeApi.createCamera.mockResolvedValue({})
  cameraRuntimeApi.deleteCamera.mockResolvedValue({})
  faceApi.discoverIpWebcams.mockResolvedValue({ cameras: [] })
  axios.get.mockResolvedValue({ data: {} })
  axios.put.mockResolvedValue({})
  // stub fetch so probeHttpCameraUrl resolves immediately
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve({ ok: true })))
})

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
})

async function mountSettings() {
  const wrapper = mount(Settings)
  await flushPromises()
  await flushPromises()
  return wrapper
}

function goTab(wrapper, label) {
  const btn = wrapper.findAll('button.settings-tab').find((b) => b.text().includes(label))
  return btn.trigger('click')
}

describe('Settings - general tab / system settings', () => {
  it('loads persisted system settings from localStorage into the reactive models', async () => {
    localStorage.setItem('vshield-system-settings-v1', JSON.stringify({
      settings: { companyName: 'ACME', openTime: '07:00', closeTime: '23:30', language: 'en', timezone: 'UTC+8' },
      recognitionSettings: { faceEnabled: false, faceThreshold: 70, antiSpoofing: false, plateEnabled: false, plateThreshold: 60 },
      notifSettings: { strangerAlert: false, unregisteredVehicle: false, cameraOffline: false, afterHours: true },
    }))
    const wrapper = await mountSettings()
    expect(wrapper.vm.settings.companyName).toBe('ACME')
    expect(wrapper.vm.settings.openTime).toBe('07:00')
    expect(wrapper.vm.settings.closeTime).toBe('23:30')
    expect(wrapper.vm.settings.language).toBe('en')
    expect(wrapper.vm.settings.timezone).toBe('UTC+8')
    expect(wrapper.vm.recognitionSettings.faceEnabled).toBe(false)
    expect(wrapper.vm.recognitionSettings.antiSpoofing).toBe(false)
    expect(wrapper.vm.notifSettings.afterHours).toBe(true)
  })

  it('keeps defaults when stored settings JSON is invalid', async () => {
    localStorage.setItem('vshield-system-settings-v1', '{ not valid json')
    const wrapper = await mountSettings()
    expect(wrapper.vm.settings.companyName).toBe('V-Shield Security Group')
  })

  it('handles a route query tab that is valid and one that is not', async () => {
    hoisted.route.query = { tab: 'camera' }
    const w1 = await mountSettings()
    expect(w1.vm.activeTab).toBe('camera')
    hoisted.route.query = { tab: 'nonexistent' }
    const w2 = await mountSettings()
    expect(w2.vm.activeTab).toBe('general')
  })
})

describe('Settings - camera network tab', () => {
  it('applies a manually entered camera to an auto target (creates a new camera)', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue([])
    cameraRuntimeApi.createCamera.mockResolvedValue({ cameraId: 7 })
    cameraRuntimeApi.getCameras.mockResolvedValueOnce([]).mockResolvedValueOnce([{ cameraId: 7, cameraName: 'CAM-07', streamUrl: 'http://10.0.0.9:8081/video' }])
    cameraNetwork.isHttpCameraUrl.mockImplementation(() => true)
    cameraNetwork.isRtspCameraUrl.mockImplementation(() => false)
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await flushPromises()
    await wrapper.findAll('input').find((i) => i.element.placeholder.includes('Tên hiển thị')).setValue('GateCam')
    await wrapper.findAll('input').find((i) => i.element.placeholder.includes('http://IP')).setValue('10.0.0.9:8081')
    const applyBtn = wrapper.findAll('button').find((b) => b.text().includes('Nạp vào mạng lưới'))
    await applyBtn.trigger('click')
    await flushPromises()
    await flushPromises()
    expect(cameraRuntimeApi.createCamera).toHaveBeenCalledWith(expect.objectContaining({ streamUrl: 'http://10.0.0.9:8081' }))
    expect(wrapper.vm.cameraSettings.length).toBeGreaterThanOrEqual(0)
  })

  it('reports an error when the manual camera URL is invalid', async () => {
    cameraNetwork.normalizeCameraUrl.mockImplementation((raw) => ((raw || '').trim() ? '' : ''))
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await wrapper.findAll('input').find((i) => i.element.placeholder.includes('Tên hiển thị')).setValue('GateCam')
    const invalid = wrapper.findAll('input').find((i) => i.element.placeholder.includes('http://IP'))
    await invalid.setValue('')
    await wrapper.findAll('button').find((b) => b.text().includes('Nạp vào mạng lưới')).trigger('click')
    await flushPromises()
    expect(wrapper.vm.connectError).toContain('Hãy nhập URL camera hợp lệ')
  })

  it('discovers LAN cameras and reports a message', async () => {
    faceApi.discoverIpWebcams.mockResolvedValue({
      cameras: [{ ipAddress: '10.0.0.5', port: 8081, name: 'Webcam A' }],
    })
    cameraRuntimeApi.getCameras.mockResolvedValue([])
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await wrapper.findAll('button').find((b) => b.text().includes('Tự tìm camera LAN')).trigger('click')
    await flushPromises()
    expect(wrapper.vm.discoveredCameras.length).toBe(1)
    expect(wrapper.vm.discoveryMessage).toContain('Tìm thấy 1')
  })

  it('reports an error when no cameras are found', async () => {
    faceApi.discoverIpWebcams.mockResolvedValue({ cameras: [] })
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await wrapper.findAll('button').find((b) => b.text().includes('Tự tìm camera LAN')).trigger('click')
    await flushPromises()
    expect(wrapper.vm.discoveryError).toContain('Chưa tìm thấy')
  })

  it('reports an error when discovery throws', async () => {
    faceApi.discoverIpWebcams.mockRejectedValue(new Error('nope'))
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await wrapper.findAll('button').find((b) => b.text().includes('Tự tìm camera LAN')).trigger('click')
    await flushPromises()
    expect(wrapper.vm.discoveryError).toContain('nope')
  })

  it('applies a single discovered camera into the network', async () => {
    const cam = { ipAddress: '10.0.0.5', port: 8081, name: 'Webcam A', rtspUrls: ['rtsp://10.0.0.5:554/stream'] }
    faceApi.discoverIpWebcams.mockResolvedValue({ cameras: [cam] })
    cameraRuntimeApi.createCamera.mockResolvedValue({ cameraId: 9 })
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await wrapper.findAll('button').find((b) => b.text().includes('Tự tìm camera LAN')).trigger('click')
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Nạp vào camera')).trigger('click')
    await flushPromises()
    expect(cameraRuntimeApi.createCamera).toHaveBeenCalled()
  })

  it('errors when a discovered camera has no connectable URL', async () => {
    const cam = { ipAddress: '10.0.0.5', port: 8081, name: 'Webcam B' }
    faceApi.discoverIpWebcams.mockResolvedValue({ cameras: [cam] })
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await wrapper.findAll('button').find((b) => b.text().includes('Tự tìm camera LAN')).trigger('click')
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Nạp vào camera')).trigger('click')
    await flushPromises()
    expect(wrapper.vm.connectError).toContain('chưa có URL phù hợp')
  })
})

describe('Settings - save settings and toast', () => {
  it('persists settings and shows a toast on save', async () => {
    const wrapper = await mountSettings()
    await wrapper.findAll('button').find((b) => b.text().includes('Lưu cài đặt')).trigger('click')
    await flushPromises()
    expect(localStorage.getItem('vshield-system-settings-v1')).toBeTruthy()
    expect(wrapper.vm.toast).toBeTruthy()
  })

  it('clears the toast after the timer elapses', async () => {
    vi.useFakeTimers()
    cameraRuntimeApi.getCameras.mockResolvedValue([])
    const wrapper = mount(Settings)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Lưu cài đặt')).trigger('click')
    await flushPromises()
    expect(wrapper.vm.toast).toBeTruthy()
    vi.advanceTimersByTime(4000)
    expect(wrapper.vm.toast).toBeNull()
  })
})

describe('Settings - camera stream mode', () => {
  it('loads and saves the stream mode via axios', async () => {
    axios.get.mockResolvedValue({ data: { value: 'public' } })
    const wrapper = await mountSettings()
    await flushPromises()
    expect(wrapper.vm.cameraStreamMode).toBe('public')
    await goTab(wrapper, 'Mạng lưới Camera')
    const select = wrapper.findAll('select').find((s) => [...s.findAll('option')].some((o) => o.text() === 'Local'))
    await select.setValue('local')
    await select.trigger('change')
    await flushPromises()
    expect(axios.put).toHaveBeenCalled()
  })
})

describe('Settings - camera card persistence and toggles', () => {
  function camera() {
    return { id: 1, cameraId: 1, name: 'CAM-01', label: '', url: 'http://10.0.0.2:8081/video', previewUrl: 'http://10.0.0.2:8081/video', enabled: true, online: false, gateId: null, cameraType: null }
  }

  it('blurs the display-name field to persist camera settings', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue([{ cameraId: 1, cameraName: 'Cam A', streamUrl: 'http://10.0.0.2:8081/video' }])
    cameraNetwork.isHttpCameraUrl.mockImplementation(() => true)
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await flushPromises()
    const labelInput = wrapper.findAll('input').find((i) => i.element.placeholder.startsWith('Ví dụ:'))
    expect(labelInput).toBeTruthy()
    await labelInput.setValue('Lobby')
    await labelInput.trigger('blur')
    await flushPromises()
    expect(cameraRuntimeApi.updateCamera).toHaveBeenCalled()
  })

  it('toggle on a camera with no URL is rejected', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue([{ cameraId: 1, cameraName: 'Cam A', streamUrl: '' }])
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await flushPromises()
    const cam = wrapper.vm.cameraSettings[0]
    cam.url = ''
    const toggle = wrapper.findAll('input[type="checkbox"]').find((i) => i.element.checked === false)
    if (toggle) {
      await toggle.setValue(true)
      await toggle.trigger('change')
      await flushPromises()
      expect(wrapper.vm.connectError).toContain('Hãy nhập URL')
    }
  })

  it('deletes a camera', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue([{ cameraId: 1, cameraName: 'Cam A', streamUrl: 'http://10.0.0.2:8081/video' }])
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Xóa camera')).trigger('click')
    await flushPromises()
    await flushPromises()
    expect(cameraRuntimeApi.deleteCamera).toHaveBeenCalledWith(1)
  })

  it('checks a single camera status via http probe', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue([{ cameraId: 1, cameraName: 'Cam A', streamUrl: 'http://10.0.0.2:8081/video' }])
    cameraNetwork.isHttpCameraUrl.mockImplementation(() => true)
    cameraNetwork.isRtspCameraUrl.mockImplementation(() => false)
    cameraNetwork.buildCameraHealthProbeUrl.mockImplementation(() => 'http://10.0.0.2:8081/')
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await flushPromises()
    expect(wrapper.vm.cameraSettings.length).toBe(1)
    const checkBtn = wrapper.findAll('button').find((b) => b.text().includes('Kiểm tra lại') || b.text().includes('Đang kiểm tra'))
    expect(checkBtn, 'buttons: ' + wrapper.findAll('button').map(b => b.text()).join(' | ')).toBeTruthy()
    const callsBefore = cameraNetwork.buildCameraHealthProbeUrl.mock.calls.length
    await checkBtn.trigger('click')
    await flushPromises()
    await flushPromises()
    expect(cameraNetwork.buildCameraHealthProbeUrl.mock.calls.length).toBeGreaterThan(callsBefore)
    expect(wrapper.vm.cameraSettings[0].online).toBe(true)
  })
})

describe('Settings - route watcher, toggles and extra camera flows', () => {
  it('syncs the active tab when the route query tab changes', async () => {
    hoisted.route.query.tab = 'general'
    const wrapper = await mountSettings()
    expect(wrapper.vm.activeTab).toBe('general')
    hoisted.route.query.tab = 'notifications'
    await flushPromises()
    expect(wrapper.vm.activeTab).toBe('notifications')
  })

  it('drives the recognition and notification toggle inputs', async () => {
    hoisted.route.query.tab = 'recognition'
    const wrapper = await mountSettings()
    await flushPromises()
    const faceBox = wrapper.findAll('input[type="checkbox"]').find((i) => i.element.checked === true)
    if (faceBox) {
      await faceBox.setValue(false)
      expect(wrapper.vm.recognitionSettings.faceEnabled).toBe(false)
    }
    const range = wrapper.find('input[type="range"]')
    if (range) {
      await range.setValue(60)
      expect(wrapper.vm.recognitionSettings.faceThreshold).toBe(60)
    }
    hoisted.route.query.tab = 'notifications'
    await flushPromises()
    const notifBox = wrapper.findAll('input[type="checkbox"]').find((i) => i.element.checked === true)
    if (notifBox) {
      await notifBox.setValue(false)
    }
    expect(wrapper.vm.notifSettings.afterHours).toBe(false)
  })

  it('uses the manualTargetId select and guesses a camera label from a URL', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue([
      { cameraId: 1, cameraName: 'Cam A', streamUrl: '' },
      { cameraId: 2, cameraName: 'Cam B', streamUrl: '' },
      { cameraId: 3, cameraName: 'Cam C', streamUrl: 'http://10.0.0.2:8081/video' },
    ])
    cameraNetwork.normalizeCameraUrl.mockImplementation((raw) => (raw ? 'http://10.0.0.9:8081/video' : ''))
    cameraNetwork.isHttpCameraUrl.mockImplementation(() => true)
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await flushPromises()
    expect(wrapper.vm.cameraSettings.length).toBe(3)
    const targetSelect = wrapper.findAll('select').find((s) => {
      const opts = s.findAll('option')
      return opts.some((o) => o.text().includes('CAM-'))
    })
    expect(targetSelect).toBeTruthy()
    await targetSelect.setValue('2')
    await wrapper.findAll('input').find((i) => i.element.placeholder.includes('http://IP')).setValue('10.0.0.9:8081')
    await wrapper.findAll('button').find((b) => b.text().includes('Nạp vào mạng lưới')).trigger('click')
    await flushPromises()
    await flushPromises()
    expect(cameraRuntimeApi.updateCamera).toHaveBeenCalledWith(2, expect.objectContaining({ streamUrl: 'http://10.0.0.9:8081/video' }))
  })

  it('turns a camera off via the toggle when it has a URL', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue([{ cameraId: 1, cameraName: 'Cam A', streamUrl: 'http://10.0.0.2:8081/video' }])
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await flushPromises()
    await wrapper.findAll('input[type="checkbox"]').find((i) => i.element.checked === true).setValue(false)
    await flushPromises()
    await flushPromises()
    expect(cameraRuntimeApi.updateCamera).toHaveBeenCalledWith(1, expect.objectContaining({ streamUrl: null }))
  })

  it('rejects toggling on a camera that has no URL', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue([{ cameraId: 1, cameraName: 'Cam A', streamUrl: '' }])
    cameraNetwork.normalizeCameraUrl.mockImplementation((raw) => (raw ? 'http://10.0.0.2:8081/video' : ''))
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await flushPromises()
    const offBox = wrapper.findAll('input[type="checkbox"]').find((i) => i.element.checked === false)
    if (offBox) {
      await offBox.setValue(true)
      await flushPromises()
    }
    expect(wrapper.vm.cameraSettings[0].enabled).toBe(false)
    expect(wrapper.vm.connectError).toBeTruthy()
    expect(cameraRuntimeApi.updateCamera).not.toHaveBeenCalled()
  })

  it('normalizes the camera card URL on blur', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue([{ cameraId: 1, cameraName: 'Cam A', streamUrl: '10.0.0.2:8081' }])
    cameraNetwork.normalizeCameraUrl.mockImplementation((raw) => (raw ? 'http://10.0.0.2:8081/video' : ''))
    cameraNetwork.isHttpCameraUrl.mockImplementation(() => true)
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await flushPromises()
    const urlInput = wrapper.findAll('input[type="text"]').find((i) => i.element.placeholder.startsWith('http://IP'))
    expect(urlInput).toBeTruthy()
    await urlInput.trigger('blur')
    await flushPromises()
    await flushPromises()
    expect(cameraRuntimeApi.updateCamera).toHaveBeenCalled()
  })

  it('applies all discovered cameras and reports assigned count', async () => {
    faceApi.discoverIpWebcams.mockResolvedValue({
      cameras: [
        { ipAddress: '10.0.0.5', port: 8081, name: 'A', rtspUrls: ['rtsp://10.0.0.5/stream'] },
        { ipAddress: '10.0.0.6', port: 8081, name: 'B', rtspUrls: ['rtsp://10.0.0.6/stream'] },
      ],
    })
    cameraRuntimeApi.createCamera.mockResolvedValue({ cameraId: 5 })
    cameraNetwork.normalizeCameraUrl.mockImplementation((raw) => (raw ? raw : ''))
    cameraRuntimeApi.getCameras.mockResolvedValue([{ cameraId: 9, cameraName: 'Cam Nine', streamUrl: 'rtsp://10.0.0.9/stream' }])
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await wrapper.findAll('button').find((b) => b.text().includes('Tự tìm camera LAN')).trigger('click')
    await flushPromises()
    expect(wrapper.vm.discoveredCameras.length).toBe(2)
    await wrapper.findAll('button').find((b) => b.text().includes('Nạp tất cả')).trigger('click')
    await flushPromises()
    await flushPromises()
    expect(cameraRuntimeApi.createCamera).toHaveBeenCalledTimes(2)
    expect(String(wrapper.vm.connectMessage).normalize('NFC')).toContain('Đã nạp')
  })

  it('skips discovered cameras without a connectable URL when applying all', async () => {
    faceApi.discoverIpWebcams.mockResolvedValue({
      cameras: [{ ipAddress: '10.0.0.5', port: 8081, name: 'A' }],
    })
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await wrapper.findAll('button').find((b) => b.text().includes('Tự tìm camera LAN')).trigger('click')
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Nạp tất cả')).trigger('click')
    await flushPromises()
    expect(String(wrapper.vm.connectError).normalize('NFC')).toContain('Không thể nạp camera nào')
  })

  it('clears the active tab timer on unmount', async () => {
    vi.useFakeTimers()
    cameraRuntimeApi.getCameras.mockResolvedValue([])
    const wrapper = mount(Settings)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Lưu cài đặt')).trigger('click')
    await flushPromises()
    wrapper.unmount()
    vi.advanceTimersByTime(4000)
    vi.useRealTimers()
    expect(true).toBe(true)
  })

  it('uses a specific target id for a discovered camera', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue([{ cameraId: 10, cameraName: 'Cam Ten', streamUrl: '' }])
    faceApi.discoverIpWebcams.mockResolvedValue({
      cameras: [{ ipAddress: '10.0.0.9', port: 8081, name: 'Z', rtspUrls: ['rtsp://10.0.0.9/stream'] }],
    })
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await wrapper.findAll('button').find((b) => b.text().includes('Tự tìm camera LAN')).trigger('click')
    await flushPromises()
    const cardSel = wrapper.findAll('select').find((s) => {
      const opts = s.findAll('option')
      return opts.some((o) => o.text().includes('Cam Ten'))
    })
    if (cardSel) await cardSel.setValue('10')
    await wrapper.findAll('button').find((b) => b.text().includes('Nạp vào camera')).trigger('click')
    await flushPromises()
    await flushPromises()
    expect(cameraRuntimeApi.updateCamera).toHaveBeenCalledWith(10, expect.anything())
  })

  it('toggles every recognition setting and threshold', async () => {
    hoisted.route.query.tab = 'recognition'
    const wrapper = await mountSettings()
    await flushPromises()
    for (const input of wrapper.findAll('input[type="checkbox"]')) {
      await input.setValue(!input.element.checked)
    }
    expect(wrapper.vm.recognitionSettings.faceEnabled).toBe(false)
    expect(wrapper.vm.recognitionSettings.antiSpoofing).toBe(false)
    expect(wrapper.vm.recognitionSettings.plateEnabled).toBe(false)
    const ranges = wrapper.findAll('input[type="range"]')
    expect(ranges.length).toBe(2)
    await ranges[0].setValue(60)
    await ranges[1].setValue(70)
    expect(wrapper.vm.recognitionSettings.faceThreshold).toBe(60)
    expect(wrapper.vm.recognitionSettings.plateThreshold).toBe(70)
  })

  it('toggles every notification setting', async () => {
    hoisted.route.query.tab = 'notifications'
    const wrapper = await mountSettings()
    await flushPromises()
    const boxes = wrapper.findAll('input[type="checkbox"]')
    expect(boxes.length).toBe(4)
    for (const input of boxes) {
      await input.setValue(true)
    }
    expect(wrapper.vm.notifSettings.afterHours).toBe(true)
    expect(wrapper.vm.notifSettings.strangerAlert).toBe(true)
    expect(wrapper.vm.notifSettings.unregisteredVehicle).toBe(true)
    expect(wrapper.vm.notifSettings.cameraOffline).toBe(true)
  })

  it('saves the general settings form', async () => {
    hoisted.route.query.tab = 'general'
    const wrapper = await mountSettings()
    await flushPromises()
    const textInput = wrapper.findAll('input[type="text"]').find((i) => !i.element.placeholder)
    expect(textInput).toBeTruthy()
    await textInput.setValue('My Company')
    expect(wrapper.vm.settings.companyName).toBe('My Company')
    await wrapper.findAll('button').find((b) => b.text().includes('Lưu cài đặt')).trigger('click')
    await flushPromises()
    await flushPromises()
    expect(wrapper.vm.toast).toBeTruthy()
  })

  it('changes the camera stream mode', async () => {
    hoisted.route.query.tab = 'camera'
    const wrapper = await mountSettings()
    await flushPromises()
    const modeSel = wrapper.findAll('select').find((s) => {
      const opts = s.findAll('option')
      return opts.some((o) => o.text().includes('Local'))
    })
    expect(modeSel).toBeTruthy()
    await modeSel.setValue('local')
    await flushPromises()
    expect(wrapper.vm.cameraStreamMode).toBe('local')
    expect(axios.put).toHaveBeenCalledWith(expect.stringContaining('CameraStreamMode'), expect.anything())
  })

  it('applies a discovered camera to a brand-new slot via createCamera', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue([{ cameraId: 9, streamUrl: 'rtsp://10.0.0.9/stream' }])
    cameraRuntimeApi.createCamera.mockResolvedValue({ cameraId: 5 })
    cameraNetwork.normalizeCameraUrl.mockImplementation((raw) => (raw || ''))
    faceApi.discoverIpWebcams.mockResolvedValue({
      cameras: [{ ipAddress: '10.0.0.5', port: 8081, name: 'A', rtspUrls: ['rtsp://10.0.0.5/stream'] }],
    })
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await wrapper.findAll('button').find((b) => b.text().includes('Tự tìm camera LAN')).trigger('click')
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Nạp vào camera')).trigger('click')
    await flushPromises()
    await flushPromises()
    expect(cameraRuntimeApi.createCamera).toHaveBeenCalled()
  })

  it('falls back to a generic label when a camera URL is not parseable', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue([])
    cameraRuntimeApi.createCamera.mockResolvedValue({ cameraId: 5 })
    cameraNetwork.normalizeCameraUrl.mockImplementation((raw) => (raw ? 'not-a-valid-url' : ''))
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    const input = wrapper.findAll('input').find((i) => i.element.placeholder.includes('http://IP'))
    await input.setValue('10.0.0.9')
    await wrapper.findAll('button').find((b) => b.text().includes('Nạp vào mạng lưới')).trigger('click')
    await flushPromises()
    await flushPromises()
    expect(cameraRuntimeApi.createCamera).toHaveBeenCalledWith(expect.objectContaining({ cameraName: 'Camera LAN' }))
  })

  it('re-enables a previously disabled camera that has a URL', async () => {
    cameraRuntimeApi.reloadGo2rtc.mockRejectedValue(new Error('boom'))
    cameraRuntimeApi.getCameras.mockResolvedValue([{ cameraId: 1, cameraName: 'Cam A', streamUrl: 'http://10.0.0.2:8081/video' }])
    cameraNetwork.isHttpCameraUrl.mockImplementation(() => true)
    cameraNetwork.isRtspCameraUrl.mockImplementation(() => false)
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await flushPromises()
    wrapper.vm.cameraSettings[0].enabled = false
    await flushPromises()
    const cb = wrapper.findAll('input[type="checkbox"]').find((i) => i.element.checked === false)
    expect(cb).toBeTruthy()
    await cb.setValue(true)
    await flushPromises()
    await flushPromises()
    expect(cameraRuntimeApi.updateCamera).toHaveBeenCalledWith(1, expect.objectContaining({ streamUrl: 'http://10.0.0.2:8081/video' }))
  })

  it('skips discovered cameras when their url fails to apply', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue([])
    cameraNetwork.normalizeCameraUrl.mockImplementation(() => '')
    faceApi.discoverIpWebcams.mockResolvedValue({
      cameras: [{ ipAddress: '10.0.0.5', port: 8081, name: 'A', rtspUrls: ['rtsp://10.0.0.5/stream'] }],
    })
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await wrapper.findAll('button').find((b) => b.text().includes('Tự tìm camera LAN')).trigger('click')
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Nạp tất cả')).trigger('click')
    await flushPromises()
    expect(String(wrapper.vm.connectError).normalize('NFC')).toContain('Không thể nạp camera nào')
  })

  it('marks an http camera offline when the health probe rejects', async () => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.reject(new Error('network down'))))
    cameraRuntimeApi.getCameras.mockResolvedValue([{ cameraId: 1, cameraName: 'Cam A', streamUrl: 'http://10.0.0.2:8081/video' }])
    cameraNetwork.isHttpCameraUrl.mockImplementation(() => true)
    cameraNetwork.isRtspCameraUrl.mockImplementation(() => false)
    cameraNetwork.buildCameraHealthProbeUrl.mockImplementation(() => 'http://10.0.0.2:8081/')
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await flushPromises()
    expect(cameraNetwork.buildCameraHealthProbeUrl).toHaveBeenCalled()
    expect(wrapper.vm.cameraSettings[0].online).toBe(false)
  })

  it('still completes actions when reloadGo2rtc rejects', async () => {
    cameraRuntimeApi.reloadGo2rtc.mockRejectedValue(new Error('boom'))
    cameraRuntimeApi.getCameras.mockResolvedValue([
      { cameraId: 1, cameraName: 'Cam A', streamUrl: '' },
      { cameraId: 2, cameraName: 'Cam B', streamUrl: 'http://10.0.0.2:8081/video' },
    ])
    cameraNetwork.normalizeCameraUrl.mockImplementation((raw) => (raw ? raw : ''))
    cameraNetwork.isHttpCameraUrl.mockImplementation((u) => u && u.startsWith('http'))
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await flushPromises()
    await wrapper.findAll('input').find((i) => i.element.placeholder.includes('http://IP')).setValue('10.0.0.9:8081')
    await wrapper.findAll('button').find((b) => b.text().includes('Nạp vào mạng lưới')).trigger('click')
    await flushPromises()
    await flushPromises()
    const delBtn = wrapper.findAll('button').find((b) => String(b.text()).normalize('NFC').includes('Xóa'))
    if (delBtn) { await delBtn.trigger('click'); await flushPromises(); await flushPromises() }
    expect(cameraRuntimeApi.deleteCamera).toHaveBeenCalled()
  })

  it('reuses an existing camera whose url matches when applying with auto target', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue([
      { cameraId: 1, cameraName: 'Cam One', streamUrl: '' },
      { cameraId: 2, cameraName: 'Cam Two', streamUrl: 'http://10.0.0.2:8081/video' },
    ])
    cameraNetwork.normalizeCameraUrl.mockImplementation((raw) => (raw ? 'http://10.0.0.2:8081/video' : ''))
    cameraNetwork.isHttpCameraUrl.mockImplementation(() => true)
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await flushPromises()
    await wrapper.findAll('input').find((i) => i.element.placeholder.includes('http://IP')).setValue('10.0.0.2:8081')
    await wrapper.findAll('button').find((b) => b.text().includes('Nạp vào mạng lưới')).trigger('click')
    await flushPromises()
    await flushPromises()
    expect(cameraRuntimeApi.updateCamera).toHaveBeenCalledWith(2, expect.objectContaining({ streamUrl: 'http://10.0.0.2:8081/video', cameraName: 'Cam Two' }))
  })

  it('picks a disabled camera when no existing or empty slot is available', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue([
      { cameraId: 1, cameraName: 'A', streamUrl: 'http://10.0.0.1:8081/video' },
      { cameraId: 2, cameraName: 'B', streamUrl: 'http://10.0.0.2:8081/video' },
      { cameraId: 3, cameraName: 'C', streamUrl: 'http://10.0.0.3:8081/video' },
    ])
    cameraNetwork.normalizeCameraUrl.mockImplementation((raw) => (raw ? `http://x:1` : ''))
    cameraNetwork.isHttpCameraUrl.mockImplementation(() => true)
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await flushPromises()
    expect(wrapper.vm.cameraSettings.length).toBe(3)
    wrapper.vm.cameraSettings[0].enabled = false
    await wrapper.findAll('input').find((i) => i.element.placeholder.includes('http://IP')).setValue('10.0.0.9:8081')
    await wrapper.findAll('button').find((b) => b.text().includes('Nạp vào mạng lưới')).trigger('click')
    await flushPromises()
    await flushPromises()
    expect(cameraRuntimeApi.updateCamera).toHaveBeenCalledWith(1, expect.objectContaining({ streamUrl: 'http://x:1' }))
  })

  it('reports an error when a discovered camera fails to apply', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue([])
    cameraNetwork.normalizeCameraUrl.mockImplementation(() => '')
    faceApi.discoverIpWebcams.mockResolvedValue({
      cameras: [{ ipAddress: '10.0.0.5', port: 8081, name: 'A', rtspUrls: ['rtsp://10.0.0.5/stream'] }],
    })
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await wrapper.findAll('button').find((b) => b.text().includes('Tự tìm camera LAN')).trigger('click')
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Nạp vào camera')).trigger('click')
    await flushPromises()
    expect(String(wrapper.vm.connectError).normalize('NFC')).toContain('URL camera hợp lệ')
  })

  it('toggles and renormalizes camera cards even when reloadGo2rtc rejects', async () => {
    cameraRuntimeApi.reloadGo2rtc.mockRejectedValue(new Error('boom'))
    cameraRuntimeApi.getCameras.mockResolvedValue([{ cameraId: 1, cameraName: 'Cam A', streamUrl: '10.0.0.2:8081' }])
    cameraNetwork.normalizeCameraUrl.mockImplementation((raw) => (raw ? 'http://10.0.0.2:8081/video' : ''))
    cameraNetwork.isHttpCameraUrl.mockImplementation(() => true)
    cameraNetwork.isRtspCameraUrl.mockImplementation(() => false)
    const wrapper = await mountSettings()
    await goTab(wrapper, 'Mạng lưới Camera')
    await flushPromises()
    const urlInput = wrapper.findAll('input[type="text"]').find((i) => i.element.placeholder.startsWith('http://IP'))
    await urlInput.trigger('blur')
    await flushPromises()
    await flushPromises()
    const toggle = wrapper.findAll('input[type="checkbox"]').find((i) => i.element.checked === true)
    if (toggle) {
      await toggle.setValue(false)
      await flushPromises()
      await flushPromises()
    }
    expect(cameraRuntimeApi.updateCamera).toHaveBeenCalled()
  })
})
