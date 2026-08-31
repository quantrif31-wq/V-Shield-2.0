import { flushPromises, mount } from '@vue/test-utils'
import { nextTick } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/lookupApi', () => ({
  getDepartments: vi.fn(),
  createDepartment: vi.fn(),
  updateDepartment: vi.fn(),
  deleteDepartment: vi.fn(),
  getPositions: vi.fn(),
  createPosition: vi.fn(),
  updatePosition: vi.fn(),
  deletePosition: vi.fn(),
}))
vi.mock('../../services/exceptionReasonApi', () => ({
  getExceptionReasons: vi.fn(),
  createExceptionReason: vi.fn(),
  updateExceptionReason: vi.fn(),
  deleteExceptionReason: vi.fn(),
}))

const lookupApi = await import('../../services/lookupApi')
const exceptionReasonApi = await import('../../services/exceptionReasonApi')
const SystemCatalog = (await import('../SystemCatalog.vue')).default
const DepartmentPosition = (await import('../DepartmentPosition.vue')).default

const sharedStubs = { ImportModal: true, ExportModal: true, RouterLink: { template: '<a><slot /></a>' } }
const routerStubs = { ...sharedStubs }

const sampleReasons = [
  { reasonId: 1, reasonCode: 'BYPASS_MANUAL', description: 'Mở cổng thủ công', usageCount: 2 },
  { reasonId: 2, reasonCode: 'TECH_ERROR', description: 'Lỗi thiết bị', usageCount: 0 },
]

beforeEach(() => {
  vi.clearAllMocks()
  lookupApi.getDepartments.mockResolvedValue({ data: [{ departmentId: 1, name: 'An ninh', employeeCount: 5 }] })
  lookupApi.getPositions.mockResolvedValue({ data: [{ positionId: 1, name: 'Trưởng', employeeCount: 2 }] })
  exceptionReasonApi.getExceptionReasons.mockResolvedValue({ data: sampleReasons })
  vi.spyOn(console, 'error').mockImplementation(() => {})
  window.confirm = vi.fn(() => true)
  window.alert = vi.fn()
})

afterEach(() => {
  vi.restoreAllMocks()
})

describe('SystemCatalog', () => {
  it('loads departments, positions and reasons on mount', async () => {
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(lookupApi.getDepartments).toHaveBeenCalled()
    expect(lookupApi.getPositions).toHaveBeenCalled()
    expect(exceptionReasonApi.getExceptionReasons).toHaveBeenCalled()
    expect(wrapper.vm.isLoading).toBe(false)
    expect(wrapper.vm.departments.length).toBe(1)
    expect(wrapper.vm.positions.length).toBe(1)
    expect(wrapper.vm.reasons.length).toBe(2)
    expect(wrapper.text()).toContain('An ninh')
    expect(wrapper.text()).toContain('Trưởng')
    expect(wrapper.text()).toContain('BYPASS_MANUAL')
  })

  it('shows loading state while fetching', async () => {
    let resolveFn
    lookupApi.getDepartments.mockReturnValue(new Promise((r) => { resolveFn = r }))
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(wrapper.vm.isLoading).toBe(true)
    expect(wrapper.text()).toContain('Đang tải phòng ban')
    resolveFn({ data: [] })
    await flushPromises()
    expect(wrapper.vm.isLoading).toBe(false)
  })

  it('resets lists on fetch error and logs it', async () => {
    lookupApi.getDepartments.mockRejectedValue(new Error('boom'))
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(console.error).toHaveBeenCalled()
    expect(wrapper.vm.departments).toEqual([])
    expect(wrapper.vm.positions).toEqual([])
    expect(wrapper.vm.reasons).toEqual([])
    expect(wrapper.vm.isLoading).toBe(false)
    expect(wrapper.text()).toContain('Chưa có phòng ban nào.')
  })

  it('renders empty states when no data', async () => {
    lookupApi.getDepartments.mockResolvedValue({ data: [] })
    lookupApi.getPositions.mockResolvedValue({ data: [] })
    exceptionReasonApi.getExceptionReasons.mockResolvedValue({ data: [] })
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(wrapper.text()).toContain('Chưa có phòng ban nào.')
    expect(wrapper.text()).toContain('Chưa có chức vụ nào.')
    expect(wrapper.text()).toContain('Chưa có lý do ngoại lệ nào.')
  })

  it('computes usedReasonCount from reasons with usage', async () => {
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(wrapper.vm.usedReasonCount).toBe(1)
  })

  it('import complete closes modal and refetches', async () => {
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.showImportModal = true
    await wrapper.vm.onImportComplete({ ok: true })
    expect(wrapper.vm.showImportModal).toBe(false)
    expect(exceptionReasonApi.getExceptionReasons).toHaveBeenCalledTimes(2)
  })

  it('opens import and export modals via header buttons', async () => {
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    const importBtn = wrapper.findAll('button').find((b) => b.text().includes('Nhập lý do ngoại lệ'))
    await importBtn.trigger('click')
    expect(wrapper.vm.showImportModal).toBe(true)
    const exportBtn = wrapper.findAll('button').find((b) => b.text().includes('Xuất lý do ngoại lệ'))
    await exportBtn.trigger('click')
    expect(wrapper.vm.showExportModal).toBe(true)
  })

  it('opens create reason modal with empty form', async () => {
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    const addBtn = wrapper.findAll('button').find((b) => b.text().includes('Thêm lý do'))
    await addBtn.trigger('click')
    expect(wrapper.vm.showReasonModal).toBe(true)
    expect(wrapper.vm.editingReasonId).toBe(null)
    expect(wrapper.vm.reasonForm.reasonCode).toBe('')
    expect(wrapper.text()).toContain('Thêm lý do ngoại lệ')
  })

  it('opens edit reason modal with prefilled form and updates title', async () => {
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.vm.openReasonModal(sampleReasons[0])
    expect(wrapper.vm.editingReasonId).toBe(1)
    expect(wrapper.vm.reasonForm.reasonCode).toBe('BYPASS_MANUAL')
    expect(wrapper.vm.reasonForm.description).toBe('Mở cổng thủ công')
    expect(wrapper.text()).toContain('Cập nhật lý do ngoại lệ')
  })

  it('edits a reason via Sửa button in the list', async () => {
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    const editBtn = wrapper.findAll('button').find((b) => b.text().includes('Sửa'))
    await editBtn.trigger('click')
    expect(wrapper.vm.editingReasonId).toBe(1)
  })

  it('closes reason modal via Hủy and via close button and via overlay click', async () => {
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.vm.openReasonModal(sampleReasons[1])
    expect(wrapper.vm.showReasonModal).toBe(true)
    const cancelBtn = wrapper.findAll('button').find((b) => b.text().includes('Hủy'))
    await cancelBtn.trigger('click')
    expect(wrapper.vm.showReasonModal).toBe(false)

    await wrapper.vm.openReasonModal(sampleReasons[1])
    const xBtn = wrapper.findAll('.modal-close').at(0)
    await xBtn.trigger('click')
    expect(wrapper.vm.showReasonModal).toBe(false)

    await wrapper.vm.openReasonModal(sampleReasons[1])
    const overlay = wrapper.find('.modal-overlay')
    await overlay.trigger('click')
    expect(wrapper.vm.showReasonModal).toBe(false)
  })

  it('validates required fields before saving', async () => {
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.vm.openReasonModal()
    wrapper.vm.reasonForm.reasonCode = '   '
    wrapper.vm.reasonForm.description = ''
    await wrapper.vm.handleSaveReason()
    expect(wrapper.vm.reasonErrors.reasonCode).toBe('Vui lòng nhập mã lý do.')
    expect(wrapper.vm.reasonErrors.description).toBe('Vui lòng nhập mô tả.')
    expect(exceptionReasonApi.createExceptionReason).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Vui lòng nhập mã lý do.')
  })

  it('clears field errors on input', async () => {
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.vm.openReasonModal()
    wrapper.vm.reasonErrors.reasonCode = 'err'
    wrapper.vm.reasonErrors.description = 'err'
    const inputs = wrapper.findAll('.modal input[type="text"]')
    await inputs.at(0).setValue('NEW_CODE')
    await inputs.at(1).setValue('Mô tả mới')
    expect(wrapper.vm.reasonErrors.reasonCode).toBe('')
    expect(wrapper.vm.reasonErrors.description).toBe('')
  })

  it('creates a reason on save and refetches', async () => {
    exceptionReasonApi.createExceptionReason.mockResolvedValue({})
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.reasonForm.reasonCode = 'NEW_CODE'
    wrapper.vm.reasonForm.description = 'Mô tả mới'
    await wrapper.vm.handleSaveReason()
    expect(exceptionReasonApi.createExceptionReason).toHaveBeenCalledWith({
      reasonCode: 'NEW_CODE',
      description: 'Mô tả mới',
    })
    expect(exceptionReasonApi.getExceptionReasons).toHaveBeenCalledTimes(2)
    expect(wrapper.vm.showReasonModal).toBe(false)
    expect(wrapper.vm.isSaving).toBe(false)
  })

  it('updates an existing reason on save', async () => {
    exceptionReasonApi.updateExceptionReason.mockResolvedValue({})
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.vm.openReasonModal(sampleReasons[0])
    wrapper.vm.reasonForm.description = 'Mô tả cập nhật'
    await wrapper.vm.handleSaveReason()
    expect(exceptionReasonApi.updateExceptionReason).toHaveBeenCalledWith(1, {
      reasonCode: 'BYPASS_MANUAL',
      description: 'Mô tả cập nhật',
    })
    expect(wrapper.vm.showReasonModal).toBe(false)
  })

  it('shows isSaving state while create is pending', async () => {
    let resolveFn
    exceptionReasonApi.createExceptionReason.mockReturnValue(new Promise((r) => { resolveFn = r }))
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.reasonForm.reasonCode = 'NEW_CODE'
    wrapper.vm.reasonForm.description = 'Mô tả'
    const pending = wrapper.vm.handleSaveReason()
    await wrapper.vm.$nextTick()
    expect(wrapper.vm.isSaving).toBe(true)
    resolveFn({})
    await pending
    await flushPromises()
    expect(wrapper.vm.isSaving).toBe(false)
  })

  it('shows form error when save fails with server message', async () => {
    exceptionReasonApi.createExceptionReason.mockRejectedValue({ response: { data: { message: 'Trùng mã' } } })
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.vm.openReasonModal()
    wrapper.vm.reasonForm.reasonCode = 'DUP'
    wrapper.vm.reasonForm.description = 'Mô tả'
    await wrapper.vm.handleSaveReason()
    expect(console.error).toHaveBeenCalled()
    expect(wrapper.vm.formError).toBe('Trùng mã')
    expect(wrapper.text()).toContain('Trùng mã')
    expect(wrapper.vm.isSaving).toBe(false)
  })

  it('shows generic form error when save fails without message', async () => {
    exceptionReasonApi.createExceptionReason.mockRejectedValue(new Error('net'))
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.reasonForm.reasonCode = 'X'
    wrapper.vm.reasonForm.description = 'Mô tả'
    await wrapper.vm.handleSaveReason()
    expect(wrapper.vm.formError).toBe('Không thể lưu lý do ngoại lệ.')
  })

  it('delete reason skips when not confirmed', async () => {
    window.confirm = vi.fn(() => false)
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.vm.handleDeleteReason(sampleReasons[0])
    expect(window.confirm).toHaveBeenCalledWith('Xóa lý do "BYPASS_MANUAL"?')
    expect(exceptionReasonApi.deleteExceptionReason).not.toHaveBeenCalled()
  })

  it('deletes reason through the Xóa button', async () => {
    exceptionReasonApi.deleteExceptionReason.mockResolvedValue({})
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    const delBtn = wrapper.findAll('button').find((b) => b.text().includes('Xóa'))
    await delBtn.trigger('click')
    expect(exceptionReasonApi.deleteExceptionReason).toHaveBeenCalledWith(1)
    expect(exceptionReasonApi.getExceptionReasons).toHaveBeenCalledTimes(2)
  })

  it('alerts when deleting a reason fails', async () => {
    exceptionReasonApi.deleteExceptionReason.mockRejectedValue({ response: { data: { message: 'Đang lưu log' } } })
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.vm.handleDeleteReason(sampleReasons[0])
    expect(console.error).toHaveBeenCalled()
    expect(window.alert).toHaveBeenCalledWith('Đang lưu log')
  })

  it('alerts with generic message when delete fails without server message', async () => {
    exceptionReasonApi.deleteExceptionReason.mockRejectedValue(new Error('net'))
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.vm.handleDeleteReason(sampleReasons[0])
    expect(window.alert).toHaveBeenCalledWith('Không thể xóa lý do ngoại lệ này.')
  })

  it('renders import and export modal stubs when flags are set', async () => {
    const wrapper = mount(SystemCatalog, { global: { stubs: sharedStubs } })
    await flushPromises()
    wrapper.vm.showImportModal = true
    wrapper.vm.showExportModal = true
    await nextTick()
    expect(wrapper.findComponent({ name: 'ImportModal' }).exists()).toBe(true)
    expect(wrapper.findComponent({ name: 'ExportModal' }).exists()).toBe(true)
  })
})

describe('DepartmentPosition', () => {
  it('loads departments and positions', async () => {
    const wrapper = mount(DepartmentPosition, {
      global: { stubs: { ...sharedStubs, RouterLink: { template: '<a><slot /></a>' } } },
    })
    await flushPromises()
    expect(lookupApi.getDepartments).toHaveBeenCalled()
    expect(lookupApi.getPositions).toHaveBeenCalled()
  })
})