import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    overview: vi.fn(),
    configHealth: vi.fn(),
    getCorrelations: vi.fn(),
    getBackupRuns: vi.fn(),
  },
}))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const OperationsDashboard = (await import('../OperationsDashboard.vue')).default

function fulfilledOverview(operationsData) {
  return Array.from({ length: 9 }, (_, i) => (i === 7 ? { data: operationsData } : { data: {} }))
}

beforeEach(() => vi.clearAllMocks())

describe('OperationsDashboard', () => {
  it('loads event summary, config health and backups', async () => {
    enterpriseApi.overview.mockResolvedValue(fulfilledOverview({
      totalEvents: 10, pendingEvents: 2, dispatchedEvents: 8, failedEvents: 0, deadLetter: 0,
    }))
    enterpriseApi.configHealth.mockResolvedValue({ data: [{ category: 'Cơ sở dữ liệu', status: 'Healthy', findings: [] }] })
    enterpriseApi.getCorrelations.mockResolvedValue({ data: [] })
    enterpriseApi.getBackupRuns.mockResolvedValue({
      data: [{ backupRunId: 1, profile: 'main', status: 'Completed', startedAtUtc: '2026-08-01T00:00:00Z', sizeBytes: 1048576, targetRpoMinutes: 15, targetRtoMinutes: 30 }],
    })
    const wrapper = mount(OperationsDashboard)
    await flushPromises()
    expect(enterpriseApi.overview).toHaveBeenCalled()
    expect(wrapper.text()).toContain('Tổng event')
    expect(wrapper.text()).toContain('10')
    expect(wrapper.text()).toContain('1.00 MB')
  })

  it('shows an error state when loading fails', async () => {
    enterpriseApi.overview.mockRejectedValue(new Error('down'))
    const wrapper = mount(OperationsDashboard)
    await flushPromises()
    expect(wrapper.text()).toContain('Không thể tải bảng điều hành')
  })
})
