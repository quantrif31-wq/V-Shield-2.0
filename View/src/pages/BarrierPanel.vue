<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Kiểm soát hạ tầng</span>
                <h1 class="page-title">Kiểm soát rào chắn & bãi đỗ</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-secondary" :disabled="loading" @click="loadAll">Làm mới</button>
            </div>
        </div>

        <div class="tab-bar">
            <button v-for="t in tabs" :key="t.id" :class="{ active: activeTab === t.id }" @click="activeTab = t.id">
                {{ t.label }}
            </button>
        </div>

        <section v-if="activeTab === 'barriers'" class="ops-panel">
            <div class="panel-head">
                <h2 class="panel-title">Rào chắn</h2>
                <button class="btn btn-primary" @click="showAddBarrier = true">Thêm rào chắn</button>
            </div>
            <div v-if="loading" class="empty-card">Đang tải rào chắn...</div>
            <div v-else-if="barriers.length === 0" class="empty-card">Chưa có rào chắn nào được cấu hình.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr><th>Tên</th><th>Làn</th><th>Trạng thái</th><th>Hoạt động</th><th>Thao tác</th></tr>
                    </thead>
                    <tbody>
                        <tr v-for="b in barriers" :key="b.barrierId">
                            <td><strong>{{ b.name }}</strong></td>
                            <td>{{ b.lane?.name || '&mdash;' }}</td>
                            <td><span class="soft-chip" :class="stateClass(b.state)">{{ stateLabel(b.state) }}</span></td>
                            <td>{{ b.isActive ? 'Có' : 'Không' }}</td>
                            <td>
                                <div class="chip-row">
                                    <button class="btn btn-sm btn-success" @click="sendCommand(b, 'Open')">Mở</button>
                                    <button class="btn btn-sm btn-secondary" @click="sendCommand(b, 'Close')">Đóng</button>
                                    <button class="btn btn-sm btn-ghost" @click="sendCommand(b, 'HoldOpen')">Giữ</button>
                                    <button class="btn btn-sm btn-danger" @click="sendCommand(b, 'LockClosed')">Khóa</button>
                                    <button class="btn btn-sm btn-ghost" @click="simulateCommand(b)">Mô phỏng</button>
                                    <button class="btn btn-sm btn-ghost" @click="showHistory(b)">Lịch sử</button>
                                </div>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </section>

        <section v-if="activeTab === 'parking'" class="ops-panel">
            <div class="panel-head">
                <h2 class="panel-title">Giấy phép đỗ xe</h2>
                <button class="btn btn-primary" @click="showAddPermit = true">Cấp phép</button>
            </div>
            <div class="toolbar-shell">
                <div class="search-bar">
                    <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <circle cx="11" cy="11" r="8" /><path d="M21 21l-4.35-4.35" />
                    </svg>
                    <input v-model="permitSearch" type="text" placeholder="Tìm theo giấy phép..." />
                </div>
                <label class="checkbox-label">
                    <input v-model="activeOnly" type="checkbox" @change="loadParking" /> Chỉ còn hiệu lực
                </label>
            </div>
            <div v-if="loadingParking" class="empty-card">Đang tải giấy phép...</div>
            <div v-else-if="permits.length === 0" class="empty-card">Không có giấy phép đỗ xe.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr><th>Khu vực</th><th>Phương tiện</th><th>Loại</th><th>Hiệu lực</th><th>Trạng thái</th></tr>
                    </thead>
                    <tbody>
                        <tr v-for="p in permits" :key="p.parkingPermitId">
                            <td>{{ p.parkingArea?.name || '&mdash;' }}</td>
                            <td>{{ p.vehicle?.licensePlate || '&mdash;' }}</td>
                            <td>{{ p.permitType }}</td>
                            <td>{{ formatDate(p.validFromUtc) }} &rarr; {{ formatDate(p.validToUtc) }}</td>
                            <td><span class="soft-chip" :class="p.isRevoked ? 'danger' : 'success'">{{ p.isRevoked ? 'Đã thu hồi' : 'Đang hoạt động' }}</span></td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </section>

        <Teleport to="body">
            <div v-if="commandTarget" class="modal-overlay" @click.self="cancelCommand">
                <div class="modal-panel" style="max-width: 460px;">
                    <div class="modal-header">
                        <h2>Lệnh rào chắn: {{ commandTarget.name }}</h2>
                        <button class="btn-close" @click="cancelCommand">&times;</button>
                    </div>
                    <div class="modal-body">
                        <p>Gửi <strong>{{ commandLabel(pendingCommand) }}</strong> tới rào chắn <strong>{{ commandTarget.name }}</strong></p>
                        <div v-if="isHighRiskCommand" class="alert alert-warning" style="margin-bottom: 12px;">
                            <strong>&#9888; Lệnh rủi ro cao:</strong> {{ commandLabel(pendingCommand) }} yêu cầu xác thực nâng cấp.
                        </div>
                        <div class="form-group">
                            <label>Lý do *</label>
                            <textarea v-model="commandReason" class="form-control" rows="2" placeholder="Nhập lý do bắt buộc cho lệnh này"></textarea>
                        </div>
                        <div v-if="commandError" class="alert alert-danger">{{ commandError }}</div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="cancelCommand">Hủy bỏ</button>
                        <button class="btn btn-primary" :disabled="saving || !commandReason" @click="confirmCommand">
                            {{ saving ? 'Đang gửi...' : `Gửi ${commandLabel(pendingCommand)}` }}
                        </button>
                    </div>
                </div>
            </div>

            <div v-if="historyBarrier" class="modal-overlay" @click.self="historyBarrier = null">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Lịch sử lệnh: {{ historyBarrier.name }}</h2>
                        <button class="btn-close" @click="historyBarrier = null">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div v-if="historyItems.length === 0" class="text-muted">Chưa có lệnh nào được ghi nhận.</div>
                        <div v-else class="table-container">
                            <table class="data-table">
                                <thead><tr><th>Thời gian</th><th>Lệnh</th><th>Lý do</th><th>Kết quả</th></tr></thead>
                                <tbody>
                                    <tr v-for="h in historyItems" :key="h.barrierCommandAuditId">
                                        <td>{{ formatDate(h.requestedAtUtc) }}</td>
                                        <td><span class="soft-chip">{{ commandLabel(h.command) }}</span></td>
                                        <td>{{ h.reason }}</td>
                                        <td>{{ h.result }}</td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="historyBarrier = null">Đóng</button>
                    </div>
                </div>
            </div>

            <div v-if="showAddBarrier" class="modal-overlay" @click.self="showAddBarrier = false">
                <div class="modal-panel" style="max-width: 440px;">
                    <div class="modal-header"><h2>Thêm rào chắn</h2><button class="btn-close" @click="showAddBarrier = false">&times;</button></div>
                    <div class="modal-body">
                        <div class="form-group"><label>Tên *</label><input v-model="newBarrier.name" class="form-control" /></div>
                        <div class="form-group"><label>Làn</label>
                            <select v-model="newBarrier.laneId" class="form-control">
                                <option :value="null">&mdash; Chọn &mdash;</option>
                                <option v-for="l in laneOptions" :key="l.laneId" :value="l.laneId">{{ l.name }}</option>
                            </select>
                        </div>
                        <div v-if="addBarrierError" class="alert alert-danger">{{ addBarrierError }}</div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="showAddBarrier = false">Hủy bỏ</button>
                        <button class="btn btn-primary" :disabled="saving" @click="submitBarrier">{{ saving ? 'Đang thêm...' : 'Thêm' }}</button>
                    </div>
                </div>
            </div>

            <!-- Step-Up Modal -->
            <StepUpModal
                :visible="stepUpVisible"
                action-label="Lệnh rào chắn rủi ro cao"
                :action-description="'Xác nhận lệnh rủi ro cao: ' + commandLabel(pendingCommand) + ' trên rào chắn ' + (commandTarget?.name || '')"
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
    { id: 'barriers', label: 'Rào chắn' },
    { id: 'parking', label: 'Bãi đỗ' },
]

const commandLabels = {
    Open: 'Mở',
    Close: 'Đóng',
    HoldOpen: 'Giữ mở',
    LockClosed: 'Khóa đóng',
    EmergencyRelease: 'Giải phóng khẩn cấp',
    ForceOpen: 'Mở cưỡng bức',
}

const stateLabels = {
    Open: 'Mở',
    Closed: 'Đóng',
    HeldOpen: 'Đang giữ mở',
    LockedClosed: 'Khóa đóng',
    Fault: 'Lỗi',
    Unknown: 'Không xác định',
}

function commandLabel(c) {
    return commandLabels[c] || c
}

function stateLabel(s) {
    return stateLabels[s] || s
}

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
        showAuditToast('success', 'Đã gửi lệnh', `${commandLabel(pendingCommand.value)} đã được gửi tới ${commandTarget.value.name}`, receiptId)
        commandTarget.value = null
    } catch (e) {
        commandError.value = e.response?.data?.message || e.message
        showAuditToast('danger', 'Gửi lệnh thất bại', commandError.value, '')
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
        showAuditToast('info', 'Mô phỏng', `${res.data?.simulatedCommand} -> ${res.data?.result}`, `SIM-${Date.now()}`)
    } catch (e) {
        showAuditToast('danger', 'Mô phỏng thất bại', e.response?.data?.message || e.message, '')
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
    if (!newBarrier.value.name) { addBarrierError.value = 'Tên là bắt buộc.'; return }
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

<style scoped>
.tab-bar button {
    transition: color var(--transition-fast), background var(--transition-fast), border-color var(--transition-fast), transform var(--transition-fast), box-shadow var(--transition-fast);
}

.tab-bar button:hover {
    transform: translateY(-1px);
}

.btn-close {
    transition: color var(--transition-fast), background var(--transition-fast), transform var(--transition-fast);
}

.btn-close:hover {
    transform: scale(1.12);
}
</style>
