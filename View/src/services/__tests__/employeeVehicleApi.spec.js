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
const employeeApi = await import('../employeeApi')
const vehicleApi = await import('../vehicleApi')
const vehicleDelegationApi = await import('../vehicleDelegationApi')

beforeEach(() => vi.clearAllMocks())

describe('employeeApi', () => {
  it('covers CRUD and profile endpoints', () => {
    employeeApi.getAll({ page: 1 })
    expect(http.get).toHaveBeenCalledWith('/Employees', { params: { page: 1 } })
    employeeApi.getById(5)
    expect(http.get).toHaveBeenCalledWith('/Employees/5')
    employeeApi.getMyProfile()
    expect(http.get).toHaveBeenCalledWith('/Employees/me')
    employeeApi.create({ name: 'A' })
    expect(http.post).toHaveBeenCalledWith('/Employees', { name: 'A' })
    employeeApi.update(5, { name: 'B' })
    expect(http.put).toHaveBeenCalledWith('/Employees/5', { name: 'B' })
    employeeApi.deleteEmployee(5)
    expect(http.delete).toHaveBeenCalledWith('/Employees/5')
  })

  it('uploads a face as multipart and reads protected images as blobs', () => {
    employeeApi.uploadFace(5, new Blob(['x']))
    expect(http.post).toHaveBeenCalledWith('/Employees/5/face', expect.any(FormData), {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    employeeApi.getProtectedFaceImage(5)
    expect(http.get).toHaveBeenCalledWith('/Employees/5/face-image', { responseType: 'blob' })
  })
})

describe('vehicleApi', () => {
  it('covers CRUD and lookup endpoints', () => {
    vehicleApi.getAll()
    expect(http.get).toHaveBeenCalledWith('/Vehicles')
    vehicleApi.getTypes()
    expect(http.get).toHaveBeenCalledWith('/Vehicles/types')
    vehicleApi.getById(3)
    expect(http.get).toHaveBeenCalledWith('/Vehicles/3')
    vehicleApi.getByLicensePlate('29A-123.45')
    expect(http.get).toHaveBeenCalledWith('/Vehicles/license-plate/29A-123.45')
    vehicleApi.getByEmployeeId(9)
    expect(http.get).toHaveBeenCalledWith('/Vehicles/employee/9')
    vehicleApi.create({ plate: 'x' })
    expect(http.post).toHaveBeenCalledWith('/Vehicles', { plate: 'x' })
    vehicleApi.update(3, { plate: 'y' })
    expect(http.put).toHaveBeenCalledWith('/Vehicles/3', { plate: 'y' })
    vehicleApi.deleteVehicle(3)
    expect(http.delete).toHaveBeenCalledWith('/Vehicles/3')
  })
})

describe('vehicleDelegationApi', () => {
  it('covers delegation lifecycle endpoints', () => {
    vehicleDelegationApi.createDelegation({ to: 1 })
    expect(http.post).toHaveBeenCalledWith('/vehicle-delegations', { to: 1 })
    vehicleDelegationApi.getOutgoing()
    expect(http.get).toHaveBeenCalledWith('/vehicle-delegations/outgoing')
    vehicleDelegationApi.getIncoming()
    expect(http.get).toHaveBeenCalledWith('/vehicle-delegations/incoming')
    vehicleDelegationApi.getAllDelegations()
    expect(http.get).toHaveBeenCalledWith('/vehicle-delegations')
    vehicleDelegationApi.approveDelegation(1)
    expect(http.patch).toHaveBeenCalledWith('/vehicle-delegations/1/approve')
    vehicleDelegationApi.rejectDelegation(1, { reason: 'no' })
    expect(http.patch).toHaveBeenCalledWith('/vehicle-delegations/1/reject', { reason: 'no' })
    vehicleDelegationApi.revokeDelegation(1)
    expect(http.patch).toHaveBeenCalledWith('/vehicle-delegations/1/revoke')
  })
})
