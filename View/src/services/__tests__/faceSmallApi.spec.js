import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../http', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
    request: vi.fn(),
    defaults: { headers: { common: {} } },
  },
}))

const http = (await import('../http')).default
const faceAccessDecisionApi = await import('../faceAccessDecisionApi')
const faceAccessPolicyComparisonApi = await import('../faceAccessPolicyComparisonApi')
const faceCameraConfigurationApi = await import('../faceCameraConfigurationApi')
const faceEnrollmentApi = await import('../faceEnrollmentApi')
const faceGateApi = await import('../faceGateApi')
const faceRecognitionEventsApi = await import('../faceRecognitionEventsApi')
const faceVideoApi = await import('../faceVideoApi')
const guidedEnrollmentApi = await import('../guidedEnrollmentApi')

beforeEach(() => vi.clearAllMocks())

describe('faceAccessDecisionApi', () => {
  it('fetches decisions and summaries', async () => {
    http.get.mockResolvedValue({ data: { items: [] } })
    await faceAccessDecisionApi.getFaceAccessDecisions({ page: 1 })
    expect(http.get).toHaveBeenCalledWith('/FaceAccessDecisions', { params: { page: 1 } })
    await faceAccessDecisionApi.getFaceAccessDecisionSummary({ day: 'x' })
    expect(http.get).toHaveBeenCalledWith('/FaceAccessDecisions/summary', { params: { day: 'x' } })
  })
})

describe('faceAccessPolicyComparisonApi', () => {
  it('fetches policy comparisons and summaries', async () => {
    http.get.mockResolvedValue({ data: [] })
    await faceAccessPolicyComparisonApi.getFacePolicyComparisons({ page: 1 })
    expect(http.get).toHaveBeenCalledWith('/FaceAccessPolicyComparisons', { params: { page: 1 } })
    await faceAccessPolicyComparisonApi.getFacePolicyComparisonSummary()
    expect(http.get).toHaveBeenCalledWith('/FaceAccessPolicyComparisons/summary', { params: {} })
  })
})

describe('faceCameraConfigurationApi', () => {
  it('lists configurations', async () => {
    http.get.mockResolvedValue({ data: [] })
    await faceCameraConfigurationApi.getFaceCameraConfigurations()
    expect(http.get).toHaveBeenCalledWith('/FaceCameraConfigurations')
  })

  it('rejects invalid camera ids', async () => {
    await expect(faceCameraConfigurationApi.updateFaceCameraConfiguration('..', {})).rejects.toThrow('Runtime camera ID')
  })

  it('updates, starts and stops a configured camera', async () => {
    http.put.mockResolvedValue({ data: {} })
    await faceCameraConfigurationApi.updateFaceCameraConfiguration('CAM-1', { threshold: 0.5 })
    expect(http.put).toHaveBeenCalledWith('/FaceCameraConfigurations/CAM-1', { threshold: 0.5 })
    http.post.mockResolvedValue({ data: {} })
    await faceCameraConfigurationApi.startConfiguredFaceCamera('CAM-1')
    await faceCameraConfigurationApi.stopConfiguredFaceCamera('CAM-1')
    await faceCameraConfigurationApi.reconcileFaceCameras()
    expect(http.post.mock.calls.map(([u]) => u)).toEqual([
      '/FaceCameraConfigurations/CAM-1/start',
      '/FaceCameraConfigurations/CAM-1/stop',
      '/FaceCameraConfigurations/reconcile',
    ])
  })
})

describe('faceEnrollmentApi', () => {
  it('covers self-service face enrollment endpoints', async () => {
    http.get.mockResolvedValue({ data: {} })
    await faceEnrollmentApi.getMyFaceStatus()
    expect(http.get).toHaveBeenCalledWith('/FaceEnrollment/my-status')
    http.post.mockResolvedValue({ data: {} })
    await faceEnrollmentApi.enrollSelf(['img'])
    expect(http.post).toHaveBeenCalledWith('/FaceEnrollment/enroll-self', { images: ['img'] })
    http.delete.mockResolvedValue({ data: {} })
    await faceEnrollmentApi.deleteMyFaceId()
    expect(http.delete).toHaveBeenCalledWith('/FaceEnrollment/self-face-id')
  })
})

describe('faceGateApi', () => {
  it('covers face gate endpoints', async () => {
    http.get.mockResolvedValue({ data: {} })
    await faceGateApi.getFaceGates()
    expect(http.get).toHaveBeenCalledWith('/face-gate/gates')
    http.post.mockResolvedValue({ data: {} })
    await faceGateApi.verifyFaceGatePassword('pw')
    expect(http.post).toHaveBeenCalledWith('/face-gate/verify-password', { password: 'pw' })
    http.get.mockResolvedValue({ data: {} })
    await faceGateApi.checkGateAccess(7, 3)
    expect(http.get).toHaveBeenCalledWith('/face-gate/check-access', { params: { employeeId: 7, gateId: 3 } })
    http.post.mockResolvedValue({ data: {} })
    await faceGateApi.recordFaceGateResult({ x: 1 })
    expect(http.post).toHaveBeenCalledWith('/face-gate/record', { x: 1 })
    http.get.mockResolvedValue({ data: [] })
    await faceGateApi.getFaceIntruders({ page: 1 })
    expect(http.get).toHaveBeenCalledWith('/face-gate/intruders', { params: { page: 1 } })
    http.delete.mockResolvedValue({ data: {} })
    await faceGateApi.deleteFaceIntruder(5)
    expect(http.delete).toHaveBeenCalledWith('/face-gate/intruders/5')
  })
})

describe('faceRecognitionEventsApi', () => {
  it('fetches events and collector health', async () => {
    http.get.mockResolvedValue({ data: [] })
    await faceRecognitionEventsApi.getFaceRecognitionEvents({ page: 1 })
    expect(http.get).toHaveBeenCalledWith('/FaceRecognitionEvents', { params: { page: 1 } })
    await faceRecognitionEventsApi.getFaceRecognitionCollectorHealth()
    expect(http.get).toHaveBeenCalledWith('/FaceRecognitionEvents/health')
  })
})

describe('faceVideoApi', () => {
  it('uploads a face video with progress reporting', async () => {
    http.post.mockResolvedValue({ data: {} })
    const onProgress = vi.fn()
    const file = new Blob(['video'])
    await faceVideoApi.uploadFaceVideo(file, 7, onProgress)
    const config = http.post.mock.calls[0][2]
    expect(http.post.mock.calls[0][0]).toBe('/Video/upload')
    expect(config.headers['Content-Type']).toBe('multipart/form-data')
    config.onUploadProgress({ loaded: 50, total: 100 })
    expect(onProgress).toHaveBeenCalledWith(50)
  })

  it('lists, reads and deletes videos', async () => {
    http.get.mockResolvedValue({ data: [] })
    await faceVideoApi.getEmployeeVideos(7)
    expect(http.get).toHaveBeenCalledWith('/Video/employee/7')
    await faceVideoApi.getProtectedVideoBlob(3)
    expect(http.get).toHaveBeenCalledWith('/Video/3/content', { responseType: 'blob' })
    http.delete.mockResolvedValue({ data: {} })
    await faceVideoApi.deleteVideo(3)
    expect(http.delete).toHaveBeenCalledWith('/Video/3')
  })
})

describe('guidedEnrollmentApi', () => {
  it('covers guided enrollment lifecycle', async () => {
    http.post.mockResolvedValue({ data: {} })
    await guidedEnrollmentApi.guidedStart({ streamUrl: 'rtsp://x', poseMode: 'full' })
    expect(http.post).toHaveBeenCalledWith('/FaceCamera/guided/start', { streamUrl: 'rtsp://x', poseMode: 'full' })
    http.get.mockResolvedValue({ data: {} })
    await guidedEnrollmentApi.guidedProgress()
    expect(http.get).toHaveBeenCalledWith('/FaceCamera/guided/progress')
    http.post.mockResolvedValue({ data: {} })
    await guidedEnrollmentApi.guidedStop()
    expect(http.post).toHaveBeenCalledWith('/FaceCamera/guided/stop')
    await guidedEnrollmentApi.guidedConfirm(9)
    expect(http.post).toHaveBeenCalledWith('/FaceCamera/guided/confirm', { subjectId: 9 })
  })
})
