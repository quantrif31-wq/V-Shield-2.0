<template>
  <div class="vs-select">
    <select v-bind="$attrs" :id="id" :value="modelValue" :disabled="disabled || loading" :aria-invalid="invalid || undefined" :aria-describedby="describedby" @change="$emit('update:modelValue', $event.target.value)">
      <option v-if="placeholder" value="" disabled>{{ loading ? 'Đang tải...' : placeholder }}</option>
      <slot />
    </select><span aria-hidden="true">⌄</span>
  </div>
</template>
<script setup>
defineOptions({ inheritAttrs: false })
defineProps({ id: { type: String, required: true }, modelValue: { type: [String, Number], default: '' }, placeholder: { type: String, default: '' }, describedby: { type: String, default: undefined }, invalid: Boolean, disabled: Boolean, loading: Boolean })
defineEmits(['update:modelValue'])
</script>
<style scoped>
.vs-select { position: relative; }.vs-select select { width: 100%; min-height: var(--control-height-md); appearance: none; padding: 0 40px 0 var(--space-3); border: 1px solid var(--border-default); border-radius: var(--radius-control); background: var(--surface-default); color: var(--text-primary); }.vs-select select:focus { border-color: var(--border-focus); box-shadow: 0 0 0 3px color-mix(in srgb, var(--border-focus) 24%, transparent); outline: 0; }.vs-select span { position: absolute; right: var(--space-3); top: 50%; transform: translateY(-50%); pointer-events: none; color: var(--text-muted); }
</style>
