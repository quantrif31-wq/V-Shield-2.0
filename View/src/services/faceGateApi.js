import http from "./http"

const BASE = "/face-gate"

export async function getFaceGates() {
  const res = await http.get(`${BASE}/gates`)
  return res.data
}

export async function verifyFaceGatePassword(password) {
  const res = await http.post(`${BASE}/verify-password`, { password })
  return res.data
}

export async function checkGateAccess(employeeId, gateId) {
  const res = await http.get(`${BASE}/check-access`, {
    params: { employeeId, gateId }
  })
  return res.data
}

export async function recordFaceGateResult(payload) {
  const res = await http.post(`${BASE}/record`, payload)
  return res.data
}

export async function getFaceIntruders(params = {}) {
  const res = await http.get(`${BASE}/intruders`, { params })
  return res.data
}

export async function deleteFaceIntruder(id) {
  const res = await http.delete(`${BASE}/intruders/${id}`)
  return res.data
}
