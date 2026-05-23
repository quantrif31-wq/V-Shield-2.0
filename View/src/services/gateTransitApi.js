import axios from "axios"
import { API_BASE_URL } from "../config/api"

const gateApiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    "Content-Type": "application/json"
  }
})

export function scanGate(payload) {
  return gateApiClient.post("/gate-transit/scan", payload)
}

export function scanGuest(payload) {
  return gateApiClient.post("/gate-transit/scan-guest", payload)
}
