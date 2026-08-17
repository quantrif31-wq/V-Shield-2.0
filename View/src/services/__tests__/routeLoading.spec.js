import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

async function freshModule() {
  const mod = await import('../routeLoading')
  return mod
}

let mod

beforeEach(async () => {
  vi.useFakeTimers()
  mod = await freshModule()
})

afterEach(() => {
  vi.clearAllTimers()
  vi.useRealTimers()
})

function createFakeRouter() {
  const hooks = {}
  return {
    beforeEach: (cb) => { hooks.beforeEach = cb },
    afterEach: (cb) => { hooks.afterEach = cb },
    onError: (cb) => { hooks.onError = cb },
    hooks,
  }
}

describe('routeLoading', () => {
  it('tracks progress through a navigation lifecycle', () => {
    const router = createFakeRouter()
    mod.installRouteProgress(router)

    expect(mod.routeProgress.active).toBe(false)
    router.hooks.beforeEach()
    expect(mod.routeProgress.active).toBe(true)
    expect(mod.routeProgress.progress).toBe(6)

    vi.advanceTimersByTime(180)
    expect(mod.routeProgress.progress).toBeGreaterThan(6)
    expect(mod.routeProgress.progress).toBeLessThanOrEqual(92)

    router.hooks.afterEach()
    expect(mod.routeProgress.progress).toBe(100)

    vi.advanceTimersByTime(420)
    expect(mod.routeProgress.active).toBe(false)
    expect(mod.routeProgress.progress).toBe(0)
  })

  it('finishes (and resets) on router error', () => {
    const router = createFakeRouter()
    mod.installRouteProgress(router)
    router.hooks.beforeEach()
    router.hooks.onError()
    expect(mod.routeProgress.progress).toBe(100)
    vi.advanceTimersByTime(420)
    expect(mod.routeProgress.active).toBe(false)
  })

  it('restarting navigation resets progress back to the start', () => {
    const router = createFakeRouter()
    mod.installRouteProgress(router)
    router.hooks.beforeEach()
    vi.advanceTimersByTime(90)
    const midProgress = mod.routeProgress.progress
    expect(midProgress).toBeGreaterThan(6)
    router.hooks.beforeEach()
    expect(mod.routeProgress.progress).toBe(6)
  })
})
