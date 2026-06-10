import axios from 'axios'
import http from './http'
import { API_BASE_URL } from '../config/api'

const publicApi = axios.create({
    baseURL: API_BASE_URL
})

export const validateToken = (token) => {
    return publicApi.get(`/pre-registrations/validate/${token}`)
}

export const submitRegistration = (token, data) => {
    return publicApi.post(`/pre-registrations/submit/${token}`, data)
}

export const getVisitorPass = (token) => {
    return publicApi.get(`/pre-registrations/visitor-pass/${token}`)
}

export const getAll = (params = {}) => {
    return http.get('/pre-registrations', { params })
}

export const getDetail = (id) => {
    return http.get(`/pre-registrations/${id}`)
}

export const updateStatus = (id, status) => {
    return http.patch(`/pre-registrations/${id}/status`, { status })
}

export const createLink = (data) => {
    return http.post('/registration-links', data)
}

export const getLinks = (params = {}) => {
    return http.get('/registration-links', { params })
}
