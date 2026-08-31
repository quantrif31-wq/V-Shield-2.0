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
vi.mock('../observability', () => ({ captureError: vi.fn(), captureEvent: vi.fn() }))
vi.mock('../realtimeStatus', () => ({
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

afterEach(() => {
  vi.useRealTimers()
})

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
    expect(notificationApi.getSeverityRank('warning')).toBe(4)
    expect(notificationApi.getSeverityRank('caution')).toBe(3)
    expect(notificationApi.getSeverityRank('info')).toBe(2)
    expect(notificationApi.getSeverityRank('success')).toBe(1)
    expect(notificationApi.getSeverityRank('nope')).toBe(2)
  })

  it('passes through a known severity', () => {
    expect(notificationApi.normalizeNotificationSeverity({ severity: 'warning' })).toBe('warning')
    expect(notificationApi.normalizeNotificationSeverity({ severity: 'critical' })).toBe('critical')
  })

  it('derives chat and approval severities', () => {
    expect(notificationApi.normalizeNotificationSeverity({ category: 'chat', title: 'New message' })).toBe('success')
    expect(notificationApi.normalizeNotificationSeverity({ category: 'approval', body: 'Approve request' })).toBe('caution')
  })

  it('derives alarm critical severity from threat keywords', () => {
    expect(notificationApi.normalizeNotificationSeverity({ category: 'alarm', referenceType: 'alarm', title: 'Khẩn cấp! Đột nhập' })).toBe('critical')
    expect(notificationApi.normalizeNotificationSeverity({ category: 'alarm', referenceType: 'alarm', title: 'Uy hiếp an ninh' })).toBe('critical')
    expect(notificationApi.normalizeNotificationSeverity({ category: 'alarm', referenceType: 'alarm', body: 'phát hiện intrusion' })).toBe('critical')
    expect(notificationApi.normalizeNotificationSeverity({ category: 'alarm', referenceType: 'alarm', body: 'duress detected' })).toBe('critical')
  })

  it('derives warning for a non-critical alarm', () => {
    expect(notificationApi.normalizeNotificationSeverity({ category: 'alarm', referenceType: 'alarm', title: 'Thông thường' })).toBe('warning')
    expect(notificationApi.normalizeNotificationSeverity({ category: 'alarm', referenceType: 'other', title: 'Khẩn cấp' })).toBe('warning')
  })

  it('defaults to info for unknown categories', () => {
    expect(notificationApi.normalizeNotificationSeverity({ category: 'unknown', body: 'anything' })).toBe('info')
    expect(notificationApi.normalizeNotificationSeverity({})).toBe('info')
  })
})

describe('notificationApi notification rules API', () => {
  it('calls the rules endpoints', () => {
    notificationApi.getNotificationRules()
    expect(http.get).toHaveBeenCalledWith('/notification-rules')
    notificationApi.createNotificationRule({ name: 'r' })
    expect(http.post).toHaveBeenCalledWith('/notification-rules', { name: 'r' })
    notificationApi.updateNotificationRule(1, { name: 'r2' })
    expect(http.put).toHaveBeenCalledWith('/notification-rules/1', { name: 'r2' })
    notificationApi.deleteNotificationRule(1)
    expect(http.delete).toHaveBeenCalledWith('/notification-rules/1')
    notificationApi.getRuleSuggestions('Admin')
    expect(http.get).toHaveBeenCalledWith('/notification-rules/suggestions', { params: { role: 'Admin' } })
  })
})

describe('notificationApi SignalR lifecycle', () => {
  async function fresh() {
    vi.resetModules()
    vi.clearAllMocks()
    hoisted.connections.length = 0
    sessionStorage.clear()
    localStorage.clear()
    http = (await import('../http')).default
    notificationApi = await import('../notificationApi')
    const conn = await notificationApi.connectNotificationHub('token')
    await Promise.resolve()
    await Promise.resolve()
    return conn
  }

  it('returns the same connection when already connected', async () => {
    const c1 = await notificationApi.connectNotificationHub('token')
    const c2 = await notificationApi.connectNotificationHub('token')
    expect(c2).toBe(c1)
  })

  it('returns the in-flight promise on a second concurrent call', async () => {
    vi.resetModules()
    vi.clearAllMocks()
    hoisted.connections.length = 0
    const origBuild = hoisted.builder.build
    hoisted.builder.build = vi.fn(() => {
      const connection = {
        state: 'Connecting',
        handlers: new Map(),
        lifecycle: {},
        start: vi.fn(async () => { await Promise.resolve(); connection.state = 'Connected' }),
        stop: vi.fn(async () => { connection.state = 'Disconnected' }),
        on: vi.fn(() => {}),
        onreconnecting: vi.fn(() => {}),
        onreconnected: vi.fn(() => {}),
        onclose: vi.fn(() => {}),
      }
      hoisted.connections.push(connection)
      return connection
    })
    http = (await import('../http')).default
    notificationApi = await import('../notificationApi')
    const p1 = notificationApi.connectNotificationHub('token')
    const p2 = notificationApi.connectNotificationHub('token')
    const r1 = await p1
    const r2 = await p2
    expect(r2).toBe(r1)
    hoisted.builder.build = origBuild
  })

  it('marks the connection stale after reconnecting is pending for 15s', async () => {
    const conn = await fresh()
    vi.useFakeTimers()
    conn.lifecycle.reconnecting()
    vi.advanceTimersByTime(16000)
    conn.lifecycle.reconnected({ connectionId: 'abc' })
    vi.advanceTimersByTime(100)
    vi.useRealTimers()
    expect(conn).toBeTruthy()
  })

  it('returns an already-connecting connection without starting a new one', async () => {
    const conn = await fresh()
    conn.state = 'Connecting'
    const result = await notificationApi.connectNotificationHub('token')
    expect(result).toBe(conn)
  })

  it('returns an already-reconnecting connection', async () => {
    const conn = await fresh()
    conn.state = 'Reconnecting'
    const result = await notificationApi.connectNotificationHub('token')
    expect(result).toBe(conn)
  })

  it('returns an already-connected connection immediately', async () => {
    const conn = await fresh()
    const result = await notificationApi.connectNotificationHub('token')
    expect(result).toBe(conn)
  })

  it('builds the URL with access token factory using sessionStorage first', async () => {
    const conn = await fresh()
    sessionStorage.setItem('v_shield_token', 'session-tok')
    const options = hoisted.builder.withUrl.mock.calls[0][1]
    expect(options.accessTokenFactory()).toBe('session-tok')
    expect(conn).toBeTruthy()
  })

  it('access token factory falls back to localStorage then the argument', async () => {
    await fresh()
    localStorage.setItem('v_shield_token', 'local-tok')
    const options = hoisted.builder.withUrl.mock.calls[0][1]
    expect(options.accessTokenFactory()).toBe('local-tok')
  })

  it('access token factory ends at the supplied token when nothing stored', async () => {
    await fresh()
    const options = hoisted.builder.withUrl.mock.calls[0][1]
    expect(options.accessTokenFactory()).toBe('token')
  })

  it('invokes reconnect lifecycle callbacks and timer', async () => {
    vi.useFakeTimers()
    const conn = await notificationApi.connectNotificationHub('token')
    conn.lifecycle.reconnecting()
    conn.lifecycle.reconnected({ connectionId: 'abc' })
    vi.advanceTimersByTime(16000)
    conn.lifecycle.close({ name: 'Error' })
    await notificationApi.disconnectNotificationHub()
    vi.useRealTimers()
    expect(conn.lifecycle).toBeTruthy()
  })

  it('invokes the onclose callback with no error', async () => {
    vi.useFakeTimers()
    const conn = await notificationApi.connectNotificationHub('token')
    conn.lifecycle.close()
    vi.useRealTimers()
    await notificationApi.disconnectNotificationHub()
    expect(conn).toBeTruthy()
  })
})

describe('notificationApi unsubscribe and state helpers', () => {
  it('invokes unsubscribe functions', async () => {
    const conn = await notificationApi.connectNotificationHub('token')
    const unsub1 = notificationApi.onNotification(vi.fn())
    const unsub2 = notificationApi.onUnreadCountChanged(vi.fn())
    unsub1()
    unsub2()
    conn.handlers.get('NewNotification')({ id: 2 })
    conn.handlers.get('UnreadCountUpdated')(3)
    expect(hoisted.connections[0]).toBe(conn)
  })

  it('exposes connection state helpers', async () => {
    const conn = await notificationApi.connectNotificationHub('token')
    const unsub = notificationApi.onNotificationConnectionState(vi.fn())
    const state = notificationApi.getNotificationConnectionState()
    expect(typeof unsub).toBe('function')
    expect(state).toEqual({})
    expect(conn).toBeTruthy()
  })
})

describe('notificationApi start failure', () => {
  it('clears the connection and reports an error when start fails', async () => {
    vi.resetModules()
    vi.clearAllMocks()
    hoisted.connections.length = 0
    const failingBuilder = hoisted.builder
    const originalBuild = failingBuilder.build
    failingBuilder.build = vi.fn(() => {
      const connection = {
        state: 'Disconnected',
        handlers: new Map(),
        lifecycle: {},
        start: vi.fn(async () => { throw new Error('start failed') }),
        stop: vi.fn(async () => {}),
        on: vi.fn(() => {}),
        onreconnecting: vi.fn(() => {}),
        onreconnected: vi.fn(() => {}),
        onclose: vi.fn(() => {}),
      }
      hoisted.connections.push(connection)
      return connection
    })
    http = (await import('../http')).default
    notificationApi = await import('../notificationApi')
    await expect(notificationApi.connectNotificationHub('token')).rejects.toThrow('start failed')
    failingBuilder.build = originalBuild
  })
})
