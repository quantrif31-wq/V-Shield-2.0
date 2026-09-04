<template>
  <div class="page-container animate-in" style="max-width: 600px; margin: 0 auto; padding-top: 1.5rem;">
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
        <select v-model="gateId" class="form-control" :disabled="busy">
          <option value="">-- Chọn cổng --</option>
          <option v-for="g in gates" :key="g.gateId" :value="g.gateId">{{ g.gateName }}</option>
        </select>
      </div>

      <div class="form-group">
        <label>Nhận dạng người qua cổng</label>
        <div class="search-box">
          <input
            v-model="searchQuery"
            type="text"
            class="form-control"
            placeholder="Nhập tên, mã nhân viên hoặc thông tin khách..."
            :disabled="busy || !!subject"
            @input="onUnifiedSearch"
          />
          <div v-if="searchResults.length" class="dropdown">
            <div v-for="item in searchResults" :key="`${item.subjectType}-${item.id}`" class="dropdown-item" @click="pickSubject(item)">
              <div class="maf-result-title">
                <strong>{{ item.fullName }}</strong>
                <span class="maf-result-type" :class="item.subjectType">{{ item.subjectType === 'employee' ? 'Nhân viên' : 'Khách' }}</span>
              </div>
              <div class="text-muted" style="font-size:12px;">{{ item.detail }}</div>
            </div>
          </div>
        </div>
        <div v-if="!subject && searchQuery.length >= 2 && !searching && !searchResults.length" class="text-muted" style="margin-top:6px;font-size:13px;">
          Không tìm thấy nhân viên hoặc khách phù hợp.
        </div>
      </div>

        <!-- Subject card (photo + info) -->
      <div v-if="subject" class="maf-photo-card" :class="photoClass">
        <div class="maf-photo-col">
          <img v-if="faceImg" :src="faceImg" class="maf-photo" />
          <div v-else class="maf-photo-fallback">{{ initials }}</div>
        </div>
        <div class="maf-info-col">
          <div class="maf-name">{{ subject.displayName }}</div>
          <div class="maf-id">{{ idLabel }}: {{ subject.idValue }}</div>
          <div class="maf-extra">{{ extraInfo }}</div>
          <div v-if="subject.idCardNumber" class="maf-extra maf-cccd">CCCD: {{ subject.idCardNumber }}</div>
        </div>
        <div v-if="resultMsg" class="maf-badge" :class="resultOk ? 'badge-ok' : 'badge-fail'">{{ resultMsg }}</div>
        <button class="maf-close" :disabled="busy" @click="clearSubject">✕</button>
      </div>

      <!-- Allow/deny actions -->
      <div v-if="subject && !resultMsg" class="maf-actions">
        <button class="maf-btn maf-btn-allow" :disabled="!gateId || busy" @click="submitDecision(true)">✅ Cho phép</button>
        <button class="maf-btn maf-btn-deny" :disabled="!gateId || busy" @click="submitDecision(false)">❌ Từ chối</button>
      </div>

      <!-- Error -->
      <div v-if="errorMsg" class="alert alert-danger">{{ errorMsg }}</div>

      <!-- Gate log result (after allow) -->
      <div v-if="logResult.show" class="maf-mask" @click="logResult.show = false">
        <div :class="['maf-dialog', logResult.ok ? 'maf-allow' : 'maf-deny']" @click.stop>
          <div style="font-size:3rem;">{{ logResult.ok ? '✅' : '❌' }}</div>
          <h2>{{ logResult.title }}</h2>
          <p>{{ logResult.message }}</p>
          <button class="btn btn-primary" style="margin-top:1rem;" @click="logResult.show = false; fullReset()">Xác nhận</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { getGates } from '../services/deviceManagementApi'
import { getAll as getEmployees, getProtectedFaceImage } from '../services/employeeApi'
import { getVisitorDirectory } from '../services/guestProfileApi'
import { getManualSubject } from '../services/gateTransitApi'
import http from '../services/http'

const gates = ref([])
const gateId = ref('')
const busy = ref(false)
const errorMsg = ref('')
const searchQuery = ref('')
const searchResults = ref([])
const searching = ref(false)

const subject = ref(null)
const faceImg = ref('')

const resultOk = ref(null)
const resultMsg = ref('')

const logResult = ref({ show: false, ok: false, title: '', message: '' })

const initials = computed(() => {
  const s = subject.value; if (!s) return ''
  const parts = (s.displayName || '').split(/\s+/)
  return parts.length > 1 ? (parts[0][0] + parts[parts.length - 1][0]).toUpperCase() : (parts[0]?.[0] || '').toUpperCase()
})
const idLabel = computed(() => subject.value?.subjectType === 'employee' ? 'Mã NV' : 'Mã KH')
const extraInfo = computed(() => {
  const s = subject.value; if (!s) return ''
  return s.subjectType === 'employee' ? (s.department || '') : `SĐT: ${s.guestPhone || '—'} · Host: ${s.hostEmployeeName || '—'}`
})
const photoClass = computed(() => {
  if (resultOk.value === null) return ''
  return resultOk.value ? 'border-ok' : 'border-fail'
})

onMounted(async () => {
  try { const r = await getGates(); gates.value = r.data || [] } catch (e) { console.error(e) }
})
onBeforeUnmount(() => { if (faceImg.value) URL.revokeObjectURL(faceImg.value) })

async function onUnifiedSearch() {
  const q = searchQuery.value?.trim()
  if (!q) { searchResults.value = []; return }
  searching.value = true
  try {
    // IDs and QR payloads are exact credentials, including short values such
    // as employee "1". Resolve them first instead of treating them as names.
    const isDirectCredential = /^\d+$/.test(q) || /^(EMP|VIS):/i.test(q)
    if (isDirectCredential) {
      const response = await getManualSubject(q)
      const data = response.data?.data
      if (!response.data?.success || !data) {
        searchResults.value = []
        return
      }
      searchResults.value = [{
        subjectType: data.subjectType,
        id: data.subjectId,
        fullName: data.fullName || 'Không rõ tên',
        detail: data.subjectType === 'employee'
          ? `Mã NV: ${data.subjectId}${data.departmentName ? ` · ${data.departmentName}` : ''}`
          : `Khách · SĐT: ${data.guestPhone || '—'} · Host: ${data.hostEmployeeName || '—'}`,
        raw: data,
        resolved: true,
      }]
      return
    }
    if (q.length < 2) { searchResults.value = []; return }
    const [employees, visitors] = await Promise.all([
      getEmployees({ name: q, pageSize: 10 }).catch(() => ({ data: [] })),
      getVisitorDirectory({ query: q, pageSize: 10, registrationStatus: 'Approved' }).catch(() => ({ data: { items: [] } })),
    ])
    const employeeItems = (employees.data?.items || employees.data || []).filter(Boolean).map(e => ({
      subjectType: 'employee', id: e.employeeId, fullName: e.fullName || e.name || 'Không rõ tên',
      detail: `Mã NV: ${e.employeeId}${e.department ? ` · ${e.department}` : ''}`, raw: e,
    }))
    const visitorItems = (visitors.data?.items || []).filter(Boolean).map(v => ({
      subjectType: 'visitor', id: v.visitorDetailId, fullName: v.fullName || 'Không rõ tên',
      detail: `Khách · SĐT: ${v.guestPhone || '—'} · Host: ${v.hostEmployeeName || '—'}`, raw: v,
    }))
    searchResults.value = [...employeeItems, ...visitorItems]
  } catch {
    // A non-existent short code is a normal operator input, not an application error.
    searchResults.value = []
  } finally {
    searching.value = false
  }
}

async function pickSubject(item) {
  searchResults.value = []
  searchQuery.value = item.fullName
  const isEmployee = item.subjectType === 'employee'
  const data = item.raw
  subject.value = isEmployee
    ? { subjectType: 'employee', displayName: data.fullName || data.name, idValue: data.employeeId || data.subjectId, employeeId: data.employeeId || data.subjectId, department: data.department || data.departmentName, faceImageUrl: data.faceImageUrl }
    : { subjectType: 'visitor', displayName: data.fullName, idValue: data.visitorDetailId || data.subjectId, visitorDetailId: data.visitorDetailId || data.subjectId, guestPhone: data.guestPhone, hostEmployeeName: data.hostEmployeeName, idCardNumber: data.idCardNumber }
  resultOk.value = null; resultMsg.value = ''; errorMsg.value = ''
  if (isEmployee && data?.faceImageUrl && !data.faceImageUrl.startsWith('http')) {
    try { const r = await getProtectedFaceImage(data.employeeId); faceImg.value = URL.createObjectURL(r.data) } catch { faceImg.value = '' }
  } else { faceImg.value = '' }
}

function clearSubject() {
  if (faceImg.value) { URL.revokeObjectURL(faceImg.value); faceImg.value = '' }
  subject.value = null; searchQuery.value = ''; searchResults.value = []
  resultOk.value = null; resultMsg.value = ''; errorMsg.value = ''
}

async function submitDecision(allow) {
  busy.value = true; errorMsg.value = ''
  const payload = { gateId: Number(gateId.value), isDenied: !allow, reason: allow ? 'Bảo vệ xác nhận đúng người' : 'Bảo vệ xác nhận không đúng người' }
  if (subject.value?.employeeId) payload.employeeId = subject.value.employeeId
  else if (subject.value?.visitorDetailId) payload.visitorDetailId = subject.value.visitorDetailId
  try {
    const res = await http.post('/QrAccess/manual-access', payload)
    resultOk.value = true; resultMsg.value = allow ? 'Cho phép' : 'Từ chối'
  } catch (e) {
    resultOk.value = false
    resultMsg.value = allow
      ? (e?.response?.data?.message || 'Từ chối — người này không có quyền vào khu vực này')
      : 'Từ chối'
  } finally {
    busy.value = false
  }
}

function fullReset() {
  gateId.value = ''; clearSubject(); errorMsg.value = ''; logResult.value = { show: false, ok: false, title: '', message: '' }
}
</script>

<style scoped>
.maf-header { display: flex; align-items: center; gap: 12px; margin-bottom: 1.25rem; }
.maf-header-icon { font-size: 2rem; }
.search-box { position: relative; }
.dropdown { position: absolute; background: var(--surface-default); border: 1px solid var(--border-subtle); border-radius: 8px; width: 100%; max-height: 220px; overflow-y: auto; z-index: 100; box-shadow: var(--shadow-sm); }
.dropdown-item { padding: 10px; cursor: pointer; font-size: 14px; transition: background var(--transition-fast); }
.dropdown-item:hover { background: var(--surface-hover); }
.maf-result-title { display: flex; align-items: center; justify-content: space-between; gap: 10px; }
.maf-result-type { flex: 0 0 auto; padding: 3px 8px; border-radius: 999px; font-size: 11px; font-weight: 800; }
.maf-result-type.employee { color: #075985; background: #e0f2fe; }
.maf-result-type.visitor { color: #166534; background: #dcfce7; }

.maf-photo-card { display: flex; align-items: center; gap: 14px; padding: 1rem; background: var(--surface-subtle); border: 3px solid var(--border-subtle); border-radius: 16px; margin-top: .75rem; position: relative; transition: border-color .25s, box-shadow .25s; }
.maf-photo-card.border-ok { border-color: var(--status-success-text); box-shadow: 0 0 0 3px rgba(34,197,94,0.2); }
.maf-photo-card.border-fail { border-color: var(--status-danger-text); box-shadow: 0 0 0 3px rgba(239,68,68,0.2); }
.maf-photo-col { flex-shrink: 0; }
.maf-photo { width: 80px; height: 80px; border-radius: 50%; object-fit: cover; border: 2px solid var(--border-default); }
.maf-photo-fallback { width: 80px; height: 80px; border-radius: 50%; background: var(--accent-primary); color: var(--text-on-interactive); display: flex; align-items: center; justify-content: center; font-size: 28px; font-weight: 900; }
.maf-info-col { flex: 1; min-width: 0; }
.maf-name { font-weight: 800; font-size: 18px; }
.maf-id { font-size: 14px; color: var(--text-secondary); margin-top: 2px; }
.maf-extra { font-size: 13px; color: var(--text-muted); margin-top: 2px; }
.maf-badge { position: absolute; top: 10px; right: 40px; padding: 4px 12px; border-radius: 999px; font-size: 13px; font-weight: 800; color: var(--text-on-interactive); }
.badge-ok { background: var(--status-success-text); }
.badge-fail { background: var(--status-danger-text); }
.maf-close { position: absolute; top: 8px; right: 8px; width: 28px; height: 28px; border-radius: 50%; border: 1px solid var(--border-subtle); background: var(--surface-default); cursor: pointer; font-size: 14px; display: grid; place-items: center; color: var(--text-disabled); transition: background var(--transition-fast), color var(--transition-fast), border-color var(--transition-fast); }
.maf-close:hover:not(:disabled) { background: var(--surface-hover); color: var(--accent-primary); border-color: var(--border-default); }
.maf-close:disabled { opacity: .5; }

.qr-input-row { display: flex; gap: 8px; }

.maf-actions { display: flex; gap: 10px; margin-top: 1.25rem; }
.maf-btn { flex: 1; height: 52px; font-size: 16px; font-weight: 800; border: none; border-radius: 12px; cursor: pointer; color: var(--text-on-interactive); transition: transform var(--transition-fast), box-shadow var(--transition-fast), background var(--transition-fast); }
.maf-btn:hover:not(:disabled) { transform: translateY(-1px); box-shadow: var(--shadow-sm); }
.maf-btn:disabled { opacity: .5; cursor: not-allowed; }
.maf-btn-allow { background: var(--status-success-text); }
.maf-btn-deny { background: var(--status-danger-text); }

.maf-mask { position: fixed; inset: 0; background: var(--surface-overlay); z-index: 300; display: grid; place-items: center; }
.maf-dialog { width: min(420px, 92vw); background: var(--surface-default); border-radius: 14px; padding: 2rem; text-align: center; }
.maf-dialog.maf-allow { border: 2px solid var(--status-success-text); }
.maf-dialog.maf-deny { border: 2px solid var(--status-danger-text); }
.maf-dialog h2 { margin: .5rem 0; }
</style>
