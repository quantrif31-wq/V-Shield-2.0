import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/biometricApi', () => ({
  getBiometricOverview: vi.fn(),
  getFaceModelHealth: vi.fn(),
  getFaceEnrollmentJobs: vi.fn(),
}))
vi.mock('../../services/faceVideoApi', () => ({ getEmployeeVideos: vi.fn() }))

const biometricApi = await import('../../services/biometricApi')
const faceVideoApi = await import('../../services/faceVideoApi')
const Biometrics = (await import('../Biometrics.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  biometricApi.getBiometricOverview.mockResolvedValue({ data: { totalEnrolled: 5 } })
  biometricApi.getFaceModelHealth.mockResolvedValue({ data: { status: 'Healthy' } })
  biometricApi.getFaceEnrollmentJobs.mockResolvedValue({ data: { items: [] } })
  faceVideoApi.getEmployeeVideos.mockResolvedValue({ data: [] })
})

describe('Biometrics', () => {
  it('loads the biometric overview on mount', async () => {
    const wrapper = mount(Biometrics)
    await flushPromises()
    expect(biometricApi.getBiometricOverview).toHaveBeenCalled()
    expect(biometricApi.getFaceModelHealth).toHaveBeenCalled()
    expect(wrapper.exists()).toBe(true)
  })
})
