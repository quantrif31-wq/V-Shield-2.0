import axios from 'axios'
import { API_BASE_URL } from '../config/api'

const userApiClient = axios.create({
    baseURL: `${API_BASE_URL}/Users`
})

// Tự động gắn JWT token
userApiClient.interceptors.request.use((config) => {
    const token = localStorage.getItem('v_shield_token')
    if (token) {
        config.headers.Authorization = `Bearer ${token}`
    }
    return config
})

userApiClient.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response && error.response.status === 401) {
            localStorage.removeItem('v_shield_token')
            localStorage.removeItem('v_shield_user')
            window.location.href = '/login'
        }
        return Promise.reject(error)
    }
)

/** Lấy danh sách tất cả tài khoản (Admin) */
export const getAll = () => userApiClient.get('/')

/** Lấy chi tiết tài khoản theo ID (Admin) */
export const getById = (id) => userApiClient.get(`/${id}`)

/**
 * Tạo tài khoản mới (Admin)
 * @param {{username, password, fullName, role}} data
 */
export const create = (data) => userApiClient.post('/', data)

/**
 * Cập nhật tài khoản (Admin)
 * @param {number} id
 * @param {{fullName?, role?, isActive?, password?}} data
 */
export const update = (id, data) => userApiClient.put(`/${id}`, data)

/**
 * Xóa tài khoản (Admin)
 * @param {number} id
 */
export const deleteUser = (id) => userApiClient.delete(`/${id}`)
