<template>
  <div class="parf-root">
    <div class="parf-field">
      <label class="parf-label">
        Lý do thực hiện
        <span v-if="required" class="parf-required">*</span>
      </label>
      <textarea
        v-model="internalReason"
        class="parf-textarea"
        :class="{ 'parf-error': showError && !internalReason.trim() && required }"
        :placeholder="placeholder"
        :rows="rows"
        :disabled="disabled"
        @input="onInput"
      ></textarea>
    </div>

    <div v-if="requireResponsibility" class="parf-responsibility">
      <label class="parf-checkbox-label">
        <input
          type="checkbox"
          v-model="acceptedResponsibility"
          :disabled="disabled"
          @change="onResponsibilityChange"
        />
        <span class="parf-checkbox-text">
          Tôi xác nhận chịu trách nhiệm về hành động này và hiểu rõ hậu quả pháp lý
          <span v-if="required" class="parf-required">*</span>
        </span>
      </label>
      <p v-if="showError && required && !acceptedResponsibility" class="parf-error-text">
        Vui lòng xác nhận trách nhiệm
      </p>
    </div>

    <div v-if="requireEscalationNote" class="parf-field">
      <label class="parf-label">
        Ghi chú chuyển tiếp (nếu cần)
      </label>
      <textarea
        v-model="internalEscalationNote"
        class="parf-textarea parf-textarea--small"
        rows="2"
        placeholder="Ghi chú cho người xử lý tiếp theo..."
        :disabled="disabled"
        @input="onEscalationChange"
      ></textarea>
    </div>
  </div>
</template>

<script>
export default {
  name: 'PrivilegedActionReasonForm',
  props: {
    modelValue: { type: String, default: '' },
    placeholder: { type: String, default: 'Nhập lý do bắt buộc cho hành động này...' },
    required: { type: Boolean, default: true },
    rows: { type: Number, default: 3 },
    disabled: { type: Boolean, default: false },
    showError: { type: Boolean, default: false },
    requireResponsibility: { type: Boolean, default: false },
    requireEscalationNote: { type: Boolean, default: false },
    escalationNote: { type: String, default: '' },
  },
  emits: ['update:modelValue', 'responsibility-change', 'update:escalationNote'],
  data() {
    return {
      internalReason: this.modelValue,
      acceptedResponsibility: false,
      internalEscalationNote: this.escalationNote,
    }
  },
  watch: {
    modelValue(val) { this.internalReason = val },
    escalationNote(val) { this.internalEscalationNote = val },
  },
  methods: {
    onInput() {
      this.$emit('update:modelValue', this.internalReason)
    },
    onResponsibilityChange() {
      this.$emit('responsibility-change', this.acceptedResponsibility)
    },
    onEscalationChange() {
      this.$emit('update:escalationNote', this.internalEscalationNote)
    },
    isValid() {
      if (this.required && !this.internalReason.trim()) return false
      if (this.requireResponsibility && !this.acceptedResponsibility) return false
      return true
    },
    getValues() {
      return {
        reason: this.internalReason.trim(),
        acceptedResponsibility: this.acceptedResponsibility,
        escalationNote: this.internalEscalationNote.trim(),
      }
    },
    reset() {
      this.internalReason = ''
      this.acceptedResponsibility = false
      this.internalEscalationNote = ''
    },
  },
}
</script>

<style scoped>
.parf-root {
  display: flex;
  flex-direction: column;
  gap: 14px;
}
.parf-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
}
.parf-label {
  font-size: 13px;
  font-weight: 700;
  color: #334155;
}
.parf-required {
  color: #dc2626;
  margin-left: 2px;
}
.parf-textarea {
  width: 100%;
  border: 1px solid var(--border-subtle);
  border-radius: 10px;
  padding: 10px 12px;
  font-size: 14px;
  font-family: inherit;
  outline: none;
  resize: vertical;
  min-height: 64px;
  background: var(--surface-subtle);
  color: var(--text-primary);
  box-sizing: border-box;
}
.parf-textarea:focus {
  border-color: var(--border-focus);
  background: var(--surface-default);
  box-shadow: 0 0 0 3px rgba(84, 196, 211, 0.16);
}
.parf-textarea--small {
  min-height: 44px;
}
.parf-error {
  border-color: var(--border-danger);
  box-shadow: 0 0 0 3px rgba(239, 68, 68, 0.1);
}
.parf-error-text {
  margin: 0;
  font-size: 12px;
  color: var(--status-danger-text);
  font-weight: 600;
}
.parf-responsibility {
  padding: 12px;
  background: var(--status-warning-bg);
  border: 1px solid var(--status-warning-border);
  border-radius: 10px;
}
.parf-checkbox-label {
  display: flex;
  gap: 10px;
  align-items: flex-start;
  cursor: pointer;
}
.parf-checkbox-label input[type="checkbox"] {
  margin-top: 2px;
  width: 18px;
  height: 18px;
  flex-shrink: 0;
  -webkit-appearance: none;
  appearance: none;
  border: 2px solid var(--border-subtle);
  border-radius: 4px;
  background: var(--surface-subtle);
  cursor: pointer;
  position: relative;
  transition: all 0.15s ease;
}
.parf-checkbox-label input[type="checkbox"]:checked {
  border-color: var(--accent-warning);
  background: var(--accent-warning);
}
.parf-checkbox-label input[type="checkbox"]:checked::after {
  content: '';
  position: absolute;
  left: 4px;
  top: 1px;
  width: 6px;
  height: 10px;
  border: solid #fff;
  border-width: 0 2px 2px 0;
  transform: rotate(45deg);
}
.parf-checkbox-text {
  font-size: 13px;
  line-height: 1.45;
  color: #431407;
  font-weight: 600;
}
</style>
