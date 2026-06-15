<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Device provisioning</span>
                <h1 class="page-title">Provisioning Wizard</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="showForm = true">New Request</button>
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
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else-if="devices.length === 0" class="empty-card">No devices registered.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Name</th><th>Type</th><th>Vendor</th><th>Status</th></tr></thead>
                        <tbody>
                            <tr v-for="d in devices" :key="d.securityDeviceId">
                                <td>{{ d.name }}</td>
                                <td>{{ d.deviceType }}</td>
                                <td>{{ d.vendor || '—' }}</td>
                                <td><span class="status-dot" :class="d.status === 'Ok' ? 'status-ok' : 'status-warn'"></span>{{ d.status }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
        </section>
        <div v-if="showForm" class="modal-overlay" @click.self="showForm = false">
            <div class="modal-box">
                <h3>New Provisioning Request</h3>
                <div class="form-group">
                    <label>Device Name</label>
                    <input v-model="form.requestedName" class="form-input" placeholder="e.g. Contour-C2" />
                </div>
                <div class="form-group">
                    <label>Device Type</label>
                    <select v-model="form.deviceType" class="form-select">
                        <option value="Controller">Controller</option>
                        <option value="Reader">Reader</option>
                        <option value="Camera">Camera</option>
                        <option value="Barrier">Barrier</option>
                    </select>
                </div>
                <div class="modal-actions">
                    <button class="btn btn-secondary" @click="showForm = false">Cancel</button>
                    <button class="btn btn-primary" :disabled="busy" @click="submitRequest">{{ busy ? 'Submitting...' : 'Submit' }}</button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const requests = ref([])
const devices = ref([])
const loading = ref(true)
const busy = ref(false)
const showForm = ref(false)
const statusFilter = ref('')
const form = ref({ requestedName: '', deviceType: 'Controller' })

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
    } catch { requests.value = []; devices.value = [] }
    finally { loading.value = false }
}

async function submitRequest() {
    if (!form.value.requestedName.trim()) return
    busy.value = true
    try {
        await enterpriseApi.createProvisioningRequest(form.value)
        showForm.value = false
        form.value = { requestedName: '', deviceType: 'Controller' }
        await loadRequests()
    } finally { busy.value = false }
}

async function approve(r) {
    if (!confirm(`Approve provisioning for "${r.requestedName}"?`)) return
    try {
        await enterpriseApi.approveProvisioningRequest(r.deviceProvisioningRequestId, { approvalNote: 'Approved via wizard' })
        await loadRequests()
    } catch { alert('Approval failed') }
}

onMounted(loadRequests)
</script>
