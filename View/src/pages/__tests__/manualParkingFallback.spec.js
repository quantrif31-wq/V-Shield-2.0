import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/employeeApi', () => ({ getAll: vi.fn(), getProtectedFaceImage: vi.fn() }))
vi.mock('../../services/guestProfileApi', () => ({ getVisitorDirectory: vi.fn() }))
vi.mock('../../services/dynamicQrVerifyApi', () => ({ verifyDynamicQr: vi.fn() }))
vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getLaneHealth: vi.fn(),
    getParkingAreas: vi.fn(),
  },
}))

const employeeApi = await import('../../services/employeeApi')
const guestProfileApi = await import('../../services/guestProfileApi')
const dynamicQrVerifyApi = await import('../../services/dynamicQrVerifyApi')
const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const ManualParkingFallback = (await import('../ManualParkingFallback.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  employeeApi.getAll.mockResolvedValue({ data: [] })
  guestProfileApi.getVisitorDirectory.mockResolvedValue({ data: { items: [] } })
  enterpriseApi.getLaneHealth.mockResolvedValue({ data: [] })
  enterpriseApi.getParkingAreas.mockResolvedValue({ data: { items: [] } })
})

describe('ManualParkingFallback', () => {
  it('loads lane health and parking areas on mount', async () => {
    const wrapper = mount(ManualParkingFallback)
    await flushPromises()
    expect(enterpriseApi.getLaneHealth).toHaveBeenCalled()
    expect(enterpriseApi.getParkingAreas).toHaveBeenCalled()
  })
})
