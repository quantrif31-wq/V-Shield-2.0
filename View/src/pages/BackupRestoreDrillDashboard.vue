<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Backup & Restore</span>
                <h1 class="page-title">Backup/Restore Drills</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="loadDrills">Refresh</button>
            </div>
        </div>
        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Drills</span><h2 class="panel-title">Restore Drills</h2></div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else-if="drills.length === 0" class="empty-card">No restore drills.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>Profile</th><th>Status</th><th>Started</th><th>RPO/RTO</th><th>Passed</th></tr></thead>
                        <tbody>
                            <tr v-for="d in drills" :key="d.restoreDrillId">
                                <td>{{ d.restoreDrillId }}</td>
                                <td>{{ d.profile }}</td>
                                <td><span class="badge" :class="d.status === 'Completed' ? 'badge-success' : 'badge-warn'">{{ d.status }}</span></td>
                                <td class="table-sub">{{ new Date(d.startedAtUtc).toLocaleString() }}</td>
                                <td class="table-sub">{{ d.targetRpoMinutes }}min / {{ d.targetRtoMinutes }}min</td>
                                <td><span class="badge" :class="d.passed ? 'badge-success' : 'badge-danger'">{{ d.passed ? 'PASS' : 'FAIL' }}</span></td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Backups</span><h2 class="panel-title">Backup Runs</h2></div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else-if="backups.length === 0" class="empty-card">No backup runs.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>Profile</th><th>Status</th><th>Started</th><th>Size</th></tr></thead>
                        <tbody>
                            <tr v-for="b in backups" :key="b.backupRunId">
                                <td>{{ b.backupRunId }}</td>
                                <td>{{ b.profile }}</td>
                                <td><span class="badge" :class="b.status === 'Completed' ? 'badge-success' : b.status === 'Failed' ? 'badge-danger' : 'badge-warn'">{{ b.status }}</span></td>
                                <td class="table-sub">{{ new Date(b.startedAtUtc).toLocaleString() }}</td>
                                <td>{{ b.sizeBytes ? (b.sizeBytes / (1024*1024)).toFixed(2) + ' MB' : '—' }}</td>
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

const backups = ref([])
const drills = ref([])
const loading = ref(true)

async function loadDrills() {
    loading.value = true
    try {
        const [backupsRes, drillsRes] = await Promise.all([
            enterpriseApi.getBackupRuns({ limit: 10 }),
            enterpriseApi.getRestoreDrills({ limit: 10 })
        ])
        backups.value = Array.isArray(backupsRes.data) ? backupsRes.data : []
        drills.value = Array.isArray(drillsRes.data) ? drillsRes.data : []
    } catch { backups.value = []; drills.value = [] }
    finally { loading.value = false }
}

onMounted(loadDrills)
</script>
