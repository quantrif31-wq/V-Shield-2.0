import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h } from 'vue'
import { mount } from '@vue/test-utils'
import { useRealtimeSync } from '../useRealtimeSync'
import { onEntityChanged } from '../../services/notificationApi'

vi.mock('../../services/notificationApi', () => ({
  onEntityChanged: vi.fn(() => vi.fn()),
}))

function setup({ entityTypes = 'Employee', refreshCallback, options } = {}) {
  let hookRef
  const callback = refreshCallback || vi.fn().mockResolvedValue(undefined)
  const Comp = defineComponent({
    setup() {
      hookRef = useRealtimeSync(entityTypes, callback, options)
      return () => h('div')
    },
  })
  const wrapper = mount(Comp)
  const call = onEntityChanged.mock.calls[onEntityChanged.mock.calls.length - 1]
  const handler = call[1]
  const result = onEntityChanged.mock.results[onEntityChanged.mock.results.length - 1]
  const unsubscribe = result && result.value
  return { wrapper, hook: hookRef, handler, unsubscribe, callback }
}

beforeEach(() => {
  vi.useFakeTimers()
  onEntityChanged.mockClear()
})

afterEach(() => {
  vi.useRealTimers()
})

describe('useRealtimeSync', () => {
  it('subscribes on mount and triggers the callback after the debounce window', async () => {
    const { handler, callback, hook } = setup({ options: { debounceMs: 250 } })
    expect(onEntityChanged).toHaveBeenCalledWith('Employee', expect.any(Function))
    const event = { aggregateType: 'Employee', occurredAtUtc: '2024-01-01T00:00:00Z' }
    handler(event)
    expect(callback).not.toHaveBeenCalled()
    await vi.advanceTimersByTimeAsync(250)
    expect(callback).toHaveBeenCalledWith(event)
    expect(hook.lastSyncAt.value).toBe('2024-01-01T00:00:00Z')
  })

  it('uses the current timestamp when occurredAtUtc is missing', async () => {
    const { handler, callback, hook } = setup({ options: { debounceMs: 250 } })
    handler({ aggregateType: 'Employee' })
    await vi.advanceTimersByTimeAsync(250)
    expect(callback).toHaveBeenCalledTimes(1)
    expect(Number.isNaN(Date.parse(hook.lastSyncAt.value))).toBe(false)
  })

  it('fires immediately (no debounce) when debounceMs <= 0', async () => {
    const { handler, callback, hook } = setup({ options: { debounceMs: 0 } })
    const event = { aggregateType: 'Employee', occurredAtUtc: '2024-02-02T00:00:00Z' }
    handler(event)
    await vi.advanceTimersByTimeAsync(0)
    expect(callback).toHaveBeenCalledWith(event)
    expect(hook.isUpdating.value).toBe(false)
  })

  it('calls refresh immediately on mount when immediate is true', async () => {
    const { callback, hook } = setup({ options: { debounceMs: 250, immediate: true } })
    await vi.advanceTimersByTimeAsync(250)
    expect(callback).toHaveBeenCalledWith(
      expect.objectContaining({ aggregateType: 'Initial', action: 'Mount' })
    )
    expect(hook.lastSyncAt.value).toBeTruthy()
  })

  it('debounces rapid events into a single refresh', async () => {
    const { handler, callback } = setup({ options: { debounceMs: 100 } })
    handler({ aggregateType: 'Employee' })
    await vi.advanceTimersByTimeAsync(50)
    handler({ aggregateType: 'Employee' })
    await vi.advanceTimersByTimeAsync(100)
    expect(callback).toHaveBeenCalledTimes(1)
  })

  it('refreshNow triggers a manual refresh', async () => {
    const { callback, hook } = setup({ options: { debounceMs: 100 } })
    hook.refreshNow()
    await vi.advanceTimersByTimeAsync(100)
    expect(callback).toHaveBeenCalledWith(
      expect.objectContaining({ aggregateType: 'Manual', action: 'Trigger' })
    )
  })

  it('clears a pending timer and unsubscribes on unmount', async () => {
    const { wrapper, handler, callback, unsubscribe } = setup({ options: { debounceMs: 250 } })
    handler({ aggregateType: 'Employee' })
    expect(callback).not.toHaveBeenCalled()
    wrapper.unmount()
    await vi.advanceTimersByTimeAsync(250)
    expect(callback).not.toHaveBeenCalled()
    expect(unsubscribe).toHaveBeenCalled()
  })
})
