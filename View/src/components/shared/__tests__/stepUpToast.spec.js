import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../../services/enterpriseSecurityApi', () => ({
  enterpriseApi: {
    stepUpStart: vi.fn(),
    stepUpVerify: vi.fn(),
    setStepUpSession: vi.fn(),
  },
}))

const enterpriseApi = (await import('../../../services/enterpriseSecurityApi')).enterpriseApi
const StepUpModal = (await import('../StepUpModal.vue')).default
const AuditReceiptToast = (await import('../AuditReceiptToast.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  vi.useFakeTimers()
  document.body.innerHTML = ''
})
afterEach(() => {
  vi.runOnlyPendingTimers()
  vi.useRealTimers()
  document.body.innerHTML = ''
})

describe('StepUpModal', () => {
  it('starts the step-up flow with a reason', async () => {
    enterpriseApi.stepUpStart.mockResolvedValue({ data: { sessionId: 'sess-1', requiresMfa: true } })
    const wrapper = mount(StepUpModal, { props: { visible: true, action: 'DeleteEvidence', requireMfa: false } })
    wrapper.vm.reason = 'Cần xóa bằng chứng sai'
    await wrapper.vm.startStepUp()
    expect(enterpriseApi.stepUpStart).toHaveBeenCalledWith('DeleteEvidence', 'Cần xóa bằng chứng sai')
    expect(wrapper.vm.stepUpStarted).toBe(true)
    expect(wrapper.vm.requireMfaCode).toBe(true)
  })

  it('verifies the step-up and emits confirmed', async () => {
    enterpriseApi.stepUpStart.mockResolvedValue({ data: { sessionId: 'sess-1', requiresMfa: false } })
    enterpriseApi.stepUpVerify.mockResolvedValue({})
    const wrapper = mount(StepUpModal, { props: { visible: true, requireMfa: false } })
    wrapper.vm.reason = 'Lý do'
    await wrapper.vm.startStepUp()
    wrapper.vm.password = 'pw123'
    await wrapper.vm.verifyStepUp()
    expect(enterpriseApi.stepUpVerify).toHaveBeenCalledWith('sess-1', 'pw123', undefined)
    expect(wrapper.emitted('confirmed')[0]).toEqual([{ sessionId: 'sess-1', reason: 'Lý do' }])
    expect(enterpriseApi.setStepUpSession).toHaveBeenCalledWith('sess-1')
  })

  it('cancels the flow and emits cancel', () => {
    const wrapper = mount(StepUpModal, { props: { visible: true } })
    wrapper.vm.handleCancel()
    expect(wrapper.emitted('cancel')).toBeTruthy()
    expect(enterpriseApi.setStepUpSession).toHaveBeenCalledWith(null)
  })
})

describe('AuditReceiptToast', () => {
  it('renders receipt details when visible', async () => {
    const wrapper = mount(AuditReceiptToast, {
      props: { visible: true, title: 'Đã ghi nhận', message: 'ok', receiptId: 'RC-123', showCopy: true },
    })
    await flushPromises()
    expect(document.body.textContent).toContain('RC-123')
    expect(document.body.textContent).toContain('Đã ghi nhận')
  })

  it('emits dismiss on close', async () => {
    const wrapper = mount(AuditReceiptToast, { props: { visible: true } })
    await flushPromises()
    document.body.querySelector('.art-close').click()
    expect(wrapper.emitted('dismiss')).toBeTruthy()
  })

  it('auto-dismisses after the configured duration', async () => {
    const wrapper = mount(AuditReceiptToast, { props: { visible: false, autoDismissMs: 5000 } })
    await wrapper.setProps({ visible: true })
    await flushPromises()
    expect(wrapper.emitted('dismiss')).toBeFalsy()
    vi.advanceTimersByTime(5000)
    expect(wrapper.emitted('dismiss')).toBeTruthy()
  })

  it('copies the receipt id to the clipboard', async () => {
    vi.stubGlobal('navigator', { clipboard: { writeText: vi.fn().mockResolvedValue() } })
    const wrapper = mount(AuditReceiptToast, { props: { visible: true, receiptId: 'RC-999', showCopy: true } })
    await flushPromises()
    document.body.querySelector('.art-copy-btn').click()
    await flushPromises()
    expect(navigator.clipboard.writeText).toHaveBeenCalledWith('RC-999')
    expect(wrapper.vm.copied).toBe(true)
  })
})
