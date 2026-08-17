import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

let useToasts

beforeEach(async () => {
  vi.useFakeTimers()
  vi.resetModules()
  const mod = await import('../useToasts')
  useToasts = mod.useToasts
})

afterEach(() => vi.useRealTimers())

describe('useToasts', () => {
  it('pushes a toast with a stable incremental id', () => {
    const { push, toasts } = useToasts()
    const first = push({ title: 'A' })
    const second = push({ title: 'B' })
    expect(first).toBe(1)
    expect(second).toBe(2)
    expect(toasts.value).toHaveLength(2)
    expect(toasts.value[0]).toMatchObject({ id: 1, title: 'A', type: 'info' })
  })

  it('removes a toast manually', () => {
    const { push, remove, toasts } = useToasts()
    const id = push({ title: 'A' })
    remove(id)
    expect(toasts.value).toHaveLength(0)
  })

  it('auto-removes after the configured duration', () => {
    const { push, toasts } = useToasts()
    push({ title: 'A', duration: 3000 })
    expect(toasts.value).toHaveLength(1)
    vi.advanceTimersByTime(2999)
    expect(toasts.value).toHaveLength(1)
    vi.advanceTimersByTime(1)
    expect(toasts.value).toHaveLength(0)
  })

  it('never auto-removes when duration is zero', () => {
    const { push, toasts } = useToasts()
    push({ title: 'A', duration: 0 })
    vi.advanceTimersByTime(10_000)
    expect(toasts.value).toHaveLength(1)
  })

  it('exposes success and error convenience helpers', () => {
    const { success, error, toasts } = useToasts()
    success('OK', 'done')
    error('Fail', 'boom')
    expect(toasts.value[0]).toMatchObject({ type: 'success', message: 'done' })
    expect(toasts.value[1]).toMatchObject({ type: 'error', message: 'boom' })
  })
})
