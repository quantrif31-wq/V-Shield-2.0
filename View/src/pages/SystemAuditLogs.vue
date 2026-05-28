<template>
  <div class="page-container animate-in">
    <h1 class="page-title">Nhật ký hệ thống</h1>

    <div class="toolbar-shell filter-grid">
      <input v-model="filters.query" type="text" placeholder="Tìm người dùng, hành động, bảng dữ liệu..." />
      <select v-model="filters.actionType">
        <option value="">Tất cả hành động</option>
        <option value="REQUEST">REQUEST</option>
        <option value="CREATE">CREATE</option>
        <option value="UPDATE">UPDATE</option>
        <option value="DELETE">DELETE</option>
      </select>
      <select v-model="filters.isSuccess">
        <option value="">Tất cả kết quả</option>
        <option value="true">Thành công</option>
        <option value="false">Thất bại</option>
      </select>
      <button class="btn btn-secondary" @click="resetFilters">Đặt lại</button>
    </div>

    <div v-if="loading" class="empty-card">Đang tải nhật ký...</div>
    <div v-else-if="errorText" class="empty-card error-text">{{ errorText }}</div>
    <div v-else class="table-container">
      <table class="data-table">
        <thead>
          <tr>
            <th>Thời gian</th>
            <th>Người dùng</th>
            <th>Hành động</th>
            <th>Đối tượng</th>
            <th>Kết quả</th>
            <th>Lý do lỗi</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="row in items"
            :key="row.id"
            class="audit-row"
            :class="{ selected: selectedRow?.id === row.id }"
            @click="openDetails(row)"
          >
            <td>{{ fmt(row.timestampUtc) }}</td>
            <td>{{ row.username || row.userId || '-' }}</td>
            <td>{{ toActionLabel(row) }}</td>
            <td>{{ toTargetLabel(row) }}</td>
            <td :style="{ color: row.isSuccess ? '#14804a' : '#c0392b' }">
              {{ row.isSuccess ? 'Thành công' : 'Thất bại' }}
            </td>
            <td>{{ row.failureReason || '-' }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <transition name="slide-right">
      <aside v-if="selectedRow" class="audit-drawer">
        <div class="drawer-head">
          <div>
            <h3>Chi tiết nhật ký</h3>
            <p>{{ fmt(selectedRow.timestampUtc) }}</p>
          </div>
          <button class="drawer-close" type="button" @click="selectedRow = null">×</button>
        </div>

        <div class="drawer-body">
          <div class="detail-grid">
            <div class="detail-item">
              <span class="detail-label">Người thực hiện</span>
              <strong>{{ selectedRow.username || selectedRow.userId || '-' }}</strong>
            </div>
            <div class="detail-item">
              <span class="detail-label">Hành động</span>
              <strong>{{ toActionLabel(selectedRow) }}</strong>
            </div>
            <div class="detail-item">
              <span class="detail-label">Đối tượng</span>
              <strong>{{ toTargetLabel(selectedRow) }}</strong>
            </div>
            <div class="detail-item">
              <span class="detail-label">Kết quả</span>
              <strong :style="{ color: selectedRow.isSuccess ? '#14804a' : '#c0392b' }">
                {{ selectedRow.isSuccess ? 'Thành công' : 'Thất bại' }}
              </strong>
            </div>
            <div class="detail-item">
              <span class="detail-label">Thiết bị</span>
              <strong>{{ detailMeta.device || '-' }}</strong>
            </div>
            <div class="detail-item">
              <span class="detail-label">Vị trí/IP</span>
              <strong>{{ detailMeta.location || detailMeta.ip || '-' }}</strong>
            </div>
          </div>

          <div v-if="selectedRow.failureReason" class="detail-block">
            <div class="block-title">Lý do lỗi</div>
            <div class="code-shell">{{ selectedRow.failureReason }}</div>
          </div>

          <div class="detail-block">
            <div class="block-title">Giá trị trước thay đổi</div>
            <pre class="code-shell">{{ formatJsonBlock(selectedRow.oldValuesJson) }}</pre>
          </div>

          <div class="detail-block">
            <div class="block-title">Giá trị sau thay đổi</div>
            <pre class="code-shell">{{ formatJsonBlock(selectedRow.newValuesJson) }}</pre>
          </div>
        </div>
      </aside>
    </transition>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { getSystemAuditLogs } from '../services/accessLogApi'

const loading = ref(false)
const items = ref([])
const selectedRow = ref(null)
const errorText = ref('')
const filters = reactive({ query: '', actionType: '', isSuccess: '' })

function fmt(v) {
  return v ? new Date(v).toLocaleString('vi-VN') : '-'
}

function parseJson(raw) {
  if (!raw) return null
  try {
    return typeof raw === 'string' ? JSON.parse(raw) : raw
  } catch {
    return null
  }
}

const detailMeta = computed(() => {
  const current = selectedRow.value
  if (!current) return {}
  const meta = parseJson(current.newValuesJson)
  if (!meta || current.actionType !== 'REQUEST') return {}

  const location = [meta.city, meta.country].filter(Boolean).join(', ')
  return {
    device: meta.device || null,
    ip: meta.realIp || meta.forwardedFor || meta.ip || null,
    location,
  }
})

function toActionLabel(row) {
  const path = (row.path || '').toLowerCase()
  const method = (row.httpMethod || '').toUpperCase()
  if (row.actionType === 'REQUEST') {
    if (path.includes('/api/auth/login')) return 'Đăng nhập'
    if (path.includes('/api/auth/logout')) return 'Đăng xuất'
    if (path.includes('/status') && method === 'PATCH') return 'Đổi trạng thái'
    return `Yêu cầu ${method || 'API'}`
  }
  if (row.actionType === 'CREATE') return 'Tạo mới dữ liệu'
  if (row.actionType === 'UPDATE') return 'Cập nhật dữ liệu'
  if (row.actionType === 'DELETE') return 'Xóa dữ liệu'
  return row.actionType || '-'
}

function toTargetLabel(row) {
  if (row.entityName) return row.entityName
  const path = row.path || ''
  const knownMap = [
    ['/api/guest-profiles/visitor-directory', 'Hồ sơ khách'],
    ['/api/pre-registrations', 'Đăng ký trước'],
    ['/api/users', 'Tài khoản người dùng'],
    ['/api/access-permission', 'Phân quyền truy cập'],
    ['/api/auth/login', 'Phiên đăng nhập'],
  ]
  const match = knownMap.find(([prefix]) => path.toLowerCase().includes(prefix))
  return match ? match[1] : path || '-'
}

function formatJsonBlock(raw) {
  const json = parseJson(raw)
  if (!json) return '-'
  try {
    return JSON.stringify(json, null, 2)
  } catch {
    return String(raw)
  }
}

function openDetails(row) {
  selectedRow.value = row
}

async function fetchData() {
  loading.value = true
  errorText.value = ''
  try {
    const params = {
      query: filters.query || undefined,
      actionType: filters.actionType || undefined,
      isSuccess: filters.isSuccess === '' ? undefined : filters.isSuccess === 'true',
      page: 1,
      pageSize: 100,
    }
    const { data } = await getSystemAuditLogs(params)
    items.value = data?.items || []
    if (data?.warning) errorText.value = data.warning

    if (selectedRow.value) {
      selectedRow.value = items.value.find((x) => x.id === selectedRow.value.id) || null
    }
  } catch {
    items.value = []
    selectedRow.value = null
    errorText.value = 'Không tải được nhật ký hệ thống. Vui lòng kiểm tra API và thử lại.'
  } finally {
    loading.value = false
  }
}

function resetFilters() {
  filters.query = ''
  filters.actionType = ''
  filters.isSuccess = ''
  fetchData()
}

let t = null
watch(
  () => [filters.query, filters.actionType, filters.isSuccess],
  () => {
    clearTimeout(t)
    t = setTimeout(fetchData, 220)
  },
)

onMounted(fetchData)
</script>

<style scoped>
.filter-grid {
  display: grid;
  grid-template-columns: 1fr 210px 190px auto;
  gap: 10px;
}

.error-text {
  color: #b42318;
}

.audit-row {
  cursor: pointer;
}

.audit-row.selected {
  background: rgba(18, 109, 130, 0.08);
}

.audit-drawer {
  position: fixed;
  top: 0;
  right: 0;
  width: min(520px, 92vw);
  height: 100vh;
  background: #f7fafc;
  border-left: 1px solid #d9e2ec;
  box-shadow: -16px 0 32px rgba(15, 23, 42, 0.16);
  z-index: 90;
  display: flex;
  flex-direction: column;
}

.drawer-head {
  display: flex;
  justify-content: space-between;
  align-items: start;
  padding: 16px 18px;
  border-bottom: 1px solid #d9e2ec;
}

.drawer-head h3 {
  margin: 0;
  font-size: 22px;
}

.drawer-head p {
  margin: 6px 0 0;
  color: #556987;
}

.drawer-close {
  border: none;
  background: transparent;
  font-size: 28px;
  color: #64748b;
  cursor: pointer;
}

.drawer-body {
  overflow: auto;
  padding: 14px 18px 24px;
}

.detail-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
  margin-bottom: 14px;
}

.detail-item {
  background: #fff;
  border: 1px solid #dde6ee;
  border-radius: 12px;
  padding: 10px 12px;
}

.detail-label {
  display: block;
  color: #6b7d90;
  font-size: 12px;
  margin-bottom: 6px;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.detail-block {
  margin-top: 10px;
}

.block-title {
  font-weight: 700;
  margin-bottom: 6px;
}

.code-shell {
  background: #0f172a;
  color: #e2e8f0;
  border-radius: 10px;
  padding: 10px 12px;
  overflow: auto;
  max-height: 240px;
  white-space: pre-wrap;
  word-break: break-word;
  font-size: 12px;
}

.slide-right-enter-active,
.slide-right-leave-active {
  transition: transform 0.2s ease, opacity 0.2s ease;
}

.slide-right-enter-from,
.slide-right-leave-to {
  transform: translateX(20px);
  opacity: 0;
}

@media (max-width: 1200px) {
  .filter-grid {
    grid-template-columns: 1fr 1fr;
  }
}
</style>
