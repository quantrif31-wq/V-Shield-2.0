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

export function getTransitLanes() {
  return http.get("/gate-transit/lanes")
}

export function updateTransitLaneDirection(laneId, direction) {
  return http.patch(`/gate-transit/lanes/${encodeURIComponent(laneId)}/direction`, { direction })
}
