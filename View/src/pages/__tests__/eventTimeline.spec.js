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

beforeEach(() => vi.clearAllMocks())
afterEach(() => vi.unstubAllGlobals())

describe('EventTimeline', () => {
  it('loads and renders security events', async () => {
    enterpriseApi.getEvents.mockResolvedValue({ data: { items: [{ securityEventId: 1, eventType: 'Intrusion', severity: 'High', summary: 'Đột nhập cổng A', occurredAtUtc: '2026-08-01T00:00:00Z' }], total: 1 } })
    const wrapper = mount(EventTimeline)
    await flushPromises()
    expect(enterpriseApi.getEvents).toHaveBeenCalledWith(expect.objectContaining({ page: 1 }))
    expect(wrapper.find('tbody').text()).toContain('Đột nhập cổng A')
  })

  it('deletes an event after confirmation', async () => {
    enterpriseApi.getEvents.mockResolvedValue({ data: { items: [{ securityEventId: 1, eventType: 'Intrusion', severity: 'High', summary: 'x' }], total: 1 } })
    const wrapper = mount(EventTimeline)
    await flushPromises()

    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    enterpriseApi.deleteEvent.mockResolvedValue({})
    await wrapper.find('tbody .btn-danger').trigger('click')
    await flushPromises()
    expect(enterpriseApi.deleteEvent).toHaveBeenCalledWith(1)
    confirmSpy.mockRestore()
  })

  it('creates a security event', async () => {
    enterpriseApi.getEvents.mockResolvedValue({ data: { items: [], total: 0 } })
    const wrapper = mount(EventTimeline)
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text().includes('Sự kiện')).trigger('click')
    await wrapper.find('.modal-box input.form-input').setValue('TamperDetected')
    await wrapper.find('.modal-box textarea').setValue('Cảm biến bị tác động')
    enterpriseApi.createEvent.mockResolvedValue({})
    await wrapper.find('.modal-box .btn-primary').trigger('click')
    await flushPromises()
    expect(enterpriseApi.createEvent).toHaveBeenCalledWith(expect.objectContaining({ eventType: 'TamperDetected', summary: 'Cảm biến bị tác động' }))
  })
})
