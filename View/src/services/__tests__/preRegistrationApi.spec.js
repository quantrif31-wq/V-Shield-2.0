import { beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => {
  const publicApi = { get: vi.fn(), post: vi.fn() }
  return { publicApi }
})

vi.mock('axios', () => ({
  default: { create: vi.fn(() => hoisted.publicApi) },
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

vi.mock('../config/api', () => ({ API_BASE_URL: 'http://localhost:5107/api' }))

const http = (await import('../http')).default
const preRegistrationApi = await import('../preRegistrationApi')

beforeEach(() => vi.clearAllMocks())

describe('preRegistrationApi', () => {
  it('uses the public axios client for token endpoints', () => {
    preRegistrationApi.validateToken('tok')
    expect(hoisted.publicApi.get).toHaveBeenCalledWith('/pre-registrations/validate/tok')
    preRegistrationApi.submitRegistration('tok', { name: 'X' })
    expect(hoisted.publicApi.post).toHaveBeenCalledWith('/pre-registrations/submit/tok', { name: 'X' })
    preRegistrationApi.getVisitorPass('tok')
    expect(hoisted.publicApi.get).toHaveBeenCalledWith('/pre-registrations/visitor-pass/tok')
  })

  it('uses the authenticated http client for management endpoints', () => {
    preRegistrationApi.getAll({ page: 1 })
    expect(http.get).toHaveBeenCalledWith('/pre-registrations', { params: { page: 1 } })
    preRegistrationApi.getDetail(3)
    expect(http.get).toHaveBeenCalledWith('/pre-registrations/3')
    preRegistrationApi.updateStatus(3, 'Approved')
    expect(http.patch).toHaveBeenCalledWith('/pre-registrations/3/status', { status: 'Approved' })
    preRegistrationApi.createLink({ token: 'x' })
    expect(http.post).toHaveBeenCalledWith('/registration-links', { token: 'x' })
    preRegistrationApi.getLinks({ page: 1 })
    expect(http.get).toHaveBeenCalledWith('/registration-links', { params: { page: 1 } })
  })
})
