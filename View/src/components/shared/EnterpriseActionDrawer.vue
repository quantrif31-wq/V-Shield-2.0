<template>
  <Teleport to="body">
    <div v-if="open" class="action-drawer-overlay" @click.self="$emit('close')">
      <aside class="action-drawer" role="dialog" aria-modal="true" :aria-label="title">
        <header><div><span>{{ eyebrow }}</span><h2>{{ title }}</h2></div><button type="button" aria-label="Đóng" @click="$emit('close')">×</button></header>
        <main><slot /></main>
        <footer v-if="$slots.footer"><slot name="footer" /></footer>
      </aside>
    </div>
  </Teleport>
</template>

<script setup>
import { onMounted, onUnmounted, watch } from 'vue'

const props = defineProps({ open: Boolean, title: { type: String, required: true }, eyebrow: { type: String, default: 'Thao tác có kiểm soát' } })
const emit = defineEmits(['close'])

function handleKeyDown(e) {
  if (e.key === 'Escape' && props.open) {
    emit('close')
  }
}

onMounted(() => {
  window.addEventListener('keydown', handleKeyDown)
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleKeyDown)
})
</script>

<style scoped>
.action-drawer-overlay { position: fixed; inset: 0; z-index: 390; display: flex; justify-content: flex-end; background: var(--surface-overlay); }
.action-drawer { width: min(520px,96vw); height: 100%; display: grid; grid-template-rows: auto 1fr auto; background: var(--surface-default); color: var(--text-primary); box-shadow: var(--shadow-overlay); }
header { display:flex; justify-content:space-between; gap:16px; align-items:center; padding:18px 20px; border-bottom:1px solid var(--border-subtle); }
header span { color:var(--text-muted); font-size:12px; text-transform:uppercase; font-weight:800; }
header h2 { margin:3px 0 0; font-size:20px; color:var(--text-primary); }
header button { width:38px; height:38px; border:1px solid var(--border-subtle); background:var(--surface-subtle); color:var(--text-secondary); font-size:24px; border-radius:8px; cursor:pointer; }
header button:hover { background:var(--surface-hover); }
main { overflow:auto; padding:20px; }
footer { display:flex; justify-content:flex-end; gap:10px; padding:14px 20px; border-top:1px solid var(--border-subtle); background:var(--surface-subtle); }
</style>
