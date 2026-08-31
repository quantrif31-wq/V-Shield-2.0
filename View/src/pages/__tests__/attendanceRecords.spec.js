import { flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../stores/auth', () => ({ authState: { user: { employeeId: 5 } } }))
vi.mock('../../services/employeeApi', () => ({ getAll: vi.fn() }))
vi.mock('../../services/lookupApi', () => ({ getDepartments: vi.fn() }))
vi.mock('../../services/attendanceApi', () => ({
  attendanceStatusLabelMap: {
    NotCheckedIn: 'Chưa chấm công',
    CheckedIn: 'Đã vào',
    Completed: 'Hoàn thành',
    Late: 'Đi trễ',
    EarlyLeave: 'Về sớm',
    LateAndEarlyLeave: 'Trễ và về sớm',
    Absent: 'Vắng mặt',
    Leave: 'Nghỉ phép',
    ForgotCheckout: 'Quên check-out',
    OutOfSchedule: 'Ngoài lịch',
    Scheduled: 'Đã lên lịch',
    Worked: 'Đã làm',
  },
  checkInAttendance: vi.fn(),
  checkOutAttendance: vi.fn(),
  getAttendances: vi.fn(),
  updateAttendance: vi.fn(),
  getAttendanceTransits: vi.fn(),
  deriveAttendance: vi.fn(),
  getAttendanceAnomalies: vi.fn(),
  detectAttendanceAnomalies: vi.fn(),
  resolveAnomaly: vi.fn(),
  markAnomalyFalsePositive: vi.fn(),
}))

const employeeApi = await import('../../services/employeeApi')
const lookupApi = await import('../../services/lookupApi')
const attendanceApi = await import('../../services/attendanceApi')
const AttendanceRecords = (await import('../AttendanceRecords.vue')).default

const norm = (s) => String(s).normalize('NFC')

const sampleAttendance = [
  {
    attendanceId: 1, employeeName: 'Nguyen Van A', departmentName: 'An ninh', workDate: '2026-01-05T00:00:00Z', shiftName: 'Ca 1',
    checkIn: '2026-01-05T07:00:00Z', checkOut: '2026-01-05T15:00:00Z', totalWorkingHours: 8, status: 'Completed',
    lateMinutes: 0, earlyLeaveMinutes: 0, overtimeHours: 0, source: 'QR', isZoneDerived: false, zoneDwellTime: 0,
  },
  {
    attendanceId: 2, employeeName: 'Tran Thi B', departmentName: null, workDate: '2026-01-05T00:00:00Z', shiftName: null,
    checkIn: '2026-01-05T08:20:00Z', checkOut: null, totalWorkingHours: 1.5, status: 'Late',
    lateMinutes: 20, earlyLeaveMinutes: 0, overtimeHours: 1.5, source: 'ZoneTransit', isZoneDerived: true, zoneDwellTime: 3.2,
  },
]

beforeEach(() => {
  vi.clearAllMocks()
  employeeApi.getAll.mockResolvedValue({ data: [{ employeeId: 5, fullName: 'Nguyen Van A' }] })
  lookupApi.getDepartments.mockResolvedValue({ data: [{ departmentId: 3, name: 'An ninh' }] })
  attendanceApi.getAttendances.mockResolvedValue({ data: sampleAttendance })
  attendanceApi.getAttendanceTransits.mockResolvedValue({ data: [{ zoneTransitId: 9, direction: 'IN', timestamp: '2026-01-05T07:05:00Z', securityZoneName: 'Cổng chính', gateName: 'G1', source: 'QR' }] })
  attendanceApi.getAttendanceAnomalies.mockResolvedValue({ data: [] })
  attendanceApi.detectAttendanceAnomalies.mockResolvedValue({ data: { detected: 2 } })
  attendanceApi.checkInAttendance.mockResolvedValue({})
  attendanceApi.checkOutAttendance.mockResolvedValue({})
  attendanceApi.updateAttendance.mockResolvedValue({})
  attendanceApi.deriveAttendance.mockResolvedValue({ data: { message: 'Đã tổng hợp' } })
  attendanceApi.resolveAnomaly.mockResolvedValue({})
  attendanceApi.markAnomalyFalsePositive.mockResolvedValue({})
})

describe('AttendanceRecords', () => {
  it('loads attendance data on mount with date defaults and lookup', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    expect(employeeApi.getAll).toHaveBeenCalled()
    expect(lookupApi.getDepartments).toHaveBeenCalled()
    expect(attendanceApi.getAttendances).toHaveBeenCalled()
    expect(wrapper.vm.filters.fromDate).toBeTruthy()
    expect(wrapper.vm.filters.toDate).toBeTruthy()
    expect(wrapper.vm.employees.length).toBe(1)
    expect(wrapper.vm.departments.length).toBe(1)
    expect(wrapper.vm.attendances.length).toBe(2)
    expect(wrapper.text()).toContain('Nguyen Van A')
    expect(wrapper.text()).toContain('Tran Thi B')
    expect(wrapper.text()).toContain('Hoàn thành')
  })

  it('renders loading state while fetching', async () => {
    let resolveFn
    attendanceApi.getAttendances.mockReturnValue(new Promise((r) => { resolveFn = r }))
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    expect(wrapper.vm.loading).toBe(true)
    expect(wrapper.text()).toContain('Đang tải dữ liệu chấm công')
    resolveFn({ data: sampleAttendance })
    await flushPromises()
    expect(wrapper.vm.loading).toBe(false)
    expect(wrapper.text()).toContain('Nguyen Van A')
  })

  it('shows error and empty states', async () => {
    attendanceApi.getAttendances.mockRejectedValue({ response: { data: { message: 'Lỗi máy chủ' } } })
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    expect(norm(wrapper.text())).toContain('Lỗi máy chủ')
    attendanceApi.getAttendances.mockResolvedValue({ data: [] })
    await wrapper.vm.loadAttendances()
    await flushPromises()
    expect(norm(wrapper.text())).toContain('Không có bản ghi phù hợp')
  })

  it('builds filter params and reloads on filter change', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    wrapper.vm.filters.fromDate = '2026-01-01'
    wrapper.vm.filters.toDate = '2026-01-31'
    wrapper.vm.filters.employeeId = '5'
    wrapper.vm.filters.departmentId = '3'
    wrapper.vm.filters.status = 'Late'
    await wrapper.vm.loadAttendances()
    expect(attendanceApi.getAttendances).toHaveBeenCalledWith({
      fromDate: '2026-01-01', toDate: '2026-01-31', employeeId: 5, departmentId: 3, status: 'Late',
    })
  })

  it('paginates attendances and changes page size', async () => {
    const many = Array.from({ length: 25 }, (_, i) => ({ attendanceId: i + 1, employeeName: `E${i}`, workDate: '2026-01-05T00:00:00Z', checkIn: '2026-01-05T07:00:00Z', checkOut: '2026-01-05T15:00:00Z', totalWorkingHours: 8, status: 'Completed' }))
    attendanceApi.getAttendances.mockResolvedValue({ data: many })
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    expect(wrapper.vm.totalPages).toBe(3)
    expect(wrapper.vm.paginatedAttendances.length).toBe(10)
    const nextBtn = wrapper.findAll('button').find((b) => norm(b.text()).includes('Trang sau'))
    await nextBtn.trigger('click')
    expect(wrapper.vm.currentPage).toBe(2)
    expect(wrapper.vm.paginatedAttendances[0].attendanceId).toBe(11)
    const prevBtn = wrapper.findAll('button').find((b) => norm(b.text()).includes('Trang trước'))
    await prevBtn.trigger('click')
    expect(wrapper.vm.currentPage).toBe(1)
    await wrapper.findAll('.size-select')[0].setValue('5')
    expect(wrapper.vm.pageSize).toBe(5)
  })

  it('toggles detail row and shows attendance note', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.toggleDetails(2)
    expect(wrapper.vm.expandedAttendanceId).toBe(2)
    expect(norm(wrapper.text())).toContain('Đi trễ')
    expect(norm(wrapper.text())).toContain('Trễ 20 phút')
    expect(norm(wrapper.text())).toContain('Về sớm')
    await wrapper.vm.toggleDetails(2)
    expect(wrapper.vm.expandedAttendanceId).toBe(null)
  })

  it('manual check-in success and failure', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.manualCheckIn()
    expect(attendanceApi.checkInAttendance).toHaveBeenCalledWith({ employeeId: 5, source: 'Manual' })
    expect(norm(wrapper.text())).toContain('Check-in thành công')
    attendanceApi.checkInAttendance.mockRejectedValue({ response: { data: { message: 'Đã chấm rồi' } } })
    await wrapper.vm.manualCheckIn()
    expect(norm(wrapper.text())).toContain('Đã chấm rồi')
  })

  it('manual check-out success and failure', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.manualCheckOut()
    expect(attendanceApi.checkOutAttendance).toHaveBeenCalledWith({ employeeId: 5, source: 'Manual' })
    attendanceApi.checkOutAttendance.mockRejectedValue({})
    await wrapper.vm.manualCheckOut()
    expect(norm(wrapper.text())).toContain('Check-out thất bại')
  })

  it('showTransitTimeline success (with data) and error', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.showTransitTimeline(sampleAttendance[0])
    expect(wrapper.vm.showTransitModal).toBe(true)
    expect(wrapper.vm.transits.length).toBe(1)
    expect(norm(wrapper.text())).toContain('VÀO')
    expect(norm(wrapper.text())).toContain('Cổng chính')
    attendanceApi.getAttendanceTransits.mockRejectedValue({})
    await wrapper.vm.showTransitTimeline(sampleAttendance[1])
    expect(norm(wrapper.text())).toContain('Không tải được lộ trình zone')
  })

  it('showTransitTimeline empty state', async () => {
    attendanceApi.getAttendanceTransits.mockResolvedValue({ data: [] })
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.showTransitTimeline(sampleAttendance[0])
    expect(norm(wrapper.text())).toContain('Không có dữ liệu di chuyển qua zone')
  })

  it('closes transit modal via close button', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.showTransitTimeline(sampleAttendance[0])
    expect(wrapper.vm.showTransitModal).toBe(true)
    const closeBtn = wrapper.findAll('.modal-close')[0]
    if (closeBtn) await closeBtn.trigger('click')
    expect(wrapper.vm.showTransitModal).toBe(false)
    await wrapper.vm.showTransitTimeline(sampleAttendance[0])
    wrapper.vm.showTransitModal = false
    await nextTick()
    expect(wrapper.vm.showTransitModal).toBe(false)
  })

  it('deriveNow success and error', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.deriveNow(null)
    expect(attendanceApi.deriveAttendance).toHaveBeenCalledWith({ employeeId: null, date: wrapper.vm.filters.fromDate || undefined })
    expect(norm(wrapper.text())).toContain('Đã tổng hợp')
    attendanceApi.deriveAttendance.mockRejectedValue({ response: { data: { message: 'Tổng hợp lỗi' } } })
    await wrapper.vm.deriveNow(null)
    expect(norm(wrapper.text())).toContain('Tổng hợp lỗi')
  })

  it('deriveNow reports processed count', async () => {
    attendanceApi.deriveAttendance.mockResolvedValue({ data: { processed: 12 } })
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.deriveNow(null)
    expect(norm(wrapper.text())).toContain('Đã xử lý 12 bản ghi')
  })

  it('openAnomalyPanel loads anomalies', async () => {
    attendanceApi.getAttendanceAnomalies.mockResolvedValue({
      data: [
        { anomalyId: 1, anomalyType: 'BuddyPunching', severity: 'cao', status: 'Open', description: 'Nghi vấn', employeeId: 7, workDate: '2026-01-05T00:00:00Z', supportingData: 'log', resolution: null },
      ],
    })
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.openAnomalyPanel()
    expect(wrapper.vm.showAnomalyModal).toBe(true)
    expect(norm(wrapper.text())).toContain('Cao')
    expect(norm(wrapper.text())).toContain('Buddy Punch')
    attendanceApi.getAttendanceAnomalies.mockRejectedValue({})
    await wrapper.vm.openAnomalyPanel()
    expect(norm(wrapper.text())).toContain('Không tải được dữ liệu bất thường')
  })

  it('openAnomalyPanel empty state', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.openAnomalyPanel()
    expect(norm(wrapper.text())).toContain('Không phát hiện bất thường')
  })

  it('runDetection success and error', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.runDetection()
    expect(attendanceApi.detectAttendanceAnomalies).toHaveBeenCalled()
    expect(norm(wrapper.text())).toContain('Phát hiện 2 bất thường mới')
    attendanceApi.detectAttendanceAnomalies.mockRejectedValue({ response: { data: { message: 'Quét lỗi' } } })
    await wrapper.vm.runDetection()
    expect(norm(wrapper.text())).toContain('Quét lỗi')
  })

  it('resolveAnomalyHandler success and error', async () => {
    attendanceApi.getAttendanceAnomalies.mockResolvedValue({ data: [{ anomalyId: 1, anomalyType: 'SuspiciousTime', severity: 'trung-binh', status: 'Open', description: 'x', employeeId: 5, workDate: '2026-01-05T00:00:00Z' }] })
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.openAnomalyPanel()
    await wrapper.vm.resolveAnomalyHandler(1)
    expect(attendanceApi.resolveAnomaly).toHaveBeenCalledWith(1, { resolution: 'Da kiem tra va xu ly.' })
    expect(wrapper.vm.anomalies).toEqual([])
    attendanceApi.resolveAnomaly.mockRejectedValue({ response: { data: { message: 'Lỗi' } } })
    await wrapper.vm.resolveAnomalyHandler(99)
    expect(norm(wrapper.text())).toContain('Lỗi')
  })

  it('falsePositiveHandler success and error', async () => {
    attendanceApi.getAttendanceAnomalies.mockResolvedValue({ data: [{ anomalyId: 2, anomalyType: 'MissingCheckOut', severity: 'thap', status: 'Open', description: 'y', employeeId: 5, workDate: '2026-01-05T00:00:00Z' }] })
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.openAnomalyPanel()
    await wrapper.vm.falsePositiveHandler(2)
    expect(attendanceApi.markAnomalyFalsePositive).toHaveBeenCalledWith(2)
    expect(wrapper.vm.anomalies).toEqual([])
    attendanceApi.markAnomalyFalsePositive.mockRejectedValue({})
    await wrapper.vm.falsePositiveHandler(44)
    expect(norm(wrapper.text())).toContain('Thất bại')
  })

  it('renders anomaly statuses, severities and resolutions', async () => {
    attendanceApi.getAttendanceAnomalies.mockResolvedValue({
      data: [
        { anomalyId: 1, anomalyType: 'UnknownType', severity: 'thap', status: 'Resolved', description: 'z', employeeId: 7, workDate: '2026-01-05T00:00:00Z', resolution: 'Da gui email' },
        { anomalyId: 2, anomalyType: 'MissingCheckOut', severity: 'cao', status: 'FalsePositive', description: 'q', employee: { fullName: 'ABC' }, workDate: '2026-01-05T00:00:00Z', supportingData: 'meta', resolution: null },
      ],
    })
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.openAnomalyPanel()
    expect(norm(wrapper.text())).toContain('Đã xử lý')
    expect(norm(wrapper.text())).toContain('FP')
    expect(norm(wrapper.text())).toContain('UnknownType')
    expect(norm(wrapper.text())).toContain('ABC')
    expect(norm(wrapper.text())).toContain('meta')
  })

  it('openEditModal populates form and submitEdit updates', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.openEditModal(sampleAttendance[0])
    expect(wrapper.vm.showModal).toBe(true)
    expect(wrapper.vm.editingId).toBe(1)
    expect(wrapper.vm.editForm.status).toBe('Completed')
    await wrapper.vm.submitEdit()
    expect(attendanceApi.updateAttendance).toHaveBeenCalledWith(1, {
      checkIn: new Date(wrapper.vm.editForm.checkIn).toISOString(),
      checkOut: new Date(wrapper.vm.editForm.checkOut).toISOString(),
      status: 'Completed', source: 'QR', note: null,
    })
    expect(wrapper.vm.showModal).toBe(false)
    expect(norm(wrapper.text())).toContain('Đã cập nhật bản ghi chấm công')
  })

  it('submitEdit keeps modal open on error and clears checkIn/out null', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.openEditModal(sampleAttendance[0])
    wrapper.vm.editForm.checkIn = ''
    wrapper.vm.editForm.checkOut = ''
    wrapper.vm.editForm.source = ''
    attendanceApi.updateAttendance.mockRejectedValue({ response: { data: { message: 'Sai dữ liệu' } } })
    await wrapper.vm.submitEdit()
    expect(norm(wrapper.text())).toContain('Sai dữ liệu')
    expect(wrapper.vm.showModal).toBe(true)
  })

  it('submitEdit returns early when no editingId', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.submitEdit()
    expect(attendanceApi.updateAttendance).not.toHaveBeenCalled()
  })

  it('exportAttendanceExcel shows error when empty and exports when data exists', async () => {
    attendanceApi.getAttendances.mockResolvedValue({ data: [] })
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.exportAttendanceExcel()
    expect(norm(wrapper.text())).toContain('Không có dữ liệu để xuất')
    attendanceApi.getAttendances.mockResolvedValue({ data: sampleAttendance })
    await wrapper.vm.loadAttendances()
    await flushPromises()
    const createObjectURL = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:mock')
    const revoke = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {})
    await wrapper.vm.exportAttendanceExcel()
    expect(createObjectURL).toHaveBeenCalled()
    expect(revoke).toHaveBeenCalled()
    expect(norm(wrapper.text())).toContain('Đã xuất file Excel')
    createObjectURL.mockRestore()
    revoke.mockRestore()
  })

  it('helper functions behave correctly', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    const vm = wrapper.vm
    expect(vm.statusLabel('Late')).toBe('Đi trễ')
    expect(vm.statusLabel('Unknown')).toBe('Unknown')
    expect(vm.statusLabel('')).toBe('--')
    expect(vm.statusTone('Late')).toBe('warning')
    expect(vm.statusTone('Completed')).toBe('success')
    expect(vm.statusTone('Absent')).toBe('danger')
    expect(vm.statusTone('Leave')).toBe('neutral')
    expect(vm.anomalyTypeLabel('BuddyPunching')).toBe('Buddy Punch')
    expect(vm.anomalyTypeLabel('SuspiciousTime')).toBe('Giờ đáng ngờ')
    expect(vm.anomalyTypeLabel('Z')).toBe('Z')
    expect(vm.formatDate(null)).toBe('--')
    expect(vm.formatDate('2026-01-05T00:00:00Z')).not.toBe('--')
    expect(vm.formatTime(null)).toBe('--:--')
    expect(vm.formatTime('2026-01-05T07:00:00Z')).not.toBe('--:--')
    expect(vm.attendanceNote({ lateMinutes: 5 })).toEqual({ text: 'Trễ 5 phút', tone: 'warning-text' })
    expect(vm.attendanceNote({ lateMinutes: 0, earlyLeaveMinutes: 10 })).toEqual({ text: 'Sớm 10 phút', tone: 'warning-text' })
    expect(vm.attendanceNote({ lateMinutes: 0, earlyLeaveMinutes: 0, overtimeHours: 2.5 })).toEqual({ text: 'Tăng ca 2.50 giờ', tone: 'success-text' })
    expect(vm.attendanceNote({ lateMinutes: 0, earlyLeaveMinutes: 0, overtimeHours: 0 })).toBeNull()
    expect(vm.formatDateTime(null)).toBe('--')
    expect(vm.formatDateTime('2026-01-05T07:00:00Z')).not.toBe('--')
    expect(vm.escapeHtml('<b>&"\'')).toContain('&lt;b&gt;')
    expect(vm.toLocalDatetimeInput(null)).toBe('')
    expect(vm.toLocalDatetimeInput('2026-01-05T07:00:00Z')).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/)
  })

  it('clears toast after timer expires', async () => {
    vi.useFakeTimers()
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.showToast('Tạm thời')
    expect(wrapper.vm.toast).toBeTruthy()
    await vi.advanceTimersByTimeAsync(3000)
    expect(wrapper.vm.toast).toBeNull()
    vi.useRealTimers()
  })

  it('clicks header action buttons via DOM', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    const normB = (s) => String(s).normalize('NFC')
    const deriveBtn = wrapper.findAll('button').find((b) => normB(b.text()).includes('Tổng hợp từ zone'))
    await deriveBtn.trigger('click')
    await flushPromises()
    expect(attendanceApi.deriveAttendance).toHaveBeenCalled()
  })

  it('clicks anomaly action buttons via DOM', async () => {
    attendanceApi.getAttendanceAnomalies.mockResolvedValue({
      data: [{ anomalyId: 5, anomalyType: 'BuddyPunching', severity: 'cao', status: 'Open', description: 'nghi', employeeId: 5, workDate: '2026-01-05T00:00:00Z', resolution: null }],
    })
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.openAnomalyPanel()
    await flushPromises()
    const normB = (s) => String(s).normalize('NFC')
    const resolveBtn = wrapper.findAll('button').find((b) => normB(b.text()).includes('Đã xử lý'))
    await resolveBtn.trigger('click')
    await flushPromises()
    expect(attendanceApi.resolveAnomaly).toHaveBeenCalledWith(5, { resolution: 'Da kiem tra va xu ly.' })
    attendanceApi.getAttendanceAnomalies.mockResolvedValue({
      data: [{ anomalyId: 6, anomalyType: 'MissingCheckOut', severity: 'thap', status: 'Open', description: 'x', employeeId: 5, workDate: '2026-01-05T00:00:00Z', resolution: null }],
    })
    await wrapper.vm.openAnomalyPanel()
    await flushPromises()
    const fpBtn = wrapper.findAll('button').find((b) => normB(b.text()).trim() === 'FP')
    await fpBtn.trigger('click')
    await flushPromises()
    expect(attendanceApi.markAnomalyFalsePositive).toHaveBeenCalledWith(6)
  })

  it('closes edit modal via Hủy button', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.openEditModal(sampleAttendance[0])
    expect(wrapper.vm.showModal).toBe(true)
    const normB = (s) => String(s).normalize('NFC')
    const cancelBtn = wrapper.findAll('button').find((b) => normB(b.text()).includes('Hủy'))
    await cancelBtn.trigger('click')
    expect(wrapper.vm.showModal).toBe(false)
  })

  it('closes anomaly modal via close button', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.openAnomalyPanel()
    expect(wrapper.vm.showAnomalyModal).toBe(true)
    const closeBtn = wrapper.findAll('.modal-close').find((b) => b.text().includes('✕'))
    await closeBtn.trigger('click')
    expect(wrapper.vm.showAnomalyModal).toBe(false)
  })

  it('closes anomaly modal via overlay self click', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.openAnomalyPanel()
    expect(wrapper.vm.showAnomalyModal).toBe(true)
    const overlay = wrapper.find('.modal-overlay')
    await overlay.trigger('click')
    expect(wrapper.vm.showAnomalyModal).toBe(false)
  })

  it('sets edit modal bound controls via setValue', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.openEditModal(sampleAttendance[0])
    await nextTick()
    const datetimeInputs = wrapper.findAll('input[type="datetime-local"]')
    await datetimeInputs.at(1).setValue('2026-01-05T16:00')
    const formSelects = wrapper.find('form').findAll('select')
    await formSelects.at(0).setValue('Late')
    await formSelects.at(1).setValue('Manual')
    const textarea = wrapper.find('textarea')
    await textarea.setValue('Ghi chú abc')
    await nextTick()
    expect(wrapper.vm.editForm.checkOut).toBe('2026-01-05T16:00')
    expect(wrapper.vm.editForm.status).toBe('Late')
    expect(wrapper.vm.editForm.source).toBe('Manual')
    expect(wrapper.vm.editForm.note).toBe('Ghi chú abc')
  })

  it('sets edit modal checkIn input via setValue', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.openEditModal(sampleAttendance[0])
    await nextTick()
    const datetimeInputs = wrapper.findAll('input[type="datetime-local"]')
    await datetimeInputs.at(0).setValue('2026-01-05T08:00')
    await nextTick()
    expect(wrapper.vm.editForm.checkIn).toBe('2026-01-05T08:00')
  })

  it('closes edit modal via close button', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.openEditModal(sampleAttendance[0])
    expect(wrapper.vm.showModal).toBe(true)
    const closeBtn = wrapper.findAll('.modal-close').find((b) => b.text().includes('✕'))
    await closeBtn.trigger('click')
    expect(wrapper.vm.showModal).toBe(false)
  })

  it('closes edit modal via overlay self click', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.openEditModal(sampleAttendance[0])
    expect(wrapper.vm.showModal).toBe(true)
    const overlay = wrapper.find('.modal-overlay')
    await overlay.trigger('click')
    expect(wrapper.vm.showModal).toBe(false)
  })

  it('closes transit modal via overlay self click', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.vm.showTransitTimeline(sampleAttendance[0])
    expect(wrapper.vm.showTransitModal).toBe(true)
    const overlay = wrapper.find('.modal-overlay')
    await overlay.trigger('click')
    expect(wrapper.vm.showTransitModal).toBe(false)
  })

  it('triggers status filter select change', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.findAll('.filter-select').at(2).setValue('Late')
    await flushPromises()
    expect(wrapper.vm.filters.status).toBe('Late')
    expect(attendanceApi.getAttendances).toHaveBeenCalledWith(expect.objectContaining({ status: 'Late' }))
  })

  it('clicks row action buttons', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    await wrapper.findAll('.btn-secondary.btn-sm').at(0).trigger('click')
    expect(wrapper.vm.showModal).toBe(true)
    wrapper.vm.showModal = false
    await nextTick()
    await wrapper.findAll('.detail-toggle').at(0).trigger('click')
    expect(wrapper.vm.expandedAttendanceId).not.toBe(null)
    await wrapper.findAll('.btn-ghost.btn-sm').at(0).trigger('click')
    expect(wrapper.vm.showTransitModal).toBe(true)
  })

  it('triggers toolbar filter control changes via DOM', async () => {
    const wrapper = mount(AttendanceRecords)
    await flushPromises()
    const dateInputs = wrapper.findAll('.date-input')
    await dateInputs.at(0).setValue('2026-01-01')
    await dateInputs.at(1).setValue('2026-01-31')
    const filterSelects = wrapper.findAll('.filter-select')
    await filterSelects.at(0).setValue('5')
    await filterSelects.at(1).setValue('3')
    await filterSelects.at(2).setValue('Late')
    await flushPromises()
    expect(attendanceApi.getAttendances).toHaveBeenCalledWith({
      fromDate: '2026-01-01', toDate: '2026-01-31', employeeId: 5, departmentId: 3, status: 'Late',
    })
  })
})
