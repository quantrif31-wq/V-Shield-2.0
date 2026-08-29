import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../stores/auth', () => ({ authState: { user: { employeeId: 5, fullName: 'An', role: 'NhanVien' } } }))
vi.mock('../../services/chatApi', () => ({
  getConversations: vi.fn(),
  getContacts: vi.fn(),
  getMessages: vi.fn(),
  sendMessage: vi.fn(),
  markRead: vi.fn(),
  connectChatHub: vi.fn(),
  disconnectChatHub: vi.fn(),
  onMessage: vi.fn(() => () => {}),
  onTyping: vi.fn(() => () => {}),
  onRead: vi.fn(() => () => {}),
  onChatConnectionState: vi.fn(() => () => {}),
}))

const chatApi = await import('../../services/chatApi')
const ChatPage = (await import('../Chat.vue')).default

beforeEach(() => {
  vi.clearAllMocks()
  vi.useFakeTimers()
  chatApi.getConversations.mockResolvedValue({ data: { data: [{ conversationId: 1, title: 'Nhóm An Ninh', participants: [] }] } })
  chatApi.getContacts.mockResolvedValue({ data: { data: [{ userId: 2, fullName: 'Bảo vệ', departmentName: 'An Ninh' }] } })
  chatApi.getMessages.mockResolvedValue({ data: { data: [] } })
  chatApi.connectChatHub.mockResolvedValue({})
})
afterEach(() => {
  vi.runOnlyPendingTimers()
  vi.useRealTimers()
})

describe('ChatPage', () => {
  it('loads conversations, contacts and connects the hub on mount', async () => {
    const wrapper = mount(ChatPage)
    await flushPromises()
    expect(chatApi.getConversations).toHaveBeenCalled()
    expect(chatApi.getContacts).toHaveBeenCalled()
    expect(chatApi.connectChatHub).toHaveBeenCalled()
    expect(wrapper.vm.conversations).toHaveLength(1)
  })

  it('filters conversations by search query', async () => {
    chatApi.getConversations.mockResolvedValue({
      data: { data: [
        { conversationId: 1, title: 'Nhóm An Ninh', participants: [] },
        { conversationId: 2, title: 'Phòng Nhân sự', participants: [] },
      ] },
    })
    const wrapper = mount(ChatPage)
    await flushPromises()
    wrapper.vm.searchQuery = 'nhân sự'
    expect(wrapper.vm.filteredConversations).toHaveLength(1)
    expect(wrapper.vm.filteredConversations[0].conversationId).toBe(2)
  })

  it('selects a conversation and loads its messages', async () => {
    chatApi.getMessages.mockResolvedValue({ data: { data: [{ messageId: 1, senderId: 5, senderName: 'An', messageType: 'Text', content: 'xin chào', sentAt: '2026-08-01T00:00:00Z' }] } })
    const wrapper = mount(ChatPage)
    await flushPromises()
    await wrapper.vm.selectConversation({ conversationId: 1, title: 'Nhóm', participants: [] })
    await flushPromises()
    expect(chatApi.getMessages).toHaveBeenCalledWith(1)
    expect(chatApi.markRead).toHaveBeenCalledWith(1)
    expect(wrapper.vm.messages).toHaveLength(1)
  })

  it('sends a message through the api', async () => {
    chatApi.sendMessage.mockResolvedValue({})
    const wrapper = mount(ChatPage)
    await flushPromises()
    wrapper.vm.selectedConvId = 1
    wrapper.vm.messageText = 'hello'
    await wrapper.vm.sendMessage()
    expect(chatApi.sendMessage).toHaveBeenCalledWith(1, 'hello', 'Text', null, expect.any(String))
    expect(wrapper.vm.messageText).toBe('')
  })

  it('cleans up local timers on unmount', async () => {
    const wrapper = mount(ChatPage)
    await flushPromises()
    wrapper.unmount()
    expect(wrapper.vm.refreshTimer).toBeNull()
  })
})
