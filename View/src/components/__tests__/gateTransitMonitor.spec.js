import { flushPromises, mount } from '@vue/test-utils'
import { afterAll, afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('jsqr', () => ({ default: vi.fn() }))
vi.mock('axios', () => ({ default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn(), delete: vi.fn() } }))
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
}))
vi.mock('../../services/gateTransitApi', () => ({ scanGate: vi.fn(), scanGuest: vi.fn() }))
vi.mock('../../services/dynamicQrVerifyApi', () => ({ verifyDynamicQr: vi.fn() }))
vi.mock('../../services/cameraRuntimeApi', () => ({
  getCameras: vi.fn(),
  startPythonQrProcess: vi.fn(),
  stopPythonQrProcess: vi.fn(),
  startPythonPlateProcess: vi.fn(),
  stopPythonPlateProcess: vi.fn(),
  startPythonSimulatedCameraProcess: vi.fn(),
  stopPythonSimulatedCameraProcess: vi.fn(),
  getPythonProcessStatus: vi.fn(),
}))
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
  QR_API_BASE_URL: 'http://qr.local:8001',
  QR_API_BASE_URL_LANE2: 'http://qr.local:8002',
}))
vi.mock('../../config/api', () => ({ PLATE_API_BASE_URL: 'http://plate.local/api' }))
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

const plateApi = await import('../../services/plateCameraApi')
const gateTransitApi = await import('../../services/gateTransitApi')
const dynamicQrVerifyApi = await import('../../services/dynamicQrVerifyApi')
const cameraRuntimeApi = await import('../../services/cameraRuntimeApi')
const runtimeServiceApi = await import('../../services/runtimeServiceApi')
const dynamicQrScannerApi = await import('../../services/dynamicQrScannerApi')
const sim = await import('../../services/simulationHarness')
const { enterpriseApi, zoneAuthorityApi } = await import('../../services/enterpriseSecurityApi')
const { authState } = await import('../../stores/auth')

const GateTransitMonitor = (await import('../GateTransitMonitor.vue')).default

const cameraFixture = {
  cameraId: 10,
  cameraName: 'Cổng chính',
  streamUrl: 'rtsp://192.168.1.10:554/stream?subtype=1',
  urlView: 'http://go2rtc.local/stream.html?src=cam_gate',
}

let alertMock

beforeEach(() => {
  vi.clearAllMocks()
  plateApi.getCameraStatus.mockResolvedValue({})
  plateApi.getCameraResult.mockResolvedValue({})
  plateApi.getLockedImages.mockResolvedValue({})
  plateApi.turnOnCamera.mockResolvedValue({ success: true, session_id: 3, message: 'OK' })
  plateApi.turnOffCamera.mockResolvedValue({ message: 'Đã tắt' })
  plateApi.resetCameraState.mockResolvedValue({})
  gateTransitApi.scanGate.mockResolvedValue({ data: { success: true, message: 'OK', receiptId: 'RCP-1' } })
  gateTransitApi.scanGuest.mockResolvedValue({ data: { success: true, message: 'OK', receiptId: 'RCP-G' } })
  dynamicQrVerifyApi.verifyDynamicQr.mockResolvedValue({ success: true, message: 'Hợp lệ', data: { type: 'EMP', employeeId: 7, employeeName: 'Nguyen A' } })
  cameraRuntimeApi.getCameras.mockResolvedValue([])
  cameraRuntimeApi.getPythonProcessStatus.mockResolvedValue({ data: {} })
  runtimeServiceApi.getRuntimeServices.mockResolvedValue([])
  runtimeServiceApi.updateRuntimeService.mockResolvedValue({})
  runtimeServiceApi.startRuntimeService.mockResolvedValue({})
  runtimeServiceApi.stopRuntimeService.mockResolvedValue({})
  dynamicQrScannerApi.getQrScanResult.mockResolvedValue({})
  dynamicQrScannerApi.startQrScanner.mockResolvedValue({})
  dynamicQrScannerApi.resetQrSession.mockResolvedValue({})
  dynamicQrScannerApi.stopQrScanner.mockResolvedValue({})
  dynamicQrScannerApi.scanQrOnce.mockResolvedValue({})
  zoneAuthorityApi.getMyZones.mockResolvedValue({ data: [{ securityZoneId: 1 }] })
  enterpriseApi.recordLaneEvent.mockResolvedValue({ data: { laneEventId: 5 } })
  enterpriseApi.recordDuressEvent.mockResolvedValue({ data: { duressEventId: 2 } })
  enterpriseApi.createInterventionRequest.mockResolvedValue({ data: { operationalInterventionRequestId: 9 } })
  enterpriseApi.createEmergencyPass.mockResolvedValue({ data: { emergencyPass: { emergencyPassId: 6 } } })
  enterpriseApi.createEmergencyState.mockResolvedValue({ data: { emergencyStateId: 11 } })
  authState.user.role = 'BaoVe'
})

beforeEach(() => {
  alertMock = vi.fn()
  vi.stubGlobal('alert', alertMock)
})

afterEach(() => {
  vi.unstubAllGlobals()
})

const stubComponents = { DecisionDrawer: true, StepUpModal: true, AuditReceiptToast: true }

const mountMonitor = async () => {
  const wrapper = mount(GateTransitMonitor, { global: { stubs: stubComponents } })
  await flushPromises()
  return wrapper
}

describe('GateTransitMonitor', () => {
  it('renders topbar, lanes and cam wall', async () => {
    const wrapper = await mountMonitor()
    expect(wrapper.text()).toContain('Điều phối cổng ra vào')
    expect(wrapper.text()).toContain('Làn 1')
    expect(wrapper.text()).toContain('Làn 2')
    expect(wrapper.findAll('.cam-cell').length).toBe(4)
    expect(wrapper.findAll('.cam-block').length).toBe(4)
  })

  it('loads cameras, zones and plate status on mount', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue([cameraFixture])
    await mountMonitor()
    expect(cameraRuntimeApi.getCameras).toHaveBeenCalled()
    expect(zoneAuthorityApi.getMyZones).toHaveBeenCalled()
    expect(plateApi.getCameraStatus).toHaveBeenCalled()
  })

  it('skips fetchUserZones when admin', async () => {
    authState.user.role = 'Admin'
    await mountMonitor()
    expect(zoneAuthorityApi.getMyZones).not.toHaveBeenCalled()
  })

  it('toggles the topbar description', async () => {
    const wrapper = await mountMonitor()
    const btn = wrapper.findAll('.topbar-toggle').find((b) => b.text().includes('Hiện mô tả'))
    await btn.trigger('click')
    expect(wrapper.vm.topbarCompact).toBe(false)
    expect(wrapper.find('.topbar-desc').isVisible()).toBe(true)
  })

  it('starts and stops the auto monitor', async () => {
    const wrapper = await mountMonitor()
    const startBtn = wrapper.findAll('.btn-auto').find((b) => b.text().includes('Bắt đầu'))
    await startBtn.trigger('click')
    await flushPromises()
    expect(wrapper.vm.autoActive).toBe(true)
    expect(wrapper.vm.lanes[0].auto.error).toContain('Chưa cấu hình camera QR')
    await wrapper.vm.stopAutoMonitor()
    await flushPromises()
    expect(wrapper.vm.autoActive).toBe(false)
  })

  it('opens the ops drawer and refreshes runtime services', async () => {
    const wrapper = await mountMonitor()
    await wrapper.find('.topbar-settings-btn').trigger('click')
    await flushPromises()
    expect(wrapper.vm.opsDrawerOpen).toBe(true)
    expect(runtimeServiceApi.getRuntimeServices).toHaveBeenCalled()
    await wrapper.find('.ops-drawer-close').trigger('click')
    expect(wrapper.vm.opsDrawerOpen).toBe(false)
  })

  it('switches the active lane tab in the drawer', async () => {
    const wrapper = await mountMonitor()
    wrapper.vm.openOpsDrawer()
    await flushPromises()
    const lane2Tab = wrapper.findAll('.ops-drawer-tab').find((b) => b.text().includes('Làn 2'))
    await lane2Tab.trigger('click')
    expect(wrapper.vm.opsActiveLaneId).toBe('lane2')
  })

  it('starts a runtime service via toggleRuntime', async () => {
    const wrapper = await mountMonitor()
    wrapper.vm.runtimeServices = [{ name: 'go2rtc', running: false, enabled: true, autoStart: true }]
    await wrapper.vm.toggleRuntime('go2rtc')
    await flushPromises()
    expect(runtimeServiceApi.startRuntimeService).toHaveBeenCalledWith('go2rtc')
  })

  it('updates autostart of a runtime service', async () => {
    const wrapper = await mountMonitor()
    wrapper.vm.runtimeServices = [{ name: 'cloudflared', running: false, enabled: true, autoStart: true }]
    await wrapper.vm.toggleRuntimeAutoStart('cloudflared')
    expect(runtimeServiceApi.updateRuntimeService).toHaveBeenCalledWith('cloudflared', { autoStart: false })
  })

  it('alerts when toggling QR python without a camera ip', async () => {
    const wrapper = await mountMonitor()
    wrapper.vm.openOpsDrawer()
    await flushPromises()
    await wrapper.vm.onToggleQrPython()
    expect(alertMock).toHaveBeenCalledWith(expect.stringContaining('Chọn URL/stream camera QR'))
  })

  it('turns off the QR python service', async () => {
    const wrapper = await mountMonitor()
    await wrapper.vm.applyQrPythonService(false)
    expect(cameraRuntimeApi.stopPythonQrProcess).toHaveBeenCalled()
    expect(dynamicQrScannerApi.stopQrScanner).toHaveBeenCalled()
  })

  it('filters cameras by keyword', async () => {
    const wrapper = await mountMonitor()
    wrapper.vm.cameras = [
      { cameraId: 1, cameraName: 'Cổng A', streamUrl: 'x', urlView: 'y' },
      { cameraId: 2, cameraName: 'Sân sau', streamUrl: 'x', urlView: 'y' },
    ]
    expect(wrapper.vm.filterCameras('cổng').map((c) => c.cameraId)).toEqual([1])
    expect(wrapper.vm.filterCameras('').length).toBe(2)
    expect(wrapper.vm.filterCameras('2').map((c) => c.cameraId)).toEqual([2])
  })

  it('selects a qr camera into the active lane', async () => {
    const wrapper = await mountMonitor()
    const lane = wrapper.vm.lanes[0]
    wrapper.vm.selectCamera(cameraFixture, lane, 'qr')
    expect(lane.qr.cameraIp).toBe('rtsp://192.168.1.10:554/stream?subtype=0')
    expect(lane.qr.viewUrl).toBe(cameraFixture.urlView)
    expect(lane.qr.previewRunning).toBe(true)
    expect(lane.cameraId).toBe(10)
  })

  it('selects a plate camera into the active lane', async () => {
    const wrapper = await mountMonitor()
    const lane = wrapper.vm.lanes[0]
    wrapper.vm.selectCamera(cameraFixture, lane, 'plate')
    expect(lane.plate.cameraIp).toBe(cameraFixture.streamUrl)
    expect(lane.plate.previewRunning).toBe(true)
  })

  it('alerts when selecting a camera without urlView', async () => {
    const wrapper = await mountMonitor()
    wrapper.vm.selectCamera({ cameraId: 5, cameraName: 'X', streamUrl: 'rtsp://x' }, wrapper.vm.lanes[0], 'qr')
    expect(alertMock).toHaveBeenCalledWith(expect.stringContaining('UrlView'))
  })

  it('reloads preview for the active lane', async () => {
    const wrapper = await mountMonitor()
    const lane = wrapper.vm.lanes[0]
    lane.qr.cameraIp = 'rtsp://x'
    lane.plate.cameraIp = 'rtsp://y'
    lane.qr.viewUrl = 'http://go2rtc.local/stream.html?src=qr'
    lane.plate.viewUrl = 'http://go2rtc.local/stream.html?src=plate'
    await wrapper.vm.previewLane(lane)
    expect(lane.qr.previewRunning).toBe(true)
    expect(lane.plate.previewRunning).toBe(true)
  })

  describe('decision drawer', () => {
    it('opens and closes the decision drawer', async () => {
      const wrapper = await mountMonitor()
      wrapper.vm.openDecisionDrawer(wrapper.vm.lanes[0])
      expect(wrapper.vm.decisionDrawerVisible).toBe(true)
      expect(wrapper.vm.decisionLaneId).toBe('lane1')
      wrapper.vm.closeDecisionDrawer()
      expect(wrapper.vm.decisionDrawerVisible).toBe(false)
    })

    it('executes a manual pass', async () => {
      const wrapper = await mountMonitor()
      wrapper.vm.openDecisionDrawer(wrapper.vm.lanes[0])
      await wrapper.vm.handleDecisionAction({ type: 'manual', reason: 'kiểm tra', details: { plateNumber: '30A-1234', subjectName: 'Nguyen' } })
      expect(enterpriseApi.recordLaneEvent).toHaveBeenCalledWith(expect.objectContaining({ eventType: 'MANUAL_PASS' }))
      expect(wrapper.vm.auditToast.visible).toBe(true)
      expect(wrapper.vm.auditToast.type).toBe('success')
    })

    it('executes allow for an employee', async () => {
      const wrapper = await mountMonitor()
      const lane = wrapper.vm.lanes[0]
      lane.plate.confirmedPlate = '30A-1234'
      lane.qr.employeeId = '7'
      wrapper.vm.openDecisionDrawer(lane)
      await wrapper.vm.handleDecisionAction({ type: 'allow', reason: 'Cho qua' })
      expect(gateTransitApi.scanGate).toHaveBeenCalledWith(expect.objectContaining({ LicensePlate: '30A-1234', EmployeeId: 7 }))
      expect(enterpriseApi.recordLaneEvent).toHaveBeenCalledWith(expect.objectContaining({ eventType: 'ACCESS_GRANTED' }))
    })

    it('executes allow for a guest', async () => {
      const wrapper = await mountMonitor()
      const lane = wrapper.vm.lanes[0]
      lane.plate.confirmedPlate = '30A-1234'
      lane.qr.guestId = '12'
      lane.qr.qrPayload = 'VIS:abc'
      wrapper.vm.openDecisionDrawer(lane)
      await wrapper.vm.handleDecisionAction({ type: 'allow', reason: 'khách' })
      expect(gateTransitApi.scanGuest).toHaveBeenCalledWith(expect.objectContaining({ VisitorDetailId: 12, QrPayload: 'VIS:abc' }))
    })

    it('throws when allowing without plate', async () => {
      const wrapper = await mountMonitor()
      wrapper.vm.openDecisionDrawer(wrapper.vm.lanes[0])
      await wrapper.vm.handleDecisionAction({ type: 'allow', reason: 'x' })
      expect(wrapper.vm.auditToast.type).toBe('danger')
    })

    it('executes deny', async () => {
      const wrapper = await mountMonitor()
      wrapper.vm.openDecisionDrawer(wrapper.vm.lanes[0])
      await wrapper.vm.handleDecisionAction({ type: 'deny', reason: 'Từ chối' })
      expect(enterpriseApi.recordLaneEvent).toHaveBeenCalledWith(expect.objectContaining({ eventType: 'ACCESS_DENIED' }))
      expect(wrapper.vm.auditToast.type).toBe('warning')
    })

    it('executes override', async () => {
      const wrapper = await mountMonitor()
      const lane = wrapper.vm.lanes[0]
      lane.plate.confirmedPlate = '30A-1234'
      lane.qr.employeeId = '7'
      wrapper.vm.openDecisionDrawer(lane)
      await wrapper.vm.handleDecisionAction({ type: 'override', reason: 'đặc biệt', responsibility: 'Management' })
      expect(enterpriseApi.recordLaneEvent).toHaveBeenCalledWith(expect.objectContaining({ eventType: 'OVERRIDE' }))
    })

    it('executes escalate', async () => {
      const wrapper = await mountMonitor()
      wrapper.vm.openDecisionDrawer(wrapper.vm.lanes[0])
      await wrapper.vm.handleDecisionAction({ type: 'escalate', reason: 'nghi vấn' })
      expect(enterpriseApi.createInterventionRequest).toHaveBeenCalled()
      expect(enterpriseApi.recordLaneEvent).toHaveBeenCalledWith(expect.objectContaining({ eventType: 'ESCALATION_REQUEST' }))
    })

    it('executes duress', async () => {
      const wrapper = await mountMonitor()
      wrapper.vm.openDecisionDrawer(wrapper.vm.lanes[0])
      await wrapper.vm.handleDecisionAction({ type: 'duress', reason: 'ép buộc', responsibility: 'BaoVe' })
      expect(enterpriseApi.recordDuressEvent).toHaveBeenCalled()
      expect(wrapper.vm.auditToast.type).toBe('danger')
    })

    it('executes unified emergency pass', async () => {
      const wrapper = await mountMonitor()
      wrapper.vm.openDecisionDrawer(wrapper.vm.lanes[0])
      await wrapper.vm.handleDecisionAction({ type: 'unified_emergency', reason: 'khẩn', details: { subjectName: 'Khach' }, responsibility: true })
      expect(enterpriseApi.createEmergencyPass).toHaveBeenCalled()
      expect(wrapper.vm.auditToast.visible).toBe(true)
    })

    it('blocks emergency for non-admin', async () => {
      const wrapper = await mountMonitor()
      wrapper.vm.openDecisionDrawer(wrapper.vm.lanes[0])
      await wrapper.vm.handleDecisionAction({ type: 'emergency', reason: 'khẩn', responsibility: true })
      expect(enterpriseApi.createEmergencyPass).not.toHaveBeenCalled()
      expect(wrapper.vm.auditToast.title).toBe('Không có quyền')
    })

    it('allows emergency for admin', async () => {
      authState.user.role = 'Admin'
      const wrapper = await mountMonitor()
      wrapper.vm.openDecisionDrawer(wrapper.vm.lanes[0])
      await wrapper.vm.handleDecisionAction({ type: 'emergency', reason: 'khẩn', responsibility: true })
      expect(enterpriseApi.createEmergencyPass).toHaveBeenCalled()
    })
  })

  describe('lane operations', () => {
    it('confirms a lane via scanGate', async () => {
      const wrapper = await mountMonitor()
      const lane = wrapper.vm.lanes[0]
      lane.plate.confirmedPlate = '30A-1234'
      lane.plate.lockedSnapshot = 'data:image/jpeg;base64,AAA'
      lane.qr.employeeId = '7'
      lane.qr.qrPayload = 'EMP:xyz'
      await wrapper.vm.confirmLane(lane)
      expect(gateTransitApi.scanGate).toHaveBeenCalledWith(expect.objectContaining({ CredentialType: 'QR', PlateSnapshotBase64: 'data:image/jpeg;base64,AAA' }))
    })

    it('alerts when confirming without plate', async () => {
      const wrapper = await mountMonitor()
      await wrapper.vm.confirmLane(wrapper.vm.lanes[0])
      expect(alertMock).toHaveBeenCalledWith(expect.stringContaining('chưa có biển số'))
    })

    it('alerts when confirming without employee id', async () => {
      const wrapper = await mountMonitor()
      const lane = wrapper.vm.lanes[0]
      lane.plate.confirmedPlate = '30A-1234'
      await wrapper.vm.confirmLane(lane)
      expect(alertMock).toHaveBeenCalledWith(expect.stringContaining('chưa có Employee ID'))
    })

    it('reads plate again by turning on the camera', async () => {
      const wrapper = await mountMonitor()
      const lane = wrapper.vm.lanes[0]
      plateApi.getCameraResult.mockResolvedValue({ session_id: 3, camera_enabled: true, confirmed_plate: '30A-1234' })
      lane.plate.cameraIp = 'rtsp://plate'
      lane.plate.currentIp = 'rtsp://plate'
      await wrapper.vm.retryPlate(lane)
      expect(lane.plate.message).toBeDefined()
      expect(lane.plate.cameraRunning).toBe(true)
      lane.plate.destroyed = true
      wrapper.vm.stopPlateLoop(lane)
      lane.qr.destroyed = true
      wrapper.vm.stopQrLoops(lane)
    })

    it('alerts retryQr when no qr url', async () => {
      const wrapper = await mountMonitor()
      await wrapper.vm.retryQr(wrapper.vm.lanes[0])
      expect(alertMock).toHaveBeenCalledWith(expect.stringContaining('QR'))
    })
  })

  describe('qr transactions', () => {
    it('verifies an employee dynamic qr', async () => {
      const wrapper = await mountMonitor()
      const lane = wrapper.vm.lanes[0]
      const result = await wrapper.vm.doVerifyQr(lane, 'EMP:abc')
      expect(dynamicQrVerifyApi.verifyDynamicQr).toHaveBeenCalled()
      expect(result.success).toBe(true)
      expect(lane.qr.verifyData).toMatchObject({ employeeId: 7 })
    })

    it('rejects empty payloads', async () => {
      const wrapper = await mountMonitor()
      const result = await wrapper.vm.doVerifyQr(wrapper.vm.lanes[0], '   ')
      expect(result.success).toBe(false)
      expect(result.message).toContain('rỗng')
    })

    it('rejects unknown qr formats', async () => {
      const wrapper = await mountMonitor()
      const result = await wrapper.vm.doVerifyQr(wrapper.vm.lanes[0], 'RANDOM:12')
      expect(result.message).toBe('QR không đúng định dạng')
    })

    it('expires an old qr session', async () => {
      const wrapper = await mountMonitor()
      const qr = wrapper.vm.lanes[0].qr
      qr.activeSessionPayload = 'EMP:old'
      qr.lastSeenAt = Date.now() - 6000
      wrapper.vm.checkQrSessionExpiry(wrapper.vm.lanes[0])
      expect(qr.activeSessionPayload).toBe('')
      expect(qr.message).toContain('hết hạn')
    })
  })

  describe('plate state', () => {
    it('applies a realtime plate state', async () => {
      const wrapper = await mountMonitor()
      const lane = wrapper.vm.lanes[0]
      lane.plate.cameraRunning = true
      lane.plate.sessionId = 3
      lane.plate.lastAppliedSessionId = 3
      await wrapper.vm.applyPlateRealtimeState(lane, {
        session_id: 3,
        camera_enabled: true,
        confirmed_plate: '30A-1234',
        scan_locked: true,
        scan_active: true,
        fps: 12,
        ocr_running: true,
        message: 'ok',
      })
      expect(lane.plate.confirmedPlate).toBe('30A-1234')
      expect(lane.plate.scanLocked).toBe(true)
      expect(lane.plate.overlayText).toBe('30A-1234')
      lane.plate.destroyed = true
      wrapper.vm.stopPlateLoop(lane)
    })

    it('plate preview status text', async () => {
      const wrapper = await mountMonitor()
      const plate = wrapper.vm.lanes[0].plate
      expect(wrapper.vm.platePreviewStatusText(plate)).toBe('Đang tắt')
      plate.previewRunning = true
      expect(wrapper.vm.platePreviewStatusText(plate)).toBe('Chờ hình ảnh')
      plate.lockedSnapshot = 'data:image/'
      expect(wrapper.vm.platePreviewStatusText(plate)).toBe('Ảnh đã chụp')
      plate.lockedSnapshot = ''
      plate.previewHealthy = true
      expect(wrapper.vm.platePreviewStatusText(plate)).toBe('Đang trực tuyến')
    })
  })

  describe('visual state helpers', () => {
    it('camera visual state for qr lanes', async () => {
      const wrapper = await mountMonitor()
      const lane = wrapper.vm.lanes[0]
      expect(wrapper.vm.cameraVisualState('qr', lane)).toBe('idle')
      expect(wrapper.vm.cameraVisualText('qr', lane)).toBe('IDLE')
      lane.qr.cameraRunning = true
      lane.qr.backendPhase = 'scanning'
      expect(wrapper.vm.cameraVisualState('qr', lane)).toBe('scanning')
      lane.qr.alert = true
      expect(wrapper.vm.cameraVisualState('qr', lane)).toBe('invalid')
    })

    it('camera visual state for plate lanes', async () => {
      const wrapper = await mountMonitor()
      const lane = wrapper.vm.lanes[0]
      lane.plate.cameraRunning = true
      lane.plate.scanActive = true
      expect(wrapper.vm.cameraVisualState('plate', lane)).toBe('scanning')
      lane.plate.confirmedPlate = '30A'
      lane.plate.scanLocked = true
      expect(wrapper.vm.cameraVisualState('plate', lane)).toBe('valid')
    })
  })

  describe('identity overlay', () => {
    it('builds overlay for employees', async () => {
      const wrapper = await mountMonitor()
      expect(wrapper.vm.buildQrIdentityOverlay({ employeeId: 7, employeeName: 'Nguyen A' })).toBe('ID: 7 | TEN: Nguyen A')
    })

    it('builds overlay for static guests', async () => {
      const wrapper = await mountMonitor()
      const overlay = wrapper.vm.buildQrIdentityOverlay({ type: 'STATIC', visitorDetailId: 12, fullName: 'Khach' })
      expect(overlay).toContain('ID: 12')
      expect(overlay).toContain('TEN: Khach')
    })
  })

  describe('decision helpers', () => {
    it('builds decision subject info', async () => {
      const wrapper = await mountMonitor()
      const lane = wrapper.vm.lanes[0]
      lane.qr.employeeId = '7'
      lane.qr.employeeName = 'Nguyen A'
      lane.plate.confirmedPlate = '30A-1234'
      const info = wrapper.vm.buildDecisionSubjectInfo(lane)
      expect(info).toMatchObject({ type: 'EMPLOYEE', name: 'Nguyen A', plate: '30A-1234' })
    })

    it('computes decision warnings', async () => {
      const wrapper = await mountMonitor()
      const lane = wrapper.vm.lanes[0]
      lane.plate.confirmedPlate = '30A-1234'
      lane.qr.alert = true
      wrapper.vm.openDecisionDrawer(lane)
      await wrapper.vm.$nextTick()
      const warnings = wrapper.vm.decisionWarnings
      expect(warnings.some((w) => w.text.includes('anti-passback'))).toBe(true)
      expect(warnings.some((w) => w.text.includes('QR không hợp lệ'))).toBe(true)
    })

    it('computes action permissions for BaoVe', async () => {
      const wrapper = await mountMonitor()
      const lane = wrapper.vm.lanes[0]
      expect(wrapper.vm.getActionPermissions(lane)).toMatchObject({ allow: false, manual: true })
      lane.qr.employeeId = '7'
      lane.plate.confirmedPlate = '30A'
      const perms = wrapper.vm.getActionPermissions(lane)
      expect(perms.allow).toBe(true)
      expect(perms.escalate).toBe(true)
    })
  })

  describe('utility helpers', () => {
    it('prefers the main qr stream', async () => {
      const wrapper = await mountMonitor()
      expect(wrapper.vm.preferMainQrStream('rtsp://x?subtype=1')).toBe('rtsp://x?subtype=0')
      expect(wrapper.vm.preferMainQrStream('rtsp://x?subtype=0')).toBe('rtsp://x?subtype=0')
      expect(wrapper.vm.preferMainQrStream('')).toBe('')
    })

    it('extracts go2rtc stream names', async () => {
      const wrapper = await mountMonitor()
      expect(wrapper.vm.extractGo2RtcStreamName('http://go2rtc/stream.html?src=cam1')).toBe('cam1')
      expect(wrapper.vm.extractGo2RtcStreamName('xyz')).toBe('')
    })

    it('resolves effective qr stream', async () => {
      const wrapper = await mountMonitor()
      const lane = wrapper.vm.lanes[0]
      lane.qr.cameraIp = 'rtsp://direct'
      expect(wrapper.vm.getEffectiveQrStream(lane)).toBe('rtsp://direct')
      lane.qr.cameraIp = 'go2rtc:cam1'
      lane.qr.viewUrl = 'http://go2rtc/stream.html?src=cam1'
      expect(wrapper.vm.getEffectiveQrStream(lane)).toBe('go2rtc:cam1')
    })

    it('builds direct camera url with mse mode', async () => {
      const wrapper = await mountMonitor()
      const url = wrapper.vm.buildDirectCameraUrl('http://go2rtc/stream.html?src=cam1')
      expect(url).toContain('mode=mse')
    })

    it('shortens long text', async () => {
      const wrapper = await mountMonitor()
      expect(wrapper.vm.shortText('')).toBe('-----')
      expect(wrapper.vm.shortText('1234567890', 5)).toBe('12345...')
      expect(wrapper.vm.shortText('abc')).toBe('abc')
    })

    it('normalizes percent and legacy boxes', async () => {
      const wrapper = await mountMonitor()
      const pct = wrapper.vm.normalizeBox({ x: 10, y: 20, width: 30, height: 40 })
      expect(pct.unit).toBe('%')
      const legacy = wrapper.vm.normalizeBox({ x1: 5, y1: 5, x2: 50, y2: 60 })
      expect(legacy).toMatchObject({ x: 5, width: 45 })
      expect(wrapper.vm.normalizeBox(null)).toBe(null)
      expect(wrapper.vm.normalizeBox({ foo: 1 })).toBe(null)
    })

    it('renders bounding styles', async () => {
      const wrapper = await mountMonitor()
      expect(wrapper.vm.boundingStyle({ x: 10, y: 20, width: 5, height: 5, unit: '%' })).toEqual({
        left: '10%', top: '20%', width: '5%', height: '5%',
      })
      expect(wrapper.vm.boundingStyle(null)).toEqual({})
    })

    it('formats dates', async () => {
      const wrapper = await mountMonitor()
      expect(wrapper.vm.formatDate('')).toBe('')
      expect(wrapper.vm.formatDate('2026-01-01T00:00:00')).toBe(new Date('2026-01-01T00:00:00').toLocaleString())
    })

    it('builds qr lane base urls by lane id', async () => {
      const wrapper = await mountMonitor()
      expect(wrapper.vm.getLaneQrApiBase(wrapper.vm.lanes[0])).toBe('http://qr.local:8001')
      expect(wrapper.vm.getLaneQrApiBase(wrapper.vm.lanes[1])).toBe('http://qr.local:8002')
    })
  })
})