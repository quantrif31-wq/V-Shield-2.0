import http from './http'

export const getAll = () => http.get('/Vehicles')
export const getTypes = () => http.get('/Vehicles/types')
export const getById = (id) => http.get(`/Vehicles/${id}`)
export const getByLicensePlate = (plate) => http.get(`/Vehicles/license-plate/${plate}`)
export const getByEmployeeId = (employeeId) => http.get(`/Vehicles/employee/${employeeId}`)
export const create = (data) => http.post('/Vehicles', data)
export const update = (id, data) => http.put(`/Vehicles/${id}`, data)
export const deleteVehicle = (id) => http.delete(`/Vehicles/${id}`)
