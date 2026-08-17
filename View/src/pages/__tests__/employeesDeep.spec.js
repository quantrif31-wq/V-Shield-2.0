import { flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({
  route: { query: {} },
  router: { replace: vi.fn() },
}))

vi.mock('vue-router', () => ({ useRoute: () => hoisted.route, useRouter: () => hoisted.router }))
vi.mock('../../services/employeeApi', () => ({
  getAll: vi.fn(),
  create: vi.fn(),
  update: vi.fn(),
  deleteEmployee: vi.fn(),
  uploadFace: vi.fn(),
  getProtectedFaceImage: vi.fn(),
}))
vi.mock('../../services/lookupApi', () => ({
  getDepartments: vi.fn(() => Promise.resolve({ data: [{ departmentId: 1, name: 'An Ninh' }] })),
  getPositions: vi.fn(() => Promise.resolve({ data: [{ positionId: 1, name: 'Bảo vệ' }] })),
}))
vi.mock('../../services/statisticsApi', () => ({ getSummary: vi.fn(() => Promise.resolve({ data: {} })) }))

const employeeApi = await import('../../services/employeeApi')
const Employees = (await import('../Employees.vue')).default

const sharedStubs = { ImportModal: true, ExportModal: true }

beforeEach(() => {
  vi.clearAllMocks()
  hoisted.route.query = {}
  employeeApi.getAll.mockResolvedValue({ data: [{ employeeId: 1, fullName: 'Nguyễn Văn An', status: true }] })
  employeeApi.getProtectedFaceImage.mockResolvedValue({ data: new Blob(['x']) })
})
afterEach(() => {
  document.body.innerHTML = ''
})

describe('Employees create', () => {
  it('creates an employee through the modal', async () => {
    employeeApi.create.mockResolvedValue({ data: { employeeId: 2 } })
    const wrapper = mount(Employees, { global: { stubs: { ...sharedStubs, RouterLink: { template: '<a><slot /></a>' } } } })
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text().includes('Thêm nhân viên')).trigger('click')
    await nextTick()
    const nameInput = document.body.querySelector('#employee-name')
    nameInput.value = 'Trần Thị Bích'
    nameInput.dispatchEvent(new Event('input'))
    await nextTick()
    document.body.querySelector('#employee-form').dispatchEvent(new Event('submit'))
    await flushPromises()
    expect(employeeApi.create).toHaveBeenCalledWith(expect.objectContaining({ fullName: 'Trần Thị Bích' }))
  })
})
