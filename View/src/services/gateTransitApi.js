import axios from "axios"
import { API_BASE_URL } from "../config/api"

const gateApiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    "Content-Type": "application/json"
  }
})

async function postWithFallback(primaryPath, legacyPath, payload) {
  try {
    return await gateApiClient.post(primaryPath, payload)
  } catch (error) {
    if (error?.response?.status === 404) {
      return gateApiClient.post(legacyPath, payload)
    }
    throw error
  }
}

export function scanGate(payload) {
  return postWithFallback("/gate-transit/scan", "/Gate/scan", payload)
}

export function scanGuest(payload) {
  return postWithFallback("/gate-transit/scan-guest", "/Gate/scan-guest", payload)
}
