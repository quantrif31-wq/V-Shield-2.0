import http from './http'
import * as signalR from '@microsoft/signalr'
import { API_ORIGIN } from '../config/api'

const API_URL = import.meta.env.VITE_API_URL || API_ORIGIN
const AUTH_TOKEN_KEY = 'v_shield_token'

let connection = null
let messageCallbacks = []
let typingCallbacks = []
let readCallbacks = []
let callCallbacks = []
let callResponseCallbacks = []
let callEndedCallbacks = []

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

  const accessToken = token || readAuthToken()

  connection = new signalR.HubConnectionBuilder()
    .withUrl(`${API_URL}/hubs/chat`, {
      accessTokenFactory: () => accessToken
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Warning)
    .build()

  connection.on('ReceiveMessage', (msg) => {
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
    console.log('ChatHub reconnecting...')
  })

  connection.onreconnected(() => {
    console.log('ChatHub reconnected')
  })

  await connection.start()
  return connection
}

export function disconnectChatHub() {
  if (connection) {
    connection.stop()
    connection = null
  }
  messageCallbacks = []
  typingCallbacks = []
  readCallbacks = []
  callCallbacks = []
  callResponseCallbacks = []
  callEndedCallbacks = []
}

export function onMessage(callback) {
  messageCallbacks.push(callback)
  return () => { messageCallbacks = messageCallbacks.filter(c => c !== callback) }
}

export function onTyping(callback) {
  typingCallbacks.push(callback)
  return () => { typingCallbacks = typingCallbacks.filter(c => c !== callback) }
}

export function onRead(callback) {
  readCallbacks.push(callback)
  return () => { readCallbacks = readCallbacks.filter(c => c !== callback) }
}

export function onIncomingCall(callback) {
  callCallbacks.push(callback)
  return () => { callCallbacks = callCallbacks.filter(c => c !== callback) }
}

export function onCallResponse(callback) {
  callResponseCallbacks.push(callback)
  return () => { callResponseCallbacks = callResponseCallbacks.filter(c => c !== callback) }
}

export function onCallEnded(callback) {
  callEndedCallbacks.push(callback)
  return () => { callEndedCallbacks = callEndedCallbacks.filter(c => c !== callback) }
}

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
