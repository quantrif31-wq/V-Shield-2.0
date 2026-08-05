<template>
  <div class="vs-field" :class="{ 'has-error': error, 'has-success': success, 'is-disabled': disabled }">
    <div class="vs-field__label-row">
      <label :for="forId" class="vs-field__label">
        {{ label }}<span v-if="required" class="vs-field__required" aria-hidden="true"> *</span>
        <span v-if="required" class="sr-only"> (bắt buộc)</span>
      </label>
      <span v-if="counter" class="vs-field__counter">{{ counter }}</span>
    </div>
    <p v-if="description" :id="descriptionId" class="vs-field__description">{{ description }}</p>
    <slot :describedby="describedby" :invalid="Boolean(error)" />
    <p v-if="error" :id="errorId" class="vs-field__message is-error" role="alert">
      <span aria-hidden="true">!</span>{{ error }}
    </p>
    <p v-else-if="success" :id="successId" class="vs-field__message is-success">
      <span aria-hidden="true">✓</span>{{ success }}
    </p>
  </div>
</template>

<script setup>
import { computed } from 'vue'
const props = defineProps({
  forId: { type: String, required: true }, label: { type: String, required: true }, description: { type: String, default: '' },
  error: { type: String, default: '' }, success: { type: String, default: '' }, counter: { type: String, default: '' }, required: Boolean, disabled: Boolean,
})
const descriptionId = computed(() => `${props.forId}-description`)
const errorId = computed(() => `${props.forId}-error`)
const successId = computed(() => `${props.forId}-success`)
const describedby = computed(() => [props.description && descriptionId.value, props.error && errorId.value, props.success && successId.value].filter(Boolean).join(' ') || undefined)
</script>

<style scoped>
.vs-field { display: grid; gap: var(--space-2); }
.vs-field__label-row { display: flex; align-items: baseline; justify-content: space-between; gap: var(--space-3); }
.vs-field__label { color: var(--text-primary); font-size: var(--type-body-size); line-height: var(--type-body-line); font-weight: 700; }
.vs-field__required, .vs-field__message.is-error { color: var(--status-danger-text); }
.vs-field__description, .vs-field__counter { color: var(--text-muted); font-size: var(--type-caption-size); line-height: var(--type-caption-line); }
.vs-field__message { display: flex; gap: var(--space-2); align-items: center; font-size: var(--type-caption-size); line-height: var(--type-caption-line); }
.vs-field__message span { display: inline-grid; place-items: center; width: 1rem; height: 1rem; border: 1px solid currentColor; border-radius: 50%; font-weight: 800; }
.vs-field__message.is-success { color: var(--status-success-text); }
.is-disabled { opacity: .68; }
</style>
