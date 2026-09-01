import { flushPromises, mount } from '@vue/test-utils'
import { reactive } from 'vue'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import Vehicles from '../Vehicles.vue'

const route = reactive({ query: {} })
const replace = vi.fn()

const hoisted = vi.hoisted(() => ({
  getAll: vi.fn(),
  getTypes: vi.fn(() => Promise.resolve({ data: [{ vehicleTypeId: 1, typeName: 'Ô tô' }] })),
  create: vi.fn(),
  update: vi.fn(),
  deleteVehicle: vi.fn(),
  getAllEmployees: vi.fn(() => Promise.resolve({ data: [{ employeeId: 8, fullName: 'Nguyễn Chủ Xe', departmentName: 'Vận hành', faceImageUrl: '' }] })),
  getProtectedFaceImage: vi.fn(() => Promise.resolve({ data: 'blob' })),
}))

vi.mock('vue-router', () => ({ useRoute: () => route, useRouter: () => ({ replace }) }))
vi.mock('../../services/notificationApi', () => ({
  onEntityChanged: () => () => {},
}))
vi.mock('../../services/vehicleApi', () => ({
  getAll: (...args) => hoisted.getAll(...args),
  getTypes: (...args) => hoisted.getTypes(...args),
  create: (...args) => hoisted.create(...args),
  update: (...args) => hoisted.update(...args),
  deleteVehicle: (...args) => hoisted.deleteVehicle(...args),
}))
vi.mock('../../services/employeeApi', () => ({
  getAll: (...args) => hoisted.getAllEmployees(...args),
  getProtectedFaceImage: (...args) => hoisted.getProtectedFaceImage(...args),
}))

const vehicleApi = hoisted
const employeeApi = hoisted

const sharedStubs = { RouterLink: true, ImportModal: true, ExportModal: true, Teleport: true }

describe('Vehicles module', () => {
  beforeEach(() => {
    route.query = {}
    replace.mockReset()
    hoisted.getAll.mockReset()
    hoisted.getAll.mockResolvedValue({ data: [{ vehicleId: 3, licensePlate: '51A-12345', vehicleTypeId: 1, vehicleTypeName: 'Ô tô', employeeId: 8, employeeFullName: 'Nguyễn Chủ Xe', description: 'Xe màu trắng' }] })
    hoisted.getAllEmployees.mockReset()
    hoisted.getAllEmployees.mockResolvedValue({ data: [{ employeeId: 8, fullName: 'Nguyễn Chủ Xe', departmentName: 'Vận hành', faceImageUrl: '' }] })
    hoisted.getTypes.mockReset()
    hoisted.getTypes.mockResolvedValue({ data: [{ vehicleTypeId: 1, typeName: 'Ô tô' }] })
    hoisted.create.mockReset()
    hoisted.update.mockReset()
    hoisted.deleteVehicle.mockReset()
  })

  it('renders vehicle, owner and semantic type in the shared table', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(wrapper.text()).toContain('51A-12345')
    expect(wrapper.text()).toContain('Nguyễn Chủ Xe')
    expect(wrapper.find('table').exists()).toBe(true)
  })

  it('persists vehicle type filtering in the route query', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.get('#vehicle-type-filter').setValue('Ô tô')
    expect(replace).toHaveBeenCalledWith({ query: expect.objectContaining({ type: 'Ô tô' }) })
  })

  it('renders owner fallback avatar via getInitials', async () => {
    vehicleApi.getAll.mockResolvedValue({ data: [{ vehicleId: 3, licensePlate: '51A-12345', vehicleTypeId: 1, vehicleTypeName: 'Ô tô', employeeId: null, employeeFullName: '', description: '' }] })
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(wrapper.vm.getInitials('')).toBe('?')
  })

  it('fetchVehicles handles a 403 permission error', async () => {
    vehicleApi.getAll.mockRejectedValue({ response: { status: 403, data: { message: 'denied' } } })
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(wrapper.vm.permissionDenied).toBe(true)
  })

  it('fetchVehicles handles a generic error', async () => {
    vehicleApi.getAll.mockRejectedValue({ response: { data: { message: 'server error' } } })
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(wrapper.vm.loadError).toBe('server error')
  })

  it('fetchReferences shows error when lookups fail', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    vehicleApi.getAll.mockResolvedValue({ data: [] })
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    employeeApi.getTypes.mockRejectedValue(new Error('boom'))
    employeeApi.getAllEmployees.mockRejectedValue(new Error('boom'))
    await wrapper.vm.fetchReferences()
    await flushPromises()
  })

  it('getEmployeeFaceSrc returns http URLs directly', async () => {
    employeeApi.getAllEmployees.mockResolvedValue({ data: [{ employeeId: 5, fullName: 'NN', faceImageUrl: 'http://x/a.jpg' }] })
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(wrapper.vm.getEmployeeFaceSrc(5)).toBe('http://x/a.jpg')
  })

  it('saveVehicle aborts when the plate is invalid', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.form.licensePlate = 'not-a-plate'
    wrapper.vm.form.employeeId = 8
    await wrapper.vm.saveVehicle()
    expect(vehicleApi.create).not.toHaveBeenCalled()
  })

  it('saveVehicle aborts when no owner is selected', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.form.licensePlate = '51A-123.45'
    wrapper.vm.form.employeeId = null
    await wrapper.vm.saveVehicle()
    expect(vehicleApi.create).not.toHaveBeenCalled()
  })

  it('saveVehicle creates a new vehicle successfully', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.form.licensePlate = '51A-123.45'
    wrapper.vm.form.employeeId = 8
    vehicleApi.create.mockResolvedValue({})
    await wrapper.vm.saveVehicle()
    expect(vehicleApi.create).toHaveBeenCalledWith(expect.objectContaining({ licensePlate: expect.any(String), employeeId: 8 }))
  })

  it('saveVehicle errors when the vehicle type cannot be matched', async () => {
    employeeApi.getTypes.mockResolvedValue({ data: [] })
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.form.licensePlate = '51A-123.45'
    wrapper.vm.form.employeeId = 8
    await wrapper.vm.saveVehicle()
    expect(wrapper.vm.modalError).toBeTruthy()
  })

  it('saveVehicle updates an existing vehicle', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.openModal({ vehicleId: 3, licensePlate: '51A-12345', vehicleTypeId: 1, employeeId: 8, description: 'x' })
    await flushPromises()
    vehicleApi.update.mockResolvedValue({})
    await wrapper.vm.saveVehicle()
    expect(vehicleApi.update).toHaveBeenCalledWith(3, expect.objectContaining({ vehicleTypeId: 1 }))
  })

  it('saveVehicle surfaces API errors and keeps the form', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.form.licensePlate = '51A-123.45'
    wrapper.vm.form.employeeId = 8
    vehicleApi.create.mockRejectedValue({ response: { data: { title: 'duplicate' } } })
    await wrapper.vm.saveVehicle()
    expect(wrapper.vm.modalError).toBe('duplicate')
  })

  it('executeDelete removes a vehicle', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.requestDelete({ vehicleId: 3, licensePlate: '51A-12345' })
    vehicleApi.deleteVehicle.mockResolvedValue({})
    await wrapper.vm.executeDelete()
    expect(vehicleApi.deleteVehicle).toHaveBeenCalledWith(3)
    expect(wrapper.vm.deleteTarget).toBe(null)
  })

  it('executeDelete reports errors', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.requestDelete({ vehicleId: 3 })
    vehicleApi.deleteVehicle.mockRejectedValue({ response: { data: { message: 'busy' } } })
    await wrapper.vm.executeDelete()
    expect(wrapper.vm.deleteTarget).not.toBe(null)
  })

  it('onImportComplete shows success message', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.onImportComplete({ successCount: 5, errorCount: 0 })
    expect(wrapper.vm.showImportModal).toBe(false)
  })

  it('onImportComplete shows an error message when there are errors', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.onImportComplete({ successCount: 1, errorCount: 2 })
    expect(wrapper.vm.showImportModal).toBe(false)
  })

  it('clearFilters resets filters and commits', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.searchQuery = 'abc'
    wrapper.vm.filterType = 'Ô tô'
    wrapper.vm.clearFilters()
    expect(wrapper.vm.searchQuery).toBe('')
    expect(wrapper.vm.filterType).toBe('')
    expect(replace).toHaveBeenCalled()
  })

  it('debouncedCommitFilters commits after a delay', async () => {
    vi.useFakeTimers()
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.searchQuery = 'abc'
    wrapper.vm.debouncedCommitFilters()
    await vi.advanceTimersByTimeAsync(400)
    expect(replace).toHaveBeenCalled()
    vi.useRealTimers()
  })

  it('setPage updates the page query', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.setPage(2)
    expect(replace).toHaveBeenCalledWith({ query: expect.objectContaining({ page: 2 }) })
  })

  it('applyQuery reads filters from the route', async () => {
    route.query = { search: '51A', type: 'Ô tô', page: '2' }
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(wrapper.vm.searchQuery).toBe('51A')
    expect(wrapper.vm.filterType).toBe('Ô tô')
    expect(wrapper.vm.currentPage).toBe(2)
  })

  it('markAvatarBroken hides the avatar for an id', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    const evt = { target: { hidden: false } }
    wrapper.vm.markAvatarBroken(8, evt)
    expect(evt.target.hidden).toBe(true)
    expect(wrapper.vm.getEmployeeFaceSrc(8)).toBe('')
  })

  it('beforeUnload sets returnValue when the form is dirty', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.openModal()
    wrapper.vm.form.licensePlate = '51A-12345'
    wrapper.vm.formBaseline = wrapper.vm.formState
    wrapper.vm.form.licensePlate = '51A-99999'
    const event = { preventDefault: vi.fn() }
    Object.defineProperty(event, 'returnValue', { writable: true, value: '' })
    wrapper.vm.beforeUnload(event)
    expect(event.preventDefault).toHaveBeenCalled()
  })

  it('beforeUnload does nothing when form is clean', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    const event = { preventDefault: vi.fn() }
    wrapper.vm.beforeUnload(event)
    expect(event.preventDefault).not.toHaveBeenCalled()
  })

  it('resolveOwner matches an owner by exact option string', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.ownerSearchQuery = 'Nguyễn Chủ Xe · ID 8'
    wrapper.vm.resolveOwner()
    expect(wrapper.vm.form.employeeId).toBe(8)
  })

  it('resolveOwner leaves employeeId null when there is no match', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.ownerSearchQuery = 'Không tồn tại'
    wrapper.vm.resolveOwner()
    expect(wrapper.vm.form.employeeId).toBe(null)
  })

  it('requestCloseModal opens the discard dialog when dirty', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.openModal()
    wrapper.vm.form.licensePlate = '51A-12345'
    wrapper.vm.formBaseline = wrapper.vm.formState
    wrapper.vm.form.licensePlate = '51A-99999'
    wrapper.vm.requestCloseModal()
    expect(wrapper.vm.showDiscardDialog).toBe(true)
  })

  it('closeModal(true) force closes the modal', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.openModal()
    wrapper.vm.closeModal(true)
    expect(wrapper.vm.showModal).toBe(false)
    expect(wrapper.vm.editingVehicle).toBe(null)
  })

  it('onPlateInput handles an empty value', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.form.licensePlate = '   '
    wrapper.vm.onPlateInput()
    expect(wrapper.vm.plateValidation.isValid).toBe(false)
  })

  it('hydrateProtectedAvatars builds object URLs for protected images', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: sharedStubs } })
    await flushPromises()
    employeeApi.getAllEmployees.mockResolvedValue({ data: [{ employeeId: 9, fullName: 'N', faceImageUrl: '/protected/9' }] })
    const urlSpy = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:url')
    const revokeSpy = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {})
    await wrapper.vm.fetchReferences()
    expect(urlSpy).toHaveBeenCalled()
    wrapper.vm.releaseProtectedAvatars()
    expect(revokeSpy).toHaveBeenCalled()
    urlSpy.mockRestore()
    revokeSpy.mockRestore()
  })
})
