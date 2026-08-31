import { flushPromises, mount } from '@vue/test-utils'
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
})

afterEach(() => {
  document.body.innerHTML = ''
  vi.useRealTimers()
})

describe('Biometrics', () => {
  it('loads the biometric overview on mount and renders metrics', async () => {
    const wrapper = mount(Biometrics)
    await flushPromises()
    expect(api.getBiometricOverview).toHaveBeenCalledWith({ query: undefined })
    expect(api.getFaceModelHealth).toHaveBeenCalled()
    expect(api.getFaceEnrollmentJobs).toHaveBeenCalled()
    expect(api.getAccessCredentials).toHaveBeenCalled()
    expect(api.getFaceCredentialBindings).toHaveBeenCalled()
    expect(wrapper.text()).toContain('12')
    expect(wrapper.text()).toContain('A')
    expect(wrapper.text()).toContain('Chưa có chức vụ')
    expect(wrapper.text()).toContain('Chưa gán phòng ban')
    expect(wrapper.text()).toContain('Chưa có dữ liệu')
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

  it('paginates employees and renders pagination controls', async () => {
    const many = Array.from({ length: 25 }, (_, i) => ({ employeeId: i + 1, fullName: `E${i}`, modelCount: 0, videoCount: 0 }))
    api.getBiometricOverview.mockResolvedValue({ data: { summary: {}, employees: many, recentModels: [], recentVideos: [] } })
    const wrapper = mount(Biometrics)
    await flushPromises()
    expect(wrapper.vm.bTotalPages).toBe(3)
    expect(wrapper.text()).toContain('Hiển thị 1–10 / 25')
    const prevBtn = wrapper.findAll('.page-btn')[0]
    const nextBtn = wrapper.findAll('.page-btn')[wrapper.findAll('.page-btn').length - 1]
    await nextBtn.trigger('click')
    await flushPromises()
    expect(wrapper.vm.bCurrentPage).toBe(2)
    expect(wrapper.vm.bPagStart).toBe(11)
    expect(wrapper.vm.bPagEnd).toBe(20)
    // click a specific page button
    const pageBtn = wrapper.findAll('.page-btn').find((b) => b.text() === '3')
    await pageBtn.trigger('click')
    expect(wrapper.vm.bCurrentPage).toBe(3)
    await prevBtn.trigger('click')
    expect(wrapper.vm.bCurrentPage).toBe(2)
    expect(wrapper.findAll('.page-btn')[0].attributes('disabled')).toBeDefined()
  })

  it('renders access credentials and face credential bindings tables', async () => {
    api.getAccessCredentials.mockResolvedValue({
      data: [
        { id: 1, employeeId: 1, employeeName: 'A', credentialType: 'Face', storedStatus: 'Stored', effectiveStatus: 'Active', maskedIdentifier: 'abc***', effectiveFromUtc: '2026-01-01T08:00:00Z', expiresAtUtc: '2027-01-01T08:00:00Z' },
        { id: 2, employeeId: 2, employeeName: 'B', credentialType: 'Pin', storedStatus: 'Stored', effectiveStatus: 'Active', maskedIdentifier: null, effectiveFromUtc: null, expiresAtUtc: null },
      ],
    })
    api.getFaceCredentialBindings.mockResolvedValue({
      data: [
        { id: 5, employeeId: 1, employeeName: 'A', credentialType: 'Face', accessCredentialId: 9, bindingStatus: 'Active', credentialEffectiveStatus: 'Active', maskedIdentifier: 'xyz', activatedAtUtc: '2026-01-01T08:00:00Z', revokedAtUtc: null },
      ],
    })
    const wrapper = mount(Biometrics)
    await flushPromises()
    expect(wrapper.text()).toContain('2 thông tin đăng nhập')
    expect(wrapper.text()).toContain('Không lưu identifier')
    expect(wrapper.text()).toContain('1 liên kết')
    expect(wrapper.text()).toContain('abc***')
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
    api.getEmployeeVideos.mockResolvedValue({ data: [{ id: 7, fileName: 'v.mp4', createdAt: '2026-01-01T08:00:00Z' }] })
    api.getEmployeeFaceCredentialBinding.mockResolvedValue({ data: { id: 3, bindingStatus: 'Active', credentialEffectiveStatus: 'Active' } })
    api.getEmployeeFaceCredentialCandidates.mockResolvedValue({ data: [{ accessCredentialId: 9, credentialType: 'Face', candidateClassification: 'Ready' }] })
    const wrapper = mount(Biometrics)
    await flushPromises()
    wrapper.vm.selectedEmployeeId = 1
    await wrapper.vm.loadEmployeeVideos()
    expect(api.getEmployeeVideos).toHaveBeenCalledWith(1)
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
    expect(api.getEmployeeVideos).not.toHaveBeenCalled()
  })

  it('handles rejected video/binding/candidate promises via allSettled', async () => {
    api.getEmployeeVideos.mockRejectedValue(new Error('v'))
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
        { jobId: 10, employeeName: 'A', status: 'Ready', canActivate: false, canCancel: true, canRetry: true, createdAtUtc: '2026-01-01T08:00:00Z', attemptCount: 1, usableFrameCount: 5, encodingCount: 2, qualityScore: 0.8, duplicateSubjectId: null, failureMessage: null },
        { jobId: 11, employeeName: 'B', status: 'Completed', canActivate: true, canCancel: false, canRetry: false, createdAtUtc: '2026-01-01T08:00:00Z', attemptCount: 1, duplicateSubjectId: 99, failureCode: 'DUP', failureMessage: 'Trùng' },
        { jobId: 12, employeeName: 'C', status: 'Failed', canActivate: false, canCancel: false, canRetry: true, createdAtUtc: '2026-01-01T08:00:00Z', attemptCount: 3, usableFrameCount: null, encodingCount: null, qualityScore: null },
      ],
    })
    const wrapper = mount(Biometrics)
    await flushPromises()
    await wrapper.vm.runJobAction({ jobId: 11 }, 'activate')
    expect(api.activateFaceEnrollmentJob).toHaveBeenCalledWith(11)
    await wrapper.vm.runJobAction({ jobId: 10 }, 'cancel')
    expect(api.cancelFaceEnrollmentJob).toHaveBeenCalledWith(10)
    await wrapper.vm.runJobAction({ jobId: 12 }, 'retry')
    expect(api.retryFaceEnrollmentJob).toHaveBeenCalledWith(12)
    expect(wrapper.text()).toContain('Trùng chủ thể')
    expect(wrapper.text()).toContain('Kích hoạt')
    expect(wrapper.text()).toContain('Hủy')
    expect(wrapper.text()).toContain('Thử lại')
  })

  it('renders face models table including runtime unavailable state', async () => {
    api.getFaceModelHealth.mockResolvedValue({
      data: {
        registryVersion: '2.0',
        models: [
          { id: 1, employeeName: 'A', modelFileName: 'a.model', version: 3, status: 'Active', encodingCount: 4, checksumPrefix: 'aa11', activatedAtUtc: '2026-01-01T08:00:00Z', registrySyncState: 'Synced' },
          { id: 2, employeeName: 'B', modelFileName: 'b.model', version: null, status: null, encodingCount: null, checksumPrefix: null, activatedAtUtc: null, registrySyncState: 'RuntimeUnavailable' },
        ],
      },
    })
    const wrapper = mount(Biometrics)
    await flushPromises()
    expect(wrapper.text()).toContain('Registry v2.0')
    expect(wrapper.text()).toContain('Runtime không khả dụng')
    expect(wrapper.text()).toContain('Thiếu metadata')
    expect(wrapper.vm.modelRuntimeUnavailable).toBe(true)
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
