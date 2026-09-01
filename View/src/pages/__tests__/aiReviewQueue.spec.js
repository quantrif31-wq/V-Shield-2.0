import { reactive } from 'vue'
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
vi.mock('../../composables/useToasts', () => ({ useToasts: () => ({ success: vi.fn(), error: vi.fn() }) }))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const AiReviewQueue = (await import('../AiReviewQueue.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  hoisted.route = reactive({ query: {} })
  hoisted.router.replace.mockReset()
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
    expect(wrapper.exists()).toBe(true)
  })

  it('applies query filters and commits them', async () => {
    hoisted.route.query = { status: 'Pending', outcome: 'Confirmed' }
    const wrapper = mount(AiReviewQueue)
    await flushPromises()
    expect(wrapper.vm.statusFilter).toBe('Pending')
    expect(wrapper.vm.outcomeFilter).toBe('Confirmed')
    expect(enterpriseApi.getAiAdjudications).toHaveBeenCalledWith(expect.objectContaining({ status: 'Pending', outcome: 'Confirmed' }))
    wrapper.vm.statusFilter = 'Reviewed'
    wrapper.vm.commitFilters()
    expect(hoisted.router.replace).toHaveBeenCalledWith({ query: { status: 'Reviewed', outcome: 'Confirmed' } })
  })

  it('ignores unsupported status in query', async () => {
    hoisted.route.query = { status: 'Bogus' }
    const wrapper = mount(AiReviewQueue)
    await flushPromises()
    expect(wrapper.vm.statusFilter).toBe('')
  })

  it('reloads items when route changes', async () => {
    const wrapper = mount(AiReviewQueue)
    await flushPromises()
    const before = enterpriseApi.getAiAdjudications.mock.calls.length
    hoisted.route.query = { status: 'Pending' }
    await flushPromises()
    expect(enterpriseApi.getAiAdjudications.mock.calls.length).toBeGreaterThan(before)
  })

  it('sets permissionDenied on items 403', async () => {
    enterpriseApi.getAiAdjudications.mockRejectedValue({ response: { status: 403 } })
    const wrapper = mount(AiReviewQueue)
    await flushPromises()
    expect(wrapper.vm.permissionDenied).toBe(true)
  })

  it('records items error', async () => {
    enterpriseApi.getAiAdjudications.mockRejectedValue({ response: { data: { message: 'itemsfail' } } })
    const wrapper = mount(AiReviewQueue)
    await flushPromises()
    expect(wrapper.vm.itemsError).toBe('itemsfail')
  })

  it('records metrics error and loads metrics array', async () => {
    enterpriseApi.getAiMetrics.mockRejectedValue({ response: { data: { message: 'metfail' } } })
    const wrapper = mount(AiReviewQueue)
    await flushPromises()
    expect(wrapper.vm.metricsError).toBe('metfail')
  })

  it('rejects non-array metrics', async () => {
    enterpriseApi.getAiMetrics.mockResolvedValue({ data: { not: 'array' } })
    const wrapper = mount(AiReviewQueue)
    await flushPromises()
    expect(wrapper.vm.metrics).toEqual([])
  })

  it('summary falls back to empty on error', async () => {
    enterpriseApi.getAiMetricsSummary.mockRejectedValue({})
    const wrapper = mount(AiReviewQueue)
    await flushPromises()
    expect(wrapper.vm.summary).toEqual({})
  })

  it('opens review and submits it', async () => {
    enterpriseApi.getAiAdjudications.mockResolvedValue({ data: { items: [{ aiAdjudicationItemId: 5, status: 'Pending', outcome: 'Confirmed', source: 'face', confidence: 0.9 }] } })
    const wrapper = mount(AiReviewQueue)
    await flushPromises()
    wrapper.vm.openReview({ aiAdjudicationItemId: 5 })
    expect(wrapper.vm.reviewTarget.aiAdjudicationItemId).toBe(5)
    wrapper.vm.reviewForm.reviewNote = 'note'
    enterpriseApi.reviewAiAdjudication.mockResolvedValue({ data: {} })
    await wrapper.vm.submitReview()
    await flushPromises()
    expect(enterpriseApi.reviewAiAdjudication).toHaveBeenCalledWith(5, expect.objectContaining({ outcome: 'Confirmed', reviewNote: 'note' }))
    expect(wrapper.vm.reviewTarget).toBeNull()
  })

  it('submitReview handles error', async () => {
    const wrapper = mount(AiReviewQueue)
    await flushPromises()
    wrapper.vm.openReview({ aiAdjudicationItemId: 2 })
    enterpriseApi.reviewAiAdjudication.mockRejectedValue({ response: { data: { message: 'subfail' } } })
    await wrapper.vm.submitReview()
    await flushPromises()
    expect(wrapper.vm.reviewError).toBe('subfail')
  })

  it('submitReview early-returns without target', async () => {
    const wrapper = mount(AiReviewQueue)
    await flushPromises()
    wrapper.vm.reviewTarget = null
    await wrapper.vm.submitReview()
    await flushPromises()
    expect(enterpriseApi.reviewAiAdjudication).not.toHaveBeenCalled()
  })

  it('label and date helpers', () => {
    const wrapper = mount(AiReviewQueue)
    expect(wrapper.vm.outcomeSemantic('Confirmed')).toBe('success')
    expect(wrapper.vm.outcomeSemantic('FalsePositive')).toBe('danger')
    expect(wrapper.vm.outcomeSemantic('FalseNegative')).toBe('danger')
    expect(wrapper.vm.outcomeSemantic('TrainingCandidate')).toBe('info')
    expect(wrapper.vm.outcomeSemantic('Other')).toBe('info')
    expect(wrapper.vm.outcomeLabel('Confirmed')).toBe('Xác nhận đúng')
    expect(wrapper.vm.outcomeLabel('FalsePositive')).toBe('Sai dương')
    expect(wrapper.vm.outcomeLabel('FalseNegative')).toBe('Sai âm')
    expect(wrapper.vm.outcomeLabel('TrainingCandidate')).toBe('Mẫu huấn luyện')
    expect(wrapper.vm.outcomeLabel('Weird')).toBe('Weird')
    expect(wrapper.vm.formatDate('2026-01-01T00:00:00Z')).toBeTruthy()
    expect(wrapper.vm.formatDate(null)).toBe('—')
  })

  it('renders adjudication rows', async () => {
    enterpriseApi.getAiAdjudications.mockResolvedValue({
      data: { items: [
        { aiAdjudicationItemId: 1, status: 'Pending', outcome: 'Confirmed', source: 'face', confidence: 0.8, capturedAtUtc: '2026-01-01T00:00:00Z' },
        { aiAdjudicationItemId: 2, status: 'Reviewed', outcome: 'FalsePositive', source: 'vehicle', confidence: 0.3 },
      ] },
    })
    const wrapper = mount(AiReviewQueue)
    await flushPromises()
    expect(wrapper.findAll('tbody tr').length).toBe(2)
  })
})
