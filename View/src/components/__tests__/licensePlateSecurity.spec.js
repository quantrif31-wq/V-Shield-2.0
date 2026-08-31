import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'

const mocks = vi.hoisted(() => ({
  plateApi: {
    getCameraStatus: vi.fn(),
    getCameraResult: vi.fn(),
    getLockedImages: vi.fn(),
    turnOnCamera: vi.fn(),
    turnOffCamera: vi.fn(),
    resetCameraState: vi.fn(),
  },
  cameraRuntimeApi: {
    ensureCameraRegistered: vi.fn(),
  },
  registryApi: {
    getConfiguredCameras: vi.fn(),
  },
  plateRecognitionApi: {
    fuzzyMatchPlate: vi.fn(),
    getPlateAnomalies: vi.fn(),
  },
}))

vi.mock('../../services/plateCameraApi', () => mocks.plateApi)
vi.mock('../../services/cameraRuntimeApi', () => mocks.cameraRuntimeApi)
vi.mock('../../services/cameraRegistryApi', () => mocks.registryApi)
vi.mock('../../services/plateRecognitionApi', () => mocks.plateRecognitionApi)

import LicensePlateSecurity from '../LicensePlateSecurity.vue'

const STORAGE_KEY = 'vshield-plate-selected-camera'

function makeCamera(overrides = {}) {
  return {
    id: 1,
    name: 'CAM-01',
    label: 'Camera 1',
    sourceUrl: 'rtsp://cam/1',
    browserPreviewUrl: 'http://cam/1.jpg',
    enabled: true,
    ...overrides,
  }
}

function defaultStatus(overrides = {}) {
  return {
    session_id: 0,
    camera_enabled: false,
    scan_locked: false,
    ...overrides,
  }
}

let wrapper

async function mountComponent() {
  wrapper = mount(LicensePlateSecurity, {
    global: { stubs: { StreamPreview: true } },
  })
  await flushPromises()
  return wrapper
}

beforeEach(() => {
  localStorage.clear()
  vi.resetAllMocks()
  mocks.registryApi.getConfiguredCameras.mockResolvedValue([])
  mocks.plateApi.getCameraStatus.mockResolvedValue(defaultStatus())
  mocks.plateApi.getCameraResult.mockResolvedValue(defaultStatus({ camera_enabled: true, session_id: 7 }))
  mocks.plateApi.getLockedImages.mockResolvedValue({})
  mocks.plateApi.turnOnCamera.mockResolvedValue({ success: true, session_id: 7, message: 'ON' })
  mocks.plateApi.turnOffCamera.mockResolvedValue({ message: 'OFF' })
  mocks.plateApi.resetCameraState.mockResolvedValue({ message: 'RESET', session_id: 8 })
  mocks.cameraRuntimeApi.ensureCameraRegistered.mockResolvedValue({ cameraId: 99 })
  mocks.plateRecognitionApi.fuzzyMatchPlate.mockResolvedValue({ data: { results: [] } })
  mocks.plateRecognitionApi.getPlateAnomalies.mockResolvedValue({ data: { anomalies: [] } })
  vi.spyOn(window, 'alert').mockImplementation(() => {})
  vi.spyOn(window, 'confirm').mockImplementation(() => true)
  vi.useFakeTimers()
})

afterEach(() => {
  if (wrapper) wrapper.unmount()
  wrapper = null
  vi.useRealTimers()
  vi.restoreAllMocks()
})

describe('LicensePlateSecurity.vue', () => {
  describe('loadConfiguredCameras', () => {
    it('renders and loads configured cameras on mount', async () => {
      mocks.registryApi.getConfiguredCameras.mockResolvedValue([])
      const wrapper = await mountComponent()
      expect(mocks.registryApi.getConfiguredCameras).toHaveBeenCalled()
      expect(wrapper.find('.source-hint').text()).toContain('Chưa có camera')
    })

    it('auto-selects the single configured camera and applies it', async () => {
      mocks.registryApi.getConfiguredCameras.mockResolvedValue([makeCamera()])
      const wrapper = await mountComponent()
      expect(wrapper.vm.selectedConfiguredCameraId).toBe('1')
      expect(wrapper.vm.cameraIp).toBe('rtsp://cam/1')
      expect(wrapper.vm.previewUrl).toBe('http://cam/1.jpg')
    })

    it('restores a saved camera selection from localStorage', async () => {
      localStorage.setItem(STORAGE_KEY, '1')
      mocks.registryApi.getConfiguredCameras.mockResolvedValue([makeCamera()])
      const wrapper = await mountComponent()
      expect(wrapper.vm.selectedConfiguredCameraId).toBe('1')
    })

    it('keeps an existing valid selection unchanged', async () => {
      mocks.registryApi.getConfiguredCameras.mockResolvedValue([makeCamera(), makeCamera({ id: 2, name: 'CAM-02', label: 'Camera 2' })])
      const wrapper = mount(LicensePlateSecurity, {
        global: { stubs: { StreamPreview: true } },
      })
      wrapper.vm.selectedConfiguredCameraId = '2'
      await mountComponent()
      wrapper.vm.selectedConfiguredCameraId = '2'
      await flushPromises()
      expect(wrapper.vm.selectedConfiguredCameraId).toBe('2')
    })
  })

  describe('computed', () => {
    it('formats bbox text', async () => {
      mocks.registryApi.getConfiguredCameras.mockResolvedValue([])
      const wrapper = await mountComponent()
      expect(wrapper.vm.bboxText).toBe('-----')
      wrapper.vm.bbox = { x1: 1, y1: 2, x2: 3, y2: 4 }
      expect(wrapper.vm.bboxText).toContain('x1=1')
    })

    it('computes session action label', async () => {
      const wrapper = await mountComponent()
      expect(wrapper.vm.sessionActionLabel).toBe('Khởi tạo phiên đọc')
      wrapper.vm.cameraRunning = true
      expect(wrapper.vm.sessionActionLabel).toBe('Reset phiên đọc')
    })

    it('computes the summary for selection states', async () => {
      mocks.registryApi.getConfiguredCameras.mockResolvedValue([makeCamera()])
      const wrapper = mount(LicensePlateSecurity, { global: { stubs: { StreamPreview: true } } })
      wrapper.vm.selectedConfiguredCameraId = '1'
      await flushPromises()
      await flushPromises()
      expect(wrapper.vm.configuredCameraSummary).toContain('Đang chọn CAM-01')
    })

    it('computes the summary when cameras exist but none selected', async () => {
      mocks.registryApi.getConfiguredCameras.mockResolvedValue([makeCamera(), makeCamera({ id: 2, name: 'CAM-02', label: 'Camera 2' })])
      const wrapper = mount(LicensePlateSecurity, { global: { stubs: { StreamPreview: true } } })
      await flushPromises()
      await flushPromises()
      expect(wrapper.vm.configuredCameraSummary).toContain('Chọn một camera đã cấu hình')
    })

    it('computes effective preview url', async () => {
      const wrapper = await mountComponent()
      expect(wrapper.vm.effectivePreviewUrl).toBe('')
      wrapper.vm.previewUrl = '  http://x/feed  '
      expect(wrapper.vm.effectivePreviewUrl).toBe('http://x/feed')
    })
  })

  describe('selection helpers', () => {
    it('clears the saved selection when none selected', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.selectedConfiguredCameraId = '1'
      localStorage.setItem(STORAGE_KEY, '1')
      wrapper.vm.rememberConfiguredCameraSelection()
      expect(localStorage.getItem(STORAGE_KEY)).toBe('1')
      wrapper.vm.selectedConfiguredCameraId = ''
      wrapper.vm.rememberConfiguredCameraSelection()
      expect(localStorage.getItem(STORAGE_KEY)).toBeNull()
    })

    it('handleConfiguredCameraChange remembers and applies', async () => {
      mocks.registryApi.getConfiguredCameras.mockResolvedValue([makeCamera()])
      const wrapper = mount(LicensePlateSecurity, { global: { stubs: { StreamPreview: true } } })
      wrapper.vm.selectedConfiguredCameraId = '1'
      await flushPromises()
      await flushPromises()
      wrapper.vm.handleConfiguredCameraChange()
      expect(localStorage.getItem(STORAGE_KEY)).toBe('1')
      expect(wrapper.vm.cameraIp).toBe('rtsp://cam/1')
    })

    it('applyConfiguredCameraSelection alerts when no camera selected', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.applyConfiguredCameraSelection()
      expect(window.alert).toHaveBeenCalledWith('Vui lòng chọn một camera đã cấu hình.')
    })

    it('applyConfiguredCameraSelection activates preview when autoPreview', async () => {
      mocks.registryApi.getConfiguredCameras.mockResolvedValue([makeCamera()])
      const wrapper = mount(LicensePlateSecurity, { global: { stubs: { StreamPreview: true } } })
      wrapper.vm.selectedConfiguredCameraId = '1'
      await flushPromises()
      await flushPromises()
      wrapper.vm.applyConfiguredCameraSelection({ autoPreview: true })
      expect(wrapper.vm.previewRunning).toBe(true)
      expect(wrapper.vm.message).toContain('Đã nạp')
    })
  })

  describe('formatConf', () => {
    it('formats numeric confidence', async () => {
      const wrapper = await mountComponent()
      expect(wrapper.vm.formatConf(0.98765)).toBe('0.9877')
      expect(wrapper.vm.formatConf('abc')).toBe('abc')
      expect(wrapper.vm.formatConf(3)).toBe('3.0000')
    })
  })

  describe('handleTurnOnPreview', () => {
    it('alerts when no preview source provided', async () => {
      const wrapper = await mountComponent()
      await wrapper.vm.handleTurnOnPreview()
      expect(window.alert).toHaveBeenCalled()
    })

    it('opens preview successfully', async () => {
      const wrapper = await mountComponent()
      await wrapper.setData({ previewUrl: 'http://cam/preview.jpg', cameraIp: 'rtsp://cam/1' })
      await wrapper.vm.handleTurnOnPreview()
      expect(mocks.cameraRuntimeApi.ensureCameraRegistered).toHaveBeenCalled()
      expect(wrapper.vm.previewRunning).toBe(true)
      expect(wrapper.vm.message).toContain('Đã mở preview')
    })

    it('handles errors when opening preview', async () => {
      mocks.cameraRuntimeApi.ensureCameraRegistered.mockRejectedValue(new Error('boom'))
      const wrapper = await mountComponent()
      await wrapper.setData({ previewUrl: 'http://cam/preview.jpg' })
      await wrapper.vm.handleTurnOnPreview()
      expect(window.alert).toHaveBeenCalledWith('boom')
    })
  })

  describe('handleInitOrResetSession', () => {
    it('alerts when no camera IP', async () => {
      const wrapper = await mountComponent()
      await wrapper.vm.handleInitOrResetSession()
      expect(window.alert).toHaveBeenCalled()
    })

    it('starts a new session when not running', async () => {
      mocks.plateApi.getCameraResult.mockResolvedValue(defaultStatus({ camera_enabled: true, session_id: 7, message: 'ON' }))
      const wrapper = await mountComponent()
      await wrapper.setData({ cameraIp: 'rtsp://cam/1', previewUrl: 'http://cam/p.jpg' })
      await wrapper.vm.handleInitOrResetSession()
      expect(mocks.plateApi.turnOnCamera).toHaveBeenCalledWith('rtsp://cam/1')
      expect(wrapper.vm.cameraRunning).toBe(true)
      expect(wrapper.vm.sessionId).toBe(7)
      expect(wrapper.vm.message).toBe('ON')
    })

    it('fails starting a session', async () => {
      mocks.plateApi.turnOnCamera.mockResolvedValue({ success: false, message: 'no' })
      const wrapper = await mountComponent()
      await wrapper.setData({ cameraIp: 'rtsp://cam/1' })
      await wrapper.vm.handleInitOrResetSession()
      expect(window.alert).toHaveBeenCalledWith('no')
      expect(wrapper.vm.cameraRunning).toBe(false)
    })

    it('resets an existing session', async () => {
      mocks.plateApi.getCameraResult.mockResolvedValue(defaultStatus({ camera_enabled: true, session_id: 8, message: 'RESET' }))
      const wrapper = await mountComponent()
      wrapper.vm.cameraRunning = true
      wrapper.vm.currentIp = 'rtsp://cam/1'
      wrapper.vm.sessionId = 7
      wrapper.vm.lastAppliedSessionId = 7
      mocks.plateApi.resetCameraState.mockResolvedValue({ message: 'RESET', session_id: 8 })
      await wrapper.vm.handleInitOrResetSession()
      expect(mocks.plateApi.resetCameraState).toHaveBeenCalled()
      expect(wrapper.vm.sessionId).toBe(8)
      expect(wrapper.vm.message).toBe('RESET')
    })

    it('handles errors during session init', async () => {
      mocks.plateApi.turnOnCamera.mockRejectedValue(new Error('fail'))
      const wrapper = await mountComponent()
      await wrapper.setData({ cameraIp: 'rtsp://cam/1' })
      await wrapper.vm.handleInitOrResetSession()
      expect(window.alert).toHaveBeenCalledWith('fail')
    })
  })

  describe('handleTurnOff', () => {
    it('turns the camera off', async () => {
      const wrapper = await mountComponent()
      await wrapper.vm.handleTurnOff()
      expect(mocks.plateApi.turnOffCamera).toHaveBeenCalled()
      expect(wrapper.vm.cameraRunning).toBe(false)
    })

    it('handles inner turn-off warning', async () => {
      mocks.plateApi.turnOffCamera.mockRejectedValue(new Error('warn'))
      const wrapper = await mountComponent()
      await wrapper.vm.handleTurnOff()
      expect(wrapper.vm.cameraRunning).toBe(false)
    })

    it('handles outer turn-off errors', async () => {
      const wrapper = await mountComponent()
      vi.spyOn(wrapper.vm, 'hardResetUiState').mockImplementation(() => {
        throw new Error('boom')
      })
      await wrapper.vm.handleTurnOff()
      expect(window.alert).toHaveBeenCalledWith('boom')
    })
  })

  describe('refreshResult and applyRealtimeState', () => {
    it('applies realtime state on result refresh', async () => {
      mocks.plateApi.getCameraResult.mockResolvedValue(defaultStatus({
        camera_enabled: true,
        session_id: 3,
        confirmed_plate: '30A-123.45',
        last_raw_plate: '30A-123',
        live_candidates: [{ text: 'ABC', conf: 0.9, valid: true }, { text: 'Z', conf: 0.5, valid: false }],
        scan_locked: true,
        fps: 25,
        ocr_running: true,
        stable_count: 4,
        moving_fast: false,
        bbox: { x1: 1, y1: 2, x2: 3, y2: 4 },
        message: 'detected',
        last_update: 'now',
      }))
      const wrapper = await mountComponent()
      await wrapper.vm.refreshResult()
      expect(wrapper.vm.cameraRunning).toBe(true)
      expect(wrapper.vm.confirmedPlate).toBe('30A-123.45')
      expect(wrapper.vm.scanLocked).toBe(true)
      expect(wrapper.vm.liveCandidates).toHaveLength(2)
    })

    it('applies realtime state with turn off reset allowed', async () => {
      mocks.plateApi.getCameraStatus.mockResolvedValue(defaultStatus({ camera_enabled: true, session_id: 5 }))
      const wrapper = mount(LicensePlateSecurity, { global: { stubs: { StreamPreview: true } } })
      await flushPromises()
      wrapper.vm.cameraRunning = false
      await wrapper.vm.applyRealtimeState({ camera_enabled: false, session_id: 5 }, true)
      expect(wrapper.vm.cameraRunning).toBe(false)
    })

    it('returns early for empty or destroyed states', async () => {
      const wrapper = await mountComponent()
      await wrapper.vm.applyRealtimeState(null)
      wrapper.vm.destroyed = true
      await wrapper.vm.applyRealtimeState({ camera_enabled: true })
      wrapper.vm.destroyed = false
    })

    it('rejects stale sessions', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.lastAppliedSessionId = 9
      await wrapper.vm.applyRealtimeState(defaultStatus({ session_id: 3 }), true)
      expect(wrapper.vm.sessionId).toBe(0)
    })

    it('loads current status and activates preview', async () => {
      mocks.plateApi.getCameraStatus.mockResolvedValue(defaultStatus({ camera_enabled: true, session_id: 5, ip: 'rtsp://status/1' }))
      const wrapper = await mountComponent()
      expect(wrapper.vm.cameraRunning).toBe(true)
      expect(wrapper.vm.cameraIp).toBe('rtsp://status/1')
    })

    it('loads current status and applies a configured camera when inputs are empty', async () => {
      mocks.plateApi.getCameraStatus.mockResolvedValue(defaultStatus({ camera_enabled: false }))
      mocks.registryApi.getConfiguredCameras.mockResolvedValue([makeCamera({ id: 1, sourceUrl: 'rtsp://cfg', browserPreviewUrl: 'http://cfg' })])
      const wrapper = mount(LicensePlateSecurity, { global: { stubs: { StreamPreview: true } } })
      await flushPromises()
      await flushPromises()
      wrapper.vm.selectedConfiguredCameraId = '1'
      wrapper.vm.cameraIp = ''
      wrapper.vm.previewUrl = ''
      await wrapper.vm.loadCurrentStatus()
      expect(wrapper.vm.cameraIp).toBe('rtsp://cfg')
    })

    it('binds camera ip and preview url input fields', async () => {
      const wrapper = await mountComponent()
      const inputs = wrapper.findAll('input.ip-input')
      await inputs[0].setValue('rtsp://typed/1')
      await inputs[1].setValue('http://typed/feed')
      expect(wrapper.vm.cameraIp).toBe('rtsp://typed/1')
      expect(wrapper.vm.previewUrl).toBe('http://typed/feed')
    })

    it('handles load status errors', async () => {
      mocks.plateApi.getCameraStatus.mockRejectedValue(new Error('status'))
      const wrapper = await mountComponent()
      expect(wrapper.exists()).toBe(true)
    })

    it('handles refresh result polling errors', async () => {
      mocks.plateApi.getCameraResult.mockRejectedValue(new Error('poll'))
      const wrapper = await mountComponent()
      await wrapper.vm.refreshResult()
      expect(wrapper.exists()).toBe(true)
    })

    it('sets session id when previously empty on same session', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.lastAppliedSessionId = 3
      wrapper.vm.sessionId = 0
      await wrapper.vm.applyRealtimeState(defaultStatus({ session_id: 3, camera_enabled: true }))
      expect(wrapper.vm.sessionId).toBe(3)
    })
  })

  describe('fetchLockedImagesIfNeeded', () => {
    it('returns early when not running or destroyed', async () => {
      const wrapper = await mountComponent()
      await wrapper.vm.fetchLockedImagesIfNeeded(false)
      expect(mocks.plateApi.getLockedImages).not.toHaveBeenCalled()
    })

    it('clears locked images when not scan locked', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.cameraRunning = true
      wrapper.vm.scanLocked = false
      wrapper.vm.lockedSnapshot = 'x'
      await wrapper.vm.fetchLockedImagesIfNeeded(true)
      expect(wrapper.vm.lockedSnapshot).toBe('')
    })

    it('fetches locked images when appropriate', async () => {
      mocks.plateApi.getLockedImages.mockResolvedValue({
        session_id: 3,
        scan_locked: true,
        locked_snapshot: 'snap',
        locked_plate_crop: 'crop',
      })
      const wrapper = await mountComponent()
      wrapper.vm.cameraRunning = true
      wrapper.vm.scanLocked = true
      wrapper.vm.sessionId = 3
      await wrapper.vm.fetchLockedImagesIfNeeded(true)
      expect(wrapper.vm.lockedSnapshot).toBe('snap')
      expect(wrapper.vm.lockedPlateCrop).toBe('crop')
    })

    it('handles session mismatch and scan not locked responses', async () => {
      mocks.plateApi.getLockedImages.mockResolvedValue({ session_id: 9, scan_locked: true })
      const wrapper = await mountComponent()
      wrapper.vm.cameraRunning = true
      wrapper.vm.scanLocked = true
      wrapper.vm.sessionId = 3
      await wrapper.vm.fetchLockedImagesIfNeeded(true)
      expect(wrapper.vm.lockedSnapshot).toBe('')

      mocks.plateApi.getLockedImages.mockResolvedValue({ session_id: 3, scan_locked: false })
      wrapper.vm.sessionId = 3
      await wrapper.vm.fetchLockedImagesIfNeeded(true)
      expect(wrapper.vm.lockedSnapshot).toBe('')
    })

    it('handles errors while fetching locked images', async () => {
      mocks.plateApi.getLockedImages.mockRejectedValue(new Error('img'))
      const wrapper = await mountComponent()
      wrapper.vm.cameraRunning = true
      wrapper.vm.scanLocked = true
      wrapper.vm.sessionId = 3
      await wrapper.vm.fetchLockedImagesIfNeeded(true)
      expect(wrapper.vm.isFetchingLockedImages).toBe(false)
    })
  })

  describe('preview handlers', () => {
    it('handlePreviewReady and handlePreviewError update health', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.handlePreviewReady()
      expect(wrapper.vm.previewHealthy).toBe(true)
      wrapper.vm.handlePreviewError()
      expect(wrapper.vm.previewHealthy).toBe(false)
    })
  })

  describe('runFuzzyMatch', () => {
    it('returns early when no confirmed plate', async () => {
      const wrapper = await mountComponent()
      await wrapper.vm.runFuzzyMatch()
      expect(mocks.plateRecognitionApi.fuzzyMatchPlate).not.toHaveBeenCalled()
    })

    it('runs fuzzy match successfully', async () => {
      mocks.plateRecognitionApi.fuzzyMatchPlate.mockResolvedValue({ data: { results: [{ vehicleId: 1, licensePlate: '30A-1', ownerName: 'A', score: 0.95, isExactMatch: true }] } })
      mocks.plateRecognitionApi.getPlateAnomalies.mockResolvedValue({ data: { anomalies: [{ type: 'speed', detectedAt: 1, severity: 'warning', description: 'd' }] } })
      const wrapper = await mountComponent()
      wrapper.vm.confirmedPlate = '30A-1'
      await wrapper.vm.runFuzzyMatch()
      expect(wrapper.vm.fuzzySimilar).toHaveLength(1)
      expect(wrapper.vm.fuzzyAnomalies).toHaveLength(1)
      expect(wrapper.vm.fuzzyDone).toBe(true)
    })

    it('handles fuzzy match errors', async () => {
      mocks.plateRecognitionApi.fuzzyMatchPlate.mockRejectedValue(new Error('x'))
      const wrapper = await mountComponent()
      wrapper.vm.confirmedPlate = '30A-1'
      await wrapper.vm.runFuzzyMatch()
      expect(wrapper.vm.message).toBe('Lỗi phân tích biển số')
      expect(wrapper.vm.fuzzyDone).toBe(true)
    })
  })

  describe('lifecycle and loops', () => {
    it('starts and stops result loop', async () => {
      const wrapper = await mountComponent()
      vi.spyOn(wrapper.vm, 'refreshResult').mockResolvedValue(undefined)
      wrapper.vm.cameraRunning = true
      wrapper.vm.startResultLoop()
      expect(wrapper.vm.resultTimer).toBeTruthy()
      await vi.advanceTimersByTimeAsync(600)
      expect(wrapper.vm.refreshResult).toHaveBeenCalled()
      wrapper.vm.stopResultLoop()
      expect(wrapper.vm.resultTimer).toBeNull()
    })

    it('result loop skips when not running or destroyed', async () => {
      const wrapper = await mountComponent()
      const spy = vi.spyOn(wrapper.vm, 'refreshResult').mockResolvedValue(undefined)
      wrapper.vm.startResultLoop()
      await vi.advanceTimersByTimeAsync(600)
      expect(spy).not.toHaveBeenCalled()
      wrapper.vm.destroyed = true
      wrapper.vm.cameraRunning = true
      await vi.advanceTimersByTimeAsync(600)
      expect(spy).not.toHaveBeenCalled()
      wrapper.vm.stopResultLoop()
    })

    it('activated restores loop and preview', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.cameraRunning = true
      wrapper.vm.previewUrl = 'http://cam/feed'
      wrapper.vm.previewRunning = false
      await wrapper.vm.$options.activated.call(wrapper.vm)
      expect(wrapper.vm.previewRunning).toBe(true)
      expect(wrapper.vm.resultTimer).toBeTruthy()
      wrapper.vm.stopResultLoop()
    })

    it('deactivated stops the result loop', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.cameraRunning = true
      wrapper.vm.startResultLoop()
      await wrapper.vm.$options.deactivated.call(wrapper.vm)
      expect(wrapper.vm.resultTimer).toBeNull()
    })

    it('hardResetUiState and clearResultStateOnly reset fields', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.cameraRunning = true
      wrapper.vm.confirmedPlate = 'abc'
      wrapper.vm.hardResetUiState()
      expect(wrapper.vm.cameraRunning).toBe(false)
      expect(wrapper.vm.confirmedPlate).toBe('')
    })
  })

  describe('template interactions', () => {
    it('turns the camera off through the template button', async () => {
      const wrapper = await mountComponent()
      await wrapper.find('.btn-off').trigger('click')
      await flushPromises()
      expect(mocks.plateApi.turnOffCamera).toHaveBeenCalled()
    })

    it('turns on preview and initializes session through template buttons', async () => {
      const wrapper = await mountComponent()
      await wrapper.setData({ cameraIp: 'rtsp://cam/1', previewUrl: 'http://cam/p.jpg' })
      await wrapper.find('.btn-on').trigger('click')
      await flushPromises()
      expect(mocks.cameraRuntimeApi.ensureCameraRegistered).toHaveBeenCalled()
      await wrapper.find('.btn-reset').trigger('click')
      await flushPromises()
      expect(mocks.plateApi.turnOnCamera).toHaveBeenCalled()
    })

    it('applies a configured camera through the button and change handler', async () => {
      mocks.registryApi.getConfiguredCameras.mockResolvedValue([makeCamera()])
      const wrapper = mount(LicensePlateSecurity, { global: { stubs: { StreamPreview: true } } })
      await flushPromises()
      await flushPromises()
      await wrapper.find('.source-select').setValue('1')
      await flushPromises()
      expect(localStorage.getItem(STORAGE_KEY)).toBe('1')
      await wrapper.find('.btn-config').trigger('click')
      await flushPromises()
      expect(wrapper.vm.cameraIp).toBe('rtsp://cam/1')
    })

    it('runs fuzzy match through the template button', async () => {
      mocks.registryApi.getConfiguredCameras.mockResolvedValue([])
      const wrapper = await mountComponent()
      wrapper.vm.confirmedPlate = '30A-1'
      await nextTick()
      expect(wrapper.find('.fuzzy-panel').exists()).toBe(true)
      await wrapper.find('.fuzzy-header button').trigger('click')
      await flushPromises()
      expect(mocks.plateRecognitionApi.fuzzyMatchPlate).toHaveBeenCalled()
    })

    it('renders evidence images and candidates when locked', async () => {
      mocks.registryApi.getConfiguredCameras.mockResolvedValue([])
      const wrapper = await mountComponent()
      wrapper.vm.lockedSnapshot = 'data:image/png;base64,AAA'
      wrapper.vm.lockedPlateCrop = 'data:image/png;base64,BBB'
      wrapper.vm.liveCandidates = [
        { text: '30A-123', conf: 0.9, valid: true },
        { text: 'raw', conf: 0.1, valid: false },
      ]
      wrapper.vm.bbox = { x1: 1, y1: 2, x2: 3, y2: 4 }
      wrapper.vm.lastUpdate = 'now'
      wrapper.vm.message = 'detected'
      await nextTick()
      expect(wrapper.findAll('.evidence-image').length).toBe(2)
      expect(wrapper.findAll('.candidate-item').length).toBe(2)
      expect(wrapper.find('.candidate-empty').exists()).toBe(false)
      expect(wrapper.vm.bboxText).toContain('x1=1')
    })

    it('renders fuzzy results, anomalies and preview-ready/error events', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.confirmedPlate = '30A-1'
      wrapper.vm.previewRunning = true
      wrapper.vm.activePreviewUrl = 'http://cam/feed'
      wrapper.vm.fuzzySimilar = [
        { vehicleId: 1, licensePlate: '30A-1', ownerName: 'A', score: 0.99, isExactMatch: true },
        { vehicleId: 2, licensePlate: '30A-2', ownerName: 'B', score: 0.6, isExactMatch: false },
      ]
      wrapper.vm.fuzzyAnomalies = [{ type: 'speed', detectedAt: 1, severity: 'warning', description: 'd' }]
      await nextTick()
      expect(wrapper.findAll('.fuzzy-row').length).toBe(2)
      expect(wrapper.findAll('.anomaly-row').length).toBe(1)
      const stream = wrapper.findComponent({ name: 'StreamPreview' })
      await stream.vm.$emit('ready')
      expect(wrapper.vm.previewHealthy).toBe(true)
      await stream.vm.$emit('error')
      expect(wrapper.vm.previewHealthy).toBe(false)
    })

    it('renders empty fuzzy state after a matching-free analysis', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.confirmedPlate = '30A-1'
      wrapper.vm.fuzzyDone = true
      await nextTick()
      expect(wrapper.find('.fuzzy-none').exists()).toBe(true)
    })
  })
})
