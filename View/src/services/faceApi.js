import axios from "axios"
import http from "./http"

export const FACE_API_ERROR_CODES = Object.freeze({
  CANCELLED: "cancelled",
  SESSION_EXPIRED: "session-expired",
  FORBIDDEN: "forbidden",
  VALIDATION: "validation",
  RELOAD_IN_PROGRESS: "reload-in-progress",
  MODEL_REJECTED: "model-rejected",
  SERVER_ERROR: "server-error",
  RUNTIME_UNAVAILABLE: "runtime-unavailable",
  BACKEND_UNREACHABLE: "backend-unreachable",
  REQUEST_FAILED: "request-failed"
})

const FACE_CAMERA_BASE_PATH = "/FaceCamera"
const FACE_CAMERAS_PATH = "/FaceCamera/cameras"
const CAMERA_ID_PATTERN = /^[A-Za-z0-9_.-]{1,64}$/

function encodeCameraId(cameraId) {
  const value = String(cameraId || "")
  if (!CAMERA_ID_PATTERN.test(value) || value.includes("..")) {
    throw normalizeFaceApiError({
      response: {
        status: 400,
        data: { message: "cameraId không hợp lệ." }
      }
    })
  }
  return encodeURIComponent(value)
}

const safeResponseMessage = (error) => {
  const message = error?.response?.data?.message
  return typeof message === "string" && message.trim() ? message.trim() : ""
}

export function normalizeFaceApiError(error) {
  if (error?.isFaceApiError) {
    return error
  }

  const status = error?.response?.status ?? null
  const cancelled = axios.isCancel(error) || error?.code === "ERR_CANCELED"
  let code = FACE_API_ERROR_CODES.REQUEST_FAILED
  let message = "Không thể xử lý yêu cầu Face ID."

  if (cancelled) {
    code = FACE_API_ERROR_CODES.CANCELLED
    message = "Yêu cầu Face ID đã được hủy."
  } else if (status === 401) {
    code = FACE_API_ERROR_CODES.SESSION_EXPIRED
    message = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại."
  } else if (status === 403) {
    code = FACE_API_ERROR_CODES.FORBIDDEN
    message = "Bạn không có quyền giám sát Face ID."
  } else if (status === 400) {
    code = FACE_API_ERROR_CODES.VALIDATION
    message = safeResponseMessage(error) || "Yêu cầu Face ID không hợp lệ."
  } else if (status === 409) {
    code = FACE_API_ERROR_CODES.RELOAD_IN_PROGRESS
    message = "Model Face ID đang được tải lại. Vui lòng thử lại sau."
  } else if (status === 422) {
    code = FACE_API_ERROR_CODES.MODEL_REJECTED
    message = safeResponseMessage(error) || "Model Face ID không hợp lệ nên bị từ chối."
  } else if (status === 500) {
    code = FACE_API_ERROR_CODES.SERVER_ERROR
    message = "Hệ thống Face ID gặp lỗi. Vui lòng thử lại sau."
  } else if (status === 503) {
    code = FACE_API_ERROR_CODES.RUNTIME_UNAVAILABLE
    message = "Face ID unavailable"
  } else if (!error?.response) {
    code = FACE_API_ERROR_CODES.BACKEND_UNREACHABLE
    message = "Không thể kết nối đến máy chủ V-Shield."
  }

  const normalized = new Error(message)
  normalized.name = "FaceApiError"
  normalized.isFaceApiError = true
  normalized.code = code
  normalized.status = status
  normalized.cancelled = cancelled
  normalized.details = status === 422 ? error?.response?.data : null
  return normalized
}

export function shouldStopFacePolling(error) {
  const normalized = normalizeFaceApiError(error)
  return normalized.code === FACE_API_ERROR_CODES.SESSION_EXPIRED ||
    normalized.code === FACE_API_ERROR_CODES.FORBIDDEN
}

async function faceRequest(config) {
  try {
    const response = await http.request(config)
    return response.data
  } catch (error) {
    throw normalizeFaceApiError(error)
  }
}

export function turnOnCamera(ip) {
  return startCamera("default", ip)
}

export function turnOffCamera() {
  return stopCamera("default")
}

export function resetCameraState() {
  return resetCamera("default")
}

export function getCameras() {
  return faceRequest({
    method: "get",
    url: FACE_CAMERAS_PATH
  })
}

export async function startCamera(cameraId, ip, laneId = null) {
  const data = { ip }
  if (laneId !== null && laneId !== undefined) {
    data.laneId = laneId
  }
  try {
    return await faceRequest({
      method: "post",
      url: `${FACE_CAMERAS_PATH}/${encodeCameraId(cameraId)}/start`,
      data
    })
  } catch (error) {
    if (error?.status === 409) {
      // If camera conflict occurred (e.g. stream URL changed while session active),
      // stop old session and seamlessly retry start with the new stream URL
      try {
        await stopCamera(cameraId)
        return await faceRequest({
          method: "post",
          url: `${FACE_CAMERAS_PATH}/${encodeCameraId(cameraId)}/start`,
          data
        })
      } catch {
        throw error
      }
    }
    throw error
  }
}

export function stopCamera(cameraId) {
  return faceRequest({
    method: "post",
    url: `${FACE_CAMERAS_PATH}/${encodeCameraId(cameraId)}/stop`
  })
}

export function resetCamera(cameraId) {
  return faceRequest({
    method: "post",
    url: `${FACE_CAMERAS_PATH}/${encodeCameraId(cameraId)}/reset`
  })
}

export function getCameraStatus(cameraId = "default") {
  return faceRequest({
    method: "get",
    url: `${FACE_CAMERAS_PATH}/${encodeCameraId(cameraId)}/status`
  })
}

export function getCameraResult(cameraId = "default") {
  return faceRequest({
    method: "get",
    url: `${FACE_CAMERAS_PATH}/${encodeCameraId(cameraId)}/result`
  })
}

export function getLockedImages(cameraId = "default") {
  return faceRequest({
    method: "get",
    url: `${FACE_CAMERAS_PATH}/${encodeCameraId(cameraId)}/locked-images`
  })
}

export function getModels() {
  return faceRequest({
    method: "get",
    url: `${FACE_CAMERA_BASE_PATH}/models`
  })
}

export function discoverIpWebcams() {
  return faceRequest({
    method: "get",
    url: `${FACE_CAMERA_BASE_PATH}/discover-ipwebcam`
  })
}

export function reloadModels() {
  return faceRequest({
    method: "post",
    url: `${FACE_CAMERA_BASE_PATH}/models/reload`
  })
}

export function liveEnroll(subjectId, images) {
  return faceRequest({
    method: "post",
    url: `${FACE_CAMERA_BASE_PATH}/enroll-live`,
    data: { subjectId, images }
  })
}
