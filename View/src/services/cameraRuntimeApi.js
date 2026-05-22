import axios from 'axios'
import { API_BASE_URL } from '../config/api'

const cameraRuntimeApiClient = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        'Content-Type': 'application/json',
    },
    timeout: 15000,
})

async function getWithFallback(primaryPath, legacyPath) {
    try {
        return await cameraRuntimeApiClient.get(primaryPath)
    } catch (error) {
        if (error?.response?.status === 404) {
            return cameraRuntimeApiClient.get(legacyPath)
        }
        throw error
    }
}

async function postWithFallback(primaryPath, legacyPath, data) {
    try {
        return await cameraRuntimeApiClient.post(primaryPath, data)
    } catch (error) {
        if (error?.response?.status === 404) {
            return cameraRuntimeApiClient.post(legacyPath, data)
        }
        throw error
    }
}

async function putWithFallback(primaryPath, legacyPath, data) {
    try {
        return await cameraRuntimeApiClient.put(primaryPath, data)
    } catch (error) {
        if (error?.response?.status === 404) {
            return cameraRuntimeApiClient.put(legacyPath, data)
        }
        throw error
    }
}

async function deleteWithFallback(primaryPath, legacyPath) {
    try {
        return await cameraRuntimeApiClient.delete(primaryPath)
    } catch (error) {
        if (error?.response?.status === 404) {
            return cameraRuntimeApiClient.delete(legacyPath)
        }
        throw error
    }
}

export async function getCameras() {
    const res = await getWithFallback('/camera-runtime', '/SetCam')
    return res.data
}

export async function getCameraById(id) {
    const res = await getWithFallback(`/camera-runtime/${id}`, `/SetCam/${id}`)
    return res.data
}

export async function createCamera(data) {
    const res = await postWithFallback('/camera-runtime', '/SetCam', data)
    return res.data
}

export async function updateCamera(id, data) {
    const res = await putWithFallback(`/camera-runtime/${id}`, `/SetCam/${id}`, data)
    return res.data
}

export async function deleteCamera(id) {
    const res = await deleteWithFallback(`/camera-runtime/${id}`, `/SetCam/${id}`)
    return res.data
}

export async function reloadGo2rtc() {
    const res = await postWithFallback('/camera-runtime/reload-go2rtc', '/SetCam/reload-go2rtc')
    return res.data
}

export async function stopGo2rtc() {
    const res = await postWithFallback('/camera-runtime/stop-go2rtc', '/SetCam/stop-go2rtc')
    return res.data
}

export async function startPythonQrProcess() {
    const res = await postWithFallback('/camera-runtime/start-python-qr', '/SetCam/start-python-qr')
    return res.data
}

export async function stopPythonQrProcess() {
    const res = await postWithFallback('/camera-runtime/stop-python-qr', '/SetCam/stop-python-qr')
    return res.data
}

export async function startPythonPlateProcess() {
    const res = await postWithFallback('/camera-runtime/start-python-plate', '/SetCam/start-python-plate')
    return res.data
}

export async function stopPythonPlateProcess() {
    const res = await postWithFallback('/camera-runtime/stop-python-plate', '/SetCam/stop-python-plate')
    return res.data
}

export async function startPythonSimulatedCameraProcess() {
    const res = await postWithFallback('/camera-runtime/start-python-cam-gia-lap', '/SetCam/start-python-cam-gia-lap')
    return res.data
}

export async function stopPythonSimulatedCameraProcess() {
    const res = await postWithFallback('/camera-runtime/stop-python-cam-gia-lap', '/SetCam/stop-python-cam-gia-lap')
    return res.data
}

export async function getPythonProcessStatus() {
    const res = await getWithFallback('/camera-runtime/status-python', '/SetCam/status-python')
    return res.data
}


