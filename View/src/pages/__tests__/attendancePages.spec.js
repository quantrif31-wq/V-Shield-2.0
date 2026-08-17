import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/attendanceApi', () => ({
  getAttendanceDailyReport: vi.fn(),
  getAttendanceMonthlyReport: vi.fn(),
  getAttendanceDepartmentReport: vi.fn(),
  getShifts: vi.fn(),
  createShift: vi.fn(),
  updateShift: vi.fn(),
  deactivateShift: vi.fn(),
  attendanceStatusLabelMap: {},
  leaveTypeLabelMap: {},
}))

const attendanceApi = await import('../../services/attendanceApi')
const AttendanceReports = (await import('../AttendanceReports.vue')).default
const AttendanceShifts = (await import('../AttendanceShifts.vue')).default

const sharedStubs = { ImportModal: true, ExportModal: true }

beforeEach(() => {
  vi.clearAllMocks()
  attendanceApi.getAttendanceDailyReport.mockResolvedValue({ data: { rows: [] } })
  attendanceApi.getAttendanceMonthlyReport.mockResolvedValue({ data: { rows: [] } })
  attendanceApi.getShifts.mockResolvedValue({ data: [] })
})

describe('AttendanceReports', () => {
  it('loads the daily report on mount', async () => {
    const wrapper = mount(AttendanceReports)
    await flushPromises()
    expect(attendanceApi.getAttendanceDailyReport).toHaveBeenCalled()
    expect(wrapper.exists()).toBe(true)
  })
})

describe('AttendanceShifts', () => {
  it('loads shifts on mount', async () => {
    const wrapper = mount(AttendanceShifts, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(attendanceApi.getShifts).toHaveBeenCalled()
  })
})
