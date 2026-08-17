import { flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getContractors: vi.fn(),
    createContractor: vi.fn(),
    revokeContractor: vi.fn(),
    getParkingAreas: vi.fn(),
    createParkingPermit: vi.fn(),
  },
}))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const ContractorManagement = (await import('../ContractorManagement.vue')).default

beforeEach(() => vi.clearAllMocks())
afterEach(() => vi.unstubAllGlobals())

describe('ContractorManagement', () => {
  it('loads contractors and computes status stats', async () => {
    enterpriseApi.getContractors.mockImplementation((params) => Promise.resolve({
      data: {
        items: params.pageSize === 1000
          ? [
            { contractorId: 1, fullName: 'A', company: 'X', status: 'Active' },
            { contractorId: 2, fullName: 'B', company: 'Y', status: 'Expiring' },
            { contractorId: 3, fullName: 'C', company: 'Z', status: 'Expired' },
          ]
          : [{ contractorId: 1, fullName: 'A', company: 'X', status: 'Active' }],
        total: 3,
      },
    }))
    const wrapper = mount(ContractorManagement)
    await flushPromises()
    expect(wrapper.find('tbody').text()).toContain('A')
    expect(wrapper.find('tbody').text()).toContain('X')
    const values = wrapper.findAll('.metric-tile strong').map((el) => el.text())
    expect(values[0]).toBe('1')
    expect(values[1]).toBe('1')
    expect(values[2]).toBe('1')
  })

  it('revokes a contractor through the modal', async () => {
    enterpriseApi.getContractors.mockResolvedValue({ data: { items: [{ contractorId: 5, fullName: 'A', company: 'X', status: 'Active' }], total: 1 } })
    const wrapper = mount(ContractorManagement)
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text() === 'Thu hồi').trigger('click')
    await nextTick()
    const textarea = document.body.querySelector('.modal-panel textarea')
    textarea.value = 'Vi phạm nội quy'
    textarea.dispatchEvent(new Event('input'))
    await nextTick()
    enterpriseApi.revokeContractor.mockResolvedValue({})
    document.body.querySelector('.modal-panel .btn-danger').click()
    await flushPromises()
    expect(enterpriseApi.revokeContractor).toHaveBeenCalledWith(5, { reason: 'Vi phạm nội quy' })
    document.body.innerHTML = ''
  })

  it('creates a contractor through the add modal', async () => {
    enterpriseApi.getContractors.mockResolvedValue({ data: { items: [], total: 0 } })
    const wrapper = mount(ContractorManagement)
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text() === 'Thêm nhà thầu').trigger('click')
    const inputs = document.body.querySelectorAll('.modal-panel input.form-control')
    inputs[0].value = 'Nguyễn A'
    inputs[0].dispatchEvent(new Event('input'))
    inputs[1].value = 'Công ty ABC'
    inputs[1].dispatchEvent(new Event('input'))
    const dates = document.body.querySelectorAll('.modal-panel input[type="date"]')
    dates[0].value = '2026-01-01'
    dates[0].dispatchEvent(new Event('input'))
    dates[1].value = '2026-12-31'
    dates[1].dispatchEvent(new Event('input'))
    enterpriseApi.createContractor.mockResolvedValue({})
    const addButton = [...document.body.querySelectorAll('.modal-panel button')].find((b) => b.textContent === 'Thêm')
    addButton.click()
    await flushPromises()
    expect(enterpriseApi.createContractor).toHaveBeenCalledWith(expect.objectContaining({ fullName: 'Nguyễn A', company: 'Công ty ABC' }))
    document.body.innerHTML = ''
  })
})
