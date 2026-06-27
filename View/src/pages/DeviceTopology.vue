<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Enterprise devices</span>
                <h1 class="page-title">Device Topology</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-secondary" @click="openOfflinePackages()">Offline Packages</button>
                <button class="btn btn-primary" :disabled="loading" @click="loadTopology">Refresh</button>
            </div>
        </div>

        <!-- Health Summary row -->
        <section class="metric-grid four" v-if="healthSummary">
            <article class="metric-tile"><span class="metric-label">Total Devices</span><strong class="metric-value">{{ healthSummary.totalDevices || topology.length }}</strong></article>
            <article class="metric-tile"><span class="metric-label">Online</span><strong class="metric-value">{{ healthSummary.onlineCount || topology.filter(d => d.status === 'Ok').length }}</strong></article>
            <article class="metric-tile"><span class="metric-label">Degraded</span><strong class="metric-value" style="color:#d97706;">{{ healthSummary.degradedCount || topology.filter(d => d.status === 'Degraded').length }}</strong></article>
            <article class="metric-tile"><span class="metric-label">Offline/Fault</span><strong class="metric-value" style="color:#dc2626;">{{ healthSummary.offlineCount || topology.filter(d => d.status === 'Offline' || d.status === 'Fault' || d.status === 'Tamper').length }}</strong></article>
        </section>

        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Overviews</span><h2 class="panel-title">Device Landscape</h2></div>
                </div>
                <div v-if="loading" class="empty-card">Loading topology...</div>
                <div v-else>
                    <div class="toolbar-shell">
                        <div class="search-bar">
                            <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <circle cx="11" cy="11" r="8" /><path d="M21 21l-4.35-4.35" />
                            </svg>
                            <input v-model="searchQuery" type="text" placeholder="Search device..." />
                        </div>
                        <select v-model="typeFilter" class="form-control" style="width:auto;">
                            <option value="">All types</option>
                            <option value="Controller">Controller</option>
                            <option value="Reader">Reader</option>
                            <option value="Camera">Camera</option>
                            <option value="Barrier">Barrier</option>
                            <option value="Sensor">Sensor</option>
                        </select>
                    </div>
                    <div class="table-container">
                        <table class="data-table">
                            <thead>
                                <tr>
                                    <th>Device</th>
                                    <th>Type</th>
                                    <th>Status</th>
                                    <th>Controller</th>
                                    <th>R/S</th>
                                    <th>Site</th>
                                    <th>Health</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr v-for="d in filteredTopology" :key="d.securityDeviceId" class="device-row" @click="openDeviceDetail(d)" :class="{ 'selected-row': selectedDevice?.securityDeviceId === d.securityDeviceId }">
                                    <td><strong>{{ d.name }}</strong></td>
                                    <td><span class="badge badge-info">{{ d.deviceType }}</span></td>
                                    <td><span class="status-dot" :class="statusClass(d.status)"></span>{{ d.status }}</td>
                                    <td>{{ d.controller?.protocol || '—' }}</td>
                                    <td>
                                        <span class="reader-count" :title="`${d.readerCount || 0} readers`">R:{{ d.readerCount || 0 }}</span>
                                        /
                                        <span :title="`${d.relayCount || 0} relays`">S:{{ d.relayCount || 0 }}/{{ d.sensorCount || 0 }}</span>
                                    </td>
                                    <td>{{ d.siteId || '—' }}</td>
                                    <td>
                                        <span class="health-dot" :class="healthDotClass(d)"></span>
                                        {{ d.healthStatus?.status || '—' }}
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Connectors</span><h2 class="panel-title">Connector & Adapter Status</h2></div>
                </div>
                <div v-if="loading" class="empty-card">Loading...</div>
                <div v-else>
                    <div v-if="connectorStatus.length > 0" class="table-container" style="margin-bottom:12px;">
                        <table class="data-table">
                            <thead><tr><th>Connector</th><th>Status</th><th>Last Seen</th></tr></thead>
                            <tbody>
                                <tr v-for="cs in connectorStatus" :key="cs.connectorId || cs.name">
                                    <td>{{ cs.name || cs.connectorId }}</td>
                                    <td><span class="status-dot" :class="cs.status === 'Connected' ? 'status-ok' : 'status-danger'"></span>{{ cs.status }}</td>
                                    <td class="table-sub">{{ cs.lastSeenUtc ? formatTime(cs.lastSeenUtc) : '—' }}</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                    <div class="table-container">
                        <table class="data-table">
                            <thead><tr><th>Protocol</th><th>Type</th><th>Status</th></tr></thead>
                            <tbody>
                                <tr v-for="a in adapters" :key="a.protocol">
                                    <td>{{ a.protocol }}</td>
                                    <td>{{ a.type }}</td>
                                    <td><span class="status-dot" :class="a.status === 'Simulated' ? 'status-ok' : 'status-warn'"></span>{{ a.status }}</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>
            </article>
        </section>

        <!-- Device Detail Drawer -->
        <Teleport to="body">
            <div v-if="selectedDevice" class="modal-overlay drawer-overlay" @click.self="selectedDevice = null">
                <div class="modal-panel drawer-panel">
                    <div class="modal-header">
                        <h2>{{ selectedDevice.name }}</h2>
                        <span class="badge badge-info" style="margin-right:8px;">{{ selectedDevice.deviceType }}</span>
                        <button class="btn-close" @click="selectedDevice = null">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="drawer-tabs">
                            <button v-for="dt in detailTabs" :key="dt.key" :class="{ active: activeDetailTab === dt.key }" @click="activeDetailTab = dt.key; loadDetailTab(dt.key)">
                                {{ dt.label }}
                            </button>
                        </div>

                        <!-- Overview Tab -->
                        <div v-if="activeDetailTab === 'overview'" class="drawer-tab-content">
                            <div class="detail-grid">
                                <div class="detail-row"><span class="detail-label">Device ID</span><span>{{ selectedDevice.securityDeviceId }}</span></div>
                                <div class="detail-row"><span class="detail-label">Type</span><span>{{ selectedDevice.deviceType }}</span></div>
                                <div class="detail-row"><span class="detail-label">Status</span><span class="status-dot" :class="statusClass(selectedDevice.status)"></span>{{ selectedDevice.status }}</div>
                                <div class="detail-row"><span class="detail-label">Vendor</span><span>{{ selectedDevice.vendor || '—' }}</span></div>
                                <div class="detail-row"><span class="detail-label">Model</span><span>{{ selectedDevice.model || '—' }}</span></div>
                                <div class="detail-row"><span class="detail-label">IP Address</span><span>{{ selectedDevice.ipAddress || '—' }}</span></div>
                                <div class="detail-row"><span class="detail-label">Site</span><span>{{ selectedDevice.siteId || '—' }}</span></div>
                                <div class="detail-row"><span class="detail-label">Controller</span><span>{{ selectedDevice.controller?.protocol || '—' }}</span></div>
                                <div class="detail-row"><span class="detail-label">Health</span><span>{{ selectedDevice.healthStatus?.status || '—' }}</span></div>
                                <div class="detail-row"><span class="detail-label">Last Seen</span><span>{{ selectedDevice.lastSeenUtc ? formatTime(selectedDevice.lastSeenUtc) : '—' }}</span></div>
                            </div>
                            <div class="chip-row" style="margin-top:12px;">
                                <button class="btn btn-sm btn-secondary" @click="diagnoseDevice(selectedDevice)">AI Diagnose</button>
                                <button class="btn btn-sm btn-secondary" @click="recordHealthForDevice(selectedDevice)">Record Health</button>
                            </div>
                            <div v-if="diagnosisResult" class="alert alert-info" style="margin-top:8px;">
                                <strong>Diagnosis:</strong> {{ diagnosisResult }}
                            </div>
                        </div>

                        <!-- Readers Tab -->
                        <div v-if="activeDetailTab === 'readers'" class="drawer-tab-content">
                            <div v-if="detailLoading" class="empty-card">Loading readers...</div>
                            <div v-else-if="readers.length === 0" class="empty-card">No readers.</div>
                            <div v-else class="table-container">
                                <table class="data-table">
                                    <thead><tr><th>Name</th><th>Type</th><th>Status</th></tr></thead>
                                    <tbody>
                                        <tr v-for="r in readers" :key="r.readerId || r.id">
                                            <td>{{ r.name || r.readerName }}</td>
                                            <td>{{ r.readerType || r.type }}</td>
                                            <td><span class="status-dot" :class="statusClass(r.status)"></span>{{ r.status }}</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </div>

                        <!-- Relays Tab -->
                        <div v-if="activeDetailTab === 'relays'" class="drawer-tab-content">
                            <div v-if="detailLoading" class="empty-card">Loading relays...</div>
                            <div v-else-if="relays.length === 0" class="empty-card">No relays.</div>
                            <div v-else class="table-container">
                                <table class="data-table">
                                    <thead><tr><th>Name</th><th>Type</th><th>State</th><th>Status</th></tr></thead>
                                    <tbody>
                                        <tr v-for="r in relays" :key="r.relayId || r.id">
                                            <td>{{ r.name || r.relayName }}</td>
                                            <td>{{ r.relayType || r.type }}</td>
                                            <td><span class="soft-chip" :class="r.state === 'Closed' ? 'success' : 'warn'">{{ r.state }}</span></td>
                                            <td><span class="status-dot" :class="statusClass(r.status)"></span>{{ r.status }}</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </div>

                        <!-- Sensors Tab -->
                        <div v-if="activeDetailTab === 'sensors'" class="drawer-tab-content">
                            <div v-if="detailLoading" class="empty-card">Loading sensors...</div>
                            <div v-else-if="sensors.length === 0" class="empty-card">No sensors.</div>
                            <div v-else class="table-container">
                                <table class="data-table">
                                    <thead><tr><th>Name</th><th>Type</th><th>Value</th><th>Status</th></tr></thead>
                                    <tbody>
                                        <tr v-for="s in sensors" :key="s.sensorId || s.id">
                                            <td>{{ s.name || s.sensorName }}</td>
                                            <td>{{ s.sensorType || s.type }}</td>
                                            <td>{{ s.value || s.currentValue || '—' }}</td>
                                            <td><span class="status-dot" :class="statusClass(s.status)"></span>{{ s.status }}</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </div>

                        <!-- Health History Tab -->
                        <div v-if="activeDetailTab === 'health'" class="drawer-tab-content">
                            <div v-if="detailLoading" class="empty-card">Loading health history...</div>
                            <div v-else-if="healthHistory.length === 0" class="empty-card">No health history.</div>
                            <div v-else class="table-container">
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
                        </div>
                    </div>
                </div>
            </div>

            <!-- Record Health Modal -->
            <div v-if="recordHealthTarget" class="modal-overlay" @click.self="recordHealthTarget = null">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Record Health — {{ recordHealthTarget.name }}</h2>
                        <button class="btn-close" @click="recordHealthTarget = null">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="form-group">
                            <label>Status</label>
                            <select v-model="healthForm.status" class="form-control">
                                <option value="Ok">Ok</option>
                                <option value="Degraded">Degraded</option>
                                <option value="Fault">Fault</option>
                                <option value="Offline">Offline</option>
                            </select>
                        </div>
                        <div class="form-group">
                            <label>Message</label>
                            <textarea v-model="healthForm.message" class="form-control" rows="2" placeholder="Optional health note"></textarea>
                        </div>
                        <div v-if="healthSaveResult" class="alert alert-success">{{ healthSaveResult }}</div>
                        <div v-else-if="healthSaveError" class="alert alert-danger">{{ healthSaveError }}</div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="recordHealthTarget = null">Close</button>
                        <button class="btn btn-primary" :disabled="healthSaving" @click="submitHealthRecord">{{ healthSaving ? 'Saving...' : 'Save' }}</button>
                    </div>
                </div>
            </div>

            <!-- Offline Packages Modal -->
            <div v-if="showOfflinePackages" class="modal-overlay" @click.self="showOfflinePackages = false">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Offline Policy Packages</h2>
                        <button class="btn-close" @click="showOfflinePackages = false">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="form-group">
                            <label>Device ID</label>
                            <input v-model.number="offlinePackageForm.deviceId" type="number" class="form-control" placeholder="Device ID" />
                        </div>
                        <div class="form-group">
                            <label>Policy Version ID</label>
                            <input v-model.number="offlinePackageForm.policyVersionId" type="number" class="form-control" placeholder="Optional" />
                        </div>
                        <div v-if="offlinePkgResult" class="alert alert-success">{{ offlinePkgResult }}</div>
                        <div v-else-if="offlinePkgError" class="alert alert-danger">{{ offlinePkgError }}</div>
                        <div class="chip-row" style="margin-top:8px;">
                            <button class="btn btn-primary" :disabled="offlinePkgSaving || !offlinePackageForm.deviceId" @click="createOfflinePackage">
                                {{ offlinePkgSaving ? 'Creating...' : 'Create Package' }}
                            </button>
                        </div>

                        <div style="margin-top:16px;">
                            <div class="detail-section-title">Existing Packages</div>
                            <div v-if="offlinePackages.length === 0" class="text-muted">No packages.</div>
                            <div v-for="pkg in offlinePackages" :key="pkg.offlinePolicyPackageId || pkg.id" class="pkg-card">
                                <div><strong>Device {{ pkg.securityDeviceId }}</strong></div>
                                <div class="text-muted">Version {{ pkg.policyVersionId }} · {{ formatTime(pkg.createdAtUtc) }}</div>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="showOfflinePackages = false">Close</button>
                    </div>
                </div>
            </div>
        </Teleport>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const topology = ref([])
const adapters = ref([])
const connectorStatus = ref([])
const healthSummary = ref(null)
const loading = ref(true)
const searchQuery = ref('')
const typeFilter = ref('')

// Device detail drawer
const selectedDevice = ref(null)
const activeDetailTab = ref('overview')
const detailLoading = ref(false)
const readers = ref([])
const relays = ref([])
const sensors = ref([])
const healthHistory = ref([])
const diagnosisResult = ref('')

// Record health
const recordHealthTarget = ref(null)
const healthForm = ref({ status: 'Ok', message: '' })
const healthSaving = ref(false)
const healthSaveResult = ref('')
const healthSaveError = ref('')

// Offline packages
const showOfflinePackages = ref(false)
const offlinePackages = ref([])
const offlinePackageForm = ref({ deviceId: null, policyVersionId: null })
const offlinePkgSaving = ref(false)
const offlinePkgResult = ref('')
const offlinePkgError = ref('')

const detailTabs = [
    { key: 'overview', label: 'Overview' },
    { key: 'readers', label: 'Readers' },
    { key: 'relays', label: 'Relays' },
    { key: 'sensors', label: 'Sensors' },
    { key: 'health', label: 'Health History' },
]

const filteredTopology = computed(() => {
    let result = topology.value
    if (searchQuery.value) {
        const q = searchQuery.value.toLowerCase()
        result = result.filter(d => d.name.toLowerCase().includes(q))
    }
    if (typeFilter.value) {
        result = result.filter(d => d.deviceType === typeFilter.value)
    }
    return result
})

async function loadTopology() {
    loading.value = true
    try {
        const [topoRes, adaptRes, connRes, healthRes] = await Promise.all([
            enterpriseApi.getTopology(),
            enterpriseApi.getAdapters().catch(() => ({ data: { adapters: [] } })),
            enterpriseApi.getConnectorStatus().catch(() => ({ data: [] })),
            enterpriseApi.getHealthSummary().catch(() => ({ data: null })),
        ])
        topology.value = Array.isArray(topoRes.data) ? topoRes.data : []
        adapters.value = adaptRes.data?.adapters || []
        connectorStatus.value = Array.isArray(connRes.data) ? connRes.data : []
        healthSummary.value = healthRes.data || null
    } catch {
        topology.value = []
        adapters.value = []
        connectorStatus.value = []
    } finally {
        loading.value = false
    }
}

function statusClass(s) {
    if (s === 'Ok' || s === 'Online' || s === 'Connected') return 'status-ok'
    if (s === 'Tamper' || s === 'Fault' || s === 'Offline') return 'status-danger'
    if (s === 'Degraded') return 'status-warn'
    return 'status-warn'
}

function healthDotClass(d) {
    const s = d.healthStatus?.status
    if (s === 'Ok' || s === 'Healthy') return 'health-ok'
    if (s === 'Degraded' || s === 'Warning') return 'health-warn'
    if (s === 'Fault' || s === 'Critical') return 'health-danger'
    return ''
}

function formatTime(utc) {
    if (!utc) return '—'
    return new Date(utc).toLocaleString('vi-VN')
}

// --- Device Detail ---
async function openDeviceDetail(d) {
    selectedDevice.value = d
    activeDetailTab.value = 'overview'
    readers.value = []
    relays.value = []
    sensors.value = []
    healthHistory.value = []
    diagnosisResult.value = ''
}

async function loadDetailTab(tab) {
    if (!selectedDevice.value) return
    const deviceId = selectedDevice.value.securityDeviceId
    detailLoading.value = true
    try {
        if (tab === 'readers') {
            const res = await enterpriseApi.getDeviceReaders(deviceId)
            readers.value = Array.isArray(res.data) ? res.data : []
        } else if (tab === 'relays') {
            const res = await enterpriseApi.getDeviceRelays(deviceId)
            relays.value = Array.isArray(res.data) ? res.data : []
        } else if (tab === 'sensors') {
            const res = await enterpriseApi.getDeviceSensors(deviceId)
            sensors.value = Array.isArray(res.data) ? res.data : []
        } else if (tab === 'health') {
            const res = await enterpriseApi.getDeviceHealthHistory(deviceId, { pageSize: 50 })
            healthHistory.value = Array.isArray(res.data) ? res.data : (res.data?.items || [])
        }
    } catch (e) {
        console.error(`Failed to load ${tab} for device ${deviceId}`, e)
    } finally {
        detailLoading.value = false
    }
}

async function diagnoseDevice(d) {
    diagnosisResult.value = ''
    try {
        const res = await enterpriseApi.diagnoseDevice(d.securityDeviceId)
        diagnosisResult.value = res.data?.diagnosis || res.data?.message || 'Diagnosis complete'
    } catch (e) {
        diagnosisResult.value = 'Diagnosis failed: ' + (e.response?.data?.message || e.message)
    }
}

// --- Offline Packages ---
function openOfflinePackages() {
    showOfflinePackages.value = true
    loadOfflinePackages()
}

// --- Record Health ---
function recordHealthForDevice(d) {
    recordHealthTarget.value = d
    healthForm.value = { status: 'Ok', message: '' }
    healthSaveResult.value = ''
    healthSaveError.value = ''
}

async function submitHealthRecord() {
    if (!recordHealthTarget.value) return
    healthSaving.value = true
    healthSaveResult.value = ''
    healthSaveError.value = ''
    try {
        await enterpriseApi.recordHealth(recordHealthTarget.value.securityDeviceId, {
            status: healthForm.value.status,
            message: healthForm.value.message || null,
        })
        healthSaveResult.value = 'Health recorded successfully!'
    } catch (e) {
        healthSaveError.value = e.response?.data?.message || e.message
    } finally {
        healthSaving.value = false
    }
}

// --- Offline Packages ---
async function loadOfflinePackages() {
    try {
        const res = await enterpriseApi.getOfflinePolicyPackages({ pageSize: 50 })
        offlinePackages.value = Array.isArray(res.data) ? res.data : (res.data?.items || [])
    } catch (e) {
        console.error('Failed to load offline packages', e)
    }
}

async function createOfflinePackage() {
    if (!offlinePackageForm.value.deviceId) return
    offlinePkgSaving.value = true
    offlinePkgResult.value = ''
    offlinePkgError.value = ''
    try {
        await enterpriseApi.createOfflinePolicyPackage({
            securityDeviceId: offlinePackageForm.value.deviceId,
            policyVersionId: offlinePackageForm.value.policyVersionId || undefined,
        })
        offlinePkgResult.value = 'Offline package created!'
        offlinePackageForm.value = { deviceId: null, policyVersionId: null }
        await loadOfflinePackages()
    } catch (e) {
        offlinePkgError.value = e.response?.data?.message || e.message
    } finally {
        offlinePkgSaving.value = false
    }
}

onMounted(loadTopology)
</script>

<style scoped>
.device-row { cursor: pointer; transition: background 0.15s; }
.device-row:hover { background: #f1f5f9; }
.selected-row { background: #e0f2fe !important; }
.reader-count { cursor: help; }
.health-dot { display: inline-block; width: 8px; height: 8px; border-radius: 50%; margin-right: 4px; }
.health-ok { background: #22c55e; }
.health-warn { background: #eab308; }
.health-danger { background: #ef4444; }

.drawer-overlay { display: flex; justify-content: flex-end; }
.drawer-panel { width: 520px; max-width: 95vw; height: 100vh; margin: 0; border-radius: 0; overflow-y: auto; background: var(--bg-card-strong); }
.drawer-tabs { display: flex; gap: 4px; margin-bottom: 16px; border-bottom: 1px solid #e2e8f0; padding-bottom: 8px; }
.drawer-tabs button { padding: 6px 14px; border: none; background: transparent; color: #51657b; font-size: 13px; border-radius: 8px; cursor: pointer; transition: all 0.15s; }
.drawer-tabs button:hover { background: #f1f5f9; }
.drawer-tabs button.active { background: #e0f2fe; color: #0369a1; font-weight: 600; }
.drawer-tab-content { min-height: 100px; }
.detail-section-title { font-size: 13px; font-weight: 600; color: #1e293b; margin-bottom: 8px; padding-bottom: 4px; border-bottom: 1px solid #e2e8f0; }
.pkg-card { padding: 8px 10px; border: 1px solid #e2e8f0; border-radius: 8px; margin-bottom: 6px; }

.toolbar-shell { display: flex; gap: 8px; margin-bottom: 8px; align-items: center; }
.search-bar { flex: 1; position: relative; }
.search-icon { position: absolute; left: 10px; top: 50%; transform: translateY(-50%); width: 16px; height: 16px; color: #94a3b8; }
.search-bar input { width: 100%; padding: 8px 10px 8px 32px; border: 1px solid #e2e8f0; border-radius: 8px; font-size: 13px; background: #fff; }
</style>
