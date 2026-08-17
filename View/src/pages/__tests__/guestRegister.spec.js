import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({ route: { params: {} } }))

vi.mock('vue-router', () => ({ useRoute: () => hoisted.route }))
vi.mock('../../services/preRegistrationApi', () => ({ validateToken: vi.fn(), submitRegistration: vi.fn() }))

const preRegistrationApi = await import('../../services/preRegistrationApi')
const GuestRegister = (await import('../GuestRegister.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  hoisted.route.params = { token: 'tok-1' }
})

describe('GuestRegister', () => {
  it('validates the registration token on mount', async () => {
    preRegistrationApi.validateToken.mockResolvedValue({ data: { fullName: 'Khách A' } })
    const wrapper = mount(GuestRegister)
    await flushPromises()
    expect(preRegistrationApi.validateToken).toHaveBeenCalledWith('tok-1')
    expect(wrapper.exists()).toBe(true)
  })

  it('shows an error for an invalid token', async () => {
    preRegistrationApi.validateToken.mockRejectedValue({ response: { data: { message: 'Token không hợp lệ' } } })
    const wrapper = mount(GuestRegister)
    await flushPromises()
    expect(wrapper.text()).toContain('Token không hợp lệ')
  })
})
