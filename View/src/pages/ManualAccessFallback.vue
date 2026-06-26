<template>
  <div class="page-container animate-in" style="max-width: 640px; margin: 0 auto; padding-top: 1.5rem;">
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
        <select v-model="selectedGateId" class="form-control" :disabled="submitting">
          <option value="">-- Chọn cổng --</option>
          <option v-for="g in gates" :key="g.gateId" :value="g.gateId">{{ g.gateName }}</option>
        </select>
      </div>

      <div class="maf-tabs">
        <button :class="['maf-tab', { active: tab === 'employee' }]" :disabled="submitting" @click="tab = 'employee'; clearSubject()">Nhân viên</button>
        <button :class="['maf-tab', { active: tab === 'visitor' }]" :disabled="submitting" @click="tab = 'visitor'; clearSubject()">Khách</button>
      </div>

      <template v-if="tab === 'employee'">
        <div class="form-group">
          <label>Tìm nhân viên (tên hoặc mã)</label>
          <div class="search-box">
            <input v-model="empQ" type="text" class="form-control" placeholder="Gõ tên hoặc mã nhân viên..." :disabled="submitting" @input="onEmpSearch" />
            <div v-if="empResults.length" class="dropdown">
              <div v-for="e in empResults" :key="e.employeeId" class="dropdown-item" @click="pickEmp(e)">
                <strong>{{ e.fullName || e.name }}</strong>
                <div class="text-muted" style="font-size:12px;">Mã: {{ e.employeeId }} &middot; {{ e.department || '' }}</div>
              </div>
            </div>
          </div>
        </div>
      </template>

      <template v-if="tab === 'visitor'">
        <div class="form-group">
          <label>Tìm khách (tên hoặc SĐT)</label>
          <div class="search-box">
            <input v-model="visQ" type="text" class="form-control" placeholder="Gõ tên khách..." :disabled="submitting" @input="onVisSearch" />
            <div v-if="visResults.length" class="dropdown">
              <div v-for="v in visResults" :key="v.visitorDetailId" class="dropdown-item" @click="pickVis(v)">
                <strong>{{ v.fullName }}</strong>
                <div class="text-muted" style="font-size:12px;">SĐT: {{ v.guestPhone || '—' }} &middot; Host: {{ v.hostEmployeeName || '—' }}</div>
              </div>
            </div>
          </div>
        </div>
        <div v-if="!selectedSubject && visQ.length >= 2 && !visLoading && !visResults.length" class="text-muted" style="margin-top:6px;font-size:13px;">
          Không tìm thấy khách phù hợp.
        </div>
      </template>

      <div v-if="selectedSubject" class="maf-subject-card">
        <div class="maf-subject-photo">
          <img v-if="faceImgUrl" :src="faceImgUrl" class="maf-face-img" alt="face" />
          <div v-else class="maf-face-fallback">{{ initials }}</div>
        </div>
        <div class="maf-subject-info">
          <div class="maf-subject-name">{{ selectedSubject.displayName }}</div>
          <div class="maf-subject-id">{{ idLabel }}: {{ selectedSubject.idValue }}</div>
          <div class="maf-subject-extra">{{ extraInfo }}</div>
        </div>
        <button class="maf-clear-btn" :disabled="submitting" @click="clearSubject">✕</button>
      </div>

      <div class="form-group" style="margin-top: 1rem;">
        <label>Lý do (không bắt buộc)</label>
        <input v-model="reasonText" type="text" class="form-control" placeholder="Nhập lý do nếu cần..." :disabled="submitting" />
      </div>

      <div v-if="errorMsg" class="alert alert-danger">{{ errorMsg }}</div>

      <div v-if="selectedGateId && selectedSubject" class="maf-actions">
        <button class="btn btn-success maf-btn-allow" :disabled="!canAct || submitting" @click="submitAllow">
          {{ submitting ? 'Đang xử lý...' : '✅ Cho phép vào' }}
        </button>
        <button class="btn btn-danger maf-btn-deny" :disabled="!canAct || submitting" @click="submitDeny">
          {{ submitting ? 'Đang xử lý...' : '❌ Không cho phép' }}
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
import { ref, computed, onBeforeUnmount } from 'vue'
import { getGates } from '../services/deviceManagementApi'
import { getAll as getEmployees, getProtectedFaceImage } from '../services/employeeApi'
import { getVisitorDirectory } from '../services/guestProfileApi'
import http from '../services/http'

const gates = ref([])
const selectedGateId = ref('')
const tab = ref('employee')

const empQ = ref('')
const empResults = ref([])

const visQ = ref('')
const visResults = ref([])
const visLoading = ref(false)

const selectedSubject = ref(null)
const faceImgUrl = ref('')
const reasonText = ref('')
const submitting = ref(false)
const errorMsg = ref('')
const result = ref({ show: false, allowed: false, message: '', subjectName: '', subjectType: '', gateName: '' })

const canAct = computed(() => !!selectedGateId.value && !!selectedSubject.value)

const initials = computed(() => {
  const s = selectedSubject.value
  if (!s) return ''
  const name = s.displayName || ''
  const parts = name.split(/\s+/)
  return parts.length > 1 ? (parts[0][0] + parts[parts.length - 1][0]).toUpperCase() : (name[0] || '').toUpperCase()
})

const idLabel = computed(() => tab.value === 'employee' ? 'Mã NV' : 'Mã KH')

const extraInfo = computed(() => {
  const s = selectedSubject.value
  if (!s) return ''
  if (tab.value === 'employee') return s.department || ''
  return `SĐT: ${s.guestPhone || '—'} · Host: ${s.hostEmployeeName || '—'}`
})

onBeforeUnmount(() => {
  if (faceImgUrl.value) URL.revokeObjectURL(faceImgUrl.value)
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
    empResults.value = (res.data?.items || res.data || []).filter(Boolean)
  }).catch(() => { empResults.value = [] })
}

async function pickEmp(e) {
  empResults.value = []
  empQ.value = e.fullName || e.name || ''
  selectedSubject.value = { displayName: e.fullName || e.name, idValue: e.employeeId, employeeId: e.employeeId, department: e.department, faceImageUrl: e.faceImageUrl }
  await loadFaceImage(e)
}

async function loadFaceImage(emp) {
  if (faceImgUrl.value) { URL.revokeObjectURL(faceImgUrl.value); faceImgUrl.value = '' }
  if (!emp?.faceImageUrl || emp.faceImageUrl.startsWith('http')) return
  try {
    const res = await getProtectedFaceImage(emp.employeeId)
    faceImgUrl.value = URL.createObjectURL(res.data)
  } catch { /* no face image */ }
}

function onVisSearch() {
  const q = visQ.value?.trim()
  if (!q || q.length < 2) { visResults.value = []; return }
  visLoading.value = true
  getVisitorDirectory({ query: q, pageSize: 10, registrationStatus: 'Approved' }).then(res => {
    visResults.value = (res.data?.items || []).filter(Boolean)
  }).catch(() => { visResults.value = [] }).finally(() => { visLoading.value = false })
}

function pickVis(v) {
  visResults.value = []
  visQ.value = v.fullName || ''
  selectedSubject.value = { displayName: v.fullName, idValue: v.visitorDetailId, visitorDetailId: v.visitorDetailId, guestPhone: v.guestPhone, hostEmployeeName: v.hostEmployeeName }
}

function clearSubject() {
  selectedSubject.value = null
  if (faceImgUrl.value) { URL.revokeObjectURL(faceImgUrl.value); faceImgUrl.value = '' }
  empQ.value = ''
  empResults.value = []
  visQ.value = ''
  visResults.value = []
  errorMsg.value = ''
}

function buildPayload(isDenied) {
  const p = { gateId: Number(selectedGateId.value), reason: reasonText.value?.trim() || null, isDenied }
  if (tab.value === 'employee' && selectedSubject.value?.employeeId) p.employeeId = selectedSubject.value.employeeId
  else if (tab.value === 'visitor' && selectedSubject.value?.visitorDetailId) p.visitorDetailId = selectedSubject.value.visitorDetailId
  return p
}

async function submitAllow() {
  errorMsg.value = ''
  submitting.value = true
  try {
    const res = await http.post('/QrAccess/manual-access', buildPayload(false))
    const data = res?.data?.data || {}
    const gate = gates.value.find(g => g.gateId === Number(selectedGateId.value))
    result.value = {
      show: true, allowed: true,
      message: res?.data?.message || 'Cho phép vào cổng',
      subjectName: data.subjectName || selectedSubject.value?.displayName || '',
      subjectType: tab.value === 'employee' ? 'Nhân viên' : 'Khách',
      gateName: gate?.gateName || `Cổng #${selectedGateId.value}`,
    }
  } catch (e) {
    const data = e?.response?.data?.data || {}
    const message = e?.response?.data?.message || e?.message || 'Từ chối'
    const gate = gates.value.find(g => g.gateId === Number(selectedGateId.value))
    result.value = {
      show: true, allowed: false,
      message: e?.response?.status === 403 ? 'Không có quyền truy cập cổng này.' : message,
      subjectName: data.subjectName || selectedSubject.value?.displayName || '',
      subjectType: tab.value === 'employee' ? 'Nhân viên' : 'Khách',
      gateName: gate?.gateName || `Cổng #${selectedGateId.value}`,
    }
  } finally {
    submitting.value = false
  }
}

async function submitDeny() {
  errorMsg.value = ''
  submitting.value = true
  try {
    const res = await http.post('/QrAccess/manual-access', buildPayload(true))
    const data = res?.response?.data?.data || {}
    const gate = gates.value.find(g => g.gateId === Number(selectedGateId.value))
    result.value = {
      show: true, allowed: false,
      message: 'Bảo vệ đã từ chối — nhân dạng không khớp.',
      subjectName: data.subjectName || selectedSubject.value?.displayName || '',
      subjectType: tab.value === 'employee' ? 'Nhân viên' : 'Khách',
      gateName: gate?.gateName || `Cổng #${selectedGateId.value}`,
    }
  } catch (e) {
    const data = e?.response?.data?.data || {}
    const gate = gates.value.find(g => g.gateId === Number(selectedGateId.value))
    result.value = {
      show: true, allowed: false,
      message: 'Bảo vệ đã từ chối — nhân dạng không khớp.',
      subjectName: data.subjectName || selectedSubject.value?.displayName || '',
      subjectType: tab.value === 'employee' ? 'Nhân viên' : 'Khách',
      gateName: gate?.gateName || `Cổng #${selectedGateId.value}`,
    }
  } finally {
    submitting.value = false
  }
}

function closeResult() { result.value.show = false }

function fullReset() {
  selectedGateId.value = ''
  clearSubject()
  reasonText.value = ''
  errorMsg.value = ''
  result.value = { show: false, allowed: false, message: '', subjectName: '', subjectType: '', gateName: '' }
}
</script>

<style scoped>
.maf-header { display: flex; align-items: center; gap: 12px; margin-bottom: 1.25rem; }
.maf-header-icon { font-size: 2rem; }
.maf-tabs { display: flex; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden; margin-bottom: 1rem; }
.maf-tab { flex: 1; padding: 10px; border: none; background: #f8fafc; font-weight: 700; cursor: pointer; font-size: 14px; }
.maf-tab.active { background: #6a8fe8; color: #fff; }
.maf-tab:disabled { opacity: .5; cursor: not-allowed; }
.search-box { position: relative; }
.dropdown { position: absolute; background: #fff; border: 1px solid #e2e8f0; border-radius: 8px; width: 100%; max-height: 220px; overflow-y: auto; z-index: 100; box-shadow: 0 8px 20px rgba(0,0,0,0.08); }
.dropdown-item { padding: 10px; cursor: pointer; font-size: 14px; }
.dropdown-item:hover { background: #f0f4ff; }

.maf-subject-card { display: flex; align-items: center; gap: 14px; padding: 1rem; background: #f8faff; border: 1px solid #dde6f0; border-radius: 14px; margin-top: .75rem; position: relative; }
.maf-subject-photo { flex-shrink: 0; }
.maf-face-img { width: 72px; height: 72px; border-radius: 50%; object-fit: cover; border: 2px solid #cbd5e1; }
.maf-face-fallback { width: 72px; height: 72px; border-radius: 50%; background: #6a8fe8; color: #fff; display: flex; align-items: center; justify-content: center; font-size: 24px; font-weight: 900; }
.maf-subject-info { flex: 1; min-width: 0; }
.maf-subject-name { font-weight: 800; font-size: 18px; }
.maf-subject-id { font-size: 14px; color: #475569; margin-top: 2px; }
.maf-subject-extra { font-size: 13px; color: #64748b; margin-top: 2px; }
.maf-clear-btn { position: absolute; top: 8px; right: 8px; width: 28px; height: 28px; border-radius: 50%; border: 1px solid #e2e8f0; background: #fff; cursor: pointer; font-size: 14px; display: grid; place-items: center; color: #94a3b8; }
.maf-clear-btn:disabled { opacity: .5; }

.maf-actions { display: flex; gap: 10px; margin-top: 1.25rem; }
.maf-btn-allow, .maf-btn-deny { flex: 1; height: 52px; font-size: 16px; font-weight: 800; border: none; border-radius: 12px; cursor: pointer; color: #fff; }
.maf-btn-allow { background: #22c55e; }
.maf-btn-allow:disabled { opacity: .5; cursor: not-allowed; }
.maf-btn-deny { background: #ef4444; }
.maf-btn-deny:disabled { opacity: .5; cursor: not-allowed; }

.maf-mask { position: fixed; inset: 0; background: rgba(2,6,23,0.45); z-index: 300; display: grid; place-items: center; }
.maf-dialog { width: min(440px, 92vw); background: #fff; border-radius: 14px; padding: 2rem; text-align: center; }
.maf-dialog.maf-allow { border: 2px solid #22c55e; }
.maf-dialog.maf-deny { border: 2px solid #ef4444; }
.maf-dialog h2 { margin: .5rem 0; }
.detail-grid { text-align: left; }
.detail-row { display: flex; justify-content: space-between; padding: 6px 0; border-bottom: 1px solid #f1f5f9; }
.detail-label { font-weight: 700; color: #475569; }
</style>
