import { flushPromises, mount } from '@vue/test-utils'
import { reactive } from 'vue'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import Employees from '../Employees.vue'

const route = reactive({ query: {} })
const replace = vi.fn()
const getAll = vi.fn()

vi.mock('vue-router', () => ({ useRoute: () => route, useRouter: () => ({ replace }) }))
vi.mock('../../services/employeeApi', () => ({
  getAll: (...args) => getAll(...args), create: vi.fn(), update: vi.fn(), deleteEmployee: vi.fn(), uploadFace: vi.fn(), getProtectedFaceImage: vi.fn(),
}))
vi.mock('../../services/lookupApi', () => ({ getDepartments: vi.fn(() => Promise.resolve({ data: [] })), getPositions: vi.fn(() => Promise.resolve({ data: [] })) }))
vi.mock('../../services/statisticsApi', () => ({ getSummary: vi.fn(() => Promise.resolve({ totalEmployees: 1, activeEmployees: 1, inactiveEmployees: 0 })) }))

describe('Employees module', () => {
  beforeEach(() => {
    route.query = {}
    replace.mockReset()
    getAll.mockResolvedValue({ data: [{ employeeId: 7, fullName: 'Nguyễn Văn An', phone: '0900000000', email: 'an@example.com', departmentName: 'An ninh', positionName: 'Nhân viên', status: true, faceImageUrl: '' }] })
  })

  it('renders API data through the shared table and semantic status', async () => {
    const wrapper = mount(Employees, { global: { stubs: { RouterLink: true, ImportModal: true, ExportModal: true } } })
    await flushPromises()
    expect(getAll).toHaveBeenCalledOnce()
    expect(wrapper.text()).toContain('Nguyễn Văn An')
    expect(wrapper.text()).toContain('Hoạt động')
    expect(wrapper.find('table').exists()).toBe(true)
  })

  it('persists status filtering in the route query', async () => {
    const wrapper = mount(Employees, { global: { stubs: { RouterLink: true, ImportModal: true, ExportModal: true } } })
    await flushPromises()
    await wrapper.get('#employee-status').setValue('false')
    expect(replace).toHaveBeenCalledWith({ query: expect.objectContaining({ status: 'false' }) })
  })
})
