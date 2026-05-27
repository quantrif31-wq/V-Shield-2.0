import http from './http'

export const getGuestProfiles = (params = {}) => http.get('/guest-profiles', { params })
export const getGuestProfileDetail = (id) => http.get(`/guest-profiles/${id}`)
export const createGuestProfile = (data) => http.post('/guest-profiles', data)
export const updateGuestProfile = (id, data) => http.put(`/guest-profiles/${id}`, data)
export const deleteGuestProfile = (id) => http.delete(`/guest-profiles/${id}`)

export const getVisitorDirectory = (params = {}) => http.get('/guest-profiles/visitor-directory', { params })
export const updateVisitorDirectoryItem = (visitorDetailId, data) =>
  http.put(`/guest-profiles/visitor-directory/${visitorDetailId}`, data)
export const deleteVisitorDirectoryItem = (visitorDetailId) =>
  http.delete(`/guest-profiles/visitor-directory/${visitorDetailId}`)
export const getVisitorAccessLogs = (visitorDetailId) =>
  http.get(`/guest-profiles/visitor-directory/${visitorDetailId}/access-logs`)
