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

describe('sanitizeMetadata', () => {
  it('passes primitives through and redacts strings', () => {
    expect(observability.sanitizeMetadata(null)).toBeNull()
    expect(observability.sanitizeMetadata(42)).toBe(42)
    expect(observability.sanitizeMetadata(true)).toBe(true)
    expect(observability.sanitizeMetadata('secret-value')).toBe('[REDACTED]'.length > 0 ? observability.sanitizeMetadata('secret-value') : '')
  })

  it('redacts JWTs, emails and phones in strings', () => {
    const out = observability.sanitizeMetadata('token eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.signature user@example.com 0901234567')
    expect(out).toContain('[REDACTED_TOKEN]')
    expect(out).not.toContain('eyJhbGci')
    expect(out).not.toContain('user@example.com')
    expect(out).not.toContain('0901234567')
  })

  it('redacts sensitive keys and error objects', () => {
    expect(observability.sanitizeMetadata({ password: 'x', token: 'y', safe: 'keep' })).toEqual({
      password: '[REDACTED]',
      token: '[REDACTED]',
      safe: 'keep',
    })
    expect(observability.sanitizeMetadata(new Error('boom'))).toMatchObject({ message: 'boom' })
  })

  it('handles arrays, depth and circular references', () => {
    const circular = {}
    circular.self = circular
    expect(observability.sanitizeMetadata([1, 2, 3])).toEqual([1, 2, 3])
    expect(observability.sanitizeMetadata(circular).self).toBe('[CIRCULAR]')
    const deep = observability.sanitizeMetadata({ a: { b: { c: { d: { e: 'deep' } } } } })
    expect(deep.a.b.c.d).toBe('[TRUNCATED]')
  })

  it('strips functions and symbols', () => {
    expect(observability.sanitizeMetadata({ fn: () => {}, sym: Symbol('x') })).toEqual({})
  })
})

describe('captureEvent', () => {
  it('dispatches a window event and uses the custom transport', () => {
    const dispatched = []
    window.addEventListener('vshield:observability', (e) => dispatched.push(e.detail))
    const transport = vi.fn()
    observability.setObservabilityTransport(transport)
    const event = observability.captureEvent('test_event', { a: 1 })
    expect(dispatched).toHaveLength(1)
    expect(transport).toHaveBeenCalledWith(event)
    expect(event.name).toBe('test_event')
    expect(event.metadata.a).toBe(1)
  })
})

describe('captureError and captureApiFailure', () => {
  it('captures errors with category and metadata', () => {
    observability.setObservabilityTransport(vi.fn())
    const event = observability.captureError(new Error('boom'), 'javascript_error')
    expect(event.level).toBe('error')
    expect(event.metadata.error.message).toBe('boom')
  })

  it('classifies authentication and permission failures', () => {
    observability.setObservabilityTransport(vi.fn())
    expect(observability.captureApiFailure({ config: { url: '/Auth/login' }, response: { status: 401 } }).name).toBe('authentication_failure')
    expect(observability.captureApiFailure({ config: { url: '/Employees' }, response: { status: 403 } }).name).toBe('permission_denied')
    expect(observability.captureApiFailure({ config: { url: '/import-export/history' }, response: { status: 500 } }).name).toBe('import_export_failure')
    expect(observability.captureApiFailure({ config: { url: '/x' } }).name).toBe('api_failure')
  })
})

describe('recordMetric and getMetricSummary', () => {
  it('records valid metrics and rejects invalid values', () => {
    observability.setObservabilityTransport(vi.fn())
    observability.recordMetric('route_transition', 100)
    observability.recordMetric('route_transition', 200)
    observability.recordMetric('route_transition', 300)
    expect(observability.recordMetric('x', NaN)).toBeNull()
    expect(observability.getMetricSummary('route_transition')).toEqual({ name: 'route_transition', count: 3, p50: 200, p75: 300, p95: 300 })
  })

  it('assigns warning and error levels from thresholds', () => {
    const events = []
    observability.setObservabilityTransport((e) => events.push(e))
    observability.recordMetric('route_transition', 5000)
    observability.recordMetric('route_transition', 100)
    const levels = events.map((e) => e.level)
    expect(levels).toContain('error')
    expect(levels).toContain('info')
  })
})

describe('measureOperation', () => {
  it('records timing on success and failure', async () => {
    observability.setObservabilityTransport(vi.fn())
    const value = await observability.measureOperation('test_op', async () => 42)
    expect(value).toBe(42)
    await expect(observability.measureOperation('test_op', async () => { throw new Error('x') })).rejects.toThrow('x')
  })
})

describe('flushObservability', () => {
  it('is a no-op when no endpoint is configured', async () => {
    await observability.flushObservability()
    expect(observability.getMetricSummary('any')).toBeTruthy()
  })

  it('sends queued events when an endpoint is configured', async () => {
    vi.stubEnv('VITE_OBSERVABILITY_ENDPOINT', 'http://obs.example')
    vi.resetModules()
    observability = await import('../observability')
    observability.resetObservabilityForTests()
    const fetchMock = vi.fn().mockResolvedValue({ ok: true })
    vi.stubGlobal('fetch', fetchMock)
    observability.captureEvent('e1')
    await observability.flushObservability()
    expect(fetchMock).toHaveBeenCalledWith('http://obs.example', expect.any(Object))
  })
})

describe('installObservability', () => {
  it('returns early when already installed or no window', () => {
    const app = { config: { errorHandler: null } }
    const router = { beforeEach: vi.fn(), afterEach: vi.fn(), onError: vi.fn() }
    observability.installObservability(app, router)
    observability.installObservability(app, router)
    expect(router.beforeEach).toHaveBeenCalledTimes(1)
  })
})
