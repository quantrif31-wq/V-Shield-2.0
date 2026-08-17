import { flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getTopology: vi.fn(),
    getAdapters: vi.fn(),
    getConnectorStatus: vi.fn(),
    getHealthSummary: vi.fn(),
    getDeviceReaders: vi.fn(),
    getDeviceRelays: vi.fn(),
    getDeviceSensors: vi.fn(),
    getDeviceHealthHistory: vi.fn(),
    diagnoseDevice: vi.fn(),
    recordHealth: vi.fn(),
    getOfflinePolicyPackages: vi.fn(),
    createOfflinePolicyPackage: vi.fn(),
  },
}))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const DeviceTopology = (await import('../DeviceTopology.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  enterpriseApi.getAdapters.mockResolvedValue({ data: { adapters: [] } })
  enterpriseApi.getConnectorStatus.mockResolvedValue({ data: [] })
  enterpriseApi.getHealthSummary.mockResolvedValue({ data: null })
})

afterEach(() => {
  document.body.innerHTML = ''
})

describe('DeviceTopology', () => {
  it('loads the device topology', async () => {
    enterpriseApi.getTopology.mockResolvedValue({ data: [{ securityDeviceId: 1, name: 'Controller-1', deviceType: 'Controller', status: 'Online', vendor: 'HID' }] })
    const wrapper = mount(DeviceTopology)
    await flushPromises()
    expect(enterpriseApi.getTopology).toHaveBeenCalled()
    expect(wrapper.find('tbody').text()).toContain('Controller-1')
  })

  it('opens the device detail drawer and loads a tab', async () => {
    enterpriseApi.getTopology.mockResolvedValue({ data: [{ securityDeviceId: 1, name: 'Controller-1', deviceType: 'Controller', status: 'Online' }] })
    enterpriseApi.getDeviceReaders.mockResolvedValue({ data: [{ readerId: 1, name: 'Reader-1' }] })
    const wrapper = mount(DeviceTopology)
    await flushPromises()

    await wrapper.find('.device-row').trigger('click')
    await nextTick()
    const tabButtons = [...document.body.querySelectorAll('.drawer-tabs button')]
    tabButtons[1].click()
    await flushPromises()
    expect(enterpriseApi.getDeviceReaders).toHaveBeenCalledWith(1)
    expect(document.body.textContent).toContain('Reader-1')
  })

  it('runs an AI diagnosis on a device', async () => {
    enterpriseApi.getTopology.mockResolvedValue({ data: [{ securityDeviceId: 2, name: 'Camera-2', deviceType: 'Camera', status: 'Online' }] })
    enterpriseApi.diagnoseDevice.mockResolvedValue({ data: { diagnosis: 'Cần vệ sinh' } })
    const wrapper = mount(DeviceTopology)
    await flushPromises()

    await wrapper.find('.device-row').trigger('click')
    await nextTick()
    const diagnoseButton = [...document.body.querySelectorAll('.drawer-panel button')].find((b) => b.textContent.toLowerCase().includes('chẩn đoán'))
    diagnoseButton.click()
    await flushPromises()
    expect(enterpriseApi.diagnoseDevice).toHaveBeenCalledWith(2)
  })
})
