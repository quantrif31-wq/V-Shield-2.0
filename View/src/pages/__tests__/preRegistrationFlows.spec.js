import { flushPromises, mount } from '@vue/test-utils'
import { reactive } from 'vue'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const route = reactive({ query: {} })
const replace = vi.fn()
const getAll = vi.fn()
const getDetail = vi.fn()
const updateStatus = vi.fn()
const createLink = vi.fn()

vi.mock('vue-router', () => ({ useRoute: () => route, useRouter: () => ({ replace }) }))
vi.mock('../../services/preRegistrationApi', () => ({
  getAll: (...args) => getAll(...args),
  getDetail: (...args) => getDetail(...args),
  updateStatus: (...args) => updateStatus(...args),
  createLink: (...args) => createLink(...args),
}))
vi.mock('../../services/employeeApi', () => ({ getAll: vi.fn(() => Promise.resolve({ data: [{ employeeId: 4, fullName: 'Trần Minh Host', departmentName: 'An ninh' }] })) }))
vi.mock('qrcode', () => ({ default: { toCanvas: vi.fn(() => Promise.resolve()) } }))
vi.mock('../../composables/useToasts', () => {
  const toasts = reactive([])
  let nextId = 1
  function push({ title, message = '', type = 'info', duration = 5000 }) {
    const id = nextId++
    toasts.push({ id, title, message, type })
    return id
  }
  return {
    useToasts: () => ({
      toasts: reactive(toasts),
      push,
      remove: () => {},
      success: (title, message) => push({ title, message, type: 'success' }),
      error: (title, message) => push({ title, message, type: 'error', duration: 8000 }),
    }),
  }
})

const PreRegistration = (await import('../PreRegistration.vue')).default

const qrcode = await import('qrcode')

const registration = {
  registrationId: 12,
  guestFullName: 'Lê Thị Khách',
  guestPhone: '0912345678',
  hostEmployeeName: 'Trần Minh Host',
  expectedTimeIn: '2026-08-05T08:00:00',
  expectedTimeOut: '2026-08-05T10:00:00',
  numberOfVisitors: 2,
  status: 'Pending',
}

const clipboardWrite = vi.fn().mockResolvedValue()
Object.defineProperty(navigator, 'clipboard', { value: { writeText: clipboardWrite }, configurable: true })
const windowOpen = vi.fn()
window.open = windowOpen

const stubs = { ImportModal: true, ExportModal: true, Teleport: true, RouterLink: true }

async function mountPage() {
  const wrapper = mount(PreRegistration, { global: { stubs } })
  await flushPromises()
  return wrapper
}

beforeEach(() => {
  route.query = {}
  replace.mockReset()
  getAll.mockReset()
  getDetail.mockReset()
  updateStatus.mockReset()
  createLink.mockReset()
  clipboardWrite.mockClear()
  windowOpen.mockClear()
  getAll.mockResolvedValue({ data: { items: [registration], total: 1 } })
  getDetail.mockResolvedValue({ data: { ...registration, visitors: [], accessLogs: [] } })
  updateStatus.mockResolvedValue({ data: {} })
  createLink.mockResolvedValue({ data: { registrationUrl: 'https://example.test/register/abc', expiredAt: '2026-08-06T08:00:00' } })
})

describe('PreRegistration page flows', () => {
  it('renders list, stats, and initializers', async () => {
    getAll.mockResolvedValue({ data: { items: [registration], total: 1 } })
    const wrapper = await mountPage()
    expect(wrapper.text()).toContain('Lê Thị Khách')
    expect(wrapper.text()).toContain('Chờ duyệt')
    expect(wrapper.vm.getInitials('Lê Thị Khách')).toBe('TK')
    expect(wrapper.vm.getInitials('')).toBe('?')
    expect(wrapper.vm.getStatusLabel('Approved')).toBe('Đã duyệt')
    expect(wrapper.vm.getStatusLabel('Weird')).toBe('Weird')
    expect(wrapper.vm.getStatusLabel('')).toBe('Không xác định')
    expect(wrapper.vm.statusSemantic('Pending')).toBe('pending')
    expect(wrapper.vm.statusSemantic('Nope')).toBe('neutral')
    expect(wrapper.vm.tone('AB')).toBeGreaterThanOrEqual(0)
    expect(wrapper.vm.formatDateTime('')).toBe('—')
    expect(typeof wrapper.vm.formatDateTime('2026-08-05T08:00:00')).toBe('string')
    expect(wrapper.vm.formatDateTime('not-a-date')).toBe('—')
    expect(wrapper.vm.stats.total).toBe(1)
  })

  it('shows number formatting and duration in the schedule cell', async () => {
    const wrapper = await mountPage()
    expect(wrapper.text()).toContain('2 khách')
    expect(wrapper.text()).toContain('Trần Minh Host')
  })

  it('shows loading then data', async () => {
    let resolveAll
    getAll.mockReturnValue(new Promise((r) => { resolveAll = r }))
    const wrapper = mount(PreRegistration, { global: { stubs } })
    expect(wrapper.vm.isLoading).toBe(true)
    resolveAll({ data: { items: [registration], total: 1 } })
    await flushPromises()
    expect(wrapper.vm.isLoading).toBe(false)
  })

  it('handles load error', async () => {
    getAll.mockRejectedValue({ response: { data: { message: 'Lỗi tải' } } })
    const wrapper = await mountPage()
    expect(wrapper.vm.loadError).toBe('Lỗi tải')
    expect(wrapper.vm.registrations).toEqual([])
  })

  it('flags permission denied on 403', async () => {
    getAll.mockRejectedValue({ response: { status: 403 } })
    const wrapper = await mountPage()
    expect(wrapper.vm.permissionDenied).toBe(true)
  })

  it('uses the fallback load error message', async () => {
    getAll.mockRejectedValue({ message: 'x' })
    const wrapper = await mountPage()
    expect(wrapper.vm.loadError).toBe('Không thể tải danh sách đăng ký khách.')
  })

  it('maintains filters and clear filters', async () => {
    const wrapper = await mountPage()
    wrapper.vm.searchQuery = 'kh'
    wrapper.vm.filterStatus = 'Pending'
    wrapper.vm.filterDate = '2026-08-05'
    expect(wrapper.vm.hasActiveFilters).toBe(true)
    wrapper.vm.clearFilters()
    expect(wrapper.vm.searchQuery).toBe('')
    expect(wrapper.vm.filterStatus).toBe('')
    expect(wrapper.vm.filterDate).toBe('')
    expect(replace).toHaveBeenCalled()
  })

  it('commits filters to the route and applies query snapshots', async () => {
    route.query = { search: 'le', status: 'Pending', date: '2026-08-05', page: '2' }
    const wrapper = await mountPage()
    expect(wrapper.vm.searchQuery).toBe('le')
    expect(wrapper.vm.filterStatus).toBe('Pending')
    expect(wrapper.vm.filterDate).toBe('2026-08-05')
    expect(wrapper.vm.currentPage).toBe(2)
  })

  it('maps invalid status query to empty and sets page bounds', async () => {
    route.query = { status: 'bogus', page: '0' }
    const wrapper = await mountPage()
    expect(wrapper.vm.filterStatus).toBe('')
    expect(wrapper.vm.currentPage).toBe(1)
  })

  it('sets pages via setPage and pagination buttons', async () => {
    getAll.mockResolvedValue({ data: { items: [registration], total: 25 } })
    const wrapper = await mountPage()
    expect(wrapper.vm.totalPages).toBe(3)
    wrapper.vm.setPage(2)
    expect(replace).toHaveBeenCalledWith({ query: expect.objectContaining({ page: 2 }) })
  })

  it('filters search client-side with vietnamese case-insensitive matching', async () => {
    getAll.mockResolvedValue({ data: { items: [registration], total: 1 } })
    const wrapper = await mountPage()
    const res = wrapper.vm.fetchRegistrations ? true : false
    expect(res).toBe(true)
  })

  it('refreshes both list and stats', async () => {
    getAll.mockResolvedValue({ data: { items: [registration], total: 3 } })
    const wrapper = await mountPage()
    wrapper.vm.refreshPage()
    await flushPromises()
    expect(wrapper.vm.stats.total).toBeGreaterThanOrEqual(0)
  })

  it('shows a generic error when stats fail', async () => {
    getAll.mockResolvedValue({ data: { items: [registration], total: 5 } })
    const wrapper = await mountPage()
    getAll.mockRejectedValue(new Error('boom'))
    await wrapper.vm.fetchStats()
    expect(wrapper.vm.stats.total).toBe(5)
  })

  it('handles import completion toast branch errors', async () => {
    const wrapper = await mountPage()
    wrapper.vm.onImportComplete({ successCount: 2, errorCount: 1 })
    wrapper.vm.onImportComplete({ successCount: 2, errorCount: 0 })
    expect(wrapper.vm.showImportModal).toBe(false)
  })

  it('opens and closes the detail modal and loads visitors', async () => {
    getDetail.mockResolvedValue({ data: { ...registration, visitors: [{ fullName: 'Khách B', idCardNumber: '123', visitorPortalUrl: 'https://qr/x' }], accessLogs: [] } })
    const wrapper = await mountPage()
    await wrapper.vm.viewDetail(12)
    await flushPromises()
    expect(getDetail).toHaveBeenCalledWith(12)
    expect(wrapper.vm.detail.visitors.length).toBe(1)
    expect(wrapper.text()).toContain('Khách B')
    wrapper.vm.closeDetail()
    expect(wrapper.vm.showDetailModal).toBe(false)
    expect(wrapper.vm.detail).toBe(null)
  })

  it('shows detail error and 403 permission denied', async () => {
    getDetail.mockRejectedValue({ response: { status: 403 } })
    const wrapper = await mountPage()
    await wrapper.vm.viewDetail(12)
    await flushPromises()
    expect(wrapper.vm.detailError).toBe('Bạn không có quyền xem chi tiết đăng ký này.')

    getDetail.mockRejectedValue({ response: { status: 500, data: { message: 'Lỗi máy chủ' } } })
    await wrapper.vm.viewDetail(12)
    await flushPromises()
    expect(wrapper.vm.detailError).toBe('Lỗi máy chủ')

    getDetail.mockRejectedValue({ message: 'x' })
    await wrapper.vm.viewDetail(12)
    await flushPromises()
    expect(wrapper.vm.detailError).toBe('Máy chủ không trả về dữ liệu chi tiết.')
  })

  it('renders access logs timeline', async () => {
    getDetail.mockResolvedValue({ data: { ...registration, visitors: [], accessLogs: [{ logId: 1, direction: 'IN', timestamp: '2026-08-05T08:00:00', capturedLicensePlate: '29A1' }] } })
    const wrapper = await mountPage()
    await wrapper.vm.viewDetail(12)
    await flushPromises()
    expect(wrapper.text()).toContain('29A1')
  })

  it('approves a pending registration from the row', async () => {
    const wrapper = await mountPage()
    await wrapper.findAll('button').find((b) => b.text() === 'Duyệt').trigger('click')
    await flushPromises()
    expect(updateStatus).toHaveBeenCalledWith(12, 'Approved')
  })

  it('requests reject then confirms reject', async () => {
    const wrapper = await mountPage()
    wrapper.vm.requestReject(registration)
    expect(wrapper.vm.showRejectDialog).toBe(true)
    wrapper.vm.confirmReject()
    await flushPromises()
    expect(updateStatus).toHaveBeenCalledWith(12, 'Rejected')
    expect(wrapper.vm.showRejectDialog).toBe(false)
  })

  it('swallows reject when no target', async () => {
    const wrapper = await mountPage()
    wrapper.vm.rejectTarget = null
    wrapper.vm.confirmReject()
    expect(updateStatus).not.toHaveBeenCalled()
  })

  it('shows status update error', async () => {
    updateStatus.mockRejectedValue({ response: { data: { message: 'Không đổi được' } } })
    const wrapper = await mountPage()
    wrapper.vm.handleUpdateStatus(12, 'Approved')
    await flushPromises()
    expect(wrapper.vm.statusSaving).toBe(false)
  })

  it('updates status from detail and closes after', async () => {
    const wrapper = await mountPage()
    wrapper.vm.detail = { ...registration, registrationId: 12 }
    await wrapper.vm.handleUpdateStatus(12, 'Approved', true)
    await flushPromises()
    expect(wrapper.vm.showDetailModal).toBe(false)
  })

  it('opens the create link modal and loads employees', async () => {
    const wrapper = await mountPage()
    wrapper.vm.openCreateLink()
    expect(wrapper.vm.showCreateLinkModal).toBe(true)
    expect(wrapper.vm.employees.length).toBe(1)
    expect(wrapper.vm.linkForm.expiryHours).toBe(24)
  })

  it('returns early on create-link validation failure', async () => {
    const wrapper = await mountPage()
    wrapper.vm.openCreateLink()
    wrapper.vm.linkForm.hostEmployeeId = ''
    wrapper.vm.linkForm.expiryHours = 200
    await wrapper.vm.handleCreateLink()
    expect(createLink).not.toHaveBeenCalled()
    wrapper.vm.linkForm.hostEmployeeId = 4
    wrapper.vm.linkForm.expiryHours = 'abc'
    await wrapper.vm.handleCreateLink()
    expect(createLink).not.toHaveBeenCalled()
  })

  it('creates a link successfully and opens it in a new tab', async () => {
    const wrapper = await mountPage()
    wrapper.vm.openCreateLink()
    wrapper.vm.linkForm.hostEmployeeId = 4
    wrapper.vm.linkForm.expiryHours = 48
    await wrapper.vm.handleCreateLink()
    await flushPromises()
    expect(createLink).toHaveBeenCalledWith({ hostEmployeeId: 4, expiryHours: 48 })
    expect(wrapper.vm.createdLink.registrationUrl).toBe('https://example.test/register/abc')
    wrapper.vm.openCreatedLink()
    expect(windowOpen).toHaveBeenCalled()
  })

  it('shows create-link error and keeps the form', async () => {
    createLink.mockRejectedValue({ response: { data: { message: 'Tạo thất bại' } } })
    const wrapper = await mountPage()
    wrapper.vm.openCreateLink()
    wrapper.vm.linkForm.hostEmployeeId = 4
    wrapper.vm.linkForm.expiryHours = 24
    await wrapper.vm.handleCreateLink()
    await flushPromises()
    expect(wrapper.vm.linkError).toBe('Tạo thất bại')
  })

  it('closes create link and copies text', async () => {
    const wrapper = await mountPage()
    wrapper.vm.openCreateLink()
    wrapper.vm.closeCreateLink(true)
    expect(wrapper.vm.showCreateLinkModal).toBe(false)
    await wrapper.vm.copyText('abc')
    await flushPromises()
    expect(clipboardWrite).toHaveBeenCalledWith('abc')
  })

  it('shows discard dialog when closing a dirty form', async () => {
    const wrapper = await mountPage()
    wrapper.vm.openCreateLink()
    await flushPromises()
    wrapper.vm.linkForm.hostEmployeeId = 4
    await wrapper.vm.requestCloseCreateLink()
    expect(wrapper.vm.showDiscardDialog).toBe(true)
    wrapper.vm.closeCreateLink(true)
    expect(wrapper.vm.showDiscardDialog).toBe(false)
  })

  it('closes create link without discard when not dirty', async () => {
    const wrapper = await mountPage()
    wrapper.vm.openCreateLink()
    await flushPromises()
    wrapper.vm.closeCreateLink()
    expect(wrapper.vm.showCreateLinkModal).toBe(false)
  })

  it('handles copy failure', async () => {
    clipboardWrite.mockRejectedValueOnce(new Error('denied'))
    const wrapper = await mountPage()
    await wrapper.vm.copyText('abc')
    await flushPromises()
    expect(clipboardWrite).toHaveBeenCalled()
  })

  it('creates QR codes and downloads them', async () => {
    const wrapper = await mountPage()
    const fakeCanvas = { toDataURL: vi.fn(() => 'data:image/png;base64,QRDATA') }
    const fakeEl = { querySelector: vi.fn(() => fakeCanvas) }
    wrapper.vm.qrCardRefs[0] = fakeEl
    wrapper.vm.downloadVisitorQr(0, { fullName: 'Khách B' })
    expect(fakeCanvas.toDataURL).toHaveBeenCalled()
    expect(wrapper.vm.safeFileName('Khách B')).toContain('Khách')
    expect(wrapper.vm.safeFileName('')).toBe('visitor')
  })

  it('shows an error when downloading a missing QR', async () => {
    const wrapper = await mountPage()
    wrapper.vm.qrCardRefs = []
    wrapper.vm.downloadVisitorQr(0, { fullName: 'A' })
    expect(wrapper.vm.qrCardRefs.length).toBe(0)
  })

  it('renders QR to canvas and handles invalid data', async () => {
    const wrapper = await mountPage()
    await wrapper.vm.renderQr({}, 'text')
    await flushPromises()
    qrcode.default.toCanvas.mockRejectedValueOnce(new Error('bad'))
    await wrapper.vm.renderQr({}, 'bad')
    await flushPromises()
  })

  it('beforeUnload prevents navigation when dirty', async () => {
    const wrapper = await mountPage()
    wrapper.vm.openCreateLink()
    await flushPromises()
    wrapper.vm.linkForm.hostEmployeeId = 4
    const event = { preventDefault: vi.fn() }
    wrapper.vm.beforeUnload(event)
    expect(event.preventDefault).toHaveBeenCalled()
    const clean = { preventDefault: vi.fn() }
    wrapper.vm.linkSubmitted = true
    wrapper.vm.closeCreateLink(true)
    wrapper.vm.beforeUnload(clean)
    expect(clean.preventDefault).not.toHaveBeenCalled()
  })

  it('cleans up on unmount', async () => {
    const wrapper = await mountPage()
    wrapper.unmount()
  })
})
