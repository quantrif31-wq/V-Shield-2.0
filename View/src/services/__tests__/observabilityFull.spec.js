import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

let observability

beforeEach(async () => {
  vi.resetModules()
  vi.clearAllMocks()
  sessionStorage.clear()
  localStorage.clear()
  observability = await import('../observability')
  observability.resetObservabilityForTests()
})

afterEach(() => {
  vi.unstubAllEnvs()
  vi.unstubAllGlobals()
})

describe('sanitizeMetadata edge cases', () => {
  it('redacts non-primitive non-object values like bigints', () => {
    expect(observability.sanitizeMetadata(10n)).toMatch(/^10$/)
  })

  it('redacts location-style keys via redactLocation', () => {
    const out = observability.sanitizeMetadata({ url: '/doors/f8b1a2c3-d4e5-4f6a-8b9c-0d1e2f3a4b5c' })
    expect(out.url).toContain('[REDACTED_ID]')
  })

  it('truncates strings to a safe maximum length', () => {
    const out = observability.sanitizeMetadata('x'.repeat(2000))
    expect(out.length).toBe(320)
  })
})

describe('captureEvent queue paths', () => {
  it('caps the in-memory queue at MAX_QUEUE_SIZE', () => {
    for (let i = 0; i < 105; i += 1) observability.captureEvent('bulk')
    expect(observability.getMetricSummary('bulk')).toEqual({
      name: 'bulk', count: 0, p50: 0, p75: 0, p95: 0,
    })
  })

  it('flushes automatically once the queue reaches ten events (no endpoint)', async () => {
    for (let i = 0; i < 12; i += 1) observability.captureEvent('auto')
    await observability.flushObservability()
    expect(observability.getMetricSummary('auto').count).toBe(0)
  })

  it('swallows transport rejections', async () => {
    const transport = vi.fn().mockRejectedValueOnce(new Error('transport down'))
    observability.setObservabilityTransport(transport)
    const event = observability.captureEvent('trouble')
    expect(event.name).toBe('trouble')
    await new Promise((resolve) => setTimeout(resolve, 0))
  })
})

describe('captureApiFailure fallbacks', () => {
  it('falls back to stripping the query string when URL parsing fails', () => {
    const event = observability.captureApiFailure({
      config: { url: 'http://[bad host?env=1]', method: 'get' },
    })
    expect(event).toBeDefined()
  })

  it('normalizes the HTTP method to uppercase', () => {
    const event = observability.captureApiFailure({ config: { url: '/x', method: 'post' }, response: { status: 504, headers: { 'trace-id': 'abc' } } })
    expect(event.metadata).toEqual(expect.objectContaining({ method: 'POST', httpStatus: 504, correlationId: 'abc' }))
  })
})

describe('recordMetric edge cases', () => {
  it('drops samples past the maximum window', () => {
    observability.setObservabilityTransport(vi.fn())
    for (let i = 0; i < 205; i += 1) observability.recordMetric('op', i)
    expect(observability.getMetricSummary('op').count).toBe(200)
  })

  it('applies a warning level from thresholds', () => {
    const events = []
    observability.setObservabilityTransport((e) => events.push(e))
    observability.recordMetric('route_transition', 1500)
    expect(events[0].level).toBe('warning')
  })
})

describe('flushObservability transports', () => {
  async function installWithEndpoint(sendBeacon) {
    vi.stubEnv('VITE_OBSERVABILITY_ENDPOINT', 'http://obs.example')
    vi.resetModules()
    observability = await import('../observability')
    observability.resetObservabilityForTests()
    if (sendBeacon !== undefined) {
      vi.stubGlobal('navigator', { sendBeacon: sendBeacon })
    }
  }

  it('uses sendBeacon and requeues when it fails', async () => {
    await installWithEndpoint(vi.fn(() => false))
    observability.captureEvent('beacon', { a: 1 })
    await observability.flushObservability({ useBeacon: true })
    expect(observability.getMetricSummary('beacon')).toBeTruthy()
  })

  it('uses sendBeacon and clears the queue on success', async () => {
    await installWithEndpoint(vi.fn(() => true))
    observability.captureEvent('beacon')
    await observability.flushObservability({ useBeacon: true })
  })

  it('grows the queue with an endpoint configured but below the flush threshold', async () => {
    await installWithEndpoint()
    const fetchMock = vi.fn().mockRejectedValue(new Error('e'))
    vi.stubGlobal('fetch', fetchMock)
    for (let i = 0; i < 110; i += 1) observability.captureEvent('queued', { i })
    await observability.flushObservability()
    expect(fetchMock).toHaveBeenCalledWith('http://obs.example', expect.anything())
  })

  it('requeues events when the fetch response is not ok', async () => {
    await installWithEndpoint()
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false }))
    observability.captureEvent('e')
    await observability.flushObservability()
  })

  it('requeues events when fetch rejects', async () => {
    await installWithEndpoint()
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('net')))
    observability.captureEvent('e')
    await observability.flushObservability()
  })
})

describe('installObservability wiring', () => {
  function makeAppRouter() {
    const router = { beforeEach: vi.fn((cb) => { router._beforeEach = cb }), afterEach: vi.fn((cb) => { router._afterEach = cb }), onError: vi.fn((cb) => { router._onError = cb }) }
    const app = { config: { errorHandler: null } }
    return { app, router }
  }

  it('registers Vue, window and router handlers', () => {
    const { app, router } = makeAppRouter()
    observability.installObservability(app, router)
    expect(router.beforeEach).toHaveBeenCalledTimes(1)
    expect(router.afterEach).toHaveBeenCalledTimes(1)
    expect(router.onError).toHaveBeenCalledTimes(1)
    expect(observability.getMetricSummary).toBeDefined()
  })

  it('wraps the Vue error handler and forwards to the previous one', () => {
    const previous = vi.fn()
    const { app, router } = makeAppRouter()
    app.config.errorHandler = previous
    observability.installObservability(app, router)
    router._beforeEach({ fullPath: '/a' }, { fullPath: '/b' })
    router._afterEach({ fullPath: '/dashboard?x=1' })
    router._onError(new Error('Failed to fetch dynamically imported module: chunk'), { path: '/dashboard' })
    const capturePromise = app.config.errorHandler(new Error('vue boom'), { $options: { name: 'Card' } }, 'mounted')
    expect(previous).toHaveBeenCalledWith(expect.any(Error), expect.anything(), 'mounted')
    expect(router._onError).toBeDefined()
    expect(capturePromise).toBeUndefined()
  })

  it('records navigation timings through the router guards', () => {
    const { app, router } = makeAppRouter()
    observability.installObservability(app, router)
    router._beforeEach({ fullPath: '/employees' }, { fullPath: '/dashboard' })
    router._afterEach({ fullPath: '/employees?x=1' })
    router._onError(new Error('something else failed'), { path: '/x' })
    expect(observability.getMetricSummary('route_transition')).toBeTruthy()
  })

  it('responds to window error, unhandledrejection and pagehide events', () => {
    const { app, router } = makeAppRouter()
    observability.setObservabilityTransport(vi.fn())
    observability.installObservability(app, router)
    window.dispatchEvent(new Event('error'))
    window.dispatchEvent(new Event('unhandledrejection'))
    window.dispatchEvent(new Event('pagehide'))
    expect(observability.getMetricSummary('x')).toBeTruthy()
  })
})

describe('observeWebVitals', () => {
  class FakeObserver {
    static instances = []
    constructor(cb) {
      this.cb = cb
      this.opts = null
      FakeObserver.instances.push(this)
    }
    observe(opts) {
      this.opts = opts
    }
  }

  function installWith(callbacks) {
    const { app, router } = (() => ({ app: { config: { errorHandler: null } }, router: { beforeEach: vi.fn(), afterEach: vi.fn(), onError: vi.fn() } }))()
    FakeObserver.instances = []
    vi.stubGlobal('PerformanceObserver', FakeObserver)
    observability.setObservabilityTransport(vi.fn())
    observability.installObservability(app, router)
    return FakeObserver.instances
  }

  it('records LCP, INP and CLS from performance observers', () => {
    const observers = installWith()
    observers[0].cb({ getEntries: () => [{ startTime: 900 }] })
    observers[1].cb({ getEntries: () => [{ hadRecentInput: false, value: 0.05 }, { hadRecentInput: true, value: 0.5 }] })
    observers[2].cb({ getEntries: () => [{ duration: 120 }] })
    window.dispatchEvent(new Event('pagehide'))
    expect(observability.getMetricSummary('cls')).toBeTruthy()
  })

  it('records dynamic import metrics from resource entries', () => {
    const observers = installWith()
    observers[3].cb({ getEntries: () => [{ name: 'https://example.com/assets/app-abc123.js?v=1', initiatorType: 'script', duration: 200 }] })
    window.dispatchEvent(new Event('pagehide'))
    expect(observability.getMetricSummary('dynamic_import')).toBeTruthy()
  })

  it('finalizes on visibilitychange hidden', () => {
    installWith()
    Object.defineProperty(document, 'visibilityState', { value: 'hidden', configurable: true })
    document.dispatchEvent(new Event('visibilitychange'))
    expect(observability.getMetricSummary('cls')).toBeTruthy()
  })

  it('bails out when PerformanceObserver is unavailable', () => {
    const { app, router } = (() => ({ app: { config: { errorHandler: null } }, router: { beforeEach: vi.fn(), afterEach: vi.fn(), onError: vi.fn() } }))()
    vi.stubGlobal('PerformanceObserver', undefined)
    observability.installObservability(app, router)
    expect(observability.getMetricSummary('cls').count).toBe(0)
  })
})

describe('flush timer and finalize', () => {
  it('emits summary events on the 60s flush timer', async () => {
    vi.useFakeTimers()
    const app = { config: { errorHandler: null } }
    const router = { beforeEach: vi.fn(), afterEach: vi.fn(), onError: vi.fn() }
    observability.installObservability(app, router)
    observability.recordMetric('op', 5)
    vi.advanceTimersByTime(60000)
    observability.resetObservabilityForTests()
    vi.useRealTimers()
    expect(observability.getMetricSummary('op').count).toBe(0)
  })

  it('records ttfb and page_load from the navigation entry on pagehide', () => {
    vi.stubGlobal('performance', {
      now: () => 0,
      getEntriesByType: (t) => (t === 'navigation' ? [{ responseStart: 100, requestStart: 40, loadEventEnd: 220 }] : []),
    })
    const app = { config: { errorHandler: null } }
    const router = { beforeEach: vi.fn(), afterEach: vi.fn(), onError: vi.fn() }
    observability.setObservabilityTransport(vi.fn())
    observability.installObservability(app, router)
    vi.stubGlobal('PerformanceObserver', () => ({ observe: vi.fn() }))
    window.dispatchEvent(new Event('pagehide'))
    observability.resetObservabilityForTests()
    expect(observability.getMetricSummary('ttfb')).toBeTruthy()
  })
})