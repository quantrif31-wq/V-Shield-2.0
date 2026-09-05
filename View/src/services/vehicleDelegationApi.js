import http from './http'

export const createDelegation = (data) => http.post('/vehicle-delegations', data)
export const createOwnershipRequest = (data) => http.post('/vehicle-delegations/ownership-requests', data)
export const getAvailableForOwnershipRequest = () => http.get('/vehicle-delegations/available-for-ownership-request')
export const getOutgoing = () => http.get('/vehicle-delegations/outgoing')
export const getIncoming = () => http.get('/vehicle-delegations/incoming')
export const getAllDelegations = () => http.get('/vehicle-delegations')
export const approveDelegation = (id) => http.patch(`/vehicle-delegations/${id}/approve`)
export const rejectDelegation = (id, data) => http.patch(`/vehicle-delegations/${id}/reject`, data)
export const revokeDelegation = (id) => http.patch(`/vehicle-delegations/${id}/revoke`)
