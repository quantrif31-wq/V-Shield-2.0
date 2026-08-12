<template>
  <Teleport to="body">
    <transition name="art-fade">
      <div v-if="visible" class="art-root" :class="`art-root--${type}`" role="alert">
        <div class="art-icon" v-html="iconSvg"></div>
        <div class="art-content">
          <div class="art-title">{{ title }}</div>
          <div class="art-message">{{ message }}</div>
          <div v-if="receiptId" class="art-receipt">
            <span class="art-receipt-label">Biên nhận:</span>
            <code class="art-receipt-id">{{ receiptId }}</code>
            <button
              v-if="showCopy"
              type="button"
              class="art-copy-btn"
              title="Sao chép mã biên nhận"
              @click="copyReceipt"
            >
              {{ copied ? 'Đã sao chép' : 'Sao chép' }}
            </button>
          </div>
          <div v-if="timestamp" class="art-timestamp">{{ timestamp }}</div>
        </div>
        <button type="button" class="art-close" aria-label="Đóng" @click="dismiss">&times;</button>
      </div>
    </transition>
  </Teleport>
</template>

<script>
export default {
  name: 'AuditReceiptToast',
  props: {
    visible: { type: Boolean, default: false },
    type: {
      type: String,
      default: 'success',
      validator: (v) => ['success', 'warning', 'danger', 'info'].includes(v),
    },
    title: { type: String, default: 'Thành công' },
    message: { type: String, default: '' },
    receiptId: { type: String, default: '' },
    showCopy: { type: Boolean, default: true },
    autoDismissMs: { type: Number, default: 8000 },
    timestamp: { type: String, default: '' },
  },
  emits: ['dismiss'],
  data() {
    return {
      copied: false,
      dismissTimer: null,
    }
  },
  computed: {
    iconSvg() {
      const icons = {
        success: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M20 6L9 17l-5-5"/></svg>',
        warning: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M12 9v4M12 17h.01"/><path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"/></svg>',
        danger: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="12" cy="12" r="10"/><path d="M15 9l-6 6M9 9l6 6"/></svg>',
        info: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="12" cy="12" r="10"/><path d="M12 16v-4M12 8h.01"/></svg>',
      }
      return icons[this.type] || icons.info
    },
  },
  watch: {
    visible(val) {
      if (val) this.startAutoDismiss()
      else this.clearAutoDismiss()
    },
  },
  beforeUnmount() {
    this.clearAutoDismiss()
  },
  methods: {
    startAutoDismiss() {
      this.clearAutoDismiss()
      if (this.autoDismissMs > 0) {
        this.dismissTimer = setTimeout(() => this.dismiss(), this.autoDismissMs)
      }
    },
    clearAutoDismiss() {
      if (this.dismissTimer) {
        clearTimeout(this.dismissTimer)
        this.dismissTimer = null
      }
    },
    dismiss() {
      this.clearAutoDismiss()
      this.$emit('dismiss')
    },
    async copyReceipt() {
      try {
        await navigator.clipboard.writeText(this.receiptId)
        this.copied = true
        setTimeout(() => (this.copied = false), 2000)
      } catch {
        // fallback
        const textarea = document.createElement('textarea')
        textarea.value = this.receiptId
        document.body.appendChild(textarea)
        textarea.select()
        document.execCommand('copy')
        document.body.removeChild(textarea)
        this.copied = true
        setTimeout(() => (this.copied = false), 2000)
      }
    },
  },
}
</script>

<style scoped>
.art-root {
  position: fixed;
  top: 20px;
  right: 20px;
  z-index: 1000;
  min-width: 360px;
  max-width: 520px;
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 14px 16px;
  border-radius: 12px;
  box-shadow: 0 16px 48px rgba(2, 6, 23, 0.18);
  border: 1px solid;
  pointer-events: auto;
}
.art-root--success {
  background: #f0fdf4;
  border-color: #bbf7d0;
  color: #166534;
}
.art-root--warning {
  background: #fffbeb;
  border-color: #fde68a;
  color: #92400e;
}
.art-root--danger {
  background: #fef2f2;
  border-color: #fecaca;
  color: #991b1b;
}
.art-root--info {
  background: #eff6ff;
  border-color: #bfdbfe;
  color: #1e40af;
}
.art-icon {
  width: 28px;
  height: 28px;
  flex-shrink: 0;
  margin-top: 2px;
}
.art-icon svg {
  width: 100%;
  height: 100%;
  display: block;
}
.art-content {
  flex: 1;
  min-width: 0;
}
.art-title {
  font-size: 15px;
  font-weight: 800;
  margin-bottom: 4px;
}
.art-message {
  font-size: 13px;
  line-height: 1.4;
  opacity: 0.9;
}
.art-receipt {
  margin-top: 8px;
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
}
.art-receipt-label {
  font-size: 12px;
  font-weight: 700;
  opacity: 0.8;
}
.art-receipt-id {
  font-size: 12px;
  font-family: 'JetBrains Mono', 'SF Mono', 'Fira Code', monospace;
  background: rgba(0, 0, 0, 0.06);
  padding: 2px 8px;
  border-radius: 4px;
  word-break: break-all;
}
.art-copy-btn {
  font-size: 11px;
  font-weight: 700;
  padding: 2px 8px;
  border: 1px solid currentColor;
  border-radius: 6px;
  background: transparent;
  cursor: pointer;
  opacity: 0.7;
  color: inherit;
}
.art-copy-btn:hover {
  opacity: 1;
}
.art-timestamp {
  margin-top: 4px;
  font-size: 11px;
  opacity: 0.6;
}
.art-close {
  flex-shrink: 0;
  width: 28px;
  height: 28px;
  border: none;
  border-radius: 6px;
  background: rgba(0, 0, 0, 0.05);
  color: inherit;
  font-size: 20px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  opacity: 0.7;
}
.art-close:hover {
  opacity: 1;
  background: rgba(0, 0, 0, 0.1);
}
.art-fade-enter-active,
.art-fade-leave-active {
  transition: all 0.3s ease;
}
.art-fade-enter-from {
  opacity: 0;
  transform: translateX(40px);
}
.art-fade-leave-to {
  opacity: 0;
  transform: translateX(40px);
}
</style>
