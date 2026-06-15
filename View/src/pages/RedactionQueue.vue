<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Privacy</span>
                <h1 class="page-title">Redaction Queue</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="loadRedactions">Refresh</button>
            </div>
        </div>
        <section class="ops-grid one">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Requests</span><h2 class="panel-title">Redaction Requests</h2></div>
                    <div class="panel-actions">
                        <select v-model="statusFilter" @change="loadRedactions" class="form-select">
                            <option value="">All</option>
                            <option value="PendingApproval">Pending Approval</option>
                            <option value="Approved">Approved</option>
                            <option value="Performed">Performed</option>
                            <option value="Verified">Verified</option>
                        </select>
                    </div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else-if="requests.length === 0" class="empty-card">No redaction requests.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>Evidence ID</th><th>Privacy Label</th><th>Reason</th><th>Status</th><th>Actions</th></tr></thead>
                        <tbody>
                            <tr v-for="r in requests" :key="r.redactionRequestId">
                                <td>{{ r.redactionRequestId }}</td>
                                <td>{{ r.evidenceItemId }}</td>
                                <td><span class="badge" :class="r.privacyLabel === 'Biometric' ? 'badge-danger' : 'badge-warn'">{{ r.privacyLabel }}</span></td>
                                <td class="table-sub">{{ r.reason.substring(0, 50) }}</td>
                                <td><span class="badge" :class="statusClass(r.status)">{{ r.status }}</span></td>
                                <td>
                                    <button v-if="r.status === 'PendingApproval'" class="btn btn-success btn-sm" @click="approve(r)">Approve</button>
                                    <button v-if="r.status === 'Approved'" class="btn btn-primary btn-sm" @click="perform(r)">Perform</button>
                                    <button v-if="r.status === 'Performed'" class="btn btn-secondary btn-sm" @click="verify(r)">Verify</button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
        </section>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const requests = ref([])
const loading = ref(true)
const statusFilter = ref('')

async function loadRedactions() {
    loading.value = true
    try {
        const res = await enterpriseApi.getRedactionRequests({ status: statusFilter.value || undefined })
        requests.value = Array.isArray(res.data) ? res.data : []
    } catch { requests.value = [] }
    finally { loading.value = false }
}

async function approve(r) {
    if (!confirm(`Approve redaction #${r.redactionRequestId}?`)) return
    try { await enterpriseApi.approveRedaction(r.redactionRequestId, {}); await loadRedactions() }
    catch { alert('Approve failed') }
}

async function perform(r) {
    const ref = prompt('Redacted storage reference (path):')
    if (!ref) return
    try { await enterpriseApi.performRedaction(r.redactionRequestId, { redactedStorageReference: ref }); await loadRedactions() }
    catch { alert('Perform failed') }
}

async function verify(r) {
    if (!confirm(`Verify redaction #${r.redactionRequestId} completed?`)) return
    try { await enterpriseApi.verifyRedaction(r.redactionRequestId, {}); await loadRedactions() }
    catch { alert('Verify failed') }
}

function statusClass(s) {
    if (s === 'Verified') return 'badge-success'
    if (s === 'Performed') return 'badge-primary'
    if (s === 'Approved') return 'badge-info'
    return 'badge-warn'
}

onMounted(loadRedactions)
</script>
