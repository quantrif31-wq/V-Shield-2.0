<template>
  <div class="bento-grid">
    <div class="bento-card section-card" style="grid-column: 1 / -1;">
      <h2 class="section-title">Quy tắc thông báo</h2>
      <p class="section-desc">Cấu hình loại sự kiện nào sẽ gửi thông báo đến người dùng.</p>

      <div v-if="loading" class="text-center" style="padding: 2rem;">Đang tải...</div>

      <div v-else-if="error" class="alert alert-error">{{ error }}</div>

      <div v-else class="rule-list">
        <div v-for="rule in rules" :key="rule.notificationRuleId" class="rule-row">
          <div class="rule-info">
            <span class="rule-event-type">{{ rule.eventType }}</span>
            <span class="rule-desc">{{ rule.description }}</span>
          </div>
          <div class="rule-channels">
            <label class="channel-badge" :class="{ active: rule.sendWeb }">
              <input type="checkbox" v-model="rule.sendWeb" @change="toggleRule(rule)" />
              Web
            </label>
            <label class="channel-badge" :class="{ active: rule.sendMobile }">
              <input type="checkbox" v-model="rule.sendMobile" @change="toggleRule(rule)" />
              Mobile
            </label>
          </div>
          <label class="toggle-switch">
            <input type="checkbox" v-model="rule.enabled" @change="toggleRule(rule)" />
            <span class="toggle-slider"></span>
          </label>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { getNotificationRules, updateNotificationRule } from '../services/notificationApi'

const rules = ref([])
const loading = ref(true)
const error = ref('')

async function loadRules() {
  try {
    loading.value = true
    const res = await getNotificationRules()
    rules.value = res.data || []
  } catch (e) {
    error.value = 'Không thể tải quy tắc thông báo.'
  } finally {
    loading.value = false
  }
}

async function toggleRule(rule) {
  try {
    await updateNotificationRule(rule.notificationRuleId, {
      eventType: rule.eventType,
      enabled: rule.enabled,
      sendWeb: rule.sendWeb,
      sendMobile: rule.sendMobile,
      description: rule.description
    })
  } catch (e) {
    await loadRules()
  }
}

onMounted(loadRules)
</script>

<style scoped>
.section-desc {
  color: var(--text-muted);
  margin-bottom: 1.5rem;
}
.rule-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}
.rule-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.75rem 1rem;
  background: var(--bg-secondary);
  border-radius: 8px;
  gap: 1rem;
}
.rule-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}
.rule-event-type {
  font-weight: 600;
  font-size: 0.9rem;
  color: var(--text-primary);
}
.rule-desc {
  font-size: 0.8rem;
  color: var(--text-muted);
}
.rule-channels {
  display: flex;
  gap: 0.5rem;
}
.channel-badge {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  padding: 0.25rem 0.6rem;
  border-radius: 4px;
  font-size: 0.75rem;
  background: var(--bg-tertiary);
  color: var(--text-muted);
  cursor: pointer;
  transition: all 0.2s;
}
.channel-badge.active {
  background: var(--primary);
  color: #fff;
}
.channel-badge input { display: none; }
.toggle-switch {
  position: relative;
  display: inline-block;
  width: 40px;
  height: 22px;
  flex-shrink: 0;
}
.toggle-switch input { opacity: 0; width: 0; height: 0; }
.toggle-slider {
  position: absolute;
  cursor: pointer;
  top: 0; left: 0; right: 0; bottom: 0;
  background: var(--bg-tertiary);
  border-radius: 22px;
  transition: 0.3s;
}
.toggle-slider::before {
  content: '';
  position: absolute;
  height: 16px; width: 16px;
  left: 3px; bottom: 3px;
  background: white;
  border-radius: 50%;
  transition: 0.3s;
}
.toggle-switch input:checked + .toggle-slider {
  background: var(--primary);
}
.toggle-switch input:checked + .toggle-slider::before {
  transform: translateX(18px);
}
.alert-error {
  color: var(--danger, #e74c3c);
  padding: 1rem;
  background: var(--bg-secondary);
  border-radius: 8px;
}
.text-center { text-align: center; }
</style>
