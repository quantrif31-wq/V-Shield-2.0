import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../http', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
    request: vi.fn(),
    defaults: { headers: { common: {} } },
  },
}))

const http = (await import('../http')).default
const cameraRuntimeApi = await import('../cameraRuntimeApi')
const cameraRegistryApi = await import('../cameraRegistryApi')

beforeEach(() => vi.clearAllMocks())

describe('cameraRuntimeApi CRUD', () => {
  it('gets cameras and camera by id', async () => {
    http.get.mockResolvedValue({ data: [{ cameraId: 1 }] })
    await expect(cameraRuntimeApi.getCameras()).resolves.toEqual([{ cameraId: 1 }])
    expect(http.get).toHaveBeenCalledWith('/camera-runtime')
    http.get.mockResolvedValue({ data: { cameraId: 1 } })
    await expect(cameraRuntimeApi.getCameraById(1)).resolves.toEqual({ cameraId: 1 })
    expect(http.get).toHaveBeenCalledWith('/camera-runtime/1')
  })

  it('creates, updates and deletes cameras', async () => {
    http.post.mockResolvedValue({ data: { id: 1 } })
    await cameraRuntimeApi.createCamera({ cameraName: 'c' })
    expect(http.post).toHaveBeenCalledWith('/camera-runtime', { cameraName: 'c' })
    http.put.mockResolvedValue({ data: { id: 1 } })
    await cameraRuntimeApi.updateCamera(1, { cameraName: 'd' })
    expect(http.put).toHaveBeenCalledWith('/camera-runtime/1', { cameraName: 'd' })
    http.delete.mockResolvedValue({ data: {} })
    await cameraRuntimeApi.deleteCamera(1)
    expect(http.delete).toHaveBeenCalledWith('/camera-runtime/1')
  })
})

describe('cameraRuntimeApi process controls', () => {
  it('posts process start/stop commands', async () => {
    http.post.mockResolvedValue({ data: {} })
    await cameraRuntimeApi.reloadGo2rtc()
    await cameraRuntimeApi.stopGo2rtc()
    await cameraRuntimeApi.startPythonQrProcess()
    await cameraRuntimeApi.stopPythonQrProcess()
    await cameraRuntimeApi.startPythonPlateProcess()
    await cameraRuntimeApi.stopPythonPlateProcess()
    await cameraRuntimeApi.startPythonSimulatedCameraProcess()
    await cameraRuntimeApi.stopPythonSimulatedCameraProcess()
    const urls = http.post.mock.calls.map(([url]) => url)
    expect(urls).toEqual([
      '/camera-runtime/reload-go2rtc',
      '/camera-runtime/stop-go2rtc',
      '/camera-runtime/start-python-qr',
      '/camera-runtime/stop-python-qr',
      '/camera-runtime/start-python-plate',
      '/camera-runtime/stop-python-plate',
      '/camera-runtime/start-python-cam-gia-lap',
      '/camera-runtime/stop-python-cam-gia-lap',
    ])
    await cameraRuntimeApi.getPythonProcessStatus()
    expect(http.get).toHaveBeenCalledWith('/camera-runtime/status-python')
  })
})

describe('cameraRuntimeApi recording and archive', () => {
  it('toggles recording and fetches segments', async () => {
    http.put.mockResolvedValue({ data: { ok: true } })
    await cameraRuntimeApi.toggleRecording(1, true, 30)
    expect(http.put).toHaveBeenCalledWith('/camera-runtime/1/recording', { enabled: true, retentionDays: 30 })
    http.get.mockResolvedValue({ data: [] })
    await cameraRuntimeApi.getRecordedSegments(1, { page: 1 })
    expect(http.get).toHaveBeenCalledWith('/camera-runtime/1/recorded-segments', { params: { page: 1 } })
    await cameraRuntimeApi.getArchiveSegments({ from: 'x' })
    expect(http.get).toHaveBeenCalledWith('/camera-runtime/archive/segments', { params: { from: 'x' } })
  })
})

describe('cameraRuntimeApi ensureCameraRegistered', () => {
  it('returns null when no usable stream url is provided', async () => {
    await expect(cameraRuntimeApi.ensureCameraRegistered({})).resolves.toBeNull()
  })

  it('creates a camera when none matches and reloads go2rtc', async () => {
    http.get.mockResolvedValue({ data: [] })
    http.post.mockResolvedValue({ data: { cameraId: 9 } })
    const created = await cameraRuntimeApi.ensureCameraRegistered({
      cameraName: 'Cổng A',
      gateId: 2,
      streamUrl: 'http://10.0.0.5:8081',
    })
    expect(created).toEqual({ cameraId: 9 })
    expect(http.post).toHaveBeenCalledWith('/camera-runtime', expect.objectContaining({
      cameraName: 'Cổng A',
      gateId: 2,
      isRecordingEnabled: true,
    }))
    expect(http.post).toHaveBeenCalledWith('/camera-runtime/reload-go2rtc')
  })

  it('returns the existing camera when it already matches', async () => {
    http.get.mockResolvedValue({
      data: [{ cameraId: 1, cameraName: 'Cổng A', cameraType: 'Network', streamUrl: 'http://10.0.0.5:8081/video', isRecordingEnabled: true, recordingRetentionDays: 30 }],
    })
    const existing = await cameraRuntimeApi.ensureCameraRegistered({
      cameraName: 'Cổng A',
      streamUrl: 'http://10.0.0.5:8081',
      recordingRetentionDays: 30,
    })
    expect(existing.cameraId).toBe(1)
    expect(http.post).not.toHaveBeenCalled()
  })

  it('updates a matched camera when settings differ', async () => {
    http.get.mockResolvedValue({
      data: [{ cameraId: 1, cameraName: '', streamUrl: 'http://10.0.0.5:8081/video', isRecordingEnabled: false, recordingRetentionDays: 7 }],
    })
    http.put.mockResolvedValue({ data: { cameraId: 1 } })
    await cameraRuntimeApi.ensureCameraRegistered({
      cameraName: 'Mới',
      streamUrl: 'http://10.0.0.5:8081',
      recordingRetentionDays: 30,
    })
    expect(http.put).toHaveBeenCalledWith('/camera-runtime/1', expect.objectContaining({
      cameraName: 'Mới',
      isRecordingEnabled: true,
      recordingRetentionDays: 30,
    }))
  })
})

describe('cameraRegistryApi', () => {
  it('maps and dedupes configured cameras', async () => {
    http.get.mockResolvedValue({
      data: [
        { cameraId: 1, cameraName: 'Cổng A', streamUrl: 'http://10.0.0.5/video', urlView: 'http://10.0.0.5:8081' },
        { cameraId: 2, cameraName: 'Cổng B', streamUrl: 'rtsp://x/y' },
        { cameraId: 1, cameraName: 'Cổng A', streamUrl: 'http://10.0.0.5/video' },
        { cameraId: 0 },
      ],
    })
    const result = await cameraRegistryApi.getConfiguredCameras()
    expect(result).toHaveLength(2)
    expect(result[0]).toMatchObject({ id: 1, name: 'CAM-01', label: 'Cổng A', enabled: true })
    expect(result[1]).toMatchObject({ id: 2, name: 'CAM-02', enabled: true })
  })
})
