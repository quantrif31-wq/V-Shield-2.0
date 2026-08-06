import http from './http'
import * as signalR from '@microsoft/signalr'
import { API_ORIGIN } from '../config/api'
import { captureError, captureEvent } from './observability'
import { getRealtimeStatus, markRealtimeUpdated, onRealtimeStatus, updateRealtimeStatus } from './realtimeStatus'

const API_URL = import.meta.env.VITE_API_URL || API_ORIGIN
const AUTH_TOKEN_KEY = 'v_shield_token'

let connection = null
let connectionPromise = null
let staleTimer = null
const messageCallbacks = new Set()
const typingCallbacks = new Set()
const readCallbacks = new Set()
const callCallbacks = new Set()
const callResponseCallbacks = new Set()
const callEndedCallbacks = new Set()

function createClientMessageId() {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID()
  }

  return `msg-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`
}

export function getConnection() {
  return connection
}

function readAuthToken() {
  return sessionStorage.getItem(AUTH_TOKEN_KEY) || localStorage.getItem(AUTH_TOKEN_KEY) || ''
}

export async function connectChatHub(token) {
  if (connection && connection.state === signalR.HubConnectionState.Connected) {
    return connection
  }
  if (connectionPromise) return connectionPromise
  if (connection && [signalR.HubConnectionState.Connecting, signalR.HubConnectionState.Reconnecting].includes(connection.state)) return connection

  connection = new signalR.HubConnectionBuilder()
    .withUrl(`${API_URL}/hubs/chat`, {
      accessTokenFactory: () => readAuthToken() || token || ''
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Warning)
    .build()

  connection.on('ReceiveMessage', (msg) => {
    markRealtimeUpdated('chat')
    messageCallbacks.forEach(cb => cb(msg))
  })

  connection.on('MessagesRead', (data) => {
    readCallbacks.forEach(cb => cb(data))
  })

  connection.on('UserTyping', (data) => {
    typingCallbacks.forEach(cb => cb(data))
  })

  connection.on('IncomingCall', (data) => {
    callCallbacks.forEach(cb => cb(data))
  })

  connection.on('CallResponse', (data) => {
    callResponseCallbacks.forEach(cb => cb(data))
  })

  connection.on('CallEnded', (data) => {
    callEndedCallbacks.forEach(cb => cb(data))
  })

  connection.onreconnecting(() => {
    updateRealtimeStatus('chat', 'reconnecting')
    captureEvent('signalr_reconnecting', { channel: 'chat' }, 'warning')
    clearTimeout(staleTimer)
    staleTimer = setTimeout(() => updateRealtimeStatus('chat', 'stale'), 15000)
  })

  connection.onreconnected((connectionId) => {
    clearTimeout(staleTimer)
    updateRealtimeStatus('chat', 'live', { connectionId, lastUpdated: new Date().toISOString() })
    captureEvent('signalr_reconnected', { channel: 'chat' })
  })

  connection.onclose((error) => {
    clearTimeout(staleTimer)
    updateRealtimeStatus('chat', 'disconnected', { connectionId: null })
    captureEvent('signalr_disconnected', { channel: 'chat', reason: error?.name || 'closed' }, error ? 'error' : 'warning')
  })

  updateRealtimeStatus('chat', 'connecting')
  const activeConnection = connection
  connectionPromise = activeConnection.start()
    .then(() => {
      updateRealtimeStatus('chat', 'live', { connectionId: activeConnection.connectionId, lastUpdated: new Date().toISOString() })
      captureEvent('signalr_connected', { channel: 'chat' })
      return activeConnection
    })
    .catch((error) => {
      if (connection === activeConnection) connection = null
      updateRealtimeStatus('chat', 'disconnected', { connectionId: null })
      captureError(error, 'signalr_connection_failure', { channel: 'chat' })
      throw error
    })
    .finally(() => { connectionPromise = null })
  return connectionPromise
}

export async function disconnectChatHub() {
  const activeConnection = connection
  connection = null
  connectionPromise = null
  clearTimeout(staleTimer)
  staleTimer = null
  if (activeConnection) await activeConnection.stop()
  messageCallbacks.clear()
  typingCallbacks.clear()
  readCallbacks.clear()
  callCallbacks.clear()
  callResponseCallbacks.clear()
  callEndedCallbacks.clear()
  updateRealtimeStatus('chat', 'disconnected', { connectionId: null })
}

export function onMessage(callback) {
  messageCallbacks.add(callback)
  return () => messageCallbacks.delete(callback)
}

export function onTyping(callback) {
  typingCallbacks.add(callback)
  return () => typingCallbacks.delete(callback)
}

export function onRead(callback) {
  readCallbacks.add(callback)
  return () => readCallbacks.delete(callback)
}

export function onIncomingCall(callback) {
  callCallbacks.add(callback)
  return () => callCallbacks.delete(callback)
}

export function onCallResponse(callback) {
  callResponseCallbacks.add(callback)
  return () => callResponseCallbacks.delete(callback)
}

export function onCallEnded(callback) {
  callEndedCallbacks.add(callback)
  return () => callEndedCallbacks.delete(callback)
}

export const onChatConnectionState = (callback) => onRealtimeStatus('chat', callback)
export const getChatConnectionState = () => getRealtimeStatus('chat')

export async function sendMessage(conversationId, content, messageType = 'Text', signalingData = null, clientMessageId = createClientMessageId()) {
  const trimmedContent = String(content || '').trim()
  if (!trimmedContent) {
    return null
  }

  if (connection && connection.state === signalR.HubConnectionState.Connected) {
    try {
      const payload = await connection.invoke('SendMessage', conversationId, trimmedContent, messageType, signalingData, clientMessageId)
      return { deliveredVia: 'hub', data: payload, clientMessageId }
    } catch (error) {
      console.warn('Chat hub send failed, falling back to HTTP.', error)
    }
  }

  const response = await http.post(`/chat/conversations/${conversationId}/messages`, {
    content: trimmedContent,
    messageType,
    clientMessageId,
    signalingData,
  })

  return { deliveredVia: 'http', data: response.data?.data || null, raw: response, clientMessageId }
}

export async function markRead(conversationId) {
  if (connection && connection.state === signalR.HubConnectionState.Connected) {
    await connection.invoke('MarkRead', conversationId)
    return
  }

  return markConversationRead(conversationId)
}

export async function sendTyping(conversationId) {
  if (connection && connection.state === signalR.HubConnectionState.Connected) {
    await connection.invoke('Typing', conversationId)
  }
}

export async function callUser(targetEmployeeId, signalingType, signalingData, conversationId = null) {
  if (connection && connection.state === signalR.HubConnectionState.Connected) {
    await connection.invoke('CallUser', targetEmployeeId, signalingType, signalingData, conversationId)
  }
}

export async function callResponse(targetEmployeeId, signalingType, signalingData) {
  if (connection && connection.state === signalR.HubConnectionState.Connected) {
    await connection.invoke('CallResponse', targetEmployeeId, signalingType, signalingData)
  }
}

export async function endCall(targetEmployeeId, conversationId = null) {
  if (connection && connection.state === signalR.HubConnectionState.Connected) {
    await connection.invoke('EndCall', targetEmployeeId, conversationId)
  }
}

// REST API
export function getContacts() {
  return http.get('/chat/contacts')
}

export function getConversations() {
  return http.get('/chat/conversations')
}

export function createConversation(employeeIds, title = null) {
  return http.post('/chat/conversations', { employeeIds, title })
}

export function getMessages(conversationId, skip = 0, take = 50) {
  return http.get(`/chat/conversations/${conversationId}/messages?skip=${skip}&take=${take}`)
}

export function markConversationRead(conversationId) {
  return http.post(`/chat/conversations/${conversationId}/read`)
}
