import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/employeeApi', () => ({ getAll: vi.fn(), getProtectedFaceImage: vi.fn() }))
vi.mock('../../services/guestProfileApi', () => ({ getVisitorDirectory: vi.fn() }))
vi.mock('../../services/dynamicQrVerifyApi', () => ({ verifyDynamicQr: vi.fn() }))
vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getLaneHealth: vi.fn(),
    getParkingAreas: vi.fn(),
    recordLaneEvent: vi.fn(),
    createParkingPermit: vi.fn(),
  },
}))

const employeeApi = await import('../../services/employeeApi')
const guestProfileApi = await import('../../services/guestProfileApi')
const dynamicQrVerifyApi = await import('../../services/dynamicQrVerifyApi')
const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const ManualParkingFallback = (await import('../ManualParkingFallback.vue')).default

const laneHealthData = [
  { laneId: 1, name: 'Lane A', direction: 'IN' },
  { laneId: 2, name: 'Lane B', direction: 'OUT' },
]
const parkingAreaData = [
  { parkingAreaId: 10, name: 'Khu A', availableSpots: 5 },
  { id: 11, name: 'Khu B', availableSpots: null },
]

beforeEach(() => {
  vi.clearAllMocks()
  employeeApi.getAll.mockResolvedValue({ data: [] })
  guestProfileApi.getVisitorDirectory.mockResolvedValue({ data: { items: [] } })
  enterpriseApi.getLaneHealth.mockResolvedValue({ data: laneHealthData })
  enterpriseApi.getParkingAreas.mockResolvedValue({ data: { items: parkingAreaData } })
  enterpriseApi.recordLaneEvent.mockResolvedValue({ data: {} })
  enterpriseApi.createParkingPermit.mockResolvedValue({ data: {} })
  employeeApi.getProtectedFaceImage.mockResolvedValue({ data: new Blob() })
  URL.createObjectURL = vi.fn(() => 'blob:face')
  URL.revokeObjectURL = vi.fn()
  vi.spyOn(console, 'error').mockImplementation(() => {})
  vi.spyOn(console, 'warn').mockImplementation(() => {})
})

afterEach(() => {
  vi.useRealTimers()
  vi.restoreAllMocks()
})

describe('ManualParkingFallback', () => {
  it('loads bootstrap and assigns default lanes on mount', async () => {
    const wrapper = mount(ManualParkingFallback)
    await flushPromises()
    expect(enterpriseApi.getLaneHealth).toHaveBeenCalled()
    expect(enterpriseApi.getParkingAreas).toHaveBeenCalledWith({ pageSize: 100 })
    expect(wrapper.vm.laneOptions).toHaveLength(2)
    expect(wrapper.vm.parkingAreas).toHaveLength(2)
    expect(wrapper.vm.lanes[0].selectedLaneId).toBe(1)
    expect(wrapper.vm.lanes[1].selectedLaneId).toBe(2)
    expect(wrapper.vm.loading).toBe(false)
  })

  it('handles bootstrap error and logs it', async () => {
    enterpriseApi.getLaneHealth.mockRejectedValue(new Error('boom'))
    const wrapper = mount(ManualParkingFallback)
    await flushPromises()
    expect(console.error).toHaveBeenCalled()
    expect(wrapper.vm.loading).toBe(false)
  })

  it('canSubmit requires all fields', async () => {
    const wrapper = mount(ManualParkingFallback)
    await flushPromises()
    const lane = wrapper.vm.lanes[0]
    expect(wrapper.vm.canSubmit(lane)).toBe(false)
    lane.selectedLaneId = 1
    lane.qrPayload = 'EMP:1'
    lane.plateNumber = '29A-1'
    expect(wrapper.vm.canSubmit(lane)).toBe(false)
    lane.subject = { kind: 'employee' }
    expect(wrapper.vm.canSubmit(lane)).toBe(true)
  })

  it('normalizePlate trims and uppercases', async () => {
    const wrapper = mount(ManualParkingFallback)
    await flushPromises()
    expect(wrapper.vm.normalizePlate('  ab-12 ')).toBe('AB-12')
    expect(wrapper.vm.normalizePlate(undefined)).toBe('')
  })

  it('buildInitials handles single, multi and empty names', async () => {
    const wrapper = mount(ManualParkingFallback)
    await flushPromises()
    expect(wrapper.vm.buildInitials('Nguyễn An')).toBe('NA')
    expect(wrapper.vm.buildInitials('Son')).toBe('SO')
    expect(wrapper.vm.buildInitials('   ')).toBe('--')
    expect(wrapper.vm.buildInitials('')).toBe('--')
  })

  it('switchLaneType toggles subject type and clears state', async () => {
    const wrapper = mount(ManualParkingFallback)
    await flushPromises()
    const lane = wrapper.vm.lanes[0]
    lane.subject = { kind: 'employee', faceUrl: 'blob:face' }
    lane.parkingAreaId = 5
    lane.qrPayload = 'x'
    lane.error = 'err'
    wrapper.vm.switchLaneType(lane, 'visitor')
    expect(lane.subjectType).toBe('visitor')
    expect(lane.subject).toBeNull()
    expect(lane.parkingAreaId).toBe(5)
    expect(lane.qrPayload).toBe('')
    expect(lane.error).toBe('')
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:face')

    lane.subject = { faceUrl: 'blob:face2' }
    wrapper.vm.switchLaneType(lane, 'employee')
    expect(lane.subjectType).toBe('employee')
    expect(lane.parkingAreaId).toBeNull()
  })

  it('resets a lane fully', async () => {
    const wrapper = mount(ManualParkingFallback)
    await flushPromises()
    const lane = wrapper.vm.lanes[0]
    Object.assign(lane, {
      selectedLaneId: 1,
      subject: { faceUrl: 'blob:face' },
      qrPayload: 'q',
      plateNumber: 'p',
      busy: true,
      error: 'e',
      resultTone: 'tone-allow',
      resultLabel: 'x',
      auditTitle: 't',
      auditMessage: 'm',
      waveLevel: 4,
      subjectType: 'visitor',
    })
    wrapper.vm.resetLane(lane)
    expect(lane.subject).toBeNull()
    expect(lane.subjectType).toBe('employee')
    expect(lane.busy).toBe(false)
    expect(lane.error).toBe('')
    expect(lane.resultTone).toBe('tone-idle')
    expect(lane.waveLevel).toBe(1)
    expect(URL.revokeObjectURL).toHaveBeenCalled()
  })

  it('clearSubject only revokes blob faces', async () => {
    const wrapper = mount(ManualParkingFallback)
    await flushPromises()
    const lane = wrapper.vm.lanes[0]
    lane.subject = { faceUrl: 'blob:face' }
    lane.searchResults = [{ key: 1 }]
    wrapper.vm.clearSubject(lane)
    expect(lane.subject).toBeNull()
    expect(lane.searchResults).toEqual([])
    expect(lane.query).toBe('')
    expect(URL.revokeObjectURL).toHaveBeenCalled()
    lane.subject = { faceUrl: 'http://cdn/x.png' }
    wrapper.vm.clearSubject(lane)
    expect(URL.revokeObjectURL).toHaveBeenCalledTimes(1)
  })

  describe('searchSubjects', () => {
    it('clears results for short query without calling API', async () => {
      vi.useFakeTimers()
      const wrapper = mount(ManualParkingFallback)
      await flushPromises()
      wrapper.vm.searchSubjects(wrapper.vm.lanes[0])
      expect(wrapper.vm.lanes[0].searching).toBe(false)
      expect(employeeApi.getAll).not.toHaveBeenCalled()
    })

    it('searches employees and maps results', async () => {
      vi.useFakeTimers()
      employeeApi.getAll.mockResolvedValue({
        data: {
          items: [
            { employeeId: 5, fullName: 'Nguyễn An', department: 'Kỹ thuật', employeeCode: 'E5', faceImageUrl: 'http://x/y.jpg' },
            { employeeId: 6, employeeCode: 'E6' },
          ],
        },
      })
      const wrapper = mount(ManualParkingFallback)
      await flushPromises()
      const lane = wrapper.vm.lanes[0]
      lane.query = 'An'
      wrapper.vm.searchSubjects(lane)
      expect(lane.searching).toBe(true)
      vi.advanceTimersByTime(260)
      await flushPromises()
      expect(employeeApi.getAll).toHaveBeenCalledWith({ name: 'An', pageSize: 8 })
      expect(lane.searchResults).toHaveLength(2)
      expect(lane.searchResults[0].displayName).toBe('Nguyễn An')
      expect(lane.searchResults[0].initials).toBe('NA')
      expect(lane.searchResults[1].initials).toBe('E6')
      expect(lane.searching).toBe(false)
    })

    it('searches visitors and maps results', async () => {
      vi.useFakeTimers()
      guestProfileApi.getVisitorDirectory.mockResolvedValue({
        data: {
          items: [
            { visitorDetailId: 9, fullName: 'Minh Khang', guestPhone: '0901', hostEmployeeName: 'Host', companyName: 'ACME', visitId: 77 },
            { guestId: 8, visitorName: 'Vy', latestVisitId: 88 },
          ],
        },
      })
      const wrapper = mount(ManualParkingFallback)
      await flushPromises()
      wrapper.vm.switchLaneType(wrapper.vm.lanes[0], 'visitor')
      const lane = wrapper.vm.lanes[0]
      lane.query = 'Mi'
      wrapper.vm.searchSubjects(lane)
      vi.advanceTimersByTime(260)
      await flushPromises()
      expect(guestProfileApi.getVisitorDirectory).toHaveBeenCalledWith({
        query: 'Mi',
        pageSize: 8,
        registrationStatus: 'Approved',
      })
      expect(lane.searchResults).toHaveLength(2)
      expect(lane.searchResults[0].kind).toBe('visitor')
      expect(lane.searchResults[0].idValue).toBe('9')
    })

    it('handles search errors', async () => {
      vi.useFakeTimers()
      employeeApi.getAll.mockRejectedValue(new Error('search fail'))
      const wrapper = mount(ManualParkingFallback)
      await flushPromises()
      const lane = wrapper.vm.lanes[0]
      lane.query = 'An'
      wrapper.vm.searchSubjects(lane)
      vi.advanceTimersByTime(260)
      await flushPromises()
      expect(lane.searchResults).toEqual([])
      expect(lane.searching).toBe(false)
    })
  })

  describe('pickSubject', () => {
    it('picks an employee with protected face image', async () => {
      employeeApi.getProtectedFaceImage.mockResolvedValue({ data: new Blob(['x']) })
      const wrapper = mount(ManualParkingFallback)
      await flushPromises()
      const lane = wrapper.vm.lanes[0]
      const item = {
        kind: 'employee',
        displayName: 'Nguyễn An',
        initials: 'NA',
        idValue: '5',
        idLabel: 'Mã NV',
        meta: 'meta',
        matchIds: ['5'],
        raw: { employeeId: 5, faceImageUrl: 'base64image', visitId: null },
      }
      await wrapper.vm.pickSubject(lane, item)
      expect(lane.subject.kind).toBe('employee')
      expect(lane.subject.faceUrl).toBe('blob:face')
      expect(lane.query).toBe('Nguyễn An')
      expect(lane.searchResults).toEqual([])
    })

    it('picks an employee with http face url (no blob fetch)', async () => {
      const wrapper = mount(ManualParkingFallback)
      await flushPromises()
      const lane = wrapper.vm.lanes[0]
      const item = {
        kind: 'employee',
        displayName: 'Son',
        initials: 'SO',
        raw: { employeeId: 5, faceImageUrl: 'http://cdn/x.jpg' },
      }
      await wrapper.vm.pickSubject(lane, item)
      expect(lane.subject.faceUrl).toBe('')
      expect(employeeApi.getProtectedFaceImage).not.toHaveBeenCalled()
    })

    it('handles protected face image failure', async () => {
      employeeApi.getProtectedFaceImage.mockRejectedValue(new Error('no'))
      const wrapper = mount(ManualParkingFallback)
      await flushPromises()
      const lane = wrapper.vm.lanes[0]
      const item = {
        kind: 'employee',
        displayName: 'Nguyễn An',
        raw: { employeeId: 5, faceImageUrl: 'data:image', visitId: null },
      }
      await wrapper.vm.pickSubject(lane, item)
      expect(lane.subject.faceUrl).toBe('')
    })

    it('picks a visitor with matchIds fallback', async () => {
      const wrapper = mount(ManualParkingFallback)
      await flushPromises()
      const lane = wrapper.vm.lanes[1]
      const item = {
        kind: 'visitor',
        displayName: 'Minh Khang',
        idValue: '9',
        raw: { visitorDetailId: 9, visitId: 77 },
      }
      await wrapper.vm.pickSubject(lane, item)
      expect(lane.subject.kind).toBe('visitor')
      expect(lane.subject.visitId).toBe(77)
      expect(lane.subject.matchIds).toEqual(['9'])
    })
  })

  describe('verifyLane', () => {
    const readyEmployeeLane = (wrapper) => {
      const lane = wrapper.vm.lanes[0]
      lane.selectedLaneId = 1
      lane.subject = { kind: 'employee', matchIds: ['5'], idValue: '5' }
      lane.qrPayload = 'EMP:5'
      lane.plateNumber = '29A-1'
      return lane
    }

    it('does nothing when not submittable', async () => {
      const wrapper = mount(ManualParkingFallback)
      await flushPromises()
      await wrapper.vm.verifyLane(wrapper.vm.lanes[0])
      expect(dynamicQrVerifyApi.verifyDynamicQr).not.toHaveBeenCalled()
    })

    it('allows a matching employee and logs the event', async () => {
      dynamicQrVerifyApi.verifyDynamicQr.mockResolvedValue({ success: true, data: { type: 'EMPLOYEE', employeeId: '5' } })
      const wrapper = mount(ManualParkingFallback)
      await flushPromises()
      const lane = readyEmployeeLane(wrapper)
      await wrapper.vm.verifyLane(lane)
      expect(dynamicQrVerifyApi.verifyDynamicQr).toHaveBeenCalledWith('EMP:5', 'manual-parking-lane-1')
      expect(enterpriseApi.recordLaneEvent).toHaveBeenCalledWith(expect.objectContaining({ eventType: 'MANUAL_PARKING_ALLOW' }))
      expect(lane.resultTone).toBe('tone-allow')
      expect(lane.resultLabel).toBe('Đã cho qua')
      expect(lane.waveLevel).toBe(4)
      expect(lane.busy).toBe(false)
    })

    it('denies on kind/id mismatch and logs deny event', async () => {
      dynamicQrVerifyApi.verifyDynamicQr.mockResolvedValue({ success: true, data: { type: 'EMPLOYEE', employeeId: '999' } })
      const wrapper = mount(ManualParkingFallback)
      await flushPromises()
      const lane = readyEmployeeLane(wrapper)
      await wrapper.vm.verifyLane(lane)
      expect(enterpriseApi.recordLaneEvent).toHaveBeenCalledWith(expect.objectContaining({ eventType: 'MANUAL_PARKING_DENY' }))
      expect(lane.resultTone).toBe('tone-deny')
      expect(lane.auditTitle).toBe('QR không khớp')
    })

    it('allows a visitor and creates a parking permit', async () => {
      dynamicQrVerifyApi.verifyDynamicQr.mockResolvedValue({ success: true, data: { visitorDetailId: '9', type: 'STATIC' } })
      const wrapper = mount(ManualParkingFallback)
      await flushPromises()
      const lane = wrapper.vm.lanes[0]
      lane.selectedLaneId = 1
      lane.subjectType = 'visitor'
      lane.subject = { kind: 'visitor', matchIds: ['9'], visitId: 77 }
      lane.parkingAreaId = 10
      lane.qrPayload = 'VIS:9'
      lane.plateNumber = '29A-1'
      await wrapper.vm.verifyLane(lane)
      expect(enterpriseApi.createParkingPermit).toHaveBeenCalledWith(expect.objectContaining({ visitId: 77, parkingAreaId: 10 }))
      expect(lane.resultTone).toBe('tone-allow')
    })

    it('handles permit creation failure gracefully', async () => {
      dynamicQrVerifyApi.verifyDynamicQr.mockResolvedValue({ success: true, data: { visitorDetailId: '9', type: 'STATIC' } })
      enterpriseApi.createParkingPermit.mockRejectedValue(new Error('permit fail'))
      const wrapper = mount(ManualParkingFallback)
      await flushPromises()
      const lane = wrapper.vm.lanes[0]
      lane.selectedLaneId = 1
      lane.subjectType = 'visitor'
      lane.subject = { kind: 'visitor', matchIds: ['9'], visitId: 77 }
      lane.parkingAreaId = 10
      lane.qrPayload = 'VIS:9'
      lane.plateNumber = '29A-1'
      await wrapper.vm.verifyLane(lane)
      expect(console.warn).toHaveBeenCalled()
      expect(lane.resultTone).toBe('tone-allow')
    })

    it('handles verification failure, sets error and logs deny', async () => {
      dynamicQrVerifyApi.verifyDynamicQr.mockRejectedValue({ message: 'bad qr' })
      const wrapper = mount(ManualParkingFallback)
      await flushPromises()
      const lane = readyEmployeeLane(wrapper)
      await wrapper.vm.verifyLane(lane)
      expect(lane.error).toBe('bad qr')
      expect(lane.resultTone).toBe('tone-deny')
      expect(enterpriseApi.recordLaneEvent).toHaveBeenCalledWith(expect.objectContaining({ eventType: 'MANUAL_PARKING_DENY' }))
      expect(lane.busy).toBe(false)
    })

    it('throws when verification success is false', async () => {
      dynamicQrVerifyApi.verifyDynamicQr.mockResolvedValue({ success: false, message: 'denied' })
      const wrapper = mount(ManualParkingFallback)
      await flushPromises()
      const lane = readyEmployeeLane(wrapper)
      await wrapper.vm.verifyLane(lane)
      expect(lane.error).toBe('denied')
    })

    it('handles failure to log a deny event after an error', async () => {
      dynamicQrVerifyApi.verifyDynamicQr.mockRejectedValue({ message: 'bad' })
      enterpriseApi.recordLaneEvent.mockRejectedValue(new Error('log fail'))
      const wrapper = mount(ManualParkingFallback)
      await flushPromises()
      const lane = readyEmployeeLane(wrapper)
      await wrapper.vm.verifyLane(lane)
      expect(console.warn).toHaveBeenCalled()
    })
  })

  it('unmounts and cleans up timers and blob urls', async () => {
    const wrapper = mount(ManualParkingFallback)
    await flushPromises()
    const lane = wrapper.vm.lanes[0]
    lane.subject = { faceUrl: 'blob:face' }
    lane.query = 'An'
    wrapper.vm.searchSubjects(lane)
    wrapper.unmount()
    expect(URL.revokeObjectURL).toHaveBeenCalled()
    vi.useRealTimers()
  })

  it('the refresh button calls loadBootstrap', async () => {
    enterpriseApi.getLaneHealth.mockResolvedValue({ data: [] })
    const wrapper = mount(ManualParkingFallback)
    await flushPromises()
    const calls = enterpriseApi.getLaneHealth.mock.calls.length
    await wrapper.find('.hero-actions button.btn-secondary').trigger('click')
    await flushPromises()
    expect(enterpriseApi.getLaneHealth.mock.calls.length).toBeGreaterThan(calls)
  })

  it('clears an existing search timer when searching again', async () => {
    vi.useFakeTimers()
    const wrapper = mount(ManualParkingFallback)
    await flushPromises()
    const lane = wrapper.vm.lanes[0]
    lane.query = 'An'
    wrapper.vm.searchSubjects(lane)
    wrapper.vm.searchSubjects(lane)
    vi.advanceTimersByTime(260)
    await flushPromises()
    expect(employeeApi.getAll).toHaveBeenCalledTimes(1)
    vi.useRealTimers()
  })

  describe('template interactions', () => {
    it('fires lane-select, type-pill, clear and reset handlers', async () => {
      const wrapper = mount(ManualParkingFallback)
      await flushPromises()
      await wrapper.findAll('select')[0].setValue('1')
      expect(wrapper.vm.lanes[0].selectedLaneId).toBe(1)
      const pills = wrapper.findAll('.type-pill')
      await pills[1].trigger('click')
      expect(wrapper.vm.lanes[0].subjectType).toBe('visitor')
      await pills[0].trigger('click')
      expect(wrapper.vm.lanes[0].subjectType).toBe('employee')
      await wrapper.find('.lane-actions button.btn-secondary').trigger('click')
      expect(wrapper.vm.lanes[0].query).toBe('')
    })

    it('fires parking-area select for a visitor lane', async () => {
      const wrapper = mount(ManualParkingFallback)
      await flushPromises()
      const lane = wrapper.vm.lanes[0]
      lane.subjectType = 'visitor'
      await wrapper.vm.$nextTick()
      const selects = wrapper.findAll('.lane-shell')[0].findAll('select')
      await selects[selects.length - 1].setValue('10')
      expect(lane.parkingAreaId).toBe(10)
    })

    it('fires search input, search result click and clear-subject', async () => {
      vi.useFakeTimers()
      employeeApi.getAll.mockResolvedValue({
        data: { items: [{ employeeId: 5, fullName: 'Nguyễn An', department: 'Kỹ thuật', employeeCode: 'E5' }] },
      })
      const wrapper = mount(ManualParkingFallback)
      await flushPromises()
      wrapper.vm.lanes[0].searchResults = [
        { key: 'emp-5', kind: 'employee', displayName: 'Nguyễn An', initials: 'NA', idValue: '5', idLabel: 'Mã NV', meta: 'x', raw: { employeeId: 5 } },
      ]
      await wrapper.vm.$nextTick()
      await wrapper.find('.lookup-shell input').setValue('An')
      await wrapper.find('button.search-result').trigger('click')
      await flushPromises()
      expect(wrapper.vm.lanes[0].subject.kind).toBe('employee')
      await wrapper.find('button.subject-clear').trigger('click')
      expect(wrapper.vm.lanes[0].subject).toBeNull()
      vi.useRealTimers()
    })

    it('verifies via qr enter and plate input handlers', async () => {
      dynamicQrVerifyApi.verifyDynamicQr.mockResolvedValue({ success: true, data: { type: 'EMPLOYEE', employeeId: '5' } })
      const wrapper = mount(ManualParkingFallback)
      await flushPromises()
      const lane = wrapper.vm.lanes[0]
      lane.selectedLaneId = 1
      lane.subject = { kind: 'employee', matchIds: ['5'], idValue: '5' }
      const plates = wrapper.findAll('.lane-shell')[0].findAll('.entry-grid input')
      await plates[0].setValue('29A-1')
      await plates[1].setValue('EMP:5')
      await plates[1].trigger('keyup.enter')
      await flushPromises()
      await wrapper.findAll('.lane-shell')[0].find('.lane-actions button.btn-primary').trigger('click')
      await flushPromises()
      expect(enterpriseApi.recordLaneEvent).toHaveBeenCalledTimes(2)
    })
  })
})
