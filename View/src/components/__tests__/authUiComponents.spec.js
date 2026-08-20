import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({ router: { replace: vi.fn() } }))

vi.mock('vue-router', () => ({ useRouter: () => hoisted.router }))
vi.mock('../../stores/auth', () => ({ changePassword: vi.fn(), logout: vi.fn() }))

const authStore = await import('../../stores/auth')
const RouteErrorBoundary = (await import('../ui/RouteErrorBoundary.vue')).default
const ForcePasswordChange = (await import('../auth/ForcePasswordChange.vue')).default

beforeEach(() => {
    vi.clearAllMocks()
    hoisted.router.replace.mockReset().mockResolvedValue()
})

describe('RouteErrorBoundary', () => {
    it('renders slot content when no error', () => {
        const wrapper = mount(RouteErrorBoundary, {
            slots: { default: '<div class="good-content">OK</div>' },
        })
        expect(wrapper.find('.good-content').exists()).toBe(true)
    })

    it('shows error UI when child throws during render', async () => {
        const BadChild = {
            methods: { boom() { throw new Error('render boom') } },
            template: '<span>{{ boom() }}</span>',
        }
        const errorSpy = vi.spyOn(console, 'error').mockImplementation(() => {})
        const wrapper = mount(RouteErrorBoundary, {
            slots: { default: BadChild },
        })
        await flushPromises()
        expect(wrapper.find('.route-error-title').text()).toContain('Nội dung không thể hiển thị')
        errorSpy.mockRestore()
    })

    it('reloads the page on button click', async () => {
        const reloadSpy = vi.fn()
        const original = window.location
        window.location = { reload: reloadSpy, href: original.href }
        try {
            const BadChild = {
                methods: { boom() { throw new Error('boom') } },
                template: '<span>{{ boom() }}</span>',
            }
            const errorSpy = vi.spyOn(console, 'error').mockImplementation(() => {})
            const wrapper = mount(RouteErrorBoundary, { slots: { default: BadChild } })
            await flushPromises()
            await wrapper.get('.route-error-btn').trigger('click')
            expect(reloadSpy).toHaveBeenCalled()
            errorSpy.mockRestore()
        } finally {
            window.location = original
        }
    })
})

describe('ForcePasswordChange', () => {
    const validCurrent = 'oldpass1'
    const validNew = 'NewPass1@'

    const fillForm = async (wrapper, opts = {}) => {
        await wrapper.get('#fpc-current').setValue(opts.current ?? validCurrent)
        await wrapper.get('#fpc-new').setValue(opts.new ?? validNew)
        await wrapper.get('#fpc-confirm').setValue(opts.confirm ?? validNew)
    }

    it('validates missing current password', async () => {
        const wrapper = mount(ForcePasswordChange)
        await wrapper.get('#fpc-new').setValue(validNew)
        await wrapper.get('#fpc-confirm').setValue(validNew)
        await wrapper.get('form').trigger('submit')
        expect(wrapper.text()).toContain('Vui lòng nhập mật khẩu hiện tại.')
    })

    it('validates missing new password', async () => {
        const wrapper = mount(ForcePasswordChange)
        await wrapper.get('#fpc-current').setValue(validCurrent)
        await wrapper.get('form').trigger('submit')
        expect(wrapper.text()).toContain('Vui lòng nhập mật khẩu mới.')
    })

    it('validates minimum password length', async () => {
        const wrapper = mount(ForcePasswordChange)
        await fillForm(wrapper, { new: 'abc', confirm: 'abc' })
        await wrapper.get('form').trigger('submit')
        expect(wrapper.text()).toContain('Mật khẩu mới phải có ít nhất 6 ký tự.')
    })

    it('requires the new password to differ from current', async () => {
        const wrapper = mount(ForcePasswordChange)
        await fillForm(wrapper, { new: 'oldpass1', confirm: 'oldpass1' })
        await wrapper.get('form').trigger('submit')
        expect(wrapper.text()).toContain('Mật khẩu mới phải khác mật khẩu hiện tại.')
    })

    it('validates password confirmation match', async () => {
        const wrapper = mount(ForcePasswordChange)
        await fillForm(wrapper, { confirm: 'Different@1' })
        await wrapper.get('form').trigger('submit')
        expect(wrapper.text()).toContain('Nhập lại mật khẩu mới không khớp.')
    })

    it('submits successfully and emits changed', async () => {
        authStore.changePassword.mockResolvedValue({})
        const wrapper = mount(ForcePasswordChange)
        await fillForm(wrapper)
        await wrapper.get('form').trigger('submit')
        await flushPromises()
        expect(authStore.changePassword).toHaveBeenCalledWith(validCurrent, validNew)
        expect(wrapper.text()).toContain('Đổi mật khẩu thành công')
        expect(wrapper.emitted('changed')).toBeTruthy()
    })

    it('surface backend error message', async () => {
        authStore.changePassword.mockRejectedValue({ response: { data: { message: 'Mật khẩu cũ sai' } } })
        const wrapper = mount(ForcePasswordChange)
        await fillForm(wrapper)
        await wrapper.get('form').trigger('submit')
        await flushPromises()
        expect(wrapper.text()).toContain('Mật khẩu cũ sai')
    })

    it('handles network error', async () => {
        authStore.changePassword.mockRejectedValue({ code: 'ERR_NETWORK' })
        const wrapper = mount(ForcePasswordChange)
        await fillForm(wrapper)
        await wrapper.get('form').trigger('submit')
        await flushPromises()
        expect(wrapper.text()).toContain('Không thể kết nối tới Core Server')
    })

    it('evaluates password strength levels', async () => {
        const wrapper = mount(ForcePasswordChange)
        await wrapper.get('#fpc-new').setValue('short')
        expect(wrapper.get('.fpc-meter').classes()).toContain('weak')
        await wrapper.get('#fpc-new').setValue('Medium1')
        expect(wrapper.get('.fpc-meter').classes()).toContain('medium')
        await wrapper.get('#fpc-new').setValue('StrongPass1@')
        expect(wrapper.get('.fpc-meter').classes()).toContain('strong')
    })

    it('toggles current password visibility', async () => {
        const wrapper = mount(ForcePasswordChange)
        const eye = wrapper.findAll('.fpc-eye')[0]
        await eye.trigger('click')
        expect(wrapper.get('#fpc-current').attributes('type')).toBe('text')
        await eye.trigger('click')
        expect(wrapper.get('#fpc-current').attributes('type')).toBe('password')
    })

    it('logs out and redirects to login', async () => {
        authStore.logout.mockResolvedValue()
        const wrapper = mount(ForcePasswordChange)
        await wrapper.get('.fpc-logout').trigger('click')
        await flushPromises()
        expect(authStore.logout).toHaveBeenCalled()
        expect(hoisted.router.replace).toHaveBeenCalledWith({ name: 'Login' })
    })
})