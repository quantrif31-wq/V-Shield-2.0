import http from "./http"

const BASE_PATH = "/FaceEnrollment"

export async function getMyFaceStatus() {
  const res = await http.get(`${BASE_PATH}/my-status`)
  return res.data
}

export async function enrollSelf(images) {
  const res = await http.post(`${BASE_PATH}/enroll-self`, { images })
  return res.data
}
