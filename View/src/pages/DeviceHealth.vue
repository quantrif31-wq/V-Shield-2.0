<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Sức khỏe thiết bị</span>
                <h1 class="page-title">Sức khỏe & Thông minh thiết bị</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-secondary" @click="showRecordModal = true">Ghi nhận sức khỏe</button>
                <button class="btn btn-primary" @click="loadAll">Làm mới</button>
            </div>
        </div>

        <!-- Health Summary -->
        <section class="metric-grid four" v-if="healthSummary">
            <article class="metric-tile"><span class="metric-label">Tổng cộng</span><strong class="metric-value">{{ healthSummary.totalDevices || 0 }}</strong></article>
            <article class="metric-tile"><span class="metric-label">Khỏe mạnh</span><strong class="metric-value" style="color:var(--status-success-text);">{{ healthSummary.healthyCount || healthSummary.onlineCount || 0 }}</strong></article>
            <article class="metric-tile"><span class="metric-label">Suy giảm</span><strong class="metric-value" style="color:var(--status-warning-text);">{{ healthSummary.degradedCount || 0 }}</strong></article>
            <article class="metric-tile"><span class="metric-label">Nghiêm trọng</span><strong class="metric-value" style="color:var(--status-danger-text);">{{ healthSummary.criticalCount || healthSummary.offlineCount || 0 }}</strong></article>
        </section>

        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Gợi ý AI</span><h2 class="panel-title">Dự đoán sức khỏe</h2></div>
                </div>
                <div v-if="loading" class="empty-card">Đang tải...</div>
                <div v-else-if="insights.length === 0" class="empty-card">Chưa có dự đoán nào.</div>
                <div v-else class="device-insight-list">
                    <div v-for="di in insights" :key="di.deviceId" class="device-insight-item" :class="'pred-' + (di.predictedStatus || '').toLowerCase()">
                        <strong>{{ di.deviceName }}</strong>
                        <span class="small-meta">{{ di.predictedStatus }}</span>
                        <div class="small-meta">{{ di.summary }}</div>
                        <button class="btn btn-sm btn-ghost" style="margin-top:4px;" @click="openDeviceHealthHistory(di.deviceId, di.deviceName)">Xem lịch sử</button>
                    </div>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Lịch sử</span><h2 class="panel-title">Lịch sử sức khỏe thiết bị</h2></div>
                </div>
                <div class="form-group">
                    <label>Mã thiết bị</label>
                    <div class="chip-row">
                        <input v-model.number="selectedDevice" type="number" class="form-control" placeholder="Nhập mã thiết bị" />
                        <button class="btn btn-secondary btn-sm" @click="loadHealthHistory">Tải</button>
                    </div>
                </div>
                <div v-if="historyLoading" class="empty-card">Đang tải lịch sử...</div>
                <div v-else-if="healthHistory.length === 0 && selectedDevice" class="empty-card">Không có lịch sử sức khỏe cho thiết bị {{ selectedDevice }}.</div>
                <div v-else-if="healthHistory.length > 0" class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Thời gian</th><th>Trạng thái</th><th>Nội dung</th></tr></thead>
                        <tbody>
                            <tr v-for="h in healthHistory" :key="h.healthLogId || h.id">
                                <td class="table-sub">{{ formatTime(h.recordedAtUtc || h.timestamp) }}</td>
                                <td><span class="status-dot" :class="statusClass(h.status)"></span>{{ h.status }}</td>
                                <td>{{ h.message || h.description || '—' }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
                <div v-else class="empty-card">Nhập mã thiết bị rồi bấm Tải.</div>
            </article>
        </section>

        <section class="ops-grid two" style="margin-top:1rem;">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Chẩn đoán</span><h2 class="panel-title">Chẩn đoán thiết bị bằng AI</h2></div>
                </div>
                <div class="form-group">
                    <label>Mã thiết bị</label>
                    <input v-model.number="diagnoseDeviceId" type="number" class="form-control" placeholder="Nhập mã thiết bị" />
                </div>
                <button class="btn btn-primary" :disabled="!diagnoseDeviceId || diagnoseBusy" @click="runDiagnosis">
                    {{ diagnoseBusy ? 'Đang chẩn đoán...' : 'Chạy chẩn đoán AI' }}
                </button>
                <div v-if="diagnosisResult" class="result-card" style="margin-top:8px;">
                    <strong>Kết quả:</strong> {{ diagnosisResult }}
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Cấu hình</span><h2 class="panel-title">Phiên bản cấu hình</h2></div>
                </div>
                <div class="form-group">
                    <label>Mã thiết bị</label>
                    <div class="chip-row">
                        <input v-model.number="configDeviceId" type="number" class="form-control" placeholder="Nhập mã thiết bị" />
                        <button class="btn btn-secondary btn-sm" @click="loadConfigs">Tải</button>
                    </div>
                </div>
                <div v-if="configLoading" class="empty-card">Đang tải...</div>
                <div v-else-if="configs.length === 0 && configDeviceId" class="empty-card">Không có phiên bản cấu hình.</div>
                <div v-else-if="configs.length > 0" class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Phiên bản</th><th>Người tạo</th><th>Thời gian</th></tr></thead>
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
                        <h2>Ghi nhận sức khỏe thiết bị</h2>
                        <button class="btn-close" @click="showRecordModal = false">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="form-group">
                            <label>Mã thiết bị *</label>
                            <input v-model.number="recordForm.deviceId" type="number" class="form-control" placeholder="Bắt buộc" />
                        </div>
                        <div class="form-group">
                            <label>Trạng thái *</label>
                            <select v-model="recordForm.status" class="form-control">
                                <option value="Ok">Tốt</option>
                                <option value="Degraded">Suy giảm</option>
                                <option value="Fault">Lỗi</option>
                                <option value="Offline">Ngoại tuyến</option>
                                <option value="Tamper">Can thiệp</option>
                            </select>
                        </div>
                        <div class="form-group">
                            <label>Nội dung</label>
                            <textarea v-model="recordForm.message" class="form-control" rows="2" placeholder="Ghi chú sức khỏe (không bắt buộc)"></textarea>
                        </div>
                        <div v-if="recordResult" class="alert alert-success">{{ recordResult }}</div>
                        <div v-else-if="recordError" class="alert alert-danger">{{ recordError }}</div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="showRecordModal = false">Hủy</button>
                        <button class="btn btn-primary" :disabled="recordSaving || !recordForm.deviceId" @click="submitRecordHealth">
                            {{ recordSaving ? 'Đang ghi...' : 'Ghi nhận' }}
                        </button>
                    </div>
                </div>
            </div>

            <!-- Device Health History Modal -->
            <div v-if="historyModal.visible" class="modal-overlay" @click.self="historyModal.visible = false">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Lịch sử sức khỏe — {{ historyModal.deviceName }}</h2>
                        <button class="btn-close" @click="historyModal.visible = false">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div v-if="historyModal.loading" class="empty-card">Đang tải...</div>
                        <div v-else-if="historyModal.items.length === 0" class="empty-card">Không có lịch sử sức khỏe.</div>
                        <div v-else class="table-container">
                            <table class="data-table">
                                <thead><tr><th>Thời gian</th><th>Trạng thái</th><th>Nội dung</th></tr></thead>
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
                        <button class="btn btn-secondary" @click="historyModal.visible = false">Đóng</button>
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
        diagnosisResult.value = res.data?.diagnosis || res.data?.message || 'Hoàn tất chẩn đoán'
    } catch (e) {
        diagnosisResult.value = 'Chẩn đoán thất bại: ' + (e.response?.data?.message || e.message)
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
        recordResult.value = 'Đã ghi nhận sức khỏe thành công!'
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
