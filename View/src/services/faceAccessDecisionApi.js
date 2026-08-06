import http from "./http"

const BASE = "/FaceAccessDecisions"

export async function getFaceAccessDecisions(params = {}) {
  return (await http.get(BASE, { params })).data
}

export async function getFaceAccessDecisionSummary(params = {}) {
  return (await http.get(`${BASE}/summary`, { params })).data
}
