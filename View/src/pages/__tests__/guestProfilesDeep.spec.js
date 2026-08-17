import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

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
  employeeApi.getAll.mockResolvedValue({ data: [] })
})

describe('GuestProfiles edit', () => {
  it('updates a visitor through the edit modal', async () => {
    guestProfileApi.getVisitorDirectory.mockResolvedValue({
      data: { items: [{ visitorDetailId: 7, fullName: 'Khách A', idCardNumber: '0123456789' }], total: 1 },
    })
    const wrapper = mount(GuestProfiles, { global: { stubs: { ImportModal: true, ExportModal: true } } })
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text() === 'Sửa').trigger('click')
    const nameInput = wrapper.findAll('.modal input[type="text"], .modal input').find((el) => !el.attributes('type'))
    await (nameInput || wrapper.find('.modal input')).setValue('Khách B')
    guestProfileApi.updateVisitorDirectoryItem.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Lưu thay đổi').trigger('click')
    await flushPromises()
    expect(guestProfileApi.updateVisitorDirectoryItem).toHaveBeenCalledWith(7, expect.objectContaining({ fullName: 'Khách B' }))
  })
})
