import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/identityApi', () => ({
  identityApi: {
    getOverview: vi.fn(),
    getProviders: vi.fn(),
    upsertProvider: vi.fn(),
    oidcChallenge: vi.fn(),
    importUsers: vi.fn(),
    importGroups: vi.fn(),
    offboardEmployee: vi.fn(),
  },
}))

const identityApi = (await import('../../services/identityApi')).identityApi
const IdentityManagement = (await import('../IdentityManagement.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  identityApi.getOverview.mockResolvedValue({ data: {} })
  identityApi.getProviders.mockResolvedValue({ data: [] })
})

describe('IdentityManagement provider', () => {
  it('creates an identity provider through the modal', async () => {
    const wrapper = mount(IdentityManagement)
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text().includes('Thêm nhà cung cấp')).trigger('click')
    const inputs = wrapper.findAll('.modal-content input')
    await inputs[0].setValue('Azure AD')
    await inputs[1].setValue('https://login.microsoftonline.com')
    identityApi.upsertProvider.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Tạo mới').trigger('click')
    await flushPromises()
    expect(identityApi.upsertProvider).toHaveBeenCalledWith(expect.objectContaining({ name: 'Azure AD', authority: 'https://login.microsoftonline.com' }))
  })
})
