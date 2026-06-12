import http from './http'

export const importFile = (entityType, file, options = {}) => {
    const formData = new FormData()
    formData.append('file', file)
    if (options.skipDuplicates !== undefined) formData.append('SkipDuplicates', options.skipDuplicates)
    if (options.updateExisting !== undefined) formData.append('UpdateExisting', options.updateExisting)
    return http.post(`/import-export/${entityType}/import`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
        timeout: 300000,
    })
}

export const previewImport = (entityType, file) => {
    const formData = new FormData()
    formData.append('file', file)
    return http.post(`/import-export/${entityType}/import/preview`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
    })
}

export const exportData = (entityType, params = {}) => {
    return http.get(`/import-export/${entityType}/export`, { params })
}

export const downloadTemplate = (entityType, format = 'csv') => {
    return http.get(`/import-export/${entityType}/template`, {
        params: { format },
        responseType: 'blob',
    })
}

export const downloadResult = (id) => {
    return http.get(`/import-export/download/${id}`, {
        responseType: 'blob',
    })
}

export const getHistory = (params = {}) => {
    return http.get('/import-export/history', { params })
}

export const getHistoryById = (id) => {
    return http.get(`/import-export/history/${id}`)
}

export const getFormats = () => {
    return http.get('/import-export/formats')
}

export const getEntities = () => {
    return http.get('/import-export/entities')
}

// === AI Import endpoints ===

export const aiAnalyze = (entityType, file) => {
    const formData = new FormData()
    formData.append('file', file)
    return http.post(`/import-export/${entityType}/ai/analyze`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
        timeout: 60000,
    })
}

export const aiOcr = (entityType, file) => {
    const formData = new FormData()
    formData.append('file', file)
    return http.post(`/import-export/${entityType}/ai/ocr`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
        timeout: 300000,
    })
}

export const aiNormalize = (entityType, sessionId) => {
    return http.post(`/import-export/${entityType}/ai/normalize`, { sessionId })
}

export const aiConfirm = (entityType, sessionId, options = {}) => {
    return http.post(`/import-export/${entityType}/ai/confirm/${sessionId}`, {
        confirmNormalization: options.confirmNormalization ?? true,
        overrideConflicts: options.overrideConflicts ?? false,
    })
}

export const aiPreview = (entityType, sessionId) => {
    return http.get(`/import-export/${entityType}/ai/preview/${sessionId}`)
}

export const aiGetSynonyms = () => {
    return http.get('/import-export/synonyms')
}

export const aiStatus = () => {
    return http.get('/import-export/status')
}
