import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'

const mocks = vi.hoisted(() => ({
  faceApi: {
    __v_isRef: false,
    __v_isReadonly: false,
    __v_isShallow: false,
    __v_isReactive: false,
    __v_raw: {},
    getCameraStatus: vi.fn(),
    getCameraResult: vi.fn(),
    getLockedImages: vi.fn(),
    startCamera: vi.fn(),
    resetCamera: vi.fn(),
    stopCamera: vi.fn(),
    normalizeFaceApiError: vi.fn(),
    shouldStopFacePolling: vi.fn(),
  },
  plateApi: {
    __v_isRef: false,
    __v_isReadonly: false,
    __v_isShallow: false,
    __v_isReactive: false,
    __v_raw: {},
    getCameraStatus: vi.fn(),
    getCameraResult: vi.fn(),
    getLockedImages: vi.fn(),
    turnOnCamera: vi.fn(),
    resetCameraState: vi.fn(),
    turnOffCamera: vi.fn(),
  },
  cameraRuntimeApi: {
    __v_isRef: false,
    __v_isReadonly: false,
    __v_isShallow: false,
    __v_isReactive: false,
    __v_raw: {},
    ensureCameraRegistered: vi.fn(),
  },
  gateTransitApi: {
    __v_isRef: false,
    __v_isReadonly: false,
    __v_isShallow: false,
    __v_isReactive: false,
    __v_raw: {},
    scanGate: vi.fn(),
  },
}))

vi.mock('../../services/faceApi', () => mocks.faceApi)
vi.mock('../../services/plateCameraApi', () => mocks.plateApi)
vi.mock('../../services/cameraRuntimeApi', () => mocks.cameraRuntimeApi)
vi.mock('../../services/gateTransitApi', () => mocks.gateTransitApi)

import ThongHanh from '../ThongHanh.vue'

function flushPromises() {
  return new Promise((resolve) => setTimeout(resolve, 0))
}

let wrapper

beforeEach(() => {
  vi.resetAllMocks()
  mocks.faceApi.getCameraStatus.mockResolvedValue({ camera_enabled: false, ip: '' })
  mocks.faceApi.getCameraResult.mockResolvedValue({})
  mocks.faceApi.getLockedImages.mockResolvedValue({ scan_locked: true, locked_snapshot: 'snap', locked_face_crop: 'crop' })
  mocks.faceApi.startCamera.mockResolvedValue({ success: true, message: 'Face ON' })
  mocks.faceApi.resetCamera.mockResolvedValue({ message: 'Face reset' })
  mocks.faceApi.stopCamera.mockResolvedValue({ message: 'Face off' })
  mocks.faceApi.normalizeFaceApiError.mockImplementation((e) => ({
    cancelled: false,
    code: e?.code || 'x',
    message: e?.message || 'kmb',
  }))
  mocks.faceApi.shouldStopFacePolling.mockReturnValue(false)
  mocks.plateApi.getCameraStatus.mockResolvedValue({ camera_enabled: false, ip: '' })
  mocks.plateApi.getCameraResult.mockResolvedValue({})
  mocks.plateApi.getLockedImages.mockResolvedValue({})
  mocks.plateApi.turnOnCamera.mockResolvedValue({ success: true, session_id: 5, message: 'Plate ON' })
  mocks.plateApi.resetCameraState.mockResolvedValue({ message: 'Plate reset', session_id: 0 })
  mocks.plateApi.turnOffCamera.mockResolvedValue({ message: 'Plate off' })
  mocks.cameraRuntimeApi.ensureCameraRegistered.mockResolvedValue({})
  mocks.gateTransitApi.scanGate.mockResolvedValue({ data: { success: true, message: 'OK' } })
  vi.spyOn(window, 'alert').mockImplementation(() => {})
})

afterEach(() => {
  if (wrapper) wrapper.unmount()
  wrapper = null
  vi.restoreAllMocks()
  vi.clearAllTimers()
})

async function mountThongHanh() {
  wrapper = mount(ThongHanh)
  await flushPromises()
  await flushPromises()
  return wrapper
}

describe('ThongHanh.vue', () => {
  it('mounts and loads the status of every lane camera', async () => {
    const wrapper = await mountThongHanh()
    expect(wrapper.vm.lanes).toHaveLength(2)
    expect(mocks.faceApi.getCameraStatus).toHaveBeenCalledTimes(2)
    expect(mocks.plateApi.getCameraStatus).toHaveBeenCalledTimes(2)
    expect(wrapper.find('.lane-card').exists()).toBe(true)
  })

  it('computes lane readiness and running state', async () => {
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    expect(wrapper.vm.isLaneReady(lane)).toBe(false)
    Object.assign(lane.face, { scanLocked: true, employeeId: '7', alert: false })
    Object.assign(lane.plate, { scanLocked: true, confirmedPlate: '30A-1' })
    expect(wrapper.vm.isLaneReady(lane)).toBe(true)
    lane.face.alert = true
    expect(wrapper.vm.isLaneReady(lane)).toBe(false)
    lane.face.cameraRunning = true
    lane.plate.cameraRunning = false
    expect(wrapper.vm.laneAnyRunning(lane)).toBe(true)
  })

  it('maps face states to text and classes', async () => {
    const wrapper = await mountThongHanh()
    const face = wrapper.vm.lanes[0].face
    expect(wrapper.vm.faceStateText(face)).toBe('CHỜ')
    face.trackingActive = true
    expect(wrapper.vm.faceStateText(face)).toBe('ĐANG QUÉT')
    face.faceMatch = true
    expect(wrapper.vm.faceStateText(face)).toBe('ĐANG SO KHỚP')
    face.identityConfirmed = true
    expect(wrapper.vm.faceStateText(face)).toBe('ĐANG XÁC NHẬN')
    face.scanLocked = true
    face.identityConfirmed = false
    face.lockReason = 'timeout'
    expect(wrapper.vm.faceStateText(face)).toBe('TIMEOUT')
    face.lockReason = 'alert'
    expect(wrapper.vm.faceStateText(face)).toBe('CẢNH BÁO')
    face.lockReason = 'confirmed'
    expect(wrapper.vm.faceStateText(face)).toBe('ĐÃ NHẬN DIỆN')
    face.lockReason = 'other'
    expect(wrapper.vm.faceStateText(face)).toBe('ĐÃ KHÓA')
    face.alert = true
    expect(wrapper.vm.faceStateClass(face)).toBe('danger-text')
    face.alert = false
    face.identityConfirmed = true
    expect(wrapper.vm.faceStateClass(face)).toBe('ok-text')
    face.identityConfirmed = false
    expect(wrapper.vm.faceStateClass(face)).toBe('warn-text')
  })

  it('builds direct camera urls with cache busters', async () => {
    const wrapper = await mountThongHanh()
    expect(wrapper.vm.buildDirectCameraUrl('')).toBe('')
    expect(wrapper.vm.buildDirectCameraUrl('  ')).toBe('')
    const withQ = wrapper.vm.buildDirectCameraUrl('http://a/face')
    expect(withQ.startsWith('http://a/face?t=')).toBe(true)
    const withAmp = wrapper.vm.buildDirectCameraUrl('http://a/face?x=1')
    expect(withAmp.startsWith('http://a/face?x=1&t=')).toBe(true)
  })

  it('classifies previewable urls', async () => {
    const wrapper = await mountThongHanh()
    expect(wrapper.vm.isImagePreviewableUrl('')).toBe(false)
    expect(wrapper.vm.isImagePreviewableUrl('data:image/png;base64,xx')).toBe(true)
    expect(wrapper.vm.isImagePreviewableUrl('rtsp://x')).toBe(false)
    expect(wrapper.vm.isImagePreviewableUrl('http://x/a.mp4')).toBe(false)
    expect(wrapper.vm.isImagePreviewableUrl('http://x/a.m3u8')).toBe(false)
    expect(wrapper.vm.isImagePreviewableUrl('http://x.jpg')).toBe(true)
    expect(wrapper.vm.isImagePreviewableUrl('/local/feed')).toBe(true)
    expect(wrapper.vm.isImagePreviewableUrl('ftp://x')).toBe(false)
  })

  it('mounts and resets previews', async () => {
    const wrapper = await mountThongHanh()
    const face = wrapper.vm.lanes[0].face
    wrapper.vm.mountPreview(face, '')
    expect(face.directCameraUrl).toBe('')
    wrapper.vm.mountPreview(face, 'http://a/face')
    expect(face.previewRunning).toBe(true)
    expect(face.previewHealthy).toBe(false)
    const key = face.directCameraKey
    face.lockedSnapshot = 'snap'
    wrapper.vm.resetPreview(face)
    expect(face.previewRunning).toBe(false)
    expect(face.directCameraKey).toBe(key + 1)
    wrapper.vm.onPreviewLoaded(face)
    expect(face.previewHealthy).toBe(true)
    wrapper.vm.onPreviewError(face)
    expect(face.previewHealthy).toBe(false)
  })

  it('enables plate previews for image and stream urls', async () => {
    const wrapper = await mountThongHanh()
    const plate = wrapper.vm.lanes[0].plate
    wrapper.vm.enablePlatePreview(plate, 'http://a/snap.jpg')
    expect(plate.previewRunning).toBe(true)
    expect(plate.directCameraUrl).toContain('http://a/snap.jpg?t=')
    wrapper.vm.resetPreview(plate)
    wrapper.vm.enablePlatePreview(plate, 'rtsp://plate')
    expect(plate.directCameraUrl).toBe('')
    expect(plate.previewRunning).toBe(true)
  })

  it('computes plate preview urls, keys and status', async () => {
    const wrapper = await mountThongHanh()
    const plate = wrapper.vm.lanes[0].plate
    expect(wrapper.vm.platePreviewDisplayUrl(plate)).toBe('')
    expect(wrapper.vm.platePreviewKey(plate)).toBe(0)
    expect(wrapper.vm.platePreviewStatusText(plate)).toBe('Preview OFF')
    expect(wrapper.vm.platePreviewStatusClass(plate)).toBe('wait')
    plate.previewRunning = true
    expect(wrapper.vm.platePreviewStatusText(plate)).toBe('Chờ ảnh')
    expect(wrapper.vm.platePreviewStatusClass(plate)).toBe('wait')
    plate.previewHealthy = true
    expect(wrapper.vm.platePreviewStatusText(plate)).toBe('Preview OK')
    expect(wrapper.vm.platePreviewStatusClass(plate)).toBe('ok')
    plate.lockedSnapshot = 'http://a/snap'
    plate.sessionId = 2
    plate.lastLockedImageSessionId = 1
    expect(wrapper.vm.platePreviewDisplayUrl(plate)).toBe('http://a/snap')
    expect(wrapper.vm.platePreviewKey(plate)).toBe('plate-capture-2-1')
    expect(wrapper.vm.platePreviewStatusText(plate)).toBe('Ảnh đã chụp')
    expect(wrapper.vm.platePreviewStatusClass(plate)).toBe('ok')
  })

  it('clears and hard resets face and plate modules', async () => {
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    Object.assign(lane.face, { employeeId: '1', trackingActive: true, alert: true, scanLocked: true, lockedSnapshot: 's' })
    wrapper.vm.hardResetFace(lane.face)
    expect(lane.face.cameraRunning).toBe(false)
    expect(lane.face.employeeId).toBe('')
    Object.assign(lane.plate, { confirmedPlate: '30A', scanLocked: true, sessionId: 2, lastAppliedSessionId: 2 })
    wrapper.vm.hardResetPlate(lane.plate)
    expect(lane.plate.cameraRunning).toBe(false)
    expect(lane.plate.confirmedPlate).toBe('')
    expect(lane.plate.sessionId).toBe(0)
  })

  it('stops face and plate loops', async () => {
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    lane.face.cameraRunning = true
    wrapper.vm.startFaceLoop(lane)
    expect(lane.face.resultTimer).toBeTruthy()
    wrapper.vm.stopFaceLoop(lane)
    expect(lane.face.resultTimer).toBeNull()
    lane.plate.cameraRunning = true
    wrapper.vm.startPlateLoop(lane)
    expect(lane.plate.resultTimer).toBeTruthy()
    wrapper.vm.stopPlateLoop(lane)
    expect(lane.plate.resultTimer).toBeNull()
  })

  it('handles face service errors across branches', async () => {
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    mocks.faceApi.normalizeFaceApiError.mockReturnValue({ cancelled: true, code: 'ok', message: '' })
    wrapper.vm.handleLaneFaceError(lane, new Error('x'))
    expect(lane.face.serviceErrorCode).toBe('')
    mocks.faceApi.normalizeFaceApiError.mockReturnValue({ cancelled: false, code: 'NX', message: 'kmb' })
    lane.face.destroyed = true
    wrapper.vm.handleLaneFaceError(lane, new Error('x'))
    expect(lane.face.serviceErrorCode).toBe('')
    lane.face.destroyed = false
    wrapper.vm.handleLaneFaceError(lane, new Error('x'))
    expect(lane.face.serviceErrorCode).toBe('NX')
    expect(lane.face.cameraConnected).toBe(false)
    expect(window.alert).toHaveBeenCalledWith('Làn 1: kmb')
    mocks.faceApi.shouldStopFacePolling.mockReturnValue(true)
    wrapper.vm.handleLaneFaceError(lane, new Error('y'), { polling: true })
    expect(lane.face.resultTimer).toBeNull()
  })

  it('loads face status and mounts the camera preview', async () => {
    mocks.faceApi.getCameraStatus.mockResolvedValue({ camera_enabled: true, ip: 'rtsp://face/1', scan_locked: false })
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    expect(lane.face.cameraRunning).toBe(true)
    expect(lane.face.currentIp).toBe('rtsp://face/1')
    expect(lane.face.cameraIp).toBe('rtsp://face/1')
    expect(lane.face.previewRunning).toBe(true)
  })

  it('handles face status load errors', async () => {
    mocks.faceApi.getCameraStatus.mockRejectedValue(new Error('down'))
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    expect(lane.face.serviceErrorCode).toBe('x')
  })

  it('loads plate status and enables its preview', async () => {
    mocks.plateApi.getCameraStatus.mockResolvedValue({ camera_enabled: false, ip: 'http://plate/snap.jpg' })
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    expect(lane.plate.currentIp).toBe('http://plate/snap.jpg')
    expect(lane.plate.cameraIp).toBe('http://plate/snap.jpg')
    expect(lane.plate.previewRunning).toBe(true)
  })

  it('refreshes face realtime state and applies locked images', async () => {
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    mocks.faceApi.getCameraResult.mockResolvedValue({
      camera_enabled: true,
      camera_connected: true,
      ip: 'rtsp://face',
      scan_locked: true,
      lock_reason: 'confirmed',
      employee_id: '7',
      message: 'hi',
    })
    await wrapper.vm.refreshFace(lane)
    expect(lane.face.scanLocked).toBe(true)
    expect(lane.face.lockReason).toBe('confirmed')
    expect(lane.face.employeeId).toBe('7')
    expect(lane.face.lockedSnapshot).toBe('snap')
  })

  it('hard resets the face when the camera is turned off', async () => {
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    lane.face.cameraRunning = true
    mocks.faceApi.getCameraResult.mockResolvedValue({ camera_enabled: false, scan_locked: false })
    await wrapper.vm.refreshFace(lane)
    expect(lane.face.cameraRunning).toBe(false)
    expect(lane.face.currentIp).toBe('')
  })

  it('refreshes plate realtime state with session tracking', async () => {
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    lane.plate.sessionId = 10
    lane.plate.lastAppliedSessionId = 10
    mocks.plateApi.getCameraResult.mockResolvedValue({ session_id: 4, camera_enabled: true })
    await wrapper.vm.refreshPlate(lane)
    expect(lane.plate.sessionId).toBe(10)
    mocks.plateApi.getCameraResult.mockResolvedValue({ session_id: 12, camera_enabled: true, confirmed_plate: '30A-1', scan_locked: true })
    await wrapper.vm.refreshPlate(lane)
    expect(lane.plate.sessionId).toBe(12)
    expect(lane.plate.confirmedPlate).toBe('30A-1')
    expect(lane.plate.scanLocked).toBe(true)
  })

  it('resets the plate when its camera is turned off', async () => {
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    lane.plate.cameraRunning = true
    mocks.plateApi.getCameraResult.mockResolvedValue({ session_id: 3, camera_enabled: false })
    await wrapper.vm.refreshPlate(lane)
    expect(lane.plate.cameraRunning).toBe(false)
    expect(lane.plate.sessionId).toBe(0)
  })

  it('fetches face locked images under different guards', async () => {
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    const face = lane.face
    face.cameraRunning = true
    face.scanLocked = false
    await wrapper.vm.fetchFaceLockedImages(lane)
    expect(face.lockedSnapshot).toBe('')
    face.scanLocked = true
    mocks.faceApi.getLockedImages.mockResolvedValue({ scan_locked: false })
    await wrapper.vm.fetchFaceLockedImages(lane)
    expect(face.lockedSnapshot).toBe('')
    mocks.faceApi.getLockedImages.mockResolvedValue({ scan_locked: true, locked_snapshot: 'a', locked_face_crop: 'b' })
    await wrapper.vm.fetchFaceLockedImages(lane)
    expect(face.lockedSnapshot).toBe('a')
    face.isFetchingLockedImages = true
    await wrapper.vm.fetchFaceLockedImages(lane)
    expect(mocks.faceApi.getLockedImages).toHaveBeenCalledTimes(2)
  })

  it('fetches plate locked images honoring session equality', async () => {
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    const plate = lane.plate
    plate.cameraRunning = true
    plate.scanLocked = false
    await wrapper.vm.fetchPlateLockedImages(lane)
    expect(plate.lockedSnapshot).toBe('')
    plate.scanLocked = true
    plate.sessionId = 9
    plate.lastLockedImageSessionId = 9
    await wrapper.vm.fetchPlateLockedImages(lane)
    expect(mocks.plateApi.getLockedImages).not.toHaveBeenCalled()
    mocks.plateApi.getLockedImages.mockResolvedValue({ session_id: 8, scan_locked: true, locked_snapshot: 'p' })
    plate.lastLockedImageSessionId = 0
    await wrapper.vm.fetchPlateLockedImages(lane)
    expect(plate.lockedSnapshot).toBe('')
    mocks.plateApi.getLockedImages.mockResolvedValue({ session_id: 9, scan_locked: true, locked_snapshot: 'p' })
    plate.previewRunning = true
    await wrapper.vm.fetchPlateLockedImages(lane)
    expect(plate.lockedSnapshot).toBe('p')
    expect(plate.previewHealthy).toBe(true)
    expect(plate.lastLockedImageSessionId).toBe(9)
  })

  it('previews a lane with face and plate cameras', async () => {
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    await wrapper.vm.previewLane(lane)
    expect(window.alert).toHaveBeenCalledWith('Vui lòng nhập ít nhất 1 URL camera')
    lane.face.cameraIp = 'rtsp://face'
    lane.plate.cameraIp = 'http://plate/snap.jpg'
    await wrapper.vm.previewLane(lane)
    expect(mocks.cameraRuntimeApi.ensureCameraRegistered).toHaveBeenCalledTimes(2)
    expect(lane.face.previewRunning).toBe(true)
    expect(lane.plate.previewRunning).toBe(true)
    expect(lane.loading).toBe(false)
  })

  it('reads all lane cameras starting both readers', async () => {
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    await wrapper.vm.readAllLane(lane)
    expect(window.alert).toHaveBeenCalled()
    lane.face.cameraIp = 'rtsp://face'
    lane.plate.cameraIp = 'rtsp://plate'
    mocks.faceApi.getCameraResult.mockResolvedValue({ camera_enabled: true, ip: 'rtsp://face' })
    mocks.plateApi.getCameraResult.mockResolvedValue({ session_id: 5, camera_enabled: true, ip: 'rtsp://plate' })
    await wrapper.vm.readAllLane(lane)
    expect(mocks.faceApi.startCamera).toHaveBeenCalledWith('lane-1-face', 'rtsp://face', 'lane-1')
    expect(mocks.plateApi.turnOnCamera).toHaveBeenCalledWith('rtsp://plate')
    expect(lane.face.cameraRunning).toBe(true)
    expect(lane.plate.cameraRunning).toBe(true)
    expect(lane.plate.sessionId).toBe(5)
    expect(lane.face.resultTimer).toBeTruthy()
    expect(lane.plate.resultTimer).toBeTruthy()
    expect(lane.loading).toBe(false)
  })

  it('resets already-running cameras on read all', async () => {
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    lane.face.cameraIp = 'rtsp://face'
    lane.plate.cameraIp = 'rtsp://plate'
    lane.face.cameraRunning = true
    lane.plate.cameraRunning = true
    await wrapper.vm.readAllLane(lane)
    expect(mocks.faceApi.resetCamera).toHaveBeenCalled()
    expect(mocks.plateApi.resetCameraState).toHaveBeenCalled()
  })

  it('retries face and plate independently', async () => {
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    await wrapper.vm.retryFace(lane)
    expect(window.alert).toHaveBeenCalled()
    lane.face.cameraIp = 'rtsp://face2'
    mocks.faceApi.getCameraResult.mockResolvedValue({ camera_enabled: true, ip: 'rtsp://face2', message: 'Face reset' })
    await wrapper.vm.retryFace(lane)
    expect(mocks.faceApi.startCamera).toHaveBeenCalled()
    expect(mocks.cameraRuntimeApi.ensureCameraRegistered).toHaveBeenCalled()
    lane.face.cameraRunning = true
    await wrapper.vm.retryFace(lane)
    expect(mocks.faceApi.resetCamera).toHaveBeenCalled()
    expect(lane.face.message).toBe('Face reset')
  })

  it('retries plate with turn on and reset paths', async () => {
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    await wrapper.vm.retryPlate(lane)
    expect(window.alert).toHaveBeenCalled()
    lane.plate.cameraIp = 'rtsp://plate2'
    mocks.plateApi.getCameraResult.mockResolvedValue({ session_id: 5, camera_enabled: true })
    await wrapper.vm.retryPlate(lane)
    expect(mocks.plateApi.turnOnCamera).toHaveBeenCalled()
    lane.plate.cameraRunning = true
    mocks.plateApi.resetCameraState.mockResolvedValue({ message: 'res', session_id: 6 })
    await wrapper.vm.retryPlate(lane)
    expect(lane.plate.sessionId).toBe(6)
  })

  it('stops a lane and resets everything', async () => {
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    lane.face.cameraIp = 'rtsp://face'
    lane.plate.cameraIp = 'rtsp://plate'
    mocks.faceApi.getCameraResult.mockResolvedValue({ camera_enabled: true, ip: 'rtsp://face' })
    mocks.plateApi.getCameraResult.mockResolvedValue({ session_id: 5, camera_enabled: true, ip: 'rtsp://plate' })
    await wrapper.vm.readAllLane(lane)
    await wrapper.vm.stopLane(lane)
    expect(mocks.faceApi.stopCamera).toHaveBeenCalled()
    expect(mocks.plateApi.turnOffCamera).toHaveBeenCalled()
    expect(lane.face.cameraRunning).toBe(false)
    expect(lane.plate.cameraRunning).toBe(false)
    expect(lane.face.resultTimer).toBeNull()
    expect(lane.plate.resultTimer).toBeNull()
    expect(lane.loading).toBe(false)
  })

  it('confirms a lane with employee and plate data', async () => {
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    await wrapper.vm.confirmLane(lane)
    expect(window.alert).toHaveBeenCalledWith('Làn 1: chưa có Employee ID')
    lane.face.employeeId = '7'
    await wrapper.vm.confirmLane(lane)
    expect(window.alert).toHaveBeenCalledWith('Làn 1: chưa có biển số')
    lane.plate.confirmedPlate = '30A-1'
    lane.plate.lockedSnapshot = 'psnap'
    lane.face.lockedFaceCrop = 'fcrop'
    await wrapper.vm.confirmLane(lane)
    expect(mocks.gateTransitApi.scanGate).toHaveBeenCalledWith(
      expect.objectContaining({ employeeId: 7, licensePlate: '30A-1', gateId: lane.gateId, credentialType: 'FACEID' })
    )
    expect(window.alert).toHaveBeenCalledWith('Làn 1: OK')
  })

  it('reports confirm failures and api errors', async () => {
    const wrapper = await mountThongHanh()
    const lane = wrapper.vm.lanes[0]
    lane.face.employeeId = '7'
    lane.plate.confirmedPlate = '30A-1'
    mocks.gateTransitApi.scanGate.mockResolvedValue({ data: { success: false } })
    await wrapper.vm.confirmLane(lane)
    expect(window.alert).toHaveBeenCalledWith('Làn 1: Xử lý thất bại')
    mocks.gateTransitApi.scanGate.mockRejectedValue({ response: { data: { message: 'Gate down' } } })
    await wrapper.vm.confirmLane(lane)
    expect(window.alert).toHaveBeenCalledWith('Làn 1: Gate down')
    expect(lane.loading).toBe(false)
  })
})