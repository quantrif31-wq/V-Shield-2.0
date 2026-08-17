import { flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/cameraRuntimeApi', () => ({
  getCameras: vi.fn(),
  toggleRecording: vi.fn(),
}))
vi.mock('../../services/preRegistrationApi', () => ({ getLinks: vi.fn(), createLink: vi.fn() }))
vi.mock('../../services/employeeApi', () => ({ getAll: vi.fn(), getProtectedFaceImage: vi.fn() }))
vi.mock('../../config/api', () => ({ API_ORIGIN: 'http://localhost:5107' }))
vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getVideoBookmarks: vi.fn(),
    getClipRequests: vi.fn(),
    createVideoBookmark: vi.fn(),
    createClipRequest: vi.fn(),
    deleteVideoBookmark: vi.fn(),
    approveClipRequest: vi.fn(),
    exportClipRequest: vi.fn(),
  },
}))

const cameraRuntimeApi = await import('../../services/cameraRuntimeApi')
const preRegistrationApi = await import('../../services/preRegistrationApi')
const employeeApi = await import('../../services/employeeApi')
const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi

const Monitoring = (await import('../Monitoring.vue')).default
const VideoSearch = (await import('../VideoSearch.vue')).default
const RegistrationLinks = (await import('../RegistrationLinks.vue')).default

const routerLinkStub = { template: '<a><slot /></a>' }

beforeEach(() => {
  vi.clearAllMocks()
  vi.stubGlobal('URL', { ...window.URL, createObjectURL: vi.fn(() => 'blob:av'), revokeObjectURL: vi.fn() })
})
afterEach(() => vi.unstubAllGlobals())

describe('Monitoring', () => {
  const cameras = [
    { cameraId: 1, cameraName: 'CAM-01', urlView: 'http://10.0.0.5/video', isRecordingEnabled: false },
    { cameraId: 2, cameraName: 'CAM-02', urlView: 'http://10.0.0.6/video', isRecordingEnabled: true },
    { cameraId: 3, cameraName: 'CAM-03', urlView: '', isRecordingEnabled: false },
  ]

  it('auto-selects the first four cameras as active', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue(cameras)
    const wrapper = mount(Monitoring, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()
    expect(wrapper.text()).toContain('CAM-01')
    expect(wrapper.text()).toContain('CAM-02')
    expect(wrapper.find('.cam-card').exists()).toBe(true)
    expect(wrapper.findAll('.cam-card').length).toBe(3)
  })

  it('toggles recording on a camera', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue(cameras)
    cameraRuntimeApi.toggleRecording.mockResolvedValue({ isRecordingEnabled: true, recordingRetentionDays: 30 })
    const wrapper = mount(Monitoring, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()

    await wrapper.find('.gear-btn').trigger('click')
    const recToggle = wrapper.findAll('.rec-toggle input')[0]
    await recToggle.setValue(true)
    await flushPromises()
    expect(cameraRuntimeApi.toggleRecording).toHaveBeenCalledWith(1, true, null)
  })

  it('shows an empty grid when nothing is selected', async () => {
    cameraRuntimeApi.getCameras.mockResolvedValue([])
    const wrapper = mount(Monitoring, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()
    expect(wrapper.text()).toContain('Chưa chọn camera')
  })
})

describe('VideoSearch', () => {
  it('lists bookmarks and clip requests', async () => {
    enterpriseApi.getVideoBookmarks.mockResolvedValue({ data: [{ videoBookmarkId: 1, securityEventId: 5, cameraId: 2, startUtc: '2026-08-01T00:00:00Z', endUtc: '2026-08-01T00:05:00Z' }] })
    enterpriseApi.getClipRequests.mockResolvedValue({ data: [{ clipRequestId: 1, cameraId: 2, status: 'Pending', retentionCategory: 'Evidence', exportReference: 'ref' }] })
    const wrapper = mount(VideoSearch)
    await flushPromises()
    expect(wrapper.text()).toContain('5')
    expect(wrapper.text()).toContain('Chờ duyệt')
  })

  it('deletes a bookmark after confirmation', async () => {
    enterpriseApi.getVideoBookmarks.mockResolvedValue({ data: [{ videoBookmarkId: 1, securityEventId: 5, startUtc: '2026-08-01T00:00:00Z', endUtc: '2026-08-01T00:05:00Z' }] })
    enterpriseApi.getClipRequests.mockResolvedValue({ data: [] })
    const wrapper = mount(VideoSearch)
    await flushPromises()

    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    enterpriseApi.deleteVideoBookmark.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Xóa').trigger('click')
    await flushPromises()
    expect(enterpriseApi.deleteVideoBookmark).toHaveBeenCalledWith(1)
    confirmSpy.mockRestore()
  })

  it('approves a pending clip with a retention category', async () => {
    enterpriseApi.getVideoBookmarks.mockResolvedValue({ data: [] })
    enterpriseApi.getClipRequests.mockResolvedValue({ data: [{ clipRequestId: 2, cameraId: 1, status: 'Pending', retentionCategory: 'Evidence' }] })
    const wrapper = mount(VideoSearch)
    await flushPromises()

    const promptSpy = vi.spyOn(window, 'prompt').mockReturnValue('Evidence')
    enterpriseApi.approveClipRequest.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Duyệt').trigger('click')
    await flushPromises()
    expect(enterpriseApi.approveClipRequest).toHaveBeenCalledWith(2, { retentionCategory: 'Evidence' })
    promptSpy.mockRestore()
  })
})

describe('RegistrationLinks', () => {
  it('lists registration links and filters by query', async () => {
    preRegistrationApi.getLinks.mockResolvedValue({ data: [{ linkId: 1, hostEmployeeName: 'Nguyễn An', token: 'abc123', isUsed: false, isExpired: false, registrationUrl: 'http://x/register/abc', createdAt: '2026-08-01T00:00:00Z', expiredAt: null }] })
    employeeApi.getAll.mockResolvedValue({ data: [] })
    const wrapper = mount(RegistrationLinks, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()
    await nextTick()
    expect(preRegistrationApi.getLinks).toHaveBeenCalled()
    expect(wrapper.find('tbody').text()).toContain('Nguyễn An')
    expect(wrapper.find('tbody').text()).toContain('abc123')
    expect(wrapper.find('tbody').text()).toContain('Còn hiệu lực')

    await wrapper.find('input[type="text"]').setValue('abc')
    await flushPromises()
    expect(wrapper.find('tbody').text()).toContain('Nguyễn An')
    await wrapper.find('input[type="text"]').setValue('zzz')
    await flushPromises()
    expect(wrapper.text()).toContain('Chưa có link đăng ký nào phù hợp.')
  })

  it('creates a link for a selected host and copies it', async () => {
    vi.stubGlobal('navigator', { clipboard: { writeText: vi.fn().mockResolvedValue() } })
    preRegistrationApi.getLinks.mockResolvedValue({ data: [] })
    employeeApi.getAll.mockResolvedValue({ data: [{ employeeId: 7, fullName: 'Nguyễn An', departmentName: 'An Ninh' }] })
    const wrapper = mount(RegistrationLinks, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text() === 'Tạo link mới').trigger('click')
    await wrapper.find('.combobox-input').setValue('Nguyễn')
    await wrapper.find('.combobox-item').trigger('click')
    preRegistrationApi.createLink.mockResolvedValue({ data: { registrationUrl: 'http://x/register/newlink' } })
    await wrapper.findAll('button').find((b) => b.text() === 'Tạo link').trigger('click')
    await flushPromises()
    expect(preRegistrationApi.createLink).toHaveBeenCalledWith({ hostEmployeeId: 7, expiryHours: 24 })
    expect(navigator.clipboard.writeText).toHaveBeenCalledWith('http://x/register/newlink')
  })

  it('requires a host employee before creating a link', async () => {
    preRegistrationApi.getLinks.mockResolvedValue({ data: [] })
    employeeApi.getAll.mockResolvedValue({ data: [] })
    const wrapper = mount(RegistrationLinks, { global: { stubs: { RouterLink: routerLinkStub } } })
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Tạo link mới').trigger('click')
    await wrapper.findAll('button').find((b) => b.text() === 'Tạo link').trigger('click')
    await flushPromises()
    expect(preRegistrationApi.createLink).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Bạn cần chọn nhân sự host trước khi tạo link.')
  })
})
