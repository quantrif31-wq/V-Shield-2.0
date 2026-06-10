import http from './http'

export const getDepartments = () => http.get('/Departments')
export const getDepartmentById = (id) => http.get(`/Departments/${id}`)
export const createDepartment = (data) => http.post('/Departments', data)
export const updateDepartment = (id, data) => http.put(`/Departments/${id}`, data)
export const deleteDepartment = (id) => http.delete(`/Departments/${id}`)

export const getPositions = () => http.get('/Positions')
export const getPositionById = (id) => http.get(`/Positions/${id}`)
export const createPosition = (data) => http.post('/Positions', data)
export const updatePosition = (id, data) => http.put(`/Positions/${id}`, data)
export const deletePosition = (id) => http.delete(`/Positions/${id}`)
