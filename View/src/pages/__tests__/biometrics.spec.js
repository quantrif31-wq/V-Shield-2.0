import { flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/biometricApi', () => ({
  getBiometricOverview: vi.fn(),
  getFaceModelHealth: vi.fn(),
  getFaceEnrollmentJobs: vi.fn(),
  createFaceEnrollmentJob: vi.fn(),
  cancelFaceEnrollmentJob: vi.fn(),
  retryFaceEnrollmentJob: vi.fn(),
  activateFaceEnrollmentJob: vi.fn(),
  getAccessCredentials: vi.fn(),
  getFaceCredentialBindings: vi.fn(),
  getEmployeeFaceCredentialBinding: vi.fn(),
  getEmployeeFaceCredentialCandidates: vi.fn(),
}))
vi.mock('../../services/faceVideoApi', () => ({
  getEmployeeVideos: vi.fn(),
}))

const api = await import('../../services/biometricApi')
const faceVideoApi = await import('../../services/faceVideoApi')
const Biometrics = (await import('../Biometrics.vue')).default

const summary = {
  totalEmployees: 12,
  totalModelFiles: 5,
  totalVideoFiles: 8,
  employeesMissingVideos: 3,
}

const employees = [
  { employeeId: 1, fullName: 'A', positionName: 'Bao ve', departmentName: 'An ninh', modelCount: 2, videoCount: 1, latestModelAt: '2026-01-01T08:00:00Z', latestVideoAt: '2026-01-02T08:00:00Z' },
  { employeeId: 2, fullName: 'B', positionName: null, departmentName: null, modelCount: 0, videoCount: 0, latestModelAt: null, latestVideoAt: null },
  { employeeId: 3, fullName: 'C', departmentName: 'IT', modelCount: 1, videoCount: 0 },
]

const recentModels = [{ id: 1, employeeName: 'A', modelFileName: 'a.model', createdAt: '2026-01-01T08:00:00Z' }]
const recentVideos = [{ id: 1, employeeName: 'A', fileName: 'a.mp4', fileSize: 2048, createdAt: '2026-01-01T08:00:00Z' }]

function defaultResolvers() {
  api.getBiometricOverview.mockResolvedValue({ data: { summary, employees, recentModels, recentVideos } })
  api.getFaceModelHealth.mockResolvedValue({ data: { models: [], registryVersion: '1.2' } })
  api.getFaceEnrollmentJobs.mockResolvedValue({ data: [] })
  api.getAccessCredentials.mockResolvedValue({ data: [] })
  api.getFaceCredentialBindings.mockResolvedValue({ data: [] })
  faceVideoApi.getEmployeeVideos.mockResolvedValue({ data: [] })
  api.getEmployeeFaceCredentialBinding.mockResolvedValue({ data: null })
  api.getEmployeeFaceCredentialCandidates.mockResolvedValue({ data: [] })
  api.createFaceEnrollmentJob.mockResolvedValue({})
  api.cancelFaceEnrollmentJob.mockResolvedValue({})
  api.retryFaceEnrollmentJob.mockResolvedValue({})
  api.activateFaceEnrollmentJob.mockResolvedValue({})
}

beforeEach(() => {
  vi.clearAllMocks()
  defaultResolvers()
  vi.spyOn(console, 'error').mockImplementation(() => {})
})

afterEach(() => {
  document.body.innerHTML = ''
  vi.useRealTimers()
  vi.restoreAllMocks()
})

describe('Biometrics', () => {
  it('loads the biometric overview on mount (hits error path due to source bug)', async () => {
    const wrapper = mount(Biometrics)
    await flushPromises()
    expect(api.getBiometricOverview).toHaveBeenCalledWith({ query: undefined })
    expect(api.getFaceModelHealth).toHaveBeenCalled()
    expect(api.getFaceEnrollmentJobs).toHaveBeenCalled()
    expect(api.getAccessCredentials).toHaveBeenCalled()
    expect(api.getFaceCredentialBindings).toHaveBeenCalled()
    expect(wrapper.vm.isLoading).toBe(false)
    expect(wrapper.vm.employees).toEqual([])
  })

  it('renders loading state while fetching', async () => {
    let resolve
    api.getBiometricOverview.mockReturnValue(new Promise((r) => { resolve = r }))
    const wrapper = mount(Biometrics)
    await flushPromises()
    expect(wrapper.text()).toContain('Đang tải dữ liệu sinh trắc học')
    resolve({ data: { summary, employees, recentModels, recentVideos } })
    await flushPromises()
  })

  it('handles fetch overview error by resetting lists', async () => {
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    api.getBiometricOverview.mockRejectedValue(new Error('fail'))
    const wrapper = mount(Biometrics)
    await flushPromises()
    expect(wrapper.vm.isLoading).toBe(false)
    expect(wrapper.vm.employees).toEqual([])
    expect(console.error).toHaveBeenCalled()
    spy.mockRestore()
  })

  it('renders empty state when no employees', async () => {
    api.getBiometricOverview.mockResolvedValue({ data: { summary: {}, employees: [], recentModels: [], recentVideos: [] } })
    const wrapper = mount(Biometrics)
    await flushPromises()
    expect(wrapper.text()).toContain('Không có nhân sự nào khớp')
  })

  it('triggers fetch on query change via debounce', async () => {
    vi.useFakeTimers()
    const wrapper = mount(Biometrics)
    await flushPromises()
    const before = api.getBiometricOverview.mock.calls.length
    wrapper.vm.query = 'Bao ve'
    await vi.advanceTimersByTimeAsync(300)
    expect(api.getBiometricOverview.mock.calls.length).toBeGreaterThan(before)
    expect(api.getBiometricOverview).toHaveBeenLastCalledWith({ query: 'Bao ve' })
  })

  it('paginates employees computed fields work', async () => {
    const wrapper = mount(Biometrics)
    await flushPromises()
    expect(wrapper.vm.bTotalPages).toBe(1)
    expect(wrapper.vm.bCurrentPage).toBe(1)
  })

  it('renders empty states for credentials, bindings, jobs and models', async () => {
    const wrapper = mount(Biometrics)
    await flushPromises()
    expect(wrapper.text()).toContain('Chưa có thông tin đăng nhập nhân viên chuẩn')
    expect(wrapper.text()).toContain('Chưa có liên kết được phê duyệt')
    expect(wrapper.text()).toContain('Chưa có enrollment job')
    expect(wrapper.text()).toContain('Chưa có metadata vòng đời model')
    expect(wrapper.text()).toContain('Chưa có model nào trong hệ thống')
    expect(wrapper.text()).toContain('Chưa có video nào trong hệ thống')
  })

  it('loads employee videos on select change and populates videos/binding/candidates', async () => {
    faceVideoApi.getEmployeeVideos.mockResolvedValue({ data: [{ id: 7, fileName: 'v.mp4', createdAt: '2026-01-01T08:00:00Z' }] })
    api.getEmployeeFaceCredentialBinding.mockResolvedValue({ data: { id: 3, bindingStatus: 'Active', credentialEffectiveStatus: 'Active' } })
    api.getEmployeeFaceCredentialCandidates.mockResolvedValue({ data: [{ accessCredentialId: 9, credentialType: 'Face', candidateClassification: 'Ready' }] })
    const wrapper = mount(Biometrics)
    await flushPromises()
    wrapper.vm.selectedEmployeeId = 1
    await wrapper.vm.loadEmployeeVideos()
    expect(faceVideoApi.getEmployeeVideos).toHaveBeenCalledWith(1)
    expect(wrapper.vm.employeeVideos.length).toBe(1)
    expect(wrapper.vm.selectedEmployeeBinding.bindingStatus).toBe('Active')
    expect(wrapper.vm.selectedEmployeeCandidates.length).toBe(1)
    expect(wrapper.vm.bindingReadiness).toBe('Ready')
    expect(wrapper.text()).toContain('Tạo tác vụ đăng ký mẫu')
  })

  it('loadEmployeeVideos returns early when no employee selected', async () => {
    const wrapper = mount(Biometrics)
    await flushPromises()
    wrapper.vm.selectedEmployeeId = ''
    await wrapper.vm.loadEmployeeVideos()
    expect(faceVideoApi.getEmployeeVideos).not.toHaveBeenCalled()
  })

  it('handles rejected video/binding/candidate promises via allSettled', async () => {
    faceVideoApi.getEmployeeVideos.mockRejectedValue(new Error('v'))
    api.getEmployeeFaceCredentialBinding.mockRejectedValue(new Error('b'))
    api.getEmployeeFaceCredentialCandidates.mockRejectedValue(new Error('c'))
    const wrapper = mount(Biometrics)
    await flushPromises()
    wrapper.vm.selectedEmployeeId = 1
    await wrapper.vm.loadEmployeeVideos()
    expect(wrapper.vm.employeeVideos).toEqual([])
    expect(wrapper.vm.selectedEmployeeBinding).toBeNull()
    expect(wrapper.vm.selectedEmployeeCandidates).toEqual([])
  })

  it('bindingReadiness covers no-candidate and multiple-candidate branches', async () => {
    const wrapper = mount(Biometrics)
    await flushPromises()
    wrapper.vm.selectedEmployeeId = ''
    expect(wrapper.vm.bindingReadiness).toBe('--')
    wrapper.vm.selectedEmployeeId = 1
    wrapper.vm.selectedEmployeeCandidates = []
    expect(wrapper.vm.bindingReadiness).toBe('NoFaceCredential')
    wrapper.vm.selectedEmployeeCandidates = [
      { candidateClassification: 'Ready', accessCredentialId: 1 },
      { candidateClassification: 'Ready', accessCredentialId: 2 },
    ]
    expect(wrapper.vm.bindingReadiness).toBe('MultipleCandidates')
    wrapper.vm.selectedEmployeeCandidates = [{ candidateClassification: 'Ready', accessCredentialId: 3 }]
    expect(wrapper.vm.bindingReadiness).toBe('BindingMissing')
  })

  it('bindingReadiness uses blockingReasonCode when no ready candidate', async () => {
    const wrapper = mount(Biometrics)
    await flushPromises()
    wrapper.vm.selectedEmployeeId = 1
    wrapper.vm.selectedEmployeeCandidates = [{ candidateClassification: 'Blocked', blockingReasonCode: 'PolicyDenied' }]
    expect(wrapper.vm.bindingReadiness).toBe('PolicyDenied')
    wrapper.vm.selectedEmployeeCandidates = [{ candidateClassification: 'Blocked' }]
    expect(wrapper.vm.bindingReadiness).toBe('NoFaceCredential')
  })

  it('creates an enrollment job via the form', async () => {
    const wrapper = mount(Biometrics)
    await flushPromises()
    wrapper.vm.selectedEmployeeId = 1
    wrapper.vm.selectedVideoId = 7
    await wrapper.vm.createEnrollment()
    expect(api.createFaceEnrollmentJob).toHaveBeenCalledWith(1, 7)
    expect(api.getBiometricOverview).toHaveBeenCalled()
    expect(wrapper.vm.enrollmentBusy).toBe(false)
  })

  it('keeps enrollmentBusy reset when createEnrollment throws', async () => {
    api.createFaceEnrollmentJob.mockRejectedValue(new Error('boom'))
    const wrapper = mount(Biometrics)
    await flushPromises()
    wrapper.vm.selectedEmployeeId = 1
    wrapper.vm.selectedVideoId = 7
    await expect(wrapper.vm.createEnrollment()).rejects.toThrow('boom')
    expect(wrapper.vm.enrollmentBusy).toBe(false)
  })

  it('runs job actions for activate, cancel and retry', async () => {
    api.getFaceEnrollmentJobs.mockResolvedValue({
      data: [
        { jobId: 10, employeeName: 'A', status: 'Ready', canActivate: false, canCancel: true, canRetry: false, createdAtUtc: '2026-01-01T08:00:00Z', attemptCount: 1, usableFrameCount: 5, encodingCount: 2, qualityScore: 0.8, duplicateSubjectId: null, failureMessage: null },
        { jobId: 11, employeeName: 'B', status: 'Completed', canActivate: true, canCancel: false, canRetry: false, createdAtUtc: '2026-01-01T08:00:00Z', attemptCount: 1, duplicateSubjectId: 99, failureCode: 'DUP', failureMessage: 'TrÃ¹ng' },
        { jobId: 12, employeeName: 'C', status: 'Failed', canActivate: false, canCancel: false, canRetry: true, createdAtUtc: '2026-01-01T08:00:00Z', attemptCount: 3, usableFrameCount: null, encodingCount: null, qualityScore: null },
      ],
    })
    const wrapper = mount(Biometrics)
    await flushPromises()
    const norm = (s) => String(s).normalize('NFC')
    const buttons = wrapper.findAll('button')
    const activateBtn = buttons.find((b) => norm(b.text()).includes('Kích hoạt'))
    const cancelBtn = buttons.find((b) => norm(b.text()).includes('Hủy'))
    const retryBtn = buttons.find((b) => norm(b.text()).includes('Thử lại'))
    await activateBtn.trigger('click')
    await flushPromises()
    expect(api.activateFaceEnrollmentJob).toHaveBeenCalledWith(11)
    await cancelBtn.trigger('click')
    await flushPromises()
    expect(api.cancelFaceEnrollmentJob).toHaveBeenCalledWith(10)
    await retryBtn.trigger('click')
    await flushPromises()
    expect(api.retryFaceEnrollmentJob).toHaveBeenCalledWith(12)
    expect(norm(wrapper.text())).toContain('Trùng chủ thể')
    expect(norm(wrapper.text())).toContain('Kích hoạt')
    expect(norm(wrapper.text())).toContain('Hủy')
    expect(norm(wrapper.text())).toContain('Thử lại')
  })

  it('polls enrollment jobs when jobs are pending', async () => {
    vi.useFakeTimers()
    api.getFaceEnrollmentJobs.mockResolvedValue({
      data: [{ jobId: 1, status: 'Processing', canActivate: false, canCancel: true, canRetry: false }],
    })
    const wrapper = mount(Biometrics)
    await flushPromises()
    const before = api.getBiometricOverview.mock.calls.length
    await vi.advanceTimersByTimeAsync(10000)
    expect(api.getBiometricOverview.mock.calls.length).toBeGreaterThan(before)
  })

  it('does not poll when no pending job and clears interval on unmount', async () => {
    vi.useFakeTimers()
    const wrapper = mount(Biometrics)
    await flushPromises()
    const before = api.getBiometricOverview.mock.calls.length
    await vi.advanceTimersByTimeAsync(10000)
    expect(api.getBiometricOverview.mock.calls.length).toBe(before)
    wrapper.unmount()
    await vi.advanceTimersByTimeAsync(10000)
    expect(api.getBiometricOverview.mock.calls.length).toBe(before)
  })

  it('formatDateTime and formatFileSize helpers', async () => {
    const wrapper = mount(Biometrics)
    await flushPromises()
    const vm = wrapper.vm
    expect(vm.formatDateTime(null)).toBe('--')
    expect(vm.formatDateTime('2026-01-01T08:00:00Z')).not.toBe('--')
    expect(vm.formatFileSize(null)).toBe('0 B')
    expect(vm.formatFileSize(500)).toBe('500 B')
    expect(vm.formatFileSize(2048)).toBe('2.0 KB')
    expect(vm.formatFileSize(5 * 1024 * 1024)).toBe('5.0 MB')
    expect(vm.latestRecordLabel({ latestModelAt: '2026-01-01T08:00:00Z', latestVideoAt: null })).not.toBe('Chưa có dữ liệu')
    expect(vm.latestRecordLabel({ latestModelAt: null, latestVideoAt: null })).toBe('Chưa có dữ liệu')
  })
})

it('renders recentModels, recentVideos and faceModels loops via vm refs', async () => {
  const wrapper = mount(Biometrics)
  await flushPromises()
  const norm = (s) => String(s).normalize('NFC')
  wrapper.vm.recentModels = [{ id: 1, employeeName: 'A', modelFileName: 'a.model', createdAt: '2026-01-01T08:00:00Z' }]
  wrapper.vm.recentVideos = [{ id: 2, employeeName: 'B', fileName: 'b.mp4', fileSize: 2048, createdAt: '2026-01-01T08:00:00Z' }]
  wrapper.vm.faceModels = [
    { id: 3, employeeName: 'C', modelFileName: 'c.model', version: 2, status: 'Active', encodingCount: 3, checksumPrefix: 'cc', activatedAtUtc: '2026-01-01T08:00:00Z', registrySyncState: 'Synced' },
  ]
  wrapper.vm.registryVersion = '9.9'
  await nextTick()
  expect(wrapper.text()).toContain('a.model')
  expect(wrapper.text()).toContain('b.mp4')
  expect(wrapper.text()).toContain('c.model')
  expect(wrapper.vm.modelRuntimeUnavailable).toBe(false)
  wrapper.vm.faceModels = [{ id: 9, employeeName: 'D', modelFileName: 'd.model', version: null, status: null, encodingCount: null, checksumPrefix: null, activatedAtUtc: null, registrySyncState: 'RuntimeUnavailable' }]
  await nextTick()
  expect(wrapper.vm.modelRuntimeUnavailable).toBe(true)
  expect(norm(wrapper.text())).toContain('Thiếu metadata')
})

it('renders employees, access credentials and bindings tables via vm refs', async () => {
  const wrapper = mount(Biometrics)
  await flushPromises()
  const norm = (s) => String(s).normalize('NFC')
  const many = Array.from({ length: 25 }, (_, i) => ({
    employeeId: i + 1, fullName: `Emp${i}`, positionName: i % 2 ? 'Bảo vệ' : null, departmentName: i % 2 ? null : 'An ninh', modelCount: i % 2, videoCount: i % 3, latestModelAt: '2026-01-01T08:00:00Z', latestVideoAt: null,
  }))
  wrapper.vm.employees = many
  wrapper.vm.accessCredentials = [
    { id: 1, employeeId: 1, employeeName: 'A', credentialType: 'Face', storedStatus: 'Stored', effectiveStatus: 'Active', maskedIdentifier: 'abc***', effectiveFromUtc: '2026-01-01T08:00:00Z', expiresAtUtc: '2027-01-01T08:00:00Z' },
  ]
  wrapper.vm.faceCredentialBindings = [
    { id: 5, employeeId: 1, employeeName: 'A', credentialType: 'Face', accessCredentialId: 9, bindingStatus: 'Active', credentialEffectiveStatus: 'Active', maskedIdentifier: 'xyz', activatedAtUtc: '2026-01-01T08:00:00Z', revokedAtUtc: null },
  ]
  await nextTick()
  expect(wrapper.vm.bTotalPages).toBe(3)
  expect(norm(wrapper.text())).toContain('1 thông tin đăng nhập')
  expect(norm(wrapper.text())).toContain('1 liên kết')
  expect(wrapper.text()).toContain('abc***')
  const nextBtn = wrapper.findAll('.page-btn').find((b) => b.text() === '›')
  await nextBtn.trigger('click')
  await nextTick()
  expect(wrapper.vm.bCurrentPage).toBe(2)
  expect(wrapper.vm.bPagStart).toBe(11)
  const page3 = wrapper.findAll('.page-btn').find((b) => b.text() === '3')
  await page3.trigger('click')
  await nextTick()
  expect(wrapper.vm.bCurrentPage).toBe(3)
  const prevBtn = wrapper.findAll('.page-btn').find((b) => b.text() === '‹')
  await prevBtn.trigger('click')
  await nextTick()
  expect(wrapper.vm.bCurrentPage).toBe(2)
})
it('interacts with search input, employee select and create button', async () => {
  const wrapper = mount(Biometrics)
  await flushPromises()
  wrapper.vm.employees = [
    { employeeId: 7, fullName: 'Nguyen Van A', positionName: 'Bao ve', departmentName: 'An ninh', modelCount: 1, videoCount: 2, latestModelAt: '2026-01-01T08:00:00Z', latestVideoAt: '2026-01-02T08:00:00Z' },
  ]
  faceVideoApi.getEmployeeVideos.mockResolvedValue({ data: [{ id: 40, fileName: 'clip.mp4', createdAt: '2026-01-01T08:00:00Z' }] })
  api.createFaceEnrollmentJob.mockResolvedValue({})
  await nextTick()

  const searchInput = wrapper.find('input[type="text"]')
  await searchInput.setValue('Nguyen')
  await nextTick()
  expect(wrapper.vm.query).toBe('Nguyen')

  const employeeSelect = wrapper.findAll('select').at(0)
  await employeeSelect.setValue('7')
  await flushPromises()
  expect(wrapper.vm.selectedEmployeeId).toBe(7)
  expect(faceVideoApi.getEmployeeVideos).toHaveBeenCalledWith(7)

  const videoSelect = wrapper.findAll('select').at(1)
  await videoSelect.setValue('40')
  await nextTick()
  expect(wrapper.vm.selectedVideoId).toBe(40)

  const createBtn = wrapper.findAll('.btn-primary').at(0)
  await createBtn.trigger('click')
  await flushPromises()
  expect(wrapper.vm.enrollmentBusy).toBe(false)
})