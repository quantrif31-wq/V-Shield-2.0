<template>
  <div class="ai-chatbot">
    <button
      v-if="!chatOpen"
      class="chat-fab"
      :class="{ 'has-unread': hasUnread }"
      :style="fabStyle"
      aria-label="Mở trợ lý AI"
      @pointerdown="startDrag"
      @click="handleFabClick"
    >
      <svg class="fab-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7">
        <path d="M12 3a9 9 0 019 9c0 4.97-4.03 9-9 9a8.7 8.7 0 01-3.8-.87L3 21l.9-3.2A8.8 8.8 0 013 12a9 9 0 019-9z"/>
        <path d="M8.5 12h.01M12 12h.01M15.5 12h.01" stroke-linecap="round"/>
      </svg>
      <span class="fab-badge" v-if="!hasInteracted">AI</span>
    </button>

    <Transition name="chat-slide">
      <div v-if="chatOpen" class="chat-panel" :class="{ 'is-streaming': streaming }">
        <!-- Header -->
        <div class="chat-header">
          <div class="chat-avatar">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
              <path d="M12 3a9 9 0 019 9c0 4.97-4.03 9-9 9a8.7 8.7 0 01-3.8-.87L3 21l.9-3.2A8.8 8.8 0 013 12a9 9 0 019-9z"/>
              <path d="M8.5 12h.01M12 12h.01M15.5 12h.01" stroke-linecap="round"/>
            </svg>
          </div>
          <div class="chat-header-info">
            <span class="chat-header-title">Trợ lý V-Shield</span>
            <div class="chat-header-status">
              <span class="status-dot" :class="{ live: !streaming }"></span>
              <span>{{ streaming ? 'Đang trả lời…' : (connected ? 'DeepSeek · trực tuyến' : 'Chưa kết nối AI') }}</span>
            </div>
          </div>
          <div class="chat-header-actions">
            <button class="icon-btn" title="Xoá hội thoại" aria-label="Xoá hội thoại" @click="clearChat">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
                <path d="M3 6h18M8 6V4a1 1 0 011-1h6a1 1 0 011 1v2m3 0v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6h14z"/>
                <path d="M10 11v6M14 11v6"/>
              </svg>
            </button>
            <button class="icon-btn" title="Đóng chat" aria-label="Đóng chat" @click="closeChat">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
              </svg>
            </button>
          </div>
        </div>

        <!-- Messages -->
        <div ref="messagesRef" class="chat-messages" @click="handleMsgClick">
          <div v-if="messages.length === 0" class="chat-welcome">
            <div class="welcome-orb">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                <path d="M12 3a9 9 0 019 9c0 4.97-4.03 9-9 9a8.7 8.7 0 01-3.8-.87L3 21l.9-3.2A8.8 8.8 0 013 12a9 9 0 019-9z"/>
                <path d="M8.5 12h.01M12 12h.01M15.5 12h.01" stroke-linecap="round"/>
              </svg>
            </div>
            <h3 class="welcome-title">Chào bạn 👋</h3>
            <p class="welcome-text">Mình là trợ lý AI của V-Shield. Hỏi mình bất cứ điều gì về vận hành, gửi xe QR, nhân sự hay an ninh.</p>
          </div>

          <div v-for="msg in messages" :key="msg.id" class="chat-msg" :class="msg.role">
            <div v-if="msg.role === 'ai'" class="msg-avatar">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
                <path d="M12 3a9 9 0 019 9c0 4.97-4.03 9-9 9a8.7 8.7 0 01-3.8-.87L3 21l.9-3.2A8.8 8.8 0 013 12a9 9 0 019-9z"/>
              </svg>
            </div>
            <div class="msg-bubble" :class="{ error: msg.error }">
              <div class="msg-text" v-html="renderMarkdown(msg.text)"></div>
              <div v-if="msg.error" class="msg-error-row">
                <button class="retry-btn" @click.stop="retryLast">Thử lại</button>
              </div>
              <div v-if="msg.role === 'ai' && msg.text && !msg.error" class="msg-meta">
                <button class="copy-btn" title="Sao chép" @click.stop="copyText(msg.text)">Sao chép</button>
                <span class="msg-time">{{ formatTime(msg.ts) }}</span>
              </div>
              <span v-if="streaming && msg.id === currentStreamId" class="stream-caret"></span>
            </div>
          </div>

          <div v-if="streaming && !currentStreamId" class="chat-msg ai">
            <div class="msg-avatar">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
                <path d="M12 3a9 9 0 019 9c0 4.97-4.03 9-9 9a8.7 8.7 0 01-3.8-.87L3 21l.9-3.2A8.8 8.8 0 013 12a9 9 0 019-9z"/>
              </svg>
            </div>
            <div class="msg-bubble typing">
              <span class="typing-dot"></span><span class="typing-dot"></span><span class="typing-dot"></span>
            </div>
          </div>
        </div>

        <!-- Agent activity (agent đang làm việc) -->
        <div v-if="agentSteps.length" class="agent-activity" :class="{ working: isAgentWorking() }">
          <div class="agent-activity-header">
            <span class="agent-orb" :class="{ spinning: isAgentWorking() }"></span>
            <span class="agent-activity-title">{{ isAgentWorking() ? 'Agent đang làm việc' : 'Agent đã hoàn thành' }}</span>
            <span class="agent-activity-count">{{ agentSteps.length }} bước</span>
            <button class="agent-collapse" :title="activityCollapsed ? 'Xem chi tiết' : 'Thu gọn'" @click="activityCollapsed = !activityCollapsed">
              {{ activityCollapsed ? '▾' : '▴' }}
            </button>
          </div>
          <div v-if="!activityCollapsed" class="agent-steps">
            <div v-for="(s, i) in agentSteps" :key="i" class="agent-step" :class="{ done: s.status === 'done', active: s.status === 'running', fail: s.ok === false }">
              <span class="step-icon">
                <span v-if="s.status === 'running'" class="step-spinner"></span>
                <span v-else-if="s.ok === false" class="step-x">✕</span>
                <span v-else class="step-check">✓</span>
              </span>
              <span class="step-skill" :title="s.tool">{{ skillIcon(s.tool) }}</span>
              <span class="step-label">{{ s.label }}</span>
            </div>
          </div>
        </div>

        <!-- Draft email (agent soạn) -->
        <div v-if="drafts.length" class="chat-drafts">
          <div v-for="d in drafts" :key="d.id" class="draft-card">
            <div class="draft-header">
              <span class="draft-title">📧 Email nháp #{{ d.id }}</span>
              <button class="draft-close" title="Đóng nháp" @click="removeDraft(d)">✕</button>
            </div>
            <input v-model="d.to" class="draft-input" placeholder="Người nhận (email, cách nhau dấu ;)" />
            <input v-model="d.subject" class="draft-input" placeholder="Tiêu đề" />
            <textarea v-model="d.body" class="draft-textarea" placeholder="Nội dung" rows="6"></textarea>
            <div class="draft-actions">
              <button class="draft-btn primary" :disabled="d.sending" @click="sendDraft(d)">
                {{ d.sending ? 'Đang gửi…' : 'Gửi' }}
              </button>
              <button class="draft-btn" :disabled="d.refining" @click="refineDraft(d, 'Viết lại cho chuẩn chuyên nghiệp, trang trọng, giữ nguyên ý.')">Viết lại</button>
              <button class="draft-btn" :disabled="d.refining" @click="refineDraft(d, 'Viết ngắn gọn hơn, súc tích.')">Ngắn gọn</button>
            </div>
            <div v-if="d.result" class="draft-result" :class="{ ok: d.sent }">{{ d.result }}</div>
            <div v-if="d.refineMsg" class="draft-result">{{ d.refineMsg }}</div>
          </div>
        </div>

        <!-- Suggestions -->
        <div v-if="messages.length <= 1 && !streaming" class="chat-suggestions">
          <button v-for="s in suggestions" :key="s.id" class="suggestion-chip" @click="sendSuggestion(s)">
            <span class="suggestion-icon">{{ s.icon }}</span>
            <span>{{ s.label }}</span>
          </button>
        </div>

        <!-- Input -->
        <div class="chat-input-bar">
          <textarea
            v-model="inputText"
            ref="inputRef"
            rows="1"
            :placeholder="streaming ? 'AI đang trả lời…' : 'Hỏi trợ lý V-Shield…'"
            :disabled="streaming"
            @keydown.enter.exact.prevent="sendMessage"
            @keydown.enter.shift.prevent="insertNewline"
            @input="autogrow"
          />
          <button
            v-if="streaming"
            class="chat-send-btn stop"
            aria-label="Dừng"
            @click="stopStream"
          >
            <svg viewBox="0 0 24 24" fill="currentColor"><rect x="6" y="6" width="12" height="12" rx="2"/></svg>
          </button>
          <button
            v-else
            class="chat-send-btn"
            :disabled="!inputText.trim()"
            aria-label="Gửi"
            @click="sendMessage"
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="22" y1="2" x2="11" y2="13"/><polyline points="22 2 15 22 11 13 2 9 22 2"/>
            </svg>
          </button>
        </div>
        <div class="chat-footer-note">
          <span v-if="!connected">AI chưa được cấu hình — liên hệ quản trị viên.</span>
          <span v-else>AI có thể sai sót — hãy kiểm tra thông tin quan trọng.</span>
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup>
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { API_BASE_URL } from '../config/api.js'

const router = useRouter()
const route = useRoute()

const AUTH_TOKEN_KEY = 'v_shield_token'
const STORE_KEY = 'vshield_ai_chat_v2'
const THREAD_KEY = 'vshield_ai_thread'
const DRAFTS_KEY = 'vshield_ai_drafts'

const chatOpen = ref(false)
const messages = ref([])
const inputText = ref('')
const streaming = ref(false)
const hasInteracted = ref(false)
const connected = ref(true)
const currentStreamId = ref(null)
const controller = ref(null)
const messagesRef = ref(null)
const inputRef = ref(null)
const threadId = ref(sessionStorage.getItem(THREAD_KEY) || '')
const drafts = ref([])
const statusText = ref('')
const agentSteps = ref([])
const activityCollapsed = ref(false)

// FAB drag
const fabStyle = ref({})
const dragState = ref({ active: false, startX: 0, startY: 0, x: 0, y: 0 })

const suggestions = [
  { id: 'huongdan', icon: '🧭', label: 'Hướng dẫn dùng V-Shield', text: 'Hướng dẫn tôi cách sử dụng V-Shield một cách tổng quan.' },
  { id: 'guixe', icon: '🚗', label: 'Quy trình gửi xe QR', text: 'Trình bày quy trình gửi xe bằng QR ở cổng ra vào từng bước.' },
  { id: 'baove', icon: '🛡️', label: 'Bảo vệ trực cổng', text: 'Bảo vệ cần làm những gì khi trực cổng và xác thực ra vào?' },
  { id: 'qrloi', icon: '⚙️', label: 'Xử lý khi QR/camera lỗi', text: 'Làm sao để xử lý thủ công khi QR hoặc camera gặp sự cố?' },
  { id: 'taoqrdong', icon: '🔳', label: 'Tạo QR động', text: 'Cách tạo QR động cho nhân viên và kiểm tra giá trị của nó.' },
  { id: 'chamcong', icon: '📊', label: 'Chấm công ra vào', text: 'Chấm công được ghi nhận như thế nào khi xe ra vào cổng?' }
]

const hasUnread = computed(() => messages.value.length === 0 && hasInteracted.value === false && chatOpen.value === false)

// ---------- markdown (safe) ----------
function esc(s) {
  return String(s || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;')
}

function renderInline(text) {
  let t = esc(text)
  t = t.replace(/^(#{1,3})\s+(.+)$/gm, (m, h, body) => `<h${Math.min(h.length + 2, 4)}>${body}</h${Math.min(h.length + 2, 4)}>`)
  t = t.replace(/`([^`\n]+)`/g, '<code>$1</code>')
  t = t.replace(/\*\*([^*\n]+)\*\*/g, '<strong>$1</strong>')
  t = t.replace(/(^|[^*])\*([^*\n]+)\*/g, '$1<em>$2</em>')
  t = t.replace(/\[([^\]]+)\]\((https?:\/\/[^)\s]+)\)/g, '<a href="$2" target="_blank" rel="noopener noreferrer">$1</a>')
  t = t.replace(/^[ \t]*[-*]\s+(.+)$/gm, '<span class="li">• $1</span>')
  t = t.replace(/^[ \t]*(\d+)\.\s+(.+)$/gm, '<span class="li">$1. $2</span>')
  t = t.replace(/\n{2,}/g, '<br><br>')
  t = t.replace(/\n/g, '<br>')
  return t
}

function renderMarkdown(src) {
  if (!src) return ''
  const parts = String(src).split(/```(\w*)\n?([\s\S]*?)```/g)
  let html = ''
  for (let i = 0; i < parts.length; i++) {
    if (i % 3 === 0) html += renderInline(parts[i])
    else if (i % 3 === 2) html += `<pre><code>${esc(parts[i].trim())}</code></pre>`
  }
  return html
}

// ---------- chat actions ----------
function token() {
  return sessionStorage.getItem(AUTH_TOKEN_KEY) || localStorage.getItem(AUTH_TOKEN_KEY) || ''
}

function addMessage(role, text, extra = {}) {
  const msg = { id: 'm' + Date.now() + Math.random().toString(36).slice(2, 7), role, text, ts: Date.now(), ...extra }
  messages.value.push(msg)
  persist()
  scrollDown()
  return msg
}

function scrollDown() {
  nextTick(() => {
    const el = messagesRef.value
    if (el) el.scrollTop = el.scrollHeight
  })
}

function formatTime(ts) {
  return new Date(ts).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
}

async function sendMessage() {
  const text = inputText.value.trim()
  if (!text || streaming.value) return
  hasInteracted.value = true
  inputText.value = ''
  autogrow()

  addMessage('user', text)

  streaming.value = true
  connected.value = true
  statusText.value = 'Đang xử lý…'
  currentStreamId.value = null
  controller.value = new AbortController()
  agentSteps.value = [{ tool: 'thinking', label: 'Đang phân tích yêu cầu…', status: 'running' }]
  activityCollapsed.value = false

  const aiMsg = addMessage('ai', '', { id: null })
  aiMsg.id = 'm' + Date.now() + Math.random().toString(36).slice(2, 7)
  currentStreamId.value = aiMsg.id
  scrollDown()

  try {
    const res = await fetch(`${API_BASE_URL}/ai-chat/stream`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token()}`
      },
      body: JSON.stringify({ threadId: threadId.value || undefined, message: text }),
      signal: controller.value.signal
    })

    if (!res.ok) throw new Error(`HTTP ${res.status}`)

    const reader = res.body.getReader()
    const decoder = new TextDecoder()
    let buffer = ''
    let done = false

    while (!done) {
      const { done: rd, value } = await reader.read()
      if (rd) break
      buffer += decoder.decode(value, { stream: true })
      let nl
      while ((nl = buffer.indexOf('\n')) !== -1) {
        let line = buffer.slice(0, nl).trim()
        buffer = buffer.slice(nl + 1)
        if (!line.startsWith('data:')) continue
        const data = line.slice(5).trim()
        if (data === '[DONE]') { done = true; break }
        if (!data) continue
        try {
          const j = JSON.parse(data)
          if (j.type === 'tool_start') {
            finishRunningSteps()
            agentSteps.value.push({ tool: j.tool, label: j.label, status: 'running' })
            statusText.value = j.label
            scrollDown()
          }
          if (j.type === 'tool_done') {
            const idx = [...agentSteps.value].reverse().findIndex((s) => s.tool === j.tool && s.status === 'running')
            if (idx >= 0) {
              const real = agentSteps.value.length - 1 - idx
              agentSteps.value[real].status = 'done'
              agentSteps.value[real].ok = j.ok !== false
              if (j.label) agentSteps.value[real].label = j.label
            }
          }
          if (j.status) statusText.value = j.status
          if (j.token) {
            aiMsg.text += j.token
            finishRunningSteps('Đã soạn xong câu trả lời.')
            statusText.value = ''
            scrollDown()
          }
          if (j.threadId) {
            threadId.value = j.threadId
            sessionStorage.setItem(THREAD_KEY, j.threadId)
          }
          if (j.draft && j.draft.id) upsertDraft(j.draft)
          if (j.error) {
            aiMsg.error = true
            aiMsg.text = aiMsg.text || j.error
            connected.value = false
            finishRunningSteps('Có lỗi xảy ra.')
            done = true
          }
          if (j.done) { done = true }
        } catch {}
      }
    }
  } catch (e) {
    if (e.name !== 'AbortError') {
      aiMsg.error = true
      aiMsg.text = aiMsg.text || `Không kết nối được AI. ${e.message}`
      connected.value = false
    } else {
      aiMsg.text = aiMsg.text || '(đã dừng)'
    }
  } finally {
    streaming.value = false
    statusText.value = ''
    currentStreamId.value = null
    controller.value = null
    persist()
    scrollDown()
  }
}

function upsertDraft(d) {
  const i = drafts.value.findIndex((x) => x.id === d.id)
  if (i >= 0) drafts.value[i] = { ...drafts.value[i], ...d, sending: false, sent: false, refineMsg: '' }
  else drafts.value.push({ ...d, sending: false, sent: false, refineMsg: '' })
  persistDrafts()
  scrollDown()
}

function finishRunningSteps(label) {
  let changed = false
  agentSteps.value.forEach((s) => {
    if (s.status === 'running') {
      s.status = 'done'
      if (label) s.label = label
      changed = true
    }
  })
  if (changed) scrollDown()
}

function skillIcon(tool) {
  return (
    {
      search_people: '🔍',
      get_person: '👤',
      get_me: '🧑‍💼',
      get_org_relation: '🏢',
      resolve_greeting: '🤝',
      draft_email: '📧',
      save_note: '📝',
      get_note: '📖',
      thinking: '🧠'
    }[tool] || '⚙️'
  )
}

function isAgentWorking() {
  return agentSteps.value.some((s) => s.status === 'running')
}

function persistDrafts() {
  try { sessionStorage.setItem(DRAFTS_KEY, JSON.stringify(drafts.value.map((d) => ({ id: d.id, to: d.to, subject: d.subject, body: d.body })))) } catch {}
}

async function sendDraft(d) {
  if (d.sending) return
  d.sending = true
  d.result = ''
  try {
    const res = await fetch(`${API_BASE_URL}/ai-chat/send-draft`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token()}` },
      body: JSON.stringify({ draftId: d.id, to: d.to, subject: d.subject, body: d.body })
    })
    const j = await res.json().catch(() => ({}))
    if (!res.ok) throw new Error(j.message || `HTTP ${res.status}`)
    d.sent = true
    d.result = j.message || 'Đã gửi.'
  } catch (e) {
    d.result = 'Lỗi gửi: ' + e.message
  } finally {
    d.sending = false
  }
}

async function refineDraft(d, instruction) {
  d.refining = true
  d.refineMsg = ''
  try {
    const res = await fetch(`${API_BASE_URL}/ai-chat/refine-draft`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token()}` },
      body: JSON.stringify({ draftId: d.id, instruction })
    })
    const j = await res.json().catch(() => ({}))
    if (!res.ok) throw new Error(j.message || `HTTP ${res.status}`)
    if (j.draft) {
      d.to = j.draft.to || d.to
      d.subject = j.draft.subject
      d.body = j.draft.body
    }
    d.refineMsg = 'Đã viết lại.'
  } catch (e) {
    d.refineMsg = 'Lỗi: ' + e.message
  } finally {
    d.refining = false
  }
}

function removeDraft(d) {
  drafts.value = drafts.value.filter((x) => x.id !== d.id)
  persistDrafts()
}

function stopStream() {
  if (controller.value) controller.value.abort()
}

function retryLast() {
  const lastUser = [...messages.value].reverse().find((m) => m.role === 'user')
  // bỏ lượt AI lỗi
  const idx = messages.value.findIndex((m) => m.role === 'ai' && m.error)
  if (idx >= 0) messages.value.splice(idx, 1)
  if (lastUser) {
    inputText.value = lastUser.text
    sendMessage()
  }
}

function sendSuggestion(s) {
  inputText.value = s.text
  sendMessage()
}

function clearChat() {
  if (streaming.value) stopStream()
  messages.value = []
  drafts.value = []
  agentSteps.value = []
  statusText.value = ''
  threadId.value = ''
  sessionStorage.removeItem(THREAD_KEY)
  sessionStorage.removeItem(DRAFTS_KEY)
  persist()
}

function copyText(text) {
  if (navigator.clipboard) navigator.clipboard.writeText(text)
}

function insertNewline() {
  const el = inputRef.value
  if (el) el.value = inputText.value
  autogrow()
}

function autogrow() {
  const el = inputRef.value
  if (!el) return
  el.style.height = 'auto'
  el.style.height = Math.min(el.scrollHeight, 140) + 'px'
}

function handleMsgClick(e) {
  const a = e.target.closest('a')
  if (!a) return
  const href = a.getAttribute('href')
  if (href && href.startsWith('/') && !href.startsWith('//')) {
    e.preventDefault()
    closeChat()
    router.push(href)
  }
}

function persist() {
  try { sessionStorage.setItem(STORE_KEY, JSON.stringify(messages.value)) } catch {}
}

function openChat() {
  chatOpen.value = true
  hasInteracted.value = true
  nextTick(scrollDown)
}
function closeChat() {
  chatOpen.value = false
}

function handleFabClick(e) {
  if (dragState.active) return
  openChat()
}

function startDrag(e) {
  const btn = e.currentTarget
  dragState.value = { active: true, startX: e.clientX, startY: e.clientY, x: 0, y: 0 }
  btn.setPointerCapture(e.pointerId)
}

function onMove(e) {
  if (!dragState.value.active) return
  const dx = e.clientX - dragState.value.startX
  const dy = e.clientY - dragState.value.startY
  if (Math.abs(dx) + Math.abs(dy) < 4) return
  dragState.value.active = false
  hasInteracted.value = true
  const vw = window.innerWidth
  const vh = window.innerHeight
  const x = Math.min(Math.max(8, dragState.value.x + dx), vw - 64)
  const y = Math.min(Math.max(8, dragState.value.y + dy), vh - 64)
  dragState.value.x = x
  dragState.value.y = y
  fabStyle.value = { right: 'auto', bottom: 'auto', left: x + 'px', top: y + 'px' }
}

function onUp() {
  dragState.value.active = false
}

// ---------- lifecycle ----------
onMounted(() => {
  try {
    const saved = JSON.parse(sessionStorage.getItem(STORE_KEY) || '[]')
    if (Array.isArray(saved) && saved.length) messages.value = saved
  } catch {}
  try {
    const savedDrafts = JSON.parse(sessionStorage.getItem(DRAFTS_KEY) || '[]')
    if (Array.isArray(savedDrafts) && savedDrafts.length) {
      drafts.value = savedDrafts.map((d) => ({ ...d, sending: false, sent: false, refineMsg: '' }))
    }
  } catch {}
  window.addEventListener('pointermove', onMove)
  window.addEventListener('pointerup', onUp)
})

onUnmounted(() => {
  window.removeEventListener('pointermove', onMove)
  window.removeEventListener('pointerup', onUp)
  if (controller.value) controller.value.abort()
})

watch(
  () => route.path,
  () => {
    if (chatOpen.value) closeChat()
  }
)
</script>

<style scoped>
.ai-chatbot {
  position: fixed;
  z-index: 9999;
  right: 24px;
  bottom: 24px;
  pointer-events: none;
}

/* ---------- FAB ---------- */
.chat-fab {
  pointer-events: auto;
  position: absolute;
  right: 0;
  bottom: 0;
  width: 58px;
  height: 58px;
  border-radius: 50%;
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  background: radial-gradient(120% 120% at 30% 20%, #0f7c82, #0b5961 55%, #18314d);
  box-shadow: 0 10px 30px rgba(15, 124, 130, 0.35);
  transition: transform var(--transition-fast, 0.18s ease), box-shadow var(--transition-fast, 0.18s ease);
  animation: fabPulse 2.6s ease-in-out infinite;
  touch-action: none;
}
.chat-fab:hover { transform: scale(1.06); box-shadow: 0 14px 36px rgba(15, 124, 130, 0.45); }
.chat-fab:active { transform: scale(0.96); }
.chat-fab .fab-icon { width: 27px; height: 27px; }
.chat-fab .fab-badge {
  position: absolute;
  top: -4px;
  right: -4px;
  background: var(--accent-success, #14866d);
  color: #fff;
  font-size: 10px;
  font-weight: 700;
  padding: 2px 7px;
  border-radius: 999px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
}
@keyframes fabPulse {
  0%, 100% { box-shadow: 0 10px 30px rgba(15, 124, 130, 0.35), 0 0 0 0 rgba(15, 124, 130, 0.25); }
  50% { box-shadow: 0 10px 30px rgba(15, 124, 130, 0.35), 0 0 0 14px rgba(15, 124, 130, 0); }
}

/* ---------- Panel ---------- */
.chat-panel {
  pointer-events: auto;
  position: absolute;
  right: 0;
  bottom: 0;
  width: min(400px, calc(100vw - 20px));
  height: min(620px, calc(100vh - 90px));
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border-radius: 22px;
  background: var(--surface-default);
  backdrop-filter: var(--glass-blur, blur(18px));
  -webkit-backdrop-filter: var(--glass-blur, blur(18px));
  border: 1px solid var(--border-default);
  box-shadow: var(--shadow-overlay);
  color: var(--text-primary);
}

.chat-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px 16px;
  background: linear-gradient(135deg, rgba(15, 124, 130, 0.12), rgba(34, 90, 115, 0.08));
  border-bottom: 1px solid var(--border-subtle);
}
.chat-avatar {
  width: 40px;
  height: 40px;
  flex: none;
  border-radius: 13px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  background: var(--accent-gradient);
  box-shadow: 0 6px 16px rgba(15, 124, 130, 0.28);
}
.chat-avatar svg { width: 23px; height: 23px; }
.chat-header-info { flex: 1; min-width: 0; }
.chat-header-title { display: block; font-weight: 700; font-size: 14.5px; letter-spacing: 0.2px; color: var(--text-primary); }
.chat-header-status { display: flex; align-items: center; gap: 6px; font-size: 11.5px; color: var(--text-muted); margin-top: 2px; }
.status-dot { width: 8px; height: 8px; border-radius: 50%; background: var(--text-disabled); }
.status-dot.live { background: var(--accent-success, #14866d); box-shadow: 0 0 0 3px rgba(20, 134, 109, 0.2); animation: dotPulse 2s infinite; }
@keyframes dotPulse { 0%,100% { opacity: 1; } 50% { opacity: 0.55; } }
.chat-header-actions { display: flex; gap: 6px; }
.icon-btn {
  width: 32px; height: 32px;
  border-radius: 9px;
  border: 1px solid var(--border-subtle);
  background: var(--surface-subtle);
  color: var(--text-secondary);
  cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  transition: background-color var(--transition-fast, 0.15s ease), color var(--transition-fast, 0.15s ease);
}
.icon-btn:hover { background: var(--surface-hover); color: var(--text-primary); border-color: var(--border-default); }
.icon-btn svg { width: 16px; height: 16px; }

/* ---------- Messages ---------- */
.chat-messages {
  flex: 1;
  overflow-y: auto;
  padding: 16px 14px 8px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  scroll-behavior: smooth;
}
.chat-messages::-webkit-scrollbar { width: 6px; }
.chat-messages::-webkit-scrollbar-thumb { background: var(--border-default); border-radius: 3px; }

.chat-welcome { text-align: center; padding: 18px 10px 6px; }
.welcome-orb {
  width: 62px; height: 62px;
  margin: 0 auto 12px;
  border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  color: #fff;
  background: var(--accent-gradient);
  box-shadow: 0 12px 30px rgba(15, 124, 130, 0.28);
}
.welcome-orb svg { width: 32px; height: 32px; }
.welcome-title { font-size: 16px; font-weight: 700; margin: 0 0 6px; color: var(--text-primary); }
.welcome-text { font-size: 12.5px; color: var(--text-muted); line-height: 1.55; margin: 0 auto; max-width: 280px; }

.chat-msg { display: flex; gap: 9px; align-items: flex-end; }
.chat-msg.user { justify-content: flex-end; }
.msg-avatar {
  width: 28px; height: 28px; flex: none;
  border-radius: 9px;
  display: flex; align-items: center; justify-content: center;
  color: #fff;
  background: var(--accent-gradient);
}
.msg-avatar svg { width: 16px; height: 16px; }
.msg-bubble {
  max-width: 84%;
  padding: 10px 13px;
  border-radius: 16px;
  font-size: 13.5px;
  line-height: 1.55;
  position: relative;
  word-break: break-word;
}
.chat-msg.ai .msg-bubble {
  background: var(--surface-subtle);
  border: 1px solid var(--border-subtle);
  color: var(--text-primary);
  border-bottom-left-radius: 6px;
}
.chat-msg.user .msg-bubble {
  background: var(--accent-gradient);
  color: #fff;
  border-bottom-right-radius: 6px;
}
.msg-bubble.error { border-color: var(--border-danger); background: var(--status-danger-bg); color: var(--status-danger-text); }
.msg-bubble :deep(pre) {
  background: var(--surface-hover);
  border: 1px solid var(--border-subtle);
  border-radius: 10px;
  padding: 10px;
  overflow-x: auto;
  margin: 8px 0;
  font-size: 12px;
  color: var(--text-primary);
}
.msg-bubble :deep(code) {
  background: var(--surface-hover);
  padding: 1px 5px;
  border-radius: 5px;
  font-size: 12px;
  color: var(--text-link);
}
.msg-bubble :deep(pre code) { background: none; padding: 0; color: inherit; }
.msg-bubble :deep(a) { color: var(--text-link); text-decoration: underline; }
.msg-bubble :deep(h3), .msg-bubble :deep(h4) { margin: 10px 0 4px; font-size: 14px; font-weight: 700; color: var(--text-primary); }
.msg-bubble :deep(.li) { display: block; }
.msg-error-row { margin-top: 8px; }
.retry-btn {
  background: var(--status-danger-bg);
  color: var(--status-danger-text);
  border: 1px solid var(--status-danger-border);
  padding: 4px 12px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
}
.msg-meta { display: flex; align-items: center; gap: 8px; margin-top: 6px; opacity: 0; transition: opacity 0.15s; }
.msg-bubble:hover .msg-meta { opacity: 1; }
.copy-btn { background: none; border: none; color: var(--text-muted); font-size: 11px; cursor: pointer; padding: 0; }
.copy-btn:hover { color: var(--text-primary); }
.msg-time { font-size: 10.5px; color: var(--text-disabled); }
.stream-caret {
  display: inline-block;
  width: 8px; height: 14px;
  margin-left: 2px;
  vertical-align: -2px;
  border-radius: 2px;
  background: var(--accent-primary);
  animation: caretBlink 0.8s step-end infinite;
}
@keyframes caretBlink { 50% { opacity: 0; } }

/* typing */
.typing { display: flex; gap: 4px; align-items: center; padding: 14px 16px; }
.typing-dot { width: 7px; height: 7px; border-radius: 50%; background: var(--text-muted); animation: bounce 1.2s infinite; }
.typing-dot:nth-child(2) { animation-delay: 0.15s; }
.typing-dot:nth-child(3) { animation-delay: 0.3s; }
@keyframes bounce { 0%,60%,100% { transform: translateY(0); opacity: 0.5; } 30% { transform: translateY(-5px); opacity: 1; } }

/* ---------- Suggestions ---------- */
.chat-suggestions {
  display: flex;
  flex-wrap: wrap;
  gap: 7px;
  padding: 0 14px 10px;
}
.suggestion-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: var(--surface-subtle);
  border: 1px solid var(--border-subtle);
  color: var(--text-secondary);
  border-radius: 999px;
  padding: 6px 11px;
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  transition: background-color var(--transition-fast, 0.15s ease), border-color var(--transition-fast, 0.15s ease), color var(--transition-fast, 0.15s ease);
}
.suggestion-chip:hover { background: var(--surface-hover); border-color: var(--border-focus); color: var(--accent-primary); }
.suggestion-icon { font-size: 12px; }

/* ---------- Input ---------- */
.chat-input-bar {
  display: flex;
  align-items: flex-end;
  gap: 8px;
  padding: 10px 12px 8px;
  border-top: 1px solid var(--border-subtle);
  background: var(--surface-subtle);
}
.chat-input-bar textarea {
  flex: 1;
  resize: none;
  border: 1px solid var(--border-default);
  background: var(--surface-default);
  color: var(--text-primary);
  border-radius: 12px;
  padding: 9px 12px;
  font-size: 13.5px;
  line-height: 1.45;
  max-height: 140px;
  outline: none;
  font-family: inherit;
  transition: border-color var(--transition-fast, 0.15s ease), box-shadow var(--transition-fast, 0.15s ease);
}
.chat-input-bar textarea:focus { border-color: var(--border-focus); box-shadow: 0 0 0 3px color-mix(in srgb, var(--border-focus) 20%, transparent); }
.chat-input-bar textarea:disabled { opacity: 0.6; }
.chat-send-btn {
  width: 40px; height: 40px; flex: none;
  border-radius: 12px;
  border: none;
  background: var(--accent-gradient);
  color: #fff;
  cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  box-shadow: 0 6px 16px rgba(15, 124, 130, 0.28);
  transition: transform 0.12s, opacity 0.15s;
}
.chat-send-btn:disabled { opacity: 0.4; cursor: not-allowed; }
.chat-send-btn:not(:disabled):hover { transform: scale(1.05); }
.chat-send-btn.stop { background: var(--status-danger-text, #9a302a); box-shadow: 0 6px 16px rgba(195, 81, 70, 0.35); }
.chat-send-btn svg { width: 18px; height: 18px; }
.chat-footer-note { padding: 0 14px 8px; font-size: 10.5px; color: var(--text-muted); text-align: center; }

/* ---------- Transition ---------- */
.chat-slide-enter-active, .chat-slide-leave-active { transition: opacity 0.2s ease, transform 0.24s cubic-bezier(0.2, 0.8, 0.3, 1); transform-origin: bottom right; }
.chat-slide-enter-from, .chat-slide-leave-to { opacity: 0; transform: translateY(14px) scale(0.96); }

@media (max-width: 480px) {
  .ai-chatbot { right: 12px; bottom: 12px; }
  .chat-panel { width: calc(100vw - 24px); height: calc(100vh - 100px); }
}

/* ---------- Agent activity (agent đang làm việc) ---------- */
.agent-activity {
  margin: 0 12px 10px;
  border-radius: 14px;
  border: 1px solid var(--border-subtle);
  background: var(--surface-subtle);
  overflow: hidden;
}
.agent-activity.working { border-color: var(--border-focus); box-shadow: 0 0 0 1px rgba(22, 142, 152, 0.18); }
.agent-activity-header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 9px 12px;
}
.agent-orb {
  width: 10px; height: 10px;
  border-radius: 50%;
  background: var(--accent-success, #14866d);
  box-shadow: 0 0 0 3px rgba(20, 134, 109, 0.18);
  flex: none;
}
.agent-orb.spinning {
  background: var(--accent-primary);
  box-shadow: 0 0 0 3px rgba(15, 124, 130, 0.25);
  animation: orbPulse 1.1s ease-in-out infinite;
}
@keyframes orbPulse { 0%,100% { opacity: 1; } 50% { opacity: 0.35; } }
.agent-activity-title { font-size: 12px; font-weight: 700; color: var(--text-primary); flex: 1; }
.agent-activity-count { font-size: 10.5px; color: var(--text-muted); }
.agent-collapse { background: none; border: none; color: var(--text-muted); cursor: pointer; font-size: 12px; padding: 0 2px; }
.agent-collapse:hover { color: var(--text-primary); }

.agent-steps {
  padding: 2px 12px 10px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.agent-step {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 12px;
  color: var(--text-secondary);
  line-height: 1.35;
}
.agent-step .step-icon { width: 15px; flex: none; display: flex; justify-content: center; }
.step-spinner {
  width: 11px; height: 11px;
  border: 2px solid var(--border-subtle);
  border-top-color: var(--accent-primary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}
.step-check { color: var(--accent-success, #14866d); font-size: 12px; }
.step-x { color: var(--accent-danger, #c35146); font-size: 12px; }
.agent-step.active { color: var(--text-primary); }
.agent-step.active .step-label { color: var(--text-primary); font-weight: 600; }
.agent-step.fail .step-label { color: var(--status-danger-text); }
.step-skill { flex: none; font-size: 12px; }
.step-label { word-break: break-word; }

.chat-drafts { padding: 0 14px 10px; display: flex; flex-direction: column; gap: 10px; }
.draft-card {
  background: var(--surface-subtle);
  border: 1px solid var(--border-subtle);
  border-radius: 14px;
  padding: 10px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.draft-header { display: flex; align-items: center; justify-content: space-between; }
.draft-title { font-size: 12px; font-weight: 700; color: var(--text-primary); }
.draft-close { background: none; border: none; color: var(--text-muted); cursor: pointer; font-size: 13px; }
.draft-close:hover { color: var(--text-primary); }
.draft-input {
  background: var(--surface-default);
  border: 1px solid var(--border-default);
  color: var(--text-primary);
  border-radius: 9px;
  padding: 7px 10px;
  font-size: 12.5px;
  outline: none;
}
.draft-input:focus { border-color: var(--border-focus); }
.draft-textarea {
  background: var(--surface-default);
  border: 1px solid var(--border-default);
  color: var(--text-primary);
  border-radius: 9px;
  padding: 8px 10px;
  font-size: 12.5px;
  line-height: 1.5;
  resize: vertical;
  outline: none;
  font-family: inherit;
}
.draft-actions { display: flex; gap: 7px; flex-wrap: wrap; }
.draft-btn {
  background: var(--surface-default);
  border: 1px solid var(--border-default);
  color: var(--text-secondary);
  border-radius: 9px;
  padding: 6px 12px;
  font-size: 12px;
  cursor: pointer;
}
.draft-btn:hover { background: var(--surface-hover); color: var(--text-primary); border-color: var(--border-strong); }
.draft-btn.primary { background: var(--accent-gradient); border: none; color: #fff; }
.draft-btn:disabled { opacity: 0.5; cursor: not-allowed; }
.draft-result { font-size: 11.5px; color: var(--status-warning-text); }
.draft-result.ok { color: var(--status-success-text); }

/* ---------- Dark Mode Overrides ---------- */
:global(:root[data-theme='dark']) .chat-panel {
  background: rgba(18, 33, 49, 0.95);
  border: 1px solid var(--border-default);
  box-shadow: 0 24px 70px rgba(0, 0, 0, 0.55);
}

:global(:root[data-theme='dark']) .chat-header {
  background: linear-gradient(135deg, rgba(53, 168, 178, 0.18), rgba(98, 169, 198, 0.12));
}

:global(:root[data-theme='dark']) .chat-msg.ai .msg-bubble {
  background: rgba(15, 29, 43, 0.9);
  border: 1px solid rgba(190, 220, 228, 0.14);
}

:global(:root[data-theme='dark']) .chat-input-bar {
  background: rgba(15, 29, 43, 0.85);
}

:global(:root[data-theme='dark']) .chat-input-bar textarea {
  background: var(--surface-subtle);
  color: var(--text-primary);
}
</style>