<template>
  <div class="chat-container">
    <div class="chat-sidebar">
      <div class="sidebar-tabs">
        <button :class="{ active: activeTab === 'conversations' }" @click="activeTab = 'conversations'">Hội thoại</button>
        <button :class="{ active: activeTab === 'contacts' }" @click="activeTab = 'contacts'">Danh bạ</button>
      </div>

      <div class="sidebar-search">
        <input v-model="searchQuery" placeholder="Tìm kiếm..." @input="onSearch" />
      </div>

      <div class="sidebar-list">
        <div v-if="activeTab === 'conversations'" class="conversation-list">
          <div v-for="conv in filteredConversations" :key="conv.conversationId"
            class="conversation-item" :class="{ active: selectedConvId === conv.conversationId }"
            @click="selectConversation(conv)">
            <div class="conv-avatar">{{ getInitials(conv) }}</div>
            <div class="conv-info">
              <div class="conv-title">{{ conv.title || getParticipantNames(conv) }}</div>
              <div class="conv-preview" v-if="conv.lastMessage">{{ conv.lastMessage.content }}</div>
            </div>
            <div class="conv-meta">
              <div class="conv-time" v-if="conv.lastMessage">{{ formatTime(conv.lastMessage.sentAt) }}</div>
              <div class="unread-badge" v-if="conv.unreadCount > 0">{{ conv.unreadCount }}</div>
            </div>
          </div>
          <div v-if="filteredConversations.length === 0" class="empty-state">Chưa có hội thoại</div>
        </div>

        <div v-if="activeTab === 'contacts'" class="contact-list">
          <div v-for="contact in filteredContacts" :key="contact.employeeId"
            class="contact-item" @click="startConversation(contact)">
            <div class="contact-avatar">{{ contact.fullName.charAt(0).toUpperCase() }}</div>
            <div class="contact-info">
              <div class="contact-name">{{ contact.fullName }}</div>
              <div class="contact-dept">{{ contact.departmentName || '' }}</div>
            </div>
          </div>
          <div v-if="filteredContacts.length === 0" class="empty-state">Không tìm thấy</div>
        </div>
      </div>
    </div>

    <div class="chat-main" v-if="selectedConvId">
      <div class="chat-header">
        <div class="chat-header-info">
          <h3>{{ currentConvTitle }}</h3>
          <span class="chat-header-members">{{ currentConvParticipants }}</span>
        </div>
        <div class="chat-header-actions">
          <button class="btn-icon" @click="startCall('audio')" title="Gọi thoại">
            <i class="fas fa-phone"></i>
          </button>
          <button class="btn-icon" @click="startCall('video')" title="Gọi video">
            <i class="fas fa-video"></i>
          </button>
        </div>
      </div>

      <div class="messages-container" ref="messagesContainer">
        <div v-for="msg in messages" :key="msg.messageId" class="message-wrapper"
          :class="{ 'message-mine': msg.senderId === myEmployeeId, 'message-system': msg.messageType.startsWith('Call') }">
          <div class="message-sender" v-if="msg.senderId !== myEmployeeId">{{ msg.senderName }}</div>
          <div class="message-bubble" :class="{ 'call-message': msg.messageType.startsWith('Call') }">
            <template v-if="msg.messageType === 'Text'">
              <div class="message-text">{{ msg.content }}</div>
            </template>
            <template v-else-if="msg.messageType === 'CallOffer' || msg.messageType === 'CallAnswer'">
              <div class="call-info">
                <i class="fas fa-phone"></i>
                <span>{{ msg.messageType === 'CallOffer' ? 'Cuộc gọi đến' : 'Đã trả lời' }}</span>
              </div>
            </template>
            <template v-else-if="msg.messageType === 'CallEnd'">
              <div class="call-info">
                <i class="fas fa-phone-slash"></i>
                <span>Cuộc gọi kết thúc</span>
              </div>
            </template>
            <div class="message-time">{{ formatTime(msg.sentAt) }}</div>
          </div>
        </div>
      </div>

      <div class="typing-indicator" v-if="typingUser">{{ typingUser }} đang nhập...</div>

      <div class="chat-input">
        <input v-model="messageText" placeholder="Nhập tin nhắn..."
          @keydown.enter="sendMessage" @input="onTyping" />
        <button @click="sendMessage" :disabled="!messageText.trim()">
          <i class="fas fa-paper-plane"></i>
        </button>
      </div>
    </div>

    <div class="chat-main chat-empty" v-else>
      <div class="empty-state-large">
        <i class="fas fa-comments"></i>
        <p>Chọn một hội thoại hoặc liên hệ để bắt đầu chat</p>
      </div>
    </div>
  </div>
</template>

<script>
import { authState } from '../stores/auth'
import * as chatApi from '../services/chatApi'

export default {
  name: 'ChatPage',
  data() {
    return {
      activeTab: 'conversations',
      searchQuery: '',
      conversations: [],
      contacts: [],
      selectedConvId: null,
      messages: [],
      messageText: '',
      typingUser: '',
      myEmployeeId: null,
      typingTimeout: null,
      currentConvTitle: '',
      currentConvParticipants: '',
    }
  },
  computed: {
    user() { return authState.user },
    filteredConversations() {
      if (!this.searchQuery) return this.conversations
      const q = this.searchQuery.toLowerCase()
      return this.conversations.filter(c => {
        const title = c.title || this.getParticipantNames(c)
        return title.toLowerCase().includes(q)
      })
    },
    filteredContacts() {
      if (!this.searchQuery) return this.contacts
      const q = this.searchQuery.toLowerCase()
      return this.contacts.filter(c => c.fullName.toLowerCase().includes(q))
    }
  },
  async mounted() {
    this.myEmployeeId = authState.user?.employeeId
    await this.loadData()
    await this.connectSignalR()
  },
  beforeUnmount() {
    chatApi.disconnectChatHub()
  },
  methods: {
    async loadData() {
      try {
        const [convRes, contactRes] = await Promise.all([
          chatApi.getConversations(),
          chatApi.getContacts()
        ])
        this.conversations = convRes.data?.data || []
        this.contacts = contactRes.data?.data || []
      } catch (e) {
        console.error('Failed to load chat data', e)
      }
    },
    async connectSignalR() {
      try {
        const token = localStorage.getItem('token') || sessionStorage.getItem('token')
        await chatApi.connectChatHub(token)
        chatApi.onMessage(this.handleNewMessage)
        chatApi.onTyping(this.handleTyping)
        chatApi.onRead(this.handleRead)
        chatApi.onIncomingCall(this.handleIncomingCall)
        chatApi.onCallResponse(this.handleCallResponse)
        chatApi.onCallEnded(this.handleCallEnded)
      } catch (e) {
        console.error('Failed to connect chat hub', e)
      }
    },
    handleNewMessage(msg) {
      if (msg.conversationId === this.selectedConvId) {
        this.messages.push(msg)
        this.$nextTick(() => this.scrollToBottom())
        chatApi.markRead(this.selectedConvId)
      }
      this.loadData()
    },
    handleTyping(data) {
      if (data.conversationId === this.selectedConvId) {
        this.typingUser = data.fullName
        clearTimeout(this.typingTimeout)
        this.typingTimeout = setTimeout(() => { this.typingUser = '' }, 3000)
      }
    },
    handleRead(data) {
      if (data.conversationId === this.selectedConvId) {
        this.messages.forEach(m => {
          if (m.senderId !== this.myEmployeeId) {
            m.isRead = true
            m.readAt = data.readAt
          }
        })
      }
    },
    handleIncomingCall(data) {
      if (confirm(`Cuộc gọi từ ${data.fromFullName}. Trả lời?`)) {
        this.acceptCall(data)
      } else {
        chatApi.endCall(data.fromEmployeeId, data.conversationId)
      }
    },
    handleCallResponse(data) {
      this.showToast(`${data.fromFullName} đã trả lời cuộc gọi`)
    },
    handleCallEnded(data) {
      this.showToast('Cuộc gọi kết thúc')
    },
    async selectConversation(conv) {
      this.selectedConvId = conv.conversationId
      this.currentConvTitle = conv.title || this.getParticipantNames(conv)
      this.currentConvParticipants = this.getParticipantNames(conv)
      this.messages = []
      this.messageText = ''

      try {
        const res = await chatApi.getMessages(conv.conversationId)
        this.messages = res.data?.data || []
        this.$nextTick(() => this.scrollToBottom())
      } catch (e) {
        console.error(e)
      }

      await chatApi.markRead(conv.conversationId)
    },
    async startConversation(contact) {
      try {
        const res = await chatApi.createConversation([contact.employeeId])
        const convId = res.data?.data?.conversationId
        if (convId) {
          await this.loadData()
          const conv = this.conversations.find(c => c.conversationId === convId)
          if (conv) await this.selectConversation(conv)
        }
      } catch (e) {
        console.error(e)
      }
    },
    async sendMessage() {
      const content = this.messageText.trim()
      if (!content || !this.selectedConvId) return
      this.messageText = ''
      await chatApi.sendMessage(this.selectedConvId, content)
    },
    onTyping() {
      if (this.selectedConvId) {
        chatApi.sendTyping(this.selectedConvId)
      }
    },
    onSearch() { },
    async startCall(type) {
      if (!this.selectedConvId) return
      const participants = this.conversations.find(c => c.conversationId === this.selectedConvId)?.participants || []
      const target = participants.find(p => p.employeeId !== this.myEmployeeId)
      if (!target) return

      const signalingData = JSON.stringify({ type, conversationId: this.selectedConvId })
      await chatApi.callUser(target.employeeId, 'CallOffer', signalingData, this.selectedConvId)
      await chatApi.sendMessage(this.selectedConvId,
        `Cuộc gọi ${type === 'video' ? 'video' : 'thoại'} bắt đầu`,
        'CallOffer', signalingData)
    },
    async acceptCall(data) {
      await chatApi.callResponse(data.fromEmployeeId, 'CallAnswer',
        JSON.stringify({ accepted: true }))
    },
    getInitials(conv) {
      const name = conv.title || this.getParticipantNames(conv)
      return name.charAt(0).toUpperCase()
    },
    getParticipantNames(conv) {
      if (!conv.participants) return 'Hội thoại'
      return conv.participants
        .filter(p => p.employeeId !== this.myEmployeeId)
        .map(p => p.fullName)
        .join(', ')
    },
    formatTime(dateStr) {
      if (!dateStr) return ''
      const d = new Date(dateStr)
      const now = new Date()
      const isToday = d.toDateString() === now.toDateString()
      if (isToday) return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
      return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' })
    },
    scrollToBottom() {
      const container = this.$refs.messagesContainer
      if (container) container.scrollTop = container.scrollHeight
    },
    showToast(msg) {
      alert(msg)
    }
  }
}
</script>

<style scoped>
.chat-container {
  display: flex;
  height: calc(100vh - 120px);
  background: #f5f5f5;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.chat-sidebar {
  width: 320px;
  min-width: 320px;
  background: white;
  border-right: 1px solid #e0e0e0;
  display: flex;
  flex-direction: column;
}

.sidebar-tabs {
  display: flex;
  border-bottom: 1px solid #e0e0e0;
}

.sidebar-tabs button {
  flex: 1;
  padding: 12px;
  border: none;
  background: none;
  cursor: pointer;
  font-size: 14px;
  color: #666;
  transition: all 0.2s;
}

.sidebar-tabs button.active {
  color: #1976D2;
  border-bottom: 2px solid #1976D2;
  font-weight: 600;
}

.sidebar-search {
  padding: 12px;
}

.sidebar-search input {
  width: 100%;
  padding: 8px 12px;
  border: 1px solid #e0e0e0;
  border-radius: 20px;
  font-size: 13px;
  outline: none;
  box-sizing: border-box;
}

.sidebar-search input:focus {
  border-color: #1976D2;
}

.sidebar-list {
  flex: 1;
  overflow-y: auto;
}

.conversation-item,
.contact-item {
  display: flex;
  padding: 12px 16px;
  cursor: pointer;
  align-items: center;
  gap: 12px;
  transition: background 0.2s;
}

.conversation-item:hover,
.contact-item:hover {
  background: #f0f7ff;
}

.conversation-item.active {
  background: #e3f2fd;
}

.conv-avatar,
.contact-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: #1976D2;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  font-size: 16px;
  flex-shrink: 0;
}

.conv-info,
.contact-info {
  flex: 1;
  min-width: 0;
}

.conv-title,
.contact-name {
  font-size: 14px;
  font-weight: 500;
  color: #333;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.conv-preview {
  font-size: 12px;
  color: #999;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  margin-top: 2px;
}

.contact-dept {
  font-size: 12px;
  color: #999;
  margin-top: 2px;
}

.conv-meta {
  text-align: right;
  flex-shrink: 0;
}

.conv-time {
  font-size: 11px;
  color: #999;
}

.unread-badge {
  background: #1976D2;
  color: white;
  font-size: 11px;
  border-radius: 10px;
  padding: 2px 7px;
  margin-top: 4px;
  display: inline-block;
}

.chat-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  background: white;
}

.chat-empty {
  align-items: center;
  justify-content: center;
  color: #999;
}

.empty-state-large i {
  font-size: 64px;
  color: #ddd;
  display: block;
  margin-bottom: 16px;
}

.chat-header {
  padding: 12px 20px;
  border-bottom: 1px solid #e0e0e0;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.chat-header-info h3 {
  margin: 0;
  font-size: 16px;
  color: #333;
}

.chat-header-members {
  font-size: 12px;
  color: #999;
}

.chat-header-actions {
  display: flex;
  gap: 8px;
}

.btn-icon {
  width: 36px;
  height: 36px;
  border: 1px solid #e0e0e0;
  border-radius: 50%;
  background: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #666;
  transition: all 0.2s;
}

.btn-icon:hover {
  background: #e3f2fd;
  color: #1976D2;
  border-color: #1976D2;
}

.messages-container {
  flex: 1;
  overflow-y: auto;
  padding: 20px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.message-wrapper {
  display: flex;
  flex-direction: column;
  max-width: 70%;
}

.message-mine {
  align-self: flex-end;
  align-items: flex-end;
}

.message-system {
  align-self: center;
  max-width: 100%;
}

.message-sender {
  font-size: 12px;
  color: #666;
  margin-bottom: 2px;
  margin-left: 4px;
}

.message-bubble {
  padding: 10px 14px;
  border-radius: 18px;
  background: #f0f0f0;
  position: relative;
}

.message-mine .message-bubble {
  background: #1976D2;
  color: white;
  border-bottom-right-radius: 4px;
}

.message-wrapper:not(.message-mine) .message-bubble {
  border-bottom-left-radius: 4px;
}

.message-text {
  font-size: 14px;
  word-wrap: break-word;
}

.message-time {
  font-size: 10px;
  color: #999;
  margin-top: 4px;
  text-align: right;
}

.message-mine .message-time {
  color: rgba(255, 255, 255, 0.7);
}

.call-message {
  background: #f0f7ff !important;
  text-align: center;
  color: #1976D2;
}

.call-info {
  display: flex;
  align-items: center;
  gap: 8px;
  justify-content: center;
}

.typing-indicator {
  padding: 4px 20px;
  font-size: 12px;
  color: #999;
  font-style: italic;
}

.chat-input {
  display: flex;
  padding: 12px 20px;
  border-top: 1px solid #e0e0e0;
  gap: 8px;
}

.chat-input input {
  flex: 1;
  padding: 10px 16px;
  border: 1px solid #e0e0e0;
  border-radius: 24px;
  font-size: 14px;
  outline: none;
}

.chat-input input:focus {
  border-color: #1976D2;
}

.chat-input button {
  width: 42px;
  height: 42px;
  border: none;
  border-radius: 50%;
  background: #1976D2;
  color: white;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.2s;
}

.chat-input button:hover {
  background: #1565C0;
}

.chat-input button:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.empty-state {
  text-align: center;
  padding: 32px 16px;
  color: #999;
  font-size: 14px;
}
</style>
