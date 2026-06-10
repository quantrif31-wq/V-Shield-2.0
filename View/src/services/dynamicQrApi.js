import http from './http'

export async function generateDynamicQr(employeeId) {
    const response = await http.post('/dynamic-qr/generate', {
        employeeId: Number(employeeId),
    })
    return response.data
}
