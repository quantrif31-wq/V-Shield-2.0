<template>
  <div class="vs-skeleton" :class="[`is-${variant}`, { 'is-animated': animated }]" :style="{ '--skeleton-lines': lines }" role="status" aria-label="Đang tải nội dung">
    <template v-if="variant === 'table'">
      <span v-for="line in lines" :key="line" class="vs-skeleton__row"></span>
    </template>
    <template v-else><span class="vs-skeleton__block"></span></template>
    <span class="sr-only">Đang tải...</span>
  </div>
</template>
<script setup>
defineProps({ variant: { type: String, default: 'card' }, lines: { type: Number, default: 5 }, animated: { type: Boolean, default: true } })
</script>
<style scoped>
.vs-skeleton { overflow: hidden; border-radius: var(--radius-card); }
.vs-skeleton__block, .vs-skeleton__row { display: block; background: linear-gradient(90deg, var(--surface-subtle), var(--surface-hover), var(--surface-subtle)); background-size: 200% 100%; }
.vs-skeleton__block { min-height: 160px; }
.vs-skeleton__row { height: var(--table-row-height); border-bottom: 1px solid var(--border-subtle); }
.is-animated span { animation: vs-shimmer 1.5s ease-in-out infinite; }
@keyframes vs-shimmer { to { background-position: -200% 0; } }
@media (prefers-reduced-motion: reduce) { .is-animated span { animation: none; } }
</style>
