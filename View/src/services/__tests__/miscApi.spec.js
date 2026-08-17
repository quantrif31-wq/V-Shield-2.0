import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../http', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    put: vi.fn(),
    patch: vi.fn(),
    delete: vi.fn(),
    request: vi.fn(),
    defaults: { headers: { common: {} } },
  },
}))

const http = (await import('../http')).default
const dynamicQrApi = await import('../dynamicQrApi')
const dynamicQrVerifyApi = await import('../dynamicQrVerifyApi')
const gateTransitApi = await import('../gateTransitApi')
const plateRecognitionApi = await import('../plateRecognitionApi')
const routingApi = await import('../routingApi')
const runtimeServiceApi = await import('../runtimeServiceApi')
const statisticsApi = await import('../statisticsApi')
const uebaApi = await import('../uebaApi')
const userApi = await import('../userApi')
const dashboardApi = await import('../dashboardApi')
const importExportApi = await import('../importExportApi')
const notificationApi = await import('../notificationApi')

beforeEach(() => vi.clearAllMocks())

describe('dynamicQrApi and dynamicQrVerifyApi', () => {
  it('generates and verifies dynamic qr payloads', async () => {
    http.post.mockResolvedValue({ data: { qr: 'x' } })
    await dynamicQrApi.generateDynamicQr('42')
    expect(http.post).toHaveBeenCalledWith('/dynamic-qr/generate', { employeeId: 42 })
    await dynamicQrVerifyApi.verifyDynamicQr('payload')
    expect(http.post).toHaveBeenCalledWith('/dynamic-qr/verify', { qrPayload: 'payload', scannerDevice: 'WEB_SCANNER' })
  })
})

describe('gateTransitApi', () => {
  it('scans gates/guests and reads manual subjects', () => {
    gateTransitApi.scanGate({ code: 'x' })
    expect(http.post).toHaveBeenCalledWith('/gate-transit/scan', { code: 'x' })
    gateTransitApi.scanGuest({ guestCode: 'y' })
    expect(http.post).toHaveBeenCalledWith('/gate-transit/scan-guest', { guestCode: 'y' })
    gateTransitApi.getManualSubject('a b/c')
    expect(http.get).toHaveBeenCalledWith('/gate-transit/manual-subject/a%20b%2Fc')
    gateTransitApi.getManualGates()
    expect(http.get).toHaveBeenCalledWith('/gate-transit/gates')
  })
})

describe('plateRecognitionApi', () => {
  it('covers plate recognition endpoints', () => {
    plateRecognitionApi.getDetectedPlates()
    expect(http.get).toHaveBeenCalledWith('/license-plates/plates')
    plateRecognitionApi.getCameraPlateSnapshot()
    expect(http.get).toHaveBeenCalledWith('/license-plates/camera-plates')
    plateRecognitionApi.fuzzyMatchPlate({ plate: '29A' })
    expect(http.post).toHaveBeenCalledWith('/license-plates/fuzzy-match', { plate: '29A' })
    plateRecognitionApi.getPlateTimeline('29A-1', { page: 1 })
    expect(http.get).toHaveBeenCalledWith('/license-plates/29A-1/timeline', { params: { page: 1 } })
    plateRecognitionApi.getPlateAnomalies('29A-1', { page: 1 })
    expect(http.get).toHaveBeenCalledWith('/license-plates/29A-1/anomalies', { params: { page: 1 } })
    plateRecognitionApi.suggestPlateCorrection({ x: 1 })
    expect(http.post).toHaveBeenCalledWith('/license-plates/suggest-correction', { x: 1 })
  })
})

describe('routingApi and runtimeServiceApi', () => {
  it('routes and manages runtime services', async () => {
    routingApi.routingApi.getRoute({ from: 'A', to: 'B' })
    expect(http.post).toHaveBeenCalledWith('/routing', { from: 'A', to: 'B' })
    http.get.mockResolvedValue({ data: [] })
    await runtimeServiceApi.getRuntimeServices()
    expect(http.get).toHaveBeenCalledWith('/runtime-services')
    http.put.mockResolvedValue({ data: {} })
    await runtimeServiceApi.updateRuntimeService('qr', { enabled: true })
    expect(http.put).toHaveBeenCalledWith('/runtime-services/qr', { enabled: true })
    http.post.mockResolvedValue({ data: {} })
    await runtimeServiceApi.startRuntimeService('qr')
    expect(http.post).toHaveBeenCalledWith('/runtime-services/qr/start')
    await runtimeServiceApi.stopRuntimeService('qr')
    expect(http.post).toHaveBeenCalledWith('/runtime-services/qr/stop')
  })
})

describe('statisticsApi and dashboardApi', () => {
  it('fetches statistics and dashboard data', async () => {
    http.get.mockResolvedValue({ data: { total: 5 } })
    await expect(statisticsApi.getSummary()).resolves.toEqual({ total: 5 })
    expect(http.get).toHaveBeenCalledWith('/Statistics/employees/summary')
    dashboardApi.getDashboardOverview()
    expect(http.get).toHaveBeenCalledWith('/dashboard/overview')
    dashboardApi.getDashboardIntelligence()
    expect(http.get).toHaveBeenCalledWith('/dashboard/intelligence')
    dashboardApi.getDashboardReports()
    expect(http.get).toHaveBeenCalledWith('/dashboard/reports')
  })
})

describe('uebaApi', () => {
  it('covers UEBA endpoints', () => {
    uebaApi.getUebaProfiles({ page: 1 })
    expect(http.get).toHaveBeenCalledWith('/ueba/profiles', { params: { page: 1 } })
    uebaApi.getUebaProfile(7)
    expect(http.get).toHaveBeenCalledWith('/ueba/profiles/7')
    uebaApi.rebuildUebaProfile(7)
    expect(http.post).toHaveBeenCalledWith('/ueba/profiles/7/rebuild')
    uebaApi.getUebaAnomalies({ page: 1 })
    expect(http.get).toHaveBeenCalledWith('/ueba/anomalies', { params: { page: 1 } })
    uebaApi.resolveUebaAnomaly(3, { x: 1 })
    expect(http.post).toHaveBeenCalledWith('/ueba/anomalies/3/resolve', { x: 1 })
    uebaApi.markUebaAnomalyFalsePositive(3)
    expect(http.post).toHaveBeenCalledWith('/ueba/anomalies/3/false-positive')
    uebaApi.getUebaSummary()
    expect(http.get).toHaveBeenCalledWith('/ueba/summary')
    uebaApi.explainEmployeeRisk(7)
    expect(http.post).toHaveBeenCalledWith('/ueba/employees/7/risk-explanation')
  })
})

describe('userApi', () => {
  it('covers user management endpoints', () => {
    userApi.getAll()
    expect(http.get).toHaveBeenCalledWith('/Users')
    userApi.getById(1)
    expect(http.get).toHaveBeenCalledWith('/Users/1')
    userApi.create({ username: 'u' })
    expect(http.post).toHaveBeenCalledWith('/Users', { username: 'u' })
    userApi.update(1, { x: 1 })
    expect(http.put).toHaveBeenCalledWith('/Users/1', { x: 1 })
    userApi.deleteUser(1)
    expect(http.delete).toHaveBeenCalledWith('/Users/1')
    userApi.resetMfa(1)
    expect(http.post).toHaveBeenCalledWith('/Users/1/mfa/reset')
    userApi.getOperationalScopeReference()
    expect(http.get).toHaveBeenCalledWith('/Users/scope-reference')
    userApi.replaceRolePermissions({ x: 1 })
    expect(http.put).toHaveBeenCalledWith('/Users/role-permissions', { x: 1 })
    userApi.getOperationalScopes(1)
    expect(http.get).toHaveBeenCalledWith('/Users/1/operational-scopes')
    userApi.replaceOperationalScopes(1, { x: 1 })
    expect(http.put).toHaveBeenCalledWith('/Users/1/operational-scopes', { x: 1 })
    userApi.getGateAccessReference()
    expect(http.get).toHaveBeenCalledWith('/Users/gate-access-reference')
    userApi.replaceRoleGatePermissions({ x: 1 })
    expect(http.put).toHaveBeenCalledWith('/Users/gate-access/roles', { x: 1 })
    userApi.getUserGateAccess(1)
    expect(http.get).toHaveBeenCalledWith('/Users/1/gate-access')
    userApi.replaceUserGateAccess(1, { x: 1 })
    expect(http.put).toHaveBeenCalledWith('/Users/1/gate-access', { x: 1 })
  })
})

describe('importExportApi', () => {
  it('imports and previews files as multipart', () => {
    const file = new Blob(['x'])
    importExportApi.importFile('Employees', file, { skipDuplicates: true })
    const config = http.post.mock.calls[0][2]
    expect(http.post.mock.calls[0][0]).toBe('/import-export/Employees/import')
    expect(config.timeout).toBe(300000)
    importExportApi.previewImport('Employees', file)
    expect(http.post.mock.calls[1][0]).toBe('/import-export/Employees/import/preview')
  })

  it('exports, downloads and lists history', () => {
    importExportApi.exportData('Employees', { format: 'csv' })
    expect(http.get).toHaveBeenCalledWith('/import-export/Employees/export', { params: { format: 'csv' } })
    importExportApi.downloadTemplate('Employees', 'xlsx')
    expect(http.get).toHaveBeenCalledWith('/import-export/Employees/template', { params: { format: 'xlsx' }, responseType: 'blob' })
    importExportApi.downloadResult(5)
    expect(http.get).toHaveBeenCalledWith('/import-export/download/5', { responseType: 'blob' })
    importExportApi.getHistory({ page: 1 })
    expect(http.get).toHaveBeenCalledWith('/import-export/history', { params: { page: 1 } })
    importExportApi.getHistoryById(5)
    expect(http.get).toHaveBeenCalledWith('/import-export/history/5')
    importExportApi.getFormats()
    expect(http.get).toHaveBeenCalledWith('/import-export/formats')
    importExportApi.getEntities()
    expect(http.get).toHaveBeenCalledWith('/import-export/entities')
  })

  it('covers AI-assisted import endpoints', () => {
    const file = new Blob(['x'])
    importExportApi.aiAnalyze('Employees', file)
    expect(http.post.mock.calls[0][0]).toBe('/import-export/Employees/ai/analyze')
    importExportApi.aiOcr('Employees', file)
    expect(http.post.mock.calls[1][0]).toBe('/import-export/Employees/ai/ocr')
    importExportApi.aiNormalize('Employees', 'sess')
    expect(http.post).toHaveBeenCalledWith('/import-export/Employees/ai/normalize', { sessionId: 'sess' })
    importExportApi.aiConfirm('Employees', 'sess', { overrideConflicts: true })
    expect(http.post).toHaveBeenCalledWith('/import-export/Employees/ai/confirm/sess', { confirmNormalization: true, overrideConflicts: true })
    importExportApi.aiPreview('Employees', 'sess')
    expect(http.get).toHaveBeenCalledWith('/import-export/Employees/ai/preview/sess')
    importExportApi.aiGetSynonyms()
    expect(http.get).toHaveBeenCalledWith('/import-export/synonyms')
    importExportApi.aiStatus()
    expect(http.get).toHaveBeenCalledWith('/import-export/status')
  })
})

describe('notificationApi helpers', () => {
  it('ranks severities with an info fallback', () => {
    expect(notificationApi.getSeverityRank('critical')).toBe(5)
    expect(notificationApi.getSeverityRank('unknown')).toBe(2)
  })

  it('normalizes explicit severities and derives from category', () => {
    expect(notificationApi.normalizeNotificationSeverity({ severity: 'warning' })).toBe('warning')
    expect(notificationApi.normalizeNotificationSeverity({ category: 'chat' })).toBe('success')
    expect(notificationApi.normalizeNotificationSeverity({ category: 'approval' })).toBe('caution')
    expect(notificationApi.normalizeNotificationSeverity({ category: 'alarm', referenceType: 'alarm', title: 'Khẩn cấp đột nhập' })).toBe('critical')
    expect(notificationApi.normalizeNotificationSeverity({ category: 'alarm', referenceType: 'alarm', title: 'thường' })).toBe('warning')
    expect(notificationApi.normalizeNotificationSeverity({})).toBe('info')
  })

  it('covers notification REST endpoints', () => {
    notificationApi.getNotifications(0, 20)
    expect(http.get).toHaveBeenCalledWith('/notifications', { params: { skip: 0, take: 20 } })
    notificationApi.getUnreadCount()
    expect(http.get).toHaveBeenCalledWith('/notifications/unread-count')
    notificationApi.markNotificationRead(1)
    expect(http.post).toHaveBeenCalledWith('/notifications/1/read')
    notificationApi.markAllNotificationsRead()
    expect(http.post).toHaveBeenCalledWith('/notifications/read-all')
    notificationApi.getNotificationRules()
    expect(http.get).toHaveBeenCalledWith('/notification-rules')
    notificationApi.createNotificationRule({ x: 1 })
    expect(http.post).toHaveBeenCalledWith('/notification-rules', { x: 1 })
    notificationApi.updateNotificationRule(1, { x: 1 })
    expect(http.put).toHaveBeenCalledWith('/notification-rules/1', { x: 1 })
    notificationApi.deleteNotificationRule(1)
    expect(http.delete).toHaveBeenCalledWith('/notification-rules/1')
    notificationApi.getRuleSuggestions('Admin')
    expect(http.get).toHaveBeenCalledWith('/notification-rules/suggestions', { params: { role: 'Admin' } })
  })
})
