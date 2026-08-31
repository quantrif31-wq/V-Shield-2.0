import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getEvents: vi.fn(),
    deleteEvent: vi.fn(),
    getSiteMaps: vi.fn(),
    getMapPlacements: vi.fn(),
    createSiteMap: vi.fn(),
    addMapPlacement: vi.fn(),
    createEvent: vi.fn(),
  },
}))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const EventTimeline = (await import('../EventTimeline.vue')).default

const sampleEvents = [
  { securityEventId: 1, eventType: 'Intrusion', severity: 'High', subjectType: 'Employee', subjectId: 'E1', plateText: '51A-123', confidence: 0.92, summary: 'Đột nhập cổng A', occurredAtUtc: '2026-08-01T00:00:00Z', sourceType: 'Camera', sourceId: 'C1', correlationId: 'corr-1' },
  { securityEventId: 2, eventType: 'Info', severity: 'Info', subjectType: null, subjectId: null, plateText: null, confidence: null, summary: null, occurredAtUtc: '2026-08-02T00:00:00Z', sourceType: 'Sensor', sourceId: 'S1', correlationId: null },
]

beforeEach(() => {
  vi.clearAllMocks()
  vi.unstubAllGlobals()
  vi.stubGlobal('confirm', vi.fn(() => true))
  vi.stubGlobal('alert', vi.fn())
  enterpriseApi.getEvents.mockResolvedValue({ data: { items: sampleEvents, total: 2 } })
  enterpriseApi.getSiteMaps.mockResolvedValue({ data: [] })
  enterpriseApi.getMapPlacements.mockResolvedValue({ data: [] })
})

afterEach(() => {
  vi.useRealTimers()
})

describe('EventTimeline', () => {
  it('loads and renders security events', async () => {
    const wrapper = mount(EventTimeline)
    await flushPromises()
    expect(enterpriseApi.getEvents).toHaveBeenCalledWith({ page: 1, pageSize: 50 })
    expect(wrapper.vm.loading).toBe(false)
    expect(wrapper.text()).toContain('Đột nhập cổng A')
    expect(wrapper.text()).toContain('51A-123')
    expect(wrapper.text()).toContain('92%')
    expect(wrapper.text()).toContain('High')
    expect(wrapper.find('tbody').text()).toContain('E1')
  })

  it('renders fallback dashes for event without plate or summary', async () => {
    const wrapper = mount(EventTimeline)
    await flushPromises()
    expect(wrapper.find('tbody').text()).toContain('—')
  })

  it('shows loading and empty states', async () => {
    enterpriseApi.getEvents.mockResolvedValue({ data: { items: [], total: 0 } })
    const wrapper = mount(EventTimeline)
    await flushPromises()
    expect(wrapper.vm.totalPages).toBe(1)
    expect(wrapper.text()).toContain('Không có sự kiện nào.')
  })

  it('handles loadEvents failure with empty events', async () => {
    enterpriseApi.getEvents.mockRejectedValue(new Error('net'))
    const wrapper = mount(EventTimeline)
    await flushPromises()
    expect(wrapper.vm.events).toEqual([])
    expect(wrapper.vm.loading).toBe(false)
  })

  it('passes filters into getEvents params', async () => {
    const wrapper = mount(EventTimeline)
    await flushPromises()
    wrapper.vm.filters.eventType = 'Intrusion'
    wrapper.vm.filters.plate = '51A'
    wrapper.vm.filters.severity = 'High'
    wrapper.vm.filters.subjectType = 'Employee'
    await wrapper.vm.loadEvents()
    expect(enterpriseApi.getEvents).toHaveBeenLastCalledWith({
      page: 1, pageSize: 50, eventType: 'Intrusion', plate: '51A', severity: 'High', subjectType: 'Employee',
    })
  })

  it('debounces reload from input events', async () => {
    vi.useFakeTimers()
    const wrapper = mount(EventTimeline)
    await flushPromises()
    wrapper.vm.filters.eventType = 'Tamper'
    wrapper.vm.debounceLoad()
    wrapper.vm.debounceLoad()
    await vi.advanceTimersByTimeAsync(300)
    expect(enterpriseApi.getEvents).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1, eventType: 'Tamper' }))
  })

  it('debounces via DOM input on event type and plate', async () => {
    vi.useFakeTimers()
    const wrapper = mount(EventTimeline)
    await flushPromises()
    const inputs = wrapper.findAll('.form-input')
    await inputs.at(0).setValue('Tamper')
    await inputs.at(1).setValue('51A')
    await vi.advanceTimersByTimeAsync(300)
    expect(enterpriseApi.getEvents).toHaveBeenLastCalledWith(expect.objectContaining({ eventType: 'Tamper', plate: '51A' }))
  })

  it('reloads immediately on severity and subjectType select changes', async () => {
    const wrapper = mount(EventTimeline)
    await flushPromises()
    const selects = wrapper.findAll('.form-select')
    await selects.at(0).setValue('Critical')
    await selects.at(1).setValue('Vehicle')
    expect(enterpriseApi.getEvents).toHaveBeenLastCalledWith(expect.objectContaining({ severity: 'Critical', subjectType: 'Vehicle' }))
  })

  it('paginates via next and prev buttons', async () => {
    enterpriseApi.getEvents.mockResolvedValue({ data: { items: sampleEvents, total: 200 } })
    const wrapper = mount(EventTimeline)
    await flushPromises()
    expect(wrapper.vm.totalPages).toBe(4)
    const nextBtn = wrapper.findAll('.page-btn:not([disabled])').find((b) => b.text() === '›')
    await nextBtn.trigger('click')
    expect(wrapper.vm.page).toBe(2)
    expect(enterpriseApi.getEvents).toHaveBeenLastCalledWith(expect.objectContaining({ page: 2 }))
    const prevBtn = wrapper.findAll('.page-btn').find((b) => b.text() === '‹')
    await prevBtn.trigger('click')
    expect(wrapper.vm.page).toBe(1)
  })

  it('disabled prev button at first page', async () => {
    const wrapper = mount(EventTimeline)
    await flushPromises()
    const prevBtn = wrapper.findAll('.page-btn').find((b) => b.text() === '‹')
    expect(prevBtn.attributes('disabled')).toBeDefined()
  })

  it('sevClass maps severities to badge classes', async () => {
    const wrapper = mount(EventTimeline)
    await flushPromises()
    expect(wrapper.vm.sevClass('Critical')).toBe('badge-danger')
    expect(wrapper.vm.sevClass('High')).toBe('badge-danger')
    expect(wrapper.vm.sevClass('Medium')).toBe('badge-warn')
    expect(wrapper.vm.sevClass('Info')).toBe('badge-info')
  })

  it('selects an event and opens the detail modal', async () => {
    const wrapper = mount(EventTimeline)
    await flushPromises()
    await wrapper.findAll('tbody tr').at(0).trigger('click')
    expect(wrapper.vm.selectedEvent.securityEventId).toBe(1)
    expect(wrapper.text()).toContain('Chi tiết sự kiện')
    expect(wrapper.text()).toContain('corr-1')
  })

  it('closes the detail modal via Đóng and via overlay click', async () => {
    const wrapper = mount(EventTimeline)
    await flushPromises()
    await wrapper.vm.selectEvent(sampleEvents[0])
    expect(wrapper.vm.selectedEvent).toBeTruthy()
    const closeBtn = wrapper.findAll('button').find((b) => b.text().includes('Đóng'))
    await closeBtn.trigger('click')
    expect(wrapper.vm.selectedEvent).toBe(null)

    await wrapper.vm.selectEvent(sampleEvents[0])
    const overlay = wrapper.findAll('.modal-overlay').at(0)
    await overlay.trigger('click')
    expect(wrapper.vm.selectedEvent).toBe(null)
  })

  it('deletes an event from the list after confirmation and reloads', async () => {
    const wrapper = mount(EventTimeline)
    await flushPromises()
    enterpriseApi.deleteEvent.mockResolvedValue({})
    await wrapper.find('tbody .btn-danger').trigger('click')
    await flushPromises()
    expect(globalThis.confirm).toHaveBeenCalledWith('Xóa sự kiện #1?')
    expect(enterpriseApi.deleteEvent).toHaveBeenCalledWith(1)
    expect(enterpriseApi.getEvents).toHaveBeenCalledTimes(2)
  })

  it('skips deletion when not confirmed', async () => {
    globalThis.confirm.mockReturnValue(false)
    const wrapper = mount(EventTimeline)
    await flushPromises()
    await wrapper.find('tbody .btn-danger').trigger('click')
    expect(enterpriseApi.deleteEvent).not.toHaveBeenCalled()
  })

  it('deletes selected event from detail modal and clears selection', async () => {
    const wrapper = mount(EventTimeline)
    await flushPromises()
    await wrapper.vm.selectEvent(sampleEvents[0])
    enterpriseApi.deleteEvent.mockResolvedValue({})
    await wrapper.vm.deleteEvent(sampleEvents[0])
    await flushPromises()
    expect(enterpriseApi.deleteEvent).toHaveBeenCalledWith(1)
    expect(wrapper.vm.selectedEvent).toBe(null)
  })

  it('alerts when deletion fails', async () => {
    enterpriseApi.deleteEvent.mockRejectedValue(new Error('denied'))
    const wrapper = mount(EventTimeline)
    await flushPromises()
    await wrapper.vm.deleteEvent(sampleEvents[0])
    await flushPromises()
    expect(globalThis.alert).toHaveBeenCalledWith('Xóa thất bại')
  })

  it('opens site maps modal via header button and loads maps', async () => {
    enterpriseApi.getSiteMaps.mockResolvedValue({ data: [{ siteMapId: 10, name: 'Tầng 1', siteId: 3, width: 100, height: 80 }] })
    const wrapper = mount(EventTimeline)
    await flushPromises()
    const mapsBtn = wrapper.findAll('button').find((b) => b.text().includes('Bản đồ khu vực'))
    await mapsBtn.trigger('click')
    expect(wrapper.vm.showSiteMaps).toBe(true)
    expect(wrapper.text()).toContain('Bản đồ khu vực')
    await wrapper.vm.loadSiteMaps()
    expect(enterpriseApi.getSiteMaps).toHaveBeenCalled()
    expect(wrapper.text()).toContain('Tầng 1')
  })

  it('loads site maps on demand and handles items shape and empty', async () => {
    enterpriseApi.getSiteMaps.mockResolvedValue({ data: { items: [{ siteMapId: 11, mapName: 'Tầng 2', siteId: 4 }] } })
    const wrapper = mount(EventTimeline)
    await flushPromises()
    await wrapper.vm.loadSiteMaps()
    expect(wrapper.vm.siteMaps.length).toBe(1)
    enterpriseApi.getSiteMaps.mockResolvedValue({ data: [] })
    await wrapper.vm.loadSiteMaps()
    expect(wrapper.vm.siteMaps).toEqual([])
    wrapper.vm.showSiteMaps = true
    await flushPromises()
    expect(wrapper.text()).toContain('Chưa có bản đồ khu vực.')
  })

  it('handles site maps load failure', async () => {
    enterpriseApi.getSiteMaps.mockRejectedValue(new Error('net'))
    const wrapper = mount(EventTimeline)
    await flushPromises()
    await wrapper.vm.loadSiteMaps()
    expect(wrapper.vm.siteMaps).toEqual([])
    expect(wrapper.vm.mapLoading).toBe(false)
  })

  it('shows placement for a map via Vị trí button and renders placements', async () => {
    enterpriseApi.getMapPlacements.mockResolvedValue({ data: [{ mapPlacementId: 5, securityDeviceId: 42, xCoordinate: 10, yCoordinate: 20 }, { mapPlacementId: 6, deviceId: 9, x: 3, y: 4 }, { id: 7, deviceId: 8, x: 5, y: 6 }] })
    const wrapper = mount(EventTimeline)
    await flushPromises()
    enterpriseApi.getSiteMaps.mockResolvedValue({ data: [{ siteMapId: 10, name: 'Tầng 1', siteId: 3, width: 100, height: 80 }, { siteMapId: 11, mapName: 'Tầng 2', siteId: 4 }, { id: 12, mapName: 'Tầng 3', siteId: 4 }] })
    await wrapper.vm.loadSiteMaps()
    wrapper.vm.showSiteMaps = true
    await flushPromises()
    const posBtn = wrapper.findAll('button').find((b) => b.text().includes('Vị trí'))
    await posBtn.trigger('click')
    await flushPromises()
    expect(enterpriseApi.getMapPlacements).toHaveBeenCalledWith(10)
    expect(wrapper.vm.selectedMap.siteMapId).toBe(10)
    expect(wrapper.text()).toContain('(10, 20)')
    expect(wrapper.text()).toContain('42')
    expect(wrapper.text()).toContain('(3, 4)')
    expect(wrapper.text()).toContain('9')
    expect(wrapper.text()).toContain('(5, 6)')
    expect(wrapper.text()).toContain('8')
    expect(wrapper.text()).toContain('100 x 80')
    expect(wrapper.text()).toContain('Tầng 2')
    expect(wrapper.text()).toContain('Tầng 3')
  })

  it('handles placements load failure and empty', async () => {
    enterpriseApi.getMapPlacements.mockRejectedValue(new Error('net'))
    const wrapper = mount(EventTimeline)
    await flushPromises()
    await wrapper.vm.showPlacementsForMap({ siteMapId: 10 })
    expect(wrapper.vm.placements).toEqual([])
    expect(wrapper.vm.placementLoading).toBe(false)
    enterpriseApi.getMapPlacements.mockResolvedValue({ data: [] })
    wrapper.vm.showSiteMaps = true
    await wrapper.vm.showPlacementsForMap({ siteMapId: 10 })
    await flushPromises()
    expect(wrapper.text()).toContain('Chưa có vị trí cho bản đồ này.')
  })

  it('toggles new map form and creates a site map', async () => {
    enterpriseApi.createSiteMap.mockResolvedValue({})
    const wrapper = mount(EventTimeline)
    await flushPromises()
    wrapper.vm.showSiteMaps = true
    await flushPromises()
    const newMapBtn = wrapper.findAll('button').find((b) => b.text().includes('Bản đồ mới'))
    await newMapBtn.trigger('click')
    expect(wrapper.vm.showNewMapForm).toBe(true)
    wrapper.vm.newMapForm.name = 'Tầng 3'
    wrapper.vm.newMapForm.siteId = 5
    await wrapper.vm.createSiteMap()
    expect(enterpriseApi.createSiteMap).toHaveBeenCalledWith({ name: 'Tầng 3', siteId: 5, width: 100, height: 100 })
    expect(wrapper.vm.showNewMapForm).toBe(false)
    expect(wrapper.vm.mapBusy).toBe(false)
  })

  it('createSiteMap skips when name is empty and alerts on failure', async () => {
    const wrapper = mount(EventTimeline)
    await flushPromises()
    wrapper.vm.newMapForm.name = ''
    await wrapper.vm.createSiteMap()
    expect(enterpriseApi.createSiteMap).not.toHaveBeenCalled()
    enterpriseApi.createSiteMap.mockRejectedValue(new Error('net'))
    wrapper.vm.newMapForm.name = 'Map'
    await wrapper.vm.createSiteMap()
    expect(globalThis.alert).toHaveBeenCalledWith('Không thể tạo bản đồ')
    expect(wrapper.vm.mapBusy).toBe(false)
  })

  it('shows mapBusy state while creating a map', async () => {
    let resolveFn
    enterpriseApi.createSiteMap.mockReturnValue(new Promise((r) => { resolveFn = r }))
    const wrapper = mount(EventTimeline)
    await flushPromises()
    wrapper.vm.newMapForm.name = 'Map'
    const pending = wrapper.vm.createSiteMap()
    await wrapper.vm.$nextTick()
    expect(wrapper.vm.mapBusy).toBe(true)
    resolveFn({})
    await pending
    await flushPromises()
    expect(wrapper.vm.mapBusy).toBe(false)
  })

  it('adds a placement and refreshes placements', async () => {
    enterpriseApi.addMapPlacement.mockResolvedValue({})
    enterpriseApi.getMapPlacements.mockResolvedValue({ data: [] })
    const wrapper = mount(EventTimeline)
    await flushPromises()
    wrapper.vm.selectedMap = { siteMapId: 10 }
    wrapper.vm.placementForm = { deviceId: 7, x: 11, y: 22 }
    await wrapper.vm.addPlacement()
    expect(enterpriseApi.addMapPlacement).toHaveBeenCalledWith(10, { securityDeviceId: 7, xCoordinate: 11, yCoordinate: 22 })
    expect(enterpriseApi.getMapPlacements).toHaveBeenCalled()
    expect(wrapper.vm.placementBusy).toBe(false)
  })

  it('addPlacement skips without selectedMap/deviceId and alerts on failure', async () => {
    const wrapper = mount(EventTimeline)
    await flushPromises()
    await wrapper.vm.addPlacement()
    expect(enterpriseApi.addMapPlacement).not.toHaveBeenCalled()
    wrapper.vm.selectedMap = { siteMapId: 10 }
    wrapper.vm.placementForm.deviceId = 7
    enterpriseApi.addMapPlacement.mockRejectedValue(new Error('net'))
    await wrapper.vm.addPlacement()
    expect(globalThis.alert).toHaveBeenCalledWith('Không thể thêm vị trí')
    expect(wrapper.vm.placementBusy).toBe(false)
  })

  it('opens create event modal and submits successfully', async () => {
    enterpriseApi.createEvent.mockResolvedValue({})
    const wrapper = mount(EventTimeline)
    await flushPromises()
    const createdBtn = wrapper.findAll('button').find((b) => b.text().includes('Sự kiện'))
    await createdBtn.trigger('click')
    expect(wrapper.vm.showCreateEvent).toBe(true)
    wrapper.vm.createEventForm.eventType = 'TamperDetected'
    wrapper.vm.createEventForm.summary = 'Mô tả'
    wrapper.vm.createEventForm.subjectType = 'Employee'
    wrapper.vm.createEventForm.subjectId = 'E9'
    await wrapper.vm.submitCreateEvent()
    expect(enterpriseApi.createEvent).toHaveBeenCalledWith({
      eventType: 'TamperDetected', severity: 'Info', subjectType: 'Employee', subjectId: 'E9', summary: 'Mô tả',
    })
    expect(wrapper.vm.showCreateEvent).toBe(false)
    expect(wrapper.vm.createEventForm.eventType).toBe('Info')
    expect(enterpriseApi.getEvents).toHaveBeenCalledTimes(2)
  })

  it('submitCreateEvent skips when event type is empty', async () => {
    const wrapper = mount(EventTimeline)
    await flushPromises()
    wrapper.vm.createEventForm.eventType = ''
    await wrapper.vm.submitCreateEvent()
    expect(enterpriseApi.createEvent).not.toHaveBeenCalled()
  })

  it('submitCreateEvent shows server error message', async () => {
    enterpriseApi.createEvent.mockRejectedValue({ response: { data: { message: 'Loại sự kiện không hợp lệ' } } })
    const wrapper = mount(EventTimeline)
    await flushPromises()
    wrapper.vm.showCreateEvent = true
    wrapper.vm.createEventForm.eventType = 'Bad'
    await wrapper.vm.submitCreateEvent()
    expect(wrapper.vm.createEventError).toBe('Loại sự kiện không hợp lệ')
    expect(wrapper.text()).toContain('Loại sự kiện không hợp lệ')
    expect(wrapper.vm.createEventBusy).toBe(false)
  })

  it('submitCreateEvent shows generic error without response', async () => {
    enterpriseApi.createEvent.mockRejectedValue(new Error('boom'))
    const wrapper = mount(EventTimeline)
    await flushPromises()
    wrapper.vm.createEventForm.eventType = 'Bad'
    await wrapper.vm.submitCreateEvent()
    expect(wrapper.vm.createEventError).toBe('boom')
  })

  it('deletes selected event from the detail modal via DOM button', async () => {
    enterpriseApi.deleteEvent.mockResolvedValue({})
    const wrapper = mount(EventTimeline)
    await flushPromises()
    await wrapper.vm.selectEvent(sampleEvents[0])
    const delBtn = wrapper.findAll('button').find((b) => b.text() === 'Xóa')
    await delBtn.trigger('click')
    await flushPromises()
    expect(enterpriseApi.deleteEvent).toHaveBeenCalledWith(1)
    expect(wrapper.vm.selectedEvent).toBe(null)
  })

  it('closes site maps modal via overlay click', async () => {
    const wrapper = mount(EventTimeline)
    await flushPromises()
    wrapper.vm.showSiteMaps = true
    await flushPromises()
    await wrapper.find('.modal-overlay').trigger('click')
    await flushPromises()
    expect(wrapper.vm.showSiteMaps).toBe(false)
  })

  it('fills the new map form via DOM inputs and cancels', async () => {
    const wrapper = mount(EventTimeline)
    await flushPromises()
    wrapper.vm.showSiteMaps = true
    await flushPromises()
    const newMapBtn = wrapper.findAll('button').find((b) => b.text().includes('Bản đồ mới'))
    await newMapBtn.trigger('click')
    await flushPromises()
    const inputs = wrapper.findAll('.modal-box .form-input')
    await inputs.at(0).setValue('Tầng 4')
    await inputs.at(1).setValue('6')
    await inputs.at(2).setValue('120')
    await inputs.at(3).setValue('140')
    expect(wrapper.vm.newMapForm.name).toBe('Tầng 4')
    expect(wrapper.vm.newMapForm.siteId).toBe(6)
    expect(wrapper.vm.newMapForm.width).toBe(120)
    expect(wrapper.vm.newMapForm.height).toBe(140)
    const cancelBtn = wrapper.findAll('button').find((b) => b.text() === 'Hủy')
    await cancelBtn.trigger('click')
    await flushPromises()
    expect(wrapper.vm.showNewMapForm).toBe(false)
  })

  it('fills the placement form via DOM inputs', async () => {
    enterpriseApi.getMapPlacements.mockResolvedValue({ data: [] })
    const wrapper = mount(EventTimeline)
    await flushPromises()
    wrapper.vm.showSiteMaps = true
    wrapper.vm.selectedMap = { siteMapId: 10, name: 'Tầng 1' }
    await flushPromises()
    const inputs = wrapper.findAll('.modal-box .form-input')
    await inputs.at(0).setValue('7')
    await inputs.at(1).setValue('11')
    await inputs.at(2).setValue('22')
    expect(wrapper.vm.placementForm.deviceId).toBe(7)
    expect(wrapper.vm.placementForm.x).toBe(11)
    expect(wrapper.vm.placementForm.y).toBe(22)
  })

  it('closes create event modal via overlay click', async () => {
    const wrapper = mount(EventTimeline)
    await flushPromises()
    wrapper.vm.showCreateEvent = true
    await flushPromises()
    await wrapper.find('.modal-overlay').trigger('click')
    await flushPromises()
    expect(wrapper.vm.showCreateEvent).toBe(false)
  })

  it('fills create event form via DOM inputs and submits from the modal button', async () => {
    enterpriseApi.createEvent.mockResolvedValue({})
    const wrapper = mount(EventTimeline)
    await flushPromises()
    wrapper.vm.showCreateEvent = true
    await flushPromises()
    const inputs = wrapper.findAll('.modal-box .form-input')
    await inputs.at(0).setValue('AccessGranted')
    await wrapper.find('.modal-box .form-select').setValue('High')
    await inputs.at(1).setValue('Vehicle')
    await inputs.at(2).setValue('V9')
    await inputs.at(3).setValue('Cho phép truy cập')
    const createBtn = wrapper.findAll('button').find((b) => b.text() === 'Tạo')
    await createBtn.trigger('click')
    await flushPromises()
    expect(enterpriseApi.createEvent).toHaveBeenCalledWith({
      eventType: 'AccessGranted', severity: 'High', subjectType: 'Vehicle', subjectId: 'V9', summary: 'Cho phép truy cập',
    })
    expect(wrapper.vm.showCreateEvent).toBe(false)
  })

  it('shows fallback dashes in detail modal for event without plate or summary', async () => {
    const wrapper = mount(EventTimeline)
    await flushPromises()
    await wrapper.vm.selectEvent(sampleEvents[1])
    expect(wrapper.vm.selectedEvent.securityEventId).toBe(2)
    expect(wrapper.text()).toContain('Biển số')
  })

  it('calls loadMapPlacements when clicking a map card body', async () => {
    enterpriseApi.getSiteMaps.mockResolvedValue({ data: [{ siteMapId: 10, name: 'Tầng 1', siteId: 3 }] })
    const wrapper = mount(EventTimeline)
    await flushPromises()
    wrapper.vm.showSiteMaps = true
    await wrapper.vm.loadSiteMaps()
    await flushPromises()
    const loadFn = vi.fn()
    wrapper.vm.$.setupState.loadMapPlacements = loadFn
    wrapper.vm.$.ctx.loadMapPlacements = loadFn
    await wrapper.find('.map-card').trigger('click')
    await flushPromises()
    expect(loadFn).toHaveBeenCalled()
  })

  it('renders placementBusy state on the Thêm button', async () => {
    const wrapper = mount(EventTimeline)
    await flushPromises()
    wrapper.vm.showSiteMaps = true
    wrapper.vm.selectedMap = { siteMapId: 10, name: 'Tầng 1' }
    wrapper.vm.placementBusy = true
    await flushPromises()
    expect(wrapper.text()).toContain('Đang thêm...')
    wrapper.vm.placementBusy = false
    await flushPromises()
    expect(wrapper.text()).toContain('Thêm')
  })

  it('shows create event success result banner', async () => {
    const wrapper = mount(EventTimeline)
    await flushPromises()
    wrapper.vm.showCreateEvent = true
    wrapper.vm.createEventResult = 'Đã tạo sự kiện!'
    await flushPromises()
    expect(wrapper.text()).toContain('Đã tạo sự kiện!')
  })

  it('falls back to empty list when getEvents returns no items', async () => {
    enterpriseApi.getEvents.mockResolvedValue({ data: { total: 0 } })
    const wrapper = mount(EventTimeline)
    await flushPromises()
    expect(wrapper.vm.events).toEqual([])
    expect(wrapper.text()).toContain('Không có sự kiện nào.')
  })

  it('falls back to empty maps when getSiteMaps returns non-array data', async () => {
    enterpriseApi.getSiteMaps.mockResolvedValue({ data: null })
    const wrapper = mount(EventTimeline)
    await flushPromises()
    await wrapper.vm.loadSiteMaps()
    expect(wrapper.vm.siteMaps).toEqual([])
  })

  it('falls back to empty placements when getMapPlacements returns non-array data', async () => {
    enterpriseApi.getMapPlacements.mockResolvedValue({ data: null })
    const wrapper = mount(EventTimeline)
    await flushPromises()
    await wrapper.vm.showPlacementsForMap({ siteMapId: 10 })
    expect(wrapper.vm.placements).toEqual([])
  })

  it('adds a placement using the map id fallback', async () => {
    enterpriseApi.addMapPlacement.mockResolvedValue({})
    enterpriseApi.getMapPlacements.mockResolvedValue({ data: [] })
    const wrapper = mount(EventTimeline)
    await flushPromises()
    wrapper.vm.selectedMap = { id: 20 }
    wrapper.vm.placementForm.deviceId = 5
    await wrapper.vm.addPlacement()
    expect(enterpriseApi.addMapPlacement).toHaveBeenCalledWith(20, { securityDeviceId: 5, xCoordinate: 50, yCoordinate: 50 })
  })

  it('renders embedded variant without the header', async () => {
    const wrapper = mount(EventTimeline, { props: { embedded: true } })
    await flushPromises()
    expect(wrapper.find('.page-header-bar').exists()).toBe(false)
    expect(wrapper.classes()).toContain('timeline-embedded')
  })

  it('closes create event modal via Hủy button', async () => {
    const wrapper = mount(EventTimeline)
    await flushPromises()
    wrapper.vm.showCreateEvent = true
    await flushPromises()
    const cancelBtn = wrapper.findAll('button').find((b) => b.text().includes('Hủy'))
    await cancelBtn.trigger('click')
    expect(wrapper.vm.showCreateEvent).toBe(false)
  })
})