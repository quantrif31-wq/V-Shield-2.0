import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({ route: null, router: { push: vi.fn() } }))

vi.mock('vue-router', async () => {
    const { reactive } = await import('vue')
    const route = reactive({ name: 'Dashboard', fullPath: '/dashboard', path: '/dashboard' })
    hoisted.route = route
    return { useRoute: () => route, useRouter: () => hoisted.router }
})
vi.mock('../../services/employeeApi', () => ({ getAll: vi.fn().mockResolvedValue({ data: [] }) }))
vi.mock('../../services/globalSearchProviders', () => ({ searchGlobal: vi.fn().mockResolvedValue([]) }))
vi.mock('../../stores/auth', async (importOriginal) => {
    const mod = await importOriginal()
    return {
        ...mod,
        authState: { user: { role: 'Admin', operationalTaskKeys: ['dashboard', 'monitoring'] } },
    }
})

const employeeApi = await import('../../services/employeeApi')
const globalSearch = await import('../../services/globalSearchProviders')
const Sidebar = (await import('../Layout/Sidebar.vue')).default

beforeEach(() => {
    vi.clearAllMocks()
    vi.useRealTimers()
    employeeApi.getAll.mockResolvedValue({ data: [] })
    globalSearch.searchGlobal.mockResolvedValue([])
    hoisted.router.push.mockReset().mockResolvedValue()
    hoisted.route.name = 'Dashboard'
    hoisted.route.fullPath = '/dashboard'
    hoisted.route.path = '/dashboard'
    hoisted.route.query = {}
})

describe('Sidebar', () => {
    it('renders nav groups with navigation buttons', async () => {
        const wrapper = mount(Sidebar, { global: { stubs: { transition: false } } })
        await flushPromises()
        expect(wrapper.get('.sidebar').attributes('aria-label')).toBe('Điều hướng chính')
        expect(wrapper.findAll('.nav-group').length).toBeGreaterThan(0)
        expect(wrapper.findAll('.nav-item-button').length).toBeGreaterThan(0)
    })

    it('loads employees badge on mount', async () => {
        employeeApi.getAll.mockResolvedValue({ data: [{}, {}, {}] })
        const wrapper = mount(Sidebar, { global: { stubs: { transition: false } } })
        await flushPromises()
        expect(employeeApi.getAll).toHaveBeenCalled()
        const employeesButton = wrapper.findAll('.nav-item-button').find((b) => b.text().includes('Nhân sự'))
        expect(employeesButton.text()).toContain('3')
    })

    it('toggles a nav group open and closed', async () => {
        const wrapper = mount(Sidebar, { global: { stubs: { transition: false } } })
        await flushPromises()
        const group = wrapper.findAll('.nav-group').find((g) => !g.classes().includes('is-open'))
        const labelText = group.find('.nav-label-text').text()
        await group.find('.nav-label-toggle').trigger('click')
        await flushPromises()
        const opened = wrapper.findAll('.nav-group').find((g) => g.find('.nav-label-text').text() === labelText)
        expect(opened.classes()).toContain('is-open')
        await opened.find('.nav-label-toggle').trigger('click')
        await flushPromises()
        const closed = wrapper.findAll('.nav-group').find((g) => g.find('.nav-label-text').text() === labelText)
        expect(closed.classes()).not.toContain('is-open')
    })

    it('navigates to a nav item and closes mobile menu', async () => {
        const wrapper = mount(Sidebar, { props: { isMobile: true, mobileOpen: true }, global: { stubs: { transition: false } } })
        await flushPromises()
        const firstButton = wrapper.findAll('.nav-item-button')[0]
        await firstButton.trigger('click')
        expect(hoisted.router.push).toHaveBeenCalled()
        expect(wrapper.emitted('close-mobile')).toBeTruthy()
    })

    it('emits close-mobile on route change when mobile', async () => {
        const wrapper = mount(Sidebar, { props: { isMobile: true, mobileOpen: true }, global: { stubs: { transition: false } } })
        await flushPromises()
        hoisted.route.fullPath = '/monitoring'
        hoisted.route.path = '/monitoring'
        await flushPromises()
        expect(wrapper.emitted('close-mobile')).toBeTruthy()
    })

    it('emits toggle when collapsed nav is clicked on desktop', async () => {
        const wrapper = mount(Sidebar, { props: { collapsed: true }, global: { stubs: { transition: false } } })
        await flushPromises()
        await wrapper.get('.sidebar-panel').trigger('click')
        expect(wrapper.emitted('toggle')).toBeTruthy()
    })

    it('emits toggle via collapse button on desktop', async () => {
        const wrapper = mount(Sidebar, { global: { stubs: { transition: false } } })
        await flushPromises()
        await wrapper.get('.sidebar-collapse-btn').trigger('click')
        expect(wrapper.emitted('toggle')).toBeTruthy()
    })

    it('emits close-mobile via mobile close button', async () => {
        const wrapper = mount(Sidebar, { props: { isMobile: true, mobileOpen: true }, global: { stubs: { transition: false } } })
        await flushPromises()
        await wrapper.get('.sidebar-mobile-close').trigger('click')
        expect(wrapper.emitted('close-mobile')).toBeTruthy()
    })

    it('searches employees and navigates on result click', async () => {
        vi.useFakeTimers()
        globalSearch.searchGlobal.mockResolvedValue([{ id: 1, name: 'Duy', type: 'employee' }])
        const wrapper = mount(Sidebar, { global: { stubs: { transition: false } } })
        await flushPromises()
        const input = wrapper.get('#sidebar-search')
        await input.setValue('Duy')
        vi.advanceTimersByTime(400)
        await flushPromises()
        expect(globalSearch.searchGlobal).toHaveBeenCalledWith('Duy')
        const dropdownItem = wrapper.findAll('.search-dropdown .dropdown-item')[0]
        await dropdownItem.trigger('click')
        expect(hoisted.router.push).toHaveBeenCalledWith({ path: '/employees', query: { search: 'Duy' } })
    })

    it('shows no-results state when search returns empty', async () => {
        vi.useFakeTimers()
        globalSearch.searchGlobal.mockResolvedValue([])
        const wrapper = mount(Sidebar, { global: { stubs: { transition: false } } })
        await flushPromises()
        await wrapper.get('#sidebar-search').setValue('xyz')
        vi.advanceTimersByTime(400)
        await flushPromises()
        expect(wrapper.text()).toContain('Không có kết quả phù hợp')
    })

    it('clears search results when query is emptied', async () => {
        vi.useFakeTimers()
        const wrapper = mount(Sidebar, { global: { stubs: { transition: false } } })
        await flushPromises()
        const input = wrapper.get('#sidebar-search')
        await input.setValue('Duy')
        await input.setValue('')
        vi.advanceTimersByTime(400)
        await flushPromises()
        expect(globalSearch.searchGlobal).not.toHaveBeenCalledWith('')
        const dropdown = wrapper.find('.search-dropdown')
        expect(dropdown.exists()).toBe(true)
        expect(dropdown.element.style.display).toBe('none')
    })

    it('opens global search via Ctrl+K shortcut', async () => {
        vi.useFakeTimers()
        const wrapper = mount(Sidebar, { props: { collapsed: true }, global: { stubs: { transition: false } } })
        await flushPromises()
        const event = new KeyboardEvent('keydown', { key: 'k', ctrlKey: true, cancelable: true })
        window.dispatchEvent(event)
        await flushPromises()
        expect(wrapper.emitted('toggle')).toBeTruthy()
    })

    it('ignores shortcut while editing an input', async () => {
        const wrapper = mount(Sidebar, { global: { stubs: { transition: false } } })
        await flushPromises()
        const input = wrapper.get('#sidebar-search')
        const event = new KeyboardEvent('keydown', { key: 'k', ctrlKey: true, bubbles: true, cancelable: true })
        input.element.dispatchEvent(event)
        await flushPromises()
        expect(wrapper.emitted('toggle')).toBeFalsy()
    })

    it('cleans up document and window listeners on unmount', async () => {
        const removeSpy = vi.spyOn(window, 'removeEventListener')
        const wrapper = mount(Sidebar, { global: { stubs: { transition: false } } })
        await flushPromises()
        wrapper.unmount()
        expect(removeSpy).toHaveBeenCalled()
        removeSpy.mockRestore()
    })

    it('hides search bar when collapsed', async () => {
        const wrapper = mount(Sidebar, { props: { collapsed: true }, global: { stubs: { transition: false } } })
        await flushPromises()
        expect(wrapper.find('.sidebar-search').exists()).toBe(false)
    })
})