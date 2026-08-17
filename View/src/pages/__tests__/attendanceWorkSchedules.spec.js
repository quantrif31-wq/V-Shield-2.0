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
  employeeApi.getAll.mockResolvedValue({ data: [] })
  lookupApi.getDepartments.mockResolvedValue({ data: [] })
  attendanceApi.getWorkSchedules.mockResolvedValue({ data: { items: [] } })
  attendanceApi.getShifts.mockResolvedValue({ data: [] })
})

describe('AttendanceWorkSchedules', () => {
  it('loads employees, departments and work schedules on mount', async () => {
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(employeeApi.getAll).toHaveBeenCalled()
    expect(lookupApi.getDepartments).toHaveBeenCalled()
    expect(attendanceApi.getWorkSchedules).toHaveBeenCalled()
  })
})
