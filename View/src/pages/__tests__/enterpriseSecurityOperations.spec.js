import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const adminAuth = { user: { role: 'Admin' } }
vi.mock('../../stores/auth', () => ({ authState: adminAuth }))

const mockEnterpriseApi = {
  overview: vi.fn(),
  configHealth: vi.fn(),
  assetMap: vi.fn(),
  getLaneHealth: vi.fn(),
  getActiveSecurityAlerts: vi.fn(),
  getHealthSummary: vi.fn(),
  stepUpStart: vi.fn(),
  stepUpVerify: vi.fn(),
  setStepUpSession: vi.fn(),
  getStepUpStatus: vi.fn(),
  upsertIdentityProvider: vi.fn(),
  importIdentityUsers: vi.fn(),
  createVirtualController: vi.fn(),
  injectSimulatorFault: vi.fn(),
  createAlarm: vi.fn(),
  startBackup: vi.fn(),
  createQaRun: vi.fn(),
  backfillDefaultSite: vi.fn(),
  simulateAccessPolicy: vi.fn(),
  getPolicyVersions: vi.fn(),
  getAccessRules: vi.fn(),
  createPolicyVersion: vi.fn(),
  submitPolicyVersion: vi.fn(),
  approvePolicyVersion: vi.fn(),
  activatePolicyVersion: vi.fn(),
  retirePolicyVersion: vi.fn(),
  createAccessRule: vi.fn(),
  startRestore: vi.fn(),
  recordSecurityCheck: vi.fn(),
  getOutboxEvents: vi.fn(),
  getBackupRuns: vi.fn(),
  getRestoreDrills: vi.fn(),
}
vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: mockEnterpriseApi,
  socIntelApi: { getIntelligence: vi.fn() },
}))

const mockAiApi = {
  analyzeIncident: vi.fn(),
  analyzeEvidence: vi.fn(),
  reviewExportRequest: vi.fn(),
  reviewRecommendation: vi.fn(),
  screenVisitor: vi.fn(),
  screenVehicle: vi.fn(),
  simulatePolicy: vi.fn(),
  explainPolicy: vi.fn(),
}
vi.mock('../../services/enterpriseAiApi', () => ({ enterpriseAiApi: mockAiApi }))

const enterpriseApi = mockEnterpriseApi
const aiApi = mockAiApi
const socIntelApi = (await import('../../services/enterpriseSecurityApi')).socIntelApi
const EnterpriseSecurityOperations = (await import('../EnterpriseSecurityOperations.vue')).default

function overviewArray() {
  return Array.from({ length: 9 }, () => ({ data: {} }))
}

function fullOverview() {
  return [
    { data: { companies: 2, sites: 3, accessPoints: 40 } },
    { data: { activeMappings: 12, enabledProviders: 1, terminatedEmployees: 3 } },
    { data: { policyVersions: 2 } },
    { data: { visits: 10, visitorCredentials: 5, watchlistEntries: 2, watchlistMatches: 1, barriers: 6, parkingPermits: 20, barrierCommands: 4 } },
    { data: { devices: 50, offlinePackages: 2 } },
    { data: { openAlarms: 7, criticalOpenAlarms: 1, openIncidents: 2, activeSops: 3, openDispatchTasks: 4 } },
    { data: { evidenceItems: 100, pendingExports: 2, activeLegalHolds: 1, collections: 3, accessLogs: 9, complianceReports: 2 } },
    { data: { pendingOutboxEvents: 3, failedOutboxEvents: 1, backupRuns: 5, restoreDrills: 2, securityChecks: 8, degradedDependencies: 1 } },
    { data: { pendingRequiredGates: 2, approvedReleaseCandidates: 4 } },
  ]
}

beforeEach(() => {
  vi.clearAllMocks()
  adminAuth.user.role = 'Admin'
  enterpriseApi.overview.mockResolvedValue(overviewArray())
  enterpriseApi.configHealth.mockResolvedValue({ data: [] })
  enterpriseApi.assetMap.mockResolvedValue({ data: {} })
  enterpriseApi.getLaneHealth.mockResolvedValue({ data: [] })
  enterpriseApi.getStepUpStatus.mockResolvedValue({ data: undefined })
  enterpriseApi.getPolicyVersions.mockResolvedValue({ data: [] })
  enterpriseApi.getAccessRules.mockResolvedValue({ data: [] })
  enterpriseApi.getOutboxEvents.mockResolvedValue({ data: { items: [] } })
  enterpriseApi.getBackupRuns.mockResolvedValue({ data: [] })
  enterpriseApi.getRestoreDrills.mockResolvedValue({ data: [] })
  socIntelApi.getIntelligence.mockResolvedValue({ data: {} })
})

afterEach(() => {
  vi.restoreAllMocks()
})

async function mountPage() {
  const wrapper = mount(EnterpriseSecurityOperations)
  await flushPromises()
  return wrapper
}

function findButton(wrapper, text) {
  const all = wrapper.findAll('button')
  return all.find((b) => b.text().includes(text))
}

async function submitForm(wrapper, buttonText) {
  const btn = findButton(wrapper, buttonText)
  const formEl = btn && btn.element.closest && btn.element.closest('form')
  if (formEl) {
    const form = wrapper.findAll('form').find((f) => f.element === formEl)
    await form.trigger('submit')
  } else if (btn) {
    await btn.trigger('click')
  }
  await flushPromises()
}

describe('EnterpriseSecurityOperations', () => {
  it('loads the enterprise overview on mount', async () => {
    const wrapper = await mountPage()
    expect(enterpriseApi.overview).toHaveBeenCalled()
    expect(wrapper.exists()).toBe(true)
    expect(enterpriseApi.getStepUpStatus).toHaveBeenCalled()
  })

  it('renders metrics when overview has data and switches workspaces', async () => {
    enterpriseApi.overview.mockResolvedValue(fullOverview())
    const wrapper = await mountPage()
    expect(wrapper.text()).toContain('Quản trị')
    expect(wrapper.text()).toContain('SOC')
    await findButton(wrapper, 'SOC').trigger('click')
    await flushPromises()
    expect(socIntelApi.getIntelligence).toHaveBeenCalled()
  })

  it('shows load error on overview failure', async () => {
    enterpriseApi.overview.mockRejectedValue({ response: { data: { message: 'boom' } } })
    const wrapper = await mountPage()
    expect(wrapper.text()).toContain('boom')
    expect(wrapper.text()).toContain('Cần chú ý')
  })

  it('handles non-admin user (hides privileged panels)', async () => {
    adminAuth.user.role = 'User'
    const wrapper = await mountPage()
    expect(wrapper.text()).not.toContain('Xác minh tăng cường')
  })

  it('analyzes an incident via AI', async () => {
    const wrapper = await mountPage()
    await findButton(wrapper, 'SOC').trigger('click')
    await flushPromises()
    await wrapper.findAll('input').find((i) => i.attributes('placeholder') === 'ID sự cố').setValue('5')
    aiApi.analyzeIncident.mockResolvedValue({ data: { severity: 'Critical', provider: 'AI', summary: 'done', recommendationId: 9, reasoningSummary: 'why' } })
    await findButton(wrapper, 'Phân tích bằng AI').trigger('click')
    await flushPromises()
    expect(aiApi.analyzeIncident).toHaveBeenCalledWith(5)
    expect(wrapper.text()).toContain('done')
    await findButton(wrapper, 'Phê duyệt').trigger('click')
    await flushPromises()
    expect(aiApi.reviewRecommendation).toHaveBeenCalledWith(9, 'Approved', expect.any(String))
  })

  it('shows incident error result when AI fails', async () => {
    const wrapper = await mountPage()
    await findButton(wrapper, 'SOC').trigger('click')
    await flushPromises()
    await wrapper.findAll('input').find((i) => i.attributes('placeholder') === 'ID sự cố').setValue('1')
    aiApi.analyzeIncident.mockRejectedValue({ message: 'nope' })
    await findButton(wrapper, 'Phân tích bằng AI').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Không thể phân tích')
  })

  it('switches to reception workspace and screens a visitor', async () => {
    const wrapper = await mountPage()
    await findButton(wrapper, 'Lễ tân').trigger('click')
    await flushPromises()
    await wrapper.findAll('input').find((i) => i.attributes('placeholder') === 'ID lượt thăm').setValue('3')
    aiApi.screenVisitor.mockResolvedValue({ data: { severity: 'Medium', summary: 'ok', provider: 'AI' } })
    await findButton(wrapper, 'Phân tích rủi ro').trigger('click')
    await flushPromises()
    expect(aiApi.screenVisitor).toHaveBeenCalledWith(3)
    expect(wrapper.text()).toContain('ok')
  })

  it('switches to gate workspace and screens a vehicle', async () => {
    const wrapper = await mountPage()
    await findButton(wrapper, 'Cổng xe').trigger('click')
    await flushPromises()
    await wrapper.findAll('input').find((i) => i.attributes('placeholder') === 'ID phương tiện').setValue('7')
    aiApi.screenVehicle.mockResolvedValue({ data: { severity: 'High', summary: 'veh ok', provider: 'AI' } })
    await findButton(wrapper, 'Phân tích rủi ro').trigger('click')
    await flushPromises()
    expect(aiApi.screenVehicle).toHaveBeenCalledWith(7)
    expect(wrapper.text()).toContain('veh ok')
  })

  it('switches to auditor workspace and analyzes evidence and export', async () => {
    const wrapper = await mountPage()
    await findButton(wrapper, 'Kiểm soát').trigger('click')
    await flushPromises()
    await wrapper.findAll('input').find((i) => i.attributes('placeholder') === 'ID chứng cứ').setValue('2')
    aiApi.analyzeEvidence.mockResolvedValue({ data: { severity: 'Low', summary: 'ev done', provider: 'AI', reasoningSummary: 'r', recommendationId: 1 } })
    await findButton(wrapper, 'Phân tích bằng AI').trigger('click')
    await flushPromises()
    expect(aiApi.analyzeEvidence).toHaveBeenCalledWith(2)
    expect(wrapper.text()).toContain('ev done')
    await findButton(wrapper, 'Từ chối').trigger('click')
    await flushPromises()
    expect(aiApi.reviewRecommendation).toHaveBeenCalledWith(1, 'Rejected', expect.any(String))

    await wrapper.findAll('input').find((i) => i.attributes('placeholder') === 'ID yêu cầu xuất').setValue('4')
    aiApi.reviewExportRequest.mockResolvedValue({ data: { severity: 'High', summary: 'exp', provider: 'AI' } })
    await findButton(wrapper, 'Kiểm tra xuất').trigger('click')
    await flushPromises()
    expect(aiApi.reviewExportRequest).toHaveBeenCalledWith(4)
  })

  it('switches to admin workspace and simulates AI policy', async () => {
    const wrapper = await mountPage()
    await findButton(wrapper, 'Quản trị').trigger('click')
    await flushPromises()
    await wrapper.findAll('input').find((i) => i.attributes('placeholder') === 'ID phiên bản chính sách').setValue('11')
    aiApi.simulatePolicy.mockResolvedValue({ data: { severity: 'Critical', summary: 'sim', provider: 'AI' } })
    await findButton(wrapper, 'Mô phỏng chính sách').trigger('click')
    await flushPromises()
    expect(aiApi.simulatePolicy).toHaveBeenCalledWith(11)

    aiApi.explainPolicy.mockResolvedValue({ data: { severity: 'Medium', summary: 'explain', provider: 'AI' } })
    await findButton(wrapper, 'Giải thích').trigger('click')
    await flushPromises()
    expect(aiApi.explainPolicy).toHaveBeenCalledWith(11)
  })

  it('performs step-up verification', async () => {
    const wrapper = await mountPage()
    enterpriseApi.stepUpStart.mockResolvedValue({ data: { sessionId: 's1' } })
    enterpriseApi.stepUpVerify.mockResolvedValue({ data: { sessionId: 's1', active: true, expiresAtUtc: '2026-01-01T00:00:00Z' } })
    await wrapper.findAll('input').find((i) => i.attributes('inputmode') === 'numeric').setValue('123456')
    await submitForm(wrapper, 'Xác minh')
    expect(enterpriseApi.stepUpStart).toHaveBeenCalled()
    expect(enterpriseApi.stepUpVerify).toHaveBeenCalled()
    expect(enterpriseApi.setStepUpSession).toHaveBeenCalledWith('s1')
    expect(wrapper.text()).toContain('Đã xác minh đến')
  })

  it('shows step-up failure message', async () => {
    const wrapper = await mountPage()
    enterpriseApi.stepUpStart.mockRejectedValue({ response: { data: { message: 'bad' } } })
    await submitForm(wrapper, 'Xác minh')
    expect(wrapper.text()).toContain('bad')
  })

  it('saves provider via runAction', async () => {
    const wrapper = await mountPage()
    enterpriseApi.upsertIdentityProvider.mockResolvedValue({ data: { message: 'saved' } })
    await submitForm(wrapper, 'Lưu nhà cung cấp')
    expect(enterpriseApi.upsertIdentityProvider).toHaveBeenCalled()
  })

  it('imports a user', async () => {
    const wrapper = await mountPage()
    enterpriseApi.importIdentityUsers.mockResolvedValue({ data: { message: 'ok' } })
    await submitForm(wrapper, 'Nhập người dùng')
    expect(enterpriseApi.importIdentityUsers).toHaveBeenCalledWith(1, [expect.objectContaining({ username: 'employee.001' })])
  })

  it('creates a virtual controller', async () => {
    const wrapper = await mountPage()
    enterpriseApi.createVirtualController.mockResolvedValue({ data: { message: 'ok' } })
    await submitForm(wrapper, 'Tạo bộ mô phỏng')
    expect(enterpriseApi.createVirtualController).toHaveBeenCalled()
  })

  it('injects a fault', async () => {
    const wrapper = await mountPage()
    wrapper.vm.faultForm.securityDeviceId = 5
    enterpriseApi.injectSimulatorFault.mockResolvedValue({ data: { message: 'ok' } })
    await submitForm(wrapper, 'Gây lỗi mô phỏng')
    expect(enterpriseApi.injectSimulatorFault).toHaveBeenCalled()
  })

  it('creates an alarm', async () => {
    const wrapper = await mountPage()
    enterpriseApi.createAlarm.mockResolvedValue({ data: { message: 'ok' } })
    await submitForm(wrapper, 'Tạo cảnh báo')
    expect(enterpriseApi.createAlarm).toHaveBeenCalledWith(expect.objectContaining({ alarmType: 'ManualDrill' }))
  })

  it('starts a backup and creates QA run and backfills site', async () => {
    const wrapper = await mountPage()
    enterpriseApi.startBackup.mockResolvedValue({ data: { message: 'ok' } })
    await submitForm(wrapper, 'Bắt đầu sao lưu')
    expect(enterpriseApi.startBackup).toHaveBeenCalled()
    enterpriseApi.createQaRun.mockResolvedValue({ data: { message: 'ok' } })
    await submitForm(wrapper, 'Ghi nhận lượt QA')
    expect(enterpriseApi.createQaRun).toHaveBeenCalled()
    enterpriseApi.backfillDefaultSite.mockResolvedValue({ data: { message: 'ok' } })
    await submitForm(wrapper, 'Chạy bổ sung an toàn')
    expect(enterpriseApi.backfillDefaultSite).toHaveBeenCalled()
  })

  it('handles runAction failure by pushing activity', async () => {
    const wrapper = await mountPage()
    enterpriseApi.createAlarm.mockRejectedValue({ response: { data: { message: 'fail' } } })
    await submitForm(wrapper, 'Tạo cảnh báo')
    expect(wrapper.text()).toContain('thất bại')
  })

  it('simulates access policy', async () => {
    const wrapper = await mountPage()
    enterpriseApi.simulateAccessPolicy.mockResolvedValue({ data: { result: 'Allow', reason: 'allowed', decisionMode: 'Simulation' } })
    await submitForm(wrapper, 'Mô phỏng quyết định')
    expect(enterpriseApi.simulateAccessPolicy).toHaveBeenCalled()
    expect(wrapper.text()).toContain('Cho phép')
  })

  it('loads policy versions and creates a version', async () => {
    const wrapper = await mountPage()
    await findButton(wrapper, 'Phiên bản').trigger('click')
    await flushPromises()
    expect(enterpriseApi.getPolicyVersions).toHaveBeenCalled()
    wrapper.vm.policyVersionForm.name = 'v2'
    wrapper.vm.policyVersionForm.changeSummary = 'summary'
    enterpriseApi.createPolicyVersion.mockResolvedValue({ data: { message: 'ok' } })
    await submitForm(wrapper, 'Tạo phiên bản mới')
    expect(enterpriseApi.createPolicyVersion).toHaveBeenCalledWith({ name: 'v2', changeSummary: 'summary' })
    expect(wrapper.text()).toContain('Đã tạo phiên bản policy mới.')
  })

  it('renders policy versions and runs governance actions', async () => {
    const wrapper = await mountPage()
    enterpriseApi.getPolicyVersions.mockResolvedValue({
      data: [
        { accessPolicyVersionId: 1, name: 'v-Draft', status: 'Draft', rules: 3, changeSummary: '' },
        { accessPolicyVersionId: 2, name: 'v-Pending', status: 'PendingApproval', rules: 1, changeSummary: '' },
        { accessPolicyVersionId: 3, name: 'v-Approved', status: 'Approved', rules: 2, changeSummary: '' },
        { accessPolicyVersionId: 4, name: 'v-Active', status: 'Active', rules: 5, changeSummary: '' },
        { accessPolicyVersionId: 5, name: 'v-Retired', status: 'Retired', rules: 0, changeSummary: '' },
      ],
    })
    await findButton(wrapper, 'Phiên bản').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('v-Draft')

    enterpriseApi.submitPolicyVersion.mockResolvedValue({ data: { message: 'ok' } })
    await findButton(wrapper, 'Gửi duyệt').trigger('click')
    await flushPromises()
    expect(enterpriseApi.submitPolicyVersion).toHaveBeenCalledWith(1)

    enterpriseApi.approvePolicyVersion.mockResolvedValue({ data: { message: 'ok' } })
    await findButton(wrapper, 'Phê duyệt').trigger('click')
    await flushPromises()
    expect(enterpriseApi.approvePolicyVersion).toHaveBeenCalledWith(2, expect.any(Object))

    enterpriseApi.activatePolicyVersion.mockResolvedValue({ data: { message: 'ok' } })
    await findButton(wrapper, 'Kích hoạt').trigger('click')
    await flushPromises()
    expect(enterpriseApi.activatePolicyVersion).toHaveBeenCalledWith(3)

    enterpriseApi.retirePolicyVersion.mockResolvedValue({ data: { message: 'ok' } })
    await findButton(wrapper, 'Ngừng hiệu lực').trigger('click')
    await flushPromises()
    expect(enterpriseApi.retirePolicyVersion).toHaveBeenCalledWith(4)
  })

  it('loads policy rules and creates a rule', async () => {
    const wrapper = await mountPage()
    await findButton(wrapper, 'Luật truy cập').trigger('click')
    await flushPromises()
    expect(enterpriseApi.getAccessRules).toHaveBeenCalled()
    wrapper.vm.policyRuleForm.accessLevelId = 9
    enterpriseApi.createAccessRule.mockResolvedValue({ data: { message: 'ok' } })
    await submitForm(wrapper, 'Tạo luật truy cập')
    expect(enterpriseApi.createAccessRule).toHaveBeenCalled()
    expect(wrapper.text()).toContain('Đã tạo luật truy cập mới.')
  })

  it('renders policy rules list with scope', async () => {
    const wrapper = await mountPage()
    enterpriseApi.getAccessRules.mockResolvedValue({
      data: [
        { accessRuleId: 1, allowAccess: true, subjectType: 'Employee', subjectId: 2, credentialType: 'QR', siteId: 1, securityZoneId: 3, accessPointId: 4, isActive: true },
        { accessRuleId: 2, allowAccess: false, subjectType: 'Visitor', credentialType: 'Any', subjectId: null, isActive: false },
      ],
    })
    await findButton(wrapper, 'Luật truy cập').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Cho phép')
  })

  it('switches to ops workspace and exercises restore, security, outbox, backups, drills', async () => {
    const wrapper = await mountPage()
    enterpriseApi.startRestore.mockResolvedValue({ data: { message: 'ok' } })
    enterpriseApi.recordSecurityCheck.mockResolvedValue({ data: { message: 'ok' } })
    await findButton(wrapper, 'Vận hành').trigger('click')
    await flushPromises()
    expect(enterpriseApi.getBackupRuns).toHaveBeenCalled()
    expect(enterpriseApi.getRestoreDrills).toHaveBeenCalled()
    expect(enterpriseApi.getOutboxEvents).toHaveBeenCalled()

    wrapper.vm.restoreForm.backupRunId = 3
    await submitForm(wrapper, 'Bắt đầu khôi phục')
    expect(enterpriseApi.startRestore).toHaveBeenCalled()

    await submitForm(wrapper, 'Ghi nhận kiểm tra')
    expect(enterpriseApi.recordSecurityCheck).toHaveBeenCalled()
  })

  it('filters and loads outbox and renders events', async () => {
    const wrapper = await mountPage()
    enterpriseApi.getOutboxEvents.mockResolvedValue({ data: { items: [{ outboxEventId: 1, eventType: 'ALARM_DRILL', status: 'Failed', retryCount: 2 }] } })
    await findButton(wrapper, 'Vận hành').trigger('click')
    await flushPromises()
    wrapper.vm.outboxFilter = 'Failed'
    await findButton(wrapper, 'Tải').trigger('click')
    await flushPromises()
    expect(enterpriseApi.getOutboxEvents).toHaveBeenCalledWith(expect.objectContaining({ status: 'Failed' }))
    expect(wrapper.text()).toContain('ALARM_DRILL')
  })

  it('renders backup runs and restore drills tables', async () => {
    const wrapper = await mountPage()
    enterpriseApi.getBackupRuns.mockResolvedValue({ data: [{ backupRunId: 1, profile: 'MediumCompany', status: 'Completed', startedAtUtc: '2026-01-01T00:00:00Z', achievedRpoMinutes: 15 }] })
    enterpriseApi.getRestoreDrills.mockResolvedValue({ data: [{ restoreDrillId: 2, status: 'Running', targetRtoMinutes: 60, startedAtUtc: '2026-01-01T00:00:00Z' }] })
    enterpriseApi.getOutboxEvents.mockResolvedValue({ data: { items: [] } })
    await findButton(wrapper, 'Vận hành').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('MediumCompany')
    expect(wrapper.text()).toContain('Đang chạy')
  })

  it('renders lane health summary with degraded lanes', async () => {
    enterpriseApi.getLaneHealth.mockResolvedValue({
      data: [
        { laneId: 1, name: 'Gate A', isDegraded: true, barrierCount: 2 },
        { laneId: 2, name: 'Gate B', isDegraded: false, barrierCount: 1 },
      ],
    })
    const wrapper = await mountPage()
    await findButton(wrapper, 'Cổng xe').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Gate A')
    expect(wrapper.text()).toContain('Cần theo dõi')
  })

  it('renders soc intel statistics and severity breakdown', async () => {
    const wrapper = await mountPage()
    socIntelApi.getIntelligence.mockResolvedValue({
      data: {
        summary: 'risk summary',
        overallRisk: 'cao',
        statistics: { totalToday: 9, changePercent: 5, criticalOpenAlarms: 1, openAlarms: 3, avgResolutionHours: 2, bySeverity: { Critical: 1, High: 2 } },
        anomalies: [],
      },
    })
    await findButton(wrapper, 'SOC').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Cảnh báo hôm nay')
  })

  it('handles no-outbox, empty backup and empty restore states and config findings', async () => {
    enterpriseApi.configHealth.mockResolvedValue({
      data: { status: 'Blocked', findings: [{ key: 'k1', status: 'Fail' }, { key: 'k2', status: 'Pass' }] },
    })
    const wrapper = await mountPage()
    await findButton(wrapper, 'Vận hành').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Chưa có sự kiện outbox.')
  })

  it('covers risk label variants via soc intel', async () => {
    const wrapper = await mountPage()
    socIntelApi.getIntelligence.mockResolvedValue({ data: { summary: 'x', overallRisk: 'trung_binh', statistics: { totalToday: 0, changePercent: -3, bySeverity: { Medium: 1 } } } })
    await findButton(wrapper, 'SOC').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Rủi ro trung bình')
    socIntelApi.getIntelligence.mockResolvedValue({ data: { summary: 'y', overallRisk: 'thap', statistics: { totalToday: 1, changePercent: 0, bySeverity: { Low: 1 } } } })
    const refreshButtons = wrapper.findAll('button').filter((b) => b.text() === 'Làm mới')
    refreshButtons[refreshButtons.length - 1].trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Rủi ro thấp')
  })
})
