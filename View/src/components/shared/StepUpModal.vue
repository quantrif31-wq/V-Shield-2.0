<template>
  <Teleport to="body">
    <div v-if="visible" class="sum-overlay" @click.self="handleCancel">
      <div class="sum-dialog" role="dialog" aria-modal="true" aria-label="Step-up xác thực">
        <div class="sum-header">
          <h2 class="sum-title">Xác thực hành động đặc quyền</h2>
          <button type="button" class="sum-close" aria-label="Đóng" @click="handleCancel">&times;</button>
        </div>

        <div class="sum-body">
          <div class="sum-context">
            <span class="sum-badge" :class="severityClass">{{ severityLabel }}</span>
            <p class="sum-action-name">{{ actionLabel }}</p>
            <p v-if="actionDescription" class="sum-action-desc">{{ actionDescription }}</p>
          </div>

          <div v-if="error" class="sum-alert sum-alert--danger">
            <span>{{ error }}</span>
          </div>

          <div v-if="!stepUpStarted" class="sum-step-init">
            <div class="sum-field">
              <label>Lý do thực hiện hành động *</label>
              <textarea
                v-model="reason"
                class="sum-input sum-textarea"
                rows="3"
                placeholder="Nhập lý do bắt buộc cho hành động này..."
                :disabled="loading"
              ></textarea>
            </div>
            <button
              type="button"
              class="sum-btn sum-btn--primary"
              :disabled="loading || !reason.trim()"
              @click="startStepUp"
            >
              {{ loading ? 'Đang xác thực...' : 'Tiếp tục xác thực' }}
            </button>
          </div>

          <div v-else class="sum-step-verify">
            <p class="sum-verify-hint">Vui lòng xác nhận lại mật khẩu để hoàn tất hành động đặc quyền.</p>
            <div class="sum-field">
              <label>Mật khẩu *</label>
              <input
                v-model="password"
                type="password"
                class="sum-input"
                placeholder="Nhập mật khẩu"
                :disabled="loading"
                @keyup.enter="verifyStepUp"
              />
            </div>
            <div v-if="requireMfaCode" class="sum-field">
              <label>Mã MFA</label>
              <input
                v-model="mfaCode"
                type="text"
                class="sum-input"
                placeholder="Nhập mã MFA nếu có"
                :disabled="loading"
                @keyup.enter="verifyStepUp"
              />
            </div>
            <div class="sum-actions">
              <button type="button" class="sum-btn sum-btn--ghost" :disabled="loading" @click="handleCancel">
                Hủy
              </button>
              <button
                type="button"
                class="sum-btn sum-btn--danger"
                :disabled="loading || !password.trim()"
                @click="verifyStepUp"
              >
                {{ loading ? 'Đang xác minh...' : 'Xác nhận thực hiện' }}
              </button>
            </div>
          </div>

          <div v-if="success" class="sum-success">
            <div class="sum-success-icon">&#10003;</div>
            <p class="sum-success-text">Xác thực thành công. Đang thực hiện hành động...</p>
          </div>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script>
import { enterpriseApi } from '../../services/enterpriseSecurityApi'

export default {
  name: 'StepUpModal',
  props: {
    visible: { type: Boolean, default: false },
    actionLabel: { type: String, default: '' },
    actionDescription: { type: String, default: '' },
    severity: {
      type: String,
      default: 'high',
      validator: (v) => ['low', 'medium', 'high', 'critical'].includes(v),
    },
    requireMfa: { type: Boolean, default: false },
  },
  emits: ['cancel', 'confirmed'],
  data() {
    return {
      reason: '',
      password: '',
      mfaCode: '',
      loading: false,
      error: '',
      stepUpStarted: false,
      stepUpSessionId: null,
      requireMfaCode: false,
      success: false,
    }
  },
  computed: {
    severityLabel() {
      const map = { low: 'Thấp', medium: 'Trung bình', high: 'Cao', critical: 'Nghiêm trọng' }
      return map[this.severity] || 'Cao'
    },
    severityClass() {
      return `sum-badge--${this.severity}`
    },
  },
  watch: {
    visible(val) {
      if (val) this.reset()
    },
  },
  methods: {
    reset() {
      this.reason = ''
      this.password = ''
      this.mfaCode = ''
      this.error = ''
      this.loading = false
      this.stepUpStarted = false
      this.stepUpSessionId = null
      this.requireMfaCode = this.requireMfa
      this.success = false
    },
    handleCancel() {
      if (this.loading) return
      this.reset()
      enterpriseApi.setStepUpSession(null)
      this.$emit('cancel')
    },
    async startStepUp() {
      if (!this.reason.trim()) {
        this.error = 'Vui lòng nhập lý do.'
        return
      }
      this.loading = true
      this.error = ''
      try {
        const res = await enterpriseApi.stepUpStart(this.actionLabel, this.reason.trim())
        this.stepUpSessionId = res.data?.sessionId
        this.requireMfaCode = this.requireMfa || res.data?.requiresMfa
        this.stepUpStarted = true
      } catch (e) {
        this.error = e?.response?.data?.message || e?.message || 'Không thể bắt đầu quy trình xác thực.'
      } finally {
        this.loading = false
      }
    },
    async verifyStepUp() {
      if (!this.password.trim()) {
        this.error = 'Vui lòng nhập mật khẩu.'
        return
      }
      this.loading = true
      this.error = ''
      try {
        await enterpriseApi.stepUpVerify(this.stepUpSessionId, this.password.trim(), this.mfaCode.trim() || undefined)
        this.success = true
        enterpriseApi.setStepUpSession(this.stepUpSessionId)
        this.$emit('confirmed', { sessionId: this.stepUpSessionId, reason: this.reason.trim() })
      } catch (e) {
        this.error = e?.response?.data?.message || e?.message || 'Xác thực thất bại.'
        enterpriseApi.setStepUpSession(null)
      } finally {
        this.loading = false
      }
    },
  },
}
</script>

<style scoped>
.sum-overlay {
  position: fixed;
  inset: 0;
  background: rgba(2, 6, 23, 0.55);
  z-index: 500;
  display: grid;
  place-items: center;
  padding: 16px;
}
.sum-dialog {
  width: min(480px, 92vw);
  background: #ffffff;
  border-radius: 16px;
  box-shadow: 0 24px 64px rgba(2, 6, 23, 0.3);
  overflow: hidden;
}
.sum-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 18px 20px 12px;
  border-bottom: 1px solid #e2e8f0;
}
.sum-title {
  margin: 0;
  font-size: 18px;
  font-weight: 800;
  color: #0f172a;
}
.sum-close {
  width: 36px;
  height: 36px;
  border: none;
  border-radius: 8px;
  background: #f1f5f9;
  color: #475569;
  font-size: 22px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
}
.sum-close:hover {
  background: #e2e8f0;
}
.sum-body {
  padding: 16px 20px 20px;
}
.sum-context {
  margin-bottom: 16px;
  padding: 12px;
  background: #f8fafc;
  border-radius: 10px;
  border: 1px solid #e9eef5;
}
.sum-badge {
  display: inline-block;
  padding: 3px 10px;
  border-radius: 999px;
  font-size: 11px;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  margin-bottom: 8px;
}
.sum-badge--low { background: #dcfce7; color: #166534; }
.sum-badge--medium { background: #fef3c7; color: #92400e; }
.sum-badge--high { background: #fee2e2; color: #991b1b; }
.sum-badge--critical { background: #fce7f3; color: #9d174d; }
.sum-action-name {
  margin: 0 0 4px;
  font-size: 16px;
  font-weight: 800;
  color: #0f172a;
}
.sum-action-desc {
  margin: 0;
  font-size: 13px;
  color: #64748b;
}
.sum-alert {
  padding: 10px 14px;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 700;
  margin-bottom: 12px;
}
.sum-alert--danger {
  background: #fee2e2;
  color: #991b1b;
  border: 1px solid #fca5a5;
}
.sum-field {
  margin-bottom: 14px;
}
.sum-field label {
  display: block;
  font-size: 13px;
  font-weight: 700;
  color: #334155;
  margin-bottom: 6px;
}
.sum-input {
  width: 100%;
  border: 1px solid #cbd5e1;
  border-radius: 10px;
  padding: 10px 12px;
  font-size: 14px;
  outline: none;
  background: #fff;
  box-sizing: border-box;
}
.sum-input:focus {
  border-color: #60a5fa;
  box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.08);
}
.sum-textarea {
  resize: vertical;
  min-height: 72px;
  font-family: inherit;
}
.sum-step-init {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.sum-step-verify {
  padding-top: 4px;
}
.sum-verify-hint {
  margin: 0 0 14px;
  font-size: 14px;
  color: #475569;
}
.sum-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 16px;
}
.sum-btn {
  min-height: 40px;
  padding: 0 18px;
  border-radius: 10px;
  font-size: 14px;
  font-weight: 700;
  border: none;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 6px;
}
.sum-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.sum-btn--primary {
  background: #2563eb;
  color: #fff;
  width: 100%;
  justify-content: center;
}
.sum-btn--primary:hover:not(:disabled) {
  background: #1d4ed8;
}
.sum-btn--danger {
  background: #dc2626;
  color: #fff;
}
.sum-btn--danger:hover:not(:disabled) {
  background: #b91c1c;
}
.sum-btn--ghost {
  background: #f1f5f9;
  color: #334155;
}
.sum-btn--ghost:hover:not(:disabled) {
  background: #e2e8f0;
}
.sum-success {
  text-align: center;
  padding: 20px 0;
}
.sum-success-icon {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background: #22c55e;
  color: #fff;
  font-size: 28px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 12px;
}
.sum-success-text {
  font-size: 15px;
  font-weight: 700;
  color: #166534;
  margin: 0;
}
</style>
