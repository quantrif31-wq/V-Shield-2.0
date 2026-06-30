<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Infrastructure Control</span>
                <h1 class="page-title">Barrier & Parking Control</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-secondary" :disabled="loading" @click="loadAll">Refresh</button>
            </div>
        </div>

        <div class="tab-bar">
            <button v-for="t in tabs" :key="t.id" :class="{ active: activeTab === t.id }" @click="activeTab = t.id">
                {{ t.label }}
            </button>
        </div>

        <section v-if="activeTab === 'barriers'" class="ops-panel">
            <div class="panel-head">
                <h2 class="panel-title">Barriers</h2>
                <button class="btn btn-primary" @click="showAddBarrier = true">Add Barrier</button>
            </div>
            <div v-if="loading" class="empty-card">Loading barriers...</div>
            <div v-else-if="barriers.length === 0" class="empty-card">No barriers configured.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr><th>Name</th><th>Lane</th><th>State</th><th>Active</th><th>Actions</th></tr>
                    </thead>
                    <tbody>
                        <tr v-for="b in barriers" :key="b.barrierId">
                            <td><strong>{{ b.name }}</strong></td>
                            <td>{{ b.lane?.name || '&mdash;' }}</td>
                            <td><span class="soft-chip" :class="stateClass(b.state)">{{ b.state }}</span></td>
                            <td>{{ b.isActive ? 'Yes' : 'No' }}</td>
                            <td>
                                <div class="chip-row">
                                    <button class="btn btn-sm btn-success" @click="sendCommand(b, 'Open')">Open</button>
                                    <button class="btn btn-sm btn-secondary" @click="sendCommand(b, 'Close')">Close</button>
                                    <button class="btn btn-sm btn-ghost" @click="sendCommand(b, 'HoldOpen')">Hold</button>
                                    <button class="btn btn-sm btn-danger" @click="sendCommand(b, 'LockClosed')">Lock</button>
                                    <button class="btn btn-sm btn-ghost" @click="simulateCommand(b)">Simulate</button>
                                    <button class="btn btn-sm btn-ghost" @click="showHistory(b)">History</button>
                                </div>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </section>

        <section v-if="activeTab === 'parking'" class="ops-panel">
            <div class="panel-head">
                <h2 class="panel-title">Parking Permits</h2>
                <button class="btn btn-primary" @click="showAddPermit = true">Issue Permit</button>
            </div>
            <div class="toolbar-shell">
                <div class="search-bar">
                    <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <circle cx="11" cy="11" r="8" /><path d="M21 21l-4.35-4.35" />
                    </svg>
                    <input v-model="permitSearch" type="text" placeholder="Search by permit..." />
                </div>
                <label class="checkbox-label">
                    <input v-model="activeOnly" type="checkbox" @change="loadParking" /> Active only
                </label>
            </div>
            <div v-if="loadingParking" class="empty-card">Loading permits...</div>
            <div v-else-if="permits.length === 0" class="empty-card">No parking permits.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr><th>Area</th><th>Vehicle</th><th>Type</th><th>Valid</th><th>Status</th></tr>
                    </thead>
                    <tbody>
                        <tr v-for="p in permits" :key="p.parkingPermitId">
                            <td>{{ p.parkingArea?.name || '&mdash;' }}</td>
                            <td>{{ p.vehicle?.licensePlate || '&mdash;' }}</td>
                            <td>{{ p.permitType }}</td>
                            <td>{{ formatDate(p.validFromUtc) }} &rarr; {{ formatDate(p.validToUtc) }}</td>
                            <td><span class="soft-chip" :class="p.isRevoked ? 'danger' : 'success'">{{ p.isRevoked ? 'Revoked' : 'Active' }}</span></td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </section>

        <Teleport to="body">
            <div v-if="commandTarget" class="modal-overlay" @click.self="cancelCommand">
                <div class="modal-panel" style="max-width: 460px;">
                    <div class="modal-header">
                        <h2>Barrier Command: {{ commandTarget.name }}</h2>
                        <button class="btn-close" @click="cancelCommand">&times;</button>
                    </div>
                    <div class="modal-body">
                        <p>Send <strong>{{ pendingCommand }}</strong> to barrier <strong>{{ commandTarget.name }}</strong></p>
                        <div v-if="isHighRiskCommand" class="alert alert-warning" style="margin-bottom: 12px;">
                            <strong>&#9888; High-risk command:</strong> {{ pendingCommand }} requires step-up authentication.
                        </div>
                        <div class="form-group">
                            <label>Reason *</label>
                            <textarea v-model="commandReason" class="form-control" rows="2" placeholder="Required reason for this command"></textarea>
                        </div>
                        <div v-if="commandError" class="alert alert-danger">{{ commandError }}</div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="cancelCommand">Cancel</button>
                        <button class="btn btn-primary" :disabled="saving || !commandReason" @click="confirmCommand">
                            {{ saving ? 'Sending...' : `Send ${pendingCommand}` }}
                        </button>
                    </div>
                </div>
            </div>

            <div v-if="historyBarrier" class="modal-overlay" @click.self="historyBarrier = null">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Command History: {{ historyBarrier.name }}</h2>
                        <button class="btn-close" @click="historyBarrier = null">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div v-if="historyItems.length === 0" class="text-muted">No commands recorded.</div>
                        <div v-else class="table-container">
                            <table class="data-table">
                                <thead><tr><th>Time</th><th>Command</th><th>Reason</th><th>Result</th></tr></thead>
                                <tbody>
                                    <tr v-for="h in historyItems" :key="h.barrierCommandAuditId">
                                        <td>{{ formatDate(h.requestedAtUtc) }}</td>
                                        <td><span class="soft-chip">{{ h.command }}</span></td>
                                        <td>{{ h.reason }}</td>
                                        <td>{{ h.result }}</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="historyBarrier = null">Close</button>
                    </div>
                </div>
            </div>

            <div v-if="showAddBarrier" class="modal-overlay" @click.self="showAddBarrier = false">
                <div class="modal-panel" style="max-width: 440px;">
                    <div class="modal-header"><h2>Add Barrier</h2><button class="btn-close" @click="showAddBarrier = false">&times;</button></div>
                    <div class="modal-body">
                        <div class="form-group"><label>Name *</label><input v-model="newBarrier.name" class="form-control" /></div>
                        <div class="form-group"><label>Lane</label>
                            <select v-model="newBarrier.laneId" class="form-control">
                                <option :value="null">&mdash; Select &mdash;</option>
                                <option v-for="l in laneOptions" :key="l.laneId" :value="l.laneId">{{ l.name }}</option>
                            </select>
                        </div>
                        <div v-if="addBarrierError" class="alert alert-danger">{{ addBarrierError }}</div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="showAddBarrier = false">Cancel</button>
                        <button class="btn btn-primary" :disabled="saving" @click="submitBarrier">{{ saving ? 'Adding...' : 'Add' }}</button>
                    </div>
                </div>
            </div>

            <!-- Step-Up Modal -->
            <StepUpModal
                :visible="stepUpVisible"
                action-label="High-risk Barrier Command"
                :action-description="'Confirm high-risk command: ' + pendingCommand + ' on barrier ' + (commandTarget?.name || '')"
                severity="high"
                :require-mfa="true"
                @cancel="onStepUpCancelled"
                @confirmed="onStepUpConfirmed"
            />

            <!-- Audit Receipt Toast -->
            <AuditReceiptToast
                :visible="auditToast.visible"
                :type="auditToast.type"
                :title="auditToast.title"
                :message="auditToast.message"
                :receipt-id="auditToast.receiptId"
                :timestamp="auditToast.timestamp"
                @dismiss="dismissAuditToast"
            />
        </Teleport>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'
import StepUpModal from '../components/shared/StepUpModal.vue'
import AuditReceiptToast from '../components/shared/AuditReceiptToast.vue'

const loading = ref(false)
const loadingParking = ref(false)
const saving = ref(false)
const activeTab = ref('barriers')
const barriers = ref([])
const permits = ref([])
const permitSearch = ref('')
const activeOnly = ref(true)

const commandTarget = ref(null)
const pendingCommand = ref('')
const commandReason = ref('')
const commandError = ref('')

const historyBarrier = ref(null)
const historyItems = ref([])
const laneOptions = ref([])

const showAddBarrier = ref(false)
const addBarrierError = ref('')
const newBarrier = ref({ name: '', laneId: null })

// Step-up state
const stepUpVisible = ref(false)

// Audit toast state
const auditToast = ref({
    visible: false,
    type: 'success',
    title: '',
    message: '',
    receiptId: '',
    timestamp: '',
})

const tabs = [
    { id: 'barriers', label: 'Barriers' },
    { id: 'parking', label: 'Parking' },
]

const isHighRiskCommand = computed(() => {
    return ['LockClosed', 'EmergencyRelease', 'ForceOpen'].includes(pendingCommand.value)
})

function stateClass(s) {
    if (!s) return ''
    return s === 'Open' ? 'success' : s === 'Closed' || s === 'Unknown' ? 'muted' : s === 'Fault' || s === 'LockedClosed' ? 'danger' : 'warn'
}

function formatDate(utc) {
    if (!utc) return '&mdash;'
    return new Date(utc).toLocaleString('vi-VN')
}

function showAuditToast(type, title, message, receiptId) {
    auditToast.value = {
        visible: true,
        type,
        title,
        message,
        receiptId,
        timestamp: new Date().toLocaleString('vi-VN'),
    }
}

function dismissAuditToast() {
    auditToast.value.visible = false
}

async function loadAll() {
    loading.value = true
    try {
        const [barriersRes, lanesRes] = await Promise.all([
            enterpriseApi.getBarriers({ active: true }),
            enterpriseApi.getLaneHealth(),
        ])
        barriers.value = barriersRes.data || []
        laneOptions.value = lanesRes.data || []
        if (activeTab.value === 'parking') await loadParking()
    } catch (e) {
        console.error('Failed to load', e)
    } finally {
        loading.value = false
    }
}

async function loadParking() {
    loadingParking.value = true
    try {
        const params = { activeOnly: activeOnly.value || undefined, pageSize: 100 }
        const res = await enterpriseApi.getParkingPermits(params)
        permits.value = res.data?.items || []
    } catch (e) {
        console.error('Failed to load permits', e)
    } finally {
        loadingParking.value = false
    }
}

function sendCommand(barrier, command) {
    commandTarget.value = barrier
    pendingCommand.value = command
    commandReason.value = ''
    commandError.value = ''
    // The modal will open, user fills reason, then confirmCommand triggers step-up for high-risk commands
}

function cancelCommand() {
    commandTarget.value = null
    pendingCommand.value = ''
    commandReason.value = ''
    commandError.value = ''
}

async function confirmCommand() {
    if (!commandTarget.value || !commandReason.value) return

    // Require step-up for high-risk commands before executing
    if (isHighRiskCommand.value) {
        stepUpVisible.value = true
        return
    }

    await executeCommand()
}

async function executeCommand() {
    if (!commandTarget.value || !commandReason.value) return
    saving.value = true
    commandError.value = ''
    try {
        await enterpriseApi.recordBarrierCommand(commandTarget.value.barrierId, {
            command: pendingCommand.value,
            reason: commandReason.value,
        })
        commandTarget.value.state = pendingCommand.value === 'Open' ? 'Open' :
            pendingCommand.value === 'Close' ? 'Closed' :
            pendingCommand.value === 'HoldOpen' ? 'HeldOpen' :
            pendingCommand.value === 'LockClosed' ? 'LockedClosed' :
            commandTarget.value.state

        const receiptId = `BR-${Date.now()}`
        showAuditToast('success', 'Command sent', `${pendingCommand.value} sent to ${commandTarget.value.name}`, receiptId)
        commandTarget.value = null
    } catch (e) {
        commandError.value = e.response?.data?.message || e.message
        showAuditToast('danger', 'Command failed', commandError.value, '')
    } finally {
        saving.value = false
    }
}

function onStepUpCancelled() {
    stepUpVisible.value = false
    commandTarget.value = null
    pendingCommand.value = ''
    commandReason.value = ''
}

async function onStepUpConfirmed(result) {
    stepUpVisible.value = false
    // Proceed with the command after step-up
    if (commandTarget.value && commandReason.value) {
        await confirmCommand()
    }
}

async function simulateCommand(barrier) {
    saving.value = true
    try {
        const res = await enterpriseApi.simulateBarrierCommand(barrier.barrierId, {
            command: 'Open',
            reason: 'Frontend simulation test',
        })
        showAuditToast('info', 'Simulation', `${res.data?.simulatedCommand} -> ${res.data?.result}`, `SIM-${Date.now()}`)
    } catch (e) {
        showAuditToast('danger', 'Simulation failed', e.response?.data?.message || e.message, '')
    } finally {
        saving.value = false
    }
}

async function showHistory(barrier) {
    historyBarrier.value = barrier
    try {
        const res = await enterpriseApi.getBarrierCommands(barrier.barrierId, { pageSize: 20 })
        historyItems.value = res.data?.items || []
    } catch (e) {
        historyItems.value = []
    }
}

async function submitBarrier() {
    if (!newBarrier.value.name) { addBarrierError.value = 'Name is required.'; return }
    addBarrierError.value = ''
    saving.value = true
    try {
        await enterpriseApi.createBarrier({
            laneId: newBarrier.value.laneId,
            name: newBarrier.value.name,
            state: 'Closed',
        })
        showAddBarrier.value = false
        newBarrier.value = { name: '', laneId: null }
        await loadAll()
    } catch (e) {
        addBarrierError.value = e.response?.data?.message || e.message
    } finally {
        saving.value = false
    }
}

onMounted(loadAll)
</script>
