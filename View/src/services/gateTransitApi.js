import http from "./http"

export function scanGate(payload) {
  return http.post("/gate-transit/scan", payload)
}

export function scanGuest(payload) {
  return http.post("/gate-transit/scan-guest", payload)
}
