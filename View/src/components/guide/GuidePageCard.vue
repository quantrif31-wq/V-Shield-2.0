<template>
  <div class="page-card" :class="{ expanded: isOpen }">
    <!-- Header - always visible -->
    <button class="card-header" @click="$emit('toggle')">
      <div class="card-header-left">
        <span class="card-icon">{{ page.icon }}</span>
        <div class="card-info">
          <strong class="card-title">{{ page.label }}</strong>
          <span class="card-desc">{{ page.mucDich.substring(0, 120) }}{{ page.mucDich.length > 120 ? '...' : '' }}</span>
        </div>
      </div>
      <div class="card-header-right">
        <div class="card-roles">
          <span v-for="r in page.roles" :key="r" class="mini-role" :style="{ background: roleColor(r) + '1a', color: roleColor(r) }">{{ r }}</span>
        </div>
        <svg class="chevron" :class="{ rotated: isOpen }" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
          <path d="M6 9l6 6 6-6"/>
        </svg>
      </div>
    </button>

    <!-- Expanded content -->
    <div v-if="isOpen" class="card-body">
      <!-- Mục đích -->
      <div class="section-block">
        <h4 class="section-title">📌 Trang này dùng để làm gì?</h4>
        <p class="section-text">{{ page.mucDich }}</p>
      </div>

      <!-- Steps -->
      <div v-if="page.steps && page.steps.length" class="section-block">
        <h4 class="section-title">📋 Các bước thực hiện</h4>
        <GuideStepList :steps="page.steps" :color="groupColor" />
      </div>

      <!-- Thành phần -->
      <div v-if="page.thanhPhan && page.thanhPhan.length" class="section-block">
        <h4 class="section-title">🔍 Giải thích các thành phần trên trang</h4>
        <div class="component-table-wrapper">
          <table class="component-table">
            <thead>
              <tr>
                <th>Thành phần</th>
                <th>Ý nghĩa</th>
                <th>Ghi chú</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(tp, idx) in page.thanhPhan" :key="idx">
                <td class="tp-name">{{ tp.ten }}</td>
                <td class="tp-meaning">{{ tp.yNghia }}</td>
                <td class="tp-note">{{ tp.ghiChu || '—' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import GuideStepList from './GuideStepList.vue'

const props = defineProps({
  page: { type: Object, required: true },
  isOpen: { type: Boolean, default: false },
  groupColor: { type: String, default: '#3b82f6' }
})

defineEmits(['toggle'])

const roleColor = (r) => ({
  Admin: '#3b82f6',
  'Bảo vệ': '#10b981',
  'Quản lý': '#8b5cf6',
  'Nhân viên': '#f59e0b',
}[r] || '#6b7280')
</script>

<style scoped>
.page-card {
  border-radius: 16px;
  border: 1px solid var(--border-color);
  background: var(--bg-card);
  overflow: hidden;
  transition: box-shadow 0.2s ease;
}
.page-card:hover { box-shadow: var(--shadow-md); }
.page-card.expanded {
  box-shadow: var(--shadow-md);
  border-color: var(--border-color-hover);
}

.card-header {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 16px 20px;
  border: none;
  background: transparent;
  color: var(--text-primary);
  cursor: pointer;
  text-align: left;
  transition: background 0.15s;
}
.card-header:hover { background: rgba(15,124,130,0.03); }

.card-header-left {
  display: flex;
  align-items: flex-start;
  gap: 14px;
  flex: 1;
  min-width: 0;
}
.card-icon {
  width: 42px;
  height: 42px;
  border-radius: 12px;
  background: rgba(15,124,130,0.08);
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.3rem;
  flex-shrink: 0;
}
.card-info { min-width: 0; }
.card-title {
  display: block;
  font-size: 0.96rem;
  color: var(--text-primary);
  margin-bottom: 4px;
}
.card-desc {
  display: block;
  font-size: 0.82rem;
  color: var(--text-secondary);
  line-height: 1.4;
}

.card-header-right {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-shrink: 0;
}
.card-roles {
  display: flex;
  gap: 4px;
  flex-wrap: wrap;
}
.mini-role {
  font-size: 0.68rem;
  font-weight: 700;
  padding: 3px 8px;
  border-radius: 999px;
  white-space: nowrap;
}
.chevron {
  width: 20px;
  height: 20px;
  color: var(--text-muted);
  transition: transform 0.25s ease;
  flex-shrink: 0;
}
.chevron.rotated { transform: rotate(180deg); }

/* Expanded body */
.card-body {
  padding: 0 20px 20px;
  border-top: 1px solid var(--border-color);
  animation: slideDown 0.25s ease;
}
.section-block {
  margin-top: 16px;
}
.section-title {
  font-size: 0.94rem;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 10px;
}
.section-text {
  font-size: 0.88rem;
  color: var(--text-secondary);
  line-height: 1.65;
}

.component-table-wrapper {
  overflow-x: auto;
  border-radius: 10px;
  border: 1px solid var(--border-color);
}
.component-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.84rem;
}
.component-table th {
  padding: 10px 12px;
  background: var(--bg-input);
  color: var(--text-muted);
  font-weight: 700;
  text-transform: uppercase;
  font-size: 0.72rem;
  letter-spacing: 0.05em;
  text-align: left;
  border-bottom: 1px solid var(--border-color);
}
.component-table td {
  padding: 10px 12px;
  border-bottom: 1px solid var(--border-color);
  vertical-align: top;
}
.component-table tr:last-child td { border-bottom: none; }
.tp-name { font-weight: 600; color: var(--text-primary); white-space: nowrap; }
.tp-meaning { color: var(--text-secondary); }
.tp-note { color: var(--text-muted); font-style: italic; }

@keyframes slideDown {
  from { opacity: 0; max-height: 0; }
  to { opacity: 1; max-height: 2000px; }
}

@media (max-width: 768px) {
  .card-header { flex-direction: column; align-items: flex-start; }
  .card-header-right { width: 100%; justify-content: space-between; }
  .component-table th, .component-table td { padding: 8px 10px; font-size: 0.8rem; }
}
</style>
