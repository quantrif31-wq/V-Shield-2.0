import { beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => {
  const reqHandlers = []
  const resFulfilled = []
  const resRejected = []
  return { reqHandlers, resFulfilled, resRejected }
})

vi.mock('../observability', () => ({
  captureApiFailure: vi.fn(),
  captureEvent: vi.fn(),
  recordMetric: vi.fn(),
}))

vi.mock('axios', () => {
  const instance = Object.assign(vi.fn(() => Promise.resolve({ data: {} })), {
    interceptors: {
      request: { use: (h) => (hoisted.reqHandlers.push(h), 0) },
      response: { use: (h, e) => (hoisted.resFulfilled.push(h), hoisted.resRejected.push(e), 0) },
    },
    defaults: { headers: { common: {} } },
  })
  return { default: { create: () => instance, post: vi.fn() } }
})

let axios
let http
let observability
let instance
let requestHandler
let responseHandler
let errorHandler

beforeEach(async () => {
  vi.resetModules()
  vi.clearAllMocks()
  hoisted.reqHandlers.length = 0
  hoisted.resFulfilled.length = 0
  hoisted.resRejected.length = 0
  sessionStorage.clear()
  localStorage.clear()
  axios = (await import('axios')).default
  http = (await import('../http')).default
  observability = await import('../observability')
  instance = axios.create()
  requestHandler = hoisted.reqHandlers[0]
  responseHandler = hoisted.resFulfilled[0]
  errorHandler = hoisted.resRejected[0]
  window.history.pushState({}, '', '/login')
})

describe('http request interceptor', () => {
  it('injects the bearer token and starts observability timing', () => {
    sessionStorage.setItem('v_shield_token', 'tok-123')
    const config = requestHandler({ headers: {} })
    expect(config.headers.Authorization).toBe('Bearer tok-123')
    expect(typeof config.metadata.observabilityStartedAt).toBe('number')
  })

  it('falls back to localStorage for the token', () => {
    localStorage.setItem('v_shield_token', 'loc-tok')
    const config = requestHandler({ headers: {} })
    expect(config.headers.Authorization).toBe('Bearer loc-tok')
  })

  it('omits the Authorization header when no token exists', () => {
    const config = requestHandler({ headers: {} })
    expect(config.headers.Authorization).toBeUndefined()
  })
})

describe('http response interceptor', () => {
  it('records an api_request metric with timing metadata', () => {
    const config = { method: 'get', url: '/Employees', metadata: { observabilityStartedAt: performance.now() } }
    responseHandler({ config, status: 200, headers: { 'x-correlation-id': 'corr-1' } })
    expect(observability.recordMetric).toHaveBeenCalledWith('api_request', expect.any(Number), expect.objectContaining({
      method: 'GET',
      path: '/Employees',
      httpStatus: 200,
      correlationId: 'corr-1',
    }))
  })

  it('skips metric recording when timing metadata is missing', () => {
    responseHandler({ config: {}, status: 200, headers: {} })
    expect(observability.recordMetric).not.toHaveBeenCalled()
  })
})

describe('http error interceptor', () => {
  it('refreshes the token and retries the original request once', async () => {
    sessionStorage.setItem('v_shield_refresh_token', 'refresh-1')
    sessionStorage.setItem('v_shield_token', 'old')
    axios.post.mockResolvedValue({ data: { token: 'new', refreshToken: 'refresh-2' } })
    instance.mockResolvedValue({ data: { ok: true } })

    const result = await errorHandler({
      config: { url: '/protected', headers: {} },
      response: { status: 401 },
    })

    expect(axios.post).toHaveBeenCalledWith(expect.stringContaining('/Auth/refresh'), { refreshToken: 'refresh-1' })
    expect(sessionStorage.getItem('v_shield_token')).toBe('new')
    expect(sessionStorage.getItem('v_shield_refresh_token')).toBe('refresh-2')
    expect(instance).toHaveBeenCalledWith(expect.objectContaining({
      headers: { Authorization: 'Bearer new' },
      _retry: true,
    }))
    expect(result).toEqual({ data: { ok: true } })
  })

  it('reuses a single refresh promise across concurrent failures', async () => {
    sessionStorage.setItem('v_shield_refresh_token', 'refresh-1')
    sessionStorage.setItem('v_shield_token', 'old')
    axios.post.mockResolvedValue({ data: { token: 'new', refreshToken: 'refresh-2' } })
    instance.mockResolvedValue({ data: {} })

    const failure = { config: { url: '/a', headers: {} }, response: { status: 401 } }
    await Promise.all([errorHandler(failure), errorHandler({ ...failure, config: { url: '/b', headers: {} } })])
    expect(axios.post).toHaveBeenCalledTimes(1)
  })

  it('clears auth state and redirects when the refresh fails', async () => {
    sessionStorage.setItem('v_shield_refresh_token', 'refresh-1')
    sessionStorage.setItem('v_shield_token', 'old')
    axios.post.mockRejectedValue(new Error('refresh failed'))

    await expect(errorHandler({
      config: { url: '/protected', headers: {} },
      response: { status: 401 },
    })).rejects.toMatchObject({ response: { status: 401 } })

    expect(sessionStorage.getItem('v_shield_token')).toBeNull()
    expect(sessionStorage.getItem('v_shield_refresh_token')).toBeNull()
  })

  it('does not attempt a refresh for login or refresh requests', async () => {
    await expect(errorHandler({
      config: { url: '/Auth/login', headers: {} },
      response: { status: 401 },
    })).rejects.toMatchObject({ response: { status: 401 } })
    await expect(errorHandler({
      config: { url: '/Auth/refresh', headers: {} },
      response: { status: 401 },
    })).rejects.toMatchObject({ response: { status: 401 } })
    expect(axios.post).not.toHaveBeenCalled()
  })

  it('redirects to login once when no refresh token is available', async () => {
    const replace = vi.fn()
    window.history.pushState({}, '', '/dashboard')
    const originalLocation = window.location
    Object.defineProperty(window, 'location', {
      value: { ...originalLocation, pathname: '/dashboard', replace },
      writable: true,
      configurable: true,
    })

    await expect(errorHandler({
      config: { url: '/protected', headers: {} },
      response: { status: 401 },
    })).rejects.toMatchObject({ response: { status: 401 } })

    expect(replace).toHaveBeenCalledWith('/login')
    expect(sessionStorage.getItem('v_shield_token')).toBeNull()
  })

  it('records the api failure before handling it', async () => {
    const failure = { config: { url: '/x', headers: {} }, response: { status: 403 } }
    await expect(errorHandler(failure)).rejects.toMatchObject({ response: { status: 403 } })
    expect(observability.captureApiFailure).toHaveBeenCalledWith(failure)
  })
})

describe('http redirect-to-login once', () => {
  it('is a no-op when a redirect is already in progress', async () => {
    const replace = vi.fn()
    window.history.pushState({}, '', '/dashboard')
    const originalLocation = window.location
    Object.defineProperty(window, 'location', {
      value: { ...originalLocation, pathname: '/dashboard', replace },
      writable: true,
      configurable: true,
    })
    await expect(errorHandler({ config: { url: '/a' }, response: { status: 401 } })).rejects.toMatchObject({ response: { status: 401 } })
    await expect(errorHandler({ config: { url: '/b' }, response: { status: 401 } })).rejects.toMatchObject({ response: { status: 401 } })
    expect(replace).toHaveBeenCalledTimes(1)
  })

  it('resets the redirect flag via the timeout when already on /login', async () => {
    const replace = vi.fn()
    vi.useFakeTimers()
    const originalLocation = window.location
    Object.defineProperty(window, 'location', {
      value: { ...originalLocation, pathname: '/login', replace },
      writable: true,
      configurable: true,
    })
    const pending = errorHandler({ config: { url: '/a' }, response: { status: 401 } })
    await pending.catch(() => {})
    expect(replace).not.toHaveBeenCalled()
    vi.advanceTimersByTime(1300)
    vi.useRealTimers()
  })
})
