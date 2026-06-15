<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Device health</span>
                <h1 class="page-title">Device Health & Intelligence</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-secondary" @click="showRecordModal = true">Record Health</button>
                <button class="btn btn-primary" @click="loadAll">Refresh</button>
            </div>
        </div>

        <!-- Health Summary -->
        <section class="metric-grid four" v-if="healthSummary">
            <article class="metric-tile"><span class="metric-label">Total</span><strong class="metric-value">{{ healthSummary.totalDevices || 0 }}</strong></article>
            <article class="metric-tile"><span class="metric-label">Healthy</span><strong class="metric-value" style="color:#16a34a;">{{ healthSummary.healthyCount || healthSummary.onlineCount || 0 }}</strong></article>
            <article class="metric-tile"><span class="metric-label">Degraded</span><strong class="metric-value" style="color:#d97706;">{{ healthSummary.degradedCount || 0 }}</strong></article>
            <article class="metric-tile"><span class="metric-label">Critical</span><strong class="metric-value" style="color:#dc2626;">{{ healthSummary.criticalCount || healthSummary.offlineCount || 0 }}</strong></article>
        </section>

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
                        <button class="btn btn-sm btn-ghost" style="margin-top:4px;" @click="openDeviceHealthHistory(di.deviceId, di.deviceName)">View History</button>
                    </div>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">History</span><h2 class="panel-title">Device Health History</h2></div>
                </div>
                <div class="form-group">
                    <label>Device ID</label>
                    <div class="chip-row">
                        <input v-model.number="selectedDevice" type="number" class="form-control" placeholder="Enter device ID" />
                        <button class="btn btn-secondary btn-sm" @click="loadHealthHistory">Load</button>
                    </div>
                </div>
                <div v-if="historyLoading" class="empty-card">Loading history...</div>
                <div v-else-if="healthHistory.length === 0 && selectedDevice" class="empty-card">No health history for device {{ selectedDevice }}.</div>
                <div v-else-if="healthHistory.length > 0" class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Time</th><th>Status</th><th>Message</th></tr></thead>
                        <tbody>
                            <tr v-for="h in healthHistory" :key="h.healthLogId || h.id">
                                <td class="table-sub">{{ formatTime(h.recordedAtUtc || h.timestamp) }}</td>
                                <td><span class="status-dot" :class="statusClass(h.status)"></span>{{ h.status }}</td>
                                <td>{{ h.message || h.description || '—' }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
                <div v-else class="empty-card">Enter a device ID and click Load.</div>
            </article>
        </section>

        <section class="ops-grid two" style="margin-top:1rem;">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Diagnose</span><h2 class="panel-title">AI Device Diagnosis</h2></div>
                </div>
                <div class="form-group">
                    <label>Device ID</label>
                    <input v-model.number="diagnoseDeviceId" type="number" class="form-control" placeholder="Enter device ID" />
                </div>
                <button class="btn btn-primary" :disabled="!diagnoseDeviceId || diagnoseBusy" @click="runDiagnosis">
                    {{ diagnoseBusy ? 'Diagnosing...' : 'Run AI Diagnosis' }}
                </button>
                <div v-if="diagnosisResult" class="result-card" style="margin-top:8px;">
                    <strong>Diagnosis:</strong> {{ diagnosisResult }}
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Configs</span><h2 class="panel-title">Configuration Versions</h2></div>
                </div>
                <div class="form-group">
                    <label>Device ID</label>
                    <div class="chip-row">
                        <input v-model.number="configDeviceId" type="number" class="form-control" placeholder="Enter device ID" />
                        <button class="btn btn-secondary btn-sm" @click="loadConfigs">Load</button>
                    </div>
                </div>
                <div v-if="configLoading" class="empty-card">Loading...</div>
                <div v-else-if="configs.length === 0 && configDeviceId" class="empty-card">No configuration versions.</div>
                <div v-else-if="configs.length > 0" class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Version</th><th>Created By</th><th>Timestamp</th></tr></thead>
                        <tbody>
                            <tr v-for="c in configs" :key="c.deviceConfigurationVersionId">
                                <td>{{ c.version }}</td>
                                <td>{{ c.createdByUserId || '—' }}</td>
                                <td class="table-sub">{{ formatTime(c.createdAtUtc) }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
        </section>

        <Teleport to="body">
            <!-- Record Health Modal -->
            <div v-if="showRecordModal" class="modal-overlay" @click.self="showRecordModal = false">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Record Device Health</h2>
                        <button class="btn-close" @click="showRecordModal = false">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="form-group">
                            <label>Device ID *</label>
                            <input v-model.number="recordForm.deviceId" type="number" class="form-control" placeholder="Required" />
                        </div>
                        <div class="form-group">
                            <label>Status *</label>
                            <select v-model="recordForm.status" class="form-control">
                                <option value="Ok">Ok</option>
                                <option value="Degraded">Degraded</option>
                                <option value="Fault">Fault</option>
                                <option value="Offline">Offline</option>
                                <option value="Tamper">Tamper</option>
                            </select>
                        </div>
                        <div class="form-group">
                            <label>Message</label>
                            <textarea v-model="recordForm.message" class="form-control" rows="2" placeholder="Optional health note"></textarea>
                        </div>
                        <div v-if="recordResult" class="alert alert-success">{{ recordResult }}</div>
                        <div v-else-if="recordError" class="alert alert-danger">{{ recordError }}</div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="showRecordModal = false">Cancel</button>
                        <button class="btn btn-primary" :disabled="recordSaving || !recordForm.deviceId" @click="submitRecordHealth">
                            {{ recordSaving ? 'Recording...' : 'Record' }}
                        </button>
                    </div>
                </div>
            </div>

            <!-- Device Health History Modal -->
            <div v-if="historyModal.visible" class="modal-overlay" @click.self="historyModal.visible = false">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Health History — {{ historyModal.deviceName }}</h2>
                        <button class="btn-close" @click="historyModal.visible = false">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div v-if="historyModal.loading" class="empty-card">Loading...</div>
                        <div v-else-if="historyModal.items.length === 0" class="empty-card">No health history.</div>
                        <div v-else class="table-container">
                            <table class="data-table">
                                <thead><tr><th>Time</th><th>Status</th><th>Message</th></tr></thead>
                                <tbody>
                                    <tr v-for="h in historyModal.items" :key="h.healthLogId || h.id">
                                        <td class="table-sub">{{ formatTime(h.recordedAtUtc || h.timestamp) }}</td>
                                        <td><span class="status-dot" :class="statusClass(h.status)"></span>{{ h.status }}</td>
                                        <td>{{ h.message || h.description || '—' }}</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="historyModal.visible = false">Close</button>
                    </div>
                </div>
            </div>
        </Teleport>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const insights = ref([])
const configs = ref([])
const loading = ref(true)
const configLoading = ref(false)
const selectedDevice = ref(1)
const historyLoading = ref(false)
const healthHistory = ref([])

// Diagnose
const diagnoseDeviceId = ref(null)
const diagnoseBusy = ref(false)
const diagnosisResult = ref('')

// Config
const configDeviceId = ref(1)

// Record Health
const showRecordModal = ref(false)
const recordForm = ref({ deviceId: null, status: 'Ok', message: '' })
const recordSaving = ref(false)
const recordResult = ref('')
const recordError = ref('')

// History Modal
const historyModal = reactive({
    visible: false,
    deviceName: '',
    deviceId: null,
    items: [],
    loading: false,
})

async function loadAll() {
    loading.value = true
    try {
        const [insightsRes, summaryRes] = await Promise.all([
            enterpriseApi.getHealthInsights(),
            enterpriseApi.getHealthSummary().catch(() => ({ data: null })),
        ])
        insights.value = Array.isArray(insightsRes.data) ? insightsRes.data : []
        healthSummary.value = summaryRes.data || null
    } catch {
        insights.value = []
    } finally {
        loading.value = false
    }
}

const healthSummary = ref(null)

async function loadHealthHistory() {
    if (!selectedDevice.value) return
    historyLoading.value = true
    try {
        const res = await enterpriseApi.getDeviceHealthHistory(selectedDevice.value, { pageSize: 50 })
        healthHistory.value = Array.isArray(res.data) ? res.data : (res.data?.items || [])
    } catch {
        healthHistory.value = []
    } finally {
        historyLoading.value = false
    }
}

async function loadConfigs() {
    if (!configDeviceId.value) return
    configLoading.value = true
    try {
        const res = await enterpriseApi.getDeviceConfigurations(configDeviceId.value)
        configs.value = Array.isArray(res.data) ? res.data : []
    } catch {
        configs.value = []
    } finally {
        configLoading.value = false
    }
}

async function runDiagnosis() {
    if (!diagnoseDeviceId.value) return
    diagnoseBusy.value = true
    diagnosisResult.value = ''
    try {
        const res = await enterpriseApi.diagnoseDevice(diagnoseDeviceId.value)
        diagnosisResult.value = res.data?.diagnosis || res.data?.message || 'Diagnosis complete'
    } catch (e) {
        diagnosisResult.value = 'Diagnosis failed: ' + (e.response?.data?.message || e.message)
    } finally {
        diagnoseBusy.value = false
    }
}

async function submitRecordHealth() {
    if (!recordForm.value.deviceId) return
    recordSaving.value = true
    recordResult.value = ''
    recordError.value = ''
    try {
        await enterpriseApi.recordHealth(recordForm.value.deviceId, {
            status: recordForm.value.status,
            message: recordForm.value.message || null,
        })
        recordResult.value = 'Health recorded successfully!'
        recordForm.value = { deviceId: null, status: 'Ok', message: '' }
    } catch (e) {
        recordError.value = e.response?.data?.message || e.message
    } finally {
        recordSaving.value = false
    }
}

async function openDeviceHealthHistory(deviceId, deviceName) {
    historyModal.visible = true
    historyModal.deviceId = deviceId
    historyModal.deviceName = deviceName
    historyModal.loading = true
    historyModal.items = []
    try {
        const res = await enterpriseApi.getDeviceHealthHistory(deviceId, { pageSize: 50 })
        historyModal.items = Array.isArray(res.data) ? res.data : (res.data?.items || [])
    } catch (e) {
        console.error('Failed to load health history', e)
    } finally {
        historyModal.loading = false
    }
}

function statusClass(s) {
    if (s === 'Ok' || s === 'Online' || s === 'Healthy') return 'status-ok'
    if (s === 'Tamper' || s === 'Fault' || s === 'Offline' || s === 'Critical') return 'status-danger'
    return 'status-warn'
}

function formatTime(utc) {
    if (!utc) return '—'
    return new Date(utc).toLocaleString('vi-VN')
}

onMounted(loadAll)
</script>
