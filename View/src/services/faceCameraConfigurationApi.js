import http from "./http"

const BASE_PATH = "/FaceCameraConfigurations"
const CAMERA_ID_PATTERN = /^[A-Za-z0-9_.-]{1,64}$/

function cameraPath(runtimeCameraId) {
  const value = String(runtimeCameraId || "")
  if (!CAMERA_ID_PATTERN.test(value) || value.includes("..")) {
    throw new Error("Runtime camera ID không hợp lệ.")
  }
  return `${BASE_PATH}/${encodeURIComponent(value)}`
}

export async function getFaceCameraConfigurations() {
  const response = await http.get(BASE_PATH)
  return response.data
}

export async function updateFaceCameraConfiguration(runtimeCameraId, data) {
  const response = await http.put(cameraPath(runtimeCameraId), data)
  return response.data
}

export async function startConfiguredFaceCamera(runtimeCameraId) {
  const response = await http.post(`${cameraPath(runtimeCameraId)}/start`)
  return response.data
}

export async function stopConfiguredFaceCamera(runtimeCameraId) {
  const response = await http.post(`${cameraPath(runtimeCameraId)}/stop`)
  return response.data
}

export async function reconcileFaceCameras() {
  const response = await http.post(`${BASE_PATH}/reconcile`)
  return response.data
}
