import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const signalRMock = vi.hoisted(() => {
  const connections = []
  const buildConnection = () => {
    const handlers = new Map()
    const lifecycle = {}
    const connection = {
      state: 'Disconnected',
      connectionId: `connection-${connections.length + 1}`,
      handlers,
      lifecycle,
      start: vi.fn(async () => { connection.state = 'Connected' }),
      stop: vi.fn(async () => { connection.state = 'Disconnected'; lifecycle.close?.() }),
      invoke: vi.fn(async () => ({})),
      on: vi.fn((name, callback) => handlers.set(name, callback)),
      onreconnecting: vi.fn(callback => { lifecycle.reconnecting = callback }),
      onreconnected: vi.fn(callback => { lifecycle.reconnected = callback }),
      onclose: vi.fn(callback => { lifecycle.close = callback }),
    }
    connections.push(connection)
    return connection
  }
  const builder = {
    options: null,
    withUrl: vi.fn((url, options) => { builder.options = { url, ...options }; return builder }),
    withAutomaticReconnect: vi.fn(() => builder),
    configureLogging: vi.fn(() => builder),
    build: vi.fn(buildConnection),
  }
  return { connections, builder }
})

vi.mock('@microsoft/signalr', () => ({
  HubConnectionState: { Disconnected: 'Disconnected', Connecting: 'Connecting', Connected: 'Connected', Reconnecting: 'Reconnecting' },
  LogLevel: { Warning: 'Warning' },
  HubConnectionBuilder: vi.fn(function HubConnectionBuilder() { return signalRMock.builder }),
}))

vi.mock('../http', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}))

import * as chatApi from '../chatApi'
import * as notificationApi from '../notificationApi'
import { resetRealtimeStatusForTests } from '../realtimeStatus'

describe('SignalR lifecycle hardening', () => {
  beforeEach(() => {
    sessionStorage.clear()
    localStorage.clear()
    resetRealtimeStatusForTests()
    signalRMock.connections.length = 0
    signalRMock.builder.build.mockClear()
  })

  afterEach(async () => {
    await chatApi.disconnectChatHub()
    await notificationApi.disconnectNotificationHub()
  })

  it('reuses a single chat connection and reads the latest token during reconnect', async () => {
    sessionStorage.setItem('v_shield_token', 'first-token')
    const [first, second] = await Promise.all([chatApi.connectChatHub(), chatApi.connectChatHub()])

    expect(first).toBe(second)
    expect(signalRMock.builder.build).toHaveBeenCalledTimes(1)
    expect(signalRMock.builder.options.accessTokenFactory()).toBe('first-token')

    sessionStorage.setItem('v_shield_token', 'refreshed-token')
    expect(signalRMock.builder.options.accessTokenFactory()).toBe('refreshed-token')
  })

  it('deduplicates handlers and exposes reconnecting/live/disconnected states', async () => {
    const states = []
    const unsubscribeState = chatApi.onChatConnectionState(state => states.push(state.status))
    const onMessage = vi.fn()
    chatApi.onMessage(onMessage)
    chatApi.onMessage(onMessage)
    const connection = await chatApi.connectChatHub('fallback-token')

    connection.handlers.get('ReceiveMessage')({ messageId: 1 })
    connection.lifecycle.reconnecting()
    connection.lifecycle.reconnected('reconnected-id')
    await chatApi.disconnectChatHub()

    expect(onMessage).toHaveBeenCalledTimes(1)
    expect(states).toEqual(expect.arrayContaining(['disconnected', 'connecting', 'live', 'reconnecting']))
    expect(states.at(-1)).toBe('disconnected')
    unsubscribeState()
  })

  it('cleans notification handlers after disconnect', async () => {
    const onNotification = vi.fn()
    notificationApi.onNotification(onNotification)
    const first = await notificationApi.connectNotificationHub('token')
    first.handlers.get('NewNotification')({ id: 1 })
    await notificationApi.disconnectNotificationHub()
    first.handlers.get('NewNotification')({ id: 2 })

    expect(onNotification).toHaveBeenCalledTimes(1)
    expect(notificationApi.getNotificationConnectionState().status).toBe('disconnected')
  })
})
