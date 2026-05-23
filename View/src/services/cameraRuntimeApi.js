import axios from 'axios'
import { API_BASE_URL } from '../config/api'

const cameraRuntimeApiClient = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        'Content-Type': 'application/json',
    },
    timeout: 15000,
})

export async function getCameras() {
    const res = await cameraRuntimeApiClient.get('/camera-runtime')
    return res.data
}

export async function getCameraById(id) {
    const res = await cameraRuntimeApiClient.get(`/camera-runtime/${id}`)
    return res.data
}

export async function createCamera(data) {
    const res = await cameraRuntimeApiClient.post('/camera-runtime', data)
    return res.data
}

export async function updateCamera(id, data) {
    const res = await cameraRuntimeApiClient.put(`/camera-runtime/${id}`, data)
    return res.data
}

export async function deleteCamera(id) {
    const res = await cameraRuntimeApiClient.delete(`/camera-runtime/${id}`)
    return res.data
}

export async function reloadGo2rtc() {
    const res = await cameraRuntimeApiClient.post('/camera-runtime/reload-go2rtc')
    return res.data
}

export async function stopGo2rtc() {
    const res = await cameraRuntimeApiClient.post('/camera-runtime/stop-go2rtc')
    return res.data
}

export async function startPythonQrProcess() {
    const res = await cameraRuntimeApiClient.post('/camera-runtime/start-python-qr')
    return res.data
}

export async function stopPythonQrProcess() {
    const res = await cameraRuntimeApiClient.post('/camera-runtime/stop-python-qr')
    return res.data
}

export async function startPythonPlateProcess() {
    const res = await cameraRuntimeApiClient.post('/camera-runtime/start-python-plate')
    return res.data
}

export async function stopPythonPlateProcess() {
    const res = await cameraRuntimeApiClient.post('/camera-runtime/stop-python-plate')
    return res.data
}

export async function startPythonSimulatedCameraProcess() {
    const res = await cameraRuntimeApiClient.post('/camera-runtime/start-python-cam-gia-lap')
    return res.data
}

export async function stopPythonSimulatedCameraProcess() {
    const res = await cameraRuntimeApiClient.post('/camera-runtime/stop-python-cam-gia-lap')
    return res.data
}

export async function getPythonProcessStatus() {
    const res = await cameraRuntimeApiClient.get('/camera-runtime/status-python')
    return res.data
}


