import http from './http'

export const getDetectedPlates = () => http.get('/license-plates/plates')
export const getCameraPlateSnapshot = () => http.get('/license-plates/camera-plates')

export const fuzzyMatchPlate = (data) => http.post('/license-plates/fuzzy-match', data)
export const getPlateTimeline = (plate, params = {}) => http.get(`/license-plates/${encodeURIComponent(plate)}/timeline`, { params })
export const getPlateAnomalies = (plate, params = {}) => http.get(`/license-plates/${encodeURIComponent(plate)}/anomalies`, { params })
export const suggestPlateCorrection = (data) => http.post('/license-plates/suggest-correction', data)
