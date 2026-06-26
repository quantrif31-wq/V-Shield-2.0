<template>
    <div class="page-container animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">UEBA</span>
                <h1 class="page-title">User & Entity Behavior Analytics</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-secondary" @click="activeTab = 'profiles'" :class="{ active: activeTab === 'profiles' }">Profiles</button>
                <button class="btn btn-secondary" @click="activeTab = 'anomalies'" :class="{ active: activeTab === 'anomalies' }">Bat thuong</button>
                <button class="btn btn-primary" @click="activeTab = 'summary'" :class="{ active: activeTab === 'summary' }">Tong quan</button>
            </div>
        </div>

        <section v-if="activeTab === 'summary'" class="ops-grid two">
            <article class="ops-panel">
                <span class="panel-kicker">Overview</span>
                <h2 class="panel-title">UEBA Dashboard</h2>
                <div v-if="summaryLoading" class="empty-card">Dang tai...</div>
                <div v-else-if="summary" class="summary-metrics">
                    <div class="metric-tile">
                        <span class="metric-label">Bat thuong dang mo</span>
                        <strong class="metric-value">{{ summary.openAnomalies }}</strong>
                    </div>
                    <div class="metric-tile">
                        <span class="metric-label">Da xu ly hom nay</span>
                        <strong class="metric-value">{{ summary.resolvedToday }}</strong>
                    </div>
                    <div class="metric-tile">
                        <span class="metric-label">Profile rui ro cao</span>
                        <strong class="metric-value">{{ summary.highRiskProfiles }}</strong>
                    </div>
                    <div class="metric-tile">
                        <span class="metric-label">Tong profile</span>
                        <strong class="metric-value">{{ summary.totalProfiles }}</strong>
                    </div>
                </div>
                <div v-if="summary?.typeDistribution?.length" class="type-dist">
                    <h3>Phan loai bat thuong</h3>
                    <div v-for="item in summary.typeDistribution" :key="item.type" class="dist-row">
                        <span>{{ typeLabel(item.type) }}</span>
                        <div class="dist-bar-wrapper">
                            <div class="dist-bar" :style="{ width: distributionWidth(item.count) }"></div>
                        </div>
                        <strong>{{ item.count }}</strong>
                    </div>
                </div>
            </article>

            <article class="ops-panel">
                <span class="panel-kicker">AI Risk Graph</span>
                <h2 class="panel-title">Giai thich rui ro AI</h2>
                <div v-if="riskExplaining.loading" class="empty-card">AI dang phan tich...</div>
                <div v-else-if="riskExplaining.result" class="risk-explanation">
                    <div class="rec-header">
                        <span class="soft-chip" :class="riskLevelClass(riskExplaining.result.severity)">
                            {{ riskExplaining.result.severity }}
                        </span>
                        <small>Confidence: {{ riskExplaining.result.confidence }}</small>
                    </div>
                    <p>{{ riskExplaining.result.summary }}</p>
                    <div v-if="riskExplaining.result.reasoningSummary" class="rec-reasoning">
                        <strong>Phan tich:</strong>
                        <p>{{ riskExplaining.result.reasoningSummary }}</p>
                    </div>
                    <div class="rec-actions">
                        <button class="btn btn-primary btn-sm" @click="approveAiRisk(riskExplaining.result.recommendationId)">
                            Duyet
                        </button>
                        <button class="btn btn-ghost btn-sm" @click="rejectAiRisk(riskExplaining.result.recommendationId)">
                            Tu choi
                        </button>
                    </div>
                </div>
                <div v-else class="empty-card">Chon nhan vien o khung ben canh de xem giai thich.</div>
            </article>

            <article class="ops-panel">
                <span class="panel-kicker">High Risk</span>
                <h2 class="panel-title">Nhan vien can chu y</h2>
                <div v-if="profilesLoading" class="empty-card">Dang tai...</div>
                <div v-else-if="profiles.length === 0" class="empty-card">Chua co du lieu profile.</div>
                <div v-else class="risk-list">
                    <div v-for="profile in profiles.slice(0, 10)" :key="profile.profileId" class="risk-item" :class="riskClass(profile.riskScore)">
                        <div class="risk-head">
                            <strong>{{ profile.employee?.fullName || 'NV#' + profile.employeeId }}</strong>
                            <span class="risk-score" :class="riskClass(profile.riskScore)">{{ profile.riskScore }}</span>
                        </div>
                        <div class="risk-meta">
                            <span>Access: {{ profile.totalAccessCount }}</span>
                            <span>Bypass: {{ profile.bypassRate }}%</span>
                            <span>Cuoi tuan: {{ profile.weekendAccessRatio }}%</span>
                        </div>
                        <div class="risk-actions">
                            <button class="btn btn-ghost btn-sm" @click="explainRisk(profile.employeeId)">Giai thich AI</button>
                        </div>
                    </div>
                </div>
            </article>
        </section>

        <section v-if="activeTab === 'profiles'" class="card panel">
            <div class="toolbar-shell">
                <div class="toolbar-filters">
                    <input v-model="profileSearch" type="text" placeholder="Tim ten hoac employeeId..." class="filter-input" />
                    <button class="btn btn-secondary" @click="loadProfiles">Tai lai</button>
                </div>
            </div>
            <div v-if="profilesLoading" class="empty-card">Dang tai...</div>
            <div v-else-if="profiles.length === 0" class="empty-card">Chua co profile. Hay tao access log de build baseline.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>Nhan vien</th>
                            <th>Access</th>
                            <th>Bat dau</th>
                            <th>Ket thuc</th>
                            <th>Cuoi tuan</th>
                            <th>Bypass</th>
                            <th>Risk</th>
                            <th>Lan cuoi</th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="profile in filteredProfiles" :key="profile.profileId">
                            <td>{{ profile.employee?.fullName || 'NV#' + profile.employeeId }}</td>
                            <td>{{ profile.totalAccessCount }}</td>
                            <td>{{ profile.typicalStartHour }}h</td>
                            <td>{{ profile.typicalEndHour }}h</td>
                            <td>{{ profile.weekendAccessRatio }}%</td>
                            <td>{{ profile.bypassRate }}%</td>
                            <td><span class="risk-score" :class="riskClass(profile.riskScore)">{{ profile.riskScore }}</span></td>
                            <td>{{ profile.daysSinceLastAccess }} ngay</td>
                            <td class="table-actions">
                                <button class="btn btn-ghost btn-sm" @click="explainRisk(profile.employeeId)">AI</button>
                                <button class="btn btn-ghost btn-sm" @click="rebuild(profile.employeeId)">Rebuild</button>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </section>

        <section v-if="activeTab === 'anomalies'" class="card panel">
            <div class="toolbar-shell">
                <div class="toolbar-filters">
                    <select v-model="anomalyFilter.severity" class="filter-select" @change="loadAnomalies">
                        <option value="">Tat ca muc</option>
                        <option value="cao">Cao</option>
                        <option value="trung-binh">Trung binh</option>
                        <option value="thap">Thap</option>
                    </select>
                    <select v-model="anomalyFilter.status" class="filter-select" @change="loadAnomalies">
                        <option value="">Tat ca trang thai</option>
                        <option value="Open">Mo</option>
                        <option value="Resolved">Da xu ly</option>
                        <option value="FalsePositive">False positive</option>
                    </select>
                    <button class="btn btn-primary" @click="loadAnomalies">Tai</button>
                </div>
            </div>
            <div v-if="anomaliesLoading" class="empty-card">Dang tai...</div>
            <div v-else-if="anomalies.length === 0" class="empty-card">Khong co bat thuong.</div>
            <div v-else class="anomaly-list">
                <div v-for="anomaly in anomalies" :key="anomaly.anomalyId" class="anomaly-card" :class="anomaly.severity">
                    <div class="anomaly-head">
                        <span class="anomaly-type-badge" :class="anomaly.severity">{{ typeLabel(anomaly.anomalyType) }}</span>
                        <span class="anomaly-severity" :class="anomaly.severity">{{ anomaly.severity }}</span>
                        <span class="anomaly-status" :class="anomaly.status">{{ anomaly.status }}</span>
                    </div>
                    <p class="anomaly-desc">{{ anomaly.description }}</p>
                    <div class="anomaly-meta">
                        <span>{{ anomaly.employee?.fullName || 'NV#' + anomaly.employeeId }}</span>
                        <span>{{ formatDate(anomaly.eventTimestamp) }}</span>
                        <span v-if="anomaly.supportingData" class="anomaly-data">{{ anomaly.supportingData }}</span>
                    </div>
                    <div v-if="anomaly.status === 'Open'" class="anomaly-actions">
                        <button class="btn btn-primary btn-sm" @click="resolve(anomaly.anomalyId)">Xu ly</button>
                        <button class="btn btn-ghost btn-sm" @click="falsePositive(anomaly.anomalyId)">FP</button>
                    </div>
                </div>
            </div>
        </section>
    </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import {
    getUebaProfiles,
    rebuildUebaProfile,
    getUebaAnomalies,
    resolveUebaAnomaly,
    markUebaAnomalyFalsePositive,
    getUebaSummary,
    explainEmployeeRisk,
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

const riskExplaining = reactive({
    employeeId: null,
    loading: false,
    result: null,
})

const filteredProfiles = computed(() => {
    if (!profileSearch.value) return profiles.value
    const query = profileSearch.value.toLowerCase()
    return profiles.value.filter((profile) =>
        profile.employee?.fullName?.toLowerCase().includes(query) ||
        String(profile.employeeId).includes(query)
    )
})

const typeLabel = (type) => ({
    UnusualTime: 'Gio bat thuong',
    UnusualGate: 'Cong la',
    UnusualFrequency: 'Tan suat cao',
    OutOfHours: 'Ngoai gio',
    RapidSuccession: 'Lien tiep nhanh',
    BypassPattern: 'Bypass',
    FirstTimeAccess: 'Lan dau',
})[type] || type

const distributionWidth = (count) => {
    const max = Math.max(...(summary.value?.typeDistribution?.map((item) => item.count) || [1]))
    return `${Math.max(8, (count / max) * 100)}%`
}

const riskClass = (score) => {
    if (score > 60) return 'high'
    if (score > 30) return 'medium'
    return 'low'
}

const riskLevelClass = (severity) => {
    switch ((severity || '').toLowerCase()) {
        case 'critical':
        case 'high':
            return 'danger'
        case 'medium':
            return 'warning'
        default:
            return 'success'
    }
}

const formatDate = (value) => value ? new Date(value).toLocaleString('vi-VN') : '--'

const explainRisk = async (employeeId) => {
    riskExplaining.employeeId = employeeId
    riskExplaining.loading = true
    riskExplaining.result = null
    try {
        const { data } = await explainEmployeeRisk(employeeId)
        riskExplaining.result = data
    } catch (error) {
        riskExplaining.result = {
            severity: 'Low',
            confidence: 0,
            summary: `Khong the phan tich: ${error.response?.data?.message || error.message}`,
            recommendationId: null,
        }
    } finally {
        riskExplaining.loading = false
    }
}

const loadSummary = async () => {
    summaryLoading.value = true
    try {
        const { data } = await getUebaSummary()
        summary.value = data
    } finally {
        summaryLoading.value = false
    }
}

const loadProfiles = async () => {
    profilesLoading.value = true
    try {
        const { data } = await getUebaProfiles()
        profiles.value = data
        if (!riskExplaining.employeeId && data.length > 0) {
            const candidate = data.find((item) => item.riskScore > 0) || data[0]
            if (candidate?.employeeId) {
                await explainRisk(candidate.employeeId)
            }
        }
    } finally {
        profilesLoading.value = false
    }
}

const loadAnomalies = async () => {
    anomaliesLoading.value = true
    try {
        const params = { maxResults: 50 }
        if (anomalyFilter.severity) params.severity = anomalyFilter.severity
        if (anomalyFilter.status) params.status = anomalyFilter.status
        const { data } = await getUebaAnomalies(params)
        anomalies.value = data
    } finally {
        anomaliesLoading.value = false
    }
}

const rebuild = async (employeeId) => {
    await rebuildUebaProfile(employeeId)
    await Promise.all([loadProfiles(), loadSummary(), loadAnomalies()])
}

const resolve = async (id) => {
    await resolveUebaAnomaly(id, { resolution: 'Da kiem tra va xu ly.' })
    await Promise.all([loadSummary(), loadAnomalies()])
}

const falsePositive = async (id) => {
    await markUebaAnomalyFalsePositive(id)
    await Promise.all([loadSummary(), loadAnomalies()])
}

const approveAiRisk = async (id) => {
    if (!id) return
    const { enterpriseAiApi } = await import('../services/enterpriseAiApi')
    await enterpriseAiApi.reviewRecommendation(id, 'Approved', 'Phe duyet sau khi xem xet')
    riskExplaining.result = null
}

const rejectAiRisk = async (id) => {
    if (!id) return
    const { enterpriseAiApi } = await import('../services/enterpriseAiApi')
    await enterpriseAiApi.reviewRecommendation(id, 'Rejected', 'Khong dong y voi phan tich')
    riskExplaining.result = null
}

onMounted(async () => {
    await Promise.all([loadSummary(), loadProfiles(), loadAnomalies()])
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
    flex-wrap: wrap;
}

.risk-actions {
    display: flex;
    justify-content: flex-end;
    margin-top: 10px;
}

.table-actions {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
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
    flex-wrap: wrap;
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
.anomaly-status.FalsePositive { background: rgba(24, 49, 77, 0.08); color: var(--text-secondary); }

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
