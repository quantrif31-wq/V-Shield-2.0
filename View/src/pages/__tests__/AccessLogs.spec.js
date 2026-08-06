import { flushPromises, mount } from '@vue/test-utils'
import { reactive } from 'vue'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import AccessLogs from '../AccessLogs.vue'

const route = reactive({ query: {} })
const replace = vi.fn()
const getAccessLogs = vi.fn()
const getAccessLogDetail = vi.fn()

vi.mock('vue-router', () => ({ useRoute: () => route, useRouter: () => ({ replace }) }))
vi.mock('../../services/accessLogApi', () => ({
  getAccessLogs: (...args) => getAccessLogs(...args),
  getAccessLogDetail: (...args) => getAccessLogDetail(...args),
  getAccessLogSummary: vi.fn(() => Promise.resolve({ data: { totalToday: 42, entriesToday: 24, exitsToday: 18, exceptionsToday: 1, bypassToday: 2, vehiclesInside: 7, successRate: 98 } })),
}))
vi.mock('../../services/deviceManagementApi', () => ({ getGates: vi.fn(() => Promise.resolve({ data: [{ gateId: 2, gateName: 'Cổng chính' }] })) }))

const log = { logId: 5, timestamp: '2026-08-05T08:30:00', actorName: 'Nguyễn Văn An', actorType: 'Employee', direction: 'IN', gateName: 'Cổng chính', cameraName: 'Camera 01', capturedLicensePlate: '51A-12345', method: 'face-and-plate', resultStatus: 'GRANTED', isBypass: false, isException: false }

describe('Access logs module', () => {
  beforeEach(() => {
    route.query = {}
    replace.mockReset()
    getAccessLogs.mockResolvedValue({ data: { items: [log], total: 1 } })
    getAccessLogDetail.mockResolvedValue({ data: log })
  })

  it('renders dense access data with semantic direction and result', async () => {
    const wrapper = mount(AccessLogs, { global: { stubs: { RouterLink: true, ExportModal: true, Teleport: true } } })
    await flushPromises()
    expect(wrapper.text()).toContain('Nguyễn Văn An')
    expect(wrapper.text()).toContain('Vào')
    expect(wrapper.text()).toContain('GRANTED')
    expect(wrapper.find('table').exists()).toBe(true)
  })

  it('normalizes reversed dates and persists filters in the URL', async () => {
    const wrapper = mount(AccessLogs, { global: { stubs: { RouterLink: true, ExportModal: true, Teleport: true } } })
    await flushPromises()
    await wrapper.get('#access-date-from').setValue('2026-08-10')
    await wrapper.get('#access-date-to').setValue('2026-08-01')
    await wrapper.findAll('button').find((button) => button.text() === 'Áp dụng lọc').trigger('click')
    expect(replace).toHaveBeenCalledWith({ query: expect.objectContaining({ from: '2026-08-01', to: '2026-08-10' }) })
  })
})
