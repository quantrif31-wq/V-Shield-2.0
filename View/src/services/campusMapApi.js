import http from './http'

export const getCampusMapLayout = () => http.get('/campus-map/layout')
export const saveCampusMapLayout = (payload) => http.put('/campus-map/layout', payload)
export const patchCampusMapLayout = (gateId, payload) => http.patch(`/campus-map/layout/${gateId}`, payload)
export const getCampusMapRealtime = () => http.get('/campus-map/realtime')
export const getCampusScene3D = () => http.get('/campus-map/scene3d')
export const createCampusSceneObject = (payload) => http.post('/campus-map/scene3d/objects', payload)
export const updateCampusSceneObject = (objectId, payload) => http.patch(`/campus-map/scene3d/objects/${objectId}`, payload)
export const deleteCampusSceneObject = (objectId) => http.delete(`/campus-map/scene3d/objects/${objectId}`)
