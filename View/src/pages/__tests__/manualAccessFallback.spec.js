import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/deviceManagementApi', () => ({ getGates: vi.fn() }))
vi.mock('../../services/employeeApi', () => ({ getAll: vi.fn(), getProtectedFaceImage: vi.fn() }))
vi.mock('../../services/guestProfileApi', () => ({ getVisitorDirectory: vi.fn() }))
vi.mock('../../services/http', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn(), patch: vi.fn(), delete: vi.fn(), request: vi.fn() },
}))

const deviceManagementApi = await import('../../services/deviceManagementApi')
const employeeApi = await import('../../services/employeeApi')
const guestProfileApi = await import('../../services/guestProfileApi')
const http = (await import('../../services/http')).default
const ManualAccessFallback = (await import('../ManualAccessFallback.vue')).default

beforeEach(() => vi.clearAllMocks())

async function mountPage() {
  deviceManagementApi.getGates.mockResolvedValue({ data: [{ gateId: 1, gateName: 'Cổng A' }] })
  const wrapper = mount(ManualAccessFallback)
  await flushPromises()
  return wrapper
}

describe('ManualAccessFallback', () => {
  it('loads gates on mount and employees on search', async () => {
    employeeApi.getAll.mockResolvedValue({ data: [{ employeeId: 7, fullName: 'Nguyễn An' }] })
    guestProfileApi.getVisitorDirectory.mockResolvedValue({ data: { items: [] } })
    const wrapper = await mountPage()
    expect(deviceManagementApi.getGates).toHaveBeenCalled()
    await wrapper.findAll('.search-box input')[0].setValue('An')
    await flushPromises()
    expect(employeeApi.getAll).toHaveBeenCalledWith(expect.objectContaining({ name: 'An' }))
  })

  it('onEmpSearch clears results when the query is too short', async () => {
    const wrapper = await mountPage()
    wrapper.vm.empQ = 'A'
    await wrapper.vm.onEmpSearch()
    expect(wrapper.vm.empResults).toEqual([])
  })

  it('onEmpSearch handles API errors', async () => {
    const wrapper = await mountPage()
    employeeApi.getAll.mockRejectedValue(new Error('boom'))
    wrapper.vm.empQ = 'Nguyễn'
    await wrapper.vm.onEmpSearch()
    expect(wrapper.vm.empResults).toEqual([])
  })

  it('pickEmp selects an employee and shows the subject card', async () => {
    const wrapper = await mountPage()
    employeeApi.getAll.mockResolvedValue({ data: { items: [{ employeeId: 7, fullName: 'Nguyễn An', department: 'Vận hành', faceImageUrl: 'http://x/a.jpg' }] } })
    wrapper.vm.empQ = 'Nguyễn'
    await wrapper.vm.onEmpSearch()
    await flushPromises()
    await wrapper.vm.pickEmp({ employeeId: 7, fullName: 'Nguyễn An', department: 'CNTT', faceImageUrl: 'http://x/a.jpg' })
    expect(wrapper.vm.subject.displayName).toBe('Nguyễn An')
    expect(wrapper.vm.faceImg).toBe('')
    expect(wrapper.find('.maf-photo-card').exists()).toBe(true)
  })

  it('pickEmp fetches a protected face image and revokes on unmount', async () => {
    const wrapper = await mountPage()
    const urlSpy = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:face')
    const revokeSpy = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {})
    employeeApi.getProtectedFaceImage.mockResolvedValue({ data: 'blobdata' })
    await wrapper.vm.pickEmp({ employeeId: 7, fullName: 'NV', faceImageUrl: '/protected/7' })
    expect(employeeApi.getProtectedFaceImage).toHaveBeenCalledWith(7)
    expect(wrapper.vm.faceImg).toBe('blob:face')
    wrapper.unmount()
    expect(revokeSpy).toHaveBeenCalled()
    urlSpy.mockRestore()
    revokeSpy.mockRestore()
  })

  it('pickEmp handles a protected face fetch error', async () => {
    const wrapper = await mountPage()
    employeeApi.getProtectedFaceImage.mockRejectedValue(new Error('x'))
    await wrapper.vm.pickEmp({ employeeId: 7, fullName: 'NV', faceImageUrl: '/protected/7' })
    expect(wrapper.vm.faceImg).toBe('')
  })

  it('pickVis selects a visitor', async () => {
    const wrapper = await mountPage()
    await wrapper.vm.switchTab('visitor')
    await wrapper.vm.pickVis({ visitorDetailId: 9, fullName: 'Khách A', guestPhone: '090', hostEmployeeName: 'Host', idCardNumber: '123' })
    expect(wrapper.vm.subject.idValue).toBe(9)
    expect(wrapper.vm.extraInfo).toContain('SĐT')
    expect(wrapper.vm.idLabel).toBe('Mã KH')
  })

  it('onVisSearch clears results for a short query', async () => {
    const wrapper = await mountPage()
    wrapper.vm.tab = 'visitor'
    wrapper.vm.visQ = 'K'
    await wrapper.vm.onVisSearch()
    expect(wrapper.vm.visResults).toEqual([])
  })

  it('onVisSearch loads visitor results and handles errors', async () => {
    const wrapper = await mountPage()
    wrapper.vm.tab = 'visitor'
    guestProfileApi.getVisitorDirectory.mockResolvedValue({ data: { items: [{ visitorDetailId: 9, fullName: 'Khách' }] } })
    wrapper.vm.visQ = 'Khách'
    await wrapper.vm.onVisSearch()
    await flushPromises()
    expect(wrapper.vm.visResults).toHaveLength(1)
    guestProfileApi.getVisitorDirectory.mockRejectedValue(new Error('boom'))
    wrapper.vm.visQ = 'Khách'
    await wrapper.vm.onVisSearch()
    await flushPromises()
    expect(wrapper.vm.visResults).toEqual([])
    expect(wrapper.vm.visLoading).toBe(false)
  })

  it('switchTab switches to visitor and clears the subject', async () => {
    const wrapper = await mountPage()
    wrapper.vm.subject = { displayName: 'X' }
    await wrapper.vm.switchTab('visitor')
    expect(wrapper.vm.tab).toBe('visitor')
    expect(wrapper.vm.subject).toBe(null)
    expect(wrapper.vm.idLabel).toBe('Mã KH')
  })

  it('submits an allow decision for an employee via UI', async () => {
    employeeApi.getAll.mockResolvedValue({ data: [{ employeeId: 7, fullName: 'Nguyễn An' }] })
    guestProfileApi.getVisitorDirectory.mockResolvedValue({ data: { items: [] } })
    http.post.mockResolvedValue({ data: {} })
    const wrapper = await mountPage()
    await wrapper.findAll('select')[0].setValue('1')
    await wrapper.findAll('.search-box input')[0].setValue('An')
    await flushPromises()
    await wrapper.find('.dropdown-item').trigger('click')
    await wrapper.find('.maf-btn-allow').trigger('click')
    await flushPromises()
    expect(http.post).toHaveBeenCalledWith('/QrAccess/manual-access', expect.objectContaining({ gateId: 1, employeeId: 7, isDenied: false }))
    expect(wrapper.vm.resultOk).toBe(true)
  })

  it('submits a deny decision for an employee', async () => {
    const wrapper = await mountPage()
    http.post.mockResolvedValue({ data: {} })
    wrapper.vm.gateId = '1'
    await wrapper.vm.pickEmp({ employeeId: 7, fullName: 'NV' })
    await wrapper.vm.submitDecision(false)
    expect(http.post).toHaveBeenCalledWith('/QrAccess/manual-access', expect.objectContaining({ gateId: 1, employeeId: 7, isDenied: true }))
    expect(wrapper.vm.photoClass).toBe('border-ok')
  })

  it('submitDecision handles API errors with the default message', async () => {
    const wrapper = await mountPage()
    http.post.mockRejectedValue({ response: { data: { message: 'no access' } } })
    wrapper.vm.gateId = '1'
    await wrapper.vm.pickEmp({ employeeId: 7, fullName: 'NV' })
    await wrapper.vm.submitDecision(true)
    expect(wrapper.vm.resultOk).toBe(false)
    expect(wrapper.vm.photoClass).toBe('border-fail')
  })

  it('submitDecision uses a fallback message when the response has none', async () => {
    const wrapper = await mountPage()
    http.post.mockRejectedValue(new Error('raw'))
    wrapper.vm.gateId = '1'
    await wrapper.vm.pickEmp({ employeeId: 7, fullName: 'NV' })
    await wrapper.vm.submitDecision(true)
    expect(wrapper.vm.resultMsg).toBe('Từ chối — người này không có quyền vào khu vực này')
  })

  it('submitDecision submits by visitor id', async () => {
    const wrapper = await mountPage()
    http.post.mockResolvedValue({ data: {} })
    wrapper.vm.gateId = '1'
    await wrapper.vm.pickVis({ visitorDetailId: 9, fullName: 'Khách' })
    await wrapper.vm.submitDecision(true)
    expect(http.post).toHaveBeenCalledWith('/QrAccess/manual-access', expect.objectContaining({ visitorDetailId: 9 }))
  })

  it('initials and idLabel computeds behave correctly', async () => {
    const wrapper = await mountPage()
    expect(wrapper.vm.initials).toBe('')
    await wrapper.vm.pickEmp({ employeeId: 7, fullName: 'Nguyễn Văn An' })
    expect(wrapper.vm.initials).toBe('NA')
    expect(wrapper.vm.idLabel).toBe('Mã NV')
    await wrapper.vm.pickEmp({ employeeId: 8, fullName: 'Son' })
    expect(wrapper.vm.initials).toBe('S')
  })

  it('fullReset clears the gate and subject', async () => {
    const wrapper = await mountPage()
    wrapper.vm.gateId = '1'
    await wrapper.vm.pickEmp({ employeeId: 7, fullName: 'NV' })
    await wrapper.vm.fullReset()
    expect(wrapper.vm.gateId).toBe('')
    expect(wrapper.vm.subject).toBe(null)
  })

  it('clearSubject revokes the current face image', async () => {
    const wrapper = await mountPage()
    const revokeSpy = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {})
    wrapper.vm.faceImg = 'blob:x'
    await wrapper.vm.pickEmp({ employeeId: 7, fullName: 'NV' })
    await wrapper.vm.clearSubject()
    expect(wrapper.vm.subject).toBe(null)
    wrapper.vm.faceImg = 'blob:x'
    await wrapper.vm.clearSubject()
    expect(revokeSpy).toHaveBeenCalled()
    revokeSpy.mockRestore()
  })
})
