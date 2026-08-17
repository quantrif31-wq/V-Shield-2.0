import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({
  route: { params: {}, query: {} },
  router: { replace: vi.fn(), push: vi.fn() },
}))

vi.mock('vue-router', () => ({
  useRoute: () => hoisted.route,
  useRouter: () => hoisted.router,
}))
vi.mock('../../services/preRegistrationApi', () => ({ getVisitorPass: vi.fn() }))
vi.mock('qrcode', () => ({ default: { toCanvas: vi.fn().mockResolvedValue() } }))
vi.mock('../../components/auth/ForcePasswordChange.vue', () => ({
  default: { name: 'ForcePasswordChange', emits: ['changed'], template: '<button @click="$emit(\'changed\')">change</button>' },
  __isTeleport: false,
  __isKeepAlive: false,
  __v_isVNode: false,
  __isSuspense: false,
  name: 'ForcePasswordChange',
  __name: 'ForcePasswordChange',
}))
vi.mock('../../components/ProjectTeamOverview.vue', () => ({
  default: { name: 'ProjectTeamOverview', template: '<div class="team-overview">TEAM</div>' },
  __isTeleport: false,
  __isKeepAlive: false,
  __v_isVNode: false,
  __isSuspense: false,
  name: 'ProjectTeamOverview',
  __name: 'ProjectTeamOverview',
}))

const preRegistrationApi = await import('../../services/preRegistrationApi')
const VisitorPass = (await import('../VisitorPass.vue')).default
const ForcePasswordChange = (await import('../ForcePasswordChange.vue')).default
const AboutProject = (await import('../AboutProject.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  vi.useFakeTimers()
  hoisted.route.params = {}
  hoisted.route.query = {}
})
afterEach(() => {
  vi.runOnlyPendingTimers()
  vi.useRealTimers()
})

describe('VisitorPass', () => {
  it('loads and draws the visitor QR', async () => {
    hoisted.route.params = { token: 'tok-1' }
    preRegistrationApi.getVisitorPass.mockResolvedValue({
      data: { visitor: { fullName: 'Khách A', visitorDetailId: 9 }, dynamicQr: { qrPayload: 'QR-PAYLOAD' } },
    })
    const wrapper = mount(VisitorPass)
    await flushPromises()
    expect(preRegistrationApi.getVisitorPass).toHaveBeenCalledWith('tok-1')
    expect(wrapper.text()).toContain('Khách A')
    expect(wrapper.find('canvas').exists()).toBe(true)
  })

  it('shows an error when the pass cannot be loaded', async () => {
    hoisted.route.params = { token: 'bad' }
    preRegistrationApi.getVisitorPass.mockRejectedValue({ response: { data: { message: 'hết hạn' } } })
    const wrapper = mount(VisitorPass)
    await flushPromises()
    expect(wrapper.text()).toContain('hết hạn')
  })
})

describe('ForcePasswordChange', () => {
  it('renders the inner component and redirects on change', async () => {
    hoisted.route.query = { redirect: '/employees' }
    const wrapper = mount(ForcePasswordChange)
    expect(wrapper.findComponent({ name: 'ForcePasswordChange' }).exists()).toBe(true)
    await wrapper.find('button').trigger('click')
    await flushPromises()
    expect(hoisted.router.replace).toHaveBeenCalledWith('/employees')
  })
})

describe('AboutProject', () => {
  it('renders the team overview', () => {
    const wrapper = mount(AboutProject)
    expect(wrapper.find('.team-overview').exists()).toBe(true)
  })
})
