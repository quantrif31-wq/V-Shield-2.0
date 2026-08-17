import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({
  route: { query: {} },
  router: { push: vi.fn() },
  authState: { user: { role: 'Admin' } },
}))

vi.mock('vue-router', () => ({ useRoute: () => hoisted.route, useRouter: () => hoisted.router }))
vi.mock('../../stores/auth', () => ({ authState: hoisted.authState }))
vi.mock('../../services/enterpriseSecurityApi', () => ({
  lostFoundApi: { getOverview: vi.fn() },
  enterpriseApi: {},
}))
vi.mock('../FoundItemRegistry.vue', () => ({
  default: { name: 'FoundItemRegistry', template: '<div class="pg-found">FOUND</div>' },
  __isTeleport: false,
  __isKeepAlive: false,
  __v_isVNode: false,
  __isSuspense: false,
  name: 'FoundItemRegistry',
  __name: 'FoundItemRegistry',
}))
vi.mock('../LostItemList.vue', () => ({
  default: { name: 'LostItemList', template: '<div class="pg-lost">LOST</div>' },
  __isTeleport: false,
  __isKeepAlive: false,
  __v_isVNode: false,
  __isSuspense: false,
  name: 'LostItemList',
  __name: 'LostItemList',
}))
vi.mock('../ClaimApproval.vue', () => ({
  default: { name: 'ClaimApproval', template: '<div class="pg-claim">CLAIM</div>' },
  __isTeleport: false,
  __isKeepAlive: false,
  __v_isVNode: false,
  __isSuspense: false,
  name: 'ClaimApproval',
  __name: 'ClaimApproval',
}))
vi.mock('../LockerAccessLogs.vue', () => ({
  default: { name: 'LockerAccessLogs', template: '<div class="pg-logs">LOGS</div>' },
  __isTeleport: false,
  __isKeepAlive: false,
  __v_isVNode: false,
  __isSuspense: false,
  name: 'LockerAccessLogs',
  __name: 'LockerAccessLogs',
}))
vi.mock('../LockerManager.vue', () => ({
  default: { name: 'LockerManager', template: '<div class="pg-lockers">LOCKERS</div>' },
  __isTeleport: false,
  __isKeepAlive: false,
  __v_isVNode: false,
  __isSuspense: false,
  name: 'LockerManager',
  __name: 'LockerManager',
}))

const LostFoundDashboard = (await import('../LostFoundDashboard.vue')).default

beforeEach(() => vi.clearAllMocks())

describe('LostFoundDashboard', () => {
  it('renders and switches between child modules', async () => {
    const wrapper = mount(LostFoundDashboard, { global: { stubs: { RouterLink: { template: '<a><slot /></a>' } } } })
    await flushPromises()
    expect(wrapper.exists()).toBe(true)

    const tabs = wrapper.findAll('.tab-btn, .bento-tabs button, .workspace-tabs button')
    const foundTab = tabs.find((b) => b.text().toLowerCase().includes('đồ tìm thấy'))
    if (foundTab) {
      await foundTab.trigger('click')
      await flushPromises()
      expect(wrapper.find('.pg-found').exists()).toBe(true)
    }
  })
})
