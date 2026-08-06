<template>
  <div class="vs-input" :class="{ 'has-prefix': $slots.prefix, 'has-suffix': $slots.suffix, 'is-invalid': invalid }">
    <span v-if="$slots.prefix" class="vs-input__prefix" aria-hidden="true"><slot name="prefix" /></span>
    <input v-bind="$attrs" :id="id" :value="modelValue" :type="type" :disabled="disabled" :readonly="readonly" :aria-invalid="invalid || undefined" :aria-describedby="describedby" @input="handleInput" />
    <span v-if="$slots.suffix" class="vs-input__suffix"><slot name="suffix" /></span>
  </div>
</template>
<script setup>
defineOptions({ inheritAttrs: false })
defineProps({ id: { type: String, required: true }, modelValue: { type: [String, Number], default: '' }, type: { type: String, default: 'text' }, describedby: { type: String, default: undefined }, invalid: Boolean, disabled: Boolean, readonly: Boolean })
const emit = defineEmits(['update:modelValue', 'input'])
function handleInput(event) { emit('update:modelValue', event.target.value); emit('input', event) }
</script>
<style scoped>
.vs-input { position: relative; }
.vs-input input { width: 100%; min-height: var(--control-height-md); padding: 0 var(--space-3); border: 1px solid var(--border-default); border-radius: var(--radius-control); background: var(--surface-default); color: var(--text-primary); }
.vs-input input:hover:not(:disabled) { border-color: var(--border-strong); }
.vs-input input:focus { border-color: var(--border-focus); box-shadow: 0 0 0 3px color-mix(in srgb, var(--border-focus) 24%, transparent); outline: 0; }
.vs-input input:disabled { background: var(--surface-subtle); color: var(--text-disabled); cursor: not-allowed; }
.vs-input input[readonly] { background: var(--surface-subtle); }
.vs-input.is-invalid input { border-color: var(--border-danger); }
.has-prefix input { padding-left: 42px; }.has-suffix input { padding-right: 42px; }
.vs-input__prefix,.vs-input__suffix { position: absolute; top: 50%; z-index: 1; display: grid; place-items: center; transform: translateY(-50%); color: var(--text-muted); }
.vs-input__prefix { left: var(--space-3); }.vs-input__suffix { right: var(--space-2); }
</style>
