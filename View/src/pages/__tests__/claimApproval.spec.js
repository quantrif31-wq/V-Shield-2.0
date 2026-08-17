import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  lostFoundApi: {
    getClaimRequests: vi.fn(),
    getLostItems: vi.fn(),
    approveClaimRequest: vi.fn(),
    rejectClaimRequest: vi.fn(),
    deleteClaimRequest: vi.fn(),
    completeClaimRequest: vi.fn(),
  },
}))

const lostFoundApi = (await import('../../services/enterpriseSecurityApi')).lostFoundApi
const ClaimApproval = (await import('../ClaimApproval.vue')).default

beforeEach(() => vi.clearAllMocks())
afterEach(() => vi.unstubAllGlobals())

describe('ClaimApproval', () => {
  it('lists claims and renders actions per status', async () => {
    lostFoundApi.getClaimRequests.mockResolvedValue({
      data: [{ claimRequestId: 1, claimantName: 'Khách A', claimantIdNumber: '123', claimantPhone: '0901', foundItem: { itemDescription: 'Ví da' }, claimantPhotoUrl: 'http://x/a', itemPhotoUrl: 'http://x/b', status: 'Pending' }],
    })
    lostFoundApi.getLostItems.mockResolvedValue({ data: { items: [] } })
    const wrapper = mount(ClaimApproval)
    await flushPromises()
    expect(wrapper.find('tbody').text()).toContain('Khách A')
    expect(wrapper.find('tbody').text()).toContain('Ví da')
    expect(wrapper.findAll('button').some((b) => b.text() === 'Duyệt')).toBe(true)
    expect(wrapper.findAll('button').some((b) => b.text() === 'Từ chối')).toBe(true)
  })

  it('approves a pending claim with an optional note', async () => {
    lostFoundApi.getClaimRequests.mockResolvedValue({
      data: [{ claimRequestId: 1, claimantName: 'Khách A', status: 'Pending' }],
    })
    lostFoundApi.getLostItems.mockResolvedValue({ data: { items: [] } })
    const wrapper = mount(ClaimApproval)
    await flushPromises()

    const promptSpy = vi.spyOn(window, 'prompt').mockReturnValue('Hồ sơ hợp lệ')
    lostFoundApi.approveClaimRequest.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Duyệt').trigger('click')
    await flushPromises()
    expect(lostFoundApi.approveClaimRequest).toHaveBeenCalledWith(1, { note: 'Hồ sơ hợp lệ' })
    promptSpy.mockRestore()
  })

  it('rejects a claim with a reason', async () => {
    lostFoundApi.getClaimRequests.mockResolvedValue({
      data: [{ claimRequestId: 1, claimantName: 'Khách A', status: 'Pending' }],
    })
    lostFoundApi.getLostItems.mockResolvedValue({ data: { items: [] } })
    const wrapper = mount(ClaimApproval)
    await flushPromises()

    const promptSpy = vi.spyOn(window, 'prompt').mockReturnValue('Thiếu giấy tờ')
    lostFoundApi.rejectClaimRequest.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Từ chối').trigger('click')
    await flushPromises()
    expect(lostFoundApi.rejectClaimRequest).toHaveBeenCalledWith(1, { reason: 'Thiếu giấy tờ' })
    promptSpy.mockRestore()
  })

  it('cancels a claim after confirmation', async () => {
    lostFoundApi.getClaimRequests.mockResolvedValue({
      data: [{ claimRequestId: 1, claimantName: 'Khách A', status: 'Pending' }],
    })
    lostFoundApi.getLostItems.mockResolvedValue({ data: { items: [] } })
    const wrapper = mount(ClaimApproval)
    await flushPromises()

    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    lostFoundApi.deleteClaimRequest.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Hủy').trigger('click')
    await flushPromises()
    expect(lostFoundApi.deleteClaimRequest).toHaveBeenCalledWith(1)
    confirmSpy.mockRestore()
  })
})
