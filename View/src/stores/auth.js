import { reactive } from 'vue'
import { login as loginApi, getMe, logoutApi as logoutApiRequest } from '../services/authApi'

const AUTH_TOKEN_KEY = 'v_shield_token'
const AUTH_USER_KEY = 'v_shield_user'
const AUTH_REFRESH_TOKEN_KEY = 'v_shield_refresh_token'

const readAuthToken = () =>
    sessionStorage.getItem(AUTH_TOKEN_KEY) || localStorage.getItem(AUTH_TOKEN_KEY) || null

const readAuthUser = () => {
    const raw = sessionStorage.getItem(AUTH_USER_KEY) || localStorage.getItem(AUTH_USER_KEY)
    return raw ? JSON.parse(raw) : null
}

const readRefreshToken = () =>
    sessionStorage.getItem(AUTH_REFRESH_TOKEN_KEY) || localStorage.getItem(AUTH_REFRESH_TOKEN_KEY) || null

const writeAuthState = (token, user, refreshToken) => {
    sessionStorage.setItem(AUTH_TOKEN_KEY, token)
    sessionStorage.setItem(AUTH_USER_KEY, JSON.stringify(user))
    if (refreshToken) {
        sessionStorage.setItem(AUTH_REFRESH_TOKEN_KEY, refreshToken)
    }
    localStorage.removeItem(AUTH_TOKEN_KEY)
    localStorage.removeItem(AUTH_USER_KEY)
    localStorage.removeItem(AUTH_REFRESH_TOKEN_KEY)
}

const clearAuthState = () => {
    sessionStorage.removeItem(AUTH_TOKEN_KEY)
    sessionStorage.removeItem(AUTH_USER_KEY)
    sessionStorage.removeItem(AUTH_REFRESH_TOKEN_KEY)
    localStorage.removeItem(AUTH_TOKEN_KEY)
    localStorage.removeItem(AUTH_USER_KEY)
    localStorage.removeItem(AUTH_REFRESH_TOKEN_KEY)
}

const state = reactive({
    token: readAuthToken(),
    refreshToken: readRefreshToken(),
    user: readAuthUser(),
})

/**
 * Đăng nhập
 * @param {string} username
 * @param {string} password
 * @returns {Promise<boolean>}
 */
export async function login(username, password, mfaCode = null) {
    const res = await loginApi(username, password, mfaCode)
    const data = res.data

    if (data.requiresMfa) {
        return {
            requiresMfa: true,
            requiresMfaSetup: data.requiresMfaSetup,
            mfaSetupSecret: data.mfaSetupSecret,
            mfaSetupUri: data.mfaSetupUri,
            message: data.message,
        }
    }

    state.token = data.token
    state.refreshToken = data.refreshToken
    state.user = {
        userId: data.userId,
        username: data.username,
        fullName: data.fullName,
        role: data.role,
        employeeId: data.employeeId,
        mfaEnabled: data.mfaEnabled,
        mfaRequired: data.mfaRequired,
        hasOperationalScopeAssignments: !!data.hasOperationalScopeAssignments,
        operationalTaskKeys: data.operationalTaskKeys || [],
    }

    writeAuthState(data.token, state.user, data.refreshToken)

    return { success: true }
}

/** Đăng xuất */
export async function logout() {
    try {
        if (state.token) {
            await logoutApiRequest(state.refreshToken || readRefreshToken())
        }
    } catch {
        // best effort: vẫn xóa phiên local để đảm bảo đăng xuất
    }
    state.token = null
    state.refreshToken = null
    state.user = null
    clearAuthState()
}

/** Kiểm tra đã đăng nhập chưa */
export function isLoggedIn() {
    return !!state.token
}

/** Kiểm tra role */
export function hasRole(role) {
    return state.user?.role === role
}

/** Lấy thông tin user từ API (verify token) */
export async function fetchUser() {
    try {
        const res = await getMe()
        state.user = {
            userId: res.data.userId,
            username: res.data.username,
            fullName: res.data.fullName,
            role: res.data.role,
            employeeId: res.data.employeeId,
            mfaEnabled: res.data.mfaEnabled,
            mfaRequired: res.data.mfaRequired,
            hasOperationalScopeAssignments: !!res.data.hasOperationalScopeAssignments,
            operationalTaskKeys: res.data.operationalTaskKeys || [],
        }
        writeAuthState(state.token, state.user, state.refreshToken)
        return true
    } catch {
        await logout()
        return false
    }
}

export { state as authState }
