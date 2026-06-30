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
defineProps({ open: Boolean, title: { type: String, required: true }, eyebrow: { type: String, default: 'Thao tác có kiểm soát' } })
defineEmits(['close'])
</script>

<style scoped>
.action-drawer-overlay { position: fixed; inset: 0; z-index: 390; display: flex; justify-content: flex-end; background: rgba(15,23,42,.48); }
.action-drawer { width: min(520px,96vw); height: 100%; display: grid; grid-template-rows: auto 1fr auto; background: #fff; box-shadow: -18px 0 40px rgba(15,23,42,.18); }
header { display:flex; justify-content:space-between; gap:16px; align-items:center; padding:18px 20px; border-bottom:1px solid #e2e8f0; }
header span { color:#64748b; font-size:12px; text-transform:uppercase; font-weight:800; }
header h2 { margin:3px 0 0; font-size:20px; color:#172033; }
header button { width:38px; height:38px; border:1px solid #cbd5e1; background:#fff; font-size:24px; cursor:pointer; }
main { overflow:auto; padding:20px; }
footer { display:flex; justify-content:flex-end; gap:10px; padding:14px 20px; border-top:1px solid #e2e8f0; background:#f8fafc; }
</style>
