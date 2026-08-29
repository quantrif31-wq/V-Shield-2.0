<template>
  <div v-if="error">
    <div class="route-error-box">
      <div class="route-error-title">Nội dung không thể hiển thị</div>
      <div class="route-error-desc">
        Trang này vừa gặp lỗi tạm thời. Vui lòng tải lại để tiếp tục.
      </div>
      <div class="route-error-actions">
        <button type="button" class="route-error-btn" @click="reload">Tải lại trang</button>
        <button type="button" class="route-error-btn secondary" @click="retry">Thử lại</button>
      </div>
    </div>
  </div>
  <slot v-else />
</template>

<script setup>
import { onErrorCaptured, ref, watch } from 'vue'
import * as vueRouter from 'vue-router'

const error = ref(null)

try {
  if (typeof vueRouter.useRoute === 'function') {
    const route = vueRouter.useRoute()
    if (route) {
      watch(
        () => route.fullPath,
        () => {
          error.value = null
        }
      )
    }
  }
} catch {
  // Outside router context
}

onErrorCaptured((err) => {
  // Chặn lỗi render trong nội dung route; không để white screen.
  error.value = err instanceof Error ? err : new Error(String(err || 'Unknown error'))
  console.error('Route content render error:', error.value)
  return false
})

function retry() {
  error.value = null
}

function reload() {
  window.location.reload()
}
</script>

<style scoped>
.route-error-box {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 10px;
  min-height: 40vh;
  padding: 24px;
  text-align: center;
  color: var(--text-primary);
}

.route-error-title {
  font-size: 1.4rem;
  font-weight: 800;
  color: var(--text-primary);
}

.route-error-desc {
  color: var(--text-secondary);
  font-size: 0.95rem;
}

.route-error-actions {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-top: 8px;
  flex-wrap: wrap;
  justify-content: center;
}

.route-error-btn {
  min-height: 40px;
  padding: 0 18px;
  border: none;
  border-radius: var(--radius-control, 10px);
  background: var(--interactive-primary);
  color: var(--text-on-interactive);
  font-weight: 700;
  cursor: pointer;
  transition: background var(--transition-fast, 0.15s ease), border-color var(--transition-fast, 0.15s ease);
}

.route-error-btn:hover {
  background: var(--interactive-primary-hover, var(--interactive-primary));
}

.route-error-btn.secondary {
  background: var(--surface-default);
  color: var(--text-primary);
  border: 1px solid var(--border-default);
}

.route-error-btn.secondary:hover {
  background: var(--surface-hover);
  border-color: var(--border-focus);
}
</style>
