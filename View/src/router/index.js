import { createRouter, createWebHistory } from 'vue-router'
import { isLoggedIn, hasRole } from '../stores/auth'
import { authState } from '../stores/auth'

const Login = () => import('../pages/Login.vue')
const ForcePasswordChange = () => import('../pages/ForcePasswordChange.vue')
const MainLayout = () => import('../components/Layout/MainLayout.vue')
const Dashboard = () => import('../pages/Dashboard.vue')
const AboutProject = () => import('../pages/AboutProject.vue')
const Employees = () => import('../pages/Employees.vue')
const Vehicles = () => import('../pages/Vehicles.vue')
const MyVehicles = () => import('../pages/MyVehicles.vue')
const MySchedule = () => import('../pages/MySchedule.vue')
const MyProfile = () => import('../pages/MyProfile.vue')
const VehicleTransfer = () => import('../pages/VehicleTransfer.vue')
const AccessLogs = () => import('../pages/AccessLogs.vue')
const Monitoring = () => import('../pages/Monitoring.vue')
const Settings = () => import('../pages/Settings.vue')
const UserManagement = () => import('../pages/UserManagement.vue')
const DepartmentPosition = () => import('../pages/DepartmentPosition.vue')
const PreRegistration = () => import('../pages/PreRegistration.vue')
const GuestRegister = () => import('../pages/GuestRegister.vue')
const VisitorPass = () => import('../pages/VisitorPass.vue')
const FaceIdSecurity = () => import('../components/FaceCamera.vue')
const LicensePlateSecurity = () => import('../components/LicensePlateSecurity.vue')
const FaceVideo = () => import('../components/FaceVideo.vue')
const GatePassageMonitor = () => import('../components/GateTransitMonitor.vue')
const FacePlateTransitMonitor = () => import('../components/ThongHanh.vue')
const DynamicQrGenerator = () => import('../components/DynamicQrGenerator.vue')
const Exceptions = () => import('../pages/Exceptions.vue')
const RegistrationLinks = () => import('../pages/RegistrationLinks.vue')
const GuestProfiles = () => import('../pages/GuestProfiles.vue')
const DeviceManagement = () => import('../pages/DeviceManagement.vue')
const Biometrics = () => import('../pages/Biometrics.vue')
const SystemCatalog = () => import('../pages/SystemCatalog.vue')
const QrAccessMonitor = () => import('../components/QrAccessMonitor.vue')
const SystemAuditLogs = () => import('../pages/SystemAuditLogs.vue')
const AttendanceShifts = () => import('../pages/AttendanceShifts.vue')
const AttendanceWorkSchedules = () => import('../pages/AttendanceWorkSchedules.vue')
const AttendanceRecords = () => import('../pages/AttendanceRecords.vue')
const LeaveRequests = () => import('../pages/LeaveRequests.vue')
const LeaveApprovals = () => import('../pages/LeaveApprovals.vue')
const AttendanceReports = () => import('../pages/AttendanceReports.vue')
const EnterpriseSecurityOperations = () => import('../pages/EnterpriseSecurityOperations.vue')
const ImportExportHistory = () => import('../pages/ImportExportHistory.vue')
const CampusMapPage = () => import('../pages/CampusMapPage.vue')
const UEBA = () => import('../pages/UEBA.vue')
const SocAlarmConsole = () => import('../pages/SocAlarmConsole.vue')
const IdentityManagement = () => import('../pages/IdentityManagement.vue')
const SiteHierarchy = () => import('../pages/SiteHierarchy.vue')
const ReceptionDashboard = () => import('../pages/ReceptionDashboard.vue')
const ManualAccessFallback = () => import('../pages/ManualAccessFallback.vue')
const ManualParkingConsole = () => import('../pages/ManualParkingConsole.vue')
const KioskCheckIn = () => import('../pages/KioskCheckIn.vue')
const HostVisitorPage = () => import('../pages/HostVisitorPage.vue')
const WatchlistQueue = () => import('../pages/WatchlistQueue.vue')
const ContractorManagement = () => import('../pages/ContractorManagement.vue')
const BarrierPanel = () => import('../pages/BarrierPanel.vue')
const DeviceTopology = () => import('../pages/DeviceTopology.vue')
const ProvisioningWizard = () => import('../pages/ProvisioningWizard.vue')
const OfflinePackages = () => import('../pages/OfflinePackages.vue')
const DeviceHealth = () => import('../pages/DeviceHealth.vue')
const SimulatorPanel = () => import('../pages/SimulatorPanel.vue')
const VideoSearch = () => import('../pages/VideoSearch.vue')
const AiReviewQueue = () => import('../pages/AiReviewQueue.vue')
const CorrelationView = () => import('../pages/CorrelationView.vue')
const EvidenceRepository = () => import('../pages/EvidenceRepository.vue')
const CameraArchive = () => import('../pages/CameraArchive.vue')
const ExportApprovalQueue = () => import('../pages/ExportApprovalQueue.vue')
const RedactionQueue = () => import('../pages/RedactionQueue.vue')
const ComplianceReports = () => import('../pages/ComplianceReports.vue')
const LostFoundDashboard = () => import('../pages/LostFoundDashboard.vue')
const Chat = () => import('../pages/Chat.vue')
const RolePermissions = () => import('../pages/RolePermissions.vue')
const NotificationRuleEditor = () => import('../pages/NotificationRuleEditor.vue')
const IncidentMapPage = () => import('../pages/IncidentMapPage.vue')
const OperationsDashboard = () => import('../pages/OperationsDashboard.vue')
const SIEMExportStatus = () => import('../pages/SIEMExportStatus.vue')
const BackupRestoreDrillDashboard = () => import('../pages/BackupRestoreDrillDashboard.vue')
const WebhookDeliveryViewer = () => import('../pages/WebhookDeliveryViewer.vue')
const VulnerabilityReleaseGateStatus = () => import('../pages/VulnerabilityReleaseGateStatus.vue')

const ROUTE_NAME_DYNAMIC_QR_GENERATOR = 'DynamicQrGenerator'

function userCanAccessTask(user, taskKey) {
    if (!user) return false
    if (user.role === 'Admin') return true
    if (!taskKey) return true
    return (user.operationalTaskKeys || []).includes(taskKey)
}

function landingRouteForRole(role) {
    const user = authState.user

    if (userCanAccessTask(user, 'dashboard')) {
        return { name: 'Dashboard' }
    }

    if (role === 'BaoVe') {
        if (userCanAccessTask(user, 'monitoring')) return { name: 'Monitoring' }
        if (userCanAccessTask(user, 'gate-transit')) return { name: 'GateTransitMonitor' }
        if (userCanAccessTask(user, 'qr-access')) return { name: 'QrAccessMonitor' }
        if (userCanAccessTask(user, 'parking')) return { name: 'Vehicles' }
        if (userCanAccessTask(user, 'lost-found')) return { name: 'LostFoundDashboard' }
    }

    if (role === 'QuanLy') {
        if (userCanAccessTask(user, 'reports')) return { name: 'AttendanceReports' }
        if (userCanAccessTask(user, 'approvals')) return { name: 'LeaveApprovals' }
        if (userCanAccessTask(user, 'metadata')) return { name: 'SiteHierarchy' }
        return { name: 'Dashboard' }
    }

    if (role === 'LeTan') {
        if (userCanAccessTask(user, 'reception')) return { name: 'ReceptionDashboard' }
        if (userCanAccessTask(user, 'guest-support')) return { name: 'GuestProfiles' }
        if (userCanAccessTask(user, 'lost-found')) return { name: 'LostFoundDashboard' }
    }

    if (role === 'NhanVien') {
        return { name: 'MyProfile' }
    }

    if (role === 'NhanSu') {
        return { name: 'Employees' }
    }

    return { name: 'Dashboard' }
}

const routes = [
    {
        path: '/login',
        name: 'Login',
        component: Login,
        meta: { guest: true },
    },
    {
        path: '/force-password-change',
        name: 'ForcePasswordChange',
        component: ForcePasswordChange,
        meta: { requiresAuth: true },
    },
    {
        path: '/register/:token',
        name: 'GuestRegister',
        component: GuestRegister,
        meta: { guest: true },
    },
    {
        path: '/visitor-pass/:token',
        name: 'VisitorPass',
        component: VisitorPass,
        meta: { guest: true },
    },
    {
        path: '/',
        component: MainLayout,
        meta: { requiresAuth: true },
        children: [
            { path: '', redirect: () => {
                return landingRouteForRole(authState.user?.role)
            }},
            { path: 'dashboard', name: 'Dashboard', component: Dashboard, meta: { allowedRoles: ['Admin', 'QuanLy'], taskKey: 'dashboard' } },
            { path: 'monitoring', name: 'Monitoring', component: Monitoring, meta: { allowedRoles: ['Admin', 'BaoVe'], taskKey: 'monitoring', keepAlive: true } },
            { path: 'monitoring/face-camera', name: 'FaceCamera', component: FaceIdSecurity, meta: { allowedRoles: ['Admin', 'BaoVe'], taskKey: 'monitoring', keepAlive: true } },
            { path: 'access-logs', name: 'AccessLogs', component: AccessLogs, meta: { allowedRoles: ['Admin', 'BaoVe', 'QuanLy'], taskKey: 'access-logs' } },
            { path: 'ueba', name: 'UEBA', component: UEBA, meta: { allowedRoles: ['Admin', 'BaoVe', 'QuanLy'], taskKey: 'monitoring' } },
            { path: 'system-audit-logs', name: 'SystemAuditLogs', component: SystemAuditLogs, meta: { allowedRoles: ['Admin', 'QuanLy'], taskKey: 'access-logs' } },
            { path: 'import-export-history', name: 'ImportExportHistory', component: ImportExportHistory, meta: { allowedRoles: ['Admin', 'QuanLy'], taskKey: 'metadata' } },
            { path: 'enterprise-security', name: 'EnterpriseSecurityOperations', component: EnterpriseSecurityOperations, meta: { allowedRoles: ['Admin'], taskKey: 'identity-mgmt' } },
            { path: 'soc-console', name: 'SocAlarmConsole', component: SocAlarmConsole, meta: { allowedRoles: ['Admin', 'BaoVe'], taskKey: 'monitoring' } },
            { path: 'incident-map/:alarmId?', name: 'IncidentMap', component: IncidentMapPage, meta: { allowedRoles: ['Admin', 'BaoVe', 'QuanLy'], taskKey: 'monitoring' } },
            { path: 'identity-management', name: 'IdentityManagement', component: IdentityManagement, meta: { allowedRoles: ['Admin'], taskKey: 'identity-mgmt' } },
            { path: 'site-hierarchy', name: 'SiteHierarchy', component: SiteHierarchy, meta: { allowedRoles: ['Admin', 'QuanLy'], taskKey: 'metadata' } },
            { path: 'exceptions', name: 'Exceptions', component: Exceptions, meta: { allowedRoles: ['Admin', 'BaoVe', 'QuanLy'], taskKey: 'monitoring' } },
            { path: 'pre-registrations', name: 'PreRegistration', component: PreRegistration, meta: { allowedRoles: ['Admin'], taskKey: 'guest-support' } },
            { path: 'registration-links', name: 'RegistrationLinks', component: RegistrationLinks, meta: { allowedRoles: ['Admin'], taskKey: 'guest-support' } },
            { path: 'guest-profiles', name: 'GuestProfiles', component: GuestProfiles, meta: { allowedRoles: ['Admin', 'LeTan'], taskKey: 'guest-support' } },
            { path: 'reception', name: 'ReceptionDashboard', component: ReceptionDashboard, meta: { allowedRoles: ['Admin', 'LeTan'], taskKey: 'reception' } },
            { path: 'kiosk', name: 'ManualAccessFallback', component: ManualAccessFallback, meta: { allowedRoles: ['Admin', 'BaoVe'], taskKey: 'qr-access' } },
            { path: 'kiosk-checkin', name: 'KioskCheckIn', component: KioskCheckIn, meta: { allowedRoles: ['Admin', 'LeTan'], taskKey: 'reception' } },
            { path: 'parking-kiosk', name: 'ManualParkingConsole', component: ManualParkingConsole, meta: { allowedRoles: ['Admin', 'BaoVe'], taskKey: 'parking' } },
            { path: 'host-visitor', name: 'HostVisitorPage', component: HostVisitorPage, meta: { allowedRoles: ['Admin', 'LeTan'], taskKey: 'guest-support' } },
            { path: 'watchlist', name: 'WatchlistQueue', component: WatchlistQueue, meta: { allowedRoles: ['Admin', 'BaoVe'], taskKey: 'monitoring' } },
            { path: 'contractors', name: 'ContractorManagement', component: ContractorManagement, meta: { allowedRoles: ['Admin'], taskKey: 'contractor-mgmt' } },
            { path: 'barrier-panel', name: 'BarrierPanel', component: BarrierPanel, meta: { allowedRoles: ['Admin', 'BaoVe'], taskKey: 'parking' } },
            { path: 'device-topology', name: 'DeviceTopology', component: DeviceTopology, meta: { allowedRoles: ['Admin'], taskKey: 'device-mgmt' } },
            { path: 'provisioning-wizard', name: 'ProvisioningWizard', component: ProvisioningWizard, meta: { allowedRoles: ['Admin'], taskKey: 'device-mgmt' } },
            { path: 'offline-packages', name: 'OfflinePackages', component: OfflinePackages, meta: { allowedRoles: ['Admin'], taskKey: 'device-mgmt' } },
            { path: 'device-health', name: 'DeviceHealth', component: DeviceHealth, meta: { allowedRoles: ['Admin'], taskKey: 'device-mgmt' } },
            { path: 'simulator-panel', name: 'SimulatorPanel', component: SimulatorPanel, meta: { allowedRoles: ['Admin'], taskKey: 'device-mgmt' } },
            { path: 'video-search', name: 'VideoSearch', component: VideoSearch, meta: { allowedRoles: ['Admin', 'BaoVe'], taskKey: 'monitoring' } },
            { path: 'ai-review-queue', name: 'AiReviewQueue', component: AiReviewQueue, meta: { allowedRoles: ['Admin', 'BaoVe'], taskKey: 'monitoring' } },
            { path: 'correlation-view', name: 'CorrelationView', component: CorrelationView, meta: { allowedRoles: ['Admin', 'BaoVe'], taskKey: 'monitoring' } },
            { path: 'camera-archive/:id?', name: 'CameraArchive', component: CameraArchive, meta: { allowedRoles: ['Admin', 'BaoVe'], taskKey: 'monitoring' } },
            { path: 'evidence-repository', name: 'EvidenceRepository', component: EvidenceRepository, meta: { allowedRoles: ['Admin'], taskKey: 'evidence-mgmt' } },
            { path: 'export-approval-queue', name: 'ExportApprovalQueue', component: ExportApprovalQueue, meta: { allowedRoles: ['Admin'], taskKey: 'evidence-mgmt' } },
            { path: 'redaction-queue', name: 'RedactionQueue', component: RedactionQueue, meta: { allowedRoles: ['Admin'], taskKey: 'evidence-mgmt' } },
            { path: 'compliance-reports', name: 'ComplianceReports', component: ComplianceReports, meta: { allowedRoles: ['Admin'], taskKey: 'evidence-mgmt' } },
            { path: 'about-project', name: 'AboutProject', component: AboutProject },
            { path: 'event-timeline', redirect: { path: '/soc-console', query: { tab: 'timeline' } }, meta: { allowedRoles: ['Admin', 'BaoVe'], taskKey: 'monitoring' } },
            { path: 'lane-dashboard', redirect: { path: '/gate-transit-monitor' }, meta: { allowedRoles: ['Admin', 'BaoVe'], taskKey: 'gate-transit' } },
            { path: 'lost-found', name: 'LostFoundDashboard', component: LostFoundDashboard, meta: { allowedRoles: ['Admin', 'BaoVe', 'LeTan'], taskKey: 'lost-found' } },
            { path: 'found-items', name: 'FoundItemRegistry', redirect: { path: '/lost-found', query: { tab: 'found' } }, meta: { allowedRoles: ['Admin', 'BaoVe', 'LeTan'], taskKey: 'lost-found' } },
            { path: 'lost-items', name: 'LostItemList', redirect: { path: '/lost-found', query: { tab: 'lost' } }, meta: { allowedRoles: ['Admin', 'BaoVe', 'LeTan'], taskKey: 'lost-found' } },
            { path: 'claim-approval', name: 'ClaimApproval', redirect: { path: '/lost-found', query: { tab: 'claim' } }, meta: { allowedRoles: ['Admin', 'BaoVe', 'LeTan'], taskKey: 'lost-found' } },
            { path: 'locker-manager', name: 'LockerManager', redirect: { path: '/lost-found', query: { tab: 'locker-config' } }, meta: { allowedRoles: ['Admin'], taskKey: 'lost-found' } },
            { path: 'locker-access-logs', name: 'LockerAccessLogs', redirect: { path: '/lost-found', query: { tab: 'locker' } }, meta: { allowedRoles: ['Admin', 'BaoVe'], taskKey: 'lost-found' } },
            { path: 'license-plate-security', name: 'LicensePlateSecurity', component: LicensePlateSecurity, meta: { allowedRoles: ['Admin', 'BaoVe'], taskKey: 'parking', keepAlive: true } },
            { path: 'gate-transit-monitor', name: 'GateTransitMonitor', component: GatePassageMonitor, meta: { allowedRoles: ['Admin', 'BaoVe'], taskKey: 'gate-transit', keepAlive: true } },
            { path: 'gate-face-transit-monitor', name: 'FacePlateTransitMonitor', component: FacePlateTransitMonitor, meta: { allowedRoles: ['Admin', 'BaoVe'], taskKey: 'gate-transit', keepAlive: true } },
            { path: 'dynamic-qr-generator', name: ROUTE_NAME_DYNAMIC_QR_GENERATOR, component: DynamicQrGenerator, meta: { allowedRoles: ['Admin'], taskKey: 'qr-access', keepAlive: true } },
            { path: 'qr-access-monitor', name: 'QrAccessMonitor', component: QrAccessMonitor, meta: { allowedRoles: ['Admin', 'BaoVe'], taskKey: 'qr-access', keepAlive: true } },
            { path: 'employees', name: 'Employees', component: Employees, meta: { allowedRoles: ['Admin', 'NhanSu'], taskKey: 'employee-directory' } },
            { path: 'vehicles', name: 'Vehicles', component: Vehicles, meta: { allowedRoles: ['Admin', 'BaoVe'], taskKey: 'parking' } },
            { path: 'my-vehicles', name: 'MyVehicles', component: MyVehicles, meta: { allowedRoles: ['NhanVien'] } },
            { path: 'my-schedule', name: 'MySchedule', component: MySchedule, meta: { allowedRoles: ['NhanVien'] } },
            { path: 'my-face-id', name: 'MyFaceId', component: () => import('../pages/MyFaceId.vue') },
            { path: 'profile', name: 'MyProfile', component: MyProfile, meta: { allowedRoles: ['NhanVien', 'NhanSu'] } },
            { path: 'vehicle-transfer', name: 'VehicleTransfer', component: VehicleTransfer, meta: { allowedRoles: ['NhanVien'] } },
            { path: 'my-dynamic-qr', name: 'MyDynamicQr', component: DynamicQrGenerator, meta: { allowedRoles: ['NhanVien', 'NhanSu'] } },
            { path: 'chat', name: 'Chat', component: Chat, meta: { allowedRoles: ['Admin', 'NhanVien', 'NhanSu', 'QuanLy', 'BaoVe', 'LeTan'] } },
            { path: 'attendance/records', name: 'AttendanceRecords', component: AttendanceRecords, meta: { allowedRoles: ['Admin'], taskKey: 'reports' } },
            { path: 'attendance/work-schedules', name: 'AttendanceWorkSchedules', component: AttendanceWorkSchedules, meta: { allowedRoles: ['Admin', 'QuanLy'], taskKey: 'metadata' } },
            { path: 'attendance/shifts', name: 'AttendanceShifts', component: AttendanceShifts, meta: { allowedRoles: ['Admin', 'QuanLy'], taskKey: 'metadata' } },
            { path: 'attendance/leave-requests', name: 'LeaveRequests', component: LeaveRequests, meta: { allowedRoles: ['Admin', 'NhanVien'] } },
            { path: 'attendance/leave-approvals', name: 'LeaveApprovals', component: LeaveApprovals, meta: { allowedRoles: ['Admin', 'QuanLy', 'NhanSu'], taskKey: 'approvals' } },
            { path: 'attendance/reports', name: 'AttendanceReports', component: AttendanceReports, meta: { allowedRoles: ['Admin', 'QuanLy'], taskKey: 'reports' } },
            { path: 'campus-map', name: 'CampusMap', component: CampusMapPage, meta: { allowedRoles: ['Admin', 'LeTan'], taskKey: 'reception' } },
            { path: 'device-management', name: 'DeviceManagement', component: DeviceManagement, meta: { allowedRoles: ['Admin'], taskKey: 'device-mgmt', keepAlive: true } },
            {
                path: 'system-catalog',
                name: 'SystemCatalog',
                component: SystemCatalog,
                meta: { allowedRoles: ['Admin', 'QuanLy'], taskKey: 'metadata' },
            },
            {
                path: 'departments-positions',
                name: 'DepartmentPosition',
                component: DepartmentPosition,
                meta: { allowedRoles: ['Admin', 'QuanLy'], taskKey: 'metadata' },
            },
            {
                path: 'role-permissions',
                name: 'RolePermissions',
                component: RolePermissions,
                meta: { allowedRoles: ['Admin', 'NhanSu'], taskKey: 'user-admin' },
            },
            {
                path: 'users',
                name: 'UserManagement',
                component: UserManagement,
                meta: { allowedRoles: ['Admin', 'NhanSu'], taskKey: 'user-admin' },
            },
            {
                path: 'settings',
                name: 'Settings',
                component: Settings,
                meta: { allowedRoles: ['Admin'], taskKey: 'system-config' },
            },
            {
                path: 'settings/notification-rules',
                name: 'NotificationRules',
                component: NotificationRuleEditor,
                meta: { allowedRoles: ['Admin', 'QuanLy'], taskKey: 'system-config' },
            },
            { path: 'operations-dashboard', name: 'OperationsDashboard', component: OperationsDashboard, meta: { allowedRoles: ['Admin', 'QuanLy'], taskKey: 'dashboard' } },
            { path: 'siem-export-status', name: 'SIEMExportStatus', component: SIEMExportStatus, meta: { allowedRoles: ['Admin'], taskKey: 'system-config' } },
            { path: 'backup-restore-drill', name: 'BackupRestoreDrillDashboard', component: BackupRestoreDrillDashboard, meta: { allowedRoles: ['Admin'], taskKey: 'system-config' } },
            { path: 'webhook-delivery-viewer', name: 'WebhookDeliveryViewer', component: WebhookDeliveryViewer, meta: { allowedRoles: ['Admin'], taskKey: 'system-config' } },
            { path: 'vulnerability-release-gate', name: 'VulnerabilityReleaseGateStatus', component: VulnerabilityReleaseGateStatus, meta: { allowedRoles: ['Admin'], taskKey: 'system-config' } },
        ],
    },
]

const router = createRouter({
    history: createWebHistory(),
    routes,
})

const DYNAMIC_IMPORT_ERROR_MARKER = 'Failed to fetch dynamically imported module'
const DYNAMIC_IMPORT_RELOAD_KEY = 'vshield:dynamic-import-reload'

// Navigation Guard
router.beforeEach((to, from, next) => {
    // Nếu route yêu cầu đăng nhập
    if (to.matched.some(matchedRoute => matchedRoute.meta.requiresAuth)) {
        if (!isLoggedIn()) {
            return next({ name: 'Login', query: { redirect: to.fullPath } })
        }
    }

    // Khi tài khoản đang ở trạng thái bắt buộc đổi mật khẩu (vừa kích hoạt MFA lần đầu)
    // thì mọi điều hướng đều phải đi qua trang đổi mật khẩu trước khi vào hệ thống.
    if (isLoggedIn() && authState.user?.requiresPasswordChange && to.name !== 'ForcePasswordChange') {
        if (to.name === 'Login') {
            return next({ name: 'ForcePasswordChange' })
        }
        return next({ name: 'ForcePasswordChange', query: { redirect: to.fullPath } })
    }

    // Nếu route yêu cầu Admin
    if (to.matched.some(matchedRoute => matchedRoute.meta.requiresAdmin)) {
        if (!hasRole('Admin')) {
            const currentRole = authState.user?.role
            return next(landingRouteForRole(currentRole))
        }
    }

    // Kiểm tra allowedRoles
    const roleMatchedRoute = to.matched.find(matchedRoute => matchedRoute.meta.allowedRoles)
    const allowedRoles = roleMatchedRoute?.meta.allowedRoles
    const requiredTaskKey = to.matched.find(matchedRoute => matchedRoute.meta.taskKey)?.meta.taskKey
    if (allowedRoles) {
        const currentRole = authState.user?.role
        const roleAllowed = allowedRoles.includes(currentRole)
        const taskAllowed = requiredTaskKey ? userCanAccessTask(authState.user, requiredTaskKey) : false
        if (!roleAllowed && !taskAllowed) {
            return next(landingRouteForRole(currentRole))
        }
    }

    if (requiredTaskKey) {
        const currentUser = authState.user
        if (!userCanAccessTask(currentUser, requiredTaskKey)) {
            return next(landingRouteForRole(currentUser?.role))
        }
    }

    // Nếu đã đăng nhập mà vào trang login thì redirect
    // Nhưng cho phép truy cập trang đăng ký khách (GuestRegister) dù đã đăng nhập
    if (to.meta.guest && isLoggedIn() && to.name !== 'GuestRegister' && to.name !== 'VisitorPass') {
        const currentRole = authState.user?.role
        return next(landingRouteForRole(currentRole))
    }

    next()
})

router.onError((error, to) => {
  console.error('Router navigation error:', error)

  const message = String(error?.message || '')
  if (!message.includes(DYNAMIC_IMPORT_ERROR_MARKER)) {
    return
  }

  if (typeof window === 'undefined') {
    return
  }

  const reloadTarget = typeof to?.fullPath === 'string' && to.fullPath ? to.fullPath : window.location.pathname
  const lastReloadTarget = window.sessionStorage.getItem(DYNAMIC_IMPORT_RELOAD_KEY)

  if (lastReloadTarget === reloadTarget) {
    // Đã thử reload target này; đặt lại khoá để lần sau thử lại, tránh kẹt vĩnh viễn.
    window.sessionStorage.removeItem(DYNAMIC_IMPORT_RELOAD_KEY)
    return
  }

  window.sessionStorage.setItem(DYNAMIC_IMPORT_RELOAD_KEY, reloadTarget)
  window.setTimeout(() => {
    window.sessionStorage.removeItem(DYNAMIC_IMPORT_RELOAD_KEY)
  }, 3000)
  window.location.assign(reloadTarget)
})

export default router
