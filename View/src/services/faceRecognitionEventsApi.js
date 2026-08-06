import http from "./http"

const BASE_PATH = "/FaceRecognitionEvents"

export async function getFaceRecognitionEvents(params = {}) {
  const response = await http.get(BASE_PATH, { params })
  return response.data
}

export async function getFaceRecognitionCollectorHealth() {
  const response = await http.get(`${BASE_PATH}/health`)
  return response.data
}
