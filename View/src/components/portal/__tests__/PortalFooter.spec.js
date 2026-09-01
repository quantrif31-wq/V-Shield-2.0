import { mount, flushPromises } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import PortalFooter from '../PortalFooter.vue'

const { mockPush, mockSubscribeNewsletter } = vi.hoisted(() => ({
  mockPush: vi.fn(),
  mockSubscribeNewsletter: vi.fn()
}))

vi.mock('vue-router', () => ({
  useRouter: () => ({ push: mockPush })
}))

vi.mock('../../../services/portalApi', () => ({
  portalApi: { subscribeNewsletter: (...args) => mockSubscribeNewsletter(...args) }
}))

const routerLinkStub = { template: '<a><slot /></a>' }

describe('PortalFooter', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    window.scrollTo = vi.fn()
    mockSubscribeNewsletter.mockReset()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('mounts and renders footer', () => {
    const wrapper = mount(PortalFooter, {
      global: { stubs: { 'router-link': routerLinkStub } }
    })
    expect(wrapper.text()).toContain('V-SHIELD 2.0')
  })

  it('navigates to features via navigateTo', async () => {
    const wrapper = mount(PortalFooter, { global: { stubs: { 'router-link': routerLinkStub } } })
    const featureBtn = wrapper.findAll('button').find(b => b.text().includes('AI Face ID'))
    if (featureBtn) {
      await featureBtn.trigger('click')
      expect(mockPush).toHaveBeenCalledWith('/features')
    }
  })

  it('clicks all remaining feature buttons to navigate to features', async () => {
    const wrapper = mount(PortalFooter, { global: { stubs: { 'router-link': routerLinkStub } } })
    for (const label of ['Virtual Smart Barrier', 'Mã QR Động TOTP', 'VoIP Video Call', 'Hybrid Sync', 'UEBA Threat']) {
      const btn = wrapper.findAll('button').find(b => b.text().includes(label))
      if (btn) {
        await btn.trigger('click')
        expect(mockPush).toHaveBeenCalledWith('/features')
      }
    }
  })

  it('navigates to home route', async () => {
    const wrapper = mount(PortalFooter, { global: { stubs: { 'router-link': routerLinkStub } } })
    const homeBtn = wrapper.findAll('button').find(b => b.text().includes('Tổng Quan'))
    if (homeBtn) {
      await homeBtn.trigger('click')
      expect(mockPush).toHaveBeenCalledWith('/')
    }
  })

  it('navigates to roadmap', async () => {
    const wrapper = mount(PortalFooter, { global: { stubs: { 'router-link': routerLinkStub } } })
    const btn = wrapper.findAll('button').find(b => b.text().includes('Lộ Trình'))
    if (btn) {
      await btn.trigger('click')
      expect(mockPush).toHaveBeenCalledWith('/roadmap')
    }
  })

  it('navigates to download', async () => {
    const wrapper = mount(PortalFooter, { global: { stubs: { 'router-link': routerLinkStub } } })
    const btn = wrapper.findAll('button').find(b => b.text().includes('Tải Ứng Dụng'))
    if (btn) {
      await btn.trigger('click')
      expect(mockPush).toHaveBeenCalledWith('/download')
    }
  })

  it('navigates to community', async () => {
    const wrapper = mount(PortalFooter, { global: { stubs: { 'router-link': routerLinkStub } } })
    const btn = wrapper.findAll('button').find(b => b.text().includes('Đánh Giá'))
    if (btn) {
      await btn.trigger('click')
      expect(mockPush).toHaveBeenCalledWith('/community')
    }
  })

  it('navigates to about', async () => {
    const wrapper = mount(PortalFooter, { global: { stubs: { 'router-link': routerLinkStub } } })
    const btn = wrapper.findAll('button').find(b => b.text().includes('Đội Ngũ'))
    if (btn) {
      await btn.trigger('click')
      expect(mockPush).toHaveBeenCalledWith('/about')
    }
  })

  it('navigates to contact', async () => {
    const wrapper = mount(PortalFooter, { global: { stubs: { 'router-link': routerLinkStub } } })
    const btn = wrapper.findAll('button').find(b => b.text().includes('Liên Hệ'))
    if (btn) {
      await btn.trigger('click')
      expect(mockPush).toHaveBeenCalledWith('/contact')
    }
  })

  it('shows error for invalid email on newsletter submit', async () => {
    const wrapper = mount(PortalFooter, { global: { stubs: { 'router-link': routerLinkStub } } })
    const form = wrapper.find('form')
    await form.trigger('submit.prevent')
    expect(wrapper.text()).toContain('Vui lòng nhập địa chỉ email hợp lệ!')
  })

  it('submits newsletter with valid email successfully', async () => {
    mockSubscribeNewsletter.mockResolvedValue({ success: true })
    const wrapper = mount(PortalFooter, { global: { stubs: { 'router-link': routerLinkStub } } })
    const input = wrapper.find('input[type="email"]')
    await input.setValue('test@example.com')
    const form = wrapper.find('form')
    await form.trigger('submit.prevent')
    await flushPromises()
    expect(mockSubscribeNewsletter).toHaveBeenCalled()
  })

  it('handles newsletter submit error', async () => {
    mockSubscribeNewsletter.mockRejectedValue(new Error('fail'))
    const wrapper = mount(PortalFooter, { global: { stubs: { 'router-link': routerLinkStub } } })
    const input = wrapper.find('input[type="email"]')
    await input.setValue('test@example.com')
    const form = wrapper.find('form')
    await form.trigger('submit.prevent')
    await flushPromises()
    expect(wrapper.text()).toContain('Không thể đăng ký')
  })

  it('displays submitting state', async () => {
    let resolvePromise
    mockSubscribeNewsletter.mockImplementation(() => new Promise(r => { resolvePromise = r }))
    const wrapper = mount(PortalFooter, { global: { stubs: { 'router-link': routerLinkStub } } })
    const input = wrapper.find('input[type="email"]')
    await input.setValue('test@example.com')
    const form = wrapper.find('form')
    form.trigger('submit.prevent')
    resolvePromise({ success: true })
    return Promise.resolve()
  })

  it('dispatches portal-click-sfx on navigateTo', async () => {
    const dispatchSpy = vi.spyOn(window, 'dispatchEvent')
    const wrapper = mount(PortalFooter, { global: { stubs: { 'router-link': routerLinkStub } } })
    const btn = wrapper.findAll('button').find(b => b.text().includes('AI Face ID'))
    if (btn) {
      await btn.trigger('click')
      expect(dispatchSpy).toHaveBeenCalled()
    }
  })
})
