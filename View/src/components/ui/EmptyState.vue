<template>
  <div class="vs-empty" :class="`is-${kind}`" :role="kind === 'error' ? 'alert' : undefined">
    <div v-if="$slots.icon" class="vs-empty__icon" aria-hidden="true"><slot name="icon" /></div>
    <h3>{{ title }}</h3><p>{{ description }}</p>
    <div v-if="$slots.actions" class="vs-empty__actions"><slot name="actions" /></div>
  </div>
</template>
<script setup>
defineProps({ title: { type: String, required: true }, description: { type: String, required: true }, kind: { type: String, default: 'empty' } })
</script>
<style scoped>
.vs-empty { min-height: 220px; display: grid; place-items: center; align-content: center; gap: var(--space-2); padding: var(--space-8); border: 1px dashed var(--border-default); border-radius: var(--radius-card); background: var(--surface-subtle); text-align: center; }
.vs-empty h3 { font-size: var(--type-h3-size); line-height: var(--type-h3-line); }
.vs-empty p { max-width: 52ch; color: var(--text-secondary); font-size: var(--type-body-size); line-height: var(--type-body-line); }
.vs-empty__icon { color: var(--text-muted); }
.vs-empty__actions { display: flex; gap: var(--space-3); margin-top: var(--space-3); }
.is-error { border-color: var(--status-danger-border); background: var(--status-danger-bg); }
.is-permission { border-style: solid; }
</style>
