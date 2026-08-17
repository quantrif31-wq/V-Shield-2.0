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

const sharedStubs = { StepUpModal: true, AuditReceiptToast: true }

beforeEach(() => {
  vi.clearAllMocks()
  enterpriseApi.getBarriers.mockResolvedValue({ data: [] })
  enterpriseApi.getLaneHealth.mockResolvedValue({ data: { lanes: [] } })
})
afterEach(() => {
  document.body.innerHTML = ''
})

describe('BarrierPanel add', () => {
  it('creates a barrier through the modal', async () => {
    const wrapper = mount(BarrierPanel, { global: { stubs: sharedStubs } })
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text() === 'Thêm rào chắn').trigger('click')
    await nextTick()
    const nameInput = document.body.querySelector('.modal-panel input')
    if (nameInput) {
      nameInput.value = 'Rào Cổng A'
      nameInput.dispatchEvent(new Event('input'))
    }
    await nextTick()
    enterpriseApi.createBarrier.mockResolvedValue({})
    const addBtn = [...document.body.querySelectorAll('.modal-panel button')].find((b) => b.textContent.trim() === 'Thêm')
    if (addBtn) addBtn.click()
    await flushPromises()
    expect(enterpriseApi.createBarrier).toHaveBeenCalled()
  })
})
