<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Reception</span>
                <h1 class="page-title">Reception Dashboard</h1>
            </div>
            <div class="header-actions">
                <button class="btn btn-primary" @click="showWalkInModal = true">Walk-in Check-in</button>
                <button class="btn btn-secondary" @click="showFormTemplatesModal = true">Form Templates</button>
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
                                    <button class="btn btn-sm btn-ghost" @click="openVisitDetail(v)">Detail</button>
                                    <button v-if="v.status === 'Approved' || v.status === 'CheckedIn'" class="btn btn-sm btn-secondary" @click="openParkingPermit(v)">Parking</button>
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
            <!-- Walk-in Check-in Modal -->
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

            <!-- Watchlist Review Modal -->
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

            <!-- Unified Visit Detail Drawer -->
            <div v-if="detailVisit" class="modal-overlay drawer-overlay" @click.self="detailVisit = null">
                <div class="modal-panel drawer-panel">
                    <div class="modal-header">
                        <h2>Visit Detail</h2>
                        <button class="btn-close" @click="detailVisit = null">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="drawer-tabs">
                            <button v-for="dt in detailTabs" :key="dt.key" :class="{ active: activeDetailTab === dt.key }" @click="activeDetailTab = dt.key">
                                {{ dt.label }}
                            </button>
                        </div>

                        <!-- Tab: Overview -->
                        <div v-if="activeDetailTab === 'overview'" class="drawer-tab-content">
                            <div v-if="detailLoading" class="empty-card">Loading detail...</div>
                            <div v-else class="detail-grid">
                                <div class="detail-row"><span class="detail-label">Visitor</span><span>{{ detailData?.visitorName || detailVisit.visitorName }}</span></div>
                                <div class="detail-row"><span class="detail-label">Phone</span><span>{{ detailData?.visitorPhone || detailVisit.visitorPhone || '—' }}</span></div>
                                <div class="detail-row"><span class="detail-label">Email</span><span>{{ detailData?.visitorEmail || detailVisit.visitorEmail || '—' }}</span></div>
                                <div class="detail-row"><span class="detail-label">Host</span><span>{{ detailData?.hostEmployee?.fullName || detailVisit.hostEmployee?.fullName || '—' }}</span></div>
                                <div class="detail-row"><span class="detail-label">Department</span><span>{{ detailData?.hostEmployee?.department || '—' }}</span></div>
                                <div class="detail-row"><span class="detail-label">Status</span><span class="soft-chip" :class="statusClass(detailData?.status || detailVisit.status)">{{ detailData?.status || detailVisit.status }}</span></div>
                                <div class="detail-row"><span class="detail-label">Time</span><span>{{ formatTime(detailData?.expectedInUtc || detailVisit.expectedInUtc) }} → {{ formatTime(detailData?.expectedOutUtc || detailVisit.expectedOutUtc) }}</span></div>
                                <div class="detail-row"><span class="detail-label">Site</span><span>{{ detailData?.site?.name || detailVisit.siteName || '—' }}</span></div>
                                <div class="detail-row"><span class="detail-label">NDA</span><span>{{ (detailData?.ndaRequired ?? detailVisit.ndaRequired) ? 'Required' : 'Not required' }}</span></div>
                                <div class="detail-row"><span class="detail-label">Escort</span><span>{{ (detailData?.escortRequired ?? detailVisit.escortRequired) ? 'Required' : 'Not required' }}</span></div>
                                <div class="detail-row"><span class="detail-label">Safety Briefing</span><span>{{ (detailData?.safetyBriefingRequired ?? detailVisit.safetyBriefingRequired) ? 'Required' : 'Not required' }}</span></div>
                            </div>
                        </div>

                        <!-- Tab: Forms (NDA, Safety, Policy) -->
                        <div v-if="activeDetailTab === 'forms'" class="drawer-tab-content">
                            <div v-if="formsLoading" class="empty-card">Loading form templates...</div>
                            <div v-else>
                                <div class="detail-section-title">Accept Forms</div>
                                <div v-if="formTemplates.length === 0" class="text-muted" style="margin-bottom:12px;">No form templates available.</div>
                                <div v-for="ft in formTemplates" :key="ft.formTemplateId || ft.id" class="form-template-card" @click="acceptFormForVisit(ft)">
                                    <div class="ft-name">{{ ft.templateName || ft.name }}</div>
                                    <div class="ft-desc">{{ ft.description || ft.category || '' }}</div>
                                    <div v-if="ft.isRequired" class="ft-badge">Required</div>
                                </div>
                                <div v-if="formAcceptSuccess" class="alert alert-success" style="margin-top:8px;">{{ formAcceptSuccess }}</div>
                                <div v-else-if="formAcceptError" class="alert alert-danger" style="margin-top:8px;">{{ formAcceptError }}</div>
                            </div>
                        </div>

                        <!-- Tab: Parking Permit -->
                        <div v-if="activeDetailTab === 'parking'" class="drawer-tab-content">
                            <div v-if="parkingLoading" class="empty-card">Loading parking areas...</div>
                            <div v-else>
                                <div class="detail-section-title">Issue Parking Permit</div>
                                <div class="form-group">
                                    <label>Parking Area</label>
                                    <select v-model="parkingPermitForm.parkingAreaId" class="form-control">
                                        <option :value="null">— Select —</option>
                                        <option v-for="pa in parkingAreas" :key="pa.parkingAreaId || pa.id" :value="pa.parkingAreaId || pa.id">{{ pa.name }} ({{ pa.availableSpots ?? '?' }} spots)</option>
                                    </select>
                                </div>
                                <div class="form-row two">
                                    <div class="form-group">
                                        <label>Valid From</label>
                                        <input v-model="parkingPermitForm.validFrom" type="datetime-local" class="form-control" />
                                    </div>
                                    <div class="form-group">
                                        <label>Valid To</label>
                                        <input v-model="parkingPermitForm.validTo" type="datetime-local" class="form-control" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label>Vehicle Plate</label>
                                    <input v-model="parkingPermitForm.plateNumber" type="text" class="form-control" placeholder="e.g. 29A-12345" />
                                </div>
                                <div v-if="parkingSuccess" class="alert alert-success" style="margin-top:8px;">{{ parkingSuccess }}</div>
                                <div v-else-if="parkingError" class="alert alert-danger" style="margin-top:8px;">{{ parkingError }}</div>
                                <div class="chip-row" style="margin-top:12px;">
                                    <button class="btn btn-primary btn-sm" :disabled="parkingSaving || !parkingPermitForm.parkingAreaId" @click="submitParkingPermit">
                                        {{ parkingSaving ? 'Issuing...' : 'Issue Permit' }}
                                    </button>
                                </div>

                                <div v-if="existingPermits.length > 0" style="margin-top:16px;">
                                    <div class="detail-section-title">Existing Permits</div>
                                    <div v-for="p in existingPermits" :key="p.parkingPermitId || p.id" class="permit-card">
                                        <div><strong>{{ p.areaName || p.parkingAreaName }}</strong></div>
                                        <div class="text-muted">{{ p.plateNumber || '—' }} · {{ formatTime(p.validFromUtc) }} → {{ formatTime(p.validToUtc) }}</div>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <!-- Tab: Escort & Watchlist -->
                        <div v-if="activeDetailTab === 'escort'" class="drawer-tab-content">
                            <div class="detail-section-title">Escort Requirements</div>
                            <div class="detail-grid">
                                <div class="detail-row"><span class="detail-label">Escort Required</span><span>{{ (detailData?.escortRequired ?? detailVisit.escortRequired) ? 'Yes' : 'No' }}</span></div>
                                <div class="detail-row"><span class="detail-label">Assigned Escort</span><span>{{ detailData?.escortEmployee?.fullName || detailVisit.escortName || '—' }}</span></div>
                            </div>
                            <div v-if="(detailData?.escortRequired ?? detailVisit.escortRequired)" class="chip-row" style="margin-top:12px;">
                                <button class="btn btn-primary btn-sm" @click="showAssignEscort = true">Assign Escort</button>
                            </div>

                            <div v-if="showAssignEscort" style="margin-top:12px;">
                                <div class="form-group">
                                    <label>Select Escort</label>
                                    <select v-model="escortEmployeeId" class="form-control">
                                        <option :value="null">— Select —</option>
                                        <option v-for="e in employees" :key="e.employeeId" :value="e.employeeId">{{ e.fullName }}</option>
                                    </select>
                                </div>
                                <div class="chip-row">
                                    <button class="btn btn-sm btn-secondary" @click="showAssignEscort = false">Cancel</button>
                                    <button class="btn btn-sm btn-primary" :disabled="!escortEmployeeId" @click="assignEscort">Assign</button>
                                </div>
                            </div>

                            <div style="margin-top:16px;">
                                <div class="detail-section-title">Watchlist Status</div>
                                <div v-if="matchForVisit" class="alert alert-warning">
                                    This visitor matches watchlist entry <strong>{{ matchForVisit.watchlistEntry?.displayName }}</strong> ({{ matchForVisit.watchlistEntry?.severity }}).
                                    <button class="btn btn-sm btn-primary" style="margin-top:8px;" @click="openReviewModal(matchForVisit)">Review Match</button>
                                </div>
                                <div v-else class="text-muted">No watchlist matches for this visitor.</div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Parking Permit Modal (standalone) -->
            <div v-if="parkingVisit" class="modal-overlay" @click.self="parkingVisit = null">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Parking Permit — {{ parkingVisit.visitorName }}</h2>
                        <button class="btn-close" @click="parkingVisit = null">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div v-if="parkingLoading" class="empty-card">Loading...</div>
                        <div v-else>
                            <div class="form-group">
                                <label>Parking Area</label>
                                <select v-model="parkingPermitForm.parkingAreaId" class="form-control">
                                    <option :value="null">— Select —</option>
                                    <option v-for="pa in parkingAreas" :key="pa.parkingAreaId || pa.id" :value="pa.parkingAreaId || pa.id">{{ pa.name }} ({{ pa.availableSpots ?? '?' }} spots)</option>
                                </select>
                            </div>
                            <div class="form-row two">
                                <div class="form-group">
                                    <label>Valid From</label>
                                    <input v-model="parkingPermitForm.validFrom" type="datetime-local" class="form-control" />
                                </div>
                                <div class="form-group">
                                    <label>Valid To</label>
                                    <input v-model="parkingPermitForm.validTo" type="datetime-local" class="form-control" />
                                </div>
                            </div>
                            <div class="form-group">
                                <label>Vehicle Plate</label>
                                <input v-model="parkingPermitForm.plateNumber" type="text" class="form-control" placeholder="e.g. 29A-12345" />
                            </div>
                            <div v-if="parkingSuccess" class="alert alert-success">{{ parkingSuccess }}</div>
                            <div v-else-if="parkingError" class="alert alert-danger">{{ parkingError }}</div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="parkingVisit = null">Close</button>
                        <button class="btn btn-primary" :disabled="parkingSaving || !parkingPermitForm.parkingAreaId" @click="submitStandaloneParkingPermit">
                            {{ parkingSaving ? 'Issuing...' : 'Issue Permit' }}
                        </button>
                    </div>
                </div>
            </div>

            <!-- Form Templates Modal -->
            <div v-if="showFormTemplatesModal" class="modal-overlay" @click.self="showFormTemplatesModal = false">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Form Templates</h2>
                        <button class="btn-close" @click="showFormTemplatesModal = false">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div v-if="formTemplatesLoading" class="empty-card">Loading...</div>
                        <div v-else>
                            <div class="form-group">
                                <label>New Template Name</label>
                                <input v-model="newTemplateName" type="text" class="form-control" placeholder="e.g. Safety Induction 2026" />
                            </div>
                            <div class="form-group">
                                <label>Category</label>
                                <select v-model="newTemplateCategory" class="form-control">
                                    <option value="NDA">NDA</option>
                                    <option value="Safety">Safety</option>
                                    <option value="Policy">Policy</option>
                                    <option value="Other">Other</option>
                                </select>
                            </div>
                            <div class="form-group">
                                <label>Description</label>
                                <textarea v-model="newTemplateDescription" class="form-control" rows="2" placeholder="Template description"></textarea>
                            </div>
                            <div v-if="templateSaveError" class="alert alert-danger">{{ templateSaveError }}</div>
                            <div class="chip-row">
                                <button class="btn btn-primary" :disabled="savingTemplates || !newTemplateName" @click="createFormTemplate">
                                    {{ savingTemplates ? 'Creating...' : 'Create Template' }}
                                </button>
                            </div>

                            <div style="margin-top:16px;">
                                <div class="detail-section-title">Existing Templates</div>
                                <div v-if="formTemplates.length === 0" class="text-muted">No templates yet.</div>
                                <div v-for="ft in formTemplates" :key="ft.formTemplateId || ft.id" class="form-template-card" style="cursor:default;">
                                    <div class="ft-name">{{ ft.templateName || ft.name }}</div>
                                    <div class="ft-desc">{{ ft.description || ft.category || '' }}</div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="showFormTemplatesModal = false">Close</button>
                    </div>
                </div>
            </div>
        </Teleport>
    </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
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

// Unified detail drawer
const activeDetailTab = ref('overview')
const detailLoading = ref(false)
const detailData = ref(null)
const detailTabs = [
    { key: 'overview', label: 'Overview' },
    { key: 'forms', label: 'Forms' },
    { key: 'parking', label: 'Parking' },
    { key: 'escort', label: 'Escort & Watchlist' },
]

// Forms
const formsLoading = ref(false)
const formTemplates = ref([])
const formAcceptSuccess = ref('')
const formAcceptError = ref('')

// Parking
const parkingVisit = ref(null)
const parkingLoading = ref(false)
const parkingAreas = ref([])
const parkingSaving = ref(false)
const parkingSuccess = ref('')
const parkingError = ref('')
const existingPermits = ref([])
const parkingPermitForm = ref({
    parkingAreaId: null,
    validFrom: '',
    validTo: '',
    plateNumber: '',
})

// Escort
const showAssignEscort = ref(false)
const escortEmployeeId = ref(null)

// Form Templates modal
const showFormTemplatesModal = ref(false)
const formTemplatesLoading = ref(false)
const savingTemplates = ref(false)
const newTemplateName = ref('')
const newTemplateCategory = ref('NDA')
const newTemplateDescription = ref('')
const templateSaveError = ref('')

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

const matchForVisit = computed(() => {
    if (!detailVisit.value) return null
    return watchlistMatches.value.find(m =>
        m.visit?.visitorName === detailVisit.value.visitorName ||
        m.visit?.visitId === detailVisit.value.visitId
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

// --- Unified Visit Detail ---
async function openVisitDetail(v) {
    detailVisit.value = v
    activeDetailTab.value = 'overview'
    detailData.value = null
    formAcceptSuccess.value = ''
    formAcceptError.value = ''
    formTemplates.value = []

    // Load detail data
    detailLoading.value = true
    try {
        const res = await enterpriseApi.getVisitDetail(v.visitId)
        detailData.value = res.data || null
    } catch (e) {
        console.error('Failed to load visit detail', e)
    } finally {
        detailLoading.value = false
    }
}

// --- Forms ---
watchDebounced(activeDetailTab, async (tab) => {
    if (tab === 'forms' && detailVisit.value && formTemplates.value.length === 0) {
        await loadFormTemplates()
    }
    if (tab === 'parking' && detailVisit.value && parkingAreas.value.length === 0) {
        await loadParkingData()
    }
}, 100)

function watchDebounced(source, callback, delay) {
    let timer = null
    watch(source, (val) => {
        clearTimeout(timer)
        timer = setTimeout(() => callback(val), delay)
    })
}

async function loadFormTemplates() {
    formsLoading.value = true
    try {
        const res = await enterpriseApi.getFormTemplates({ pageSize: 50 })
        formTemplates.value = res.data?.items || []
    } catch (e) {
        console.error('Failed to load form templates', e)
    } finally {
        formsLoading.value = false
    }
}

async function acceptFormForVisit(ft) {
    if (!detailVisit.value) return
    formAcceptSuccess.value = ''
    formAcceptError.value = ''
    try {
        await enterpriseApi.acceptForm(detailVisit.value.visitId, {
            formTemplateId: ft.formTemplateId || ft.id,
        })
        formAcceptSuccess.value = `Form "${ft.templateName || ft.name}" accepted.`
    } catch (e) {
        formAcceptError.value = e.response?.data?.message || e.message
    }
}

// --- Parking ---
async function loadParkingData() {
    parkingLoading.value = true
    try {
        const [areasRes, permitsRes] = await Promise.all([
            enterpriseApi.getParkingAreas({ pageSize: 50 }),
            enterpriseApi.getParkingPermits({ visitId: detailVisit.value?.visitId }),
        ])
        parkingAreas.value = areasRes.data?.items || []
        existingPermits.value = permitsRes.data?.items || []
    } catch (e) {
        console.error('Failed to load parking data', e)
    } finally {
        parkingLoading.value = false
    }
}

async function submitParkingPermit() {
    if (!detailVisit.value || !parkingPermitForm.value.parkingAreaId) return
    parkingSaving.value = true
    parkingSuccess.value = ''
    parkingError.value = ''
    try {
        const from = parkingPermitForm.value.validFrom ? new Date(parkingPermitForm.value.validFrom).toISOString() : new Date().toISOString()
        const to = parkingPermitForm.value.validTo ? new Date(parkingPermitForm.value.validTo).toISOString() : new Date(Date.now() + 24 * 3600000).toISOString()
        await enterpriseApi.createParkingPermit({
            visitId: detailVisit.value.visitId,
            parkingAreaId: parkingPermitForm.value.parkingAreaId,
            validFromUtc: from,
            validToUtc: to,
            plateNumber: parkingPermitForm.value.plateNumber || null,
        })
        parkingSuccess.value = 'Parking permit issued successfully!'
        parkingPermitForm.value = { parkingAreaId: null, validFrom: '', validTo: '', plateNumber: '' }
        await loadParkingData()
    } catch (e) {
        parkingError.value = e.response?.data?.message || e.message
    } finally {
        parkingSaving.value = false
    }
}

// Standalone Parking Permit Modal
function openParkingPermit(v) {
    parkingVisit.value = v
    parkingPermitForm.value = { parkingAreaId: null, validFrom: '', validTo: '', plateNumber: '' }
    parkingSuccess.value = ''
    parkingError.value = ''
    loadParkingAreasOnly()
}

async function loadParkingAreasOnly() {
    parkingLoading.value = true
    try {
        const res = await enterpriseApi.getParkingAreas({ pageSize: 50 })
        parkingAreas.value = res.data?.items || []
    } catch (e) {
        console.error('Failed to load parking areas', e)
    } finally {
        parkingLoading.value = false
    }
}

async function submitStandaloneParkingPermit() {
    if (!parkingVisit.value || !parkingPermitForm.value.parkingAreaId) return
    parkingSaving.value = true
    parkingSuccess.value = ''
    parkingError.value = ''
    try {
        const from = parkingPermitForm.value.validFrom ? new Date(parkingPermitForm.value.validFrom).toISOString() : new Date().toISOString()
        const to = parkingPermitForm.value.validTo ? new Date(parkingPermitForm.value.validTo).toISOString() : new Date(Date.now() + 24 * 3600000).toISOString()
        await enterpriseApi.createParkingPermit({
            visitId: parkingVisit.value.visitId,
            parkingAreaId: parkingPermitForm.value.parkingAreaId,
            validFromUtc: from,
            validToUtc: to,
            plateNumber: parkingPermitForm.value.plateNumber || null,
        })
        parkingSuccess.value = 'Parking permit issued!'
        parkingPermitForm.value = { parkingAreaId: null, validFrom: '', validTo: '', plateNumber: '' }
    } catch (e) {
        parkingError.value = e.response?.data?.message || e.message
    } finally {
        parkingSaving.value = false
    }
}

// --- Escort ---
async function assignEscort() {
    if (!detailVisit.value || !escortEmployeeId.value) return
    try {
        await enterpriseApi.checkInVisit(detailVisit.value.visitId, {
            idDocumentType: '',
            idDocumentReference: '',
            verificationStatus: 'Verified',
            escortEmployeeId: escortEmployeeId.value,
        })
        showAssignEscort.value = false
        alert(`Escort assigned to visit #${detailVisit.value.visitId}`)
        await openVisitDetail(detailVisit.value)
    } catch (e) {
        alert('Failed to assign escort: ' + (e.response?.data?.message || e.message))
    }
}

// --- Form Templates Modal ---
async function loadFormTemplatesForModal() {
    formTemplatesLoading.value = true
    try {
        const res = await enterpriseApi.getFormTemplates({ pageSize: 50 })
        formTemplates.value = res.data?.items || []
    } catch (e) {
        console.error('Failed to load form templates', e)
    } finally {
        formTemplatesLoading.value = false
    }
}

async function createFormTemplate() {
    if (!newTemplateName.value) return
    savingTemplates.value = true
    templateSaveError.value = ''
    try {
        await enterpriseApi.createFormTemplate({
            templateName: newTemplateName.value,
            category: newTemplateCategory.value,
            description: newTemplateDescription.value || null,
        })
        newTemplateName.value = ''
        newTemplateDescription.value = ''
        await loadFormTemplatesForModal()
    } catch (e) {
        templateSaveError.value = e.response?.data?.message || e.message
    } finally {
        savingTemplates.value = false
    }
}

onMounted(loadAll)
</script>

<style scoped>
.drawer-overlay {
    display: flex;
    justify-content: flex-end;
}
.drawer-panel {
    width: 520px;
    max-width: 95vw;
    height: 100vh;
    margin: 0;
    border-radius: 0;
    overflow-y: auto;
}
.drawer-tabs {
    display: flex;
    gap: 4px;
    margin-bottom: 16px;
    border-bottom: 1px solid #e2e8f0;
    padding-bottom: 8px;
}
.drawer-tabs button {
    padding: 6px 14px;
    border: none;
    background: transparent;
    color: #51657b;
    font-size: 13px;
    border-radius: 8px;
    cursor: pointer;
    transition: all 0.15s;
}
.drawer-tabs button:hover {
    background: #f1f5f9;
}
.drawer-tabs button.active {
    background: #e0f2fe;
    color: #0369a1;
    font-weight: 600;
}
.drawer-tab-content {
    min-height: 200px;
}
.detail-section-title {
    font-size: 13px;
    font-weight: 600;
    color: #1e293b;
    margin-bottom: 8px;
    padding-bottom: 4px;
    border-bottom: 1px solid #e2e8f0;
}
.form-template-card {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 8px 10px;
    border: 1px solid #e2e8f0;
    border-radius: 8px;
    margin-bottom: 6px;
    cursor: pointer;
    transition: background 0.15s;
}
.form-template-card:hover {
    background: #f8fafc;
}
.ft-name {
    font-weight: 500;
    font-size: 13px;
    flex: 1;
}
.ft-desc {
    font-size: 12px;
    color: #64748b;
}
.ft-badge {
    font-size: 10px;
    padding: 2px 6px;
    border-radius: 4px;
    background: #fef3c7;
    color: #92400e;
}
.permit-card {
    padding: 8px 10px;
    border: 1px solid #e2e8f0;
    border-radius: 8px;
    margin-bottom: 6px;
}
</style>
