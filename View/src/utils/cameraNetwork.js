const HTTP_CAMERA_PROTOCOL_REGEX = /^https?:\/\//i
const RTSP_CAMERA_PROTOCOL_REGEX = /^rtsp:\/\//i
const HLS_CAMERA_PATH_REGEX = /\.m3u8($|[?#])/i
const BROWSER_VIDEO_PATH_REGEX = /\.(mp4|webm|ogg)($|[?#])/i
const STREAM_PREVIEW_PATHS = new Set(["/video", "/videofeed"])

export const isHttpCameraUrl = (url) => HTTP_CAMERA_PROTOCOL_REGEX.test(url || "")
export const isRtspCameraUrl = (url) => RTSP_CAMERA_PROTOCOL_REGEX.test(url || "")
export const isHlsCameraUrl = (url) => {
  const value = (url || "").trim()
  return isHttpCameraUrl(value) && HLS_CAMERA_PATH_REGEX.test(value)
}
export const isBrowserVideoCameraUrl = (url) => {
  const value = (url || "").trim()
  return isHttpCameraUrl(value) && BROWSER_VIDEO_PATH_REGEX.test(value)
}
export const looksLikeHostInput = (value) => /^[\w.-]+(?::\d+)?(?:\/.*)?$/i.test((value || "").trim())

export const normalizeCameraUrl = (rawValue) => {
  let value = (rawValue || "").trim()
  if (!value) return ""
  value = value.replace(/^\/+/, "")
  if (!/^[a-zA-Z][a-zA-Z0-9+.-]*:\/\//.test(value) && looksLikeHostInput(value)) value = `http://${value}`

  try {
    const parsedUrl = new URL(value)
    if ((parsedUrl.protocol === "http:" || parsedUrl.protocol === "https:") && (!parsedUrl.pathname || parsedUrl.pathname === "/")) {
      if (parsedUrl.port === "8081") parsedUrl.pathname = "/video"
      else if (parsedUrl.port === "8080") parsedUrl.pathname = "/videofeed"
    }
    return parsedUrl.toString()
  } catch {
    return (rawValue || "").trim()
  }
}

export const isKnownStreamPreviewUrl = (url) => {
  if (!isHttpCameraUrl(url)) return false
  try {
    const parsedUrl = new URL(normalizeCameraUrl(url))
    return STREAM_PREVIEW_PATHS.has(parsedUrl.pathname.toLowerCase())
  } catch {
    return false
  }
}

export const buildCameraHealthProbeUrl = (url) => {
  if (!isHttpCameraUrl(url)) return ""
  try {
    const parsedUrl = new URL(normalizeCameraUrl(url))
    if (!STREAM_PREVIEW_PATHS.has(parsedUrl.pathname.toLowerCase())) return parsedUrl.toString()
    parsedUrl.pathname = "/"
    parsedUrl.search = ""
    parsedUrl.hash = ""
    return parsedUrl.toString()
  } catch {
    return (url || "").trim()
  }
}

export const shouldAppendPreviewCacheBust = (url) => isHttpCameraUrl(url) && !isKnownStreamPreviewUrl(url)
export const resolveCameraPreviewUrl = (camera) => camera?.previewUrl?.trim() || camera?.url?.trim() || ""
export const resolveCameraSourceUrl = (camera) => camera?.url?.trim() || camera?.previewUrl?.trim() || ""

export const extractCameraDisplayParts = (cameraLike, fallbackIndex = 1) => {
  const fallbackSlotName = `CAM-${String(fallbackIndex).padStart(2, "0")}`
  if (cameraLike && typeof cameraLike === "object") {
    return { slotName: cameraLike.name?.trim() || fallbackSlotName, sourceName: cameraLike.label?.trim() || "" }
  }
  const name = String(cameraLike || "").trim()
  return { slotName: name || fallbackSlotName, sourceName: "" }
}
