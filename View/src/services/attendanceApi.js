import http from './http'

export const attendanceStatusLabelMap = {
    Scheduled: 'Đã lên lịch',
    Worked: 'Đã làm',
    Leave: 'Nghỉ phép',
    Absent: 'Vắng mặt',
    Cancelled: 'Đã hủy',
    Changed: 'Đổi ca',
    NotCheckedIn: 'Chưa chấm công',
    CheckedIn: 'Đã vào',
    Completed: 'Hoàn thành',
    Late: 'Đi trễ',
    EarlyLeave: 'Về sớm',
    LateAndEarlyLeave: 'Trễ và về sớm',
    ForgotCheckout: 'Quên check-out',
    OutOfSchedule: 'Ngoài lịch',
    Pending: 'Chờ duyệt',
    Approved: 'Đã duyệt',
    Rejected: 'Đã từ chối',
}

export const leaveTypeLabelMap = {
    AnnualLeave: 'Nghỉ phép năm',
    SickLeave: 'Nghỉ bệnh',
    UnpaidLeave: 'Nghỉ không lương',
    PersonalLeave: 'Nghỉ việc riêng',
    Other: 'Khác',
}

export const getShifts = (params = {}) => http.get('/shifts', { params })
export const getShiftById = (id) => http.get(`/shifts/${id}`)
export const createShift = (data) => http.post('/shifts', data)
export const updateShift = (id, data) => http.put(`/shifts/${id}`, data)
export const deactivateShift = (id) => http.patch(`/shifts/${id}/deactivate`)

export const getWorkSchedules = (params = {}) => http.get('/work-schedules', { params })
export const getWorkScheduleById = (id) => http.get(`/work-schedules/${id}`)
export const getWorkSchedulesByEmployee = (employeeId) => http.get(`/work-schedules/employee/${employeeId}`)
export const createWorkSchedule = (data) => http.post('/work-schedules', data)
export const updateWorkSchedule = (id, data) => http.put(`/work-schedules/${id}`, data)
export const cancelWorkSchedule = (id) => http.patch(`/work-schedules/${id}/cancel`)

export const getAttendances = (params = {}) => http.get('/attendances', { params })
export const getAttendanceById = (id) => http.get(`/attendances/${id}`)
export const getAttendancesByEmployee = (employeeId) => http.get(`/attendances/employee/${employeeId}`)
export const checkInAttendance = (data) => http.post('/attendances/check-in', data)
export const checkOutAttendance = (data) => http.post('/attendances/check-out', data)
export const updateAttendance = (id, data) => http.put(`/attendances/${id}`, data)
export const recalculateAttendance = (data = {}) => http.post('/attendances/recalculate', data)

export const getZoneTransits = (params = {}) => http.get('/attendances/zone-transits', { params })
export const getAttendanceTransits = (id) => http.get(`/attendances/${id}/transits`)
export const deriveAttendance = (data = {}) => http.post('/attendances/derive', data)
export const deriveAttendanceBatch = (data = {}) => http.post('/attendances/derive-batch', data)

export const getLeaveRequests = (params = {}) => http.get('/leave-requests', { params })
export const getMyLeaveRequests = () => http.get('/leave-requests/my')
export const getLeaveRequestById = (id) => http.get(`/leave-requests/${id}`)
export const createLeaveRequest = (data) => http.post('/leave-requests', data)
export const approveLeaveRequest = (id) => http.put(`/leave-requests/${id}/approve`)
export const rejectLeaveRequest = (id, data) => http.put(`/leave-requests/${id}/reject`, data)
export const cancelLeaveRequest = (id) => http.put(`/leave-requests/${id}/cancel`)

export const getAttendanceAnomalies = (params = {}) => http.get('/attendances/anomalies', { params })
export const detectAttendanceAnomalies = (params = {}) => http.post('/attendances/anomalies/detect', null, { params })
export const resolveAnomaly = (id, data) => http.post(`/attendances/anomalies/${id}/resolve`, data)
export const markAnomalyFalsePositive = (id) => http.post(`/attendances/anomalies/${id}/false-positive`)
export const predictAbsences = (employeeId, params = {}) => http.get(`/attendances/anomalies/predict-absences/${employeeId}`, { params })

export const getAttendanceDailyReport = (params = {}) => http.get('/reports/attendance/daily', { params })
export const getAttendanceMonthlyReport = (params = {}) => http.get('/reports/attendance/monthly', { params })
export const getAttendanceDepartmentReport = (params = {}) => http.get('/reports/attendance/department', { params })
export const getAttendanceLateReport = (params = {}) => http.get('/reports/attendance/late', { params })
export const getAttendanceOvertimeReport = (params = {}) => http.get('/reports/attendance/overtime', { params })
export const getLeaveMonthlyReport = (params = {}) => http.get('/reports/leave/monthly', { params })

