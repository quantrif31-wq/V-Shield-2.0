import http from './http'
import { normalizeCameraUrl } from "../utils/cameraNetwork"

export async function getCameras() {
    const res = await http.get('/camera-runtime')
    return res.data
}

export async function getCameraById(id) {
    const res = await http.get(`/camera-runtime/${id}`)
    return res.data
}

export async function createCamera(data) {
    const res = await http.post('/camera-runtime', data)
    return res.data
}

export async function updateCamera(id, data) {
    const res = await http.put(`/camera-runtime/${id}`, data)
    return res.data
}

export async function deleteCamera(id) {
    const res = await http.delete(`/camera-runtime/${id}`)
    return res.data
}

export async function reloadGo2rtc() {
    const res = await http.post('/camera-runtime/reload-go2rtc')
    return res.data
}

export async function stopGo2rtc() {
    const res = await http.post('/camera-runtime/stop-go2rtc')
    return res.data
}

export async function startPythonQrProcess() {
    const res = await http.post('/camera-runtime/start-python-qr')
    return res.data
}

export async function stopPythonQrProcess() {
    const res = await http.post('/camera-runtime/stop-python-qr')
    return res.data
}

export async function startPythonPlateProcess() {
    const res = await http.post('/camera-runtime/start-python-plate')
    return res.data
}

export async function stopPythonPlateProcess() {
    const res = await http.post('/camera-runtime/stop-python-plate')
    return res.data
}

export async function startPythonSimulatedCameraProcess() {
    const res = await http.post('/camera-runtime/start-python-cam-gia-lap')
    return res.data
}

export async function stopPythonSimulatedCameraProcess() {
    const res = await http.post('/camera-runtime/stop-python-cam-gia-lap')
    return res.data
}

export async function getPythonProcessStatus() {
    const res = await http.get('/camera-runtime/status-python')
    return res.data
}

export async function toggleRecording(cameraId, enabled, retentionDays) {
    const res = await http.put(`/camera-runtime/${cameraId}/recording`, { enabled, retentionDays })
    return res.data
}

export async function getRecordedSegments(cameraId, params) {
    const res = await http.get(`/camera-runtime/${cameraId}/recorded-segments`, { params })
    return res.data
}

export async function getArchiveSegments(params) {
    const res = await http.get('/camera-runtime/archive/segments', { params })
    return res.data
}

function normalizeComparableUrl(url) {
    return normalizeCameraUrl(url || "").trim().toLowerCase()
}

function isSameCameraUrl(left, right) {
    const normalizedLeft = normalizeComparableUrl(left)
    const normalizedRight = normalizeComparableUrl(right)
    return !!normalizedLeft && normalizedLeft === normalizedRight
}

export async function ensureCameraRegistered({
    cameraName,
    cameraType,
    gateId = null,
    streamUrl,
    previewUrl,
    recordingRetentionDays = 30,
}) {
    const normalizedStreamUrl = normalizeCameraUrl(streamUrl || previewUrl || "")
    const normalizedPreviewUrl = normalizeCameraUrl(previewUrl || "")

    if (!normalizedStreamUrl) {
        return null
    }

    const cameras = await getCameras()
    const existing = (Array.isArray(cameras) ? cameras : []).find((camera) =>
        isSameCameraUrl(camera?.streamUrl, normalizedStreamUrl) ||
        isSameCameraUrl(camera?.urlView, normalizedStreamUrl) ||
        (normalizedPreviewUrl && (
            isSameCameraUrl(camera?.streamUrl, normalizedPreviewUrl) ||
            isSameCameraUrl(camera?.urlView, normalizedPreviewUrl)
        ))
    )

    const fallbackName = (cameraName || "Network Camera").trim() || "Network Camera"
    const fallbackType = (cameraType || "Network").trim() || "Network"

    if (!existing) {
        const created = await createCamera({
            cameraName: fallbackName,
            gateId,
            cameraType: fallbackType,
            streamUrl: normalizedStreamUrl,
            isRecordingEnabled: true,
            recordingRetentionDays,
        })
        await reloadGo2rtc().catch(() => {})
        return created
    }

    const nextCameraName = String(existing.cameraName || "").trim() || fallbackName
    const nextCameraType = String(existing.cameraType || "").trim() || fallbackType
    const nextGateId = existing.gateId ?? gateId ?? null
    const shouldUpdate =
        !isSameCameraUrl(existing.streamUrl, normalizedStreamUrl) ||
        String(existing.cameraName || "").trim() !== nextCameraName ||
        String(existing.cameraType || "").trim() !== nextCameraType ||
        (existing.gateId ?? null) !== nextGateId ||
        existing.isRecordingEnabled !== true ||
        Number(existing.recordingRetentionDays || 0) !== Number(recordingRetentionDays)

    if (!shouldUpdate) {
        return existing
    }

    const updated = await updateCamera(existing.cameraId, {
        cameraName: nextCameraName,
        gateId: nextGateId,
        cameraType: nextCameraType,
        streamUrl: normalizedStreamUrl,
        isRecordingEnabled: true,
        recordingRetentionDays,
    })
    await reloadGo2rtc().catch(() => {})
    return updated
}


