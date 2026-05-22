import http from './http'

async function getWithFallback(primaryPath, legacyPath) {
  try {
    return await http.get(primaryPath)
  } catch (error) {
    if (error?.response?.status === 404) {
      return http.get(legacyPath)
    }
    throw error
  }
}

export const getDetectedPlates = () => getWithFallback('/license-plates/plates', '/BienSo/plates')
export const getCameraPlateSnapshot = () => getWithFallback('/license-plates/camera-plates', '/BienSo/camera-plates')
