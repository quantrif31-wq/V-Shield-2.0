const SENSITIVE_KEY = /(password|passphrase|mfa|otp|token|authorization|cookie|secret|face|image|photo|qr|evidence|content|messagebody|fullname|email|phone|idcard|identitynumber)/i
const JWT_PATTERN = /\beyJ[a-zA-Z0-9_-]{8,}\.[a-zA-Z0-9_-]{8,}(?:\.[a-zA-Z0-9_-]{8,})?\b/g
const BEARER_PATTERN = /Bearer\s+[a-zA-Z0-9._~+/-]+=*/gi
const EMAIL_PATTERN = /\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b/gi
const PHONE_PATTERN = /(?<!\d)(?:\+?84|0)[\d\s.-]{8,13}(?!\d)/g
const LOCATION_KEY = /(path|route|url|uri|href|source)/i
const MAX_STRING_LENGTH = 320
const MAX_QUEUE_SIZE = 100
const MAX_METRIC_SAMPLES = 200

const environment = import.meta.env.MODE || 'unknown'
const appVersion = import.meta.env.VITE_APP_VERSION || '2.0.0-rc1'
const endpoint = String(import.meta.env.VITE_OBSERVABILITY_ENDPOINT || '').trim()
const queue = []
const metricSamples = new Map()
let customTransport = null
let flushTimer = null
let installed = false
let routeStartedAt = 0
let routeFrom = ''

const metricThresholds = {
  lcp: { warning: 2500, critical: 4000 },
  inp: { warning: 200, critical: 500 },
  cls: { warning: 0.1, critical: 0.25 },
  ttfb: { warning: 800, critical: 1800 },
  page_load: { warning: 3000, critical: 6000 },
  route_transition: { warning: 1200, critical: 3000 },
  dynamic_import: { warning: 1500, critical: 4000 },
  map_initialization: { warning: 2500, critical: 6000 },
  camera_initialization: { warning: 3000, critical: 8000 },
}

function redactString(value) {
  return String(value)
    .replace(JWT_PATTERN, '[REDACTED_TOKEN]')
    .replace(BEARER_PATTERN, 'Bearer [REDACTED_TOKEN]')
    .replace(EMAIL_PATTERN, '[REDACTED_EMAIL]')
    .replace(PHONE_PATTERN, '[REDACTED_PHONE]')
    .slice(0, MAX_STRING_LENGTH)
}

function redactLocation(value) {
  const raw = String(value || '')
  let pathname = raw.split(/[?#]/)[0]
  let origin = ''
  try {
    const parsed = new URL(raw, typeof window === 'undefined' ? 'http://local' : window.location.origin)
    pathname = parsed.pathname
    origin = /^https?:\/\//i.test(raw) ? parsed.origin : ''
  } catch {}
  const safePath = pathname.split('/').map(segment => (
    /^[0-9a-f]{8}-[0-9a-f-]{27,}$/i.test(segment) || /^[A-Za-z0-9_-]{24,}$/.test(segment)
      ? '[REDACTED_ID]'
      : redactString(segment)
  )).join('/')
  return `${origin}${safePath}`.slice(0, MAX_STRING_LENGTH)
}

export function sanitizeMetadata(value, depth = 0, seen = new WeakSet()) {
  if (value === null || value === undefined) return value
  if (typeof value === 'string') return redactString(value)
  if (typeof value === 'number' || typeof value === 'boolean') return value
  if (typeof value === 'function' || typeof value === 'symbol') return undefined
  if (depth >= 4) return '[TRUNCATED]'
  if (value instanceof Error) {
    return {
      name: redactString(value.name || 'Error'),
      message: redactString(value.message || 'Unexpected error'),
    }
  }
  if (typeof value !== 'object') return redactString(value)
  if (seen.has(value)) return '[CIRCULAR]'
  seen.add(value)

  if (Array.isArray(value)) {
    return value.slice(0, 20).map((item) => sanitizeMetadata(item, depth + 1, seen))
  }

  return Object.fromEntries(Object.entries(value).slice(0, 40).map(([key, item]) => [
    key,
    SENSITIVE_KEY.test(key)
      ? '[REDACTED]'
      : LOCATION_KEY.test(key) && typeof item === 'string'
        ? redactLocation(item)
        : sanitizeMetadata(item, depth + 1, seen),
  ]).filter(([, item]) => item !== undefined))
}

function currentContext() {
  let user = null
  try {
    const raw = sessionStorage.getItem('v_shield_user') || localStorage.getItem('v_shield_user')
    user = raw ? JSON.parse(raw) : null
  } catch {}

  const route = typeof window === 'undefined' ? '' : redactLocation(window.location.pathname)
  const module = route.split('/').filter(Boolean)[0] || 'shell'
  return {
    route,
    module,
    userRole: user?.role || 'anonymous',
    site: user?.siteCode || user?.tenantCode || undefined,
  }
}

function buildEvent(name, metadata = {}, level = 'info') {
  return {
    name,
    level,
    timestamp: new Date().toISOString(),
    environment,
    appVersion,
    ...currentContext(),
    metadata: sanitizeMetadata(metadata),
  }
}

export function setObservabilityTransport(transport) {
  customTransport = typeof transport === 'function' ? transport : null
}

export function captureEvent(name, metadata = {}, level = 'info') {
  const event = buildEvent(name, metadata, level)
  if (typeof window !== 'undefined') {
    window.dispatchEvent(new CustomEvent('vshield:observability', { detail: event }))
  }
  if (customTransport) {
    Promise.resolve(customTransport(event)).catch(() => {})
    return event
  }
  if (!endpoint) return event
  queue.push(event)
  if (queue.length > MAX_QUEUE_SIZE) queue.splice(0, queue.length - MAX_QUEUE_SIZE)
  if (queue.length >= 10) flushObservability()
  return event
}

export function captureError(error, category = 'javascript_error', metadata = {}) {
  return captureEvent(category, {
    ...metadata,
    error: error instanceof Error ? { name: error.name, message: error.message } : { message: String(error || 'Unknown error') },
  }, 'error')
}

function normalizedRequestPath(config = {}) {
  try {
    const base = config.baseURL || window.location.origin
    return redactLocation(new URL(config.url || '', base).pathname)
  } catch {
    return String(config.url || '').split('?')[0]
  }
}

export function captureApiFailure(error) {
  const status = Number(error?.response?.status) || 0
  const path = normalizedRequestPath(error?.config)
  const lowerPath = path.toLowerCase()
  const category = lowerPath.includes('/auth/')
    ? 'authentication_failure'
    : /\/(import|export)(?:\/|$)/.test(lowerPath) || lowerPath.includes('/import-export/')
      ? 'import_export_failure'
      : status === 403
        ? 'permission_denied'
        : 'api_failure'
  return captureEvent(category, {
    method: String(error?.config?.method || 'GET').toUpperCase(),
    path,
    httpStatus: status || undefined,
    correlationId: error?.response?.headers?.['x-correlation-id'] || error?.response?.headers?.['trace-id'] || undefined,
    errorCategory: error?.code || (status ? `http_${status}` : 'network_unavailable'),
    timeoutMs: error?.config?.timeout || undefined,
  }, status >= 500 || !status ? 'error' : 'warning')
}

function percentile(values, ratio) {
  if (!values.length) return 0
  const sorted = [...values].sort((a, b) => a - b)
  return sorted[Math.min(sorted.length - 1, Math.ceil(sorted.length * ratio) - 1)]
}

export function getMetricSummary(name) {
  const values = metricSamples.get(name) || []
  return {
    name,
    count: values.length,
    p50: percentile(values, 0.5),
    p75: percentile(values, 0.75),
    p95: percentile(values, 0.95),
  }
}

export function recordMetric(name, value, metadata = {}) {
  const numericValue = Number(value)
  if (!Number.isFinite(numericValue) || numericValue < 0) return null
  const samples = metricSamples.get(name) || []
  samples.push(numericValue)
  if (samples.length > MAX_METRIC_SAMPLES) samples.shift()
  metricSamples.set(name, samples)
  const thresholds = metricThresholds[name]
  const level = thresholds && numericValue >= thresholds.critical ? 'error' : thresholds && numericValue >= thresholds.warning ? 'warning' : 'info'
  return captureEvent('performance_metric', { metric: name, value: numericValue, ...metadata }, level)
}

export async function measureOperation(name, operation, metadata = {}) {
  const startedAt = performance.now()
  try {
    return await operation()
  } catch (error) {
    captureError(error, `${name}_failure`, metadata)
    throw error
  } finally {
    recordMetric(name, performance.now() - startedAt, metadata)
  }
}

export async function flushObservability({ useBeacon = false } = {}) {
  if (!endpoint || !queue.length) return
  const events = queue.splice(0, queue.length)
  const body = JSON.stringify({ events })
  try {
    if (useBeacon && navigator.sendBeacon) {
      if (!navigator.sendBeacon(endpoint, new Blob([body], { type: 'application/json' }))) queue.unshift(...events)
      return
    }
    const response = await fetch(endpoint, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body, keepalive: true, credentials: 'omit' })
    if (!response.ok) queue.unshift(...events)
  } catch {
    queue.unshift(...events)
  }
}

function observeWebVitals() {
  if (typeof PerformanceObserver === 'undefined') return
  let cls = 0
  let lastLcp = null
  try {
    const observer = new PerformanceObserver((list) => { lastLcp = list.getEntries().at(-1) || lastLcp })
    observer.observe({ type: 'largest-contentful-paint', buffered: true })
  } catch {}
  try {
    const observer = new PerformanceObserver((list) => {
      for (const entry of list.getEntries()) if (!entry.hadRecentInput) cls += entry.value
    })
    observer.observe({ type: 'layout-shift', buffered: true })
  } catch {}
  try {
    let maxInp = 0
    const observer = new PerformanceObserver((list) => {
      for (const entry of list.getEntries()) maxInp = Math.max(maxInp, entry.duration || 0)
      if (maxInp) recordMetric('inp', maxInp)
    })
    observer.observe({ type: 'event', buffered: true, durationThreshold: 40 })
  } catch {}
  try {
    const observer = new PerformanceObserver((list) => {
      for (const entry of list.getEntries()) {
        if (entry.initiatorType === 'script' && /\/assets\/.*\.js(?:\?|$)/.test(entry.name)) {
          recordMetric('dynamic_import', entry.duration, { asset: new URL(entry.name).pathname.split('/').at(-1) })
        }
      }
    })
    observer.observe({ type: 'resource', buffered: true })
  } catch {}
  const finalize = () => {
    if (lastLcp) recordMetric('lcp', lastLcp.startTime)
    recordMetric('cls', cls)
    const navigation = performance.getEntriesByType('navigation')[0]
    if (navigation) {
      recordMetric('ttfb', Math.max(0, navigation.responseStart - navigation.requestStart))
      recordMetric('page_load', navigation.loadEventEnd || performance.now())
    }
  }
  window.addEventListener('pagehide', finalize, { once: true })
  document.addEventListener('visibilitychange', () => { if (document.visibilityState === 'hidden') finalize() }, { once: true })
}

export function installObservability(app, router) {
  if (installed || typeof window === 'undefined') return
  installed = true
  const previousHandler = app.config.errorHandler
  app.config.errorHandler = (error, instance, info) => {
    captureError(error, 'vue_component_error', { component: instance?.$options?.name, info })
    previousHandler?.(error, instance, info)
  }
  window.addEventListener('error', (event) => captureError(event.error || event.message, 'unhandled_javascript_error', { source: event.filename, line: event.lineno }))
  window.addEventListener('unhandledrejection', (event) => captureError(event.reason, 'unhandled_promise_rejection'))
  window.addEventListener('pagehide', () => flushObservability({ useBeacon: true }))

  router.beforeEach((to, from) => {
    routeStartedAt = performance.now()
    routeFrom = from.fullPath
  })
  router.afterEach((to) => {
    recordMetric('route_transition', performance.now() - routeStartedAt, { fromRoute: routeFrom, toRoute: to.fullPath.split('?')[0] })
  })
  router.onError((error, to) => {
    const dynamicImport = String(error?.message || '').includes('dynamically imported module')
    captureError(error, dynamicImport ? 'route_chunk_load_failure' : 'route_navigation_failure', { route: to?.path })
  })

  observeWebVitals()
  flushTimer = window.setInterval(() => {
    for (const name of metricSamples.keys()) captureEvent('performance_summary', getMetricSummary(name))
    flushObservability()
  }, 60000)
}

export function resetObservabilityForTests() {
  queue.length = 0
  metricSamples.clear()
  customTransport = null
  if (flushTimer) clearInterval(flushTimer)
  flushTimer = null
}
