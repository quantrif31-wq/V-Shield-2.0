import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/employeeApi', () => ({ getAll: vi.fn() }))
vi.mock('../../services/lookupApi', () => ({ getDepartments: vi.fn() }))
vi.mock('../../services/attendanceApi', () => ({
  getWorkSchedules: vi.fn(),
  createWorkSchedule: vi.fn(),
  updateWorkSchedule: vi.fn(),
  cancelWorkSchedule: vi.fn(),
  getShifts: vi.fn(),
  attendanceStatusLabelMap: {},
  leaveTypeLabelMap: {},
}))

const employeeApi = await import('../../services/employeeApi')
const lookupApi = await import('../../services/lookupApi')
const attendanceApi = await import('../../services/attendanceApi')
const AttendanceWorkSchedules = (await import('../AttendanceWorkSchedules.vue')).default

const sharedStubs = { ImportModal: true, ExportModal: true }

beforeEach(() => {
  vi.clearAllMocks()
  employeeApi.getAll.mockResolvedValue({ data: [{ employeeId: 7, fullName: 'Nguyễn An' }] })
  lookupApi.getDepartments.mockResolvedValue({ data: [] })
  attendanceApi.getWorkSchedules.mockResolvedValue({ data: { items: [] } })
  attendanceApi.getShifts.mockResolvedValue({ data: [{ shiftId: 1, shiftName: 'Ca sáng', startTime: '08:00', endTime: '16:00' }] })
})

describe('AttendanceWorkSchedules create', () => {
  it('creates a work schedule through the modal', async () => {
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text().includes('Tạo lịch làm')).trigger('click')
    const selects = wrapper.findAll('.modal form select')
    await selects[0].setValue('7')
    await selects[1].setValue('1')
    await wrapper.find('.modal form input[type="date"]').setValue('2026-08-10')
    attendanceApi.createWorkSchedule.mockResolvedValue({})
    await wrapper.find('.modal form').trigger('submit')
    await flushPromises()
    expect(attendanceApi.createWorkSchedule).toHaveBeenCalledWith(expect.objectContaining({ employeeId: 7, shiftId: 1, workDate: '2026-08-10' }))
  })
})
