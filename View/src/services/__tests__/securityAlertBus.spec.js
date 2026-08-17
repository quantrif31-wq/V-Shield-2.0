import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../enterpriseSecurityApi', () => ({
  enterpriseApi: { getActiveSecurityAlerts: vi.fn() },
}))

const { enterpriseApi } = await import('../enterpriseSecurityApi')
const { refreshSecurityAlerts, securityAlertState, startSecurityAlertPolling, stopSecurityAlertPolling } = await import('../securityAlertBus')

beforeEach(() => {
  vi.useFakeTimers()
  vi.clearAllMocks()
  securityAlertState.items = []
  securityAlertState.criticalCount = 0
  securityAlertState.loading = false
  securityAlertState.lastUpdatedAt = null
  securityAlertState.error = ''
  stopSecurityAlertPolling()
})

afterEach(() => {
  stopSecurityAlertPolling()
  vi.useRealTimers()
})

describe('securityAlertBus', () => {
  it('refreshes alerts from the API', async () => {
    enterpriseApi.getActiveSecurityAlerts.mockResolvedValue({
      data: { items: [{ id: 1 }], criticalCount: 2, generatedAtUtc: '2026-01-01T00:00:00Z' },
    })
    await refreshSecurityAlerts()
    expect(securityAlertState.items).toEqual([{ id: 1 }])
    expect(securityAlertState.criticalCount).toBe(2)
    expect(securityAlertState.lastUpdatedAt).toBe('2026-01-01T00:00:00Z')
    expect(securityAlertState.loading).toBe(false)
    expect(securityAlertState.error).toBe('')
  })

  it('records an error message when the API fails', async () => {
    enterpriseApi.getActiveSecurityAlerts.mockRejectedValue({
      response: { data: { message: 'down' } },
    })
    await refreshSecurityAlerts()
    expect(securityAlertState.error).toBe('down')
    expect(securityAlertState.loading).toBe(false)
  })

  it('falls back to a generic message when no server message is present', async () => {
    enterpriseApi.getActiveSecurityAlerts.mockRejectedValue(new Error('x'))
    await refreshSecurityAlerts()
    expect(securityAlertState.error).toBe('Không thể cập nhật cảnh báo an ninh.')
  })

  it('polls on an interval and stops on demand', () => {
    startSecurityAlertPolling(5000)
    expect(enterpriseApi.getActiveSecurityAlerts).toHaveBeenCalledTimes(1)
    vi.advanceTimersByTime(15_000)
    expect(enterpriseApi.getActiveSecurityAlerts).toHaveBeenCalledTimes(4)
    stopSecurityAlertPolling()
    vi.advanceTimersByTime(15_000)
    expect(enterpriseApi.getActiveSecurityAlerts).toHaveBeenCalledTimes(4)
  })
})
