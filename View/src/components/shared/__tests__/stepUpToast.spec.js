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

  it('requires a reason before starting the step-up', async () => {
    const wrapper = mount(StepUpModal, { props: { visible: true } })
    await wrapper.vm.startStepUp()
    expect(wrapper.vm.error).toContain('lý do')
    expect(enterpriseApi.stepUpStart).not.toHaveBeenCalled()
    expect(wrapper.vm.stepUpStarted).toBe(false)
  })

  it('surfaces an error when starting the step-up fails', async () => {
    enterpriseApi.stepUpStart.mockRejectedValue({ response: { data: { message: 'Bắt đầu thất bại' } } })
    const wrapper = mount(StepUpModal, { props: { visible: true } })
    wrapper.vm.reason = 'Lý do'
    await wrapper.vm.startStepUp()
    expect(wrapper.vm.error).toContain('Bắt đầu thất bại')
    expect(wrapper.vm.loading).toBe(false)
    expect(wrapper.vm.stepUpStarted).toBe(false)
  })

  it('requires a password before verifying', async () => {
    const wrapper = mount(StepUpModal, { props: { visible: true } })
    wrapper.vm.reason = 'Lý do'
    await wrapper.vm.startStepUp()
    await wrapper.vm.verifyStepUp()
    expect(wrapper.vm.error).toContain('mật khẩu')
    expect(enterpriseApi.stepUpVerify).not.toHaveBeenCalled()
  })

  it('surfaces an error and clears session when verification fails', async () => {
    enterpriseApi.stepUpStart.mockResolvedValue({ data: { sessionId: 'sess-x', requiresMfa: false } })
    enterpriseApi.stepUpVerify.mockRejectedValue(new Error('Sai mật khẩu'))
    const wrapper = mount(StepUpModal, { props: { visible: true, requireMfa: false } })
    wrapper.vm.reason = 'Lý do'
    await wrapper.vm.startStepUp()
    wrapper.vm.password = 'wrong'
    await wrapper.vm.verifyStepUp()
    expect(wrapper.vm.error).toContain('Sai mật khẩu')
    expect(enterpriseApi.setStepUpSession).toHaveBeenCalledWith(null)
    expect(wrapper.emitted('confirmed')).toBeFalsy()
  })

  it('renders the severity badge and steps through the flow in the template', async () => {
    const wrapper = mount(StepUpModal, {
      props: { visible: true, severity: 'critical', actionLabel: 'Kích hoạt khẩn cấp', actionDescription: 'Mô tả', requireMfa: true },
    })
    const badge = document.body.querySelector('.sum-badge--critical')
    expect(badge).toBeTruthy()
    expect(document.body.textContent).toContain('Kích hoạt khẩn cấp')
    expect(document.body.textContent).toContain('Mô tả')
    expect(wrapper.vm.severityClass).toBe('sum-badge--critical')
    expect(wrapper.vm.severityLabel).toBe('Nghiêm trọng')

    wrapper.vm.reason = 'Lý do'
    wrapper.vm.password = 'pw'
    await wrapper.vm.startStepUp()
    expect(document.body.querySelector('.sum-step-verify')).toBeTruthy()
    expect(document.body.querySelector('input[type="password"]')).toBeTruthy()

    const mfaLabel = document.body.textContent
    expect(mfaLabel).toContain('Mã MFA')
    const mfaInput = document.body.querySelectorAll('input[type="text"]')[0]
    mfaInput.value = '123456'
    mfaInput.dispatchEvent(new Event('input'))
    expect(wrapper.vm.mfaCode).toBe('123456')
  })

  it('cancels via Escape key', async () => {
    const wrapper = mount(StepUpModal, { props: { visible: true }, attachTo: document.body })
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))
    expect(wrapper.emitted('cancel')).toBeTruthy()
  })

  it('cancels via overlay click', async () => {
    const wrapper = mount(StepUpModal, { props: { visible: true }, attachTo: document.body })
    document.body.querySelector('.sum-overlay').dispatchEvent(new MouseEvent('click', { bubbles: true }))
    expect(wrapper.emitted('cancel')).toBeTruthy()
  })

  it('renders nothing when not visible and resets on becoming visible', async () => {
    const wrapper = mount(StepUpModal, { props: { visible: false, requireMfa: true } })
    expect(wrapper.find('.sum-overlay').exists()).toBe(false)
    wrapper.vm.reason = 'x'
    await wrapper.setProps({ visible: true })
    expect(wrapper.vm.reason).toBe('')
    expect(wrapper.vm.requireMfaCode).toBe(true)
  })

  it('cancels from the verify step via the ghost button', async () => {
    enterpriseApi.stepUpStart.mockResolvedValue({ data: { sessionId: 'sess-2', requiresMfa: false } })
    const wrapper = mount(StepUpModal, { props: { visible: true, requireMfa: false }, attachTo: document.body })
    wrapper.vm.reason = 'Lý do'
    await wrapper.vm.startStepUp()
    expect(document.body.querySelector('.sum-step-verify')).toBeTruthy()
    document.body.querySelector('.sum-btn--ghost').click()
    expect(wrapper.emitted('cancel')).toBeTruthy()
  })

  it('disables the confirm button until a password is entered', async () => {
    enterpriseApi.stepUpStart.mockResolvedValue({ data: { sessionId: 'sess-3', requiresMfa: false } })
    enterpriseApi.stepUpVerify.mockResolvedValue({})
    const wrapper = mount(StepUpModal, { props: { visible: true, requireMfa: false }, attachTo: document.body })
    wrapper.vm.reason = 'Lý do'
    await wrapper.vm.startStepUp()
    const dangerBtn = () => document.body.querySelector('.sum-btn--danger')
    expect(dangerBtn().disabled).toBe(true)
    wrapper.vm.password = 'pw'
    await flushPromises()
    expect(dangerBtn().disabled).toBe(false)
    dangerBtn().click()
    await flushPromises()
    expect(wrapper.emitted('confirmed')).toBeTruthy()
  })

  it('removes the keydown listener on unmount', async () => {
    const spy = vi.spyOn(window, 'removeEventListener')
    const wrapper = mount(StepUpModal, { props: { visible: true }, attachTo: document.body })
    wrapper.unmount()
    expect(spy).toHaveBeenCalledWith('keydown', expect.any(Function))
    expect(document.body.querySelector('.sum-overlay')).toBeNull()
    spy.mockRestore()
  })

  it('closes via the header close button', () => {
    const wrapper = mount(StepUpModal, { props: { visible: true }, attachTo: document.body })
    document.body.querySelector('.sum-close').click()
    expect(wrapper.emitted('cancel')).toBeTruthy()
  })

  it('starts step-up by typing a reason and clicking the Continue button', async () => {
    enterpriseApi.stepUpStart.mockResolvedValue({ data: { sessionId: 'sess-4', requiresMfa: false } })
    const wrapper = mount(StepUpModal, { props: { visible: true, requireMfa: false }, attachTo: document.body })
    const primary = () => document.body.querySelector('.sum-btn--primary')
    expect(primary().disabled).toBe(true)
    const ta = document.body.querySelector('.sum-textarea')
    ta.value = 'Lý do cần nhập'
    ta.dispatchEvent(new Event('input'))
    await flushPromises()
    expect(primary().disabled).toBe(false)
    primary().click()
    await flushPromises()
    expect(enterpriseApi.stepUpStart).toHaveBeenCalledWith('AllPrivilegedActions', 'Lý do cần nhập')
    expect(document.body.querySelector('.sum-step-verify')).toBeTruthy()
  })

  it('verifies via pressing Enter on the MFA input', async () => {
    enterpriseApi.stepUpStart.mockResolvedValue({ data: { sessionId: 'sess-5', requiresMfa: true } })
    enterpriseApi.stepUpVerify.mockResolvedValue({})
    const wrapper = mount(StepUpModal, { props: { visible: true, requireMfa: true }, attachTo: document.body })
    document.body.querySelector('.sum-textarea').value = 'Lý do'
    document.body.querySelector('.sum-textarea').dispatchEvent(new Event('input'))
    await flushPromises()
    document.body.querySelector('.sum-btn--primary').click()
    await flushPromises()
    expect(document.body.querySelector('.sum-step-verify')).toBeTruthy()
    wrapper.vm.password = 'pw'
    const mfa = document.body.querySelector('input[type="text"]')
    mfa.value = '123456'
    mfa.dispatchEvent(new Event('input'))
    const pwFields = document.body.querySelectorAll('.sum-field')
    const pwInput = pwFields[pwFields.length - 2].querySelector('input') || document.body.querySelector('.sum-input:not([type="text"])')
    if (pwInput) {
      pwInput.value = 'pw'
      pwInput.dispatchEvent(new Event('input'))
    }
    mfa.dispatchEvent(new KeyboardEvent('keyup', { key: 'Enter', bubbles: true }))
    await flushPromises()
    expect(enterpriseApi.stepUpVerify).toHaveBeenCalled()
  })

  it('verifies by pressing Enter on the password input', async () => {
    enterpriseApi.stepUpStart.mockResolvedValue({ data: { sessionId: 'sess-6', requiresMfa: false } })
    enterpriseApi.stepUpVerify.mockResolvedValue({})
    const wrapper = mount(StepUpModal, { props: { visible: true, requireMfa: false }, attachTo: document.body })
    document.body.querySelector('.sum-textarea').value = 'Lý do'
    document.body.querySelector('.sum-textarea').dispatchEvent(new Event('input'))
    await flushPromises()
    document.body.querySelector('.sum-btn--primary').click()
    await flushPromises()
    const pw = document.body.querySelector('input[type="password"]')
    pw.value = 'pw'
    pw.dispatchEvent(new Event('input'))
    pw.dispatchEvent(new KeyboardEvent('keyup', { key: 'Enter', bubbles: true }))
    await flushPromises()
    expect(enterpriseApi.stepUpVerify).toHaveBeenCalledWith('sess-6', 'pw', undefined)
    expect(wrapper.emitted('confirmed')).toBeTruthy()
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
