<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Device health</span>
                <h1 class="page-title">Device Health & Intelligence</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="loadAll">Refresh</button>
            </div>
        </div>
        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">AI Insights</span><h2 class="panel-title">Health Predictions</h2></div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else-if="insights.length === 0" class="empty-card">No insights available.</div>
                <div v-else class="device-insight-list">
                    <div v-for="di in insights" :key="di.deviceId" class="device-insight-item" :class="'pred-' + (di.predictedStatus || '').toLowerCase()">
                        <strong>{{ di.deviceName }}</strong>
                        <span class="small-meta">{{ di.predictedStatus }}</span>
                        <div class="small-meta">{{ di.summary }}</div>
                    </div>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Configs</span><h2 class="panel-title">Configuration Versions</h2></div>
                </div>
                <div class="form-group">
                    <label>Device ID</label>
                    <input v-model.number="selectedDevice" type="number" class="form-input" placeholder="Enter device ID" />
                </div>
                <button class="btn btn-secondary btn-sm" @click="loadConfigs">Load</button>
                <div v-if="configLoading" class="empty-card">Loading...</div>
                <div v-else-if="configs.length === 0" class="empty-card">No configuration versions.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Version</th><th>Created By</th><th>Timestamp</th></tr></thead>
                        <tbody>
                            <tr v-for="c in configs" :key="c.deviceConfigurationVersionId">
                                <td>{{ c.version }}</td>
                                <td>{{ c.createdByUserId || '—' }}</td>
                                <td class="table-sub">{{ new Date(c.createdAtUtc).toLocaleString() }}</td>
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

const insights = ref([])
const configs = ref([])
const loading = ref(true)
const configLoading = ref(false)
const selectedDevice = ref(1)

async function loadAll() {
    loading.value = true
    try {
        const res = await enterpriseApi.getHealthInsights()
        insights.value = Array.isArray(res.data) ? res.data : []
    } catch { insights.value = [] }
    finally { loading.value = false }
}

async function loadConfigs() {
    if (!selectedDevice.value) return
    configLoading.value = true
    try {
        const res = await enterpriseApi.getDeviceConfigurations(selectedDevice.value)
        configs.value = Array.isArray(res.data) ? res.data : []
    } catch { configs.value = [] }
    finally { configLoading.value = false }
}

onMounted(loadAll)
</script>
