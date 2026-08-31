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
vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: { setStepUpSession: vi.fn() },
}))

const userApi = await import('../../services/userApi')
const employeeApi = await import('../../services/employeeApi')
const securityApi = await import('../../services/enterpriseSecurityApi')
const UserManagement = (await import('../UserManagement.vue')).default

const sharedStubs = { ImportModal: true, ExportModal: true, StepUpModal: true }

const user = (over = {}) => ({
  userId: 1,
  username: 'admin',
  fullName: 'Admin',
  role: 'Admin',
  isActive: true,
  mfaEnabled: true,
  createdAt: '2026-01-05T00:00:00Z',
  employeeId: 1,
  ...over,
})

const scopeRef = {
  data: {
    tasksByRole: { Admin: ['dashboard', 'users'] },
    taskCatalog: [
      { taskKey: 'dashboard', label: 'Dashboard' },
      { taskKey: 'users', label: 'Quản lý tài khoản' },
    ],
    sites: [{ siteId: 1, name: 'Site A' }],
    gates: [{ gateId: 10, name: 'Cổng chính' }],
    lanes: [{ laneId: 100, name: 'Làn 1' }],
    zones: [{ securityZoneId: 1000, name: 'Khu A' }],
  },
}

async function mountPage(users = [user()], employees = []) {
  userApi.getAll.mockResolvedValue({ data: users })
  employeeApi.getAll.mockResolvedValue({ data: employees })
  const wrapper = mount(UserManagement, { global: { stubs: sharedStubs } })
  await flushPromises()
  return wrapper
}

beforeEach(() => {
  vi.clearAllMocks()
  vi.unstubAllGlobals()
})

describe('UserManagement', () => {
  it('loads and renders user accounts', async () => {
    const wrapper = await mountPage([user()])
    expect(userApi.getAll).toHaveBeenCalled()
    expect(wrapper.find('tbody').text()).toContain('admin')
    expect(wrapper.find('.stat-val.blue').text()).toBe('1')
    expect(wrapper.find('.stat-val.green').text()).toBe('1')
    expect(wrapper.find('.stat-val.red').text()).toBe('0')
  })

  it('shows loading state then renders', async () => {
    let resolveUsers
    userApi.getAll.mockReturnValue(new Promise((r) => { resolveUsers = r }))
    employeeApi.getAll.mockResolvedValue({ data: [] })
    const wrapper = mount(UserManagement, { global: { stubs: sharedStubs } })
    expect(wrapper.text()).toContain('Đang tải dữ liệu')
    resolveUsers({ data: [user()] })
    await flushPromises()
    expect(wrapper.find('tbody').text()).toContain('admin')
  })

  it('shows load error and retries', async () => {
    userApi.getAll.mockRejectedValueOnce({ code: 'ERR_NETWORK' })
    employeeApi.getAll.mockResolvedValue({ data: [] })
    const wrapper = mount(UserManagement, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(wrapper.text()).toContain('Không thể kết nối đến server')
    userApi.getAll.mockResolvedValue({ data: [user()] })
    await wrapper.findAll('button').find((b) => b.text().includes('Thử lại')).trigger('click')
    await flushPromises()
    expect(wrapper.find('tbody').text()).toContain('admin')
  })

  it('shows a generic load error message', async () => {
    userApi.getAll.mockRejectedValueOnce({ message: 'boom' })
    employeeApi.getAll.mockResolvedValue({ data: [] })
    const wrapper = mount(UserManagement, { global: { stubs: sharedStubs } })
    await flushPromises()
    expect(wrapper.text()).toContain('Không thể tải danh sách tài khoản')
  })

  it('filters users by search query', async () => {
    const wrapper = await mountPage([
      user({ userId: 1, username: 'admin', fullName: 'Admin' }),
      user({ userId: 2, username: 'baove', fullName: 'Bảo vệ', role: 'BaoVe' }),
    ])
    await wrapper.find('.search-box input').setValue('baove')
    await flushPromises()
    expect(wrapper.find('tbody').text()).toContain('baove')
    expect(wrapper.find('tbody').text()).not.toContain('admin')
  })

  it('filters by role and status', async () => {
    const wrapper = await mountPage([
      user({ userId: 1, username: 'admin', role: 'Admin', isActive: true }),
      user({ userId: 2, username: 'baove', role: 'BaoVe', isActive: false }),
    ])
    const selects = wrapper.findAll('.minimal-select')
    await selects[0].setValue('BaoVe')
    await flushPromises()
    expect(wrapper.find('tbody').text()).toContain('baove')
    expect(wrapper.find('tbody').text()).not.toContain('admin')

    await selects[0].setValue('')
    await wrapper.findAll('.minimal-select')[1].setValue('inactive')
    await flushPromises()
    expect(wrapper.find('tbody').text()).toContain('baove')
    expect(wrapper.find('tbody').text()).not.toContain('admin')
  })

  it('renders the empty table row when no users match', async () => {
    const wrapper = await mountPage([user()])
    await wrapper.find('.search-box input').setValue('zzz')
    await flushPromises()
    expect(wrapper.text()).toContain('Không tìm thấy tài khoản nào')
  })

  it('opens import and export modals', async () => {
    const wrapper = await mountPage()
    await wrapper.findAll('header button')[0].trigger('click')
    expect(wrapper.vm.showImportModal).toBe(true)
    await wrapper.findAll('header button')[1].trigger('click')
    expect(wrapper.vm.showExportModal).toBe(true)
  })

  it('handles import complete callback', async () => {
    const wrapper = await mountPage()
    userApi.getAll.mockResolvedValue({ data: [user()] })
    wrapper.vm.onImportComplete()
    await flushPromises()
    expect(wrapper.vm.showImportModal).toBe(false)
    expect(userApi.getAll).toHaveBeenCalledTimes(2)
  })

  it('validates the create form fields', async () => {
    const wrapper = await mountPage([], [])
    wrapper.vm.openCreateModal()
    await flushPromises()
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(wrapper.text()).toContain('Vui lòng nhập tên đăng nhập.')
    expect(wrapper.text()).toContain('Vui lòng nhập mật khẩu.')
    expect(wrapper.text()).toContain('Vui lòng chọn nhân viên')
  })

  it('validates username format and max lengths', async () => {
    const wrapper = await mountPage([], [])
    wrapper.vm.openCreateModal()
    await flushPromises()
    const inputs = wrapper.findAll('form .sleek-input')
    await inputs[0].setValue('h')
    await inputs[1].setValue('pass1')
    employeeApi.getAll.mockResolvedValue({ data: [{ employeeId: 5, fullName: 'Nguyễn An' }] })
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(wrapper.text()).toContain('Tên đăng nhập tối thiểu 3 ký tự.')

    const inputs2 = wrapper.findAll('form .sleek-input')
    await inputs2[0].setValue('bad name')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(wrapper.text()).toContain('Tên đăng nhập chỉ gồm chữ cái, số và dấu gạch dưới.')

    const inputs3 = wrapper.findAll('form .sleek-input')
    await inputs3[1].setValue('123')
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(wrapper.text()).toContain('Mật khẩu tối thiểu 6 ký tự.')
  })

  it('creates a user with an assigned employee', async () => {
    const wrapper = await mountPage([], [{ employeeId: 7, fullName: 'Nguyễn An' }])
    wrapper.vm.openCreateModal()
    await flushPromises()
    await wrapper.find('form .sleek-input').setValue('nhanvien1')
    await wrapper.findAll('form .sleek-input')[1].setValue('Staff@123')
    await wrapper.find('form .combo-input').setValue('Nguyễn')
    const employeeOption = wrapper.findAll('.combo-option').find((li) => li.text().includes('Nguyễn An'))
    await employeeOption.trigger('mousedown')
    userApi.create.mockResolvedValue({})
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(userApi.create).toHaveBeenCalledWith(expect.objectContaining({ username: 'nhanvien1', employeeId: 7 }))
  })

  it('shows create error message on failure', async () => {
    const wrapper = await mountPage([], [{ employeeId: 7, fullName: 'Nguyễn An' }])
    wrapper.vm.openCreateModal()
    await flushPromises()
    await wrapper.find('form .sleek-input').setValue('nhanvien1')
    await wrapper.findAll('form .sleek-input')[1].setValue('Staff@123')
    await wrapper.find('form .combo-input').setValue('Nguyễn')
    await wrapper.findAll('.combo-option').find((li) => li.text().includes('Nguyễn An')).trigger('mousedown')
    userApi.create.mockRejectedValue({ response: { data: { message: 'Trùng tên' } } })
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(wrapper.text()).toContain('Trùng tên')
  })

  it('edits an existing user via the update path', async () => {
    const wrapper = await mountPage([user({ role: 'QuanLy', isActive: true })], [{ employeeId: 1, fullName: 'Admin' }])
    wrapper.vm.openEditModal(user({ role: 'QuanLy', isActive: true }))
    await flushPromises()
    expect(wrapper.vm.isEditing).toBe(true)
    expect(wrapper.text()).toContain('Cập nhật tài khoản')
    const selects = wrapper.findAll('form .sleek-select')
    await selects[0].setValue('BaoVe')
    userApi.update.mockResolvedValue({})
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(userApi.update).toHaveBeenCalledWith(1, expect.objectContaining({ role: 'BaoVe', employeeId: 1 }))
  })

  it('sends password when editing with a new password', async () => {
    const wrapper = await mountPage([user()], [{ employeeId: 1, fullName: 'Admin' }])
    wrapper.vm.openEditModal(user())
    await flushPromises()
    const inputs = wrapper.findAll('form input')
    const passInput = inputs.find((i) => i.attributes('type') === 'password')
    await passInput.setValue('NewPass@1')
    userApi.update.mockResolvedValue({})
    await wrapper.find('form').trigger('submit')
    await flushPromises()
    expect(userApi.update).toHaveBeenCalledWith(1, expect.objectContaining({ password: 'NewPass@1' }))
  })

  it('closes the modal via the cancel button and backdrop', async () => {
    const wrapper = await mountPage()
    wrapper.vm.openCreateModal()
    await flushPromises()
    expect(wrapper.vm.showModal).toBe(true)
    await wrapper.findAll('.btn-secondary').find((b) => b.text().includes('Hủy')).trigger('click')
    expect(wrapper.vm.showModal).toBe(false)
  })

  it('clears employee selection and filters the employee dropdown', async () => {
    const wrapper = await mountPage([], [
      { employeeId: 7, fullName: 'Nguyễn An', phone: '0901', email: 'a@x.com' },
      { employeeId: 8, fullName: 'Trần B', phone: '0902' },
    ])
    wrapper.vm.openCreateModal()
    await flushPromises()
    await wrapper.find('form .combo-input').trigger('focus')
    expect(wrapper.find('.combo-dropdown').exists()).toBe(true)
    await wrapper.find('form .combo-input').setValue('0901')
    const options = wrapper.findAll('.combo-option')
    expect(options.length).toBe(1)
    await wrapper.findAll('.combo-option').find((li) => li.text().includes('Nguyễn An')).trigger('mousedown')
    await flushPromises()
    expect(wrapper.vm.modalForm.employeeId).toBe(7)
    expect(wrapper.vm.modalForm.fullName).toBe('Nguyễn An')
    await wrapper.find('.combo-clear-btn').trigger('click')
    expect(wrapper.vm.modalForm.employeeId).toBe(null)
    expect(wrapper.vm.modalForm.fullName).toBe('')
  })

  it('clears fullName when search text changes after selection', async () => {
    const wrapper = await mountPage([], [{ employeeId: 7, fullName: 'Nguyễn An' }])
    wrapper.vm.openCreateModal()
    await flushPromises()
    wrapper.vm.modalForm.fullName = 'Nguyễn An'
    await wrapper.find('form .combo-input').setValue('Nguyễn Anh')
    expect(wrapper.vm.modalForm.fullName).toBe('')
    expect(wrapper.vm.modalForm.employeeId).toBe(null)
  })

  it('handles outside click to close the dropdown', async () => {
    const wrapper = await mountPage()
    wrapper.vm.openCreateModal()
    await flushPromises()
    wrapper.vm.showEmployeeDropdown = true
    wrapper.vm.handleClickOutside({ target: document.createElement('div') })
    expect(wrapper.vm.showEmployeeDropdown).toBe(false)
    wrapper.vm.showEmployeeDropdown = true
    wrapper.vm.handleClickOutside({ target: wrapper.vm.comboBoxRef })
    expect(wrapper.vm.showEmployeeDropdown).toBe(true)
  })

  it('deletes a user after confirmation and refresh', async () => {
    const wrapper = await mountPage([user()])
    await wrapper.find('tbody .icon-btn.action-reject').trigger('click')
    await flushPromises()
    expect(wrapper.vm.showDeleteModal).toBe(true)
    userApi.deleteUser.mockResolvedValue({})
    await wrapper.find('.modal-backdrop .btn-danger').trigger('click')
    await flushPromises()
    expect(userApi.deleteUser).toHaveBeenCalledWith(1)
    expect(wrapper.vm.showDeleteModal).toBe(false)
  })

  it('shows delete error message', async () => {
    const wrapper = await mountPage([user()])
    await wrapper.find('tbody .icon-btn.action-reject').trigger('click')
    await flushPromises()
    userApi.deleteUser.mockRejectedValue({ response: { data: { message: 'Không xóa được' } } })
    await wrapper.find('.modal-backdrop .btn-danger').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Không xóa được')
  })

  it('cancels the delete modal', async () => {
    const wrapper = await mountPage([user()])
    await wrapper.find('tbody .icon-btn.action-reject').trigger('click')
    await flushPromises()
    await wrapper.findAll('.btn-secondary').find((b) => b.text().includes('Hủy')).trigger('click')
    expect(wrapper.vm.showDeleteModal).toBe(false)
  })

  it('resets MFA after confirm', async () => {
    const wrapper = await mountPage([user({ mfaEnabled: true })])
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    await wrapper.findAll('tbody .icon-btn')[2].trigger('click')
    await flushPromises()
    expect(userApi.resetMfa).toHaveBeenCalledWith(1)
  })

  it('skips reset MFA when not confirmed', async () => {
    const wrapper = await mountPage([user({ mfaEnabled: true })])
    vi.spyOn(window, 'confirm').mockReturnValue(false)
    await wrapper.findAll('tbody .icon-btn')[2].trigger('click')
    await flushPromises()
    expect(userApi.resetMfa).not.toHaveBeenCalled()
  })

  it('shows MFA reset error', async () => {
    const wrapper = await mountPage([user({ mfaEnabled: true })])
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    userApi.resetMfa.mockRejectedValue({ response: { data: { message: 'MFA lỗi' } } })
    await wrapper.findAll('tbody .icon-btn')[2].trigger('click')
    await flushPromises()
    expect(wrapper.vm.modalError).toBe('MFA lỗi')
  })

  it('disables MFA reset button when mfa is not enabled', async () => {
    const wrapper = await mountPage([user({ mfaEnabled: false })])
    expect(wrapper.findAll('tbody .icon-btn')[2].attributes('disabled')).toBeDefined()
  })

  it('renders helper values: initials, avatar, label, date', async () => {
    const wrapper = await mountPage([user({ role: 'NhanVien', createdAt: undefined })])
    expect(wrapper.vm.getInitials('Nguyễn Văn An')).toBe('NV')
    expect(wrapper.vm.getInitials('')).toBe('?')
    expect(wrapper.vm.getRoleLabel('QuanLy')).toBe('Quản lý')
    expect(wrapper.vm.getRoleLabel('Unknown')).toBe('Unknown')
    expect(wrapper.vm.getAvatarColor('AB')).toBeTruthy()
    expect(wrapper.vm.getRoleBadgeClass('Admin')).toBe('admin')
    expect(wrapper.vm.getRoleBadgeClass('BaoVe')).toBe('guard')
    expect(wrapper.vm.getRoleBadgeClass('Anything')).toBe('staff')
    expect(wrapper.vm.formatDate(undefined)).toBe('-')
    expect(typeof wrapper.vm.formatDate('2026-01-05T00:00:00Z')).toBe('string')
  })
})

describe('UserManagement scope management', () => {
  async function openScope(users = [user()]) {
    userApi.getAll.mockResolvedValue({ data: users })
    employeeApi.getAll.mockResolvedValue({ data: [] })
    userApi.getOperationalScopeReference.mockResolvedValue(scopeRef)
    userApi.getOperationalScopes.mockResolvedValue({ data: [] })
    userApi.getUserGateAccess.mockResolvedValue({
      data: { gates: [{ gateId: 10, gateName: 'Cổng chính', location: 'Cổng A', defaultAllowed: true, accessMode: 'inherit', effectiveAllowed: true }] },
    })
    const wrapper = mount(UserManagement, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.find('tbody .icon-btn').trigger('click')
    await flushPromises()
    return wrapper
  }

  it('opens scope modal with permission overrides and gate access', async () => {
    const wrapper = await openScope()
    expect(wrapper.vm.showScopeModal).toBe(true)
    expect(userApi.getOperationalScopeReference).toHaveBeenCalled()
    expect(userApi.getOperationalScopes).toHaveBeenCalledWith(1)
    expect(userApi.getUserGateAccess).toHaveBeenCalledWith(1)
    expect(wrapper.vm.permissionOverrides.length).toBe(2)
    expect(wrapper.text()).toContain('Đang dùng quyền mặc định')
  })

  it('switches to gates tab and sets gate modes', async () => {
    const wrapper = await openScope()
    const tabs = wrapper.findAll('.tab-btn')
    await tabs[1].trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Quyền qua cổng riêng')
    const gate = wrapper.vm.gateAccessItems[0]
    wrapper.vm.setGateMode(gate, 'allow')
    expect(gate.accessMode).toBe('allow')
    expect(gate.effectiveAllowed).toBe(true)
    wrapper.vm.setGateMode(gate, 'deny')
    expect(gate.accessMode).toBe('deny')
    expect(gate.effectiveAllowed).toBe(false)
    wrapper.vm.setGateMode(gate, 'inherit')
    expect(gate.effectiveAllowed).toBe(true)
    expect(wrapper.vm.describeGateMode(wrapper.vm.gateAccessItems[0])).toContain('mặc định')
  })

  it('describes gate modes for allow, deny and default off', async () => {
    const wrapper = await openScope()
    expect(wrapper.vm.describeGateMode({ accessMode: 'allow' })).toContain('mở thêm quyền')
    expect(wrapper.vm.describeGateMode({ accessMode: 'deny' })).toContain('bị chặn riêng')
    expect(wrapper.vm.describeGateMode({ accessMode: 'inherit', defaultAllowed: false })).toContain('KHÔNG được qua')
  })

  it('shows the admin zone lock note for admin users', async () => {
    const wrapper = await openScope([user({ role: 'Admin' })])
    await wrapper.findAll('.tab-btn')[1].trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Admin luôn được qua mọi cổng')
  })

  it('adds and removes scope detail rows', async () => {
    const wrapper = await openScope()
    wrapper.vm.addScopeRow()
    wrapper.vm.addScopeRow()
    expect(wrapper.vm.scopeItems.length).toBe(2)
    expect(wrapper.vm.mapScopeToUi().taskKey).toBe('')
    wrapper.vm.removeScopeRow(0)
    expect(wrapper.vm.scopeItems.length).toBe(1)
  })

  it('maps and describes access modes', async () => {
    const wrapper = await openScope()
    const mapped = wrapper.vm.mapScopeToUi({ taskKey: 'users', siteId: 3, gateId: 4, laneId: 5, securityZoneId: 6, canView: true, canManage: false, note: 'x' })
    expect(mapped.siteId).toBe('3')
    expect(mapped.gateId).toBe('4')
    expect(mapped.laneId).toBe('5')
    expect(mapped.securityZoneId).toBe('6')
    expect(mapped.canManage).toBe(false)
    expect(mapped.note).toBe('x')
    expect(wrapper.vm.describeAccessMode('allow')).toContain('mở thêm quyền')
    expect(wrapper.vm.describeAccessMode('deny')).toContain('bị chặn riêng')
    expect(wrapper.vm.describeAccessMode('inherit')).toContain('mặc định')
  })

  it('shows empty scope state when there are no detail rows', async () => {
    const wrapper = await openScope()
    expect(wrapper.text()).toContain('Chưa có dòng giới hạn')
  })

  it('saves scopes through step-up and confirms', async () => {
    const wrapper = await openScope()
    wrapper.vm.addScopeRow()
    wrapper.vm.scopeItems[0].taskKey = 'users'
    wrapper.vm.scopeItems[0].siteId = '1'
    wrapper.vm.permissionOverrides[0].accessMode = 'allow'
    wrapper.vm.saveScopes()
    expect(wrapper.vm.stepUpVisible).toBe(true)
    wrapper.vm.onStepUpCancelled()
    expect(wrapper.vm.stepUpVisible).toBe(false)

    wrapper.vm.saveScopes()
    wrapper.vm.gateAccessItems[0].accessMode = 'allow'
    wrapper.vm.permissionOverrides[0].accessMode = 'inherit'
    userApi.replaceOperationalScopes.mockResolvedValue({})
    userApi.replaceUserGateAccess.mockResolvedValue({})
    const payload = wrapper.vm.buildGatePayload()
    expect(payload).toEqual([{ gateId: 10, accessMode: 'allow' }])
    await wrapper.vm.onStepUpConfirmed({ sessionId: 'sess' })
    await flushPromises()
    expect(securityApi.enterpriseApi.setStepUpSession).toHaveBeenCalledWith('sess')
    expect(userApi.replaceOperationalScopes).toHaveBeenCalledTimes(1)
    expect(userApi.replaceUserGateAccess).toHaveBeenCalledTimes(1)
    expect(wrapper.vm.showScopeModal).toBe(false)
  })

  it('cleans up the outside-click listener on unmount', async () => {
    const removeSpy = vi.spyOn(document, 'removeEventListener')
    const wrapper = await openScope()
    wrapper.unmount()
    expect(removeSpy).toHaveBeenCalledWith('click', wrapper.vm.handleClickOutside)
  })

  it('closes the scope modal and resets state', async () => {
    const wrapper = await openScope()
    await wrapper.findAll('.btn-secondary').find((b) => b.text().includes('Đóng')).trigger('click')
    expect(wrapper.vm.showScopeModal).toBe(false)
    expect(wrapper.vm.scopeTarget).toBe(null)
  })

  it('handles scope load errors gracefully', async () => {
    userApi.getAll.mockResolvedValue({ data: [user()] })
    employeeApi.getAll.mockResolvedValue({ data: [] })
    userApi.getOperationalScopeReference.mockRejectedValue({ response: { data: { message: 'Ref lỗi' } } })
    const wrapper = mount(UserManagement, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.find('tbody .icon-btn').trigger('click')
    await flushPromises()
    expect(wrapper.vm.scopeError).toBe('Ref lỗi')
  })

  it('handles gate access load error', async () => {
    userApi.getAll.mockResolvedValue({ data: [user()] })
    employeeApi.getAll.mockResolvedValue({ data: [] })
    userApi.getOperationalScopeReference.mockResolvedValue(scopeRef)
    userApi.getOperationalScopes.mockResolvedValue({ data: [] })
    userApi.getUserGateAccess.mockRejectedValue({ response: { data: { message: 'Gate lỗi' } } })
    const wrapper = mount(UserManagement, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.find('tbody .icon-btn').trigger('click')
    await flushPromises()
    expect(wrapper.vm.gateAccessError).toBe('Gate lỗi')
  })

  it('renders gate data from an override scope fetch', async () => {
    userApi.getAll.mockResolvedValue({ data: [user()] })
    employeeApi.getAll.mockResolvedValue({ data: [] })
    userApi.getOperationalScopeReference.mockResolvedValue(scopeRef)
    userApi.getOperationalScopes.mockResolvedValue({ data: [{ taskKey: 'users', canView: true }] })
    userApi.getUserGateAccess.mockResolvedValue({ data: { gates: [] } })
    const wrapper = mount(UserManagement, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.find('tbody .icon-btn').trigger('click')
    await flushPromises()
    const usersOvr = wrapper.vm.permissionOverrides.find((p) => p.taskKey === 'users')
    expect(usersOvr.accessMode).toBe('allow')
  })

  it('maps a deny override scope', async () => {
    userApi.getAll.mockResolvedValue({ data: [user()] })
    employeeApi.getAll.mockResolvedValue({ data: [] })
    userApi.getOperationalScopeReference.mockResolvedValue(scopeRef)
    userApi.getOperationalScopes.mockResolvedValue({ data: [{ taskKey: 'users', canView: false, canManage: false }] })
    userApi.getUserGateAccess.mockResolvedValue({ data: { gates: [] } })
    const wrapper = mount(UserManagement, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.find('tbody .icon-btn').trigger('click')
    await flushPromises()
    const usersOvr = wrapper.vm.permissionOverrides.find((p) => p.taskKey === 'users')
    expect(usersOvr.accessMode).toBe('deny')
  })

  it('defaults permission override access when no override exists', async () => {
    userApi.getAll.mockResolvedValue({ data: [user({ role: 'Admin' })] })
    employeeApi.getAll.mockResolvedValue({ data: [] })
    userApi.getOperationalScopeReference.mockResolvedValue(scopeRef)
    userApi.getOperationalScopes.mockResolvedValue({ data: [] })
    userApi.getUserGateAccess.mockResolvedValue({ data: { gates: [] } })
    const wrapper = mount(UserManagement, { global: { stubs: sharedStubs } })
    await flushPromises()
    await wrapper.find('tbody .icon-btn').trigger('click')
    await flushPromises()
    const dashboard = wrapper.vm.permissionOverrides.find((p) => p.taskKey === 'dashboard')
    expect(dashboard.defaultAllowed).toBe(true)
  })
})
