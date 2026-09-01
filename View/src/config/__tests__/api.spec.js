import { afterEach, describe, expect, it, vi } from 'vitest'

function clearEnv() {
  delete import.meta.env.VITE_API_BASE_URL
  delete import.meta.env.VITE_PLATE_API_BASE_URL
}

async function loadApi() {
  vi.resetModules()
  return await import('../api')
}

afterEach(() => {
  clearEnv()
  vi.unstubAllGlobals()
})

describe('config/api', () => {
  it('derives API urls from the fallback ports', async () => {
    clearEnv()
    const { API_BASE_URL, API_ORIGIN, PLATE_API_BASE_URL, PLATE_API_ORIGIN } = await loadApi()
    expect(API_BASE_URL.endsWith(':5107/api')).toBe(true)
    expect(API_ORIGIN.endsWith(':5107')).toBe(true)
    expect(API_ORIGIN.endsWith('/api')).toBe(false)
    expect(PLATE_API_BASE_URL.endsWith(':5002/api')).toBe(true)
    expect(PLATE_API_ORIGIN.endsWith(':5002')).toBe(true)
    expect(PLATE_API_ORIGIN.endsWith('/api')).toBe(false)
  })

  it('appends /api to an explicit base url that lacks the suffix', async () => {
    import.meta.env.VITE_API_BASE_URL = 'https://example.com'
    const { API_BASE_URL, API_ORIGIN } = await loadApi()
    expect(API_BASE_URL).toBe('https://example.com/api')
    expect(API_ORIGIN).toBe('https://example.com')
  })

  it('keeps an explicit /api suffix and strips it for the origin', async () => {
    import.meta.env.VITE_API_BASE_URL = 'https://example.com/api'
    const { API_BASE_URL, API_ORIGIN } = await loadApi()
    expect(API_BASE_URL).toBe('https://example.com/api')
    expect(API_ORIGIN).toBe('https://example.com')
  })

  it('uses the fallback port when no window exists (SSR)', async () => {
    clearEnv()
    vi.stubGlobal('window', undefined)
    const { API_BASE_URL } = await loadApi()
    expect(API_BASE_URL).toBe('http://localhost:5107/api')
  })

  it('keeps a non-standard port on a production hostname', async () => {
    clearEnv()
    vi.stubGlobal('window', { location: { protocol: 'https:', hostname: 'v-shield.site', port: '8443' } })
    const { API_BASE_URL } = await loadApi()
    expect(API_BASE_URL).toBe('https://v-shield.site:8443/api')
  })

  it('uses an explicit service (plate) base url directly', async () => {
    import.meta.env.VITE_PLATE_API_BASE_URL = 'http://plate.local:5002/api'
    const { PLATE_API_BASE_URL, PLATE_API_ORIGIN } = await loadApi()
    expect(PLATE_API_BASE_URL).toBe('http://plate.local:5002/api')
    expect(PLATE_API_ORIGIN).toBe('http://plate.local:5002')
  })

  it('rewrites remote v-shield.site URLs to /api when window.location is localhost', async () => {
    import.meta.env.VITE_API_BASE_URL = 'https://v-shield.site/api'
    import.meta.env.VITE_PLATE_API_BASE_URL = 'https://v-shield.site/api/PlateCamera'
    vi.stubGlobal('window', { location: { protocol: 'http:', hostname: 'localhost', port: '5173' } })
    const { API_BASE_URL, PLATE_API_BASE_URL } = await loadApi()
    expect(API_BASE_URL).toBe('/api')
    expect(PLATE_API_BASE_URL).toBe('/api/PlateCamera')
  })
})
