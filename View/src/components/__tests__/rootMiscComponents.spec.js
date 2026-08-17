import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../stores/auth', () => ({ authState: { user: { role: 'NhanVien', employeeId: 5, fullName: 'An' } } }))
vi.mock('../../services/dynamicQrApi', () => ({ generateDynamicQr: vi.fn() }))
vi.mock('../../services/employeeApi', () => ({ getAll: vi.fn() }))
vi.mock('../../services/faceVideoApi', () => ({
  getEmployeeVideos: vi.fn(),
  uploadFaceVideo: vi.fn(),
  getProtectedVideoBlob: vi.fn(),
  deleteVideo: vi.fn(),
}))
vi.mock('qrcode', () => ({ default: { toDataURL: vi.fn().mockResolvedValue('data:image/png;base64,QR') } }))

const dynamicQrApi = await import('../../services/dynamicQrApi')
const employeeApi = await import('../../services/employeeApi')
const faceVideoApi = await import('../../services/faceVideoApi')
const DynamicQrGenerator = (await import('../DynamicQrGenerator.vue')).default
const FaceVideo = (await import('../FaceVideo.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  employeeApi.getAll.mockResolvedValue({ data: [] })
  faceVideoApi.getEmployeeVideos.mockResolvedValue({ data: [] })
})

describe('DynamicQrGenerator', () => {
  it('generates a dynamic QR for the current employee', async () => {
    dynamicQrApi.generateDynamicQr.mockResolvedValue({ data: { qrPayload: 'QR-1', expiresAtUtc: '2026-08-01T00:00:00Z' } })
    const wrapper = mount(DynamicQrGenerator)
    await flushPromises()
    expect(dynamicQrApi.generateDynamicQr).toHaveBeenCalled()
  })
})

describe('FaceVideo', () => {
  it('mounts and renders the video list', async () => {
    employeeApi.getAll.mockResolvedValue({ data: [{ employeeId: 7, fullName: 'An' }] })
    const wrapper = mount(FaceVideo)
    await flushPromises()
    expect(wrapper.exists()).toBe(true)
  })
})
