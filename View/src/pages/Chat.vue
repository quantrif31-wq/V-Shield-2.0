<template>
  <div class="chat-container animate-in">
    <div class="chat-realtime-status chat-realtime-global" :class="`is-${hubStatus}`" role="status" aria-live="polite">
      <span class="status-dot" aria-hidden="true"></span>
      <span>{{ hubStatusLabel }}</span>
      <span v-if="hubLastUpdated">· Cập nhật {{ formatTime(hubLastUpdated) }}</span>
      <span v-if="hubStatus !== 'live'">· Tin nhắn vẫn được gửi qua API</span>
    </div>
    <div class="chat-sidebar">
      <div class="sidebar-tabs">
        <button :class="{ active: activeTab === 'conversations' }" @click="activeTab = 'conversations'">Hội thoại</button>
        <button :class="{ active: activeTab === 'contacts' }" @click="activeTab = 'contacts'">Danh bạ</button>
        <button v-if="activeTab === 'contacts'" style="margin-left:auto;padding:8px 10px;font-size:12px;border:none;background:none;cursor:pointer;color:var(--accent-primary);" @click="showFilters = !showFilters">
          <i class="fas fa-filter"></i>
        </button>
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
          <div v-if="showFilters" class="contact-filters">
            <select v-model="filterDepartment" @change="onSearch">
              <option value="">Tất cả phòng ban</option>
              <option v-for="dept in departments" :key="dept" :value="dept">{{ dept }}</option>
            </select>
            <select v-model="filterPosition" @change="onSearch">
              <option value="">Tất cả chức vụ</option>
              <option v-for="pos in positions" :key="pos" :value="pos">{{ pos }}</option>
            </select>
          </div>
          <div v-for="contact in filteredContacts" :key="contact.employeeId"
            class="contact-item" @click="startConversation(contact)" :title="`Phòng: ${contact.departmentName || '--'} | Chức vụ: ${contact.positionName || '--'}`">
            <div class="contact-avatar" :style="{ background: getAvatarColor(contact.fullName) }">{{ contact.fullName.charAt(0).toUpperCase() }}</div>
            <div class="contact-info">
              <div class="contact-name">{{ contact.fullName }}</div>
              <div class="contact-dept">{{ contact.departmentName || '' }}</div>
              <div class="contact-meta">
                <span v-if="contact.positionName" class="contact-position">{{ contact.positionName }}</span>
                <span v-if="contact.email" class="contact-email">{{ contact.email }}</span>
              </div>
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
          :class="{ 'message-mine': isOwnMessage(msg), 'message-system': msg.messageType.startsWith('Call') }">
          <div class="message-sender" v-if="!isOwnMessage(msg)">{{ msg.senderName }}</div>
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
      <div class="chat-error" v-if="sendError">{{ sendError }}</div>

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
      showFilters: false,
      filterDepartment: '',
      filterPosition: '',
      sendError: '',
      hubConnected: false,
      hubStatus: 'disconnected',
      hubLastUpdated: null,
      hubUnsubscribers: [],
      refreshTimer: null,
    }
  },
  computed: {
    user() { return authState.user },
    hubStatusLabel() {
      return {
        connecting: 'Đang kết nối',
        live: 'Live',
        reconnecting: 'Đang kết nối lại',
        stale: 'Dữ liệu có thể đã cũ',
        disconnected: 'Đã ngắt kết nối',
      }[this.hubStatus] || 'Đã ngắt kết nối'
    },
    filteredConversations() {
      if (!this.searchQuery) return this.conversations
      const q = this.searchQuery.toLowerCase()
      return this.conversations.filter(c => {
        const title = c.title || this.getParticipantNames(c)
        return title.toLowerCase().includes(q)
      })
    },
    departments() {
      return [...new Set(this.contacts.map(c => c.departmentName).filter(Boolean))]
    },
    positions() {
      return [...new Set(this.contacts.map(c => c.positionName).filter(Boolean))]
    },
    filteredContacts() {
      let result = this.contacts
      const q = this.searchQuery?.toLowerCase()
      if (q) {
        result = result.filter(c =>
          c.fullName.toLowerCase().includes(q) ||
          (c.departmentName || '').toLowerCase().includes(q) ||
          (c.positionName || '').toLowerCase().includes(q) ||
          (c.email || '').toLowerCase().includes(q)
        )
      }
      if (this.filterDepartment) {
        result = result.filter(c => c.departmentName === this.filterDepartment)
      }
      if (this.filterPosition) {
        result = result.filter(c => c.positionName === this.filterPosition)
      }
      return result
    }
  },
  async mounted() {
    this.myEmployeeId = authState.user?.employeeId
    await this.loadData()
    await this.connectSignalR()
    this.startRefreshLoop()
  },
  beforeUnmount() {
    if (this.refreshTimer) {
      clearInterval(this.refreshTimer)
      this.refreshTimer = null
    }
    this.hubUnsubscribers.forEach(unsubscribe => unsubscribe())
    this.hubUnsubscribers = []
    void chatApi.disconnectChatHub()
  },
  methods: {
    normalizeEmployeeId(value) {
      if (value === null || value === undefined || value === '') return null
      const normalized = Number(value)
      return Number.isNaN(normalized) ? String(value) : normalized
    },
    normalizeText(value) {
      return String(value || '').trim().toLowerCase()
    },
    isSameEmployee(left, right) {
      const normalizedLeft = this.normalizeEmployeeId(left)
      const normalizedRight = this.normalizeEmployeeId(right)
      return normalizedLeft !== null && normalizedLeft === normalizedRight
    },
    isOwnMessage(message) {
      if (this.isSameEmployee(message?.senderId, this.myEmployeeId)) {
        return true
      }

      const senderName = this.normalizeText(message?.senderName)
      const currentFullName = this.normalizeText(this.user?.fullName)
      const currentUsername = this.normalizeText(this.user?.username)

      return !!senderName && (senderName === currentFullName || senderName === currentUsername)
    },
    buildMessageDedupKey(message) {
      if (!message) return null

      if (message.messageId !== null && message.messageId !== undefined && !String(message.messageId).startsWith('temp-')) {
        return `message:${message.messageId}`
      }

      if (message.clientMessageId) {
        const senderId = this.normalizeEmployeeId(message.senderId)
        return `client:${message.conversationId}:${senderId}:${message.clientMessageId}`
      }

      const senderId = this.normalizeEmployeeId(message.senderId)
      const senderName = this.normalizeText(message.senderName)
      const content = this.normalizeText(message.content)
      const messageType = String(message.messageType || 'Text').trim()
      const sentAt = message.sentAt ? new Date(message.sentAt).toISOString() : ''

      return `fallback:${message.conversationId}:${senderId ?? senderName}:${messageType}:${content}:${sentAt}`
    },
    deduplicateMessages(messages) {
      const uniqueMessages = []
      const seen = new Set()

      for (const message of messages || []) {
        const dedupKey = this.buildMessageDedupKey(message)
        if (dedupKey && seen.has(dedupKey)) {
          continue
        }

        if (dedupKey) {
          seen.add(dedupKey)
        }

        uniqueMessages.push(message)
      }

      return uniqueMessages
    },
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
    async refreshSelectedConversation() {
      if (!this.selectedConvId) return

      try {
        const res = await chatApi.getMessages(this.selectedConvId)
        this.messages = this.deduplicateMessages(res.data?.data || [])
      } catch (e) {
        console.error('Failed to refresh messages', e)
      }
    },
    startRefreshLoop() {
      this.refreshTimer = setInterval(async () => {
        await this.loadData()
        await this.refreshSelectedConversation()
      }, 3000)
    },
    async connectSignalR() {
      this.hubUnsubscribers.push(chatApi.onChatConnectionState((state) => {
        this.hubStatus = state.status
        this.hubConnected = state.status === 'live'
        this.hubLastUpdated = state.lastUpdated
      }))
      try {
        await chatApi.connectChatHub()
        this.hubUnsubscribers.push(
          chatApi.onMessage(this.handleNewMessage),
          chatApi.onTyping(this.handleTyping),
          chatApi.onRead(this.handleRead),
          chatApi.onIncomingCall(this.handleIncomingCall),
          chatApi.onCallResponse(this.handleCallResponse),
          chatApi.onCallEnded(this.handleCallEnded),
        )
      } catch (e) {
        console.error('Failed to connect chat hub', e)
      }
    },
    handleNewMessage(msg) {
      if (msg.conversationId === this.selectedConvId) {
        const existingMessageIndex = this.messages.findIndex(existing =>
          existing.messageId === msg.messageId ||
          (msg.clientMessageId && existing.clientMessageId === msg.clientMessageId)
        )
        if (existingMessageIndex !== -1) {
          this.messages.splice(existingMessageIndex, 1, {
            ...this.messages[existingMessageIndex],
            ...msg,
            pending: false,
          })
          this.$nextTick(() => this.scrollToBottom())
          chatApi.markRead(this.selectedConvId)
          this.loadData()
          return
        }

        const pendingIndex = this.messages.findIndex(existing =>
          existing.pending &&
          (
            (msg.clientMessageId && existing.clientMessageId === msg.clientMessageId) ||
            (this.isOwnMessage(msg) && existing.content === msg.content)
          )
        )
        if (pendingIndex !== -1) {
          this.messages.splice(pendingIndex, 1, {
            ...this.messages[pendingIndex],
            ...msg,
            pending: false,
          })
        } else {
          this.messages.push(msg)
        }
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
          if (!this.isOwnMessage(m)) {
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
      this.sendError = ''

      try {
        const res = await chatApi.getMessages(conv.conversationId)
        this.messages = this.deduplicateMessages(res.data?.data || [])
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
      this.sendError = ''
      this.messageText = ''
      const clientMessageId = `local-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`
      const tempMessage = {
        messageId: `temp-${Date.now()}`,
        conversationId: this.selectedConvId,
        senderId: this.myEmployeeId,
        clientMessageId,
        senderName: this.user?.fullName || this.user?.username || 'Bạn',
        content,
        messageType: 'Text',
        sentAt: new Date().toISOString(),
        isRead: false,
        pending: true,
      }

      this.messages.push(tempMessage)
      this.$nextTick(() => this.scrollToBottom())

      try {
        const result = await chatApi.sendMessage(this.selectedConvId, content, 'Text', null, clientMessageId)
        if (result?.data) {
          const index = this.messages.findIndex(m =>
            m.messageId === tempMessage.messageId ||
            m.clientMessageId === clientMessageId
          )
          if (index !== -1) {
            this.messages.splice(index, 1, {
              ...result.data,
              pending: false,
            })
          }
        }
        await this.loadData()
      } catch (e) {
        this.messages = this.messages.filter(m => m.messageId !== tempMessage.messageId)
        this.messageText = content
        this.sendError = e?.response?.data?.message || 'Không gửi được tin nhắn. Vui lòng thử lại.'
        console.error(e)
      }
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
      const target = participants.find(p => !this.isSameEmployee(p.employeeId, this.myEmployeeId))
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
    getAvatarColor(name) {
      const colors = ['#1976D2','#388E3C','#D32F2F','#F57C00','#7B1FA2','#00796B','#5C6BC0','#E64A19','#C2185B','#303F9F']
      let hash = 0
      for (let i = 0; i < (name || '').length; i++) hash = name.charCodeAt(i) + ((hash << 5) - hash)
      return colors[Math.abs(hash) % colors.length]
    },
    getParticipantNames(conv) {
      if (!conv.participants) return 'Hội thoại'
      return conv.participants
        .filter(p => !this.isSameEmployee(p.employeeId, this.myEmployeeId))
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
  position: relative;
  display: flex;
  height: calc(100vh - 120px);
  background: var(--surface-subtle);
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.chat-sidebar {
  width: 320px;
  min-width: 320px;
  background: var(--surface-default);
  border-right: 1px solid var(--border-subtle);
  display: flex;
  flex-direction: column;
}

.sidebar-tabs {
  display: flex;
  border-bottom: 1px solid var(--border-subtle);
}

.sidebar-tabs button {
  flex: 1;
  padding: 12px;
  border: none;
  background: none;
  cursor: pointer;
  font-size: 14px;
  color: var(--text-muted);
  transition: all 0.2s;
}

.sidebar-tabs button.active {
  color: var(--accent-primary);
  border-bottom: 2px solid var(--accent-primary);
  font-weight: 600;
}

.sidebar-search {
  padding: 12px;
}

.sidebar-search input {
  width: 100%;
  padding: 8px 12px;
  border: 1px solid var(--border-subtle);
  border-radius: 20px;
  font-size: 13px;
  outline: none;
  box-sizing: border-box;
}

.sidebar-search input:focus {
  border-color: var(--accent-primary);
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
  transition: background 0.2s, transform 0.2s, box-shadow 0.2s;
}

.conversation-item:hover,
.contact-item:hover {
  background: var(--surface-hover);
  transform: translateY(-1px);
  box-shadow: var(--shadow-xs);
}

.conversation-item.active {
  background: var(--surface-selected);
}

.conv-avatar,
.contact-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: #1976D2;
  color: var(--text-on-interactive);
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
  color: var(--text-primary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.conv-preview {
  font-size: 12px;
  color: var(--text-muted);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  margin-top: 2px;
}

.contact-dept {
  font-size: 12px;
  color: var(--text-muted);
  margin-top: 2px;
}

.contact-meta {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  margin-top: 2px;
}

.contact-position {
  font-size: 11px;
  color: var(--accent-primary);
  background: var(--status-info-bg);
  padding: 1px 6px;
  border-radius: 4px;
}

.contact-email {
  font-size: 11px;
  color: var(--text-muted);
}

.contact-filters {
  padding: 8px 12px;
  display: flex;
  gap: 6px;
  border-bottom: 1px solid var(--border-subtle);
}

.contact-filters select {
  flex: 1;
  padding: 6px 8px;
  font-size: 12px;
  border: 1px solid var(--border-subtle);
  border-radius: 6px;
  outline: none;
  background: var(--surface-default);
}

.conv-meta {
  text-align: right;
  flex-shrink: 0;
}

.conv-time {
  font-size: 11px;
  color: var(--text-muted);
}

.unread-badge {
  background: var(--accent-primary);
  color: var(--text-on-interactive);
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
  background: var(--surface-default);
}

.chat-empty {
  align-items: center;
  justify-content: center;
  color: var(--text-muted);
}

.empty-state-large i {
  font-size: 64px;
  color: var(--text-disabled);
  display: block;
  margin-bottom: 16px;
}

.chat-header {
  padding: 12px 20px;
  border-bottom: 1px solid var(--border-subtle);
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.chat-header-info h3 {
  margin: 0;
  font-size: 16px;
  color: var(--text-primary);
}

.chat-header-members {
  font-size: 12px;
  color: var(--text-muted);
}

.chat-header-actions {
  display: flex;
  gap: 8px;
}

.btn-icon {
  width: 36px;
  height: 36px;
  border: 1px solid var(--border-subtle);
  border-radius: 50%;
  background: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--text-muted);
  transition: all 0.2s;
}

.btn-icon:hover {
  background: var(--surface-selected);
  color: var(--accent-primary);
  border-color: var(--accent-primary);
  transform: translateY(-1px);
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
  color: var(--text-muted);
  margin-bottom: 2px;
  margin-left: 4px;
}

.message-bubble {
  padding: 10px 14px;
  border-radius: 18px;
  background: var(--surface-subtle);
  position: relative;
}

.message-mine .message-bubble {
  background: #1976D2;
  color: var(--text-on-interactive);
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
  color: var(--text-muted);
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
  color: var(--text-muted);
  font-style: italic;
}

.chat-realtime-status,
.chat-error {
  padding: 4px 20px 0;
  font-size: 12px;
}

.chat-realtime-status {
  display: flex;
  align-items: center;
  gap: 5px;
  color: var(--status-success-text);
}
.chat-realtime-global { position: absolute; z-index: 2; right: 16px; top: 8px; padding: 4px 8px; border-radius: 999px; background: rgba(255,255,255,.92); }

.chat-realtime-status:not(.is-live) {
  color: var(--status-warning-text);
}

.chat-realtime-status.is-disconnected { color: var(--status-danger-text); }
.status-dot { width: 7px; height: 7px; border-radius: 50%; background: currentColor; }

.chat-error {
  color: var(--status-danger-text);
}

.chat-input {
  display: flex;
  padding: 12px 20px;
  border-top: 1px solid var(--border-subtle);
  gap: 8px;
}

.chat-input input {
  flex: 1;
  padding: 10px 16px;
  border: 1px solid var(--border-subtle);
  border-radius: 24px;
  font-size: 14px;
  outline: none;
}

.chat-input input:focus {
  border-color: var(--accent-primary);
}

.chat-input button {
  width: 42px;
  height: 42px;
  border: none;
  border-radius: 50%;
  background: #1976D2;
  color: var(--text-on-interactive);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.2s, transform 0.2s, box-shadow 0.2s;
}

.chat-input button:hover:not(:disabled) {
  background: #1565C0;
  transform: translateY(-1px);
  box-shadow: 0 6px 14px rgba(25, 118, 210, 0.3);
}

.chat-input button:disabled {
  background: var(--interactive-disabled);
  cursor: not-allowed;
}

.empty-state {
  text-align: center;
  padding: 32px 16px;
  color: var(--text-muted);
  font-size: 14px;
}
</style>
