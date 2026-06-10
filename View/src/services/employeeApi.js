import http from './http'

export const getAll = (params = {}) => http.get('/Employees', { params })
export const getById = (id) => http.get(`/Employees/${id}`)
export const create = (data) => http.post('/Employees', data)
export const update = (id, data) => http.put(`/Employees/${id}`, data)
export const deleteEmployee = (id) => http.delete(`/Employees/${id}`)

export const uploadFace = (id, file) => {
    const formData = new FormData()
    formData.append('file', file)
    return http.post(`/Employees/${id}/face`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
    })
}

export const getProtectedFaceImage = (id) => {
    return http.get(`/Employees/${id}/face-image`, {
        responseType: 'blob',
    })
}
