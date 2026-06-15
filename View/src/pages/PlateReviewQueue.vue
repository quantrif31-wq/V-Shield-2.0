<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">AI Adjudication</span>
                <h1 class="page-title">Plate Review Queue</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-secondary" :disabled="loading" @click="loadAll">Refresh</button>
            </div>
        </div>

        <section class="metric-grid three">
            <article class="metric-tile"><span class="metric-label">Pending</span><strong class="metric-value">{{ stats.pending }}</strong></article>
            <article class="metric-tile"><span class="metric-label">Reviewed</span><strong class="metric-value">{{ stats.reviewed }}</strong></article>
            <article class="metric-tile"><span class="metric-label">Confirmed</span><strong class="metric-value">{{ stats.confirmed }}</strong></article>
        </section>

        <div class="toolbar-shell">
            <div class="search-bar">
                <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <circle cx="11" cy="11" r="8" /><path d="M21 21l-4.35-4.35" />
                </svg>
                <input v-model="filterStatus" type="text" placeholder="Filter by status (Pending, Reviewed, Confirmed, FalsePositive)..." />
            </div>
            <select v-model="aiSource" class="form-control" style="width: auto;" @change="loadAll">
                <option value="">All sources</option>
                <option value="ALPR">ALPR</option>
                <option value="FaceAI">Face AI</option>
                <option value="Unknown">Unknown</option>
            </select>
        </div>

        <section class="ops-panel">
            <div v-if="loading" class="empty-card">Loading adjudications...</div>
            <div v-else-if="filteredItems.length === 0" class="empty-card">No items to review.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr><th>ID</th><th>Source</th><th>Confidence</th><th>Status</th><th>Outcome</th><th>Created</th><th>Actions</th></tr>
                    </thead>
                    <tbody>
                        <tr v-for="item in filteredItems" :key="item.aiAdjudicationItemId">
                            <td>{{ item.aiAdjudicationItemId }}</td>
                            <td>{{ item.aiSource }}/{{ item.modelVersion }}</td>
                            <td>
                                <span v-if="item.confidence != null" class="soft-chip" :class="confClass(item.confidence)">
                                    {{ (item.confidence * 100).toFixed(0) }}%
                                </span>
                                <span v-else>—</span>
                            </td>
                            <td><span class="soft-chip" :class="item.status === 'Pending' ? 'warn' : 'info'">{{ item.status }}</span></td>
                            <td>{{ item.outcome || '—' }}</td>
                            <td>{{ formatDate(item.createdAtUtc) }}</td>
                            <td>
                                <button v-if="item.status === 'Pending'" class="btn btn-sm btn-primary" @click="openReview(item)">Review</button>
                                <button v-else class="btn btn-sm btn-ghost" @click="openReview(item)">View</button>
                            </td>
                        </tr>
                    </tbody>
                </table>
                <div v-if="totalPages > 1" class="pagination-bar">
                    <button :disabled="page <= 1" @click="page--; loadAll()">Prev</button>
                    <span>{{ page }} / {{ totalPages }}</span>
                    <button :disabled="page >= totalPages" @click="page++; loadAll()">Next</button>
                </div>
            </div>
        </section>

        <Teleport to="body">
            <div v-if="reviewItem" class="modal-overlay" @click.self="reviewItem = null">
                <div class="modal-panel" style="max-width: 500px;">
                    <div class="modal-header">
                        <h2>Review AI Adjudication #{{ reviewItem.aiAdjudicationItemId }}</h2>
                        <button class="btn-close" @click="reviewItem = null">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="detail-grid">
                            <div class="detail-row"><span class="detail-label">Source</span><span>{{ reviewItem.aiSource }} v{{ reviewItem.modelVersion }}</span></div>
                            <div class="detail-row"><span class="detail-label">Confidence</span><span>{{ reviewItem.confidence != null ? (reviewItem.confidence * 100).toFixed(0) + '%' : '—' }}</span></div>
                            <div class="detail-row"><span class="detail-label">Current Status</span><span>{{ reviewItem.status }}</span></div>
                        </div>
                        <div class="form-group" style="margin-top: 1rem;">
                            <label>Review Decision</label>
                            <select v-model="reviewOutcome" class="form-control">
                                <option value="">— Select —</option>
                                <option value="Confirmed">Confirmed — Correct</option>
                                <option value="FalsePositive">False Positive</option>
                                <option value="FalseNegative">False Negative</option>
                                <option value="TrainingCandidate">Training Candidate</option>
                            </select>
                        </div>
                        <div class="form-group">
                            <label>Review Note</label>
                            <textarea v-model="reviewNote" class="form-control" rows="3" placeholder="Optional note"></textarea>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="reviewItem = null">Cancel</button>
                        <button class="btn btn-primary" :disabled="saving" @click="submitReview">{{ saving ? 'Saving...' : 'Submit Review' }}</button>
                    </div>
                </div>
            </div>
        </Teleport>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'

const loading = ref(false)
const saving = ref(false)
const items = ref([])
const page = ref(1)
const totalPages = ref(1)
const filterStatus = ref('')
const aiSource = ref('')
const reviewItem = ref(null)
const reviewOutcome = ref('')
const reviewNote = ref('')

const stats = computed(() => {
    const all = items.value
    return {
        pending: all.filter(i => i.status === 'Pending').length,
        reviewed: all.filter(i => i.status === 'Reviewed').length,
        confirmed: all.filter(i => i.outcome === 'Confirmed').length,
    }
})

const filteredItems = computed(() => {
    if (!filterStatus.value) return items.value
    return items.value.filter(i => i.status.toLowerCase().includes(filterStatus.value.toLowerCase()))
})

function confClass(c) {
    if (c == null) return ''
    return c >= 0.8 ? 'success' : c >= 0.5 ? 'warn' : 'danger'
}

function formatDate(utc) {
    if (!utc) return '—'
    return new Date(utc).toLocaleString('vi-VN')
}

async function loadAll() {
    loading.value = true
    try {
        const params = { page: page.value, pageSize: 25 }
        if (aiSource.value) params.aiSource = aiSource.value
        const res = await enterpriseApi.getAdjudications(params)
        const data = res.data || {}
        items.value = data.items || []
        totalPages.value = Math.ceil((data.total || 0) / 25)
    } catch (e) {
        console.error('Failed to load adjudications', e)
    } finally {
        loading.value = false
    }
}

function openReview(item) {
    reviewItem.value = item
    reviewOutcome.value = item.outcome || ''
    reviewNote.value = item.reviewNote || ''
}

async function submitReview() {
    if (!reviewItem.value) return
    saving.value = true
    try {
        const status = reviewOutcome.value ? 'Reviewed' : 'Closed'
        await enterpriseApi.reviewAdjudication(reviewItem.value.aiAdjudicationItemId, {
            status,
            outcome: reviewOutcome.value || null,
            reviewNote: reviewNote.value || null,
        })
        reviewItem.value.status = status
        reviewItem.value.outcome = reviewOutcome.value
        reviewItem.value.reviewNote = reviewNote.value
        reviewItem.value = null
        await loadAll()
    } catch (e) {
        alert('Review failed: ' + (e.response?.data?.message || e.message))
    } finally {
        saving.value = false
    }
}

onMounted(loadAll)
</script>
