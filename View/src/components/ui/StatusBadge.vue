<template>
  <span class="vs-status" :class="[`is-${semanticVariant}`, `is-${size}`]" :aria-label="accessibleLabel">
    <span v-if="dot" class="vs-status__dot" aria-hidden="true"></span>
    <span v-if="$slots.icon" class="vs-status__icon" aria-hidden="true"><slot name="icon" /></span>
    <span><slot>{{ label }}</slot></span>
  </span>
</template>

<script setup>
import { computed } from 'vue'
const props = defineProps({ status: { type: String, default: 'neutral' }, label: { type: String, default: '' }, size: { type: String, default: 'medium' }, dot: Boolean, srLabel: { type: String, default: '' } })
const statusMap = { online: 'success', active: 'success', approved: 'success', offline: 'neutral', inactive: 'neutral', stale: 'warning', pending: 'warning', warning: 'warning', disconnected: 'danger', rejected: 'danger', critical: 'danger', info: 'info' }
const semanticVariant = computed(() => statusMap[props.status] || props.status || 'neutral')
const accessibleLabel = computed(() => props.srLabel || props.label || props.status)
</script>

<style scoped>
.vs-status { display: inline-flex; align-items: center; gap: 6px; min-height: 26px; padding: 3px 9px; border: 1px solid; border-radius: var(--radius-pill); font-size: var(--type-caption-size); line-height: var(--type-caption-line); font-weight: 700; white-space: nowrap; }
.vs-status__dot { width: 7px; height: 7px; border-radius: 50%; background: currentColor; }
.is-info { color: var(--status-info-text); background: var(--status-info-bg); border-color: var(--status-info-border); }
.is-success { color: var(--status-success-text); background: var(--status-success-bg); border-color: var(--status-success-border); }
.is-warning { color: var(--status-warning-text); background: var(--status-warning-bg); border-color: var(--status-warning-border); }
.is-danger { color: var(--status-danger-text); background: var(--status-danger-bg); border-color: var(--status-danger-border); }
.is-neutral { color: var(--status-neutral-text); background: var(--status-neutral-bg); border-color: var(--status-neutral-border); }
.is-small { min-height: 22px; padding: 2px 7px; }
</style>
