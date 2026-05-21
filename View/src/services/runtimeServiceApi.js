import api from './http'

export async function getRuntimeServices() {
  const res = await api.get('/runtime-services')
  return res.data
}

export async function updateRuntimeService(name, payload) {
  const res = await api.put(`/runtime-services/${name}`, payload)
  return res.data
}

export async function startRuntimeService(name) {
  const res = await api.post(`/runtime-services/${name}/start`)
  return res.data
}

export async function stopRuntimeService(name) {
  const res = await api.post(`/runtime-services/${name}/stop`)
  return res.data
}
