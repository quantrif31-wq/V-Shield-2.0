import { flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
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

beforeEach(() => {
  vi.clearAllMocks()
  lostFoundApi.getClaimRequests.mockResolvedValue({
    data: [{ claimRequestId: 1, claimantName: 'Khách A', status: 'Approved', returnPhotoUrl: 'http://x/return.jpg' }],
  })
  lostFoundApi.getLostItems.mockResolvedValue({ data: { items: [] } })
})
afterEach(() => {
  document.body.innerHTML = ''
})

describe('ClaimApproval completion', () => {
  it('completes a claim after recording the handover', async () => {
    const wrapper = mount(ClaimApproval)
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text().includes('Trả đồ')).trigger('click')
    await nextTick()
    const note = document.body.querySelector('.modal-panel textarea')
    note.value = 'Đã giao đồ tận tay'
    note.dispatchEvent(new Event('input'))
    await nextTick()

    lostFoundApi.completeClaimRequest.mockResolvedValue({})
    document.body.querySelector('.modal-panel .btn-primary').click()
    await flushPromises()
    expect(lostFoundApi.completeClaimRequest).toHaveBeenCalledWith(1, expect.objectContaining({ handoverNote: 'Đã giao đồ tận tay' }))
  })
})
