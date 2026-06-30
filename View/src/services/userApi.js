import http from './http'

export const getAll = () => http.get('/Users')
export const getById = (id) => http.get(`/Users/${id}`)
export const create = (data) => http.post('/Users', data)
export const update = (id, data) => http.put(`/Users/${id}`, data)
export const deleteUser = (id) => http.delete(`/Users/${id}`)
export const resetMfa = (id) => http.post(`/Users/${id}/mfa/reset`)
export const getOperationalScopeReference = () => http.get('/Users/scope-reference')
export const getOperationalScopes = (id) => http.get(`/Users/${id}/operational-scopes`)
export const replaceOperationalScopes = (id, data) => http.put(`/Users/${id}/operational-scopes`, data)
