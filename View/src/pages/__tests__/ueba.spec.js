import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/uebaApi', () => ({
  getUebaProfiles: vi.fn(),
  rebuildUebaProfile: vi.fn(),
  getUebaAnomalies: vi.fn(),
  resolveUebaAnomaly: vi.fn(),
  markUebaAnomalyFalsePositive: vi.fn(),
  getUebaSummary: vi.fn(),
  explainEmployeeRisk: vi.fn(),
}))

const uebaApi = await import('../../services/uebaApi')
const UEBA = (await import('../UEBA.vue')).default

beforeEach(() => vi.clearAllMocks())

describe('UEBA', () => {
  it('loads summary, profiles and anomalies on mount', async () => {
    uebaApi.getUebaSummary.mockResolvedValue({ data: { riskProfiles: 3, openAnomalies: 2 } })
    uebaApi.getUebaProfiles.mockResolvedValue({ data: [{ employeeId: 7, employee: { fullName: 'An' }, riskLevel: 'Medium' }] })
    uebaApi.getUebaAnomalies.mockResolvedValue({ data: { items: [{ anomalyId: 1, severity: 'High', status: 'Open' }] } })
    const wrapper = mount(UEBA)
    await flushPromises()
    expect(uebaApi.getUebaSummary).toHaveBeenCalled()
    expect(uebaApi.getUebaProfiles).toHaveBeenCalled()
    expect(uebaApi.getUebaAnomalies).toHaveBeenCalled()
    expect(wrapper.text()).toContain('An')
  })

  it('filters profiles by search query', async () => {
    uebaApi.getUebaSummary.mockResolvedValue({ data: {} })
    uebaApi.getUebaProfiles.mockResolvedValue({
      data: [
        { employeeId: 7, employee: { fullName: 'Nguyễn An' }, riskLevel: 'Low' },
        { employeeId: 8, employee: { fullName: 'Trần B' }, riskLevel: 'High' },
      ],
    })
    uebaApi.getUebaAnomalies.mockResolvedValue({ data: { items: [] } })
    const wrapper = mount(UEBA)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Hồ sơ')).trigger('click')
    await flushPromises()
    await wrapper.find('.card.panel input').setValue('Trần')
    await flushPromises()
    expect(wrapper.text()).toContain('Trần B')
    expect(wrapper.text()).not.toContain('Nguyễn An')
  })
})
