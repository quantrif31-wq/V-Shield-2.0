const trimTrailingSlash = (value = "") => String(value || "").replace(/\/+$/, "")

const resolveQrApiBaseUrl = (configuredValue, fallbackPort) => {
  const configured = trimTrailingSlash(configuredValue || "")
  if (configured) return configured

  if (typeof window !== "undefined") {
    const { protocol, hostname } = window.location
    return `${protocol}//${hostname}:${fallbackPort}`
  }

  return `http://localhost:${fallbackPort}`
}

const QR_API_BASE_URL = resolveQrApiBaseUrl(import.meta.env.VITE_QR_API_BASE_URL, 8001)
const QR_API_BASE_URL_LANE2 = resolveQrApiBaseUrl(
  import.meta.env.VITE_QR_API_BASE_URL_LANE2,
  8002
)

async function request(path, options = {}, baseUrl = QR_API_BASE_URL) {
  let response
  try {
    response = await fetch(`${baseUrl}${path}`, options)
  } catch (error) {
    throw new Error(
      `Không kết nối được dịch vụ QR tại ${baseUrl}. Hãy kiểm tra QR_Dong.py đang chạy đúng cổng.`
    )
  }

  let data = null
  try {
    data = await response.json()
  } catch {
    data = null
  }

  if (!response.ok) {
    const message =
      data?.message ||
      data?.detail ||
      `QR service loi ${response.status}`

    throw new Error(message)
  }

  return data
}

export async function startQrScanner(rtsp, baseUrl = QR_API_BASE_URL) {
  return request("/qr/start", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ rtsp }),
  }, baseUrl)
}

export async function scanQrOnce(baseUrl = QR_API_BASE_URL) {
  return request("/qr/scan", {
    method: "POST",
  }, baseUrl)
}

export async function resetQrSession(baseUrl = QR_API_BASE_URL) {
  return request("/qr/reset", {
    method: "POST",
  }, baseUrl)
}

export async function stopQrScanner(baseUrl = QR_API_BASE_URL) {
  return request("/qr/stop", {
    method: "POST",
  }, baseUrl)
}

export async function getQrScanResult(baseUrl = QR_API_BASE_URL) {
  return request("/qr/result", {}, baseUrl)
}


export { QR_API_BASE_URL, QR_API_BASE_URL_LANE2 }

