import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const getTopology = vi.fn()
const getAdapters = vi.fn()
const getConnectorStatus = vi.fn()
const getHealthSummary = vi.fn()
const getDeviceReaders = vi.fn()
const getDeviceRelays = vi.fn()
const getDeviceSensors = vi.fn()
const getDeviceHealthHistory = vi.fn()
const diagnoseDevice = vi.fn()
const recordHealth = vi.fn()
const getOfflinePolicyPackages = vi.fn()
const createOfflinePolicyPackage = vi.fn()

vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getTopology: (...a) => getTopology(...a),
    getAdapters: (...a) => getAdapters(...a),
    getConnectorStatus: (...a) => getConnectorStatus(...a),
    getHealthSummary: (...a) => getHealthSummary(...a),
    getDeviceReaders: (...a) => getDeviceReaders(...a),
    getDeviceRelays: (...a) => getDeviceRelays(...a),
    getDeviceSensors: (...a) => getDeviceSensors(...a),
    getDeviceHealthHistory: (...a) => getDeviceHealthHistory(...a),
    diagnoseDevice: (...a) => diagnoseDevice(...a),
    recordHealth: (...a) => recordHealth(...a),
    getOfflinePolicyPackages: (...a) => getOfflinePolicyPackages(...a),
    createOfflinePolicyPackage: (...a) => createOfflinePolicyPackage(...a),
  },
}))

import DeviceTopology from '../DeviceTopology.vue'

const consoleErrorMock = vi.fn()

const topo = [
  { securityDeviceId: 1, name: 'Controller A', deviceType: 'Controller', status: 'Ok', healthStatus: { status: 'Healthy' } },
  { securityDeviceId: 2, name: 'Reader B', deviceType: 'Reader', status: 'Degraded', healthStatus: { status: 'Warning' } },
  { securityDeviceId: 3, name: 'Camera C', deviceType: 'Camera', status: 'Tamper', healthStatus: { status: 'Critical' } },
]

beforeEach(() => {
  vi.clearAllMocks()
  consoleErrorMock.mockClear()
  console.error = consoleErrorMock
  getTopology.mockResolvedValue({ data: topo })
  getAdapters.mockResolvedValue({ data: { adapters: [] } })
  getConnectorStatus.mockResolvedValue({ data: [] })
  getHealthSummary.mockResolvedValue({ data: null })
})

afterEach(() => {
  document.body.innerHTML = ''
})

describe('DeviceTopology flows', () => {
  it('maps type and status labels', () => {
    const wrapper = mount(DeviceTopology)
    expect(wrapper.vm.typeLabel('Controller')).toBe('Bộ điều khiển')
    expect(wrapper.vm.typeLabel('Unknown')).toBe('Unknown')
    expect(wrapper.vm.typeLabel(undefined)).toBe('—')
    expect(wrapper.vm.statusLabel('Connected')).toBe('Đã kết nối')
    expect(wrapper.vm.statusLabel('Custom')).toBe('Custom')
    expect(wrapper.vm.statusLabel(undefined)).toBe('—')
  })

  it('filters topology by search and type', async () => {
    const wrapper = mount(DeviceTopology)
    await flushPromises()
    expect(wrapper.vm.filteredTopology.length).toBe(3)
    wrapper.vm.searchQuery = 'reader'
    expect(wrapper.vm.filteredTopology.length).toBe(1)
    expect(wrapper.vm.filteredTopology[0].name).toBe('Reader B')
    wrapper.vm.searchQuery = ''
    wrapper.vm.typeFilter = 'Camera'
    expect(wrapper.vm.filteredTopology.length).toBe(1)
  })

  it('maps status and health dot classes', () => {
    const wrapper = mount(DeviceTopology)
    for (const s of ['Ok', 'Online', 'Connected', 'Tamper', 'Fault', 'Offline', 'Degraded', 'Other']) {
      expect(wrapper.vm.statusClass(s)).toBeTruthy()
    }
    expect(wrapper.vm.healthDotClass({ healthStatus: { status: 'Healthy' } })).toBe('health-ok')
    expect(wrapper.vm.healthDotClass({ healthStatus: { status: 'Warning' } })).toBe('health-warn')
    expect(wrapper.vm.healthDotClass({ healthStatus: { status: 'Critical' } })).toBe('health-danger')
    expect(wrapper.vm.healthDotClass({})).toBe('')
    expect(wrapper.vm.formatTime(null)).toBe('—')
    expect(wrapper.vm.formatTime('2026-08-01T00:00:00Z')).toBeTruthy()
  })

  it('renders a health summary row when provided', async () => {
    getHealthSummary.mockResolvedValue({ data: { totalDevices: 9, onlineCount: 8, degradedCount: 1, offlineCount: 0 } })
    const wrapper = mount(DeviceTopology)
    await flushPromises()
    expect(wrapper.text()).toContain('9')
  })

  it('loads relays, sensors and health tabs', async () => {
    getDeviceRelays.mockResolvedValue({ data: [{ relayId: 1, name: 'Relay-1' }] })
    getDeviceSensors.mockResolvedValue({ data: [{ sensorId: 1, name: 'Sensor-1' }] })
    getDeviceHealthHistory.mockResolvedValue({ data: [{ healthLogId: 1, status: 'Ok', recordedAtUtc: '2026-08-01T00:00:00Z' }] })
    const wrapper = mount(DeviceTopology)
    await flushPromises()
    await wrapper.vm.openDeviceDetail({ securityDeviceId: 1, name: 'Controller A', deviceType: 'Controller', status: 'Ok' })
    await wrapper.vm.loadDetailTab('relays')
    await flushPromises()
    expect(getDeviceRelays).toHaveBeenCalledWith(1)
    expect(wrapper.vm.relays.length).toBe(1)
    await wrapper.vm.loadDetailTab('sensors')
    await flushPromises()
    expect(wrapper.vm.sensors.length).toBe(1)
    await wrapper.vm.loadDetailTab('health')
    await flushPromises()
    expect(wrapper.vm.healthHistory.length).toBe(1)
    expect(wrapper.vm.detailLoading).toBe(false)
  })

  it('loadDetailTab resolves items when data is an object', async () => {
    getDeviceHealthHistory.mockResolvedValue({ data: { items: [{ healthLogId: 2 }] } })
    const wrapper = mount(DeviceTopology)
    await flushPromises()
    await wrapper.vm.openDeviceDetail({ securityDeviceId: 1 })
    await wrapper.vm.loadDetailTab('health')
    await flushPromises()
    expect(wrapper.vm.healthHistory.length).toBe(1)
  })

  it('loadDetailTab does nothing without a selected device', async () => {
    const wrapper = mount(DeviceTopology)
    await flushPromises()
    await wrapper.vm.loadDetailTab('readers')
    expect(getDeviceReaders).not.toHaveBeenCalled()
  })

  it('logs when loading a detail tab fails', async () => {
    getDeviceReaders.mockRejectedValue(new Error('bad'))
    const wrapper = mount(DeviceTopology)
    await flushPromises()
    await wrapper.vm.openDeviceDetail({ securityDeviceId: 1 })
    await wrapper.vm.loadDetailTab('readers')
    await flushPromises()
    expect(console.error).toHaveBeenCalled()
  })

  it('fallback to empty topology on load failure', async () => {
    getTopology.mockRejectedValue(new Error('x'))
    const wrapper = mount(DeviceTopology)
    await flushPromises()
    expect(wrapper.vm.topology).toEqual([])
    expect(wrapper.vm.loading).toBe(false)
  })

  it('surfaces a diagnose error', async () => {
    diagnoseDevice.mockRejectedValue({ response: { data: { message: 'ai down' } } })
    const wrapper = mount(DeviceTopology)
    await flushPromises()
    await wrapper.vm.openDeviceDetail({ securityDeviceId: 3 })
    await wrapper.vm.diagnoseDevice({ securityDeviceId: 3 })
    await flushPromises()
    expect(wrapper.vm.diagnosisResult).toContain('ai down')
  })

  it('opens offline packages and loads existing packages', async () => {
    getOfflinePolicyPackages.mockResolvedValue({ data: { items: [{ offlinePolicyPackageId: 1, securityDeviceId: 7, policyVersionId: 2 }] } })
    const wrapper = mount(DeviceTopology)
    await flushPromises()
    await wrapper.vm.openOfflinePackages()
    await flushPromises()
    expect(wrapper.vm.showOfflinePackages).toBe(true)
    expect(wrapper.vm.offlinePackages.length).toBe(1)
  })

  it('creates an offline package', async () => {
    createOfflinePolicyPackage.mockResolvedValue({})
    const wrapper = mount(DeviceTopology)
    await flushPromises()
    await wrapper.vm.openOfflinePackages()
    wrapper.vm.offlinePackageForm = { deviceId: 7, policyVersionId: 3 }
    await wrapper.vm.createOfflinePackage()
    await flushPromises()
    expect(createOfflinePolicyPackage).toHaveBeenCalledWith(expect.objectContaining({ securityDeviceId: 7, policyVersionId: 3 }))
    expect(wrapper.vm.offlinePkgResult).toContain('thành công')
  })

  it('surfaces offline package creation error', async () => {
    createOfflinePolicyPackage.mockRejectedValue({ message: 'duplicate' })
    const wrapper = mount(DeviceTopology)
    await flushPromises()
    await wrapper.vm.openOfflinePackages()
    wrapper.vm.offlinePackageForm = { deviceId: 7, policyVersionId: null }
    await wrapper.vm.createOfflinePackage()
    await flushPromises()
    expect(wrapper.vm.offlinePkgError).toBe('duplicate')
  })

  it('createOfflinePackage returns without a device id', async () => {
    const wrapper = mount(DeviceTopology)
    await flushPromises()
    wrapper.vm.offlinePackageForm = { deviceId: null, policyVersionId: null }
    await wrapper.vm.createOfflinePackage()
    expect(createOfflinePolicyPackage).not.toHaveBeenCalled()
  })

  it('records health for a device', async () => {
    recordHealth.mockResolvedValue({})
    const wrapper = mount(DeviceTopology)
    await flushPromises()
    await wrapper.vm.openDeviceDetail({ securityDeviceId: 1, name: 'Controller A' })
    wrapper.vm.recordHealthForDevice({ securityDeviceId: 1, name: 'Controller A' })
    expect(wrapper.vm.recordHealthTarget.securityDeviceId).toBe(1)
    await wrapper.vm.submitHealthRecord()
    await flushPromises()
    expect(recordHealth).toHaveBeenCalledWith(1, expect.objectContaining({ status: 'Ok' }))
    expect(wrapper.vm.healthSaveResult).toContain('thành công')
  })

  it('surfaces a health record error', async () => {
    recordHealth.mockRejectedValue({ response: { data: { message: 'no' } } })
    const wrapper = mount(DeviceTopology)
    await flushPromises()
    wrapper.vm.recordHealthForDevice({ securityDeviceId: 1, name: 'Controller A' })
    await wrapper.vm.submitHealthRecord()
    await flushPromises()
    expect(wrapper.vm.healthSaveError).toBe('no')
  })

  it('submitHealthRecord returns without a target', async () => {
    const wrapper = mount(DeviceTopology)
    await flushPromises()
    await wrapper.vm.submitHealthRecord()
    expect(recordHealth).not.toHaveBeenCalled()
  })

  it('logs when loading offline packages fails', async () => {
    getOfflinePolicyPackages.mockRejectedValue(new Error('boom'))
    const wrapper = mount(DeviceTopology)
    await flushPromises()
    await wrapper.vm.openOfflinePackages()
    await flushPromises()
    expect(console.error).toHaveBeenCalled()
  })

  it('switches detail tabs via the drawer buttons', async () => {
    getDeviceRelays.mockResolvedValue({ data: [] })
    const wrapper = mount(DeviceTopology)
    await flushPromises()
    await wrapper.find('.device-row').trigger('click')
    await new Promise((r) => setTimeout(r, 0))
    const tabButtons = [...document.body.querySelectorAll('.drawer-tabs button')]
    tabButtons[2].click()
    await flushPromises()
    expect(getDeviceRelays).toHaveBeenCalled()
  })
})
