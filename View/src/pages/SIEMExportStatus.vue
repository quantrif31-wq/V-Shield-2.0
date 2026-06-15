<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">SIEM</span>
                <h1 class="page-title">SIEM Export Status</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="loadExports">Refresh</button>
            </div>
        </div>
        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Exports</span><h2 class="panel-title">SIEM Export Events</h2></div>
                    <div class="panel-actions">
                        <select v-model="statusFilter" @change="loadExports" class="form-select">
                            <option value="">All</option>
                            <option value="Pending">Pending</option>
                            <option value="Completed">Completed</option>
                        </select>
                    </div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else-if="exports.length === 0" class="empty-card">No SIEM exports.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>Event ID</th><th>Event Type</th><th>Status</th><th>Correlation</th><th>Created</th></tr></thead>
                        <tbody>
                            <tr v-for="e in exports" :key="e.outboxEventId">
                                <td>{{ e.outboxEventId }}</td>
                                <td>{{ e.sourceId || '—' }}</td>
                                <td><span class="badge badge-info">{{ e.eventType }}</span></td>
                                <td><span class="badge" :class="e.status === 'Completed' ? 'badge-success' : 'badge-warn'">{{ e.status }}</span></td>
                                <td class="table-sub">{{ e.correlationId }}</td>
                                <td class="table-sub">{{ new Date(e.createdAtUtc).toLocaleString() }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Schema</span><h2 class="panel-title">Export Schema Validation</h2></div>
                </div>
                <div class="empty-card">
                    <p>SIEM exports use standardized event schema with correlation IDs.</p>
                    <p>Supported event types: SecurityEvent, Alarm, AccessDenied, etc.</p>
                    <p>Payload includes: source, target, timestamp, severity, actor.</p>
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
        const res = await enterpriseApi.getSiemExports({ status: statusFilter.value || undefined })
        exports.value = Array.isArray(res.data) ? res.data : []
    } catch { exports.value = [] }
    finally { loading.value = false }
}

onMounted(loadExports)
</script>
