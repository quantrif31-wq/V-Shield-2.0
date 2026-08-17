import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => {
  const connections = []
  const builder = {
    options: null,
    withUrl: vi.fn((url, options) => { builder.options = { url, ...options }; return builder }),
    withAutomaticReconnect: vi.fn(() => builder),
    configureLogging: vi.fn(() => builder),
    build: vi.fn(() => {
      const connection = {
        state: 'Disconnected',
        connectionId: 'conn-1',
        handlers: new Map(),
        lifecycle: {},
        start: vi.fn(async () => { connection.state = 'Connected' }),
        stop: vi.fn(async () => { connection.state = 'Disconnected' }),
        invoke: vi.fn(async () => ({})),
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

let chatApi
let http

beforeEach(async () => {
  vi.resetModules()
  vi.clearAllMocks()
  hoisted.connections.length = 0
  hoisted.builder.options = null
  sessionStorage.clear()
  localStorage.clear()
  http = (await import('../http')).default
  chatApi = await import('../chatApi')
})

afterEach(() => {
  sessionStorage.clear()
  localStorage.clear()
})

describe('chatApi REST', () => {
  it('fetches contacts and conversations', () => {
    chatApi.getContacts()
    expect(http.get).toHaveBeenCalledWith('/chat/contacts')
    chatApi.getConversations()
    expect(http.get).toHaveBeenCalledWith('/chat/conversations')
  })

  it('fetches messages and creates conversations', () => {
    chatApi.getMessages(3, 0, 50)
    expect(http.get).toHaveBeenCalledWith('/chat/conversations/3/messages?skip=0&take=50')
    chatApi.createConversation([1, 2], 'Nhóm')
    expect(http.post).toHaveBeenCalledWith('/chat/conversations', { employeeIds: [1, 2], title: 'Nhóm' })
    chatApi.markConversationRead(3)
    expect(http.post).toHaveBeenCalledWith('/chat/conversations/3/read')
  })

  it('sends a message via HTTP when no hub connection exists', async () => {
    http.post.mockResolvedValue({ data: { data: { ok: true } } })
    const result = await chatApi.sendMessage(1, 'xin chào')
    expect(http.post).toHaveBeenCalledWith('/chat/conversations/1/messages', expect.objectContaining({ content: 'xin chào' }))
    expect(result.deliveredVia).toBe('http')
  })

  it('returns null for empty message content', async () => {
    const result = await chatApi.sendMessage(1, '   ')
    expect(result).toBeNull()
  })
})

describe('chatApi SignalR', () => {
  it('connects to the chat hub and reports live status', async () => {
    const connection = await chatApi.connectChatHub('token-1')
    expect(hoisted.builder.options.url).toBe('http://localhost:5107/hubs/chat')
    expect(hoisted.builder.options.accessTokenFactory()).toBe('token-1')
    expect(connection.state).toBe('Connected')
  })

  it('sends a message over the hub when connected', async () => {
    const connection = await chatApi.connectChatHub('token')
    connection.invoke.mockResolvedValue({ id: 5 })
    const result = await chatApi.sendMessage(1, 'hi', 'Text', { sig: 1 }, 'client-1')
    expect(connection.invoke).toHaveBeenCalledWith('SendMessage', 1, 'hi', 'Text', { sig: 1 }, 'client-1')
    expect(result.deliveredVia).toBe('hub')
  })

  it('invokes mark-read, typing and call actions over the hub', async () => {
    const connection = await chatApi.connectChatHub('token')
    await chatApi.markRead(2)
    expect(connection.invoke).toHaveBeenCalledWith('MarkRead', 2)
    await chatApi.sendTyping(2)
    expect(connection.invoke).toHaveBeenCalledWith('Typing', 2)
    await chatApi.callUser(7, 'video', { x: 1 })
    expect(connection.invoke).toHaveBeenCalledWith('CallUser', 7, 'video', { x: 1 }, null)
  })

  it('registers and invokes message callbacks', async () => {
    const connection = await chatApi.connectChatHub('token')
    const cb = vi.fn()
    chatApi.onMessage(cb)
    connection.handlers.get('ReceiveMessage')('hello')
    expect(cb).toHaveBeenCalledWith('hello')
  })

  it('disconnects and clears callbacks', async () => {
    const connection = await chatApi.connectChatHub('token')
    const cb = vi.fn()
    chatApi.onMessage(cb)
    await chatApi.disconnectChatHub()
    expect(connection.stop).toHaveBeenCalled()
    connection.handlers.get('ReceiveMessage')('hello')
    expect(cb).not.toHaveBeenCalled()
  })
})
