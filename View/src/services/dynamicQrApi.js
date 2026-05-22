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

export async function generateDynamicQr(employeeId) {
    const response = await postWithFallback('/dynamic-qr/generate', '/QR_Dong/generate', {
        employeeId: Number(employeeId),
    })
    return response.data
}

export default api
