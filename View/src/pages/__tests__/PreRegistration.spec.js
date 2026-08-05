import { flushPromises, mount } from '@vue/test-utils'
import { reactive } from 'vue'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import PreRegistration from '../PreRegistration.vue'

const route = reactive({ query: {} })
const replace = vi.fn()
const getAll = vi.fn()
const getDetail = vi.fn()
const updateStatus = vi.fn()
const createLink = vi.fn()

vi.mock('vue-router', () => ({ useRoute: () => route, useRouter: () => ({ replace }) }))
vi.mock('../../services/preRegistrationApi', () => ({
  getAll: (...args) => getAll(...args),
  getDetail: (...args) => getDetail(...args),
  updateStatus: (...args) => updateStatus(...args),
  createLink: (...args) => createLink(...args),
}))
vi.mock('../../services/employeeApi', () => ({ getAll: vi.fn(() => Promise.resolve({ data: [{ employeeId: 4, fullName: 'Trần Minh Host', departmentName: 'An ninh' }] })) }))
vi.mock('qrcode', () => ({ default: { toCanvas: vi.fn(() => Promise.resolve()) } }))

const registration = {
  registrationId: 12,
  guestFullName: 'Lê Thị Khách',
  guestPhone: '0912345678',
  hostEmployeeName: 'Trần Minh Host',
  expectedTimeIn: '2026-08-05T08:00:00',
  expectedTimeOut: '2026-08-05T10:00:00',
  numberOfVisitors: 2,
  status: 'Pending',
}

describe('Pre-registration visitor module', () => {
  beforeEach(() => {
    route.query = {}
    replace.mockReset()
    getAll.mockResolvedValue({ data: { items: [registration], total: 1 } })
    getDetail.mockResolvedValue({ data: { ...registration, visitors: [], accessLogs: [] } })
    updateStatus.mockResolvedValue({ data: {} })
    createLink.mockResolvedValue({ data: { registrationUrl: 'https://example.test/register/abc', expiredAt: '2026-08-06T08:00:00' } })
  })

  it('renders the visitor queue with semantic status and detail action', async () => {
    const wrapper = mount(PreRegistration, { global: { stubs: { RouterLink: true, Teleport: true } } })
    await flushPromises()
    expect(wrapper.text()).toContain('Lê Thị Khách')
    expect(wrapper.text()).toContain('Chờ duyệt')
    await wrapper.findAll('button').find((button) => button.text() === 'Chi tiết').trigger('click')
    await flushPromises()
    expect(getDetail).toHaveBeenCalledWith(12)
    expect(wrapper.text()).toContain('Chi tiết đăng ký #12')
  })

  it('persists status filtering in the route query', async () => {
    const wrapper = mount(PreRegistration, { global: { stubs: { RouterLink: true, Teleport: true } } })
    await flushPromises()
    await wrapper.get('#visitor-status').setValue('Approved')
    expect(replace).toHaveBeenCalledWith({ query: expect.objectContaining({ status: 'Approved' }) })
  })
})
