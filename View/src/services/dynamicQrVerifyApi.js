import http from './http'

export async function verifyDynamicQr(qrPayload, scannerDevice = 'WEB_SCANNER') {
    const response = await http.post('/dynamic-qr/verify', {
        qrPayload,
        scannerDevice,
    })
    return response.data
}
