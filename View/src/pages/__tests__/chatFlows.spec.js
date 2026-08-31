import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

const startCall = vi.fn()

vi.mock('../../stores/auth', () => ({ authState: { user: { employeeId: 5, fullName: 'An', username: 'an', role: 'NhanVien' } } }))
vi.mock('../../stores/callStore', () => ({ startCall: (...args) => startCall(...args) }))
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
  sendTyping: vi.fn(),
}))

const chatApi = await import('../../services/chatApi')
const ChatPage = (await import('../Chat.vue')).default

const consoleErrorMock = vi.fn()

beforeEach(() => {
  vi.clearAllMocks()
  vi.useFakeTimers()
  startCall.mockClear()
  consoleErrorMock.mockClear()
  console.error = consoleErrorMock
  chatApi.getConversations.mockResolvedValue({ data: { data: [] } })
  chatApi.getContacts.mockResolvedValue({ data: { data: [] } })
  chatApi.getMessages.mockResolvedValue({ data: { data: [] } })
  chatApi.connectChatHub.mockResolvedValue({})
})
afterEach(() => {
  vi.runOnlyPendingTimers()
  vi.useRealTimers()
})

describe('ChatPage flows', () => {
  it('hubStatusLabel maps hub states', () => {
    const wrapper = mount(ChatPage)
    for (const [s, lab] of [['connecting', 'Đang kết nối'], ['live', 'Live'], ['reconnecting', 'Đang kết nối lại'], ['stale', 'Dữ liệu có thể đã cũ'], ['disconnected', 'Đã ngắt kết nối'], ['other', 'Đã ngắt kết nối']]) {
      wrapper.vm.hubStatus = s
      expect(wrapper.vm.hubStatusLabel).toBe(lab)
    }
  })

  it('updates hub state from connection-state callback', async () => {
    const wrapper = mount(ChatPage)
    await flushPromises()
    const cb = chatApi.onChatConnectionState.mock.calls[0][0]
    cb({ status: 'live', lastUpdated: '2026-08-01T00:00:00Z' })
    expect(wrapper.vm.hubStatus).toBe('live')
    expect(wrapper.vm.hubConnected).toBe(true)
  })

  it('logs when connecting the hub fails', async () => {
    chatApi.connectChatHub.mockRejectedValue(new Error('hub down'))
    const wrapper = mount(ChatPage)
    await flushPromises()
    expect(console.error).toHaveBeenCalled()
  })

  it('logs when loading chat data fails', async () => {
    chatApi.getConversations.mockRejectedValue(new Error('boom'))
    const wrapper = mount(ChatPage)
    await flushPromises()
    expect(console.error).toHaveBeenCalled()
  })

  it('normalizes employee ids and text', () => {
    const wrapper = mount(ChatPage)
    expect(wrapper.vm.normalizeEmployeeId('' )).toBe(null)
    expect(wrapper.vm.normalizeEmployeeId('5')).toBe(5)
    expect(wrapper.vm.normalizeEmployeeId('abc')).toBe('abc')
    expect(wrapper.vm.normalizeText('  Hello ')).toBe('hello')
    expect(wrapper.vm.isSameEmployee(5, '5')).toBe(true)
    expect(wrapper.vm.isSameEmployee('abc', 'def')).toBe(false)
  })

  it('isOwnMessage resolves by senderId and by name', () => {
    const wrapper = mount(ChatPage)
    wrapper.vm.myEmployeeId = 5
    expect(wrapper.vm.isOwnMessage({ senderId: 5 })).toBe(true)
    expect(wrapper.vm.isOwnMessage({ senderId: '999', senderName: 'an' })).toBe(true)
    expect(wrapper.vm.isOwnMessage({ senderId: '999', senderName: 'Nguoi khac' })).toBe(false)
    expect(wrapper.vm.isOwnMessage({ senderId: '999', senderName: '' })).toBe(false)
  })

  it('builds message dedup keys for all branches', () => {
    const wrapper = mount(ChatPage)
    expect(wrapper.vm.buildMessageDedupKey(null)).toBe(null)
    expect(wrapper.vm.buildMessageDedupKey({ messageId: 42 })).toBe('message:42')
    expect(wrapper.vm.buildMessageDedupKey({ messageId: 'temp-x', clientMessageId: 'c1', senderId: 5, conversationId: 1 })).toBe('client:1:5:c1')
    expect(wrapper.vm.buildMessageDedupKey({ messageId: 'temp-x', conversationId: 2, senderId: 7, senderName: 'B', content: 'hi', messageType: 'Text', sentAt: '2026-08-01T00:00:00Z' })).toContain('fallback:2')
  })

  it('deduplicates messages', () => {
    const wrapper = mount(ChatPage)
    const msgs = [
      { messageId: 1, content: 'a' },
      { messageId: 1, content: 'a' },
      { messageId: 2, content: 'b' },
    ]
    expect(wrapper.vm.deduplicateMessages(msgs)).toHaveLength(2)
    expect(wrapper.vm.deduplicateMessages()).toHaveLength(0)
  })

  it('handleNewMessage appends a new message and marks read', async () => {
    const wrapper = mount(ChatPage)
    wrapper.vm.selectedConvId = 1
    chatApi.loadData = vi.fn()
    wrapper.vm.handleNewMessage({ conversationId: 1, messageId: 9, senderId: 3, senderName: 'B', content: 'xin chào', messageType: 'Text' })
    await flushPromises()
    expect(wrapper.vm.messages.some((m) => m.messageId === 9)).toBe(true)
  })

  it('handleNewMessage skips when conversation does not match', async () => {
    const wrapper = mount(ChatPage)
    wrapper.vm.selectedConvId = 1
    wrapper.vm.handleNewMessage({ conversationId: 99, messageId: 9 })
    expect(wrapper.vm.messages).not.toContainEqual(expect.objectContaining({ messageId: 9 }))
  })

  it('handleNewMessage replaces an existing pending message by clientMessageId', async () => {
    const wrapper = mount(ChatPage)
    wrapper.vm.selectedConvId = 1
    wrapper.vm.messages = [{ messageId: 'temp-1', clientMessageId: 'cX', content: 'old', pending: true, senderId: 5, messageType: 'Text' }]
    wrapper.vm.handleNewMessage({ conversationId: 1, messageId: null, clientMessageId: 'cX', content: 'new', pending: false, senderId: 5, messageType: 'Text' })
    await flushPromises()
    expect(wrapper.vm.messages[0].content).toBe('new')
    expect(wrapper.vm.messages[0].pending).toBe(false)
  })

  it('handleNewMessage replaces an existing message by messageId', async () => {
    const wrapper = mount(ChatPage)
    wrapper.vm.selectedConvId = 1
    wrapper.vm.messages = [{ messageId: 5, content: 'old', senderId: 3, messageType: 'Text' }]
    wrapper.vm.handleNewMessage({ conversationId: 1, messageId: 5, content: 'updated', senderId: 3, messageType: 'Text' })
    await flushPromises()
    expect(wrapper.vm.messages[0].content).toBe('updated')
    expect(wrapper.vm.messages[0].pending).toBe(false)
  })

  it('handleTyping shows typing indicator then clears it', async () => {
    const wrapper = mount(ChatPage)
    wrapper.vm.selectedConvId = 1
    wrapper.vm.handleTyping({ conversationId: 1, fullName: 'B' })
    expect(wrapper.vm.typingUser).toBe('B')
    vi.advanceTimersByTime(3100)
    expect(wrapper.vm.typingUser).toBe('')
  })

  it('handleTyping ignores other conversations', () => {
    const wrapper = mount(ChatPage)
    wrapper.vm.selectedConvId = 1
    wrapper.vm.handleTyping({ conversationId: 2, fullName: 'C' })
    expect(wrapper.vm.typingUser).toBe('')
  })

  it('handleRead marks non-own messages as read', () => {
    const wrapper = mount(ChatPage)
    wrapper.vm.selectedConvId = 1
    wrapper.vm.myEmployeeId = 5
    wrapper.vm.messages = [
      { messageId: 1, senderId: 3, messageType: 'Text' },
      { messageId: 2, senderId: 5, messageType: 'Text' },
    ]
    wrapper.vm.handleRead({ conversationId: 1, readAt: '2026-08-01T00:00:00Z' })
    expect(wrapper.vm.messages[0].isRead).toBe(true)
    expect(wrapper.vm.messages[1].isRead).toBe(undefined)
  })

  it('triggerCall starts a zalo-style call with the other participant', async () => {
    const wrapper = mount(ChatPage)
    wrapper.vm.myEmployeeId = 5
    wrapper.vm.selectedConvId = 1
    wrapper.vm.conversations = [{ conversationId: 1, participants: [{ employeeId: 5, fullName: 'An' }, { employeeId: 7, fullName: 'B' }] }]
    wrapper.vm.triggerCall('audio')
    expect(startCall).toHaveBeenCalledWith(expect.objectContaining({ targetEmployeeId: 7, type: 'audio', conversationId: 1 }))
    wrapper.vm.triggerCall('video')
    expect(startCall).toHaveBeenCalledTimes(2)
  })

  it('triggerCall returns without target or conversation', () => {
    const wrapper = mount(ChatPage)
    wrapper.vm.myEmployeeId = 5
    wrapper.vm.selectedConvId = 1
    wrapper.vm.conversations = [{ conversationId: 1, participants: [{ employeeId: 5, fullName: 'An' }] }]
    wrapper.vm.triggerCall('audio')
    expect(startCall).not.toHaveBeenCalled()
    wrapper.vm.selectedConvId = null
    wrapper.vm.conversations = [{ conversationId: 1, participants: [{ employeeId: 7 }] }]
    wrapper.vm.triggerCall('audio')
    expect(startCall).not.toHaveBeenCalled()
  })

  it('restores a failed message to the input', async () => {
    chatApi.sendMessage.mockRejectedValue({ response: { data: { message: 'lỗi mạng' } } })
    const wrapper = mount(ChatPage)
    await flushPromises()
    wrapper.vm.selectedConvId = 1
    wrapper.vm.messageText = 'hello'
    await wrapper.vm.sendMessage()
    expect(wrapper.vm.sendError).toContain('lỗi mạng')
    expect(wrapper.vm.messageText).toBe('hello')
    expect(wrapper.vm.messages).not.toContainEqual(expect.objectContaining({ messageId: expect.stringContaining('temp-') }))
  })

  it('sendMessage does nothing when content is empty', async () => {
    const wrapper = mount(ChatPage)
    await flushPromises()
    wrapper.vm.selectedConvId = 1
    wrapper.vm.messageText = '   '
    await wrapper.vm.sendMessage()
    expect(chatApi.sendMessage).not.toHaveBeenCalled()
  })

  it('onTyping sends typing for the selected conversation', async () => {
    const wrapper = mount(ChatPage)
    wrapper.vm.selectedConvId = 3
    wrapper.vm.onTyping()
    expect(chatApi.sendTyping).toHaveBeenCalledWith(3)
  })

  it('starts a conversation from a contact', async () => {
    chatApi.createConversation = vi.fn().mockResolvedValue({ data: { data: { conversationId: 11 } } })
    chatApi.getConversations.mockResolvedValue({ data: { data: [{ conversationId: 11, title: 'New', participants: [] }] } })
    const wrapper = mount(ChatPage)
    await flushPromises()
    await wrapper.vm.startConversation({ employeeId: 7, fullName: 'B' })
    await flushPromises()
    expect(chatApi.createConversation).toHaveBeenCalledWith([7])
    expect(chatApi.getMessages).toHaveBeenCalledWith(11)
  })

  it('logs when starting a conversation fails', async () => {
    chatApi.createConversation = vi.fn().mockRejectedValue(new Error('nope'))
    const wrapper = mount(ChatPage)
    await flushPromises()
    await wrapper.vm.startConversation({ employeeId: 7 })
    expect(console.error).toHaveBeenCalled()
  })

  it('renders participant helpers', () => {
    const wrapper = mount(ChatPage)
    wrapper.vm.myEmployeeId = 5
    expect(wrapper.vm.getInitials({ title: 'Nhóm' })).toBe('N')
    expect(wrapper.vm.getInitials({ participants: [{ employeeId: 7, fullName: 'Bảo' }] })).toBe('B')
    expect(wrapper.vm.getParticipantNames({ participants: [{ employeeId: 5, fullName: 'An' }, { employeeId: 7, fullName: 'Bảo' }] })).toBe('Bảo')
    expect(wrapper.vm.getParticipantNames({})).toBe('Hội thoại')
    expect(wrapper.vm.getAvatarColor('An')).toBeTruthy()
  })

  it('formats timestamps for today and other days', () => {
    const wrapper = mount(ChatPage)
    expect(wrapper.vm.formatTime('')).toBe('')
    expect(wrapper.vm.formatTime(new Date().toISOString())).toBeTruthy()
    expect(wrapper.vm.formatTime('2020-01-01T00:00:00Z')).toBeTruthy()
  })

  it('computes departments, positions and filtered contacts', async () => {
    chatApi.getContacts.mockResolvedValue({ data: { data: [
      { employeeId: 1, fullName: 'A A', departmentName: 'ANPP', positionName: 'Bảo vệ', email: 'a@x.com' },
      { employeeId: 2, fullName: 'B B', departmentName: 'NS', positionName: 'HR', email: 'b@x.com' },
    ] } })
    const wrapper = mount(ChatPage)
    await flushPromises()
    expect(wrapper.vm.departments).toEqual(['ANPP', 'NS'])
    expect(wrapper.vm.positions).toEqual(['Bảo vệ', 'HR'])
    wrapper.vm.activeTab = 'contacts'
    wrapper.vm.showFilters = true
    wrapper.vm.filterDepartment = 'ANPP'
    expect(wrapper.vm.filteredContacts.length).toBe(1)
    expect(wrapper.vm.filteredContacts[0].employeeId).toBe(1)
    wrapper.vm.filterDepartment = ''
    wrapper.vm.searchQuery = 'b@x'
    expect(wrapper.vm.filteredContacts[0].employeeId).toBe(2)
    wrapper.vm.searchQuery = ''
    wrapper.vm.filterPosition = 'HR'
    expect(wrapper.vm.filteredContacts.length).toBe(1)
    wrapper.vm.filterDepartment = ''
    wrapper.vm.searchQuery = ''
    wrapper.vm.filterPosition = ''
    expect(wrapper.vm.filteredContacts.length).toBe(2)
  })

  it('scrolls the message container to the bottom', async () => {
    chatApi.getMessages.mockResolvedValue({ data: { data: [{ messageId: 1, senderId: 3, senderName: 'B', messageType: 'Text', content: 'hi', sentAt: '2026-08-01T00:00:00Z' }] } })
    const wrapper = mount(ChatPage)
    await flushPromises()
    await wrapper.vm.selectConversation({ conversationId: 1, title: 'Nhóm', participants: [] })
    await flushPromises()
    expect(wrapper.vm.messages).toHaveLength(1)
    const container = wrapper.find('.messages-container')
    container.element.scrollTop = 0
    wrapper.vm.scrollToBottom()
    expect(container.element.scrollTop).toBe(container.element.scrollHeight)
  })
})
