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
})