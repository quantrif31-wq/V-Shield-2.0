import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  lostFoundApi: {
    getFoundItems: vi.fn(),
    getAvailableCompartments: vi.fn(),
    getLostItems: vi.fn(),
    createFoundItem: vi.fn(),
    updateFoundItem: vi.fn(),
    deleteFoundItem: vi.fn(),
    createClaimRequest: vi.fn(),
    createLostItem: vi.fn(),
    updateLostItem: vi.fn(),
    closeLostItem: vi.fn(),
    deleteLostItem: vi.fn(),
  },
}))

const lostFoundApi = (await import('../../services/enterpriseSecurityApi')).lostFoundApi

const FoundItemRegistry = (await import('../FoundItemRegistry.vue')).default
const LostItemList = (await import('../LostItemList.vue')).default

beforeEach(() => vi.clearAllMocks())

const flushMacro = () => new Promise((resolve) => setTimeout(resolve, 20))

function fireFileChange(fileInput, file) {
  Object.defineProperty(fileInput, 'files', { value: file ? [file] : [], configurable: true })
  fileInput.dispatchEvent(new Event('change', { bubbles: true }))
}

afterEach(() => {
  document.body.innerHTML = ''
})

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

describe('FoundItemRegistry deep', () => {
  const foundItem = {
    foundItemReportId: 1,
    foundByName: 'Bảo vệ A',
    foundByIdNumber: '123',
    foundByPhone: '090111',
    foundLocation: 'Sảnh',
    itemDescription: 'Ví da',
    storageLocation: 'Kho 1',
    status: 'Unclaimed',
    finderPhotoUrl: 'f.png',
    photoUrl: 'i.png',
  }

  function withItems(items = [foundItem], comps = []) {
    lostFoundApi.getFoundItems.mockResolvedValue({ data: { items, total: items.length } })
    lostFoundApi.getAvailableCompartments.mockResolvedValue({ data: comps })
    lostFoundApi.getLostItems.mockResolvedValue({ data: { items: [], total: 0 } })
  }

  it('handles loadItems api error', async () => {
    lostFoundApi.getFoundItems.mockRejectedValue(new Error('boom'))
    lostFoundApi.getAvailableCompartments.mockResolvedValue({ data: [] })
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    expect(wrapper.find('.empty-state').text()).toContain('Chưa có dữ liệu')
    wrapper.unmount()
  })

  it('handles loadCompartments api error', async () => {
    lostFoundApi.getFoundItems.mockResolvedValue({ data: { items: [], total: 0 } })
    lostFoundApi.getAvailableCompartments.mockRejectedValue(new Error('boom'))
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    wrapper.unmount()
  })

  it('shows storage labels with locker compartments', async () => {
    withItems([{ ...foundItem, storageLocation: '' }], [])
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    wrapper.unmount()
  })

  it('opens create, resets and validates required fields', async () => {
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    withItems()
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Nhận đồ')).trigger('click')
    expect(document.body.querySelector('.modal-panel')).toBeTruthy()
    await wrapper.vm.submit()
    expect(alertSpy).toHaveBeenCalled()
    alertSpy.mockRestore()
    wrapper.unmount()
  })

  it('opens create, fills fields and creates a found item', async () => {
    withItems()
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Nhận đồ')).trigger('click')

    wrapper.vm.form.foundByName = 'Bảo vệ B'
    wrapper.vm.form.foundByPhone = '0999'
    wrapper.vm.form.foundByIdNumber = '456'
    wrapper.vm.form.foundLocation = 'Cổng 2'
    wrapper.vm.form.itemDescription = 'Chìa khóa'
    wrapper.vm.form.finderPhotoUrl = 'f.png'
    wrapper.vm.form.itemPhotoUrl = 'i.png'
    lostFoundApi.createFoundItem.mockResolvedValue({})
    await wrapper.vm.submit()
    await flushPromises()
    expect(lostFoundApi.createFoundItem).toHaveBeenCalled()
    expect(document.body.querySelector('.modal-panel')).toBeNull()
    wrapper.unmount()
  })

  it('requires a finder photo before submitting', async () => {
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    withItems()
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Nhận đồ')).trigger('click')
    wrapper.vm.form.foundByName = 'A'; wrapper.vm.form.foundByPhone = '1'
    wrapper.vm.form.foundByIdNumber = '2'; wrapper.vm.form.foundLocation = '3'
    wrapper.vm.form.itemDescription = '4'
    wrapper.vm.form.itemPhotoUrl = 'i.png'
    await wrapper.vm.submit()
    expect(alertSpy).toHaveBeenCalledWith('Cần có ảnh người nhặt được.')
    alertSpy.mockRestore()
    wrapper.unmount()
  })

  it('requires an item photo before submitting', async () => {
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    withItems()
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Nhận đồ')).trigger('click')
    wrapper.vm.form.foundByName = 'A'; wrapper.vm.form.foundByPhone = '1'
    wrapper.vm.form.foundByIdNumber = '2'; wrapper.vm.form.foundLocation = '3'
    wrapper.vm.form.itemDescription = '4'
    wrapper.vm.form.finderPhotoUrl = 'f.png'
    await wrapper.vm.submit()
    expect(alertSpy).toHaveBeenCalledWith('Cần có ảnh đồ vật.')
    alertSpy.mockRestore()
    wrapper.unmount()
  })

  it('updates an existing found item in edit mode', async () => {
    withItems([{ ...foundItem, storageLocation: '', lockerCompartment: { cabinet: { name: 'Tủ A' }, code: 'B2' } }])
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Sửa').trigger('click')
    expect(wrapper.vm.editing.foundItemReportId).toBe(1)
    wrapper.vm.form.finderPhotoUrl = 'f.png'
    wrapper.vm.form.itemPhotoUrl = 'i.png'
    lostFoundApi.updateFoundItem.mockResolvedValue({})
    await wrapper.vm.submit()
    await flushPromises()
    expect(lostFoundApi.updateFoundItem).toHaveBeenCalled()
    wrapper.unmount()
  })

  it('handles create submission api error', async () => {
    withItems()
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Nhận đồ')).trigger('click')
    wrapper.vm.form.foundByName = 'A'; wrapper.vm.form.foundByPhone = '1'
    wrapper.vm.form.foundByIdNumber = '2'; wrapper.vm.form.foundLocation = '3'
    wrapper.vm.form.itemDescription = '4'
    wrapper.vm.form.finderPhotoUrl = 'f.png'; wrapper.vm.form.itemPhotoUrl = 'i.png'
    lostFoundApi.createFoundItem.mockRejectedValue(new Error('fail'))
    await wrapper.vm.submit()
    expect(alertSpy).toHaveBeenCalledWith('Lỗi: fail')
    alertSpy.mockRestore()
    wrapper.unmount()
  })

  it('opens claim form with lost item options and submits a claim', async () => {
    withItems()
    lostFoundApi.getLostItems.mockResolvedValue({
      data: { items: [{ lostItemReportId: 9, reporterName: 'Chủ', itemDescription: 'Điện thoại' }], total: 1 },
    })
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Tạo yêu cầu').trigger('click')
    await flushPromises()
    expect(document.body.querySelector('.modal-panel')).toBeTruthy()
    wrapper.vm.claimForm.claimantName = 'Nguyễn'
    wrapper.vm.claimForm.claimantIdNumber = '111'
    wrapper.vm.claimForm.claimantPhotoUrl = 'c.png'
    wrapper.vm.claimForm.itemPhotoUrl = 'x.png'
    lostFoundApi.createClaimRequest.mockResolvedValue({})
    await wrapper.vm.submitClaim()
    await flushPromises()
    expect(lostFoundApi.createClaimRequest).toHaveBeenCalled()
    wrapper.unmount()
  })

  it('validates claim required fields and photos', async () => {
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    withItems()
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Tạo yêu cầu').trigger('click')
    await flushPromises()
    await wrapper.vm.submitClaim()
    expect(alertSpy).toHaveBeenCalledWith('Cần nhập tên và CCCD/CMND người nhận.')

    wrapper.vm.claimForm.claimantName = 'A'
    wrapper.vm.claimForm.claimantIdNumber = '2'
    await wrapper.vm.submitClaim()
    expect(alertSpy).toHaveBeenCalledWith('Cần có ảnh người nhận.')

    wrapper.vm.claimForm.claimantPhotoUrl = 'c.png'
    await wrapper.vm.submitClaim()
    expect(alertSpy).toHaveBeenCalledWith('Cần có ảnh đối chiếu với đồ vật.')
    alertSpy.mockRestore()
    wrapper.unmount()
  })

  it('handles claim submission api error', async () => {
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    withItems()
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Tạo yêu cầu').trigger('click')
    await flushPromises()
    wrapper.vm.claimForm.claimantName = 'A'
    wrapper.vm.claimForm.claimantIdNumber = '2'
    wrapper.vm.claimForm.claimantPhotoUrl = 'c.png'
    wrapper.vm.claimForm.itemPhotoUrl = 'x.png'
    lostFoundApi.createClaimRequest.mockRejectedValue(new Error('claim fail'))
    await wrapper.vm.submitClaim()
    expect(alertSpy).toHaveBeenCalledWith('Lỗi: claim fail')
    alertSpy.mockRestore()
    wrapper.unmount()
  })

  it('skips claim submit with no target', async () => {
    withItems()
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    wrapper.vm.claimTarget = null
    await wrapper.vm.submitClaim()
    wrapper.unmount()
  })

  it('opens evidence modal and shows photos', async () => {
    withItems()
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Xem hồ sơ').trigger('click')
    expect(wrapper.vm.evidencePreview.finderPhotoUrl).toBe('f.png')
    expect(wrapper.vm.evidencePreview.itemPhotoUrl).toBe('i.png')
    wrapper.unmount()
  })

  it('reads finder and item photos from file inputs', async () => {
    withItems()
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Nhận đồ')).trigger('click')
    await flushPromises()

    const fileInputs = document.body.querySelectorAll('input[type="file"]')
    const file = new File(['abc'], 'a.png', { type: 'image/png' })
    fireFileChange(fileInputs[0], file)
    await flushMacro()
    expect(wrapper.vm.form.finderPhotoBase64).toBeTruthy()

    fireFileChange(fileInputs[1], file)
    await flushMacro()
    expect(wrapper.vm.form.itemPhotoBase64).toBeTruthy()

    fireFileChange(fileInputs[0], null)
    await flushMacro()
    wrapper.unmount()
  })

  it('reads claimant and item photos from claim file inputs', async () => {
    withItems()
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Tạo yêu cầu').trigger('click')
    await flushPromises()

    const fileInputs = document.body.querySelectorAll('input[type="file"]')
    const file = new File(['abc'], 'b.png', { type: 'image/png' })
    fireFileChange(fileInputs[0], file)
    await flushMacro()
    expect(wrapper.vm.claimForm.claimantPhotoBase64).toBeTruthy()

    fireFileChange(fileInputs[1], file)
    await flushMacro()
    expect(wrapper.vm.claimForm.itemPhotoBase64).toBeTruthy()
    wrapper.unmount()
  })

  it('renders claim photo previews', async () => {
    withItems()
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Tạo yêu cầu').trigger('click')
    await flushPromises()
    wrapper.vm.claimForm.claimantPhotoPreview = 'data:image/png;base64,xx'
    wrapper.vm.claimForm.itemPhotoPreview = 'data:image/png;base64,yy'
    await flushPromises()
    expect(document.body.querySelectorAll('.photo-preview').length).toBe(2)
    wrapper.unmount()
  })

  it('deletes only after confirmation and handles delete error', async () => {
    withItems()
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false)
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Xóa').trigger('click')
    expect(lostFoundApi.deleteFoundItem).not.toHaveBeenCalled()

    confirmSpy.mockReturnValue(true)
    lostFoundApi.deleteFoundItem.mockRejectedValue(new Error('del fail'))
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    await wrapper.findAll('button').find((b) => b.text() === 'Xóa').trigger('click')
    await flushPromises()
    expect(alertSpy).toHaveBeenCalledWith('Lỗi: del fail')
    confirmSpy.mockRestore()
    alertSpy.mockRestore()
    wrapper.unmount()
  })

  it('maps status classes and labels', async () => {
    withItems([{ ...foundItem, status: 'ClaimPending' }], [])
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    withItems([{ ...foundItem, status: 'Returned' }], [])
    wrapper.vm.items = [{ status: 'Returned' }, { status: 'Unknown' }]
    await flushPromises()
    expect(wrapper.vm.statusLabel('Unknown')).toBe('Unknown')
    expect(wrapper.vm.statusClass('Unknown')).toBe('secondary')
    wrapper.unmount()
  })

  it('renders a full photo profile badge', async () => {
    withItems()
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    expect(wrapper.find('.badge-success').exists()).toBe(true)
    wrapper.unmount()
  })

  it('renders missing photo badge when photos absent and shows empty row', async () => {
    withItems([{ ...foundItem, finderPhotoUrl: '', photoUrl: '' }])
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    expect(wrapper.find('.badge-warning').exists()).toBe(true)
    wrapper.unmount()
  })

  it('handles loadLostItemOptions api error when opening claim', async () => {
    withItems()
    lostFoundApi.getLostItems.mockRejectedValue(new Error('lost fail'))
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Tạo yêu cầu').trigger('click')
    await flushPromises()
    expect(wrapper.vm.lostItemOptions).toEqual([])
    wrapper.unmount()
  })

  it('returns early when claim file input has no file', async () => {
    withItems()
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Tạo yêu cầu').trigger('click')
    await flushPromises()
    const fileInputs = document.body.querySelectorAll('input[type="file"]')
    fireFileChange(fileInputs[0], null)
    await flushMacro()
    expect(wrapper.vm.claimForm.claimantPhotoBase64).toBeNull()
    wrapper.unmount()
  })
})

describe('LostItemList deep', () => {
  const lostItem = {
    lostItemReportId: 1,
    reporterName: 'Nhân viên B',
    reporterPhone: '0901',
    reporterIdNumber: '1234',
    reporterEmail: 'a@b.c',
    itemDescription: 'Điện thoại',
    lastSeenLocation: 'Căng tin',
    lostAtUtc: '2026-08-01T00:00:00Z',
    status: 'Pending',
    reporterPhotoUrl: 'r.png',
    photoUrl: 'i.png',
  }

  function withItems(items = [lostItem]) {
    lostFoundApi.getLostItems.mockResolvedValue({ data: { items, total: items.length } })
  }

  it('handles loadItems api error', async () => {
    lostFoundApi.getLostItems.mockRejectedValue(new Error('boom'))
    const wrapper = mount(LostItemList)
    await flushPromises()
    expect(wrapper.find('.empty-state').text()).toContain('Chưa có dữ liệu')
    wrapper.unmount()
  })

  it('opens create, validates, fills and creates a lost item', async () => {
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    withItems()
    const wrapper = mount(LostItemList)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Báo mất')).trigger('click')
    await wrapper.vm.submit()
    expect(alertSpy).toHaveBeenCalledWith('Vui lòng nhập đầy đủ thông tin bắt buộc.')

    wrapper.vm.form.reporterName = 'A'
    wrapper.vm.form.reporterPhone = '1'
    wrapper.vm.form.reporterIdNumber = '2'
    wrapper.vm.form.itemDescription = '3'
    wrapper.vm.form.lostAtUtc = '2026-08-01T10:00'
    await wrapper.vm.submit()
    expect(alertSpy).toHaveBeenCalledWith('Cần có ảnh người báo.')

    wrapper.vm.form.reporterPhotoUrl = 'r.png'
    await wrapper.vm.submit()
    expect(alertSpy).toHaveBeenCalledWith('Cần có ảnh đồ vật.')

    wrapper.vm.form.itemPhotoUrl = 'i.png'
    lostFoundApi.createLostItem.mockResolvedValue({})
    await wrapper.vm.submit()
    await flushPromises()
    expect(lostFoundApi.createLostItem).toHaveBeenCalled()
    alertSpy.mockRestore()
    wrapper.unmount()
  })

  it('edits an existing lost item and submits update', async () => {
    withItems()
    const wrapper = mount(LostItemList)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Sửa').trigger('click')
    expect(wrapper.vm.editing.lostItemReportId).toBe(1)
    wrapper.vm.form.reporterPhotoUrl = 'r.png'
    wrapper.vm.form.itemPhotoUrl = 'i.png'
    lostFoundApi.updateLostItem.mockResolvedValue({})
    await wrapper.vm.submit()
    await flushPromises()
    expect(lostFoundApi.updateLostItem).toHaveBeenCalled()
    wrapper.unmount()
  })

  it('handles create submission api error', async () => {
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    withItems()
    const wrapper = mount(LostItemList)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Báo mất')).trigger('click')
    wrapper.vm.form.reporterName = 'A'; wrapper.vm.form.reporterPhone = '1'
    wrapper.vm.form.reporterIdNumber = '2'; wrapper.vm.form.itemDescription = '3'
    wrapper.vm.form.lostAtUtc = '2026-08-01T10:00'
    wrapper.vm.form.reporterPhotoUrl = 'r.png'; wrapper.vm.form.itemPhotoUrl = 'i.png'
    lostFoundApi.createLostItem.mockRejectedValue(new Error('create fail'))
    await wrapper.vm.submit()
    expect(alertSpy).toHaveBeenCalledWith('Lỗi: create fail')
    alertSpy.mockRestore()
    wrapper.unmount()
  })

  it('reads reporter and item photos from file inputs', async () => {
    withItems()
    const wrapper = mount(LostItemList)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Báo mất')).trigger('click')
    await flushPromises()

    const fileInputs = document.body.querySelectorAll('input[type="file"]')
    const file = new File(['abc'], 'a.png', { type: 'image/png' })
    fireFileChange(fileInputs[0], file)
    await flushMacro()
    expect(wrapper.vm.form.reporterPhotoBase64).toBeTruthy()

    fireFileChange(fileInputs[1], file)
    await flushMacro()
    expect(wrapper.vm.form.itemPhotoBase64).toBeTruthy()
    wrapper.unmount()
  })

  it('opens evidence modal and shows photos', async () => {
    withItems()
    const wrapper = mount(LostItemList)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Xem hồ sơ').trigger('click')
    expect(wrapper.vm.evidencePreview.reporterPhotoUrl).toBe('r.png')
    expect(wrapper.vm.evidencePreview.itemPhotoUrl).toBe('i.png')
    wrapper.unmount()
  })

  it('closes only after confirmation and handles close error', async () => {
    withItems()
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false)
    const wrapper = mount(LostItemList)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Đóng').trigger('click')
    expect(lostFoundApi.closeLostItem).not.toHaveBeenCalled()

    confirmSpy.mockReturnValue(true)
    lostFoundApi.closeLostItem.mockRejectedValue(new Error('close fail'))
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    await wrapper.findAll('button').find((b) => b.text() === 'Đóng').trigger('click')
    await flushPromises()
    expect(alertSpy).toHaveBeenCalledWith('Lỗi: close fail')
    confirmSpy.mockRestore()
    alertSpy.mockRestore()
    wrapper.unmount()
  })

  it('handles delete error and confirmation decline', async () => {
    withItems()
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(false)
    const wrapper = mount(LostItemList)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Xóa').trigger('click')
    expect(lostFoundApi.deleteLostItem).not.toHaveBeenCalled()

    confirmSpy.mockReturnValue(true)
    lostFoundApi.deleteLostItem.mockRejectedValue(new Error('del fail'))
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {})
    await wrapper.findAll('button').find((b) => b.text() === 'Xóa').trigger('click')
    await flushPromises()
    expect(alertSpy).toHaveBeenCalledWith('Lỗi: del fail')
    confirmSpy.mockRestore()
    alertSpy.mockRestore()
    wrapper.unmount()
  })

  it('maps statuses, classes and dates', async () => {
    withItems()
    const wrapper = mount(LostItemList)
    await flushPromises()
    expect(wrapper.vm.statusLabel('MatchFound')).toBe('Đã ghép')
    expect(wrapper.vm.statusLabel('Claimed')).toBe('Đã nhận lại')
    expect(wrapper.vm.statusLabel('Closed')).toBe('Đã đóng')
    expect(wrapper.vm.statusClass('MatchFound')).toBe('primary')
    expect(wrapper.vm.statusClass('Claimed')).toBe('success')
    expect(wrapper.vm.statusClass('Closed')).toBe('secondary')
    expect(wrapper.vm.statusClass('Unknown')).toBe('secondary')
    expect(wrapper.vm.statusLabel('Unknown')).toBe('Unknown')
    expect(wrapper.vm.formatDate('2026-08-01T00:00:00Z')).toBeTruthy()
    wrapper.unmount()
  })

  it('renders full profile badge, missing photos and empty row', async () => {
    withItems([{ ...lostItem, reporterPhotoUrl: '', photoUrl: '' }])
    const wrapper = mount(LostItemList)
    await flushPromises()
    expect(wrapper.find('.badge-warning').exists()).toBe(true)

    lostFoundApi.getLostItems.mockResolvedValue({ data: { items: [], total: 0 } })
    await wrapper.vm.loadItems()
    await flushPromises()
    expect(wrapper.find('.empty-state').text()).toContain('Chưa có dữ liệu')
    expect(wrapper.vm.formatDate('')).toBe('')
    wrapper.unmount()
  })

  it('refetches when filter changes', async () => {
    withItems()
    const wrapper = mount(LostItemList)
    await flushPromises()
    await wrapper.findAll('select')[0].setValue('Closed')
    await flushPromises()
    expect(lostFoundApi.getLostItems).toHaveBeenLastCalledWith({ status: 'Closed', page: 1, pageSize: 100 })
    wrapper.unmount()
  })

  it('converts lostAtUtc to datetime-local in edit', async () => {
    withItems()
    const wrapper = mount(LostItemList)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Sửa').trigger('click')
    expect(wrapper.vm.form.lostAtUtc).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/)
    wrapper.unmount()
  })
})

describe('FoundItemRegistry template controls', () => {
  const comps = [
    { lockerCompartmentId: 5, code: 'B2', cabinet: { name: 'Tủ A' } },
    { lockerCompartmentId: 6, code: 'C1', lockerCabinetId: 7 },
  ]

  function wire(items = [{ foundItemReportId: 1, foundByName: 'A', status: 'Unclaimed' }]) {
    lostFoundApi.getFoundItems.mockResolvedValue({ data: { items, total: items.length } })
    lostFoundApi.getAvailableCompartments.mockResolvedValue({ data: comps })
    lostFoundApi.getLostItems.mockResolvedValue({ data: { items: [], total: 0 } })
  }

  function setValue(el, value) {
    el.value = value
    el.dispatchEvent(new Event('input', { bubbles: true }))
  }

  it('interacts with create form inputs and select via template', async () => {
    wire()
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Nhận đồ')).trigger('click')
    await flushPromises()

    const panel = document.body.querySelector('.modal-panel')
    const inputs = panel.querySelectorAll('input')
    const ta = panel.querySelector('textarea')
    const sel = panel.querySelector('select')
    setValue(inputs[0], 'Bảo vệ X')
    setValue(inputs[2], '0900')
    setValue(inputs[3], 'Cổng 3')
    setValue(ta, 'Mô tả ABC')
    setValue(inputs[4], 'Kho 2')
    sel.value = '5'
    sel.dispatchEvent(new Event('change', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.form.foundByName).toBe('Bảo vệ X')
    expect(wrapper.vm.form.lockerCompartmentId).toBe(5)
    expect(wrapper.vm.form.storageLocation).toBe('Kho 2')

    const huys = [...panel.querySelectorAll('button')].filter((b) => b.textContent.trim() === 'Hủy')
    huys[0].dispatchEvent(new Event('click', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.showForm).toBe(false)
    wrapper.unmount()
  })

  it('closes create form via overlay self click and covers compartment fallback', async () => {
    lostFoundApi.getFoundItems.mockResolvedValue({ data: { items: [], total: 0 } })
    lostFoundApi.getAvailableCompartments.mockResolvedValue({ data: comps })
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Nhận đồ')).trigger('click')
    await flushPromises()
    const overlay = document.body.querySelector('.modal-overlay')
    overlay.dispatchEvent(new Event('click', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.showForm).toBe(false)
    wrapper.unmount()
  })

  it('sets remaining create inputs and cancels via Hủy', async () => {
    wire()
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Nhận đồ')).trigger('click')
    await flushPromises()
    const inputs = document.body.querySelector('.modal-panel').querySelectorAll('input')
    setValue(inputs[1], '111')
    setValue(inputs[3], 'Sảnh')
    await flushPromises()
    const huys = [...document.body.querySelectorAll('.modal-panel button')].filter((b) => b.textContent.trim() === 'Hủy')
    huys[0].dispatchEvent(new Event('click', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.showForm).toBe(false)
    wrapper.unmount()
  })

  it('interacts with claim form inputs and closes via Hủy', async () => {
    wire([{ foundItemReportId: 1, foundByName: 'A', status: 'Unclaimed' }])
    lostFoundApi.getLostItems.mockResolvedValue({ data: { items: [{ lostItemReportId: 9, reporterName: 'Ch', itemDescription: 'ĐT' }], total: 1 } })
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Tạo yêu cầu').trigger('click')
    await flushPromises()

    const panel = document.body.querySelector('.modal-panel')
    const inputs = panel.querySelectorAll('input')
    const select = panel.querySelector('select')
    setValue(inputs[0], 'Người nhận')
    setValue(inputs[1], '999')
    setValue(inputs[2], '090')
    select.value = '9'
    select.dispatchEvent(new Event('change', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.claimForm.claimantName).toBe('Người nhận')
    expect(wrapper.vm.claimForm.lostItemReportId).toBe(9)

    const huys = [...panel.querySelectorAll('button')].filter((b) => b.textContent.trim() === 'Hủy')
    huys[0].dispatchEvent(new Event('click', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.showClaimForm).toBe(false)
    wrapper.unmount()
  })

  it('sets claim proof url and closes claim form via overlay', async () => {
    wire([{ foundItemReportId: 1, foundByName: 'A', status: 'Unclaimed' }])
    lostFoundApi.getLostItems.mockResolvedValue({ data: { items: [], total: 0 } })
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Tạo yêu cầu').trigger('click')
    await flushPromises()
    const inputs = document.body.querySelector('.modal-panel').querySelectorAll('input')
    setValue(inputs[3], 'http://proof')
    await flushPromises()
    expect(wrapper.vm.claimForm.proofDocumentUrl).toBe('http://proof')
    const overlay = document.body.querySelector('.modal-overlay')
    overlay.dispatchEvent(new Event('click', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.showClaimForm).toBe(false)
    wrapper.unmount()
  })

  it('closes evidence modal via Đóng button and overlay', async () => {
    wire([{ foundItemReportId: 1, foundByName: 'A', status: 'Unclaimed', finderPhotoUrl: 'f', photoUrl: 'i' }])
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Xem hồ sơ').trigger('click')
    await flushPromises()
    const closeBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.trim() === 'Đóng')
    closeBtn.dispatchEvent(new Event('click', { bubbles: true }))
    expect(wrapper.vm.showEvidenceModal).toBe(false)
    wrapper.unmount()
  })

  it('closes evidence modal via overlay self click', async () => {
    wire([{ foundItemReportId: 1, foundByName: 'A', status: 'Unclaimed', finderPhotoUrl: 'f', photoUrl: 'i' }])
    const wrapper = mount(FoundItemRegistry)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Xem hồ sơ').trigger('click')
    await flushPromises()
    const overlays = document.body.querySelectorAll('.modal-overlay')
    const lastOverlay = overlays[overlays.length - 1]
    lastOverlay.dispatchEvent(new Event('click', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.showEvidenceModal).toBe(false)
    wrapper.unmount()
  })
})

describe('LostItemList template controls', () => {
  function wire(items = [{ lostItemReportId: 1, reporterName: 'A', itemDescription: 'ĐT', status: 'Pending' }]) {
    lostFoundApi.getLostItems.mockResolvedValue({ data: { items, total: items.length } })
  }

  function setValue(el, value) {
    el.value = value
    el.dispatchEvent(new Event('input', { bubbles: true }))
  }

  it('interacts with create form inputs via template and cancels', async () => {
    wire()
    const wrapper = mount(LostItemList)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Báo mất')).trigger('click')
    await flushPromises()

    const panel = document.body.querySelector('.modal-panel')
    const inputs = panel.querySelectorAll('input')
    const ta = panel.querySelector('textarea')
    setValue(inputs[0], 'Người báo')
    setValue(inputs[1], '222')
    setValue(inputs[2], '091')
    setValue(inputs[3], 'a@b.c')
    setValue(inputs[4], 'Nơi mất')
    setValue(inputs[5], '2026-08-01T10:00')
    setValue(ta, 'Mô tả')
    await flushPromises()
    expect(wrapper.vm.form.reporterName).toBe('Người báo')
    expect(wrapper.vm.form.lostAtUtc).toBe('2026-08-01T10:00')

    const huys = [...panel.querySelectorAll('button')].filter((b) => b.textContent.trim() === 'Hủy')
    huys[0].dispatchEvent(new Event('click', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.showForm).toBe(false)
    wrapper.unmount()
  })

  it('closes create form via overlay self click', async () => {
    wire()
    const wrapper = mount(LostItemList)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Báo mất')).trigger('click')
    await flushPromises()
    const overlay = document.body.querySelector('.modal-overlay')
    overlay.dispatchEvent(new Event('click', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.showForm).toBe(false)
    wrapper.unmount()
  })

  it('closes evidence modal via Đóng and overlay', async () => {
    wire([{ lostItemReportId: 1, reporterName: 'A', itemDescription: 'ĐT', status: 'Pending', reporterPhotoUrl: 'r', photoUrl: 'i' }])
    const wrapper = mount(LostItemList)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Xem hồ sơ').trigger('click')
    await flushPromises()
    const closeBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.trim() === 'Đóng')
    closeBtn.dispatchEvent(new Event('click', { bubbles: true }))
    expect(wrapper.vm.showEvidenceModal).toBe(false)
    wrapper.unmount()
  })

  it('closes evidence modal via overlay self click', async () => {
    wire([{ lostItemReportId: 1, reporterName: 'A', itemDescription: 'ĐT', status: 'Pending', reporterPhotoUrl: 'r', photoUrl: 'i' }])
    const wrapper = mount(LostItemList)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Xem hồ sơ').trigger('click')
    await flushPromises()
    const overlays = document.body.querySelectorAll('.modal-overlay')
    const lastOverlay = overlays[overlays.length - 1]
    lastOverlay.dispatchEvent(new Event('click', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.showEvidenceModal).toBe(false)
    wrapper.unmount()
  })
})
