import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const push = vi.fn()
vi.mock('vue-router', () => ({ useRouter: () => ({ push }) }))
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
vi.mock('../../services/notificationApi', () => ({
  onEntityChanged: () => () => {},
}))

const lookupApi = await import('../../services/lookupApi')
const DepartmentPosition = (await import('../DepartmentPosition.vue')).default

const sharedStubs = { ImportModal: true, ExportModal: true }

beforeEach(() => {
  vi.clearAllMocks()
  push.mockReset()
  lookupApi.getDepartments.mockResolvedValue({ data: [{ departmentId: 1, name: 'Phòng Nhân sự', employeeCount: 3 }] })
  lookupApi.getPositions.mockResolvedValue({ data: [{ positionId: 2, name: 'Trưởng phòng', employeeCount: 1 }] })
})

async function mountPage() {
  const wrapper = mount(DepartmentPosition, {
    global: { stubs: { ...sharedStubs, RouterLink: { template: '<a><slot /></a>' } } },
  })
  await flushPromises()
  return wrapper
}

describe('DepartmentPosition', () => {
  it('creates a department through the modal', async () => {
    const wrapper = await mountPage()
    await wrapper.findAll('button').find((b) => b.classes().includes('rounded-btn')).trigger('click')
    await wrapper.find('.modern-modal input').setValue('Phòng Nhân sự')
    lookupApi.createDepartment.mockResolvedValue({})
    await wrapper.find('.modern-modal form').trigger('submit')
    await flushPromises()
    expect(lookupApi.createDepartment).toHaveBeenCalledWith(expect.objectContaining({ name: 'Phòng Nhân sự' }))
  })

  it('renders departments and positions with their counts', async () => {
    const wrapper = await mountPage()
    expect(wrapper.find('table').text()).toContain('Phòng Nhân sự')
    expect(wrapper.text()).toContain('Trưởng phòng')
  })

  it('department modal validates empty name', async () => {
    const wrapper = await mountPage()
    await wrapper.findAll('button').find((b) => b.classes().includes('rounded-btn')).trigger('click')
    await wrapper.find('.modern-modal form').trigger('submit')
    expect(wrapper.vm.deptErrors.name).toBeTruthy()
    expect(lookupApi.createDepartment).not.toHaveBeenCalled()
  })

  it('updates an existing department', async () => {
    const wrapper = await mountPage()
    wrapper.vm.openDeptModal({ departmentId: 1, name: 'Cũ' })
    await flushPromises()
    await wrapper.find('.modern-modal input').setValue('Phòng Mới')
    lookupApi.updateDepartment.mockResolvedValue({})
    await wrapper.find('.modern-modal form').trigger('submit')
    await flushPromises()
    expect(lookupApi.updateDepartment).toHaveBeenCalledWith(1, expect.objectContaining({ name: 'Phòng Mới' }))
  })

  it('department submit surfaces server errors', async () => {
    const wrapper = await mountPage()
    await wrapper.findAll('button').find((b) => b.classes().includes('rounded-btn')).trigger('click')
    await wrapper.find('.modern-modal input').setValue('Tên')
    lookupApi.createDepartment.mockRejectedValue({ response: { data: { message: 'trùng' } } })
    await wrapper.find('.modern-modal form').trigger('submit')
    await flushPromises()
    expect(wrapper.vm.modalError).toBe('trùng')
  })

  it('creates a position through the modal', async () => {
    const wrapper = await mountPage()
    wrapper.vm.openPosModal()
    await flushPromises()
    await wrapper.find('.modern-modal input').setValue('Nhân viên hỗ trợ')
    lookupApi.createPosition.mockResolvedValue({})
    await wrapper.find('.modern-modal form').trigger('submit')
    await flushPromises()
    expect(lookupApi.createPosition).toHaveBeenCalledWith(expect.objectContaining({ name: 'Nhân viên hỗ trợ' }))
  })

  it('position modal validates empty name', async () => {
    const wrapper = await mountPage()
    wrapper.vm.openPosModal()
    await flushPromises()
    await wrapper.find('.modern-modal form').trigger('submit')
    expect(wrapper.vm.posErrors.name).toBeTruthy()
    expect(lookupApi.createPosition).not.toHaveBeenCalled()
  })

  it('updates an existing position', async () => {
    const wrapper = await mountPage()
    wrapper.vm.openPosModal({ positionId: 2, name: 'Cũ' })
    await flushPromises()
    await wrapper.find('.modern-modal input').setValue('Vai trò mới')
    lookupApi.updatePosition.mockResolvedValue({})
    await wrapper.find('.modern-modal form').trigger('submit')
    await flushPromises()
    expect(lookupApi.updatePosition).toHaveBeenCalledWith(2, expect.objectContaining({ name: 'Vai trò mới' }))
  })

  it('position submit surfaces server errors', async () => {
    const wrapper = await mountPage()
    wrapper.vm.openPosModal()
    await flushPromises()
    await wrapper.find('.modern-modal input').setValue('Tên')
    lookupApi.createPosition.mockRejectedValue({ response: { data: { message: 'x' } } })
    await wrapper.find('.modern-modal form').trigger('submit')
    await flushPromises()
    expect(wrapper.vm.modalError).toBe('x')
  })

  it('deletes a department after confirmation', async () => {
    const wrapper = await mountPage()
    wrapper.vm.confirmDeleteDept({ departmentId: 1, name: 'HR' })
    expect(wrapper.vm.showDeleteModal).toBe(true)
    lookupApi.deleteDepartment.mockResolvedValue({})
    await wrapper.vm.handleDelete()
    expect(lookupApi.deleteDepartment).toHaveBeenCalledWith(1)
  })

  it('deletes a position after confirmation', async () => {
    const wrapper = await mountPage()
    wrapper.vm.confirmDeletePos({ positionId: 2, name: 'POS' })
    expect(wrapper.vm.showDeleteModal).toBe(true)
    lookupApi.deletePosition.mockResolvedValue({})
    await wrapper.vm.handleDelete()
    expect(lookupApi.deletePosition).toHaveBeenCalledWith(2)
  })

  it('delete reports server errors', async () => {
    const wrapper = await mountPage()
    wrapper.vm.confirmDeleteDept({ departmentId: 1, name: 'HR' })
    lookupApi.deleteDepartment.mockRejectedValue(new Error('x'))
    await wrapper.vm.handleDelete()
    expect(wrapper.vm.modalError).toBeTruthy()
  })

  it('navigates to role permissions', async () => {
    const wrapper = await mountPage()
    wrapper.vm.goToRolePermissions({ positionId: 2 })
    expect(push).toHaveBeenCalledWith({ name: 'RolePermissions' })
  })

  it('onDeptImportComplete shows success and error toasts', async () => {
    const wrapper = await mountPage()
    wrapper.vm.showDeptImport = true
    wrapper.vm.onDeptImportComplete({ successCount: 2, errorCount: 0 })
    expect(wrapper.vm.showDeptImport).toBe(false)
    wrapper.vm.onDeptImportComplete({ successCount: 1, errorCount: 1 })
    expect(wrapper.vm.toast.type).toBe('error')
  })

  it('onPosImportComplete shows a toast', async () => {
    const wrapper = await mountPage()
    wrapper.vm.showPosImport = true
    wrapper.vm.onPosImportComplete({ successCount: 3, errorCount: 0 })
    expect(wrapper.vm.showPosImport).toBe(false)
    expect(wrapper.vm.toast.message).toContain('3')
  })

  it('fetchDepartments handles errors', async () => {
    const err = vi.spyOn(console, 'error').mockImplementation(() => {})
    lookupApi.getDepartments.mockRejectedValue(new Error('boom'))
    const wrapper = mount(DepartmentPosition, { global: { stubs: { ...sharedStubs, RouterLink: true } } })
    await flushPromises()
    expect(err).toHaveBeenCalled()
    err.mockRestore()
  })

  it('fetchPositions handles errors', async () => {
    const err = vi.spyOn(console, 'error').mockImplementation(() => {})
    lookupApi.getPositions.mockRejectedValue(new Error('boom'))
    const wrapper = mount(DepartmentPosition, { global: { stubs: { ...sharedStubs, RouterLink: true } } })
    await flushPromises()
    expect(err).toHaveBeenCalled()
    err.mockRestore()
  })
})
