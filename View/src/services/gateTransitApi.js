import http from "./http"

export function scanGate(payload) {
  return http.post("/gate-transit/scan", payload)
}

export function scanGuest(payload) {
  return http.post("/gate-transit/scan-guest", payload)
}

export function getManualSubject(code) {
  return http.get(`/gate-transit/manual-subject/${encodeURIComponent(code)}`)
}

export function getManualGates() {
  return http.get("/gate-transit/gates")
}
