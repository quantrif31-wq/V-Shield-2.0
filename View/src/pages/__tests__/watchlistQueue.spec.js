import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({
  route: { query: {} },
  router: { replace: vi.fn() },
}))

vi.mock('vue-router', () => ({ useRoute: () => hoisted.route, useRouter: () => hoisted.router }))
vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getWatchlistMatches: vi.fn(),
    getWatchlistEntries: vi.fn(),
  },
}))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const WatchlistQueue = (await import('../WatchlistQueue.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  hoisted.route.query = {}
  enterpriseApi.getWatchlistMatches.mockResolvedValue({ data: { items: [], total: 0 } })
  enterpriseApi.getWatchlistEntries.mockResolvedValue({ data: [] })
})

describe('WatchlistQueue', () => {
  it('loads watchlist matches on mount', async () => {
    const wrapper = mount(WatchlistQueue)
    await flushPromises()
    expect(enterpriseApi.getWatchlistMatches).toHaveBeenCalled()
  })

  it('loads entries when the entries tab is active', async () => {
    hoisted.route.query = { tab: 'entries' }
    const wrapper = mount(WatchlistQueue)
    await flushPromises()
    expect(enterpriseApi.getWatchlistEntries).toHaveBeenCalled()
  })
})
