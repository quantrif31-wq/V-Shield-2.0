import axios from 'axios'
import { API_BASE_URL } from '../config/api'

const userApiClient = axios.create({
    baseURL: `${API_BASE_URL}/Users`
})

// Tá»± Ä‘á»™ng gáº¯n JWT token
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

/** Láº¥y danh sÃ¡ch táº¥t cáº£ tÃ i khoáº£n (Admin) */
export const getAll = () => userApiClient.get('/')

/** Láº¥y chi tiáº¿t tÃ i khoáº£n theo ID (Admin) */
export const getById = (id) => userApiClient.get(`/${id}`)

/**
 * Táº¡o tÃ i khoáº£n má»›i (Admin)
 * @param {{username, password, fullName, role}} data
 */
export const create = (data) => userApiClient.post('/', data)

/**
 * Cáº­p nháº­t tÃ i khoáº£n (Admin)
 * @param {number} id
 * @param {{fullName?, role?, isActive?, password?}} data
 */
export const update = (id, data) => userApiClient.put(`/${id}`, data)

/**
 * XÃ³a tÃ i khoáº£n (Admin)
 * @param {number} id
 */
export const deleteUser = (id) => userApiClient.delete(`/${id}`)

