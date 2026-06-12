import http from './http'

export const getUebaProfiles = (params = {}) => http.get('/ueba/profiles', { params })
export const getUebaProfile = (employeeId) => http.get(`/ueba/profiles/${employeeId}`)
export const rebuildUebaProfile = (employeeId) => http.post(`/ueba/profiles/${employeeId}/rebuild`)
export const getUebaAnomalies = (params = {}) => http.get('/ueba/anomalies', { params })
export const resolveUebaAnomaly = (id, data) => http.post(`/ueba/anomalies/${id}/resolve`, data)
export const markUebaAnomalyFalsePositive = (id) => http.post(`/ueba/anomalies/${id}/false-positive`)
export const getUebaSummary = () => http.get('/ueba/summary')
