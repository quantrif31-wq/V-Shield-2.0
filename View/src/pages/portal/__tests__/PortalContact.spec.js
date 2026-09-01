import { mount, flushPromises } from '@vue/test-utils'
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import PortalContact from '../PortalContact.vue'

const { mockSubmitFeedback } = vi.hoisted(() => ({
  mockSubmitFeedback: vi.fn()
}))

vi.mock('../../../services/portalApi', () => ({
  portalApi: { submitFeedback: (...args) => mockSubmitFeedback(...args) }
}))

describe('PortalContact', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.useFakeTimers()
    mockSubmitFeedback.mockReset()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
  })

  function mountContact(user = null) {
    return mount(PortalContact, {
      global: { provide: { communityUser: { value: user } } }
    })
  }

  it('mounts and renders faq section', () => {
    const wrapper = mountContact()
    expect(wrapper.text()).toContain('LIÊN HỆ & ĐÓNG GÓP')
  })

  it('prefills form with community user info', async () => {
    const wrapper = mountContact({ fullName: 'Community User', email: 'c@c.com' })
    await wrapper.vm.$nextTick()
    const inputs = wrapper.findAll('input')
    expect(inputs[0].element.value).toBe('Community User')
    expect(inputs[1].element.value).toBe('c@c.com')
  })

  it('toggles FAQ open and closed', async () => {
    const wrapper = mountContact()
    const faqBtn = wrapper.findAll('button').find(b => b.text().includes('V-Shield 2.0 hoạt động'))
    await faqBtn.trigger('click')
    expect(wrapper.vm.openFaqIndex).toBe(0)
    await faqBtn.trigger('click')
    expect(wrapper.vm.openFaqIndex).toBeNull()
  })

  it('shows validation error when form incomplete', async () => {
    const wrapper = mountContact()
    const form = wrapper.find('form')
    await form.trigger('submit.prevent')
    expect(wrapper.vm.feedbackToast).toContain('Vui lòng điền đầy đủ')
  })

  it('submits feedback successfully', async () => {
    mockSubmitFeedback.mockResolvedValue({ success: true })
    const wrapper = mountContact()
    const inputs = wrapper.findAll('input')
    await inputs[0].setValue('Test Name')
    await inputs[1].setValue('test@test.com')
    await wrapper.find('textarea').setValue('Some message')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()
    expect(mockSubmitFeedback).toHaveBeenCalled()
    expect(wrapper.vm.feedbackToast).toContain('gửi thành công')
  })

  it('handles feedback submission error', async () => {
    mockSubmitFeedback.mockRejectedValue(new Error('fail'))
    const wrapper = mountContact()
    const inputs = wrapper.findAll('input')
    await inputs[0].setValue('Test Name')
    await inputs[1].setValue('test@test.com')
    await wrapper.find('textarea').setValue('Some message')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()
    expect(wrapper.vm.feedbackToast).toContain('Không thể gửi đóng góp')
  })

  it('changes feedback category select', async () => {
    const wrapper = mountContact()
    const select = wrapper.find('select')
    await select.setValue('Bug')
    expect(wrapper.vm.feedbackForm.category).toBe('Bug')
  })

  it('clears feedback toast after timeout', async () => {
    mockSubmitFeedback.mockResolvedValue({ success: true })
    const wrapper = mountContact()
    const inputs = wrapper.findAll('input')
    await inputs[0].setValue('Test Name')
    await inputs[1].setValue('test@test.com')
    await wrapper.find('textarea').setValue('Some message')
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()
    vi.advanceTimersByTime(5001)
    expect(wrapper.vm.feedbackToast).toBe('')
  })
})
