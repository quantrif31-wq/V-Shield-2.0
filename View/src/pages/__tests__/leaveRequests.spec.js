import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/attendanceApi', () => ({
  attendanceStatusLabelMap: { Pending: 'Chờ duyệt', Approved: 'Đã duyệt', Rejected: 'Đã từ chối', Cancelled: 'Đã hủy' },
  cancelLeaveRequest: vi.fn(),
  createLeaveRequest: vi.fn(),
  getLeaveRequests: vi.fn(),
  leaveTypeLabelMap: { AnnualLeave: 'Nghỉ phép năm', SickLeave: 'Nghỉ bệnh' },
}))

const attendanceApi = await import('../../services/attendanceApi')
const LeaveRequests = (await import('../LeaveRequests.vue')).default

beforeEach(() => vi.clearAllMocks())

describe('LeaveRequests', () => {
  it('lists the employees leave requests', async () => {
    attendanceApi.getLeaveRequests.mockResolvedValue({
      data: [{ leaveRequestId: 1, leaveType: 'AnnualLeave', startDate: '2026-08-01T00:00:00Z', endDate: '2026-08-02T00:00:00Z', reason: 'việc gia đình', status: 'Pending', approverName: null, createdAt: '2026-07-01T00:00:00Z' }],
    })
    const wrapper = mount(LeaveRequests)
    await flushPromises()
    expect(wrapper.find('tbody').text()).toContain('Nghỉ phép năm')
    expect(wrapper.find('tbody').text()).toContain('việc gia đình')
    expect(wrapper.find('tbody').text()).toContain('Chờ duyệt')
  })

  it('creates a leave request through the modal', async () => {
    attendanceApi.getLeaveRequests.mockResolvedValue({ data: [] })
    const wrapper = mount(LeaveRequests)
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text() === 'Tạo đơn nghỉ').trigger('click')
    await wrapper.find('form select').setValue('SickLeave')
    const dateInputs = wrapper.findAll('form input[type="date"]')
    await dateInputs[0].setValue('2026-08-10')
    await dateInputs[1].setValue('2026-08-11')
    await wrapper.find('form textarea').setValue('Ốm')
    attendanceApi.createLeaveRequest.mockResolvedValue({})
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(attendanceApi.createLeaveRequest).toHaveBeenCalledWith(expect.objectContaining({ leaveType: 'SickLeave', reason: 'Ốm' }))
    expect(wrapper.text()).toContain('Đã gửi đơn xin nghỉ')
  })

  it('cancels a pending request through the confirm dialog', async () => {
    attendanceApi.getLeaveRequests.mockResolvedValue({
      data: [{ leaveRequestId: 3, leaveType: 'AnnualLeave', status: 'Pending' }],
    })
    const wrapper = mount(LeaveRequests)
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text() === 'Hủy đơn').trigger('click')
    attendanceApi.cancelLeaveRequest.mockResolvedValue({})
    await wrapper.find('.modal.mini .btn-danger').trigger('click')
    await flushPromises()
    expect(attendanceApi.cancelLeaveRequest).toHaveBeenCalledWith(3)
    expect(wrapper.text()).toContain('Đã hủy đơn nghỉ')
  })
})
