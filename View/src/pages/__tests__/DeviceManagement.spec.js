import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import DeviceManagement from '../DeviceManagement.vue'

const getDeviceOverview = vi.fn()
const deleteCamera = vi.fn()

vi.mock('../../services/deviceManagementApi', () => ({
  getDeviceOverview: (...args) => getDeviceOverview(...args),
  createCamera: vi.fn(), createGate: vi.fn(), updateCamera: vi.fn(), updateGate: vi.fn(),
  deleteCamera: (...args) => deleteCamera(...args), deleteGate: vi.fn(),
}))

const overview = {
  summary: { camerasConfigured: 1, gatesConfigured: 1, camerasLinkedToGate: 1, unassignedCameras: 0 },
  cameras: [{ cameraId: 7, cameraName: 'Camera cổng chính', cameraType: 'ANPR', gateId: 3, gateName: 'Cổng chính', streamUrl: 'rtsp://camera.local/live', isOnline: true }],
  gates: [{ gateId: 3, gateName: 'Cổng chính', location: 'Sảnh A', cameraCount: 1, accessLogCount: 42 }],
}

describe('Device management module', () => {
  beforeEach(() => {
    getDeviceOverview.mockResolvedValue({ data: overview })
    deleteCamera.mockResolvedValue({ data: {} })
  })

  it('renders cameras and gates with semantic health states', async () => {
    const wrapper = mount(DeviceManagement, { global: { stubs: { RouterLink: true, Teleport: true } } })
    await flushPromises()
    expect(wrapper.text()).toContain('Camera cổng chính')
    expect(wrapper.text()).toContain('Trực tuyến')
    expect(wrapper.text()).toContain('Sảnh A')
    expect(wrapper.findAll('table')).toHaveLength(2)
  })

  it('uses the shared destructive confirmation before deleting', async () => {
    const wrapper = mount(DeviceManagement, { global: { stubs: { RouterLink: true, Teleport: true } } })
    await flushPromises()
    const deleteButtons = wrapper.findAll('button').filter((button) => button.text() === 'Xóa')
    await deleteButtons[0].trigger('click')
    expect(wrapper.text()).toContain('Xóa camera?')
    expect(deleteCamera).not.toHaveBeenCalled()
  })
})
