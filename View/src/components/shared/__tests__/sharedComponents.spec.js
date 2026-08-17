import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const hoisted = vi.hoisted(() => ({ router: { push: vi.fn() } }))

vi.mock('vue-router', () => ({ useRouter: () => hoisted.router }))

const { securityAlertState } = await import('../../../services/securityAlertBus')
const GlobalEmergencyBanner = (await import('../GlobalEmergencyBanner.vue')).default
const PrivilegedActionReasonForm = (await import('../PrivilegedActionReasonForm.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  securityAlertState.items = []
})

describe('GlobalEmergencyBanner', () => {
  it('renders nothing when there are no alerts', () => {
    const wrapper = mount(GlobalEmergencyBanner)
    expect(wrapper.find('.emergency-banner').exists()).toBe(false)
  })

  it('renders the first alert and the remaining count', () => {
    securityAlertState.items = [
      { id: 'a', title: 'Khẩn cấp', message: 'Hỏa hoạn', route: '/soc-console' },
      { id: 'b', title: 'Khẩn cấp 2', message: 'x' },
    ]
    const wrapper = mount(GlobalEmergencyBanner)
    expect(wrapper.find('.emergency-banner').exists()).toBe(true)
    expect(wrapper.text()).toContain('Khẩn cấp')
    expect(wrapper.text()).toContain('+1')
  })

  it('dismisses the current alert', async () => {
    securityAlertState.items = [{ id: 'a', title: 'Khẩn cấp', message: 'x' }]
    const wrapper = mount(GlobalEmergencyBanner)
    await wrapper.find('.emergency-dismiss').trigger('click')
    expect(wrapper.find('.emergency-banner').exists()).toBe(false)
  })

  it('navigates to the alert route from details', async () => {
    securityAlertState.items = [{ id: 'a', title: 'Khẩn cấp', message: 'x', route: '/soc-console' }]
    const wrapper = mount(GlobalEmergencyBanner)
    await wrapper.find('.emergency-action').trigger('click')
    expect(hoisted.router.push).toHaveBeenCalledWith('/soc-console')
  })
})

describe('PrivilegedActionReasonForm', () => {
  it('emits the reason as the user types', async () => {
    const wrapper = mount(PrivilegedActionReasonForm)
    await wrapper.find('textarea').setValue('Lý do kiểm toán')
    expect(wrapper.emitted('update:modelValue')[0]).toEqual(['Lý do kiểm toán'])
  })

  it('validates the required reason', () => {
    const wrapper = mount(PrivilegedActionReasonForm)
    expect(wrapper.vm.isValid()).toBe(false)
    wrapper.vm.internalReason = 'Hợp lệ'
    expect(wrapper.vm.isValid()).toBe(true)
  })

  it('returns the collected values', () => {
    const wrapper = mount(PrivilegedActionReasonForm, { props: { requireResponsibility: true, requireEscalationNote: true } })
    wrapper.vm.internalReason = ' Lý do '
    wrapper.vm.acceptedResponsibility = true
    wrapper.vm.internalEscalationNote = ' Ghi chú '
    expect(wrapper.vm.getValues()).toEqual({
      reason: 'Lý do',
      acceptedResponsibility: true,
      escalationNote: 'Ghi chú',
    })
  })

  it('requires responsibility acceptance when configured', async () => {
    const wrapper = mount(PrivilegedActionReasonForm, { props: { requireResponsibility: true } })
    wrapper.vm.internalReason = 'x'
    expect(wrapper.vm.isValid()).toBe(false)
    await wrapper.find('input[type="checkbox"]').setValue(true)
    expect(wrapper.emitted('responsibility-change')[0]).toEqual([true])
    expect(wrapper.vm.isValid()).toBe(true)
  })

  it('resets all fields', () => {
    const wrapper = mount(PrivilegedActionReasonForm, { props: { requireResponsibility: true } })
    wrapper.vm.internalReason = 'x'
    wrapper.vm.acceptedResponsibility = true
    wrapper.vm.internalEscalationNote = 'y'
    wrapper.vm.reset()
    expect(wrapper.vm.internalReason).toBe('')
    expect(wrapper.vm.acceptedResponsibility).toBe(false)
    expect(wrapper.vm.internalEscalationNote).toBe('')
  })
})
