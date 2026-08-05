import axios from 'axios'
import { API_BASE_URL } from '../config/api'
import { captureApiFailure, recordMetric } from './observability'

const http = axios.create({
    baseURL: API_BASE_URL,
})

const AUTH_TOKEN_KEY = 'v_shield_token'
const AUTH_REFRESH_TOKEN_KEY = 'v_shield_refresh_token'
const AUTH_USER_KEY = 'v_shield_user'
let refreshPromise = null
let redirectingToLogin = false

function clearLocalAuthState() {
    sessionStorage.removeItem(AUTH_TOKEN_KEY)
    sessionStorage.removeItem(AUTH_USER_KEY)
    sessionStorage.removeItem(AUTH_REFRESH_TOKEN_KEY)
    localStorage.removeItem(AUTH_TOKEN_KEY)
    localStorage.removeItem(AUTH_USER_KEY)
    localStorage.removeItem(AUTH_REFRESH_TOKEN_KEY)
}

function redirectToLoginOnce() {
    if (redirectingToLogin) {
        return
    }

    redirectingToLogin = true
    clearLocalAuthState()

    if (window.location.pathname !== '/login') {
        window.location.replace('/login')
        return
    }

    window.setTimeout(() => {
        redirectingToLogin = false
    }, 1200)
}

http.interceptors.request.use((config) => {
    config.metadata = { ...(config.metadata || {}), observabilityStartedAt: performance.now() }
    const token = sessionStorage.getItem(AUTH_TOKEN_KEY) || localStorage.getItem(AUTH_TOKEN_KEY)
    if (token) {
        config.headers.Authorization = `Bearer ${token}`
    }
    return config
})

http.interceptors.response.use(
    (response) => {
        const startedAt = response.config?.metadata?.observabilityStartedAt
        if (Number.isFinite(startedAt)) {
            recordMetric('api_request', performance.now() - startedAt, {
                method: String(response.config?.method || 'GET').toUpperCase(),
                path: String(response.config?.url || '').split('?')[0],
                httpStatus: response.status,
                correlationId: response.headers?.['x-correlation-id'] || response.headers?.['trace-id'] || undefined,
            })
        }
        return response
    },
    async (error) => {
        captureApiFailure(error)
        const requestUrl = String(error.config?.url || '').toLowerCase()
        const isLoginRequest = requestUrl.includes('/auth/login')
        const isRefreshRequest = requestUrl.includes('/auth/refresh')
        const originalRequest = error.config || {}
        const status = error.response?.status

        if (status === 401 && !isLoginRequest && !isRefreshRequest && !originalRequest._retry) {
            const refreshToken = sessionStorage.getItem(AUTH_REFRESH_TOKEN_KEY) || localStorage.getItem(AUTH_REFRESH_TOKEN_KEY)
            if (refreshToken) {
                try {
                    originalRequest._retry = true
                    if (!refreshPromise) {
                        refreshPromise = axios
                            .post(`${API_BASE_URL}/Auth/refresh`, { refreshToken })
                            .finally(() => {
                                refreshPromise = null
                            })
                    }

                    const refreshResponse = await refreshPromise
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
                    clearLocalAuthState()
                }
            }

            redirectToLoginOnce()
        }
        return Promise.reject(error)
    }
)

export default http
