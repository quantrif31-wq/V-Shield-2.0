import axios from 'axios'
import { API_BASE_URL } from '../config/api'

const employeeApiClient = axios.create({
    baseURL: `${API_BASE_URL}/Employees`
})

// Tự động gắn JWT token
employeeApiClient.interceptors.request.use((config) => {
    const token = localStorage.getItem('v_shield_token')
    if (token) {
        config.headers.Authorization = `Bearer ${token}`
    }
    return config
})

employeeApiClient.interceptors.response.use(
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

/**
 * Lấy danh sách nhân viên (có filter)
 * @param {{search?, departmentId?, positionId?, status?}} params
 */
export const getAll = (params = {}) => employeeApiClient.get('/', { params })

/** Lấy chi tiết nhân viên */
export const getById = (id) => employeeApiClient.get(`/${id}`)

/** Tạo nhân viên mới */
export const create = (data) => employeeApiClient.post('/', data)

/** Cập nhật nhân viên */
export const update = (id, data) => employeeApiClient.put(`/${id}`, data)

/** Xóa nhân viên */
export const deleteEmployee = (id) => employeeApiClient.delete(`/${id}`)

/**
 * Upload ảnh khuôn mặt
 * @param {number} id
 * @param {File} file
 */
export const uploadFace = (id, file) => {
    const formData = new FormData()
    formData.append('file', file)
    return employeeApiClient.post(`/${id}/face`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
    })
}
