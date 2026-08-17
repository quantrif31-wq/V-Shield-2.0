import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../stores/auth', () => ({ fetchUser: vi.fn() }))
vi.mock('../../services/userApi', () => ({
  getOperationalScopeReference: vi.fn(),
  replaceRolePermissions: vi.fn(),
  getGateAccessReference: vi.fn(),
  replaceRoleGatePermissions: vi.fn(),
}))
vi.mock('../../services/enterpriseSecurityApi', () => ({ enterpriseApi: {} }))

const authStore = await import('../../stores/auth')
const userApi = await import('../../services/userApi')
const RolePermissions = (await import('../RolePermissions.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  authStore.fetchUser.mockResolvedValue(true)
})

describe('RolePermissions', () => {
  it('loads the role reference on mount', async () => {
    userApi.getOperationalScopeReference.mockResolvedValue({ data: { taskCatalog: [], tasksByRole: {} } })
    const wrapper = mount(RolePermissions, { global: { stubs: { StepUpModal: true } } })
    await flushPromises()
    expect(userApi.getOperationalScopeReference).toHaveBeenCalled()
    expect(wrapper.exists()).toBe(true)
  })
})
