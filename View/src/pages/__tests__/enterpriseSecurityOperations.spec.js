import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../stores/auth', () => ({ authState: { user: { role: 'Admin' } } }))
vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    overview: vi.fn(),
    configHealth: vi.fn(),
    assetMap: vi.fn(),
    getLaneHealth: vi.fn(),
    getActiveSecurityAlerts: vi.fn(),
    getHealthSummary: vi.fn(),
  },
  socIntelApi: { getIntelligence: vi.fn() },
}))
vi.mock('../../services/enterpriseAiApi', () => ({ enterpriseAiApi: {} }))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const socIntelApi = (await import('../../services/enterpriseSecurityApi')).socIntelApi
const EnterpriseSecurityOperations = (await import('../EnterpriseSecurityOperations.vue')).default

function overviewArray() {
  return Array.from({ length: 9 }, () => ({ data: {} }))
}

beforeEach(() => {
  vi.clearAllMocks()
  enterpriseApi.overview.mockResolvedValue(overviewArray())
  enterpriseApi.configHealth.mockResolvedValue({ data: [] })
  enterpriseApi.assetMap.mockResolvedValue({ data: {} })
  enterpriseApi.getLaneHealth.mockResolvedValue({ data: [] })
  enterpriseApi.getActiveSecurityAlerts.mockResolvedValue({ data: { items: [], criticalCount: 0 } })
  enterpriseApi.getHealthSummary.mockResolvedValue({ data: null })
  socIntelApi.getIntelligence.mockResolvedValue({ data: {} })
})

describe('EnterpriseSecurityOperations', () => {
  it('loads the enterprise overview on mount', async () => {
    const wrapper = mount(EnterpriseSecurityOperations)
    await flushPromises()
    expect(enterpriseApi.overview).toHaveBeenCalled()
    expect(wrapper.exists()).toBe(true)
  })
})
