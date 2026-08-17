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
const { enterpriseApi, lostFoundApi, socIntelApi, zoneAuthorityApi } = await import('../enterpriseSecurityApi')
const { enterpriseAiApi } = await import('../enterpriseAiApi')
const { identityApi } = await import('../identityApi')
const { socApi } = await import('../socApi')

beforeEach(() => vi.clearAllMocks())

function sweep(api, skip = []) {
  for (const name of Object.keys(api)) {
    if (skip.includes(name)) continue
    expect(() => api[name]({ a: 1 }, { b: 2 })).not.toThrow()
  }
}

describe('enterpriseApi.overview', () => {
  it('resolves all overview endpoints and normalizes rejections', async () => {
    http.get.mockResolvedValue({ data: { ok: true } })
    http.get.mockRejectedValueOnce({})
    const results = await enterpriseApi.overview()
    expect(results).toHaveLength(9)
    expect(results[0]).toEqual({ data: {} })
    expect(results[1]).toEqual({ data: { ok: true } })
  })
})

describe('enterpriseApi.setStepUpSession', () => {
  it('sets and clears the step-up session header', () => {
    enterpriseApi.setStepUpSession('sess-1')
    expect(http.defaults.headers.common['X-Step-Up-Session-Id']).toBe('sess-1')
    enterpriseApi.setStepUpSession(null)
    expect(http.defaults.headers.common['X-Step-Up-Session-Id']).toBeUndefined()
  })
})

describe('enterpriseApi endpoint coverage', () => {
  it('calls http for every endpoint without throwing', () => {
    sweep(enterpriseApi, ['overview', 'setStepUpSession'])
    expect(http.get.mock.calls.length + http.post.mock.calls.length + http.patch.mock.calls.length + http.delete.mock.calls.length)
      .toBeGreaterThan(80)
  })

  it('passes a duress header when creating an emergency pass under duress', () => {
    enterpriseApi.createEmergencyPass({ pass: 1 }, true)
    expect(http.post).toHaveBeenCalledWith('/enterprise/access-policy/emergency-passes', { pass: 1 }, { headers: { 'X-Duress-Signal': '1' } })
    enterpriseApi.createEmergencyPass({ pass: 1 }, false)
    expect(http.post).toHaveBeenCalledWith('/enterprise/access-policy/emergency-passes', { pass: 1 }, undefined)
  })
})

describe('zoneAuthorityApi', () => {
  it('builds query strings from provided filters', () => {
    zoneAuthorityApi.getAuthorities({ userId: 7, securityZoneId: 3 })
    expect(http.get).toHaveBeenCalledWith('/enterprise/access-policy/zone-authorities?userId=7&securityZoneId=3')
    zoneAuthorityApi.getAuthorities({})
    expect(http.get).toHaveBeenCalledWith('/enterprise/access-policy/zone-authorities?')
  })

  it('covers the remaining authority helpers', () => {
    zoneAuthorityApi.createAuthority({ x: 1 })
    expect(http.post).toHaveBeenCalledWith('/enterprise/access-policy/zone-authorities', { x: 1 })
    zoneAuthorityApi.revokeAuthority(2)
    expect(http.delete).toHaveBeenCalledWith('/enterprise/access-policy/zone-authorities/2')
    zoneAuthorityApi.getMyZones()
    expect(http.get).toHaveBeenCalledWith('/enterprise/access-policy/zone-authorities/my-zones')
    zoneAuthorityApi.checkCanOverride(4)
    expect(http.get).toHaveBeenCalledWith('/enterprise/access-policy/zone-authorities/can-override?securityZoneId=4')
  })
})

describe('lostFoundApi and socIntelApi', () => {
  it('covers lost-found and SOC intelligence endpoints', () => {
    sweep(lostFoundApi)
    sweep(socIntelApi)
    expect(http.get.mock.calls.length + http.post.mock.calls.length + http.put.mock.calls.length + http.patch.mock.calls.length + http.delete.mock.calls.length)
      .toBeGreaterThan(20)
  })
})

describe('enterpriseAiApi', () => {
  it('calls http for every AI endpoint without throwing', () => {
    sweep(enterpriseAiApi)
    expect(http.post.mock.calls.length + http.get.mock.calls.length + http.patch.mock.calls.length).toBeGreaterThan(15)
  })
})

describe('identityApi', () => {
  it('calls http for every identity endpoint without throwing', () => {
    sweep(identityApi)
    expect(http.get.mock.calls.length + http.post.mock.calls.length + http.patch.mock.calls.length).toBeGreaterThan(8)
  })
})

describe('socApi', () => {
  it('calls http for every SOC endpoint without throwing', () => {
    sweep(socApi)
    expect(http.get.mock.calls.length + http.post.mock.calls.length + http.patch.mock.calls.length).toBeGreaterThan(20)
  })
})
