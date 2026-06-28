import { createRouter, createWebHistory } from 'vue-router'
import { isLoggedIn, hasRole } from '../stores/auth'
import { authState } from '../stores/auth'

const Login = () => import('../pages/Login.vue')
const MainLayout = () => import('../components/Layout/MainLayout.vue')
const Dashboard = () => import('../pages/Dashboard.vue')
const AboutProject = () => import('../pages/AboutProject.vue')
const Employees = () => import('../pages/Employees.vue')
const Vehicles = () => import('../pages/Vehicles.vue')
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
const DynamicQrGenerator = () => import('../components/DynamicQrGenerator.vue')
const Exceptions = () => import('../pages/Exceptions.vue')
const RegistrationLinks = () => import('../pages/RegistrationLinks.vue')
const GuestProfiles = () => import('../pages/GuestProfiles.vue')
const DeviceManagement = () => import('../pages/DeviceManagement.vue')
const Biometrics = () => import('../pages/Biometrics.vue')
const SystemCatalog = () => import('../pages/SystemCatalog.vue')
const QrAccessMonitor = () => import('../components/QrAccessMonitor.vue')
const AccessPermissionManager = () => import('../components/AccessPermissionManager.vue')
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
const ManualParkingFallback = () => import('../pages/ManualParkingFallback.vue')
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
const GuideViewer = () => import('../pages/GuideViewer.vue')
const LostFoundDashboard = () => import('../pages/LostFoundDashboard.vue')

const ROUTE_NAME_DYNAMIC_QR_GENERATOR = 'DynamicQrGenerator'

function landingRouteForRole(role) {
    if (role === 'Staff') return { name: ROUTE_NAME_DYNAMIC_QR_GENERATOR }
    if (role === 'BaoVe') return { name: 'GateTransitMonitor' }
    if (role === 'QuanLy') return { name: 'Exceptions' }
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
            { path: 'dashboard', name: 'Dashboard', component: Dashboard, meta: { allowedRoles: ['Admin', 'BaoVe', 'QuanLy'] } },
            { path: 'monitoring', name: 'Monitoring', component: Monitoring, meta: { allowedRoles: ['Admin', 'BaoVe', 'QuanLy'], keepAlive: true } },
            { path: 'access-logs', name: 'AccessLogs', component: AccessLogs, meta: { allowedRoles: ['Admin', 'BaoVe', 'QuanLy'] } },
            { path: 'ueba', name: 'UEBA', component: UEBA, meta: { allowedRoles: ['Admin', 'BaoVe', 'QuanLy'] } },
            { path: 'system-audit-logs', name: 'SystemAuditLogs', component: SystemAuditLogs, meta: { allowedRoles: ['Admin', 'QuanLy'] } },
            { path: 'import-export-history', name: 'ImportExportHistory', component: ImportExportHistory, meta: { allowedRoles: ['Admin', 'QuanLy'] } },
            { path: 'enterprise-security', name: 'EnterpriseSecurityOperations', component: EnterpriseSecurityOperations, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'soc-console', name: 'SocAlarmConsole', component: SocAlarmConsole, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'identity-management', name: 'IdentityManagement', component: IdentityManagement, meta: { allowedRoles: ['Admin'] } },
            { path: 'site-hierarchy', name: 'SiteHierarchy', component: SiteHierarchy, meta: { allowedRoles: ['Admin', 'QuanLy'] } },
            { path: 'exceptions', name: 'Exceptions', component: Exceptions, meta: { allowedRoles: ['Admin', 'BaoVe', 'QuanLy'] } },
            { path: 'pre-registrations', name: 'PreRegistration', component: PreRegistration, meta: { allowedRoles: ['Admin'] } },
            { path: 'registration-links', name: 'RegistrationLinks', component: RegistrationLinks, meta: { allowedRoles: ['Admin'] } },
            { path: 'guest-profiles', name: 'GuestProfiles', component: GuestProfiles, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'reception', name: 'ReceptionDashboard', component: ReceptionDashboard, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'kiosk', name: 'ManualAccessFallback', component: ManualAccessFallback, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'parking-kiosk', name: 'ManualParkingFallback', component: ManualParkingFallback, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'host-visitor', name: 'HostVisitorPage', component: HostVisitorPage, meta: { allowedRoles: ['Admin', 'Staff', 'BaoVe'] } },
            { path: 'watchlist', name: 'WatchlistQueue', component: WatchlistQueue, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'contractors', name: 'ContractorManagement', component: ContractorManagement, meta: { allowedRoles: ['Admin'] } },
            { path: 'barrier-panel', name: 'BarrierPanel', component: BarrierPanel, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'device-topology', name: 'DeviceTopology', component: DeviceTopology, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'provisioning-wizard', name: 'ProvisioningWizard', component: ProvisioningWizard, meta: { allowedRoles: ['Admin'] } },
            { path: 'offline-packages', name: 'OfflinePackages', component: OfflinePackages, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'device-health', name: 'DeviceHealth', component: DeviceHealth, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'simulator-panel', name: 'SimulatorPanel', component: SimulatorPanel, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'video-search', name: 'VideoSearch', component: VideoSearch, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'ai-review-queue', name: 'AiReviewQueue', component: AiReviewQueue, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'correlation-view', name: 'CorrelationView', component: CorrelationView, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'camera-archive/:id', name: 'CameraArchive', component: CameraArchive, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'evidence-repository', name: 'EvidenceRepository', component: EvidenceRepository, meta: { allowedRoles: ['Admin'] } },
            { path: 'export-approval-queue', name: 'ExportApprovalQueue', component: ExportApprovalQueue, meta: { allowedRoles: ['Admin'] } },
            { path: 'redaction-queue', name: 'RedactionQueue', component: RedactionQueue, meta: { allowedRoles: ['Admin'] } },
            { path: 'compliance-reports', name: 'ComplianceReports', component: ComplianceReports, meta: { allowedRoles: ['Admin'] } },
            { path: 'about-project', name: 'AboutProject', component: AboutProject },
            { path: 'guide', name: 'GuideViewer', component: GuideViewer },
            { path: 'lost-found', name: 'LostFoundDashboard', component: LostFoundDashboard, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'found-items', name: 'FoundItemRegistry', redirect: { path: '/lost-found', query: { tab: 'found' } }, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'lost-items', name: 'LostItemList', redirect: { path: '/lost-found', query: { tab: 'lost' } }, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'claim-approval', name: 'ClaimApproval', redirect: { path: '/lost-found', query: { tab: 'claim' } }, meta: { allowedRoles: ['Admin'] } },
            { path: 'locker-manager', name: 'LockerManager', redirect: { path: '/lost-found', query: { tab: 'locker-config' } }, meta: { allowedRoles: ['Admin'] } },
            { path: 'locker-access-logs', name: 'LockerAccessLogs', redirect: { path: '/lost-found', query: { tab: 'locker' } }, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'license-plate-security', name: 'LicensePlateSecurity', component: LicensePlateSecurity, meta: { allowedRoles: ['Admin', 'BaoVe'], keepAlive: true } },
            { path: 'gate-transit-monitor', name: 'GateTransitMonitor', component: GatePassageMonitor, meta: { allowedRoles: ['Admin', 'BaoVe'], keepAlive: true } },
            { path: 'dynamic-qr-generator', name: ROUTE_NAME_DYNAMIC_QR_GENERATOR, component: DynamicQrGenerator, meta: { allowedRoles: ['Admin', 'Staff', 'BaoVe'], keepAlive: true } },
            { path: 'qr-access-monitor', name: 'QrAccessMonitor', component: QrAccessMonitor, meta: { allowedRoles: ['Admin', 'BaoVe'], keepAlive: true } },
            { path: 'access-permission-manager', name: 'AccessPermissionManager', component: AccessPermissionManager, meta: { allowedRoles: ['Admin', 'BaoVe'], keepAlive: true } },
            { path: 'employees', name: 'Employees', component: Employees, meta: { allowedRoles: ['Admin'] } },
            { path: 'vehicles', name: 'Vehicles', component: Vehicles, meta: { allowedRoles: ['Admin', 'BaoVe', 'QuanLy'] } },
            { path: 'attendance/records', name: 'AttendanceRecords', component: AttendanceRecords, meta: { allowedRoles: ['Admin', 'Staff', 'BaoVe', 'QuanLy'] } },
            { path: 'attendance/work-schedules', name: 'AttendanceWorkSchedules', component: AttendanceWorkSchedules, meta: { allowedRoles: ['Admin', 'Staff'] } },
            { path: 'attendance/shifts', name: 'AttendanceShifts', component: AttendanceShifts, meta: { allowedRoles: ['Admin', 'Staff'] } },
            { path: 'attendance/leave-requests', name: 'LeaveRequests', component: LeaveRequests, meta: { allowedRoles: ['Admin', 'Staff'] } },
            { path: 'attendance/leave-approvals', name: 'LeaveApprovals', component: LeaveApprovals, meta: { allowedRoles: ['Admin', 'Staff'] } },
            { path: 'attendance/reports', name: 'AttendanceReports', component: AttendanceReports, meta: { allowedRoles: ['Admin', 'Staff', 'QuanLy'] } },
            { path: 'campus-map', name: 'CampusMap', component: CampusMapPage, meta: { allowedRoles: ['Admin', 'Staff', 'BaoVe'] } },
            { path: 'device-management', name: 'DeviceManagement', component: DeviceManagement, meta: { allowedRoles: ['Admin', 'BaoVe'], keepAlive: true } },
            {
                path: 'system-catalog',
                name: 'SystemCatalog',
                component: SystemCatalog,
                meta: { allowedRoles: ['Admin', 'QuanLy'] },
            },
            {
                path: 'departments-positions',
                name: 'DepartmentPosition',
                component: DepartmentPosition,
                meta: { allowedRoles: ['Admin', 'QuanLy'] },
            },
            {
                path: 'users',
                name: 'UserManagement',
                component: UserManagement,
                meta: { requiresAdmin: true, allowedRoles: ['Admin'] },
            },
            {
                path: 'settings',
                name: 'Settings',
                component: Settings,
                meta: { allowedRoles: ['Admin'] },
            },
        ],
    },
]

const router = createRouter({
    history: createWebHistory(),
    routes,
})

// Navigation Guard
router.beforeEach((to, from, next) => {
    // Nếu route yêu cầu đăng nhập
    if (to.matched.some(matchedRoute => matchedRoute.meta.requiresAuth)) {
        if (!isLoggedIn()) {
            return next({ name: 'Login', query: { redirect: to.fullPath } })
        }
    }

    // Nếu route yêu cầu Admin
    if (to.matched.some(matchedRoute => matchedRoute.meta.requiresAdmin)) {
        if (!hasRole('Admin')) {
            const currentRole = authState.user?.role
            return next(landingRouteForRole(currentRole))
        }
    }

    // Kiểm tra allowedRoles
    const allowedRoles = to.matched.find(matchedRoute => matchedRoute.meta.allowedRoles)?.meta.allowedRoles
    if (allowedRoles) {
        const currentRole = authState.user?.role
        if (!allowedRoles.includes(currentRole)) {
            return next(landingRouteForRole(currentRole))
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

export default router

