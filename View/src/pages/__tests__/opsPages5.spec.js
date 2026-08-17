import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    resetDemoScenarios: vi.fn(),
    createVirtualController: vi.fn(),
    injectSimulatorFault: vi.fn(),
    simulateOfflineScan: vi.fn(),
    recordHealth: vi.fn(),
    registerController: vi.fn(),
    createDevice: vi.fn(),
    getConnectorStatus: vi.fn(),
    getProvisioningRequests: vi.fn(),
    getTopology: vi.fn(),
    createProvisioningRequest: vi.fn(),
    approveProvisioningRequest: vi.fn(),
  },
}))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi

const SimulatorPanel = (await import('../SimulatorPanel.vue')).default
const ProvisioningWizard = (await import('../ProvisioningWizard.vue')).default

beforeEach(() => vi.clearAllMocks())
afterEach(() => vi.unstubAllGlobals())

describe('SimulatorPanel', () => {
  it('creates a virtual controller', async () => {
    const wrapper = mount(SimulatorPanel)
    const panel = wrapper.findAll('.ops-panel')[0]
    await panel.find('input.form-input').setValue('Sim-Controller-1')
    enterpriseApi.createVirtualController.mockResolvedValue({ data: { name: 'Sim-Controller-1', securityDeviceId: 9 } })
    await panel.findAll('button').find((b) => b.text() === 'Tạo').trigger('click')
    await flushPromises()
    expect(enterpriseApi.createVirtualController).toHaveBeenCalledWith(expect.objectContaining({ name: 'Sim-Controller-1' }))
    expect(wrapper.text()).toContain('Đã tạo: Sim-Controller-1')
  })

  it('injects a fault for a device', async () => {
    const wrapper = mount(SimulatorPanel)
    const panel = wrapper.findAll('.ops-panel')[1]
    await panel.findAll('input.form-input')[0].setValue(3)
    enterpriseApi.injectSimulatorFault.mockResolvedValue({ data: { status: 'Tamper' } })
    await panel.findAll('button').find((b) => b.text() === 'Chèn sự cố').trigger('click')
    await flushPromises()
    expect(enterpriseApi.injectSimulatorFault).toHaveBeenCalledWith(expect.objectContaining({ securityDeviceId: 3, status: 'Tamper' }))
    expect(wrapper.text()).toContain('Đã chèn sự cố: Tamper')
  })

  it('simulates an offline decision', async () => {
    const wrapper = mount(SimulatorPanel)
    const panel = wrapper.findAll('.ops-panel')[2]
    await panel.findAll('input.form-input')[0].setValue(4)
    enterpriseApi.simulateOfflineScan.mockResolvedValue({ data: { result: 'Allow', reason: 'QR hợp lệ' } })
    await panel.findAll('button').find((b) => b.text() === 'Mô phỏng').trigger('click')
    await flushPromises()
    expect(enterpriseApi.simulateOfflineScan).toHaveBeenCalledWith(expect.objectContaining({ securityDeviceId: 4 }))
    expect(wrapper.text()).toContain('Allow')
  })

  it('resets demo data', async () => {
    const wrapper = mount(SimulatorPanel)
    enterpriseApi.resetDemoScenarios.mockResolvedValue({ data: { summary: { interventionRequests: 2, securityDevices: 1, evidenceItems: 3 } } })
    await wrapper.findAll('button').find((b) => b.text().includes('Đặt lại dữ liệu demo')).trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Đã đặt lại: 2 yêu cầu')
  })
})

describe('ProvisioningWizard', () => {
  it('loads provisioning requests and devices', async () => {
    enterpriseApi.getProvisioningRequests.mockResolvedValue({ data: [{ deviceProvisioningRequestId: 1, requestedName: 'Contour-C2', deviceType: 'Controller', status: 'Pending', approvalNote: null }] })
    enterpriseApi.getTopology.mockResolvedValue({ data: [{ securityDeviceId: 9, name: 'Door-1', deviceType: 'Reader', vendor: 'HID', status: 'Ok' }] })
    const wrapper = mount(ProvisioningWizard)
    await flushPromises()
    expect(wrapper.find('tbody').text()).toContain('Contour-C2')
    expect(wrapper.text()).toContain('Door-1')
  })

  it('submits a new provisioning request', async () => {
    enterpriseApi.getProvisioningRequests.mockResolvedValue({ data: [] })
    enterpriseApi.getTopology.mockResolvedValue({ data: [] })
    const wrapper = mount(ProvisioningWizard)
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text() === 'Yêu cầu mới').trigger('click')
    await wrapper.find('.modal-box input.form-input').setValue('New-Device')
    enterpriseApi.createProvisioningRequest.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Gửi').trigger('click')
    await flushPromises()
    expect(enterpriseApi.createProvisioningRequest).toHaveBeenCalledWith(expect.objectContaining({ requestedName: 'New-Device' }))
  })

  it('approves a pending request after confirmation', async () => {
    enterpriseApi.getProvisioningRequests.mockResolvedValue({ data: [{ deviceProvisioningRequestId: 1, requestedName: 'Contour-C2', deviceType: 'Controller', status: 'Pending' }] })
    enterpriseApi.getTopology.mockResolvedValue({ data: [] })
    const wrapper = mount(ProvisioningWizard)
    await flushPromises()

    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    enterpriseApi.approveProvisioningRequest.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Duyệt').trigger('click')
    await flushPromises()
    expect(enterpriseApi.approveProvisioningRequest).toHaveBeenCalledWith(1, expect.any(Object))
    confirmSpy.mockRestore()
  })

  it('creates a device through the modal', async () => {
    enterpriseApi.getProvisioningRequests.mockResolvedValue({ data: [] })
    enterpriseApi.getTopology.mockResolvedValue({ data: [] })
    const wrapper = mount(ProvisioningWizard)
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text() === 'Tạo thiết bị').trigger('click')
    await wrapper.find('.modal-box input.form-input').setValue('Door-Reader-03')
    enterpriseApi.createDevice.mockResolvedValue({ data: { securityDeviceId: 12 } })
    await wrapper.findAll('button').find((b) => b.text() === 'Tạo').trigger('click')
    await flushPromises()
    expect(enterpriseApi.createDevice).toHaveBeenCalledWith(expect.objectContaining({ name: 'Door-Reader-03' }))
    expect(wrapper.text()).toContain('Đã tạo thiết bị! ID: 12')
  })
})
