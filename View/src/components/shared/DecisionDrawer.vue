<template>
  <Teleport to="body">
    <div v-if="visible" class="dd-root" @click.self="handleClose">
      <aside class="dd-panel" role="dialog" aria-modal="true" aria-label="Bảng quyết định làn">
        <div class="dd-header">
          <h2 class="dd-title">Quyết định: {{ laneName }}</h2>
          <button type="button" class="dd-close" aria-label="Đóng" @click="handleClose">&times;</button>
        </div>

        <div class="dd-body">
          <!-- Subject Status -->
          <div class="dd-subject">
            <div class="dd-subject-row">
              <span class="dd-subject-label">Đối tượng</span>
              <span class="dd-subject-value">{{ subjectName || '---' }}</span>
            </div>
            <div class="dd-subject-row">
              <span class="dd-subject-label">ID</span>
              <span class="dd-subject-value dd-subject-value--mono">{{ subjectId || '---' }}</span>
            </div>
            <div class="dd-subject-row" v-if="subjectType">
              <span class="dd-subject-label">Loại</span>
              <span class="dd-subject-badge" :class="subjectType === 'EMPLOYEE' ? 'dd-badge--emp' : 'dd-badge--guest'">
                {{ subjectType === 'EMPLOYEE' ? 'Nhân viên' : 'Khách' }}
              </span>
            </div>
            <div class="dd-subject-row" v-if="plateNumber">
              <span class="dd-subject-label">Biển số</span>
              <span class="dd-subject-value dd-subject-value--plate">{{ plateNumber }}</span>
            </div>
            <div class="dd-subject-row" v-if="qrPayload">
              <span class="dd-subject-label">QR</span>
              <span class="dd-subject-value dd-subject-value--mono dd-subject-value--truncate">{{ qrPayload }}</span>
            </div>
          </div>

          <!-- Warnings -->
          <div v-if="warnings.length > 0" class="dd-warnings">
            <div v-for="(w, i) in warnings" :key="i" class="dd-warning" :class="`dd-warning--${w.severity || 'warn'}`">
              <span v-html="w.icon || '&#9888;'" class="dd-warning-icon"></span>
              <span>{{ w.text }}</span>
            </div>
          </div>

          <!-- Actions -->
          <div class="dd-actions-section">
            <div class="dd-actions-label">Thao tác quyết định</div>

            <!-- Safe Actions -->
            <button
              type="button"
              class="dd-action dd-action--primary"
              :disabled="!canAllow || loading"
              @click="emitAction('allow')"
            >
              <span class="dd-action-icon">&#10003;</span>
              <span class="dd-action-text">
                <strong>Cho qua</strong>
                <small>Theo quy tắc thường</small>
              </span>
            </button>

            <button
              type="button"
              class="dd-action dd-action--danger"
              :disabled="!canDeny || loading"
              @click="emitAction('deny')"
            >
              <span class="dd-action-icon">&#10007;</span>
              <span class="dd-action-text">
                <strong>Từ chối</strong>
                <small>Từ chối không cho qua</small>
              </span>
            </button>

            <button
              type="button"
              class="dd-action dd-action--warning"
              :disabled="!canManual || loading"
              @click="openReasonForm('manual')"
            >
              <span class="dd-action-icon">&#9998;</span>
              <span class="dd-action-text">
                <strong>Vận hành thủ công</strong>
                <small>Chuyển manual mode và xử lý</small>
              </span>
            </button>

            <!-- Unified Emergency Override -->
            <button
              type="button"
              class="dd-action dd-action--unified"
              :disabled="!canUnifiedEmergency || loading"
              @pointerdown="startPress"
              @pointerup="endPress"
              @pointerleave="cancelPress"
            >
              <span class="dd-action-icon">&#9888;&#65039;</span>
              <span class="dd-action-text">
                <strong>Cấp quyền khẩn cấp</strong>
                <small>Cho qua, chịu trách nhiệm</small>
              </span>
            </button>

            <!-- Escalation -->
            <button
              type="button"
              class="dd-action dd-action--admin"
              :disabled="!canEscalate || loading"
              @click="openReasonForm('escalate')"
            >
              <span class="dd-action-icon">&#8593;</span>
              <span class="dd-action-text">
                <strong>Xin phép quản lý</strong>
                <small>Gửi yêu cầu can thiệp lên cấp trên</small>
              </span>
            </button>
          </div>
        </div>

        <!-- Reason / Responsibility Form -->
        <transition name="dd-slide">
          <div v-if="formMode" class="dd-form-section">
            <div class="dd-form-head">
              <h3>{{ formTitle }}</h3>
            </div>
            <PrivilegedActionReasonForm
              ref="reasonForm"
              v-model="actionReason"
              :required="true"
              :require-responsibility="formMode === 'override' || formMode === 'duress' || formMode === 'emergency'"
              :show-error="formError"
              placeholder="Nhập lý do cho hành động này..."
              :disabled="saving"
              @responsibility-change="responsibilityAccepted = $event"
            />
            <div v-if="formMode === 'manual' || formMode === 'emergency'" class="dd-manual-grid">
              <label>
                <span>Họ tên / đơn vị</span>
                <input v-model.trim="manualSubjectName" type="text" placeholder="Ví dụ: Kíp cấp cứu 115" />
              </label>
              <label>
                <span>Mã người / giấy tờ</span>
                <input v-model.trim="manualSubjectId" type="text" placeholder="Mã nhân viên hoặc giấy tờ" />
              </label>
              <label class="dd-manual-full">
                <span>Biển số</span>
                <input v-model.trim="manualPlateNumber" type="text" placeholder="51A-123.45" />
              </label>
            </div>
            <div v-if="formError" class="dd-form-error">Vui lòng nhập lý do và xác nhận trách nhiệm.</div>
            <div class="dd-form-actions">
              <button class="dd-btn dd-btn--ghost" :disabled="saving" @click="cancelForm">Hủy</button>
              <button
                class="dd-btn dd-btn--primary"
                :disabled="saving || !actionReason.trim()"
                @click="submitForm"
              >
                {{ saving ? 'Đang xử lý...' : formSubmitLabel }}
              </button>
            </div>
          </div>
        </transition>
      </aside>
    </div>
  </Teleport>
</template>

<script>
import PrivilegedActionReasonForm from './PrivilegedActionReasonForm.vue'

export default {
  name: 'DecisionDrawer',
  components: { PrivilegedActionReasonForm },
  props: {
    visible: { type: Boolean, default: false },
    laneName: { type: String, default: '' },
    subjectName: { type: String, default: '' },
    subjectId: { type: [String, Number], default: '' },
    subjectType: { type: String, default: '' },
    plateNumber: { type: String, default: '' },
    qrPayload: { type: String, default: '' },
    warnings: { type: Array, default: () => [] },
    canAllow: { type: Boolean, default: true },
    canDeny: { type: Boolean, default: true },
    canManual: { type: Boolean, default: true },
    canOverride: { type: Boolean, default: true },
    canDuress: { type: Boolean, default: true },
    canEscalate: { type: Boolean, default: true },
    canEmergency: { type: Boolean, default: false },
    canUnifiedEmergency: { type: Boolean, default: false },
    requireReasonForAllow: { type: Boolean, default: false },
    requireReasonForDeny: { type: Boolean, default: false },
    loading: { type: Boolean, default: false },
  },
  emits: ['close', 'action'],
  data() {
    return {
      formMode: '',
      actionReason: '',
      responsibilityAccepted: false,
      formError: false,
      saving: false,
      currentAction: null,
      manualSubjectName: '',
      manualSubjectId: '',
      manualPlateNumber: '',
      _isDuress: false,
      pressTimer: null,
    }
  },
    computed: {
      formTitle() {
        const titles = {
          manual: 'Vận hành thủ công',
          override: 'Cho qua có chịu trách nhiệm',
          duress: 'Ghi nhận ép buộc (Duress)',
          escalate: 'Xin phép quản lý',
          emergency: 'Cấp quyền khẩn cấp ngay',
          unified_emergency: 'Cấp quyền khẩn cấp',
        }
        return titles[this.formMode] || 'Xác nhận hành động'
      },
      formSubmitLabel() {
        const labels = {
          allow: 'Xác nhận cho qua',
          deny: 'Xác nhận từ chối',
          manual: 'Xác nhận chuyển manual',
          override: 'Xác nhận chịu trách nhiệm',
          duress: 'Gửi tín hiệu duress',
          escalate: 'Gửi yêu cầu',
          emergency: 'Cấp quyền và phát cảnh báo',
          unified_emergency: 'Cấp quyền và phát cảnh báo',
        }
        return labels[this.formMode] || 'Xác nhận'
      },
    },
  watch: {
    visible(val) {
      if (!val) this.resetForm()
    },
  },
  mounted() {
    this._onKeyDown = (e) => {
      if (e.key === 'Escape' && this.visible) {
        this.handleClose()
      }
    }
    window.addEventListener('keydown', this._onKeyDown)
  },
  beforeUnmount() {
    if (this._onKeyDown) {
      window.removeEventListener('keydown', this._onKeyDown)
    }
  },
  methods: {
    handleClose() {
      if (this.saving) return
      this.resetForm()
      this.$emit('close')
    },
    emitAction(type) {
      if ((type === 'allow' && this.requireReasonForAllow) || (type === 'deny' && this.requireReasonForDeny)) {
        this.openReasonForm(type)
        return
      }
      this.$emit('action', { type, reason: '', responsibility: false })
    },
    openReasonForm(mode) {
      this.formMode = mode
      this.actionReason = ''
      this.responsibilityAccepted = false
      this.formError = false
      this.seedManualFields()
    },
    openStepUp(mode) {
      this.formMode = mode
      this.formError = false
      // For emergency - always require reason + escalation
      this.actionReason = ''
      this.responsibilityAccepted = false
      this.seedManualFields()
    },
    cancelForm() {
      this.resetForm()
    },
    resetForm() {
      this.formMode = ''
      this.actionReason = ''
      this.responsibilityAccepted = false
      this.formError = false
      this.saving = false
      this.manualSubjectName = ''
      this.manualSubjectId = ''
      this.manualPlateNumber = ''
      this._isDuress = false
      this.cancelPress()
    },
    startPress() {
      this.pressTimer = setTimeout(() => {
        this._isDuress = true
        if (navigator.vibrate) navigator.vibrate(30)
      }, 1500)
    },
    endPress() {
      if (this.pressTimer) {
        clearTimeout(this.pressTimer)
        this.pressTimer = null
      }
      this.openUnifiedForm(this._isDuress)
    },
    cancelPress() {
      if (this.pressTimer) {
        clearTimeout(this.pressTimer)
        this.pressTimer = null
      }
      this._isDuress = false
    },
    openUnifiedForm(isDuress) {
      this.formMode = 'unified_emergency'
      this._isDuress = isDuress
      this.actionReason = ''
      this.responsibilityAccepted = false
      this.formError = false
      this.seedManualFields()
    },
    seedManualFields() {
      this.manualSubjectName = this.subjectName || ''
      this.manualSubjectId = String(this.subjectId || '')
      this.manualPlateNumber = this.plateNumber || ''
    },
    async submitForm() {
      if (!this.actionReason.trim()) {
        this.formError = true
        return
      }
      if ((this.formMode === 'override' || this.formMode === 'duress' || this.formMode === 'emergency' || this.formMode === 'unified_emergency') && !this.responsibilityAccepted) {
        this.formError = true
        return
      }
      if ((this.formMode === 'manual' || this.formMode === 'emergency' || this.formMode === 'unified_emergency') && !this.manualSubjectName && !this.manualPlateNumber) {
        this.formError = true
        return
      }
      this.formError = false
      this.saving = true
      const payload = {
        type: this.formMode,
        reason: this.actionReason.trim(),
        responsibility: this.responsibilityAccepted,
        details: {
          subjectName: this.manualSubjectName,
          subjectId: this.manualSubjectId,
          plateNumber: this.manualPlateNumber,
        },
      }
      if (this.formMode === 'unified_emergency') {
        payload._duress = this._isDuress
      }
      this.$emit('action', payload)
    },
    resetSaving() {
      this.saving = false
    },
  },
}
</script>

<style scoped>
.dd-root {
  position: fixed;
  inset: 0;
  background: rgba(2, 6, 23, 0.45);
  z-index: 400;
  display: flex;
  justify-content: flex-end;
}
.dd-panel {
  width: min(440px, 92vw);
  height: 100%;
  background: var(--surface-default);
  border-left: 1px solid var(--border-subtle);
  box-shadow: -16px 0 40px rgba(15, 23, 42, 0.12);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.dd-manual-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-top: 12px; }
.dd-manual-grid label { display: grid; gap: 5px; color: var(--text-secondary); font-size: 12px; font-weight: 700; }
.dd-manual-grid input { width: 100%; min-height: 40px; border: 1px solid var(--border-subtle); background: var(--surface-subtle); padding: 8px 10px; color: var(--text-primary); border-radius: 8px; }
.dd-manual-full { grid-column: 1 / -1; }
.dd-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 18px;
  border-bottom: 1px solid var(--border-subtle);
  flex-shrink: 0;
}
.dd-title {
  margin: 0;
  font-size: 18px;
  font-weight: 800;
  color: var(--text-primary);
}
.dd-close {
  width: 36px;
  height: 36px;
  border: none;
  border-radius: 8px;
  background: var(--surface-subtle);
  color: var(--text-secondary);
  font-size: 22px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
}
.dd-close:hover {
  background: var(--surface-hover);
}
.dd-body {
  flex: 1;
  overflow-y: auto;
  padding: 16px 18px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.dd-hero {
  display: flex;
  align-items: flex-start;
  gap: 12px;
}
.dd-hero-avatar {
  width: 44px;
  height: 44px;
  border-radius: 10px;
  overflow: hidden;
  background: var(--surface-subtle);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 20px;
  flex-shrink: 0;
}
.dd-hero-avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
.dd-hero-info {
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.dd-hero-name {
  font-size: 15px;
  font-weight: 800;
  color: var(--text-primary);
}
.dd-hero-meta {
  font-size: 12px;
  color: var(--text-secondary);
}
.dd-hero-lane {
  font-size: 12px;
  font-weight: 700;
  color: var(--accent-primary);
}
.dd-badge-row {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}
.dd-badge {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 3px 8px;
  border-radius: 6px;
  font-size: 11px;
  font-weight: 700;
}
.dd-badge--critical { background: var(--status-danger-bg); color: var(--status-danger-text); }
.dd-badge--high { background: var(--status-warning-bg); color: var(--status-warning-text); }
.dd-badge--medium { background: var(--status-info-bg); color: var(--status-info-text); }
.dd-badge--low { background: var(--status-success-bg); color: var(--status-success-text); }
.dd-badge--pending { background: var(--status-warning-bg); color: var(--status-warning-text); }
.dd-badge--accepted { background: var(--status-info-bg); color: var(--status-info-text); }
.dd-badge--executed { background: var(--status-success-bg); color: var(--status-success-text); }
.dd-badge--rejected { background: var(--status-danger-bg); color: var(--status-danger-text); }
.dd-badge--closed { background: var(--status-neutral-bg); color: var(--status-neutral-text); }
.dd-badge--category { background: var(--surface-subtle); color: var(--text-secondary); border: 1px solid var(--border-subtle); }
.dd-plate {
  font-family: monospace;
  font-size: 12px;
  font-weight: 800;
  background: var(--surface-subtle);
  color: var(--text-primary);
  border: 1px solid var(--border-subtle);
  border-radius: 4px;
  padding: 2px 6px;
}
.dd-warning {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  border-radius: 8px;
  font-size: 13px;
  font-weight: 600;
  line-height: 1.4;
}
.dd-warning--critical { background: var(--status-danger-bg); color: var(--status-danger-text); border: 1px solid var(--status-danger-border); }
.dd-warning--warn { background: var(--status-warning-bg); color: var(--status-warning-text); border: 1px solid var(--status-warning-border); }
.dd-warning--info { background: var(--status-info-bg); color: var(--status-info-text); border: 1px solid var(--status-info-border); }
.dd-warning-icon { font-size: 16px; flex-shrink: 0; }
.dd-actions-section {
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.dd-actions-label {
  font-size: 11px;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--text-muted);
  margin-bottom: 4px;
}
.dd-action {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
  padding: 12px 14px;
  border: 1px solid var(--border-subtle);
  border-radius: 12px;
  background: var(--surface-default);
  cursor: pointer;
  transition: all 0.15s ease;
  text-align: left;
  font-family: inherit;
}
.dd-action:hover:not(:disabled) {
  border-color: var(--border-focus);
  background: var(--surface-hover);
  box-shadow: 0 2px 8px rgba(15, 23, 42, 0.06);
}
.dd-action:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}
.dd-action-icon {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  flex-shrink: 0;
}
.dd-action--primary .dd-action-icon { background: var(--status-success-bg); color: var(--status-success-text); }
.dd-action--danger .dd-action-icon { background: var(--status-danger-bg); color: var(--status-danger-text); }
.dd-action--warning .dd-action-icon { background: var(--status-warning-bg); color: var(--status-warning-text); }
.dd-action--caution .dd-action-icon { background: var(--status-warning-bg); color: var(--status-warning-text); }
.dd-action--duress .dd-action-icon { background: var(--status-danger-bg); color: var(--status-danger-text); }
.dd-action--admin .dd-action-icon { background: var(--status-info-bg); color: var(--status-info-text); }
.dd-action--unified .dd-action-icon { background: var(--status-warning-bg); color: var(--status-warning-text); }
.dd-action-text {
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.dd-action-text strong {
  font-size: 14px;
  color: var(--text-primary);
}
.dd-action-text small {
  font-size: 12px;
  color: var(--text-muted);
}
.dd-form-section {
  border-top: 1px solid var(--border-subtle);
  padding: 16px 18px;
  flex-shrink: 0;
  background: var(--surface-subtle);
}
.dd-form-head h3 {
  margin: 0 0 12px;
  font-size: 15px;
  font-weight: 800;
  color: var(--text-primary);
}
.dd-form-error {
  margin-top: 8px;
  padding: 8px 12px;
  border: 1px solid var(--status-danger-border);
  border-radius: 8px;
  background: var(--status-danger-bg);
  color: var(--status-danger-text);
  font-size: 13px;
  font-weight: 600;
}
.dd-form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 14px;
}
.dd-btn {
  min-height: 40px;
  padding: 0 18px;
  border-radius: 10px;
  font-size: 14px;
  font-weight: 700;
  border: 1px solid transparent;
  cursor: pointer;
  transition: background-color var(--transition-fast, 0.15s ease), border-color var(--transition-fast, 0.15s ease);
}
.dd-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.dd-btn--primary {
  background: var(--interactive-primary);
  color: var(--text-on-interactive);
}
.dd-btn--primary:hover:not(:disabled) {
  background: var(--interactive-primary-hover);
}
.dd-btn--ghost {
  background: var(--surface-subtle);
  border-color: var(--border-subtle);
  color: var(--text-secondary);
}
.dd-btn--ghost:hover:not(:disabled) {
  background: var(--surface-hover);
  color: var(--text-primary);
}
.dd-slide-enter-active,
.dd-slide-leave-active {
  transition: all 0.25s ease;
}
.dd-slide-enter-from,
.dd-slide-leave-to {
  transform: translateY(20px);
  opacity: 0;
}
</style>
