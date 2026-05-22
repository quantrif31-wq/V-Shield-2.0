import axios from 'axios'
import { API_BASE_URL } from '../config/api'

const api = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        'Content-Type': 'application/json',
    },
    timeout: 15000,
})

async function postWithFallback(primaryPath, legacyPath, payload) {
    try {
        return await api.post(primaryPath, payload)
    } catch (error) {
        if (error?.response?.status === 404) {
            return api.post(legacyPath, payload)
        }
        throw error
    }
}

export async function verifyDynamicQr(qrPayload, scannerDevice = 'WEB_SCANNER') {
    const response = await postWithFallback('/dynamic-qr/verify', '/QR_Dong/verify', {
        qrPayload,
        scannerDevice,
    })
    return response.data
}

export default api
