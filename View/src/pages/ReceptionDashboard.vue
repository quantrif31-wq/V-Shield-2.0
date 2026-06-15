<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Reception</span>
                <h1 class="page-title">Reception Dashboard</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="showWalkInModal = true">Walk-in Check-in</button>
                <button class="btn btn-secondary" :disabled="loading" @click="loadAll">Refresh</button>
            </div>
        </div>

        <section class="metric-grid four">
            <article class="metric-tile">
                <span class="metric-label">Today's Visits</span>
                <strong class="metric-value">{{ todayCount }}</strong>
            </article>
            <article class="metric-tile">
                <span class="metric-label">Checked In</span>
                <strong class="metric-value">{{ checkedInCount }}</strong>
            </article>
            <article class="metric-tile">
                <span class="metric-label">Overstays</span>
                <strong class="metric-value">{{ overstayCount }}</strong>
            </article>
            <article class="metric-tile">
                <span class="metric-label">Pending Watchlist</span>
                <strong class="metric-value">{{ pendingWatchlist }}</strong>
            </article>
        </section>

        <div class="tab-bar">
            <button v-for="tab in tabs" :key="tab.id" :class="{ active: activeTab === tab.id }" @click="activeTab = tab.id">
                {{ tab.label }}
            </button>
        </div>

        <section v-if="activeTab === 'today'" class="ops-panel">
            <div class="toolbar-shell">
                <div class="search-bar">
                    <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <circle cx="11" cy="11" r="8" /><path d="M21 21l-4.35-4.35" />
                    </svg>
                    <input v-model="searchQuery" type="text" placeholder="Search visitor name or phone..." />
                </div>
            </div>
            <div v-if="loading" class="empty-card">Loading visits...</div>
            <div v-else-if="filteredVisits.length === 0" class="empty-card">No visits for today.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr>
                            <th>Visitor</th>
                            <th>Host</th>
                            <th>Time</th>
                            <th>Status</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="v in filteredVisits" :key="v.visitId">
                            <td>
                                <strong>{{ v.visitorName }}</strong>
                                <div class="text-muted">{{ v.visitorPhone }}</div>
                            </td>
                            <td>{{ v.hostEmployee?.fullName || '—' }}</td>
                            <td>
                                <div>{{ formatTime(v.expectedInUtc) }} - {{ formatTime(v.expectedOutUtc) }}</div>
                            </td>
                            <td>
                                <span class="soft-chip" :class="statusClass(v.status)">{{ v.status }}</span>
                            </td>
                            <td>
                                <div class="chip-row">
                                    <button v-if="v.status === 'Approved' || v.status === 'Invited'" class="btn btn-sm btn-primary" @click="checkInVisit(v)">Check-in</button>
                                    <button v-if="v.status === 'CheckedIn'" class="btn btn-sm btn-secondary" @click="checkOutVisit(v)">Check-out</button>
                                    <button class="btn btn-sm btn-ghost" @click="viewDetail(v)">Detail</button>
                                </div>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </section>

        <section v-if="activeTab === 'overstays'" class="ops-panel">
            <div v-if="loading" class="empty-card">Loading overstays...</div>
            <div v-else-if="overstays.length === 0" class="empty-card">No overstays.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr><th>Visitor</th><th>Host</th><th>Expected Out</th><th>Status</th></tr>
                    </thead>
                    <tbody>
                        <tr v-for="v in overstays" :key="v.visitId">
                            <td><strong>{{ v.visitorName }}</strong></td>
                            <td>{{ v.hostEmployee?.fullName || '—' }}</td>
                            <td>{{ formatTime(v.expectedOutUtc) }}</td>
                            <td><span class="soft-chip danger">{{ v.status }}</span></td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </section>

        <section v-if="activeTab === 'watchlist'" class="ops-panel">
            <div v-if="loading" class="empty-card">Loading watchlist matches...</div>
            <div v-else-if="watchlistMatches.length === 0" class="empty-card">No pending matches.</div>
            <div v-else class="table-container">
                <table class="data-table">
                    <thead>
                        <tr><th>Entry</th><th>Severity</th><th>Matched</th><th>Status</th><th>Review</th></tr>
                    </thead>
                    <tbody>
                        <tr v-for="m in watchlistMatches" :key="m.watchlistMatchId">
                            <td>{{ m.watchlistEntry?.displayName || '—' }}</td>
                            <td><span class="soft-chip" :class="severityClass(m.watchlistEntry?.severity)">{{ m.watchlistEntry?.severity }}</span></td>
                            <td>{{ formatTime(m.matchedAtUtc) }}</td>
                            <td><span class="soft-chip">{{ m.status }}</span></td>
                            <td>
                                <button class="btn btn-sm btn-primary" @click="openReviewModal(m)">Review</button>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </section>

        <Teleport to="body">
            <div v-if="showWalkInModal" class="modal-overlay" @click.self="showWalkInModal = false">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Walk-in Check-in</h2>
                        <button class="btn-close" @click="showWalkInModal = false">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="form-group">
                            <label>Visitor Name *</label>
                            <input v-model="walkIn.name" type="text" class="form-control" placeholder="Full name" />
                        </div>
                        <div class="form-row two">
                            <div class="form-group">
                                <label>Phone</label>
                                <input v-model="walkIn.phone" type="text" class="form-control" />
                            </div>
                            <div class="form-group">
                                <label>Email</label>
                                <input v-model="walkIn.email" type="email" class="form-control" />
                            </div>
                        </div>
                        <div class="form-group">
                            <label>Host Employee</label>
                            <select v-model="walkIn.hostEmployeeId" class="form-control">
                                <option :value="null">— Select host —</option>
                                <option v-for="e in employees" :key="e.employeeId" :value="e.employeeId">{{ e.fullName }}</option>
                            </select>
                        </div>
                        <div class="form-row two">
                            <div class="form-group">
                                <label>Expected In</label>
                                <input v-model="walkIn.expectedIn" type="datetime-local" class="form-control" />
                            </div>
                            <div class="form-group">
                                <label>Expected Out</label>
                                <input v-model="walkIn.expectedOut" type="datetime-local" class="form-control" />
                            </div>
                        </div>
                        <div class="form-row two">
                            <label class="checkbox-label">
                                <input v-model="walkIn.ndaRequired" type="checkbox" /> NDA required
                            </label>
                            <label class="checkbox-label">
                                <input v-model="walkIn.escortRequired" type="checkbox" /> Escort required
                            </label>
                        </div>
                        <div class="form-group">
                            <label>ID Document</label>
                            <div class="form-row two">
                                <input v-model="walkIn.idDocType" type="text" class="form-control" placeholder="Type (e.g. CCCD, Passport)" />
                                <input v-model="walkIn.idDocRef" type="text" class="form-control" placeholder="Reference number" />
                            </div>
                        </div>
                        <div v-if="walkInError" class="alert alert-danger">{{ walkInError }}</div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="showWalkInModal = false">Cancel</button>
                        <button class="btn btn-primary" :disabled="saving" @click="submitWalkIn">{{ saving ? 'Processing...' : 'Check-in' }}</button>
                    </div>
                </div>
            </div>

            <div v-if="reviewMatch" class="modal-overlay" @click.self="reviewMatch = null">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Review Watchlist Match</h2>
                        <button class="btn-close" @click="reviewMatch = null">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="detail-grid">
                            <div class="detail-row"><span class="detail-label">Entry</span><span>{{ reviewMatch.watchlistEntry?.displayName }}</span></div>
                            <div class="detail-row"><span class="detail-label">Severity</span><span>{{ reviewMatch.watchlistEntry?.severity }}</span></div>
                            <div class="detail-row"><span class="detail-label">Reason</span><span>{{ reviewMatch.watchlistEntry?.reason }}</span></div>
                            <div class="detail-row"><span class="detail-label">Visitor</span><span>{{ reviewMatch.visit?.visitorName }}</span></div>
                        </div>
                        <div class="form-group">
                            <label>Review Decision</label>
                            <select v-model="reviewStatus" class="form-control">
                                <option value="Confirmed">Confirmed — Take action</option>
                                <option value="FalsePositive">False Positive</option>
                                <option value="Escalated">Escalate to Security</option>
                                <option value="Closed">Closed — No action</option>
                            </select>
                        </div>
                        <div class="form-group">
                            <label>Note</label>
                            <textarea v-model="reviewNote" class="form-control" rows="3" placeholder="Optional review note"></textarea>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="reviewMatch = null">Cancel</button>
                        <button class="btn btn-primary" :disabled="saving" @click="submitReview">{{ saving ? 'Saving...' : 'Submit Review' }}</button>
                    </div>
                </div>
            </div>

            <div v-if="detailVisit" class="modal-overlay" @click.self="detailVisit = null">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Visit Detail</h2>
                        <button class="btn-close" @click="detailVisit = null">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="detail-grid">
                            <div class="detail-row"><span class="detail-label">Visitor</span><span>{{ detailVisit.visitorName }}</span></div>
                            <div class="detail-row"><span class="detail-label">Phone</span><span>{{ detailVisit.visitorPhone || '—' }}</span></div>
                            <div class="detail-row"><span class="detail-label">Email</span><span>{{ detailVisit.visitorEmail || '—' }}</span></div>
                            <div class="detail-row"><span class="detail-label">Host</span><span>{{ detailVisit.hostEmployee?.fullName || '—' }}</span></div>
                            <div class="detail-row"><span class="detail-label">Status</span><span>{{ detailVisit.status }}</span></div>
                            <div class="detail-row"><span class="detail-label">Time</span><span>{{ formatTime(detailVisit.expectedInUtc) }} → {{ formatTime(detailVisit.expectedOutUtc) }}</span></div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="detailVisit = null">Close</button>
                    </div>
                </div>
            </div>
        </Teleport>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'
import * as employeeApi from '../services/employeeApi'

const loading = ref(false)
const saving = ref(false)
const activeTab = ref('today')
const searchQuery = ref('')
const visits = ref([])
const overstays = ref([])
const watchlistMatches = ref([])
const employees = ref([])
const todayCount = ref(0)
const checkedInCount = ref(0)
const overstayCount = ref(0)
const pendingWatchlist = ref(0)

const showWalkInModal = ref(false)
const walkInError = ref('')
const detailVisit = ref(null)
const reviewMatch = ref(null)
const reviewStatus = ref('Confirmed')
const reviewNote = ref('')

const tabs = [
    { id: 'today', label: "Today's Visits" },
    { id: 'overstays', label: 'Overstays' },
    { id: 'watchlist', label: 'Watchlist Matches' },
]

const walkIn = ref({
    name: '', phone: '', email: '', hostEmployeeId: null,
    expectedIn: '', expectedOut: '',
    ndaRequired: false, escortRequired: false,
    idDocType: '', idDocRef: '',
})

const filteredVisits = computed(() => {
    if (!searchQuery.value) return visits.value
    const q = searchQuery.value.toLowerCase()
    return visits.value.filter(v =>
        v.visitorName.toLowerCase().includes(q) ||
        (v.visitorPhone && v.visitorPhone.includes(q))
    )
})

function statusClass(s) {
    return s === 'CheckedIn' ? 'success' : s === 'Overstay' ? 'danger' : s === 'Approved' ? 'info' : ''
}

function severityClass(s) {
    return s === 'Critical' || s === 'High' ? 'danger' : s === 'Medium' ? 'warn' : 'info'
}

function formatTime(utc) {
    if (!utc) return '—'
    return new Date(utc).toLocaleString('vi-VN')
}

async function loadAll() {
    loading.value = true
    try {
        const today = new Date()
        today.setHours(0, 0, 0, 0)
        const tomorrow = new Date(today)
        tomorrow.setDate(tomorrow.getDate() + 1)

        const [visitsRes, overstaysRes, matchesRes, overviewRes, empRes] = await Promise.all([
            enterpriseApi.getVisits({ dateFrom: today.toISOString(), dateTo: tomorrow.toISOString(), pageSize: 100 }),
            enterpriseApi.getOverstays(),
            enterpriseApi.getWatchlistMatches({ status: 'Pending', pageSize: 50 }),
            enterpriseApi.overview(),
            employeeApi.getAll({ pageSize: 200 }),
        ])
        visits.value = visitsRes.data?.items || []
        overstays.value = overstaysRes.data || []
        watchlistMatches.value = matchesRes.data?.items || []
        employees.value = empRes.data || []

        const ov = visitsRes.data || {}
        todayCount.value = ov.total || visits.value.length
        checkedInCount.value = visits.value.filter(v => v.status === 'CheckedIn').length
        overstayCount.value = overstays.value.length
        pendingWatchlist.value = matchesRes.data?.total || 0
    } catch (e) {
        console.error('Failed to load reception data', e)
    } finally {
        loading.value = false
    }
}

async function checkInVisit(v) {
    saving.value = true
    try {
        await enterpriseApi.checkInVisit(v.visitId, {
            idDocumentType: '',
            idDocumentReference: '',
            verificationStatus: 'Verified',
        })
        v.status = 'CheckedIn'
    } catch (e) {
        alert('Check-in failed: ' + (e.response?.data?.message || e.message))
    } finally {
        saving.value = false
    }
}

async function checkOutVisit(v) {
    saving.value = true
    try {
        await enterpriseApi.checkOutVisit(v.visitId)
        v.status = 'CheckedOut'
    } catch (e) {
        alert('Check-out failed: ' + (e.response?.data?.message || e.message))
    } finally {
        saving.value = false
    }
}

async function submitWalkIn() {
    if (!walkIn.value.name) { walkInError.value = 'Visitor name is required.'; return }
    walkInError.value = ''
    saving.value = true
    try {
        const expectedIn = walkIn.value.expectedIn ? new Date(walkIn.value.expectedIn).toISOString() : new Date().toISOString()
        const expectedOut = walkIn.value.expectedOut ? new Date(walkIn.value.expectedOut).toISOString() : new Date(Date.now() + 4 * 3600000).toISOString()
        const res = await enterpriseApi.createVisit({
            visitorName: walkIn.value.name,
            visitorPhone: walkIn.value.phone || null,
            visitorEmail: walkIn.value.email || null,
            hostEmployeeId: walkIn.value.hostEmployeeId,
            expectedInUtc: expectedIn,
            expectedOutUtc: expectedOut,
            ndaRequired: walkIn.value.ndaRequired,
            escortRequired: walkIn.value.escortRequired,
            safetyBriefingRequired: false,
        })
        const visitId = res.data?.visitId
        if (visitId) {
            await enterpriseApi.checkInVisit(visitId, {
                idDocumentType: walkIn.value.idDocType || null,
                idDocumentReference: walkIn.value.idDocRef || null,
                verificationStatus: 'Verified',
            })
        }
        showWalkInModal.value = false
        walkIn.value = { name: '', phone: '', email: '', hostEmployeeId: null, expectedIn: '', expectedOut: '', ndaRequired: false, escortRequired: false, idDocType: '', idDocRef: '' }
        await loadAll()
    } catch (e) {
        walkInError.value = e.response?.data?.message || e.message
    } finally {
        saving.value = false
    }
}

function viewDetail(v) {
    detailVisit.value = v
}

function openReviewModal(m) {
    reviewMatch.value = m
    reviewStatus.value = m.status === 'Pending' ? 'Confirmed' : m.status
    reviewNote.value = ''
}

async function submitReview() {
    if (!reviewMatch.value) return
    saving.value = true
    try {
        await enterpriseApi.reviewWatchlistMatch(reviewMatch.value.watchlistMatchId, {
            status: reviewStatus.value,
            reviewNote: reviewNote.value || null,
        })
        reviewMatch.value.status = reviewStatus.value
        reviewMatch.value = null
        await loadAll()
    } catch (e) {
        alert('Review failed: ' + (e.response?.data?.message || e.message))
    } finally {
        saving.value = false
    }
}

onMounted(loadAll)
</script>
