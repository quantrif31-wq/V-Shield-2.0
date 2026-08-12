<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Cổng chủ nhà</span>
                <h1 class="page-title">Mời khách đến thăm</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="showForm = true" v-if="!showForm">Lời mời mới</button>
                <button class="btn btn-secondary" @click="showFormTemplates = true">Biểu mẫu</button>
            </div>
        </div>

        <section v-if="showForm" class="ops-panel">
            <h2 class="panel-title">Tạo lời mời khách</h2>
            <div class="form-group">
                <label>Tên khách *</label>
                <input v-model="form.name" type="text" class="form-control" placeholder="Họ và tên" />
            </div>
            <div class="form-row two">
                <div class="form-group">
                    <label>Số điện thoại</label>
                    <input v-model="form.phone" type="text" class="form-control" />
                </div>
                <div class="form-group">
                    <label>Email</label>
                    <input v-model="form.email" type="email" class="form-control" />
                </div>
            </div>
            <div class="form-row two">
                <div class="form-group">
                    <label>Ngày/giờ đến dự kiến *</label>
                    <input v-model="form.expectedIn" type="datetime-local" class="form-control" />
                </div>
                <div class="form-group">
                    <label>Ngày/giờ rời đi dự kiến *</label>
                    <input v-model="form.expectedOut" type="datetime-local" class="form-control" />
                </div>
            </div>
            <div class="form-group">
                <label>Khu vực</label>
                <select v-model="form.siteId" class="form-control">
                    <option :value="null">— Chọn khu vực —</option>
                    <option v-for="s in sites" :key="s.siteId" :value="s.siteId">{{ s.name }}</option>
                </select>
            </div>
            <div class="form-row two">
                <label class="checkbox-label"><input v-model="form.ndaRequired" type="checkbox" /> Yêu cầu NDA</label>
                <label class="checkbox-label"><input v-model="form.escortRequired" type="checkbox" /> Yêu cầu hộ tống</label>
                <label class="checkbox-label"><input v-model="form.safetyBriefingRequired" type="checkbox" /> Phổ biến an toàn</label>
                <label class="checkbox-label"><input v-model="form.parkingRequired" type="checkbox" /> Giấy phép đỗ xe</label>
            </div>
            <div v-if="form.parkingRequired" class="form-group">
                <label>Biển số xe</label>
                <input v-model="form.plateNumber" type="text" class="form-control" placeholder="vd. 29A-12345" />
            </div>
            <div v-if="formExtra.ndaTemplate" class="form-group">
                <label>Đính kèm mẫu NDA</label>
                <select v-model="formExtra.selectedNdaTemplateId" class="form-control">
                    <option :value="null">— Không —</option>
                    <option v-for="ft in formTemplates" :key="ft.formTemplateId || ft.id" :value="ft.formTemplateId || ft.id">
                        {{ ft.templateName || ft.name }}
                    </option>
                </select>
            </div>
            <div v-if="formError" class="alert alert-danger">{{ formError }}</div>
            <div v-if="formSuccess" class="alert alert-success">{{ formSuccess }}</div>
            <div class="chip-row">
                <button class="btn btn-secondary" @click="showForm = false">Hủy</button>
                <button class="btn btn-primary" :disabled="saving" @click="submitInvitation">{{ saving ? 'Đang gửi...' : 'Gửi lời mời' }}</button>
            </div>
        </section>

        <section class="ops-panel" style="margin-top: 1rem;">
            <div class="panel-head">
                <h2 class="panel-title">Lời mời của tôi</h2>
            </div>
            <div class="toolbar-shell">
                <div class="search-bar">
                    <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <circle cx="11" cy="11" r="8" /><path d="M21 21l-4.35-4.35" />
                    </svg>
                    <input v-model="searchQuery" type="text" placeholder="Tìm khách..." />
                </div>
            </div>
            <div v-if="loading" class="empty-card">Đang tải...</div>
            <div v-else-if="filteredInvitations.length === 0" class="empty-card">Chưa có lời mời nào.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr><th>Khách</th><th>SĐT</th><th>Thời gian</th><th>Trạng thái</th><th>Thao tác</th></tr>
                    </thead>
                    <tbody>
                        <tr v-for="v in filteredInvitations" :key="v.visitId">
                            <td><strong>{{ v.visitorName }}</strong></td>
                            <td>{{ v.visitorPhone || '—' }}</td>
                            <td>{{ formatDate(v.expectedInUtc) }}</td>
                            <td><span class="soft-chip" :class="statusClass(v.status)">{{ v.status }}</span></td>
                            <td>
                                <div class="chip-row">
                                    <button v-if="v.status === 'Approved' || v.status === 'Invited'" class="btn btn-sm btn-primary" @click="issueCredential(v)">Cấp QR</button>
                                    <button v-if="v.status === 'Approved' || v.status === 'CheckedIn'" class="btn btn-sm btn-secondary" @click="openParkingForm(v)">Đỗ xe</button>
                                    <button class="btn btn-sm btn-ghost" @click="viewDetail(v)">Chi tiết</button>
                                </div>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </section>

        <Teleport to="body">
            <div v-if="credentialVisit" class="modal-overlay" @click.self="credentialVisit = null">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Cấp thẻ truy cập khách</h2>
                        <button class="btn-close" @click="credentialVisit = null">&times;</button>
                    </div>
                    <div class="modal-body">
                        <p>Cấp thẻ QR cho <strong>{{ credentialVisit.visitorName }}</strong></p>
                        <div class="form-row two">
                            <div class="form-group">
                                <label>Có hiệu lực từ</label>
                                <input v-model="credFrom" type="datetime-local" class="form-control" />
                            </div>
                            <div class="form-group">
                                <label>Đến</label>
                                <input v-model="credTo" type="datetime-local" class="form-control" />
                            </div>
                        </div>
                        <div v-if="credSuccess" class="alert alert-success">Đã cấp thẻ! Tham chiếu: {{ credSuccess }}</div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="credentialVisit = null">Đóng</button>
                        <button class="btn btn-primary" :disabled="saving" @click="submitCredential">{{ saving ? 'Đang cấp...' : 'Cấp thẻ' }}</button>
                    </div>
                </div>
            </div>

            <!-- Parking Form Modal -->
            <div v-if="parkingTarget" class="modal-overlay" @click.self="parkingTarget = null">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Giấy phép đỗ xe — {{ parkingTarget.visitorName }}</h2>
                        <button class="btn-close" @click="parkingTarget = null">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="form-group">
                            <label>Khu đỗ xe</label>
                            <select v-model="parkingForm.areaId" class="form-control">
                                <option :value="null">— Chọn —</option>
                                <option v-for="pa in parkingAreas" :key="pa.parkingAreaId || pa.id" :value="pa.parkingAreaId || pa.id">{{ pa.name }}</option>
                            </select>
                        </div>
                        <div class="form-row two">
                            <div class="form-group">
                                <label>Từ</label>
                                <input v-model="parkingForm.from" type="datetime-local" class="form-control" />
                            </div>
                            <div class="form-group">
                                <label>Đến</label>
                                <input v-model="parkingForm.to" type="datetime-local" class="form-control" />
                            </div>
                        </div>
                        <div class="form-group">
                            <label>Biển số xe</label>
                            <input v-model="parkingForm.plate" type="text" class="form-control" placeholder="vd. 29A-12345" />
                        </div>
                        <div v-if="parkingError" class="alert alert-danger">{{ parkingError }}</div>
                        <div v-if="parkingDone" class="alert alert-success">{{ parkingDone }}</div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="parkingTarget = null">Đóng</button>
                        <button class="btn btn-primary" :disabled="parkingSaving || !parkingForm.areaId" @click="submitParking">
                            {{ parkingSaving ? 'Đang cấp...' : 'Cấp giấy phép' }}
                        </button>
                    </div>
                </div>
            </div>

            <!-- Form Templates Modal -->
            <div v-if="showFormTemplates" class="modal-overlay" @click.self="showFormTemplates = false">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Biểu mẫu</h2>
                        <button class="btn-close" @click="showFormTemplates = false">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div v-if="formTemplates.length === 0" class="empty-card">Chưa có biểu mẫu nào.</div>
                        <div v-for="ft in formTemplates" :key="ft.formTemplateId || ft.id" class="template-card">
                            <div><strong>{{ ft.templateName || ft.name }}</strong></div>
                            <div class="text-muted">{{ ft.description || ft.category || '' }}</div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="showFormTemplates = false">Đóng</button>
                    </div>
                </div>
            </div>
        </Teleport>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'
import * as employeeApi from '../services/employeeApi'

const showForm = ref(false)
const loading = ref(false)
const saving = ref(false)
const searchQuery = ref('')
const invitations = ref([])
const sites = ref([])
const credentialVisit = ref(null)
const credFrom = ref('')
const credTo = ref('')
const credSuccess = ref('')
const formError = ref('')
const formSuccess = ref('')

// Parking form
const parkingTarget = ref(null)
const parkingAreas = ref([])
const parkingSaving = ref(false)
const parkingError = ref('')
const parkingDone = ref('')
const parkingForm = ref({ areaId: null, from: '', to: '', plate: '' })

// Form templates
const showFormTemplates = ref(false)
const formTemplates = ref([])

const currentUser = ref(null)
const AUTH_USER_KEY = 'v_shield_user'

const form = ref({
    name: '', phone: '', email: '',
    expectedIn: '', expectedOut: '',
    siteId: null, ndaRequired: false,
    escortRequired: false, safetyBriefingRequired: false,
    parkingRequired: false, plateNumber: '',
})

const formExtra = ref({
    selectedNdaTemplateId: null,
    ndaTemplate: false,
})

const filteredInvitations = computed(() => {
    if (!searchQuery.value) return invitations.value
    const q = searchQuery.value.toLowerCase()
    return invitations.value.filter(v => v.visitorName.toLowerCase().includes(q))
})

function statusClass(s) {
    return s === 'CheckedIn' ? 'success' : s === 'Overstay' ? 'danger' : s === 'Approved' ? 'info' : ''
}

function formatDate(utc) {
    if (!utc) return '—'
    return new Date(utc).toLocaleString('vi-VN')
}

async function loadData() {
    loading.value = true
    try {
        const stored = sessionStorage.getItem(AUTH_USER_KEY) || localStorage.getItem(AUTH_USER_KEY)
        if (stored) {
            const parsed = JSON.parse(stored)
            currentUser.value = parsed
        }
        const empId = currentUser.value?.employeeId
        if (empId) {
            const res = await enterpriseApi.getVisits({ hostEmployeeId: empId, pageSize: 100 })
            invitations.value = res.data?.items || []
        }
        // Load form templates
        try {
            const ftRes = await enterpriseApi.getFormTemplates({ pageSize: 50 })
            formTemplates.value = ftRes.data?.items || []
        } catch (_) {}
    } catch (e) {
        console.error('Failed to load invitations', e)
    } finally {
        loading.value = false
    }
}

async function submitInvitation() {
    if (!form.value.name) { formError.value = 'Tên khách là bắt buộc.'; return }
    if (!form.value.expectedIn || !form.value.expectedOut) { formError.value = 'Thời gian dự kiến là bắt buộc.'; return }
    formError.value = ''
    formSuccess.value = ''
    saving.value = true
    try {
        const stored = sessionStorage.getItem(AUTH_USER_KEY) || localStorage.getItem(AUTH_USER_KEY)
        const empId = stored ? JSON.parse(stored).employeeId : null
        const res = await enterpriseApi.createVisit({
            visitorName: form.value.name,
            visitorPhone: form.value.phone || null,
            visitorEmail: form.value.email || null,
            hostEmployeeId: empId,
            expectedInUtc: new Date(form.value.expectedIn).toISOString(),
            expectedOutUtc: new Date(form.value.expectedOut).toISOString(),
            siteId: form.value.siteId,
            ndaRequired: form.value.ndaRequired,
            escortRequired: form.value.escortRequired,
            safetyBriefingRequired: form.value.safetyBriefingRequired,
        })
        const visitId = res.data?.visitId
        if (visitId && form.value.parkingRequired && form.value.plateNumber) {
            try {
                const from = new Date(form.value.expectedIn).toISOString()
                const to = new Date(form.value.expectedOut).toISOString()
                await enterpriseApi.createParkingPermit({
                    visitId,
                    validFromUtc: from,
                    validToUtc: to,
                    plateNumber: form.value.plateNumber,
                })
            } catch (_) {}
        }
        if (visitId && formExtra.value.selectedNdaTemplateId) {
            try {
                await enterpriseApi.acceptForm(visitId, {
                    formTemplateId: formExtra.value.selectedNdaTemplateId,
                })
            } catch (_) {}
        }
        formSuccess.value = `Đã gửi lời mời tới ${form.value.name}!`
        form.value = { name: '', phone: '', email: '', expectedIn: '', expectedOut: '', siteId: null, ndaRequired: false, escortRequired: false, safetyBriefingRequired: false, parkingRequired: false, plateNumber: '' }
        formExtra.value = { selectedNdaTemplateId: null, ndaTemplate: false }
        showForm.value = false
        await loadData()
    } catch (e) {
        formError.value = e.response?.data?.message || e.message
    } finally {
        saving.value = false
    }
}

function viewDetail(v) {
    alert(`Khách: ${v.visitorName}\nTrạng thái: ${v.status}\nThời gian: ${formatDate(v.expectedInUtc)} → ${formatDate(v.expectedOutUtc)}`)
}

function issueCredential(v) {
    credentialVisit.value = v
    credFrom.value = ''
    credTo.value = ''
    credSuccess.value = ''
}

async function submitCredential() {
    if (!credentialVisit.value) return
    saving.value = true
    try {
        const from = credFrom.value ? new Date(credFrom.value).toISOString() : new Date().toISOString()
        const to = credTo.value ? new Date(credTo.value).toISOString() : new Date(Date.now() + 24 * 3600000).toISOString()
        const res = await enterpriseApi.issueVisitorCredential(credentialVisit.value.visitId, {
            credentialType: 'QR',
            validFromUtc: from,
            validToUtc: to,
        })
        credSuccess.value = res.data?.credentialReference || 'issued'
    } catch (e) {
        alert('Thất bại: ' + (e.response?.data?.message || e.message))
    } finally {
        saving.value = false
    }
}

// --- Parking ---
async function openParkingForm(v) {
    parkingTarget.value = v
    parkingForm.value = { areaId: null, from: '', to: '', plate: '' }
    parkingError.value = ''
    parkingDone.value = ''
    try {
        const res = await enterpriseApi.getParkingAreas({ pageSize: 50 })
        parkingAreas.value = res.data?.items || []
    } catch (e) {
        console.error('Failed to load parking areas', e)
    }
}

async function submitParking() {
    if (!parkingTarget.value || !parkingForm.value.areaId) return
    parkingSaving.value = true
    parkingError.value = ''
    parkingDone.value = ''
    try {
        const from = parkingForm.value.from ? new Date(parkingForm.value.from).toISOString() : new Date().toISOString()
        const to = parkingForm.value.to ? new Date(parkingForm.value.to).toISOString() : new Date(Date.now() + 24 * 3600000).toISOString()
        await enterpriseApi.createParkingPermit({
            visitId: parkingTarget.value.visitId,
            parkingAreaId: parkingForm.value.areaId,
            validFromUtc: from,
            validToUtc: to,
            plateNumber: parkingForm.value.plate || null,
        })
        parkingDone.value = 'Đã cấp giấy phép đỗ xe!'
    } catch (e) {
        parkingError.value = e.response?.data?.message || e.message
    } finally {
        parkingSaving.value = false
    }
}

onMounted(loadData)
</script>

<style scoped>
.template-card {
    padding: 10px 12px;
    border: 1px solid var(--border-subtle);
    border-radius: 8px;
    margin-bottom: 8px;
}
</style>
