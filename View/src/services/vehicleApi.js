import axios from 'axios'
import { API_BASE_URL } from '../config/api'

const vehicleApiClient = axios.create({
    baseURL: `${API_BASE_URL}/Vehicles`
})

// Tá»± Ä‘á»™ng gáº¯n JWT token
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

/** Láº¥y danh sÃ¡ch táº¥t cáº£ phÆ°Æ¡ng tiá»‡n */
export const getAll = () => vehicleApiClient.get('/')

/** Láº¥y danh má»¥c loáº¡i xe */
export const getTypes = () => vehicleApiClient.get('/types')

/** Láº¥y phÆ°Æ¡ng tiá»‡n theo ID */
export const getById = (id) => vehicleApiClient.get(`/${id}`)

/** Tra cá»©u phÆ°Æ¡ng tiá»‡n theo biá»ƒn sá»‘ */
export const getByLicensePlate = (plate) => vehicleApiClient.get(`/license-plate/${plate}`)

/** Láº¥y danh sÃ¡ch phÆ°Æ¡ng tiá»‡n cá»§a má»™t nhÃ¢n viÃªn */
export const getByEmployeeId = (employeeId) => vehicleApiClient.get(`/employee/${employeeId}`)

/** ÄÄƒng kÃ½ phÆ°Æ¡ng tiá»‡n má»›i */
export const create = (data) => vehicleApiClient.post('/', data)

/** Cáº­p nháº­t thÃ´ng tin phÆ°Æ¡ng tiá»‡n */
export const update = (id, data) => vehicleApiClient.put(`/${id}`, data)

/** XÃ³a Ä‘Äƒng kÃ½ phÆ°Æ¡ng tiá»‡n */
export const deleteVehicle = (id) => vehicleApiClient.delete(`/${id}`)

