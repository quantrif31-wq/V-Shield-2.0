import http from './http'

export const getBiometricOverview = (params = {}) => http.get('/biometrics/overview', { params })
export const getFaceModelHealth = () => http.get('/FaceModels/health')
export const getFaceEnrollmentJobs = () => http.get('/FaceEnrollments')
export const createFaceEnrollmentJob = (employeeId, employeeFaceVideoId) =>
    http.post('/FaceEnrollments', { employeeId, employeeFaceVideoId })
export const cancelFaceEnrollmentJob = (jobId) => http.post(`/FaceEnrollments/${jobId}/cancel`)
export const retryFaceEnrollmentJob = (jobId) => http.post(`/FaceEnrollments/${jobId}/retry`)
export const activateFaceEnrollmentJob = (jobId) => http.post(`/FaceEnrollments/${jobId}/activate`)
export const getAccessCredentials = () => http.get('/AccessCredentials')
