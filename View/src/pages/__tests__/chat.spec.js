import { flushPromises, mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'

vi.mock('../../stores/auth', () => ({ authState: { user: { userId: 5, fullName: 'An' } } }))
vi.mock('../../services/chatApi', () => ({
  getContacts: vi.fn(),
  getConversations: vi.fn(),
  sendMessage: vi.fn(),
  markRead: vi.fn(),
  connectChatHub: vi.fn(),
  onMessage: vi.fn(() => () => {}),
  onTyping: vi.fn(() => () => {}),
  onRead: vi.fn(() => () => {}),
  onIncomingCall: vi.fn(() => () => {}),
  onCallResponse: vi.fn(() => () => {}),
  onCallEnded: vi.fn(() => () => {}),
  callUser: vi.fn(),
  callResponse: vi.fn(),
  endCall: vi.fn(),
  onChatConnectionState: vi.fn(() => () => {}),
}))

const chatApi = await import('../../services/chatApi')
const Chat = (await import('../Chat.vue')).default

beforeEach(() => vi.clearAllMocks())

describe('Chat', () => {
  it('loads conversations and contacts', async () => {
    chatApi.getConversations.mockResolvedValue({
      data: [{ conversationId: 1, participantNames: 'An, Bảo vệ', lastMessage: 'xin chào' }],
    })
    chatApi.getContacts.mockResolvedValue({ data: [{ userId: 2, fullName: 'Bảo vệ' }] })
    const wrapper = mount(Chat)
    await flushPromises()
    expect(chatApi.getConversations).toHaveBeenCalled()
    expect(chatApi.getContacts).toHaveBeenCalled()
  })
})
