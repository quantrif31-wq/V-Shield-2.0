import http from './http'

const fallback = [
  { urls: ['stun:stun.l.google.com:19302', 'stun:stun.cloudflare.com:3478'] },
]

let cached = null
let expiresAt = 0
let pending = null

export async function getIceServers() {
  if (cached && Date.now() < expiresAt) return cached
  if (pending) return pending
  pending = http.get('/realtime/ice-configuration')
    .then(({ data }) => Array.isArray(data?.iceServers) && data.iceServers.length ? data.iceServers : fallback)
    .catch(() => fallback)
    .then((servers) => {
      cached = servers
      // TURN credentials are short-lived. Refresh well before the default
      // one-hour expiry while avoiding an HTTP call for every camera tile.
      expiresAt = Date.now() + 45 * 60 * 1000
      return servers
    })
    .finally(() => { pending = null })
  return pending
}
