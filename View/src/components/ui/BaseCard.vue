<template>
  <component :is="interactive ? 'button' : 'section'" class="vs-card" :class="[`is-${variant}`, { 'is-interactive': interactive }]" :type="interactive ? 'button' : undefined">
    <slot />
  </component>
</template>
<script setup>
defineProps({ variant: { type: String, default: 'static' }, interactive: Boolean })
</script>
<style scoped>
.vs-card { width: 100%; padding: var(--space-5); border: 1px solid var(--border-subtle); border-radius: var(--radius-card); background: var(--surface-default); color: var(--text-primary); box-shadow: var(--shadow-xs); text-align: start; }
.is-kpi { padding: var(--space-4); }
.is-alert { border-color: var(--status-danger-border); background: var(--status-danger-bg); }
.is-panel { border-radius: var(--radius-panel); box-shadow: var(--shadow-sm); }
.is-interactive { cursor: pointer; transition: background-color var(--transition-fast), border-color var(--transition-fast); }
.is-interactive:hover { background: var(--surface-hover); border-color: var(--border-strong); }
</style>
