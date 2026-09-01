import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/authApi', () => ({
  login: vi.fn(),
  getMe: vi.fn(),
  logoutApi: vi.fn(),
  changePassword: vi.fn(),
}))

vi.mock('../../services/observability', () => ({
  captureEvent: vi.fn(),
}))

const authApi = await import('../../services/authApi')
const observability = await import('../../services/observability')
const { login, logout, changePassword, fetchUser, isLoggedIn, hasRole, authState } = await import('../../stores/auth')

beforeEach(() => {
  localStorage.clear()
  sessionStorage.clear()
  vi.clearAllMocks()
  authState.token = null
  authState.refreshToken = null
  authState.user = null
})

const loginResponse = {
  data: {
    requiresMfa: false,
    token: 'access-token',
    refreshToken: 'refresh-token',
    userId: 1,
    username: 'admin',
    fullName: 'Admin',
    role: 'Admin',
    employeeId: 10,
    mfaEnabled: true,
    mfaRequired: false,
    requiresPasswordChange: false,
    hasOperationalScopeAssignments: false,
    operationalTaskKeys: [],
  },
}

describe('auth store', () => {
  it('logs in and persists token/user to sessionStorage', async () => {
    authApi.login.mockResolvedValue(loginResponse)
    const result = await login('admin', 'secret')
    expect(result).toEqual({ success: true, requiresPasswordChange: false })
    expect(authState.token).toBe('access-token')
    expect(authState.user.role).toBe('Admin')
    expect(sessionStorage.getItem('v_shield_token')).toBe('access-token')
    expect(sessionStorage.getItem('v_shield_refresh_token')).toBe('refresh-token')
    expect(observability.captureEvent).toHaveBeenCalledWith('authentication_success', expect.anything())
  })

  it('returns MFA requirement without persisting auth', async () => {
    authApi.login.mockResolvedValue({
      data: { requiresMfa: true, mfaSetupSecret: 'S3', mfaSetupUri: 'otpauth://', message: 'enter code' },
    })
    const result = await login('admin', 'secret', '123456')
    expect(result.requiresMfa).toBe(true)
    expect(result.mfaSetupSecret).toBe('S3')
    expect(authState.token).toBeNull()
    expect(sessionStorage.getItem('v_shield_token')).toBeNull()
  })

  it('logs out, clears local state and notifies', async () => {
    authApi.login.mockResolvedValue(loginResponse)
    await login('admin', 'secret')
    authApi.logoutApi.mockResolvedValue({})
    await logout()
    expect(authState.token).toBeNull()
    expect(authState.user).toBeNull()
    expect(sessionStorage.getItem('v_shield_token')).toBeNull()
    expect(observability.captureEvent).toHaveBeenCalledWith('authentication_logout')
  })

  it('logout tolerates API failures', async () => {
    authApi.login.mockResolvedValue(loginResponse)
    await login('admin', 'secret')
    authApi.logoutApi.mockRejectedValue(new Error('network'))
    await expect(logout()).resolves.toBeUndefined()
    expect(authState.token).toBeNull()
  })

  it('exposes login state and role helpers', async () => {
    authApi.login.mockResolvedValue(loginResponse)
    expect(isLoggedIn()).toBe(false)
    await login('admin', 'secret')
    expect(isLoggedIn()).toBe(true)
    expect(hasRole('Admin')).toBe(true)
    expect(hasRole('BaoVe')).toBe(false)
  })

  it('changes password and clears the change-required flag', async () => {
    authApi.login.mockResolvedValue({
      data: { ...loginResponse.data, requiresPasswordChange: true },
    })
    await login('admin', 'secret')
    expect(authState.user.requiresPasswordChange).toBe(true)
    authApi.changePassword.mockResolvedValue({})
    await changePassword('old', 'new')
    expect(authState.user.requiresPasswordChange).toBe(false)
    expect(observability.captureEvent).toHaveBeenCalledWith('password_changed')
  })

  it('fetchUser refreshes the current user from the API', async () => {
    authApi.login.mockResolvedValue(loginResponse)
    await login('admin', 'secret')
    authApi.getMe.mockResolvedValue({ data: { ...loginResponse.data, fullName: 'Updated' } })
    const ok = await fetchUser()
    expect(ok).toBe(true)
    expect(authState.user.fullName).toBe('Updated')
    expect(observability.captureEvent).toHaveBeenCalledWith('authentication_session_verified', expect.anything())
  })

  it('fetchUser logs out when the session cannot be verified', async () => {
    authApi.login.mockResolvedValue(loginResponse)
    await login('admin', 'secret')
    authApi.getMe.mockRejectedValue(new Error('401'))
    const ok = await fetchUser()
    expect(ok).toBe(false)
    expect(authState.token).toBeNull()
  })
})

describe('auth store storage error handling', () => {
  const readThrowing = { getItem: () => { throw new Error('read blocked') }, setItem: () => {}, removeItem: () => {} }
  const writeThrowing = { getItem: () => null, setItem: () => { throw new Error('write blocked') }, removeItem: () => {} }
  const clearThrowing = { getItem: () => null, setItem: () => {}, removeItem: () => { throw new Error('remove blocked') } }

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.resetModules()
  })

  it('falls back to null state when storage reads throw', async () => {
    vi.resetModules()
    vi.stubGlobal('sessionStorage', readThrowing)
    vi.stubGlobal('localStorage', readThrowing)
    const mod = await import('../../stores/auth')
    expect(mod.authState.token).toBeNull()
    expect(mod.authState.refreshToken).toBeNull()
    expect(mod.authState.user).toBeNull()
  })

  it('warns but still logs in when persisting auth state fails', async () => {
    vi.resetModules()
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {})
    vi.stubGlobal('sessionStorage', writeThrowing)
    vi.stubGlobal('localStorage', writeThrowing)
    const mod = await import('../../stores/auth')
    authApi.login.mockResolvedValue(loginResponse)
    const result = await mod.login('admin', 'secret')
    expect(result.success).toBe(true)
    expect(mod.authState.token).toBe('access-token')
    expect(warn).toHaveBeenCalled()
    warn.mockRestore()
  })

  it('warns but still logs out when clearing auth state fails', async () => {
    vi.resetModules()
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {})
    vi.stubGlobal('sessionStorage', clearThrowing)
    vi.stubGlobal('localStorage', clearThrowing)
    const mod = await import('../../stores/auth')
    authApi.logoutApi.mockResolvedValue({})
    mod.authState.token = 'access-token'
    await mod.logout()
    expect(mod.authState.token).toBeNull()
    expect(warn).toHaveBeenCalled()
    warn.mockRestore()
  })
})
