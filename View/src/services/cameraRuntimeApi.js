import http from './http'

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


