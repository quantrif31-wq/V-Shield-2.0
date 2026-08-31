import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../PrivilegedActionReasonForm.vue', () => ({
  default: {
    name: 'PrivilegedActionReasonForm',
    props: ['modelValue', 'required', 'requireResponsibility', 'showError', 'placeholder', 'disabled'],
    emits: ['update:modelValue', 'responsibility-change'],
    template: '<div class="parf"><input class="parf-input" :value="modelValue" @input="$emit(\'update:modelValue\', $event.target.value)" /><button class="parf-resp" type="button" @click="$emit(\'responsibility-change\', true)">resp</button></div>',
  },
}))

const DecisionDrawer = (await import('../DecisionDrawer.vue')).default

const props = {
  visible: true,
  laneName: 'Cổng A',
  subjectName: 'An',
  subjectId: '123',
  subjectType: 'EMPLOYEE',
  plateNumber: '51A-123',
  qrPayload: 'qr-xyz',
  warnings: [{ severity: 'warn', text: 'Cảnh báo' }],
}

function mountDrawer(overrides = {}, { noStub = false } = {}) {
  const wrapper = mount(DecisionDrawer, {
    props: { ...props, ...overrides },
    global: noStub ? {} : { stubs: { PrivilegedActionReasonForm: true } },
    attachTo: document.body,
  })
  return wrapper
}

beforeEach(() => {
  vi.clearAllMocks()
  vi.spyOn(window, 'alert').mockImplementation(() => {})
})
afterEach(() => {
  document.body.innerHTML = ''
  window.alert.mockRestore()
})

describe('DecisionDrawer full coverage', () => {
  it('applies lifecycle listeners and handles Escape close', async () => {
    const wrapper = mountDrawer()
    await flushPromises()
    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))
    expect(wrapper.emitted('close')).toBeTruthy()
    wrapper.unmount()
    expect(document.body.querySelector('.dd-root')).toBeNull()
  })

  it('renders warnings and subject metadata', async () => {
    const wrapper = mountDrawer()
    await flushPromises()
    expect(document.body.textContent).toContain('Cảnh báo')
    expect(document.body.textContent).toContain('Nhân viên')
    expect(document.body.textContent).toContain('51A-123')
    expect(document.body.textContent).toContain('qr-xyz')
    wrapper.unmount()
  })

  it('renders guest subject type and missing-id fallbacks', async () => {
    const wrapper = mountDrawer({ subjectType: 'GUEST', subjectId: '', plateNumber: '', qrPayload: '' })
    await flushPromises()
    expect(document.body.textContent).toContain('Khách')
    wrapper.unmount()
  })

  it('emits allow action without reason when not required', async () => {
    const wrapper = mountDrawer()
    await flushPromises()
    const allowBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Cho qua'))
    allowBtn.click()
    expect(wrapper.emitted('action')[0][0]).toEqual({ type: 'allow', reason: '', responsibility: false })
    wrapper.unmount()
  })

  it('emits deny action without reason when not required', async () => {
    const wrapper = mountDrawer()
    await flushPromises()
    const denyBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Từ chối'))
    denyBtn.click()
    expect(wrapper.emitted('action')[0][0]).toEqual({ type: 'deny', reason: '', responsibility: false })
    wrapper.unmount()
  })

  it('opens reason form for allow when requireReasonForAllow is set', async () => {
    const wrapper = mountDrawer({ requireReasonForAllow: true })
    await flushPromises()
    const allowBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Cho qua'))
    allowBtn.click()
    await flushPromises()
    expect(document.body.querySelector('.dd-form-section')).toBeTruthy()
    expect(wrapper.emitted('action')).toBeFalsy()
    wrapper.unmount()
  })

  it('opens reason form for deny when requireReasonForDeny is set', async () => {
    const wrapper = mountDrawer({ requireReasonForDeny: true })
    await flushPromises()
    const denyBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Từ chối'))
    denyBtn.click()
    await flushPromises()
    expect(document.body.querySelector('.dd-form-section')).toBeTruthy()
    wrapper.unmount()
  })

  it('handles loading state disabling actions and saving guards close', async () => {
    const wrapper = mountDrawer({ loading: true })
    await flushPromises()
    const allowBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Cho qua'))
    expect(allowBtn.disabled).toBe(true)
    wrapper.unmount()
  })

  it('opens manual form via button and cancels it', async () => {
    const wrapper = mountDrawer()
    await flushPromises()
    const manualBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Vận hành thủ công'))
    manualBtn.click()
    await flushPromises()
    expect(document.body.querySelector('.dd-form-section')).toBeTruthy()
    const cancelBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.trim() === 'Hủy')
    cancelBtn.click()
    expect(wrapper.vm.formMode).toBe('')
    wrapper.unmount()
  })

  it('opens escalate form via button and checks submit label', async () => {
    const wrapper = mountDrawer()
    await flushPromises()
    const escalateBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Xin phép quản lý'))
    escalateBtn.click()
    await flushPromises()
    expect(document.body.querySelector('.dd-form-section')).toBeTruthy()
    wrapper.vm.actionReason = 'ok'
    await flushPromises()
    expect(document.body.textContent).toContain('Gửi yêu cầu')
    wrapper.unmount()
  })

  it('uses unified emergency press flow', async () => {
    const wrapper = mountDrawer({ canUnifiedEmergency: true })
    await flushPromises()
    const emergBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Cấp quyền khẩn cấp'))
    emergencyBtnTrigger(wrapper, emergBtn, 'unified')
    await flushPromises()
    expect(document.body.querySelector('.dd-form-section')).toBeTruthy()
    wrapper.unmount()
  })

  it('handles startPress timeout setting duress and cancels press', async () => {
    vi.useFakeTimers()
    const wrapper = mountDrawer({ canUnifiedEmergency: true })
    await flushPromises()
    const emergBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Cấp quyền khẩn cấp'))
    emitPointer('pointerdown', emergBtn)
    vi.advanceTimersByTime(1600)
    await flushPromises()
    expect(wrapper.vm._isDuress).toBe(true)
    wrapper.vm.cancelPress()
    expect(wrapper.vm._isDuress).toBe(false)
    wrapper.unmount()
    vi.useRealTimers()
  })

  it('submits unified emergency form with action payload', async () => {
    const wrapper = mountDrawer({ canUnifiedEmergency: true })
    await flushPromises()
    const emergBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Cấp quyền khẩn cấp'))
    emergencyBtnTrigger(wrapper, emergBtn, 'unified')
    wrapper.vm._isDuress = true
    wrapper.vm.actionReason = 'khẩn cấp'
    wrapper.vm.responsibilityAccepted = true
    wrapper.vm.manualSubjectName = 'Kíp 115'
    await wrapper.vm.submitForm()
    expect(wrapper.emitted('action')[0][0]).toMatchObject({ type: 'unified_emergency', _duress: true })
    wrapper.unmount()
  })

  it('submitForm returns on empty reason', async () => {
    const wrapper = mountDrawer({ canManual: true })
    await flushPromises()
    wrapper.vm.openReasonForm('manual')
    wrapper.vm.actionReason = '   '
    await wrapper.vm.submitForm()
    expect(wrapper.vm.formError).toBe(true)
    expect(wrapper.emitted('action')).toBeFalsy()
    wrapper.unmount()
  })

  it('submitForm requires responsibility for override', async () => {
    const wrapper = mountDrawer()
    await flushPromises()
    wrapper.vm.openReasonForm('override')
    wrapper.vm.actionReason = 'ok'
    await wrapper.vm.submitForm()
    expect(wrapper.vm.formError).toBe(true)
    wrapper.unmount()
  })

  it('submitForm requires manual identity for emergency', async () => {
    const wrapper = mountDrawer()
    await flushPromises()
    wrapper.vm.openStepUp('emergency')
    wrapper.vm.actionReason = 'ok'
    wrapper.vm.responsibilityAccepted = true
    wrapper.vm.manualSubjectName = ''
    wrapper.vm.manualPlateNumber = ''
    await wrapper.vm.submitForm()
    expect(wrapper.vm.formError).toBe(true)
    wrapper.unmount()
  })

  it('submitForm succeeds for manual action', async () => {
    const wrapper = mountDrawer()
    await flushPromises()
    wrapper.vm.openReasonForm('manual')
    wrapper.vm.actionReason = 'lý do'
    wrapper.vm.manualSubjectName = 'Kíp'
    await wrapper.vm.submitForm()
    const payload = wrapper.emitted('action')[0][0]
    expect(payload.type).toBe('manual')
    expect(payload.details.subjectName).toBe('Kíp')
    wrapper.unmount()
  })

  it('submitForm success uses resetSaving', async () => {
    const wrapper = mountDrawer()
    await flushPromises()
    wrapper.vm.openReasonForm('duress')
    wrapper.vm.actionReason = 'ok'
    wrapper.vm.responsibilityAccepted = true
    await wrapper.vm.submitForm()
    expect(wrapper.emitted('action')[0][0].type).toBe('duress')
    wrapper.vm.resetSaving()
    expect(wrapper.vm.saving).toBe(false)
    wrapper.unmount()
  })

  it('resetForm clears state via visible watcher', async () => {
    const wrapper = mountDrawer()
    await flushPromises()
    wrapper.vm.openReasonForm('escalate')
    wrapper.vm.actionReason = 'x'
    await wrapper.setProps({ visible: false })
    await flushPromises()
    expect(wrapper.vm.formMode).toBe('')
    expect(wrapper.vm.actionReason).toBe('')
    wrapper.unmount()
  })

  it('handleClose returns early while saving', async () => {
    const wrapper = mountDrawer()
    await flushPromises()
    wrapper.vm.saving = true
    wrapper.vm.openReasonForm('manual')
    wrapper.vm.handleClose()
    expect(wrapper.emitted('close')).toBeFalsy()
    expect(wrapper.vm.formMode).toBe('manual')
    wrapper.unmount()
  })

  it('manual fields are seeded from props', async () => {
    const wrapper = mountDrawer()
    await flushPromises()
    wrapper.vm.openReasonForm('manual')
    expect(wrapper.vm.manualSubjectName).toBe('An')
    expect(wrapper.vm.manualSubjectId).toBe('123')
    expect(wrapper.vm.manualPlateNumber).toBe('51A-123')
    wrapper.unmount()
  })

  it('formTitle reflects current mode', () => {
    const wrapper = mountDrawer({ visible: false })
    expect(wrapper.vm.formTitle).toBe('Xác nhận hành động')
    wrapper.vm.formMode = 'override'
    expect(wrapper.vm.formTitle).toBe('Cho qua có chịu trách nhiệm')
    wrapper.unmount()
  })

  it('seedManualFields handles missing subject', async () => {
    const wrapper = mountDrawer({ subjectName: '', subjectId: '', plateNumber: '' })
    await flushPromises()
    wrapper.vm.openReasonForm('manual')
    expect(wrapper.vm.manualSubjectId).toBe('')
    wrapper.unmount()
  })

  it('runs the warnings default factory when omitted', async () => {
    const wrapper = mount(DecisionDrawer, {
      props: { visible: true, laneName: 'Cổng A' },
      global: { stubs: { PrivilegedActionReasonForm: true } },
      attachTo: document.body,
    })
    await flushPromises()
    expect(wrapper.vm.warnings).toEqual([])
    wrapper.unmount()
  })

  it('closes via overlay self-click and close button', async () => {
    const wrapper = mountDrawer()
    await flushPromises()
    const root = document.body.querySelector('.dd-root')
    const closeBtn = document.body.querySelector('.dd-close')
    root.dispatchEvent(new Event('click', { bubbles: true }))
    expect(wrapper.emitted('close')).toHaveLength(1)
    closeBtn.dispatchEvent(new MouseEvent('click', { bubbles: true }))
    expect(wrapper.emitted('close')).toHaveLength(2)
    wrapper.unmount()
  })

  it('handles pointerleave cancel on the unified emergency button', async () => {
    const wrapper = mountDrawer({ canUnifiedEmergency: true })
    await flushPromises()
    wrapper.vm._isDuress = true
    wrapper.vm.pressTimer = 123
    const emergBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Cấp quyền khẩn cấp'))
    emergBtn.dispatchEvent(new Event('pointerleave', { bubbles: true }))
    expect(wrapper.vm._isDuress).toBe(false)
    wrapper.unmount()
  })

  it('drives the reason form and manual inputs through the template and submits', async () => {
    const wrapper = mountDrawer({}, { noStub: true })
    await flushPromises()
    const manualBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Vận hành thủ công'))
    manualBtn.click()
    await flushPromises()

    const reasonInput = document.body.querySelector('.parf-input')
    reasonInput.value = 'lý do thủ công'
    reasonInput.dispatchEvent(new Event('input', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.actionReason).toBe('lý do thủ công')

    const manualInputs = document.body.querySelectorAll('.dd-manual-grid input')
    manualInputs[0].value = 'Kíp cấp cứu'
    manualInputs[0].dispatchEvent(new Event('input', { bubbles: true }))
    manualInputs[1].value = 'EMP-01'
    manualInputs[1].dispatchEvent(new Event('input', { bubbles: true }))
    manualInputs[2].value = '51A-999'
    manualInputs[2].dispatchEvent(new Event('input', { bubbles: true }))
    await flushPromises()
    expect(wrapper.vm.manualPlateNumber).toBe('51A-999')

    const submitBtn = [...document.body.querySelectorAll('button')].find((b) => b.textContent.includes('Xác nhận chuyển manual'))
    expect(submitBtn.disabled).toBe(false)
    submitBtn.click()
    await flushPromises()
    expect(wrapper.emitted('action')[0][0]).toMatchObject({ type: 'manual', reason: 'lý do thủ công' })
    wrapper.unmount()
  })

  it('drives responsibility change through the reason form', async () => {
    const wrapper = mountDrawer({}, { noStub: true })
    await flushPromises()
    wrapper.vm.openReasonForm('override')
    await flushPromises()
    const respBtn = document.body.querySelector('.parf-resp')
    respBtn.dispatchEvent(new Event('click', { bubbles: true }))
    expect(wrapper.vm.responsibilityAccepted).toBe(true)
    wrapper.unmount()
  })
})

function emergencyBtnTrigger(wrapper, btn, mode) {
  emitPointer('pointerdown', btn)
  emitPointer('pointerup', btn)
}

function emitPointer(type, el) {
  const evt = new Event(type, { bubbles: true })
  el.dispatchEvent(evt)
}
