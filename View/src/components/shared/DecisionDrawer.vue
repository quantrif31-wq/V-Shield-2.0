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
            <div class="dd-actions-label">Thao tác</div>

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

            <!-- Responsibility Actions -->
            <button
              type="button"
              class="dd-action dd-action--caution"
              :disabled="!canOverride || loading"
              @click="openResponsibilityForm('override')"
            >
              <span class="dd-action-icon">&#9888;</span>
              <span class="dd-action-text">
                <strong>Cho qua có chịu trách nhiệm</strong>
                <small>Override quy tắc, tự chịu trách nhiệm</small>
              </span>
            </button>

            <!-- Duress -->
            <button
              type="button"
              class="dd-action dd-action--danger dd-action--duress"
              :disabled="!canDuress || loading"
              @click="openReasonForm('duress')"
            >
              <span class="dd-action-icon">&#9940;</span>
              <span class="dd-action-text">
                <strong>Ghi nhận ép buộc / Duress</strong>
                <small>Báo động đang bị ép buộc</small>
              </span>
            </button>

            <!-- Emergency / Escalation -->
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

            <button
              type="button"
              class="dd-action dd-action--admin"
              :disabled="!canEmergency || loading"
              @click="openStepUp('emergency')"
            >
              <span class="dd-action-icon">&#9888;&#65039;</span>
              <span class="dd-action-text">
                <strong>Cấp quyền khẩn cấp</strong>
                <small>Yêu cầu Admin cấp temporary grant</small>
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
              :require-responsibility="formMode === 'override' || formMode === 'duress'"
              :show-error="formError"
              placeholder="Nhập lý do cho hành động này..."
              :disabled="saving"
              @responsibility-change="responsibilityAccepted = $event"
            />
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
    }
  },
  computed: {
    formTitle() {
      const titles = {
        manual: 'Vận hành thủ công',
        override: 'Cho qua có chịu trách nhiệm',
        duress: 'Ghi nhận ép buộc (Duress)',
        escalate: 'Xin phép quản lý',
        emergency: 'Yêu cầu cấp quyền khẩn cấp',
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
        emergency: 'Gửi yêu cầu khẩn cấp',
      }
      return labels[this.formMode] || 'Xác nhận'
    },
  },
  watch: {
    visible(val) {
      if (!val) this.resetForm()
    },
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
    },
    openStepUp(mode) {
      this.formMode = mode
      this.formError = false
      // For emergency - always require reason + escalation
      this.actionReason = ''
      this.responsibilityAccepted = false
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
    },
    async submitForm() {
      if (!this.actionReason.trim()) {
        this.formError = true
        return
      }
      if ((this.formMode === 'override' || this.formMode === 'duress') && !this.responsibilityAccepted) {
        this.formError = true
        return
      }
      this.formError = false
      this.saving = true
      this.$emit('action', {
        type: this.formMode,
        reason: this.actionReason.trim(),
        responsibility: this.responsibilityAccepted,
      })
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
  background: #ffffff;
  border-left: 1px solid #e2e8f0;
  box-shadow: -16px 0 40px rgba(15, 23, 42, 0.12);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.dd-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 18px;
  border-bottom: 1px solid #e2e8f0;
  flex-shrink: 0;
}
.dd-title {
  margin: 0;
  font-size: 18px;
  font-weight: 800;
  color: #0f172a;
}
.dd-close {
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
.dd-close:hover {
  background: #e2e8f0;
}
.dd-body {
  flex: 1;
  overflow-y: auto;
  padding: 14px 18px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.dd-subject {
  background: #f8fafc;
  border: 1px solid #e9eef5;
  border-radius: 12px;
  padding: 12px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}
.dd-subject-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 8px;
}
.dd-subject-label {
  font-size: 12px;
  font-weight: 700;
  color: #64748b;
  text-transform: uppercase;
  letter-spacing: 0.03em;
}
.dd-subject-value {
  font-size: 14px;
  font-weight: 700;
  color: #0f172a;
  text-align: right;
  word-break: break-word;
}
.dd-subject-value--mono {
  font-family: 'JetBrains Mono', 'SF Mono', monospace;
  font-size: 13px;
}
.dd-subject-value--plate {
  font-family: 'JetBrains Mono', 'SF Mono', monospace;
  font-size: 15px;
  color: #15803d;
  letter-spacing: 1px;
}
.dd-subject-value--truncate {
  max-width: 200px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.dd-subject-badge {
  padding: 2px 10px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 700;
}
.dd-badge--emp {
  background: #dcfce7;
  color: #166534;
}
.dd-badge--guest {
  background: #dbeafe;
  color: #1e40af;
}
.dd-warnings {
  display: flex;
  flex-direction: column;
  gap: 8px;
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
.dd-warning--critical { background: #fef2f2; color: #991b1b; border: 1px solid #fecaca; }
.dd-warning--warn { background: #fffbeb; color: #92400e; border: 1px solid #fde68a; }
.dd-warning--info { background: #eff6ff; color: #1e40af; border: 1px solid #bfdbfe; }
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
  color: #64748b;
  margin-bottom: 4px;
}
.dd-action {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
  padding: 12px 14px;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  background: #ffffff;
  cursor: pointer;
  transition: all 0.15s ease;
  text-align: left;
  font-family: inherit;
}
.dd-action:hover:not(:disabled) {
  border-color: #94a3b8;
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
.dd-action--primary .dd-action-icon { background: #dcfce7; color: #166534; }
.dd-action--danger .dd-action-icon { background: #fee2e2; color: #991b1b; }
.dd-action--warning .dd-action-icon { background: #fef3c7; color: #92400e; }
.dd-action--caution .dd-action-icon { background: #fff7ed; color: #c2410c; }
.dd-action--duress .dd-action-icon { background: #fce7f3; color: #9d174d; }
.dd-action--admin .dd-action-icon { background: #eff6ff; color: #1d4ed8; }
.dd-action-text {
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.dd-action-text strong {
  font-size: 14px;
  color: #0f172a;
}
.dd-action-text small {
  font-size: 12px;
  color: #64748b;
}
.dd-form-section {
  border-top: 1px solid #e2e8f0;
  padding: 16px 18px;
  flex-shrink: 0;
  background: #fafcff;
}
.dd-form-head h3 {
  margin: 0 0 12px;
  font-size: 15px;
  font-weight: 800;
  color: #0f172a;
}
.dd-form-error {
  margin-top: 8px;
  padding: 8px 12px;
  background: #fef2f2;
  border: 1px solid #fecaca;
  border-radius: 8px;
  color: #991b1b;
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
  border: none;
  cursor: pointer;
}
.dd-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.dd-btn--primary {
  background: #2563eb;
  color: #fff;
}
.dd-btn--ghost {
  background: #f1f5f9;
  color: #334155;
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
