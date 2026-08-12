<template>
  <div class="page-container animate-in audit-page">
    <section class="panel topbar">
      <h1>Nhật ký hệ thống</h1>
      <p>Theo dõi toàn bộ thao tác trong ứng dụng, mở chi tiết để xem dữ liệu trước/sau và metadata.</p>
    </section>

    <section class="panel filters">
      <div class="field field-query">
        <label>Tìm kiếm tổng quát</label>
        <input v-model="filters.query" type="text" placeholder="Tên người dùng, hành động, bảng dữ liệu..." />
      </div>

      <div class="field">
        <label>Loại hành động</label>
        <select v-model="filters.actionType">
          <option value="">Tất cả hành động</option>
          <option value="REQUEST">REQUEST</option>
          <option value="CREATE">CREATE</option>
          <option value="UPDATE">UPDATE</option>
          <option value="DELETE">DELETE</option>
        </select>
      </div>

      <div class="field">
        <label>Kết quả</label>
        <select v-model="filters.isSuccess">
          <option value="">Tất cả kết quả</option>
          <option value="true">Thành công</option>
          <option value="false">Thất bại</option>
        </select>
      </div>

      <div class="actions">
        <button class="btn btn-subtle" @click="resetFilters">Đặt lại</button>
      </div>
    </section>

    <div v-if="loading" class="empty-card">Đang tải nhật ký...</div>
    <div v-else-if="errorText" class="empty-card error-text">{{ errorText }}</div>

    <section v-else class="panel table-panel">
      <div class="panel-head">
        <h2>Bảng sự kiện hệ thống</h2>
        <span>Tổng bản ghi: {{ items.length }}</span>
      </div>

      <div class="table-wrap">
        <table class="table">
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
            <tr v-if="items.length === 0">
              <td colspan="6" class="empty">Không có dữ liệu phù hợp với bộ lọc hiện tại.</td>
            </tr>

            <tr
              v-for="row in items"
              :key="row.id"
              class="audit-row"
              :class="{ selected: selectedRow?.id === row.id }"
              @click="openDetails(row)"
            >
              <td>{{ fmt(row.timestampUtc) }}</td>
              <td><div class="main">{{ row.username || row.userId || '-' }}</div></td>
              <td><div class="main">{{ toActionLabel(row) }}</div></td>
              <td><div class="sub">{{ toTargetLabel(row) }}</div></td>
              <td>
                <span class="status-pill" :class="row.isSuccess ? 'ok' : 'fail'">
                  {{ row.isSuccess ? 'Thành công' : 'Thất bại' }}
                </span>
              </td>
              <td>{{ row.failureReason || '-' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

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
              <strong :style="{ color: selectedRow.isSuccess ? 'var(--status-success-text)' : 'var(--status-danger-text)' }">
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

function looksLikeRequestMeta(meta) {
  if (!meta || typeof meta !== 'object') return false
  return !!(meta.device || meta.ip || meta.realIp || meta.forwardedFor || meta.country || meta.city)
}

function normalizeDetailMeta(meta) {
  if (!looksLikeRequestMeta(meta)) return {}
  const location = [meta.city, meta.country].filter(Boolean).join(', ')
  return {
    device: meta.device || null,
    ip: meta.realIp || meta.forwardedFor || meta.ip || null,
    location,
  }
}

function findClosestRequestMetaForRow(row) {
  if (!row) return null
  const currentTs = new Date(row.timestampUtc || 0).getTime()
  if (!Number.isFinite(currentTs) || currentTs <= 0) return null

  const candidates = items.value.filter((x) => {
    if (x.actionType !== 'REQUEST') return false
    if ((x.username || '') !== (row.username || '')) return false

    const ts = new Date(x.timestampUtc || 0).getTime()
    if (!Number.isFinite(ts) || ts <= 0) return false

    const diff = Math.abs(currentTs - ts)
    if (diff > 15000) return false

    const meta = parseJson(x.newValuesJson)
    return looksLikeRequestMeta(meta)
  })

  if (!candidates.length) return null
  candidates.sort((a, b) => {
    const da = Math.abs(currentTs - new Date(a.timestampUtc).getTime())
    const db = Math.abs(currentTs - new Date(b.timestampUtc).getTime())
    return da - db
  })

  return parseJson(candidates[0].newValuesJson)
}

const detailMeta = computed(() => {
  const current = selectedRow.value
  if (!current) return {}

  const ownMeta = parseJson(current.newValuesJson)
  if (current.actionType === 'REQUEST' && looksLikeRequestMeta(ownMeta)) {
    return normalizeDetailMeta(ownMeta)
  }

  if (looksLikeRequestMeta(ownMeta)) {
    return normalizeDetailMeta(ownMeta)
  }

  const fallbackMeta = findClosestRequestMetaForRow(current)
  return normalizeDetailMeta(fallbackMeta)
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
  if (looksLikeRequestMeta(json)) return '-'
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
.audit-page { padding-bottom: 18px; }
.panel { background: var(--surface-default); border: 1px solid var(--border-subtle); border-radius: 16px; padding: 16px; margin-top: 14px; }
.topbar h1 { margin: 0; font-size: 34px; line-height: 1.08; font-weight: 900; color: var(--text-primary); }
.topbar p { margin: 8px 0 0; color: var(--text-secondary); font-size: 15px; }

.filters { display: grid; grid-template-columns: 1.4fr 220px 200px auto; gap: 12px; align-items: end; }
.field label { display: block; margin-bottom: 6px; font-size: 13px; font-weight: 700; color: var(--text-secondary); }
.field input,
.field select { width: 100%; height: 42px; border: 1px solid var(--border-default); border-radius: 10px; padding: 0 12px; background: var(--surface-default); color: var(--text-primary); }
.actions { display: flex; align-items: end; }

.btn { height: 40px; border: none; border-radius: 10px; padding: 0 14px; font-weight: 700; cursor: pointer; transition: background-color .16s ease, box-shadow .16s ease, transform .16s ease; }
.btn-subtle { background: var(--surface-subtle); color: var(--text-primary); }
.btn-subtle:hover { background: var(--surface-hover); box-shadow: var(--shadow-xs); }

.panel-head { display: flex; justify-content: space-between; align-items: center; margin-bottom: 10px; }
.panel-head h2 { margin: 0; font-size: 20px; font-weight: 800; color: var(--text-primary); }
.panel-head span { color: var(--text-muted); font-weight: 600; }

.table-wrap { overflow-x: auto; overflow-y: visible; position: relative; }
.table { width: 100%; border-collapse: separate; border-spacing: 0; min-width: 980px; }
.table thead th { text-align: left; font-size: 12px; text-transform: uppercase; letter-spacing: .05em; color: var(--text-muted); padding: 12px 14px; background: var(--surface-subtle); border-top: 1px solid var(--border-subtle); border-bottom: 1px solid var(--border-subtle); }
.table thead th:first-child { border-left: 1px solid var(--border-subtle); border-top-left-radius: 10px; }
.table thead th:last-child { border-right: 1px solid var(--border-subtle); border-top-right-radius: 10px; }
.table tbody td { border-bottom: 1px solid var(--border-subtle); padding: 12px 14px; vertical-align: middle; color: var(--text-primary); }
.table tbody tr { background: var(--surface-default); transition: background-color .16s ease; }
.table tbody tr:hover { background: var(--surface-hover); }
.audit-row { cursor: pointer; }
.audit-row.selected { background: var(--surface-selected) !important; }
.main { font-weight: 700; color: var(--text-primary); }
.sub { color: var(--text-secondary); }
.empty { text-align: center; color: var(--text-muted); padding: 20px 0; }

.status-pill { display: inline-flex; align-items: center; border-radius: 999px; padding: 4px 10px; font-size: 12px; font-weight: 700; }
.status-pill.ok { background: var(--status-success-bg); color: var(--status-success-text); border: 1px solid var(--status-success-border); }
.status-pill.fail { background: var(--status-danger-bg); color: var(--status-danger-text); border: 1px solid var(--status-danger-border); }

.empty-card { border-radius: 12px; padding: 12px; margin-top: 14px; background: var(--surface-subtle); border: 1px solid var(--border-subtle); color: var(--text-secondary); }
.error-text { color: var(--status-danger-text); }

.audit-drawer {
  position: fixed;
  top: 0;
  right: 0;
  width: min(520px, 92vw);
  height: 100vh;
  background: var(--surface-raised);
  border-left: 1px solid var(--border-subtle);
  box-shadow: -16px 0 32px rgba(15, 23, 42, 0.16);
  z-index: 90;
  display: flex;
  flex-direction: column;
}
.drawer-head { display: flex; justify-content: space-between; align-items: start; padding: 16px 18px; border-bottom: 1px solid var(--border-subtle); }
.drawer-head h3 { margin: 0; font-size: 22px; }
.drawer-head p { margin: 6px 0 0; color: var(--text-muted); }
.drawer-close { border: none; background: transparent; font-size: 28px; color: var(--text-muted); cursor: pointer; transition: color .16s ease; }
.drawer-close:hover { color: var(--text-primary); }
.drawer-body { overflow: auto; padding: 14px 18px 24px; }
.detail-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; margin-bottom: 14px; }
.detail-item { background: var(--surface-default); border: 1px solid var(--border-subtle); border-radius: 12px; padding: 10px 12px; }
.detail-label { display: block; color: var(--text-muted); font-size: 12px; margin-bottom: 6px; text-transform: uppercase; letter-spacing: 0.04em; }
.detail-block { margin-top: 10px; }
.block-title { font-weight: 700; margin-bottom: 6px; }
.code-shell { background: #0f172a; color: #e2e8f0; border-radius: 10px; padding: 10px 12px; overflow: auto; max-height: 240px; white-space: pre-wrap; word-break: break-word; font-size: 12px; }

.slide-right-enter-active,
.slide-right-leave-active { transition: transform 0.2s ease, opacity 0.2s ease; }
.slide-right-enter-from,
.slide-right-leave-to { transform: translateX(20px); opacity: 0; }

@media (max-width: 1280px) {
  .filters { grid-template-columns: 1fr 1fr; }
  .field-query { grid-column: 1 / -1; }
}
</style>
