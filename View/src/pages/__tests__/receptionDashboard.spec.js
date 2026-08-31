import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getReceptionOverview: vi.fn(),
    getReceptionBoard: vi.fn(),
    getVisitDetail: vi.fn(),
    getReceptionLostFound: vi.fn(),
    createReceptionInteraction: vi.fn(),
    checkInVisit: vi.fn(),
    checkOutVisit: vi.fn(),
  },
}))

const norm = (s) => String(s).normalize('NFC')
const findButton = (wrapper, text) =>
  wrapper.findAll('button').find((b) => norm(b.text()).includes(norm(text)))
const bodyButtons = () => [...document.querySelectorAll('button')]
const findBodyButton = (text) =>
  bodyButtons().find((el) => norm(el.textContent).includes(norm(text)))
const modalCount = () => document.querySelectorAll('.modal-overlay').length
const triggerBody = async (el) => {
  el.dispatchEvent(new Event('click', { bubbles: true }))
}

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const ReceptionDashboard = (await import('../ReceptionDashboard.vue')).default

const arrivals = [
  { visitId: 1, visitorName: 'Nguyen Van A', visitorPhone: '090111', status: 'Approved', hostEmployee: { fullName: 'Host A' }, expectedInUtc: '2026-01-01T09:00:00Z' },
  { visitId: 2, visitorName: 'Tran Thi B', status: 'Invited', expectedInUtc: '2026-01-01T10:00:00Z' },
]
const overdue = [
  { visitId: 3, visitorName: 'Le Van C', status: 'Overstay', site: { name: 'Toa A' }, expectedOutUtc: '2026-01-01T08:00:00Z' },
]
const lateArrivals = [
  { visitId: 4, visitorName: 'Pham Van D', visitorPhone: '090222', status: 'Approved', hostEmployee: { fullName: 'Host D' }, expectedInUtc: '2026-01-01T07:00:00Z' },
]
const activeVisitors = [
  { visitId: 5, visitorName: 'Vu Thi E', status: 'CheckedIn' },
]
const allVisits = [...arrivals, ...overdue, ...lateArrivals, ...activeVisitors]

const overviewData = {
  todayVisits: 8,
  activeVisitors: 3,
  pendingArrivals: 2,
  lateArrivalsNeedFollowUp: 4,
  overdueVisitors: 1,
  openSecurityRequests: 2,
  lostFoundCases: 5,
}

function visitDetail(visit, extra = {}) {
  return {
    data: {
      visit,
      receptionContext: {
        currentPresence: 'OnSite',
        latestParkingPermit: { parkingArea: { name: 'Bai A' } },
        latestLaneEvent: { direction: 'Entry', plateText: 'ABC-123' },
        interactions: [
          { receptionInteractionId: 10, summary: 'Da goi cho host', status: 'Resolved', interactionType: 'HostContact', createdAtUtc: '2026-01-01T09:30:00Z', detailNote: 'Chi tiet', resolutionNote: 'Ket qua' },
        ],
      },
      ...extra,
    },
  }
}

beforeEach(() => {
  vi.clearAllMocks()
  enterpriseApi.getReceptionOverview.mockResolvedValue({ data: overviewData })
  enterpriseApi.getReceptionBoard.mockResolvedValue({
    data: { arrivals, overdue, lateArrivals, activeVisitors, recentInteractions: [] },
  })
  enterpriseApi.getVisitDetail.mockImplementation((id) =>
    Promise.resolve(visitDetail(allVisits.find((v) => v.visitId === id)))
  )
  enterpriseApi.getReceptionLostFound.mockResolvedValue({
    data: { lostItems: [{ lostItemReportId: 20, itemDescription: 'Vi da', reporterName: 'Bao ve A' }], foundItems: [{ foundItemReportId: 21, itemDescription: 'Dien thoai', foundByName: 'Bao ve B' }] },
  })
  enterpriseApi.createReceptionInteraction.mockResolvedValue({})
  enterpriseApi.checkInVisit.mockResolvedValue({})
  enterpriseApi.checkOutVisit.mockResolvedValue({})
})

afterEach(() => {
  document.body.innerHTML = ''
})

describe('ReceptionDashboard', () => {
  it('loads the overview and board on mount and renders metrics', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    expect(enterpriseApi.getReceptionOverview).toHaveBeenCalled()
    expect(enterpriseApi.getReceptionBoard).toHaveBeenCalled()
    expect(wrapper.text()).toContain('8')
    expect(wrapper.text()).toContain('Nguyen Van A')
  })

  it('renders loading state while board loads', async () => {
    let resolveBoard
    enterpriseApi.getReceptionBoard.mockReturnValue(new Promise((res) => { resolveBoard = res }))
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    expect(wrapper.text()).toContain('Đang tải dữ liệu lễ tân')
    resolveBoard({ data: { arrivals: [], overdue: [], lateArrivals: [], activeVisitors: [], recentInteractions: [] } })
    await flushPromises()
    expect(enterpriseApi.getReceptionOverview).toHaveBeenCalled()
  })

  it('calls window.location.assign for lost found workspace', async () => {
    const assign = vi.fn()
    Object.defineProperty(window, 'location', { value: { assign }, writable: true, configurable: true })
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    await findButton(wrapper, 'Tra cứu đồ thất lạc').trigger('click')
    expect(assign).toHaveBeenCalledWith('/lost-found')
  })

  it('switches to overdue tab and renders overdue visits', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    await wrapper.findAll('.reception-tabs button')[1].trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Le Van C')
    expect(wrapper.text()).toContain('Toa A')
  })

  it('switches to follow-up tab and renders late arrivals', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    await wrapper.findAll('.reception-tabs button')[2].trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Pham Van D')
    expect(wrapper.text()).toContain('Host D')
  })

  it('switches to lost-found tab and opens lost found workspace with claim tab', async () => {
    const assign = vi.fn()
    Object.defineProperty(window, 'location', { value: { assign }, writable: true, configurable: true })
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    await wrapper.findAll('.reception-tabs button')[3].trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('chuyển sang màn hình chuyên dụng')
    await findButton(wrapper, 'Mở khu trao trả').trigger('click')
    expect(assign).toHaveBeenCalledWith('/lost-found?tab=claim')
  })

  it('renders empty state for board tabs with no data', async () => {
    enterpriseApi.getReceptionBoard.mockResolvedValue({ data: { arrivals: [], overdue: [], lateArrivals: [], activeVisitors: [], recentInteractions: [] } })
    enterpriseApi.getVisitDetail.mockResolvedValue({ data: { visit: null, receptionContext: {} } })
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    expect(wrapper.text()).toContain('Không có lịch tiếp đón nào hôm nay')
    expect(wrapper.text()).toContain('Chưa chọn hồ sơ')
  })

  it('selects a visit card and loads the detail profile', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    await wrapper.findAll('.visit-card')[0].trigger('click')
    await flushPromises()
    expect(enterpriseApi.getVisitDetail).toHaveBeenCalledWith(1)
    expect(wrapper.text()).toContain('Nguyen Van A')
    expect(wrapper.text()).toContain('Đang mở hồ sơ')
    expect(wrapper.text()).toContain('Da goi cho host')
  })

  it('auto-selects the first priority visit when no selection exists', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    expect(enterpriseApi.getVisitDetail).toHaveBeenCalledWith(3)
  })

  it('opens interaction modal via call-host action and submits', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    await wrapper.findAll('.visit-card')[0].trigger('click')
    await flushPromises()
    await findButton(wrapper, 'Gọi người liên hệ').trigger('click')
    expect(modalCount()).toBe(1)
    await triggerBody(findBodyButton('Lưu nhật ký'))
    await flushPromises()
    expect(enterpriseApi.createReceptionInteraction).toHaveBeenCalled()
    expect(enterpriseApi.getVisitDetail).toHaveBeenCalledWith(1)
    expect(modalCount()).toBe(0)
  })

  it('opens interaction modal via security dispatch with securityRequested true', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    await wrapper.findAll('.visit-card')[0].trigger('click')
    await flushPromises()
    await findButton(wrapper, 'Gọi bảo vệ').trigger('click')
    expect(modalCount()).toBe(1)
    expect(wrapper.vm.interactionForm.interactionType).toBe('SecurityDispatch')
    expect(wrapper.vm.interactionForm.securityRequested).toBe(true)
  })

  it('opens interaction modal via parking and wayfinding actions', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    await wrapper.findAll('.visit-card')[0].trigger('click')
    await flushPromises()
    await findButton(wrapper, 'Xác nhận xe').trigger('click')
    expect(wrapper.vm.interactionForm.interactionType).toBe('ParkingInquiry')
    await triggerBody(findBodyButton('Đóng'))
    await findButton(wrapper, 'Hướng dẫn khách').trigger('click')
    expect(wrapper.vm.interactionForm.interactionType).toBe('Wayfinding')
  })

  it('selects a visit card in overdue tab', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    await wrapper.findAll('.reception-tabs button')[1].trigger('click')
    await flushPromises()
    await wrapper.findAll('.visit-card')[0].trigger('click')
    await flushPromises()
    expect(enterpriseApi.getVisitDetail).toHaveBeenCalledWith(3)
    expect(wrapper.text()).toContain('Le Van C')
  })

  it('selects a visit card in follow-up tab', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    await wrapper.findAll('.reception-tabs button')[2].trigger('click')
    await flushPromises()
    await wrapper.findAll('.visit-card')[0].trigger('click')
    await flushPromises()
    expect(enterpriseApi.getVisitDetail).toHaveBeenCalledWith(4)
    expect(wrapper.text()).toContain('Pham Van D')
  })

  it('opens lost found manager without tab via main button', async () => {
    const assign = vi.fn()
    Object.defineProperty(window, 'location', { value: { assign }, writable: true, configurable: true })
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    await wrapper.findAll('.reception-tabs button')[3].trigger('click')
    await flushPromises()
    await findButton(wrapper, 'Mở quản lý đồ thất lạc').trigger('click')
    expect(assign).toHaveBeenCalledWith('/lost-found')
  })

  it('closes modal via overlay and close button and fills the full form', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    await wrapper.findAll('.visit-card')[0].trigger('click')
    await flushPromises()
    await findButton(wrapper, 'Gọi người liên hệ').trigger('click')
    expect(modalCount()).toBe(1)

    const overlay = document.querySelector('.modal-overlay')
    const selects = overlay.querySelectorAll('select')
    const inputs = overlay.querySelectorAll('input')
    const textareas = overlay.querySelectorAll('textarea')
    const checkbox = overlay.querySelector('input[type="checkbox"]')

    selects[0].value = 'SecurityDispatch'
    await selects[0].dispatchEvent(new Event('change', { bubbles: true }))
    selects[1].value = 'Resolved'
    await selects[1].dispatchEvent(new Event('change', { bubbles: true }))

    inputs[0].value = 'Tóm tắt A'
    await inputs[0].dispatchEvent(new Event('input', { bubbles: true }))

    inputs[1].value = 'Liên hệ A'
    await inputs[1].dispatchEvent(new Event('input', { bubbles: true }))
    inputs[2].value = '091'
    await inputs[2].dispatchEvent(new Event('input', { bubbles: true }))

    textareas[0].value = 'Chi tiết A'
    await textareas[0].dispatchEvent(new Event('input', { bubbles: true }))
    inputs[3].value = 'ABC-123'
    await inputs[3].dispatchEvent(new Event('input', { bubbles: true }))
    checkbox.checked = true
    await checkbox.dispatchEvent(new Event('change', { bubbles: true }))
    textareas[1].value = 'Kết quả A'
    await textareas[1].dispatchEvent(new Event('input', { bubbles: true }))

    expect(wrapper.vm.interactionForm.interactionType).toBe('SecurityDispatch')
    expect(wrapper.vm.interactionForm.status).toBe('Resolved')
    expect(wrapper.vm.interactionForm.summary).toBe('Tóm tắt A')
    expect(wrapper.vm.interactionForm.contactPersonName).toBe('Liên hệ A')
    expect(wrapper.vm.interactionForm.contactPersonPhone).toBe('091')
    expect(wrapper.vm.interactionForm.detailNote).toBe('Chi tiết A')
    expect(wrapper.vm.interactionForm.relatedVehiclePlate).toBe('ABC-123')
    expect(wrapper.vm.interactionForm.securityRequested).toBe(true)
    expect(wrapper.vm.interactionForm.resolutionNote).toBe('Kết quả A')

    overlay.dispatchEvent(new Event('click', { bubbles: true }))
    await flushPromises()
    expect(modalCount()).toBe(0)

    await findButton(wrapper, 'Gọi người liên hệ').trigger('click')
    expect(modalCount()).toBe(1)
    triggerBody(document.querySelector('.modal-overlay .btn-close'))
    await flushPromises()
    expect(wrapper.vm.showInteractionModal).toBe(false)
  })

  it('shows save error when creating interaction fails', async () => {
    enterpriseApi.createReceptionInteraction.mockRejectedValue({ response: { data: { message: 'Lỗi máy chủ' } } })
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    await wrapper.findAll('.visit-card')[0].trigger('click')
    await flushPromises()
    await findButton(wrapper, 'Gọi bảo vệ').trigger('click')
    await triggerBody(findBodyButton('Lưu nhật ký'))
    await flushPromises()
    expect(document.body.textContent).toContain('Lỗi máy chủ')
  })

  it('sets saveError fallback when error has no response', async () => {
    enterpriseApi.createReceptionInteraction.mockRejectedValue(new Error('boom'))
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    await wrapper.findAll('.visit-card')[0].trigger('click')
    await flushPromises()
    await findButton(wrapper, 'Gọi người liên hệ').trigger('click')
    await triggerBody(findBodyButton('Lưu nhật ký'))
    await flushPromises()
    expect(document.body.textContent).toContain('Không thể lưu nhật ký lễ tân')
  })

  it('check-in a selected visit', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    await wrapper.findAll('.visit-card')[0].trigger('click')
    await flushPromises()
    await findButton(wrapper, 'Xác nhận đã đến').trigger('click')
    await flushPromises()
    expect(enterpriseApi.checkInVisit).toHaveBeenCalledWith(1, { verificationStatus: 'Verified' })
    expect(enterpriseApi.getReceptionOverview).toHaveBeenCalled()
    expect(enterpriseApi.getVisitDetail).toHaveBeenCalledWith(1)
  })

  it('check-out a checked-in visit', async () => {
    enterpriseApi.getVisitDetail.mockResolvedValue(visitDetail({ visitId: 5, visitorName: 'Vu Thi E', status: 'CheckedIn' }))
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    // select an activeVisitor whose status is CheckedIn -> canCheckOut true
    await wrapper.vm.selectVisit(allVisits.find((v) => v.visitId === 5))
    await flushPromises()
    await findButton(wrapper, 'Xác nhận đã rời').trigger('click')
    await flushPromises()
    expect(enterpriseApi.checkOutVisit).toHaveBeenCalledWith(5)
  })

  it('check-in/check-out no-op when no visit detail exists', async () => {
    enterpriseApi.getVisitDetail.mockResolvedValue({ data: { visit: null, receptionContext: {} } })
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    await wrapper.vm.checkInSelectedVisit()
    await wrapper.vm.checkOutSelectedVisit()
    expect(enterpriseApi.checkInVisit).not.toHaveBeenCalled()
    expect(enterpriseApi.checkOutVisit).not.toHaveBeenCalled()
  })

  it('loads lost found items when a query is provided', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    wrapper.vm.lostFoundQuery = 'vi'
    await wrapper.vm.loadLostFound()
    expect(enterpriseApi.getReceptionLostFound).toHaveBeenCalledWith({ search: 'vi' })
    expect(wrapper.vm.lostFound.lostItems).toEqual([{ lostItemReportId: 20, itemDescription: 'Vi da', reporterName: 'Bao ve A' }])
    expect(wrapper.vm.lostFound.foundItems.length).toBe(1)
  })

  it('clears lost found items when query is empty', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    wrapper.vm.lostFound.lostItems = [{ id: 1 }]
    wrapper.vm.lostFound.foundItems = [{ id: 2 }]
    wrapper.vm.lostFoundQuery = '   '
    await wrapper.vm.loadLostFound()
    expect(enterpriseApi.getReceptionLostFound).not.toHaveBeenCalled()
    expect(wrapper.vm.lostFound.lostItems).toEqual([])
    expect(wrapper.vm.lostFound.foundItems).toEqual([])
  })

  it('reloads all via refresh button', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    const callsBefore = enterpriseApi.getReceptionBoard.mock.calls.length
    await findButton(wrapper, 'Làm mới toàn bộ').trigger('click')
    await flushPromises()
    expect(enterpriseApi.getReceptionBoard.mock.calls.length).toBeGreaterThan(callsBefore)
  })

  it('calls loadBoard on search enter', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    const input = wrapper.find('input[type="text"]')
    await input.setValue('Le Van C')
    await input.trigger('keyup.enter')
    await flushPromises()
    expect(enterpriseApi.getReceptionBoard).toHaveBeenCalledWith({ search: 'Le Van C' })
  })

  it('keeps selected visit after board reload when still present', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    await wrapper.findAll('.visit-card')[0].trigger('click')
    await flushPromises()
    expect(wrapper.vm.selectedVisit.visitId).toBe(1)
    await wrapper.vm.loadBoard()
    await flushPromises()
    expect(wrapper.vm.selectedVisit.visitId).toBe(1)
  })

  it('statusLabel maps known statuses and falls back to raw value', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    const vm = wrapper.vm
    expect(vm.statusLabel('Invited')).toBe('Đã mời')
    expect(vm.statusLabel('Approved')).toBe('Đã duyệt')
    expect(vm.statusLabel('CheckedIn')).toBe('Đã vào')
    expect(vm.statusLabel('Overstay')).toBe('Quá giờ')
    expect(vm.statusLabel('CheckedOut')).toBe('Đã ra')
    expect(vm.statusLabel('Denied')).toBe('Từ chối')
    expect(vm.statusLabel('Unknown')).toBe('Unknown')
  })

  it('interaction label helpers map values', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    const vm = wrapper.vm
    expect(vm.statusClass('CheckedIn')).toBe('success')
    expect(vm.statusClass('Overstay')).toBe('danger')
    expect(vm.statusClass('Denied')).toBe('danger')
    expect(vm.statusClass('Approved')).toBe('info')
    expect(vm.statusClass('Unknown')).toBe('warning')
    expect(vm.interactionTypeLabel('HostContact')).toBe('Liên hệ người phụ trách')
    expect(vm.interactionTypeLabel('VisitorSupport')).toBe('Hỗ trợ khách')
    expect(vm.interactionTypeLabel('SecurityDispatch')).toBe('Gọi bảo vệ')
    expect(vm.interactionTypeLabel('ParkingInquiry')).toBe('Xác nhận xe')
    expect(vm.interactionTypeLabel('LostFoundSupport')).toBe('Tra cứu đồ thất lạc')
    expect(vm.interactionTypeLabel('Wayfinding')).toBe('Chỉ đường')
    expect(vm.interactionTypeLabel('FollowUp')).toBe('Theo dõi bổ sung')
    expect(vm.interactionTypeLabel('Bogus')).toBe('Bogus')
    expect(vm.interactionStatusLabel('Open')).toBe('Mới mở')
    expect(vm.interactionStatusLabel('InProgress')).toBe('Đang xử lý')
    expect(vm.interactionStatusLabel('Resolved')).toBe('Đã xử lý xong')
    expect(vm.interactionStatusLabel('Escalated')).toBe('Đã chuyển tiếp')
    expect(vm.interactionStatusLabel('Cancelled')).toBe('Hủy')
    expect(vm.interactionStatusLabel('Bogus')).toBe('Bogus')
    expect(vm.interactionStatusSemantic('Resolved')).toBe('success')
    expect(vm.interactionStatusSemantic('InProgress')).toBe('info')
    expect(vm.interactionStatusSemantic('Escalated')).toBe('danger')
    expect(vm.interactionStatusSemantic('Cancelled')).toBe('danger')
    expect(vm.interactionStatusSemantic('Bogus')).toBe('warning')
  })

  it('presence, parking and date helpers', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    const vm = wrapper.vm
    expect(vm.presenceLabel('OnSite')).toBe('Đang ở trong khuôn viên')
    expect(vm.presenceLabel('OffSite')).toBe('Không còn trong khuôn viên')
    expect(vm.parkingLabel(undefined, undefined)).toBe('Chưa có dữ liệu xe liên kết')
    expect(vm.parkingLabel({ parkingArea: { name: 'Bai B' } }, undefined)).toContain('Bai B')
    expect(vm.parkingLabel({ parkingArea: { name: 'Bai B' } }, { direction: 'Entry', plateText: 'DEF-9' })).toContain('Xe vẫn còn ghi nhận trong bãi')
    expect(vm.parkingLabel({ parkingArea: { name: 'Bai B' } }, { direction: 'Exit', plateText: 'XYZ-8' })).toContain('Xe có thể đã ra')
    expect(vm.parkingLabel({ parkingArea: { name: 'Bai B' } }, { direction: 'Exit' })).toContain('chưa rõ biển số')
    expect(vm.formatDateTime('2026-01-01T09:00:00Z')).not.toBe('Chưa có')
    expect(vm.formatDateTime(null)).toBe('Chưa có')
  })

  it('displays resolution note and detail note in timeline', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    await wrapper.findAll('.visit-card')[0].trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Chi tiet')
    expect(wrapper.text()).toContain('Ket qua')
  })

  it('openInteractionForLostFound populates form from an item', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    wrapper.vm.openInteractionForLostFound('LostFoundSupport', 'Tra cứu đồ', {
      lostItemReportId: 20,
      reporterName: 'Bao ve A',
      reporterPhone: '098',
      itemDescription: 'Vi da mau den',
    })
    expect(wrapper.vm.interactionForm.interactionType).toBe('LostFoundSupport')
    expect(wrapper.vm.interactionForm.lostItemReportId).toBe(20)
    expect(wrapper.vm.interactionForm.contactPersonName).toBe('Bao ve A')
    expect(wrapper.vm.interactionForm.detailNote).toBe('Vi da mau den')
    expect(wrapper.vm.showInteractionModal).toBe(true)
  })

  it('openInteractionForLostFound uses found item fields', async () => {
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    wrapper.vm.openInteractionForLostFound('LostFoundSupport', 'Tra cứu đồ', {
      foundItemReportId: 21,
      foundByName: 'Bao ve B',
      foundByPhone: '097',
      itemDescription: 'Dien thoai',
    })
    expect(wrapper.vm.interactionForm.foundItemReportId).toBe(21)
    expect(wrapper.vm.interactionForm.contactPersonName).toBe('Bao ve B')
    expect(wrapper.vm.interactionForm.contactPersonPhone).toBe('097')
  })

  it('renders no interactions empty state', async () => {
    enterpriseApi.getVisitDetail.mockResolvedValue({
      data: { visit: allVisits[0], receptionContext: { currentPresence: 'OffSite', latestParkingPermit: null, latestLaneEvent: null, interactions: [] } },
    })
    const wrapper = mount(ReceptionDashboard)
    await flushPromises()
    await wrapper.findAll('.visit-card')[0].trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Chưa có nhật ký xử lý nào')
  })
})
