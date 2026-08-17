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
const DepartmentPosition = (await import('../DepartmentPosition.vue')).default

const sharedStubs = { ImportModal: true, ExportModal: true }

beforeEach(() => {
  vi.clearAllMocks()
  lookupApi.getDepartments.mockResolvedValue({ data: [] })
  lookupApi.getPositions.mockResolvedValue({ data: [] })
})

describe('DepartmentPosition create', () => {
  it('creates a department through the modal', async () => {
    const wrapper = mount(DepartmentPosition, {
      global: { stubs: { ...sharedStubs, RouterLink: { template: '<a><slot /></a>' } } },
    })
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.classes().includes('rounded-btn')).trigger('click')
    await wrapper.find('.modern-modal input').setValue('Phòng Nhân sự')
    lookupApi.createDepartment.mockResolvedValue({})
    await wrapper.find('.modern-modal form').trigger('submit')
    await flushPromises()
    expect(lookupApi.createDepartment).toHaveBeenCalledWith(expect.objectContaining({ name: 'Phòng Nhân sự' }))
  })
})
