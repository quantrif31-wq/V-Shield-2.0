import http from './http'
import * as signalR from '@microsoft/signalr'
import { API_ORIGIN } from '../config/api'

const API_URL = import.meta.env.VITE_API_URL || API_ORIGIN

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

  connection.on('NewNotification', (notification) => {
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
