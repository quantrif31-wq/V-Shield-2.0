import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../http', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
    request: vi.fn(),
    defaults: { headers: { common: {} } },
  },
}))

const http = (await import('../http')).default
const accessLogApi = await import('../accessLogApi')
const deviceManagementApi = await import('../deviceManagementApi')
const campusMapApi = await import('../campusMapApi')
const lookupApi = await import('../lookupApi')
const guestProfileApi = await import('../guestProfileApi')
const exceptionReasonApi = await import('../exceptionReasonApi')

beforeEach(() => vi.clearAllMocks())

describe('accessLogApi', () => {
  it('covers access-log endpoints', () => {
    accessLogApi.getAccessLogs({ page: 1 })
    expect(http.get).toHaveBeenCalledWith('/access-logs', { params: { page: 1 } })
    accessLogApi.getAccessLogSummary()
    expect(http.get).toHaveBeenCalledWith('/access-logs/summary')
    accessLogApi.getAccessLogDetail(9)
    expect(http.get).toHaveBeenCalledWith('/access-logs/9')
    accessLogApi.getExceptions({ page: 1 })
    expect(http.get).toHaveBeenCalledWith('/access-logs/exceptions', { params: { page: 1 } })
    accessLogApi.getSystemAuditLogs({ page: 1 })
    expect(http.get).toHaveBeenCalledWith('/access-logs/system-audit', { params: { page: 1 } })
  })
})

describe('deviceManagementApi', () => {
  it('covers device and gate CRUD', () => {
    deviceManagementApi.getDeviceOverview()
    expect(http.get).toHaveBeenCalledWith('/device-management/overview')
    deviceManagementApi.getCameras({ gateId: 1 })
    expect(http.get).toHaveBeenCalledWith('/device-management/cameras', { params: { gateId: 1 } })
    deviceManagementApi.createCamera({ name: 'c' })
    expect(http.post).toHaveBeenCalledWith('/device-management/cameras', { name: 'c' })
    deviceManagementApi.updateCamera(1, { name: 'd' })
    expect(http.put).toHaveBeenCalledWith('/device-management/cameras/1', { name: 'd' })
    deviceManagementApi.deleteCamera(1)
    expect(http.delete).toHaveBeenCalledWith('/device-management/cameras/1')
    deviceManagementApi.getGates({ page: 1 })
    expect(http.get).toHaveBeenCalledWith('/device-management/gates', { params: { page: 1 } })
    deviceManagementApi.createGate({ name: 'g' })
    expect(http.post).toHaveBeenCalledWith('/device-management/gates', { name: 'g' })
    deviceManagementApi.updateGate(1, { name: 'h' })
    expect(http.put).toHaveBeenCalledWith('/device-management/gates/1', { name: 'h' })
    deviceManagementApi.deleteGate(1)
    expect(http.delete).toHaveBeenCalledWith('/device-management/gates/1')
  })
})

describe('campusMapApi', () => {
  it('covers campus map layout and 3d scene endpoints', () => {
    campusMapApi.getCampusMapLayout()
    expect(http.get).toHaveBeenCalledWith('/campus-map/layout')
    campusMapApi.saveCampusMapLayout({ layers: [] })
    expect(http.put).toHaveBeenCalledWith('/campus-map/layout', { layers: [] })
    campusMapApi.patchCampusMapLayout(3, { x: 1 })
    expect(http.patch).toHaveBeenCalledWith('/campus-map/layout/3', { x: 1 })
    campusMapApi.getCampusMapRealtime()
    expect(http.get).toHaveBeenCalledWith('/campus-map/realtime')
    campusMapApi.getCampusScene3D()
    expect(http.get).toHaveBeenCalledWith('/campus-map/scene3d')
    campusMapApi.createCampusSceneObject({ type: 'camera' })
    expect(http.post).toHaveBeenCalledWith('/campus-map/scene3d/objects', { type: 'camera' })
    campusMapApi.updateCampusSceneObject(5, { x: 1 })
    expect(http.patch).toHaveBeenCalledWith('/campus-map/scene3d/objects/5', { x: 1 })
    campusMapApi.deleteCampusSceneObject(5)
    expect(http.delete).toHaveBeenCalledWith('/campus-map/scene3d/objects/5')
  })
})

describe('lookupApi', () => {
  it('covers department CRUD', () => {
    lookupApi.getDepartments()
    expect(http.get).toHaveBeenCalledWith('/Departments')
    lookupApi.getDepartmentById(1)
    expect(http.get).toHaveBeenCalledWith('/Departments/1')
    lookupApi.createDepartment({ name: 'An Ninh' })
    expect(http.post).toHaveBeenCalledWith('/Departments', { name: 'An Ninh' })
    lookupApi.updateDepartment(1, { name: 'Bảo Vệ' })
    expect(http.put).toHaveBeenCalledWith('/Departments/1', { name: 'Bảo Vệ' })
    lookupApi.deleteDepartment(1)
    expect(http.delete).toHaveBeenCalledWith('/Departments/1')
  })

  it('covers position CRUD', () => {
    lookupApi.getPositions()
    expect(http.get).toHaveBeenCalledWith('/Positions')
    lookupApi.getPositionById(2)
    expect(http.get).toHaveBeenCalledWith('/Positions/2')
    lookupApi.createPosition({ name: 'Bảo vệ' })
    expect(http.post).toHaveBeenCalledWith('/Positions', { name: 'Bảo vệ' })
    lookupApi.updatePosition(2, { name: 'Lễ tân' })
    expect(http.put).toHaveBeenCalledWith('/Positions/2', { name: 'Lễ tân' })
    lookupApi.deletePosition(2)
    expect(http.delete).toHaveBeenCalledWith('/Positions/2')
  })
})

describe('guestProfileApi', () => {
  it('covers guest profile CRUD', () => {
    guestProfileApi.getGuestProfiles({ page: 1 })
    expect(http.get).toHaveBeenCalledWith('/guest-profiles', { params: { page: 1 } })
    guestProfileApi.getGuestProfileDetail(1)
    expect(http.get).toHaveBeenCalledWith('/guest-profiles/1')
    guestProfileApi.createGuestProfile({ fullName: 'K' })
    expect(http.post).toHaveBeenCalledWith('/guest-profiles', { fullName: 'K' })
    guestProfileApi.updateGuestProfile(1, { fullName: 'L' })
    expect(http.put).toHaveBeenCalledWith('/guest-profiles/1', { fullName: 'L' })
    guestProfileApi.deleteGuestProfile(1)
    expect(http.delete).toHaveBeenCalledWith('/guest-profiles/1')
  })

  it('covers visitor directory endpoints', () => {
    guestProfileApi.getVisitorDirectory({ q: 'x' })
    expect(http.get).toHaveBeenCalledWith('/guest-profiles/visitor-directory', { params: { q: 'x' } })
    guestProfileApi.updateVisitorDirectoryItem(2, { x: 1 })
    expect(http.put).toHaveBeenCalledWith('/guest-profiles/visitor-directory/2', { x: 1 })
    guestProfileApi.deleteVisitorDirectoryItem(2)
    expect(http.delete).toHaveBeenCalledWith('/guest-profiles/visitor-directory/2')
    guestProfileApi.getVisitorAccessLogs(2)
    expect(http.get).toHaveBeenCalledWith('/guest-profiles/visitor-directory/2/access-logs')
  })
})

describe('exceptionReasonApi', () => {
  it('covers exception reason CRUD', () => {
    exceptionReasonApi.getExceptionReasons()
    expect(http.get).toHaveBeenCalledWith('/exception-reasons')
    exceptionReasonApi.createExceptionReason({ name: 'r' })
    expect(http.post).toHaveBeenCalledWith('/exception-reasons', { name: 'r' })
    exceptionReasonApi.updateExceptionReason(1, { name: 's' })
    expect(http.put).toHaveBeenCalledWith('/exception-reasons/1', { name: 's' })
    exceptionReasonApi.deleteExceptionReason(1)
    expect(http.delete).toHaveBeenCalledWith('/exception-reasons/1')
  })
})
