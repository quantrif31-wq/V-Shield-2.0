<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Device simulator</span>
                <h1 class="page-title">Simulator Panel</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-secondary" :disabled="loading" @click="loadConnectorStatus">Connector Status</button>
            </div>
        </div>

        <section class="ops-grid three">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Virtual</span><h2 class="panel-title">Create Virtual Controller</h2></div>
                </div>
                <div class="form-group">
                    <label>Name</label>
                    <input v-model="vcForm.name" class="form-input" placeholder="e.g. Sim-Controller-1" />
                </div>
                <div class="form-group">
                    <label>Protocol</label>
                    <select v-model="vcForm.protocol" class="form-select">
                        <option value="OSDP-Sim">OSDP-Sim</option>
                        <option value="OSDP">OSDP</option>
                    </select>
                </div>
                <div class="form-group">
                    <label>Max Credentials</label>
                    <input v-model.number="vcForm.maxCredentials" type="number" class="form-input" />
                </div>
                <button class="btn btn-primary" :disabled="vcBusy" @click="createVirtual">{{ vcBusy ? 'Creating...' : 'Create' }}</button>
                <div v-if="vcResult" class="success-card">{{ vcResult }}</div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Fault</span><h2 class="panel-title">Inject Fault</h2></div>
                </div>
                <div class="form-group">
                    <label>Device ID</label>
                    <input v-model.number="faultForm.deviceId" type="number" class="form-input" />
                </div>
                <div class="form-group">
                    <label>Fault Type</label>
                    <select v-model="faultForm.faultType" class="form-select">
                        <option value="Tamper">Tamper</option>
                        <option value="Offline">Offline</option>
                        <option value="Fault">Fault</option>
                        <option value="Degraded">Degraded</option>
                    </select>
                </div>
                <div class="form-group">
                    <label>Severity</label>
                    <select v-model="faultForm.severity" class="form-select">
                        <option value="High">High</option>
                        <option value="Medium">Medium</option>
                        <option value="Low">Low</option>
                    </select>
                </div>
                <div class="form-group">
                    <label>Message</label>
                    <input v-model="faultForm.message" class="form-input" placeholder="Optional" />
                </div>
                <button class="btn btn-danger" :disabled="faultBusy" @click="injectFault">{{ faultBusy ? 'Injecting...' : 'Inject Fault' }}</button>
                <div v-if="faultResult" class="success-card">{{ faultResult }}</div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Offline</span><h2 class="panel-title">Simulate Offline Decision</h2></div>
                </div>
                <div class="form-group">
                    <label>Device ID</label>
                    <input v-model.number="offlineForm.deviceId" type="number" class="form-input" />
                </div>
                <div class="form-group">
                    <label>Subject Type</label>
                    <select v-model="offlineForm.subjectType" class="form-select">
                        <option value="Employee">Employee</option>
                        <option value="Visitor">Visitor</option>
                        <option value="Contractor">Contractor</option>
                    </select>
                </div>
                <div class="form-group">
                    <label>Subject ID</label>
                    <input v-model.number="offlineForm.subjectId" type="number" class="form-input" placeholder="Optional" />
                </div>
                <div class="form-group">
                    <label>Credential Type</label>
                    <select v-model="offlineForm.credentialType" class="form-select">
                        <option value="QR">QR</option>
                        <option value="Card">Card</option>
                        <option value="Pin">Pin</option>
                        <option value="Any">Any</option>
                    </select>
                </div>
                <button class="btn btn-primary" :disabled="offlineBusy" @click="simulateOffline">{{ offlineBusy ? 'Simulating...' : 'Simulate' }}</button>
                <div v-if="offlineResult" class="result-card" :class="offlineResult.result === 'Allow' ? 'result-allow' : 'result-deny'">
                    {{ offlineResult.result }}: {{ offlineResult.reason }}
                </div>
            </article>
        </section>

        <section class="ops-grid three" style="margin-top:1rem;">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Health</span><h2 class="panel-title">Record Device Health</h2></div>
                </div>
                <div class="form-group">
                    <label>Device ID</label>
                    <input v-model.number="healthForm.deviceId" type="number" class="form-input" />
                </div>
                <div class="form-group">
                    <label>Status</label>
                    <select v-model="healthForm.status" class="form-select">
                        <option value="Ok">Ok</option>
                        <option value="Degraded">Degraded</option>
                        <option value="Fault">Fault</option>
                        <option value="Offline">Offline</option>
                    </select>
                </div>
                <div class="form-group">
                    <label>Message</label>
                    <input v-model="healthForm.message" class="form-input" placeholder="Optional note" />
                </div>
                <button class="btn btn-primary" :disabled="healthBusy || !healthForm.deviceId" @click="recordHealth">{{ healthBusy ? 'Recording...' : 'Record' }}</button>
                <div v-if="healthResult" class="success-card">{{ healthResult }}</div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Register</span><h2 class="panel-title">Register Controller</h2></div>
                </div>
                <div class="form-group">
                    <label>Device ID</label>
                    <input v-model.number="regForm.deviceId" type="number" class="form-input" />
                </div>
                <div class="form-group">
                    <label>Protocol</label>
                    <select v-model="regForm.protocol" class="form-select">
                        <option value="OSDP">OSDP</option>
                        <option value="Wiegand">Wiegand</option>
                        <option value="RS-485">RS-485</option>
                    </select>
                </div>
                <div class="form-group">
                    <label>Max Credentials</label>
                    <input v-model.number="regForm.maxCredentials" type="number" class="form-input" />
                </div>
                <button class="btn btn-primary" :disabled="regBusy || !regForm.deviceId" @click="registerController">{{ regBusy ? 'Registering...' : 'Register' }}</button>
                <div v-if="regResult" class="success-card">{{ regResult }}</div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Create</span><h2 class="panel-title">Create Device</h2></div>
                </div>
                <div class="form-group">
                    <label>Device Name</label>
                    <input v-model="createForm.name" class="form-input" placeholder="e.g. Sim-Device-01" />
                </div>
                <div class="form-group">
                    <label>Type</label>
                    <select v-model="createForm.deviceType" class="form-select">
                        <option value="Controller">Controller</option>
                        <option value="Reader">Reader</option>
                        <option value="Camera">Camera</option>
                        <option value="Barrier">Barrier</option>
                    </select>
                </div>
                <button class="btn btn-primary" :disabled="createBusy || !createForm.name.trim()" @click="createDevice">{{ createBusy ? 'Creating...' : 'Create' }}</button>
                <div v-if="createResult" class="success-card">{{ createResult }}</div>
            </article>
        </section>

        <!-- Connector Status Modal -->
        <Teleport to="body">
            <div v-if="showConnectorStatus" class="modal-overlay" @click.self="showConnectorStatus = false">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Connector Status</h2>
                        <button class="btn-close" @click="showConnectorStatus = false">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div v-if="connectorLoading" class="empty-card">Loading...</div>
                        <div v-else-if="connectorStatus.length === 0" class="empty-card">No connectors.</div>
                        <div v-else class="table-container">
                            <table class="data-table">
                                <thead><tr><th>Connector</th><th>Status</th><th>Last Seen</th></tr></thead>
                                <tbody>
                                    <tr v-for="cs in connectorStatus" :key="cs.connectorId || cs.name">
                                        <td>{{ cs.name || cs.connectorId }}</td>
                                        <td><span class="status-dot" :class="cs.status === 'Connected' ? 'status-ok' : 'status-danger'"></span>{{ cs.status }}</td>
                                        <td class="table-sub">{{ formatTime(cs.lastSeenUtc) }}</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="showConnectorStatus = false">Close</button>
                    </div>
                </div>
            </div>
        </Teleport>
    </div>
</template>

<script setup>
import { ref } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const vcForm = ref({ name: '', protocol: 'OSDP-Sim', maxCredentials: 50000 })
const vcBusy = ref(false)
const vcResult = ref('')

const faultForm = ref({ deviceId: null, faultType: 'Tamper', severity: 'High', message: '' })
const faultBusy = ref(false)
const faultResult = ref('')

const offlineForm = ref({ deviceId: null, subjectType: 'Employee', subjectId: null, credentialType: 'QR' })
const offlineBusy = ref(false)
const offlineResult = ref(null)

// Record Health
const healthForm = ref({ deviceId: null, status: 'Ok', message: '' })
const healthBusy = ref(false)
const healthResult = ref('')

// Register Controller
const regForm = ref({ deviceId: null, protocol: 'OSDP', maxCredentials: 50000 })
const regBusy = ref(false)
const regResult = ref('')

// Create Device
const createForm = ref({ name: '', deviceType: 'Controller' })
const createBusy = ref(false)
const createResult = ref('')

// Connector Status
const showConnectorStatus = ref(false)
const connectorLoading = ref(false)
const connectorStatus = ref([])
const loading = ref(false)

async function createVirtual() {
    if (!vcForm.value.name.trim()) return
    vcBusy.value = true
    try {
        const res = await enterpriseApi.createVirtualController(vcForm.value)
        vcResult.value = `Created: ${res.data.name} (ID: ${res.data.securityDeviceId})`
    } catch {
        vcResult.value = 'Failed to create'
    } finally {
        vcBusy.value = false
    }
}

async function injectFault() {
    if (!faultForm.value.deviceId) return
    faultBusy.value = true
    try {
        const res = await enterpriseApi.injectSimulatorFault({
            securityDeviceId: faultForm.value.deviceId,
            status: faultForm.value.faultType,
            severity: faultForm.value.severity,
            message: faultForm.value.message
        })
        faultResult.value = `Fault injected: ${res.data.status}`
    } catch {
        faultResult.value = 'Failed to inject fault'
    } finally {
        faultBusy.value = false
    }
}

async function simulateOffline() {
    if (!offlineForm.value.deviceId) return
    offlineBusy.value = true
    try {
        const res = await enterpriseApi.simulateOfflineScan({
            securityDeviceId: offlineForm.value.deviceId,
            subjectType: offlineForm.value.subjectType,
            subjectId: offlineForm.value.subjectId || undefined,
            credentialType: offlineForm.value.credentialType
        })
        offlineResult.value = res.data
    } catch {
        offlineResult.value = { result: 'Error', reason: 'Simulation failed' }
    } finally {
        offlineBusy.value = false
    }
}

// Record Health
async function recordHealth() {
    if (!healthForm.value.deviceId) return
    healthBusy.value = true
    healthResult.value = ''
    try {
        await enterpriseApi.recordHealth(healthForm.value.deviceId, {
            status: healthForm.value.status,
            message: healthForm.value.message || null,
        })
        healthResult.value = 'Health recorded successfully!'
        healthForm.value = { deviceId: null, status: 'Ok', message: '' }
    } catch (e) {
        healthResult.value = 'Failed: ' + (e.response?.data?.message || e.message)
    } finally {
        healthBusy.value = false
    }
}

// Register Controller
async function registerController() {
    if (!regForm.value.deviceId) return
    regBusy.value = true
    regResult.value = ''
    try {
        const res = await enterpriseApi.registerController(regForm.value.deviceId, {
            protocol: regForm.value.protocol,
            maxCredentials: regForm.value.maxCredentials,
        })
        regResult.value = 'Controller registered!'
        regForm.value = { deviceId: null, protocol: 'OSDP', maxCredentials: 50000 }
    } catch (e) {
        regResult.value = 'Failed: ' + (e.response?.data?.message || e.message)
    } finally {
        regBusy.value = false
    }
}

// Create Device
async function createDevice() {
    if (!createForm.value.name.trim()) return
    createBusy.value = true
    createResult.value = ''
    try {
        const res = await enterpriseApi.createDevice({
            name: createForm.value.name,
            deviceType: createForm.value.deviceType,
        })
        createResult.value = `Created! ID: ${res.data?.securityDeviceId || res.data?.id}`
        createForm.value = { name: '', deviceType: 'Controller' }
    } catch (e) {
        createResult.value = 'Failed: ' + (e.response?.data?.message || e.message)
    } finally {
        createBusy.value = false
    }
}

// Connector Status
async function loadConnectorStatus() {
    showConnectorStatus.value = true
    connectorLoading.value = true
    try {
        const res = await enterpriseApi.getConnectorStatus()
        connectorStatus.value = Array.isArray(res.data) ? res.data : []
    } catch {
        connectorStatus.value = []
    } finally {
        connectorLoading.value = false
    }
}

function formatTime(utc) {
    if (!utc) return '—'
    return new Date(utc).toLocaleString('vi-VN')
}
</script>
