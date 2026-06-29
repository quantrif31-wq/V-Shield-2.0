import http from './http'

export const routingApi = {
    getRoute(payload) {
        return http.post('/routing', payload)
    }
}
