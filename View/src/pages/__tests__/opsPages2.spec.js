import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getVisits: vi.fn(),
    acceptForm: vi.fn(),
    checkInVisit: vi.fn(),
    getSiemExports: vi.fn(),
    getWebhookSubscriptions: vi.fn(),
    getWebhookDeliveries: vi.fn(),
    createWebhookSubscription: vi.fn(),
    getHealthInsights: vi.fn(),
    getHealthSummary: vi.fn(),
    getDeviceHealthHistory: vi.fn(),
    getDeviceConfigurations: vi.fn(),
    diagnoseDevice: vi.fn(),
    recordHealth: vi.fn(),
  },
}))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi

const KioskCheckIn = (await import('../KioskCheckIn.vue')).default
const SIEMExportStatus = (await import('../SIEMExportStatus.vue')).default
const WebhookDeliveryViewer = (await import('../WebhookDeliveryViewer.vue')).default
const DeviceHealth = (await import('../DeviceHealth.vue')).default

beforeEach(() => vi.clearAllMocks())

describe('KioskCheckIn', () => {
  it('searches visits, confirms and checks in', async () => {
    enterpriseApi.getVisits.mockResolvedValue({
      data: { items: [{ visitId: 5, visitorName: 'Khách A', visitorPhone: '0901', status: 'Approved', expectedInUtc: '2026-08-01T08:00:00Z', expectedOutUtc: '2026-08-01T10:00:00Z', hostEmployee: { fullName: 'Chủ nhà' }, ndaRequired: false }] },
    })
    const wrapper = mount(KioskCheckIn)
    await wrapper.find('input').setValue('Khách')
    await flushPromises()
    expect(enterpriseApi.getVisits).toHaveBeenCalledWith({ search: 'Khách', status: 'Approved', pageSize: 10 })
    expect(wrapper.text()).toContain('Khách A')

    await wrapper.find('.kiosk-result').trigger('click')
    expect(wrapper.text()).toContain('Xác nhận check-in')

    enterpriseApi.checkInVisit.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Check in').trigger('click')
    await flushPromises()
    expect(enterpriseApi.checkInVisit).toHaveBeenCalledWith(5, expect.objectContaining({ verificationStatus: 'Verified' }))
    expect(wrapper.text()).toContain('Check-in thành công!')
  })

  it('requires NDA acceptance before check-in when required', async () => {
    enterpriseApi.getVisits.mockResolvedValue({
      data: { items: [{ visitId: 1, visitorName: 'Khách NDA', status: 'Approved', ndaRequired: true }] },
    })
    const wrapper = mount(KioskCheckIn)
    await wrapper.find('input').setValue('NDA')
    await flushPromises()
    await wrapper.find('.kiosk-result').trigger('click')
    const checkInButton = wrapper.findAll('button').find((b) => b.text() === 'Check in')
    expect(checkInButton.attributes('disabled')).toBeDefined()

    await wrapper.find('input[type="checkbox"]').setValue(true)
    expect(checkInButton.attributes('disabled')).toBeUndefined()
  })
})

describe('SIEMExportStatus', () => {
  it('lists SIEM exports', async () => {
    enterpriseApi.getSiemExports.mockResolvedValue({ data: [{ outboxEventId: 1, sourceId: 5, eventType: 'Alarm', status: 'Completed', correlationId: 'c-1', createdAtUtc: '2026-08-01T00:00:00Z' }] })
    const wrapper = mount(SIEMExportStatus)
    await flushPromises()
    expect(wrapper.text()).toContain('Alarm')
    expect(wrapper.text()).toContain('Hoàn thành')
  })

  it('shows an empty state', async () => {
    enterpriseApi.getSiemExports.mockResolvedValue({ data: [] })
    const wrapper = mount(SIEMExportStatus)
    await flushPromises()
    expect(wrapper.text()).toContain('Không có dữ liệu xuất SIEM.')
  })
})

describe('WebhookDeliveryViewer', () => {
  it('lists subscriptions and computes delivery stats', async () => {
    enterpriseApi.getWebhookSubscriptions.mockResolvedValue({ data: [{ webhookSubscriptionId: 1, targetUrl: 'https://h', secretReference: 'secret-ref', eventTypes: '*', isActive: true, createdAtUtc: '2026-08-01T00:00:00Z' }] })
    enterpriseApi.getWebhookDeliveries.mockResolvedValue({
      data: [
        { status: 'Delivered' },
        { status: 'Delivered' },
        { status: 'Failed' },
      ],
    })
    const wrapper = mount(WebhookDeliveryViewer)
    await flushPromises()
    expect(wrapper.text()).toContain('https://h')
    expect(wrapper.text()).toContain('Hoạt động')
    expect(wrapper.find('.kpi-card strong').text()).toBe('3')
  })

  it('creates a webhook subscription', async () => {
    enterpriseApi.getWebhookSubscriptions.mockResolvedValue({ data: [] })
    enterpriseApi.getWebhookDeliveries.mockResolvedValue({ data: [] })
    const wrapper = mount(WebhookDeliveryViewer)
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text().includes('Tạo mới')).trigger('click')
    const inputs = wrapper.findAll('input.form-input')
    await inputs[0].setValue('https://new-hook')
    enterpriseApi.createWebhookSubscription.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Tạo').trigger('click')
    await flushPromises()
    expect(enterpriseApi.createWebhookSubscription).toHaveBeenCalledWith(expect.objectContaining({ targetUrl: 'https://new-hook' }))
  })
})

describe('DeviceHealth', () => {
  it('loads insights and health summary', async () => {
    enterpriseApi.getHealthInsights.mockResolvedValue({ data: [{ deviceId: 1, deviceName: 'CAM-01', predictedStatus: 'Degraded', summary: 'Sắp hỏng' }] })
    enterpriseApi.getHealthSummary.mockResolvedValue({ data: { totalDevices: 10, onlineCount: 8, degradedCount: 1, offlineCount: 1 } })
    const wrapper = mount(DeviceHealth)
    await flushPromises()
    expect(wrapper.text()).toContain('CAM-01')
    expect(wrapper.text()).toContain('Degraded')
    expect(wrapper.find('.metric-tile strong').text()).toBe('10')
  })

  it('loads health history for a device', async () => {
    enterpriseApi.getHealthInsights.mockResolvedValue({ data: [] })
    enterpriseApi.getHealthSummary.mockResolvedValue({ data: null })
    enterpriseApi.getDeviceHealthHistory.mockResolvedValue({ data: [{ healthLogId: 1, recordedAtUtc: '2026-08-01T00:00:00Z', status: 'Ok', message: 'Tốt' }] })
    const wrapper = mount(DeviceHealth)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Tải').trigger('click')
    await flushPromises()
    expect(enterpriseApi.getDeviceHealthHistory).toHaveBeenCalledWith(1, { pageSize: 50 })
    expect(wrapper.text()).toContain('Tốt')
  })

  it('runs an AI diagnosis', async () => {
    enterpriseApi.getHealthInsights.mockResolvedValue({ data: [] })
    enterpriseApi.getHealthSummary.mockResolvedValue({ data: null })
    enterpriseApi.diagnoseDevice.mockResolvedValue({ data: { diagnosis: 'Cần thay lens' } })
    const wrapper = mount(DeviceHealth)
    await flushPromises()
    await wrapper.findAll('input[type="number"]')[1].setValue(9)
    await wrapper.findAll('button').find((b) => b.text() === 'Chạy chẩn đoán AI').trigger('click')
    await flushPromises()
    expect(enterpriseApi.diagnoseDevice).toHaveBeenCalledWith(9)
    expect(wrapper.text()).toContain('Cần thay lens')
  })
})
