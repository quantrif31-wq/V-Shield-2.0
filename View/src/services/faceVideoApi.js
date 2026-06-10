import http from './http'

export const uploadFaceVideo = async (file, employeeId, onProgress) => {
    const formData = new FormData()
    formData.append('file', file)

    if (employeeId) {
        formData.append('employeeId', employeeId)
    }

    return http.post('/Video/upload', formData, {
        headers: {
            'Content-Type': 'multipart/form-data',
        },
        onUploadProgress: (e) => {
            if (onProgress) {
                const percent = Math.round((e.loaded * 100) / e.total)
                onProgress(percent)
            }
        },
    })
}

export const getEmployeeVideos = (employeeId) => {
    return http.get(`/Video/employee/${employeeId}`)
}

export const getProtectedVideoBlob = (id) => {
    return http.get(`/Video/${id}/content`, {
        responseType: 'blob',
    })
}

export const deleteVideo = (id) => {
    return http.delete(`/Video/${id}`)
}
