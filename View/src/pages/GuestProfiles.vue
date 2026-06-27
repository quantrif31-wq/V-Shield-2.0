<template>
  <div class="page-container ops-page animate-in">
    <div class="page-header-bar">
      <div>
        <span class="panel-kicker">DANH BẠ KHÁCH</span>
        <h1 class="page-title">Quản lý khách</h1>
      </div>
      <div class="header-actions">
        <button class="btn btn-secondary" @click="showFormTemplatesModal = true">Mẫu biểu</button>
      </div>
    </div>

    <section class="ops-panel">
      <div class="filters">
        <div class="field">
          <label>Tìm khách (Tên, CCCD hoặc ID)</label>
          <div class="combo-box">
            <input
              v-model.trim="visitorFilterKeyword"
              type="text"
              placeholder="Ví dụ: Nguyễn Văn A hoặc 12"
              @focus="showVisitorFilterDropdown = true"
              @input="handleVisitorFilterInput"
            />
            <ul v-if="showVisitorFilterDropdown && visitorFilterOptions.length" class="combo-menu">
              <li
                v-for="visitor in visitorFilterOptions"
                :key="visitor.visitorDetailId"
                class="combo-item"
                @mousedown.prevent="selectVisitorFilter(visitor)"
              >
                {{ visitor.fullName }} (ID {{ visitor.visitorDetailId }}) - CCCD: {{ visitor.idCardNumber || '-' }}
              </li>
            </ul>
          </div>
        </div>

        <div class="field">
          <label>Lọc theo host phụ trách</label>
          <div class="combo-box">
            <input
              v-model.trim="hostFilterKeyword"
              type="text"
              placeholder="Gõ tên hoặc ID host"
              @focus="showHostFilterDropdown = true"
              @input="handleHostFilterInput"
            />
            <ul v-if="showHostFilterDropdown && hostFilterOptions.length" class="combo-menu">
              <li class="combo-item" @mousedown.prevent="selectAllHostFilter">Tất cả host</li>
              <li
                v-for="emp in hostFilterOptions"
                :key="emp.employeeId"
                class="combo-item"
                @mousedown.prevent="selectHostFilter(emp)"
              >
                {{ emp.fullName }} (ID {{ emp.employeeId }})
              </li>
            </ul>
          </div>
        </div>

        <div class="field">
          <label>Trạng thái phiếu</label>
          <select v-model="filters.registrationStatus">
            <option value="">Tất cả trạng thái</option>
            <option value="Pending">Chờ duyệt</option>
            <option value="Approved">Đã duyệt</option>
            <option value="Rejected">Từ chối</option>
          </select>
        </div>

        <div class="actions">
          <button class="btn btn-subtle" :disabled="isLoading" @click="resetFilters">Đặt lại</button>
        </div>
      </div>

      <div v-if="isLoading" class="empty-card">Đang tải dữ liệu khách...</div>
      <div v-else-if="rows.length === 0" class="empty-card">Chưa có dữ liệu khách.</div>
      <div v-else class="table-container">
        <table class="data-table">
          <thead>
            <tr>
              <th>ID khách</th>
              <th>Tên khách</th>
              <th>CCCD</th>
              <th>Host phụ trách</th>
              <th>Liên hệ</th>
              <th>Trạng thái</th>
              <th>Giấy tờ</th>
              <th>Thao tác</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in rows" :key="item.visitorDetailId">
              <td>{{ item.visitorDetailId }}</td>
              <td class="table-main">{{ item.fullName }}</td>
              <td>{{ item.idCardNumber || '-' }}</td>
              <td>{{ item.hostEmployeeName || '-' }}</td>
              <td>{{ item.guestPhone || '-' }}</td>
              <td>{{ registrationStatusLabel(item.registrationStatus) }}</td>
              <td>
                <span v-if="item.ndaStatus" class="soft-chip" :class="item.ndaStatus === 'Signed' ? 'success' : 'warn'">{{ ndaStatusLabel(item.ndaStatus) }}</span>
                <span v-else class="text-muted">—</span>
              </td>
              <td>
                <div class="panel-actions">
                  <button class="btn btn-secondary btn-sm" @click="openModal(item)">Sửa</button>
                  <button class="btn btn-secondary btn-sm" @click="openLogModal(item)">Lịch sử</button>
                  <button class="btn btn-secondary btn-sm" @click="openParkingInfo(item)">Xe</button>
                  <button class="btn btn-danger btn-sm" @click="handleDelete(item)">Xóa</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-if="!isLoading && total > 0" class="pagination-bar">
        <span>Hiển thị {{ rows.length }} / {{ total }}</span>
      </div>
    </section>

    <transition name="modal">
      <div v-if="showModal" class="modal-overlay" @click.self="closeModal">
        <div class="modal">
          <div class="modal-header">
            <h3 class="modal-title">Cập nhật thông tin khách</h3>
            <button class="modal-close" @click="closeModal">x</button>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>Tên khách</label>
              <input v-model="form.fullName" type="text" />
            </div>
            <div class="form-group">
              <label>CCCD</label>
              <input v-model="form.idCardNumber" type="text" />
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label>Host phụ trách</label>
              <select v-model="form.hostEmployeeId">
                <option :value="null">Không gán host</option>
                <option v-for="emp in employees" :key="emp.employeeId" :value="emp.employeeId">
                  {{ emp.fullName }} (ID {{ emp.employeeId }})
                </option>
              </select>
            </div>
          </div>

          <div class="form-group">
            <label>Trạng thái NDA</label>
            <select v-model="form.ndaStatus" class="form-control">
              <option value="">— Chưa có —</option>
              <option value="Signed">Đã ký</option>
              <option value="Pending">Chờ ký</option>
              <option value="NotRequired">Không yêu cầu</option>
            </select>
          </div>

          <div v-if="formError" class="empty-card error-card">{{ formError }}</div>

          <div class="modal-footer">
            <button class="btn btn-secondary" @click="closeModal">Hủy</button>
            <button class="btn btn-primary" :disabled="isSaving" @click="handleSave">
              {{ isSaving ? 'Đang lưu...' : 'Lưu thay đổi' }}
            </button>
          </div>
        </div>
      </div>
    </transition>

    <transition name="modal">
      <div v-if="showLogModal" class="modal-overlay" @click.self="closeLogModal">
        <div class="modal">
          <div class="modal-header">
            <h3 class="modal-title">Lịch sử ra vào - {{ logTargetName }}</h3>
            <button class="modal-close" @click="closeLogModal">x</button>
          </div>
          <div v-if="isLogLoading" class="empty-card">Đang tải lịch sử...</div>
          <div v-else-if="accessLogs.length === 0" class="empty-card">Không có log.</div>
          <div v-else class="table-container">
            <table class="data-table">
              <thead>
                <tr>
                  <th>Thời gian</th>
                  <th>Hướng</th>
                  <th>Gate</th>
                  <th>Camera</th>
                  <th>Kết quả</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="log in accessLogs" :key="log.logId">
                  <td>{{ formatDateTime(log.timestamp) }}</td>
                  <td>{{ log.direction }}</td>
                  <td>{{ log.gateName || '-' }}</td>
                  <td>{{ log.cameraName || '-' }}</td>
                  <td>{{ resultStatusLabel(log.resultStatus) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </transition>

    <!-- Parking Info Modal -->
    <transition name="modal">
      <div v-if="showParkingModal" class="modal-overlay" @click.self="closeParkingModal">
        <div class="modal">
          <div class="modal-header">
            <h3 class="modal-title">Giấy phép đỗ xe - {{ parkingTargetName }}</h3>
            <button class="modal-close" @click="closeParkingModal">x</button>
          </div>
          <div v-if="isParkingLoading" class="empty-card">Đang tải thông tin xe...</div>
          <div v-else-if="parkingPermits.length === 0" class="empty-card">Không có permit đậu xe.</div>
          <div v-else class="table-container">
            <table class="data-table">
              <thead>
                <tr>
                  <th>Khu vực</th>
                  <th>Biển số</th>
                  <th>Hiệu lực từ</th>
                  <th>Hiệu lực đến</th>
                  <th>Trạng thái</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="p in parkingPermits" :key="p.parkingPermitId || p.id">
                  <td>{{ p.areaName || p.parkingAreaName || '-' }}</td>
                  <td>{{ p.plateNumber || '-' }}</td>
                  <td>{{ formatDateTime(p.validFromUtc) }}</td>
                  <td>{{ formatDateTime(p.validToUtc) }}</td>
                  <td>
                    <span class="soft-chip" :class="new Date(p.validToUtc) > new Date() ? 'success' : 'danger'">
                      {{ new Date(p.validToUtc) > new Date() ? 'Còn hiệu lực' : 'Hết hạn' }}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </transition>

    <!-- Form Templates Modal -->
    <transition name="modal">
      <div v-if="showFormTemplatesModal" class="modal-overlay" @click.self="showFormTemplatesModal = false">
        <div class="modal">
          <div class="modal-header">
            <h3 class="modal-title">Mẫu biểu</h3>
            <button class="modal-close" @click="showFormTemplatesModal = false">x</button>
          </div>
          <div v-if="formTemplates.length === 0" class="empty-card">Không có mẫu biểu.</div>
          <div v-else>
            <div v-for="ft in formTemplates" :key="ft.formTemplateId || ft.id" class="template-item">
              <div><strong>{{ ft.templateName || ft.name }}</strong></div>
              <div class="text-muted">{{ ft.description || ft.category || '' }}</div>
            </div>
          </div>
        </div>
      </div>
    </transition>
  </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted, reactive, ref, watch } from 'vue'
import { getAll as getEmployees } from '../services/employeeApi'
import { deleteVisitorDirectoryItem, getVisitorAccessLogs, getVisitorDirectory, updateVisitorDirectoryItem } from '../services/guestProfileApi'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const isLoading = ref(true)
const isSaving = ref(false)
const query = ref('')
const rows = ref([])
const total = ref(0)
const showModal = ref(false)
const showLogModal = ref(false)
const isLogLoading = ref(false)
const accessLogs = ref([])
const logTargetName = ref('')
const formError = ref('')
const employees = ref([])
const editingId = ref(null)

// Parking
const showParkingModal = ref(false)
const isParkingLoading = ref(false)
const parkingTargetName = ref('')
const parkingPermits = ref([])

// Form Templates
const showFormTemplatesModal = ref(false)
const formTemplates = ref([])

const form = reactive({
  fullName: '',
  idCardNumber: '',
  hostEmployeeId: null,
  ndaStatus: '',
})
const filters = reactive({
  hostEmployeeId: null,
  registrationStatus: ''
})
const visitorFilterKeyword = ref('')
const hostFilterKeyword = ref('')
const showVisitorFilterDropdown = ref(false)
const showHostFilterDropdown = ref(false)

function normalizeText(input) {
  return String(input || '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/\u0111/g, 'd')
    .replace(/\u0110/g, 'd')
    .toLowerCase()
    .trim()
}

const visitorFilterOptions = computed(() => {
  const q = normalizeText(visitorFilterKeyword.value)
  const all = rows.value || []
  if (!q) return all.slice(0, 5)
  return all
    .filter((v) =>
      normalizeText(v.fullName).includes(q) ||
      String(v.visitorDetailId || '').includes(q) ||
      normalizeText(v.idCardNumber || '').includes(q)
    )
    .slice(0, 5)
})

const hostFilterOptions = computed(() => {
  const q = normalizeText(hostFilterKeyword.value)
  const all = employees.value || []
  if (!q) return all.slice(0, 5)
  return all
    .filter((e) => normalizeText(e.fullName).includes(q) || String(e.employeeId || '').includes(q))
    .slice(0, 5)
})

const fetchRows = async () => {
  isLoading.value = true
  try {
    const params = {
      query: query.value || undefined,
      page: 1,
      pageSize: 100,
      hostEmployeeId: filters.hostEmployeeId || undefined,
      registrationStatus: filters.registrationStatus || undefined
    }
    const { data } = await getVisitorDirectory(params)
    rows.value = data?.items || []
    total.value = data?.total || 0
  } finally {
    isLoading.value = false
  }
}

const fetchEmployees = async () => {
  const { data } = await getEmployees({ search: '' })
  employees.value = Array.isArray(data) ? data : (data?.items || [])
}

const fetchFormTemplates = async () => {
  try {
    const res = await enterpriseApi.getFormTemplates({ pageSize: 50 })
    formTemplates.value = res.data?.items || []
  } catch (e) {
    console.error('Failed to load form templates', e)
  }
}

function handleVisitorFilterInput() {
  showVisitorFilterDropdown.value = true
  query.value = visitorFilterKeyword.value
}
function selectVisitorFilter(visitor) {
  visitorFilterKeyword.value = `${visitor.fullName} (ID ${visitor.visitorDetailId})`
  query.value = String(visitor.visitorDetailId)
  showVisitorFilterDropdown.value = false
  fetchRows()
}
function handleHostFilterInput() {
  showHostFilterDropdown.value = true
  filters.hostEmployeeId = null
}
function selectAllHostFilter() {
  filters.hostEmployeeId = null
  hostFilterKeyword.value = ''
  showHostFilterDropdown.value = false
  fetchRows()
}
function selectHostFilter(emp) {
  filters.hostEmployeeId = emp.employeeId
  hostFilterKeyword.value = `${emp.fullName} (ID ${emp.employeeId})`
  showHostFilterDropdown.value = false
  fetchRows()
}
function resetFilters() {
  query.value = ''
  filters.hostEmployeeId = null
  filters.registrationStatus = ''
  visitorFilterKeyword.value = ''
  hostFilterKeyword.value = ''
  showVisitorFilterDropdown.value = false
  showHostFilterDropdown.value = false
  fetchRows()
}
function closeAllComboboxes() {
  showVisitorFilterDropdown.value = false
  showHostFilterDropdown.value = false
}
function handleDocumentClick(event) {
  const target = event.target
  if (!(target instanceof Element)) return
  if (target.closest('.combo-box')) return
  closeAllComboboxes()
}

const openModal = (item) => {
  editingId.value = item.visitorDetailId
  form.fullName = item.fullName || ''
  form.idCardNumber = item.idCardNumber || ''
  form.hostEmployeeId = item.hostEmployeeId || null
  form.ndaStatus = item.ndaStatus || ''
  formError.value = ''
  showModal.value = true
}

const closeModal = () => {
  showModal.value = false
  editingId.value = null
  form.fullName = ''
  form.idCardNumber = ''
  form.hostEmployeeId = null
  form.ndaStatus = ''
  formError.value = ''
}

const handleSave = async () => {
  formError.value = ''
  if (!editingId.value) return
  if (!form.fullName.trim()) {
    formError.value = 'Tên khách là bắt buộc.'
    return
  }

  isSaving.value = true
  try {
    await updateVisitorDirectoryItem(editingId.value, {
      fullName: form.fullName.trim(),
      idCardNumber: form.idCardNumber || null,
      hostEmployeeId: form.hostEmployeeId || null,
      ndaStatus: form.ndaStatus || null,
    })
    await fetchRows()
    closeModal()
  } catch (e) {
    formError.value = e?.response?.data?.message || 'Không thể cập nhật khách.'
  } finally {
    isSaving.value = false
  }
}

const handleDelete = async (item) => {
  const ok = window.confirm(`Xóa khách "${item.fullName}" (ID ${item.visitorDetailId})?`)
  if (!ok) return
  try {
    await deleteVisitorDirectoryItem(item.visitorDetailId)
    await fetchRows()
  } catch (e) {
    window.alert(e?.response?.data?.message || 'Không thể xóa khách này.')
  }
}

const openLogModal = async (item) => {
  showLogModal.value = true
  isLogLoading.value = true
  accessLogs.value = []
  logTargetName.value = item.fullName
  try {
    const { data } = await getVisitorAccessLogs(item.visitorDetailId)
    accessLogs.value = data?.items || []
  } finally {
    isLogLoading.value = false
  }
}

const closeLogModal = () => {
  showLogModal.value = false
  accessLogs.value = []
  logTargetName.value = ''
}

// Parking info
const openParkingInfo = async (item) => {
  showParkingModal.value = true
  isParkingLoading.value = true
  parkingTargetName.value = item.fullName
  parkingPermits.value = []
  try {
    const res = await enterpriseApi.getParkingPermits({ visitorDetailId: item.visitorDetailId, pageSize: 20 })
    parkingPermits.value = res.data?.items || []
  } catch (e) {
    console.error('Failed to load parking permits', e)
  } finally {
    isParkingLoading.value = false
  }
}

const closeParkingModal = () => {
  showParkingModal.value = false
  parkingPermits.value = []
  parkingTargetName.value = ''
}

const registrationStatusLabel = (value) => ({
  Pending: 'Chờ duyệt',
  Approved: 'Đã duyệt',
  Rejected: 'Từ chối',
})[value] || value || '-'

const ndaStatusLabel = (value) => ({
  Signed: 'Đã ký',
  Pending: 'Chờ ký',
  NotRequired: 'Không yêu cầu',
})[value] || value || '-'

const resultStatusLabel = (value) => ({
  Approved: 'Đã duyệt',
  Rejected: 'Từ chối',
  Granted: 'Cho phép',
  Denied: 'Từ chối truy cập',
  Success: 'Thành công',
  Failed: 'Thất bại',
})[value] || value || '-'

const formatDateTime = (value) => {
  if (!value) return '-'
  return new Date(value).toLocaleString('vi-VN')
}

let timer = null
watch(query, () => {
  clearTimeout(timer)
  timer = setTimeout(fetchRows, 250)
})
watch(
  () => [filters.hostEmployeeId, filters.registrationStatus],
  () => {
    fetchRows()
  }
)

onMounted(async () => {
  await Promise.all([fetchRows(), fetchEmployees(), fetchFormTemplates()])
  document.addEventListener('click', handleDocumentClick)
})
onUnmounted(() => {
  document.removeEventListener('click', handleDocumentClick)
})
</script>

<style scoped>
.filters { display: grid; grid-template-columns: 1.2fr 1fr 0.7fr auto; gap: 12px; align-items: end; margin-bottom: 12px; }
.field { display: flex; flex-direction: column; gap: 6px; }
.field label { font-size: 12px; color: #51657b; font-weight: 600; }
.field select, .combo-box input { width: 100%; min-height: 38px; border: 1px solid #cfe0ea; border-radius: 10px; padding: 8px 10px; background: #f8fcff; }
.combo-box { position: relative; }
.combo-menu { position: absolute; z-index: 20; top: calc(100% + 6px); left: 0; right: 0; background: #ffffff; border: 1px solid #cfe0ea; border-radius: 10px; box-shadow: 0 8px 24px rgba(15, 23, 42, 0.08); max-height: 220px; overflow-y: auto; }
.combo-item { padding: 10px 12px; cursor: pointer; font-size: 13px; color: #17304b; }
.combo-item:hover { background: #eef7ff; }
.actions { display: flex; justify-content: flex-end; }
.btn-subtle { border: 1px solid #cfe0ea; background: #fff; color: #1f3650; border-radius: 10px; padding: 8px 12px; }
.template-item { padding: 8px 10px; border: 1px solid #e2e8f0; border-radius: 8px; margin-bottom: 6px; }
@media (max-width: 1100px) { .filters { grid-template-columns: 1fr; } .actions { justify-content: flex-start; } }
</style>
