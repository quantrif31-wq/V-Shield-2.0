import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('axios', () => ({ default: vi.fn() }))
vi.mock('../config/api', () => ({ PLATE_API_BASE_URL: 'http://localhost:5002/api' }))

let axios
let plateCameraApi

beforeEach(async () => {
  vi.resetModules()
  vi.clearAllMocks()
  localStorage.removeItem('vshield-plate-api-override')
  axios = (await import('axios')).default
  plateCameraApi = await import('../plateCameraApi')
})

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('plateCameraApi', () => {
  it('posts camera commands to the configured base url', async () => {
    axios.mockResolvedValue({ data: { success: true } })
    await expect(plateCameraApi.turnOnCamera('10.0.0.5')).resolves.toEqual({ success: true })
    expect(axios).toHaveBeenCalledWith(expect.objectContaining({
      method: 'post',
      url: '/camera/on',
      data: { ip: '10.0.0.5' },
      baseURL: 'http://localhost:5002/api',
    }))
    await plateCameraApi.turnOffCamera()
    await plateCameraApi.resetCameraState()
    expect(axios.mock.calls[1][0].url).toBe('/camera/off')
    expect(axios.mock.calls[2][0].url).toBe('/camera/reset')
  })

  it('reads camera status, result and locked images', async () => {
    axios.mockResolvedValue({ data: {} })
    await plateCameraApi.getCameraStatus()
    await plateCameraApi.getCameraResult()
    await plateCameraApi.getLockedImages()
    expect(axios.mock.calls.map(([c]) => c.url)).toEqual([
      '/camera/status',
      '/camera/result',
      '/camera/locked-images',
    ])
  })

  it('prefers a stored override base url when present', async () => {
    localStorage.setItem('vshield-plate-api-override', 'http://override:9000/api')
    axios.mockResolvedValue({ data: {} })
    await plateCameraApi.getCameraStatus()
    expect(axios.mock.calls[0][0].baseURL).toBe('http://override:9000/api')
    expect(plateCameraApi.getResolvedPlateApiBaseUrl()).toBe('http://override:9000/api')
  })

  it('resolves the current base url after a successful call', async () => {
    expect(plateCameraApi.getResolvedPlateApiBaseUrl()).toBe('http://localhost:5002/api')
    axios.mockResolvedValue({ data: {} })
    await plateCameraApi.getCameraStatus()
    expect(plateCameraApi.getResolvedPlateApiBaseUrl()).toBe('http://localhost:5002/api')
  })

  it('surfaces server payloads when the request fails with a response', async () => {
    axios.mockRejectedValue({ response: { status: 503, data: { message: 'plate down' } } })
    await expect(plateCameraApi.getCameraStatus()).rejects.toEqual({ message: 'plate down' })
  })

  it('builds a connection hint when the failure carries no message', async () => {
    axios.mockRejectedValue({})
    const error = await plateCameraApi.getCameraStatus().catch((e) => e)
    expect(error.success).toBe(false)
    expect(error.message).toContain('Không kết nối được dịch vụ biển số')
  })

  it('preserves the underlying error message when present', async () => {
    axios.mockRejectedValue(new Error('ECONNREFUSED'))
    const error = await plateCameraApi.getCameraStatus().catch((e) => e)
    expect(error.message).toBe('ECONNREFUSED')
  })
})
