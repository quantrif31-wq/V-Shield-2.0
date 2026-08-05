import { flushPromises, mount } from '@vue/test-utils'
import { reactive } from 'vue'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import Vehicles from '../Vehicles.vue'

const route = reactive({ query: {} })
const replace = vi.fn()
const getAll = vi.fn()

vi.mock('vue-router', () => ({ useRoute: () => route, useRouter: () => ({ replace }) }))
vi.mock('../../services/vehicleApi', () => ({
  getAll: (...args) => getAll(...args),
  getTypes: vi.fn(() => Promise.resolve({ data: [{ vehicleTypeId: 1, typeName: 'Ô tô' }] })),
  create: vi.fn(), update: vi.fn(), deleteVehicle: vi.fn(),
}))
vi.mock('../../services/employeeApi', () => ({
  getAll: vi.fn(() => Promise.resolve({ data: [{ employeeId: 8, fullName: 'Nguyễn Chủ Xe', departmentName: 'Vận hành', faceImageUrl: '' }] })),
  getProtectedFaceImage: vi.fn(),
}))

describe('Vehicles module', () => {
  beforeEach(() => {
    route.query = {}
    replace.mockReset()
    getAll.mockResolvedValue({ data: [{ vehicleId: 3, licensePlate: '51A-12345', vehicleTypeId: 1, vehicleTypeName: 'Ô tô', employeeId: 8, employeeFullName: 'Nguyễn Chủ Xe', description: 'Xe màu trắng' }] })
  })

  it('renders vehicle, owner and semantic type in the shared table', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: { RouterLink: true, ImportModal: true, ExportModal: true, Teleport: true } } })
    await flushPromises()
    expect(wrapper.text()).toContain('51A-12345')
    expect(wrapper.text()).toContain('Nguyễn Chủ Xe')
    expect(wrapper.find('table').exists()).toBe(true)
  })

  it('persists vehicle type filtering in the route query', async () => {
    const wrapper = mount(Vehicles, { global: { stubs: { RouterLink: true, ImportModal: true, ExportModal: true, Teleport: true } } })
    await flushPromises()
    await wrapper.get('#vehicle-type-filter').setValue('Ô tô')
    expect(replace).toHaveBeenCalledWith({ query: expect.objectContaining({ type: 'Ô tô' }) })
  })
})
