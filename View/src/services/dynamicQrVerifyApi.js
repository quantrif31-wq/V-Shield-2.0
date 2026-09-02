import http from './http'

export async function verifyDynamicQr(qrPayload, scannerDevice = 'WEB_SCANNER') {
    const response = await http.post('/dynamic-qr/verify', {
        qrPayload,
        scannerDevice,
    })
    return response.data
}

// Gate-aware verification is the authoritative path for a physical lane. It
// records both granted and denied attempts; deferTransit keeps the later
// plate-confirmed gate transaction as the sole attendance update.
export async function verifyQrForGate(payload) {
    const response = await http.post('/QrAccess/scan-access', payload)
    return response.data
}
