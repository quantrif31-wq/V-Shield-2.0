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
    updateClaimRequest: vi.fn(),
  },
}))

const lostFoundApi = (await import('../../services/enterpriseSecurityApi')).lostFoundApi
const ClaimApproval = (await import('../ClaimApproval.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  lostFoundApi.getLostItems.mockResolvedValue({ data: { items: [{ lostItemReportId: 9, reporterName: 'Báo mất', itemDescription: 'Ví da' }] } })
})
afterEach(() => {
  vi.unstubAllGlobals()
  document.body.innerHTML = ''
})

function stubFileReader(dataUrl = 'data:image/png;base64,AAA') {
  class FakeFileReader {
    onload = null
    onerror = null
    readAsDataURL() {
      this.result = dataUrl
      if (this.onload) this.onload()
    }
  }
  vi.stubGlobal('FileReader', FakeFileReader)
}

describe('ClaimApproval', () => {
  async function mountWith(claims) {
    lostFoundApi.getClaimRequests.mockResolvedValue({ data: claims })
    const wrapper = mount(ClaimApproval)
    await flushPromises()
    return wrapper
  }

  it('lists claims and renders actions per status', async () => {
    const wrapper = await mountWith([
      { claimRequestId: 1, claimantName: 'Khách A', claimantIdNumber: '123', claimantPhone: '0901', foundItem: { itemDescription: 'Ví da' }, status: 'Pending' },
      { claimRequestId: 2, claimantName: 'Khách B', status: 'Approved' },
      { claimRequestId: 3, claimantName: 'Khách C', status: 'Completed' },
      { claimRequestId: 4, claimantName: 'Khách D', status: 'Rejected' },
      { claimRequestId: 5, claimantName: 'Khách E', status: 'Cancelled' },
    ])
    expect(wrapper.find('tbody').text()).toContain('Khách A')
    expect(wrapper.find('tbody').text()).toContain('Ví da')
    const buttons = wrapper.findAll('tbody button').map((b) => b.text())
    expect(buttons).toContain('Duyệt')
    expect(buttons).toContain('Từ chối')
    expect(buttons).toContain('Trả đồ')
    expect(buttons).toContain('In biên bản')
    expect(buttons).toContain('Sửa')
    expect(buttons).toContain('Hủy')
  })

  it('shows empty state when there are no claims', async () => {
    const wrapper = await mountWith([])
    expect(wrapper.find('.empty-state').exists()).toBe(true)
  })

  it('reloads claims when the filter changes', async () => {
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'A', status: 'Pending' }])
    await wrapper.find('select.form-control').setValue('Pending')
    await flushPromises()
    expect(lostFoundApi.getClaimRequests).toHaveBeenLastCalledWith({ status: 'Pending' })
  })

  it('handles load errors gracefully', async () => {
    const err = vi.spyOn(console, 'error').mockImplementation(() => {})
    lostFoundApi.getClaimRequests.mockRejectedValue(new Error('boom'))
    const wrapper = mount(ClaimApproval)
    await flushPromises()
    expect(wrapper.find('.empty-state').exists()).toBe(true)
    err.mockRestore()
  })

  it('handles lost item option load errors', async () => {
    const err = vi.spyOn(console, 'error').mockImplementation(() => {})
    lostFoundApi.getClaimRequests.mockResolvedValue({ data: [] })
    lostFoundApi.getLostItems.mockRejectedValue(new Error('boom'))
    const wrapper = mount(ClaimApproval)
    await flushPromises()
    expect(err).toHaveBeenCalled()
    err.mockRestore()
  })

  it('approves a pending claim with an optional note', async () => {
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'Khách A', status: 'Pending' }])
    const promptSpy = vi.spyOn(window, 'prompt').mockReturnValue('Hồ sơ hợp lệ')
    lostFoundApi.approveClaimRequest.mockResolvedValue({})
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Duyệt').trigger('click')
    await flushPromises()
    expect(lostFoundApi.approveClaimRequest).toHaveBeenCalledWith(1, { note: 'Hồ sơ hợp lệ' })
    promptSpy.mockRestore()
  })

  it('approve handles API error with alert', async () => {
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'A', status: 'Pending' }])
    vi.spyOn(window, 'prompt').mockReturnValue('note')
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    lostFoundApi.approveClaimRequest.mockRejectedValue({ response: { data: { message: 'lỗi duyệt' } } })
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Duyệt').trigger('click')
    await flushPromises()
    expect(alertSpy).toHaveBeenCalled()
    alertSpy.mockRestore()
  })

  it('rejects a claim with a reason', async () => {
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'Khách A', status: 'Pending' }])
    const promptSpy = vi.spyOn(window, 'prompt').mockReturnValue('Thiếu giấy tờ')
    lostFoundApi.rejectClaimRequest.mockResolvedValue({})
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Từ chối').trigger('click')
    await flushPromises()
    expect(lostFoundApi.rejectClaimRequest).toHaveBeenCalledWith(1, { reason: 'Thiếu giấy tờ' })
    promptSpy.mockRestore()
  })

  it('reject with empty reason does not call api', async () => {
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'A', status: 'Pending' }])
    vi.spyOn(window, 'prompt').mockReturnValue('   ')
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Từ chối').trigger('click')
    await flushPromises()
    expect(lostFoundApi.rejectClaimRequest).not.toHaveBeenCalled()
  })

  it('reject handles API error with alert', async () => {
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'A', status: 'Pending' }])
    vi.spyOn(window, 'prompt').mockReturnValue('lý do')
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    lostFoundApi.rejectClaimRequest.mockRejectedValue(new Error('nope'))
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Từ chối').trigger('click')
    await flushPromises()
    expect(alertSpy).toHaveBeenCalled()
    alertSpy.mockRestore()
  })

  it('cancels a claim after confirmation', async () => {
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'Khách A', status: 'Pending' }])
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    lostFoundApi.deleteClaimRequest.mockResolvedValue({})
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Hủy').trigger('click')
    await flushPromises()
    expect(lostFoundApi.deleteClaimRequest).toHaveBeenCalledWith(1)
    confirmSpy.mockRestore()
  })

  it('cancel without confirmation does nothing', async () => {
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'A', status: 'Pending' }])
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Hủy').trigger('click')
    await flushPromises()
    expect(lostFoundApi.deleteClaimRequest).not.toHaveBeenCalled()
  })

  it('cancel handles API error with alert', async () => {
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'A', status: 'Pending' }])
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    lostFoundApi.deleteClaimRequest.mockRejectedValue({ response: { data: { message: 'x' } } })
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Hủy').trigger('click')
    await flushPromises()
    expect(alertSpy).toHaveBeenCalled()
    alertSpy.mockRestore()
  })

  it('opens the evidence modal', async () => {
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'A', status: 'Approved', claimantPhotoUrl: 'http://x/a', itemPhotoUrl: 'http://x/b', returnPhotoUrl: 'http://x/c', proofDocumentUrl: 'http://x/d', reviewNote: 'r', rejectionReason: 'why', witnessName: 'w', handoverNote: 'h' }])
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Xem hồ sơ').trigger('click')
    await nextTick()
    expect(document.body.querySelector('.evidence-meta').textContent).toContain('http://x/d')
  })

  it('opens edit modal and submits edit successfully', async () => {
    stubFileReader()
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'A', claimantIdNumber: '123', status: 'Pending', claimantPhotoUrl: 'http://x/a', itemPhotoUrl: 'http://x/b', foundItemReportId: 4 }])
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Sửa').trigger('click')
    await nextTick()

    const editInputs = document.body.querySelectorAll('.modal-panel input')
    editInputs[0].value = 'Nguyễn Văn A'
    editInputs[0].dispatchEvent(new Event('input'))
    editInputs[1].value = '0123456789'
    editInputs[1].dispatchEvent(new Event('input'))
    await nextTick()

    lostFoundApi.updateClaimRequest.mockResolvedValue({})
    document.body.querySelector('.modal-panel .btn-primary').click()
    await flushPromises()
    expect(lostFoundApi.updateClaimRequest).toHaveBeenCalledWith(1, expect.objectContaining({ claimantName: 'Nguyễn Văn A' }))
  })

  it('edit modal validates required name and id', async () => {
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'A', claimantIdNumber: '123', status: 'Pending' }])
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Sửa').trigger('click')
    await nextTick()
    const inputs = document.body.querySelectorAll('.modal-panel input')
    inputs[0].value = ''
    inputs[0].dispatchEvent(new Event('input'))
    inputs[1].value = ''
    inputs[1].dispatchEvent(new Event('input'))
    await nextTick()
    document.body.querySelector('.modal-panel .btn-primary').click()
    await flushPromises()
    expect(alertSpy).toHaveBeenCalled()
    expect(lostFoundApi.updateClaimRequest).not.toHaveBeenCalled()
    alertSpy.mockRestore()
  })

  it('edit modal validates claimant photo presence', async () => {
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'A', claimantIdNumber: '123', status: 'Pending' }])
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Sửa').trigger('click')
    await nextTick()
    const inputs = document.body.querySelectorAll('.modal-panel input')
    inputs[0].value = 'Nguyễn Văn A'
    inputs[0].dispatchEvent(new Event('input'))
    inputs[1].value = '0123'
    inputs[1].dispatchEvent(new Event('input'))
    await nextTick()
    document.body.querySelector('.modal-panel .btn-primary').click()
    await flushPromises()
    expect(alertSpy).toHaveBeenCalled()
    expect(lostFoundApi.updateClaimRequest).not.toHaveBeenCalled()
    alertSpy.mockRestore()
  })

  it('edit modal validates item photo presence', async () => {
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'A', claimantIdNumber: '123', status: 'Pending', claimantPhotoUrl: 'http://x/a' }])
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Sửa').trigger('click')
    await nextTick()
    const inputs = document.body.querySelectorAll('.modal-panel input')
    inputs[0].value = 'Nguyễn Văn A'
    inputs[0].dispatchEvent(new Event('input'))
    inputs[1].value = '0123'
    inputs[1].dispatchEvent(new Event('input'))
    await nextTick()
    document.body.querySelector('.modal-panel .btn-primary').click()
    await flushPromises()
    expect(alertSpy).toHaveBeenCalled()
    expect(lostFoundApi.updateClaimRequest).not.toHaveBeenCalled()
    alertSpy.mockRestore()
  })

  it('submit edit handles API error with alert', async () => {
    stubFileReader()
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'A', claimantIdNumber: '123', status: 'Pending', claimantPhotoUrl: 'http://x/a', itemPhotoUrl: 'http://x/b' }])
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Sửa').trigger('click')
    await nextTick()
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    lostFoundApi.updateClaimRequest.mockRejectedValue({ response: { data: { message: 'lỗi lưu' } } })
    document.body.querySelector('.modal-panel .btn-primary').click()
    await flushPromises()
    expect(alertSpy).toHaveBeenCalled()
    alertSpy.mockRestore()
  })

  it('handles edit file change for claimant and item', async () => {
    stubFileReader()
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'A', claimantIdNumber: '123', status: 'Pending' }])
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Sửa').trigger('click')
    await nextTick()
    const fileInputs = document.body.querySelectorAll('.modal-panel input[type="file"]')
    const file = new File(['x'], 'x.png', { type: 'image/png' })
    Object.defineProperty(fileInputs[0], 'files', { value: [file] })
    fileInputs[0].dispatchEvent(new Event('change'))
    await flushPromises()
    Object.defineProperty(fileInputs[1], 'files', { value: [file] })
    fileInputs[1].dispatchEvent(new Event('change'))
    await flushPromises()
    expect(wrapper.vm.editForm.claimantPhotoBase64).toBeTruthy()
    expect(wrapper.vm.editForm.itemPhotoBase64).toBeTruthy()
  })

  it('edit file change with no file does nothing', async () => {
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'A', claimantIdNumber: '123', status: 'Pending' }])
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Sửa').trigger('click')
    await nextTick()
    const fileInputs = document.body.querySelectorAll('.modal-panel input[type="file"]')
    Object.defineProperty(fileInputs[0], 'files', { value: [] })
    fileInputs[0].dispatchEvent(new Event('change'))
    await flushPromises()
    expect(wrapper.vm.editForm.claimantPhotoBase64).toBeFalsy()
  })

  it('handles complete file change for claimant and return', async () => {
    stubFileReader()
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'A', status: 'Approved' }])
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Trả đồ').trigger('click')
    await nextTick()
    const fileInputs = document.body.querySelectorAll('.modal-panel input[type="file"]')
    const file = new File(['x'], 'x.png', { type: 'image/png' })
    Object.defineProperty(fileInputs[0], 'files', { value: [file] })
    fileInputs[0].dispatchEvent(new Event('change'))
    await flushPromises()
    Object.defineProperty(fileInputs[1], 'files', { value: [file] })
    fileInputs[1].dispatchEvent(new Event('change'))
    await flushPromises()
    expect(wrapper.vm.completeForm.claimantPhotoBase64).toBeTruthy()
    expect(wrapper.vm.completeForm.returnPhotoBase64).toBeTruthy()
  })

  it('complete validates handover note and return photo', async () => {
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'A', status: 'Approved' }])
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Trả đồ').trigger('click')
    await nextTick()
    document.body.querySelector('.modal-panel .btn-primary').click()
    await flushPromises()
    expect(alertSpy).toHaveBeenCalled()
    alertSpy.mockRestore()
  })

  it('complete validates return photo after note provided', async () => {
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'A', status: 'Approved' }])
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Trả đồ').trigger('click')
    await nextTick()
    const textarea = document.body.querySelector('.modal-panel textarea')
    textarea.value = 'Bàn giao đầy đủ'
    textarea.dispatchEvent(new Event('input'))
    await nextTick()
    document.body.querySelector('.modal-panel .btn-primary').click()
    await flushPromises()
    expect(alertSpy).toHaveBeenCalled()
    expect(lostFoundApi.completeClaimRequest).not.toHaveBeenCalled()
    alertSpy.mockRestore()
  })

  it('complete handles API error with alert', async () => {
    stubFileReader()
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'A', status: 'Approved' }])
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Trả đồ').trigger('click')
    await nextTick()
    const textarea = document.body.querySelector('.modal-panel textarea')
    textarea.value = 'Bàn giao đầy đủ'
    textarea.dispatchEvent(new Event('input'))
    await nextTick()
    const fileInputs = document.body.querySelectorAll('.modal-panel input[type="file"]')
    const file = new File(['x'], 'x.png', { type: 'image/png' })
    Object.defineProperty(fileInputs[1], 'files', { value: [file] })
    fileInputs[1].dispatchEvent(new Event('change'))
    await flushPromises()
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    lostFoundApi.completeClaimRequest.mockRejectedValue({ response: { data: { message: 'lỗi' } } })
    document.body.querySelector('.modal-panel .btn-primary').click()
    await flushPromises()
    expect(alertSpy).toHaveBeenCalled()
    alertSpy.mockRestore()
  })

  it('completes a claim after recording the handover', async () => {
    stubFileReader()
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'A', status: 'Approved' }])
    await wrapper.findAll('tbody button').find((b) => b.text() === 'Trả đồ').trigger('click')
    await nextTick()
    const textarea = document.body.querySelector('.modal-panel textarea')
    textarea.value = 'Đã giao đồ tận tay'
    textarea.dispatchEvent(new Event('input'))
    await nextTick()
    const fileInputs = document.body.querySelectorAll('.modal-panel input[type="file"]')
    const file = new File(['x'], 'x.png', { type: 'image/png' })
    Object.defineProperty(fileInputs[1], 'files', { value: [file] })
    fileInputs[1].dispatchEvent(new Event('change'))
    await flushPromises()
    lostFoundApi.completeClaimRequest.mockResolvedValue({})
    document.body.querySelector('.modal-panel .btn-primary').click()
    await flushPromises()
    expect(lostFoundApi.completeClaimRequest).toHaveBeenCalledWith(1, expect.objectContaining({ handoverNote: 'Đã giao đồ tận tay' }))
  })

  it('printReceipt opens a print window', async () => {
    const printWindow = { document: { open: vi.fn(), write: vi.fn(), close: vi.fn() }, focus: vi.fn(), print: vi.fn() }
    vi.spyOn(window, 'open').mockReturnValue(printWindow)
    vi.useFakeTimers()
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'Khách A', claimantIdNumber: '123', claimantPhone: '090', status: 'Completed', completedAtUtc: '2026-08-01T00:00:00Z', handoverNote: 'note', witnessName: 'w', proofDocumentUrl: 'p', foundItem: { itemDescription: 'Ví', foundAtUtc: '2026-07-01', foundLocation: 'loc', storageLocation: 'sto' }, returnPhotoUrl: 'http://x/ret' }])
    await wrapper.findAll('tbody button').find((b) => b.text() === 'In biên bản').trigger('click')
    expect(window.open).toHaveBeenCalled()
    expect(printWindow.document.write).toHaveBeenCalled()
    vi.runAllTimers()
    expect(printWindow.print).toHaveBeenCalled()
    vi.useRealTimers()
  })

  it('printReceipt alerts when window cannot be opened', async () => {
    vi.spyOn(window, 'open').mockReturnValue(null)
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    const wrapper = await mountWith([{ claimRequestId: 1, claimantName: 'A', status: 'Completed' }])
    await wrapper.findAll('tbody button').find((b) => b.text() === 'In biên bản').trigger('click')
    expect(alertSpy).toHaveBeenCalled()
    alertSpy.mockRestore()
  })
})
