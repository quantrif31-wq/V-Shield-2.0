import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/attendanceApi', () => ({
  getShifts: vi.fn(),
  createShift: vi.fn(),
  updateShift: vi.fn(),
  deactivateShift: vi.fn(),
  attendanceStatusLabelMap: {},
  leaveTypeLabelMap: {},
}))

const attendanceApi = await import('../../services/attendanceApi')
const AttendanceShifts = (await import('../AttendanceShifts.vue')).default

const sharedStubs = { ImportModal: true, ExportModal: true }

beforeEach(() => {
  vi.clearAllMocks()
  attendanceApi.getShifts.mockResolvedValue({ data: [] })
})

describe('AttendanceShifts', () => {
  it('loads shifts on mount', async () => {
    const wrapper = mount(AttendanceShifts, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(attendanceApi.getShifts).toHaveBeenCalled()
  })

  it('creates a shift through the modal', async () => {
    const wrapper = mount(AttendanceShifts, { global: { stubs: sharedStubs } })
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text().includes('Thêm ca') || b.text().includes('Thêm ca làm')).trigger('click')
    const inputs = wrapper.findAll('form input')
    await inputs[0].setValue('Ca sáng')
    await inputs[1].setValue('08:00')
    await inputs[2].setValue('16:00')
    attendanceApi.createShift.mockResolvedValue({})
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(attendanceApi.createShift).toHaveBeenCalledWith(expect.objectContaining({ shiftName: 'Ca sáng' }))
  })
})
