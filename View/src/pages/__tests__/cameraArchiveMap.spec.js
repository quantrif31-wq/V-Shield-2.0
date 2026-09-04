import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({
  route: { params: {}, query: {} },
  router: { back: vi.fn() },
}))

vi.mock('vue-router', () => ({
  useRoute: () => hoisted.route,
  useRouter: () => hoisted.router,
}))

vi.mock('../../services/socApi', () => ({ socApi: { getAlarm: vi.fn() } }))
vi.mock('../../services/cameraRuntimeApi', () => ({ getArchiveSegments: vi.fn(), getCameras: vi.fn(), getDvrStatus: vi.fn() }))

const socApi = (await import('../../services/socApi')).socApi
const cameraRuntimeApi = await import('../../services/cameraRuntimeApi')

const IncidentMapPage = (await import('../IncidentMapPage.vue')).default
const CameraArchive = (await import('../CameraArchive.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  cameraRuntimeApi.getDvrStatus.mockResolvedValue([])
})

describe('IncidentMapPage', () => {
  it('shows a hint when no alarm is selected', () => {
    hoisted.route.params = {}
    hoisted.route.query = {}
    const wrapper = mount(IncidentMapPage, { global: { mocks: { $route: hoisted.route }, stubs: { RouterLink: { template: '<a><slot /></a>' } } } })
    expect(wrapper.text()).toContain('Chọn một báo động từ danh sách')
  })
})

describe('CameraArchive', () => {
  it('loads cameras and segments with pagination', async () => {
    hoisted.route.params = { id: '2' }
    cameraRuntimeApi.getCameras.mockResolvedValue([{ cameraId: 2, cameraName: 'CAM-02', gateId: 1, gateName: 'Cổng A', gateLocation: 'Sảnh', cameraType: 'IP' }])
    cameraRuntimeApi.getArchiveSegments.mockResolvedValue({
      items: [{ segmentId: 1, cameraId: 2, cameraName: 'CAM-02', startedAt: '2026-08-01T00:00:00Z', endedAt: '2026-08-01T00:05:00Z', durationSeconds: 300, fileSizeBytes: 5 * 1024 * 1024, storageUrl: 'http://x/1.mp4' }],
      total: 1,
    })
    const wrapper = mount(CameraArchive)
    await flushPromises()
    expect(wrapper.text()).toContain('CAM-02')
    expect(wrapper.text()).toContain('5m 0s')
    expect(wrapper.text()).toContain('5.00 MB')
  })

  it('toggles the video player', async () => {
    hoisted.route.params = {}
    cameraRuntimeApi.getCameras.mockResolvedValue([])
    cameraRuntimeApi.getArchiveSegments.mockResolvedValue({
      items: [{ segmentId: 1, cameraId: 2, cameraName: 'CAM-02', startedAt: '2026-08-01T00:00:00Z', endedAt: '2026-08-01T00:05:00Z', durationSeconds: 60, fileSizeBytes: 1024, storageUrl: 'http://x/1.mp4' }],
      total: 1,
    })
    const wrapper = mount(CameraArchive)
    await flushPromises()
    expect(wrapper.find('video').exists()).toBe(false)
    await wrapper.findAll('button').find((b) => b.text() === 'Xem video').trigger('click')
    expect(wrapper.find('video').exists()).toBe(true)
    expect(wrapper.find('video').attributes('src')).toBe('http://x/1.mp4')
  })

  it('resets filters back to the route camera', async () => {
    hoisted.route.params = { id: '2' }
    cameraRuntimeApi.getCameras.mockResolvedValue([])
    cameraRuntimeApi.getArchiveSegments.mockResolvedValue({ items: [], total: 0 })
    const wrapper = mount(CameraArchive)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Tìm video').trigger('click')
    await wrapper.findAll('button').find((b) => b.text() === 'Đặt lại').trigger('click')
    await flushPromises()
    expect(cameraRuntimeApi.getArchiveSegments).toHaveBeenLastCalledWith(expect.objectContaining({ cameraId: 2 }))
  })

  it('keeps the all-camera filter and lists each matching DVR by camera name', async () => {
    hoisted.route.params = {}
    cameraRuntimeApi.getCameras.mockResolvedValue([{ cameraId: 1, cameraName: 'cam1' }])
    cameraRuntimeApi.getDvrStatus.mockResolvedValue([{ cameraId: 1, cameraName: 'cam1', recordingDate: '2026-09-04', segmentCount: 12, durationSeconds: 48 }])
    cameraRuntimeApi.getArchiveSegments.mockResolvedValue({ items: [], total: 0 })

    const wrapper = mount(CameraArchive)
    await flushPromises()

    expect(wrapper.find('select').element.value).toBe('')
    expect(wrapper.text()).toContain('DVR liên tục ngày')
    expect(wrapper.text()).toContain('cam1')
    expect(cameraRuntimeApi.getArchiveSegments).toHaveBeenLastCalledWith(expect.not.objectContaining({ cameraId: expect.anything() }))
  })
})
