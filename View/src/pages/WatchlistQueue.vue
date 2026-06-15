<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Security Screening</span>
                <h1 class="page-title">Watchlist Queue</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="showAddEntry = true">Add Entry</button>
                <button class="btn btn-secondary" :disabled="loading" @click="loadAll">Refresh</button>
            </div>
        </div>

        <div class="tab-bar">
            <button v-for="t in tabs" :key="t.id" :class="{ active: activeTab === t.id }" @click="activeTab = t.id; loadAll()">
                {{ t.label }}
            </button>
        </div>

        <section v-if="activeTab === 'matches'" class="ops-panel">
            <div class="toolbar-shell">
                <div class="search-bar">
                    <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <circle cx="11" cy="11" r="8" /><path d="M21 21l-4.35-4.35" />
                    </svg>
                    <input v-model="filterStatus" type="text" placeholder="Filter by status (Pending, Confirmed, FalsePositive, Escalated, Closed)..." />
                </div>
                <select v-model="severityFilter" class="form-control" style="width: auto;" @change="loadMatches">
                    <option value="">All Severity</option>
                    <option value="Critical">Critical</option>
                    <option value="High">High</option>
                    <option value="Medium">Medium</option>
                    <option value="Low">Low</option>
                </select>
            </div>
            <div v-if="loading" class="empty-card">Loading matches...</div>
            <div v-else-if="matches.length === 0" class="empty-card">No watchlist matches.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr><th>Severity</th><th>Entry</th><th>Visitor</th><th>Matched At</th><th>Status</th><th>Actions</th></tr>
                    </thead>
                    <tbody>
                        <tr v-for="m in filteredMatches" :key="m.watchlistMatchId" :class="{ 'match-critical': m.watchlistEntry?.severity === 'Critical', 'match-high': m.watchlistEntry?.severity === 'High' }">
                            <td>
                                <span class="severity-icon" :class="severityClass(m.watchlistEntry?.severity)">
                                    {{ severityIcon(m.watchlistEntry?.severity) }}
                                </span>
                                <span class="soft-chip" :class="severityClass(m.watchlistEntry?.severity)">{{ m.watchlistEntry?.severity }}</span>
                            </td>
                            <td>{{ m.watchlistEntry?.displayName || '—' }}</td>
                            <td>
                                <span @click="showVisitorDetail(m)" class="link-text">{{ m.visit?.visitorName || '—' }}</span>
                            </td>
                            <td>{{ formatDate(m.matchedAtUtc) }}</td>
                            <td>
                                <span class="soft-chip" :class="matchStatusClass(m.status)">{{ m.status }}</span>
                                <span v-if="m.status === 'Pending' && m.matchedAtUtc" class="pending-time">
                                    {{ hoursSince(m.matchedAtUtc) }}h
                                </span>
                            </td>
                            <td>
                                <button class="btn btn-sm btn-primary" @click="openReview(m)">Review</button>
                            </td>
                        </tr>
                    </tbody>
                </table>
                <div v-if="totalPages > 1" class="pagination-bar">
                    <button :disabled="page <= 1" @click="page--; loadMatches()">Prev</button>
                    <span>{{ page }} / {{ totalPages }}</span>
                    <button :disabled="page >= totalPages" @click="page++; loadMatches()">Next</button>
                </div>
            </div>
        </section>

        <section v-if="activeTab === 'entries'" class="ops-panel">
            <div class="toolbar-shell">
                <div class="search-bar">
                    <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <circle cx="11" cy="11" r="8" /><path d="M21 21l-4.35-4.35" />
                    </svg>
                    <input v-model="entrySearch" type="text" placeholder="Search entries..." />
                </div>
            </div>
            <div v-if="loading" class="empty-card">Loading entries...</div>
            <div v-else-if="filteredEntries.length === 0" class="empty-card">No watchlist entries.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr><th>Name</th><th>Type</th><th>Identifier</th><th>Severity</th><th>Active</th><th>Reason</th></tr>
                    </thead>
                    <tbody>
                        <tr v-for="e in filteredEntries" :key="e.watchlistEntryId">
                            <td>{{ e.displayName }}</td>
                            <td>{{ e.entityType }}</td>
                            <td>{{ e.identifier || '—' }}</td>
                            <td><span class="soft-chip" :class="severityClass(e.severity)">{{ e.severity }}</span></td>
                            <td>
                                <span class="soft-chip" :class="e.isActive ? 'success' : 'muted'">{{ e.isActive ? 'Active' : 'Inactive' }}</span>
                            </td>
                            <td>{{ e.reason }}</td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </section>

        <Teleport to="body">
            <div v-if="reviewMatch" class="modal-overlay" @click.self="reviewMatch = null">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Review Watchlist Match</h2>
                        <button class="btn-close" @click="reviewMatch = null">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="detail-grid">
                            <div class="detail-row"><span class="detail-label">Entry</span><span>{{ reviewMatch.watchlistEntry?.displayName }}</span></div>
                            <div class="detail-row"><span class="detail-label">Severity</span><span class="soft-chip" :class="severityClass(reviewMatch.watchlistEntry?.severity)">{{ reviewMatch.watchlistEntry?.severity }}</span></div>
                            <div class="detail-row"><span class="detail-label">Reason</span><span>{{ reviewMatch.watchlistEntry?.reason }}</span></div>
                            <div class="detail-row"><span class="detail-label">Visitor</span><span>{{ reviewMatch.visit?.visitorName }}</span></div>
                            <div class="detail-row"><span class="detail-label">Matched</span><span>{{ formatDate(reviewMatch.matchedAtUtc) }}</span></div>
                        </div>
                        <div class="form-group" style="margin-top:12px;">
                            <label>Decision</label>
                            <select v-model="reviewDecision" class="form-control">
                                <option value="Confirmed">Confirmed</option>
                                <option value="FalsePositive">False Positive</option>
                                <option value="Escalated">Escalated</option>
                                <option value="Closed">Closed</option>
                            </select>
                        </div>
                        <div class="form-group">
                            <label>Note</label>
                            <textarea v-model="reviewNote" class="form-control" rows="3"></textarea>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="reviewMatch = null">Cancel</button>
                        <button class="btn btn-primary" :disabled="saving" @click="submitReview">{{ saving ? 'Saving...' : 'Submit Review' }}</button>
                    </div>
                </div>
            </div>

            <div v-if="showAddEntry" class="modal-overlay" @click.self="showAddEntry = false">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Add Watchlist Entry</h2>
                        <button class="btn-close" @click="showAddEntry = false">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="form-group">
                            <label>Name *</label>
                            <input v-model="newEntry.name" type="text" class="form-control" />
                        </div>
                        <div class="form-row two">
                            <div class="form-group">
                                <label>Type</label>
                                <select v-model="newEntry.entityType" class="form-control">
                                    <option value="Person">Person</option>
                                    <option value="Vehicle">Vehicle</option>
                                </select>
                            </div>
                            <div class="form-group">
                                <label>Identifier</label>
                                <input v-model="newEntry.identifier" type="text" class="form-control" placeholder="Phone / Plate / ID" />
                            </div>
                        </div>
                        <div class="form-row two">
                            <div class="form-group">
                                <label>Severity</label>
                                <select v-model="newEntry.severity" class="form-control">
                                    <option value="Low">Low</option>
                                    <option value="Medium">Medium</option>
                                    <option value="High">High</option>
                                    <option value="Critical">Critical</option>
                                </select>
                            </div>
                            <div class="form-group">
                                <label>Reason</label>
                                <input v-model="newEntry.reason" type="text" class="form-control" />
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="showAddEntry = false">Cancel</button>
                        <button class="btn btn-primary" :disabled="saving" @click="submitEntry">{{ saving ? 'Adding...' : 'Add' }}</button>
                    </div>
                </div>
            </div>

            <!-- Visitor Detail Modal -->
            <div v-if="visitorDetail" class="modal-overlay" @click.self="visitorDetail = null">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Visitor Detail</h2>
                        <button class="btn-close" @click="visitorDetail = null">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="detail-grid">
                            <div class="detail-row"><span class="detail-label">Name</span><span>{{ visitorDetail.visitorName }}</span></div>
                            <div class="detail-row"><span class="detail-label">Phone</span><span>{{ visitorDetail.visitorPhone || '—' }}</span></div>
                            <div class="detail-row"><span class="detail-label">Host</span><span>{{ visitorDetail.hostEmployee?.fullName || '—' }}</span></div>
                            <div class="detail-row"><span class="detail-label">Status</span><span class="soft-chip" :class="statusClass(visitorDetail.status)">{{ visitorDetail.status }}</span></div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="visitorDetail = null">Close</button>
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
const activeTab = ref('matches')
const matches = ref([])
const entries = ref([])
const page = ref(1)
const totalPages = ref(1)
const filterStatus = ref('')
const severityFilter = ref('')
const entrySearch = ref('')
const reviewMatch = ref(null)
const reviewDecision = ref('Confirmed')
const reviewNote = ref('')
const showAddEntry = ref(false)
const visitorDetail = ref(null)

const newEntry = ref({ name: '', entityType: 'Person', identifier: '', severity: 'Medium', reason: '' })

const tabs = [
    { id: 'matches', label: 'Matches' },
    { id: 'entries', label: 'Entries' },
]

const filteredMatches = computed(() => {
    let result = matches.value
    if (filterStatus.value) {
        result = result.filter(m => m.status.toLowerCase().includes(filterStatus.value.toLowerCase()))
    }
    if (severityFilter.value) {
        result = result.filter(m => m.watchlistEntry?.severity === severityFilter.value)
    }
    return result
})

const filteredEntries = computed(() => {
    if (!entrySearch.value) return entries.value
    const q = entrySearch.value.toLowerCase()
    return entries.value.filter(e =>
        e.displayName.toLowerCase().includes(q) ||
        (e.identifier && e.identifier.toLowerCase().includes(q)) ||
        (e.reason && e.reason.toLowerCase().includes(q))
    )
})

function severityClass(s) {
    if (!s) return ''
    return s === 'Critical' || s === 'High' ? 'danger' : s === 'Medium' ? 'warn' : 'info'
}

function severityIcon(s) {
    if (!s) return '•'
    if (s === 'Critical') return '🔴'
    if (s === 'High') return '🟠'
    if (s === 'Medium') return '🟡'
    return '🟢'
}

function matchStatusClass(s) {
    if (!s) return ''
    return s === 'Pending' ? 'warn' : s === 'Confirmed' ? 'danger' : s === 'FalsePositive' ? 'info' : 'success'
}

function statusClass(s) {
    return s === 'CheckedIn' ? 'success' : s === 'Overstay' ? 'danger' : s === 'Approved' ? 'info' : ''
}

function formatDate(utc) {
    if (!utc) return '—'
    return new Date(utc).toLocaleString('vi-VN')
}

function hoursSince(utc) {
    if (!utc) return 0
    return Math.round((Date.now() - new Date(utc).getTime()) / 3600000)
}

async function loadAll() {
    loading.value = true
    try {
        if (activeTab.value === 'matches') {
            await loadMatches()
        } else {
            const res = await enterpriseApi.getWatchlistEntries({ active: true })
            entries.value = res.data || []
        }
    } catch (e) {
        console.error('Failed to load', e)
    } finally {
        loading.value = false
    }
}

async function loadMatches() {
    const params = { page: page.value, pageSize: 25 }
    if (severityFilter.value) params.severity = severityFilter.value
    const res = await enterpriseApi.getWatchlistMatches(params)
    const data = res.data || {}
    matches.value = data.items || []
    totalPages.value = Math.ceil((data.total || 0) / 25)
}

function openReview(m) {
    reviewMatch.value = m
    reviewDecision.value = m.status === 'Pending' ? 'Confirmed' : m.status
    reviewNote.value = ''
}

async function submitReview() {
    if (!reviewMatch.value) return
    saving.value = true
    try {
        await enterpriseApi.reviewWatchlistMatch(reviewMatch.value.watchlistMatchId, {
            status: reviewDecision.value,
            reviewNote: reviewNote.value || null,
        })
        reviewMatch.value.status = reviewDecision.value
        reviewMatch.value = null
        await loadMatches()
    } catch (e) {
        alert('Review failed: ' + (e.response?.data?.message || e.message))
    } finally {
        saving.value = false
    }
}

async function submitEntry() {
    if (!newEntry.value.name) return
    saving.value = true
    try {
        await enterpriseApi.createWatchlistEntry({
            entityType: newEntry.value.entityType,
            displayName: newEntry.value.name,
            identifier: newEntry.value.identifier || null,
            severity: newEntry.value.severity,
            reason: newEntry.value.reason || 'Security watchlist',
        })
        showAddEntry.value = false
        newEntry.value = { name: '', entityType: 'Person', identifier: '', severity: 'Medium', reason: '' }
        if (activeTab.value === 'entries') await loadAll()
    } catch (e) {
        alert('Failed: ' + (e.response?.data?.message || e.message))
    } finally {
        saving.value = false
    }
}

function showVisitorDetail(m) {
    visitorDetail.value = m.visit
}

onMounted(loadAll)
</script>

<style scoped>
.match-critical { background: #fef2f2 !important; }
.match-high { background: #fff7ed !important; }
.severity-icon { margin-right: 4px; font-size: 12px; }
.link-text { color: #0369a1; cursor: pointer; text-decoration: underline; }
.link-text:hover { color: #075985; }
.pending-time { font-size: 11px; color: #64748b; margin-left: 4px; }
</style>
