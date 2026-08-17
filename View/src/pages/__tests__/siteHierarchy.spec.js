import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getHierarchy: vi.fn(),
    getBackfillStatus: vi.fn(),
    configHealth: vi.fn(),
    getLaneHealth: vi.fn(),
    getPolicyOverview: vi.fn(),
  },
}))
vi.mock('../../services/accessLogApi', () => ({ getSystemAuditLogs: vi.fn() }))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const accessLogApi = await import('../../services/accessLogApi')
const SiteHierarchy = (await import('../SiteHierarchy.vue')).default

const sharedStubs = {
  SpatialInfrastructureWorkspace: true,
  ImportModal: true,
  ExportModal: true,
}

beforeEach(() => {
  vi.clearAllMocks()
  enterpriseApi.getHierarchy.mockResolvedValue({ data: [] })
  enterpriseApi.getBackfillStatus.mockResolvedValue({ data: {} })
  enterpriseApi.configHealth.mockResolvedValue({ data: [] })
  enterpriseApi.getLaneHealth.mockResolvedValue({ data: [] })
  enterpriseApi.getPolicyOverview.mockResolvedValue({ data: {} })
  accessLogApi.getSystemAuditLogs.mockResolvedValue({ data: { items: [] } })
})

describe('SiteHierarchy', () => {
  it('loads the hierarchy on mount', async () => {
    const wrapper = mount(SiteHierarchy, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(enterpriseApi.getHierarchy).toHaveBeenCalled()
    expect(wrapper.exists()).toBe(true)
  })
})
