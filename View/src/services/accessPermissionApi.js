import http from './http'

export const getEmployeePermissionMatrix = (params = {}) =>
    http.get('/access-permissions/employee-matrix', { params })

export const getVisitorPermissionMatrix = (params = {}) =>
    http.get('/access-permissions/visitor-matrix', { params })

export const setAccessPermission = (payload) =>
    http.post('/access-permissions/set-permission', payload)

export const deleteEmployeeAccessPermission = (employeeId, gateId) =>
    http.delete(`/access-permissions/employee/${employeeId}/gate/${gateId}`)

export const deleteVisitorAccessPermission = (visitorDetailId, gateId) =>
    http.delete(`/access-permissions/visitor/${visitorDetailId}/gate/${gateId}`)
