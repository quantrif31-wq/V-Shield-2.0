import axios from 'axios'
import { API_BASE_URL } from '../config/api'

const employeeApiClient = axios.create({
    baseURL: `${API_BASE_URL}/Employees`
})

// Tá»± Ä‘á»™ng gáº¯n JWT token
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
 * Láº¥y danh sÃ¡ch nhÃ¢n viÃªn (cÃ³ filter)
 * @param {{search?, departmentId?, positionId?, status?}} params
 */
export const getAll = (params = {}) => employeeApiClient.get('/', { params })

/** Láº¥y chi tiáº¿t nhÃ¢n viÃªn */
export const getById = (id) => employeeApiClient.get(`/${id}`)

/** Táº¡o nhÃ¢n viÃªn má»›i */
export const create = (data) => employeeApiClient.post('/', data)

/** Cáº­p nháº­t nhÃ¢n viÃªn */
export const update = (id, data) => employeeApiClient.put(`/${id}`, data)

/** XÃ³a nhÃ¢n viÃªn */
export const deleteEmployee = (id) => employeeApiClient.delete(`/${id}`)

/**
 * Upload áº£nh khuÃ´n máº·t
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

