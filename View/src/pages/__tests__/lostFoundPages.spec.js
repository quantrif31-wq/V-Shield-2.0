import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  lostFoundApi: {
    getFoundItems: vi.fn(),
    getAvailableCompartments: vi.fn(),
    getLostItems: vi.fn(),
    createFoundItem: vi.fn(),
    updateFoundItem: vi.fn(),
    deleteFoundItem: vi.fn(),
    createClaimRequest: vi.fn(),
    closeLostItem: vi.fn(),
    deleteLostItem: vi.fn(),
  },
}))

const lostFoundApi = (await import('../../services/enterpriseSecurityApi')).lostFoundApi

const FoundItemRegistry = (await import('../FoundItemRegistry.vue')).default
const LostItemList = (await import('../LostItemList.vue')).default

beforeEach(() => vi.clearAllMocks())

describe('FoundItemRegistry', () => {
  it('lists found items with status badges', async () => {
    lostFoundApi.getFoundItems.mockResolvedValue({
      data: { items: [{ foundItemReportId: 1, foundByName: 'Bảo vệ A', foundByIdNumber: '123', itemDescription: 'Ví da', foundLocation: 'Sảnh', status: 'Unclaimed', storageLocation: 'Tủ A' }], total: 1 },
    })
    lostFoundApi.getAvailableCompartments.mockResolvedValue({ data: [] })
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    expect(wrapper.find('tbody').text()).toContain('Bảo vệ A')
    expect(wrapper.find('tbody').text()).toContain('Ví da')
    expect(wrapper.find('tbody').text()).toContain('Chưa trả')
  })

  it('deletes a found item after confirmation', async () => {
    lostFoundApi.getFoundItems.mockResolvedValue({
      data: { items: [{ foundItemReportId: 1, foundByName: 'A', itemDescription: 'Ví', status: 'Unclaimed' }], total: 1 },
    })
    lostFoundApi.getAvailableCompartments.mockResolvedValue({ data: [] })
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()

    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    lostFoundApi.deleteFoundItem.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Xóa').trigger('click')
    await flushPromises()
    expect(lostFoundApi.deleteFoundItem).toHaveBeenCalledWith(1)
    confirmSpy.mockRestore()
  })

  it('refetches when the status filter changes', async () => {
    lostFoundApi.getFoundItems.mockResolvedValue({ data: { items: [], total: 0 } })
    lostFoundApi.getAvailableCompartments.mockResolvedValue({ data: [] })
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('select')[0].setValue('ClaimPending')
    await flushPromises()
    expect(lostFoundApi.getFoundItems).toHaveBeenLastCalledWith({ status: 'ClaimPending', page: 1, pageSize: 100 })
  })
})

describe('LostItemList', () => {
  it('lists lost item reports', async () => {
    lostFoundApi.getLostItems.mockResolvedValue({
      data: { items: [{ lostItemReportId: 1, reporterName: 'Nhân viên B', reporterPhone: '0901', itemDescription: 'Điện thoại', lostAtUtc: '2026-08-01T00:00:00Z', status: 'Pending' }], total: 1 },
    })
    const wrapper = mount(LostItemList)
    await flushPromises()
    expect(wrapper.find('tbody').text()).toContain('Nhân viên B')
    expect(wrapper.find('tbody').text()).toContain('Điện thoại')
    expect(wrapper.find('tbody').text()).toContain('Chờ xử lý')
  })

  it('closes and deletes a report after confirmation', async () => {
    lostFoundApi.getLostItems.mockResolvedValue({
      data: { items: [{ lostItemReportId: 1, reporterName: 'A', itemDescription: 'Điện thoại', status: 'Pending' }], total: 1 },
    })
    const wrapper = mount(LostItemList)
    await flushPromises()

    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    lostFoundApi.closeLostItem.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Đóng').trigger('click')
    await flushPromises()
    expect(lostFoundApi.closeLostItem).toHaveBeenCalledWith(1)

    lostFoundApi.deleteLostItem.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Xóa').trigger('click')
    await flushPromises()
    expect(lostFoundApi.deleteLostItem).toHaveBeenCalledWith(1)
    confirmSpy.mockRestore()
  })
})
