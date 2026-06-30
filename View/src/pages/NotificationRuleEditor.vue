<template>
  <div class="bento-grid">
    <div class="bento-card section-card" style="grid-column: 1 / -1;">
      <div class="section-head">
        <div>
          <h2 class="section-title">Quy tắc thông báo</h2>
          <p class="section-desc">Cấu hình đúng kênh, vai trò nhận và mức độ cảnh báo cho từng loại sự kiện.</p>
        </div>
        <button class="btn btn-primary" @click="createFromDraft" :disabled="saving || !draft.eventType">
          {{ saving ? 'Đang lưu...' : 'Thêm quy tắc' }}
        </button>
      </div>

      <div class="draft-card">
        <div class="draft-grid">
          <label class="field-group">
            <span>Gợi ý theo vai trò</span>
            <select v-model="selectedRole" class="form-select" @change="loadSuggestions">
              <option value="Admin">Admin</option>
              <option value="QuanLy">Quản lý</option>
              <option value="BaoVe">Bảo vệ</option>
              <option value="LeTan">Lễ tân</option>
              <option value="NhanSu">Nhân sự</option>
            </select>
          </label>
          <label class="field-group">
            <span>Loại sự kiện</span>
            <select v-model="draft.eventType" class="form-select">
              <option value="">Chọn sự kiện</option>
              <option v-for="item in suggestions" :key="item.eventType" :value="item.eventType">
                {{ item.label }} ({{ item.eventType }})
              </option>
            </select>
          </label>
          <label class="field-group">
            <span>Ngưỡng severity</span>
            <select v-model="draft.severityMin" class="form-select">
              <option value="">Tất cả</option>
              <option value="Critical">Critical</option>
              <option value="High">High</option>
              <option value="Medium">Medium</option>
              <option value="Low">Low</option>
            </select>
          </label>
          <label class="field-group">
            <span>Vai trò nhận</span>
            <select v-model="draft.recipientRole" class="form-select">
              <option value="">Không cố định</option>
              <option value="Admin">Admin</option>
              <option value="QuanLy">Quản lý</option>
              <option value="BaoVe">Bảo vệ</option>
              <option value="LeTan">Lễ tân</option>
              <option value="NhanSu">Nhân sự</option>
            </select>
          </label>
        </div>
        <div class="draft-flags">
          <label class="channel-badge" :class="{ active: draft.notifyWeb }"><input v-model="draft.notifyWeb" type="checkbox" />Web</label>
          <label class="channel-badge" :class="{ active: draft.notifyMobile }"><input v-model="draft.notifyMobile" type="checkbox" />Mobile</label>
          <label class="channel-badge" :class="{ active: draft.isActive }"><input v-model="draft.isActive" type="checkbox" />Kích hoạt</label>
        </div>
      </div>

      <div v-if="loading" class="text-center" style="padding: 2rem;">Đang tải...</div>
      <div v-else-if="error" class="alert alert-error">{{ error }}</div>

      <div v-else class="rule-list">
        <div v-for="rule in rules" :key="rule.id" class="rule-row">
          <div class="rule-info">
            <span class="rule-event-type">{{ rule.eventType }}</span>
            <span class="rule-desc">
              {{ rule.severityMin ? `Từ ${rule.severityMin}` : 'Mọi mức độ' }}
              · {{ rule.recipientRole || 'Chưa khóa vai trò nhận' }}
            </span>
          </div>
          <div class="rule-channels">
            <label class="channel-badge" :class="{ active: rule.notifyWeb }">
              <input type="checkbox" v-model="rule.notifyWeb" @change="toggleRule(rule)" />
              Web
            </label>
            <label class="channel-badge" :class="{ active: rule.notifyMobile }">
              <input type="checkbox" v-model="rule.notifyMobile" @change="toggleRule(rule)" />
              Mobile
            </label>
          </div>
          <label class="toggle-switch">
            <input type="checkbox" v-model="rule.isActive" @change="toggleRule(rule)" />
            <span class="toggle-slider"></span>
          </label>
          <button class="btn btn-danger btn-sm" @click="removeRule(rule)" :disabled="saving">Xóa</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import {
  createNotificationRule,
  deleteNotificationRule,
  getNotificationRules,
  getRuleSuggestions,
  updateNotificationRule,
} from '../services/notificationApi'

const rules = ref([])
const suggestions = ref([])
const selectedRole = ref('Admin')
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const draft = reactive({
  eventType: '',
  severityMin: '',
  recipientRole: 'Admin',
  notifyWeb: true,
  notifyMobile: true,
  isActive: true,
})

function mapRule(item) {
  return {
    id: item.id,
    eventType: item.eventType,
    severityMin: item.severityMin || '',
    recipientUserId: item.recipientUserId ?? null,
    recipientRole: item.recipientRole || '',
    notifyWeb: !!item.notifyWeb,
    notifyMobile: !!item.notifyMobile,
    isActive: !!item.isActive,
  }
}

async function loadRules() {
  try {
    loading.value = true
    error.value = ''
    const res = await getNotificationRules()
    rules.value = (res.data?.data || []).map(mapRule)
  } catch {
    error.value = 'Không thể tải quy tắc thông báo.'
  } finally {
    loading.value = false
  }
}

async function loadSuggestions() {
  try {
    const res = await getRuleSuggestions(selectedRole.value)
    suggestions.value = (res.data?.data || []).map((item) => ({
      eventType: item.eventType,
      label: item.label,
    }))
    if (!draft.eventType && suggestions.value.length) {
      draft.eventType = suggestions.value[0].eventType
    }
  } catch {
    suggestions.value = []
  }
}

async function toggleRule(rule) {
  try {
    saving.value = true
    await updateNotificationRule(rule.id, {
      eventType: rule.eventType,
      severityMin: rule.severityMin || null,
      recipientUserId: rule.recipientUserId,
      recipientRole: rule.recipientRole || null,
      notifyWeb: rule.notifyWeb,
      notifyMobile: rule.notifyMobile,
      isActive: rule.isActive,
    })
  } catch {
    await loadRules()
  } finally {
    saving.value = false
  }
}

async function createFromDraft() {
  try {
    saving.value = true
    await createNotificationRule({
      eventType: draft.eventType,
      severityMin: draft.severityMin || null,
      recipientRole: draft.recipientRole || null,
      recipientUserId: null,
      notifyWeb: draft.notifyWeb,
      notifyMobile: draft.notifyMobile,
      isActive: draft.isActive,
    })
    await loadRules()
  } catch {
    error.value = 'Không thể tạo quy tắc mới.'
  } finally {
    saving.value = false
  }
}

async function removeRule(rule) {
  const confirmed = window.confirm(`Xóa quy tắc ${rule.eventType}?`)
  if (!confirmed) return

  try {
    saving.value = true
    await deleteNotificationRule(rule.id)
    await loadRules()
  } catch {
    error.value = 'Không thể xóa quy tắc.'
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  await Promise.all([loadRules(), loadSuggestions()])
})
</script>

<style scoped>
.section-head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 1rem;
  margin-bottom: 1rem;
}
.section-desc {
  color: var(--text-muted);
  margin-bottom: 0;
}
.draft-card {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding: 1rem;
  border-radius: 12px;
  border: 1px solid var(--border-color);
  background: var(--bg-secondary);
  margin-bottom: 1.5rem;
}
.draft-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 0.75rem;
}
.field-group {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  font-size: 0.85rem;
}
.draft-flags {
  display: flex;
  gap: 0.75rem;
  flex-wrap: wrap;
}
.rule-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}
.rule-row {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto auto auto;
  align-items: center;
  padding: 0.75rem 1rem;
  background: var(--bg-secondary);
  border-radius: 8px;
  gap: 1rem;
}
.rule-info {
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
}
.toggle-switch input { opacity: 0; width: 0; height: 0; }
.toggle-slider {
  position: absolute;
  inset: 0;
  cursor: pointer;
  background: var(--bg-tertiary);
  border-radius: 22px;
  transition: 0.3s;
}
.toggle-slider::before {
  content: '';
  position: absolute;
  height: 16px;
  width: 16px;
  left: 3px;
  bottom: 3px;
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
