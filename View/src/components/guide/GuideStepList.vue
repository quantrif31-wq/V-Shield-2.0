<template>
  <div class="step-list">
    <div v-for="(step, idx) in steps" :key="idx" class="step-item">
      <div class="step-number" :style="{ background: color }">{{ idx + 1 }}</div>
      <div class="step-body">
        <h4 class="step-title">{{ step.title }}</h4>
        <p class="step-mota">{{ step.moTa }}</p>
        <div v-if="step.nhapGi" class="step-action">
          <span class="action-tag input-tag">📝 Nhập:</span>
          <span class="action-text">{{ step.nhapGi }}</span>
        </div>
        <div v-if="step.bamGi" class="step-action">
          <span class="action-tag click-tag">👆 Bấm:</span>
          <span class="action-text">{{ step.bamGi }}</span>
        </div>
        <div v-if="step.ketQua" class="step-result">
          <span class="result-label">✅ Kết quả:</span>
          <span class="result-text">{{ step.ketQua }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  steps: { type: Array, default: () => [] },
  color: { type: String, default: '#3b82f6' }
})
</script>

<style scoped>
.step-list {
  display: flex;
  flex-direction: column;
  gap: 14px;
}
.step-item {
  display: grid;
  grid-template-columns: 38px 1fr;
  gap: 14px;
}
.step-number {
  width: 38px;
  height: 38px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  font-weight: 700;
  font-size: 0.9rem;
  flex-shrink: 0;
}
.step-body { min-width: 0; }
.step-title {
  font-size: 0.96rem;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 4px;
}
.step-mota {
  font-size: 0.88rem;
  color: var(--text-secondary);
  line-height: 1.55;
  margin-bottom: 8px;
}
.step-action {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  margin-bottom: 4px;
  padding: 6px 10px;
  border-radius: 8px;
  background: rgba(24,49,77,0.03);
}
.action-tag {
  flex-shrink: 0;
  font-size: 0.78rem;
  font-weight: 700;
  padding: 2px 8px;
  border-radius: 6px;
}
.input-tag { background: rgba(16,185,129,0.1); color: #10b981; }
.click-tag { background: rgba(59,130,246,0.1); color: #3b82f6; }
.action-text { font-size: 0.86rem; color: var(--text-primary); }
.step-result {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  padding: 8px 12px;
  border-radius: 8px;
  background: rgba(16,185,129,0.05);
  border: 1px solid rgba(16,185,129,0.12);
}
.result-label {
  flex-shrink: 0;
  font-size: 0.82rem;
  font-weight: 700;
  color: #10b981;
}
.result-text {
  font-size: 0.86rem;
  color: var(--text-secondary);
  line-height: 1.45;
}

@media (max-width: 600px) {
  .step-item { grid-template-columns: 1fr; gap: 8px; }
  .step-number { width: 32px; height: 32px; font-size: 0.8rem; }
}
</style>
