import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getReceptionOverview: vi.fn(),
    getReceptionBoard: vi.fn(),
    getReceptionLostFound: vi.fn(),
  },
}))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const ReceptionDashboard = (await import('../ReceptionDashboard.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  enterpriseApi.getReceptionOverview.mockResolvedValue({ data: { expectedToday: 5, checkedInToday: 3 } })
  enterpriseApi.getReceptionBoard.mockResolvedValue({ data: { items: [] } })
  enterpriseApi.getReceptionLostFound.mockResolvedValue({ data: { items: [] } })
})

describe('ReceptionDashboard', () => {
  it('loads the reception overview and board', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    expect(enterpriseApi.getReceptionOverview).toHaveBeenCalled()
    expect(enterpriseApi.getReceptionBoard).toHaveBeenCalled()
    expect(wrapper.exists()).toBe(true)
  })
})
