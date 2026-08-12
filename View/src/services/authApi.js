import http from './http'

export const login = (username, password, mfaCode = null) => {
    return http.post('/Auth/login', { username, password, mfaCode })
}

export const getMe = () => {
    return http.get('/Auth/me')
}

export const refreshSession = (refreshToken) => {
    return http.post('/Auth/refresh', { refreshToken })
}

export const logoutApi = (refreshToken = null) => {
    return http.post('/Auth/logout', { refreshToken })
}

export const changePassword = (currentPassword, newPassword) => {
    return http.post('/Auth/change-password', { currentPassword, newPassword })
}
