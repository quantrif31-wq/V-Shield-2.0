import http from './http'

export const getDashboardOverview = () => http.get('/dashboard/overview')

export const getDashboardIntelligence = () => http.get('/dashboard/intelligence')

export const getDashboardReports = () => http.get('/dashboard/reports')
