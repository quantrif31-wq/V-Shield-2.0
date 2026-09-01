import { mount } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import PortalNavbar from '../PortalNavbar.vue'

const h = vi.hoisted(() => ({
  mockPush: vi.fn(),
  currentRoutePath: '/',
  currentAuthUser: null
}))

vi.mock('vue-router', () => ({
  useRouter: () => ({ push: h.mockPush }),
  useRoute: () => ({ get path() { return h.currentRoutePath } })
}))

vi.mock('../../../stores/auth', () => ({
  authState: { get user() { return h.currentAuthUser } }
}))

vi.mock('../../../utils/portalAudio', () => ({ mechaAudio: {} }))
vi.mock('../../../utils/portalVoiceSynth', () => ({ tacticalVoice: {} }))

describe('PortalNavbar', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    h.currentRoutePath = '/'
    h.currentAuthUser = null
  })

  const routerLinkStub = { template: '<a><slot /></a>' }

  function mountNavbar(props = {}) {
    return mount(PortalNavbar, {
      props,
      global: { stubs: { 'router-link': routerLinkStub, PortalAudioToggle: true } }
    })
  }

  it('mounts and renders brand', () => {
    const wrapper = mountNavbar()
    expect(wrapper.text()).toContain('V-SHIELD')
  })

  it('navigates via desktop nav buttons', async () => {
    const wrapper = mountNavbar()
    const featuresBtn = wrapper.findAll('button').find(b => b.text().includes('GIẢI PHÁP AI'))
    await featuresBtn.trigger('click')
    expect(h.mockPush).toHaveBeenCalledWith('/features')
    expect(wrapper.vm.mobileMenuOpen).toBe(false)
  })

  it('activates home link on portal path', () => {
    h.currentRoutePath = '/portal'
    const wrapper = mountNavbar()
    const overviewBtn = wrapper.findAll('button').find(b => b.text().includes('TỔNG QUAN'))
    expect(overviewBtn.classes()).toContain('text-amber-400')
  })

  it('opens mobile menu and navigates', async () => {
    const wrapper = mountNavbar()
    const menuBtn = wrapper.find('button[aria-label="Menu"]')
    await menuBtn.trigger('click')
    expect(wrapper.vm.mobileMenuOpen).toBe(true)
    const drawerBtn = wrapper.findAll('button').find(b => b.text().includes('//'))
    await drawerBtn.trigger('click')
    expect(h.mockPush).toHaveBeenCalled()
    expect(wrapper.vm.mobileMenuOpen).toBe(false)
  })

  it('emits openAuth when account button clicked', async () => {
    const wrapper = mountNavbar()
    const accountBtn = wrapper.findAll('button').find(b => b.text().includes('TÀI KHOẢN'))
    await accountBtn.trigger('click')
    expect(wrapper.emitted('openAuth')).toBeTruthy()
  })

  it('shows community user profile and emits openAuth on click', async () => {
    const wrapper = mountNavbar({ communityUser: { fullName: 'Test User', avatarUrl: 'x' } })
    expect(wrapper.text()).toContain('Test User')
    const avatarBtn = wrapper.findAll('button').find(b => b.text().includes('Test User'))
    await avatarBtn.trigger('click')
    expect(wrapper.emitted('openAuth')).toBeTruthy()
  })

  it('dispatches sfx event on interactions', async () => {
    const dispatchSpy = vi.spyOn(window, 'dispatchEvent')
    const wrapper = mountNavbar()
    const accountBtn = wrapper.findAll('button').find(b => b.text().includes('TÀI KHOẢN'))
    await accountBtn.trigger('click')
    expect(dispatchSpy).toHaveBeenCalled()
  })

  it('renders dashboard link when logged in', () => {
    h.currentAuthUser = { fullName: 'Admin' }
    const wrapper = mountNavbar()
    expect(wrapper.text()).toContain('DASHBOARD')
  })
})
