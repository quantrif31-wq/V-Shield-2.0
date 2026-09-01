import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import SimulatorPanel from '../SimulatorPanel.vue'

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
  },
}))

const { enterpriseApi } = await import('../../services/enterpriseSecurityApi')

async function mountPage() {
  const wrapper = mount(SimulatorPanel, { attachTo: document.body })
  await flushPromises()
  return wrapper
}

beforeEach(() => {
  vi.clearAllMocks()
  enterpriseApi.resetDemoScenarios.mockResolvedValue({ data: { summary: { interventionRequests: 2, securityDevices: 3, evidenceItems: 5 } } })
  enterpriseApi.createVirtualController.mockResolvedValue({ data: { name: 'VC', securityDeviceId: 10 } })
  enterpriseApi.injectSimulatorFault.mockResolvedValue({ data: { status: 'Tamper' } })
  enterpriseApi.simulateOfflineScan.mockResolvedValue({ data: { result: 'Allow', reason: 'OK' } })
  enterpriseApi.recordHealth.mockResolvedValue({ data: {} })
  enterpriseApi.registerController.mockResolvedValue({ data: {} })
  enterpriseApi.createDevice.mockResolvedValue({ data: { securityDeviceId: 20 } })
  enterpriseApi.getConnectorStatus.mockResolvedValue({ data: [{ connectorId: 1, name: 'C1', status: 'Connected', lastSeenUtc: '2026-08-01' }] })
})

describe('SimulatorPanel', () => {
  it('renders panels and header', async () => {
    const wrapper = await mountPage()
    expect(wrapper.text()).toContain('Bảng điều khiển mô phỏng')
    expect(wrapper.text()).toContain('Tạo bộ điều khiển ảo')
  })

  it('resetDemo shows a summary', async () => {
    const wrapper = await mountPage()
    await wrapper.vm.resetDemo()
    expect(enterpriseApi.resetDemoScenarios).toHaveBeenCalled()
    expect(wrapper.vm.resetResult).toContain('2 yêu cầu')
    expect(wrapper.vm.resetBusy).toBe(false)
  })

  it('resetDemo handles failure', async () => {
    const wrapper = await mountPage()
    enterpriseApi.resetDemoScenarios.mockRejectedValue({ response: { data: { message: 'x' } } })
    await wrapper.vm.resetDemo()
    expect(wrapper.vm.resetResult).toBe('x')
    expect(wrapper.vm.resetBusy).toBe(false)
  })

  it('createVirtual requires a name', async () => {
    const wrapper = await mountPage()
    await wrapper.vm.createVirtual()
    expect(enterpriseApi.createVirtualController).not.toHaveBeenCalled()
  })

  it('createVirtual creates a virtual controller', async () => {
    const wrapper = await mountPage()
    wrapper.vm.vcForm.name = 'Sim-1'
    await wrapper.vm.createVirtual()
    expect(enterpriseApi.createVirtualController).toHaveBeenCalledWith({ name: 'Sim-1', protocol: 'OSDP-Sim', maxCredentials: 50000 })
    expect(wrapper.vm.vcResult).toContain('VC')
    expect(wrapper.vm.vcBusy).toBe(false)
  })

  it('createVirtual handles failure', async () => {
    const wrapper = await mountPage()
    enterpriseApi.createVirtualController.mockRejectedValue(new Error('boom'))
    wrapper.vm.vcForm.name = 'Sim-1'
    await wrapper.vm.createVirtual()
    expect(wrapper.vm.vcResult).toBe('Không thể tạo')
  })

  it('injectFault requires a device id', async () => {
    const wrapper = await mountPage()
    await wrapper.vm.injectFault()
    expect(enterpriseApi.injectSimulatorFault).not.toHaveBeenCalled()
  })

  it('injectFault injects a fault', async () => {
    const wrapper = await mountPage()
    wrapper.vm.faultForm.deviceId = 5
    wrapper.vm.faultForm.message = 'm'
    await wrapper.vm.injectFault()
    expect(enterpriseApi.injectSimulatorFault).toHaveBeenCalledWith({ securityDeviceId: 5, status: 'Tamper', severity: 'High', message: 'm' })
    expect(wrapper.vm.faultResult).toContain('Tamper')
    expect(wrapper.vm.faultBusy).toBe(false)
  })

  it('injectFault handles failure', async () => {
    const wrapper = await mountPage()
    enterpriseApi.injectSimulatorFault.mockRejectedValue(new Error('boom'))
    wrapper.vm.faultForm.deviceId = 5
    await wrapper.vm.injectFault()
    expect(wrapper.vm.faultResult).toBe('Không thể chèn sự cố')
  })

  it('simulateOffline requires a device id', async () => {
    const wrapper = await mountPage()
    await wrapper.vm.simulateOffline()
    expect(enterpriseApi.simulateOfflineScan).not.toHaveBeenCalled()
  })

  it('simulateOffline returns the decision', async () => {
    const wrapper = await mountPage()
    wrapper.vm.offlineForm.deviceId = 5
    wrapper.vm.offlineForm.subjectId = 7
    await wrapper.vm.simulateOffline()
    expect(enterpriseApi.simulateOfflineScan).toHaveBeenCalledWith({ securityDeviceId: 5, subjectType: 'Employee', subjectId: 7, credentialType: 'QR' })
    expect(wrapper.vm.offlineResult.result).toBe('Allow')
    expect(wrapper.vm.offlineBusy).toBe(false)
  })

  it('simulateOffline handles failure', async () => {
    const wrapper = await mountPage()
    enterpriseApi.simulateOfflineScan.mockRejectedValue(new Error('boom'))
    wrapper.vm.offlineForm.deviceId = 5
    await wrapper.vm.simulateOffline()
    expect(wrapper.vm.offlineResult.result).toBe('Error')
  })

  it('recordHealth requires a device id', async () => {
    const wrapper = await mountPage()
    await wrapper.vm.recordHealth()
    expect(enterpriseApi.recordHealth).not.toHaveBeenCalled()
  })

  it('recordHealth records device health', async () => {
    const wrapper = await mountPage()
    wrapper.vm.healthForm.deviceId = 5
    wrapper.vm.healthForm.status = 'Ok'
    await wrapper.vm.recordHealth()
    expect(enterpriseApi.recordHealth).toHaveBeenCalledWith(5, { status: 'Ok', message: null })
    expect(wrapper.vm.healthResult).toContain('thành công')
    expect(wrapper.vm.healthBusy).toBe(false)
  })

  it('recordHealth handles failure', async () => {
    const wrapper = await mountPage()
    const err = new Error('boom')
    err.response = { data: { message: 'fail' } }
    enterpriseApi.recordHealth.mockRejectedValue(err)
    wrapper.vm.healthForm.deviceId = 5
    await wrapper.vm.recordHealth()
    expect(wrapper.vm.healthResult).toContain('fail')
  })

  it('registerController requires a device id', async () => {
    const wrapper = await mountPage()
    await wrapper.vm.registerController()
    expect(enterpriseApi.registerController).not.toHaveBeenCalled()
  })

  it('registerController registers a controller', async () => {
    const wrapper = await mountPage()
    wrapper.vm.regForm.deviceId = 5
    await wrapper.vm.registerController()
    expect(enterpriseApi.registerController).toHaveBeenCalledWith(5, { protocol: 'OSDP', maxCredentials: 50000 })
    expect(wrapper.vm.regResult).toContain('thành công')
    expect(wrapper.vm.regBusy).toBe(false)
  })

  it('registerController handles failure', async () => {
    const wrapper = await mountPage()
    enterpriseApi.registerController.mockRejectedValue(new Error('boom'))
    wrapper.vm.regForm.deviceId = 5
    await wrapper.vm.registerController()
    expect(wrapper.vm.regResult).toContain('Thất bại')
  })

  it('createDevice requires a name', async () => {
    const wrapper = await mountPage()
    await wrapper.vm.createDevice()
    expect(enterpriseApi.createDevice).not.toHaveBeenCalled()
  })

  it('createDevice creates a device', async () => {
    const wrapper = await mountPage()
    wrapper.vm.createForm.name = 'Dev-1'
    await wrapper.vm.createDevice()
    expect(enterpriseApi.createDevice).toHaveBeenCalledWith({ name: 'Dev-1', deviceType: 'Controller' })
    expect(wrapper.vm.createResult).toContain('20')
    expect(wrapper.vm.createBusy).toBe(false)
  })

  it('createDevice handles failure', async () => {
    const wrapper = await mountPage()
    const err = new Error('boom')
    err.message = 'm'
    enterpriseApi.createDevice.mockRejectedValue(err)
    wrapper.vm.createForm.name = 'Dev-1'
    await wrapper.vm.createDevice()
    expect(wrapper.vm.createResult).toContain('m')
  })

  it('loadConnectorStatus shows connector modal', async () => {
    const wrapper = await mountPage()
    await wrapper.vm.loadConnectorStatus()
    expect(wrapper.vm.showConnectorStatus).toBe(true)
    expect(wrapper.vm.connectorStatus).toHaveLength(1)
    expect(wrapper.vm.connectorLoading).toBe(false)
    expect(wrapper.text()).toContain('C1')
  })

  it('loadConnectorStatus handles failure', async () => {
    const wrapper = await mountPage()
    enterpriseApi.getConnectorStatus.mockRejectedValue(new Error('boom'))
    await wrapper.vm.loadConnectorStatus()
    expect(wrapper.vm.connectorStatus).toEqual([])
    expect(wrapper.vm.showConnectorStatus).toBe(true)
    expect(wrapper.text()).toContain('Không có kết nối nào')
  })

  it('formatTime returns a dash for empty values', async () => {
    const wrapper = await mountPage()
    expect(wrapper.vm.formatTime('')).toBe('—')
    expect(wrapper.vm.formatTime('2026-08-01T00:00:00Z')).not.toBe('—')
  })

  it('closes the connector modal', async () => {
    const wrapper = await mountPage()
    await wrapper.vm.loadConnectorStatus()
    wrapper.vm.showConnectorStatus = false
    expect(wrapper.vm.showConnectorStatus).toBe(false)
  })
})
