import http from './http'

export const getBiometricOverview = (params = {}) => http.get('/biometrics/overview', { params })
export const getFaceModelHealth = () => http.get('/FaceModels/health')
