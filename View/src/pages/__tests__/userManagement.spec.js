import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/userApi', () => ({
  getAll: vi.fn(),
  create: vi.fn(),
  update: vi.fn(),
  deleteUser: vi.fn(),
  resetMfa: vi.fn(),
  getOperationalScopeReference: vi.fn(),
  getOperationalScopes: vi.fn(),
  replaceOperationalScopes: vi.fn(),
  getUserGateAccess: vi.fn(),
  replaceUserGateAccess: vi.fn(),
}))
vi.mock('../../services/employeeApi', () => ({ getAll: vi.fn() }))
vi.mock('../../services/enterpriseSecurityApi', () => ({ enterpriseApi: {} }))

const userApi = await import('../../services/userApi')
const employeeApi = await import('../../services/employeeApi')
const UserManagement = (await import('../UserManagement.vue')).default

const sharedStubs = { ImportModal: true, ExportModal: true, StepUpModal: true }

beforeEach(() => {
  vi.clearAllMocks()
  vi.unstubAllGlobals()
})

describe('UserManagement', () => {
  it('loads and renders user accounts', async () => {
    userApi.getAll.mockResolvedValue({ data: [{ userId: 1, username: 'admin', fullName: 'Admin', role: 'Admin', isActive: true, employeeId: 1 }] })
    employeeApi.getAll.mockResolvedValue({ data: [] })
    const wrapper = mount(UserManagement, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(userApi.getAll).toHaveBeenCalled()
    expect(wrapper.find('tbody').text()).toContain('admin')
  })

  it('filters users by search query', async () => {
    userApi.getAll.mockResolvedValue({
      data: [
        { userId: 1, username: 'admin', fullName: 'Admin', role: 'Admin', isActive: true },
        { userId: 2, username: 'baove', fullName: 'Bảo vệ', role: 'BaoVe', isActive: true },
      ],
    })
    employeeApi.getAll.mockResolvedValue({ data: [] })
    const wrapper = mount(UserManagement, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.find('.search-box input').setValue('baove')
    await flushPromises()
    expect(wrapper.find('tbody').text()).toContain('baove')
    expect(wrapper.find('tbody').text()).not.toContain('admin')
  })

  it('deletes a user after confirmation', async () => {
    userApi.getAll.mockResolvedValue({ data: [{ userId: 1, username: 'admin', fullName: 'Admin', role: 'Admin', isActive: true, employeeId: 1 }] })
    employeeApi.getAll.mockResolvedValue({ data: [{ employeeId: 1, fullName: 'Admin' }] })
    const wrapper = mount(UserManagement, { global: { stubs: sharedStubs } })
    await flushPromises()

    await wrapper.find('tbody .icon-btn.action-reject').trigger('click')
    await flushPromises()
    userApi.deleteUser.mockResolvedValue({})
    await wrapper.find('.modal-backdrop .btn-danger').trigger('click')
    await flushPromises()
    expect(userApi.deleteUser).toHaveBeenCalledWith(1)
  })

  it('creates a user with an assigned employee', async () => {
    userApi.getAll.mockResolvedValue({ data: [] })
    employeeApi.getAll.mockResolvedValue({ data: [{ employeeId: 7, fullName: 'Nguyễn An' }] })
    const wrapper = mount(UserManagement, { global: { stubs: sharedStubs } })
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text().includes('Thêm tài khoản') || b.text().includes('Tạo tài khoản')).trigger('click')
    const inputs = wrapper.findAll('form .sleek-input')
    await inputs[0].setValue('nhanvien1')
    await inputs[1].setValue('Staff@123')
    await wrapper.find('form .combo-input').setValue('Nguyễn')
    const employeeOption = wrapper.findAll('.combo-option').find((li) => li.text().includes('Nguyễn An'))
    await employeeOption.trigger('mousedown')
    userApi.create.mockResolvedValue({})
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(userApi.create).toHaveBeenCalledWith(expect.objectContaining({ username: 'nhanvien1', employeeId: 7 }))
  })
})
