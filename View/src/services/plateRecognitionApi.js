import http from './http'

export const getDetectedPlates = () => http.get('/license-plates/plates')
export const getCameraPlateSnapshot = () => http.get('/license-plates/camera-plates')
