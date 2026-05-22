import { getCameras } from "./cameraRuntimeApi"
import {
  isHttpCameraUrl,
  normalizeCameraUrl,
} from "../utils/cameraNetwork"

const toSlotName = (cameraId) => `CAM-${String(cameraId).padStart(2, "0")}`

const mapApiCamera = (item) => {
  const sourceUrl = normalizeCameraUrl(item?.streamUrl || "")
  const urlView = String(item?.urlView || "").trim()
  const previewUrl = isHttpCameraUrl(urlView)
    ? normalizeCameraUrl(urlView)
    : (isHttpCameraUrl(sourceUrl) ? sourceUrl : "")

  return {
    id: Number(item?.cameraId || 0),
    name: toSlotName(item?.cameraId || 0),
    label: String(item?.cameraName || "").trim() || toSlotName(item?.cameraId || 0),
    sourceUrl,
    browserPreviewUrl: previewUrl,
    enabled: Boolean(sourceUrl || previewUrl),
  }
}

const dedupeById = (items) => {
  const seen = new Set()
  return items.filter((item) => {
    const id = Number(item?.id || 0)
    if (!id || seen.has(id)) return false
    seen.add(id)
    return true
  })
}

export const getConfiguredCameras = async () => {
  const apiItems = await getCameras()
  const mapped = Array.isArray(apiItems) ? apiItems.map(mapApiCamera) : []
  return dedupeById(mapped.filter((item) => item.enabled))
}

