<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Evidence export</span>
                <h1 class="page-title">Export Approval Queue</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="loadExports">Refresh</button>
            </div>
        </div>
        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Requests</span><h2 class="panel-title">Export Requests</h2></div>
                    <div class="panel-actions">
                        <select v-model="statusFilter" @change="loadExports" class="form-select">
                            <option value="">All</option>
                            <option value="PendingApproval">Pending</option>
                            <option value="Approved">Approved</option>
                            <option value="Rejected">Rejected</option>
                        </select>
                    </div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else-if="exports.length === 0" class="empty-card">No export requests.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>Evidence ID</th><th>Collection ID</th><th>Purpose</th><th>Recipient</th><th>Status</th><th>Requested</th><th>Actions</th></tr></thead>
                        <tbody>
                            <tr v-for="r in exports" :key="r.evidenceExportRequestId">
                                <td>{{ r.evidenceExportRequestId }}</td>
                                <td>{{ r.evidenceItemId || '—' }}</td>
                                <td>{{ r.evidenceCollectionId || '—' }}</td>
                                <td class="table-sub">{{ r.purpose.substring(0, 40) }}</td>
                                <td>{{ r.recipient }}</td>
                                <td><span class="badge" :class="r.status === 'Approved' ? 'badge-success' : r.status === 'Rejected' ? 'badge-danger' : 'badge-warn'">{{ r.status }}</span></td>
                                <td class="table-sub">{{ new Date(r.requestedAtUtc).toLocaleString() }}</td>
                                <td>
                                    <button v-if="r.status === 'PendingApproval'" class="btn btn-success btn-sm" @click="approve(r)">Approve</button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Info</span><h2 class="panel-title">Approval Instructions</h2></div>
                </div>
                <div class="empty-card">
                    <p>Each export requires privileged approval with step-up MFA.</p>
                    <p>Watermark (e.g. case ID, recipient name) is embedded on approval.</p>
                    <p>Signature is generated automatically using HMAC-SHA256.</p>
                    <p>Evidence hash is verified before export to ensure integrity.</p>
                </div>
            </article>
        </section>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const exports = ref([])
const loading = ref(true)
const statusFilter = ref('')

async function loadExports() {
    loading.value = true
    try {
        const res = await enterpriseApi.getExportRequests({ status: statusFilter.value || undefined })
        exports.value = Array.isArray(res.data) ? res.data : []
    } catch { exports.value = [] }
    finally { loading.value = false }
}

async function approve(r) {
    const watermark = prompt('Watermark text (e.g. Case #123):')
    if (!watermark) return
    try {
        await enterpriseApi.approveExportRequest(r.evidenceExportRequestId, { watermark })
        await loadExports()
    } catch { alert('Approval failed') }
}

onMounted(loadExports)
</script>
