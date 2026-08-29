<template>
  <section class="enterprise-table-shell">
    <header v-if="$slots.toolbar" class="enterprise-table-toolbar"><slot name="toolbar" /></header>
    <div v-if="loading" class="enterprise-table-state">Đang tải dữ liệu...</div>
    <div v-else-if="error" class="enterprise-table-state enterprise-table-state--error">{{ error }}</div>
    <div v-else-if="!rows.length" class="enterprise-table-state">
      <strong>{{ emptyTitle }}</strong><span>{{ emptyMessage }}</span><slot name="empty" />
    </div>
    <div v-else class="enterprise-table-scroll">
      <table :class="[`density-${density}`, { 'sticky-header': stickyHeader }]">
        <thead><tr><th v-for="column in columns" :key="column.key" :style="column.width ? { width: column.width } : undefined">{{ column.label }}</th><th v-if="$slots.rowActions" class="actions-column">Thao tác</th></tr></thead>
        <tbody>
          <tr v-for="row in rows" :key="row[rowKey]">
            <td v-for="column in columns" :key="column.key"><slot :name="`cell:${column.key}`" :row="row" :value="row[column.key]">{{ row[column.key] ?? '---' }}</slot></td>
            <td v-if="$slots.rowActions" class="actions-column"><slot name="rowActions" :row="row" /></td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>

<script setup>
defineProps({
  columns: { type: Array, default: () => [] },
  rows: { type: Array, default: () => [] },
  loading: { type: Boolean, default: false },
  error: { type: String, default: '' },
  emptyTitle: { type: String, default: 'Chưa có dữ liệu' },
  emptyMessage: { type: String, default: 'Dữ liệu sẽ xuất hiện tại đây khi phát sinh.' },
  rowKey: { type: String, default: 'id' },
  density: { type: String, default: 'comfortable' },
  stickyHeader: { type: Boolean, default: true },
})
</script>

<style scoped>
.enterprise-table-shell { overflow: hidden; border: 1px solid var(--border-subtle); background: var(--surface-default); box-shadow: var(--shadow-sm); }
.enterprise-table-toolbar { padding: 12px 14px; border-bottom: 1px solid var(--border-subtle); background: var(--surface-subtle); }
.enterprise-table-scroll { overflow: auto; max-width: 100%; }
table { width: 100%; border-collapse: collapse; background: var(--surface-default); color: var(--text-primary); }
th,td { padding: 12px 14px; border-bottom: 1px solid var(--border-subtle); text-align: left; vertical-align: middle; }
th { font-size: 12px; font-weight: 800; color: var(--text-muted); background: var(--surface-subtle); text-transform: uppercase; }
.sticky-header th { position: sticky; top: 0; z-index: 1; }
tbody tr:hover { background: var(--surface-hover); }
.density-compact th,.density-compact td { padding: 8px 10px; }
.actions-column { width: 1%; white-space: nowrap; text-align: right; }
.enterprise-table-state { min-height: 150px; display: grid; place-content: center; gap: 5px; text-align: center; color: var(--text-muted); padding: 24px; }
.enterprise-table-state strong { color: var(--text-primary); }
.enterprise-table-state--error { color: var(--status-danger-text); background: var(--status-danger-bg); }
</style>
