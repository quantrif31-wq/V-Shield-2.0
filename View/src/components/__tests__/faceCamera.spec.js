import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'

const mocks = vi.hoisted(() => ({
  faceApi: {
    startCamera: vi.fn(),
    stopCamera: vi.fn(),
    resetCamera: vi.fn(),
    getCameraStatus: vi.fn(),
    getCameraResult: vi.fn(),
    getLockedImages: vi.fn(),
    getCameras: vi.fn(),
    normalizeFaceApiError: vi.fn(),
    shouldStopFacePolling: vi.fn(),
  },
  cameraRuntimeApi: {
    ensureCameraRegistered: vi.fn(),
    getCameras: vi.fn(),
  },
  faceCameraConfigApi: {
    getFaceCameraConfigurations: vi.fn(),
    startConfiguredFaceCamera: vi.fn(),
    stopConfiguredFaceCamera: vi.fn(),
  },
  faceGateApi: {
    getFaceGates: vi.fn(),
    verifyFaceGatePassword: vi.fn(),
    checkGateAccess: vi.fn(),
    recordFaceGateResult: vi.fn(),
    getFaceIntruders: vi.fn(),
    deleteFaceIntruder: vi.fn(),
  },
  observability: {
    captureError: vi.fn(),
    recordMetric: vi.fn(),
  },
}))

vi.mock('../../services/faceApi', () => mocks.faceApi)
vi.mock('../../services/cameraRuntimeApi', () => mocks.cameraRuntimeApi)
vi.mock('../../services/faceCameraConfigurationApi', () => mocks.faceCameraConfigApi)
vi.mock('../../services/faceGateApi', () => mocks.faceGateApi)
vi.mock('../../services/observability', () => mocks.observability)

import FaceCamera from '../FaceCamera.vue'

function gate(gateId, gateName, location) {
  return { gateId, gateName, location }
}

function cam(cameraId, cameraName, overrides = {}) {
  return { cameraId, cameraName, streamUrl: 'rtsp://cam/' + cameraId, urlView: 'http://cam/' + cameraId + '.jpg', ...overrides }
}

function defaultStatus(overrides = {}) {
  return {
    camera_enabled: false,
    camera_connected: false,
    scan_locked: false,
    faces: [],
    ...overrides,
  }
}

function makeFace(overrides = {}) {
  return {
    id: 't1',
    status: 'confirmed',
    employee_id: '7',
    match: true,
    distance: 0.5,
    bbox: { left: 10, top: 20, right: 60, bottom: 80 },
    ...overrides,
  }
}

let wrapper

async function mountComponent() {
  wrapper = mount(FaceCamera)
  await flushPromises()
  await flushPromises()
  return wrapper
}

beforeEach(() => {
  localStorage.clear()
  vi.resetAllMocks()
  mocks.faceApi.getCameraStatus.mockResolvedValue(defaultStatus())
  mocks.faceApi.getCameraResult.mockResolvedValue(defaultStatus())
  mocks.faceApi.getLockedImages.mockResolvedValue({})
  mocks.faceApi.getCameras.mockResolvedValue({ sessions: [{ cameraId: 'monitoring-face-camera' }] })
  mocks.faceApi.startCamera.mockResolvedValue({ success: true, message: 'START' })
  mocks.faceApi.stopCamera.mockResolvedValue({ message: 'STOP' })
  mocks.faceApi.resetCamera.mockResolvedValue({ message: 'RESET' })
  mocks.faceApi.normalizeFaceApiError.mockImplementation((error) => ({
    cancelled: false,
    code: error?.response?.status || 'x',
    message: error?.message || 'err',
  }))
  mocks.faceApi.shouldStopFacePolling.mockReturnValue(false)
  mocks.cameraRuntimeApi.getCameras.mockResolvedValue([])
  mocks.cameraRuntimeApi.ensureCameraRegistered.mockResolvedValue({ cameraId: 1, urlView: 'http://preview/stream.html' })
  mocks.faceCameraConfigApi.getFaceCameraConfigurations.mockResolvedValue([])
  mocks.faceCameraConfigApi.startConfiguredFaceCamera.mockResolvedValue({ success: true })
  mocks.faceCameraConfigApi.stopConfiguredFaceCamera.mockResolvedValue({ success: true })
  mocks.faceGateApi.getFaceGates.mockResolvedValue({ gates: [] })
  mocks.faceGateApi.verifyFaceGatePassword.mockResolvedValue({ success: true })
  mocks.faceGateApi.checkGateAccess.mockResolvedValue({ success: true, allowed: true })
  mocks.faceGateApi.recordFaceGateResult.mockResolvedValue({ success: true })
  mocks.faceGateApi.getFaceIntruders.mockResolvedValue({ items: [], total: 0 })
  mocks.faceGateApi.deleteFaceIntruder.mockResolvedValue({ success: true })
  mocks.observability.captureError.mockReturnValue(undefined)
  mocks.observability.recordMetric.mockReturnValue(undefined)
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

describe('FaceCamera.vue', () => {
  describe('mounted and gates', () => {
    it('loads gates, cameras, status and intruders on mount', async () => {
      mocks.faceGateApi.getFaceGates.mockResolvedValue({ gates: [gate(1, 'Cổng A', 'Tòa A'), gate(2, 'Cổng B')] })
      mocks.cameraRuntimeApi.getCameras.mockResolvedValue([cam(1, 'CAM1'), cam(2, 'CAM2', { location: 'X' })])
      const wrapper = await mountComponent()
      expect(mocks.faceGateApi.getFaceGates).toHaveBeenCalled()
      expect(mocks.cameraRuntimeApi.getCameras).toHaveBeenCalled()
      expect(mocks.faceApi.getCameraStatus).toHaveBeenCalled()
      expect(mocks.faceGateApi.getFaceIntruders).toHaveBeenCalled()
      expect(wrapper.vm.gates).toHaveLength(2)
      expect(wrapper.vm.allCameras).toHaveLength(2)
    })

    it('does not poll a FaceID session that has never been created', async () => {
      mocks.faceApi.getCameras.mockResolvedValue({ sessions: [] })
      const wrapper = await mountComponent()
      expect(mocks.faceApi.getCameraStatus).not.toHaveBeenCalled()
      expect(wrapper.vm.message).toContain('Chưa khởi động')
    })

    it('starts result loop on mount when camera is running', async () => {
      mocks.faceApi.getCameraStatus.mockResolvedValue(defaultStatus({ camera_enabled: true, scan_locked: false, ip: 'rtsp://live' }))
      const wrapper = await mountComponent()
      expect(wrapper.vm.cameraRunning).toBe(true)
      expect(wrapper.vm.resultTimer).toBeTruthy()
      wrapper.vm.stopResultLoop()
    })

    it('loads gates and cameras even on errors', async () => {
      mocks.faceGateApi.getFaceGates.mockRejectedValue(new Error('gates'))
      mocks.cameraRuntimeApi.getCameras.mockRejectedValue(new Error('cams'))
      const wrapper = await mountComponent()
      expect(wrapper.vm.gates).toEqual([])
      expect(wrapper.vm.allCameras).toEqual([])
    })

    it('handles mount status errors', async () => {
      mocks.faceApi.getCameraStatus.mockRejectedValue(new Error('status'))
      const wrapper = await mountComponent()
      expect(wrapper.vm.faceServiceError).toBeTruthy()
    })
  })

  describe('computed', () => {
    it('computes activeCameraId from props', async () => {
      const wrapper = await mountComponent()
      expect(wrapper.vm.activeCameraId).toBe('monitoring-face-camera')
    })

    it('maps liveFaces with access state', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.faces = [
        { match: true, employee_id: '7', status: 'confirmed', accessState: 'allowed' },
        { match: false, status: 'intruder' },
      ]
      const faces = wrapper.vm.liveFaces
      expect(faces[0].allowed).toBe(true)
      expect(faces[0].known).toBe(true)
      expect(faces[0].accessState).toBe('allowed')
      expect(faces[1].status).toBe('intruder')
    })

    it('computes detectionLabel across states', async () => {
      const wrapper = await mountComponent()
      expect(wrapper.vm.detectionLabel).toBe('Idle')
      wrapper.vm.trackingActive = true
      expect(wrapper.vm.detectionLabel).toBe('Chưa nhận diện')
      wrapper.vm.faceMatch = true
      expect(wrapper.vm.detectionLabel).toBe('Đang xác minh')
      wrapper.vm.faceMatch = false
      wrapper.vm.identityConfirmed = true
      expect(wrapper.vm.detectionLabel).toBe('Đã nhận diện')
      wrapper.vm.scanLocked = true
      wrapper.vm.lockReason = 'confirmed'
      expect(wrapper.vm.detectionLabel).toBe('Đã xác nhận danh tính')
      wrapper.vm.lockReason = 'timeout'
      expect(wrapper.vm.detectionLabel).toBe('Hết thời gian chờ')
      wrapper.vm.lockReason = 'alert'
      expect(wrapper.vm.detectionLabel).toBe('Cảnh báo')
      wrapper.vm.lockReason = 'other'
      expect(wrapper.vm.detectionLabel).toBe('Đã khóa')
    })

    it('computes detectionState', async () => {
      const wrapper = await mountComponent()
      expect(wrapper.vm.detectionState).toBe('idle')
      wrapper.vm.trackingActive = true
      expect(wrapper.vm.detectionState).toBe('track')
      wrapper.vm.faceMatch = true
      expect(wrapper.vm.detectionState).toBe('verify')
      wrapper.vm.identityConfirmed = true
      expect(wrapper.vm.detectionState).toBe('hit')
      wrapper.vm.scanLocked = true
      wrapper.vm.lockReason = 'confirmed'
      expect(wrapper.vm.detectionState).toBe('hit')
      wrapper.vm.lockReason = 'alert'
      expect(wrapper.vm.detectionState).toBe('locked')
    })

    it('computes distanceText', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.distance = 'abc'
      expect(wrapper.vm.distanceText).toBe('— — — —')
      wrapper.vm.distance = 1.23456
      expect(wrapper.vm.distanceText).toBe('1.2346')
    })
  })

  describe('face box and label helpers', () => {
    it('categorizes box class', async () => {
      const wrapper = await mountComponent()
      expect(wrapper.vm.faceBoxClass({ status: 'confirmed', accessState: 'allowed' })).toBe('box-ok')
      expect(wrapper.vm.faceBoxClass({ status: 'confirmed', accessState: 'denied' })).toBe('box-denied')
      expect(wrapper.vm.faceBoxClass({ status: 'confirmed', accessState: 'blacklist' })).toBe('box-denied')
      expect(wrapper.vm.faceBoxClass({ status: 'confirmed', accessState: 'unknown' })).toBe('box-denied')
      expect(wrapper.vm.faceBoxClass({ status: 'intruder' })).toBe('box-denied')
      expect(wrapper.vm.faceBoxClass({ status: 'pending' })).toBe('box-pending')
    })

    it('categorizes id class', async () => {
      const wrapper = await mountComponent()
      expect(wrapper.vm.faceIdClass({ status: 'confirmed', accessState: 'allowed' })).toBe('id-ok')
      expect(wrapper.vm.faceIdClass({ status: 'confirmed', accessState: 'denied' })).toBe('id-denied')
      expect(wrapper.vm.faceIdClass({ status: 'intruder' })).toBe('id-denied')
      expect(wrapper.vm.faceIdClass({ status: 'x' })).toBe('id-pending')
    })

    it('labels faces', async () => {
      const wrapper = await mountComponent()
      expect(wrapper.vm.faceLabel({ status: 'confirmed', employee_id: '7', known: true, accessState: 'allowed' })).toBe('NV-7 ✓')
      expect(wrapper.vm.faceLabel({ status: 'confirmed', employee_id: '7', known: true, accessState: 'denied' })).toBe('NV-7 ✕')
      expect(wrapper.vm.faceLabel({ status: 'confirmed', employee_id: '7', known: true, accessState: 'blacklist' })).toBe('NV-7 🚫')
      expect(wrapper.vm.faceLabel({ status: 'confirmed', employee_id: '7', known: true, accessState: 'pending' })).toBe('NV-7 …')
      expect(wrapper.vm.faceLabel({ status: 'intruder' })).toBe('???')
    })

    it('prefixId formats known and unknown', async () => {
      const wrapper = await mountComponent()
      expect(wrapper.vm.prefixId('7', true)).toBe('NV-7')
      expect(wrapper.vm.prefixId('7', false)).toBe('KH-7')
    })

    it('formats time', async () => {
      const wrapper = await mountComponent()
      expect(wrapper.vm.fmtTime('')).toBe('')
      expect(wrapper.vm.fmtTime(new Date('2023-01-01T12:00:00'))).toBeTruthy()
    })

    it('falls back to the raw value when toLocaleString fails', async () => {
      const spy = vi.spyOn(Date.prototype, 'toLocaleString').mockImplementation(() => {
        throw new Error('locale-fail')
      })
      const wrapper = await mountComponent()
      expect(wrapper.vm.fmtTime('2023-01-01T00:00:00Z')).toBe('2023-01-01T00:00:00Z')
      spy.mockRestore()
    })

    it('badgeLabel maps reasons', async () => {
      const wrapper = await mountComponent()
      expect(wrapper.vm.badgeLabel('unknown')).toBe('Không nhận diện')
      expect(wrapper.vm.badgeLabel('denied')).toBe('Từ chối')
      expect(wrapper.vm.badgeLabel('blacklist')).toBe('Danh sách đen')
      expect(wrapper.vm.badgeLabel('other')).toBe('other')
    })

    it('builds box style percentages', async () => {
      const wrapper = await mountComponent()
      const style = wrapper.vm.faceBoxStyle({ bbox: { left: 0, top: 0, right: 480, bottom: 270 } })
      expect(style.left).toBe('0%')
      expect(style.width).toBe('100%')
    })
  })

  describe('gate selection and password modal', () => {
    it('opens the password modal on gate change', async () => {
      mocks.faceGateApi.getFaceGates.mockResolvedValue({ gates: [gate(1, 'Cổng A')] })
      const wrapper = await mountComponent()
      wrapper.vm.selectedGateId = 1
      wrapper.vm.onGateChange()
      expect(wrapper.vm.showPasswordModal).toBe(true)
      expect(wrapper.vm.pendingGateName).toBe('Cổng A')
    })

    it('does nothing when gate change has no match', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.selectedGateId = 99
      wrapper.vm.onGateChange()
      expect(wrapper.vm.showPasswordModal).toBe(false)
    })

    it('confirms the gate password successfully', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.pendingGateName = 'Cổng A'
      wrapper.vm.pendingGateId = 1
      wrapper.vm.passwordInput = 'abc'
      await wrapper.vm.confirmPassword()
      expect(mocks.faceGateApi.verifyFaceGatePassword).toHaveBeenCalledWith('abc')
      expect(wrapper.vm.activeGateName).toBe('Cổng A')
      expect(wrapper.vm.showPasswordModal).toBe(false)
      expect(wrapper.vm.cameraRunning).toBe(false)
    })

    it('shows error when password verification fails', async () => {
      mocks.faceGateApi.verifyFaceGatePassword.mockRejectedValue({ response: { data: { message: 'Sai' } } })
      const wrapper = await mountComponent()
      wrapper.vm.passwordInput = 'bad'
      await wrapper.vm.confirmPassword()
      expect(wrapper.vm.passwordError).toBe('Sai')
    })

    it('shows generic error on password failure', async () => {
      mocks.faceGateApi.verifyFaceGatePassword.mockRejectedValue(new Error('boom'))
      const wrapper = await mountComponent()
      await wrapper.vm.confirmPassword()
      expect(wrapper.vm.passwordError).toBe('boom')
    })
  })

  describe('camera change', () => {
    it('handles camera change and mounts registered preview', async () => {
      mocks.cameraRuntimeApi.getCameras.mockResolvedValue([cam(1, 'CAM1')])
      mocks.cameraRuntimeApi.ensureCameraRegistered.mockResolvedValue({ cameraId: 1, urlView: 'http://preview/stream.html' })
      const wrapper = await mountComponent()
      wrapper.vm.cameraSearch = 'CAM1'
      await wrapper.vm.onCameraChange()
      expect(wrapper.vm.cameraIp).toBe('rtsp://cam/1')
      expect(wrapper.vm.message).toContain('CAM1')
      expect(wrapper.vm.previewRunning).toBe(false)
    })

    it('does nothing when camera change has no match', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.cameraSearch = 'NOPE'
      await wrapper.vm.onCameraChange()
      expect(wrapper.vm.cameraIp).toBe('')
    })

    it('handles camera change registration errors', async () => {
      mocks.cameraRuntimeApi.getCameras.mockResolvedValue([cam(1, 'CAM1')])
      mocks.cameraRuntimeApi.ensureCameraRegistered.mockRejectedValue(new Error('reg'))
      const wrapper = await mountComponent()
      wrapper.vm.cameraSearch = 'CAM1'
      await wrapper.vm.onCameraChange()
      expect(wrapper.vm.cameraIp).toBe('rtsp://cam/1')
    })
  })

  describe('handleStartOrReset', () => {
    it('alerts when no camera ip', async () => {
      const wrapper = await mountComponent()
      await wrapper.vm.handleStartOrReset()
      expect(window.alert).toHaveBeenCalledWith('Vui lòng chọn camera trước')
    })

    it('alerts when no gate selected', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.cameraIp = 'rtsp://x'
      await wrapper.vm.handleStartOrReset()
      expect(window.alert).toHaveBeenCalledWith('Vui lòng chọn cổng trước')
    })

    it('starts a new camera session', async () => {
      mocks.faceApi.getCameraResult.mockResolvedValue(defaultStatus({ camera_enabled: true, scan_locked: false, message: 'START' }))
      const wrapper = await mountComponent()
      wrapper.vm.cameraIp = 'rtsp://x'
      wrapper.vm.activeGateName = 'Cổng A'
      mocks.cameraRuntimeApi.ensureCameraRegistered.mockResolvedValue({ urlView: 'http://preview/stream.html' })
      await wrapper.vm.handleStartOrReset()
      expect(mocks.faceApi.startCamera).toHaveBeenCalled()
      expect(wrapper.vm.cameraRunning).toBe(true)
      wrapper.vm.stopResultLoop()
    })

    it('starts FaceID through go2rtc when the selected camera has a relay preview', async () => {
      mocks.cameraRuntimeApi.getCameras.mockResolvedValue([
        cam(1, 'CAM1', { urlView: 'http://go2rtc/stream.html?src=cam1' })
      ])
      const wrapper = await mountComponent()
      wrapper.vm.cameraSearch = 'CAM1'
      wrapper.vm.cameraIp = 'rtsp://camera-lan/stream'
      wrapper.vm.activeGateName = 'Cổng A'

      await wrapper.vm.handleStartOrReset()

      expect(mocks.faceApi.startCamera).toHaveBeenCalledWith(
        'monitoring-face-camera', 'rtsp://go2rtc:8554/cam1', null
      )
      wrapper.vm.stopResultLoop()
    })

    it('fails to start when result not successful', async () => {
      mocks.faceApi.startCamera.mockResolvedValue({ success: false, message: 'no' })
      const wrapper = await mountComponent()
      wrapper.vm.cameraIp = 'rtsp://x'
      wrapper.vm.activeGateName = 'Cổng A'
      await wrapper.vm.handleStartOrReset()
      expect(window.alert).toHaveBeenCalledWith('no')
      expect(wrapper.vm.cameraRunning).toBe(false)
    })

    it('resets the running camera session', async () => {
      mocks.faceApi.getCameraResult.mockResolvedValue(defaultStatus({ camera_enabled: true, scan_locked: false, message: 'RESET' }))
      const wrapper = await mountComponent()
      wrapper.vm.cameraIp = 'rtsp://x'
      wrapper.vm.activeGateName = 'Cổng A'
      wrapper.vm.cameraRunning = true
      wrapper.vm.previewRunning = true
      await wrapper.vm.handleStartOrReset()
      expect(mocks.faceApi.resetCamera).toHaveBeenCalled()
      expect(wrapper.vm.message).toBe('RESET')
    })

    it('handles start errors', async () => {
      mocks.faceApi.startCamera.mockRejectedValue(new Error('boom'))
      const wrapper = await mountComponent()
      wrapper.vm.cameraIp = 'rtsp://x'
      wrapper.vm.activeGateName = 'Cổng A'
      await wrapper.vm.handleStartOrReset()
      expect(mocks.observability.captureError).toHaveBeenCalled()
      expect(mocks.observability.recordMetric).toHaveBeenCalled()
    })
  })

  describe('handleTurnOff', () => {
    it('turns the camera off', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.cameraRunning = true
      await wrapper.vm.handleTurnOff()
      expect(mocks.faceApi.stopCamera).toHaveBeenCalled()
      expect(wrapper.vm.cameraRunning).toBe(false)
    })

    it('handles stop 404 as already stopped', async () => {
      mocks.faceApi.stopCamera.mockRejectedValue({ status: 404 })
      const wrapper = await mountComponent()
      await wrapper.vm.handleTurnOff()
      expect(mocks.faceApi.stopCamera).toHaveBeenCalled()
      expect(wrapper.vm.clearFaceServiceError).toBeDefined()
      expect(wrapper.vm.cameraRunning).toBe(false)
    })

    it('handles other stop errors via face service error', async () => {
      mocks.faceApi.stopCamera.mockRejectedValue({ status: 500 })
      const wrapper = await mountComponent()
      await wrapper.vm.handleTurnOff()
      expect(wrapper.vm.faceServiceError).toBeTruthy()
    })

    it('handles outer turn off errors', async () => {
      const wrapper = await mountComponent()
      vi.spyOn(wrapper.vm, 'hardResetUiState').mockImplementation(() => {
        throw new Error('boom')
      })
      await wrapper.vm.handleTurnOff()
      expect(wrapper.vm.faceServiceError).toBeTruthy()
    })
  })

  describe('loadCurrentStatus and refreshResult', () => {
    it('loads current status and mounts direct preview when running', async () => {
      mocks.faceApi.getCameraStatus.mockResolvedValue(defaultStatus({ camera_enabled: true, ip: 'rtsp://live', faces: [] }))
      const wrapper = await mountComponent()
      wrapper.vm.currentIp = 'rtsp://live'
      await wrapper.vm.loadCurrentStatus()
      expect(wrapper.vm.previewRunning).toBe(true)
    })

    it('refreshResult applies realtime state', async () => {
      mocks.faceApi.getCameraResult.mockResolvedValue(defaultStatus({
        camera_enabled: true,
        scan_locked: false,
        faces: [],
        message: 'scanning',
      }))
      const wrapper = await mountComponent()
      await wrapper.vm.refreshResult()
      expect(wrapper.vm.message).toBe('scanning')
    })

    it('handles refreshResult errors', async () => {
      mocks.faceApi.getCameraResult.mockRejectedValue(new Error('poll'))
      const wrapper = await mountComponent()
      await wrapper.vm.refreshResult()
      expect(wrapper.vm.faceServiceError).toBeTruthy()
    })
  })

  describe('applyRealtimeState', () => {
    it('returns early when res is null', async () => {
      const wrapper = await mountComponent()
      await wrapper.vm.applyRealtimeState(null)
      expect(wrapper.vm.cameraRunning).toBe(false)
    })

    it('applies state and turns off when reset allowed', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.cameraRunning = true
      await wrapper.vm.applyRealtimeState(defaultStatus({ camera_enabled: false, faces: [] }), true)
      expect(wrapper.vm.cameraRunning).toBe(false)
    })
  })

  describe('resolveFaces', () => {
    it('returns early when camera not running', async () => {
      const wrapper = await mountComponent()
      await wrapper.vm.resolveFaces()
      expect(mocks.faceGateApi.checkGateAccess).not.toHaveBeenCalled()
    })

    it('records unknown intruders once per track', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.cameraRunning = true
      wrapper.vm.faces = [{ id: 'tk1', status: 'intruder', snapshot_b64: 's', crop_b64: 'c' }]
      await wrapper.vm.resolveFaces()
      expect(mocks.faceGateApi.recordFaceGateResult).toHaveBeenCalledTimes(1)
      await wrapper.vm.resolveFaces()
      expect(mocks.faceGateApi.recordFaceGateResult).toHaveBeenCalledTimes(1)
    })

    it('skips unknown intruders without employee and isIntruder false', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.cameraRunning = true
      wrapper.vm.faces = [{ id: 'tk0', status: 'new' }]
      await wrapper.vm.resolveFaces()
      expect(mocks.faceGateApi.recordFaceGateResult).not.toHaveBeenCalled()
    })

    it('records an allowed known face once', async () => {
      mocks.faceGateApi.checkGateAccess.mockResolvedValue({ success: true, allowed: true, employeeName: 'An' })
      const wrapper = await mountComponent()
      wrapper.vm.cameraRunning = true
      wrapper.vm.faces = [makeFace({ employee_id: '7', id: 'tk2' })]
      await wrapper.vm.resolveFaces()
      expect(mocks.faceGateApi.checkGateAccess).toHaveBeenCalledWith(7, null)
      expect(mocks.faceGateApi.recordFaceGateResult).toHaveBeenCalledTimes(1)
      expect(wrapper.vm.faces[0].accessState).toBe('allowed')
    })

    it('records blacklisted and denied faces and loads intruder count', async () => {
      mocks.faceGateApi.checkGateAccess.mockResolvedValueOnce({ success: true, blacklist: true, employeeName: 'B', blacklistReason: 'r' })
        .mockResolvedValueOnce({ success: true, allowed: false, employeeName: 'C', reason: 'nope' })
      const wrapper = await mountComponent()
      wrapper.vm.cameraRunning = true
      wrapper.vm.faces = [makeFace({ employee_id: '8', id: 'tk-a' }), makeFace({ employee_id: '9', id: 'tk-b' })]
      await wrapper.vm.resolveFaces()
      expect(wrapper.vm.faces[0].accessState).toBe('blacklist')
      expect(wrapper.vm.faces[1].accessState).toBe('denied')
      expect(mocks.faceGateApi.getFaceIntruders).toHaveBeenCalled()
    })

    it('marks unknown access state and handles errors', async () => {
      mocks.faceGateApi.checkGateAccess.mockResolvedValueOnce({ success: true, allowed: null, employeeName: 'D' })
        .mockRejectedValueOnce(new Error('acct'))
      const wrapper = await mountComponent()
      wrapper.vm.cameraRunning = true
      wrapper.vm.faces = [makeFace({ employee_id: '10', id: 'tk-c' }), makeFace({ employee_id: '11', id: 'tk-d' })]
      await wrapper.vm.resolveFaces()
      expect(wrapper.vm.faces[0].accessState).toBe('unknown')
      expect(wrapper.vm.faces[1].accessState).toBe('unknown')
    })

    it('handles unknown intruder record errors', async () => {
      mocks.faceGateApi.recordFaceGateResult.mockRejectedValueOnce(new Error('rec'))
      const wrapper = await mountComponent()
      wrapper.vm.cameraRunning = true
      wrapper.vm.faces = [{ id: 'tk-e', status: 'intruder' }]
      await wrapper.vm.resolveFaces()
      expect(wrapper.vm.recordedTrackIds.has('tk-e')).toBe(false)
    })
  })

  describe('intruders', () => {
    it('loads intruders with a filter', async () => {
      mocks.faceGateApi.getFaceIntruders.mockResolvedValue({ items: [{ id: 1, reason: 'unknown' }], total: 1 })
      const wrapper = await mountComponent()
      await wrapper.vm.loadIntruders('unknown')
      expect(wrapper.vm.intruderFilter).toBe('unknown')
      expect(wrapper.vm.intruders).toHaveLength(1)
      expect(wrapper.vm.intruderCount).toBe(1)
    })

    it('handles load intruders errors', async () => {
      mocks.faceGateApi.getFaceIntruders.mockRejectedValue(new Error('int'))
      const wrapper = await mountComponent()
      await wrapper.vm.loadIntruders('denied')
      expect(wrapper.vm.intruders).toEqual([])
    })

    it('loads intruder count', async () => {
      mocks.faceGateApi.getFaceIntruders.mockResolvedValue({ total: 3 })
      const wrapper = await mountComponent()
      await wrapper.vm.loadIntruderCount()
      expect(wrapper.vm.intruderCount).toBe(3)
    })

    it('deletes a single intruder', async () => {
      const wrapper = await mountComponent()
      await wrapper.vm.deleteOneIntruder(5)
      expect(mocks.faceGateApi.deleteFaceIntruder).toHaveBeenCalledWith(5)
    })

    it('handles delete intruder errors', async () => {
      mocks.faceGateApi.deleteFaceIntruder.mockRejectedValue(new Error('del'))
      const wrapper = await mountComponent()
      await wrapper.vm.deleteOneIntruder(5)
      expect(wrapper.exists()).toBe(true)
    })

    it('clears all intruders after confirm', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.intruders = [{ id: 1 }, { id: 2 }]
      await wrapper.vm.clearAllIntruders()
      expect(mocks.faceGateApi.deleteFaceIntruder).toHaveBeenCalledTimes(2)
    })

    it('does not clear all intruders when not confirmed', async () => {
      window.confirm.mockImplementation(() => false)
      const wrapper = await mountComponent()
      wrapper.vm.intruders = [{ id: 1 }]
      await wrapper.vm.clearAllIntruders()
      expect(mocks.faceGateApi.deleteFaceIntruder).not.toHaveBeenCalled()
    })
  })

  describe('loops', () => {
    it('starts and stops intruder loop', async () => {
      const wrapper = await mountComponent()
      vi.spyOn(wrapper.vm, 'loadIntruderCount').mockResolvedValue(undefined)
      wrapper.vm.startIntruderLoop()
      await vi.advanceTimersByTimeAsync(5100)
      expect(wrapper.vm.loadIntruderCount).toHaveBeenCalled()
      wrapper.vm.stopIntruderLoop()
      expect(wrapper.vm.intruderTimer).toBeNull()
    })

    it('does not load count on intruder tick when destroyed', async () => {
      const wrapper = await mountComponent()
      vi.spyOn(wrapper.vm, 'loadIntruderCount').mockResolvedValue(undefined)
      wrapper.vm.destroyed = true
      wrapper.vm.startIntruderLoop()
      await vi.advanceTimersByTimeAsync(5100)
      expect(wrapper.vm.loadIntruderCount).not.toHaveBeenCalled()
      wrapper.vm.stopIntruderLoop()
    })

    it('startResultLoop refreshes result when running', async () => {
      const wrapper = await mountComponent()
      vi.spyOn(wrapper.vm, 'refreshResult').mockResolvedValue(undefined)
      wrapper.vm.cameraRunning = true
      wrapper.vm.startResultLoop()
      await vi.advanceTimersByTimeAsync(600)
      expect(wrapper.vm.refreshResult).toHaveBeenCalled()
      wrapper.vm.stopResultLoop()
    })

    it('startResultLoop skips when not running or busy', async () => {
      const wrapper = await mountComponent()
      vi.spyOn(wrapper.vm, 'refreshResult').mockResolvedValue(undefined)
      wrapper.vm.startResultLoop()
      await vi.advanceTimersByTimeAsync(600)
      expect(wrapper.vm.refreshResult).not.toHaveBeenCalled()
      wrapper.vm.stopResultLoop()
    })
  })

  describe('state helpers', () => {
    it('clearResultStateOnly resets recognition fields', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.confirmedEmployeeId = '7'
      wrapper.vm.faces = [{}]
      wrapper.vm.clearResultStateOnly()
      expect(wrapper.vm.confirmedEmployeeId).toBe('')
      expect(wrapper.vm.faces).toEqual([])
      expect(wrapper.vm.recordedTrackIds.size).toBe(0)
    })

    it('hardResetUiState resets camera state', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.cameraRunning = true
      wrapper.vm.hardResetUiState()
      expect(wrapper.vm.cameraRunning).toBe(false)
    })

    it('clearFaceServiceError resets error', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.faceServiceError = { code: 'x' }
      wrapper.vm.clearFaceServiceError()
      expect(wrapper.vm.faceServiceError).toBeNull()
    })
  })

  describe('handleFaceServiceError', () => {
    it('returns early when cancelled or destroyed', async () => {
      mocks.faceApi.normalizeFaceApiError.mockReturnValue({ cancelled: true })
      const wrapper = await mountComponent()
      wrapper.vm.handleFaceServiceError(new Error('x'))
      expect(wrapper.vm.faceServiceError).toBeNull()
      mocks.faceApi.normalizeFaceApiError.mockReturnValue({ cancelled: false })
      wrapper.vm.destroyed = true
      wrapper.vm.handleFaceServiceError(new Error('x'))
      wrapper.vm.destroyed = false
    })

    it('sets error, stops poll and alerts when not polling', async () => {
      mocks.faceApi.normalizeFaceApiError.mockReturnValue({ cancelled: false, code: 'forbidden', message: 'stop' })
      mocks.faceApi.shouldStopFacePolling.mockReturnValue(true)
      const wrapper = await mountComponent()
      wrapper.vm.handleFaceServiceError(new Error('x'))
      expect(wrapper.vm.faceServiceError.message).toBe('stop')
      expect(window.alert).toHaveBeenCalledWith('stop')
    })

    it('does not alert when polling', async () => {
      mocks.faceApi.normalizeFaceApiError.mockReturnValue({ cancelled: false, code: 'x', message: 'poll' })
      const wrapper = await mountComponent()
      wrapper.vm.handleFaceServiceError(new Error('x'), { polling: true })
      expect(window.alert).not.toHaveBeenCalled()
    })
  })

  describe('direct preview helpers', () => {
    it('mounts direct preview', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.previewRetryTimer = setTimeout(() => {}, 100)
      wrapper.vm.mountDirectPreview('rtsp://live')
      expect(wrapper.vm.directCameraSourceUrl).toBe('rtsp://live')
      expect(wrapper.vm.previewRunning).toBe(true)
    })

    it('does nothing when mounting empty url', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.mountDirectPreview('   ')
      expect(wrapper.vm.previewRunning).toBe(false)
    })

    it('resets direct preview', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.previewRetryTimer = setTimeout(() => {}, 100)
      wrapper.vm.previewRunning = true
      wrapper.vm.directCameraUrl = 'http://x'
      wrapper.vm.resetDirectPreview()
      expect(wrapper.vm.previewRunning).toBe(false)
      expect(wrapper.vm.directCameraUrl).toBe('')
    })

    it('mounts registered preview with stream.html url', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.mountRegisteredPreview({ urlView: 'http://phost/stream.html' }, 'rtsp://s')
      expect(wrapper.vm.previewRunning).toBe(true)
      expect(wrapper.vm.directCameraUrl).toContain('stream.html')
    })

    it('falls back to preview url when URL parsing throws', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.mountRegisteredPreview({ urlView: 'http://[bad' }, 'rtsp://s')
      expect(wrapper.vm.previewRunning).toBe(true)
    })

    it('mounts registered preview with direct http url', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.mountRegisteredPreview({}, 'http://cam/live')
      expect(wrapper.vm.previewRunning).toBe(true)
    })

    it('throws when registered preview has no valid url', async () => {
      const wrapper = await mountComponent()
      expect(() => wrapper.vm.mountRegisteredPreview({}, 'rtsp://only')).toThrow('Camera chưa có URL preview')
    })

    it('builds direct camera url with timestamp', async () => {
      const wrapper = await mountComponent()
      expect(wrapper.vm.buildDirectCameraUrl('')).toBe('')
      expect(wrapper.vm.buildDirectCameraUrl('http://x/feed')).toContain('?t=')
      expect(wrapper.vm.buildDirectCameraUrl('http://x/feed?q=1')).toContain('&t=')
    })

    it('detects image urls', async () => {
      const wrapper = await mountComponent()
      expect(wrapper.vm.isImageUrl('')).toBe(false)
      expect(wrapper.vm.isImageUrl(123)).toBe(false)
      expect(wrapper.vm.isImageUrl('http://x/a.jpg')).toBe(true)
      expect(wrapper.vm.isImageUrl('http://x/a.jpeg')).toBe(true)
      expect(wrapper.vm.isImageUrl('http://x/a.png')).toBe(true)
      expect(wrapper.vm.isImageUrl('http://x/a.webp')).toBe(true)
      expect(wrapper.vm.isImageUrl('http://x/frame.jpg')).toBe(true)
      expect(wrapper.vm.isImageUrl('http://x/qr/frame.jpg')).toBe(true)
      expect(wrapper.vm.isImageUrl('http://x/plate/frame.jpg')).toBe(true)
      expect(wrapper.vm.isImageUrl('http://x/video_feed')).toBe(true)
      expect(wrapper.vm.isImageUrl('data:image/png;base64,xx')).toBe(true)
      expect(wrapper.vm.isImageUrl('http://x/snapshot')).toBe(true)
      expect(wrapper.vm.isImageUrl('http://x/feed.mp4')).toBe(false)
      expect(wrapper.vm.isImageUrl(null)).toBe(false)
    })
  })

  describe('handleDoubleClick', () => {
    it('requests fullscreen when no fullscreen element', async () => {
      const requestFullscreen = vi.fn().mockResolvedValue(undefined)
      Object.defineProperty(document, 'fullscreenElement', { configurable: true, value: null, writable: true })
      const wrapper = await mountComponent()
      if (wrapper.vm.$refs.videoWrapperRef) {
        wrapper.vm.$refs.videoWrapperRef.requestFullscreen = requestFullscreen
        wrapper.vm.$refs.videoWrapperRef.webkitRequestFullscreen = requestFullscreen
      }
      await wrapper.vm.handleDoubleClick()
      expect(requestFullscreen).toHaveBeenCalled()
    })

    it('exits fullscreen when there is a fullscreen element', async () => {
      const exitFullscreen = vi.fn().mockResolvedValue(undefined)
      Object.defineProperty(document, 'fullscreenElement', { configurable: true, value: {}, writable: true })
      document.exitFullscreen = exitFullscreen
      document.webkitExitFullscreen = exitFullscreen
      const wrapper = await mountComponent()
      await wrapper.vm.handleDoubleClick()
      expect(exitFullscreen).toHaveBeenCalled()
    })

    it('uses webkit fullscreen fallback when standard is missing', async () => {
      const webkitReq = vi.fn().mockResolvedValue(undefined)
      Object.defineProperty(document, 'fullscreenElement', { configurable: true, value: null, writable: true })
      const wrapper = await mountComponent()
      if (wrapper.vm.$refs.videoWrapperRef) {
        wrapper.vm.$refs.videoWrapperRef.requestFullscreen = undefined
        wrapper.vm.$refs.videoWrapperRef.webkitRequestFullscreen = webkitReq
      }
      await wrapper.vm.handleDoubleClick()
      expect(webkitReq).toHaveBeenCalled()
    })

    it('uses webkit exit fullscreen fallback when standard is missing', async () => {
      const webkitExit = vi.fn().mockResolvedValue(undefined)
      Object.defineProperty(document, 'fullscreenElement', { configurable: true, value: {}, writable: true })
      document.exitFullscreen = undefined
      document.webkitExitFullscreen = webkitExit
      const wrapper = await mountComponent()
      await wrapper.vm.handleDoubleClick()
      expect(webkitExit).toHaveBeenCalled()
    })

    it('does nothing when ref is missing', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.$refs.videoWrapperRef = null
      await wrapper.vm.handleDoubleClick()
      expect(wrapper.exists()).toBe(true)
    })

    it('catches fullscreen errors', async () => {
      const req = vi.fn().mockRejectedValue(new Error('fs'))
      Object.defineProperty(document, 'fullscreenElement', { configurable: true, value: null, writable: true })
      const wrapper = await mountComponent()
      if (wrapper.vm.$refs.videoWrapperRef) {
        wrapper.vm.$refs.videoWrapperRef.requestFullscreen = req
      }
      await wrapper.vm.handleDoubleClick()
      expect(wrapper.exists()).toBe(true)
    })
  })

  describe('template interactions', () => {
    function clickTab(wrapper, label) {
      const btn = wrapper.findAll('.tab-btn').find((b) => b.text().includes(label))
      return btn.trigger('click')
    }

    it('switches tabs and loads intruders via filter chips', async () => {
      mocks.faceGateApi.getFaceIntruders.mockResolvedValue({ items: [{ id: 1, reason: 'unknown' }, { id: 2, reason: 'denied' }], total: 2 })
      const wrapper = await mountComponent()
      await clickTab(wrapper, 'Kẻ xâm nhập')
      expect(wrapper.vm.tab).toBe('intruders')
      await nextTick()
      expect(wrapper.findAll('.intruder-card').length).toBe(2)
      await wrapper.findAll('.chip')[0].trigger('click')
      await wrapper.findAll('.chip')[1].trigger('click')
      await wrapper.findAll('.chip')[2].trigger('click')
      await wrapper.findAll('.chip')[3].trigger('click')
      expect(wrapper.vm.intruderFilter).toBe('blacklist')
    })

    it('renders intruder details and deletes a single intruder', async () => {
      mocks.faceGateApi.getFaceIntruders.mockResolvedValue({ items: [{ id: 5, reason: 'denied', employeeId: '9', reasonDetail: 'no access', gateName: 'G1', occurredAtUtc: new Date().toISOString(), snapshotBase64: 'data:image/png;base64,x' }], total: 1 })
      const wrapper = await mountComponent()
      await clickTab(wrapper, 'Kẻ xâm nhập')
      await nextTick()
      expect(wrapper.findAll('.intruder-card').length).toBe(1)
      await wrapper.find('.intruder-del').trigger('click')
      await flushPromises()
      expect(mocks.faceGateApi.deleteFaceIntruder).toHaveBeenCalledWith(5)
    })

    it('renders intruder placeholders for employees and empty photos', async () => {
      mocks.faceGateApi.getFaceIntruders.mockResolvedValue({ items: [{ id: 1, reason: 'blacklist', employeeId: '9', reasonDetail: 'black', door: 'D' }], total: 1 })
      const wrapper = await mountComponent()
      await clickTab(wrapper, 'Kẻ xâm nhập')
      await nextTick()
      expect(wrapper.find('.photo-empty').exists()).toBe(true)
    })

    it('clears all intruders via the toolbar button', async () => {
      mocks.faceGateApi.getFaceIntruders.mockResolvedValue({ items: [{ id: 1, reason: 'unknown' }, { id: 2, reason: 'unknown' }], total: 2 })
      const wrapper = await mountComponent()
      await clickTab(wrapper, 'Kẻ xâm nhập')
      await nextTick()
      await wrapper.find('.intruder-toolbar .btn-outline').trigger('click')
      await flushPromises()
      expect(mocks.faceGateApi.deleteFaceIntruder).toHaveBeenCalledTimes(2)
    })

    it('opts out of clearing intruders when not confirmed', async () => {
      window.confirm.mockImplementation(() => false)
      mocks.faceGateApi.getFaceIntruders.mockResolvedValue({ items: [{ id: 1, reason: 'unknown' }], total: 1 })
      const wrapper = await mountComponent()
      await clickTab(wrapper, 'Kẻ xâm nhập')
      await nextTick()
      await wrapper.find('.intruder-toolbar .btn-outline').trigger('click')
      expect(mocks.faceGateApi.deleteFaceIntruder).not.toHaveBeenCalled()
    })

    it('opens the password modal and cancels it', async () => {
      mocks.faceGateApi.getFaceGates.mockResolvedValue({ gates: [gate(1, 'Cổng A')] })
      const wrapper = await mountComponent()
      wrapper.vm.showPasswordModal = true
      wrapper.vm.pendingGateName = 'Cổng A'
      await nextTick()
      expect(wrapper.find('.modal-box').exists()).toBe(true)
      await wrapper.findAll('.modal-actions button')[1].trigger('click')
      expect(wrapper.vm.showPasswordModal).toBe(false)
    })

    it('confirms password through modal input and enter key', async () => {
      mocks.faceGateApi.getFaceGates.mockResolvedValue({ gates: [gate(1, 'Cổng A')] })
      const wrapper = await mountComponent()
      wrapper.vm.showPasswordModal = true
      wrapper.vm.pendingGateName = 'Cổng A'
      await nextTick()
      await wrapper.find('.modal-input').setValue('abc')
      await wrapper.findAll('.modal-actions button')[0].trigger('click')
      await flushPromises()
      expect(mocks.faceGateApi.verifyFaceGatePassword).toHaveBeenCalledWith('abc')
      expect(wrapper.vm.showPasswordModal).toBe(false)
    })

    it('closes the password modal by clicking the backdrop', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.showPasswordModal = true
      await nextTick()
      await wrapper.find('.modal-backdrop').trigger('click')
      expect(wrapper.vm.showPasswordModal).toBe(false)
    })

    it('drives the start and stop buttons through the template', async () => {
      mocks.faceApi.getCameraResult.mockResolvedValue(defaultStatus({ camera_enabled: true, scan_locked: false, message: 'START' }))
      const wrapper = await mountComponent()
      wrapper.vm.cameraIp = 'rtsp://x'
      wrapper.vm.activeGateName = 'Cổng A'
      await nextTick()
      await wrapper.find('.start-btn').trigger('click')
      await flushPromises()
      expect(mocks.faceApi.startCamera).toHaveBeenCalled()
      await nextTick()
      await wrapper.find('.stop-btn').trigger('click')
      await flushPromises()
      expect(mocks.faceApi.stopCamera).toHaveBeenCalled()
    })

    it('changes the gate and camera selects through the template', async () => {
      mocks.faceGateApi.getFaceGates.mockResolvedValue({ gates: [gate(1, 'Cổng A')] })
      mocks.cameraRuntimeApi.getCameras.mockResolvedValue([cam(1, 'CAM1')])
      const wrapper = await mountComponent()
      await wrapper.find('.gate-select').setValue('1')
      await nextTick()
      expect(wrapper.vm.showPasswordModal).toBe(true)
      wrapper.vm.showPasswordModal = false
      await wrapper.findAll('.gate-select')[1].setValue('CAM1')
      await flushPromises()
      expect(wrapper.vm.cameraSearch).toBe('CAM1')
    })

    it('renders face boxes for live faces', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.cameraRunning = true
      wrapper.vm.faces = [makeFace({ status: 'intruder' }), makeFace({ employee_id: '7', accessState: 'allowed' })]
      await nextTick()
      expect(wrapper.findAll('.face-box').length).toBe(2)
    })

    it('renders the video placeholder and toasts', async () => {
      const wrapper = await mountComponent()
      expect(wrapper.find('.video-placeholder').exists()).toBe(true)
      wrapper.vm.cameraRunning = true
      wrapper.vm.message = 'x'
      await nextTick()
      expect(wrapper.find('.face-toast-float').exists()).toBe(true)
    })

    it('renders the face service error float', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.faceServiceError = { message: 'down' }
      await nextTick()
      expect(wrapper.find('.face-error-float').exists()).toBe(true)
    })

    it('switches back to the scan tab', async () => {
      const wrapper = await mountComponent()
      wrapper.vm.tab = 'intruders'
      await nextTick()
      await clickTab(wrapper, 'Quét')
      expect(wrapper.vm.tab).toBe('scan')
    })

    it('triggers fullscreen through a double click on the video frame', async () => {
      const requestFullscreen = vi.fn().mockResolvedValue(undefined)
      Object.defineProperty(document, 'fullscreenElement', { configurable: true, value: null, writable: true })
      const wrapper = await mountComponent()
      if (wrapper.vm.$refs.videoWrapperRef) {
        wrapper.vm.$refs.videoWrapperRef.requestFullscreen = requestFullscreen
      }
      await wrapper.find('.video-frame').trigger('dblclick')
      expect(requestFullscreen).toHaveBeenCalled()
    })

    it('confirms password with enter key on the modal input', async () => {
      mocks.faceGateApi.getFaceGates.mockResolvedValue({ gates: [gate(1, 'Cổng A')] })
      const wrapper = await mountComponent()
      wrapper.vm.showPasswordModal = true
      wrapper.vm.pendingGateName = 'Cổng A'
      await nextTick()
      await wrapper.find('.modal-input').setValue('secret')
      await wrapper.find('.modal-input').trigger('keydown', { key: 'Enter' })
      await flushPromises()
      expect(mocks.faceGateApi.verifyFaceGatePassword).toHaveBeenCalledWith('secret')
    })
  })
})
