import { createRouter, createWebHistory } from 'vue-router'
import { isLoggedIn, hasRole } from '../stores/auth'
import { authState } from '../stores/auth'

import Login from '../pages/Login.vue'
import MainLayout from '../components/Layout/MainLayout.vue'
import Dashboard from '../pages/Dashboard.vue'
import AboutProject from '../pages/AboutProject.vue'
import Employees from '../pages/Employees.vue'
import Vehicles from '../pages/Vehicles.vue'
import AccessLogs from '../pages/AccessLogs.vue'
import Monitoring from '../pages/Monitoring.vue'
import Settings from '../pages/Settings.vue'
import UserManagement from '../pages/UserManagement.vue'
import DepartmentPosition from '../pages/DepartmentPosition.vue'
import PreRegistration from '../pages/PreRegistration.vue'
import GuestRegister from '../pages/GuestRegister.vue'

import FaceIdSecurity from '../components/FaceCamera.vue'
import LicensePlateSecurity from '../components/LicensePlateSecurity.vue'
import FaceVideo from '../components/FaceVideo.vue'
import GatePassageMonitor from '../components/GateTransitMonitor.vue'
import DynamicQrGenerator from '../components/DynamicQrGenerator.vue'
import DynamicQrScanner from '../components/DynamicQrScanner.vue'
import Exceptions from '../pages/Exceptions.vue'
import RegistrationLinks from '../pages/RegistrationLinks.vue'
import GuestProfiles from '../pages/GuestProfiles.vue'
import DeviceManagement from '../pages/DeviceManagement.vue'
import Biometrics from '../pages/Biometrics.vue'
import SystemCatalog from '../pages/SystemCatalog.vue'
import QrAccessMonitor from '../components/QrAccessMonitor.vue'
import AccessPermissionManager from '../components/AccessPermissionManager.vue'

const ROUTE_NAME_DYNAMIC_QR_GENERATOR = 'DynamicQrGenerator'

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
        path: '/',
        component: MainLayout,
        meta: { requiresAuth: true },
        children: [
            { path: '', redirect: () => {
                const currentRole = authState.user?.role
                if (currentRole === 'Staff') return { name: ROUTE_NAME_DYNAMIC_QR_GENERATOR }
                return { name: 'Dashboard' }
            }},
            { path: 'dashboard', name: 'Dashboard', component: Dashboard, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'monitoring', name: 'Monitoring', component: Monitoring, meta: { allowedRoles: ['Admin', 'BaoVe'], keepAlive: true } },
            { path: 'access-logs', name: 'AccessLogs', component: AccessLogs, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'exceptions', name: 'Exceptions', component: Exceptions, meta: { allowedRoles: ['Admin', 'BaoVe'] } },
            { path: 'pre-registrations', name: 'PreRegistration', component: PreRegistration, meta: { allowedRoles: ['Admin'] } },
            { path: 'registration-links', name: 'RegistrationLinks', component: RegistrationLinks, meta: { allowedRoles: ['Admin'] } },
            { path: 'guest-profiles', name: 'GuestProfiles', component: GuestProfiles, meta: { allowedRoles: ['Admin'] } },
            { path: 'about-project', name: 'AboutProject', component: AboutProject },
            { path: 'face-id-security', name: 'FaceIdSecurity', component: FaceIdSecurity, meta: { allowedRoles: ['Admin', 'BaoVe'], keepAlive: true } },
            { path: 'license-plate-security', name: 'LicensePlateSecurity', component: LicensePlateSecurity, meta: { allowedRoles: ['Admin', 'BaoVe'], keepAlive: true } },
            { path: 'face-video-monitor', name: 'FaceVideoMonitor', component: FaceVideo, meta: { allowedRoles: ['Admin', 'BaoVe'], keepAlive: true } },
            { path: 'gate-transit-monitor', name: 'GateTransitMonitor', component: GatePassageMonitor, meta: { allowedRoles: ['Admin', 'BaoVe'], keepAlive: true } },
            { path: 'dynamic-qr-generator', name: ROUTE_NAME_DYNAMIC_QR_GENERATOR, component: DynamicQrGenerator, meta: { allowedRoles: ['Admin', 'Staff', 'BaoVe'], keepAlive: true } },
            { path: 'dynamic-qr-scanner', name: 'DynamicQrScanner', component: DynamicQrScanner, meta: { allowedRoles: ['Admin', 'BaoVe'], keepAlive: true } },
            { path: 'qr-access-monitor', name: 'QrAccessMonitor', component: QrAccessMonitor, meta: { allowedRoles: ['Admin', 'BaoVe'], keepAlive: true } },
            { path: 'access-permission-manager', name: 'AccessPermissionManager', component: AccessPermissionManager, meta: { allowedRoles: ['Admin', 'BaoVe'], keepAlive: true } },
            { path: 'biometrics', name: 'Biometrics', component: Biometrics, meta: { allowedRoles: ['Admin'] } },
            { path: 'employees', name: 'Employees', component: Employees, meta: { allowedRoles: ['Admin'] } },
            { path: 'vehicles', name: 'Vehicles', component: Vehicles, meta: { allowedRoles: ['Admin'] } },
            { path: 'device-management', name: 'DeviceManagement', component: DeviceManagement, meta: { allowedRoles: ['Admin'], keepAlive: true } },
            {
                path: 'system-catalog',
                name: 'SystemCatalog',
                component: SystemCatalog,
                meta: { requiresAdmin: true, allowedRoles: ['Admin'] },
            },
            {
                path: 'departments-positions',
                name: 'DepartmentPosition',
                component: DepartmentPosition,
                meta: { requiresAdmin: true, allowedRoles: ['Admin'] },
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
    // Náº¿u route yÃªu cáº§u Ä‘Äƒng nháº­p
    if (to.matched.some(matchedRoute => matchedRoute.meta.requiresAuth)) {
        if (!isLoggedIn()) {
            return next({ name: 'Login', query: { redirect: to.fullPath } })
        }
    }

    // Náº¿u route yÃªu cáº§u Admin
    if (to.matched.some(matchedRoute => matchedRoute.meta.requiresAdmin)) {
        if (!hasRole('Admin')) {
            const currentRole = authState.user?.role
            if (currentRole === 'Staff') return next({ name: ROUTE_NAME_DYNAMIC_QR_GENERATOR })
            return next({ name: 'Dashboard' })
        }
    }

    // Kiá»ƒm tra allowedRoles
    const allowedRoles = to.matched.find(matchedRoute => matchedRoute.meta.allowedRoles)?.meta.allowedRoles
    if (allowedRoles) {
        const currentRole = authState.user?.role
        if (!allowedRoles.includes(currentRole)) {
            if (currentRole === 'Staff') return next({ name: ROUTE_NAME_DYNAMIC_QR_GENERATOR })
            return next({ name: 'Dashboard' })
        }
    }

    // Náº¿u Ä‘Ã£ Ä‘Äƒng nháº­p mÃ  vÃ o trang login â†’ redirect
    // NhÆ°ng cho phÃ©p truy cáº­p trang Ä‘Äƒng kÃ½ khÃ¡ch (GuestRegister) dÃ¹ Ä‘Ã£ Ä‘Äƒng nháº­p
    if (to.meta.guest && isLoggedIn() && to.name !== 'GuestRegister') {
        const currentRole = authState.user?.role
        if (currentRole === 'Staff') return next({ name: ROUTE_NAME_DYNAMIC_QR_GENERATOR })
        return next({ name: 'Dashboard' })
    }

    next()
})

export default router

