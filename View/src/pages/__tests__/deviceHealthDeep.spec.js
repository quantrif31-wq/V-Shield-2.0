import { flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getHealthInsights: vi.fn(),
    getHealthSummary: vi.fn(),
    getDeviceHealthHistory: vi.fn(),
    getDeviceConfigurations: vi.fn(),
    diagnoseDevice: vi.fn(),
    recordHealth: vi.fn(),
  },
}))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const DeviceHealth = (await import('../DeviceHealth.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  enterpriseApi.getHealthInsights.mockResolvedValue({ data: [] })
  enterpriseApi.getHealthSummary.mockResolvedValue({ data: null })
})
afterEach(() => {
  document.body.innerHTML = ''
})

describe('DeviceHealth record', () => {
  it('records device health through the modal', async () => {
    const wrapper = mount(DeviceHealth)
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text().includes('Ghi nhận sức khỏe')).trigger('click')
    await nextTick()
    const deviceInput = document.body.querySelector('.modal-panel input[type="number"]')
    deviceInput.value = '3'
    deviceInput.dispatchEvent(new Event('input'))
    await nextTick()

    enterpriseApi.recordHealth.mockResolvedValue({})
    const saveBtn = [...document.body.querySelectorAll('.modal-panel button')].find((b) => b.textContent.includes('Ghi nhận'))
    saveBtn.click()
    await flushPromises()
    expect(enterpriseApi.recordHealth).toHaveBeenCalledWith(3, expect.objectContaining({ status: 'Ok' }))
  })
})
