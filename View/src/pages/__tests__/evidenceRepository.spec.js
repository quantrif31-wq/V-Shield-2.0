import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getEvidenceItems: vi.fn(),
    getEvidenceOverview: vi.fn(),
    getRetentionPolicies: vi.fn(),
    getLegalHolds: vi.fn(),
  },
}))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const EvidenceRepository = (await import('../EvidenceRepository.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  enterpriseApi.getEvidenceOverview.mockResolvedValue({ data: { totalItems: 10 } })
  enterpriseApi.getRetentionPolicies.mockResolvedValue({ data: { items: [] } })
  enterpriseApi.getLegalHolds.mockResolvedValue({ data: { items: [] } })
})

describe('EvidenceRepository', () => {
  it('loads evidence items and governance data', async () => {
    enterpriseApi.getEvidenceItems.mockResolvedValue({
      data: { items: [{ evidenceItemId: 1, label: 'Video cổng A', status: 'Verified' }], total: 1 },
    })
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    expect(enterpriseApi.getEvidenceItems).toHaveBeenCalled()
    expect(enterpriseApi.getRetentionPolicies).toHaveBeenCalled()
    expect(enterpriseApi.getLegalHolds).toHaveBeenCalled()
  })
})
