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
  attendanceStatusLabelMap: { Scheduled: 'Đã lên lịch', Worked: 'Đã làm việc', Leave: 'Nghỉ phép', Absent: 'Vắng', Cancelled: 'Đã hủy', Changed: 'Đã thay đổi' },
  leaveTypeLabelMap: {},
}))

const employeeApi = await import('../../services/employeeApi')
const lookupApi = await import('../../services/lookupApi')
const attendanceApi = await import('../../services/attendanceApi')
const AttendanceWorkSchedules = (await import('../AttendanceWorkSchedules.vue')).default

const sharedStubs = { ImportModal: true, ExportModal: true }

beforeEach(() => {
  vi.clearAllMocks()
  employeeApi.getAll.mockResolvedValue({ data: [{ employeeId: 7, fullName: 'Nguyễn An', departmentId: 2, departmentName: 'Vận hành' }] })
  lookupApi.getDepartments.mockResolvedValue({ data: [{ departmentId: 2, name: 'Vận hành' }] })
  attendanceApi.getWorkSchedules.mockResolvedValue({ data: [] })
  attendanceApi.getShifts.mockResolvedValue({ data: [{ shiftId: 1, shiftName: 'Ca sáng', startTime: '08:00', endTime: '16:00' }] })
})

describe('AttendanceWorkSchedules', () => {
  it('loads employees, departments and work schedules on mount', async () => {
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(employeeApi.getAll).toHaveBeenCalled()
    expect(lookupApi.getDepartments).toHaveBeenCalled()
    expect(attendanceApi.getWorkSchedules).toHaveBeenCalled()
  })

  it('renders schedules and formatting helpers', async () => {
    attendanceApi.getWorkSchedules.mockResolvedValue({ data: [{ scheduleId: 1, employeeName: 'NV', departmentName: 'Dep', workDate: '2026-08-10T00:00:00Z', shiftName: 'Sáng', shiftStartTime: '08:00:00', shiftEndTime: '16:00:00', status: 'Scheduled', note: 'ghi chú' }] })
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(wrapper.find('table').text()).toContain('NV')
    expect(wrapper.vm.formatDate('')).toBe('--')
    expect(wrapper.vm.formatTime('08:15:30')).toBe('08:15')
    expect(wrapper.vm.statusLabel('Nope')).toBe('Nope')
  })

  it('shows error text when loading schedules fails', async () => {
    attendanceApi.getWorkSchedules.mockRejectedValue({ response: { data: { message: 'tải lỗi' } } })
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(wrapper.vm.error).toBe('tải lỗi')
  })

  it('loadLookups swallows errors', async () => {
    const err = vi.spyOn(console, 'error').mockImplementation(() => {})
    attendanceApi.getShifts.mockRejectedValue(new Error('boom'))
    employeeApi.getAll.mockRejectedValue(new Error('boom'))
    lookupApi.getDepartments.mockRejectedValue(new Error('boom'))
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(wrapper.vm.shifts).toEqual([])
    err.mockRestore()
  })

  it('creates a work schedule through the modal', async () => {
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.vm.openCreateModal()
    const modal = wrapper.find('.modal')
    const selects = modal.findAll('select')
    await selects[0].setValue('7')
    await selects[1].setValue('1')
    await wrapper.find('.modal input[type="date"]').setValue('2026-08-10')
    attendanceApi.createWorkSchedule.mockResolvedValue({})
    await modal.find('form').trigger('submit')
    await flushPromises()
    expect(attendanceApi.createWorkSchedule).toHaveBeenCalledWith(expect.objectContaining({ employeeId: 7, shiftId: 1, workDate: '2026-08-10' }))
  })

  it('submitForm validates required fields and aborts', async () => {
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.vm.openCreateModal()
    await wrapper.find('.modal form').trigger('submit')
    expect(wrapper.vm.fieldErrors.employeeId).toBeTruthy()
    expect(attendanceApi.createWorkSchedule).not.toHaveBeenCalled()
  })

  it('submitForm updates an existing schedule', async () => {
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.openEditModal({ scheduleId: 5, employeeId: 7, shiftId: 1, workDate: '2026-08-10T00:00:00Z', note: 'n' })
    await flushPromises()
    attendanceApi.updateWorkSchedule.mockResolvedValue({})
    await wrapper.find('.modal form').trigger('submit')
    await flushPromises()
    expect(attendanceApi.updateWorkSchedule).toHaveBeenCalledWith(5, expect.objectContaining({ employeeId: 7 }))
  })

  it('submitForm surfaces API errors', async () => {
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.vm.openCreateModal()
    const selects = wrapper.find('.modal').findAll('select')
    await selects[0].setValue('7')
    await selects[1].setValue('1')
    await wrapper.find('.modal input[type="date"]').setValue('2026-08-10')
    attendanceApi.createWorkSchedule.mockRejectedValue({ response: { data: { message: 'lưu lỗi' } } })
    await wrapper.find('.modal form').trigger('submit')
    await flushPromises()
    expect(wrapper.vm.modalError).toBe('lưu lỗi')
  })

  it('bulk form validates and aborts on empty selection', async () => {
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.vm.openBulkModal()
    wrapper.vm.bulkForm.shiftId = 1
    wrapper.vm.bulkForm.fromDate = '2026-08-10'
    wrapper.vm.bulkForm.toDate = '2026-08-11'
    await wrapper.vm.submitBulkForm()
    expect(wrapper.vm.bulkErrors.employeeIds).toBeTruthy()
    expect(attendanceApi.createWorkSchedule).not.toHaveBeenCalled()
  })

  it('bulk form rejects an inverted date range', async () => {
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.vm.openBulkModal()
    wrapper.vm.bulkForm.shiftId = 1
    wrapper.vm.bulkForm.employeeIds = [7]
    wrapper.vm.bulkForm.fromDate = '2026-08-11'
    wrapper.vm.bulkForm.toDate = '2026-08-10'
    await wrapper.vm.submitBulkForm()
    expect(wrapper.vm.bulkErrors.toDate).toBeTruthy()
  })

  it('bulk form creates schedules across dates', async () => {
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.vm.openBulkModal()
    wrapper.vm.bulkForm.shiftId = 1
    wrapper.vm.bulkForm.employeeIds = [7]
    wrapper.vm.bulkForm.fromDate = '2026-08-10'
    wrapper.vm.bulkForm.toDate = '2026-08-11'
    attendanceApi.createWorkSchedule.mockResolvedValue({})
    await wrapper.vm.submitBulkForm()
    await flushPromises()
    expect(attendanceApi.createWorkSchedule).toHaveBeenCalledTimes(2)
  })

  it('bulk form counts duplicated and failed schedules', async () => {
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.vm.openBulkModal()
    wrapper.vm.bulkForm.shiftId = 1
    wrapper.vm.bulkForm.employeeIds = [7, 8]
    wrapper.vm.bulkForm.fromDate = '2026-08-10'
    wrapper.vm.bulkForm.toDate = '2026-08-10'
    attendanceApi.createWorkSchedule
      .mockRejectedValueOnce({ response: { status: 409 } })
      .mockRejectedValueOnce(new Error('x'))
    await wrapper.vm.submitBulkForm()
    await flushPromises()
    expect(wrapper.vm.bulkError).toBeTruthy()
  })

  it('selectFilteredEmployees and clearBulkEmployees manage the selection', async () => {
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.vm.openBulkModal()
    await wrapper.vm.selectFilteredEmployees()
    expect(wrapper.vm.bulkForm.employeeIds).toContain(7)
    await wrapper.vm.clearBulkEmployees()
    expect(wrapper.vm.bulkForm.employeeIds).toEqual([])
  })

  it('toggleBulkEmployee adds and removes an employee', async () => {
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.vm.openBulkModal()
    wrapper.vm.toggleBulkEmployee(7)
    expect(wrapper.vm.bulkForm.employeeIds).toEqual([7])
    wrapper.vm.toggleBulkEmployee(7)
    expect(wrapper.vm.bulkForm.employeeIds).toEqual([])
  })

  it('filteredBulkEmployees filters by department', async () => {
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.vm.openBulkModal()
    wrapper.vm.bulkForm.departmentId = '2'
    expect(wrapper.vm.filteredBulkEmployees).toHaveLength(1)
  })

  it('enumerateDates returns each day in the range', async () => {
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    const dates = wrapper.vm.enumerateDates('2026-08-10', '2026-08-12')
    expect(dates).toHaveLength(3)
  })

  it('cancels a schedule through the confirm dialog', async () => {
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.confirmCancel({ scheduleId: 3, employeeName: 'NV', workDate: '2026-08-10' })
    expect(wrapper.vm.confirmDialog.open).toBe(true)
    attendanceApi.cancelWorkSchedule.mockResolvedValue({})
    await wrapper.vm.handleCancelSchedule()
    expect(attendanceApi.cancelWorkSchedule).toHaveBeenCalledWith(3)
  })

  it('handleCancelSchedule reports failure via toast', async () => {
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.confirmDialog.scheduleId = 3
    attendanceApi.cancelWorkSchedule.mockRejectedValue({ response: { data: { message: 'hủy lỗi' } } })
    await wrapper.vm.handleCancelSchedule()
    expect(wrapper.vm.toast.type).toBe('error')
  })

  it('handleCancelSchedule does nothing without a schedule id', async () => {
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.confirmDialog.scheduleId = null
    await wrapper.vm.handleCancelSchedule()
    expect(attendanceApi.cancelWorkSchedule).not.toHaveBeenCalled()
  })

  it('onImportComplete shows success or error toast', async () => {
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.showImportModal = true
    wrapper.vm.onImportComplete({ successCount: 3, errorCount: 0 })
    expect(wrapper.vm.showImportModal).toBe(false)
    wrapper.vm.onImportComplete({ successCount: 1, errorCount: 2 })
    expect(wrapper.vm.toast.type).toBe('error')
  })

  it('clearFieldError and clearBulkError clear errors', async () => {
    const wrapper = mount(AttendanceWorkSchedules, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.fieldErrors.employeeId = 'x'
    wrapper.vm.clearFieldError('employeeId')
    expect(wrapper.vm.fieldErrors.employeeId).toBe('')
    wrapper.vm.bulkErrors.fromDate = 'x'
    wrapper.vm.clearBulkError('fromDate')
    expect(wrapper.vm.bulkErrors.fromDate).toBe('')
  })
})
