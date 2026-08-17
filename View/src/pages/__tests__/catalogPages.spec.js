import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/lookupApi', () => ({
  getDepartments: vi.fn(),
  createDepartment: vi.fn(),
  updateDepartment: vi.fn(),
  deleteDepartment: vi.fn(),
  getPositions: vi.fn(),
  createPosition: vi.fn(),
  updatePosition: vi.fn(),
  deletePosition: vi.fn(),
}))
vi.mock('../../services/exceptionReasonApi', () => ({
  getExceptionReasons: vi.fn(),
  createExceptionReason: vi.fn(),
  updateExceptionReason: vi.fn(),
  deleteExceptionReason: vi.fn(),
}))

const lookupApi = await import('../../services/lookupApi')
const exceptionReasonApi = await import('../../services/exceptionReasonApi')
const SystemCatalog = (await import('../SystemCatalog.vue')).default
const DepartmentPosition = (await import('../DepartmentPosition.vue')).default

const sharedStubs = { ImportModal: true, ExportModal: true }

beforeEach(() => {
  vi.clearAllMocks()
  lookupApi.getDepartments.mockResolvedValue({ data: [] })
  lookupApi.getPositions.mockResolvedValue({ data: [] })
  exceptionReasonApi.getExceptionReasons.mockResolvedValue({ data: [] })
})

describe('SystemCatalog', () => {
  it('loads departments and positions', async () => {
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(lookupApi.getDepartments).toHaveBeenCalled()
    expect(lookupApi.getPositions).toHaveBeenCalled()
  })
})

describe('DepartmentPosition', () => {
  it('loads departments and positions', async () => {
    const wrapper = mount(DepartmentPosition, {
      global: { stubs: { ...sharedStubs, RouterLink: { template: '<a><slot /></a>' } } },
    })
    await flushPromises()
    expect(lookupApi.getDepartments).toHaveBeenCalled()
    expect(lookupApi.getPositions).toHaveBeenCalled()
  })
})
