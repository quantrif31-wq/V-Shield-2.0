import axios from 'axios'
import { API_BASE_URL } from '../config/api'

const vehicleApiClient = axios.create({
    baseURL: `${API_BASE_URL}/Vehicles`
})

// Tự động gắn JWT token
vehicleApiClient.interceptors.request.use((config) => {
    const token = localStorage.getItem('v_shield_token')
    if (token) {
        config.headers.Authorization = `Bearer ${token}`
    }
    return config
})

vehicleApiClient.interceptors.response.use(
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

/** Lấy danh sách tất cả phương tiện */
export const getAll = () => vehicleApiClient.get('/')

/** Lấy danh mục loại xe */
export const getTypes = () => vehicleApiClient.get('/types')

/** Lấy phương tiện theo ID */
export const getById = (id) => vehicleApiClient.get(`/${id}`)

/** Tra cứu phương tiện theo biển số */
export const getByLicensePlate = (plate) => vehicleApiClient.get(`/license-plate/${plate}`)

/** Lấy danh sách phương tiện của một nhân viên */
export const getByEmployeeId = (employeeId) => vehicleApiClient.get(`/employee/${employeeId}`)

/** Đăng ký phương tiện mới */
export const create = (data) => vehicleApiClient.post('/', data)

/** Cập nhật thông tin phương tiện */
export const update = (id, data) => vehicleApiClient.put(`/${id}`, data)

/** Xóa đăng ký phương tiện */
export const deleteVehicle = (id) => vehicleApiClient.delete(`/${id}`)
