import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/employeeApi', () => ({ getAll: vi.fn() }))
vi.mock('../../services/lookupApi', () => ({ getDepartments: vi.fn() }))
vi.mock('../../services/attendanceApi', () => ({
  approveLeaveRequest: vi.fn(),
  attendanceStatusLabelMap: { Pending: 'Chờ duyệt', Approved: 'Đã duyệt', Rejected: 'Đã từ chối', Cancelled: 'Đã hủy' },
  getLeaveRequests: vi.fn(),
  leaveTypeLabelMap: { AnnualLeave: 'Nghỉ phép năm' },
  rejectLeaveRequest: vi.fn(),
}))

const employeeApi = await import('../../services/employeeApi')
const lookupApi = await import('../../services/lookupApi')
const attendanceApi = await import('../../services/attendanceApi')
const LeaveApprovals = (await import('../LeaveApprovals.vue')).default

beforeEach(() => vi.clearAllMocks())

describe('LeaveApprovals', () => {
  it('loads leave requests and lookup options', async () => {
    employeeApi.getAll.mockResolvedValue({ data: [{ employeeId: 1, fullName: 'An' }] })
    lookupApi.getDepartments.mockResolvedValue({ data: [{ departmentId: 2, name: 'An Ninh' }] })
    attendanceApi.getLeaveRequests.mockResolvedValue({
      data: [{ leaveRequestId: 1, employeeName: 'An', departmentName: 'An Ninh', leaveType: 'AnnualLeave', startDate: '2026-08-01T00:00:00Z', endDate: '2026-08-02T00:00:00Z', reason: 'việc gia đình', status: 'Pending', createdAt: '2026-07-01T00:00:00Z' }],
    })
    const wrapper = mount(LeaveApprovals)
    await flushPromises()
    expect(attendanceApi.getLeaveRequests).toHaveBeenCalledWith({ status: 'Pending' })
    expect(wrapper.find('tbody').text()).toContain('An')
    expect(wrapper.find('tbody').text()).toContain('Nghỉ phép năm')
    expect(wrapper.find('tbody').text()).toContain('Chờ duyệt')
  })

  it('approves a pending request', async () => {
    employeeApi.getAll.mockResolvedValue({ data: [] })
    lookupApi.getDepartments.mockResolvedValue({ data: [] })
    attendanceApi.getLeaveRequests.mockResolvedValue({ data: [{ leaveRequestId: 1, employeeName: 'An', status: 'Pending' }] })
    const wrapper = mount(LeaveApprovals)
    await flushPromises()

    attendanceApi.approveLeaveRequest.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Duyệt').trigger('click')
    await flushPromises()
    expect(attendanceApi.approveLeaveRequest).toHaveBeenCalledWith(1)
    expect(wrapper.text()).toContain('Đã duyệt đơn nghỉ')
  })

  it('rejects a request through the dialog', async () => {
    employeeApi.getAll.mockResolvedValue({ data: [] })
    lookupApi.getDepartments.mockResolvedValue({ data: [] })
    attendanceApi.getLeaveRequests.mockResolvedValue({ data: [{ leaveRequestId: 2, employeeName: 'B', status: 'Pending' }] })
    const wrapper = mount(LeaveApprovals)
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text() === 'Từ chối').trigger('click')
    await wrapper.find('textarea').setValue('Sai lý do')
    attendanceApi.rejectLeaveRequest.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Xác nhận từ chối').trigger('click')
    await flushPromises()
    expect(attendanceApi.rejectLeaveRequest).toHaveBeenCalledWith(2, { rejectReason: 'Sai lý do' })
  })

  it('requires a rejection reason', async () => {
    employeeApi.getAll.mockResolvedValue({ data: [] })
    lookupApi.getDepartments.mockResolvedValue({ data: [] })
    attendanceApi.getLeaveRequests.mockResolvedValue({ data: [{ leaveRequestId: 2, employeeName: 'B', status: 'Pending' }] })
    const wrapper = mount(LeaveApprovals)
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text() === 'Từ chối').trigger('click')
    await wrapper.findAll('button').find((b) => b.text() === 'Xác nhận từ chối').trigger('click')
    await flushPromises()
    expect(attendanceApi.rejectLeaveRequest).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Vui lòng nhập lý do từ chối.')
  })
})
