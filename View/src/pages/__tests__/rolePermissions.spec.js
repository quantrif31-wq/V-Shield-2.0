import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../stores/auth', () => ({ fetchUser: vi.fn() }))
vi.mock('../../services/userApi', () => ({
  getOperationalScopeReference: vi.fn(),
  replaceRolePermissions: vi.fn(),
  getGateAccessReference: vi.fn(),
  replaceRoleGatePermissions: vi.fn(),
}))
vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: { setStepUpSession: vi.fn() },
}))

const authStore = await import('../../stores/auth')
const userApi = await import('../../services/userApi')
const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const RolePermissions = (await import('../RolePermissions.vue')).default

const StepUpStub = {
  name: 'StepUpModal',
  template: '<div class="stepup-stub" />',
  props: ['visible'],
  emits: ['cancel', 'confirmed'],
}

const taskCatalog = [
  { taskKey: 'dashboard_view', label: 'Bảng điều khiển', routes: ['/dashboard'] },
  { taskKey: 'gates_view', label: 'Cổng', routes: ['/gates'] },
]
const tasksByRole = { Admin: ['dashboard_view'], QuanLy: [] }

const gates = [
  { gateId: 'g1', gateName: 'Cổng A', location: 'Lối vào' },
  { gateId: 'g2', gateName: 'Cổng B', location: null },
]
const gatesByRole = { Admin: ['g1'], QuanLy: [] }

beforeEach(() => {
  vi.clearAllMocks()
  authStore.fetchUser.mockResolvedValue(true)
  userApi.getOperationalScopeReference.mockResolvedValue({ data: { taskCatalog, tasksByRole } })
  userApi.replaceRolePermissions.mockResolvedValue({ data: {} })
  userApi.getGateAccessReference.mockResolvedValue({ data: { gates, gatesByRole } })
  userApi.replaceRoleGatePermissions.mockResolvedValue({ data: {} })
  enterpriseApi.setStepUpSession.mockImplementation(() => {})
  vi.spyOn(window, 'confirm').mockReturnValue(true)
})

afterEach(() => {
  vi.restoreAllMocks()
})

const mountComponent = () =>
  mount(RolePermissions, { global: { stubs: { StepUpModal: StepUpStub } } })

const stepUp = (wrapper) => wrapper.findComponent(StepUpStub)

describe('RolePermissions', () => {
  it('loads the role reference on mount and builds the matrix', async () => {
    const wrapper = mountComponent()
    await flushPromises()
    expect(userApi.getOperationalScopeReference).toHaveBeenCalled()
    expect(wrapper.vm.loading).toBe(false)
    expect(wrapper.vm.taskCatalog).toHaveLength(2)
    expect(wrapper.vm.draftPermissions.Admin.dashboard_view).toBe(true)
    expect(wrapper.vm.draftPermissions.Admin.gates_view).toBe(false)
    expect(wrapper.vm.draftPermissions.QuanLy.dashboard_view).toBe(false)
    expect(wrapper.text()).toContain('Bảng điều khiển')
    expect(wrapper.text()).toContain('Admin')
    expect(wrapper.text()).toContain('Quản lý')
  })

  it('shows load error and retries', async () => {
    userApi.getOperationalScopeReference.mockRejectedValueOnce({ response: { data: { message: 'server down' } } })
    const wrapper = mountComponent()
    await flushPromises()
    expect(wrapper.vm.loadError).toBe('server down')
    expect(wrapper.text()).toContain('server down')
    await wrapper.find('button.btn-primary').trigger('click')
    await flushPromises()
    expect(userApi.getOperationalScopeReference).toHaveBeenCalledTimes(2)
    expect(wrapper.vm.loadError).toBe('')
  })

  it('uses a generic message when error has no response data', async () => {
    userApi.getOperationalScopeReference.mockRejectedValueOnce(new Error('x'))
    const wrapper = mountComponent()
    await flushPromises()
    expect(wrapper.vm.loadError).toBe('Không thể tải dữ liệu quyền theo vai trò')
  })

  it('exposes role helper functions', async () => {
    const wrapper = mountComponent()
    await flushPromises()
    expect(wrapper.vm.getRoleLabel('BaoVe')).toBe('Bảo vệ')
    expect(wrapper.vm.getRoleLabel('Unknown')).toBe('Unknown')
    expect(wrapper.vm.getRoleBadgeClass('Admin')).toBe('admin')
    expect(wrapper.vm.getRoleBadgeClass('NhanVien')).toBe('staff')
    expect(wrapper.vm.getRoleBadgeClass('NhanSu')).toBe('staff')
    expect(wrapper.vm.getRoleBadgeClass('Whatever')).toBe('staff')
  })

  it('switches to gates tab, reloads draft and clears feedback on checkbox change', async () => {
    const wrapper = mountComponent()
    await flushPromises()
    wrapper.vm.feedbackMessage = 'some'
    await wrapper.findAll('.tab-btn')[1].trigger('click')
    await flushPromises()
    expect(wrapper.vm.activeTab).toBe('gates')
    expect(wrapper.vm.gateLoading).toBe(false)
    expect(userApi.getGateAccessReference).toHaveBeenCalled()
    expect(wrapper.vm.gateDraftPermissions.Admin.g1).toBe(true)
    expect(wrapper.vm.gateDraftPermissions.QuanLy.g1).toBe(false)
    expect(wrapper.vm.feedbackMessage).toBe('')
    expect(wrapper.text()).toContain('Cổng A')

    await wrapper.find('.matrix-toggle input').trigger('change')
    expect(wrapper.vm.feedbackMessage).toBe('')

    await wrapper.findAll('.tab-btn')[0].trigger('click')
    expect(wrapper.vm.activeTab).toBe('tasks')
  })

  it('reloads the tasks draft via the undo button', async () => {
    const wrapper = mountComponent()
    await flushPromises()
    wrapper.vm.draftPermissions.Admin.dashboard_view = false
    await wrapper.findAll('.header-actions button')[0].trigger('click')
    expect(wrapper.vm.draftPermissions.Admin.dashboard_view).toBe(true)
  })

  it('reloads the gates draft on the gates tab', async () => {
    const wrapper = mountComponent()
    await flushPromises()
    await wrapper.findAll('.tab-btn')[1].trigger('click')
    await flushPromises()
    wrapper.vm.gateDraftPermissions.Admin.g1 = false
    await wrapper.findAll('.header-actions button')[0].trigger('click')
    expect(wrapper.vm.gateDraftPermissions.Admin.g1).toBe(true)
  })

  it('handles gate reference load error', async () => {
    userApi.getGateAccessReference.mockRejectedValueOnce({ response: { data: { message: 'gates down' } } })
    const wrapper = mountComponent()
    await flushPromises()
    await wrapper.findAll('.tab-btn')[1].trigger('click')
    await flushPromises()
    expect(wrapper.vm.gateError).toBe('gates down')
  })

  it('uses generic gate error message', async () => {
    userApi.getGateAccessReference.mockRejectedValueOnce(new Error('x'))
    const wrapper = mountComponent()
    await flushPromises()
    await wrapper.findAll('.tab-btn')[1].trigger('click')
    await flushPromises()
    expect(wrapper.vm.gateError).toBe('Không thể tải dữ liệu quyền qua cổng')
  })

  it('saves permissions after step-up confirmation', async () => {
    const wrapper = mountComponent()
    await flushPromises()
    await wrapper.findAll('.header-actions button')[2].trigger('click')
    expect(wrapper.vm.stepUpVisible).toBe(true)
    expect(wrapper.vm.pendingAction).toBe('save')
    await stepUp(wrapper).vm.$emit('confirmed', { sessionId: 'tok' })
    await flushPromises()
    expect(enterpriseApi.setStepUpSession).toHaveBeenCalledWith('tok')
    expect(enterpriseApi.setStepUpSession).toHaveBeenLastCalledWith(null)
    expect(userApi.replaceRolePermissions).toHaveBeenCalledTimes(1)
    expect(wrapper.vm.feedbackTone).toBe('success')
    expect(wrapper.vm.feedbackMessage).toBe('Đã lưu ma trận quyền theo vai trò.')
  })

  it('handles save permissions failure', async () => {
    userApi.replaceRolePermissions.mockRejectedValueOnce({ response: { data: { message: 'nope' } } })
    const wrapper = mountComponent()
    await flushPromises()
    await wrapper.findAll('.header-actions button')[2].trigger('click')
    await stepUp(wrapper).vm.$emit('confirmed', { sessionId: 'tok' })
    await flushPromises()
    expect(wrapper.vm.feedbackTone).toBe('error')
    expect(wrapper.vm.feedbackMessage).toBe('nope')
  })

  it('handles save permissions error without response data', async () => {
    userApi.replaceRolePermissions.mockRejectedValueOnce(new Error('x'))
    const wrapper = mountComponent()
    await flushPromises()
    await wrapper.findAll('.header-actions button')[2].trigger('click')
    await stepUp(wrapper).vm.$emit('confirmed', { sessionId: 'tok' })
    await flushPromises()
    expect(wrapper.vm.feedbackMessage).toBe('Không thể lưu ma trận quyền')
  })

  it('saves gate permissions when on gates tab', async () => {
    const wrapper = mountComponent()
    await flushPromises()
    await wrapper.findAll('.tab-btn')[1].trigger('click')
    await flushPromises()
    await wrapper.findAll('.header-actions button')[2].trigger('click')
    expect(wrapper.vm.pendingAction).toBe('save')
    await stepUp(wrapper).vm.$emit('confirmed', {})
    await flushPromises()
    expect(userApi.replaceRoleGatePermissions).toHaveBeenCalledTimes(1)
    expect(wrapper.vm.feedbackMessage).toBe('Đã lưu ma trận quyền qua cổng theo vai trò.')
  })

  it('handles gate save failure', async () => {
    userApi.replaceRoleGatePermissions.mockRejectedValueOnce(new Error('x'))
    const wrapper = mountComponent()
    await flushPromises()
    await wrapper.findAll('.tab-btn')[1].trigger('click')
    await flushPromises()
    await wrapper.findAll('.header-actions button')[2].trigger('click')
    await stepUp(wrapper).vm.$emit('confirmed', {})
    await flushPromises()
    expect(wrapper.vm.feedbackMessage).toBe('Không thể lưu ma trận quyền qua cổng')
  })

  it('resets permissions to defaults after confirmation', async () => {
    const wrapper = mountComponent()
    await flushPromises()
    await wrapper.findAll('.header-actions button')[1].trigger('click')
    expect(window.confirm).toHaveBeenCalled()
    expect(wrapper.vm.pendingAction).toBe('reset')
    await stepUp(wrapper).vm.$emit('confirmed', { sessionId: 'tok' })
    await flushPromises()
    expect(userApi.replaceRolePermissions).toHaveBeenCalledWith([])
    expect(wrapper.vm.feedbackMessage).toBe('Đã khôi phục ma trận quyền mặc định.')
    expect(enterpriseApi.setStepUpSession).toHaveBeenCalled()
  })

  it('aborts reset when confirmation is cancelled', async () => {
    window.confirm.mockReturnValue(false)
    const wrapper = mountComponent()
    await flushPromises()
    await wrapper.findAll('.header-actions button')[1].trigger('click')
    expect(wrapper.vm.stepUpVisible).toBe(false)
    expect(window.confirm).toHaveBeenCalled()
  })

  it('handles reset failure', async () => {
    userApi.replaceRolePermissions.mockRejectedValueOnce(new Error('x'))
    const wrapper = mountComponent()
    await flushPromises()
    await wrapper.findAll('.header-actions button')[1].trigger('click')
    await stepUp(wrapper).vm.$emit('confirmed', {})
    await flushPromises()
    expect(wrapper.vm.feedbackMessage).toBe('Không thể khôi phục ma trận mặc định')
  })

  it('resets gate defaults, handling failure generically', async () => {
    const wrapper = mountComponent()
    await flushPromises()
    await wrapper.findAll('.tab-btn')[1].trigger('click')
    await flushPromises()
    await wrapper.findAll('.header-actions button')[1].trigger('click')
    expect(window.confirm).toHaveBeenCalled()
    expect(wrapper.vm.pendingAction).toBe('reset')
    await stepUp(wrapper).vm.$emit('confirmed', {})
    await flushPromises()
    expect(userApi.replaceRoleGatePermissions).toHaveBeenCalledWith([])
    expect(wrapper.vm.feedbackMessage).toBe('Đã khôi phục ma trận quyền qua cổng mặc định.')
  })

  it('cancels step-up flow', async () => {
    const wrapper = mountComponent()
    await flushPromises()
    await wrapper.findAll('.header-actions button')[2].trigger('click')
    expect(wrapper.vm.stepUpVisible).toBe(true)
    await stepUp(wrapper).vm.$emit('cancel')
    expect(wrapper.vm.stepUpVisible).toBe(false)
  })

  it('clears feedback from the tasks matrix toggle', async () => {
    const wrapper = mountComponent()
    await flushPromises()
    wrapper.vm.feedbackMessage = 'clear me'
    const tasksToggle = wrapper.find('.bento-card.matrix-card .matrix-toggle input')
    await tasksToggle.trigger('change')
    expect(wrapper.vm.feedbackMessage).toBe('')
  })

  it('aborts gate reset when confirmation is cancelled', async () => {
    window.confirm.mockReturnValue(false)
    const wrapper = mountComponent()
    await flushPromises()
    await wrapper.findAll('.tab-btn')[1].trigger('click')
    await flushPromises()
    await wrapper.findAll('.header-actions button')[1].trigger('click')
    expect(wrapper.vm.stepUpVisible).toBe(false)
    expect(window.confirm).toHaveBeenCalled()
  })

  it('handles gate reset failure', async () => {
    userApi.replaceRoleGatePermissions.mockRejectedValueOnce(new Error('x'))
    const wrapper = mountComponent()
    await flushPromises()
    await wrapper.findAll('.tab-btn')[1].trigger('click')
    await flushPromises()
    await wrapper.findAll('.header-actions button')[1].trigger('click')
    await stepUp(wrapper).vm.$emit('confirmed', {})
    await flushPromises()
    expect(wrapper.vm.feedbackTone).toBe('error')
    expect(wrapper.vm.feedbackMessage).toBe('Không thể khôi phục ma trận mặc định')
  })
})
