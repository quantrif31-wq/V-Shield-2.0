import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getVisits: vi.fn(),
    createVisit: vi.fn(),
    getSites: vi.fn(),
    getFormTemplates: vi.fn(),
  },
}))
vi.mock('../../services/employeeApi', () => ({ getAll: vi.fn() }))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const HostVisitorPage = (await import('../HostVisitorPage.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  sessionStorage.setItem('v_shield_user', JSON.stringify({ employeeId: 5, fullName: 'Host A' }))
  enterpriseApi.getSites.mockResolvedValue({ data: [] })
  enterpriseApi.getFormTemplates.mockResolvedValue({ data: { items: [] } })
})

describe('HostVisitorPage', () => {
  it('loads the host visits from the API', async () => {
    enterpriseApi.getVisits.mockResolvedValue({ data: { items: [{ visitId: 1, visitorName: 'Khách X', status: 'Approved' }] } })
    const wrapper = mount(HostVisitorPage)
    await flushPromises()
    expect(enterpriseApi.getVisits).toHaveBeenCalledWith(expect.objectContaining({ hostEmployeeId: 5 }))
    expect(wrapper.find('tbody').text()).toContain('Khách X')
  })

  it('creates a visit invitation', async () => {
    enterpriseApi.getVisits.mockResolvedValue({ data: { items: [] } })
    const wrapper = mount(HostVisitorPage)
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text().toLowerCase().includes('lời mời') && b.text().toLowerCase().includes('mới')).trigger('click')
    const inputs = wrapper.findAll('section.form-control, .form-control')
    await wrapper.findAll('.form-control')[0].setValue('Khách Mới')
    const dateInputs = wrapper.findAll('input[type="datetime-local"]')
    await dateInputs[0].setValue('2026-08-10T09:00')
    await dateInputs[1].setValue('2026-08-10T17:00')
    enterpriseApi.createVisit.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text().includes('Gửi lời mời')).trigger('click')
    await flushPromises()
    expect(enterpriseApi.createVisit).toHaveBeenCalledWith(expect.objectContaining({ visitorName: 'Khách Mới', hostEmployeeId: 5 }))
  })
})
