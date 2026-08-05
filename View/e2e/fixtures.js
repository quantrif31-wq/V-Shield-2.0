import { test as base, expect } from '@playwright/test'

export const roleProfiles = {
  Admin: { role: 'Admin', tasks: [] },
  BaoVe: { role: 'BaoVe', tasks: ['monitoring', 'access-logs', 'parking', 'gate-transit', 'qr-access'] },
  QuanLy: { role: 'QuanLy', tasks: ['dashboard', 'access-logs', 'reports', 'approvals', 'metadata'] },
  LeTan: { role: 'LeTan', tasks: ['reception', 'guest-support'] },
  NhanSu: { role: 'NhanSu', tasks: ['employee-directory', 'user-admin', 'approvals'] },
}

function userFor(role) {
  const profile = roleProfiles[role] || roleProfiles.Admin
  return { userId: 9001, username: `visual.${role.toLowerCase()}`, fullName: `Visual Test ${role}`, role: profile.role, employeeId: 101, mfaEnabled: true, mfaRequired: true, hasOperationalScopeAssignments: profile.role !== 'Admin', operationalTaskKeys: profile.tasks }
}

const samples = {
  employee: { employeeId: 101, fullName: 'Nguyễn Minh An', phone: '0901234567', email: 'an@vshield.test', departmentName: 'An ninh', positionName: 'Chuyên viên', departmentId: 1, positionId: 1, status: true, faceImageUrl: '' },
  registration: { registrationId: 201, guestFullName: 'Trần Thu Hà', guestPhone: '0912345678', hostEmployeeName: 'Nguyễn Minh An', expectedTimeIn: '2026-08-05T08:00:00+07:00', expectedTimeOut: '2026-08-05T10:00:00+07:00', numberOfVisitors: 2, status: 'Pending' },
  vehicle: { vehicleId: 301, licensePlate: '51A-12345', vehicleTypeId: 1, vehicleTypeName: 'Ô tô', employeeId: 101, employeeFullName: 'Nguyễn Minh An', description: 'Sedan màu trắng' },
  access: { logId: 401, timestamp: '2026-08-05T08:30:00+07:00', actorName: 'Nguyễn Minh An', actorType: 'Employee', direction: 'IN', gateName: 'Cổng chính', cameraName: 'ANPR-01', capturedLicensePlate: '51A-12345', method: 'face-and-plate', resultStatus: 'GRANTED', isBypass: false, isException: false },
}

function json(route, body, status = 200) { return route.fulfill({ status, contentType: 'application/json; charset=utf-8', body: JSON.stringify(body) }) }

async function installApiMock(page, state) {
  await page.route('**:5107/api/**', async (route) => {
    const request = route.request(); const url = new URL(request.url()); const path = url.pathname.toLowerCase(); const empty = state === 'empty'
    if (state === 'loading' && path === '/api/employees') { await new Promise((resolve) => setTimeout(resolve, 2500)); return json(route, [samples.employee]) }
    if (state === 'forbidden' && path === '/api/employees') return json(route, { message: 'Bạn không có quyền xem danh sách nhân viên.' }, 403)
    if (state === 'error' && path === '/api/employees') return json(route, { message: 'Không thể tải dữ liệu thử nghiệm.' }, 503)
    if (path === '/api/employees') return json(route, empty ? [] : [samples.employee])
    if (path === '/api/departments') return json(route, [{ departmentId: 1, name: 'An ninh' }])
    if (path === '/api/positions') return json(route, [{ positionId: 1, name: 'Chuyên viên' }])
    if (path === '/api/statistics/employees/summary') return json(route, { totalEmployees: empty ? 0 : 1, activeEmployees: empty ? 0 : 1, inactiveEmployees: 0 })
    if (path === '/api/pre-registrations') return json(route, { items: empty ? [] : [samples.registration], total: empty ? 0 : 1 })
    if (path.match(/^\/api\/pre-registrations\/\d+$/)) return json(route, { ...samples.registration, visitors: [], accessLogs: [] })
    if (path === '/api/vehicles/types') return json(route, [{ vehicleTypeId: 1, typeName: 'Ô tô' }, { vehicleTypeId: 2, typeName: 'Xe máy' }])
    if (path === '/api/vehicles') return json(route, empty ? [] : [samples.vehicle])
    if (path === '/api/access-logs/summary') return json(route, { totalToday: empty ? 0 : 42, entriesToday: empty ? 0 : 24, exitsToday: empty ? 0 : 18, exceptionsToday: 1, bypassToday: 2, vehiclesInside: 7, successRate: 98 })
    if (path === '/api/access-logs') return json(route, { items: empty ? [] : [samples.access], total: empty ? 0 : 1 })
    if (path.match(/^\/api\/access-logs\/\d+$/)) return json(route, samples.access)
    if (path === '/api/device-management/gates') return json(route, [{ gateId: 1, gateName: 'Cổng chính' }])
    if (path === '/api/device-management/overview') return json(route, { summary: { camerasConfigured: empty ? 0 : 2, gatesConfigured: empty ? 0 : 1, camerasLinkedToGate: empty ? 0 : 2, unassignedCameras: 0 }, cameras: empty ? [] : [{ cameraId: 1, cameraName: 'Camera cổng chính', cameraType: 'ANPR', gateId: 1, gateName: 'Cổng chính', streamUrl: 'rtsp://camera/stream', isOnline: true }, { cameraId: 2, cameraName: 'Camera hành lang', cameraType: 'Face', gateId: 1, gateName: 'Cổng chính', streamUrl: 'rtsp://camera-02/stream', isOnline: false, status: 'Disconnected' }], gates: empty ? [] : [{ gateId: 1, gateName: 'Cổng chính', location: 'Sảnh A', cameraCount: 2, accessLogCount: 42 }] })
    if (path === '/api/dashboard/overview') return json(route, { snapshot: { generatedAtUtc: '2026-08-05T02:00:00Z', openAlarms: 2, pendingInterventions: 1, activeVisitors: 12, vehiclesInside: 7, employeesWorkingToday: 86, employeesNotCheckedIn: 3, employeesLateToday: 2, totalShiftsToday: 91, totalOvertimeHoursToday: 6.5, recognitionCoverage: 97, guestProfiles: 248, camerasConfigured: 18, gatesConfigured: 6 }, weeklyTraffic: [{ label: 'T2', entries: 82, exits: 76 }, { label: 'T3', entries: 94, exits: 91 }], recentActivities: [] })
    if (path === '/api/dashboard/intelligence') return json(route, { insights: [], generatedAtUtc: '2026-08-05T02:00:00Z' })
    if (path === '/api/enterprise/visitor-vehicle/lane-health') return json(route, [{ laneId: 1, laneName: 'Làn cổng chính', status: 'Healthy' }, { laneId: 2, laneName: 'Làn phụ', status: 'Degraded' }])
    if (path === '/api/vip-alerts') return json(route, [])
    if (path === '/api/enterprise/visitor-vehicle/watchlist-matches') return json(route, { items: empty ? [] : [{ watchlistMatchId: 1, status: 'Pending', matchedAtUtc: '2026-08-05T01:00:00Z', watchlistEntry: { displayName: 'Đối tượng mẫu', severity: 'High', reason: 'Cần xác minh danh tính' }, visit: { visitorName: 'Khách cần rà soát', visitorPhone: '0900000000', status: 'Approved', hostEmployee: { fullName: 'Nguyễn Minh An' } } }], total: empty ? 0 : 1 })
    if (path === '/api/enterprise/visitor-vehicle/watchlist-entries') return json(route, empty ? [] : [{ watchlistEntryId: 1, displayName: 'Đối tượng mẫu', entityType: 'Person', identifier: 'ID-001', severity: 'High', isActive: true, reason: 'Cần xác minh' }])
    if (path === '/api/enterprise/situational-awareness/ai-metrics/summary') return json(route, { pendingReviews: empty ? 0 : 3, totalReviewed: 48, precisionProxy: 96, totalFalsePositive: 2, totalFalseNegative: 1, totalTrainingCandidate: 4, recentDriftScore: 0.08, driftDetected: false })
    if (path === '/api/enterprise/situational-awareness/ai-metrics') return json(route, empty ? [] : [{ aiPerformanceMetricId: 1, metricName: 'Precision', metricValue: 0.96, aiSource: 'Face', capturedAtUtc: '2026-08-05T01:00:00Z' }])
    if (path === '/api/enterprise/situational-awareness/ai-adjudications') return json(route, { items: empty ? [] : [{ aiAdjudicationItemId: 1, aiSource: 'Face', modelVersion: 'v2.4', confidence: 0.93, status: 'Pending', outcome: null }], total: empty ? 0 : 1 })
    if (path === '/api/enterprise/evidence/redaction-requests') return json(route, empty ? [] : [{ redactionRequestId: 1, evidenceItemId: 22, privacyLabel: 'Biometric', reason: 'Che khuôn mặt người không liên quan', status: 'PendingApproval' }])
    if (path === '/api/enterprise/operations/config-health') return json(route, empty ? [] : [{ category: 'Event Bus', status: 'Healthy', findings: [] }])
    if (path === '/api/enterprise/situational-awareness/correlations') return json(route, empty ? [] : [{ time: '08:00', count: 8 }, { time: '09:00', count: 12 }])
    if (path === '/api/enterprise/operations/backup-runs') return json(route, empty ? [] : [{ backupRunId: 1, profile: 'Nightly', status: 'Completed', startedAtUtc: '2026-08-05T01:00:00Z', sizeBytes: 1048576, targetRpoMinutes: 30, targetRtoMinutes: 60 }])
    if (path === '/api/enterprise/operations/overview') return json(route, { totalEvents: 120, pendingEvents: 4, dispatchedEvents: 110, failedEvents: 3, deadLetter: 3 })
    if (path.includes('/api/enterprise/') && path.endsWith('/overview')) return json(route, {})
    if (path.includes('/notifications/unread-count')) return json(route, { count: 0 })
    if (path.includes('/notifications')) return json(route, { items: [], total: 0 })
    return json(route, request.method() === 'GET' ? { items: [], total: 0 } : {})
  })
}

export const test = base.extend({
  role: ['Admin', { option: true }],
  mockState: ['default', { option: true }],
  _authenticated: [async ({ context, role }, use) => {
    const user = userFor(role)
    await context.addInitScript(({ user }) => {
      sessionStorage.setItem('v_shield_token', 'visual-test-token')
      sessionStorage.setItem('v_shield_refresh_token', 'visual-test-refresh')
      sessionStorage.setItem('v_shield_user', JSON.stringify(user))
      localStorage.setItem('vshield-theme', 'light')
      localStorage.setItem('vshield-density', 'comfortable')
    }, { user })
    await use()
  }, { auto: true }],
  _mockApi: [async ({ page, mockState }, use) => { await installApiMock(page, mockState); await use() }, { auto: true }],
})

export { expect }
