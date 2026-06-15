<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Device provisioning</span>
                <h1 class="page-title">Provisioning Wizard</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="showRequestForm = true">New Request</button>
                <button class="btn btn-secondary" @click="showCreateDevice = true">Create Device</button>
                <button class="btn btn-secondary" @click="showRegisterController = true">Register Controller</button>
            </div>
        </div>

        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Requests</span><h2 class="panel-title">Provisioning Requests</h2></div>
                    <div class="panel-actions">
                        <select v-model="statusFilter" @change="loadRequests" class="form-select">
                            <option value="">All</option>
                            <option value="Pending">Pending</option>
                            <option value="Approved">Approved</option>
                        </select>
                    </div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else-if="requests.length === 0" class="empty-card">No provisioning requests.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Device</th><th>Type</th><th>Status</th><th>Approval Note</th><th>Actions</th></tr></thead>
                        <tbody>
                            <tr v-for="r in requests" :key="r.deviceProvisioningRequestId">
                                <td>{{ r.requestedName }}</td>
                                <td>{{ r.deviceType }}</td>
                                <td><span class="badge" :class="r.status === 'Approved' ? 'badge-success' : 'badge-warn'">{{ r.status }}</span></td>
                                <td class="table-sub">{{ r.approvalNote || '—' }}</td>
                                <td>
                                    <button v-if="r.status === 'Pending'" class="btn btn-success btn-sm" @click="approve(r)">Approve</button>
                                    <button v-if="r.status === 'Approved'" class="btn btn-primary btn-sm" @click="finalizeProvisioning(r)">Finalize</button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Devices</span><h2 class="panel-title">Registered Devices</h2></div>
                </div>
                <div class="toolbar-shell">
                    <div class="search-bar">
                        <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <circle cx="11" cy="11" r="8" /><path d="M21 21l-4.35-4.35" />
                        </svg>
                        <input v-model="deviceSearch" type="text" placeholder="Search devices..." />
                    </div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else-if="filteredDevices.length === 0" class="empty-card">No devices registered.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Name</th><th>Type</th><th>Vendor</th><th>Status</th><th>Actions</th></tr></thead>
                        <tbody>
                            <tr v-for="d in filteredDevices" :key="d.securityDeviceId" class="device-row" @click="selectedDevice = d">
                                <td>{{ d.name }}</td>
                                <td>{{ d.deviceType }}</td>
                                <td>{{ d.vendor || '—' }}</td>
                                <td><span class="status-dot" :class="statusClass(d.status)"></span>{{ d.status }}</td>
                                <td>
                                    <button class="btn btn-sm btn-ghost" @click.stop="selectedDevice = d">Detail</button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
        </section>

        <!-- New Request Modal -->
        <div v-if="showRequestForm" class="modal-overlay" @click.self="showRequestForm = false">
            <div class="modal-box">
                <h3>New Provisioning Request</h3>
                <div class="form-group">
                    <label>Device Name</label>
                    <input v-model="requestForm.requestedName" class="form-input" placeholder="e.g. Contour-C2" />
                </div>
                <div class="form-group">
                    <label>Device Type</label>
                    <select v-model="requestForm.deviceType" class="form-select">
                        <option value="Controller">Controller</option>
                        <option value="Reader">Reader</option>
                        <option value="Camera">Camera</option>
                        <option value="Barrier">Barrier</option>
                    </select>
                </div>
                <div class="form-group">
                    <label>Site ID</label>
                    <input v-model.number="requestForm.siteId" type="number" class="form-input" placeholder="Optional" />
                </div>
                <div class="modal-actions">
                    <button class="btn btn-secondary" @click="showRequestForm = false">Cancel</button>
                    <button class="btn btn-primary" :disabled="busy || !requestForm.requestedName.trim()" @click="submitRequest">
                        {{ busy ? 'Submitting...' : 'Submit' }}
                    </button>
                </div>
            </div>
        </div>

        <!-- Create Device Modal -->
        <div v-if="showCreateDevice" class="modal-overlay" @click.self="showCreateDevice = false">
            <div class="modal-box">
                <h3>Create Device</h3>
                <div class="form-group">
                    <label>Device Name *</label>
                    <input v-model="createForm.name" class="form-input" placeholder="e.g. Door-Reader-03" />
                </div>
                <div class="form-group">
                    <label>Device Type *</label>
                    <select v-model="createForm.deviceType" class="form-select">
                        <option value="Controller">Controller</option>
                        <option value="Reader">Reader</option>
                        <option value="Camera">Camera</option>
                        <option value="Barrier">Barrier</option>
                        <option value="Sensor">Sensor</option>
                    </select>
                </div>
                <div class="form-row two">
                    <div class="form-group">
                        <label>Vendor</label>
                        <input v-model="createForm.vendor" class="form-input" placeholder="e.g. HID" />
                    </div>
                    <div class="form-group">
                        <label>Model</label>
                        <input v-model="createForm.model" class="form-input" placeholder="e.g. Signo-20" />
                    </div>
                </div>
                <div class="form-group">
                    <label>IP Address</label>
                    <input v-model="createForm.ipAddress" class="form-input" placeholder="e.g. 192.168.1.100" />
                </div>
                <div class="form-group">
                    <label>Site ID</label>
                    <input v-model.number="createForm.siteId" type="number" class="form-input" placeholder="Optional" />
                </div>
                <div v-if="createResult" class="success-card">{{ createResult }}</div>
                <div v-else-if="createError" class="alert alert-danger">{{ createError }}</div>
                <div class="modal-actions">
                    <button class="btn btn-secondary" @click="showCreateDevice = false">Cancel</button>
                    <button class="btn btn-primary" :disabled="createBusy || !createForm.name.trim()" @click="submitCreateDevice">
                        {{ createBusy ? 'Creating...' : 'Create' }}
                    </button>
                </div>
            </div>
        </div>

        <!-- Register Controller Modal -->
        <div v-if="showRegisterController" class="modal-overlay" @click.self="showRegisterController = false">
            <div class="modal-box">
                <h3>Register Controller</h3>
                <div class="form-group">
                    <label>Parent Device ID *</label>
                    <input v-model.number="regForm.deviceId" type="number" class="form-input" placeholder="Device ID to register controller for" />
                </div>
                <div class="form-group">
                    <label>Protocol</label>
                    <select v-model="regForm.protocol" class="form-select">
                        <option value="OSDP">OSDP</option>
                        <option value="Wiegand">Wiegand</option>
                        <option value="RS-485">RS-485</option>
                    </select>
                </div>
                <div class="form-group">
                    <label>Max Credentials</label>
                    <input v-model.number="regForm.maxCredentials" type="number" class="form-input" value="50000" />
                </div>
                <div v-if="regResult" class="success-card">{{ regResult }}</div>
                <div v-else-if="regError" class="alert alert-danger">{{ regError }}</div>
                <div class="modal-actions">
                    <button class="btn btn-secondary" @click="showRegisterController = false">Cancel</button>
                    <button class="btn btn-primary" :disabled="regBusy || !regForm.deviceId" @click="submitRegisterController">
                        {{ regBusy ? 'Registering...' : 'Register' }}
                    </button>
                </div>
            </div>
        </div>

        <!-- Device Detail Modal -->
        <div v-if="selectedDevice" class="modal-overlay" @click.self="selectedDevice = null">
            <div class="modal-box">
                <h3>Device Detail — {{ selectedDevice.name }}</h3>
                <div class="detail-grid" style="margin:12px 0;">
                    <div class="detail-row"><span class="detail-label">ID</span><span>{{ selectedDevice.securityDeviceId }}</span></div>
                    <div class="detail-row"><span class="detail-label">Type</span><span>{{ selectedDevice.deviceType }}</span></div>
                    <div class="detail-row"><span class="detail-label">Status</span><span class="status-dot" :class="statusClass(selectedDevice.status)"></span>{{ selectedDevice.status }}</div>
                    <div class="detail-row"><span class="detail-label">Vendor</span><span>{{ selectedDevice.vendor || '—' }}</span></div>
                    <div class="detail-row"><span class="detail-label">Model</span><span>{{ selectedDevice.model || '—' }}</span></div>
                    <div class="detail-row"><span class="detail-label">Site</span><span>{{ selectedDevice.siteId || '—' }}</span></div>
                </div>
                <div class="modal-actions">
                    <button class="btn btn-secondary" @click="selectedDevice = null">Close</button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const requests = ref([])
const devices = ref([])
const loading = ref(true)
const busy = ref(false)
const showRequestForm = ref(false)
const statusFilter = ref('')
const deviceSearch = ref('')
const selectedDevice = ref(null)

// Create Device
const showCreateDevice = ref(false)
const createBusy = ref(false)
const createResult = ref('')
const createError = ref('')
const createForm = ref({
    name: '', deviceType: 'Controller', vendor: '',
    model: '', ipAddress: '', siteId: null,
})

// Register Controller
const showRegisterController = ref(false)
const regBusy = ref(false)
const regResult = ref('')
const regError = ref('')
const regForm = ref({ deviceId: null, protocol: 'OSDP', maxCredentials: 50000 })

// Request form
const requestForm = ref({ requestedName: '', deviceType: 'Controller', siteId: null })

const filteredDevices = computed(() => {
    if (!deviceSearch.value) return devices.value
    const q = deviceSearch.value.toLowerCase()
    return devices.value.filter(d => d.name.toLowerCase().includes(q))
})

async function loadRequests() {
    loading.value = true
    try {
        const [reqRes, devRes] = await Promise.all([
            enterpriseApi.getProvisioningRequests({ status: statusFilter.value || undefined }),
            enterpriseApi.getTopology(),
        ])
        requests.value = Array.isArray(reqRes.data) ? reqRes.data : []
        const topo = Array.isArray(devRes.data) ? devRes.data : []
        devices.value = topo
    } catch {
        requests.value = []
        devices.value = []
    } finally {
        loading.value = false
    }
}

async function submitRequest() {
    if (!requestForm.value.requestedName.trim()) return
    busy.value = true
    try {
        await enterpriseApi.createProvisioningRequest(requestForm.value)
        showRequestForm.value = false
        requestForm.value = { requestedName: '', deviceType: 'Controller', siteId: null }
        await loadRequests()
    } finally {
        busy.value = false
    }
}

async function approve(r) {
    if (!confirm(`Approve provisioning for "${r.requestedName}"?`)) return
    try {
        await enterpriseApi.approveProvisioningRequest(r.deviceProvisioningRequestId, { approvalNote: 'Approved via wizard' })
        await loadRequests()
    } catch {
        alert('Approval failed')
    }
}

async function finalizeProvisioning(r) {
    // After approval, auto-create the device
    if (!confirm(`Finalize provisioning for "${r.requestedName}" by creating the device?`)) return
    busy.value = true
    try {
        await enterpriseApi.createDevice({
            name: r.requestedName,
            deviceType: r.deviceType,
            siteId: r.siteId || null,
        })
        alert(`Device "${r.requestedName}" created successfully!`)
        await loadRequests()
    } catch (e) {
        alert('Failed to finalize: ' + (e.response?.data?.message || e.message))
    } finally {
        busy.value = false
    }
}

async function submitCreateDevice() {
    if (!createForm.value.name.trim()) return
    createBusy.value = true
    createResult.value = ''
    createError.value = ''
    try {
        const res = await enterpriseApi.createDevice({
            name: createForm.value.name,
            deviceType: createForm.value.deviceType,
            vendor: createForm.value.vendor || null,
            model: createForm.value.model || null,
            ipAddress: createForm.value.ipAddress || null,
            siteId: createForm.value.siteId || null,
        })
        createResult.value = `Device created! ID: ${res.data?.securityDeviceId || res.data?.id}`
        createForm.value = { name: '', deviceType: 'Controller', vendor: '', model: '', ipAddress: '', siteId: null }
        await loadRequests()
    } catch (e) {
        createError.value = e.response?.data?.message || e.message
    } finally {
        createBusy.value = false
    }
}

async function submitRegisterController() {
    if (!regForm.value.deviceId) return
    regBusy.value = true
    regResult.value = ''
    regError.value = ''
    try {
        const res = await enterpriseApi.registerController(regForm.value.deviceId, {
            protocol: regForm.value.protocol,
            maxCredentials: regForm.value.maxCredentials,
        })
        regResult.value = `Controller registered! ${res.data?.message || ''}`
        regForm.value = { deviceId: null, protocol: 'OSDP', maxCredentials: 50000 }
    } catch (e) {
        regError.value = e.response?.data?.message || e.message
    } finally {
        regBusy.value = false
    }
}

function statusClass(s) {
    if (s === 'Ok' || s === 'Online') return 'status-ok'
    if (s === 'Tamper' || s === 'Fault') return 'status-danger'
    return 'status-warn'
}

onMounted(loadRequests)
</script>

<style scoped>
.device-row { cursor: pointer; transition: background 0.15s; }
.device-row:hover { background: #f1f5f9; }
.toolbar-shell { margin-bottom: 8px; }
.search-bar { position: relative; }
.search-icon { position: absolute; left: 10px; top: 50%; transform: translateY(-50%); width: 16px; height: 16px; color: #94a3b8; }
.search-bar input { width: 100%; padding: 8px 10px 8px 32px; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 13px; background: #fff; }
</style>
