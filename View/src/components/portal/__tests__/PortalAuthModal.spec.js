import { mount, flushPromises } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import PortalAuthModal from '../PortalAuthModal.vue'

const { mockAuthGoogle } = vi.hoisted(() => ({
  mockAuthGoogle: vi.fn()
}))

vi.mock('../../../services/portalApi', () => ({
  portalApi: { authGoogle: (...args) => mockAuthGoogle(...args) }
}))

const routerLinkStub = { template: '<a><slot /></a>' }

describe('PortalAuthModal', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.useFakeTimers()
    localStorage.clear()
    mockAuthGoogle.mockReset()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
  })

  it('renders nothing when show is false', () => {
    const wrapper = mount(PortalAuthModal, {
      props: { show: false },
      global: { stubs: { 'router-link': routerLinkStub } }
    })
    expect(wrapper.find('[role="dialog"]').exists()).toBe(false)
  })

  it('shows logged-in user profile when currentUser provided', () => {
    const wrapper = mount(PortalAuthModal, {
      props: { show: true, currentUser: { fullName: 'Test', email: 't@t.com', avatarUrl: 'x', role: 'Admin' } },
      global: { stubs: { 'router-link': routerLinkStub } }
    })
    expect(wrapper.text()).toContain('Test')
    expect(wrapper.text()).toContain('t@t.com')
  })

  it('handles logout', async () => {
    const wrapper = mount(PortalAuthModal, {
      props: { show: true, currentUser: { fullName: 'Test', email: 't@t.com' } },
      global: { stubs: { 'router-link': routerLinkStub } }
    })
    const logoutBtn = wrapper.findAll('button').find(b => b.text().includes('Đăng xuất'))
    await logoutBtn.trigger('click')
    expect(wrapper.emitted('logout')).toBeTruthy()
    expect(wrapper.emitted('close')).toBeTruthy()
  })

  it('submits google login successfully with demo email', async () => {
    mockAuthGoogle.mockResolvedValue({ success: true, data: { fullName: 'Test User', email: 't@t.com' } })
    const wrapper = mount(PortalAuthModal, {
      props: { show: true },
      global: { stubs: { 'router-link': routerLinkStub } }
    })
    const loginBtn = wrapper.findAll('button').find(b => b.text().includes('Google SSO'))
    await loginBtn.trigger('click')
    await flushPromises()
    expect(wrapper.emitted('login-success')).toBeTruthy()
    vi.advanceTimersByTime(1001)
    expect(wrapper.emitted('close')).toBeTruthy()
  })

  it('submits google login using email and name inputs', async () => {
    mockAuthGoogle.mockResolvedValue({ success: true, data: { fullName: 'Custom', email: 'c@c.com' } })
    const wrapper = mount(PortalAuthModal, {
      props: { show: true },
      global: { stubs: { 'router-link': routerLinkStub } }
    })
    wrapper.find('input[type="text"]').setValue('Custom Name')
    wrapper.find('input[type="email"]').setValue('custom@mail.com')
    const loginBtn = wrapper.findAll('button').find(b => b.text().includes('Xác Nhận Đăng Nhập'))
    await loginBtn.trigger('click')
    await flushPromises()
    expect(wrapper.emitted('login-success')).toBeTruthy()
  })

  it('shows error when login returns no data', async () => {
    mockAuthGoogle.mockResolvedValue({ success: false, message: 'Login failed' })
    const wrapper = mount(PortalAuthModal, {
      props: { show: true },
      global: { stubs: { 'router-link': routerLinkStub } }
    })
    const loginBtn = wrapper.findAll('button').find(b => b.text().includes('Google SSO'))
    await loginBtn.trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Login failed')
  })

  it('shows connection error on rejected promise', async () => {
    mockAuthGoogle.mockRejectedValue(new Error('network'))
    const wrapper = mount(PortalAuthModal, {
      props: { show: true },
      global: { stubs: { 'router-link': routerLinkStub } }
    })
    const loginBtn = wrapper.findAll('button').find(b => b.text().includes('Google SSO'))
    await loginBtn.trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Lỗi kết nối máy chủ Google OAuth!')
  })

  it('closes modal when backdrop is clicked', async () => {
    const wrapper = mount(PortalAuthModal, {
      props: { show: true },
      global: { stubs: { 'router-link': routerLinkStub } }
    })
    await wrapper.get('.fixed.inset-0.bg-slate-950\\/80').trigger('click')
    expect(wrapper.emitted('close')).toBeTruthy()
  })
})
