import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    getEvidenceItems: vi.fn(),
    getEvidenceOverview: vi.fn(),
    getRetentionPolicies: vi.fn(),
    getLegalHolds: vi.fn(),
    createRetentionPolicy: vi.fn(),
    updateRetentionPolicy: vi.fn(),
    dryRunRetention: vi.fn(),
    purgeEvidence: vi.fn(),
    releaseLegalHold: vi.fn(),
    createLegalHold: vi.fn(),
    getChainOfCustody: vi.fn(),
    getEvidenceAccessLogs: vi.fn(),
    getEvidenceCollections: vi.fn(),
    getEvidenceCollectionDetail: vi.fn(),
    createEvidenceCollection: vi.fn(),
    addEvidenceCollectionItem: vi.fn(),
    closeEvidenceCollection: vi.fn(),
    addCustodyEntry: vi.fn(),
    createExportRequest: vi.fn(),
    createRedactionRequest: vi.fn(),
    createEvidenceItem: vi.fn(),
    verifyEvidenceHash: vi.fn(),
  },
}))

const enterpriseApi = (await import('../../services/enterpriseSecurityApi')).enterpriseApi
const EvidenceRepository = (await import('../EvidenceRepository.vue')).default

const evItem = {
  evidenceItemId: 1,
  evidenceType: 'Video',
  sourceType: 'Camera',
  sourceReference: 'ref-001',
  privacyLabel: 'Internal',
  retentionCategory: 'Default',
  hashSha256: 'abc1234567890',
  isLegalHold: false,
  createdAtUtc: '2026-08-01T00:00:00Z',
}

const evPolicy = {
  retentionPolicyId: 10,
  name: 'Video Sự cố',
  evidenceType: 'Video',
  retentionCategory: 'Sự cố',
  retentionDays: 365,
  purgeMode: 'ReviewRequired',
  isActive: true,
}

const legalHold = {
  legalHoldId: 20,
  evidenceItemId: 1,
  reason: 'Điều tra sự cố',
  appliedAtUtc: '2026-08-01T00:00:00Z',
  status: 'Active',
}

function clickText(cfg) {
  const btn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.trim().includes(cfg.text))
  if (!btn) throw new Error('button not found: ' + cfg.text)
  btn.dispatchEvent(new MouseEvent('click', { bubbles: true }))
}

beforeEach(() => {
  vi.clearAllMocks()
  vi.spyOn(window, 'alert').mockImplementation(() => {})
  vi.spyOn(window, 'confirm').mockReturnValue(true)
  vi.spyOn(window, 'prompt').mockReturnValue('lý do')
  enterpriseApi.getEvidenceOverview.mockResolvedValue({ data: { totalItems: 10 } })
  enterpriseApi.getEvidenceItems.mockResolvedValue({ data: { items: [evItem], total: 1 } })
  enterpriseApi.getRetentionPolicies.mockResolvedValue({ data: [evPolicy] })
  enterpriseApi.getLegalHolds.mockResolvedValue({ data: { items: [legalHold] } })
  enterpriseApi.getChainOfCustody.mockResolvedValue({ data: [] })
  enterpriseApi.getEvidenceCollections.mockResolvedValue({ data: [] })
  enterpriseApi.getEvidenceCollectionDetail.mockResolvedValue({ data: { items: [] } })
})

afterEach(() => {
  document.body.innerHTML = ''
  window.alert.mockRestore()
  window.confirm.mockRestore()
  window.prompt.mockRestore()
})

describe('EvidenceRepository items list', () => {
  it('loads items and renders badges', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    expect(wrapper.find('tbody').text()).toContain('Video')
    expect(wrapper.find('tbody').text()).toContain('Internal')
    wrapper.unmount()
  })

  it('renders privacy classes across labels', async () => {
    expect.assertions(6)
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    expect(wrapper.vm.privacyClass('Biometric')).toBe('badge-danger')
    expect(wrapper.vm.privacyClass('PersonalIdentity')).toBe('badge-danger')
    expect(wrapper.vm.privacyClass('SensitiveSite')).toBe('badge-warn')
    expect(wrapper.vm.privacyClass('VisitorDocument')).toBe('badge-primary')
    expect(wrapper.vm.privacyClass('Internal')).toBe('badge-info')
    expect(wrapper.vm.privacyClass('Public')).toBe('badge-info')
    wrapper.unmount()
  })

  it('refetches on evidence type filter change', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    const selects = wrapper.findAll('select')
    await selects[0].setValue('Document')
    await flushPromises()
    expect(enterpriseApi.getEvidenceItems).toHaveBeenLastCalledWith(
      expect.objectContaining({ evidenceType: 'Document' }),
    )
    wrapper.unmount()
  })

  it('refetches on privacy and legal hold filter change', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    const selects = wrapper.findAll('select')
    await selects[1].setValue('Biometric')
    await flushPromises()
    await selects[2].setValue('true')
    await flushPromises()
    expect(enterpriseApi.getEvidenceItems).toHaveBeenLastCalledWith(
      expect.objectContaining({ privacyLabel: 'Biometric', isLegalHold: true }),
    )
    wrapper.unmount()
  })

  it('loads items with a legal hold flag false filter', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    const selects = wrapper.findAll('select')
    await selects[2].setValue('false')
    await flushPromises()
    expect(enterpriseApi.getEvidenceItems).toHaveBeenLastCalledWith(
      expect.objectContaining({ isLegalHold: false }),
    )
    wrapper.unmount()
  })

  it('paginates forward and backward with disabled states', async () => {
    enterpriseApi.getEvidenceItems.mockResolvedValue({ data: { items: [evItem], total: 100 } })
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    const buttons = wrapper.findAll('.page-btn')
    await buttons[1].trigger('click')
    await flushPromises()
    expect(wrapper.vm.page).toBe(2)
    await wrapper.findAll('.page-btn')[0].trigger('click')
    await flushPromises()
    expect(wrapper.vm.page).toBe(1)
    wrapper.unmount()
  })

  it('shows loading state initially and empty state with no items', async () => {
    enterpriseApi.getEvidenceItems.mockResolvedValue({ data: { items: [], total: 0 } })
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    expect(wrapper.text()).toContain('Chưa có mục bằng chứng')
    wrapper.unmount()
  })

  it('handles loadItems api error', async () => {
    enterpriseApi.getEvidenceItems.mockRejectedValue(new Error('boom'))
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    expect(wrapper.vm.items).toEqual([])
    wrapper.unmount()
  })
})

describe('EvidenceRepository governance', () => {
  async function goGovernance(wrapper) {
    await wrapper.findAll('button').find((b) => b.text().includes('Lưu giữ & Khóa')).trigger('click')
    await flushPromises()
  }

  it('switches to governance and renders policies and holds', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await goGovernance(wrapper)
    expect(wrapper.text()).toContain('Video Sự cố')
    expect(wrapper.text()).toContain('1 đang khóa')
    wrapper.unmount()
  })

  it('renders empty states for policies and holds', async () => {
    enterpriseApi.getRetentionPolicies.mockResolvedValue({ data: [] })
    enterpriseApi.getLegalHolds.mockResolvedValue({ data: [] })
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await goGovernance(wrapper)
    expect(wrapper.text()).toContain('Chưa có chính sách lưu giữ')
    expect(wrapper.text()).toContain('Không có khóa pháp lý')
    wrapper.unmount()
  })

  it('handles governance load errors', async () => {
    enterpriseApi.getRetentionPolicies.mockRejectedValue(new Error('p'))
    enterpriseApi.getLegalHolds.mockRejectedValue(new Error('h'))
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    expect(wrapper.vm.retentionPolicies).toEqual([])
    expect(wrapper.vm.legalHolds).toEqual([])
    wrapper.unmount()
  })

  it('submits a retention policy successfully', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await goGovernance(wrapper)
    await wrapper.findAll('button').find((b) => b.text().includes('Chính sách lưu giữ')).trigger('click')
    await flushPromises()
    wrapper.vm.policyForm = { name: 'P1', evidenceType: 'Video', retentionCategory: 'Sự cố', retentionDays: 90, purgeMode: 'Auto', isActive: false }
    enterpriseApi.createRetentionPolicy.mockResolvedValue({})
    await wrapper.vm.submitRetentionPolicy()
    await flushPromises()
    expect(enterpriseApi.createRetentionPolicy).toHaveBeenCalled()
    expect(wrapper.vm.governanceMessage).toContain('Đã tạo')
    wrapper.unmount()
  })

  it('returns early when policy name or days missing', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await goGovernance(wrapper)
    wrapper.vm.policyForm = { name: '', retentionDays: 90 }
    await wrapper.vm.submitRetentionPolicy()
    expect(enterpriseApi.createRetentionPolicy).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('handles retention policy submit error', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await goGovernance(wrapper)
    wrapper.vm.policyForm = { name: 'P1', retentionDays: 90 }
    enterpriseApi.createRetentionPolicy.mockRejectedValue({ response: { data: { message: 'nope' } } })
    await wrapper.vm.submitRetentionPolicy()
    expect(wrapper.vm.governanceMessageType).toBe('error')
    expect(wrapper.vm.governanceMessage).toContain('nope')
    wrapper.unmount()
  })

  it('toggles a retention policy and errors', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await goGovernance(wrapper)
    enterpriseApi.updateRetentionPolicy.mockResolvedValue({})
    await wrapper.vm.toggleRetentionPolicy(evPolicy, false)
    expect(enterpriseApi.updateRetentionPolicy).toHaveBeenCalledWith(10, { isActive: false })
    enterpriseApi.updateRetentionPolicy.mockRejectedValue({ response: { data: { message: 'x' } } })
    await wrapper.vm.toggleRetentionPolicy(evPolicy, true)
    expect(wrapper.vm.governanceMessageType).toBe('error')
    wrapper.unmount()
  })

  it('renders policy active toggle button and click', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await goGovernance(wrapper)
    enterpriseApi.updateRetentionPolicy.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text().trim() === 'Ngừng').trigger('click')
    await flushPromises()
    expect(enterpriseApi.updateRetentionPolicy).toHaveBeenCalled()
    wrapper.unmount()
  })

  it('runs retention dry run successfully', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await goGovernance(wrapper)
    enterpriseApi.dryRunRetention.mockResolvedValue({ data: { candidates: [{ evidenceItemId: 1 }] } })
    await wrapper.vm.runRetentionDryRun()
    expect(wrapper.vm.dryRunResult).toEqual({ candidates: [{ evidenceItemId: 1 }] })
    wrapper.unmount()
  })

  it('handles dry run error', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await goGovernance(wrapper)
    enterpriseApi.dryRunRetention.mockRejectedValue(new Error('fail'))
    await wrapper.vm.runRetentionDryRun()
    expect(window.alert).toHaveBeenCalledWith('Chạy thử thất bại')
    wrapper.unmount()
  })

  it('opens dry run modal via the run button', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await goGovernance(wrapper)
    enterpriseApi.dryRunRetention.mockResolvedValue({ data: { candidates: [{ evidenceItemId: 1 }] } })
    await wrapper.findAll('button').find((b) => b.text().trim() === 'Chạy thử').trigger('click')
    await flushPromises()
    expect(wrapper.vm.dryRunResult).toBeTruthy()
    wrapper.unmount()
  })

  it('confirm purge returns early when confirm declined', async () => {
    window.confirm.mockReturnValue(false)
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await wrapper.vm.confirmGovernancePurge()
    expect(enterpriseApi.purgeEvidence).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('confirm purge errors on empty candidates', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    wrapper.vm.dryRunResult = { candidates: [] }
    await wrapper.vm.confirmGovernancePurge()
    expect(wrapper.vm.governanceMessageType).toBe('error')
    wrapper.unmount()
  })

  it('confirm purge succeeds', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    wrapper.vm.dryRunResult = { candidates: [{ evidenceItemId: 1 }] }
    enterpriseApi.purgeEvidence.mockResolvedValue({})
    await wrapper.vm.confirmGovernancePurge()
    expect(enterpriseApi.purgeEvidence).toHaveBeenCalled()
    expect(wrapper.vm.governanceMessage).toContain('Đã purge')
    wrapper.unmount()
  })

  it('confirm purge handles error and clears result via purge confirm button', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await goGovernance(wrapper)
    enterpriseApi.dryRunRetention.mockResolvedValue({ data: { candidates: [{ evidenceItemId: 1 }] } })
    await wrapper.findAll('button').find((b) => b.text().trim() === 'Chạy thử').trigger('click')
    await flushPromises()
    enterpriseApi.purgeEvidence.mockRejectedValue(new Error('purge fail'))
    await wrapper.findAll('button').find((b) => b.text().includes('Xóa các mục đã liệt kê')).trigger('click')
    await flushPromises()
    expect(window.alert).toHaveBeenCalledWith('Xóa dữ liệu thất bại')
    wrapper.unmount()
  })

  it('formats hold scope across variants', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    expect(wrapper.vm.formatHoldScope({ evidenceItemId: 1 })).toBe('Evidence #1')
    expect(wrapper.vm.formatHoldScope({ evidenceCollectionId: 3 })).toBe('Collection #3')
    expect(wrapper.vm.formatHoldScope({})).toBe('Unknown')
    wrapper.unmount()
  })

  it('releases hold from governance and errors', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await goGovernance(wrapper)
    enterpriseApi.releaseLegalHold.mockResolvedValue({})
    await wrapper.vm.releaseHoldFromGovernance(legalHold)
    expect(enterpriseApi.releaseLegalHold).toHaveBeenCalledWith(20, { reason: 'lý do' })
    enterpriseApi.releaseLegalHold.mockRejectedValue({ response: { data: { message: 'e' } } })
    await wrapper.vm.releaseHoldFromGovernance(legalHold)
    expect(wrapper.vm.governanceMessageType).toBe('error')
    wrapper.unmount()
  })

  it('releases hold without reason returns early and updates detail isLegalHold', async () => {
    window.prompt.mockReturnValue('')
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    wrapper.vm.detail = { ...evItem, isLegalHold: true }
    await wrapper.vm.releaseHoldFromGovernance(legalHold)
    expect(enterpriseApi.releaseLegalHold).not.toHaveBeenCalled()
    wrapper.vm.detail = { ...evItem, isLegalHold: true }
    window.prompt.mockReturnValue('lý do')
    enterpriseApi.releaseLegalHold.mockResolvedValue({})
    await wrapper.vm.releaseHoldFromGovernance({ ...legalHold, evidenceItemId: 1 })
    expect(wrapper.vm.detail.isLegalHold).toBe(false)
    wrapper.unmount()
  })

  it('releases hold via governance button', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await goGovernance(wrapper)
    enterpriseApi.releaseLegalHold.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text().trim() === 'Gỡ khóa').trigger('click')
    await flushPromises()
    expect(enterpriseApi.releaseLegalHold).toHaveBeenCalledWith(20, { reason: 'lý do' })
    wrapper.unmount()
  })
})

describe('EvidenceRepository detail drawer', () => {
  async function openDetail(wrapper) {
    await wrapper.findAll('button').find((b) => b.text() === 'Chi tiết').trigger('click')
    await flushPromises()
  }

  it('opens detail drawer and loads custody', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await openDetail(wrapper)
    expect(wrapper.vm.detail.evidenceItemId).toBe(1)
    expect(enterpriseApi.getChainOfCustody).toHaveBeenCalledWith(1)
    wrapper.unmount()
  })

  it('handles getChainOfCustody error in viewDetail', async () => {
    enterpriseApi.getChainOfCustody.mockRejectedValue(new Error('c'))
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await openDetail(wrapper)
    expect(wrapper.vm.custody).toEqual([])
    wrapper.unmount()
  })

  it('closes detail drawer', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await openDetail(wrapper)
    await wrapper.vm.closeDetail()
    expect(wrapper.vm.detail).toBeNull()
    wrapper.unmount()
  })

  it('loads custody tab and empty state', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await openDetail(wrapper)
    await wrapper.vm.loadDetailTab('custody')
    expect(wrapper.vm.custody).toEqual([])
    wrapper.unmount()
  })

  it('loads access tab with logs and empty state', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await openDetail(wrapper)
    enterpriseApi.getEvidenceAccessLogs.mockResolvedValue({ data: [{ evidenceAccessLogId: 3, userId: 'u1', action: 'view', accessedAtUtc: '2026-08-01T00:00:00Z' }] })
    await wrapper.vm.loadDetailTab('access')
    expect(wrapper.vm.accessLogs).toHaveLength(1)
    enterpriseApi.getEvidenceAccessLogs.mockResolvedValue({ data: [] })
    await wrapper.vm.loadDetailTab('access')
    expect(wrapper.vm.accessLogs).toEqual([])
    wrapper.unmount()
  })

  it('loads collections tab variants', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await openDetail(wrapper)
    enterpriseApi.getEvidenceCollections.mockResolvedValue({ data: [{ evidenceCollectionId: 4, name: 'C1' }] })
    await wrapper.vm.loadDetailTab('collections')
    expect(wrapper.vm.evidenceCollections).toHaveLength(1)
    enterpriseApi.getEvidenceCollections.mockRejectedValue(new Error('x'))
    await wrapper.vm.loadDetailTab('collections')
    wrapper.unmount()
  })

  it('bails out of loadDetailTab without detail', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    wrapper.vm.detail = null
    await wrapper.vm.loadDetailTab('overview')
    expect(enterpriseApi.getChainOfCustody).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('switches detail tabs via drawer buttons', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await openDetail(wrapper)
    const tabs = document.body.querySelectorAll('.drawer-tabs button')
    tabs[1].dispatchEvent(new MouseEvent('click', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.activeDetailTab).toBe('custody')
    tabs[2].dispatchEvent(new MouseEvent('click', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.activeDetailTab).toBe('access')
    wrapper.unmount()
  })

  it('closes detail via drawer overlay and X button', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await openDetail(wrapper)
    const close = document.body.querySelector('.btn-close')
    close.dispatchEvent(new MouseEvent('click', { bubbles: true }))
    expect(wrapper.vm.detail).toBeNull()
    wrapper.unmount()
  })

  it('verifies hash success and failure', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await openDetail(wrapper)
    enterpriseApi.verifyEvidenceHash.mockResolvedValue({ data: { message: 'OK' } })
    await wrapper.vm.verifyHash()
    expect(wrapper.vm.hashResult.valid).toBe(true)
    enterpriseApi.verifyEvidenceHash.mockRejectedValue(new Error('x'))
    await wrapper.vm.verifyHash()
    expect(wrapper.vm.hashResult.valid).toBe(false)
    wrapper.unmount()
  })

  it('submits export request with validation, success, error', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await openDetail(wrapper)
    wrapper.vm.exportForm = { recipient: '', purpose: '' }
    await wrapper.vm.submitExportRequest()
    expect(enterpriseApi.createExportRequest).not.toHaveBeenCalled()
    wrapper.vm.exportForm = { recipient: 'a@b.c', purpose: 'xét duyệt' }
    enterpriseApi.createExportRequest.mockResolvedValue({})
    await wrapper.vm.submitExportRequest()
    expect(wrapper.vm.exportResult).toContain('Yêu cầu xuất')
    wrapper.vm.exportForm = { recipient: 'a@b.c', purpose: 'x' }
    enterpriseApi.createExportRequest.mockRejectedValue({ response: { data: { message: 'e1' } } })
    await wrapper.vm.submitExportRequest()
    expect(wrapper.vm.exportError).toContain('e1')
    wrapper.unmount()
  })

  it('submits export via template button', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await openDetail(wrapper)
    const btn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Yêu cầu xuất'))
    btn.dispatchEvent(new MouseEvent('click', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.showCreateExport).toBe(true)
    wrapper.vm.exportForm = { recipient: 'a', purpose: 'b' }
    enterpriseApi.createExportRequest.mockResolvedValue({})
    const send = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Gửi yêu cầu'))
    send.dispatchEvent(new MouseEvent('click', { bubbles: true }))
    await flushPromises()
    expect(enterpriseApi.createExportRequest).toHaveBeenCalled()
    wrapper.unmount()
  })

  it('submits redaction request with validation, success, error', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await openDetail(wrapper)
    wrapper.vm.redactionForm = { privacyLabel: 'PersonalIdentity', reason: '' }
    await wrapper.vm.submitRedactionRequest()
    expect(enterpriseApi.createRedactionRequest).not.toHaveBeenCalled()
    wrapper.vm.redactionForm = { privacyLabel: 'Biometric', reason: 'che' }
    enterpriseApi.createRedactionRequest.mockResolvedValue({})
    await wrapper.vm.submitRedactionRequest()
    expect(wrapper.vm.redactResult).toContain('che dữ liệu')
    wrapper.vm.redactionForm = { privacyLabel: 'Biometric', reason: 'che' }
    enterpriseApi.createRedactionRequest.mockRejectedValue({ response: { data: { message: 'e2' } } })
    await wrapper.vm.submitRedactionRequest()
    expect(wrapper.vm.redactError).toContain('e2')
    wrapper.unmount()
  })

  it('submits redaction via template buttons', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await openDetail(wrapper)
    clickText({ text: 'Yêu cầu che dữ liệu' })
    await flushPromises()
    expect(wrapper.vm.showCreateRedaction).toBe(true)
    wrapper.vm.redactionForm = { privacyLabel: 'PersonalIdentity', reason: 'lý do' }
    enterpriseApi.createRedactionRequest.mockResolvedValue({})
    clickText({ text: 'Gửi yêu cầu' })
    await flushPromises()
    expect(enterpriseApi.createRedactionRequest).toHaveBeenCalled()
    wrapper.unmount()
  })

  it('applies legal hold with cancel and success and error', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await openDetail(wrapper)
    window.prompt.mockReturnValue('')
    await wrapper.vm.applyLegalHold()
    expect(enterpriseApi.createLegalHold).not.toHaveBeenCalled()
    window.prompt.mockReturnValue('lý do')
    enterpriseApi.createLegalHold.mockResolvedValue({})
    wrapper.vm.detail = { ...evItem, isLegalHold: false }
    await wrapper.vm.applyLegalHold()
    expect(enterpriseApi.createLegalHold).toHaveBeenCalled()
    expect(wrapper.vm.detail.isLegalHold).toBe(true)
    expect(wrapper.vm.actionSuccess).toContain('khóa pháp lý')
    enterpriseApi.createLegalHold.mockRejectedValue({ response: { data: { message: 'e3' } } })
    await wrapper.vm.applyLegalHold()
    expect(wrapper.vm.actionError).toContain('e3')
    wrapper.unmount()
  })

  it('applies legal hold via template button', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await openDetail(wrapper)
    wrapper.vm.detail = { ...evItem, isLegalHold: false }
    enterpriseApi.createLegalHold.mockResolvedValue({})
    await flushPromises()
    const applyBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Áp dụng Khóa pháp lý'))
    applyBtn.dispatchEvent(new MouseEvent('click', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.detail.isLegalHold).toBe(true)
    wrapper.unmount()
  })

  it('releases legal hold with cancel, success and no-active-hold', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await openDetail(wrapper)
    window.prompt.mockReturnValue('')
    await wrapper.vm.releaseLegalHold()
    expect(enterpriseApi.releaseLegalHold).not.toHaveBeenCalled()
    window.prompt.mockReturnValue('lý do')
    enterpriseApi.getLegalHolds.mockResolvedValue({ data: { items: [{ ...legalHold, status: 'Active' }] } })
    enterpriseApi.releaseLegalHold.mockResolvedValue({})
    wrapper.vm.detail = { ...evItem, isLegalHold: true }
    await wrapper.vm.releaseLegalHold()
    expect(wrapper.vm.detail.isLegalHold).toBe(false)
    enterpriseApi.getLegalHolds.mockResolvedValue({ data: { items: [{ ...legalHold, status: 'Released' }] } })
    await wrapper.vm.releaseLegalHold()
    expect(wrapper.vm.detail.isLegalHold).toBe(false)
    wrapper.unmount()
  })

  it('releases legal hold with error', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await openDetail(wrapper)
    window.prompt.mockReturnValue('lý do')
    enterpriseApi.getLegalHolds.mockResolvedValue({ data: { items: [{ ...legalHold, status: 'Active' }] } })
    enterpriseApi.releaseLegalHold.mockRejectedValue({ response: { data: { message: 'e4' } } })
    await wrapper.vm.releaseLegalHold()
    expect(wrapper.vm.actionError).toContain('e4')
    wrapper.unmount()
  })

  it('submits custody entry with validation, success and error', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await openDetail(wrapper)
    wrapper.vm.detail = null
    await wrapper.vm.submitCustodyEntry()
    expect(enterpriseApi.addCustodyEntry).not.toHaveBeenCalled()
    wrapper.vm.detail = evItem
    enterpriseApi.addCustodyEntry.mockResolvedValue({})
    enterpriseApi.getChainOfCustody.mockResolvedValue({ data: [{ chainOfCustodyEntryId: 1, action: 'Transferred' }] })
    await wrapper.vm.submitCustodyEntry()
    expect(enterpriseApi.addCustodyEntry).toHaveBeenCalledWith(1, expect.anything())
    expect(wrapper.vm.showAddCustody).toBe(false)
    enterpriseApi.addCustodyEntry.mockRejectedValue(new Error('cust fail'))
    await wrapper.vm.submitCustodyEntry()
    expect(window.alert).toHaveBeenCalledWith('Không thể thêm lệnh chuyển')
    wrapper.unmount()
  })

  it('opens and submits add custody via template', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await openDetail(wrapper)
    wrapper.vm.activeDetailTab = 'custody'
    await flushPromises()
    enterpriseApi.addCustodyEntry.mockResolvedValue({})
    enterpriseApi.getChainOfCustody.mockResolvedValue({ data: [] })
    const addBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Thêm lệnh chuyển'))
    addBtn.dispatchEvent(new MouseEvent('click', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.showAddCustody).toBe(true)
    const inputs = document.body.querySelectorAll('.drawer-tab-content input')
    inputs[0].value = 'Bàn giao'
    inputs[0].dispatchEvent(new Event('input', { bubbles: true }))
    inputs[1].value = 'Phòng A'
    inputs[1].dispatchEvent(new Event('input', { bubbles: true }))
    const saveBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.trim() === 'Lưu')
    saveBtn.dispatchEvent(new MouseEvent('click', { bubbles: true }))
    await flushPromises()
    expect(enterpriseApi.addCustodyEntry).toHaveBeenCalled()
    wrapper.unmount()
  })
})

describe('EvidenceRepository create item', () => {
  it('creates an evidence item successfully', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    wrapper.vm.showCreateItem = true
    await flushPromises()
    wrapper.vm.createForm = { evidenceType: 'Video', sourceType: 'Camera', sourceReference: 'r', storageReference: 's3://x', privacyLabel: 'Internal' }
    enterpriseApi.createEvidenceItem.mockResolvedValue({})
    await wrapper.vm.submitCreateItem()
    expect(enterpriseApi.createEvidenceItem).toHaveBeenCalled()
    expect(wrapper.vm.showCreateItem).toBe(false)
    wrapper.vm.createForm = { evidenceType: 'Video', sourceType: 'Camera', sourceReference: 'r', storageReference: 's', privacyLabel: 'Internal' }
    enterpriseApi.createEvidenceItem.mockRejectedValue({ response: { data: { message: 'ce' } } })
    await wrapper.vm.submitCreateItem()
    expect(wrapper.vm.createError).toContain('ce')
    wrapper.unmount()
  })

  it('returns early when create evidence type missing', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    wrapper.vm.createForm = { evidenceType: '' }
    await wrapper.vm.submitCreateItem()
    expect(enterpriseApi.createEvidenceItem).not.toHaveBeenCalled()
    wrapper.unmount()
  })

  it('creates via template button', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().trim() === '+ Bằng chứng').trigger('click')
    await flushPromises()
    expect(wrapper.vm.showCreateItem).toBe(true)
    wrapper.vm.createForm = { evidenceType: 'Log' }
    enterpriseApi.createEvidenceItem.mockResolvedValue({})
    const createBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.trim() === 'Tạo' && b.parentElement.classList.contains('modal-footer'))
    createBtn.dispatchEvent(new MouseEvent('click', { bubbles: true }))
    await flushPromises()
    expect(enterpriseApi.createEvidenceItem).toHaveBeenCalled()
    wrapper.unmount()
  })
})

describe('EvidenceRepository collections', () => {
  it('opens collections modal and loads collections', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Bộ sưu tập').trigger('click')
    await flushPromises()
    enterpriseApi.getEvidenceCollections.mockResolvedValue({ data: [{ evidenceCollectionId: 4, name: 'C1', status: 'Open', itemCount: 2 }] })
    await wrapper.vm.loadCollections()
    expect(wrapper.vm.collections).toHaveLength(1)
    wrapper.unmount()
  })

  it('handles loadCollections error', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Bộ sưu tập').trigger('click')
    await flushPromises()
    enterpriseApi.getEvidenceCollections.mockRejectedValue(new Error('x'))
    await wrapper.vm.loadCollections()
    expect(wrapper.vm.collections).toEqual([])
    wrapper.unmount()
  })

  it('shows collection detail with items and empty', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    wrapper.vm.collections = [{ evidenceCollectionId: 4, name: 'C1', status: 'Open' }]
    enterpriseApi.getEvidenceCollectionDetail.mockResolvedValue({ data: { items: [{ evidenceItemId: 1 }] } })
    await wrapper.vm.showCollectionDetail(wrapper.vm.collections[0])
    expect(wrapper.vm.collectionItems).toHaveLength(1)
    enterpriseApi.getEvidenceCollectionDetail.mockRejectedValue(new Error('x'))
    await wrapper.vm.showCollectionDetail(wrapper.vm.collections[0])
    expect(wrapper.vm.collectionItems).toEqual([])
    wrapper.unmount()
  })

  it('creates collection with validation, success and error', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    wrapper.vm.collectionForm = { name: '', description: '' }
    await wrapper.vm.createCollection()
    expect(enterpriseApi.createEvidenceCollection).not.toHaveBeenCalled()
    wrapper.vm.collectionForm = { name: 'Cnew', description: 'd' }
    enterpriseApi.createEvidenceCollection.mockResolvedValue({})
    await wrapper.vm.createCollection()
    expect(wrapper.vm.colResult).toContain('Đã tạo')
    wrapper.vm.collectionForm = { name: 'Cnew', description: '' }
    enterpriseApi.createEvidenceCollection.mockRejectedValue({ response: { data: { message: 'ce' } } })
    await wrapper.vm.createCollection()
    expect(wrapper.vm.colResult).toContain('ce')
    wrapper.unmount()
  })

  it('adds item to collection with validation, success and error', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    wrapper.vm.selectedCollection = null
    await wrapper.vm.addItemToCollection()
    expect(enterpriseApi.addEvidenceCollectionItem).not.toHaveBeenCalled()
    wrapper.vm.selectedCollection = { evidenceCollectionId: 4 }
    wrapper.vm.addToCollectionForm = { evidenceItemId: null }
    await wrapper.vm.addItemToCollection()
    expect(enterpriseApi.addEvidenceCollectionItem).not.toHaveBeenCalled()
    wrapper.vm.addToCollectionForm = { evidenceItemId: 9 }
    enterpriseApi.addEvidenceCollectionItem.mockResolvedValue({})
    enterpriseApi.getEvidenceCollectionDetail.mockResolvedValue({ data: { items: [] } })
    await wrapper.vm.addItemToCollection()
    expect(enterpriseApi.addEvidenceCollectionItem).toHaveBeenCalledWith(4, { evidenceItemId: 9 })
    wrapper.vm.addToCollectionForm = { evidenceItemId: 9 }
    enterpriseApi.addEvidenceCollectionItem.mockRejectedValue(new Error('x'))
    await wrapper.vm.addItemToCollection()
    expect(window.alert).toHaveBeenCalledWith('Không thể thêm mục')
    wrapper.unmount()
  })

  it('closes collection with confirm false, success and error', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    wrapper.vm.selectedCollection = null
    await wrapper.vm.closeCollection()
    expect(enterpriseApi.closeEvidenceCollection).not.toHaveBeenCalled()
    wrapper.vm.selectedCollection = { evidenceCollectionId: 4, name: 'C1' }
    window.confirm.mockReturnValue(false)
    await wrapper.vm.closeCollection()
    expect(enterpriseApi.closeEvidenceCollection).not.toHaveBeenCalled()
    window.confirm.mockReturnValue(true)
    enterpriseApi.closeEvidenceCollection.mockResolvedValue({})
    await wrapper.vm.closeCollection()
    expect(wrapper.vm.selectedCollection.status).toBe('Closed')
    window.confirm.mockReturnValue(true)
    enterpriseApi.closeEvidenceCollection.mockRejectedValue(new Error('x'))
    await wrapper.vm.closeCollection()
    expect(window.alert).toHaveBeenCalledWith('Không thể đóng bộ sưu tập')
    wrapper.unmount()
  })

  it('drives collections modal interactions via template', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text() === 'Bộ sưu tập').trigger('click')
    await flushPromises()
    enterpriseApi.getEvidenceCollections.mockResolvedValue({ data: [{ evidenceCollectionId: 4, name: 'C1', status: 'Open', itemCount: 1, description: 'd' }] })
    await wrapper.vm.loadCollections()
    await flushPromises()
    const card = [...document.body.querySelectorAll('.collection-card')].find((c) => c.textContent.includes('C1'))
    enterpriseApi.getEvidenceCollectionDetail.mockResolvedValue({ data: { items: [{ evidenceItemId: 1 }] } })
    card.dispatchEvent(new MouseEvent('click', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.selectedCollection.name).toBe('C1')
    wrapper.unmount()
  })

  it('creates a new collection via template buttons', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().trim() === 'Bộ sưu tập').trigger('click')
    await flushPromises()
    enterpriseApi.getEvidenceCollections.mockResolvedValue({ data: [{ evidenceCollectionId: 4, name: 'C1', status: 'Open', itemCount: 1 }] })
    await wrapper.vm.loadCollections()
    await flushPromises()
    await [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Bộ sưu tập mới')).dispatchEvent(new MouseEvent('click', { bubbles: true }))
    await flushPromises()
    wrapper.vm.collectionForm = { name: 'Cnew', description: 'd' }
    enterpriseApi.createEvidenceCollection.mockResolvedValue({})
    await [...document.body.querySelectorAll('button')].find((b) => b.textContent.trim() === 'Tạo' && b.parentElement.classList.contains('chip-row')).dispatchEvent(new MouseEvent('click', { bubbles: true }))
    await flushPromises()
    expect(enterpriseApi.createEvidenceCollection).toHaveBeenCalled()
    wrapper.unmount()
  })
})

describe('EvidenceRepository refresh and tabs', () => {
  it('refreshes items view', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    wrapper.vm.activeRepoTab = 'items'
    await wrapper.vm.refreshCurrentView()
    expect(enterpriseApi.getEvidenceItems).toHaveBeenCalled()
    wrapper.unmount()
  })

  it('refreshes governance view', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    wrapper.vm.activeRepoTab = 'governance'
    await wrapper.vm.refreshCurrentView()
    expect(enterpriseApi.getRetentionPolicies).toHaveBeenCalled()
    wrapper.unmount()
  })

  it('switches tabs and renders detail umbrella', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await wrapper.vm.viewDetail(evItem)
    wrapper.vm.activeDetailTab = 'overview'
    wrapper.vm.hashResult = { valid: true, message: 'OK' }
    await flushPromises()
    expect(document.body.textContent).toContain('Xác minh Hash')
    wrapper.unmount()
  })
})

describe('EvidenceRepository template inline coverage', () => {
  function clickEl(el) {
    el.dispatchEvent(new MouseEvent('click', { bubbles: true }))
  }
  function bodyButton(text) {
    const b = [...document.body.querySelectorAll('button')].find((x) => x.textContent.includes(text))
    if (!b) throw new Error('button not found: ' + text)
    return b
  }
  function panelBy(text) {
    const p = [...document.body.querySelectorAll('.modal-panel')].find((x) => x.textContent.includes(text))
    if (!p) throw new Error('panel not found: ' + text)
    return p
  }
  async function goGovernance2(wrapper) {
    await wrapper.findAll('button').find((b) => b.text().includes('Lưu giữ & Khóa pháp lý')).trigger('click')
    await flushPromises()
  }

  it('drives repo tabs and policy composer inputs', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('Lưu giữ & Khóa pháp lý')).trigger('click')
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().includes('+ Chính sách lưu giữ')).trigger('click')
    await flushPromises()
    const c = wrapper.find('.policy-composer')
    await c.findAll('input').at(0).setValue('Policy1')
    await c.findAll('select').at(0).setValue('Video')
    await c.findAll('input').at(1).setValue('Sự cố')
    await c.findAll('input').at(2).setValue('90')
    await c.findAll('select').at(1).setValue('Auto')
    await c.findAll('input').at(3).setValue(false)
    await flushPromises()
    expect(wrapper.vm.policyForm.name).toBe('Policy1')
    expect(wrapper.vm.policyForm.evidenceType).toBe('Video')
    expect(wrapper.vm.policyForm.retentionCategory).toBe('Sự cố')
    expect(wrapper.vm.policyForm.purgeMode).toBe('Auto')
    expect(wrapper.vm.policyForm.isActive).toBe(false)
    await wrapper.findAll('button').find((b) => b.text().includes('Mục bằng chứng')).trigger('click')
    await flushPromises()
    expect(wrapper.vm.activeRepoTab).toBe('items')
    wrapper.unmount()
  })

  it('closes dry run modal via overlay self and button', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    await goGovernance2(wrapper)
    wrapper.vm.dryRunResult = { candidates: [{ evidenceItemId: 1 }] }
    await flushPromises()
    await wrapper.find('.modal-overlay').trigger('click')
    await flushPromises()
    expect(wrapper.vm.dryRunResult).toBe(null)
    wrapper.vm.dryRunResult = { candidates: [{ evidenceItemId: 1 }] }
    await flushPromises()
    await wrapper.findAll('button').find((b) => b.text().trim() === 'Đóng').trigger('click')
    await flushPromises()
    expect(wrapper.vm.dryRunResult).toBe(null)
    wrapper.unmount()
  })

  it('drives custody and access and collections drawer tabs', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    enterpriseApi.getChainOfCustody.mockResolvedValue({ data: [{ chainOfCustodyEntryId: 1, action: 'Transferred', actorUserId: 'u', fromCustodian: 'a', toCustodian: 'b', note: 'n', createdAtUtc: '2026-01-01T00:00:00Z' }] })
    enterpriseApi.getEvidenceAccessLogs.mockResolvedValue({ data: [{ evidenceAccessLogId: 1, userId: 'u', action: 'View', accessedAtUtc: '2026-01-01T00:00:00Z' }] })
    enterpriseApi.getEvidenceCollections.mockResolvedValue({ data: { items: [{ evidenceCollectionId: 9, name: 'C9', status: 'Open', itemCount: 2 }] } })
    await wrapper.vm.viewDetail(evItem)
    await flushPromises()
    clickEl([...document.body.querySelectorAll('.drawer-tabs button')].find((b) => b.textContent.includes('Chuỗi bàn giao')))
    await flushPromises()
    expect(document.body.textContent).toContain('Transferred')
    expect(wrapper.vm.custody).toHaveLength(1)
    wrapper.vm.showAddCustody = true
    await flushPromises()
    const note = document.querySelector('.drawer-panel textarea')
    note.value = 'ghi chú'
    note.dispatchEvent(new Event('input', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.custodyForm.note).toBe('ghi chú')
    clickEl([...document.body.querySelectorAll('.chip-row button')].find((b) => b.textContent.trim() === 'Hủy'))
    await flushPromises()
    expect(wrapper.vm.showAddCustody).toBe(false)
    clickEl([...document.body.querySelectorAll('.drawer-tabs button')].find((b) => b.textContent.includes('Nhật ký truy cập')))
    await flushPromises()
    expect(wrapper.vm.accessLogs).toHaveLength(1)
    expect(document.body.textContent).toContain('View')
    clickEl([...document.body.querySelectorAll('.drawer-tabs button')].find((b) => b.textContent.includes('Bộ sưu tập')))
    await flushPromises()
    expect(wrapper.vm.evidenceCollections).toHaveLength(1)
    expect(document.body.textContent).toContain('C9')
    clickEl([...document.body.querySelectorAll('.drawer-panel .collection-card')].find((c) => c.textContent.includes('C9')))
    await flushPromises()
    expect(wrapper.vm.selectedCollection.name).toBe('C9')
    wrapper.unmount()
  })

  it('drives export modal overlays and inputs', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    wrapper.vm.detail = null
    wrapper.vm.showCreateExport = true
    await flushPromises()
    clickEl(panelBy('Yêu cầu xuất').closest('.modal-overlay'))
    await flushPromises()
    expect(wrapper.vm.showCreateExport).toBe(false)
    wrapper.vm.showCreateExport = true
    await flushPromises()
    clickEl(panelBy('Yêu cầu xuất').querySelector('.btn-close'))
    await flushPromises()
    expect(wrapper.vm.showCreateExport).toBe(false)
    wrapper.vm.showCreateExport = true
    await flushPromises()
    const p = panelBy('Yêu cầu xuất')
    const rec = p.querySelector('input[placeholder*="Email"]')
    rec.value = 'a@b.c'
    rec.dispatchEvent(new Event('input', { bubbles: true }))
    const pur = p.querySelector('textarea[placeholder*="Vì sao cần xuất"]')
    pur.value = 'purpose'
    pur.dispatchEvent(new Event('input', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.exportForm.recipient).toBe('a@b.c')
    expect(wrapper.vm.exportForm.purpose).toBe('purpose')
    clickEl([...p.querySelectorAll('.modal-footer button')].find((b) => b.textContent.trim() === 'Hủy'))
    await flushPromises()
    expect(wrapper.vm.showCreateExport).toBe(false)
    wrapper.unmount()
  })

  it('drives redaction modal overlays and inputs', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    wrapper.vm.detail = null
    wrapper.vm.showCreateRedaction = true
    await flushPromises()
    clickEl(panelBy('Yêu cầu che dữ liệu').closest('.modal-overlay'))
    await flushPromises()
    expect(wrapper.vm.showCreateRedaction).toBe(false)
    wrapper.vm.showCreateRedaction = true
    await flushPromises()
    clickEl(panelBy('Yêu cầu che dữ liệu').querySelector('.btn-close'))
    await flushPromises()
    expect(wrapper.vm.showCreateRedaction).toBe(false)
    wrapper.vm.showCreateRedaction = true
    await flushPromises()
    const p = panelBy('Yêu cầu che dữ liệu')
    const sel = p.querySelector('select')
    sel.value = 'Biometric'
    sel.dispatchEvent(new Event('change', { bubbles: true }))
    const reason = p.querySelector('textarea[placeholder*="che dữ liệu"]')
    reason.value = 'r'
    reason.dispatchEvent(new Event('input', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.redactionForm.privacyLabel).toBe('Biometric')
    expect(wrapper.vm.redactionForm.reason).toBe('r')
    clickEl([...p.querySelectorAll('.modal-footer button')].find((b) => b.textContent.trim() === 'Hủy'))
    await flushPromises()
    expect(wrapper.vm.showCreateRedaction).toBe(false)
    wrapper.unmount()
  })

  it('drives create item modal overlays and inputs', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    wrapper.vm.showCreateItem = true
    await flushPromises()
    clickEl(panelBy('Tạo mục bằng chứng').closest('.modal-overlay'))
    await flushPromises()
    expect(wrapper.vm.showCreateItem).toBe(false)
    wrapper.vm.showCreateItem = true
    await flushPromises()
    clickEl(panelBy('Tạo mục bằng chứng').querySelector('.btn-close'))
    await flushPromises()
    expect(wrapper.vm.showCreateItem).toBe(false)
    wrapper.vm.showCreateItem = true
    await flushPromises()
    const p = panelBy('Tạo mục bằng chứng')
    const sel = p.querySelectorAll('select')
    sel[0].value = 'Image'
    sel[0].dispatchEvent(new Event('change', { bubbles: true }))
    sel[1].value = 'Biometric'
    sel[1].dispatchEvent(new Event('change', { bubbles: true }))
    const srcType = p.querySelector('input[placeholder*="Camera"]')
    srcType.value = 'Camera'
    srcType.dispatchEvent(new Event('input', { bubbles: true }))
    const srcRef = p.querySelector('input[placeholder*="Tham chiếu tùy chọn"]')
    srcRef.value = 'r'
    srcRef.dispatchEvent(new Event('input', { bubbles: true }))
    const storage = p.querySelector('input[placeholder*="S3"]')
    storage.value = 's3://x'
    storage.dispatchEvent(new Event('input', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.createForm.evidenceType).toBe('Image')
    expect(wrapper.vm.createForm.sourceType).toBe('Camera')
    expect(wrapper.vm.createForm.sourceReference).toBe('r')
    expect(wrapper.vm.createForm.storageReference).toBe('s3://x')
    expect(wrapper.vm.createForm.privacyLabel).toBe('Biometric')
    clickEl([...p.querySelectorAll('.modal-footer button')].find((b) => b.textContent.trim() === 'Hủy'))
    await flushPromises()
    expect(wrapper.vm.showCreateItem).toBe(false)
    wrapper.unmount()
  })

  it('drives collections modal overlays and new collection form', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    wrapper.vm.showCollections = true
    await flushPromises()
    clickEl(panelBy('Bộ sưu tập bằng chứng').closest('.modal-overlay'))
    await flushPromises()
    expect(wrapper.vm.showCollections).toBe(false)
    wrapper.vm.showCollections = true
    await flushPromises()
    clickEl(panelBy('Bộ sưu tập bằng chứng').querySelector('.btn-close'))
    await flushPromises()
    expect(wrapper.vm.showCollections).toBe(false)
    wrapper.vm.showCollections = true
    wrapper.vm.showNewCollection = true
    await flushPromises()
    const p = panelBy('Bộ sưu tập bằng chứng')
    const nameInput = p.querySelector('input[placeholder*="Vụ việc"]')
    nameInput.value = 'Cnew'
    nameInput.dispatchEvent(new Event('input', { bubbles: true }))
    const desc = p.querySelector('textarea')
    desc.value = 'd'
    desc.dispatchEvent(new Event('input', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.collectionForm.name).toBe('Cnew')
    expect(wrapper.vm.collectionForm.description).toBe('d')
    clickEl([...p.querySelectorAll('.chip-row button')].find((b) => b.textContent.trim() === 'Hủy'))
    await flushPromises()
    expect(wrapper.vm.showNewCollection).toBe(false)
    wrapper.unmount()
  })

  it('drives add-to-collection inputs in collection detail', async () => {
    const wrapper = mount(EvidenceRepository)
    await flushPromises()
    wrapper.vm.showCollections = true
    wrapper.vm.selectedCollection = { evidenceCollectionId: 4, name: 'C1', status: 'Open' }
    wrapper.vm.collectionItems = []
    await flushPromises()
    clickEl(bodyButton('Thêm mục'))
    await flushPromises()
    expect(wrapper.vm.showAddToCollection).toBe(true)
    const num = document.body.querySelector('.modal-overlay input[type="number"]')
    num.value = '9'
    num.dispatchEvent(new Event('input', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.addToCollectionForm.evidenceItemId).toBe(9)
    wrapper.unmount()
  })
})
