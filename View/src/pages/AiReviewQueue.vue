<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">AI quality</span>
                <h1 class="page-title">AI Review Queue</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="loadAll">Refresh</button>
            </div>
        </div>
        <section class="ops-grid three">
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Summary</span><h2 class="panel-title">Model Quality</h2></div>
                </div>
                <div v-if="summary" class="kpi-row wrap">
                    <div class="kpi-card"><strong>{{ summary.pendingReviews }}</strong><span>Pending</span></div>
                    <div class="kpi-card"><strong>{{ summary.totalReviewed }}</strong><span>Reviewed</span></div>
                    <div class="kpi-card"><strong>{{ summary.precisionProxy }}%</strong><span>Precision</span></div>
                    <div class="kpi-card"><strong>{{ summary.totalFalsePositive }}</strong><span>FP</span></div>
                    <div class="kpi-card"><strong>{{ summary.totalFalseNegative }}</strong><span>FN</span></div>
                    <div class="kpi-card"><strong>{{ summary.totalTrainingCandidate }}</strong><span>Training</span></div>
                    <div class="kpi-card" :class="summary.driftDetected ? 'drift-alert' : ''">
                        <strong>{{ summary.recentDriftScore ?? '—' }}</strong><span>Drift {{ summary.driftDetected ? '⚠️' : '✓' }}</span>
                    </div>
                </div>
                <div v-else class="empty-card">Loading summary...</div>
            </article>
            <article class="ops-panel wide-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Items</span><h2 class="panel-title">Adjudication Items</h2></div>
                    <div class="panel-actions">
                        <select v-model="statusFilter" @change="loadItems" class="form-select">
                            <option value="">All</option>
                            <option value="Pending">Pending</option>
                            <option value="Reviewed">Reviewed</option>
                        </select>
                        <select v-model="outcomeFilter" @change="loadItems" class="form-select">
                            <option value="">All Outcomes</option>
                            <option value="Confirmed">Confirmed</option>
                            <option value="FalsePositive">False Positive</option>
                            <option value="FalseNegative">False Negative</option>
                            <option value="TrainingCandidate">Training Candidate</option>
                        </select>
                    </div>
                </div>
                <div v-if="loadingItems" class="empty-card">Loading...</div>
                <div v-else-if="items.length === 0" class="empty-card">No adjudication items.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>ID</th><th>AI Source</th><th>Model</th><th>Confidence</th><th>Status</th><th>Outcome</th><th>Review Note</th><th>Actions</th></tr></thead>
                        <tbody>
                            <tr v-for="item in items" :key="item.aiAdjudicationItemId">
                                <td>{{ item.aiAdjudicationItemId }}</td>
                                <td>{{ item.aiSource }}</td>
                                <td class="table-sub">{{ item.modelVersion }}</td>
                                <td>{{ item.confidence != null ? (item.confidence * 100).toFixed(0) + '%' : '—' }}</td>
                                <td><span class="badge" :class="item.status === 'Reviewed' ? 'badge-success' : 'badge-warn'">{{ item.status }}</span></td>
                                <td><span v-if="item.outcome" class="badge" :class="outcomeClass(item.outcome)">{{ item.outcome }}</span><span v-else class="table-sub">—</span></td>
                                <td class="table-sub">{{ item.reviewNote || '—' }}</td>
                                <td>
                                    <button v-if="item.status === 'Pending'" class="btn btn-secondary btn-sm" @click="reviewItem(item)">Review</button>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
            <article class="ops-panel">
                <div class="panel-head">
                    <div><span class="panel-kicker">Metrics</span><h2 class="panel-title">Recent Performance Metrics</h2></div>
                </div>
                <div v-if="loadingMetrics" class="empty-card">Loading...</div>
                <div v-else-if="metrics.length === 0" class="empty-card">No metrics recorded.</div>
                <div v-else class="table-container">
                    <table class="data-table">
                        <thead><tr><th>Metric</th><th>Value</th><th>Source</th><th>Time</th></tr></thead>
                        <tbody>
                            <tr v-for="m in metrics" :key="m.aiPerformanceMetricId">
                                <td>{{ m.metricName }}</td>
                                <td><strong>{{ m.metricValue }}</strong></td>
                                <td class="table-sub">{{ m.aiSource }}</td>
                                <td class="table-sub">{{ new Date(m.capturedAtUtc).toLocaleString() }}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </article>
        </section>
        <div v-if="reviewTarget" class="modal-overlay" @click.self="reviewTarget = null">
            <div class="modal-box">
                <h3>Review Adjudication #{{ reviewTarget.aiAdjudicationItemId }}</h3>
                <p class="table-sub">AI Source: {{ reviewTarget.aiSource }} | Model: {{ reviewTarget.modelVersion }} | Confidence: {{ reviewTarget.confidence }}</p>
                <div class="form-group">
                    <label>Outcome</label>
                    <select v-model="reviewForm.outcome" class="form-select">
                        <option value="Confirmed">Confirmed</option>
                        <option value="FalsePositive">False Positive</option>
                        <option value="FalseNegative">False Negative</option>
                        <option value="TrainingCandidate">Training Candidate</option>
                    </select>
                </div>
                <div class="form-group">
                    <label>Review Note</label>
                    <textarea v-model="reviewForm.reviewNote" class="form-input" rows="3"></textarea>
                </div>
                <div class="modal-actions">
                    <button class="btn btn-secondary" @click="reviewTarget = null">Cancel</button>
                    <button class="btn btn-primary" :disabled="busy" @click="submitReview">{{ busy ? 'Submitting...' : 'Submit Review' }}</button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const items = ref([])
const metrics = ref([])
const summary = ref(null)
const loadingItems = ref(true)
const loadingMetrics = ref(true)
const busy = ref(false)
const statusFilter = ref('')
const outcomeFilter = ref('')
const reviewTarget = ref(null)
const reviewForm = ref({ outcome: 'Confirmed', reviewNote: '' })

async function loadAll() {
    await Promise.all([loadItems(), loadMetrics(), loadSummary()])
}

async function loadSummary() {
    try { const res = await enterpriseApi.getAiMetricsSummary(); summary.value = res.data }
    catch { summary.value = null }
}

async function loadItems() {
    loadingItems.value = true
    try {
        const params = { page: 1, pageSize: 100 }
        if (statusFilter.value) params.status = statusFilter.value
        if (outcomeFilter.value) params.outcome = outcomeFilter.value
        const res = await enterpriseApi.getAiAdjudications(params)
        items.value = res.data.items || []
    } catch { items.value = [] }
    finally { loadingItems.value = false }
}

async function loadMetrics() {
    loadingMetrics.value = true
    try { const res = await enterpriseApi.getAiMetrics({ limit: 30 }); metrics.value = Array.isArray(res.data) ? res.data : [] }
    catch { metrics.value = [] }
    finally { loadingMetrics.value = false }
}

function reviewItem(item) {
    reviewTarget.value = item
    reviewForm.value = { outcome: 'Confirmed', reviewNote: '' }
}

async function submitReview() {
    if (!reviewTarget.value) return
    busy.value = true
    try {
        await enterpriseApi.reviewAiAdjudication(reviewTarget.value.aiAdjudicationItemId, reviewForm.value)
        reviewTarget.value = null
        await loadAll()
    } finally { busy.value = false }
}

function outcomeClass(o) {
    if (o === 'Confirmed') return 'badge-success'
    if (o === 'FalsePositive' || o === 'FalseNegative') return 'badge-danger'
    if (o === 'TrainingCandidate') return 'badge-primary'
    return 'badge-info'
}

onMounted(loadAll)
</script>
