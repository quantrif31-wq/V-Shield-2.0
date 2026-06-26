<template>
  <div class="page-container animate-in" style="max-width: 720px; margin: 0 auto; padding-top: 1.5rem;">
    <div class="card" style="padding: 1.5rem;">
      <div class="maf-header">
        <div class="maf-header-icon">⚠️</div>
        <div>
          <h1 class="page-title" style="margin: 0;">Vào cổng thủ công</h1>
          <p class="text-muted" style="margin: 4px 0 0;">Dùng khi QR Access Monitor bị tê liệt</p>
        </div>
      </div>

      <div class="form-group">
        <label>Cổng vào</label>
        <select v-model="selectedGateId" class="form-control">
          <option value="">-- Chọn cổng --</option>
          <option v-for="g in gates" :key="g.gateId" :value="g.gateId">{{ g.gateName }}</option>
        </select>
      </div>

      <div class="maf-tabs">
        <button :class="['maf-tab', { active: tab === 'employee' }]" @click="tab = 'employee'">Nhân viên</button>
        <button :class="['maf-tab', { active: tab === 'visitor' }]" @click="tab = 'visitor'">Khách</button>
      </div>

      <template v-if="tab === 'employee'">
        <div class="form-group">
          <label>Tìm nhân viên (tên hoặc mã)</label>
          <div class="search-box">
            <input v-model="empQ" type="text" class="form-control" placeholder="Gõ tên hoặc mã nhân viên..." @input="onEmpSearch" />
            <div v-if="empResults.length" class="dropdown">
              <div v-for="e in empResults" :key="e.employeeId" class="dropdown-item" @click="pickEmp(e)">
                <strong>{{ e.fullName || e.name }}</strong>
                <div class="text-muted" style="font-size:12px;">Mã: {{ e.employeeId }} &middot; {{ e.department || '' }}</div>
              </div>
            </div>
          </div>
        </div>
        <div v-if="selectedEmp" class="card maf-selected">
          <div class="maf-selected-row">
            <div>
              <strong>{{ selectedEmp.fullName || selectedEmp.name }}</strong>
              <div class="text-muted" style="font-size:13px;">Mã NV: {{ selectedEmp.employeeId }}</div>
            </div>
            <button class="btn btn-sm btn-secondary" @click="clearEmp">Bỏ chọn</button>
          </div>
        </div>
      </template>

      <template v-if="tab === 'visitor'">
        <div class="form-group">
          <label>Tìm khách (tên hoặc SĐT)</label>
          <div class="search-box">
            <input v-model="visQ" type="text" class="form-control" placeholder="Gõ tên khách..." @input="onVisSearch" />
            <div v-if="visResults.length" class="dropdown">
              <div v-for="v in visResults" :key="v.visitorDetailId" class="dropdown-item" @click="pickVis(v)">
                <strong>{{ v.fullName }}</strong>
                <div class="text-muted" style="font-size:12px;">SĐT: {{ v.guestPhone || '—' }} &middot; Host: {{ v.hostEmployeeName || '—' }}</div>
              </div>
            </div>
          </div>
        </div>
        <div v-if="selectedVis" class="card maf-selected">
          <div class="maf-selected-row">
            <div>
              <strong>{{ selectedVis.fullName }}</strong>
              <div class="text-muted" style="font-size:13px;">Mã KH: {{ selectedVis.visitorDetailId }} &middot; Host: {{ selectedVis.hostEmployeeName || '—' }}</div>
            </div>
            <button class="btn btn-sm btn-secondary" @click="clearVis">Bỏ chọn</button>
          </div>
        </div>
        <div v-if="!selectedVis && visQ.length >= 2 && !visLoading && !visResults.length" class="text-muted" style="margin-top:6px;font-size:13px;">
          Không tìm thấy khách phù hợp.
        </div>
      </template>

      <div class="form-row two" style="margin-top:1rem;">
        <div class="form-group">
          <label>Biển số xe (không bắt buộc)</label>
          <input v-model="plateNumber" type="text" class="form-control" placeholder="Ví dụ: 29A-12345" />
        </div>
        <div class="form-group">
          <label>&nbsp;</label>
          <div></div>
        </div>
      </div>

      <div class="form-group">
        <label>Lý do vào thủ công (không bắt buộc)</label>
        <textarea v-model="reasonText" class="form-control" rows="2" placeholder="Nhập lý do..."></textarea>
      </div>

      <div v-if="errorMsg" class="alert alert-danger">{{ errorMsg }}</div>

      <div style="margin-top:1.25rem;">
        <button class="btn btn-primary" style="width:100%;height:48px;font-size:16px;" :disabled="!canSubmit || submitting" @click="submit">
          {{ submitting ? 'Đang xử lý...' : 'Xác nhận vào cổng' }}
        </button>
      </div>
    </div>

    <div v-if="result.show" class="maf-mask" @click="closeResult">
      <div :class="['maf-dialog', result.allowed ? 'maf-allow' : 'maf-deny']" @click.stop>
        <div style="font-size:3rem;">{{ result.allowed ? '✅' : '❌' }}</div>
        <h2>{{ result.allowed ? 'CHO PHÉP VÀO' : 'TỪ CHỐI' }}</h2>
        <p>{{ result.message }}</p>
        <div class="detail-grid" style="margin-top:.75rem;">
          <div class="detail-row"><span class="detail-label">Đối tượng</span><span>{{ result.subjectName }}</span></div>
          <div class="detail-row"><span class="detail-label">Loại</span><span>{{ result.subjectType }}</span></div>
          <div class="detail-row"><span class="detail-label">Cổng</span><span>{{ result.gateName }}</span></div>
        </div>
        <button class="btn btn-primary" style="margin-top:1rem;" @click="closeResult; fullReset()">Xác nhận</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { getGates } from '../services/deviceManagementApi'
import { getAll as getEmployees } from '../services/employeeApi'
import { getVisitorDirectory } from '../services/guestProfileApi'
import http from '../services/http'

const gates = ref([])
const selectedGateId = ref('')
const tab = ref('employee')

const empQ = ref('')
const empResults = ref([])
const selectedEmp = ref(null)

const visQ = ref('')
const visResults = ref([])
const selectedVis = ref(null)
const visLoading = ref(false)

const plateNumber = ref('')
const reasonText = ref('')
const submitting = ref(false)
const errorMsg = ref('')

const result = ref({ show: false, allowed: false, message: '', subjectName: '', subjectType: '', gateName: '' })

const canSubmit = computed(() => {
  if (!selectedGateId.value) return false
  if (tab.value === 'employee' && !selectedEmp.value) return false
  if (tab.value === 'visitor' && !selectedVis.value) return false
  return true
})

onMounted(async () => {
  try {
    const res = await getGates()
    gates.value = res.data || []
  } catch (e) {
    console.error('Load gates failed', e)
  }
})

function onEmpSearch() {
  const q = empQ.value?.trim()
  if (!q || q.length < 2) { empResults.value = []; return }
  getEmployees({ name: q, pageSize: 10 }).then(res => {
    empResults.value = res.data?.items || res.data || []
  }).catch(() => { empResults.value = [] })
}

function pickEmp(e) {
  selectedEmp.value = e
  empResults.value = []
  empQ.value = e.fullName || e.name || ''
}

function clearEmp() {
  selectedEmp.value = null
  empQ.value = ''
  empResults.value = []
}

function onVisSearch() {
  const q = visQ.value?.trim()
  if (!q || q.length < 2) { visResults.value = []; return }
  visLoading.value = true
  getVisitorDirectory({ query: q, pageSize: 10, registrationStatus: 'Approved' }).then(res => {
    visResults.value = res.data?.items || []
  }).catch(() => { visResults.value = [] }).finally(() => { visLoading.value = false })
}

function pickVis(v) {
  selectedVis.value = v
  visResults.value = []
  visQ.value = v.fullName || ''
}

function clearVis() {
  selectedVis.value = null
  visQ.value = ''
  visResults.value = []
}

async function submit() {
  if (!canSubmit.value) return
  errorMsg.value = ''
  submitting.value = true

  const payload = {
    gateId: Number(selectedGateId.value),
    plateNumber: plateNumber.value?.trim() || null,
    reason: reasonText.value?.trim() || null,
  }
  if (tab.value === 'employee' && selectedEmp.value) payload.employeeId = selectedEmp.value.employeeId
  else if (tab.value === 'visitor' && selectedVis.value) payload.visitorDetailId = selectedVis.value.visitorDetailId

  try {
    const res = await http.post('/QrAccess/manual-access', payload)
    const data = res?.data?.data || {}
    const gate = gates.value.find(g => g.gateId === Number(selectedGateId.value))
    result.value = {
      show: true, allowed: true,
      message: res?.data?.message || 'Cho phép vào cổng',
      subjectName: data.subjectName || '', subjectType: tab.value === 'employee' ? 'Nhân viên' : 'Khách',
      gateName: gate?.gateName || `Cổng #${selectedGateId.value}`,
    }
  } catch (e) {
    const status = Number(e?.response?.status || 0)
    const data = e?.response?.data?.data || {}
    const message = e?.response?.data?.message || e?.message || 'Từ chối'
    const gate = gates.value.find(g => g.gateId === Number(selectedGateId.value))
    result.value = {
      show: true, allowed: false,
      message: status === 403 ? 'Không có quyền truy cập cổng này.' : message,
      subjectName: data.subjectName || '', subjectType: tab.value === 'employee' ? 'Nhân viên' : 'Khách',
      gateName: gate?.gateName || `Cổng #${selectedGateId.value}`,
    }
  } finally {
    submitting.value = false
  }
}

function closeResult() { result.value.show = false }

function fullReset() {
  selectedGateId.value = ''
  clearEmp()
  clearVis()
  plateNumber.value = ''
  reasonText.value = ''
  errorMsg.value = ''
}
</script>

<style scoped>
.maf-header { display: flex; align-items: center; gap: 12px; margin-bottom: 1.25rem; }
.maf-header-icon { font-size: 2rem; }
.maf-tabs { display: flex; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden; margin-bottom: 1rem; }
.maf-tab { flex: 1; padding: 10px; border: none; background: #f8fafc; font-weight: 700; cursor: pointer; font-size: 14px; }
.maf-tab.active { background: #6a8fe8; color: #fff; }
.search-box { position: relative; }
.dropdown { position: absolute; background: #fff; border: 1px solid #e2e8f0; border-radius: 8px; width: 100%; max-height: 220px; overflow-y: auto; z-index: 100; box-shadow: 0 8px 20px rgba(0,0,0,0.08); }
.dropdown-item { padding: 10px; cursor: pointer; font-size: 14px; }
.dropdown-item:hover { background: #f0f4ff; }
.maf-selected { padding: .75rem; margin-top: .5rem; background: #f0f9ff; }
.maf-selected-row { display: flex; justify-content: space-between; align-items: center; }
.maf-mask { position: fixed; inset: 0; background: rgba(2,6,23,0.45); z-index: 300; display: grid; place-items: center; }
.maf-dialog { width: min(440px, 92vw); background: #fff; border-radius: 14px; padding: 2rem; text-align: center; }
.maf-dialog.maf-allow { border: 2px solid #22c55e; }
.maf-dialog.maf-deny { border: 2px solid #ef4444; }
.maf-dialog h2 { margin: .5rem 0; }
.detail-grid { text-align: left; }
.detail-row { display: flex; justify-content: space-between; padding: 6px 0; border-bottom: 1px solid #f1f5f9; }
.detail-label { font-weight: 700; color: #475569; }
</style>
