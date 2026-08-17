import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({ authState: { user: { employeeId: 7 } } }))

vi.mock('../../stores/auth', () => ({ authState: hoisted.authState }))
vi.mock('../../services/employeeApi', () => ({ getMyProfile: vi.fn() }))
vi.mock('../../services/vehicleApi', () => ({ getByEmployeeId: vi.fn() }))
vi.mock('../../services/attendanceApi', () => ({ getWorkSchedules: vi.fn() }))
vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getComplianceReports: vi.fn(),
    runComplianceReport: vi.fn(),
    getExportRequests: vi.fn(),
    approveExportRequest: vi.fn(),
    getBackupRuns: vi.fn(),
    getRestoreDrills: vi.fn(),
  },
}))

const employeeApi = await import('../../services/employeeApi')
const vehicleApi = await import('../../services/vehicleApi')
const attendanceApi = await import('../../services/attendanceApi')
const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi

const MyProfile = (await import('../MyProfile.vue')).default
const MyVehicles = (await import('../MyVehicles.vue')).default
const MySchedule = (await import('../MySchedule.vue')).default
const ComplianceReports = (await import('../ComplianceReports.vue')).default
const ExportApprovalQueue = (await import('../ExportApprovalQueue.vue')).default
const BackupRestoreDrillDashboard = (await import('../BackupRestoreDrillDashboard.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  hoisted.authState.user = { employeeId: 7 }
})

describe('MyProfile', () => {
  it('renders the personal profile from the API', async () => {
    employeeApi.getMyProfile.mockResolvedValue({
      data: { employeeId: 7, fullName: 'Nguyễn Văn An', departmentName: 'An Ninh', positionName: 'Bảo vệ', email: 'an@example.com', phone: '0901' },
    })
    const wrapper = mount(MyProfile)
    await flushPromises()
    expect(employeeApi.getMyProfile).toHaveBeenCalledOnce()
    expect(wrapper.text()).toContain('Nguyễn Văn An')
    expect(wrapper.text()).toContain('An Ninh')
    expect(wrapper.text()).toContain('#7')
  })

  it('shows an error when the profile cannot be loaded', async () => {
    employeeApi.getMyProfile.mockRejectedValue(new Error('x'))
    const wrapper = mount(MyProfile)
    await flushPromises()
    expect(wrapper.text()).toContain('Không thể tải thông tin cá nhân.')
  })
})

describe('MyVehicles', () => {
  it('renders the vehicles of the current employee', async () => {
    vehicleApi.getByEmployeeId.mockResolvedValue({
      data: [{ vehicleId: 1, licensePlate: '29A-123.45', vehicleTypeName: 'Ô tô', parkingStatus: 'IN', description: 'xe cty' }],
    })
    const wrapper = mount(MyVehicles)
    await flushPromises()
    expect(vehicleApi.getByEmployeeId).toHaveBeenCalledWith(7)
    expect(wrapper.text()).toContain('29A-123.45')
    expect(wrapper.text()).toContain('Trong bãi')
  })

  it('shows an empty state when there are no vehicles', async () => {
    vehicleApi.getByEmployeeId.mockResolvedValue({ data: [] })
    const wrapper = mount(MyVehicles)
    await flushPromises()
    expect(wrapper.text()).toContain('Bạn chưa gửi xe nào trong bãi.')
  })

  it('shows an error and retries loading', async () => {
    vehicleApi.getByEmployeeId.mockRejectedValue(new Error('x'))
    const wrapper = mount(MyVehicles)
    await flushPromises()
    expect(wrapper.text()).toContain('Không thể tải danh sách xe.')

    vehicleApi.getByEmployeeId.mockResolvedValue({
      data: [{ vehicleId: 1, licensePlate: '29A-123.45', parkingStatus: 'IN' }],
    })
    await wrapper.find('button').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('29A-123.45')
  })
})

describe('MySchedule', () => {
  it('loads the work schedule for the current month', async () => {
    attendanceApi.getWorkSchedules.mockResolvedValue({
      data: [{ workScheduleId: 1, workDate: '2026-08-01', shiftName: 'Ca sáng', startTime: '08:00', endTime: '16:00', status: 'Scheduled' }],
    })
    const wrapper = mount(MySchedule)
    await flushPromises()
    expect(attendanceApi.getWorkSchedules).toHaveBeenCalledWith(expect.objectContaining({ employeeId: 7, fromDate: expect.any(String), toDate: expect.any(String) }))
    expect(wrapper.text()).toContain('Ca sáng')
    expect(wrapper.text()).toContain('Đã lên lịch')
  })

  it('skips the request when the employee is unknown', async () => {
    hoisted.authState.user = null
    const wrapper = mount(MySchedule)
    await flushPromises()
    expect(attendanceApi.getWorkSchedules).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Chưa có lịch làm việc.')
  })
})

describe('ComplianceReports', () => {
  it('lists compliance reports', async () => {
    enterpriseApi.getComplianceReports.mockResolvedValue({
      data: [{ complianceReportRunId: 1, reportType: 'AccessReview', status: 'Completed', periodStartUtc: '2026-08-01T00:00:00Z', periodEndUtc: '2026-08-02T00:00:00Z', outputReference: 'ref-1', createdAtUtc: '2026-08-03T00:00:00Z' }],
    })
    const wrapper = mount(ComplianceReports)
    await flushPromises()
    expect(enterpriseApi.getComplianceReports).toHaveBeenCalledWith(expect.objectContaining({ limit: 50 }))
    expect(wrapper.text()).toContain('Rà soát truy cập')
    expect(wrapper.text()).toContain('Hoàn tất')
  })

  it('shows an empty card when no reports exist', async () => {
    enterpriseApi.getComplianceReports.mockResolvedValue({ data: [] })
    const wrapper = mount(ComplianceReports)
    await flushPromises()
    expect(wrapper.text()).toContain('Chưa có báo cáo tuân thủ.')
  })
})

describe('ExportApprovalQueue', () => {
  it('lists export requests and approves pending ones', async () => {
    enterpriseApi.getExportRequests.mockResolvedValue({
      data: [{ evidenceExportRequestId: 5, evidenceItemId: 9, purpose: 'Điều tra', recipient: 'admin', status: 'PendingApproval', requestedAtUtc: '2026-08-01T00:00:00Z' }],
    })
    const wrapper = mount(ExportApprovalQueue)
    await flushPromises()
    expect(wrapper.text()).toContain('Điều tra')
    const approveButton = wrapper.findAll('button').find((b) => b.text() === 'Phê duyệt')
    expect(approveButton).toBeDefined()

    const promptSpy = vi.spyOn(window, 'prompt').mockReturnValue('Vụ việc #1')
    enterpriseApi.approveExportRequest.mockResolvedValue({})
    enterpriseApi.getExportRequests.mockResolvedValue({ data: [] })
    await approveButton.trigger('click')
    await flushPromises()
    expect(enterpriseApi.approveExportRequest).toHaveBeenCalledWith(5, { watermark: 'Vụ việc #1' })
    promptSpy.mockRestore()
  })
})

describe('BackupRestoreDrillDashboard', () => {
  it('loads backup runs and restore drills together', async () => {
    enterpriseApi.getBackupRuns.mockResolvedValue({ data: [{ backupRunId: 1, profile: 'main', status: 'Completed', startedAtUtc: '2026-08-01T00:00:00Z', sizeBytes: 1048576 }] })
    enterpriseApi.getRestoreDrills.mockResolvedValue({ data: [{ restoreDrillId: 1, profile: 'main', status: 'Completed', startedAtUtc: '2026-08-01T00:00:00Z', targetRpoMinutes: 15, targetRtoMinutes: 30, passed: true }] })
    const wrapper = mount(BackupRestoreDrillDashboard)
    await flushPromises()
    expect(enterpriseApi.getBackupRuns).toHaveBeenCalledWith({ limit: 10 })
    expect(enterpriseApi.getRestoreDrills).toHaveBeenCalledWith({ limit: 10 })
    expect(wrapper.text()).toContain('1.00 MB')
    expect(wrapper.text()).toContain('15min')
    expect(wrapper.text()).toContain('ĐẠT')
  })
})
