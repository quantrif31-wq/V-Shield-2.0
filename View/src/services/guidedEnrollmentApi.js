import http from "./http"

const BASE_PATH = "/FaceCamera/guided"

export async function guidedStart({ streamUrl, poseMode = "full" }) {
  const res = await http.post(`${BASE_PATH}/start`, { streamUrl, poseMode })
  return res.data
}

export async function guidedProgress() {
  const res = await http.get(`${BASE_PATH}/progress`)
  return res.data
}

export async function guidedStop() {
  const res = await http.post(`${BASE_PATH}/stop`)
  return res.data
}

export async function guidedConfirm(subjectId) {
  const res = await http.post(`${BASE_PATH}/confirm`, { subjectId })
  return res.data
}
