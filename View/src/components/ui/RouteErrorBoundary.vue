<template>
  <div v-if="error">
    <div class="route-error-box">
      <div class="route-error-title">Nội dung không thể hiển thị</div>
      <div class="route-error-desc">
        Trang này vừa gặp lỗi tạm thời. Vui lòng tải lại để tiếp tục.
      </div>
      <button type="button" class="route-error-btn" @click="reload">Tải lại trang</button>
    </div>
  </div>
  <slot v-else />
</template>

<script setup>
import { onErrorCaptured, ref } from 'vue'

const error = ref(null)

onErrorCaptured((err) => {
  // Chặn lỗi render trong nội dung route; không để white screen.
  error.value = err instanceof Error ? err : new Error(String(err || 'Unknown error'))
  console.error('Route content render error:', error.value)
  return false
})

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
  color: var(--text-primary, #1e293b);
}

.route-error-title {
  font-size: 1.4rem;
  font-weight: 800;
}

.route-error-desc {
  color: var(--text-secondary, #64748b);
  font-size: 0.95rem;
}

.route-error-btn {
  margin-top: 8px;
  min-height: 40px;
  padding: 0 18px;
  border: none;
  border-radius: 10px;
  background: var(--accent-primary, #2563eb);
  color: #fff;
  font-weight: 700;
  cursor: pointer;
}
</style>
