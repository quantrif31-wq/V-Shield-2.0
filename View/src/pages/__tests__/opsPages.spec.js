import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getCorrelations: vi.fn(),
    getCorrelationDetail: vi.fn(),
    runCorrelation: vi.fn(),
    getOfflinePolicyPackages: vi.fn(),
    createOfflinePolicyPackage: vi.fn(),
    getOutboxEvents: vi.fn(),
    getWebhookDeliveries: vi.fn(),
    dispatchEvent: vi.fn(),
    getSecurityChecks: vi.fn(),
  },
}))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi

const CorrelationView = (await import('../CorrelationView.vue')).default
const OfflinePackages = (await import('../OfflinePackages.vue')).default
const OutboxViewer = (await import('../OutboxViewer.vue')).default
const VulnerabilityReleaseGateStatus = (await import('../VulnerabilityReleaseGateStatus.vue')).default

beforeEach(() => vi.clearAllMocks())

describe('CorrelationView', () => {
  it('lists correlations and opens a detail panel on selection', async () => {
    enterpriseApi.getCorrelations.mockResolvedValue({
      data: [{ eventCorrelationId: 1, ruleName: 'R1', severity: 'Critical', summary: 'Đột nhập', createdAtUtc: '2026-08-01T00:00:00Z' }],
    })
    const wrapper = mount(CorrelationView)
    await flushPromises()
    expect(wrapper.text()).toContain('R1')

    enterpriseApi.getCorrelationDetail.mockResolvedValue({
      data: { correlation: { ruleName: 'R1', severity: 'Critical', summary: 'Đột nhập' }, events: [{ securityEventId: 9, occurredAtUtc: '2026-08-01T00:00:00Z', eventType: 'INTRUSION', severity: 'High', subjectId: 'emp-1', plateText: '29A-1' }] },
    })
    await wrapper.get('.clickable-row').trigger('click')
    await flushPromises()
    expect(enterpriseApi.getCorrelationDetail).toHaveBeenCalledWith(1)
    expect(wrapper.text()).toContain('INTRUSION')
    expect(wrapper.text()).toContain('emp-1')
  })

  it('shows an empty state when there are no correlations', async () => {
    enterpriseApi.getCorrelations.mockResolvedValue({ data: [] })
    const wrapper = mount(CorrelationView)
    await flushPromises()
    expect(wrapper.text()).toContain('Chưa có tương quan.')
  })
})

describe('OfflinePackages', () => {
  it('lists packages and creates a new one through the form', async () => {
    enterpriseApi.getOfflinePolicyPackages.mockResolvedValue({
      data: [{ offlinePolicyPackageId: 1, securityDeviceId: 5, packageVersion: '1.0.0', status: 'Published', payloadHash: 'abcdef1234567890' }],
    })
    const wrapper = mount(OfflinePackages)
    await flushPromises()
    expect(wrapper.text()).toContain('1.0.0')
    expect(wrapper.text()).toContain('Đã xuất bản')

    await wrapper.findAll('button').find((b) => b.text() === 'Tạo gói').trigger('click')
    await wrapper.find('input[type="number"]').setValue(9)
    await wrapper.find('textarea').setValue('{"allowAll":true}')
    enterpriseApi.createOfflinePolicyPackage.mockResolvedValue({})
    enterpriseApi.getOfflinePolicyPackages.mockResolvedValue({ data: [] })
    await wrapper.findAll('button').find((b) => b.text() === 'Tạo').trigger('click')
    await flushPromises()
    expect(enterpriseApi.createOfflinePolicyPackage).toHaveBeenCalledWith(expect.objectContaining({ securityDeviceId: 9 }))
  })

  it('ignores submit when no device id is entered', async () => {
    enterpriseApi.getOfflinePolicyPackages.mockResolvedValue({ data: [] })
    const wrapper = mount(OfflinePackages)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Tạo gói').trigger('click')
    await wrapper.findAll('button').find((b) => b.text() === 'Tạo').trigger('click')
    expect(enterpriseApi.createOfflinePolicyPackage).not.toHaveBeenCalled()
  })
})

describe('OutboxViewer', () => {
  it('lists outbox events and webhook deliveries', async () => {
    enterpriseApi.getOutboxEvents.mockResolvedValue({ data: [{ outboxEventId: 1, eventType: 'AccessGranted', aggregateType: 'Access', status: 'Pending', correlationId: 'c-1', createdAtUtc: '2026-08-01T00:00:00Z' }] })
    enterpriseApi.getWebhookDeliveries.mockResolvedValue({ data: [{ webhookDeliveryId: 2, outboxEventId: 1, targetUrl: 'https://hook', signature: 'sig', attemptCount: 3, status: 'Delivered' }] })
    const wrapper = mount(OutboxViewer)
    await flushPromises()
    expect(enterpriseApi.getOutboxEvents).toHaveBeenCalledWith(expect.any(Object))
    expect(wrapper.text()).toContain('AccessGranted')
    expect(wrapper.text()).toContain('https://hook')
  })

  it('dispatches a pending event after confirmation', async () => {
    enterpriseApi.getOutboxEvents.mockResolvedValue({
      data: [{ outboxEventId: 1, eventType: 'X', aggregateType: 'A', status: 'Pending', correlationId: 'c', createdAtUtc: '2026-08-01T00:00:00Z' }],
    })
    enterpriseApi.getWebhookDeliveries.mockResolvedValue({ data: [] })
    const wrapper = mount(OutboxViewer)
    await flushPromises()

    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    enterpriseApi.dispatchEvent.mockResolvedValue({})
    enterpriseApi.getOutboxEvents.mockResolvedValue({ data: [] })
    await wrapper.findAll('button').find((b) => b.text() === 'Gửi đi').trigger('click')
    await flushPromises()
    expect(enterpriseApi.dispatchEvent).toHaveBeenCalledWith(1)
    confirmSpy.mockRestore()
  })
})

describe('VulnerabilityReleaseGateStatus', () => {
  it('computes gate status from check results', async () => {
    enterpriseApi.getSecurityChecks.mockResolvedValue({
      data: [
        { securityOperationsCheckId: 1, checkType: 'Dependency', targetName: 'npm', result: 'Pass', resultDetails: 'ok', timestamp: '2026-08-01T00:00:00Z' },
        { securityOperationsCheckId: 2, checkType: 'Secret', targetName: 'env', result: 'Fail', resultDetails: 'leak', timestamp: '2026-08-01T00:00:00Z' },
      ],
    })
    const wrapper = mount(VulnerabilityReleaseGateStatus)
    await flushPromises()
    expect(wrapper.text()).toContain('Đạt')
    expect(wrapper.text()).toContain('1/2')
    expect(wrapper.text()).toContain('Chặn')
  })

  it('treats an empty check set as unknown', async () => {
    enterpriseApi.getSecurityChecks.mockResolvedValue({ data: [] })
    const wrapper = mount(VulnerabilityReleaseGateStatus)
    await flushPromises()
    expect(wrapper.text()).toContain('Không có kiểm tra bảo mật.')
  })
})
