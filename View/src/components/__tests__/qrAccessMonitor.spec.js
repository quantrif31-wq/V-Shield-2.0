import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'

const mocks = vi.hoisted(() => ({
  http: { get: vi.fn(), post: vi.fn() },
  cameraRuntimeApi: { getCameras: vi.fn() },
  gateTransitApi: { getManualGates: vi.fn() },
  runtimeServiceApi: {
    getRuntimeServices: vi.fn(),
    startRuntimeService: vi.fn(),
    stopRuntimeService: vi.fn(),
    updateRuntimeService: vi.fn(),
  },
  qrScannerApi: {
    startQrScanner: vi.fn(),
    resetQrSession: vi.fn(),
    stopQrScanner: vi.fn(),
    getQrScanResult: vi.fn(),
    scanQrOnce: vi.fn(),
  },
  viewPrefs: { loadViewPrefs: vi.fn(), saveViewPrefs: vi.fn() },
}))

vi.mock('../../services/http', () => ({ default: mocks.http }))
vi.mock('../../services/cameraRuntimeApi', () => mocks.cameraRuntimeApi)
vi.mock('../../services/gateTransitApi', () => mocks.gateTransitApi)
vi.mock('../../services/runtimeServiceApi', () => mocks.runtimeServiceApi)
vi.mock('../../services/dynamicQrScannerApi', () => mocks.qrScannerApi)
vi.mock('../../services/viewPrefs', () => mocks.viewPrefs)

import QrAccessMonitor from '../QrAccessMonitor.vue'

const gate1 = { gateId: 1, gateName: 'Cong A', location: 'Tang 1' }
const gate2 = { gateId: 2, gateName: 'Cong B', location: 'Tang 2' }
const cam1 = { cameraId: 11, cameraName: 'Cam A', gateId: 1, streamUrl: 'rtsp://a', urlView: 'http://a/view' }
const camFree = { cameraId: 22, cameraName: 'Cam Tu do', gateId: 0, streamUrl: 'rtsp://f', urlView: 'http://f/view' }

function flushPromises() {
  return new Promise((resolve) => setTimeout(resolve, 0))
}

let wrapper

beforeEach(() => {
  vi.resetAllMocks()
  mocks.cameraRuntimeApi.getCameras.mockResolvedValue([cam1, camFree])
  mocks.gateTransitApi.getManualGates.mockResolvedValue({ data: { data: [gate1, gate2] } })
  mocks.runtimeServiceApi.getRuntimeServices.mockResolvedValue([{ name: 'python_qr', running: false, autoStart: false, enabled: true }])
  mocks.http.get.mockResolvedValue({ data: { mfaRequired: false } })
  mocks.http.post.mockResolvedValue({ data: { data: {} } })
  mocks.qrScannerApi.getQrScanResult.mockResolvedValue(null)
  mocks.viewPrefs.loadViewPrefs.mockReturnValue(null)
  vi.spyOn(window, 'alert').mockImplementation(() => {})
})

afterEach(() => {
  if (wrapper) wrapper.unmount()
  wrapper = null
  vi.restoreAllMocks()
  vi.clearAllTimers()
})

async function mountMonitor(prefs) {
  mocks.viewPrefs.loadViewPrefs.mockReturnValue(prefs || null)
  wrapper = mount(QrAccessMonitor)
  await flushPromises()
  await flushPromises()
  return wrapper
}

describe('QrAccessMonitor.vue', () => {
  it('mounts and loads cameras, gates and runtime services', async () => {
    const wrapper = await mountMonitor()
    expect(wrapper.vm.cameras).toHaveLength(2)
    expect(wrapper.vm.gates).toHaveLength(2)
    expect(wrapper.vm.gateLoading).toBe(false)
    expect(wrapper.vm.runtimeLoading).toBe(false)
    expect(mocks.cameraRuntimeApi.getCameras).toHaveBeenCalled()
    expect(mocks.gateTransitApi.getManualGates).toHaveBeenCalled()
    expect(mocks.runtimeServiceApi.getRuntimeServices).toHaveBeenCalled()
  })

  it('handles the gate load failing gracefully', async () => {
    mocks.gateTransitApi.getManualGates.mockRejectedValue(new Error('boom'))
    const wrapper = await mountMonitor()
    expect(wrapper.vm.gates).toEqual([])
    expect(wrapper.vm.gateLoading).toBe(false)
  })

  it('maps gate ids to names', async () => {
    const wrapper = await mountMonitor()
    expect(wrapper.vm.gateNameById(1)).toBe('Cong A')
    expect(wrapper.vm.gateNameById(999)).toBe('')
    expect(wrapper.vm.gateNameById(0)).toBe('')
  })

  it('restores a saved term setup when the gate and camera still exist', async () => {
    const wrapper = await mountMonitor({
      appliedGateId: 1,
      gateName: 'Cong A',
      cameraId: 11,
      cameraName: 'Cam A',
      cameraVerified: true,
    })
    const term = wrapper.vm.terminals[0]
    expect(term.appliedGateId).toBe(1)
    expect(term.cameraId).toBe(11)
    expect(term.cameraVerified).toBe(true)
    expect(term.cameraIp).toBe('rtsp://a')
    expect(term.viewUrl).toBe('http://a/view')
    expect(wrapper.vm.cameraSearch.term1).toBe('Cam A')
  })

  it('clears a verified camera when it no longer belongs to the stored gate', async () => {
    const wrapper = await mountMonitor({
      appliedGateId: 2,
      cameraId: 11,
      cameraVerified: true,
      cameraIp: 'rtsp://x',
      viewUrl: 'http://x/view',
    })
    const term = wrapper.vm.terminals[0]
    expect(term.cameraVerified).toBe(false)
    expect(term.cameraId).toBeNull()
    expect(term.cameraIp).toBe('')
  })

  it('does not restore setup without saved prefs', async () => {
    const wrapper = await mountMonitor({ appliedGateId: 99 })
    const term = wrapper.vm.terminals[0]
    expect(term.appliedGateId).toBeNull()
  })

  it('persists the term setup via saveViewPrefs', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    term.appliedGateId = 2
    term.cameraId = 11
    term.cameraName = 'Cam A'
    term.cameraIp = 'rtsp://a'
    term.viewUrl = 'http://a/view'
    term.cameraVerified = true
    wrapper.vm.persistTermSetup(term)
    expect(mocks.viewPrefs.saveViewPrefs).toHaveBeenCalledWith('QrAccessMonitor', {
      appliedGateId: 2,
      gateName: 'Cong B',
      cameraId: 11,
      cameraName: 'Cam A',
      cameraIp: 'rtsp://a',
      viewUrl: 'http://a/view',
      cameraVerified: true,
    })
  })

  it('computes runtime helpers and toggle classes', async () => {
    const wrapper = await mountMonitor()
    wrapper.vm.runtimeServices = [{ name: 'python_qr', running: true, autoStart: true, enabled: false }]
    wrapper.vm.runtimeBusy = { python_qr: true }
    expect(wrapper.vm.runtimeRunning('python_qr')).toBe(true)
    expect(wrapper.vm.runtimeAutoStart('python_qr')).toBe(true)
    expect(wrapper.vm.runtimeEnabled('python_qr')).toBe(false)
    expect(wrapper.vm.runtimeIsBusy('python_qr')).toBe(true)
    expect(wrapper.vm.runtimeState('python_qr').name).toBe('python_qr')
    expect(wrapper.vm.toggleSwitchClass('python_qr', true)).toEqual({ on: true, pending: true })
    expect(wrapper.vm.runtimeEnabled('unknown')).toBe(false)
    expect(wrapper.vm.runtimeRunning('unknown')).toBe(false)
  })

  it('starts the python_qr runtime when it is off', async () => {
    const wrapper = await mountMonitor()
    await wrapper.vm.toggleRuntime('python_qr')
    expect(mocks.runtimeServiceApi.startRuntimeService).toHaveBeenCalledWith('python_qr')
    expect(mocks.runtimeServiceApi.getRuntimeServices).toHaveBeenCalled()
    expect(wrapper.vm.runtimeBusy.python_qr).toBe(false)
  })

  it('stops the python_qr runtime when it is running', async () => {
    mocks.runtimeServiceApi.getRuntimeServices.mockResolvedValue([{ name: 'python_qr', running: true, autoStart: false, enabled: true }])
    const wrapper = await mountMonitor()
    await wrapper.vm.toggleRuntime('python_qr')
    expect(mocks.runtimeServiceApi.stopRuntimeService).toHaveBeenCalledWith('python_qr')
  })

  it('ignores toggle while busy and alerts on failure', async () => {
    const wrapper = await mountMonitor()
    wrapper.vm.runtimeBusy = { python_qr: true }
    await wrapper.vm.toggleRuntime('python_qr')
    expect(mocks.runtimeServiceApi.startRuntimeService).not.toHaveBeenCalled()
    wrapper.vm.runtimeBusy = {}
    mocks.runtimeServiceApi.startRuntimeService.mockRejectedValue(new Error('down'))
    await wrapper.vm.toggleRuntime('python_qr')
    expect(window.alert).toHaveBeenCalled()
    expect(wrapper.vm.runtimeBusy.python_qr).toBe(false)
  })

  it('toggles runtime auto start and alerts on failure', async () => {
    const wrapper = await mountMonitor()
    await wrapper.vm.toggleRuntimeAutoStart('python_qr')
    expect(mocks.runtimeServiceApi.updateRuntimeService).toHaveBeenCalledWith('python_qr', { autoStart: true })
    mocks.runtimeServiceApi.updateRuntimeService.mockRejectedValue(new Error('down'))
    await wrapper.vm.toggleRuntimeAutoStart('python_qr')
    expect(window.alert).toHaveBeenCalled()
  })

  it('opens the gate lock modal for a different gate and fetches mfa status', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    term.appliedGateId = 1
    mocks.http.get.mockResolvedValue({ data: { mfaRequired: true } })
    term.gateId = 2
    wrapper.vm.onSelectGate(term)
    await flushPromises()
    expect(wrapper.vm.gateLockModal.open).toBe(true)
    expect(wrapper.vm.gateLockModal.targetGateId).toBe(2)
    expect(wrapper.vm.gateLockModal.mfaRequired).toBe(true)
    expect(mocks.http.get).toHaveBeenCalledWith('/Auth/me')
  })

  it('resets the select for no target and no-ops for the same gate', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    term.appliedGateId = 1
    term.gateId = null
    wrapper.vm.onSelectGate(term)
    expect(term.gateId).toBe(1)
    expect(wrapper.vm.gateLockModal.open).toBe(false)
    term.gateId = 1
    wrapper.vm.onSelectGate(term)
    expect(wrapper.vm.gateLockModal.open).toBe(false)
  })

  it('shows a security error when the mfa check fails', async () => {
    const wrapper = await mountMonitor()
    wrapper.vm.gateLockModal = { open: true, error: '' }
    mocks.http.get.mockRejectedValue(new Error('fail'))
    await wrapper.vm.fetchGateLockMfaRequired()
    expect(wrapper.vm.gateLockModal.error).toContain('Không lấy được')
  })

  it('validates password and mfa before confirming the gate change', async () => {
    const wrapper = await mountMonitor()
    wrapper.vm.gateLockModal = {
      open: true,
      term: wrapper.vm.terminals[0],
      password: '',
      mfaCode: '',
      mfaRequired: true,
      prevGateId: null,
      loading: false,
      error: '',
    }
    await wrapper.vm.confirmGateLock()
    expect(wrapper.vm.gateLockModal.error).toContain('mật khẩu')
    wrapper.vm.gateLockModal.password = 'secret'
    wrapper.vm.gateLockModal.mfaCode = ''
    await wrapper.vm.confirmGateLock()
    expect(wrapper.vm.gateLockModal.error).toContain('xác thực hai bước')
  })

  it('confirms the gate change through step-up on success', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    wrapper.vm.gateLockModal = {
      open: true,
      term,
      targetGateId: 2,
      targetGateName: 'Cong B',
      prevGateId: 1,
      password: 'secret',
      mfaCode: '123456',
      mfaRequired: true,
      loading: false,
      error: '',
    }
    mocks.http.post.mockResolvedValue({ data: { sessionId: 's1' } })
    await wrapper.vm.confirmGateLock()
    expect(mocks.http.post).toHaveBeenCalledWith('/Auth/step-up/start', expect.objectContaining({ action: 'GateSelection' }))
    expect(mocks.http.post).toHaveBeenCalledWith('/Auth/step-up/verify', expect.objectContaining({ password: 'secret', mfaCode: '123456' }))
    expect(wrapper.vm.gateLockModal.open).toBe(false)
    expect(term.appliedGateId).toBe(2)
    expect(term.cameraId).toBeNull()
    expect(mocks.viewPrefs.saveViewPrefs).toHaveBeenCalled()
  })

  it('rejects the gate change with a bad-credentials message on 401', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    wrapper.vm.gateLockModal = {
      open: true,
      term,
      targetGateId: 2,
      targetGateName: 'Cong B',
      prevGateId: 1,
      password: 'wrong',
      mfaCode: '',
      mfaRequired: false,
      loading: false,
      error: '',
    }
    mocks.http.post.mockRejectedValue({ response: { status: 401 } })
    await wrapper.vm.confirmGateLock()
    expect(wrapper.vm.gateLockModal.error).toContain('không đúng')
    expect(wrapper.vm.gateLockModal.password).toBe('')
    expect(wrapper.vm.gateLockModal.loading).toBe(false)
  })

  it('cancels the gate lock and restores the previous gate', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    term.gateId = 2
    wrapper.vm.gateLockModal = { open: true, term, prevGateId: 1, loading: false }
    wrapper.vm.cancelGateLock()
    expect(term.gateId).toBe(1)
    expect(wrapper.vm.gateLockModal.open).toBe(false)
  })

  it('filters cameras by gate and keyword', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    term.appliedGateId = 1
    expect(wrapper.vm.filterCameras('', term)).toEqual([cam1])
    expect(wrapper.vm.filterCameras('99', term)).toEqual([])
    expect(wrapper.vm.filterCameras('Cam', term)).toEqual([cam1])
    term.appliedGateId = 0
    expect(wrapper.vm.filterCameras('', term)).toEqual([camFree])
  })

  it('limits dropdown matches and hides when verified', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    term.appliedGateId = 1
    wrapper.vm.cameraSearch.term1 = 'Cam A'
    term.cameraVerified = true
    term.cameraId = 11
    wrapper.vm.cameraOpen.term1 = true
    expect(wrapper.vm.dropdownMatches(term)).toEqual([cam1])
    expect(wrapper.vm.isDropVisible(term)).toBe(false)
    term.cameraId = 999
    expect(wrapper.vm.isDropVisible(term)).toBe(true)
    wrapper.vm.cameraOpen.term1 = false
    expect(wrapper.vm.isDropVisible(term)).toBe(false)
  })

  it('warns on choose camera without urls and opens the auth modal otherwise', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    wrapper.vm.onChooseCamera({ cameraId: 5, cameraName: 'Bad' }, term)
    expect(window.alert).toHaveBeenCalled()
    wrapper.vm.onChooseCamera(cam1, term)
    expect(wrapper.vm.authModal.open).toBe(true)
    expect(wrapper.vm.authModal.cameraId).toBe(11)
  })

  it('closes the auth modal unless busy', async () => {
    const wrapper = await mountMonitor()
    wrapper.vm.authModal = { open: true, loading: true }
    wrapper.vm.closeAuthModal()
    expect(wrapper.vm.authModal.open).toBe(true)
    wrapper.vm.authModal.loading = false
    wrapper.vm.closeAuthModal()
    expect(wrapper.vm.authModal.open).toBe(false)
  })

  it('confirms camera auth and persists the camera', async () => {
    mocks.cameraRuntimeApi.getCameras.mockResolvedValue([cam1])
    mocks.viewPrefs.loadViewPrefs.mockReturnValue({ appliedGateId: 1, cameraId: 11, cameraVerified: true, cameraIp: 'rtsp://a', viewUrl: 'http://a/view' })
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    wrapper.vm.authModal = {
      open: true,
      termId: 'term1',
      cameraId: 11,
      cameraName: 'Cam A',
      cameraIp: 'rtsp://a',
      viewUrl: 'http://a/view',
      gateId: 1,
      loading: false,
      error: '',
    }
    mocks.http.post.mockResolvedValue({ data: {} })
    await wrapper.vm.confirmCameraAuth()
    expect(term.cameraVerified).toBe(true)
    expect(term.cameraId).toBe(11)
    expect(term.cameraName).toBe('Cam A')
    expect(wrapper.vm.authModal.open).toBe(false)
    expect(mocks.viewPrefs.saveViewPrefs).toHaveBeenCalled()
  })

  it('surfaces camera auth errors', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    wrapper.vm.authModal = { open: true, termId: 'term1', cameraId: 11, cameraIp: 'rtsp://a', viewUrl: 'http://a', loading: false, error: '' }
    mocks.http.post.mockRejectedValue({ response: { data: { message: 'sai mật khẩu' } } })
    await wrapper.vm.confirmCameraAuth()
    expect(wrapper.vm.authModal.error).toContain('sai mật khẩu')
    expect(wrapper.vm.authModal.loading).toBe(false)
    expect(term.cameraVerified).toBe(false)
  })

  it('guards startScanner with alerts for missing setup', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    await wrapper.vm.startScanner(term)
    expect(window.alert).toHaveBeenCalled()
    term.appliedGateId = 1
    await wrapper.vm.startScanner(term)
    expect(window.alert).toHaveBeenCalled()
    term.cameraId = 11
    term.cameraVerified = true
    await wrapper.vm.startScanner(term)
    expect(window.alert).toHaveBeenCalled()
    expect(mocks.qrScannerApi.startQrScanner).not.toHaveBeenCalled()
  })

  it('starts the scanner and begins polling', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    Object.assign(term, { appliedGateId: 1, cameraId: 11, cameraVerified: true, cameraIp: 'rtsp://a', viewUrl: 'http://a/view' })
    await wrapper.vm.startScanner(term)
    expect(mocks.runtimeServiceApi.startRuntimeService).toHaveBeenCalledWith('python_qr')
    expect(mocks.qrScannerApi.startQrScanner).toHaveBeenCalledWith('rtsp://a')
    expect(term.previewRunning).toBe(true)
    expect(term.continuousActive).toBe(true)
    expect(term.resultTimer).toBeTruthy()
    expect(mocks.qrScannerApi.resetQrSession).toHaveBeenCalled()
    expect(mocks.qrScannerApi.scanQrOnce).toHaveBeenCalled()
  })

  it('does not restart the runtime when python_qr is already running', async () => {
    mocks.runtimeServiceApi.getRuntimeServices.mockResolvedValue([{ name: 'python_qr', running: true, autoStart: false, enabled: true }])
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    Object.assign(term, { appliedGateId: 1, cameraId: 11, cameraVerified: true, cameraIp: 'rtsp://a', viewUrl: 'http://a/view' })
    await wrapper.vm.startScanner(term)
    expect(mocks.runtimeServiceApi.startRuntimeService).not.toHaveBeenCalled()
  })

  it('stops the scanner and cleans up state', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    Object.assign(term, { appliedGateId: 1, cameraId: 11, cameraVerified: true, cameraIp: 'rtsp://a', viewUrl: 'http://a/view' })
    await wrapper.vm.startScanner(term)
    await wrapper.vm.stopScanner(term)
    expect(term.previewRunning).toBe(false)
    expect(term.continuousActive).toBe(false)
    expect(term.resultTimer).toBeNull()
    expect(mocks.qrScannerApi.stopQrScanner).toHaveBeenCalled()
  })

  it('tracks a session reset when the scan session is gone', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    term.sessionActive = true
    term.permissionState = 'allow'
    mocks.qrScannerApi.getQrScanResult.mockResolvedValue({ session_active: false })
    await wrapper.vm.pullQrResult(term)
    expect(term.sessionActive).toBe(false)
    expect(term.permissionState).toBe('idle')
  })

  it('short-circuits on cooldown payloads', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    term.scanSessionActive = true
    term.sessionTimer = setTimeout(() => {}, 1000)
    mocks.qrScannerApi.getQrScanResult.mockResolvedValue({ session_active: true, cooldown_payload: 'x' })
    await wrapper.vm.pullQrResult(term)
    expect(term.scanSessionActive).toBe(false)
    expect(term.sessionTimer).toBeNull()
  })

  it('processes a new locked QR and allows access', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    Object.assign(term, { cameraId: 11, appliedGateId: 1, previewRunning: true })
    mocks.qrScannerApi.getQrScanResult.mockResolvedValue({ session_active: true, locked: true, qr: 'PAYLOAD', locked_at: 123 })
    mocks.http.post.mockResolvedValue({ data: { data: { employeeId: 'E1', subjectName: 'Nam' }, message: 'Cho phep' } })
    await wrapper.vm.pullQrResult(term)
    expect(mocks.http.post).toHaveBeenCalledWith('/QrAccess/scan-access', expect.objectContaining({ QrPayload: 'PAYLOAD', CameraId: 11, GateId: 1 }))
    expect(term.sessionLocked).toBe(true)
    expect(term.permissionState).toBe('allow')
    expect(term.identityLabel).toContain('#1')
    expect(term.identityLabel).toContain('Nhân viên')
    expect(term.traceCounter).toBe(1)
  })

  it('ignores repeated locks for the same payload', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    term.lastLockedAt = 123
    mocks.qrScannerApi.getQrScanResult.mockResolvedValue({ session_active: true, locked: true, qr: 'PAYLOAD', locked_at: 123 })
    await wrapper.vm.pullQrResult(term)
    expect(mocks.http.post).not.toHaveBeenCalled()
  })

  it('marks access as denied for a visitor with a 401', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    term.cameraId = 11
    term.appliedGateId = 1
    mocks.http.post.mockRejectedValue({ response: { status: 401, data: { message: 'x' } } })
    await wrapper.vm.callApiScanAccess(term, 'PAY')
    expect(term.permissionState).toBe('deny')
    expect(term.verifyMessage).toContain('Phiên đăng nhập không hợp lệ')
    expect(term.verifiedType).toBe('Khách')
  })

  it('marks access denied with a generic message otherwise', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    mocks.http.post.mockRejectedValue({ response: { status: 500, data: { data: {}, message: 'Nội bộ' } } })
    await wrapper.vm.callApiScanAccess(term, 'PAY')
    expect(term.permissionState).toBe('deny')
    expect(term.verifyMessage).toBe('Nội bộ')
  })

  it('builds identity labels and preview classes', async () => {
    const wrapper = await mountMonitor()
    expect(wrapper.vm.buildIdentityLabel(0, '', '', '')).toBe('')
    expect(wrapper.vm.buildIdentityLabel(5, 'Nhân viên', 'E1', 'Nam')).toContain('#5')
    expect(wrapper.vm.buildIdentityLabel(0, '', '12', '')).toContain('Đối tượng')
    expect(wrapper.vm.buildIdentityLabel(0, 'Khách', '12', '')).toContain('Chưa rõ')
    const term = wrapper.vm.terminals[0]
    term.permissionState = 'allow'
    expect(wrapper.vm.previewStateClass(term)).toBe('state-allow')
    term.permissionState = 'deny'
    expect(wrapper.vm.previewStateClass(term)).toBe('state-deny')
    term.permissionState = 'idle'
    expect(wrapper.vm.previewStateClass(term)).toBe('state-idle')
  })

  it('computes status pill text and classes for all states', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    expect(wrapper.vm.statusPillText(term)).toBe('OFFLINE')
    expect(wrapper.vm.statusPillClass(term)).toBe('wait')
    term.previewRunning = true
    expect(wrapper.vm.statusPillText(term)).toBe('SẴN SÀNG')
    expect(wrapper.vm.statusPillClass(term)).toBe('neutral')
    term.permissionState = 'allow'
    expect(wrapper.vm.statusPillText(term)).toBe('CHO PHEP')
    expect(wrapper.vm.statusPillClass(term)).toBe('ok')
    term.permissionState = 'deny'
    expect(wrapper.vm.statusPillText(term)).toBe('TỪ CHỐI')
    expect(wrapper.vm.statusPillClass(term)).toBe('danger')
    term.permissionState = 'scanning'
    expect(wrapper.vm.statusPillText(term)).toBe('ĐANG QUÉT')
    term.permissionState = 'idle'
    term.continuousActive = true
    expect(wrapper.vm.statusPillText(term)).toBe('ĐANG CHẠY')
  })

  it('stops the scanner for every terminal on unmount', async () => {
    const wrapper = await mountMonitor()
    const term = wrapper.vm.terminals[0]
    Object.assign(term, { appliedGateId: 1, cameraId: 11, cameraVerified: true, cameraIp: 'rtsp://a', viewUrl: 'http://a/view' })
    await wrapper.vm.startScanner(term)
    wrapper.unmount()
    expect(term.previewRunning).toBe(false)
  })
})