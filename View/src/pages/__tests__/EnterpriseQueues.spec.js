import { flushPromises, mount } from '@vue/test-utils'
import { reactive } from 'vue'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import RedactionQueue from '../RedactionQueue.vue'
import OperationsDashboard from '../OperationsDashboard.vue'

const route = reactive({ query: {} })
const replace = vi.fn()
const api = vi.hoisted(() => ({
  getRedactionRequests: vi.fn(), approveRedaction: vi.fn(), verifyRedaction: vi.fn(), performRedaction: vi.fn(),
  overview: vi.fn(), configHealth: vi.fn(), getCorrelations: vi.fn(), getBackupRuns: vi.fn(),
}))
vi.mock('vue-router', () => ({ useRoute: () => route, useRouter: () => ({ replace }) }))
vi.mock('../../services/enterpriseSecurityApi', () => ({ enterpriseApi: api }))

describe('Enterprise queue and dashboard states', () => {
  beforeEach(() => {
    route.query = {}
    replace.mockReset()
    api.getRedactionRequests.mockResolvedValue({ data: [{ redactionRequestId: 9, evidenceItemId: 4, privacyLabel: 'Biometric', reason: 'Che khuôn mặt người không liên quan', status: 'PendingApproval' }] })
    api.overview.mockResolvedValue({ data: { totalEvents: 120, pendingEvents: 4, dispatchedEvents: 110, failedEvents: 3, deadLetter: 3 } })
    api.configHealth.mockResolvedValue({ data: [{ category: 'Event Bus', status: 'Healthy', findings: [] }] })
    api.getCorrelations.mockResolvedValue({ data: [{ time: '08:00', count: 7 }, { time: '09:00', count: 12 }] })
    api.getBackupRuns.mockResolvedValue({ data: [{ backupRunId: 2, profile: 'Nightly', status: 'Completed', startedAtUtc: '2026-08-05T01:00:00', sizeBytes: 1048576, targetRpoMinutes: 30, targetRtoMinutes: 60 }] })
  })

  it('renders redaction workflow and requires shared confirmation before approval', async () => {
    const wrapper = mount(RedactionQueue, { global: { stubs: { RouterLink: true, Teleport: true } } })
    await flushPromises()
    expect(wrapper.text()).toContain('Sinh trắc học')
    await wrapper.findAll('button').find((button) => button.text() === 'Phê duyệt').trigger('click')
    expect(wrapper.text()).toContain('Phê duyệt yêu cầu redaction?')
    expect(api.approveRedaction).not.toHaveBeenCalled()
  })

  it('renders real trend data and backup health on operations dashboard', async () => {
    const wrapper = mount(OperationsDashboard, { global: { stubs: { RouterLink: true } } })
    await flushPromises()
    expect(wrapper.text()).toContain('08:00')
    expect(wrapper.text()).toContain('12')
    expect(wrapper.text()).toContain('Nightly')
    expect(wrapper.text()).toContain('Completed')
  })
})
