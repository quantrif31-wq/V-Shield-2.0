import http from './http'

export const getSummary = () => http.get('/Statistics/employees/summary').then(res => res.data)
