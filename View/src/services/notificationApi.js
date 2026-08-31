import http from './http'
import * as signalR from '@microsoft/signalr'
import { API_ORIGIN } from '../config/api'
import { captureError, captureEvent } from './observability'
import { getRealtimeStatus, markRealtimeUpdated, onRealtimeStatus, updateRealtimeStatus } from './realtimeStatus'

const API_URL = import.meta.env.VITE_API_URL || API_ORIGIN

let connection = null
let connectionPromise = null
let staleTimer = null
const notificationCallbacks = new Set()
const unreadCountCallbacks = new Set()
const syncEventCallbacks = new Set()

const SEVERITY_RANK = {
  success: 1,
  info: 2,
  caution: 3,
  warning: 4,
  critical: 5
}

// SignalR connection
export async function connectNotificationHub(token) {
  if (connection && connection.state === signalR.HubConnectionState.Connected) {
    return connection
  }
  if (connectionPromise) return connectionPromise
  if (connection && [signalR.HubConnectionState.Connecting, signalR.HubConnectionState.Reconnecting].includes(connection.state)) return connection
  connection = new signalR.HubConnectionBuilder()
    .withUrl(`${API_URL}/hubs/notifications`, {
      accessTokenFactory: () => sessionStorage.getItem('v_shield_token') || localStorage.getItem('v_shield_token') || token || ''
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Warning)
    .build()

  connection.on('NewNotification', (notification) => {
    markRealtimeUpdated('notifications')
    notificationCallbacks.forEach(cb => cb(notification))
  })

  connection.on('UnreadCountUpdated', (count) => {
    markRealtimeUpdated('notifications')
    unreadCountCallbacks.forEach(cb => cb(count))
  })

  connection.on('SyncEventApplied', (event) => {
    markRealtimeUpdated('sync')
    syncEventCallbacks.forEach(cb => cb(event))
  })

  connection.onreconnecting(() => {
    updateRealtimeStatus('notifications', 'reconnecting')
    captureEvent('signalr_reconnecting', { channel: 'notifications' }, 'warning')
    clearTimeout(staleTimer)
    staleTimer = setTimeout(() => updateRealtimeStatus('notifications', 'stale'), 15000)
  })
  connection.onreconnected((connectionId) => {
    clearTimeout(staleTimer)
    updateRealtimeStatus('notifications', 'live', { connectionId, lastUpdated: new Date().toISOString() })
    captureEvent('signalr_reconnected', { channel: 'notifications' })
  })
  connection.onclose((error) => {
    clearTimeout(staleTimer)
    updateRealtimeStatus('notifications', 'disconnected', { connectionId: null })
    captureEvent('signalr_disconnected', { channel: 'notifications', reason: error?.name || 'closed' }, error ? 'error' : 'warning')
  })

  updateRealtimeStatus('notifications', 'connecting')
  const activeConnection = connection
  connectionPromise = activeConnection.start()
    .then(() => {
      updateRealtimeStatus('notifications', 'live', { connectionId: activeConnection.connectionId, lastUpdated: new Date().toISOString() })
      captureEvent('signalr_connected', { channel: 'notifications' })
      return activeConnection
    })
    .catch((error) => {
      if (connection === activeConnection) connection = null
      updateRealtimeStatus('notifications', 'disconnected', { connectionId: null })
      captureError(error, 'signalr_connection_failure', { channel: 'notifications' })
      throw error
    })
    .finally(() => { connectionPromise = null })
  return connectionPromise
}

export async function disconnectNotificationHub() {
  const activeConnection = connection
  connection = null
  connectionPromise = null
  clearTimeout(staleTimer)
  staleTimer = null
  if (activeConnection) await activeConnection.stop()
  notificationCallbacks.clear()
  unreadCountCallbacks.clear()
  syncEventCallbacks.clear()
  updateRealtimeStatus('notifications', 'disconnected', { connectionId: null })
}

// Subscribe / unsubscribe
export function onNotification(callback) {
  notificationCallbacks.add(callback)
  return () => notificationCallbacks.delete(callback)
}

export function onUnreadCountChanged(callback) {
  unreadCountCallbacks.add(callback)
  return () => unreadCountCallbacks.delete(callback)
}

export function onSyncEvent(callback) {
  syncEventCallbacks.add(callback)
  return () => syncEventCallbacks.delete(callback)
}

export function onEntityChanged(entityType, callback) {
  const handler = (event) => {
    if (!entityType) {
      callback(event)
      return
    }
    const targetTypes = Array.isArray(entityType) ? entityType : [entityType]
    const matched = targetTypes.some(t => String(t).toLowerCase() === String(event?.aggregateType || '').toLowerCase())
    if (matched) {
      callback(event)
    }
  }
  syncEventCallbacks.add(handler)
  return () => syncEventCallbacks.delete(handler)
}

export function emitLocalEntityChanged(aggregateType, aggregateId, action = 'Upsert') {
  const event = {
    aggregateType,
    aggregateId: String(aggregateId || ''),
    action,
    sourceSystem: 'Local',
    occurredAtUtc: new Date().toISOString()
  }
  syncEventCallbacks.forEach(cb => cb(event))
}

export const onNotificationConnectionState = (callback) => onRealtimeStatus('notifications', callback)
export const getNotificationConnectionState = () => getRealtimeStatus('notifications')

export function getSeverityRank(severity) {
  return SEVERITY_RANK[severity] || SEVERITY_RANK.info
}

export function normalizeNotificationSeverity(notification = {}) {
  if (notification.severity && SEVERITY_RANK[notification.severity]) {
    return notification.severity
  }

  const category = String(notification.category || '').toLowerCase()
  const referenceType = String(notification.referenceType || '').toLowerCase()
  const combinedText = `${notification.title || ''} ${notification.body || ''}`.toLowerCase()

  if (category === 'chat') return 'success'
  if (category === 'approval') return 'caution'
  if (category === 'alarm') {
    if (
      referenceType === 'alarm' &&
      (combinedText.includes('khẩn cấp') ||
        combinedText.includes('uy hiếp') ||
        combinedText.includes('đột nhập') ||
        combinedText.includes('duress') ||
        combinedText.includes('intrusion'))
    ) {
      return 'critical'
    }

    return 'warning'
  }

  return 'info'
}

// REST API endpoints
export function getNotifications(skip = 0, take = 50) {
  return http.get('/notifications', { params: { skip, take } })
}

export function getUnreadCount() {
  return http.get('/notifications/unread-count')
}

export function markNotificationRead(id) {
  return http.post(`/notifications/${id}/read`)
}

export function markAllNotificationsRead() {
  return http.post('/notifications/read-all')
}

// Notification Rules API
export function getNotificationRules() {
  return http.get('/notification-rules')
}

export function createNotificationRule(rule) {
  return http.post('/notification-rules', rule)
}

export function updateNotificationRule(id, rule) {
  return http.put(`/notification-rules/${id}`, rule)
}

export function deleteNotificationRule(id) {
  return http.delete(`/notification-rules/${id}`)
}

export function getRuleSuggestions(role) {
  return http.get('/notification-rules/suggestions', { params: { role } })
}
