import http from "./http"

const BASE = "/FaceAccessPolicyComparisons"

export async function getFacePolicyComparisons(params = {}) {
  return (await http.get(BASE, { params })).data
}

export async function getFacePolicyComparisonSummary(params = {}) {
  return (await http.get(`${BASE}/summary`, { params })).data
}
