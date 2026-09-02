import axios from "axios"
import { PLATE_API_BASE_URL } from "../config/api"

const DEFAULT_TIMEOUT_MS = 15000
const LOOPBACK_PROBE_TIMEOUT_MS = 1200
const PLATE_API_OVERRIDE_STORAGE_KEY = "vshield-plate-api-override"
const LOOPBACK_PLATE_API_BASE_URLS = [
  "http://127.0.0.1:5002/api",
  "http://localhost:5002/api"
]

let activePlateApiBaseUrl = ""

const trimTrailingSlash = (value = "") => String(value || "").replace(/\/+$/, "")

const isBrowser = () => typeof window !== "undefined"

const isLoopbackHostname = (hostname = "") => {
  const normalizedHostname = String(hostname || "").trim().toLowerCase()
  return (
    normalizedHostname === "localhost" ||
    normalizedHostname === "127.0.0.1" ||
    normalizedHostname === "::1" ||
    normalizedHostname === "[::1]"
  )
}

const isLoopbackBaseUrl = (value) => {
  try {
    const parsedUrl = new URL(value)
    return isLoopbackHostname(parsedUrl.hostname)
  } catch {
    return false
  }
}

const shouldPreferLocalLoopback = () => {
  if (!isBrowser()) return false
  return !isLoopbackHostname(window.location.hostname)
}

const readPlateApiOverride = () => {
  if (!isBrowser()) return ""
  return trimTrailingSlash(
    window.localStorage.getItem(PLATE_API_OVERRIDE_STORAGE_KEY) || ""
  )
}

const getCandidateBaseUrls = () => {
  const orderedBaseUrls = []
  const seenBaseUrls = new Set()

  const addBaseUrl = (value) => {
    const normalizedValue = trimTrailingSlash(value)
    if (!normalizedValue || seenBaseUrls.has(normalizedValue)) {
      return
    }

    seenBaseUrls.add(normalizedValue)
    orderedBaseUrls.push(normalizedValue)
  }

  addBaseUrl(activePlateApiBaseUrl)
  addBaseUrl(readPlateApiOverride())

  if (shouldPreferLocalLoopback()) {
    LOOPBACK_PLATE_API_BASE_URLS.forEach(addBaseUrl)
  }

  addBaseUrl(PLATE_API_BASE_URL)

  return orderedBaseUrls
}

const getTimeoutForBaseUrl = (baseUrl) => {
  const normalizedBaseUrl = trimTrailingSlash(baseUrl)
  const normalizedConfiguredBaseUrl = trimTrailingSlash(PLATE_API_BASE_URL)

  if (normalizedBaseUrl === normalizedConfiguredBaseUrl) {
    return DEFAULT_TIMEOUT_MS
  }

  return isLoopbackBaseUrl(normalizedBaseUrl)
    ? LOOPBACK_PROBE_TIMEOUT_MS
    : DEFAULT_TIMEOUT_MS
}

const normalizeError = (error, attemptedBaseUrls = []) => {
  if (error?.response?.data) {
    return error.response.data
  }

  const attemptedBaseUrlText = attemptedBaseUrls.filter(Boolean).join(" -> ")

  return {
    success: false,
    message:
      error?.message ||
      (attemptedBaseUrlText
        ? `Không kết nối được dịch vụ biển số (${attemptedBaseUrlText}).`
        : "Network error")
  }
}

async function requestWithBaseUrlFallback(config) {
  const candidateBaseUrls = getCandidateBaseUrls()
  const attemptedBaseUrls = []

  for (const baseURL of candidateBaseUrls) {
    attemptedBaseUrls.push(baseURL)

    try {
      const response = await axios({
        ...config,
        baseURL,
        timeout: getTimeoutForBaseUrl(baseURL)
      })

      activePlateApiBaseUrl = baseURL
      return response.data
    } catch (error) {
      if (error?.response) {
        activePlateApiBaseUrl = baseURL
        throw normalizeError(error, attemptedBaseUrls)
      }

      if (baseURL === candidateBaseUrls[candidateBaseUrls.length - 1]) {
        throw normalizeError(error, attemptedBaseUrls)
      }
    }
  }

  throw normalizeError(null, attemptedBaseUrls)
}

export async function turnOnCamera(ip) {
  const cleanIp = String(ip || "").trim()
  if (!cleanIp) {
    return { success: false, message: "Chưa cấu hình URL camera biển số" }
  }
  return requestWithBaseUrlFallback({
    method: "post",
    url: "/camera/on",
    data: { ip: cleanIp }
  })
}

export async function turnOffCamera() {
  return requestWithBaseUrlFallback({
    method: "post",
    url: "/camera/off"
  })
}

export async function resetCameraState() {
  return requestWithBaseUrlFallback({
    method: "post",
    url: "/camera/reset"
  })
}

export async function getCameraStatus() {
  return requestWithBaseUrlFallback({
    method: "get",
    url: "/camera/status"
  })
}

export async function getCameraResult() {
  return requestWithBaseUrlFallback({
    method: "get",
    url: "/camera/result"
  })
}

export async function getLockedImages() {
  return requestWithBaseUrlFallback({
    method: "get",
    url: "/camera/locked-images"
  })
}

export function getResolvedPlateApiBaseUrl() {
  return activePlateApiBaseUrl || getCandidateBaseUrls()[0] || PLATE_API_BASE_URL
}

// Each physical lane must own a separate runtime endpoint.  Do not share the
// module-level fallback state between them, otherwise a transient lane-1
// failure can silently send lane-2 commands to the wrong camera worker.
export function createPlateCameraApi(baseUrl) {
  const fixedBaseUrl = trimTrailingSlash(baseUrl)
  const request = async (config) => {
    try {
      const response = await axios({ ...config, baseURL: fixedBaseUrl, timeout: DEFAULT_TIMEOUT_MS })
      return response.data
    } catch (error) {
      throw normalizeError(error, [fixedBaseUrl])
    }
  }
  return {
    turnOnCamera: (ip) => request({ method: "post", url: "/camera/on", data: { ip: String(ip || "").trim() } }),
    turnOffCamera: () => request({ method: "post", url: "/camera/off" }),
    resetCameraState: () => request({ method: "post", url: "/camera/reset" }),
    getCameraStatus: () => request({ method: "get", url: "/camera/status" }),
    getCameraResult: () => request({ method: "get", url: "/camera/result" }),
    getLockedImages: () => request({ method: "get", url: "/camera/locked-images" }),
    getResolvedPlateApiBaseUrl: () => fixedBaseUrl,
  }
}
