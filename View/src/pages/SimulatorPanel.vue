<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Device simulator</span>
                <h1 class="page-title">Simulator Panel</h1>
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

async function createVirtual() {
    if (!vcForm.value.name.trim()) return
    vcBusy.value = true
    try {
        const res = await enterpriseApi.createVirtualController(vcForm.value)
        vcResult.value = `Created: ${res.data.name} (ID: ${res.data.securityDeviceId})`
    } catch { vcResult.value = 'Failed to create' }
    finally { vcBusy.value = false }
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
    } catch { faultResult.value = 'Failed to inject fault' }
    finally { faultBusy.value = false }
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
    } catch { offlineResult.value = { result: 'Error', reason: 'Simulation failed' } }
    finally { offlineBusy.value = false }
}
</script>
