<template>
  <div class="guide-hero">
    <div class="guide-hero-bg" aria-hidden="true">
      <div class="hero-orb a"></div>
      <div class="hero-orb b"></div>
    </div>
    <div class="guide-hero-content">
      <span class="guide-kicker">📖 Hướng dẫn sử dụng</span>
      <h1 class="guide-title">V-Shield Security Platform</h1>
      <p class="guide-subtitle">
        Tài liệu hướng dẫn bằng tiếng Việt — giải thích đơn giản, dễ hiểu, có hình ảnh minh họa.
        Chọn vai trò của bạn để xem hướng dẫn phù hợp.
      </p>
      <div class="guide-role-chips">
        <button
          v-for="r in roles"
          :key="r.id"
          class="role-chip"
          :class="{ active: activeRole === r.id }"
          :style="{ '--chip-color': chipColor(r.id) }"
          @click="$emit('select-role', r.id)"
        >
          <span class="role-dot" :style="{ background: chipColor(r.id) }"></span>
          {{ r.label }}
        </button>
      </div>
      <div class="guide-meta">
        <span class="guide-meta-chip">Phiên bản 2.0</span>
        <span class="guide-meta-chip">Cập nhật 06/2026</span>
        <span class="guide-meta-chip">{{ totalPages }} trang hướng dẫn</span>
      </div>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  activeRole: { type: String, default: 'all' },
  totalPages: { type: Number, default: 0 }
})

defineEmits(['select-role'])

const roles = [
  { id: 'all', label: 'Tất cả vai trò' },
  { id: 'Admin', label: 'Admin' },
  { id: 'Bảo vệ', label: 'Bảo vệ' },
  { id: 'Quản lý', label: 'Quản lý' },
  { id: 'Lễ tân', label: 'Lễ tân' },
]

const chipColor = (id) => ({
  all: '#6b7280',
  Admin: '#3b82f6',
  'Bảo vệ': '#10b981',
  'Quản lý': '#8b5cf6',
  'Lễ tân': '#f59e0b',
}[id] || '#6b7280')
</script>

<style scoped>
.guide-hero {
  position: relative;
  padding: 40px 32px 32px;
  overflow: hidden;
  background: linear-gradient(180deg, rgba(16,32,51,0.97), rgba(24,49,77,0.94));
  color: #eefbfc;
  border-radius: 0 0 32px 32px;
}
.guide-hero-bg {
  position: absolute;
  inset: 0;
  pointer-events: none;
}
.hero-orb {
  position: absolute;
  border-radius: 999px;
  filter: blur(80px);
  opacity: 0.3;
}
.hero-orb.a { width: 400px; height: 400px; top: -100px; right: -60px; background: rgba(84,196,211,0.4); }
.hero-orb.b { width: 300px; height: 300px; bottom: -80px; left: 20%; background: rgba(216,155,55,0.2); }
.guide-hero-content {
  position: relative;
  z-index: 1;
  max-width: 900px;
  margin: 0 auto;
}
.guide-kicker {
  display: inline-flex;
  padding: 8px 16px;
  border-radius: 999px;
  background: rgba(84,196,211,0.12);
  color: #b8f7ff;
  font-size: 0.85rem;
  font-weight: 700;
  letter-spacing: 0.08em;
}
.guide-title {
  margin-top: 18px;
  font-family: var(--font-heading);
  font-size: clamp(2rem, 4vw, 3.2rem);
  font-weight: 700;
  line-height: 1.02;
  letter-spacing: -0.04em;
}
.guide-subtitle {
  margin-top: 12px;
  font-size: 1rem;
  line-height: 1.6;
  color: rgba(222,241,246,0.82);
  max-width: 64ch;
}
.guide-role-chips {
  margin-top: 20px;
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
.role-chip {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 10px 18px;
  border-radius: 999px;
  border: 1px solid rgba(255,255,255,0.12);
  background: rgba(255,255,255,0.06);
  color: rgba(222,241,246,0.78);
  font-size: 0.88rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}
.role-chip:hover {
  background: rgba(255,255,255,0.1);
  color: #fff;
}
.role-chip.active {
  background: rgba(255,255,255,0.14);
  color: #fff;
  border-color: var(--chip-color, #6b7280);
  box-shadow: inset 0 0 0 1px var(--chip-color, #6b7280);
}
.role-dot { width: 8px; height: 8px; border-radius: 50%; flex-shrink: 0; }
.guide-meta { margin-top: 18px; display: flex; flex-wrap: wrap; gap: 10px; }
.guide-meta-chip {
  padding: 6px 14px;
  border-radius: 999px;
  background: rgba(255,255,255,0.08);
  border: 1px solid rgba(255,255,255,0.1);
  color: rgba(222,241,246,0.78);
  font-size: 0.8rem;
  font-weight: 600;
}

@media (max-width: 768px) {
  .guide-hero { padding: 28px 18px 24px; }
  .guide-role-chips { gap: 6px; }
  .role-chip { padding: 8px 14px; font-size: 0.82rem; }
}
</style>
