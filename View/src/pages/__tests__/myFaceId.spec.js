import { flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/faceEnrollmentApi', () => ({
  getMyFaceStatus: vi.fn(),
  enrollSelf: vi.fn(),
  deleteMyFaceId: vi.fn(),
}))
vi.mock('../../services/faceLandmarker', () => ({
  loadLandmarker: vi.fn(),
  detectFace: vi.fn(),
}))
vi.mock('../../services/poseGuideClient', () => ({
  PoseGuideClient: class {
    constructor() {
      this.update = vi.fn(() => ({ guidance: 'Nhìn thẳng vào camera', progress: 1, complete: false, coveredAngles: ['straight'] }))
    }
  },
}))

const faceEnrollmentApi = await import('../../services/faceEnrollmentApi')
const faceLandmarker = await import('../../services/faceLandmarker')
const MyFaceId = (await import('../MyFaceId.vue')).default

let wrappers = []

beforeEach(() => {
  vi.clearAllMocks()
  wrappers = []
})
afterEach(() => {
  wrappers.forEach((w) => w.unmount())
  vi.useRealTimers()
})

function mountPage() {
  const wrapper = mount(MyFaceId)
  wrappers.push(wrapper)
  return wrapper
}

describe('MyFaceId', () => {
  it('loads the current face status', async () => {
    faceEnrollmentApi.getMyFaceStatus.mockResolvedValue({ hasFaceId: true, modelFileName: 'a.dat', encodingCount: 5, version: 2 })
    const wrapper = mountPage()
    await flushPromises()
    expect(wrapper.text()).toContain('Đã đăng ký Face ID')
    expect(wrapper.text()).toContain('5')
  })

  it('shows a guidance panel when no face is registered', async () => {
    faceEnrollmentApi.getMyFaceStatus.mockResolvedValue({ hasFaceId: false })
    const wrapper = mountPage()
    await flushPromises()
    expect(wrapper.text()).toContain('Chưa đăng ký')
    expect(wrapper.text()).toContain('Hướng dẫn')
  })

  it('removes the face id after confirmation', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    faceEnrollmentApi.getMyFaceStatus.mockResolvedValue({ hasFaceId: true, modelFileName: 'a.dat', encodingCount: 5, version: 2 })
    faceEnrollmentApi.deleteMyFaceId.mockResolvedValue({ message: 'Đã gỡ' })
    const wrapper = mountPage()
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text() === 'Gỡ Face ID').trigger('click')
    await flushPromises()
    expect(faceEnrollmentApi.deleteMyFaceId).toHaveBeenCalled()
    expect(wrapper.text()).toContain('Đã gỡ')
  })

  it('asks for confirmation before replacing an existing face id', async () => {
    faceEnrollmentApi.getMyFaceStatus.mockResolvedValue({ hasFaceId: true, modelFileName: 'a.dat', encodingCount: 5, version: 2 })
    const wrapper = mountPage()
    await flushPromises()

    wrapper.vm.streamActive = true
    await nextTick()
    await wrapper.findAll('button').find((b) => b.text() === 'Bắt đầu quay').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Gỡ Face ID cũ?')
  })

  it('submits captured frames to enroll', async () => {
    faceEnrollmentApi.getMyFaceStatus.mockResolvedValue({ hasFaceId: false })
    faceEnrollmentApi.enrollSelf.mockResolvedValue({ modelFileName: 'new.dat', encodingCount: 3, registryVersion: 2 })
    const wrapper = mountPage()
    await flushPromises()

    wrapper.vm.frames = ['data:image/jpeg;base64,xxx']
    await nextTick()
    await wrapper.findAll('button').find((b) => b.text() === 'Gửi đăng ký').trigger('click')
    await flushPromises()
    expect(faceEnrollmentApi.enrollSelf).toHaveBeenCalledWith(['data:image/jpeg;base64,xxx'])
    expect(wrapper.text()).toContain('Đăng ký Face ID thành công!')
  })

  it('starts the guided enrollment flow after replacing', async () => {
    vi.useFakeTimers()
    faceEnrollmentApi.getMyFaceStatus.mockResolvedValue({ hasFaceId: true, modelFileName: 'a.dat', encodingCount: 5, version: 2 })
    faceLandmarker.loadLandmarker.mockResolvedValue({})
    const wrapper = mountPage()
    await flushPromises()

    wrapper.vm.streamActive = true
    await nextTick()
    await wrapper.findAll('button').find((b) => b.text() === 'Bắt đầu quay').trigger('click')
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Đồng ý, bắt đầu quay')).trigger('click')
    await flushPromises()
    expect(faceLandmarker.loadLandmarker).toHaveBeenCalled()
    expect(wrapper.vm.capturing).toBe(true)
  })
})
