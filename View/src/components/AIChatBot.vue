<template>
  <div class="ai-chatbot">
    <button
      v-if="!chatOpen"
      class="chat-fab"
      :class="{ pulse: !hasInteracted, dragging: dragState.active }"
      :style="fabStyle"
      aria-label="Mo tro ly AI"
      @pointerdown="startDrag"
      @click="handleFabClick"
    >
      <svg class="fab-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8">
        <path d="M21 15a2 2 0 01-2 2H7l-4 4V5a2 2 0 012-2h14a2 2 0 012 2z"/>
      </svg>
      <span class="fab-badge">AI</span>
    </button>

    <Transition name="chat-slide">
      <div v-if="chatOpen" class="chat-dialog">
        <div class="chat-header">
          <div class="chat-header-left">
            <div class="chat-avatar">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
                <rect x="3" y="11" width="18" height="10" rx="2"/>
                <circle cx="12" cy="16" r="1.5"/>
                <path d="M7 11V7a5 5 0 0110 0v4"/>
              </svg>
            </div>
            <div>
              <span class="chat-header-title">Tro ly V-Shield</span>
              <div class="chat-header-status">
                <span class="status-dot"></span>
                <span>San sang ho tro</span>
              </div>
            </div>
          </div>
          <button class="chat-close" aria-label="Dong chat" @click="closeChat">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
            </svg>
          </button>
        </div>

        <div ref="messagesRef" class="chat-messages" @click="handleMsgClick">
          <div
            v-for="msg in messages"
            :key="msg.id"
            class="chat-msg"
            :class="msg.role"
          >
            <div v-if="msg.role === 'ai'" class="msg-avatar">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
                <rect x="3" y="11" width="18" height="10" rx="2"/>
                <circle cx="12" cy="16" r="1.5"/>
                <path d="M7 11V7a5 5 0 0110 0v4"/>
              </svg>
            </div>
            <div class="msg-bubble" v-html="msg.text"></div>
          </div>

          <div v-if="isTyping" class="chat-msg ai">
            <div class="msg-avatar">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6">
                <rect x="3" y="11" width="18" height="10" rx="2"/>
                <circle cx="12" cy="16" r="1.5"/>
                <path d="M7 11V7a5 5 0 0110 0v4"/>
              </svg>
            </div>
            <div class="msg-bubble typing">
              <span class="typing-dot"></span>
              <span class="typing-dot"></span>
              <span class="typing-dot"></span>
            </div>
          </div>
        </div>

        <div v-if="showSuggestions && !isTyping" class="chat-suggestions">
          <button
            v-for="suggestion in suggestions"
            :key="suggestion.id"
            class="suggestion-chip"
            @click="sendSuggestion(suggestion)"
          >
            <span class="suggestion-icon">{{ suggestion.icon }}</span>
            <span>{{ suggestion.label }}</span>
          </button>
        </div>

        <div class="chat-input-bar">
          <input
            v-model="inputText"
            type="text"
            placeholder="Nhap cau hoi..."
            :disabled="isTyping"
            @keydown.enter.prevent="sendMessage"
          />
          <button
            class="chat-send-btn"
            :disabled="!inputText.trim() || isTyping"
            @click="sendMessage"
          >
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="22" y1="2" x2="11" y2="13"/><polyline points="22 2 15 22 11 13 2 9 22 2"/>
            </svg>
          </button>
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup>
import { computed, nextTick, onUnmounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const router = useRouter()
const route = useRoute()
const chatOpen = ref(false)
const messages = ref([])
const inputText = ref('')
const isTyping = ref(false)
const hasInteracted = ref(false)
const showSuggestions = ref(true)
const messagesRef = ref(null)
const fabOffset = ref({ x: 0, y: 0 })
const dragState = ref({
  active: false,
  pointerId: null,
  startX: 0,
  startY: 0,
  originX: 0,
  originY: 0,
  moved: false
})

const fabStyle = computed(() => ({
  transform: `translate(${fabOffset.value.x}px, ${fabOffset.value.y}px)`
}))

const suggestions = [
  { id: 'guide', icon: '📖', label: 'Huong dan su dung phan mem', text: 'Huong dan toi su dung phan mem V-Shield' },
  { id: 'admin', icon: '🔐', label: 'Admin co the lam gi?', text: 'Toi la Admin, toi co the lam gi tren V-Shield?' },
  { id: 'baove', icon: '🛡️', label: 'Bao ve can lam gi?', text: 'Toi la Bao ve, can lam nhung gi khi truc cong?' },
  { id: 'reception', icon: '🛎️', label: 'Le tan can biet', text: 'Toi la Le tan, can dung V-Shield nhu the nao?' },
  { id: 'quanly', icon: '📊', label: 'Quan ly van hanh', text: 'Toi la Quan ly, cac chuc nang danh cho toi?' },
  { id: 'manual', icon: '⌨️', label: 'Xu ly khi QR loi', text: 'Lam the nao de xu ly thu cong khi QR hoac camera loi?' },
]

function addMessage(role, text) {
  messages.value.push({
    id: Date.now() + Math.random(),
    role,
    text,
    timestamp: new Date()
  })
}

async function scrollToBottom() {
  await nextTick()
  if (messagesRef.value) {
    messagesRef.value.scrollTop = messagesRef.value.scrollHeight
  }
}

function resetFabPosition() {
  fabOffset.value = { x: 0, y: 0 }
  dragState.value = {
    active: false,
    pointerId: null,
    startX: 0,
    startY: 0,
    originX: 0,
    originY: 0,
    moved: false
  }
}

function openChat() {
  resetFabPosition()
  chatOpen.value = true
  hasInteracted.value = true
  if (messages.value.length === 0) {
    addMessage('ai', 'Xin chao! Toi la <strong>Tro ly V-Shield</strong>. Toi co the giup ban:<br>• 📖 Huong dan su dung toan bo he thong<br>• 🔐 Giai thich chuc nang theo vai tro<br>• ❌ Tra loi cau hoi thuong gap<br><br>Ban muon tim hieu dieu gi truoc?')
  }
  scrollToBottom()
}

function closeChat() {
  chatOpen.value = false
}

function startDrag(event) {
  if (chatOpen.value) return

  dragState.value = {
    active: true,
    pointerId: event.pointerId,
    startX: event.clientX,
    startY: event.clientY,
    originX: fabOffset.value.x,
    originY: fabOffset.value.y,
    moved: false
  }

  event.currentTarget?.setPointerCapture?.(event.pointerId)
  window.addEventListener('pointermove', onDragMove)
  window.addEventListener('pointerup', endDrag)
  window.addEventListener('pointercancel', endDrag)
}

function onDragMove(event) {
  if (!dragState.value.active || event.pointerId !== dragState.value.pointerId) return

  const deltaX = event.clientX - dragState.value.startX
  const deltaY = event.clientY - dragState.value.startY

  if (Math.abs(deltaX) > 4 || Math.abs(deltaY) > 4) {
    dragState.value.moved = true
  }

  const maxRight = Math.max(window.innerWidth - 92, 0)
  const maxBottom = Math.max(window.innerHeight - 92, 0)

  fabOffset.value = {
    x: Math.min(Math.max(dragState.value.originX + deltaX, -maxRight), 24),
    y: Math.min(Math.max(dragState.value.originY + deltaY, -maxBottom), 24)
  }
}

function endDrag(event) {
  if (dragState.value.pointerId !== null && event.pointerId !== dragState.value.pointerId) return

  dragState.value.active = false
  dragState.value.pointerId = null
  window.removeEventListener('pointermove', onDragMove)
  window.removeEventListener('pointerup', endDrag)
  window.removeEventListener('pointercancel', endDrag)
}

function handleFabClick() {
  if (dragState.value.moved) {
    dragState.value.moved = false
    return
  }

  openChat()
}

async function simulateTyping(callback) {
  isTyping.value = true
  showSuggestions.value = false
  await scrollToBottom()
  await new Promise(r => setTimeout(r, 600 + Math.random() * 400))
  await callback()
  isTyping.value = false
  showSuggestions.value = true
  await scrollToBottom()
}

function handleGuideResponse(userMessage) {
  const msg = userMessage.toLowerCase()

  if (msg.includes('huong dan') || msg.includes('cach dung') || msg.includes('su dung') || msg.includes('bat dau')) {
    addMessage('ai', `📖 <strong>Huong dan su dung V-Shield</strong><br><br>V-Shield la nen tang kiem soat ra vao thong minh voi day du tinh nang:<br><br>👉 <a href="/guide" class="chat-link">Mo Huong dan su dung day du →</a><br><br>Trong huong dan co:<br>• ✅ Tong quan he thong<br>• ✅ Luong hoat dong cho tung vai tro<br>• ✅ Danh muc tat ca trang chuc nang<br>• ✅ Chi tiet nut bam, o nhap lieu tung man hinh<br>• ✅ Cau hoi thuong gap`)
    return
  }

  if (msg.includes('admin') || msg.includes('quan tri')) {
    addMessage('ai', `🔐 <strong>Quyen han cua Admin</strong><br><br>Admin co <strong>toan quyen</strong> tren he thong V-Shield:<br><br>• 📊 Dashboard tong quan & AI Intelligence<br>• 📹 Giam sat camera, QR dong, bien so<br>• 👥 Quan ly nhan su, tai khoan, phan quyen<br>• 🚗 Quan ly phuong tien, cham cong<br>• 🏢 Quan ly khach, nha thau, watchlist<br>• ⚙️ Cau hinh camera, thiet bi, policy<br>• 🔒 SOC, Evidence, Compliance, Retention<br><br>👉 <a href="/guide" class="chat-link">Xem chi tiet trong Huong dan →</a>`)
    return
  }

  if (msg.includes('bao ve') || msg.includes('baove') || msg.includes('truc cong')) {
    addMessage('ai', `🛡️ <strong>Quyen han cua Bao ve</strong><br><br>Bao ve co the truy cap cac chuc nang:<br><br>• 📹 Giam sat camera truc tiep<br>• 🔍 Tra cuu lich su vao/ra<br>• 📱 Xac thuc QR dong + bien so<br>• 🚪 Dieu phoi thong hanh, cho qua thu cong co truy vet<br>• 🏪 Reception check-in khach<br>• ⚠️ Gui yeu cau xu ly ngoai le va duress<br>• 📋 Watchlist, Lane dashboard, Barrier<br><br>👉 <a href="/guide" class="chat-link">Xem luong cong viec chi tiet →</a>`)
    return
  }

  if (msg.includes('le tan') || msg.includes('reception')) {
    addMessage('ai', `🛎️ <strong>Quyen han cua Le tan</strong><br><br>Le tan co the su dung:<br><br>• 🏪 Don tiep va check-in khach tai quay<br>• 🔎 Tra cuu khach con trong khuon vien hay da qua gio<br>• 🎒 Tim do that lac va theo doi viec trao tra<br>• 🚗 Kiem tra xe khach con trong bai khong<br>• 🛡️ Goi Bao ve ho tro khi co tinh huong phat sinh<br>• 📋 Xem cac man hinh can thiet de ho tro khach nhanh chong<br><br>👉 <a href="/guide" class="chat-link">Xem huong dan cho Le tan →</a>`)
    return
  }

  if (msg.includes('quan ly') || msg.includes('quanly') || msg.includes('manager')) {
    addMessage('ai', `📊 <strong>Quyen han cua Quan ly</strong><br><br>Quan ly co the:<br><br>• 📊 Dashboard tong quan<br>• 📹 Giam sat camera & lich su<br>• 🚗 Quan ly phuong tien<br>• 📋 Bao cao cham cong<br>• 🏢 Danh muc he thong<br>• ⚠️ Xem & xu ly ngoai le<br><br>👉 <a href="/guide" class="chat-link">Xem chi tiet trong Huong dan →</a>`)
    return
  }

  if (msg.includes('thu cong') || msg.includes('camera loi') || msg.includes('qr loi') || msg.includes('khong doc')) {
    addMessage('ai', `⌨️ <strong>Van hanh thu cong tai cong</strong><br><br>1. Mo <a href="/gate-transit-monitor" class="chat-link">Control Room</a><br>2. Chon lan va mo bang quyet dinh<br>3. Chon “Van hanh thu cong”<br>4. Nhap ho ten hoac bien so, ly do xac minh<br>5. Xac nhan cho qua<br><br>He thong se tao su kien MANUAL_PASS cung nguoi thao tac va ly do de hau kiem.`)
    return
  }

  if (msg.includes('qr') || msg.includes('ma')) {
    addMessage('ai', `📱 <strong>QR Dong</strong><br><br>QR dong la ma QR thay doi theo chu ky (mac dinh 30s), tang cuong bao mat.<br><br><strong>Nguoi dung duoc cap quyen:</strong> Dang nhap → mo trang QR → giu man hinh de quet tai cong.<br><br><strong>Admin:</strong> Vao Tao QR dong, nhap Employee ID, bam "Phat QR realtime".<br><br>👉 Vao <a href="/dynamic-qr-generator" class="chat-link">Tao QR dong</a> ngay.`)
    return
  }

  if (msg.includes('cam on') || msg.includes('thank')) {
    addMessage('ai', 'Khong co gi! Neu can them thong tin, ban co the:<br><br>• 📖 Xem <a href="/guide" class="chat-link">Huong dan day du</a><br>• ❓ Dat cau hoi khac cho toi<br>• 📧 Lien he Admin he thong')
    return
  }

  addMessage('ai', `Xin chao! Toi co the giup gi cho ban?<br><br>Hay thu cac goi y ben duoi hoac go cau hoi cua ban:<br>• "Huong dan su dung V-Shield"<br>• "Admin co the lam gi?"<br>• "Bao ve can lam gi?"<br>• "Cach tao QR dong"<br>• "Xu ly the nao khi QR loi?"`)
}

function sendSuggestion(suggestion) {
  addMessage('user', suggestion.text)
  showSuggestions.value = false
  simulateTyping(() => handleGuideResponse(suggestion.text))
}

function handleMsgClick(event) {
  const link = event.target.closest('a[href]')
  if (!link) return
  const href = link.getAttribute('href')
  if (href && href.startsWith('/')) {
    event.preventDefault()
    closeChat()
    router.push(href)
  }
}

watch(() => route.path, () => {
  if (chatOpen.value) closeChat()
})

async function sendMessage() {
  const text = inputText.value.trim()
  if (!text || isTyping.value) return

  inputText.value = ''
  addMessage('user', text)
  showSuggestions.value = false
  await simulateTyping(() => handleGuideResponse(text))
}

onUnmounted(() => {
  window.removeEventListener('pointermove', onDragMove)
  window.removeEventListener('pointerup', endDrag)
  window.removeEventListener('pointercancel', endDrag)
})
</script>

<style scoped>
.ai-chatbot {
  position: fixed;
  bottom: 24px;
  right: 24px;
  z-index: 9999;
  font-family: var(--font-body);
}

.chat-fab {
  position: relative;
  width: 60px;
  height: 60px;
  border-radius: 50%;
  border: none;
  background: var(--accent-gradient);
  color: #fff;
  cursor: grab;
  box-shadow: 0 8px 28px rgba(15,124,130,0.28);
  display: flex;
  align-items: center;
  justify-content: center;
  transition: transform var(--transition-fast), box-shadow var(--transition-fast);
  touch-action: none;
  user-select: none;
}

.chat-fab:hover {
  transform: translate(var(--fab-offset-x, 0), var(--fab-offset-y, 0)) translateY(-2px) scale(1.04);
  box-shadow: 0 12px 36px rgba(15,124,130,0.35);
}

.chat-fab.dragging {
  cursor: grabbing;
  transition: box-shadow var(--transition-fast);
}

.chat-fab.pulse {
  animation: fabPulse 2.5s ease-in-out infinite;
}

@keyframes fabPulse {
  0%, 100% { box-shadow: 0 8px 28px rgba(15,124,130,0.28); }
  50% { box-shadow: 0 8px 36px rgba(15,124,130,0.48), 0 0 0 12px rgba(84,196,211,0.08); }
}

.fab-icon { width: 26px; height: 26px; }

.fab-badge {
  position: absolute;
  top: -4px;
  right: -4px;
  min-width: 22px;
  height: 22px;
  padding: 0 6px;
  border-radius: 999px;
  background: var(--accent-danger);
  color: #fff;
  font-size: 0.65rem;
  font-weight: 800;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 12px rgba(195,81,70,0.3);
}

.chat-dialog {
  position: absolute;
  bottom: 0;
  right: 0;
  width: 380px;
  max-height: 600px;
  height: min(600px, calc(100vh - 100px));
  display: flex;
  flex-direction: column;
  background: var(--bg-card-strong);
  border: 1px solid var(--border-color);
  border-radius: 24px;
  box-shadow: var(--shadow-xl);
  overflow: hidden;
  backdrop-filter: var(--glass-blur);
}

.chat-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 16px 18px;
  border-bottom: 1px solid var(--border-color);
  flex-shrink: 0;
}

.chat-header-left { display: flex; align-items: center; gap: 10px; }

.chat-avatar {
  width: 40px;
  height: 40px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, rgba(15,124,130,0.12), rgba(84,196,211,0.08));
  color: var(--accent-primary);
}

.chat-avatar svg { width: 20px; height: 20px; }
.chat-header-title { display: block; font-size: 0.96rem; font-weight: 700; color: var(--text-primary); }
.chat-header-status { display: flex; align-items: center; gap: 6px; margin-top: 2px; }
.status-dot { width: 6px; height: 6px; border-radius: 50%; background: var(--accent-success); box-shadow: 0 0 0 3px rgba(20,134,109,0.12); }
.chat-header-status span { font-size: 0.76rem; color: var(--text-muted); }

.chat-close {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  border: none;
  background: rgba(0,0,0,0.04);
  color: var(--text-secondary);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all var(--transition-fast);
}

.chat-close:hover { background: rgba(195,81,70,0.1); color: var(--accent-danger); }
.chat-close svg { width: 18px; height: 18px; }

.chat-messages {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  scroll-behavior: smooth;
}

.chat-messages::-webkit-scrollbar { width: 4px; }
.chat-messages::-webkit-scrollbar-thumb { background: rgba(61,93,118,0.2); border-radius: 999px; }

.chat-msg { display: flex; gap: 8px; max-width: 92%; }
.chat-msg.user { align-self: flex-end; flex-direction: row-reverse; }
.chat-msg.ai { align-self: flex-start; }

.msg-avatar {
  width: 28px;
  height: 28px;
  border-radius: 8px;
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(15,124,130,0.1);
  color: var(--accent-primary);
  font-size: 0.7rem;
}

.msg-avatar svg { width: 14px; height: 14px; }

.msg-bubble {
  padding: 10px 14px;
  border-radius: 14px;
  font-size: 0.88rem;
  line-height: 1.55;
  word-break: break-word;
}

.chat-msg.ai .msg-bubble {
  background: rgba(236,244,246,0.72);
  border: 1px solid var(--border-color);
  color: var(--text-primary);
  border-bottom-left-radius: 4px;
}

.chat-msg.user .msg-bubble {
  background: var(--accent-gradient);
  color: #fff;
  border-bottom-right-radius: 4px;
}

.msg-bubble :deep(a) { color: var(--accent-primary); font-weight: 600; text-decoration: underline; }
.chat-msg.user .msg-bubble :deep(a) { color: #fff; text-decoration: underline; }

.typing { display: flex; align-items: center; gap: 4px; padding: 14px 20px !important; }

.typing-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: rgba(15,124,130,0.4);
  animation: typingBounce 1.2s ease-in-out infinite;
}

.typing-dot:nth-child(2) { animation-delay: 0.2s; }
.typing-dot:nth-child(3) { animation-delay: 0.4s; }

@keyframes typingBounce {
  0%, 60%, 100% { transform: translateY(0); }
  30% { transform: translateY(-6px); }
}

.chat-suggestions {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  padding: 8px 16px 12px;
  border-top: 1px solid var(--border-color);
  flex-shrink: 0;
}

.suggestion-chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 8px 12px;
  border-radius: 999px;
  border: 1px solid var(--border-color);
  background: var(--bg-input);
  color: var(--text-secondary);
  font-size: 0.78rem;
  font-weight: 600;
  cursor: pointer;
  transition: all var(--transition-fast);
}

.suggestion-chip:hover { border-color: var(--border-color-hover); background: rgba(15,124,130,0.06); color: var(--accent-primary); }
.suggestion-icon { font-size: 0.9rem; }

.chat-input-bar {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 16px 14px;
  border-top: 1px solid var(--border-color);
  flex-shrink: 0;
}

.chat-input-bar input {
  flex: 1;
  min-height: 44px;
  padding: 0 16px;
  border-radius: 14px;
  border: 1px solid var(--border-color);
  background: var(--bg-input);
  color: var(--text-primary);
  font-size: 0.9rem;
  outline: none;
  transition: border-color var(--transition-fast);
}

.chat-input-bar input:focus { border-color: rgba(15,124,130,0.36); box-shadow: 0 0 0 3px rgba(84,196,211,0.12); }
.chat-input-bar input::placeholder { color: var(--text-muted); }

.chat-send-btn {
  width: 44px;
  height: 44px;
  border-radius: 14px;
  border: none;
  background: var(--accent-gradient);
  color: #fff;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: all var(--transition-fast);
}

.chat-send-btn:hover:not(:disabled) { transform: translateY(-1px); filter: brightness(1.05); }
.chat-send-btn:disabled { opacity: 0.5; cursor: not-allowed; }
.chat-send-btn svg { width: 18px; height: 18px; }

.chat-slide-enter-active,
.chat-slide-leave-active { transition: all 0.28s cubic-bezier(0.22, 1, 0.36, 1); }

.chat-slide-enter-from,
.chat-slide-leave-to { opacity: 0; transform: translateY(20px) scale(0.94); }

@media (max-width: 480px) {
  .ai-chatbot { bottom: 0; right: 0; left: 0; }
  .chat-dialog { position: fixed; bottom: 0; left: 0; right: 0; width: 100%; height: calc(100vh - 60px); max-height: none; border-radius: 20px 20px 0 0; }
}
</style>
