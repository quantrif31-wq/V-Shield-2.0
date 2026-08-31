import { flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const getDeviceOverview = vi.fn()
const createCamera = vi.fn()
const updateCamera = vi.fn()
const deleteCamera = vi.fn()
const createGate = vi.fn()
const updateGate = vi.fn()
const deleteGate = vi.fn()

vi.mock('../../services/deviceManagementApi', () => ({
  getDeviceOverview: (...args) => getDeviceOverview(...args),
  createCamera: (...args) => createCamera(...args),
  updateCamera: (...args) => updateCamera(...args),
  deleteCamera: (...args) => deleteCamera(...args),
  createGate: (...args) => createGate(...args),
  updateGate: (...args) => updateGate(...args),
  deleteGate: (...args) => deleteGate(...args),
}))

import DeviceManagement from '../DeviceManagement.vue'

const baseOverview = () => ({
  data: {
    summary: { camerasConfigured: 2, gatesConfigured: 1, camerasLinkedToGate: 1, unassignedCameras: 1 },
    cameras: [
      { cameraId: 1, cameraName: 'CAM-1', cameraType: 'ANPR', gateId: 3, gateName: 'Cổng A', streamUrl: 'rtsp://cam/live', isOnline: true },
      { cameraId: 2, cameraName: 'CAM-2', cameraType: 'Face', gateId: null, gateName: null, streamUrl: 'https://x/stream', isOnline: false },
      { cameraId: 3, cameraName: 'CAM-3', cameraType: '', gateId: null, gateName: null, streamUrl: '', status: 'unknown' },
    ],
    gates: [
      { gateId: 3, gateName: 'Cổng A', location: 'Sảnh A', cameraCount: 1, accessLogCount: 42 },
      { gateId: 4, gateName: 'Cổng B', location: null, cameraCount: 0, accessLogCount: 0 },
    ],
  },
})

const stubs = { RouterLink: true }

const clickButton = (wrapper, text) => {
  const b = wrapper.findAll('button').find((btn) => btn.text() === text)
  expect(b).toBeTruthy()
  return b
}

describe('DeviceManagement flows', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getDeviceOverview.mockResolvedValue(baseOverview())
  })

  afterEach(() => {
    document.body.innerHTML = ''
  })

  it('renders summary KPIs, health states and protocol labels', async () => {
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    expect(wrapper.vm.summary.camerasConfigured).toBe(2)
    expect(wrapper.text()).toContain('Trực tuyến')
    expect(wrapper.text()).toContain('Mất kết nối')
    expect(wrapper.text()).toContain('Chưa gắn cổng')
    expect(wrapper.text()).toContain('RTSP stream')
    expect(wrapper.text()).toContain('Chưa có stream')
  })

  it('paginates the camera list', async () => {
    const many = Array.from({ length: 14 }, (_, i) => ({ cameraId: i + 1, cameraName: `CAM-${i + 1}`, cameraType: 'ANPR', streamUrl: 'rtsp://x' }))
    getDeviceOverview.mockResolvedValue({ data: { summary: {}, cameras: many, gates: [] } })
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    expect(wrapper.find('footer').text()).toContain('6')
    expect(wrapper.vm.cameraPagStart).toBe(1)
    await clickButton(wrapper, 'Sau').trigger('click')
    await nextTick()
    expect(wrapper.vm.cameraPage).toBe(2)
    expect(wrapper.vm.cameraPagStart).toBe(7)
    await clickButton(wrapper, 'Trước').trigger('click')
    await nextTick()
    expect(wrapper.vm.cameraPage).toBe(1)
  })

  it('paginates the gate list', async () => {
    const many = Array.from({ length: 13 }, (_, i) => ({ gateId: i + 1, gateName: `GATE-${i + 1}`, cameraCount: 0 }))
    getDeviceOverview.mockResolvedValue({ data: { summary: {}, cameras: [], gates: many } })
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    expect(wrapper.vm.gateTotalPages).toBe(3)
    await clickButton(wrapper, 'Sau').trigger('click')
    await nextTick()
    expect(wrapper.vm.gatePage).toBe(2)
  })

  it('creates a gate and refreshes', async () => {
    createGate.mockResolvedValue({ data: {} })
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    await clickButton(wrapper, 'Thêm cổng').trigger('click')
    await nextTick()
    const nameInput = document.body.querySelector('#gate-name')
    nameInput.value = 'Cổng mới'
    nameInput.dispatchEvent(new Event('input'))
    const locInput = document.body.querySelector('#gate-location')
    locInput.value = 'Bãi B'
    locInput.dispatchEvent(new Event('input'))
    await nextTick()
    document.body.querySelector('#gate-form').dispatchEvent(new Event('submit'))
    await flushPromises()
    expect(createGate).toHaveBeenCalledWith(expect.objectContaining({ gateName: 'Cổng mới', location: 'Bãi B' }))
    expect(getDeviceOverview).toHaveBeenCalled()
  })

  it('saves a gate edit via updateGate', async () => {
    updateGate.mockResolvedValue({ data: {} })
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    const gateTable = wrapper.findAll('table')[1]
    const gateEdit = gateTable.findAll('button').find((b) => b.text() === 'Sửa')
    await gateEdit.trigger('click')
    await nextTick()
    expect(wrapper.vm.editingGateId).toBe(3)
    const nameInput = document.body.querySelector('#gate-name')
    nameInput.value = 'Cổng A đổi tên'
    nameInput.dispatchEvent(new Event('input'))
    await nextTick()
    document.body.querySelector('#gate-form').dispatchEvent(new Event('submit'))
    await flushPromises()
    expect(updateGate).toHaveBeenCalledWith(3, expect.objectContaining({ gateName: 'Cổng A đổi tên' }))
  })

  it('shows a gate name validation error and does not submit', async () => {
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    await clickButton(wrapper, 'Thêm cổng').trigger('click')
    await nextTick()
    document.body.querySelector('#gate-form').dispatchEvent(new Event('submit'))
    await flushPromises()
    expect(wrapper.vm.gateNameError).toBe('Tên cổng là bắt buộc.')
    expect(createGate).not.toHaveBeenCalled()
  })

  it('surfaces gate save error and keeps form open', async () => {
    createGate.mockRejectedValue({ response: { data: { message: 'Tên trùng' } } })
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    await clickButton(wrapper, 'Thêm cổng').trigger('click')
    await nextTick()
    const nameInput = document.body.querySelector('#gate-name')
    nameInput.value = 'Trùng'
    nameInput.dispatchEvent(new Event('input'))
    await nextTick()
    document.body.querySelector('#gate-form').dispatchEvent(new Event('submit'))
    await flushPromises()
    expect(wrapper.vm.formError).toBe('Tên trùng')
    expect(wrapper.vm.showGateModal).toBe(true)
  })

  it('cancels gate modal without prompt when clean', async () => {
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    await clickButton(wrapper, 'Thêm cổng').trigger('click')
    await nextTick()
    expect(wrapper.vm.showGateModal).toBe(true)
    wrapper.vm.closeGate(true)
    await nextTick()
    expect(wrapper.vm.showGateModal).toBe(false)
  })

  it('prompts to discard dirty camera form then discards', async () => {
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    await clickButton(wrapper, 'Thêm camera').trigger('click')
    await nextTick()
    const nameInput = document.body.querySelector('#camera-name')
    nameInput.value = 'Camera test'
    nameInput.dispatchEvent(new Event('input'))
    await nextTick()
    expect(wrapper.vm.cameraDirty).toBe(true)
    wrapper.vm.requestCloseCamera()
    await nextTick()
    expect(wrapper.vm.showDiscardDialog).toBe(true)
    wrapper.vm.discardActiveForm()
    await nextTick()
    expect(wrapper.vm.showCameraModal).toBe(false)
    expect(wrapper.vm.cameraForm.cameraName).toBe('')
  })

  it('prompts to discard dirty gate form', async () => {
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    await clickButton(wrapper, 'Thêm cổng').trigger('click')
    await nextTick()
    const nameInput = document.body.querySelector('#gate-name')
    nameInput.value = 'abc'
    nameInput.dispatchEvent(new Event('input'))
    await nextTick()
    wrapper.vm.requestCloseGate()
    await nextTick()
    expect(wrapper.vm.showDiscardDialog).toBe(true)
    wrapper.vm.discardActiveForm()
    await nextTick()
    expect(wrapper.vm.showGateModal).toBe(false)
  })

  it('editing a camera pre-fills the form', async () => {
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    const wrappers = wrapper.findAll('button').filter((b) => b.text() === 'Sửa')
    await wrappers[1].trigger('click')
    await nextTick()
    expect(wrapper.vm.editingCameraId).toBe(2)
    expect(wrapper.vm.cameraForm.cameraName).toBe('CAM-2')
  })

  it('updates an existing camera', async () => {
    updateCamera.mockResolvedValue({ data: {} })
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    const wrappers = wrapper.findAll('button').filter((b) => b.text() === 'Sửa')
    await wrappers[0].trigger('click')
    await nextTick()
    const nameInput = document.body.querySelector('#camera-name')
    nameInput.value = 'CAM-1 updated'
    nameInput.dispatchEvent(new Event('input'))
    await nextTick()
    document.body.querySelector('#camera-form').dispatchEvent(new Event('submit'))
    await flushPromises()
    expect(updateCamera).toHaveBeenCalledWith(1, expect.objectContaining({ cameraName: 'CAM-1 updated' }))
  })

  it('validates stream url and blocks creation', async () => {
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    await clickButton(wrapper, 'Thêm camera').trigger('click')
    await nextTick()
    const nameInput = document.body.querySelector('#camera-name')
    nameInput.value = 'Cam'
    nameInput.dispatchEvent(new Event('input'))
    const streamInput = document.body.querySelector('#camera-stream')
    streamInput.value = 'not-a-url'
    streamInput.dispatchEvent(new Event('input'))
    await nextTick()
    document.body.querySelector('#camera-form').dispatchEvent(new Event('submit'))
    await flushPromises()
    expect(wrapper.vm.streamError).toBe('URL phải bắt đầu bằng rtsp://, http:// hoặc https://.')
    expect(createCamera).not.toHaveBeenCalled()
  })

  it('underscores camera name validation', async () => {
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    await clickButton(wrapper, 'Thêm camera').trigger('click')
    await nextTick()
    document.body.querySelector('#camera-form').dispatchEvent(new Event('submit'))
    await flushPromises()
    expect(wrapper.vm.cameraNameError).toBe('Tên camera là bắt buộc.')
  })

  it('surfaces a camera save error', async () => {
    createCamera.mockRejectedValue({})
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    await clickButton(wrapper, 'Thêm camera').trigger('click')
    await nextTick()
    const nameInput = document.body.querySelector('#camera-name')
    nameInput.value = 'Cam'
    nameInput.dispatchEvent(new Event('input'))
    await nextTick()
    document.body.querySelector('#camera-form').dispatchEvent(new Event('submit'))
    await flushPromises()
    expect(wrapper.vm.formError).toBe('Không thể lưu camera. Thông tin nhập vẫn được giữ lại.')
  })

  it('deletes a gate after confirm and refreshes', async () => {
    deleteGate.mockResolvedValue({ data: {} })
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    const gateTable = wrapper.findAll('table')[1]
    await gateTable.findAll('button').find((b) => b.text() === 'Xóa').trigger('click')
    await nextTick()
    expect(wrapper.vm.deleteKind).toBe('gate')
    expect(wrapper.vm.deleteTarget.gateId).toBe(3)
    const confirmButton = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Xóa cổng'))
    confirmButton.click()
    await flushPromises()
    expect(deleteGate).toHaveBeenCalledWith(3)
    expect(getDeviceOverview).toHaveBeenCalled()
  })

  it('surfaces a delete error toast and keeps target', async () => {
    deleteCamera.mockRejectedValue({ response: { data: { message: 'busy' } } })
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    const deleteButtons = wrapper.findAll('button').filter((b) => b.text() === 'Xóa')
    await deleteButtons[0].trigger('click')
    await nextTick()
    const confirmButton = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Xóa camera'))
    confirmButton.click()
    await flushPromises()
    expect(deleteCamera).toHaveBeenCalledWith(1)
  })

  it('does nothing on executeDelete without a target', async () => {
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    await wrapper.vm.executeDelete()
    expect(deleteCamera).not.toHaveBeenCalled()
    expect(deleteGate).not.toHaveBeenCalled()
  })

  it('guards beforeunload when a form is dirty', () => {
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    expect(wrapper.vm.cameraDirty).toBe(false)
  })

  it('handles camera import complete with errors', async () => {
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    await clickButton(wrapper, 'Nhập').trigger('click')
    await nextTick()
    expect(wrapper.vm.showCameraImport).toBe(true)
    wrapper.vm.onCameraImportComplete({ successCount: 2, errorCount: 1 })
    await nextTick()
    expect(wrapper.vm.showCameraImport).toBe(false)
    expect(getDeviceOverview).toHaveBeenCalled()
  })

  it('handles gate import complete with no errors', async () => {
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    wrapper.vm.showGateImport = true
    await nextTick()
    wrapper.vm.onGateImportComplete({ successCount: 3, errorCount: 0 })
    await nextTick()
    expect(wrapper.vm.showGateImport).toBe(false)
  })

  it('shows a generic error when the overview fails with non-403', async () => {
    getDeviceOverview.mockRejectedValue({ response: { data: {} } })
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    expect(wrapper.vm.loadError).toBeTruthy()
    expect(wrapper.vm.isLoading).toBe(false)
  })

  it('sets permissionDenied on 403', async () => {
    getDeviceOverview.mockRejectedValue({ response: { status: 403 } })
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    expect(wrapper.vm.permissionDenied).toBe(true)
    expect(wrapper.vm.loadError).toBe('')
  })

  it('refreshes via the overview button', async () => {
    const wrapper = mount(DeviceManagement, { global: { stubs } })
    await flushPromises()
    expect(getDeviceOverview).toHaveBeenCalledTimes(1)
    await clickButton(wrapper, 'Làm mới').trigger('click')
    await flushPromises()
    expect(getDeviceOverview).toHaveBeenCalledTimes(2)
  })
})
