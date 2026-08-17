import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => {
  const guards = {}
  const fakeRouter = {
    beforeEach: (fn) => { guards.beforeEach = fn },
    onError: (fn) => { guards.onError = fn },
  }
  return { guards, fakeRouter }
})

vi.mock('vue-router', () => ({
  createRouter: () => hoisted.fakeRouter,
  createWebHistory: () => ({}),
}))

vi.mock('../../stores/auth', () => ({
  isLoggedIn: vi.fn(),
  hasRole: vi.fn(),
  authState: { user: null },
}))

const { isLoggedIn, hasRole, authState } = await import('../../stores/auth')
await import('../index')

let beforeEachGuard
let onErrorGuard

beforeEach(() => {
  vi.clearAllMocks()
  sessionStorage.clear()
  authState.user = null
  beforeEachGuard = hoisted.guards.beforeEach
  onErrorGuard = hoisted.guards.onError
  window.history.pushState({}, '', '/')
})

afterEach(() => {
  vi.clearAllTimers()
  vi.useRealTimers()
})

function runGuard(to, from = {}) {
  const next = vi.fn()
  beforeEachGuard({ meta: {}, matched: [], name: undefined, fullPath: '/', ...to }, from, next)
  return next
}

describe('router authentication guard', () => {
  it('redirects to login when an authenticated route is visited logged out', () => {
    isLoggedIn.mockReturnValue(false)
    const next = runGuard({ matched: [{ meta: { requiresAuth: true } }], fullPath: '/employees' })
    expect(next).toHaveBeenCalledWith({ name: 'Login', query: { redirect: '/employees' } })
  })

  it('forces password change for users that must change it', () => {
    isLoggedIn.mockReturnValue(true)
    authState.user = { role: 'Admin', requiresPasswordChange: true }
    let next = runGuard({ name: 'Employees', matched: [] })
    expect(next).toHaveBeenCalledWith({ name: 'ForcePasswordChange', query: { redirect: expect.anything() } })
    next = runGuard({ name: 'Login', matched: [] })
    expect(next).toHaveBeenCalledWith({ name: 'ForcePasswordChange' })
  })

  it('redirects non-admins away from admin-only routes', () => {
    isLoggedIn.mockReturnValue(true)
    authState.user = { role: 'BaoVe' }
    const next = runGuard({ matched: [{ meta: { requiresAdmin: true } }] })
    expect(next).toHaveBeenCalledWith({ name: 'Dashboard' })
  })

  it('blocks roles that are neither in allowedRoles nor granted the task', () => {
    isLoggedIn.mockReturnValue(true)
    authState.user = { role: 'BaoVe', operationalTaskKeys: [] }
    const next = runGuard({ matched: [{ meta: { allowedRoles: ['Admin'], taskKey: 'reports' } }] })
    expect(next).toHaveBeenCalledWith({ name: 'Dashboard' })
  })

  it('admits a role granted through operationalTaskKeys', () => {
    isLoggedIn.mockReturnValue(true)
    authState.user = { role: 'BaoVe', operationalTaskKeys: ['reports'] }
    const next = runGuard({ matched: [{ meta: { allowedRoles: ['Admin'], taskKey: 'reports' } }] })
    expect(next).toHaveBeenCalledWith()
  })

  it('admits a directly allowed role', () => {
    isLoggedIn.mockReturnValue(true)
    authState.user = { role: 'Admin' }
    const next = runGuard({ matched: [{ meta: { allowedRoles: ['Admin'], taskKey: 'reports' } }] })
    expect(next).toHaveBeenCalledWith()
  })

  it('enforces task keys even without allowedRoles', () => {
    isLoggedIn.mockReturnValue(true)
    authState.user = { role: 'QuanLy', operationalTaskKeys: [] }
    const next = runGuard({ matched: [{ meta: { taskKey: 'reports' } }] })
    expect(next).toHaveBeenCalledWith({ name: 'Dashboard' })
  })

  it('keeps logged-in users out of guest pages except registration', () => {
    isLoggedIn.mockReturnValue(true)
    authState.user = { role: 'BaoVe' }
    let next = runGuard({ name: 'Login', meta: { guest: true }, matched: [] })
    expect(next).toHaveBeenCalledWith({ name: 'Dashboard' })
    next = runGuard({ name: 'GuestRegister', meta: { guest: true }, matched: [] })
    expect(next).toHaveBeenCalledWith()
  })

  it('lets navigation through without restrictions', () => {
    isLoggedIn.mockReturnValue(true)
    authState.user = { role: 'Admin' }
    const next = runGuard({ name: 'Dashboard', matched: [] })
    expect(next).toHaveBeenCalledWith()
  })
})

describe('router dynamic-import error handler', () => {
  const MARKER = 'Failed to fetch dynamically imported module'

  it('ignores errors unrelated to dynamic imports', () => {
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    vi.useFakeTimers()
    onErrorGuard(new Error('boom'), { fullPath: '/x' })
    expect(sessionStorage.getItem('vshield:dynamic-import-reload')).toBeNull()
    spy.mockRestore()
  })

  it('avoids reload loops for the same target', () => {
    vi.useFakeTimers()
    sessionStorage.setItem('vshield:dynamic-import-reload', '/employees')
    onErrorGuard(new Error(MARKER), { fullPath: '/employees' })
    expect(sessionStorage.getItem('vshield:dynamic-import-reload')).toBeNull()
  })

  it('reloads once for a new target and clears the guard key', () => {
    vi.useFakeTimers()
    const assign = vi.fn()
    const originalLocation = window.location
    Object.defineProperty(window, 'location', {
      value: { ...originalLocation, pathname: '/dashboard', assign },
      writable: true,
      configurable: true,
    })
    onErrorGuard(new Error(MARKER), { fullPath: '/employees' })
    expect(sessionStorage.getItem('vshield:dynamic-import-reload')).toBe('/employees')
    expect(assign).toHaveBeenCalledWith('/employees')
    vi.advanceTimersByTime(3000)
    expect(sessionStorage.getItem('vshield:dynamic-import-reload')).toBeNull()
  })

  it('falls back to the current path when the destination is unknown', () => {
    vi.useFakeTimers()
    const assign = vi.fn()
    const originalLocation = window.location
    Object.defineProperty(window, 'location', {
      value: { ...originalLocation, pathname: '/dashboard', assign },
      writable: true,
      configurable: true,
    })
    onErrorGuard(new Error(MARKER), {})
    expect(assign).toHaveBeenCalledWith('/dashboard')
  })
})
