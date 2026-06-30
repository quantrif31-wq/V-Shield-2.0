import http from './http'

export const identityApi = {
    getOverview() {
        return http.get('/enterprise/identity/overview')
    },
    getProviders() {
        return http.get('/enterprise/identity/providers')
    },
    upsertProvider(payload) {
        return http.post('/enterprise/identity/providers', payload)
    },
    oidcChallenge(providerId, redirectUri, state) {
        return http.get(`/enterprise/identity/providers/${providerId}/oidc-challenge`, {
            params: { redirectUri, state },
        })
    },
    oidcCallback(providerId, code, redirectUri) {
        return http.post(`/enterprise/identity/providers/${providerId}/oidc-callback`, {
            code,
            redirectUri,
        })
    },
    importUsers(providerId, users) {
        return http.post('/enterprise/identity/import/users', { providerId, users })
    },
    importGroups(groups) {
        return http.post('/enterprise/identity/import/groups', { groups })
    },
    getRevocationProof(employeeId) {
        return http.get(`/enterprise/identity/employees/${employeeId}/revocation-proof`)
    },
    offboardEmployee(employeeId, reason) {
        return http.patch(`/enterprise/identity/employees/${employeeId}/offboard`, { reason })
    },
}
