import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

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

beforeEach(() => vi.clearAllMocks())
afterEach(() => vi.unstubAllGlobals())

const overviewData = {
  providers: 2,
  enabledProviders: 1,
  mappings: 5,
  activeMappings: 4,
  activeEmployees: 100,
  suspendedEmployees: 3,
  terminatedEmployees: 10,
  recertificationCampaigns: 2,
}

describe('IdentityManagement', () => {
  it('loads overview metrics and providers', async () => {
    identityApi.getOverview.mockResolvedValue({ data: overviewData })
    identityApi.getProviders.mockResolvedValue({ data: [{ externalIdentityProviderId: 1, name: 'Azure AD', protocol: 'OIDC', authority: 'https://login.microsoftonline.com', isEnabled: true, clientId: 'cid' }] })
    const wrapper = mount(IdentityManagement)
    await flushPromises()
    expect(wrapper.text()).toContain('Azure AD')
    expect(wrapper.text()).toContain('Đã bật')
  })

  it('imports users from raw text', async () => {
    identityApi.getOverview.mockResolvedValue({ data: overviewData })
    identityApi.getProviders.mockResolvedValue({ data: [{ externalIdentityProviderId: 1, name: 'Azure AD', isEnabled: true }] })
    identityApi.importUsers.mockResolvedValue({ data: { results: [{ externalSubject: 'sub-1', username: 'john', status: 'Imported' }] } })
    const wrapper = mount(IdentityManagement)
    await flushPromises()

    await wrapper.findAll('.workspace-tabs button').find((b) => b.text().includes('Nhập người dùng')).trigger('click')
    await wrapper.findAll('select')[0].setValue('1')
    await wrapper.find('textarea').setValue('sub-1,john,John,john@example.com,LeTan')
    await wrapper.findAll('button').find((b) => b.text() === 'Nhập người dùng').trigger('click')
    await flushPromises()
    expect(identityApi.importUsers).toHaveBeenCalledWith(1, expect.arrayContaining([expect.objectContaining({ externalSubject: 'sub-1', username: 'john' })]))
    expect(wrapper.text()).toContain('Imported')
  })

  it('offboards an employee after confirmation', async () => {
    vi.spyOn(window, 'alert').mockImplementation(() => {})
    identityApi.getOverview.mockResolvedValue({ data: overviewData })
    identityApi.getProviders.mockResolvedValue({ data: [] })
    identityApi.offboardEmployee.mockResolvedValue({ data: { tokenVersion: 3 } })
    const wrapper = mount(IdentityManagement)
    await flushPromises()

    await wrapper.findAll('button').find((b) => b.text() === 'Ngừng cấp phát nhân viên').trigger('click')
    await wrapper.find('input[type="number"]').setValue(7)
    await wrapper.findAll('button').find((b) => b.text().includes('Xác nhận ngừng cấp phát')).trigger('click')
    await flushPromises()
    expect(identityApi.offboardEmployee).toHaveBeenCalledWith(7, expect.anything())
  })
})
