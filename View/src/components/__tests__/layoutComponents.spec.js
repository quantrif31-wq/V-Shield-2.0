import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({ route: { name: 'Dashboard', fullPath: '/dashboard' }, router: { push: vi.fn() } }))

vi.mock('vue-router', () => ({ useRoute: () => hoisted.route, useRouter: () => hoisted.router }))
vi.mock('../../services/securityAlertBus', () => ({
    securityAlertState: { items: [] },
    refreshSecurityAlerts: vi.fn().mockResolvedValue(),
    startSecurityAlertPolling: vi.fn(),
    stopSecurityAlertPolling: vi.fn(),
}))
vi.mock('../../services/notificationApi', async (importOriginal) => {
    const mod = await importOriginal()
    return {
        ...mod,
        connectNotificationHub: vi.fn(),
        disconnectNotificationHub: vi.fn(),
        getNotifications: vi.fn(),
        getUnreadCount: vi.fn(),
        markAllNotificationsRead: vi.fn(),
        markNotificationRead: vi.fn(),
        onNotification: vi.fn(),
        onUnreadCountChanged: vi.fn(),
    }
})
vi.mock('../../services/enterpriseSecurityApi', () => ({
    enterpriseApi: { acknowledgeDuressEvent: vi.fn(), getActiveSecurityAlerts: vi.fn().mockResolvedValue({ data: { items: [] } }) },
}))
vi.mock('../../services/socApi', () => ({ socApi: { acknowledgeAlarm: vi.fn().mockResolvedValue() } }))
vi.mock('../../composables/usePreferences', async () => {
    const { computed, ref } = await import('vue')
    const isDark = ref(false)
    const density = ref('comfortable')
    return {
        usePreferences: () => ({
            isDark: computed(() => isDark.value),
            density: computed(() => density.value),
            setDensity: (v) => { density.value = v },
            toggleTheme: () => { isDark.value = !isDark.value },
        }),
    }
})

const securityAlertBus = await import('../../services/securityAlertBus')
const notificationApi = await import('../../services/notificationApi')
const authModule = await import('../../stores/auth')
const MainLayout = (await import('../Layout/MainLayout.vue')).default
const Header = (await import('../Layout/Header.vue')).default

const stubs = {
    Sidebar: {
        template: '<div class="stub-sidebar"><button class="sidebar-fake" @click="$emit(\'toggle\')"></button></div>',
    },
    Header: {
        props: ['collapsed', 'isMobile'],
        emits: ['toggle-sidebar'],
        template: '<div class="stub-header"><button class="header-fake" @click="$emit(\'toggle-sidebar\')"></button></div>',
    },
    AIChatBot: true,
    RouteErrorBoundary: { template: '<div class="stub-boundary"><slot /></div>' },
    RouterView: { template: '<div class="stub-view" />' },
    transition: false,
    'keep-alive': false,
    'router-view': false,
    Transition: false,
}

beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
    sessionStorage.clear()
    window.innerWidth = 1280
    document.documentElement.dataset.theme = 'light'
    document.documentElement.dataset.density = 'comfortable'
    if (authModule) {
        authModule.authState.user = { fullName: 'Nguyen Van A', role: 'Admin', username: 'admin' }
    }
})

describe('MainLayout', () => {
    it('mounts and starts security alert polling', async () => {
        mount(MainLayout, { global: { stubs } })
        await flushPromises()
        expect(securityAlertBus.startSecurityAlertPolling).toHaveBeenCalled()
    })

    it('restores sidebar collapsed state from localStorage', async () => {
        localStorage.setItem('vshield-sidebar-collapsed', 'true')
        const wrapper = mount(MainLayout, { global: { stubs } })
        await flushPromises()
        expect(wrapper.get('.ops-shell').classes()).toContain('nav-collapsed')
    })

    it('collapses desktop sidebar when toggled', async () => {
        const wrapper = mount(MainLayout, { global: { stubs } })
        await flushPromises()
        wrapper.find('.header-fake').trigger('click')
        await flushPromises()
        expect(wrapper.get('.ops-shell').classes()).toContain('nav-collapsed')
        expect(localStorage.getItem('vshield-sidebar-collapsed')).toBe('true')
    })

    it('opens mobile sidebar when toggled on small viewport', async () => {
        window.innerWidth = 800
        const wrapper = mount(MainLayout, { global: { stubs } })
        await flushPromises()
        expect(wrapper.get('.ops-shell').classes()).toContain('is-mobile')
        wrapper.find('.sidebar-fake').trigger('click')
        await flushPromises()
        expect(wrapper.get('.ops-shell').classes()).toContain('nav-open')
        expect(wrapper.find('.shell-scrim').exists()).toBe(true)
        wrapper.find('.shell-scrim').trigger('click')
        await flushPromises()
        expect(wrapper.get('.ops-shell').classes()).not.toContain('nav-open')
    })

    it('resizes across the breakpoint and closes mobile sidebar on desktop', async () => {
        window.innerWidth = 800
        const wrapper = mount(MainLayout, { global: { stubs } })
        await flushPromises()
        expect(wrapper.get('.ops-shell').classes()).toContain('is-mobile')
        wrapper.find('.sidebar-fake').trigger('click')
        await flushPromises()
        expect(wrapper.get('.ops-shell').classes()).toContain('nav-open')
        window.innerWidth = 1280
        window.dispatchEvent(new Event('resize'))
        await flushPromises()
        expect(wrapper.get('.ops-shell').classes()).not.toContain('is-mobile')
        expect(wrapper.get('.ops-shell').classes()).not.toContain('nav-open')
    })

    it('persists collapsed value via localStorage on toggle', async () => {
        localStorage.setItem('vshield-sidebar-collapsed', 'false')
        const wrapper = mount(MainLayout, { global: { stubs } })
        await flushPromises()
        wrapper.find('.sidebar-fake').trigger('click')
        await flushPromises()
        expect(localStorage.getItem('vshield-sidebar-collapsed')).toBe('true')
    })

    it('cleans up listeners and polling on unmount', async () => {
        const wrapper = mount(MainLayout, { global: { stubs } })
        await flushPromises()
        wrapper.unmount()
        expect(securityAlertBus.stopSecurityAlertPolling).toHaveBeenCalled()
    })
})

describe('Header', () => {
    it('renders route title and user identity', async () => {
        hoisted.route.name = 'Dashboard'
        const wrapper = mount(Header, { global: { stubs: { transition: false } } })
        expect(wrapper.text()).toContain('Tổng quan hệ thống')
        expect(wrapper.text()).toContain('Nguyen Van A')
        expect(wrapper.text()).toContain('Quản trị viên')
    })

    it('falls back to default title for unknown routes', async () => {
        hoisted.route.name = 'UnknownRoute'
        const wrapper = mount(Header, { global: { stubs: { transition: false } } })
        expect(wrapper.text()).toContain('V-Shield Trung tâm điều hành')
    })

    it('derives user initial and avatar', async () => {
        authModule.authState.user = { username: 'quantrivien' }
        hoisted.route.name = 'Monitoring'
        const wrapper = mount(Header, { global: { stubs: { transition: false } } })
        expect(wrapper.text()).toContain('Giám sát trực tiếp')
        expect(wrapper.find('.user-avatar').text()).toBe('Q')
    })

    it('maps known and unknown roles to labels', async () => {
        authModule.authState.user = { role: 'LeTan', username: 'letan' }
        const first = mount(Header, { global: { stubs: { transition: false } } })
        expect(first.text()).toContain('Lễ tân')
        first.unmount()
        authModule.authState.user = { role: 'CustomRole', username: 'x' }
        const second = mount(Header, { global: { stubs: { transition: false } } })
        expect(second.text()).toContain('CustomRole')
    })

    it('shows status chip state and notification dropdown', async () => {
        hoisted.route.name = 'Dashboard'
        notificationApi.getNotifications.mockResolvedValue({ data: { data: [{ id: 1, title: 'Xe ra', severity: 'warning', isRead: false }] } })
        notificationApi.getUnreadCount.mockResolvedValue({ data: { count: 1 } })
        const wrapper = mount(Header, { global: { stubs: { transition: false } } })
        await flushPromises()
        expect(notificationApi.getNotifications).toHaveBeenCalled()
        expect(notificationApi.getUnreadCount).toHaveBeenCalled()
        const trigger = wrapper.find('.notification-trigger')
        expect(trigger.exists()).toBe(true)
        await trigger.trigger('click')
        expect(wrapper.find('.notification-dropdown').exists()).toBe(true)
        expect(wrapper.text()).toContain('Xe ra')
    })

    it('merges security alerts into the feed and shows status summary', async () => {
        hoisted.route.name = 'Dashboard'
        notificationApi.getNotifications.mockResolvedValue({ data: { data: [] } })
        notificationApi.getUnreadCount.mockResolvedValue({ data: { count: 0 } })
        securityAlertBus.securityAlertState.items = [
            { id: 'alarm-1', title: 'Đột nhập khu A', severity: 'critical', kind: 'alarm', generatedAtUtc: '2026-01-01T00:00:00Z', locationLabel: 'Khu A' },
        ]
        const wrapper = mount(Header, { global: { stubs: { transition: false } } })
        await flushPromises()
        await wrapper.find('.notification-trigger').trigger('click')
        expect(wrapper.find('.notification-dropdown').exists()).toBe(true)
        expect(wrapper.text()).toContain('Đột nhập khu A')
        expect(wrapper.get('.status-chip').classes()).toContain('status-critical')
    })

    it('logs out and navigates to login', async () => {
        const logoutSpy = vi.spyOn(authModule, 'logout').mockResolvedValue()
        hoisted.router.push.mockClear()
        hoisted.route.name = 'Dashboard'
        const wrapper = mount(Header, {
            global: {
                stubs: { transition: false },
            },
        })
        await wrapper.find('.header-user').trigger('click')
        expect(wrapper.find('.user-dropdown').exists()).toBe(true)
        const logoutButton = wrapper.findAll('button').find((b) => b.text().includes('Đăng xuất an toàn'))
        await logoutButton.trigger('click')
        expect(logoutSpy).toHaveBeenCalled()
        expect(hoisted.router.push).toHaveBeenCalledWith('/login')
        logoutSpy.mockRestore()
    })

    it('navigates when a notification item is clicked', async () => {
        hoisted.route.name = 'Dashboard'
        hoisted.router.push.mockClear()
        notificationApi.getNotifications.mockResolvedValue({
            data: {
                data: [{ id: 7, title: 'Xe ra', severity: 'success', isRead: true, actionUrl: '/access-logs', createdAt: '2026-01-01T00:00:00Z' }],
            },
        })
        notificationApi.getUnreadCount.mockResolvedValue({ data: { count: 0 } })
        notificationApi.markNotificationRead.mockResolvedValue({})
        const wrapper = mount(Header, { global: { stubs: { transition: false } } })
        await flushPromises()
        await wrapper.find('.notification-trigger').trigger('click')
        const item = wrapper.findAll('.notification-item').find((n) => n.text().includes('Xe ra'))
        await item.trigger('click')
        await flushPromises()
        expect(hoisted.router.push).toHaveBeenCalledWith('/access-logs')
    })

    it('marks a notification read on click when unread', async () => {
        hoisted.route.name = 'Dashboard'
        notificationApi.getNotifications.mockResolvedValue({
            data: {
                data: [{ id: 8, title: 'Yêu cầu duyệt', severity: 'info', isRead: false, actionUrl: '/leave-approvals', createdAt: '2026-01-01T00:00:00Z' }],
            },
        })
        notificationApi.getUnreadCount.mockResolvedValue({ data: { count: 1 } })
        const wrapper = mount(Header, { global: { stubs: { transition: false } } })
        await flushPromises()
        await wrapper.find('.notification-trigger').trigger('click')
        const item = wrapper.findAll('.notification-item').find((n) => n.text().includes('Yêu cầu duyệt'))
        await item.trigger('click')
        await flushPromises()
        expect(notificationApi.markNotificationRead).toHaveBeenCalledWith(8)
    })

    it('toggles theme and density preferences', async () => {
        hoisted.route.name = 'Dashboard'
        const wrapper = mount(Header, { global: { stubs: { transition: false } } })
        const themeButton = wrapper.findAll('button.header-action').find((b) => b.attributes('aria-label') === 'Chuyển sang chế độ phòng điều khiển tối')
        await themeButton.trigger('click')
        await flushPromises()
        const after = mount(Header, { global: { stubs: { transition: false } } })
        expect(after.find('button[aria-label="Chuyển sang giao diện sáng"]').exists()).toBe(true)
        after.unmount()
        wrapper.unmount()
    })

    it('switches density via the user dropdown', async () => {
        hoisted.route.name = 'Dashboard'
        const wrapper = mount(Header, { global: { stubs: { transition: false } } })
        await wrapper.find('.header-user').trigger('click')
        const compactButton = wrapper.findAll('.density-actions button').find((b) => b.text().includes('Gọn'))
        await compactButton.trigger('click')
        await flushPromises()
        await wrapper.find('.header-user').trigger('click')
        wrapper.unmount()
    })
})