<template>
    <div class="page-container animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">UEBA</span>
                <h1 class="page-title">User & Entity Behavior Analytics</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-secondary" @click="activeTab = 'profiles'" :class="{ active: activeTab === 'profiles' }">Profile</button>
                <button class="btn btn-secondary" @click="activeTab = 'anomalies'" :class="{ active: activeTab === 'anomalies' }">Bất thường</button>
                <button class="btn btn-primary" @click="activeTab = 'summary'" :class="{ active: activeTab === 'summary' }">Tổng quan</button>
            </div>
        </div>

        <section v-if="activeTab === 'summary'" class="ops-grid two">
            <article class="ops-panel">
                <span class="panel-kicker">Overview</span>
                <h2 class="panel-title">UEBA Dashboard</h2>
                <div v-if="summaryLoading" class="empty-card">Đang tải...</div>
                <div v-else-if="summary" class="summary-metrics">
                    <div class="metric-tile">
                        <span class="metric-label">Bất thường đang mở</span>
                        <strong class="metric-value">{{ summary.openAnomalies }}</strong>
                    </div>
                    <div class="metric-tile">
                        <span class="metric-label">Đã xử lý hôm nay</span>
                        <strong class="metric-value">{{ summary.resolvedToday }}</strong>
                    </div>
                    <div class="metric-tile">
                        <span class="metric-label">Profile rủi ro cao</span>
                        <strong class="metric-value">{{ summary.highRiskProfiles }}</strong>
                    </div>
                    <div class="metric-tile">
                        <span class="metric-label">Tổng profile</span>
                        <strong class="metric-value">{{ summary.totalProfiles }}</strong>
                    </div>
                </div>
                <div v-if="summary?.typeDistribution?.length" class="type-dist">
                    <h3>Phân loại bất thường</h3>
                    <div v-for="item in summary.typeDistribution" :key="item.type" class="dist-row">
                        <span>{{ typeLabel(item.type) }}</span>
                        <div class="dist-bar-wrapper">
                            <div class="dist-bar" :style="{ width: (item.count / Math.max(...summary.typeDistribution.map(t => t.count)) * 100) + '%' }"></div>
                        </div>
                        <strong>{{ item.count }}</strong>
                    </div>
                </div>
            </article>

            <article class="ops-panel">
                <span class="panel-kicker">High Risk</span>
                <h2 class="panel-title">Nhân viên rủi ro cao</h2>
                <div v-if="profilesLoading" class="empty-card">Đang tải...</div>
                <div v-else-if="profiles.length === 0" class="empty-card">Chưa có dữ liệu profile.</div>
                <div v-else class="risk-list">
                    <div v-for="p in profiles.slice(0, 10)" :key="p.profileId" class="risk-item" :class="riskClass(p.riskScore)">
                        <div class="risk-head">
                            <strong>{{ p.employee?.fullName || 'NV#' + p.employeeId }}</strong>
                            <span class="risk-score" :class="riskClass(p.riskScore)">{{ p.riskScore }}</span>
                        </div>
                        <div class="risk-meta">
                            <span>Access: {{ p.totalAccessCount }}</span>
                            <span>Bypass: {{ p.bypassRate }}%</span>
                            <span>Cuối tuần: {{ p.weekendAccessRatio }}%</span>
                        </div>
                    </div>
                </div>
            </article>
        </section>

        <section v-if="activeTab === 'profiles'" class="card panel">
            <div class="toolbar-shell">
                <div class="toolbar-filters">
                    <input v-model="profileSearch" type="text" placeholder="Tìm employeeId..." class="filter-input" />
                    <button class="btn btn-secondary" @click="loadProfiles">Tải lại</button>
                </div>
            </div>
            <div v-if="profilesLoading" class="empty-card">Đang tải...</div>
            <div v-else-if="profiles.length === 0" class="empty-card">Chưa có profile. Quét access log để tạo baseline.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>Nhân viên</th>
                            <th>Access</th>
                            <th>Giờ T2</th>
                            <th>Giờ T9</th>
                            <th>Cuối tuần</th>
                            <th>Bypass</th>
                            <th>Risk</th>
                            <th>Lần cuối</th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="p in filteredProfiles" :key="p.profileId">
                            <td>{{ p.employee?.fullName || 'NV#' + p.employeeId }}</td>
                            <td>{{ p.totalAccessCount }}</td>
                            <td>{{ p.typicalStartHour }}h</td>
                            <td>{{ p.typicalEndHour }}h</td>
                            <td>{{ p.weekendAccessRatio }}%</td>
                            <td>{{ p.bypassRate }}%</td>
                            <td><span class="risk-score" :class="riskClass(p.riskScore)">{{ p.riskScore }}</span></td>
                            <td>{{ p.daysSinceLastAccess }} ngày</td>
                            <td><button class="btn btn-ghost btn-sm" @click="rebuild(p.employeeId)">Rebuild</button></td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </section>

        <section v-if="activeTab === 'anomalies'" class="card panel">
            <div class="toolbar-shell">
                <div class="toolbar-filters">
                    <select v-model="anomalyFilter.severity" class="filter-select" @change="loadAnomalies">
                        <option value="">Tất cả mức</option>
                        <option value="cao">Cao</option>
                        <option value="trung-binh">TB</option>
                        <option value="thap">Thấp</option>
                    </select>
                    <select v-model="anomalyFilter.status" class="filter-select" @change="loadAnomalies">
                        <option value="">Tất cả trạng thái</option>
                        <option value="Open">Mở</option>
                        <option value="Resolved">Đã xử lý</option>
                    </select>
                    <button class="btn btn-primary" @click="loadAnomalies">Tải</button>
                </div>
            </div>
            <div v-if="anomaliesLoading" class="empty-card">Đang tải...</div>
            <div v-else-if="anomalies.length === 0" class="empty-card">Không có bất thường.</div>
            <div v-else class="anomaly-list">
                <div v-for="a in anomalies" :key="a.anomalyId" class="anomaly-card" :class="a.severity">
                    <div class="anomaly-head">
                        <span class="anomaly-type-badge" :class="a.severity">{{ typeLabel(a.anomalyType) }}</span>
                        <span class="anomaly-severity" :class="a.severity">{{ a.severity }}</span>
                        <span class="anomaly-status" :class="a.status">{{ a.status }}</span>
                    </div>
                    <p class="anomaly-desc">{{ a.description }}</p>
                    <div class="anomaly-meta">
                        <span>{{ a.employee?.fullName || 'NV#' + a.employeeId }}</span>
                        <span>{{ formatDate(a.eventTimestamp) }}</span>
                        <span v-if="a.supportingData" class="anomaly-data">{{ a.supportingData }}</span>
                    </div>
                    <div v-if="a.status === 'Open'" class="anomaly-actions">
                        <button class="btn btn-primary btn-sm" @click="resolve(a.anomalyId)">Xử lý</button>
                        <button class="btn btn-ghost btn-sm" @click="falsePositive(a.anomalyId)">FP</button>
                    </div>
                </div>
            </div>
        </section>
    </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import {
    getUebaProfiles, rebuildUebaProfile, getUebaAnomalies,
    resolveUebaAnomaly, markUebaAnomalyFalsePositive, getUebaSummary,
} from '../services/uebaApi'

const activeTab = ref('summary')

const summary = ref(null)
const summaryLoading = ref(false)

const profiles = ref([])
const profilesLoading = ref(false)
const profileSearch = ref('')

const anomalies = ref([])
const anomaliesLoading = ref(false)
const anomalyFilter = reactive({ severity: '', status: 'Open' })

const filteredProfiles = computed(() => {
    if (!profileSearch.value) return profiles.value
    const q = profileSearch.value.toLowerCase()
    return profiles.value.filter(p =>
        p.employee?.fullName?.toLowerCase().includes(q) ||
        String(p.employeeId).includes(q)
    )
})

const typeLabel = (type) => ({
    UnusualTime: 'Giờ bất thường',
    UnusualGate: 'Cổng lạ',
    UnusualFrequency: 'Tần suất cao',
    OutOfHours: 'Ngoài giờ',
    RapidSuccession: 'Liên tiếp nhanh',
    BypassPattern: 'Bypass',
    FirstTimeAccess: 'Lần đầu',
})[type] || type

const riskClass = (score) => {
    if (score > 60) return 'high'
    if (score > 30) return 'medium'
    return 'low'
}

const formatDate = (v) => v ? new Date(v).toLocaleString('vi-VN') : '--'

const loadSummary = async () => {
    summaryLoading.value = true
    try {
        const { data } = await getUebaSummary()
        summary.value = data
    } catch { /* ignore */ }
    finally { summaryLoading.value = false }
}

const loadProfiles = async () => {
    profilesLoading.value = true
    try {
        const { data } = await getUebaProfiles()
        profiles.value = data
    } catch { /* ignore */ }
    finally { profilesLoading.value = false }
}

const rebuild = async (employeeId) => {
    try {
        await rebuildUebaProfile(employeeId)
        await loadProfiles()
    } catch { /* ignore */ }
}

const loadAnomalies = async () => {
    anomaliesLoading.value = true
    try {
        const params = { maxResults: 50 }
        if (anomalyFilter.severity) params.severity = anomalyFilter.severity
        if (anomalyFilter.status) params.status = anomalyFilter.status
        const { data } = await getUebaAnomalies(params)
        anomalies.value = data
    } catch { /* ignore */ }
    finally { anomaliesLoading.value = false }
}

const resolve = async (id) => {
    try {
        await resolveUebaAnomaly(id, { resolution: 'Da kiem tra.' })
        anomalies.value = anomalies.value.filter(a => a.anomalyId !== id)
    } catch { /* ignore */ }
}

const falsePositive = async (id) => {
    try {
        await markUebaAnomalyFalsePositive(id)
        anomalies.value = anomalies.value.filter(a => a.anomalyId !== id)
    } catch { /* ignore */ }
}

onMounted(async () => {
    await Promise.all([loadSummary(), loadProfiles()])
})
</script>

<style scoped>
.summary-metrics {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 12px;
    margin-bottom: 20px;
}

.type-dist h3 {
    font-size: 0.9rem;
    margin-bottom: 12px;
    color: var(--text-secondary);
}

.dist-row {
    display: flex;
    align-items: center;
    gap: 10px;
    margin-bottom: 8px;
}

.dist-row span {
    min-width: 120px;
    font-size: 0.82rem;
    color: var(--text-secondary);
}

.dist-bar-wrapper {
    flex: 1;
    height: 8px;
    background: rgba(24, 49, 77, 0.06);
    border-radius: 10px;
    overflow: hidden;
}

.dist-bar {
    height: 100%;
    background: var(--accent-primary);
    border-radius: 10px;
    transition: width 0.4s ease;
}

.dist-row strong {
    min-width: 30px;
    text-align: right;
    font-size: 0.85rem;
}

.risk-list {
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.risk-item {
    padding: 10px 12px;
    border-radius: 12px;
    border: 1px solid rgba(24, 49, 77, 0.06);
    background: rgba(236, 244, 246, 0.4);
}

.risk-item.high { border-color: rgba(200, 50, 50, 0.2); }
.risk-item.medium { border-color: rgba(216, 155, 55, 0.2); }

.risk-head {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 6px;
}

.risk-head strong { font-size: 0.88rem; }

.risk-score {
    font-size: 0.78rem;
    font-weight: 700;
    padding: 1px 8px;
    border-radius: 10px;
}

.risk-score.high { background: rgba(200, 50, 50, 0.12); color: #c83232; }
.risk-score.medium { background: rgba(216, 155, 55, 0.12); color: #b86f21; }
.risk-score.low { background: rgba(84, 196, 211, 0.1); color: var(--accent-primary); }

.risk-meta {
    display: flex;
    gap: 14px;
    font-size: 0.74rem;
    color: var(--text-muted);
}

.anomaly-list {
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.anomaly-card {
    padding: 14px;
    border-radius: 16px;
    border: 1px solid rgba(24, 49, 77, 0.08);
    background: rgba(236, 244, 246, 0.5);
}

.anomaly-card.cao { border-color: rgba(200, 50, 50, 0.3); background: rgba(200, 50, 50, 0.04); }
.anomaly-card.trung-binh { border-color: rgba(216, 155, 55, 0.25); background: rgba(216, 155, 55, 0.04); }

.anomaly-head {
    display: flex;
    gap: 8px;
    align-items: center;
    margin-bottom: 8px;
}

.anomaly-type-badge {
    font-size: 0.72rem;
    font-weight: 700;
    padding: 2px 10px;
    border-radius: 20px;
    background: rgba(84, 196, 211, 0.1);
    color: var(--accent-primary);
}

.anomaly-type-badge.cao { background: rgba(200, 50, 50, 0.1); color: #c83232; }
.anomaly-type-badge.trung-binh { background: rgba(216, 155, 55, 0.1); color: #b86f21; }

.anomaly-severity { font-size: 0.68rem; padding: 1px 8px; border-radius: 12px; }
.anomaly-severity.cao { background: rgba(200, 50, 50, 0.12); color: #c83232; }
.anomaly-severity.trung-binh { background: rgba(216, 155, 55, 0.12); color: #b86f21; }
.anomaly-severity.thap { background: rgba(24, 49, 77, 0.06); color: var(--text-muted); }

.anomaly-status {
    font-size: 0.68rem;
    margin-left: auto;
    padding: 1px 8px;
    border-radius: 12px;
}

.anomaly-status.Open { background: rgba(84, 196, 211, 0.1); color: var(--accent-primary); }
.anomaly-status.Resolved { background: rgba(20, 134, 109, 0.1); color: var(--accent-success); }

.anomaly-desc {
    font-size: 0.88rem;
    line-height: 1.6;
    color: var(--text-primary);
    margin: 0 0 8px 0;
}

.anomaly-meta {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
    font-size: 0.78rem;
    color: var(--text-secondary);
}

.anomaly-data {
    font-family: monospace;
    background: rgba(24, 49, 77, 0.04);
    padding: 1px 6px;
    border-radius: 4px;
}

.anomaly-actions {
    display: flex;
    gap: 8px;
    margin-top: 10px;
}
</style>
