import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({ route: { query: {}, params: {} } }))

vi.mock('vue-router', () => ({ useRoute: () => hoisted.route }))
vi.mock('../../services/socApi', () => ({
  socApi: {
    overview: vi.fn(),
    getAlarms: vi.fn(),
    getIncidents: vi.fn(),
    getSopExecutions: vi.fn(),
    getDispatchTasks: vi.fn(),
    getIntelligence: vi.fn(),
    classifyAlarm: vi.fn(),
    escalationRisk: vi.fn(),
    recommendSop: vi.fn(),
    getAlarmComments: vi.fn(),
    getIncidentTimeline: vi.fn(),
    acknowledgeAlarm: vi.fn(),
    assignAlarm: vi.fn(),
    closeAlarm: vi.fn(),
    addComment: vi.fn(),
    addIncidentTimelineItem: vi.fn(),
    startSopExecution: vi.fn(),
    createIncident: vi.fn(),
    closeIncident: vi.fn(),
  },
}))
vi.mock('../EventTimeline.vue', () => ({
  default: { name: 'EventTimeline', template: '<div class="timeline">EVENTS</div>' },
  __isTeleport: false,
}))

const socApi = (await import('../../services/socApi')).socApi
const SocAlarmConsole = (await import('../SocAlarmConsole.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  hoisted.route = { query: {}, params: {} }
  socApi.overview.mockResolvedValue({ data: { criticalOpenAlarms: 2, openAlarms: 5, activeSops: 1, openIncidents: 3, openDispatchTasks: 2, oldestOpenAlarmAgeMinutes: 10 } })
  socApi.getAlarms.mockResolvedValue({ data: { total: 1, page: 1, pageSize: 20, items: [{ alarmId: 1, alarmType: 'Xâm nhập', summary: 'Tóm tắt', severity: 'High', state: 'New', createdAtUtc: '2026-08-01T00:00:00Z', latitude: 10.7 }] } })
  socApi.getIncidents.mockResolvedValue({ data: { total: 1, page: 1, pageSize: 20, items: [{ incidentId: 2, title: 'Sự cố A', severity: 'Medium', status: 'Open', openedAtUtc: '2026-08-01', outcome: 'Đang xử lý' }] } })
  socApi.getSopExecutions.mockResolvedValue({ data: { items: [{ sopExecutionId: 3, sopTemplateId: 9, status: 'Running', startedAtUtc: '2026-08-01' }] } })
  socApi.getDispatchTasks.mockResolvedValue({ data: { items: [{ dispatchTaskId: 4, status: 'Pending', priority: 'High', locationText: 'Cổng A', instructions: 'Xử lý', createdAtUtc: '2026-08-01' }] } })
  socApi.getIntelligence.mockResolvedValue({ data: { summary: 'ok', statistics: { totalToday: 5, changePercent: 10, criticalOpenAlarms: 1, avgResolutionHours: 3 }, anomalies: [{ type: 'spike', detail: 'x', severity: 'high' }] } })
})

async function mountPage() {
  const wrapper = mount(SocAlarmConsole)
  await flushPromises()
  return wrapper
}

describe('SocAlarmConsole', () => {
  it('loads the SOC overview, alarms and incidents', async () => {
    const wrapper = await mountPage()
    expect(socApi.overview).toHaveBeenCalled()
    expect(socApi.getAlarms).toHaveBeenCalled()
    expect(socApi.getIncidents).toHaveBeenCalled()
  })

  it('computes risk label and class based on overview', async () => {
    const wrapper = await mountPage()
    expect(wrapper.vm.riskLabel).toContain('nghiêm trọng')
    wrapper.vm.overview.criticalOpenAlarms = 0
    wrapper.vm.overview.openAlarms = 12
    expect(wrapper.vm.riskLabel).toContain('Nhiều')
    expect(wrapper.vm.riskClass).toBe('warn')
    wrapper.vm.overview.openAlarms = 3
    expect(wrapper.vm.riskClass).toBe('success')
  })

  it('switches tabs and syncs tab from the route', async () => {
    hoisted.route.query = { tab: 'intel' }
    const wrapper = await mountPage()
    expect(wrapper.vm.tab).toBe('intel')
    wrapper.vm.tab = 'sops'
    expect(wrapper.vm.sopTotal).toBe(1)
    expect(wrapper.vm.dispatchTotal).toBe(1)
  })

  it('selects an alarm and loads AI insights and comments', async () => {
    const wrapper = await mountPage()
    socApi.classifyAlarm.mockResolvedValue({ data: { predictedSeverity: 'High', predictedAlarmType: 'X', confidence: 0.9, matchedKeywords: ['a'] } })
    socApi.escalationRisk.mockResolvedValue({ data: { riskScore: 70, recommendation: 'Chuyển cấp', factors: ['f1'] } })
    socApi.recommendSop.mockResolvedValue({ data: [{ sopTemplateId: 9, name: 'SOP1', relevanceScore: 80, stepCount: 3, reason: 'r' }] })
    socApi.getAlarmComments.mockResolvedValue({ data: [{ alarmCommentId: 1, userId: 5, comment: 'c', createdAtUtc: '2026-08-01' }] })
    await wrapper.vm.selectAlarm({ alarmId: 1, alarmType: 'X', severity: 'High', state: 'New', createdAtUtc: '2026-08-01', summary: 's' })
    await flushPromises()
    expect(wrapper.vm.classification.predictedSeverity).toBe('High')
    expect(wrapper.vm.escalation.riskScore).toBe(70)
    expect(wrapper.vm.sopRecommendations).toHaveLength(1)
    expect(wrapper.vm.comments).toHaveLength(1)
  })

  it('selects an incident and loads its timeline', async () => {
    const wrapper = await mountPage()
    socApi.getIncidentTimeline.mockResolvedValue({ data: [{ incidentTimelineItemId: 1, itemType: 'Note', text: 'x', createdAtUtc: '2026-08-01' }] })
    await wrapper.vm.selectIncident({ incidentId: 2, title: 'Sự cố A', severity: 'Medium', status: 'Open', openedAtUtc: '2026-08-01' })
    await flushPromises()
    expect(wrapper.vm.timelineItems).toHaveLength(1)
  })

  it('acknowledges a new alarm', async () => {
    const wrapper = await mountPage()
    await wrapper.vm.selectAlarm({ alarmId: 1, alarmType: 'X', severity: 'High', state: 'New', createdAtUtc: '2026-08-01', summary: 's' })
    socApi.acknowledgeAlarm.mockResolvedValue({})
    await wrapper.vm.acknowledgeAlarm()
    expect(socApi.acknowledgeAlarm).toHaveBeenCalledWith(1)
    expect(wrapper.vm.selectedAlarm.state).toBe('Acknowledged')
  })

  it('assignAlarm requires a user id', async () => {
    const wrapper = await mountPage()
    await wrapper.vm.selectAlarm({ alarmId: 1, alarmType: 'X', severity: 'High', state: 'New', createdAtUtc: '2026-08-01', summary: 's' })
    await wrapper.vm.assignAlarm()
    expect(socApi.assignAlarm).not.toHaveBeenCalled()
    wrapper.vm.assignForm.userId = 5
    wrapper.vm.assignForm.note = 'note'
    socApi.assignAlarm.mockResolvedValue({})
    await wrapper.vm.assignAlarm()
    expect(socApi.assignAlarm).toHaveBeenCalledWith(1, { assignedToUserId: 5, note: 'note' })
    expect(wrapper.vm.selectedAlarm.state).toBe('Assigned')
  })

  it('closeAlarmAction closes an alarm', async () => {
    const wrapper = await mountPage()
    await wrapper.vm.selectAlarm({ alarmId: 1, alarmType: 'X', severity: 'High', state: 'New', createdAtUtc: '2026-08-01', summary: 's' })
    wrapper.vm.closeForm.note = 'done'
    socApi.closeAlarm.mockResolvedValue({})
    await wrapper.vm.closeAlarmAction()
    expect(socApi.closeAlarm).toHaveBeenCalledWith(1, { note: 'done' })
    expect(wrapper.vm.selectedAlarm.state).toBe('Closed')
  })

  it('addComment adds and refreshes comments', async () => {
    const wrapper = await mountPage()
    await wrapper.vm.selectAlarm({ alarmId: 1, alarmType: 'X', severity: 'High', state: 'New', createdAtUtc: '2026-08-01', summary: 's' })
    await wrapper.vm.addComment()
    expect(socApi.addComment).not.toHaveBeenCalled()
    wrapper.vm.newComment = '  '
    await wrapper.vm.addComment()
    expect(socApi.addComment).not.toHaveBeenCalled()
    wrapper.vm.newComment = 'hay quá'
    socApi.addComment.mockResolvedValue({})
    socApi.getAlarmComments.mockResolvedValue({ data: [{ alarmCommentId: 2, comment: 'hay quá' }] })
    await wrapper.vm.addComment()
    expect(socApi.addComment).toHaveBeenCalledWith(1, { comment: 'hay quá' })
    expect(wrapper.vm.newComment).toBe('')
  })

  it('addTimelineItem adds and refreshes the timeline', async () => {
    const wrapper = await mountPage()
    await wrapper.vm.selectIncident({ incidentId: 2, title: 'A', severity: 'Medium', status: 'Open', openedAtUtc: '2026-08-01' })
    await wrapper.vm.addTimelineItem()
    expect(socApi.addIncidentTimelineItem).not.toHaveBeenCalled()
    wrapper.vm.newTimelineText = 'ghi chú'
    socApi.addIncidentTimelineItem.mockResolvedValue({})
    socApi.getIncidentTimeline.mockResolvedValue({ data: [{ incidentTimelineItemId: 3, text: 'ghi chú' }] })
    await wrapper.vm.addTimelineItem()
    expect(socApi.addIncidentTimelineItem).toHaveBeenCalledWith(2, { text: 'ghi chú' })
  })

  it('startSop launches a SOP execution and reloads', async () => {
    const wrapper = await mountPage()
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    await wrapper.vm.selectAlarm({ alarmId: 1, alarmType: 'X', severity: 'High', state: 'New', createdAtUtc: '2026-08-01', summary: 's' })
    socApi.startSopExecution.mockResolvedValue({})
    await wrapper.vm.startSop(9)
    expect(socApi.startSopExecution).toHaveBeenCalledWith({ alarmId: 1, sopTemplateId: 9 })
    expect(alertSpy).toHaveBeenCalled()
    alertSpy.mockRestore()
  })

  it('createIncidentAction creates a new incident', async () => {
    const wrapper = await mountPage()
    wrapper.vm.showIncidentForm = true
    wrapper.vm.incidentForm.title = 'Sự cố mới'
    wrapper.vm.incidentForm.severity = 'High'
    socApi.createIncident.mockResolvedValue({})
    await wrapper.vm.createIncidentAction()
    expect(socApi.createIncident).toHaveBeenCalledWith({ title: 'Sự cố mới', severity: 'High' })
    expect(wrapper.vm.showIncidentForm).toBe(false)
  })

  it('closeIncidentAction closes an incident', async () => {
    const wrapper = await mountPage()
    await wrapper.vm.selectIncident({ incidentId: 2, title: 'A', severity: 'Medium', status: 'Open', openedAtUtc: '2026-08-01' })
    wrapper.vm.closeIncidentForm.note = 'xong'
    socApi.closeIncident.mockResolvedValue({})
    await wrapper.vm.closeIncidentAction()
    expect(socApi.closeIncident).toHaveBeenCalledWith(2, { note: 'xong' })
    expect(wrapper.vm.selectedIncident.status).toBe('Closed')
  })

  it('severityColor and priorityColor map severities', async () => {
    const wrapper = await mountPage()
    expect(wrapper.vm.severityColor('Critical')).toBe('#d44747')
    expect(wrapper.vm.severityColor('High')).toBe('#d49b47')
    expect(wrapper.vm.severityColor('Medium')).toBe('#47a3d4')
    expect(wrapper.vm.severityColor('Low')).toBe('#74b47a')
    expect(wrapper.vm.priorityColor('Critical')).toBe('#d44747')
    expect(wrapper.vm.priorityColor('Medium')).toBe('#d49b47')
    expect(wrapper.vm.priorityColor('Low')).toBe('#74b47a')
  })

  it('stateClass maps all states', async () => {
    const wrapper = await mountPage()
    expect(wrapper.vm.stateClass('New')).toBe('state-new')
    expect(wrapper.vm.stateClass('Acknowledged')).toBe('state-ack')
    expect(wrapper.vm.stateClass('Assigned')).toBe('state-assigned')
    expect(wrapper.vm.stateClass('Escalated')).toBe('state-escalated')
    expect(wrapper.vm.stateClass('Closed')).toBe('state-closed')
    expect(wrapper.vm.stateClass('Other')).toBe('')
  })

  it('formatTime handles invalid values', async () => {
    const wrapper = await mountPage()
    expect(wrapper.vm.formatTime('')).toBe('')
    expect(wrapper.vm.formatTime(null)).toBe('')
  })

  it('handles load errors gracefully', async () => {
    socApi.overview.mockRejectedValue(new Error('boom'))
    socApi.getAlarms.mockRejectedValue(new Error('boom'))
    socApi.getIncidents.mockRejectedValue(new Error('boom'))
    const wrapper = await mountPage()
    expect(wrapper.vm.overview.openAlarms).toBe(0)
  })

  it('selectAlarm handles API errors', async () => {
    const wrapper = await mountPage()
    socApi.classifyAlarm.mockRejectedValue(new Error('boom'))
    await wrapper.vm.selectAlarm({ alarmId: 1, alarmType: 'X', severity: 'High', state: 'New', createdAtUtc: '2026-08-01', summary: 's' })
    await flushPromises()
    expect(wrapper.vm.classification).toBe(null)
  })
})
