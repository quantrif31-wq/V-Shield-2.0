import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('axios', () => ({
  default: { isCancel: vi.fn(() => false) },
}))

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

const axios = (await import('axios')).default
const http = (await import('../http')).default
const faceApi = await import('../faceApi')

beforeEach(() => vi.clearAllMocks())

describe('normalizeFaceApiError', () => {
  it('passes through already normalized errors', () => {
    const err = new Error('x')
    err.isFaceApiError = true
    expect(faceApi.normalizeFaceApiError(err)).toBe(err)
  })

  it.each([
    [401, 'session-expired'],
    [403, 'forbidden'],
    [400, 'validation'],
    [409, 'reload-in-progress'],
    [422, 'model-rejected'],
    [500, 'server-error'],
    [503, 'runtime-unavailable'],
  ])('maps http %s to code %s', (status, code) => {
    const err = faceApi.normalizeFaceApiError({ response: { status, data: { message: 'm' } } })
    expect(err.code).toBe(code)
    expect(err.isFaceApiError).toBe(true)
    expect(err.status).toBe(status)
  })

  it('maps cancelled requests', () => {
    axios.isCancel.mockReturnValueOnce(true)
    const err = faceApi.normalizeFaceApiError({ code: 'ERR_CANCELED' })
    expect(err.code).toBe('cancelled')
    expect(err.cancelled).toBe(true)
  })

  it('maps unreachable backends', () => {
    const err = faceApi.normalizeFaceApiError(new Error('network'))
    expect(err.code).toBe('backend-unreachable')
  })

  it('keeps server message for validation errors', () => {
    const err = faceApi.normalizeFaceApiError({ response: { status: 400, data: { message: 'không hợp lệ' } } })
    expect(err.message).toBe('không hợp lệ')
  })

  it('records details for rejected models', () => {
    const err = faceApi.normalizeFaceApiError({ response: { status: 422, data: { issues: [1] } } })
    expect(err.details).toEqual({ issues: [1] })
  })
})

describe('shouldStopFacePolling', () => {
  it('stops on session expiry and forbidden', () => {
    expect(faceApi.shouldStopFacePolling({ response: { status: 401 } })).toBe(true)
    expect(faceApi.shouldStopFacePolling({ response: { status: 403 } })).toBe(true)
    expect(faceApi.shouldStopFacePolling({ response: { status: 500 } })).toBe(false)
  })
})

describe('faceApi camera commands', () => {
  it('rejects invalid camera ids synchronously', () => {
    expect(() => faceApi.startCamera('bad../id', '1.2.3.4')).toThrowError('cameraId không hợp lệ')
  })

  it('starts a camera with optional lane id', async () => {
    http.request.mockResolvedValue({ data: { ok: true } })
    await expect(faceApi.startCamera('CAM-01', '10.0.0.5', 3)).resolves.toEqual({ ok: true })
    expect(http.request).toHaveBeenCalledWith(expect.objectContaining({
      method: 'post',
      url: '/FaceCamera/cameras/CAM-01/start',
      data: { ip: '10.0.0.5', laneId: 3 },
    }))
  })

  it('covers stop/reset/status/result/locked helpers', async () => {
    http.request.mockResolvedValue({ data: {} })
    await faceApi.turnOnCamera('1.2.3.4')
    await faceApi.turnOffCamera()
    await faceApi.resetCameraState()
    await faceApi.getCameras()
    await faceApi.getCameraStatus()
    await faceApi.getCameraResult()
    await faceApi.getLockedImages()
    await faceApi.getModels()
    await faceApi.discoverIpWebcams()
    await faceApi.reloadModels()
    await faceApi.liveEnroll(7, ['img'])
    expect(http.request.mock.calls.length).toBeGreaterThanOrEqual(10)
  })

  it('normalizes failures raised by faceRequest', async () => {
    http.request.mockRejectedValue({ response: { status: 403 } })
    await expect(faceApi.getCameras()).rejects.toMatchObject({ code: 'forbidden' })
  })
})
