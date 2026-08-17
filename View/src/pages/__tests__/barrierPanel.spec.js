import { flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getBarriers: vi.fn(),
    getLaneHealth: vi.fn(),
    getParkingPermits: vi.fn(),
    recordBarrierCommand: vi.fn(),
    simulateBarrierCommand: vi.fn(),
    getBarrierCommands: vi.fn(),
    createBarrier: vi.fn(),
  },
}))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const BarrierPanel = (await import('../BarrierPanel.vue')).default

const sharedStubs = {
  StepUpModal: true,
  AuditReceiptToast: true,
}

beforeEach(() => vi.clearAllMocks())
afterEach(() => {
  document.body.innerHTML = ''
  vi.unstubAllGlobals()
})

describe('BarrierPanel', () => {
  it('loads barriers and lane health', async () => {
    enterpriseApi.getBarriers.mockResolvedValue({ data: [{ barrierId: 1, name: 'Rào A', lane: { name: 'Lane 1' }, state: 'Closed', isActive: true }] })
    enterpriseApi.getLaneHealth.mockResolvedValue({ data: { lanes: [] } })
    const wrapper = mount(BarrierPanel, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(wrapper.find('tbody').text()).toContain('Rào A')
    expect(wrapper.find('tbody').text()).toContain('Đóng')
  })

  it('sends a barrier command after providing a reason', async () => {
    enterpriseApi.getBarriers.mockResolvedValue({ data: [{ barrierId: 1, name: 'Rào A', state: 'Closed' }] })
    enterpriseApi.getLaneHealth.mockResolvedValue({ data: { lanes: [] } })
    const wrapper = mount(BarrierPanel, { global: { stubs: sharedStubs } })
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text() === 'Mở').trigger('click')
    await nextTick()
    const textarea = document.body.querySelector('.modal-panel textarea')
    textarea.value = 'Cần mở cổng'
    textarea.dispatchEvent(new Event('input'))
    await nextTick()
    enterpriseApi.recordBarrierCommand.mockResolvedValue({ data: { audit: { auditReceiptId: 'r1' } } })
    document.body.querySelector('.modal-panel .btn-primary').click()
    await flushPromises()
    expect(enterpriseApi.recordBarrierCommand).toHaveBeenCalledWith(1, expect.objectContaining({ command: 'Open' }))
  })

  it('loads parking permits', async () => {
    enterpriseApi.getBarriers.mockResolvedValue({ data: [] })
    enterpriseApi.getLaneHealth.mockResolvedValue({ data: { lanes: [] } })
    enterpriseApi.getParkingPermits.mockResolvedValue({ data: { items: [{ parkingPermitId: 1, parkingArea: { name: 'A1' }, vehicle: { licensePlate: '29A-1' }, permitType: 'Staff', isRevoked: false, validFromUtc: '2026-01-01T00:00:00Z', validToUtc: '2026-12-31T00:00:00Z' }] } })
    const wrapper = mount(BarrierPanel, { global: { stubs: sharedStubs } })
    await flushPromises()

    await wrapper.findAll('.tab-bar button')[1].trigger('click')
    await wrapper.findAll('button').find((b) => b.text() === 'Làm mới').trigger('click')
    await flushPromises()
    expect(enterpriseApi.getParkingPermits).toHaveBeenCalled()
    expect(wrapper.find('tbody').text()).toContain('29A-1')
  })
})
