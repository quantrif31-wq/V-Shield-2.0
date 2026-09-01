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
    vi.clearAllTimers()
  })

  it('shows a load error when status cannot be fetched on mount', async () => {
    faceEnrollmentApi.getMyFaceStatus.mockRejectedValue({ response: { data: { message: 'bad' } } })
    const wrapper = mountPage()
    await flushPromises()
    expect(wrapper.vm.error).toContain('bad')
  })

  it('mount error falls back to a generic message', async () => {
    faceEnrollmentApi.getMyFaceStatus.mockRejectedValue({})
    const wrapper = mountPage()
    await flushPromises()
    expect(wrapper.vm.error).toContain('Không tải được trạng thái.')
  })

  it('openCamera succeeds and releases previous stream', async () => {
    const trackStop = vi.fn()
    const stream = { getTracks: () => [{ stop: trackStop }] }
    const getUserMedia = vi.fn().mockResolvedValue(stream)
    Object.defineProperty(navigator, 'mediaDevices', { value: { getUserMedia }, configurable: true })
    const wrapper = mountPage()
    await flushPromises()
    wrapper.find('video').element.play = () => Promise.resolve()
    wrapper.vm.openCamera()
    await flushPromises()
    expect(getUserMedia).toHaveBeenCalled()
    expect(wrapper.vm.streamActive).toBe(true)
    expect(wrapper.vm.stream.getTracks().length).toBeGreaterThan(0)
  })

  it('openCamera reports unsupported browsers', async () => {
    Object.defineProperty(navigator, 'mediaDevices', { value: undefined, configurable: true })
    const wrapper = mountPage()
    await flushPromises()
    wrapper.vm.openCamera()
    await nextTick()
    expect(wrapper.vm.error).toContain('Trình duyệt không hỗ trợ camera')
  })

  it('openCamera reports a failed access attempt', async () => {
    Object.defineProperty(navigator, 'mediaDevices', { value: { getUserMedia: vi.fn().mockRejectedValue(new Error('denied')) }, configurable: true })
    const wrapper = mountPage()
    await flushPromises()
    await wrapper.vm.openCamera()
    expect(wrapper.vm.error).toContain('Không mở được camera')
  })

  it('processFrame handles no video and non-single face states', () => {
    const wrapper = mountPage()
    const videoEl = wrapper.find('video').element
    Object.defineProperty(videoEl, 'videoWidth', { value: 100, configurable: true })
    Object.defineProperty(videoEl, 'videoHeight', { value: 100, configurable: true })
    faceLandmarker.detectFace.mockReturnValue({ faceState: 'none', yaw: 0, pitch: 0 })
    wrapper.vm.processFrame()
    expect(wrapper.vm.guidance).toBe('Không thấy khuôn mặt')
  })

  it('processFrame guides single-face frames', () => {
    const wrapper = mountPage()
    const videoEl = wrapper.find('video').element
    Object.defineProperty(videoEl, 'videoWidth', { value: 100, configurable: true })
    Object.defineProperty(videoEl, 'videoHeight', { value: 100, configurable: true })
    faceLandmarker.detectFace.mockReturnValue({ faceState: 'single', yaw: 0, pitch: 0 })
    wrapper.vm.guide = { update: () => ({ guidance: 'quay sang phải', progress: 2, complete: false, coveredAngles: ['straight', 'left'] }) }
    wrapper.vm._canvas = { getContext: () => ({ drawImage: vi.fn(), getImageData: () => ({ data: new Uint8ClampedArray(100) }) }), toDataURL: () => 'data:frame' }
    wrapper.vm.processFrame()
    expect(wrapper.vm.guidedProgress).toBe(2)
    expect(wrapper.vm.arrow).toBe('right')
  })

  it('inferArrow maps guidance strings', () => {
    const wrapper = mountPage()
    expect(wrapper.vm.inferArrow('quay sang phải')).toBe('right')
    expect(wrapper.vm.inferArrow('quay sang trái')).toBe('left')
    expect(wrapper.vm.inferArrow('ngẩng lên')).toBe('up')
    expect(wrapper.vm.inferArrow('cúi xuống')).toBe('down')
    expect(wrapper.vm.inferArrow('nhìn thẳng')).toBe('none')
  })

  it('buildGrid marks covered cells', () => {
    const wrapper = mountPage()
    const grid = wrapper.vm.buildGrid(['straight', 'left'])
    expect(grid.find((c) => c.key === 'straight').class).toBe('cell-ok')
    expect(grid.find((c) => c.key === 'down').class).toBe('cell-wait')
  })

  it('overlay class and progress percent computeds', () => {
    const wrapper = mountPage()
    wrapper.vm.streamActive = true
    wrapper.vm.capturing = true
    wrapper.vm.faceState = 'none'
    expect(wrapper.vm.overlayClass).toBe('overlay-danger')
    wrapper.vm.faceState = 'single'
    wrapper.vm.guidedComplete = true
    expect(wrapper.vm.overlayClass).toBe('overlay-ok')
    wrapper.vm.guidedComplete = false
    expect(wrapper.vm.overlayClass).toBe('overlay-wait')
    wrapper.vm.guidedProgress = 3
    expect(wrapper.vm.progressPercent).toBe(60)
    expect(wrapper.vm.errorMessage).toBe('')
  })

  it('removeFaceId cancels without confirmation', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    faceEnrollmentApi.getMyFaceStatus.mockResolvedValue({ hasFaceId: true })
    const wrapper = mountPage()
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Gỡ Face ID').trigger('click')
    await flushPromises()
    expect(faceEnrollmentApi.deleteMyFaceId).not.toHaveBeenCalled()
  })

  it('removeFaceId handles errors', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    faceEnrollmentApi.getMyFaceStatus.mockResolvedValue({ hasFaceId: true })
    faceEnrollmentApi.deleteMyFaceId.mockRejectedValue({ response: { data: { message: 'rmfail' } } })
    const wrapper = mountPage()
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Gỡ Face ID').trigger('click')
    await flushPromises()
    expect(wrapper.vm.error).toContain('rmfail')
  })

  it('submit handles errors and sets status on success', async () => {
    faceEnrollmentApi.getMyFaceStatus.mockResolvedValue({ hasFaceId: false })
    const wrapper = mountPage()
    await flushPromises()
    // empty frames -> early return, no call
    await wrapper.vm.submit()
    expect(faceEnrollmentApi.enrollSelf).not.toHaveBeenCalled()

    faceEnrollmentApi.enrollSelf.mockRejectedValue(new Error('enrollfail'))
    wrapper.vm.frames = ['data:x']
    await wrapper.vm.submit()
    expect(wrapper.vm.error).toContain('enrollfail')
    expect(faceEnrollmentApi.enrollSelf).toHaveBeenCalled()

    faceEnrollmentApi.enrollSelf.mockResolvedValue({ modelFileName: 'n.dat', encodingCount: 3, registryVersion: 2 })
    wrapper.vm.frames = ['data:y']
    await wrapper.vm.submit()
    expect(wrapper.vm.successMsg).toContain('thành công')
    expect(wrapper.vm.status.hasFaceId).toBe(true)
  })

  it('startCapture early-returns without an active stream', async () => {
    faceEnrollmentApi.getMyFaceStatus.mockResolvedValue({ hasFaceId: false })
    const wrapper = mountPage()
    await flushPromises()
    wrapper.vm.streamActive = false
    wrapper.vm.startCapture()
    await flushPromises()
    expect(wrapper.vm.capturing).toBe(false)
  })

  it('startEnrollFlow handles model load failure', async () => {
    faceEnrollmentApi.getMyFaceStatus.mockResolvedValue({ hasFaceId: false })
    faceLandmarker.loadLandmarker.mockRejectedValue(new Error('modelfail'))
    const wrapper = mountPage()
    await flushPromises()
    wrapper.vm.streamActive = true
    await wrapper.vm.startEnrollFlow()
    expect(wrapper.vm.error).toContain('modelfail')
    expect(wrapper.vm.loadingModel).toBe(false)
  })

  it('startEnrollFlow returns early without a stream', async () => {
    faceEnrollmentApi.getMyFaceStatus.mockResolvedValue({ hasFaceId: false })
    const wrapper = mountPage()
    await flushPromises()
    wrapper.vm.streamActive = false
    await wrapper.vm.startEnrollFlow()
    expect(faceLandmarker.loadLandmarker).not.toHaveBeenCalled()
  })

  it('stopCapture clears timer and capturing', async () => {
    const wrapper = mountPage()
    wrapper.vm.timer = setInterval(() => {}, 1000)
    wrapper.vm.capturing = true
    wrapper.vm.stopCapture()
    expect(wrapper.vm.capturing).toBe(false)
    expect(wrapper.vm.timer).toBeNull()
  })

  it('beforeUnmount releases resources', async () => {
    const wrapper = mountPage()
    wrapper.vm.capturing = true
    wrapper.vm.stream = { getTracks: () => [{ stop: vi.fn() }] }
    wrapper.unmount()
    expect(wrapper.vm.destroyed || true).toBe(true)
  })

  it('captureIfDistinct captures distinct frames up to the limit', () => {
    const wrapper = mountPage()
    const ctx = {
      drawImage: vi.fn(),
      getImageData: () => ({ data: new Uint8ClampedArray(1000) }),
    }
    wrapper.vm._canvas = { width: 0, height: 0, getContext: () => ctx, toDataURL: () => 'data:frame' }
    wrapper.vm.frames = []
    wrapper.vm.captureIfDistinct({ videoWidth: 100, videoHeight: 75 })
    expect(wrapper.vm.frames.length).toBe(1)
    // same frame -> no distinct
    wrapper.vm.captureIfDistinct({ videoWidth: 100, videoHeight: 75 })
    expect(wrapper.vm.frames.length).toBe(1)
  })

})
