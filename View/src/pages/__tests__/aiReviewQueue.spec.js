import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({
  route: { query: {} },
  router: { replace: vi.fn() },
}))

vi.mock('vue-router', () => ({ useRoute: () => hoisted.route, useRouter: () => hoisted.router }))
vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getAiAdjudications: vi.fn(),
    getAiMetrics: vi.fn(),
    getAiMetricsSummary: vi.fn(),
    reviewAiAdjudication: vi.fn(),
  },
}))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const AiReviewQueue = (await import('../AiReviewQueue.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  hoisted.route.query = {}
  enterpriseApi.getAiAdjudications.mockResolvedValue({ data: { items: [] } })
  enterpriseApi.getAiMetrics.mockResolvedValue({ data: [] })
  enterpriseApi.getAiMetricsSummary.mockResolvedValue({ data: {} })
})

describe('AiReviewQueue', () => {
  it('loads adjudications, metrics and summary on mount', async () => {
    const wrapper = mount(AiReviewQueue)
    await flushPromises()
    expect(enterpriseApi.getAiAdjudications).toHaveBeenCalled()
    expect(enterpriseApi.getAiMetrics).toHaveBeenCalled()
    expect(enterpriseApi.getAiMetricsSummary).toHaveBeenCalled()
  })
})
