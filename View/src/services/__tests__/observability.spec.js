import { afterEach, describe, expect, it } from 'vitest'
import {
  captureApiFailure,
  captureEvent,
  getMetricSummary,
  recordMetric,
  resetObservabilityForTests,
  sanitizeMetadata,
  setObservabilityTransport,
} from '../observability'

afterEach(() => resetObservabilityForTests())

describe('frontend observability privacy boundary', () => {
  it('redacts credentials, tokens and personal data before transport', async () => {
    const received = []
    setObservabilityTransport((event) => received.push(event))
    captureEvent('authentication_failure', {
      password: 'NeverLogMe',
      mfaCode: '123456',
      accessToken: 'eyJhbGciOiJIUzI1NiJ9.payload.signature',
      email: 'person@example.com',
      safeStatus: 401,
    })

    expect(received).toHaveLength(1)
    expect(received[0].metadata).toEqual(expect.objectContaining({
      password: '[REDACTED]',
      mfaCode: '[REDACTED]',
      accessToken: '[REDACTED]',
      email: '[REDACTED]',
      safeStatus: 401,
    }))
    expect(JSON.stringify(received[0])).not.toContain('NeverLogMe')
    expect(JSON.stringify(received[0])).not.toContain('123456')
  })

  it('removes PII patterns from unstructured error strings', () => {
    const result = sanitizeMetadata('User person@example.com / 0901234567 failed with Bearer secret-value')
    expect(result).not.toContain('person@example.com')
    expect(result).not.toContain('0901234567')
    expect(result).not.toContain('secret-value')
  })

  it('removes query strings and opaque secrets embedded in request paths', () => {
    const event = captureApiFailure({
      config: { url: '/pre-registrations/validate/0123456789abcdef0123456789abcdef?mfa=123456', method: 'get' },
      response: { status: 401, headers: {} },
    })

    expect(event.metadata.path).toBe('/pre-registrations/validate/[REDACTED_ID]')
    expect(JSON.stringify(event)).not.toContain('0123456789abcdef')
    expect(JSON.stringify(event)).not.toContain('123456')
  })

  it('records percentile summaries without failing on a single slow sample', () => {
    ;[100, 200, 300, 400, 500].forEach((value) => recordMetric('route_transition', value))
    expect(getMetricSummary('route_transition')).toEqual({
      name: 'route_transition', count: 5, p50: 300, p75: 400, p95: 500,
    })
  })

  it('classifies API permission and network failures without request payloads', () => {
    const received = []
    setObservabilityTransport((event) => received.push(event))
    captureApiFailure({
      config: { method: 'post', url: '/Employees?email=person@example.com', data: { password: 'secret' } },
      response: { status: 403, headers: { 'x-correlation-id': 'trace-123' } },
    })
    expect(received[0].name).toBe('permission_denied')
    expect(received[0].metadata).toEqual(expect.objectContaining({ path: '/Employees', httpStatus: 403, correlationId: 'trace-123' }))
    expect(JSON.stringify(received[0])).not.toContain('person@example.com')
    expect(JSON.stringify(received[0])).not.toContain('secret')
  })
})
