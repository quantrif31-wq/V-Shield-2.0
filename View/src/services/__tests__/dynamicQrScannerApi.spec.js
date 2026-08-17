import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  QR_API_BASE_URL,
  QR_API_BASE_URL_LANE2,
  getQrScanResult,
  resetQrSession,
  scanQrOnce,
  startQrScanner,
  stopQrScanner,
} from '../dynamicQrScannerApi'

afterEach(() => vi.unstubAllGlobals())

describe('dynamicQrScannerApi', () => {
  it('exposes the two lane base urls', () => {
    expect(QR_API_BASE_URL).toContain(':8001')
    expect(QR_API_BASE_URL_LANE2).toContain(':8002')
  })

  it('starts the scanner with the rtsp payload', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: true, json: async () => ({ started: true }) })
    vi.stubGlobal('fetch', fetchMock)
    await startQrScanner('rtsp://cam/stream', 'http://qr:8001')
    expect(fetchMock).toHaveBeenCalledWith('http://qr:8001/qr/start', expect.objectContaining({
      method: 'POST',
      body: JSON.stringify({ rtsp: 'rtsp://cam/stream' }),
    }))
  })

  it('scans, resets and stops via POST', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: true, json: async () => ({ ok: true }) })
    vi.stubGlobal('fetch', fetchMock)
    await scanQrOnce('http://qr:8001')
    await resetQrSession('http://qr:8001')
    await stopQrScanner('http://qr:8001')
    expect(fetchMock.mock.calls.map(([url]) => url)).toEqual([
      'http://qr:8001/qr/scan',
      'http://qr:8001/qr/reset',
      'http://qr:8001/qr/stop',
    ])
  })

  it('reads the scan result via GET', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: true, json: async () => ({ code: 'QR1' }) })
    vi.stubGlobal('fetch', fetchMock)
    await expect(getQrScanResult('http://qr:8001')).resolves.toEqual({ code: 'QR1' })
    expect(fetchMock).toHaveBeenCalledWith('http://qr:8001/qr/result', {})
  })

  it('surfaces the server message when the response is not ok', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: false,
      status: 400,
      json: async () => ({ message: 'invalid rtsp' }),
    }))
    await expect(startQrScanner('bad', 'http://qr:8001')).rejects.toThrow('invalid rtsp')
  })

  it('falls back to status text when no message is provided', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: false,
      status: 500,
      json: async () => { throw new Error('no json') },
    }))
    await expect(startQrScanner('bad', 'http://qr:8001')).rejects.toThrow('QR service loi 500')
  })

  it('wraps network failures with a connection hint', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('ECONNREFUSED')))
    await expect(startQrScanner('rtsp://cam', 'http://qr:8001')).rejects.toThrow(
      'Không kết nối được dịch vụ QR tại http://qr:8001'
    )
  })
})
