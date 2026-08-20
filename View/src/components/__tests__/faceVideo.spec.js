import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest'
import { shallowMount } from '@vue/test-utils'

const { authState, loginUser } = vi.hoisted(() => {
  const authState = { user: { role: 'Admin', employeeId: null, fullName: 'Admin' } }
  return {
    authState,
    loginUser: (user) => { authState.user = user },
  }
})

vi.mock('../../stores/auth', () => ({ authState }))

vi.mock('../../services/employeeApi', () => ({
  getAll: vi.fn(),
}))

vi.mock('../../services/faceVideoApi', () => ({
  uploadFaceVideo: vi.fn(),
  getEmployeeVideos: vi.fn(),
  getProtectedVideoBlob: vi.fn(),
  deleteVideo: vi.fn(),
}))

import FaceVideo from '../FaceVideo.vue'
import { getAll as getEmployees } from '../../services/employeeApi'
import { uploadFaceVideo, getEmployeeVideos, getProtectedVideoBlob, deleteVideo } from '../../services/faceVideoApi'

Object.defineProperty(URL, 'createObjectURL', { writable: true, value: vi.fn(() => 'blob:mock-url') })
Object.defineProperty(URL, 'revokeObjectURL', { writable: true, value: vi.fn() })

beforeEach(() => {
  vi.clearAllMocks()
  getEmployees.mockResolvedValue({ data: [] })
  getEmployeeVideos.mockResolvedValue({ data: [] })
})

afterEach(() => {
  vi.restoreAllMocks()
})

describe('FaceVideo.vue', () => {
  it('loads videos for a non-admin from the auth user employeeId', async () => {
    loginUser({ role: 'User', employeeId: 42 })
    getEmployeeVideos.mockResolvedValue({ data: [{ id: 1 }] })
    getProtectedVideoBlob.mockResolvedValue({ data: new Blob() })
    const wrapper = shallowMount(FaceVideo)
    await wrapper.vm.$nextTick()
    expect(wrapper.vm.selectedEmployeeId).toBe(42)
    expect(getEmployeeVideos).toHaveBeenCalledWith(42)
    expect(wrapper.vm.videos).toEqual([{ id: 1 }])
  })

  it('loads employees for an admin and lists their videos', async () => {
    loginUser({ role: 'Admin', employeeId: null })
    getEmployees.mockResolvedValue({ data: [{ employeeId: 7, fullName: 'Nguyen A' }] })
    getEmployeeVideos.mockResolvedValue({ data: [] })
    const wrapper = shallowMount(FaceVideo)
    await flushPromises()
    expect(wrapper.vm.isAdmin).toBe(true)
    expect(getEmployees).toHaveBeenCalled()
    expect(wrapper.vm.employees).toEqual([{ employeeId: 7, fullName: 'Nguyen A' }])
  })

  it('handles large files and rejects them', async () => {
    const wrapper = shallowMount(FaceVideo)
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    const big = new File(['x'.repeat(51 * 1024 * 1024)], 'a.mp4', { type: 'video/mp4' })
    wrapper.vm.handleFile({ target: { files: [big] } })
    expect(alertSpy).toHaveBeenCalledWith('Video tối đa 50MB')
    expect(wrapper.vm.file).toBeNull()
  })

  it('accepts a valid file and creates a preview url', async () => {
    const wrapper = shallowMount(FaceVideo)
    const small = new File(['x'], 'a.mp4', { type: 'video/mp4' })
    wrapper.vm.handleFile({ target: { files: [small] } })
    expect(wrapper.vm.file).toBe(small)
    expect(URL.createObjectURL).toHaveBeenCalledWith(small)
  })

  it('ignores empty file selection', async () => {
    const wrapper = shallowMount(FaceVideo)
    const before = wrapper.vm.file
    wrapper.vm.handleFile({ target: { files: [] } })
    expect(wrapper.vm.file).toBe(before)
  })

  it('requires an employee selection for an admin upload', async () => {
    loginUser({ role: 'Admin', employeeId: null })
    const wrapper = shallowMount(FaceVideo)
    await flushPromises()
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    wrapper.vm.file = new File(['x'], 'a.mp4')
    wrapper.vm.selectedEmployeeId = ''
    await wrapper.vm.upload()
    expect(alertSpy).toHaveBeenCalledWith('Vui lòng chọn nhân viên trước khi upload')
    expect(uploadFaceVideo).not.toHaveBeenCalled()
  })

  it('uploads a video with a progress callback and reloads', async () => {
    const wrapper = shallowMount(FaceVideo)
    wrapper.vm.selectedEmployeeId = 5
    wrapper.vm.file = new File(['x'], 'a.mp4')
    uploadFaceVideo.mockImplementation(async () => {})
    getEmployeeVideos.mockResolvedValue({ data: [] })
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    await wrapper.vm.upload()
    expect(uploadFaceVideo).toHaveBeenCalledWith(expect.any(File), 5, expect.any(Function))
    expect(wrapper.vm.uploading).toBe(false)
    expect(wrapper.vm.file).toBeNull()
    expect(alertSpy).toHaveBeenCalledWith('Upload thành công')
    expect(getEmployeeVideos).toHaveBeenCalledWith(5)
  })

  it('reports upload errors with a server message', async () => {
    const wrapper = shallowMount(FaceVideo)
    wrapper.vm.selectedEmployeeId = 5
    wrapper.vm.file = new File(['x'], 'a.mp4')
    uploadFaceVideo.mockRejectedValue({ response: { data: { message: 'server boom' } } })
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    await wrapper.vm.upload()
    expect(alertSpy).toHaveBeenCalledWith('server boom')
  })

  it('loads protected video urls for each video', async () => {
    const wrapper = shallowMount(FaceVideo)
    wrapper.vm.videos = [{ id: 1 }, { id: 2 }]
    getProtectedVideoBlob.mockResolvedValue({ data: new Blob(['x']) })
    await wrapper.vm.loadProtectedVideoUrls()
    expect(expect.any(Object)).toBeDefined()
    expect(URL.createObjectURL).toHaveBeenCalledTimes(2)
  })

  it('revokes and clears object urls on release and on unmount', async () => {
    const wrapper = shallowMount(FaceVideo)
    wrapper.vm.videoPlaybackUrls = { 1: 'blob:a', 2: 'blob:b' }
    wrapper.vm.releaseVideoObjectUrls()
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:a')
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:b')
    expect(wrapper.vm.videoPlaybackUrls).toEqual({})
    wrapper.vm.videoPlaybackUrls = { 3: 'blob:c' }
    wrapper.unmount()
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:c')
  })

  it('deletes a video after confirmation and reloads the list', async () => {
    const wrapper = shallowMount(FaceVideo)
    wrapper.vm.selectedEmployeeId = 9
    getEmployeeVideos.mockResolvedValue({ data: [] })
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    await wrapper.vm.remove(11)
    expect(deleteVideo).toHaveBeenCalledWith(11)
    expect(getEmployeeVideos).toHaveBeenCalledWith(9)
  })

  it('skips deletion when the user declines the confirmation', async () => {
    const wrapper = shallowMount(FaceVideo)
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false)
    await wrapper.vm.remove(11)
    expect(deleteVideo).not.toHaveBeenCalled()
  })

  it('shows the preview when a file is selected and uploads from the template', async () => {
    loginUser({ role: 'User', employeeId: 3 })
    getEmployeeVideos.mockResolvedValue({ data: [] })
    getProtectedVideoBlob.mockResolvedValue({ data: new Blob() })
    const wrapper = shallowMount(FaceVideo)
    await flushPromises()
    wrapper.vm.handleFile({ target: { files: [new File(['x'], 'b.mp4')] } })
    await wrapper.vm.$nextTick()
    expect(wrapper.vm.previewUrl).toBeTruthy()
    await wrapper.vm.$nextTick()
  })
})

async function flushPromises() {
  return new Promise((resolve) => setTimeout(resolve, 0))
}