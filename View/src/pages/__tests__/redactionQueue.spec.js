import { flushPromises, mount } from '@vue/test-utils'
import { reactive } from 'vue'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const route = reactive({ query: {} })
const replace = vi.fn()
const api = vi.hoisted(() => ({
  getRedactionRequests: vi.fn(),
  approveRedaction: vi.fn(),
  verifyRedaction: vi.fn(),
  performRedaction: vi.fn(),
}))
vi.mock('vue-router', () => ({ useRoute: () => route, useRouter: () => ({ replace }) }))
vi.mock('../../services/enterpriseSecurityApi', () => ({ enterpriseApi: api }))
vi.mock('../../composables/useToasts', () => ({ useToasts: () => ({ success: vi.fn(), error: vi.fn() }) }))

import RedactionQueue from '../RedactionQueue.vue'

const stubs = { RouterLink: true, Teleport: true }

function pending(row = {}) {
  return { redactionRequestId: 9, evidenceItemId: 4, privacyLabel: 'Biometric', reason: 'Che mặt', status: 'PendingApproval', ...row }
}

beforeEach(() => {
  route.query = {}
  replace.mockReset()
  api.getRedactionRequests.mockReset()
  api.approveRedaction.mockReset()
  api.verifyRedaction.mockReset()
  api.performRedaction.mockReset()
  api.getRedactionRequests.mockResolvedValue({ data: [pending()] })
})

describe('RedactionQueue', () => {
  it('loads redactions on mount and applies query status', async () => {
    route.query = { status: 'Approved' }
    const wrapper = mount(RedactionQueue, { global: { stubs } })
    await flushPromises()
    expect(api.getRedactionRequests).toHaveBeenCalledWith({ status: 'Approved' })
    expect(wrapper.text()).toContain('Yêu cầu')
  })

  it('commits a status filter to the router', async () => {
    const wrapper = mount(RedactionQueue, { global: { stubs } })
    await flushPromises()
    wrapper.vm.statusFilter = 'Verified'
    wrapper.vm.commitFilter()
    expect(replace).toHaveBeenCalledWith({ query: { status: 'Verified' } })
    wrapper.vm.statusFilter = ''
    wrapper.vm.commitFilter()
    expect(replace).toHaveBeenCalledWith({ query: { status: undefined } })
  })

  it('reloads when route query changes', async () => {
    const wrapper = mount(RedactionQueue, { global: { stubs } })
    await flushPromises()
    const before = api.getRedactionRequests.mock.calls.length
    route.query = { status: 'Performed' }
    await flushPromises()
    expect(api.getRedactionRequests.mock.calls.length).toBeGreaterThan(before)
  })

  it('sets permissionDenied on 403', async () => {
    api.getRedactionRequests.mockRejectedValue({ response: { status: 403 } })
    const wrapper = mount(RedactionQueue, { global: { stubs } })
    await flushPromises()
    expect(wrapper.vm.permissionDenied).toBe(true)
    expect(wrapper.vm.loadError).toBe('')
  })

  it('sets loadError on failure', async () => {
    api.getRedactionRequests.mockRejectedValue({ response: { data: { message: 'boom' } } })
    const wrapper = mount(RedactionQueue, { global: { stubs } })
    await flushPromises()
    expect(wrapper.vm.loadError).toBe('boom')
  })

  it('requests approval and confirms it', async () => {
    api.approveRedaction.mockResolvedValue({ data: {} })
    const wrapper = mount(RedactionQueue, { global: { stubs } })
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Phê duyệt').trigger('click')
    expect(wrapper.vm.actionKind).toBe('approve')
    expect(wrapper.text()).toContain('Phê duyệt yêu cầu redaction?')
    await wrapper.vm.confirmAction()
    await flushPromises()
    expect(api.approveRedaction).toHaveBeenCalledWith(9, {})
    expect(wrapper.vm.actionTarget).toBeNull()
  })

  it('handles confirmAction error', async () => {
    api.approveRedaction.mockRejectedValue({ response: { data: { message: 'deny' } } })
    const wrapper = mount(RedactionQueue, { global: { stubs } })
    await flushPromises()
    wrapper.vm.requestAction('approve', pending())
    await wrapper.vm.confirmAction()
    await flushPromises()
  })

  it('opens and performs redaction with a valid reference', async () => {
    api.performRedaction.mockResolvedValue({ data: {} })
    const wrapper = mount(RedactionQueue, { global: { stubs } })
    await flushPromises()
    api.getRedactionRequests.mockResolvedValue({ data: [pending({ status: 'Approved' })] })
    // use Approved row and performTarget via openPerform
    wrapper.vm.openPerform(pending({ redactionRequestId: 12 }))
    wrapper.vm.storageReference = 'evidence/redacted/x'
    await wrapper.vm.perform()
    await flushPromises()
    expect(api.performRedaction).toHaveBeenCalledWith(12, { redactedStorageReference: 'evidence/redacted/x' })
    expect(wrapper.vm.performTarget).toBeNull()
  })

  it('performs with empty reference returns early on validation', async () => {
    const wrapper = mount(RedactionQueue, { global: { stubs } })
    await flushPromises()
    wrapper.vm.openPerform(pending())
    await wrapper.vm.perform()
    expect(api.performRedaction).not.toHaveBeenCalled()
  })

  it('shows actionError on perform failure', async () => {
    api.performRedaction.mockRejectedValue({ response: { data: { message: 'nope' } } })
    const wrapper = mount(RedactionQueue, { global: { stubs } })
    await flushPromises()
    wrapper.vm.openPerform(pending({ redactionRequestId: 5 }))
    wrapper.vm.storageReference = 'some/ref'
    await wrapper.vm.perform()
    await flushPromises()
    expect(wrapper.vm.actionError).toBe('nope')
  })

  it('verifies a performed redaction', async () => {
    api.verifyRedaction.mockResolvedValue({ data: {} })
    const wrapper = mount(RedactionQueue, { global: { stubs } })
    await flushPromises()
    wrapper.vm.requestAction('verify', pending({ redactionRequestId: 7 }))
    await wrapper.vm.confirmAction()
    await flushPromises()
    expect(api.verifyRedaction).toHaveBeenCalledWith(7, {})
  })

  it('confirmAction early-returns without a target', async () => {
    const wrapper = mount(RedactionQueue, { global: { stubs } })
    await flushPromises()
    wrapper.vm.actionTarget = null
    await wrapper.vm.confirmAction()
    expect(api.approveRedaction).not.toHaveBeenCalled()
    expect(api.verifyRedaction).not.toHaveBeenCalled()
  })

  it('label helpers produce expected outputs', () => {
    const wrapper = mount(RedactionQueue, { global: { stubs } })
    expect(wrapper.vm.statusSemantic('Verified')).toBe('success')
    expect(wrapper.vm.statusSemantic('Performed')).toBe('info')
    expect(wrapper.vm.statusSemantic('Approved')).toBe('info')
    expect(wrapper.vm.statusSemantic('PendingApproval')).toBe('warning')
    expect(wrapper.vm.statusSemantic('Unknown')).toBe('neutral')
    expect(wrapper.vm.statusLabel('Verified')).toBe('Đã xác minh')
    expect(wrapper.vm.statusLabel('Performed')).toBe('Đã thực thi')
    expect(wrapper.vm.statusLabel('Unknown')).toBe('Unknown')
    expect(wrapper.vm.privacyLabel('Biometric')).toBe('Sinh trắc học')
    expect(wrapper.vm.privacyLabel('')).toBe('Không phân loại')
  })

  it('renders table rows for approved and performed statuses', async () => {
    api.getRedactionRequests.mockResolvedValue({
      data: [
        pending({ redactionRequestId: 1, status: 'Approved', privacyLabel: 'Other' }),
        pending({ redactionRequestId: 2, status: 'Performed', privacyLabel: 'Biometric' }),
        pending({ redactionRequestId: 3, status: 'Verified' }),
        pending({ redactionRequestId: 4, status: 'Weird' }),
      ],
    })
    const wrapper = mount(RedactionQueue, { global: { stubs } })
    await flushPromises()
    expect(wrapper.findAll('tbody tr').length).toBe(4)
  })
})
