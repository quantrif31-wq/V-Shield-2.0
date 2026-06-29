import http from './http'
import * as signalR from '@microsoft/signalr'

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000'

let connection = null
let notificationCallbacks = []
let unreadCountCallbacks = []

// SignalR connection
export async function connectNotificationHub(token) {
  if (connection && connection.state === signalR.HubConnectionState.Connected) {
    return connection
  }
  connection = new signalR.HubConnectionBuilder()
    .withUrl(`${API_URL}/hubs/notifications`, {
      accessTokenFactory: () => token
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .build()

  connection.on('ReceiveNotification', (notification) => {
    notificationCallbacks.forEach(cb => cb(notification))
  })

  connection.on('UnreadCountUpdated', (count) => {
    unreadCountCallbacks.forEach(cb => cb(count))
  })

  connection.onreconnecting(() => console.log('NotificationHub reconnecting...'))
  connection.onreconnected(() => console.log('NotificationHub reconnected'))
  connection.onclose(() => console.log('NotificationHub closed'))

  await connection.start()
  return connection
}

export async function disconnectNotificationHub() {
  if (connection) {
    await connection.stop()
    connection = null
  }
}

// Subscribe / unsubscribe
export function onNotification(callback) {
  notificationCallbacks.push(callback)
  return () => { notificationCallbacks = notificationCallbacks.filter(c => c !== callback) }
}

export function onUnreadCountChanged(callback) {
  unreadCountCallbacks.push(callback)
  return () => { unreadCountCallbacks = unreadCountCallbacks.filter(c => c !== callback) }
}

// REST API endpoints
export function getNotifications(skip = 0, take = 50) {
  return http.get('/api/notifications', { params: { skip, take } })
}

export function getUnreadCount() {
  return http.get('/api/notifications/unread-count')
}

export function markNotificationRead(id) {
  return http.post(`/api/notifications/${id}/read`)
}

export function markAllNotificationsRead() {
  return http.post('/api/notifications/read-all')
}

// Notification Rules API
export function getNotificationRules() {
  return http.get('/api/notification-rules')
}

export function createNotificationRule(rule) {
  return http.post('/api/notification-rules', rule)
}

export function updateNotificationRule(id, rule) {
  return http.put(`/api/notification-rules/${id}`, rule)
}

export function deleteNotificationRule(id) {
  return http.delete(`/api/notification-rules/${id}`)
}

export function getRuleSuggestions(role) {
  return http.get('/api/notification-rules/suggestions', { params: { role } })
}
