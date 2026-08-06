const channels = new Map()
const listeners = new Map()

function defaultState(channel) {
  return { channel, status: 'disconnected', lastUpdated: null, connectionId: null }
}

export function updateRealtimeStatus(channel, status, details = {}) {
  const previous = channels.get(channel) || defaultState(channel)
  const next = {
    ...previous,
    ...details,
    channel,
    status,
    lastUpdated: details.lastUpdated || previous.lastUpdated,
  }
  channels.set(channel, next)
  for (const listener of listeners.get(channel) || []) listener({ ...next })
  return next
}

export function markRealtimeUpdated(channel, details = {}) {
  return updateRealtimeStatus(channel, channels.get(channel)?.status || 'live', {
    ...details,
    lastUpdated: new Date().toISOString(),
  })
}

export function getRealtimeStatus(channel) {
  return { ...(channels.get(channel) || defaultState(channel)) }
}

export function onRealtimeStatus(channel, listener) {
  const channelListeners = listeners.get(channel) || new Set()
  channelListeners.add(listener)
  listeners.set(channel, channelListeners)
  listener(getRealtimeStatus(channel))
  return () => {
    channelListeners.delete(listener)
    if (!channelListeners.size) listeners.delete(channel)
  }
}

export function resetRealtimeStatusForTests() {
  channels.clear()
  listeners.clear()
}
