<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Operations</span>
                <h1 class="page-title">Operations Dashboard</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="loadData">Refresh</button>
            </div>
        </div>
        <section class="ops-grid three">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Events</span><h2 class="panel-title">Event Status</h2></div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else>
                    <div class="kpi-row">
                        <div class="kpi-card"><strong>{{ summary.totalEvents }}</strong><span>Tổng events</span></div>
                        <div class="kpi-card"><strong>{{ summary.pendingEvents }}</strong><span>Đang chờ</span></div>
                        <div class="kpi-card"><strong>{{ summary.dispatchedEvents }}</strong><span>Đã dispatch</span></div>
                        <div class="kpi-card"><strong>{{ summary.failedEvents }}</strong><span>Failed</span></div>
                        <div class="kpi-card"><strong>{{ summary.deadLetter }}</strong><span>Dead Letter</span></div>
                    </div>
                    <div v-if="eventTrends.length" class="chart-container">
                        <h3>Trends (24h)</h3>
                        <div class="chart-bars">
                            <div v-for="bar in eventTrends" :key="bar.time" class="bar-column">
                                <div class="bar-fill" :style="{ height: bar.count / maxEvents * 100 + '%' }"></div>
                                <span class="bar-label">{{ bar.time }}</span>
                            </div>
                        </div>
                    </div>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Security</span><h2 class="panel-title">Configuration Health</h2></div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else-if="configHealth.length === 0" class="empty-card">No health checks.</div>
                <div v-else>
                    <div v-for="item in configHealth" :key="item.category" class="health-item">
                        <div class="health-header">
                            <span :class="itemClass(item.status)">{{ item.category }}</span>
                            <span :class="statusClass(item.status)">{{ item.status }}</span>
                        </div>
                        <div v-if="item.findings && item.findings.length > 0" class="findings">
                            <div v-for="finding in item.findings" :key="finding.id" class="finding">
                                <span :class="findingSeverityClass(finding.severity)">{{ finding.severity }}:</span>
                                <span>{{ finding.message }}</span>
                            </div>
                        </div>
                    </div>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Backups</span><h2 class="panel-title">Backup Operations</h2></div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else-if="backups.length === 0" class="empty-card">No backup runs.</div>
                <div v-else>
                    <div class="table-container">
                        <table class="data-table">
                            <thead><tr><th>ID</th><th>Profile</th><th>Status</th><th>Started</th><th>Size</th><th>RPO/RTO</th></tr></thead>
                            <tbody>
                                <tr v-for="b in backups" :key="b.backupRunId">
                                    <td>{{ b.backupRunId }}</td>
                                    <td>{{ b.profile }}</td>
                                    <td><span class="badge" :class="b.status === 'Completed' ? 'badge-success' : b.status === 'Failed' ? 'badge-danger' : 'badge-warn'">{{ b.status }}</span></td>
                                    <td class="table-sub">{{ new Date(b.startedAtUtc).toLocaleString() }}</td>
                                    <td>{{ b.sizeBytes ? (b.sizeBytes / (1024*1024)).toFixed(2) + ' MB' : '—' }}</td>
                                    <td class="table-sub">{{ b.targetRpoMinutes }}min / {{ b.targetRtoMinutes }}min</td>
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

const summary = ref({ totalEvents: 0, pendingEvents: 0, dispatchedEvents: 0, failedEvents: 0, deadLetter: 0 })
const configHealth = ref([])
const eventTrends = ref([])
const backups = ref([])
const loading = ref(true)

async function loadData() {
    loading.value = true
    try {
        const [overviewRes, configRes, trendsRes, backupsRes] = await Promise.all([
            enterpriseApi.overview(),
            enterpriseApi.configHealth(),
            enterpriseApi.getCorrelations({ limit: 24 }),
            enterpriseApi.getBackupRuns({ limit: 10 })
        ])
        summary.value = overviewRes.data
        configHealth.value = Array.isArray(configRes.data) ? configRes.data : []
        eventTrends.value = Array.isRes?.data || []
        backups.value = Array.isArray(backupsRes.data) ? backupsRes.data : []
    } catch { summary.value = { totalEvents: 0, pendingEvents: 0, dispatchedEvents: 0, failedEvents: 0, deadLetter: 0 }; configHealth.value = []; eventTrends.value = []; backups.value = [] }
    finally { loading.value = false }
}

function itemClass(s) {
    if (s === 'Blocked' || s === 'Critical') return 'danger'
    if (s === 'Warning') return 'warn'
    return 'success'
}

function statusClass(s) {
    if (s === 'Blocked' || s === 'Failed' || s === 'Critical') return 'danger'
    if (s === 'Warning') return 'warn'
    return 'success'
}

function findingSeverityClass(s) {
    if (s === 'Critical') return 'danger'
    if (s === 'Warning') return 'warn'
    return 'info'
}

onMounted(loadData)
</script>
<style>
.kpi-row { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 1rem; margin: 1rem 0; }
.kpi-card { background: #f8f9fa; border: 1px solid #dee2e6; border-radius: 4px; padding: 1rem; text-align: center; }
.kpi-card strong { font-size: 1.5rem; display: block; }
.kpi-card span { font-size: 0.9rem; color: #6c757d; }
.chart-container { margin-top: 1rem; }
.chart-bars { display: flex; align-items: flex-end; height: 40px; gap: 0.5rem; padding: 0 0.5rem; }
.bar-column { flex: 1; text-align: center; }
.bar-fill { background: #007bff; border-radius: 2px 2px 0 0; }
.bar-label { font-size: 0.7rem; margin-top: 0.25rem; }
.health-item { border: 1px solid #dee2e6; border-radius: 4px; padding: 0.75rem; margin: 0.5rem 0; }
.health-header { display: flex; justify-content: space-between; margin-bottom: 0.5rem; }
.findings { margin-left: 1rem; }
.finding { font-size: 0.85rem; margin: 0.25rem 0; }
</style>
