import { flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/deviceManagementApi', () => ({
  getDeviceOverview: vi.fn(),
  getCameras: vi.fn(),
  createCamera: vi.fn(),
  updateCamera: vi.fn(),
  deleteCamera: vi.fn(),
  getGates: vi.fn(),
  createGate: vi.fn(),
  updateGate: vi.fn(),
  deleteGate: vi.fn(),
}))

const deviceManagementApi = await import('../../services/deviceManagementApi')
const DeviceManagement = (await import('../DeviceManagement.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  deviceManagementApi.getDeviceOverview.mockResolvedValue({
    data: { summary: {}, cameras: [], gates: [] },
  })
})
afterEach(() => {
  document.body.innerHTML = ''
})

describe('DeviceManagement camera flow', () => {
  it('creates a camera through the modal', async () => {
    const wrapper = mount(DeviceManagement)
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text() === 'Thêm camera').trigger('click')
    await nextTick()

    const nameInput = document.body.querySelector('#camera-name')
    nameInput.value = 'Cổng A'
    nameInput.dispatchEvent(new Event('input'))
    const streamInput = document.body.querySelector('#camera-stream')
    streamInput.value = 'rtsp://camera.local/stream'
    streamInput.dispatchEvent(new Event('input'))
    await nextTick()

    deviceManagementApi.createCamera.mockResolvedValue({})
    document.body.querySelector('#camera-form').dispatchEvent(new Event('submit'))
    await flushPromises()
    expect(deviceManagementApi.createCamera).toHaveBeenCalledWith(expect.objectContaining({ cameraName: 'Cổng A' }))
  })

  it('deletes a camera after confirmation', async () => {
    deviceManagementApi.getDeviceOverview.mockResolvedValue({
      data: { summary: {}, cameras: [{ cameraId: 1, cameraName: 'CAM-1' }], gates: [] },
    })
    const wrapper = mount(DeviceManagement)
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text() === 'Xóa').trigger('click')
    await nextTick()
    const confirmButton = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Xóa'))
    deviceManagementApi.deleteCamera.mockResolvedValue({})
    confirmButton.click()
    await flushPromises()
    expect(deviceManagementApi.deleteCamera).toHaveBeenCalledWith(1)
  })
})
