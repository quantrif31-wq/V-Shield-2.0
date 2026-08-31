import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import * as chatApi from '../chatApi'
import http from '../http'

const hoisted = vi.hoisted(() => {
  const connections = []
  const builder = {
    options: null,
    failStart: false,
    withUrl: vi.fn((url, options) => { builder.options = { url, ...options }; return builder }),
    withAutomaticReconnect: vi.fn(() => builder),
    configureLogging: vi.fn(() => builder),
    build: vi.fn(() => {
      const connection = {
        state: 'Disconnected',
        connectionId: 'conn-1',
        handlers: new Map(),
        lifecycle: {},
        start: vi.fn(() => Promise.resolve().then(() => {
          if (builder.failStart) throw new Error('start failed')
          connection.state = 'Connected'
        })),
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

const rtHoisted = vi.hoisted(() => {
  return {
    updateRealtimeStatus: vi.fn(),
    markRealtimeUpdated: vi.fn(),
    getRealtimeStatus: vi.fn(() => ({})),
    onRealtimeStatus: vi.fn(() => () => {}),
  }
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
  updateRealtimeStatus: rtHoisted.updateRealtimeStatus,
  markRealtimeUpdated: rtHoisted.markRealtimeUpdated,
  getRealtimeStatus: rtHoisted.getRealtimeStatus,
  onRealtimeStatus: rtHoisted.onRealtimeStatus,
}))

beforeEach(async () => {
  vi.clearAllMocks()
  await chatApi.disconnectChatHub()
  hoisted.connections.length = 0
  hoisted.builder.options = null
  hoisted.builder.failStart = false
  sessionStorage.clear()
  localStorage.clear()
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

describe('chatApi subscriptions and lifecycle', () => {
  async function connect() {
    return chatApi.connectChatHub('token')
  }

  it('getConnection returns null before connecting and the connection after', async () => {
    expect(chatApi.getConnection()).toBeNull()
    const connection = await connect()
    expect(chatApi.getConnection()).toBe(connection)
  })

  it('registers and fires typing/read/incall/callresp/callended callbacks', async () => {
    const connection = await connect()
    const onTypingCb = vi.fn()
    const onReadCb = vi.fn()
    const onIncomingCallCb = vi.fn()
    const onCallResponseCb = vi.fn()
    const onCallEndedCb = vi.fn()
    chatApi.onTyping(onTypingCb)
    chatApi.onRead(onReadCb)
    chatApi.onIncomingCall(onIncomingCallCb)
    chatApi.onCallResponse(onCallResponseCb)
    chatApi.onCallEnded(onCallEndedCb)
    connection.handlers.get('UserTyping')({ emp: 1 })
    connection.handlers.get('MessagesRead')({ conv: 9 })
    connection.handlers.get('IncomingCall')({ kind: 'video' })
    connection.handlers.get('CallResponse')({ ok: true })
    connection.handlers.get('CallEnded')({ reason: 'bye' })
    expect(onTypingCb).toHaveBeenCalledWith({ emp: 1 })
    expect(onReadCb).toHaveBeenCalledWith({ conv: 9 })
    expect(onIncomingCallCb).toHaveBeenCalledWith({ kind: 'video' })
    expect(onCallResponseCb).toHaveBeenCalledWith({ ok: true })
    expect(onCallEndedCb).toHaveBeenCalledWith({ reason: 'bye' })
  })

  it('unsubscribes returned functions stop callbacks firing', async () => {
    const connection = await connect()
    const cb = vi.fn()
    const unsubTyping = chatApi.onTyping(cb)
    const unsubRead = chatApi.onRead(cb)
    const unsubMsg = chatApi.onMessage(cb)
    const unsubCall = chatApi.onIncomingCall(cb)
    const unsubCallResp = chatApi.onCallResponse(cb)
    const unsubCallEnded = chatApi.onCallEnded(cb)
    unsubTyping()
    unsubRead()
    unsubMsg()
    unsubCall()
    unsubCallResp()
    unsubCallEnded()
    connection.handlers.get('UserTyping')({ emp: 1 })
    connection.handlers.get('MessagesRead')({ conv: 9 })
    connection.handlers.get('ReceiveMessage')('hi')
    connection.handlers.get('IncomingCall')({ kind: 'video' })
    connection.handlers.get('CallResponse')({ ok: true })
    connection.handlers.get('CallEnded')({ reason: 'bye' })
    expect(cb).not.toHaveBeenCalled()
  })

  it('exports connection state accessors', () => {
    const unsub = chatApi.onChatConnectionState(vi.fn())
    expect(typeof unsub).toBe('function')
    expect(chatApi.getChatConnectionState()).toEqual({})
  })

  it('runs lifecycle handlers for reconnect, reconnected and close', async () => {
    vi.useFakeTimers()
    const connection = await connect()
    connection.lifecycle.reconnecting()
    connection.lifecycle.reconnected('conn-2')
    connection.lifecycle.close(new Error('boom'))
    connection.lifecycle.close(null)
    vi.runAllTimers()
    expect(connection.lifecycle.reconnecting).toBeDefined()
    vi.useRealTimers()
  })

  it('marks chat stale after reconnecting remains down', async () => {
    vi.useFakeTimers()
    await chatApi.connectChatHub('token')
    const conn = hoisted.connections[0]
    conn.lifecycle.reconnecting()
    vi.advanceTimersByTime(15000)
    expect(rtHoisted.updateRealtimeStatus).toHaveBeenCalledWith('chat', 'stale')
    vi.useRealTimers()
  })

  it('calls callResponse and endCall over the hub when connected', async () => {
    const connection = await connect()
    await chatApi.callResponse(4, 'offer', { sdp: 'x' })
    expect(connection.invoke).toHaveBeenCalledWith('CallResponse', 4, 'offer', { sdp: 'x' })
    await chatApi.endCall(4, 11)
    expect(connection.invoke).toHaveBeenCalledWith('EndCall', 4, 11)
    await chatApi.endCall(5)
    expect(connection.invoke).toHaveBeenCalledWith('EndCall', 5, null)
  })

  it('markRead falls back to HTTP when not connected', async () => {
    await chatApi.markRead(8)
    expect(http.post).toHaveBeenCalledWith('/chat/conversations/8/read')
  })

  it('marks read over the hub when connected', async () => {
    const connection = await connect()
    await chatApi.markRead(2)
    expect(connection.invoke).toHaveBeenCalledWith('MarkRead', 2)
    expect(http.post).not.toHaveBeenCalled()
  })

  it('sendMessage falls back to HTTP when the hub invoke fails', async () => {
    const connection = await connect()
    http.post.mockResolvedValue({ data: { data: { ok: true } } })
    connection.invoke.mockRejectedValueOnce(new Error('hub fail'))
    const result = await chatApi.sendMessage(1, 'hello')
    expect(http.post).toHaveBeenCalledWith('/chat/conversations/1/messages', expect.objectContaining({ content: 'hello' }))
    expect(result.deliveredVia).toBe('http')
  })

  it('creates a fallback client message id when crypto.randomUUID is missing', async () => {
    const original = globalThis.crypto
    Object.defineProperty(globalThis, 'crypto', { value: {}, configurable: true, writable: true })
    const result = await chatApi.sendMessage(1, 'with uuid fallback')
    expect(chatApi).toBeDefined()
    const body = http.post.mock.calls[0][1]
    expect(body.clientMessageId).toMatch(/^msg-\d+-/)
    Object.defineProperty(globalThis, 'crypto', { value: original, configurable: true, writable: true })
  })

  it('returns an existing connectionPromise when connecting concurrently', async () => {
    const p1 = chatApi.connectChatHub('token')
    const p2 = chatApi.connectChatHub('token')
    expect(await p1).toBe(await p2)
  })

  it('reuses an already-connected connection', async () => {
    const connection = await connect()
    const again = await chatApi.connectChatHub('token')
    expect(again).toBe(connection)
    expect(hoisted.connections).toHaveLength(1)
  })

  it('returns an actively connecting connection when reconnecting state', async () => {
    const connection = await connect()
    connection.state = 'Reconnecting'
    const result = await chatApi.connectChatHub('token')
    expect(result).toBe(connection)
  })

  it('propagates start failures and clears the connection', async () => {
    hoisted.builder.failStart = true
    await expect(chatApi.connectChatHub('token')).rejects.toThrow('start failed')
    expect(chatApi.getConnection()).toBeNull()
  })
})
