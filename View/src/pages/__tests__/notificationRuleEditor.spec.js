import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../services/notificationApi', () => ({
  createNotificationRule: vi.fn(),
  deleteNotificationRule: vi.fn(),
  getNotificationRules: vi.fn(),
  getRuleSuggestions: vi.fn(),
  updateNotificationRule: vi.fn(),
}))

const notificationApi = await import('../../services/notificationApi')
const NotificationRuleEditor = (await import('../NotificationRuleEditor.vue')).default

beforeEach(() => vi.clearAllMocks())
afterEach(() => vi.unstubAllGlobals())

describe('NotificationRuleEditor', () => {
  it('loads rules and suggestions', async () => {
    notificationApi.getNotificationRules.mockResolvedValue({
      data: { data: [{ id: 1, eventType: 'Intrusion', severityMin: 'High', recipientRole: 'BaoVe', notifyWeb: true, notifyMobile: false, isActive: true }] },
    })
    notificationApi.getRuleSuggestions.mockResolvedValue({
      data: { data: [{ eventType: 'Intrusion', label: 'Xâm nhập' }] },
    })
    const wrapper = mount(NotificationRuleEditor)
    await flushPromises()
    expect(wrapper.text()).toContain('Intrusion')
    expect(wrapper.text()).toContain('Từ High')
    expect(notificationApi.getRuleSuggestions).toHaveBeenCalledWith('Admin')
  })

  it('creates a rule from the draft', async () => {
    notificationApi.getNotificationRules.mockResolvedValue({ data: { data: [] } })
    notificationApi.getRuleSuggestions.mockResolvedValue({ data: { data: [{ eventType: 'Intrusion', label: 'Xâm nhập' }] } })
    const wrapper = mount(NotificationRuleEditor)
    await flushPromises()

    notificationApi.createNotificationRule.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Thêm quy tắc').trigger('click')
    await flushPromises()
    expect(notificationApi.createNotificationRule).toHaveBeenCalledWith(expect.objectContaining({ eventType: 'Intrusion' }))
  })

  it('toggles a rule channel and persists the change', async () => {
    notificationApi.getNotificationRules.mockResolvedValue({
      data: { data: [{ id: 1, eventType: 'Intrusion', notifyWeb: true, notifyMobile: false, isActive: true }] },
    })
    notificationApi.getRuleSuggestions.mockResolvedValue({ data: { data: [] } })
    const wrapper = mount(NotificationRuleEditor)
    await flushPromises()

    notificationApi.updateNotificationRule.mockResolvedValue({})
    await wrapper.findAll('.rule-row .rule-channels input[type="checkbox"]')[1].setValue(true)
    await flushPromises()
    expect(notificationApi.updateNotificationRule).toHaveBeenCalledWith(1, expect.objectContaining({ notifyMobile: true }))
  })

  it('deletes a rule after confirmation', async () => {
    notificationApi.getNotificationRules.mockResolvedValue({
      data: { data: [{ id: 1, eventType: 'Intrusion', notifyWeb: true, notifyMobile: false, isActive: true }] },
    })
    notificationApi.getRuleSuggestions.mockResolvedValue({ data: { data: [] } })
    const wrapper = mount(NotificationRuleEditor)
    await flushPromises()

    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true)
    notificationApi.deleteNotificationRule.mockResolvedValue({})
    await wrapper.findAll('button').find((b) => b.text() === 'Xóa').trigger('click')
    await flushPromises()
    expect(notificationApi.deleteNotificationRule).toHaveBeenCalledWith(1)
    confirmSpy.mockRestore()
  })
})
