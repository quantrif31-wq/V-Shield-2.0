import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/employeeApi', () => ({ getAll: vi.fn() }))
vi.mock('../../services/guestProfileApi', () => ({
  deleteVisitorDirectoryItem: vi.fn(),
  getVisitorAccessLogs: vi.fn(),
  getVisitorDirectory: vi.fn(),
  updateVisitorDirectoryItem: vi.fn(),
}))
vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: { getFormTemplates: vi.fn(), getParkingPermits: vi.fn() },
}))

const employeeApi = await import('../../services/employeeApi')
const guestProfileApi = await import('../../services/guestProfileApi')
const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const GuestProfiles = (await import('../GuestProfiles.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  enterpriseApi.getFormTemplates.mockResolvedValue({ data: { items: [] } })
  enterpriseApi.getParkingPermits.mockResolvedValue({ data: [] })
})
afterEach(() => vi.unstubAllGlobals())

describe('GuestProfiles', () => {
  it('loads and renders the visitor directory', async () => {
    guestProfileApi.getVisitorDirectory.mockResolvedValue({
      data: { items: [{ visitorDetailId: 7, fullName: 'Khách A', idCardNumber: '0123456789', hostEmployeeName: 'Host 1', guestPhone: '0901', registrationStatus: 'Approved', ndaStatus: 'Signed' }], total: 1 },
    })
    employeeApi.getAll.mockResolvedValue({ data: [] })
    const wrapper = mount(GuestProfiles, { global: { stubs: { ImportModal: true, ExportModal: true } } })
    await flushPromises()
    expect(wrapper.find('tbody').text()).toContain('Khách A')
    expect(wrapper.find('tbody').text()).toContain('Host 1')
  })

  it('deletes a visitor after confirmation', async () => {
    guestProfileApi.getVisitorDirectory.mockResolvedValue({
      data: { items: [{ visitorDetailId: 7, fullName: 'Khách A' }], total: 1 },
    })
    employeeApi.getAll.mockResolvedValue({ data: [] })
    const wrapper = mount(GuestProfiles, { global: { stubs: { ImportModal: true, ExportModal: true } } })
    await flushPromises()

    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    guestProfileApi.deleteVisitorDirectoryItem.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Xóa').trigger('click')
    await flushPromises()
    expect(guestProfileApi.deleteVisitorDirectoryItem).toHaveBeenCalledWith(7)
    confirmSpy.mockRestore()
  })

  it('opens the access-log history for a visitor', async () => {
    guestProfileApi.getVisitorDirectory.mockResolvedValue({
      data: { items: [{ visitorDetailId: 7, fullName: 'Khách A' }], total: 1 },
    })
    employeeApi.getAll.mockResolvedValue({ data: [] })
    guestProfileApi.getVisitorAccessLogs.mockResolvedValue({
      data: { items: [{ logId: 1, timestamp: '2026-08-01T00:00:00Z', direction: 'IN', gateName: 'Cổng A', resultStatus: 'Granted' }] },
    })
    const wrapper = mount(GuestProfiles, { global: { stubs: { ImportModal: true, ExportModal: true } } })
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text() === 'Lịch sử').trigger('click')
    await flushPromises()
    expect(guestProfileApi.getVisitorAccessLogs).toHaveBeenCalledWith(7)
    expect(wrapper.text()).toContain('Cổng A')
  })
})
