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

      <div class="maf-tabs">
        <button :class="['maf-tab', { active: tab === 'employee' }]" :disabled="busy" @click="switchTab('employee')">Nhân viên</button>
        <button :class="['maf-tab', { active: tab === 'visitor' }]" :disabled="busy" @click="switchTab('visitor')">Khách</button>
      </div>

      <!-- Employee: name search + QR code -->
      <template v-if="tab === 'employee'">
        <div class="form-group">
          <label>Tên hoặc mã nhân viên</label>
          <div class="search-box">
            <input v-model="empQ" type="text" class="form-control" placeholder="Gõ tên hoặc mã NV..." :disabled="busy" @input="onEmpSearch" />
            <div v-if="empResults.length" class="dropdown">
              <div v-for="e in empResults" :key="e.employeeId" class="dropdown-item" @click="pickEmp(e)">
                <strong>{{ e.fullName || e.name }}</strong>
                <div class="text-muted" style="font-size:12px;">Mã: {{ e.employeeId }} &middot; {{ e.department || '' }}</div>
              </div>
            </div>
          </div>
        </div>
      </template>

      <!-- Visitor: name search only -->
      <template v-if="tab === 'visitor'">
        <div class="form-group">
          <label>Tên khách</label>
          <div class="search-box">
            <input v-model="visQ" type="text" class="form-control" placeholder="Gõ tên khách..." :disabled="busy" @input="onVisSearch" />
            <div v-if="visResults.length" class="dropdown">
              <div v-for="v in visResults" :key="v.visitorDetailId" class="dropdown-item" @click="pickVis(v)">
                <strong>{{ v.fullName }}</strong>
                <div class="text-muted" style="font-size:12px;">SĐT: {{ v.guestPhone || '—' }} &middot; Host: {{ v.hostEmployeeName || '—' }}</div>
              </div>
            </div>
          </div>
        </div>
        <div v-if="!subject && visQ.length >= 2 && !visLoading && !visResults.length" class="text-muted" style="margin-top:6px;font-size:13px;">
          Không tìm thấy khách phù hợp.
        </div>
      </template>

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
        </div>
        <div v-if="resultMsg" class="maf-badge" :class="resultOk ? 'badge-ok' : 'badge-fail'">{{ resultMsg }}</div>
        <button class="maf-close" :disabled="busy" @click="clearSubject">✕</button>
      </div>

      <!-- QR code input (employee only) -->
      <div v-if="tab === 'employee' && subject" class="form-group" style="margin-top:1rem;">
        <label>Mã xác nhận (QR động)</label>
        <div class="qr-input-row">
          <input v-model="qrCode" type="text" class="form-control" placeholder="Nhập mã QR từ điện thoại..." :disabled="busy" @keyup.enter="verify" />
          <button class="btn btn-primary" style="flex-shrink:0;padding:0 20px;height:44px;" :disabled="!canVerify || busy" @click="verify">
            {{ busy ? '...' : 'Xác thực' }}
          </button>
        </div>
      </div>

      <!-- Visitor: allow/deny buttons -->
      <div v-if="tab === 'visitor' && subject && !resultMsg" class="maf-actions">
        <button class="maf-btn maf-btn-allow" :disabled="!gateId || busy" @click="submitVisitor(true)">✅ Cho phép</button>
        <button class="maf-btn maf-btn-deny" :disabled="!gateId || busy" @click="submitVisitor(false)">❌ Từ chối</button>
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
import { verifyDynamicQr } from '../services/dynamicQrVerifyApi'
import http from '../services/http'

const gates = ref([])
const gateId = ref('')
const tab = ref('employee')
const busy = ref(false)
const errorMsg = ref('')

const empQ = ref('')
const empResults = ref([])

const visQ = ref('')
const visResults = ref([])
const visLoading = ref(false)

const subject = ref(null)
const faceImg = ref('')
const qrCode = ref('')

const resultOk = ref(null)
const resultMsg = ref('')

const logResult = ref({ show: false, ok: false, title: '', message: '' })

const initials = computed(() => {
  const s = subject.value; if (!s) return ''
  const parts = (s.displayName || '').split(/\s+/)
  return parts.length > 1 ? (parts[0][0] + parts[parts.length - 1][0]).toUpperCase() : (parts[0]?.[0] || '').toUpperCase()
})
const idLabel = computed(() => tab.value === 'employee' ? 'Mã NV' : 'Mã KH')
const extraInfo = computed(() => {
  const s = subject.value; if (!s) return ''
  return tab.value === 'employee' ? (s.department || '') : `SĐT: ${s.guestPhone || '—'} · Host: ${s.hostEmployeeName || '—'}`
})
const photoClass = computed(() => {
  if (resultOk.value === null) return ''
  return resultOk.value ? 'border-ok' : 'border-fail'
})
const canVerify = computed(() => !!gateId.value && !!subject.value && qrCode.value?.trim()?.length > 0)

onMounted(async () => {
  try { const r = await getGates(); gates.value = r.data || [] } catch (e) { console.error(e) }
})
onBeforeUnmount(() => { if (faceImg.value) URL.revokeObjectURL(faceImg.value) })

function switchTab(t) {
  tab.value = t; clearSubject(); qrCode.value = ''; resultOk.value = null; resultMsg.value = ''; errorMsg.value = ''
}

function onEmpSearch() {
  const q = empQ.value?.trim()
  if (!q || q.length < 2) { empResults.value = []; return }
  getEmployees({ name: q, pageSize: 10 }).then(r => {
    empResults.value = (r.data?.items || r.data || []).filter(Boolean)
  }).catch(() => { empResults.value = [] })
}

async function pickEmp(e) {
  empResults.value = []
  empQ.value = e.fullName || e.name || ''
  subject.value = { displayName: e.fullName || e.name, idValue: e.employeeId, employeeId: e.employeeId, department: e.department, faceImageUrl: e.faceImageUrl }
  resultOk.value = null; resultMsg.value = ''; qrCode.value = ''; errorMsg.value = ''
  if (e?.faceImageUrl && !e.faceImageUrl.startsWith('http')) {
    try { const r = await getProtectedFaceImage(e.employeeId); faceImg.value = URL.createObjectURL(r.data) } catch { faceImg.value = '' }
  } else { faceImg.value = '' }
}

function onVisSearch() {
  const q = visQ.value?.trim()
  if (!q || q.length < 2) { visResults.value = []; return }
  visLoading.value = true
  getVisitorDirectory({ query: q, pageSize: 10, registrationStatus: 'Approved' }).then(r => {
    visResults.value = (r.data?.items || []).filter(Boolean)
  }).catch(() => { visResults.value = [] }).finally(() => { visLoading.value = false })
}

function pickVis(v) {
  visResults.value = []
  visQ.value = v.fullName || ''
  subject.value = { displayName: v.fullName, idValue: v.visitorDetailId, visitorDetailId: v.visitorDetailId, guestPhone: v.guestPhone, hostEmployeeName: v.hostEmployeeName }
  resultOk.value = null; resultMsg.value = ''; errorMsg.value = ''
}

function clearSubject() {
  if (faceImg.value) { URL.revokeObjectURL(faceImg.value); faceImg.value = '' }
  subject.value = null; empQ.value = ''; empResults.value = []; visQ.value = ''; visResults.value = []
  resultOk.value = null; resultMsg.value = ''; qrCode.value = ''; errorMsg.value = ''
}

async function verify() {
  if (!canVerify.value) return
  busy.value = true; errorMsg.value = ''; resultOk.value = null; resultMsg.value = ''
  try {
    const data = await verifyDynamicQr(qrCode.value.trim(), 'manual-fallback')
    if (data?.success && data?.data?.employeeId === subject.value?.employeeId) {
      resultOk.value = true
      resultMsg.value = 'Cho phép'
      await logGateAccess(false)
    } else {
      resultOk.value = false
      resultMsg.value = 'Từ chối — QR không khớp'
      await logGateAccess(true)
    }
  } catch (e) {
    resultOk.value = false
    resultMsg.value = 'Từ chối — QR không hợp lệ'
    await logGateAccess(true)
  } finally {
    busy.value = false
  }
}

async function logGateAccess(isDenied) {
  const payload = { gateId: Number(gateId.value), isDenied, reason: resultMsg.value || null }
  if (subject.value?.employeeId) payload.employeeId = subject.value.employeeId
  else if (subject.value?.visitorDetailId) payload.visitorDetailId = subject.value.visitorDetailId
  try {
    await http.post('/QrAccess/manual-access', payload)
  } catch { /* log best-effort */ }
}

async function submitVisitor(allow) {
  busy.value = true; errorMsg.value = ''
  const payload = { gateId: Number(gateId.value), isDenied: !allow, reason: allow ? 'Bảo vệ cho phép' : 'Bảo vệ từ chối' }
  if (subject.value?.visitorDetailId) payload.visitorDetailId = subject.value.visitorDetailId
  try {
    await http.post('/QrAccess/manual-access', payload)
    resultOk.value = allow; resultMsg.value = allow ? 'Cho phép' : 'Từ chối'
  } catch (e) {
    errorMsg.value = e?.response?.data?.message || e?.message || 'Lỗi'
  } finally {
    busy.value = false
  }
}

function fullReset() {
  gateId.value = ''; clearSubject(); qrCode.value = ''; errorMsg.value = ''; logResult.value = { show: false, ok: false, title: '', message: '' }
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

.maf-photo-card { display: flex; align-items: center; gap: 14px; padding: 1rem; background: #f8faff; border: 3px solid #dde6f0; border-radius: 16px; margin-top: .75rem; position: relative; transition: border-color .25s, box-shadow .25s; }
.maf-photo-card.border-ok { border-color: #22c55e; box-shadow: 0 0 0 3px rgba(34,197,94,0.2); }
.maf-photo-card.border-fail { border-color: #ef4444; box-shadow: 0 0 0 3px rgba(239,68,68,0.2); }
.maf-photo-col { flex-shrink: 0; }
.maf-photo { width: 80px; height: 80px; border-radius: 50%; object-fit: cover; border: 2px solid #cbd5e1; }
.maf-photo-fallback { width: 80px; height: 80px; border-radius: 50%; background: #6a8fe8; color: #fff; display: flex; align-items: center; justify-content: center; font-size: 28px; font-weight: 900; }
.maf-info-col { flex: 1; min-width: 0; }
.maf-name { font-weight: 800; font-size: 18px; }
.maf-id { font-size: 14px; color: #475569; margin-top: 2px; }
.maf-extra { font-size: 13px; color: #64748b; margin-top: 2px; }
.maf-badge { position: absolute; top: 10px; right: 40px; padding: 4px 12px; border-radius: 999px; font-size: 13px; font-weight: 800; color: #fff; }
.badge-ok { background: #22c55e; }
.badge-fail { background: #ef4444; }
.maf-close { position: absolute; top: 8px; right: 8px; width: 28px; height: 28px; border-radius: 50%; border: 1px solid #e2e8f0; background: #fff; cursor: pointer; font-size: 14px; display: grid; place-items: center; color: #94a3b8; }
.maf-close:disabled { opacity: .5; }

.qr-input-row { display: flex; gap: 8px; }

.maf-actions { display: flex; gap: 10px; margin-top: 1.25rem; }
.maf-btn { flex: 1; height: 52px; font-size: 16px; font-weight: 800; border: none; border-radius: 12px; cursor: pointer; color: #fff; }
.maf-btn:disabled { opacity: .5; cursor: not-allowed; }
.maf-btn-allow { background: #22c55e; }
.maf-btn-deny { background: #ef4444; }

.maf-mask { position: fixed; inset: 0; background: rgba(2,6,23,0.45); z-index: 300; display: grid; place-items: center; }
.maf-dialog { width: min(420px, 92vw); background: #fff; border-radius: 14px; padding: 2rem; text-align: center; }
.maf-dialog.maf-allow { border: 2px solid #22c55e; }
.maf-dialog.maf-deny { border: 2px solid #ef4444; }
.maf-dialog h2 { margin: .5rem 0; }
</style>
