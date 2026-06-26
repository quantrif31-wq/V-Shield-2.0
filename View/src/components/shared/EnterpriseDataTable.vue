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
.enterprise-table-shell { overflow: hidden; border: 1px solid #dbe3ea; background: #fff; box-shadow: 0 5px 16px rgba(15,23,42,.05); }
.enterprise-table-toolbar { padding: 12px 14px; border-bottom: 1px solid #e2e8f0; background: #f8fafc; }
.enterprise-table-scroll { overflow: auto; max-width: 100%; }
table { width: 100%; border-collapse: collapse; background: #fff; color: #172033; }
th,td { padding: 12px 14px; border-bottom: 1px solid #e7edf2; text-align: left; vertical-align: middle; }
th { font-size: 12px; font-weight: 800; color: #475569; background: #eef3f7; text-transform: uppercase; }
.sticky-header th { position: sticky; top: 0; z-index: 1; }
tbody tr:hover { background: #f4f8fa; }
.density-compact th,.density-compact td { padding: 8px 10px; }
.actions-column { width: 1%; white-space: nowrap; text-align: right; }
.enterprise-table-state { min-height: 150px; display: grid; place-content: center; gap: 5px; text-align: center; color: #64748b; padding: 24px; }
.enterprise-table-state strong { color: #25364a; }
.enterprise-table-state--error { color: #b42318; background: #fff7f6; }
</style>
