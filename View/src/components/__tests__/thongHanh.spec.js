import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('axios', () => {
  const instance = {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
    interceptors: { request: { use: vi.fn() }, response: { use: vi.fn() } }
  }
  return {
    default: {
      ...instance,
      create: vi.fn(() => instance)
    }
  }
})
vi.mock('../../services/notificationApi', () => ({
  onEntityChanged: vi.fn(() => vi.fn()),
  onSyncEvent: vi.fn(() => vi.fn())
}))
vi.mock('../../services/faceApi', () => ({
  __v_isRef: false,
  __v_isReadonly: false,
  __v_isShallow: false,
  __v_isReactive: false,
  __v_raw: {},
  startCamera: vi.fn(),
  stopCamera: vi.fn(),
  resetCamera: vi.fn(),
  getCameraStatus: vi.fn(),
  getCameraResult: vi.fn(),
  getLockedImages: vi.fn(),
  getCameras: vi.fn(),
}))
vi.mock('../../services/plateCameraApi', () => ({
  __v_isRef: false,
  __v_isReadonly: false,
  __v_isShallow: false,
  __v_isReactive: false,
  __v_raw: {},
  turnOnCamera: vi.fn(),
  turnOffCamera: vi.fn(),
  resetCameraState: vi.fn(),
  getCameraStatus: vi.fn(),
  getCameraResult: vi.fn(),
  getLockedImages: vi.fn(),
  getResolvedPlateApiBaseUrl: vi.fn(() => 'http://plate.local/api'),
  createPlateCameraApi: vi.fn(() => ({
    turnOnCamera: vi.fn(), turnOffCamera: vi.fn(), resetCameraState: vi.fn(),
    getCameraStatus: vi.fn(), getCameraResult: vi.fn(), getLockedImages: vi.fn(),
    getResolvedPlateApiBaseUrl: vi.fn(() => 'http://plate.local-lane2/api'),
  })),
}))
vi.mock('../../services/gateTransitApi', () => ({
  scanGate: vi.fn(), scanGuest: vi.fn(), getTransitLanes: vi.fn(), updateTransitLaneDirection: vi.fn()
}))
vi.mock('../../services/cameraRuntimeApi', () => ({
  getCameras: vi.fn(),
  getPythonProcessStatus: vi.fn(),
}))
vi.mock('../../services/runtimeServiceApi', () => ({
  getRuntimeServices: vi.fn(),
  updateRuntimeService: vi.fn(),
  startRuntimeService: vi.fn(),
  stopRuntimeService: vi.fn(),
}))
vi.mock('../../config/api', () => ({
  PLATE_API_BASE_URL: 'http://plate.local/api',
  PLATE_API_BASE_URL_LANE2: 'http://plate.local-lane2/api',
  API_BASE_URL: 'http://localhost:5107/api',
  API_ORIGIN: 'http://localhost:5107'
}))
vi.mock('../../utils/cameraNetwork', () => ({ normalizeCameraUrl: (url) => String(url || '') }))
vi.mock('../../services/simulationHarness', () => ({ isSimMode: vi.fn(() => false), installSimulation: vi.fn() }))
vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    recordLaneEvent: vi.fn(),
    recordDuressEvent: vi.fn(),
    createInterventionRequest: vi.fn(),
    createEmergencyPass: vi.fn(),
    createEmergencyState: vi.fn(),
  },
  zoneAuthorityApi: { getMyZones: vi.fn() },
}))
vi.mock('../../stores/auth', () => ({
  authState: { user: { role: 'BaoVe', userId: 3, employeeId: 7, fullName: 'Baove 1' } },
  hasRole: vi.fn(() => true),
}))

const faceApi = await import('../../services/faceApi')
const plateApi = await import('../../services/plateCameraApi')
const gateTransitApi = await import('../../services/gateTransitApi')
const cameraRuntimeApi = await import('../../services/cameraRuntimeApi')
const runtimeServiceApi = await import('../../services/runtimeServiceApi')
const { enterpriseApi, zoneAuthorityApi } = await import('../../services/enterpriseSecurityApi')
const { authState } = await import('../../stores/auth')

const axios = (await import('axios')).default

const ThongHanh = (await import('../ThongHanh.vue')).default

const cameraFixture = {
  cameraId: 10,
  cameraName: 'Cổng chính',
  streamUrl: 'rtsp://192.168.1.10:554/stream?subtype=1',
  urlView: 'http://go2rtc.local/stream.html?src=cam_gate',
}

let alertMock

beforeEach(() => {
  vi.clearAllMocks()
  faceApi.getCameraStatus.mockResolvedValue({ camera_enabled: false, ip: '' })
  faceApi.getCameraResult.mockResolvedValue({})
  faceApi.getLockedImages.mockResolvedValue({ scan_locked: false })
  faceApi.startCamera.mockResolvedValue({ success: true, message: 'Face ON' })
  faceApi.stopCamera.mockResolvedValue({ message: 'Face off' })
  faceApi.resetCamera.mockResolvedValue({ message: 'Face reset' })
  faceApi.getCameras.mockResolvedValue([])

  plateApi.getCameraStatus.mockResolvedValue({})
  plateApi.getCameraResult.mockResolvedValue({})
  plateApi.getLockedImages.mockResolvedValue({})
  plateApi.turnOnCamera.mockResolvedValue({ success: true, session_id: 3, message: 'OK' })
  plateApi.turnOffCamera.mockResolvedValue({ message: 'Đã tắt' })
  plateApi.resetCameraState.mockResolvedValue({})

  gateTransitApi.scanGate.mockResolvedValue({ data: { success: true, message: 'OK', receiptId: 'RCP-1' } })
  gateTransitApi.scanGuest.mockResolvedValue({ data: { success: true, message: 'OK', receiptId: 'RCP-G' } })
  gateTransitApi.getTransitLanes.mockResolvedValue({ data: { data: [] } })
  gateTransitApi.updateTransitLaneDirection.mockResolvedValue({ data: { data: { direction: 'IN' } } })

  cameraRuntimeApi.getCameras.mockResolvedValue([])
  cameraRuntimeApi.getPythonProcessStatus.mockResolvedValue({ data: {} })

  runtimeServiceApi.getRuntimeServices.mockResolvedValue([])
  runtimeServiceApi.updateRuntimeService.mockResolvedValue({})
  runtimeServiceApi.startRuntimeService.mockResolvedValue({})
  runtimeServiceApi.stopRuntimeService.mockResolvedValue({})

  zoneAuthorityApi.getMyZones.mockResolvedValue({ data: [{ securityZoneId: 1 }] })
  enterpriseApi.recordLaneEvent.mockResolvedValue({ data: { laneEventId: 5 } })
  enterpriseApi.recordDuressEvent.mockResolvedValue({ data: { duressEventId: 2 } })
  enterpriseApi.createInterventionRequest.mockResolvedValue({ data: { operationalInterventionRequestId: 9 } })
  enterpriseApi.createEmergencyPass.mockResolvedValue({ data: { emergencyPass: { emergencyPassId: 6 } } })
  enterpriseApi.createEmergencyState.mockResolvedValue({ data: { emergencyStateId: 11 } })

  authState.user.role = 'BaoVe'
  axios.get.mockResolvedValue({ data: { status: 'ok' } })

  alertMock = vi.fn()
  vi.stubGlobal('alert', alertMock)
})

afterEach(() => {
  vi.unstubAllGlobals()
})

const stubComponents = { DecisionDrawer: true, StepUpModal: true, AuditReceiptToast: true }

const mountMonitor = async () => {
  const wrapper = mount(ThongHanh, { global: { stubs: stubComponents } })
  await flushPromises()
  return wrapper
}

describe('ThongHanh.vue (Face Transit Monitor)', () => {
  it('renders topbar, lanes and 4 camera cells', async () => {
    const wrapper = await mountMonitor()
    expect(wrapper.text()).toContain('Điều phối thông hành khuôn mặt')
    expect(wrapper.text()).toContain('Làn 1')
    expect(wrapper.text()).toContain('Làn 2')
    expect(wrapper.findAll('.cam-cell').length).toBe(4)
    expect(wrapper.findAll('.cam-block').length).toBe(4)
  })

  it('loads cameras, zones, face and plate status on mount', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue([cameraFixture])
    await mountMonitor()
    expect(cameraRuntimeApi.getCameras).toHaveBeenCalled()
    expect(zoneAuthorityApi.getMyZones).toHaveBeenCalled()
    expect(faceApi.getCameraStatus).toHaveBeenCalled()
    expect(plateApi.getCameraStatus).toHaveBeenCalled()
  })

  it('computes lane readiness correctly', async () => {
    const wrapper = await mountMonitor()
    const lane = wrapper.vm.lanes[0]
    expect(wrapper.vm.isLaneReady(lane)).toBe(false)
    lane.face.scanLocked = true
    lane.face.employeeId = '7'
    lane.plate.scanLocked = true
    lane.plate.confirmedPlate = '30A-12345'
    lane.face.alert = false
    expect(wrapper.vm.isLaneReady(lane)).toBe(true)
    lane.face.alert = true
    expect(wrapper.vm.isLaneReady(lane)).toBe(false)
  })

  it('starts and stops auto monitor', async () => {
    const wrapper = await mountMonitor()
    const startBtn = wrapper.findAll('.btn-auto').find((b) => b.text().includes('Bắt đầu'))
    await startBtn.trigger('click')
    await flushPromises()
    expect(wrapper.vm.autoActive).toBe(true)
    await wrapper.vm.stopAutoMonitor()
    await flushPromises()
    expect(wrapper.vm.autoActive).toBe(false)
  })

  it('loads and persists lane direction', async () => {
    gateTransitApi.getTransitLanes.mockResolvedValue({
      data: { data: [{ laneId: 1, direction: 'OUT' }, { laneId: 2, direction: 'OUT' }] }
    })
    const wrapper = await mountMonitor()
    expect(wrapper.vm.lanes.map(l => l.direction)).toEqual(['OUT', 'OUT'])

    wrapper.vm.lanes[0].direction = 'IN'
    await wrapper.vm.saveLaneDirection(wrapper.vm.lanes[0])
    expect(gateTransitApi.updateTransitLaneDirection).toHaveBeenCalledWith(1, 'IN')
    expect(wrapper.vm.lanes[0].savedDirection).toBe('IN')
  })

  it('refreshes face realtime state and fetches locked images when locked', async () => {
    faceApi.getCameraResult.mockResolvedValue({
      camera_enabled: true,
      camera_connected: true,
      scan_locked: true,
      lock_reason: 'confirmed',
      employee_id: '7',
      employee_name: 'Nguyen Van A',
      identity_confirmed: true
    })
    faceApi.getLockedImages.mockResolvedValue({
      scan_locked: true,
      locked_snapshot: 'data:image/jpeg;base64,SNAP',
      locked_face_crop: 'data:image/jpeg;base64,CROP'
    })
    const wrapper = await mountMonitor()
    const lane = wrapper.vm.lanes[0]
    lane.face.cameraRunning = true
    await wrapper.vm.refreshFace(lane)

    expect(lane.face.scanLocked).toBe(true)
    expect(lane.face.employeeId).toBe('7')
    expect(lane.face.employeeName).toBe('Nguyen Van A')
    expect(faceApi.getLockedImages).toHaveBeenCalled()
    expect(lane.face.lockedSnapshot).toBe('data:image/jpeg;base64,SNAP')
  })

  it('auto decides session when both plate and face are recognized', async () => {
    const wrapper = await mountMonitor()
    const lane = wrapper.vm.lanes[0]
    wrapper.vm.autoActive = true
    lane.auto.on = true
    lane.plate.confirmedPlate = '59K-99999'
    lane.face.employeeId = '7'

    await wrapper.vm.autoDecideSession(lane)

    expect(gateTransitApi.scanGate).toHaveBeenCalledWith(expect.objectContaining({
      LicensePlate: '59K-99999',
      EmployeeId: 7,
      CredentialType: 'FACE'
    }))
    expect(lane.auto.saved).toBe(true)
    expect(lane.auto.status).toBe('decided')
  })

  it('manually confirms lane with face and plate data', async () => {
    const wrapper = await mountMonitor()
    const lane = wrapper.vm.lanes[0]
    lane.face.employeeId = '7'
    lane.plate.confirmedPlate = '30A-11111'
    lane.plate.lockedSnapshot = 'data:image/jpeg;base64,PLATE'
    lane.face.lockedSnapshot = 'data:image/jpeg;base64,FACE'

    await wrapper.vm.confirmLane(lane)

    expect(gateTransitApi.scanGate).toHaveBeenCalledWith(expect.objectContaining({
      EmployeeId: 7,
      LicensePlate: '30A-11111',
      CredentialType: 'FACE',
      PlateSnapshotBase64: 'data:image/jpeg;base64,PLATE',
      FaceSnapshotBase64: 'data:image/jpeg;base64,FACE'
    }))
    expect(alertMock).toHaveBeenCalledWith('Làn 1: OK')
  })

  it('handles decision drawer allow action', async () => {
    const wrapper = await mountMonitor()
    const lane = wrapper.vm.lanes[0]
    wrapper.vm.decisionLaneId = lane.id
    lane.face.employeeId = '7'
    lane.plate.confirmedPlate = '30A-12345'

    await wrapper.vm.handleDecisionAction({
      type: 'allow',
      reason: 'Xác nhận hợp lệ'
    })

    expect(gateTransitApi.scanGate).toHaveBeenCalledWith(expect.objectContaining({
      EmployeeId: 7,
      LicensePlate: '30A-12345',
      CredentialType: 'FACE'
    }))
    expect(enterpriseApi.recordLaneEvent).toHaveBeenCalledWith(expect.objectContaining({
      eventType: 'ACCESS_GRANTED',
      plateText: '30A-12345'
    }))
  })

  it('handles decision drawer deny action', async () => {
    const wrapper = await mountMonitor()
    const lane = wrapper.vm.lanes[0]
    wrapper.vm.decisionLaneId = lane.id
    lane.plate.confirmedPlate = '30A-12345'

    await wrapper.vm.handleDecisionAction({
      type: 'deny',
      reason: 'Không đúng nhân viên'
    })

    expect(enterpriseApi.recordLaneEvent).toHaveBeenCalledWith(expect.objectContaining({
      eventType: 'ACCESS_DENIED',
      plateText: '30A-12345'
    }))
    expect(lane.face.employeeId).toBe('')
    expect(lane.plate.confirmedPlate).toBe('')
  })

  it('stops lane and releases both cameras', async () => {
    const wrapper = await mountMonitor()
    const lane = wrapper.vm.lanes[0]
    lane.face.cameraRunning = true
    lane.plate.cameraRunning = true

    await wrapper.vm.stopLane(lane)

    expect(faceApi.stopCamera).toHaveBeenCalled()
    expect(plateApi.turnOffCamera).toHaveBeenCalled()
    expect(lane.face.cameraRunning).toBe(false)
    expect(lane.plate.cameraRunning).toBe(false)
  })
})