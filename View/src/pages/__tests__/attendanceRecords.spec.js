import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../stores/auth', () => ({ authState: { user: { employeeId: 5 } } }))
vi.mock('../../services/employeeApi', () => ({ getAll: vi.fn() }))
vi.mock('../../services/lookupApi', () => ({ getDepartments: vi.fn() }))
vi.mock('../../services/attendanceApi', () => ({
  attendanceStatusLabelMap: { Scheduled: 'Đã lên lịch', Worked: 'Đã làm' },
  getAttendances: vi.fn(),
  getAttendanceTransits: vi.fn(),
  getAttendanceAnomalies: vi.fn(),
  deriveAttendance: vi.fn(),
  getWorkSchedules: vi.fn(),
}))

const employeeApi = await import('../../services/employeeApi')
const lookupApi = await import('../../services/lookupApi')
const attendanceApi = await import('../../services/attendanceApi')
const AttendanceRecords = (await import('../AttendanceRecords.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  employeeApi.getAll.mockResolvedValue({ data: [] })
  lookupApi.getDepartments.mockResolvedValue({ data: [] })
  attendanceApi.getAttendances.mockResolvedValue({ data: [] })
  attendanceApi.getAttendanceTransits.mockResolvedValue({ data: [] })
  attendanceApi.getAttendanceAnomalies.mockResolvedValue({ data: [] })
  attendanceApi.getWorkSchedules.mockResolvedValue({ data: [] })
})

describe('AttendanceRecords', () => {
  it('loads attendance data on mount', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    expect(employeeApi.getAll).toHaveBeenCalled()
    expect(attendanceApi.getAttendances).toHaveBeenCalled()
    expect(wrapper.exists()).toBe(true)
  })
})
