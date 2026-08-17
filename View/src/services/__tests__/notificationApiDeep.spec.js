import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => {
  const connections = []
  const builder = {
    withUrl: vi.fn(() => builder),
    withAutomaticReconnect: vi.fn(() => builder),
    configureLogging: vi.fn(() => builder),
    build: vi.fn(() => {
      const connection = {
        state: 'Disconnected',
        handlers: new Map(),
        lifecycle: {},
        start: vi.fn(async () => { connection.state = 'Connected' }),
        stop: vi.fn(async () => { connection.state = 'Disconnected' }),
        on: vi.fn((name, cb) => connection.handlers.set(name, cb)),
        onreconnecting: vi.fn((cb) => { connection.lifecycle.reconnecting = cb }),
        onreconnected: vi.fn((cb) => { connection.lifecycle.reconnected = cb }),
        onclose: vi.fn((cb) => { connection.lifecycle.close = cb }),
      }
      connections.push(connection)
      return connection
    }),
  }
  return { connections, builder }
})

vi.mock('@microsoft/signalr', () => ({
  HubConnectionState: { Disconnected: 'Disconnected', Connecting: 'Connecting', Connected: 'Connected', Reconnecting: 'Reconnecting' },
  LogLevel: { Warning: 'Warning' },
  HubConnectionBuilder: vi.fn(function HubConnectionBuilder() { return hoisted.builder }),
}))
vi.mock('../http', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn(), delete: vi.fn(), request: vi.fn() },
}))
vi.mock('../config/api', () => ({ API_ORIGIN: 'http://localhost:5107' }))
vi.mock('./observability', () => ({ captureError: vi.fn(), captureEvent: vi.fn() }))
vi.mock('./realtimeStatus', () => ({
  updateRealtimeStatus: vi.fn(),
  markRealtimeUpdated: vi.fn(),
  getRealtimeStatus: vi.fn(() => ({})),
  onRealtimeStatus: vi.fn(() => () => {}),
}))

let notificationApi
let http

beforeEach(async () => {
  vi.resetModules()
  vi.clearAllMocks()
  hoisted.connections.length = 0
  http = (await import('../http')).default
  notificationApi = await import('../notificationApi')
})

afterEach(() => {})

describe('notificationApi SignalR', () => {
  it('connects and dispatches incoming notifications', async () => {
    const connection = await notificationApi.connectNotificationHub('token')
    expect(connection.state).toBe('Connected')
    const cb = vi.fn()
    notificationApi.onNotification(cb)
    connection.handlers.get('NewNotification')({ id: 1 })
    expect(cb).toHaveBeenCalledWith({ id: 1 })
  })

  it('dispatches unread count updates', async () => {
    const connection = await notificationApi.connectNotificationHub('token')
    const cb = vi.fn()
    notificationApi.onUnreadCountChanged(cb)
    connection.handlers.get('UnreadCountUpdated')(5)
    expect(cb).toHaveBeenCalledWith(5)
  })

  it('disconnects and stops the hub', async () => {
    const connection = await notificationApi.connectNotificationHub('token')
    await notificationApi.disconnectNotificationHub()
    expect(connection.stop).toHaveBeenCalled()
  })
})

describe('notificationApi REST', () => {
  it('fetches notifications and unread count', () => {
    notificationApi.getNotifications(0, 20)
    expect(http.get).toHaveBeenCalledWith('/notifications', { params: { skip: 0, take: 20 } })
    notificationApi.getUnreadCount()
    expect(http.get).toHaveBeenCalledWith('/notifications/unread-count')
  })

  it('marks notifications read', () => {
    notificationApi.markNotificationRead(1)
    expect(http.post).toHaveBeenCalledWith('/notifications/1/read')
    notificationApi.markAllNotificationsRead()
    expect(http.post).toHaveBeenCalledWith('/notifications/read-all')
  })
})

describe('notificationApi severity helpers', () => {
  it('ranks severities', () => {
    expect(notificationApi.getSeverityRank('critical')).toBe(5)
    expect(notificationApi.getSeverityRank('info')).toBe(2)
    expect(notificationApi.getSeverityRank('nope')).toBe(2)
  })

  it('derives severity from alarm text', () => {
    expect(notificationApi.normalizeNotificationSeverity({ severity: 'warning' })).toBe('warning')
    expect(notificationApi.normalizeNotificationSeverity({ category: 'alarm', referenceType: 'alarm', title: 'Đột nhập khẩn cấp' })).toBe('critical')
    expect(notificationApi.normalizeNotificationSeverity({ category: 'alarm', referenceType: 'alarm', body: 'uy hiếp' })).toBe('critical')
    expect(notificationApi.normalizeNotificationSeverity({ category: 'alarm', referenceType: 'alarm', title: 'thường' })).toBe('warning')
    expect(notificationApi.normalizeNotificationSeverity({ category: 'chat' })).toBe('success')
    expect(notificationApi.normalizeNotificationSeverity({ category: 'approval' })).toBe('caution')
    expect(notificationApi.normalizeNotificationSeverity({})).toBe('info')
  })
})
