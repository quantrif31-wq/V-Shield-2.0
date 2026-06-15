<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Contractor Management</span>
                <h1 class="page-title">Contractors</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="showAddModal = true">Add Contractor</button>
                <button class="btn btn-secondary" :disabled="loading" @click="loadAll">Refresh</button>
            </div>
        </div>

        <section class="metric-grid four">
            <article class="metric-tile"><span class="metric-label">Active</span><strong class="metric-value">{{ stats.active }}</strong></article>
            <article class="metric-tile"><span class="metric-label">Expiring</span><strong class="metric-value">{{ stats.expiring }}</strong></article>
            <article class="metric-tile"><span class="metric-label">Expired</span><strong class="metric-value">{{ stats.expired }}</strong></article>
            <article class="metric-tile"><span class="metric-label">Revoked</span><strong class="metric-value">{{ stats.revoked }}</strong></article>
        </section>

        <div class="toolbar-shell">
            <div class="search-bar">
                <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <circle cx="11" cy="11" r="8" /><path d="M21 21l-4.35-4.35" />
                </svg>
                <input v-model="searchQuery" type="text" placeholder="Search by name or company..." @input="loadAll" />
            </div>
            <select v-model="statusFilter" class="form-control" style="width: auto;" @change="loadAll">
                <option value="">All statuses</option>
                <option value="Active">Active</option>
                <option value="Expiring">Expiring</option>
                <option value="Expired">Expired</option>
                <option value="Revoked">Revoked</option>
            </select>
        </div>

        <section class="ops-panel">
            <div v-if="loading" class="empty-card">Loading contractors...</div>
            <div v-else-if="contractors.length === 0" class="empty-card">No contractors found.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr><th>Name</th><th>Company</th><th>Contract</th><th>Status</th><th>Training</th><th>Parking</th><th>Actions</th></tr>
                    </thead>
                    <tbody>
                        <tr v-for="c in contractors" :key="c.contractorId">
                            <td>
                                <strong>{{ c.fullName }}</strong>
                                <div class="text-muted">{{ c.phone || c.email }}</div>
                            </td>
                            <td>{{ c.company }}</td>
                            <td>
                                <div>{{ formatDate(c.contractFromUtc) }}</div>
                                <div class="text-muted">→ {{ formatDate(c.contractToUtc) }}</div>
                            </td>
                            <td><span class="soft-chip" :class="statusClass(c.status)">{{ c.status }}</span></td>
                            <td>
                                <span v-if="c.requiredTraining" class="text-muted">{{ c.requiredTraining }}</span>
                                <span v-else class="text-muted">—</span>
                            </td>
                            <td>
                                <button v-if="c.status === 'Active'" class="btn btn-sm btn-ghost" @click="openContractorParking(c)">Permit</button>
                                <span v-else class="text-muted">—</span>
                            </td>
                            <td>
                                <div class="chip-row">
                                    <button class="btn btn-sm btn-ghost" @click="viewDetail(c)">Detail</button>
                                    <button v-if="c.status === 'Active' || c.status === 'Expiring'" class="btn btn-sm btn-danger" @click="confirmRevoke(c)">Revoke</button>
                                </div>
                            </td>
                        </tr>
                    </tbody>
                </table>
                <div v-if="totalPages > 1" class="pagination-bar">
                    <button :disabled="page <= 1" @click="page--; loadAll()">Prev</button>
                    <span>{{ page }} / {{ totalPages }}</span>
                    <button :disabled="page >= totalPages" @click="page++; loadAll()">Next</button>
                </div>
            </div>
        </section>

        <Teleport to="body">
            <div v-if="showAddModal" class="modal-overlay" @click.self="showAddModal = false">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Add Contractor</h2>
                        <button class="btn-close" @click="showAddModal = false">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="form-row two">
                            <div class="form-group">
                                <label>Full Name *</label>
                                <input v-model="form.fullName" type="text" class="form-control" />
                            </div>
                            <div class="form-group">
                                <label>Company *</label>
                                <input v-model="form.company" type="text" class="form-control" />
                            </div>
                        </div>
                        <div class="form-row two">
                            <div class="form-group">
                                <label>Phone</label>
                                <input v-model="form.phone" type="text" class="form-control" />
                            </div>
                            <div class="form-group">
                                <label>Email</label>
                                <input v-model="form.email" type="email" class="form-control" />
                            </div>
                        </div>
                        <div class="form-row two">
                            <div class="form-group">
                                <label>Contract From *</label>
                                <input v-model="form.contractFrom" type="date" class="form-control" />
                            </div>
                            <div class="form-group">
                                <label>Contract To *</label>
                                <input v-model="form.contractTo" type="date" class="form-control" />
                            </div>
                        </div>
                        <div class="form-group">
                            <label>Site</label>
                            <select v-model="form.siteId" class="form-control">
                                <option :value="null">— Select —</option>
                                <option v-for="s in sites" :key="s.siteId || s.id" :value="s.siteId || s.id">{{ s.name }}</option>
                            </select>
                        </div>
                        <div class="form-group">
                            <label>Required Training</label>
                            <textarea v-model="form.requiredTraining" class="form-control" rows="2" placeholder="Safety induction, site-specific training..."></textarea>
                        </div>
                        <div v-if="formError" class="alert alert-danger">{{ formError }}</div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="showAddModal = false">Cancel</button>
                        <button class="btn btn-primary" :disabled="saving" @click="submitContractor">{{ saving ? 'Adding...' : 'Add' }}</button>
                    </div>
                </div>
            </div>

            <div v-if="revokeTarget" class="modal-overlay" @click.self="revokeTarget = null">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Revoke Contractor</h2>
                        <button class="btn-close" @click="revokeTarget = null">&times;</button>
                    </div>
                    <div class="modal-body">
                        <p>Revoke access for <strong>{{ revokeTarget.fullName }}</strong> ({{ revokeTarget.company }})?</p>
                        <div class="form-group">
                            <label>Reason *</label>
                            <textarea v-model="revokeReason" class="form-control" rows="3" placeholder="Required reason for revocation"></textarea>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="revokeTarget = null">Cancel</button>
                        <button class="btn btn-danger" :disabled="saving || !revokeReason" @click="submitRevoke">{{ saving ? 'Revoking...' : 'Revoke Access' }}</button>
                    </div>
                </div>
            </div>

            <!-- Contractor Parking Permit Modal -->
            <div v-if="parkingTarget" class="modal-overlay" @click.self="parkingTarget = null">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Parking Permit — {{ parkingTarget.fullName }}</h2>
                        <button class="btn-close" @click="parkingTarget = null">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div v-if="parkingLoading" class="empty-card">Loading parking areas...</div>
                        <div v-else>
                            <div class="form-group">
                                <label>Parking Area</label>
                                <select v-model="parkingForm.areaId" class="form-control">
                                    <option :value="null">— Select —</option>
                                    <option v-for="pa in parkingAreas" :key="pa.parkingAreaId || pa.id" :value="pa.parkingAreaId || pa.id">{{ pa.name }} ({{ pa.availableSpots ?? '?' }} spots)</option>
                                </select>
                            </div>
                            <div class="form-row two">
                                <div class="form-group">
                                    <label>Valid From</label>
                                    <input v-model="parkingForm.from" type="date" class="form-control" />
                                </div>
                                <div class="form-group">
                                    <label>Valid To</label>
                                    <input v-model="parkingForm.to" type="date" class="form-control" />
                                </div>
                            </div>
                            <div class="form-group">
                                <label>Vehicle Plate</label>
                                <input v-model="parkingForm.plate" type="text" class="form-control" placeholder="e.g. 29A-12345" />
                            </div>
                            <div v-if="parkingDone" class="alert alert-success">{{ parkingDone }}</div>
                            <div v-else-if="parkingError" class="alert alert-danger">{{ parkingError }}</div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="parkingTarget = null">Close</button>
                        <button class="btn btn-primary" :disabled="parkingSaving || !parkingForm.areaId" @click="submitContractorParking">
                            {{ parkingSaving ? 'Issuing...' : 'Issue Permit' }}
                        </button>
                    </div>
                </div>
            </div>
        </Teleport>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const loading = ref(false)
const saving = ref(false)
const contractors = ref([])
const page = ref(1)
const totalPages = ref(1)
const searchQuery = ref('')
const statusFilter = ref('')
const sites = ref([])
const showAddModal = ref(false)
const revokeTarget = ref(null)
const revokeReason = ref('')
const formError = ref('')

// Parking
const parkingTarget = ref(null)
const parkingLoading = ref(false)
const parkingAreas = ref([])
const parkingSaving = ref(false)
const parkingError = ref('')
const parkingDone = ref('')
const parkingForm = ref({ areaId: null, from: '', to: '', plate: '' })

const form = reactive({
    fullName: '', company: '', phone: '', email: '',
    contractFrom: '', contractTo: '', siteId: null, requiredTraining: '',
})

const stats = reactive({ active: 0, expiring: 0, expired: 0, revoked: 0 })

function statusClass(s) {
    return s === 'Active' ? 'success' : s === 'Expiring' ? 'warn' : s === 'Expired' ? 'danger' : 'muted'
}

function formatDate(utc) {
    if (!utc) return '—'
    return new Date(utc).toLocaleDateString('vi-VN')
}

async function loadAll() {
    loading.value = true
    try {
        const params = { page: page.value, pageSize: 25 }
        if (statusFilter.value) params.status = statusFilter.value
        if (searchQuery.value) params.search = searchQuery.value
        const res = await enterpriseApi.getContractors(params)
        const data = res.data || {}
        contractors.value = data.items || []
        totalPages.value = Math.ceil((data.total || 0) / 25)

        const all = await enterpriseApi.getContractors({ pageSize: 1000 })
        const allData = all.data?.items || []
        stats.active = allData.filter(c => c.status === 'Active').length
        stats.expiring = allData.filter(c => c.status === 'Expiring').length
        stats.expired = allData.filter(c => c.status === 'Expired').length
        stats.revoked = allData.filter(c => c.status === 'Revoked').length
    } catch (e) {
        console.error('Failed to load contractors', e)
    } finally {
        loading.value = false
    }
}

function viewDetail(c) {
    alert(`Contractor: ${c.fullName}\nCompany: ${c.company}\nContract: ${formatDate(c.contractFromUtc)} → ${formatDate(c.contractToUtc)}\nStatus: ${c.status}\nTraining: ${c.requiredTraining || '—'}`)
}

function confirmRevoke(c) {
    revokeTarget.value = c
    revokeReason.value = ''
}

async function submitRevoke() {
    if (!revokeTarget.value || !revokeReason.value) return
    saving.value = true
    try {
        await enterpriseApi.revokeContractor(revokeTarget.value.contractorId, { reason: revokeReason.value })
        revokeTarget.value.status = 'Revoked'
        revokeTarget.value = null
        await loadAll()
    } catch (e) {
        alert('Revoke failed: ' + (e.response?.data?.message || e.message))
    } finally {
        saving.value = false
    }
}

async function submitContractor() {
    if (!form.fullName || !form.company) { formError.value = 'Name and company are required.'; return }
    if (!form.contractFrom || !form.contractTo) { formError.value = 'Contract dates are required.'; return }
    formError.value = ''
    saving.value = true
    try {
        await enterpriseApi.createContractor({
            fullName: form.fullName,
            company: form.company,
            phone: form.phone || null,
            email: form.email || null,
            contractFromUtc: new Date(form.contractFrom).toISOString(),
            contractToUtc: new Date(form.contractTo).toISOString(),
            siteId: form.siteId,
            requiredTraining: form.requiredTraining || null,
        })
        showAddModal.value = false
        Object.assign(form, { fullName: '', company: '', phone: '', email: '', contractFrom: '', contractTo: '', siteId: null, requiredTraining: '' })
        await loadAll()
    } catch (e) {
        formError.value = e.response?.data?.message || e.message
    } finally {
        saving.value = false
    }
}

// --- Parking ---
async function openContractorParking(c) {
    parkingTarget.value = c
    parkingForm.value = { areaId: null, from: '', to: '', plate: '' }
    parkingError.value = ''
    parkingDone.value = ''
    parkingLoading.value = true
    try {
        const res = await enterpriseApi.getParkingAreas({ pageSize: 50 })
        parkingAreas.value = res.data?.items || []
    } catch (e) {
        console.error('Failed to load parking areas', e)
    } finally {
        parkingLoading.value = false
    }
}

async function submitContractorParking() {
    if (!parkingTarget.value || !parkingForm.value.areaId) return
    parkingSaving.value = true
    parkingError.value = ''
    parkingDone.value = ''
    try {
        // For contractor, we create a parking permit with contractorId in payload
        const from = parkingForm.value.from ? new Date(parkingForm.value.from).toISOString() : new Date().toISOString()
        const to = parkingForm.value.to ? new Date(parkingForm.value.to).toISOString() : new Date(Date.now() + 30 * 24 * 3600000).toISOString()
        await enterpriseApi.createParkingPermit({
            contractorId: parkingTarget.value.contractorId,
            parkingAreaId: parkingForm.value.areaId,
            validFromUtc: from,
            validToUtc: to,
            plateNumber: parkingForm.value.plate || null,
        })
        parkingDone.value = `Parking permit issued for ${parkingTarget.value.fullName}!`
        parkingForm.value = { areaId: null, from: '', to: '', plate: '' }
    } catch (e) {
        parkingError.value = e.response?.data?.message || e.message
    } finally {
        parkingSaving.value = false
    }
}

onMounted(loadAll)
</script>
