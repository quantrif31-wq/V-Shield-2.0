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
const authApi = await import('../authApi')

beforeEach(() => vi.clearAllMocks())

describe('authApi', () => {
  it('login posts credentials and optional mfa code', () => {
    authApi.login('admin', 'pw', '123456')
    expect(http.post).toHaveBeenCalledWith('/Auth/login', { username: 'admin', password: 'pw', mfaCode: '123456' })
    authApi.login('admin', 'pw')
    expect(http.post).toHaveBeenCalledWith('/Auth/login', { username: 'admin', password: 'pw', mfaCode: null })
  })

  it('getMe fetches the current user', () => {
    authApi.getMe()
    expect(http.get).toHaveBeenCalledWith('/Auth/me')
  })

  it('refreshSession posts the refresh token', () => {
    authApi.refreshSession('rt')
    expect(http.post).toHaveBeenCalledWith('/Auth/refresh', { refreshToken: 'rt' })
  })

  it('logoutApi posts a refresh token', () => {
    authApi.logoutApi('rt')
    expect(http.post).toHaveBeenCalledWith('/Auth/logout', { refreshToken: 'rt' })
  })

  it('changePassword posts the new credentials', () => {
    authApi.changePassword('old', 'new')
    expect(http.post).toHaveBeenCalledWith('/Auth/change-password', { currentPassword: 'old', newPassword: 'new' })
  })
})
