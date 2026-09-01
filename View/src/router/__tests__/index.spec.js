import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => {
  const guards = {}
  const routes = []
  const fakeRouter = {
    beforeEach: (fn) => { guards.beforeEach = fn },
    onError: (fn) => { guards.onError = fn },
  }
  return { guards, fakeRouter, routes }
})

vi.mock('../pages/Login.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../components/portal/PortalLayout.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/portal/PortalHome.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/portal/PortalFeatures.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/portal/PortalRoadmap.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/portal/PortalDownload.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/portal/PortalCommunity.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/portal/PortalAbout.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/portal/PortalContact.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/ForcePasswordChange.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../components/Layout/MainLayout.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/Dashboard.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/AboutProject.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/Employees.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/Vehicles.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/MyVehicles.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/MySchedule.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/MyProfile.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/VehicleTransfer.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/AccessLogs.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/Monitoring.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/Settings.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/UserManagement.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/DepartmentPosition.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/PreRegistration.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/GuestRegister.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/VisitorPass.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../components/FaceCamera.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../components/LicensePlateSecurity.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../components/FaceVideo.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../components/GateTransitMonitor.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../components/ThongHanh.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../components/DynamicQrGenerator.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/Exceptions.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/RegistrationLinks.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/GuestProfiles.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/DeviceManagement.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/Biometrics.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/SystemCatalog.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../components/QrAccessMonitor.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/SystemAuditLogs.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/AttendanceShifts.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/AttendanceWorkSchedules.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/AttendanceRecords.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/LeaveRequests.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/LeaveApprovals.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/AttendanceReports.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/EnterpriseSecurityOperations.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/ImportExportHistory.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/CampusMapPage.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/UEBA.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/SocAlarmConsole.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/IdentityManagement.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/SiteHierarchy.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/ReceptionDashboard.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/ManualAccessFallback.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/ManualParkingConsole.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/KioskCheckIn.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/HostVisitorPage.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/WatchlistQueue.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/ContractorManagement.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/BarrierPanel.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/DeviceTopology.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/ProvisioningWizard.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/OfflinePackages.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/DeviceHealth.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/SimulatorPanel.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/VideoSearch.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/AiReviewQueue.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/CorrelationView.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/EvidenceRepository.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/CameraArchive.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/ExportApprovalQueue.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/RedactionQueue.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/ComplianceReports.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/LostFoundDashboard.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/Chat.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/RolePermissions.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/NotificationRuleEditor.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/IncidentMapPage.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/OperationsDashboard.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/SIEMExportStatus.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/BackupRestoreDrillDashboard.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/WebhookDeliveryViewer.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/VulnerabilityReleaseGateStatus.vue', () => ({ default: { name: 'Mock' } }))
vi.mock('../pages/MyFaceId.vue', () => ({ default: { name: 'Mock' } }))

vi.mock('vue-router', () => ({
  createRouter: ({ routes }) => { hoisted.routes.push(...routes); return hoisted.fakeRouter },
  createWebHistory: () => ({}),
}))

vi.mock('../../stores/auth', () => ({
  isLoggedIn: vi.fn(),
  hasRole: vi.fn(),
  authState: { user: null },
}))

const { isLoggedIn, hasRole, authState } = await import('../../stores/auth')
await import('../index')

let beforeEachGuard
let onErrorGuard

beforeEach(() => {
  vi.clearAllMocks()
  sessionStorage.clear()
  authState.user = null
  beforeEachGuard = hoisted.guards.beforeEach
  onErrorGuard = hoisted.guards.onError
  window.history.pushState({}, '', '/')
})

afterEach(() => {
  vi.clearAllTimers()
  vi.useRealTimers()
})

function runGuard(to, from = {}) {
  const next = vi.fn()
  beforeEachGuard({ meta: {}, matched: [], name: undefined, fullPath: '/', ...to }, from, next)
  return next
}

describe('router route definitions', () => {
  it('defines all expected routes and lazy components resolve', async () => {
    const collect = (list) => list.flatMap((r) => [r, ...(r.children ? collect(r.children) : [])])
    const flat = collect(hoisted.routes)
    expect(flat.some((r) => r.name === 'Dashboard')).toBe(true)
    expect(flat.some((r) => r.name === 'Login')).toBe(true)
    expect(flat.some((r) => r.name === 'DynamicQrGenerator')).toBe(true)
    const componentSlots = flat.filter((r) => r.component && typeof r.component === 'function')
    expect(componentSlots.length).toBeGreaterThan(50)
    await Promise.all(componentSlots.map(async (r) => {
      const mod = await r.component()
      expect(mod).toBeTruthy()
    }))
  }, 20000)
})

describe('router authentication guard', () => {
  it('redirects to login when an authenticated route is visited logged out', () => {
    isLoggedIn.mockReturnValue(false)
    const next = runGuard({ matched: [{ meta: { requiresAuth: true } }], fullPath: '/employees' })
    expect(next).toHaveBeenCalledWith({ name: 'Login', query: { redirect: '/employees' } })
  })

  it('forces password change for users that must change it', () => {
    isLoggedIn.mockReturnValue(true)
    authState.user = { role: 'Admin', requiresPasswordChange: true }
    let next = runGuard({ name: 'Employees', matched: [] })
    expect(next).toHaveBeenCalledWith({ name: 'ForcePasswordChange', query: { redirect: expect.anything() } })
    next = runGuard({ name: 'Login', matched: [] })
    expect(next).toHaveBeenCalledWith({ name: 'ForcePasswordChange' })
  })

  it('redirects non-admins away from admin-only routes', () => {
    isLoggedIn.mockReturnValue(true)
    authState.user = { role: 'BaoVe' }
    const next = runGuard({ matched: [{ meta: { requiresAdmin: true } }] })
    expect(next).toHaveBeenCalledWith({ name: 'Dashboard' })
  })

  it('blocks roles that are neither in allowedRoles nor granted the task', () => {
    isLoggedIn.mockReturnValue(true)
    authState.user = { role: 'BaoVe', operationalTaskKeys: [] }
    const next = runGuard({ matched: [{ meta: { allowedRoles: ['Admin'], taskKey: 'reports' } }] })
    expect(next).toHaveBeenCalledWith({ name: 'Dashboard' })
  })

  it('admits a role granted through operationalTaskKeys', () => {
    isLoggedIn.mockReturnValue(true)
    authState.user = { role: 'BaoVe', operationalTaskKeys: ['reports'] }
    const next = runGuard({ matched: [{ meta: { allowedRoles: ['Admin'], taskKey: 'reports' } }] })
    expect(next).toHaveBeenCalledWith()
  })

  it('admits a directly allowed role', () => {
    isLoggedIn.mockReturnValue(true)
    authState.user = { role: 'Admin' }
    const next = runGuard({ matched: [{ meta: { allowedRoles: ['Admin'], taskKey: 'reports' } }] })
    expect(next).toHaveBeenCalledWith()
  })

  it('enforces task keys even without allowedRoles', () => {
    isLoggedIn.mockReturnValue(true)
    authState.user = { role: 'QuanLy', operationalTaskKeys: [] }
    const next = runGuard({ matched: [{ meta: { taskKey: 'reports' } }] })
    expect(next).toHaveBeenCalledWith({ name: 'Dashboard' })
  })

  it('keeps logged-in users out of guest pages except registration', () => {
    isLoggedIn.mockReturnValue(true)
    authState.user = { role: 'BaoVe' }
    let next = runGuard({ name: 'Login', meta: { guest: true }, matched: [] })
    expect(next).toHaveBeenCalledWith({ name: 'Dashboard' })
    next = runGuard({ name: 'GuestRegister', meta: { guest: true }, matched: [] })
    expect(next).toHaveBeenCalledWith()
  })

  it('lets navigation through without restrictions', () => {
    isLoggedIn.mockReturnValue(true)
    authState.user = { role: 'Admin' }
    const next = runGuard({ name: 'Dashboard', matched: [] })
    expect(next).toHaveBeenCalledWith()
  })
})

describe('router landing route selection', () => {
  function expectLanding(role, operationalTaskKeys, expectedName) {
    isLoggedIn.mockReturnValue(true)
    authState.user = { role, operationalTaskKeys: operationalTaskKeys ?? [] }
    const next = runGuard({ name: 'Login', meta: { guest: true }, matched: [] })
    expect(next).toHaveBeenCalledWith({ name: expectedName })
  }

  it('sends BaoVe to monitoring when allowed', () => {
    expectLanding('BaoVe', ['monitoring'], 'Monitoring')
  })

  it('sends any user with dashboard task access to the dashboard', () => {
    expectLanding('BaoVe', ['dashboard'], 'Dashboard')
  })

  it('sends BaoVe to gate transit when monitoring is unavailable', () => {
    expectLanding('BaoVe', ['gate-transit'], 'GateTransitMonitor')
  })

  it('sends BaoVe to qr access monitor', () => {
    expectLanding('BaoVe', ['qr-access'], 'QrAccessMonitor')
  })

  it('sends BaoVe to vehicles for parking', () => {
    expectLanding('BaoVe', ['parking'], 'Vehicles')
  })

  it('sends BaoVe to lost found', () => {
    expectLanding('BaoVe', ['lost-found'], 'LostFoundDashboard')
  })

  it('sends QuanLy to reports', () => {
    expectLanding('QuanLy', ['reports'], 'AttendanceReports')
  })

  it('sends QuanLy to approvals', () => {
    expectLanding('QuanLy', ['approvals'], 'LeaveApprovals')
  })

  it('sends QuanLy to site hierarchy for metadata', () => {
    expectLanding('QuanLy', ['metadata'], 'SiteHierarchy')
  })

  it('sends QuanLy to dashboard as default', () => {
    expectLanding('QuanLy', [], 'Dashboard')
  })

  it('sends LeTan to reception', () => {
    expectLanding('LeTan', ['reception'], 'ReceptionDashboard')
  })

  it('sends LeTan to guest profiles', () => {
    expectLanding('LeTan', ['guest-support'], 'GuestProfiles')
  })

  it('sends LeTan to lost found', () => {
    expectLanding('LeTan', ['lost-found'], 'LostFoundDashboard')
  })

  it('sends NhanVien to my profile', () => {
    expectLanding('NhanVien', [], 'MyProfile')
  })

  it('sends NhanSu to employees', () => {
    expectLanding('NhanSu', [], 'Employees')
  })

  it('sends unknown role to dashboard', () => {
    expectLanding('MysteryRole', [], 'Dashboard')
  })
})

describe('router dynamic-import error handler', () => {
  const MARKER = 'Failed to fetch dynamically imported module'

  it('ignores errors unrelated to dynamic imports', () => {
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    vi.useFakeTimers()
    onErrorGuard(new Error('boom'), { fullPath: '/x' })
    expect(sessionStorage.getItem('vshield:dynamic-import-reload')).toBeNull()
    spy.mockRestore()
  })

  it('avoids reload loops for the same target', () => {
    vi.useFakeTimers()
    sessionStorage.setItem('vshield:dynamic-import-reload', '/employees')
    onErrorGuard(new Error(MARKER), { fullPath: '/employees' })
    expect(sessionStorage.getItem('vshield:dynamic-import-reload')).toBeNull()
  })

  it('reloads once for a new target and clears the guard key', () => {
    vi.useFakeTimers()
    const assign = vi.fn()
    const originalLocation = window.location
    Object.defineProperty(window, 'location', {
      value: { ...originalLocation, pathname: '/dashboard', assign },
      writable: true,
      configurable: true,
    })
    onErrorGuard(new Error(MARKER), { fullPath: '/employees' })
    expect(sessionStorage.getItem('vshield:dynamic-import-reload')).toBe('/employees')
    expect(assign).toHaveBeenCalledWith('/employees')
    vi.advanceTimersByTime(3000)
    expect(sessionStorage.getItem('vshield:dynamic-import-reload')).toBeNull()
  })

  it('falls back to the current path when the destination is unknown', () => {
    vi.useFakeTimers()
    const assign = vi.fn()
    const originalLocation = window.location
    Object.defineProperty(window, 'location', {
      value: { ...originalLocation, pathname: '/dashboard', assign },
      writable: true,
      configurable: true,
    })
    onErrorGuard(new Error(MARKER), {})
    expect(assign).toHaveBeenCalledWith('/dashboard')
  })

  it('returns early when window is undefined', () => {
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    vi.unstubAllGlobals()
    const originalLocation = window.location
    Object.defineProperty(window, 'location', {
      value: { ...originalLocation, assign: vi.fn(), pathname: '/x' },
      writable: true,
      configurable: true,
    })
    vi.stubGlobal('window', undefined)
    expect(() => onErrorGuard(new Error(MARKER), { fullPath: '/x' })).not.toThrow()
    spy.mockRestore()
  })
})
