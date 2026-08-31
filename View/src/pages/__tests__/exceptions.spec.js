import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const authState = vi.hoisted(() => ({ user: { role: 'Admin', fullName: 'Admin' } }))
vi.mock('../../stores/auth', () => ({ authState }))
vi.mock('../../services/accessLogApi', () => ({ getExceptions: vi.fn() }))
vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getLaneEvents: vi.fn(),
    getEvidenceItems: vi.fn(),
    getBarriers: vi.fn(),
    getBarrierCommands: vi.fn(),
    getCorrelations: vi.fn(),
    getInterventionRequests: vi.fn(),
    createInterventionRequest: vi.fn(),
    acceptInterventionRequest: vi.fn(),
    rejectInterventionRequest: vi.fn(),
    executeInterventionRequest: vi.fn(),
    recordLaneEvent: vi.fn(),
  },
}))

const accessLogApi = await import('../../services/accessLogApi')
const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const Exceptions = (await import('../Exceptions.vue')).default

const sharedStubs = {
  ExceptionCaseTimeline: true,
}

function rawItem(overrides = {}) {
  return {
    logId: 1,
    actorName: 'An',
    actorType: 'Staff',
    employeeId: 5,
    capturedLicensePlate: '51A-123.45',
    gateId: 7,
    gateName: 'Cổng A',
    method: 'manual',
    resultStatus: 'ALLOWED',
    exceptionReasonCode: 'TEMP_ACCESS',
    exceptionReasonDescription: 'Khách tạm',
    note: 'camera lỗi',
    timestamp: '2026-08-01T02:00:00.000Z',
    isBypass: false,
    ...overrides,
  }
}

function requestItem(overrides = {}) {
  return {
    operationalInterventionRequestId: 10,
    interventionType: 'temporary_grant',
    reason: 'Vụ việc ngoại lệ cần can thiệp',
    priority: 'high',
    status: 'Pending',
    requestedByUserId: 1,
    createdAtUtc: '2026-08-01T02:00:00.000Z',
    note: '',
    rejectedByUserId: 2,
    acceptedByUserId: 3,
    executedByUserId: 4,
    ...overrides,
  }
}

beforeEach(() => {
  vi.clearAllMocks()
  authState.user = { role: 'Admin', fullName: 'Admin' }
  accessLogApi.getExceptions.mockResolvedValue({ data: { items: [] } })
  enterpriseApi.getLaneEvents.mockResolvedValue({ data: { items: [] } })
  enterpriseApi.getEvidenceItems.mockResolvedValue({ data: { items: [] } })
  enterpriseApi.getBarriers.mockResolvedValue({ data: [] })
  enterpriseApi.getBarrierCommands.mockResolvedValue({ data: { items: [] } })
  enterpriseApi.getCorrelations.mockResolvedValue({ data: { items: [] } })
  enterpriseApi.getInterventionRequests.mockResolvedValue({ data: { items: [] } })
  enterpriseApi.createInterventionRequest.mockResolvedValue({ data: { operationalInterventionRequestId: 10 } })
  enterpriseApi.acceptInterventionRequest.mockResolvedValue({ data: { status: 'Accepted' } })
  enterpriseApi.rejectInterventionRequest.mockResolvedValue({ data: { status: 'Rejected' } })
  enterpriseApi.executeInterventionRequest.mockResolvedValue({ data: { status: 'Executed' } })
  enterpriseApi.recordLaneEvent.mockResolvedValue({ data: {} })
})

describe('Exceptions - loadAll and case building', () => {
  it('loads exceptions, maps cases, selects the first and syncs intervention statuses', async () => {
    accessLogApi.getExceptions.mockResolvedValue({
      data: {
        items: [
          rawItem({ logId: 1, exceptionReasonCode: 'TEMP_ACCESS', note: 'ok' }),
          rawItem({ logId: 2, exceptionReasonCode: 'TAILGATING', note: 'bypass' }),
          rawItem({ logId: 3, exceptionReasonCode: 'PLATE_REVIEW', note: 'UEBA_DEMO_SCENARIO: x' }),
        ],
      },
    })
    enterpriseApi.getInterventionRequests.mockResolvedValue({
      data: { items: [{ ...requestItem({ operationalInterventionRequestId: 10 }), status: 'Accepted' }] },
    })
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()

    expect(accessLogApi.getExceptions).toHaveBeenCalledWith({ pageSize: 100 })
    expect(wrapper.vm.exceptionCases).toHaveLength(2)
    expect(wrapper.vm.exceptionCases[0].category).toBe('pending_approval')
    expect(wrapper.vm.selectedCase).toEqual(wrapper.vm.exceptionCases[0])
    expect(wrapper.vm.loading).toBe(false)
    expect(wrapper.vm.openCaseCount).toBe(2)
  })

  it('keeps the previous selection after reload when it still exists', async () => {
    accessLogApi.getExceptions.mockResolvedValue({
      data: { items: [{ ...rawItem({ logId: 5, exceptionReasonCode: 'DEVICE' }), subjectName: 'May' }] },
    })
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    const keepId = wrapper.vm.exceptionCases[0].id
    wrapper.vm.selectedCase = wrapper.vm.exceptionCases[0]
    await wrapper.vm.loadAll()
    expect(wrapper.vm.selectedCase.id).toBe(keepId)
    expect(wrapper.vm.loading).toBe(false)
  })

  it('falls back to the first case when the previous selection disappears', async () => {
    accessLogApi.getExceptions.mockResolvedValue({
      data: { items: [{ ...rawItem({ logId: 1, exceptionReasonCode: 'TEMP_ACCESS' }) }] },
    })
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.selectedCase = { id: 'ghost' }
    await wrapper.vm.loadAll()
    expect(wrapper.vm.selectedCase.id).toBe(wrapper.vm.exceptionCases[0].id)
  })

  it('propagates loadAll failures but resets loading state', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    accessLogApi.getExceptions.mockRejectedValue(new Error('boom'))
    await expect(wrapper.vm.loadAll()).rejects.toThrow('boom')
    expect(wrapper.vm.loading).toBe(false)
  })

  it('classifies every exception reason into a category + severity', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    const b = (overrides) => wrapper.vm.buildCaseFromException(rawItem(overrides))

    expect(b({ logId: 1, isBypass: true }).category).toBe('manual_override')
    expect(b({ logId: 2, isBypass: true }).severity).toBe('high')
    expect(b({ logId: 3, exceptionReasonCode: 'TAILGATING', isBypass: false }).category).toBe('manual_override')
    expect(b({ logId: 4, exceptionReasonCode: 'QR_EXPIRED' }).category).toBe('pending_approval')
    expect(b({ logId: 5, exceptionReasonCode: 'QR_REPLAY' }).severity).toBe('medium')
    expect(b({ logId: 6, exceptionReasonCode: 'PLATE_REVIEW' }).severity).toBe('high')
    expect(b({ logId: 7, exceptionReasonCode: 'EMERGENCY_BYPASS', note: 'x' }).category).toBe('emergency_pass')
    expect(b({ logId: 8, exceptionReasonCode: '', note: 'tinh huong khan cap' }).category).toBe('emergency_pass')
    expect(b({ logId: 9, exceptionReasonCode: '', note: 'xe cap cuu huy' }).severity).toBe('critical')
    expect(b({ logId: 10, exceptionReasonCode: 'DURESS_PIN', note: 'x' }).category).toBe('duress')
    expect(b({ logId: 11, exceptionReasonCode: '', note: 'co ep buoc' }).severity).toBe('critical')
    expect(b({ logId: 12, exceptionReasonCode: '', note: 'duress event' }).category).toBe('duress')
    expect(b({ logId: 13, exceptionReasonCode: 'DEVICE_OFFLINE' }).category).toBe('device_degraded')
    expect(b({ logId: 14, exceptionReasonCode: '', note: 'scanner offline' }).category).toBe('device_degraded')
    expect(b({ logId: 15, exceptionReasonCode: '', note: 'camera down' }).category).toBe('device_degraded')
    expect(b({ logId: 16, exceptionReasonCode: '', note: 'degraded' }).severity).toBe('high')
    expect(b({ logId: 17, exceptionReasonCode: '', note: '', resultStatus: 'DENIED' }).category).toBe('data_mismatch')
    expect(b({ logId: 18, exceptionReasonCode: '', note: '' }).category).toBe('data_mismatch')
    expect(b({ logId: 19 }).subjectName).toBe('An')
    expect(b({ logId: 20, actorName: '' }).subjectName).toBe('Chưa rõ')
    expect(b({ logId: 21 }).reason).toBe('Khách tạm')
    expect(b({ logId: 22, exceptionReasonDescription: '' }).reason).toBe('Ngoại lệ cần đối soát')
    expect(b({ logId: 23 }).timeline[0].type).toBe('system')

    const first = b({ logId: 24 })
    expect(first.resolvedLaneId).toBeNull()
    expect(first.resolvedLaneName).toBe('')
    expect(first.interventionRequestId).toBeNull()
    expect(first.workflowStatus).toBe('Pending')
  })
})

describe('Exceptions - label and formatting helpers', () => {
  it('maps category labels and falls back to the raw value', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(wrapper.vm.categoryLabel('manual_override')).toBe('Override')
    expect(wrapper.vm.categoryLabel('duress')).toBe('Duress')
    expect(wrapper.vm.categoryLabel('unknown-cat')).toBe('unknown-cat')
  })

  it('maps severity, priority, workflow and status labels', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(wrapper.vm.severityLabel('critical')).toBe('Nghiêm trọng')
    expect(wrapper.vm.severityLabel('nope')).toBe('nope')
    expect(wrapper.vm.priorityLabel('low')).toBe('Thấp')
    expect(wrapper.vm.priorityLabel('nope')).toBe('nope')
    expect(wrapper.vm.workflowStatusLabel('Executed')).toBe('Đã thực thi')
    expect(wrapper.vm.workflowStatusLabel('nope')).toBe('nope')
    expect(wrapper.vm.statusLabel('Expired')).toBe('Hết hạn')
    expect(wrapper.vm.statusLabel('nope')).toBe('nope')
    expect(wrapper.vm.interventionTypeLabel('other')).toBe('Khác')
    expect(wrapper.vm.interventionTypeLabel('nope')).toBe('nope')
    expect(wrapper.vm.methodLabel('plate')).toBe('QR + biển số')
    expect(wrapper.vm.methodLabel('nope')).toBe('nope')
    expect(wrapper.vm.methodLabel('')).toBe('Hệ thống')
  })

  it('maps priority to a severity and defaults unknown priorities', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(wrapper.vm.prioritySeverity('critical')).toBe('critical')
    expect(wrapper.vm.prioritySeverity('high')).toBe('high')
    expect(wrapper.vm.prioritySeverity('medium')).toBe('medium')
    expect(wrapper.vm.prioritySeverity('low')).toBe('low')
    expect(wrapper.vm.prioritySeverity('weird')).toBe('medium')
  })

  it('formats datetimes and relative times', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(wrapper.vm.formatDateTime()).toBe('---')
    expect(wrapper.vm.formatDateTime('2026-08-01T02:00:00.000Z')).not.toBe('---')

    expect(wrapper.vm.formatRelativeTime()).toBe('---')
    expect(wrapper.vm.formatRelativeTime(new Date(Date.now() - 30 * 1000).toISOString())).toBe('Vừa xong')
    expect(wrapper.vm.formatRelativeTime(new Date(Date.now() - 3 * 60000).toISOString())).toBe('3 phút')
    expect(wrapper.vm.formatRelativeTime(new Date(Date.now() - 2 * 3600000).toISOString())).toBe('2 giờ')
    expect(wrapper.vm.formatRelativeTime(new Date(Date.now() - 3 * 86400000).toISOString())).toBe('3 ngày')
  })
})

describe('Exceptions - computed lists', () => {
  it('filters and sorts cases by category, search and severity', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.exceptionCases = [
      { id: 1, category: 'emergency_pass', severity: 'critical', subjectName: 'Binh', plateText: '', lastEventAt: '2026-08-01T02:00:00Z' },
      { id: 2, category: 'pending_approval', severity: 'high', subjectName: 'An', plateText: '51A-12', lastEventAt: '2026-08-01T03:00:00Z' },
      { id: 3, category: 'pending_approval', severity: 'high', subjectName: 'Yen', plateText: '', lastEventAt: '2026-08-01T01:00:00Z' },
      { id: 4, category: 'weird', severity: undefined, subjectName: 'Mai', plateText: '', lastEventAt: '2026-08-01T04:00:00Z' },
    ]
    expect(wrapper.vm.categoryCount('emergency_pass')).toBe(1)
    expect(wrapper.vm.caseCategories[0].count).toBe(4)
    expect(wrapper.vm.filteredCases[0].id).toBe(1)
    expect(wrapper.vm.filteredCases[3].id).toBe(4)

    wrapper.vm.activeCategory = 'pending_approval'
    expect(wrapper.vm.filteredCases).toHaveLength(2)
    expect(wrapper.vm.filteredCases[0].id).toBe(2)

    wrapper.vm.activeCategory = 'all'
    wrapper.vm.searchQuery = '51A-12'
    expect(wrapper.vm.filteredCases).toHaveLength(1)
    wrapper.vm.searchQuery = 'an'
    expect(wrapper.vm.filteredCases.map((c) => c.id)).toEqual([2])
  })

  it('counts open, pending and executed summary values', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.exceptionCases = [
      { category: 'x', workflowStatus: 'Closed' },
      { category: 'y', workflowStatus: 'Pending' },
      { category: 'z', workflowStatus: 'Executed' },
    ]
    wrapper.vm.interventionRequests = [
      { status: 'Pending' },
      { status: 'Pending' },
      { status: 'Executed' },
    ]
    expect(wrapper.vm.openCaseCount).toBe(1)
    expect(wrapper.vm.pendingInterventionCount).toBe(2)
    expect(wrapper.vm.executedInterventionCount).toBe(1)
  })

  it('computes intervention filter counts and handles missing status', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.interventionRequests = [
      { status: 'Pending' },
      { status: 'Accepted' },
      { status: 'Executed' },
      { status: 'Rejected' },
      { status: 'Expired' },
      { status: undefined },
    ]
    const filters = wrapper.vm.interventionFilters
    expect(filters.find((f) => f.value === 'all').count).toBe(6)
    expect(filters.find((f) => f.value === 'Pending').count).toBe(2)
    expect(filters.find((f) => f.value === 'Accepted').count).toBe(1)
    expect(filters.find((f) => f.value === 'Executed').count).toBe(1)
    expect(filters.find((f) => f.value === 'Rejected').count).toBe(1)
    expect(filters.find((f) => f.value === 'Expired').count).toBe(1)
  })

  it('filters and sorts intervention requests', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.interventionRequests = [
      { operationalInterventionRequestId: 1, priority: 'high', status: 'Pending', createdAtUtc: '2026-08-01T02:00:00Z' },
      { operationalInterventionRequestId: 2, priority: 'medium', status: 'Accepted', createdAtUtc: '2026-08-01T03:00:00Z' },
      { operationalInterventionRequestId: 3, priority: undefined, status: 'Pending', createdAtUtc: '2026-08-01T01:00:00Z' },
    ]
    expect(wrapper.vm.filteredInterventions[0].operationalInterventionRequestId).toBe(1)
    expect(wrapper.vm.filteredInterventions[2].operationalInterventionRequestId).toBe(3)
    wrapper.vm.interventionFilter = 'Accepted'
    expect(wrapper.vm.filteredInterventions).toHaveLength(1)
  })

  it('hides device_degraded category from guards unless it has cases', async () => {
    authState.user = { role: 'BaoVe' }
    const empty = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(empty.vm.caseCategories.find((c) => c.id === 'device_degraded')).toBeUndefined()

    empty.vm.exceptionCases = [{ id: 1, category: 'device_degraded', severity: 'high' }]
    expect(empty.vm.caseCategories.find((c) => c.id === 'device_degraded').count).toBe(1)
  })
})

describe('Exceptions - role based permissions', () => {
  it('exposes role flags and create permission', async () => {
    authState.user = { role: 'Admin' }
    const admin = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(admin.vm.isAdmin).toBe(true)
    expect(admin.vm.canManuallyCreateIntervention).toBe(true)

    authState.user = { role: 'QuanLy' }
    const quanLy = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(quanLy.vm.isQuanLy).toBe(true)
    expect(quanLy.vm.canManuallyCreateIntervention).toBe(false)

    authState.user = { role: 'BaoVe' }
    const baoVe = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(baoVe.vm.isBaoVe).toBe(true)
    expect(baoVe.vm.canManuallyCreateIntervention).toBe(true)
  })

  it('computes intervention action labels per role', async () => {
    const admin = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(admin.vm.casePrimaryActionLabel({ category: 'emergency_pass' })).toBe('Tạo và thực thi')
    expect(admin.vm.casePrimaryActionLabel({ category: 'data_mismatch' })).toBe('Tạo và duyệt')

    authState.user = { role: 'BaoVe' }
    const baoVe = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(baoVe.vm.casePrimaryActionLabel({ category: 'duress' })).toBe('Tạo yêu cầu can thiệp')

    authState.user = { role: 'QuanLy' }
    const quanLy = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(quanLy.vm.casePrimaryActionLabel({ category: 'x' })).toBe('Tạo và duyệt')
  })

  it('decides whether to execute immediately', async () => {
    const admin = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(admin.vm.shouldExecuteImmediately({ category: 'emergency_pass' })).toBe(true)
    expect(admin.vm.shouldExecuteImmediately({ category: 'device_degraded' })).toBe(true)
    expect(admin.vm.shouldExecuteImmediately({ category: 'pending_approval', employeeId: 5 })).toBe(true)
    expect(admin.vm.shouldExecuteImmediately({ category: 'pending_approval', employeeId: null })).toBe(false)
    expect(admin.vm.shouldExecuteImmediately({ category: 'data_mismatch' })).toBe(false)

    authState.user = { role: 'BaoVe' }
    const baoVe = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(baoVe.vm.shouldExecuteImmediately({ category: 'emergency_pass' })).toBe(false)
  })

  it('applies canCreate/canClose/canAccept/canReject/canExecute gates per role', async () => {
    authState.user = { role: 'BaoVe' }
    const baoVe = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(baoVe.vm.canCreateIntervention({ workflowStatus: 'Pending' })).toBe(true)
    expect(baoVe.vm.canCreateIntervention({ workflowStatus: 'Closed' })).toBe(false)
    expect(baoVe.vm.canCreateIntervention(null)).toBe(false)
    expect(baoVe.vm.canCloseCase({})).toBe(false)
    expect(baoVe.vm.canAcceptIntervention({ status: 'Pending' })).toBe(false)
    expect(baoVe.vm.canExecuteIntervention({ status: 'Accepted' })).toBe(false)

    authState.user = { role: 'QuanLy' }
    const quanLy = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(quanLy.vm.canCloseCase({})).toBe(true)
    expect(quanLy.vm.canCreateIntervention({ workflowStatus: 'Pending', category: 'duress' })).toBe(false)
    expect(quanLy.vm.canCreateIntervention({ workflowStatus: 'Pending', category: 'other' })).toBe(true)
    expect(quanLy.vm.canAcceptIntervention({ status: 'Pending' })).toBe(true)
    expect(quanLy.vm.canAcceptIntervention({ status: 'Accepted' })).toBe(false)
    expect(quanLy.vm.canRejectIntervention({ status: 'Pending' })).toBe(true)
    expect(quanLy.vm.canExecuteIntervention({ status: 'Accepted' })).toBe(false)

    authState.user = { role: 'Admin' }
    const admin = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(admin.vm.canAcceptIntervention(null)).toBe(false)
    expect(admin.vm.canExecuteIntervention({ status: 'Accepted' })).toBe(true)
    expect(admin.vm.canExecuteIntervention({ status: 'Pending' })).toBe(false)
  })

  it('builds intervention payloads for every category', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    const base = { sourceLogId: 5, reason: 'r', note: 'n', resolvedLaneId: 3, resolvedLaneName: 'Lane A', subjectName: 'An', employeeId: 5, actorType: 'Staff', plateText: '51A-12', severity: 'critical' }
    expect(wrapper.vm.buildInterventionPayload({ ...base, category: 'pending_approval' }).interventionType).toBe('temporary_grant')
    expect(wrapper.vm.buildInterventionPayload({ ...base, category: 'pending_approval', employeeId: null }).interventionType).toBe('policy_override')
    expect(wrapper.vm.buildInterventionPayload({ ...base, category: 'manual_override' }).interventionType).toBe('policy_override')
    expect(wrapper.vm.buildInterventionPayload({ ...base, category: 'device_degraded' }).interventionType).toBe('device_override')
    expect(wrapper.vm.buildInterventionPayload({ ...base, category: 'duress' }).interventionType).toBe('emergency_override')
    expect(wrapper.vm.buildInterventionPayload({ ...base, category: 'other' }).interventionType).toBe('other')
    expect(wrapper.vm.buildInterventionPayload({ ...base, category: 'other', severity: 'high' }).expiresInMinutes).toBe(240)
    expect(wrapper.vm.buildInterventionPayload({ ...base, category: 'duress', severity: 'critical', reason: '', note: '' }).expiresInMinutes).toBe(60)
    const light = wrapper.vm.buildInterventionPayload({ category: 'other', sourceLogId: 5 })
    expect(light.laneId).toBeUndefined()
    expect(light.subjectName).toBeUndefined()
    expect(light.reason).toBe('Vụ việc ngoại lệ cần can thiệp')
  })
})

describe('Exceptions - selection and primary actions', () => {
  it('selects a case and resets detail panels', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    const item = { id: 1, sourceLogId: 9 }
    wrapper.vm.selectCase(item)
    expect(wrapper.vm.selectedCase.id).toBe(1)
    expect(wrapper.vm.selectedCase.sourceLogId).toBe(9)
    expect(wrapper.vm.detailTab).toBe('timeline')
    expect(wrapper.vm.laneEvents).toEqual([])
    expect(wrapper.vm.caseActionMessage).toBe('')
  })

  it('selects an intervention and loads its rejection note', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    const item = { operationalInterventionRequestId: 3, rejectionReason: 'trùng lịch' }
    wrapper.vm.selectIntervention(item)
    expect(wrapper.vm.selectedIntervention.operationalInterventionRequestId).toBe(3)
    expect(wrapper.vm.interventionReviewNote).toBe('trùng lịch')
    expect(wrapper.vm.interventionMessage).toBe('')
  })

  it('runs the primary action as an admin and executes immediately', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    const item = wrapper.vm.buildCaseFromException(rawItem({ logId: 1, exceptionReasonCode: 'EMERGENCY_BYPASS', exceptionReasonDescription: 'Khẩn' }))
    await wrapper.vm.runPrimaryCaseAction(item)
    expect(enterpriseApi.createInterventionRequest).toHaveBeenCalled()
    expect(enterpriseApi.acceptInterventionRequest).toHaveBeenCalledWith(10, expect.any(Object))
    expect(enterpriseApi.executeInterventionRequest).toHaveBeenCalledWith(10, expect.any(Object))
    expect(item.workflowStatus).toBe('Executed')
    expect(item.timeline.some((t) => t.title.includes('thực thi'))).toBe(true)
    expect(wrapper.vm.caseActionMessage).toBe('Đã tạo và thực thi yêu cầu #10.')
    expect(wrapper.vm.saving).toBe(false)
  })

  it('runs the primary action as an admin without immediate execution', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    const item = wrapper.vm.buildCaseFromException(rawItem({ logId: 1, exceptionReasonCode: 'TAILGATING', employeeId: null }))
    await wrapper.vm.runPrimaryCaseAction(item)
    expect(enterpriseApi.executeInterventionRequest).not.toHaveBeenCalled()
    expect(wrapper.vm.caseActionMessage).toBe('Đã tạo và duyệt yêu cầu #10.')
  })

  it('runs the primary action as a guard without approve/execute', async () => {
    authState.user = { role: 'BaoVe' }
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    const item = wrapper.vm.buildCaseFromException(rawItem({ logId: 1, exceptionReasonCode: 'DURESS_PIN' }))
    await wrapper.vm.runPrimaryCaseAction(item)
    expect(enterpriseApi.acceptInterventionRequest).not.toHaveBeenCalled()
    expect(enterpriseApi.executeInterventionRequest).not.toHaveBeenCalled()
    expect(wrapper.vm.caseActionMessage).toBe('Đã tạo yêu cầu can thiệp #10.')
  })

  it('runs the primary action as a manager', async () => {
    authState.user = { role: 'QuanLy' }
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    const item = wrapper.vm.buildCaseFromException(rawItem({ logId: 1, exceptionReasonCode: 'DURESS_PIN' }))
    await wrapper.vm.runPrimaryCaseAction(item)
    expect(enterpriseApi.acceptInterventionRequest).toHaveBeenCalled()
    expect(enterpriseApi.executeInterventionRequest).not.toHaveBeenCalled()
    expect(wrapper.vm.caseActionMessage).toBe('Đã tạo và duyệt yêu cầu #10.')
  })

  it('skips approve/execute when the created request has no id', async () => {
    enterpriseApi.createInterventionRequest.mockResolvedValue({ data: {} })
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    const item = wrapper.vm.buildCaseFromException(rawItem({ logId: 1, exceptionReasonCode: 'EMERGENCY_BYPASS' }))
    await wrapper.vm.runPrimaryCaseAction(item)
    expect(enterpriseApi.acceptInterventionRequest).not.toHaveBeenCalled()
    expect(enterpriseApi.executeInterventionRequest).not.toHaveBeenCalled()
  })

  it('surfaces create workflow errors', async () => {
    enterpriseApi.createInterventionRequest.mockRejectedValue({ response: { data: { message: 'hết sức' } } })
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    const item = wrapper.vm.buildCaseFromException(rawItem({ logId: 1 }))
    await wrapper.vm.runPrimaryCaseAction(item)
    expect(wrapper.vm.caseActionError).toBe(true)
    expect(wrapper.vm.caseActionMessage).toBe('hết sức')

    enterpriseApi.createInterventionRequest.mockRejectedValue(new Error('plain'))
    await wrapper.vm.runPrimaryCaseAction(item)
    expect(wrapper.vm.caseActionMessage).toBe('Không thể tạo workflow can thiệp cho vụ việc này.')
  })

  it('closes a case and records a lane event', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    const item = wrapper.vm.buildCaseFromException(rawItem({ logId: 1 }))
    await wrapper.vm.closeCase(item)
    expect(enterpriseApi.recordLaneEvent).toHaveBeenCalledWith(expect.objectContaining({ eventType: 'CASE_CLOSED', plateText: '51A-123.45' }))
    expect(item.workflowStatus).toBe('Closed')
    expect(wrapper.vm.caseActionMessage).toContain('đã được khóa sổ')
  })

  it('surfaces close case errors', async () => {
    enterpriseApi.recordLaneEvent.mockRejectedValue({ response: { data: { message: 'khóa lỗi' } } })
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    const item = wrapper.vm.buildCaseFromException(rawItem({ logId: 1 }))
    await wrapper.vm.closeCase(item)
    expect(wrapper.vm.caseActionError).toBe(true)
    expect(wrapper.vm.caseActionMessage).toBe('khóa lỗi')
  })
})

describe('Exceptions - detail panels', () => {
  it('loads lane events with and without a plate', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()

    const withPlate = { plateText: '51A-12' }
    enterpriseApi.getLaneEvents.mockResolvedValue({ data: { items: [{ laneId: 3, lane: { name: 'Lane A' } }] } })
    await wrapper.vm.loadLaneEvents(withPlate)
    expect(enterpriseApi.getLaneEvents).toHaveBeenCalledWith({ plateText: '51A-12', pageSize: 20 })
    expect(withPlate.resolvedLaneId).toBe(3)
    expect(withPlate.resolvedLaneName).toBe('Lane A')

    const withLaneField = { plateText: '51B' }
    enterpriseApi.getLaneEvents.mockResolvedValue({ data: { items: [{ lane: { laneId: 4, name: 'Lane B' } }] } })
    await wrapper.vm.loadLaneEvents(withLaneField)
    expect(withLaneField.resolvedLaneId).toBe(4)
    expect(withLaneField.resolvedLaneName).toBe('Lane B')

    const noPlate = { plateText: '' }
    await wrapper.vm.loadLaneEvents(noPlate)
    expect(enterpriseApi.getLaneEvents).toHaveBeenLastCalledWith({ pageSize: 20 })

    const noItem = { plateText: 'x' }
    wrapper.vm.selectedCase = null
    await wrapper.vm.loadLaneEvents(null)
    expect(noItem.resolvedLaneId).toBeUndefined()

    enterpriseApi.getLaneEvents.mockRejectedValue(new Error('nope'))
    await wrapper.vm.loadLaneEvents({ plateText: '51C' })
    expect(wrapper.vm.laneEvents).toEqual([])
    expect(wrapper.vm.loadingLaneEvents).toBe(false)
  })

  it('loads evidence and correlations with fallback query and errors', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()

    enterpriseApi.getEvidenceItems.mockResolvedValue({ data: { items: [{ evidenceId: 1 }] } })
    await wrapper.vm.loadEvidence({ subjectName: 'Binh', plateText: '' })
    expect(enterpriseApi.getEvidenceItems).toHaveBeenCalledWith({ query: 'Binh', pageSize: 10 })
    expect(wrapper.vm.evidenceItems).toHaveLength(1)

    enterpriseApi.getEvidenceItems.mockRejectedValue(new Error('e'))
    await wrapper.vm.loadEvidence({ subjectName: '', plateText: '51A' })
    expect(wrapper.vm.evidenceItems).toEqual([])

    enterpriseApi.getCorrelations.mockResolvedValue({ data: { items: [{ correlationId: 2 }] } })
    await wrapper.vm.loadCorrelations({ subjectName: 'Yen' })
    expect(wrapper.vm.correlations).toHaveLength(1)

    enterpriseApi.getCorrelations.mockRejectedValue(new Error('c'))
    await wrapper.vm.loadCorrelations({ subjectName: 'Q' })
    expect(wrapper.vm.correlations).toEqual([])
  })

  it('loads barrier commands after resolving the lane', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()

    const resolved = { resolvedLaneId: 6 }
    enterpriseApi.getBarriers.mockResolvedValue({ data: [{ barrierId: 2 }] })
    enterpriseApi.getBarrierCommands.mockResolvedValue({ data: { items: [{ barrierCommandId: 7 }] } })
    await wrapper.vm.loadBarrierCommands(resolved)
    expect(enterpriseApi.getBarrierCommands).toHaveBeenCalledWith(2, { pageSize: 20 })
    expect(wrapper.vm.barrierCommands).toHaveLength(1)
  })

  it('reports when a lane cannot be resolved', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    enterpriseApi.getLaneEvents.mockResolvedValue({ data: { items: [] } })
    await wrapper.vm.loadBarrierCommands({ resolvedLaneId: null, plateText: '51A' })
    expect(wrapper.vm.barrierMessage).toContain('chưa truy ra được làn')
  })

  it('reports when no barrier is attached to the lane', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    enterpriseApi.getBarriers.mockResolvedValue({ data: [] })
    await wrapper.vm.loadBarrierCommands({ resolvedLaneId: 6 })
    expect(wrapper.vm.barrierMessage).toBe('Không có barrier nào được gắn với lane này.')
  })

  it('reports barrier command load errors', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    enterpriseApi.getBarriers.mockResolvedValue({ data: [{ barrierId: 2 }] })
    enterpriseApi.getBarrierCommands.mockRejectedValue(new Error('bc'))
    await wrapper.vm.loadBarrierCommands({ resolvedLaneId: 6 })
    expect(wrapper.vm.barrierMessage).toBe('Không thể tải lịch sử barrier command.')
  })
})

describe('Exceptions - interventions workflow', () => {
  it('loads interventions and selects the preferred request', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    const items = [
      requestItem({ operationalInterventionRequestId: 11 }),
      requestItem({ operationalInterventionRequestId: 12 }),
    ]
    enterpriseApi.getInterventionRequests.mockResolvedValue({ data: { items } })
    await wrapper.vm.loadInterventions(12)
    expect(wrapper.vm.selectedIntervention.operationalInterventionRequestId).toBe(12)

    wrapper.vm.selectedIntervention = null
    await wrapper.vm.loadInterventions()
    expect(wrapper.vm.selectedIntervention.operationalInterventionRequestId).toBe(11)
  })

  it('clears the selection when no interventions exist', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    enterpriseApi.getInterventionRequests.mockResolvedValue({ data: { items: [requestItem()] } })
    await wrapper.vm.loadInterventions()
    expect(wrapper.vm.selectedIntervention).not.toBeNull()
    enterpriseApi.getInterventionRequests.mockResolvedValue({ data: { items: [] } })
    await wrapper.vm.loadInterventions()
    expect(wrapper.vm.selectedIntervention).toBeNull()
  })

  it('handles intervention load errors', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    enterpriseApi.getInterventionRequests.mockRejectedValue(new Error('x'))
    await wrapper.vm.loadInterventions()
    expect(wrapper.vm.interventionRequests).toEqual([])
    expect(wrapper.vm.selectedIntervention).toBeNull()
  })

  it('refreshes the intervention selection and syncs cases', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.exceptionCases = [{ id: 1, interventionRequestId: 10, workflowStatus: 'Pending' }]
    enterpriseApi.getInterventionRequests.mockResolvedValue({ data: { items: [requestItem({ status: 'Executed' })] } })
    await wrapper.vm.refreshInterventionSelection(10, 'hoàn tất')
    expect(wrapper.vm.interventionMessage).toBe('hoàn tất')
    expect(wrapper.vm.interventionError).toBe(false)
    expect(wrapper.vm.exceptionCases[0].workflowStatus).toBe('Executed')
  })

  it('accepts an intervention', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    const item = requestItem()
    enterpriseApi.acceptInterventionRequest.mockResolvedValue({ data: { status: 'Accepted', operationalInterventionRequestId: 10 } })
    enterpriseApi.getInterventionRequests.mockResolvedValue({ data: { items: [requestItem({ status: 'Accepted' })] } })
    await wrapper.vm.acceptIntervention(item)
    expect(wrapper.vm.interventionMessage).toBe('Yêu cầu #10 đã được chấp nhận.')
    expect(wrapper.vm.interventionError).toBe(false)
  })

  it('rejects an intervention', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    const item = requestItem()
    enterpriseApi.rejectInterventionRequest.mockResolvedValue({ data: { status: 'Rejected' } })
    enterpriseApi.getInterventionRequests.mockResolvedValue({ data: { items: [requestItem({ status: 'Rejected' })] } })
    await wrapper.vm.rejectIntervention(item)
    expect(wrapper.vm.interventionMessage).toBe('Yêu cầu #10 đã bị từ chối.')
  })

  it('executes an intervention', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    const item = requestItem()
    enterpriseApi.executeInterventionRequest.mockResolvedValue({ data: { request: { status: 'Executed' } } })
    enterpriseApi.getInterventionRequests.mockResolvedValue({ data: { items: [requestItem({ status: 'Executed' })] } })
    await wrapper.vm.executeIntervention(item)
    expect(wrapper.vm.interventionMessage).toBe('Yêu cầu #10 đã được thực thi.')
  })

  it('surfaces accept/reject/execute errors', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    enterpriseApi.acceptInterventionRequest.mockRejectedValue(new Error('x'))
    await wrapper.vm.acceptIntervention(requestItem())
    expect(wrapper.vm.interventionError).toBe(true)
    expect(wrapper.vm.interventionMessage).toBe('Chấp nhận thất bại.')

    enterpriseApi.rejectInterventionRequest.mockRejectedValue({ response: { data: { message: 'từ chối lỗi' } } })
    await wrapper.vm.rejectIntervention(requestItem())
    expect(wrapper.vm.interventionMessage).toBe('từ chối lỗi')

    enterpriseApi.executeInterventionRequest.mockRejectedValue(new Error('y'))
    await wrapper.vm.executeIntervention(requestItem())
    expect(wrapper.vm.interventionMessage).toBe('Thực thi thất bại.')
  })

  it('builds intervention timelines from all recorded states', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(wrapper.vm.buildInterventionTimeline(requestItem({ rejectedAtUtc: 't', acceptedAtUtc: 't', executedAtUtc: 't', requestedByUserId: 0 }))).toHaveLength(4)
    expect(wrapper.vm.buildInterventionTimeline(requestItem({ rejectedAtUtc: 't' }))).toHaveLength(2)
    expect(wrapper.vm.buildInterventionTimeline(requestItem({ acceptedAtUtc: 't' }))).toHaveLength(2)
    expect(wrapper.vm.buildInterventionTimeline(requestItem({ executedAtUtc: 't' }))).toHaveLength(2)
    expect(wrapper.vm.buildInterventionTimeline(requestItem())).toHaveLength(1)
  })

  it('syncs case workflow statuses from interventions', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.interventionRequests = [requestItem({ operationalInterventionRequestId: 10, status: 'Executed' })]
    wrapper.vm.exceptionCases = [
      { id: 1, interventionRequestId: 10, workflowStatus: 'Pending' },
      { id: 2, interventionRequestId: null, workflowStatus: 'Pending' },
      { id: 3, interventionRequestId: 999, workflowStatus: 'Pending' },
    ]
    wrapper.vm.syncCasesWithInterventions()
    expect(wrapper.vm.exceptionCases[0].workflowStatus).toBe('Executed')
    expect(wrapper.vm.exceptionCases[1].workflowStatus).toBe('Pending')
    expect(wrapper.vm.exceptionCases[2].workflowStatus).toBe('Pending')
  })

  it('submits a manual intervention request', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.createForm.interventionType = 'device_override'
    wrapper.vm.createForm.subjectName = 'An'
    wrapper.vm.createForm.plateNumber = '51A'
    wrapper.vm.createForm.laneName = 'Lane 1'
    wrapper.vm.createForm.priority = 'high'
    wrapper.vm.createForm.reason = 'doanh'
    wrapper.vm.createForm.note = 'ghi chú'
    await wrapper.vm.submitInterventionRequest()
    expect(enterpriseApi.createInterventionRequest).toHaveBeenCalledWith(expect.objectContaining({ interventionType: 'device_override', expiresInMinutes: 240, laneName: 'Lane 1' }))
    expect(wrapper.vm.createMessage).toBe('Đã gửi yêu cầu can thiệp.')
    expect(wrapper.vm.createForm.reason).toBe('')
    expect(wrapper.vm.creating).toBe(false)
  })

  it('surfaces manual intervention submit errors', async () => {
    const wrapper = mount(Exceptions, { global: { stubs: sharedStubs } })
    await flushPromises()
    enterpriseApi.createInterventionRequest.mockRejectedValue({ response: { data: { message: 'hết mức' } } })
    wrapper.vm.createForm.reason = 'lý do'
    await wrapper.vm.submitInterventionRequest()
    expect(wrapper.vm.createError).toBe(true)
    expect(wrapper.vm.createMessage).toBe('hết mức')
  })
})