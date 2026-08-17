import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../http', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
    request: vi.fn(),
    defaults: { headers: { common: {} } },
  },
}))

const http = (await import('../http')).default
const attendanceApi = await import('../attendanceApi')
const biometricApi = await import('../biometricApi')

beforeEach(() => vi.clearAllMocks())

describe('attendanceApi', () => {
  it('exposes label maps for attendance states', () => {
    expect(attendanceApi.attendanceStatusLabelMap.Scheduled).toBe('Đã lên lịch')
    expect(attendanceApi.leaveTypeLabelMap.AnnualLeave).toBe('Nghỉ phép năm')
  })

  it('covers shift endpoints', () => {
    attendanceApi.getShifts({ active: true })
    expect(http.get).toHaveBeenCalledWith('/shifts', { params: { active: true } })
    attendanceApi.getShiftById(1)
    expect(http.get).toHaveBeenCalledWith('/shifts/1')
    attendanceApi.createShift({ name: 'Sáng' })
    expect(http.post).toHaveBeenCalledWith('/shifts', { name: 'Sáng' })
    attendanceApi.updateShift(1, { name: 'Chiều' })
    expect(http.put).toHaveBeenCalledWith('/shifts/1', { name: 'Chiều' })
    attendanceApi.deactivateShift(1)
    expect(http.patch).toHaveBeenCalledWith('/shifts/1/deactivate')
  })

  it('covers work-schedule endpoints', () => {
    attendanceApi.getWorkSchedules({ from: '2026-01-01' })
    expect(http.get).toHaveBeenCalledWith('/work-schedules', { params: { from: '2026-01-01' } })
    attendanceApi.getWorkScheduleById(2)
    expect(http.get).toHaveBeenCalledWith('/work-schedules/2')
    attendanceApi.getWorkSchedulesByEmployee(7)
    expect(http.get).toHaveBeenCalledWith('/work-schedules/employee/7')
    attendanceApi.createWorkSchedule({ employeeId: 7 })
    expect(http.post).toHaveBeenCalledWith('/work-schedules', { employeeId: 7 })
    attendanceApi.updateWorkSchedule(2, { x: 1 })
    expect(http.put).toHaveBeenCalledWith('/work-schedules/2', { x: 1 })
    attendanceApi.cancelWorkSchedule(2)
    expect(http.patch).toHaveBeenCalledWith('/work-schedules/2/cancel')
  })

  it('covers attendance records and check-in/out', () => {
    attendanceApi.getAttendances({ day: '2026-01-01' })
    expect(http.get).toHaveBeenCalledWith('/attendances', { params: { day: '2026-01-01' } })
    attendanceApi.getAttendanceById(3)
    expect(http.get).toHaveBeenCalledWith('/attendances/3')
    attendanceApi.getAttendancesByEmployee(7)
    expect(http.get).toHaveBeenCalledWith('/attendances/employee/7')
    attendanceApi.checkInAttendance({ employeeId: 7 })
    expect(http.post).toHaveBeenCalledWith('/attendances/check-in', { employeeId: 7 })
    attendanceApi.checkOutAttendance({ employeeId: 7 })
    expect(http.post).toHaveBeenCalledWith('/attendances/check-out', { employeeId: 7 })
    attendanceApi.updateAttendance(3, { x: 1 })
    expect(http.put).toHaveBeenCalledWith('/attendances/3', { x: 1 })
    attendanceApi.recalculateAttendance({ day: 'x' })
    expect(http.post).toHaveBeenCalledWith('/attendances/recalculate', { day: 'x' })
  })

  it('covers transit, derive and leave-request endpoints', () => {
    attendanceApi.getZoneTransits({ gateId: 1 })
    expect(http.get).toHaveBeenCalledWith('/attendances/zone-transits', { params: { gateId: 1 } })
    attendanceApi.getAttendanceTransits(3)
    expect(http.get).toHaveBeenCalledWith('/attendances/3/transits')
    attendanceApi.deriveAttendance({ employeeId: 7 })
    expect(http.post).toHaveBeenCalledWith('/attendances/derive', { employeeId: 7 })
    attendanceApi.deriveAttendanceBatch([1, 2])
    expect(http.post).toHaveBeenCalledWith('/attendances/derive-batch', [1, 2])
    attendanceApi.getLeaveRequests({ status: 'Pending' })
    expect(http.get).toHaveBeenCalledWith('/leave-requests', { params: { status: 'Pending' } })
    attendanceApi.getMyLeaveRequests()
    expect(http.get).toHaveBeenCalledWith('/leave-requests/my')
    attendanceApi.getLeaveRequestById(4)
    expect(http.get).toHaveBeenCalledWith('/leave-requests/4')
    attendanceApi.createLeaveRequest({ from: 'x' })
    expect(http.post).toHaveBeenCalledWith('/leave-requests', { from: 'x' })
    attendanceApi.approveLeaveRequest(4)
    expect(http.put).toHaveBeenCalledWith('/leave-requests/4/approve')
    attendanceApi.rejectLeaveRequest(4, { reason: 'r' })
    expect(http.put).toHaveBeenCalledWith('/leave-requests/4/reject', { reason: 'r' })
    attendanceApi.cancelLeaveRequest(4)
    expect(http.put).toHaveBeenCalledWith('/leave-requests/4/cancel')
  })

  it('covers anomalies, predictions and reports', () => {
    attendanceApi.getAttendanceAnomalies({ day: 'x' })
    expect(http.get).toHaveBeenCalledWith('/attendances/anomalies', { params: { day: 'x' } })
    attendanceApi.detectAttendanceAnomalies({ day: 'x' })
    expect(http.post).toHaveBeenCalledWith('/attendances/anomalies/detect', null, { params: { day: 'x' } })
    attendanceApi.resolveAnomaly(5, { note: 'ok' })
    expect(http.post).toHaveBeenCalledWith('/attendances/anomalies/5/resolve', { note: 'ok' })
    attendanceApi.markAnomalyFalsePositive(5)
    expect(http.post).toHaveBeenCalledWith('/attendances/anomalies/5/false-positive')
    attendanceApi.predictAbsences(7, { horizon: 30 })
    expect(http.get).toHaveBeenCalledWith('/attendances/anomalies/predict-absences/7', { params: { horizon: 30 } })
    attendanceApi.getAttendanceDailyReport({ day: 'x' })
    expect(http.get).toHaveBeenCalledWith('/reports/attendance/daily', { params: { day: 'x' } })
    attendanceApi.getAttendanceMonthlyReport({ month: 1 })
    expect(http.get).toHaveBeenCalledWith('/reports/attendance/monthly', { params: { month: 1 } })
    attendanceApi.getAttendanceDepartmentReport({ dept: 1 })
    expect(http.get).toHaveBeenCalledWith('/reports/attendance/department', { params: { dept: 1 } })
    attendanceApi.getAttendanceLateReport({ month: 1 })
    expect(http.get).toHaveBeenCalledWith('/reports/attendance/late', { params: { month: 1 } })
    attendanceApi.getAttendanceOvertimeReport({ month: 1 })
    expect(http.get).toHaveBeenCalledWith('/reports/attendance/overtime', { params: { month: 1 } })
    attendanceApi.getLeaveMonthlyReport({ month: 1 })
    expect(http.get).toHaveBeenCalledWith('/reports/leave/monthly', { params: { month: 1 } })
  })
})

describe('biometricApi', () => {
  it('covers biometric overview and face model health', () => {
    biometricApi.getBiometricOverview({ page: 1 })
    expect(http.get).toHaveBeenCalledWith('/biometrics/overview', { params: { page: 1 } })
    biometricApi.getFaceModelHealth()
    expect(http.get).toHaveBeenCalledWith('/FaceModels/health')
  })

  it('covers face enrollment jobs', () => {
    biometricApi.getFaceEnrollmentJobs()
    expect(http.get).toHaveBeenCalledWith('/FaceEnrollments')
    biometricApi.createFaceEnrollmentJob(7, 8)
    expect(http.post).toHaveBeenCalledWith('/FaceEnrollments', { employeeId: 7, employeeFaceVideoId: 8 })
    biometricApi.cancelFaceEnrollmentJob(1)
    expect(http.post).toHaveBeenCalledWith('/FaceEnrollments/1/cancel')
    biometricApi.retryFaceEnrollmentJob(1)
    expect(http.post).toHaveBeenCalledWith('/FaceEnrollments/1/retry')
    biometricApi.activateFaceEnrollmentJob(1)
    expect(http.post).toHaveBeenCalledWith('/FaceEnrollments/1/activate')
  })

  it('covers credential bindings', () => {
    biometricApi.getAccessCredentials()
    expect(http.get).toHaveBeenCalledWith('/AccessCredentials')
    biometricApi.getFaceCredentialBindings()
    expect(http.get).toHaveBeenCalledWith('/FaceCredentialBindings')
    biometricApi.getEmployeeFaceCredentialBinding(7)
    expect(http.get).toHaveBeenCalledWith('/Employees/7/face-credential-binding')
    biometricApi.getEmployeeFaceCredentialCandidates(7)
    expect(http.get).toHaveBeenCalledWith('/Employees/7/face-credential-candidates')
  })
})
