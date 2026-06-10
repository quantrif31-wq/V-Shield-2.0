import axios from 'axios'
import { API_BASE_URL } from '../config/api'

const http = axios.create({
    baseURL: API_BASE_URL,
})

const AUTH_TOKEN_KEY = 'v_shield_token'
const AUTH_REFRESH_TOKEN_KEY = 'v_shield_refresh_token'
const AUTH_USER_KEY = 'v_shield_user'

http.interceptors.request.use((config) => {
    const token = sessionStorage.getItem(AUTH_TOKEN_KEY) || localStorage.getItem(AUTH_TOKEN_KEY)
    if (token) {
        config.headers.Authorization = `Bearer ${token}`
    }
    return config
})

http.interceptors.response.use(
    (response) => response,
    async (error) => {
        const requestUrl = String(error.config?.url || '').toLowerCase()
        const isLoginRequest = requestUrl.includes('/auth/login')
        const isRefreshRequest = requestUrl.includes('/auth/refresh')
        const originalRequest = error.config || {}

        if (error.response && error.response.status === 401 && !isLoginRequest && !isRefreshRequest && !originalRequest._retry) {
            const refreshToken = sessionStorage.getItem(AUTH_REFRESH_TOKEN_KEY) || localStorage.getItem(AUTH_REFRESH_TOKEN_KEY)
            if (refreshToken) {
                try {
                    originalRequest._retry = true
                    const refreshResponse = await axios.post(`${API_BASE_URL}/Auth/refresh`, { refreshToken })
                    const nextToken = refreshResponse.data.token
                    const nextRefreshToken = refreshResponse.data.refreshToken

                    sessionStorage.setItem(AUTH_TOKEN_KEY, nextToken)
                    sessionStorage.setItem(AUTH_REFRESH_TOKEN_KEY, nextRefreshToken)
                    localStorage.removeItem(AUTH_TOKEN_KEY)
                    localStorage.removeItem(AUTH_REFRESH_TOKEN_KEY)

                    originalRequest.headers = originalRequest.headers || {}
                    originalRequest.headers.Authorization = `Bearer ${nextToken}`
                    return http(originalRequest)
                } catch {
                    // fall through to clearing local session
                }
            }

            sessionStorage.removeItem(AUTH_TOKEN_KEY)
            sessionStorage.removeItem(AUTH_USER_KEY)
            sessionStorage.removeItem(AUTH_REFRESH_TOKEN_KEY)
            localStorage.removeItem(AUTH_TOKEN_KEY)
            localStorage.removeItem(AUTH_USER_KEY)
            localStorage.removeItem(AUTH_REFRESH_TOKEN_KEY)
            window.location.href = '/login'
        }
        return Promise.reject(error)
    }
)

export default http
