import axios from 'axios'
import { API_BASE_URL } from '../config/api'

const authApiClient = axios.create({
    baseURL: `${API_BASE_URL}/Auth`
})

// Tá»± Ä‘á»™ng gáº¯n JWT token vÃ o má»—i request
authApiClient.interceptors.request.use((config) => {
    const token = localStorage.getItem('v_shield_token')
    if (token) {
        config.headers.Authorization = `Bearer ${token}`
    }
    return config
})

// Tá»± Ä‘á»™ng xá»­ lÃ½ 401 â†’ redirect login
authApiClient.interceptors.response.use(
    (response) => response,
    (error) => {
        const requestUrl = String(error.config?.url || '').toLowerCase()
        const isLoginRequest = requestUrl.endsWith('/login') || requestUrl === '/login'

        if (error.response && error.response.status === 401 && !isLoginRequest) {
            localStorage.removeItem('v_shield_token')
            localStorage.removeItem('v_shield_user')
            window.location.href = '/login'
        }
        return Promise.reject(error)
    }
)

/**
 * ÄÄƒng nháº­p
 * @param {string} username
 * @param {string} password
 * @returns {Promise<{token, username, fullName, role, expiresAt}>}
 */
export const login = (username, password) => {
    return authApiClient.post('/login', { username, password })
}

/**
 * Láº¥y thÃ´ng tin user Ä‘ang Ä‘Äƒng nháº­p
 * @returns {Promise<{userId, username, fullName, role, isActive, createdAt}>}
 */
export const getMe = () => {
    return authApiClient.get('/me')
}

