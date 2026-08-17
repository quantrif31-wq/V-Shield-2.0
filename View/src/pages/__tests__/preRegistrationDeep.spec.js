import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({ route: { query: {} }, router: { replace: vi.fn() } }))

vi.mock('vue-router', () => ({ useRoute: () => hoisted.route, useRouter: () => hoisted.router }))
vi.mock('../../services/preRegistrationApi', () => ({
  getAll: vi.fn(),
  getDetail: vi.fn(),
  updateStatus: vi.fn(),
  createLink: vi.fn(),
}))
vi.mock('../../services/employeeApi', () => ({ getAll: vi.fn() }))
vi.mock('qrcode', () => ({ default: { toDataURL: vi.fn().mockResolvedValue('data:image/png;base64,QR') } }))

const preRegistrationApi = await import('../../services/preRegistrationApi')
const employeeApi = await import('../../services/employeeApi')
const PreRegistration = (await import('../PreRegistration.vue')).default

const sharedStubs = { ImportModal: true, ExportModal: true }

beforeEach(() => {
  vi.clearAllMocks()
  hoisted.route.query = {}
  preRegistrationApi.getAll.mockResolvedValue({
    data: { items: [{ registrationId: 1, guestFullName: 'Khách A', status: 'Pending' }], total: 1 },
  })
  employeeApi.getAll.mockResolvedValue({ data: [] })
})

describe('PreRegistration approval', () => {
  it('approves a pending registration', async () => {
    const wrapper = mount(PreRegistration, { global: { stubs: sharedStubs } })
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text() === 'Duyệt').trigger('click')
    await flushPromises()
    expect(preRegistrationApi.updateStatus).toHaveBeenCalledWith(1, 'Approved')
  })

  it('opens the reject dialog for a pending registration', async () => {
    const wrapper = mount(PreRegistration, { global: { stubs: sharedStubs } })
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text() === 'Từ chối').trigger('click')
    await flushPromises()
    expect(wrapper.findComponent({ name: 'ConfirmDialog' }).exists()).toBe(true)
  })
})
