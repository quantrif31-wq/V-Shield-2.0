import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../stores/auth', () => ({ authState: { user: { role: 'Admin' } } }))
vi.mock('../../services/accessLogApi', () => ({ getExceptions: vi.fn() }))
vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getActiveSecurityAlerts: vi.fn(),
    getInterventionRequests: vi.fn(),
    getLaneEvents: vi.fn(),
    getEvidenceItems: vi.fn(),
  },
}))

const accessLogApi = await import('../../services/accessLogApi')
const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const Exceptions = (await import('../Exceptions.vue')).default

const sharedStubs = {
  ExceptionCaseTimeline: true,
}

beforeEach(() => {
  vi.clearAllMocks()
  enterpriseApi.getActiveSecurityAlerts.mockResolvedValue({ data: { items: [], criticalCount: 0 } })
  enterpriseApi.getInterventionRequests.mockResolvedValue({ data: [] })
  enterpriseApi.getLaneEvents.mockResolvedValue({ data: [] })
  enterpriseApi.getEvidenceItems.mockResolvedValue({ data: [] })
})

describe('Exceptions', () => {
  it('loads the exceptions list', async () => {
    accessLogApi.getExceptions.mockResolvedValue({
      data: { items: [{ exceptionLogId: 1, employeeName: 'An', gateName: 'Cổng A', reason: 'Mất thẻ', createdAt: '2026-08-01T00:00:00Z' }], total: 1 },
    })
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(accessLogApi.getExceptions).toHaveBeenCalled()
    expect(wrapper.exists()).toBe(true)
  })
})
