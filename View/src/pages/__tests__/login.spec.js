import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({
  route: { query: {} },
  router: { push: vi.fn() },
  login: vi.fn(),
}))

vi.mock('vue-router', () => ({
  useRoute: () => hoisted.route,
  useRouter: () => hoisted.router,
}))
vi.mock('../../stores/auth', () => ({ login: (...args) => hoisted.login(...args) }))
vi.mock('../../services/identityApi', () => ({
  identityApi: { getProviders: vi.fn(), oidcChallenge: vi.fn() },
}))
vi.mock('qrcode', () => ({ default: { toDataURL: vi.fn().mockResolvedValue('data:image/png;base64,QR') } }))

const identityApi = (await import('../../services/identityApi')).identityApi
const Login = (await import('../Login.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  vi.useFakeTimers()
  hoisted.route.query = {}
  identityApi.getProviders.mockResolvedValue({ data: [] })
})
afterEach(() => {
  vi.runOnlyPendingTimers()
  vi.useRealTimers()
})

describe('Login', () => {
  it('shows a validation error when credentials are missing', async () => {
    const wrapper = mount(Login, { global: { stubs: { BaseButton: true, ForcePasswordChange: true } } })
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(hoisted.login).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Vui lòng điền đầy đủ thông tin xác thực.')
  })

  it('logs in and redirects to the dashboard', async () => {
    hoisted.login.mockResolvedValue({ success: true, requiresPasswordChange: false })
    const wrapper = mount(Login, { global: { stubs: { BaseButton: true, ForcePasswordChange: true } } })
    await wrapper.find('#username').setValue('admin')
    await wrapper.find('#password').setValue('secret')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(hoisted.login).toHaveBeenCalledWith('admin', 'secret', null)
    vi.advanceTimersByTime(1000)
    expect(hoisted.router.push).toHaveBeenCalledWith('/')
  })

  it('redirects to the requested path when provided', async () => {
    hoisted.route.query = { redirect: '/employees' }
    hoisted.login.mockResolvedValue({ success: true })
    const wrapper = mount(Login, { global: { stubs: { BaseButton: true, ForcePasswordChange: true } } })
    await wrapper.find('#username').setValue('admin')
    await wrapper.find('#password').setValue('secret')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    vi.advanceTimersByTime(1000)
    expect(hoisted.router.push).toHaveBeenCalledWith('/employees')
  })

  it('requests an MFA code when required', async () => {
    hoisted.login.mockResolvedValue({ requiresMfa: true, mfaSetupUri: 'otpauth://totp/VShield:admin', mfaSetupSecret: 'SECRET', message: 'cần mã' })
    const wrapper = mount(Login, { global: { stubs: { BaseButton: true, ForcePasswordChange: true } } })
    await wrapper.find('#username').setValue('admin')
    await wrapper.find('#password').setValue('secret')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(wrapper.text()).toContain('cần mã')
    expect(wrapper.find('#mfa-code').exists()).toBe(true)
  })

  it('shows the password-change screen when required', async () => {
    hoisted.login.mockResolvedValue({ success: true, requiresPasswordChange: true })
    const wrapper = mount(Login, { global: { stubs: { BaseButton: true, ForcePasswordChange: true } } })
    await wrapper.find('#username').setValue('admin')
    await wrapper.find('#password').setValue('secret')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(wrapper.findComponent({ name: 'ForcePasswordChange' }).exists()).toBe(true)
  })

  it('shows an error on wrong credentials', async () => {
    hoisted.login.mockRejectedValue({ response: { status: 401 } })
    const wrapper = mount(Login, { global: { stubs: { BaseButton: true, ForcePasswordChange: true } } })
    await wrapper.find('#username').setValue('admin')
    await wrapper.find('#password').setValue('wrong')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(wrapper.text()).toContain('Tên đăng nhập hoặc mật khẩu không đúng.')
  })

  it('surfaces network failures', async () => {
    hoisted.login.mockRejectedValue({ code: 'ERR_NETWORK' })
    const wrapper = mount(Login, { global: { stubs: { BaseButton: true, ForcePasswordChange: true } } })
    await wrapper.find('#username').setValue('admin')
    await wrapper.find('#password').setValue('secret')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(wrapper.text()).toContain('Không thể kết nối tới Core Server.')
  })
})
