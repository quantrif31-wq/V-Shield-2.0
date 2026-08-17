import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getComplianceReports: vi.fn(),
    runComplianceReport: vi.fn(),
    getCorrelations: vi.fn(),
    getCorrelationDetail: vi.fn(),
    runCorrelation: vi.fn(),
  },
}))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const ComplianceReports = (await import('../ComplianceReports.vue')).default
const CorrelationView = (await import('../CorrelationView.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  enterpriseApi.getComplianceReports.mockResolvedValue({ data: [] })
  enterpriseApi.getCorrelations.mockResolvedValue({ data: [] })
})
afterEach(() => vi.unstubAllGlobals())

describe('ComplianceReports run', () => {
  it('runs a report using the prompted inputs', async () => {
    const wrapper = mount(ComplianceReports)
    await flushPromises()
    const promptSpy = vi.spyOn(window, 'prompt')
      .mockReturnValueOnce('AccessReview')
      .mockReturnValueOnce('30')
    enterpriseApi.runComplianceReport.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Chạy báo cáo').trigger('click')
    await flushPromises()
    expect(enterpriseApi.runComplianceReport).toHaveBeenCalledWith(expect.objectContaining({ reportType: 'AccessReview' }))
    promptSpy.mockRestore()
  })
})

describe('CorrelationView run', () => {
  it('runs a correlation analysis', async () => {
    const wrapper = mount(CorrelationView)
    await flushPromises()
    enterpriseApi.runCorrelation.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Chạy tương quan').trigger('click')
    await flushPromises()
    expect(enterpriseApi.runCorrelation).toHaveBeenCalledWith(expect.objectContaining({ minimumEvents: 2 }))
  })
})
