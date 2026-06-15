<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Multi-signal</span>
                <h1 class="page-title">Correlation View</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="runCorrelation">Run Correlation</button>
                <button class="btn btn-secondary" @click="loadCorrelations">Refresh</button>
            </div>
        </div>
        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Correlations</span><h2 class="panel-title">Event Correlations</h2></div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else-if="correlations.length === 0" class="empty-card">No correlations. Click "Run Correlation" to analyze events.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Rule</th><th>Severity</th><th>Summary</th><th>Time</th><th>Actions</th></tr></thead>
                        <tbody>
                            <tr v-for="c in correlations" :key="c.eventCorrelationId" class="clickable-row" @click="selectCorrelation(c)">
                                <td>{{ c.ruleName }}</td>
                                <td><span class="badge" :class="c.severity === 'High' || c.severity === 'Critical' ? 'badge-danger' : 'badge-warn'">{{ c.severity }}</span></td>
                                <td class="table-sub">{{ c.summary }}</td>
                                <td class="table-sub">{{ new Date(c.createdAtUtc).toLocaleString() }}</td>
                                <td><button class="btn btn-secondary btn-sm" @click.stop="selectCorrelation(c)">View</button></td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Detail</span><h2 class="panel-title">Correlation Events</h2></div>
                </div>
                <div v-if="!detail" class="empty-card">Select a correlation to view linked events.</div>
                <div v-else>
                    <div class="detail-grid">
                        <div><strong>Rule:</strong> {{ detail.correlation.ruleName }}</div>
                        <div><strong>Severity:</strong> {{ detail.correlation.severity }}</div>
                        <div><strong>Summary:</strong> {{ detail.correlation.summary }}</div>
                    </div>
                    <div v-if="detail.events.length === 0" class="empty-card">No linked events.</div>
                    <div v-else class="table-container">
                        <table class="data-table">
                            <thead><tr><th>Time</th><th>Type</th><th>Severity</th><th>Subject</th><th>Plate</th></tr></thead>
                            <tbody>
                                <tr v-for="e in detail.events" :key="e.securityEventId">
                                    <td class="table-sub">{{ new Date(e.occurredAtUtc).toLocaleString() }}</td>
                                    <td><span class="badge badge-info">{{ e.eventType }}</span></td>
                                    <td><span class="badge" :class="sevClass(e.severity)">{{ e.severity }}</span></td>
                                    <td>{{ e.subjectType || '—' }}:{{ e.subjectId || '' }}</td>
                                    <td>{{ e.plateText || '—' }}</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </article>
        </section>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const correlations = ref([])
const detail = ref(null)
const loading = ref(true)

async function loadCorrelations() {
    loading.value = true
    try { const res = await enterpriseApi.getCorrelations({ limit: 50 }); correlations.value = Array.isArray(res.data) ? res.data : [] }
    catch { correlations.value = [] }
    finally { loading.value = false }
}

async function selectCorrelation(c) {
    try { const res = await enterpriseApi.getCorrelationDetail(c.eventCorrelationId); detail.value = res.data }
    catch { detail.value = null }
}

async function runCorrelation() {
    try {
        await enterpriseApi.runCorrelation({ sinceUtc: new Date(Date.now() - 3600000).toISOString(), minimumEvents: 2 })
        await loadCorrelations()
    } catch { alert('Run correlation failed') }
}

function sevClass(s) {
    if (s === 'Critical' || s === 'High') return 'badge-danger'
    if (s === 'Medium') return 'badge-warn'
    return 'badge-info'
}

onMounted(loadCorrelations)
</script>
