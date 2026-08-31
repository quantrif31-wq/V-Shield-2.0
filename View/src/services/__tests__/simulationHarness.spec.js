import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../http', () => ({ default: { get: vi.fn(), post: vi.fn() } }))

let http
let sim

const makeComponent = () => ({
  lanes: [
    { id: 'lane1', plate: {}, qr: {}, auto: {}, plateApi: {} },
    { id: 'lane2', plate: {}, qr: {}, auto: {}, plateApi: {} },
  ],
  clearQrState: vi.fn(),
  clearPlateState: vi.fn(),
  applyPlateRealtimeState: vi.fn(),
  startAutoMonitor: vi.fn(),
})

beforeEach(async () => {
  vi.resetModules()
  vi.clearAllMocks()
  http = (await import('../http')).default
  http.get.mockResolvedValue({ data: [] })
  http.post.mockResolvedValue({ data: { data: {} } })
  sim = await import('../simulationHarness')
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('simulationHarness', () => {
  it('isSimMode returns false in test env', () => {
    expect(sim.isSimMode()).toBe(false)
  })

  it('installSimulation returns early without a component', () => {
    expect(sim.installSimulation(null)).toBe(null)
    expect(sim.installSimulation(undefined)).toBe(undefined)
  })

  it('installs sim state and api overrides', () => {
    const component = makeComponent()
    const result = sim.installSimulation(component)
    expect(result).toBe(component)
    expect(component.__sim).toBeDefined()
    expect(component.simState.expanded).toBe(true)
    expect(component.simEnable).toBe(true)
    expect(typeof component.getLaneQrScanResult).toBe('function')
    expect(typeof component.startLaneQrScanner).toBe('function')
    expect(typeof component.simGenerateQr).toBe('function')
    expect(typeof component.simToggleQr).toBe('function')
    expect(typeof component.simSetPlate).toBe('function')
  })

  it('replaces the plate api with sim implementations', async () => {
    const component = makeComponent()
    sim.installSimulation(component)
    const api = component.lanes[0].plateApi
    expect(await api.turnOnCamera()).toMatchObject({ success: true })
    expect(await api.turnOffCamera()).toMatchObject({ success: true })
    expect(await api.resetCameraState()).toMatchObject({ success: true })
    expect(await api.getCameraStatus()).toMatchObject({ ip: 'sim-camera' })
    expect(await api.getLockedImages()).toMatchObject({ scan_locked: true })
  })

  it('simGetLaneQrScanResult returns locked sim payload for lane1', async () => {
    const component = makeComponent()
    sim.installSimulation(component)
    component.__sim.empPayload = 'EMP:x'
    component.simToggleQr('lane1', true)
    const res = await component.getLaneQrScanResult(component.lanes[0])
    expect(res.phase).toBe('locked')
    expect(res.candidate_source).toBe('sim')
  })

  it('simGetLaneQrScanResult returns idle when no payload', async () => {
    const component = makeComponent()
    sim.installSimulation(component)
    const res = await component.getLaneQrScanResult(component.lanes[1])
    expect(res.phase).toBe('idle')
    expect(res.locked_payload).toBe('')
  })

  it('simRefreshPlate pushes realtime state through the component', async () => {
    const component = makeComponent()
    sim.installSimulation(component)
    component.__sim.plateLane = 'lane1'
    component.__sim.plateValue = '59K-12345'
    await component.refreshPlate(component.lanes[0])
    expect(component.applyPlateRealtimeState).toHaveBeenCalledWith(
      component.lanes[0],
      expect.objectContaining({ confirmed_plate: '59K-12345', scan_locked: true }),
      true
    )
  })

  it('simGenerateQr posts to the api and stores payload', async () => {
    const component = makeComponent()
    sim.installSimulation(component)
    http.post.mockResolvedValue({ data: { data: { qrPayload: 'EMP:abc', employeeId: 7, employeeName: 'Nguyen A' } } })
    await component.simGenerateQr()
    expect(http.post).toHaveBeenCalledWith('/dynamic-qr/my')
    expect(component.__sim.empPayload).toBe('EMP:abc')
    expect(component.__sim.empEmployeeId).toBe(7)
  })

  it('simGenerateQr handles failure responses', async () => {
    const component = makeComponent()
    sim.installSimulation(component)
    http.post.mockResolvedValue({ data: { message: 'no qr' } })
    await component.simGenerateQr()
    expect(component.__sim.empPayload).toBe('')
  })

  it('simToggleQr on lane2 uses the qrLane2 slot', async () => {
    const component = makeComponent()
    sim.installSimulation(component)
    component.__sim.empPayload = 'EMP:x'
    await component.simToggleQr('lane2', true)
    expect(component.__sim.qrLane2).toBe('EMP:x')
    expect(component.simState.qr2).toBe(true)
    await component.simToggleQr('lane2', false)
    expect(component.__sim.qrLane2).toBe('')
  })

  it('simSetPlate attaches a plate and logs', async () => {
    const component = makeComponent()
    sim.installSimulation(component)
    component.simSetPlate('lane1', '30A-1234')
    expect(component.__sim.plateLane).toBe('lane1')
    expect(component.__sim.plateValue).toBe('30A-1234')
    expect(component.simState.injectPlate).toBe('30A-1234')
  })

  it('simUseForeign loads foreign plates when empty', async () => {
    const component = makeComponent()
    sim.installSimulation(component)
    http.get.mockImplementation((url) => {
      if (url === '/vehicles') return Promise.resolve({ data: [{ licensePlate: '30B-9999' }] })
      return Promise.resolve({ data: { data: [{ EmployeeId: 8, ParkingStatus: 'IN' }] } })
    })
    await component.simUseForeign()
    expect(component.__sim.foreignPlate).toBe('30B-9999')
    expect(component.simState.injectPlate).toBe('30B-9999')
  })

  it('simResetAll clears lane state', async () => {
    const component = makeComponent()
    sim.installSimulation(component)
    component.__sim.qrLane1 = 'EMP:x'
    component.__sim.plateValue = '30A-1'
    component.simResetAll()
    expect(component.__sim.qrLane1).toBe('')
    expect(component.__sim.plateValue).toBe('')
    expect(component.clearQrState).toHaveBeenCalled()
    expect(component.clearPlateState).toHaveBeenCalled()
  })

  it('simSyncLaneConfig maps gate and lane ids', async () => {
    const component = makeComponent()
    http.get.mockImplementation((url) => {
      if (url === '/gate-transit/gates') return Promise.resolve({ data: { data: [{ GateId: 99, Name: 'Cong' }] } })
      if (url === '/enterprise/visitor-vehicle/lane-health') return Promise.resolve({ data: [{ Direction: 'entry', LaneId: 777 }, { Direction: 'exit', LaneId: 888 }] })
      return Promise.resolve({ data: [] })
    })
    sim.installSimulation(component)
    await component.simSyncLaneConfig()
    expect(component.simState.laneSynced).toBe(true)
    expect(component.lanes[0].laneId).toBe(777)
    expect(component.lanes[1].laneId).toBe(888)
  })

  it('isSimMode returns true when simulate param is present', () => {
    const url = new URL(window.location.href)
    url.searchParams.set('simulate', '1')
    window.history.replaceState({}, '', url.toString())
    expect(sim.isSimMode()).toBe(true)
    url.searchParams.delete('simulate')
    window.history.replaceState({}, '', url.toString())
  })

  it('isSimMode returns true from localStorage when key equals 1', () => {
    window.localStorage.setItem('vshield_sim', '1')
    expect(sim.isSimMode()).toBe(true)
    window.localStorage.removeItem('vshield_sim')
  })

  it('installSimulation returns the component when already installed', () => {
    const component = makeComponent()
    sim.installSimulation(component)
    const second = sim.installSimulation(component)
    expect(second).toBe(component)
  })

  it('simClearPlate clears the plate value for a lane', () => {
    const component = makeComponent()
    sim.installSimulation(component)
    component.__sim.plateValue = '30A-1234'
    component.simClearPlate('lane2')
    expect(component.__sim.plateValue).toBe('')
    expect(component.__sim.plateLane).toBe('lane2')
  })

  it('simSetTargetLane updates the keyword', () => {
    const component = makeComponent()
    sim.installSimulation(component)
    component.simSetTargetLane('lane2')
    expect(component.simState.targetLane).toBe('lane2')
  })

  it('simMakeAllowPlate generates and injects an allow plate', () => {
    const component = makeComponent()
    sim.installSimulation(component)
    component.simMakeAllowPlate()
    expect(component.simState.injectPlate).toMatch(/^59K-\d{5}$/)
    expect(component.__sim.plateValue).toMatch(/^59K-\d{5}$/)
  })

  it('simSetPlate logs and returns early when plate is empty', () => {
    const component = makeComponent()
    sim.installSimulation(component)
    component.simSetPlate('lane1', '')
    expect(component.__sim.plateValue).toBe('')
  })

  it('simRefreshForeign warns when no foreign vehicle is found', async () => {
    const component = makeComponent()
    sim.installSimulation(component)
    http.get.mockImplementation((url) => {
      if (url === '/vehicles') return Promise.resolve({ data: [] })
      return Promise.resolve({ data: {} })
    })
    await component.simRefreshForeign()
    expect(component.__sim.foreignPlate).toBe('')
  })

  it('simRefreshForeign falls back to a non-IN vehicle', async () => {
    const component = makeComponent()
    sim.installSimulation(component)
    component.__sim.empEmployeeId = 3
    http.get.mockImplementation((url) => {
      if (url === '/vehicles') return Promise.resolve({ data: [{ licensePlate: '30C-1111' }, { licensePlate: '30D-2222' }] })
      return Promise.resolve({ data: { data: [{ EmployeeId: 8, ParkingStatus: 'OUT' }] } })
    })
    await component.simRefreshForeign()
    expect(component.__sim.foreignPlate).toBe('30C-1111')
    expect(component.__sim.foreignOwner).toBe(8)
  })

  it('simRunScenario runs an ALLOW scenario', async () => {
    const component = makeComponent()
    sim.installSimulation(component)
    component.autoActive = true
    component.__sim.empPayload = 'EMP:x'
    await component.simRunScenario('lane1', 'allow')
    expect(component.__sim.plateValue).toMatch(/^59K-\d{5}$/)
    expect(component.simState.qr1).toBe(true)
  })

  it('simRunScenario runs a DENY scenario', async () => {
    const component = makeComponent()
    sim.installSimulation(component)
    component.autoActive = true
    component.__sim.empPayload = 'EMP:x'
    component.__sim.foreignPlate = '30B-9999'
    component.__sim.foreignOwner = 9
    await component.simRunScenario('lane2', 'deny')
    expect(component.__sim.plateValue).toBe('30B-9999')
  })

  it('simRunScenario bails out when no payload can be generated', async () => {
    const component = makeComponent()
    sim.installSimulation(component)
    component.autoActive = true
    http.post.mockResolvedValue({ data: { message: 'no qr' } })
    await component.simRunScenario('lane2', 'deny')
    expect(component.simState.qr2).toBe(false)
  })

  it('simUseForeign does nothing when foreign plate is still absent', async () => {
    const component = makeComponent()
    sim.installSimulation(component)
    http.get.mockResolvedValue({ data: [] })
    await component.simUseForeign()
    expect(component.simState.injectPlate).toBe('59K-12345')
  })

  it('simRefreshPlate handles an idle lane without a plate', async () => {
    const component = makeComponent()
    sim.installSimulation(component)
    component.__sim.plateLane = 'lane2'
    component.__sim.plateValue = ''
    await component.refreshPlate(component.lanes[0])
    expect(component.applyPlateRealtimeState).toHaveBeenCalledWith(
      component.lanes[0],
      expect.objectContaining({ confirmed_plate: '', scan_locked: false }),
      true
    )
  })

  it('simRefreshPlate alerts warn on realtime apply errors', async () => {
    const component = makeComponent()
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {})
    sim.installSimulation(component)
    component.applyPlateRealtimeState.mockRejectedValueOnce(new Error('boom'))
    component.__sim.plateLane = 'lane1'
    component.__sim.plateValue = '30A-1234'
    await component.refreshPlate(component.lanes[0])
    expect(warn).toHaveBeenCalled()
    warn.mockRestore()
  })

  it('simRunScenario starts auto-monitor when not yet active', async () => {
    const component = makeComponent()
    sim.installSimulation(component)
    component.autoActive = false
    component.__sim.empPayload = 'EMP:x'
    await component.simRunScenario('lane1', 'allow')
    expect(component.startAutoMonitor).toHaveBeenCalled()
    expect(component.__sim.plateValue).toMatch(/^59K-\d{5}$/)
  })

  it('simSyncLaneConfig logs an error when the api rejects', async () => {
    const component = makeComponent()
    http.get.mockRejectedValue(new Error('network down'))
    sim.installSimulation(component)
    await component.simSyncLaneConfig()
    expect(component.simState.laneSynced).toBe(false)
  })

  it('calls the one-shot lane scanner api stubs', async () => {
    const component = makeComponent()
    sim.installSimulation(component)
    await expect(component.startLaneQrScanner()).resolves.toMatchObject({ success: true })
    await expect(component.scanLaneQrOnce()).resolves.toMatchObject({ success: true })
    await expect(component.resetLaneQrSession()).resolves.toMatchObject({ success: true })
    await expect(component.stopLaneQrScanner()).resolves.toMatchObject({ success: true })
  })

  it('isSimMode returns false when window is unavailable', () => {
    const originalWindow = globalThis.window
    vi.stubGlobal('window', undefined)
    expect(sim.isSimMode()).toBe(false)
    vi.stubGlobal('window', originalWindow)
  })

  it('simGenerateQr logs an error when the api rejects', async () => {
    const component = makeComponent()
    const logSpy = vi.spyOn(console, 'log').mockImplementation(() => {})
    http.post.mockRejectedValueOnce(new Error('boom'))
    sim.installSimulation(component)
    await component.simGenerateQr()
    expect(logSpy).toHaveBeenCalled()
    logSpy.mockRestore()
  })

  it('simToggleQr generates a payload on demand before enabling', async () => {
    const component = makeComponent()
    http.post.mockResolvedValue({ data: { data: { qrPayload: 'EMP:new' } } })
    sim.installSimulation(component)
    await component.simToggleQr('lane1', true)
    expect(component.__sim.qrLane1).toBe('EMP:new')
    expect(component.simState.qr1).toBe(true)
  })

  it('simSetPlate logs when reassigning to a different lane', () => {
    const component = makeComponent()
    sim.installSimulation(component)
    component.simSetPlate('lane1', '30A-1111')
    component.simSetPlate('lane2', '30B-2222')
    expect(component.__sim.plateLane).toBe('lane2')
    expect(component.__sim.plateValue).toBe('30B-2222')
  })

  it('simRefreshForeign picks an IN vehicle owned by another employee', async () => {
    const component = makeComponent()
    http.get.mockImplementation((url) => {
      if (url === '/vehicles') return Promise.resolve({ data: [{ licensePlate: '30E-1234' }] })
      return Promise.resolve({ data: { data: [{ EmployeeId: 0, ParkingStatus: 'IN' }, { EmployeeId: 8, ParkingStatus: 'IN' }] } })
    })
    sim.installSimulation(component)
    component.__sim.empEmployeeId = 3
    await component.simRefreshForeign()
    expect(component.__sim.foreignPlate).toBe('30E-1234')
    expect(component.__sim.foreignOwner).toBe(8)
  })

  it('simRefreshForeign logs an error when the api rejects', async () => {
    const component = makeComponent()
    const logSpy = vi.spyOn(console, 'log').mockImplementation(() => {})
    sim.installSimulation(component)
    http.get.mockRejectedValue(new Error('down'))
    await component.simRefreshForeign()
    expect(logSpy).toHaveBeenCalled()
    logSpy.mockRestore()
  })

  it('simRunScenario bails out on deny when no foreign plate exists', async () => {
    const component = makeComponent()
    component.autoActive = true
    http.get.mockResolvedValue({ data: [] })
    sim.installSimulation(component)
    component.__sim.empPayload = 'EMP:x'
    await component.simRunScenario('lane2', 'deny')
    expect(component.__sim.plateValue).toBe('')
  })

  it('simSyncLaneConfig skips lanes not present in the component', async () => {
    const component = makeComponent()
    component.lanes = [{ id: 'custom1', plate: {}, qr: {} }, { id: 'custom2', plate: {}, qr: {} }]
    http.get.mockImplementation((url) => {
      if (url === '/gate-transit/gates') return Promise.resolve({ data: { data: [] } })
      if (url === '/enterprise/visitor-vehicle/lane-health') return Promise.resolve({ data: [{ Direction: 'entry', LaneId: 777 }] })
      return Promise.resolve({ data: [] })
    })
    sim.installSimulation(component)
    await component.simSyncLaneConfig()
    expect(component.simState.laneSynced).toBe(true)
  })

  it('simSyncLaneConfig unwraps alternative gates payload shapes', async () => {
    const component = makeComponent()
    http.get.mockImplementation((url) => {
      if (url === '/gate-transit/gates') return Promise.resolve({ data: [{ GateId: 1 }] })
      if (url === '/enterprise/visitor-vehicle/lane-health') return Promise.resolve({ data: { items: [{ Direction: 'entry', LaneId: 5 }, { Direction: 'exit', LaneId: 6 }] } })
      return Promise.resolve({ data: [] })
    })
    sim.installSimulation(component)
    await component.simSyncLaneConfig()
    expect(component.simState.laneSynced).toBe(true)
    expect(component.lanes[0].laneId).toBe(5)
    expect(component.lanes[1].laneId).toBe(6)
  })
})