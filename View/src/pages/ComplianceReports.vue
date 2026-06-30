<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Compliance</span>
                <h1 class="page-title">Compliance Reports</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="runReport">Run Report</button>
                <button class="btn btn-secondary" @click="loadReports">Refresh</button>
            </div>
        </div>
        <section class="ops-grid one">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Reports</span><h2 class="panel-title">Generated Reports</h2></div>
                    <div class="panel-actions">
                        <select v-model="reportTypeFilter" @change="loadReports" class="form-select">
                            <option value="">All Types</option>
                            <option value="AccessReview">Access Review</option>
                            <option value="TerminatedUserRevocation">Terminated User Revocation</option>
                            <option value="VisitorLog">Visitor Log</option>
                            <option value="EvidenceAccess">Evidence Access</option>
                            <option value="PrivilegedAction">Privileged Action</option>
                            <option value="AlarmSLA">Alarm SLA</option>
                            <option value="DeviceHealth">Device Health</option>
                        </select>
                    </div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else-if="reports.length === 0" class="empty-card">No compliance reports. Click "Run Report" to generate one.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Type</th><th>Period</th><th>Status</th><th>Output</th><th>Generated</th></tr></thead>
                        <tbody>
                            <tr v-for="r in reports" :key="r.complianceReportRunId">
                                <td><span class="badge badge-info">{{ r.reportType }}</span></td>
                                <td class="table-sub">{{ new Date(r.periodStartUtc).toLocaleDateString() }} — {{ new Date(r.periodEndUtc).toLocaleDateString() }}</td>
                                <td><span class="badge badge-success">{{ r.status }}</span></td>
                                <td class="table-sub">{{ r.outputReference || '—' }}</td>
                                <td class="table-sub">{{ new Date(r.createdAtUtc).toLocaleString() }}</td>
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

const reports = ref([])
const loading = ref(true)
const reportTypeFilter = ref('')

async function loadReports() {
    loading.value = true
    try {
        const res = await enterpriseApi.getComplianceReports({ reportType: reportTypeFilter.value || undefined, limit: 50 })
        reports.value = Array.isArray(res.data) ? res.data : []
    } catch { reports.value = [] }
    finally { loading.value = false }
}

async function runReport() {
    const type = prompt('Report type (AccessReview, TerminatedUserRevocation, VisitorLog, EvidenceAccess, PrivilegedAction, AlarmSLA, DeviceHealth):', 'AccessReview')
    if (!type) return
    const days = prompt('Days to look back:', '30')
    if (!days) return
    const end = new Date()
    const start = new Date(end.getTime() - parseInt(days) * 86400000)
    try {
        await enterpriseApi.runComplianceReport({
            reportType: type,
            periodStartUtc: start.toISOString(),
            periodEndUtc: end.toISOString()
        })
        await loadReports()
    } catch { alert('Run report failed') }
}

onMounted(loadReports)
</script>
