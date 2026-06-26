<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Exception Management</span>
                <h1 class="page-title">Trung tam ngoai le</h1>
                <p class="page-subtitle">Hang doi case can doi soat, xin can thiep va khoa so thao tac.</p>
            </div>
            <div class="header-actions">
                <span class="soft-chip warn">{{ openCaseCount }} case mo</span>
                <span class="soft-chip danger">{{ pendingInterventionCount }} yeu cau cho xu ly</span>
                <button class="btn btn-secondary btn-sm" :disabled="loading" @click="loadAll">Refresh</button>
            </div>
        </div>

        <section class="summary-strip">
            <article class="summary-card">
                <span class="summary-label">Case dang mo</span>
                <strong class="summary-value">{{ openCaseCount }}</strong>
                <small>{{ categoryCount('pending_approval') }} cho phe duyet</small>
            </article>
            <article class="summary-card">
                <span class="summary-label">Override / bypass</span>
                <strong class="summary-value">{{ categoryCount('manual_override') }}</strong>
                <small>{{ categoryCount('data_mismatch') }} lech du lieu</small>
            </article>
            <article class="summary-card">
                <span class="summary-label">Device / duress</span>
                <strong class="summary-value">{{ categoryCount('device_degraded') + categoryCount('duress') }}</strong>
                <small>{{ categoryCount('emergency_pass') }} khan cap</small>
            </article>
            <article class="summary-card">
                <span class="summary-label">Workflow can thiep</span>
                <strong class="summary-value">{{ interventionRequests.length }}</strong>
                <small>{{ executedInterventionCount }} da thuc thi</small>
            </article>
        </section>

        <div class="tab-bar">
            <button :class="{ active: activeTab === 'cases' }" @click="activeTab = 'cases'">
                Case ngoai le
                <span v-if="exceptionCases.length" class="tab-count">{{ exceptionCases.length }}</span>
            </button>
            <button :class="{ active: activeTab === 'interventions' }" @click="activeTab = 'interventions'">
                Hang doi can thiep
                <span v-if="pendingInterventionCount" class="tab-count danger">{{ pendingInterventionCount }}</span>
            </button>
        </div>

        <div v-if="activeTab === 'cases'" class="workspace-grid">
            <section class="workspace-pane queue-pane">
                <div class="pane-header">
                    <div>
                        <h2 class="panel-title">Danh sach case</h2>
                        <p class="pane-subtitle">Tap trung vao case moi nhat va co nguy co thao tac.</p>
                    </div>
                    <div class="search-shell">
                        <input v-model.trim="searchQuery" type="text" placeholder="Tim theo ten, bien so, log..." class="filter-input" />
                    </div>
                </div>

                <div class="filter-pills">
                    <button
                        v-for="category in caseCategories"
                        :key="category.id"
                        class="pill-btn"
                        :class="{ active: activeCategory === category.id }"
                        @click="activeCategory = category.id"
                    >
                        {{ category.label }}
                        <span v-if="category.count" class="tab-count">{{ category.count }}</span>
                    </button>
                </div>

                <div v-if="loading" class="empty-card">Dang tai danh sach case...</div>
                <div v-else-if="filteredCases.length === 0" class="empty-card">Khong co case phu hop bo loc hien tai.</div>
                <div v-else class="queue-list">
                    <button
                        v-for="item in filteredCases"
                        :key="item.id"
                        class="queue-item"
                        :class="{ selected: selectedCase?.id === item.id, [`severity-${item.severity}`]: true }"
                        @click="selectCase(item)"
                    >
                        <div class="queue-head">
                            <span class="case-badge" :class="`badge-${item.category}`">{{ categoryLabel(item.category) }}</span>
                            <span class="time-label">{{ formatRelativeTime(item.lastEventAt) }}</span>
                        </div>
                        <strong class="queue-title">{{ item.subjectName || item.plateText || `Log #${item.sourceLogId}` }}</strong>
                        <div class="queue-meta">
                            <span v-if="item.plateText" class="plate-badge">{{ item.plateText }}</span>
                            <span>{{ item.gateName || 'Chua gan cong' }}</span>
                        </div>
                        <div class="queue-footer">
                            <span class="severity-badge" :class="`severity-${item.severity}`">{{ severityLabel(item.severity) }}</span>
                            <span v-if="item.workflowStatus" class="status-badge" :class="`status-${item.workflowStatus.toLowerCase()}`">
                                {{ workflowStatusLabel(item.workflowStatus) }}
                            </span>
                        </div>
                    </button>
                </div>
            </section>

            <section class="workspace-pane detail-pane">
                <div v-if="!selectedCase" class="empty-card">Chon mot case de xem chi tiet va thao tac.</div>
                <template v-else>
                    <div class="detail-header">
                        <div>
                            <span class="panel-kicker">Case #{{ selectedCase.id }}</span>
                            <h2 class="detail-title">{{ selectedCase.subjectName || 'Unknown subject' }}</h2>
                            <p class="detail-subtitle">{{ selectedCase.reason || 'Chua mo ta ly do' }}</p>
                        </div>
                        <div class="detail-actions">
                            <button
                                v-if="canCreateIntervention(selectedCase)"
                                class="btn btn-primary btn-sm"
                                :disabled="saving"
                                @click="runPrimaryCaseAction(selectedCase)"
                            >
                                {{ casePrimaryActionLabel(selectedCase) }}
                            </button>
                            <button
                                v-if="canCloseCase(selectedCase)"
                                class="btn btn-secondary btn-sm"
                                :disabled="saving"
                                @click="closeCase(selectedCase)"
                            >
                                Khoa so case
                            </button>
                        </div>
                    </div>

                    <div v-if="caseActionMessage" class="alert" :class="caseActionError ? 'alert-danger' : 'alert-success'">
                        {{ caseActionMessage }}
                    </div>

                    <div class="meta-grid">
                        <div class="meta-item">
                            <span class="meta-label">Danh muc</span>
                            <span class="case-badge" :class="`badge-${selectedCase.category}`">{{ categoryLabel(selectedCase.category) }}</span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Muc do</span>
                            <span class="severity-badge" :class="`severity-${selectedCase.severity}`">{{ severityLabel(selectedCase.severity) }}</span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Trang thai log</span>
                            <span>{{ selectedCase.resultStatus || '---' }}</span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Bien so</span>
                            <span>{{ selectedCase.plateText || '---' }}</span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Cong</span>
                            <span>{{ selectedCase.gateName || '---' }}</span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Thoi gian</span>
                            <span>{{ formatDateTime(selectedCase.lastEventAt) }}</span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Nguon</span>
                            <span>{{ methodLabel(selectedCase.method) }}</span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Ma ly do</span>
                            <span>{{ selectedCase.reasonCode || 'UNCLASSIFIED' }}</span>
                        </div>
                    </div>

                    <div class="detail-tabs">
                        <button :class="{ active: detailTab === 'timeline' }" @click="detailTab = 'timeline'">Timeline</button>
                        <button :class="{ active: detailTab === 'events' }" @click="detailTab = 'events'; loadLaneEvents(selectedCase)">Lane events</button>
                        <button :class="{ active: detailTab === 'evidence' }" @click="detailTab = 'evidence'; loadEvidence(selectedCase)">Evidence</button>
                        <button :class="{ active: detailTab === 'barriers' }" @click="detailTab = 'barriers'; loadBarrierCommands(selectedCase)">Barrier</button>
                        <button :class="{ active: detailTab === 'correlations' }" @click="detailTab = 'correlations'; loadCorrelations(selectedCase)">Correlation</button>
                    </div>

                    <div v-if="detailTab === 'timeline'" class="detail-body">
                        <ExceptionCaseTimeline :items="selectedCase.timeline" />
                    </div>

                    <div v-else-if="detailTab === 'events'" class="detail-body">
                        <div v-if="loadingLaneEvents" class="empty-card compact">Dang tai lane events...</div>
                        <div v-else-if="laneEvents.length === 0" class="empty-card compact">Khong co lane event lien quan tu plate hien tai.</div>
                        <div v-else class="compact-list">
                            <div v-for="event in laneEvents" :key="event.laneEventId" class="compact-row">
                                <span class="compact-time">{{ formatDateTime(event.occurredAtUtc) }}</span>
                                <span class="soft-chip">{{ event.eventType }}</span>
                                <span>{{ event.lane?.name || selectedCase.resolvedLaneName || 'Unknown lane' }}</span>
                                <span class="text-muted">{{ event.note || '' }}</span>
                            </div>
                        </div>
                    </div>

                    <div v-else-if="detailTab === 'evidence'" class="detail-body">
                        <div v-if="loadingEvidence" class="empty-card compact">Dang tai evidence...</div>
                        <div v-else-if="evidenceItems.length === 0" class="empty-card compact">Chua co evidence phu hop.</div>
                        <div v-else class="compact-list">
                            <div v-for="item in evidenceItems" :key="item.evidenceItemId" class="compact-row">
                                <span class="compact-time">{{ formatDateTime(item.createdAtUtc) }}</span>
                                <strong>{{ item.fileName || `Evidence #${item.evidenceItemId}` }}</strong>
                                <span class="text-muted">{{ item.classification || '' }}</span>
                            </div>
                        </div>
                    </div>

                    <div v-else-if="detailTab === 'barriers'" class="detail-body">
                        <div v-if="loadingBarriers" class="empty-card compact">Dang tai barrier commands...</div>
                        <div v-else-if="barrierMessage" class="empty-card compact">{{ barrierMessage }}</div>
                        <div v-else-if="barrierCommands.length === 0" class="empty-card compact">Chua co lenh barrier lien quan.</div>
                        <div v-else class="compact-list">
                            <div v-for="command in barrierCommands" :key="command.barrierCommandAuditId" class="compact-row">
                                <span class="compact-time">{{ formatDateTime(command.requestedAtUtc) }}</span>
                                <span class="soft-chip">{{ command.command }}</span>
                                <span>{{ command.reason || '' }}</span>
                            </div>
                        </div>
                    </div>

                    <div v-else class="detail-body">
                        <div v-if="loadingCorrelations" class="empty-card compact">Dang tai correlations...</div>
                        <div v-else-if="correlations.length === 0" class="empty-card compact">Chua co correlation phu hop.</div>
                        <div v-else class="compact-list">
                            <div v-for="correlation in correlations" :key="correlation.correlationId" class="compact-row">
                                <span class="compact-time">{{ formatDateTime(correlation.createdAtUtc) }}</span>
                                <strong>{{ correlation.correlationType || 'Correlation' }}</strong>
                                <span class="text-muted">{{ correlation.summary || correlation.description || '' }}</span>
                            </div>
                        </div>
                    </div>
                </template>
            </section>
        </div>

        <div v-else class="workspace-grid">
            <section class="workspace-pane queue-pane">
                <div class="pane-header">
                    <div>
                        <h2 class="panel-title">Hang doi can thiep</h2>
                        <p class="pane-subtitle">Mot noi cho ca bao ve, quan ly va admin thao tac theo workflow that.</p>
                    </div>
                    <button v-if="canManuallyCreateIntervention" class="btn btn-primary btn-sm" @click="showCreateModal = true">+ Tao yeu cau</button>
                </div>

                <div class="filter-pills">
                    <button
                        v-for="filter in interventionFilters"
                        :key="filter.value"
                        class="pill-btn"
                        :class="{ active: interventionFilter === filter.value }"
                        @click="interventionFilter = filter.value"
                    >
                        {{ filter.label }}
                        <span v-if="filter.count" class="tab-count">{{ filter.count }}</span>
                    </button>
                </div>

                <div v-if="loadingInterventions" class="empty-card">Dang tai yeu cau can thiep...</div>
                <div v-else-if="filteredInterventions.length === 0" class="empty-card">Khong co yeu cau phu hop bo loc hien tai.</div>
                <div v-else class="queue-list">
                    <button
                        v-for="item in filteredInterventions"
                        :key="item.operationalInterventionRequestId"
                        class="queue-item"
                        :class="{ selected: selectedIntervention?.operationalInterventionRequestId === item.operationalInterventionRequestId, [`severity-${prioritySeverity(item.priority)}`]: true }"
                        @click="selectIntervention(item)"
                    >
                        <div class="queue-head">
                            <span class="case-badge" :class="`badge-intv-${item.interventionType}`">{{ interventionTypeLabel(item.interventionType) }}</span>
                            <span class="time-label">{{ formatRelativeTime(item.createdAtUtc) }}</span>
                        </div>
                        <strong class="queue-title">{{ item.subjectName || item.plateNumber || `Request #${item.operationalInterventionRequestId}` }}</strong>
                        <div class="queue-meta">
                            <span>{{ priorityLabel(item.priority) }}</span>
                            <span>{{ item.laneName || item.laneId || 'Khong gan lane' }}</span>
                        </div>
                        <div class="queue-footer">
                            <span class="status-badge" :class="`status-${(item.status || 'Pending').toLowerCase()}`">{{ statusLabel(item.status) }}</span>
                        </div>
                    </button>
                </div>
            </section>

            <section class="workspace-pane detail-pane">
                <div v-if="!selectedIntervention" class="empty-card">Chon mot yeu cau de xu ly.</div>
                <template v-else>
                    <div class="detail-header">
                        <div>
                            <span class="panel-kicker">Request #{{ selectedIntervention.operationalInterventionRequestId }}</span>
                            <h2 class="detail-title">{{ interventionTypeLabel(selectedIntervention.interventionType) }}</h2>
                            <p class="detail-subtitle">{{ selectedIntervention.reason }}</p>
                        </div>
                        <div class="detail-actions">
                            <button
                                v-if="canAcceptIntervention(selectedIntervention)"
                                class="btn btn-success btn-sm"
                                :disabled="savingIntervention"
                                @click="acceptIntervention(selectedIntervention)"
                            >
                                Phe duyet
                            </button>
                            <button
                                v-if="canRejectIntervention(selectedIntervention)"
                                class="btn btn-warning btn-sm"
                                :disabled="savingIntervention || !interventionReviewNote.trim()"
                                @click="rejectIntervention(selectedIntervention)"
                            >
                                Tu choi
                            </button>
                            <button
                                v-if="canExecuteIntervention(selectedIntervention)"
                                class="btn btn-primary btn-sm"
                                :disabled="savingIntervention"
                                @click="executeIntervention(selectedIntervention)"
                            >
                                Thuc thi cap quyen
                            </button>
                        </div>
                    </div>

                    <div v-if="interventionMessage" class="alert" :class="interventionError ? 'alert-danger' : 'alert-success'">
                        {{ interventionMessage }}
                    </div>

                    <div class="meta-grid">
                        <div class="meta-item">
                            <span class="meta-label">Trang thai</span>
                            <span class="status-badge" :class="`status-${(selectedIntervention.status || 'Pending').toLowerCase()}`">
                                {{ statusLabel(selectedIntervention.status) }}
                            </span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Uu tien</span>
                            <span class="severity-badge" :class="`severity-${prioritySeverity(selectedIntervention.priority)}`">
                                {{ priorityLabel(selectedIntervention.priority) }}
                            </span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Doi tuong</span>
                            <span>{{ selectedIntervention.subjectName || '---' }}</span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Bien so</span>
                            <span>{{ selectedIntervention.plateNumber || '---' }}</span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Lane</span>
                            <span>{{ selectedIntervention.laneName || selectedIntervention.laneId || '---' }}</span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Tao luc</span>
                            <span>{{ formatDateTime(selectedIntervention.createdAtUtc) }}</span>
                        </div>
                    </div>

                    <div class="detail-section">
                        <label class="meta-label" for="review-note">Ghi chu duyet / ly do tu choi</label>
                        <textarea
                            id="review-note"
                            v-model="interventionReviewNote"
                            class="form-control"
                            rows="3"
                            placeholder="Ghi ro can cu, pham vi va trach nhiem..."
                        />
                    </div>

                    <div class="detail-section">
                        <h3 class="detail-section-title">Timeline workflow</h3>
                        <ExceptionCaseTimeline :items="buildInterventionTimeline(selectedIntervention)" />
                    </div>
                </template>
            </section>
        </div>

        <Teleport to="body">
            <div v-if="showCreateModal" class="modal-backdrop" @click.self="showCreateModal = false">
                <div class="modal-card">
                    <div class="modal-header">
                        <h2>Tao yeu cau can thiep</h2>
                        <button class="btn btn-ghost btn-sm" @click="showCreateModal = false">Dong</button>
                    </div>
                    <div class="modal-body">
                        <div v-if="createMessage" class="alert" :class="createError ? 'alert-danger' : 'alert-success'">
                            {{ createMessage }}
                        </div>
                        <div class="form-grid">
                            <label>
                                <span>Loai can thiep</span>
                                <select v-model="createForm.interventionType" class="form-control">
                                    <option value="temporary_grant">Temporary grant</option>
                                    <option value="policy_override">Policy override</option>
                                    <option value="device_override">Device override</option>
                                    <option value="emergency_override">Emergency override</option>
                                    <option value="other">Other</option>
                                </select>
                            </label>
                            <label>
                                <span>Uu tien</span>
                                <select v-model="createForm.priority" class="form-control">
                                    <option value="low">Low</option>
                                    <option value="medium">Medium</option>
                                    <option value="high">High</option>
                                    <option value="critical">Critical</option>
                                </select>
                            </label>
                            <label>
                                <span>Doi tuong</span>
                                <input v-model.trim="createForm.subjectName" class="form-control" />
                            </label>
                            <label>
                                <span>Bien so</span>
                                <input v-model.trim="createForm.plateNumber" class="form-control" />
                            </label>
                            <label>
                                <span>Lane / khu vuc</span>
                                <input v-model.trim="createForm.laneName" class="form-control" />
                            </label>
                            <label class="form-span-2">
                                <span>Ly do</span>
                                <textarea v-model.trim="createForm.reason" class="form-control" rows="3" />
                            </label>
                            <label class="form-span-2">
                                <span>Ghi chu</span>
                                <textarea v-model.trim="createForm.note" class="form-control" rows="2" />
                            </label>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="showCreateModal = false">Huy</button>
                        <button class="btn btn-primary" :disabled="creating || !createForm.reason.trim()" @click="submitInterventionRequest">
                            {{ creating ? 'Dang gui...' : 'Gui yeu cau' }}
                        </button>
                    </div>
                </div>
            </div>
        </Teleport>
    </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'
import { getExceptions } from '../services/accessLogApi'
import { authState } from '../stores/auth'
import ExceptionCaseTimeline from '../components/shared/ExceptionCaseTimeline.vue'

const loading = ref(false)
const saving = ref(false)
const loadingLaneEvents = ref(false)
const loadingEvidence = ref(false)
const loadingBarriers = ref(false)
const loadingCorrelations = ref(false)
const loadingInterventions = ref(false)
const savingIntervention = ref(false)
const creating = ref(false)

const activeTab = ref('cases')
const activeCategory = ref('all')
const detailTab = ref('timeline')
const searchQuery = ref('')

const exceptionCases = ref([])
const selectedCase = ref(null)
const laneEvents = ref([])
const evidenceItems = ref([])
const barrierCommands = ref([])
const correlations = ref([])
const barrierMessage = ref('')

const interventionRequests = ref([])
const selectedIntervention = ref(null)
const interventionFilter = ref('all')
const interventionReviewNote = ref('')

const caseActionMessage = ref('')
const caseActionError = ref(false)
const interventionMessage = ref('')
const interventionError = ref(false)
const showCreateModal = ref(false)
const createMessage = ref('')
const createError = ref(false)

const createForm = reactive({
    interventionType: 'temporary_grant',
    subjectName: '',
    plateNumber: '',
    laneName: '',
    priority: 'medium',
    reason: '',
    note: '',
})

const currentRole = computed(() => authState.user?.role || 'BaoVe')
const isAdmin = computed(() => currentRole.value === 'Admin')
const isQuanLy = computed(() => currentRole.value === 'QuanLy')
const isBaoVe = computed(() => currentRole.value === 'BaoVe')
const canManuallyCreateIntervention = computed(() => isAdmin.value || isBaoVe.value)

const caseCategories = computed(() => {
    const categories = [
        { id: 'all', label: 'Tat ca', count: exceptionCases.value.length },
        { id: 'pending_approval', label: 'Cho phe duyet', count: categoryCount('pending_approval') },
        { id: 'manual_override', label: 'Override / bypass', count: categoryCount('manual_override') },
        { id: 'data_mismatch', label: 'Lech du lieu', count: categoryCount('data_mismatch') },
        { id: 'device_degraded', label: 'Device loi', count: categoryCount('device_degraded') },
        { id: 'emergency_pass', label: 'Khan cap', count: categoryCount('emergency_pass') },
        { id: 'duress', label: 'Duress', count: categoryCount('duress') },
    ]

    if (isBaoVe.value) {
        return categories.filter((item) => item.id !== 'device_degraded' || item.count > 0)
    }

    return categories
})

const filteredCases = computed(() => {
    let items = exceptionCases.value
    if (activeCategory.value !== 'all') {
        items = items.filter((item) => item.category === activeCategory.value)
    }

    if (searchQuery.value) {
        const query = searchQuery.value.toLowerCase()
        items = items.filter((item) =>
            (item.subjectName || '').toLowerCase().includes(query) ||
            (item.plateText || '').toLowerCase().includes(query) ||
            String(item.id).includes(query) ||
            String(item.sourceLogId || '').includes(query)
        )
    }

    const severityOrder = { critical: 0, high: 1, medium: 2, low: 3 }
    return [...items].sort((left, right) => {
        const severityCompare = (severityOrder[left.severity] ?? 99) - (severityOrder[right.severity] ?? 99)
        if (severityCompare !== 0) return severityCompare
        return new Date(right.lastEventAt || 0) - new Date(left.lastEventAt || 0)
    })
})

const openCaseCount = computed(() => exceptionCases.value.filter((item) => !['Closed', 'Executed'].includes(item.workflowStatus)).length)
const pendingInterventionCount = computed(() => interventionRequests.value.filter((item) => item.status === 'Pending').length)
const executedInterventionCount = computed(() => interventionRequests.value.filter((item) => item.status === 'Executed').length)

const interventionFilters = computed(() => {
    const counts = interventionRequests.value.reduce((acc, item) => {
        const key = item.status || 'Pending'
        acc[key] = (acc[key] || 0) + 1
        return acc
    }, {})

    return [
        { value: 'all', label: 'Tat ca', count: interventionRequests.value.length },
        { value: 'Pending', label: 'Cho xu ly', count: counts.Pending || 0 },
        { value: 'Accepted', label: 'Da chap nhan', count: counts.Accepted || 0 },
        { value: 'Executed', label: 'Da thuc thi', count: counts.Executed || 0 },
        { value: 'Rejected', label: 'Da tu choi', count: counts.Rejected || 0 },
        { value: 'Expired', label: 'Het han', count: counts.Expired || 0 },
    ]
})

const filteredInterventions = computed(() => {
    let items = interventionRequests.value
    if (interventionFilter.value !== 'all') {
        items = items.filter((item) => item.status === interventionFilter.value)
    }

    const priorityOrder = { critical: 0, high: 1, medium: 2, low: 3 }
    return [...items].sort((left, right) => {
        const priorityCompare = (priorityOrder[left.priority] ?? 99) - (priorityOrder[right.priority] ?? 99)
        if (priorityCompare !== 0) return priorityCompare
        return new Date(right.createdAtUtc || 0) - new Date(left.createdAtUtc || 0)
    })
})

function categoryCount(category) {
    return exceptionCases.value.filter((item) => item.category === category).length
}

function categoryLabel(value) {
    return {
        data_mismatch: 'Lech du lieu',
        manual_override: 'Override',
        device_degraded: 'Device loi',
        emergency_pass: 'Khan cap',
        duress: 'Duress',
        pending_approval: 'Cho phe duyet',
    }[value] || value
}

function severityLabel(value) {
    return {
        critical: 'Nghiem trong',
        high: 'Cao',
        medium: 'Trung binh',
        low: 'Thap',
    }[value] || value
}

function priorityLabel(value) {
    return {
        critical: 'Critical',
        high: 'High',
        medium: 'Medium',
        low: 'Low',
    }[value] || value
}

function prioritySeverity(priority) {
    return {
        critical: 'critical',
        high: 'high',
        medium: 'medium',
        low: 'low',
    }[priority] || 'medium'
}

function workflowStatusLabel(value) {
    return {
        Pending: 'Cho xu ly',
        Accepted: 'Da chap nhan',
        Executed: 'Da thuc thi',
        Rejected: 'Da tu choi',
        Escalated: 'Da chuyen hang doi',
        Closed: 'Da khoa so',
    }[value] || value
}

function statusLabel(value) {
    return {
        Pending: 'Cho xu ly',
        Accepted: 'Da chap nhan',
        Rejected: 'Da tu choi',
        Executed: 'Da thuc thi',
        Expired: 'Het han',
    }[value] || value
}

function interventionTypeLabel(value) {
    return {
        temporary_grant: 'Temporary grant',
        policy_override: 'Policy override',
        device_override: 'Device override',
        emergency_override: 'Emergency override',
        other: 'Other',
    }[value] || value
}

function methodLabel(value) {
    return {
        manual: 'Thu cong / bypass',
        plate: 'QR + bien so',
        system: 'He thong',
        'face-and-plate': 'Da nguon',
        face: 'Face',
    }[value] || value || 'He thong'
}

function formatDateTime(value) {
    if (!value) return '---'
    return new Date(value).toLocaleString('vi-VN')
}

function formatRelativeTime(value) {
    if (!value) return '---'
    const diffMs = Date.now() - new Date(value).getTime()
    const diffMinutes = Math.floor(diffMs / 60000)
    if (diffMinutes < 1) return 'Vua xong'
    if (diffMinutes < 60) return `${diffMinutes} phut`
    const diffHours = Math.floor(diffMinutes / 60)
    if (diffHours < 24) return `${diffHours} gio`
    return `${Math.floor(diffHours / 24)} ngay`
}

function buildCaseFromException(item) {
    const reasonCode = String(item.exceptionReasonCode || '').toUpperCase()
    const note = String(item.note || '')
    const noteLower = note.toLowerCase()
    const status = String(item.resultStatus || '').toUpperCase()

    let category = 'data_mismatch'
    let severity = 'medium'

    if (item.isBypass === true || reasonCode === 'TAILGATING') {
        category = 'manual_override'
        severity = 'high'
    } else if (reasonCode === 'TEMP_ACCESS' || reasonCode === 'QR_EXPIRED' || reasonCode === 'QR_REPLAY' || reasonCode === 'PLATE_REVIEW') {
        category = 'pending_approval'
        severity = reasonCode === 'PLATE_REVIEW' ? 'high' : 'medium'
    } else if (reasonCode.includes('EMERGENCY') || noteLower.includes('khan cap') || noteLower.includes('cap cuu')) {
        category = 'emergency_pass'
        severity = 'critical'
    } else if (reasonCode.includes('DURESS') || noteLower.includes('ep buoc') || noteLower.includes('duress')) {
        category = 'duress'
        severity = 'critical'
    } else if (reasonCode.includes('DEVICE') || noteLower.includes('camera') || noteLower.includes('scanner') || noteLower.includes('offline') || noteLower.includes('degraded')) {
        category = 'device_degraded'
        severity = 'high'
    } else if (status === 'DENIED') {
        category = 'data_mismatch'
        severity = 'medium'
    }

    return {
        id: item.logId,
        sourceLogId: item.logId,
        category,
        severity,
        subjectName: item.actorName || 'Unknown',
        actorType: item.actorType || 'Unknown',
        employeeId: item.employeeId || null,
        plateText: item.capturedLicensePlate || '',
        gateId: item.gateId || null,
        gateName: item.gateName || '',
        laneName: '',
        method: item.method || 'system',
        resultStatus: item.resultStatus || '',
        reasonCode,
        reason: item.exceptionReasonDescription || 'Ngoai le can doi soat',
        note,
        isBypass: item.isBypass === true,
        lastEventAt: item.timestamp,
        workflowStatus: 'Pending',
        interventionRequestId: null,
        resolvedLaneId: null,
        resolvedLaneName: '',
        timeline: [
            {
                id: `log-${item.logId}`,
                type: item.isBypass ? 'warning' : 'system',
                title: item.exceptionReasonDescription || 'Su kien ngoai le',
                description: note || item.resultStatus || 'Khong co mo ta bo sung',
                timestamp: item.timestamp,
                actor: item.actorName || '',
                reason: item.exceptionReasonCode || '',
            },
        ],
    }
}

function selectCase(item) {
    selectedCase.value = item
    detailTab.value = 'timeline'
    laneEvents.value = []
    evidenceItems.value = []
    barrierCommands.value = []
    correlations.value = []
    barrierMessage.value = ''
    caseActionMessage.value = ''
}

function selectIntervention(item) {
    selectedIntervention.value = item
    interventionReviewNote.value = item.rejectionReason || ''
    interventionMessage.value = ''
}

function canCreateIntervention(item) {
    if (!item || ['Closed', 'Executed'].includes(item.workflowStatus)) return false
    if (isBaoVe.value) return true
    if (isQuanLy.value) return item.category !== 'duress'
    return true
}

function canCloseCase(item) {
    return Boolean(item) && (isAdmin.value || isQuanLy.value)
}

function canAcceptIntervention(item) {
    return Boolean(item) && ['Pending'].includes(item.status) && (isAdmin.value || isQuanLy.value)
}

function canRejectIntervention(item) {
    return Boolean(item) && ['Pending'].includes(item.status) && (isAdmin.value || isQuanLy.value)
}

function canExecuteIntervention(item) {
    return Boolean(item) && item.status === 'Accepted' && isAdmin.value
}

function casePrimaryActionLabel(item) {
    if (isBaoVe.value) return 'Tao yeu cau can thiep'
    if (isQuanLy.value) return 'Tao va duyet'
    if (shouldExecuteImmediately(item)) return 'Tao va thuc thi'
    return 'Tao va duyet'
}

function shouldExecuteImmediately(item) {
    if (!isAdmin.value) return false
    if (item.category === 'emergency_pass' || item.category === 'device_degraded') return true
    if (item.category === 'pending_approval' && item.employeeId) return true
    return false
}

function buildInterventionPayload(item) {
    let interventionType = 'other'
    if (item.category === 'pending_approval' || item.category === 'data_mismatch') {
        interventionType = item.employeeId ? 'temporary_grant' : 'policy_override'
    } else if (item.category === 'manual_override') {
        interventionType = 'policy_override'
    } else if (item.category === 'device_degraded') {
        interventionType = 'device_override'
    } else if (item.category === 'emergency_pass' || item.category === 'duress') {
        interventionType = 'emergency_override'
    }

    return {
        interventionType,
        reason: item.reason || item.note || 'Case ngoai le can can thiep',
        laneId: item.resolvedLaneId ? String(item.resolvedLaneId) : undefined,
        laneName: item.resolvedLaneName || item.gateName || undefined,
        subjectName: item.subjectName || undefined,
        subjectId: item.employeeId ? String(item.employeeId) : undefined,
        subjectType: item.actorType || undefined,
        plateNumber: item.plateText || undefined,
        qrPayload: undefined,
        note: `SourceLogId=${item.sourceLogId}; ${item.note || ''}`.trim(),
        priority: item.severity === 'critical' ? 'critical' : item.severity === 'high' ? 'high' : 'medium',
        expiresInMinutes: item.category === 'emergency_pass' || item.category === 'duress' ? 60 : 240,
    }
}

async function runPrimaryCaseAction(item) {
    saving.value = true
    caseActionMessage.value = ''
    caseActionError.value = false

    try {
        const payload = buildInterventionPayload(item)
        const created = await enterpriseApi.createInterventionRequest(payload)
        const requestId = created.data?.operationalInterventionRequestId

        item.interventionRequestId = requestId
        item.workflowStatus = 'Pending'
        item.timeline.push({
            id: `request-${requestId}`,
            type: 'warning',
            title: 'Yeu cau can thiep da duoc tao',
            description: payload.reason,
            timestamp: new Date().toISOString(),
            actor: currentRole.value,
            reason: payload.interventionType,
        })

        if ((isAdmin.value || isQuanLy.value) && requestId) {
            await enterpriseApi.acceptInterventionRequest(requestId, { note: `Duyet tu case #${item.sourceLogId}` })
            item.workflowStatus = 'Accepted'
            item.timeline.push({
                id: `accepted-${requestId}`,
                type: 'approve',
                title: 'Yeu cau da duoc chap nhan',
                description: 'Chap nhan ngay tu trang ngoai le',
                timestamp: new Date().toISOString(),
                actor: currentRole.value,
                reason: payload.interventionType,
            })
        }

        if (shouldExecuteImmediately(item) && requestId) {
            await enterpriseApi.executeInterventionRequest(requestId, { note: `Thuc thi nhanh tu case #${item.sourceLogId}` })
            item.workflowStatus = 'Executed'
            item.timeline.push({
                id: `executed-${requestId}`,
                type: 'success',
                title: 'Yeu cau da duoc thuc thi',
                description: 'Da tao hieu luc van hanh tu workflow can thiep',
                timestamp: new Date().toISOString(),
                actor: currentRole.value,
                reason: payload.interventionType,
            })
        }

        await loadInterventions(requestId)
        caseActionMessage.value = isBaoVe.value
            ? `Da tao yeu cau can thiep #${requestId}.`
            : item.workflowStatus === 'Executed'
                ? `Da tao va thuc thi yeu cau #${requestId}.`
                : `Da tao va duyet yeu cau #${requestId}.`
    } catch (error) {
        caseActionError.value = true
        caseActionMessage.value = error?.response?.data?.message || 'Khong the tao workflow can thiep cho case nay.'
    } finally {
        saving.value = false
    }
}

async function closeCase(item) {
    saving.value = true
    caseActionMessage.value = ''
    caseActionError.value = false

    try {
        await enterpriseApi.recordLaneEvent({
            laneId: item.resolvedLaneId || null,
            plateText: item.plateText || undefined,
            eventType: 'CASE_CLOSED',
            note: `Case #${item.sourceLogId} duoc khoa so boi ${currentRole.value}`,
        })
        item.workflowStatus = 'Closed'
        item.timeline.push({
            id: `closed-${Date.now()}`,
            type: 'close',
            title: 'Case da duoc khoa so',
            description: 'Khong con yeu cau xu ly tiep tren hang doi.',
            timestamp: new Date().toISOString(),
            actor: currentRole.value,
            reason: 'Closed from exception desk',
        })
        caseActionMessage.value = `Case #${item.sourceLogId} da duoc khoa so.`
    } catch (error) {
        caseActionError.value = true
        caseActionMessage.value = error?.response?.data?.message || 'Khong the khoa so case.'
    } finally {
        saving.value = false
    }
}

async function loadLaneEvents(item = selectedCase.value) {
    if (!item) return
    loadingLaneEvents.value = true
    try {
        const params = {}
        if (item.plateText) {
            params.plateText = item.plateText
        }
        const response = await enterpriseApi.getLaneEvents({ ...params, pageSize: 20 })
        laneEvents.value = response.data?.items || []
        const firstWithLane = laneEvents.value.find((entry) => entry.laneId || entry.lane?.laneId)
        if (firstWithLane) {
            item.resolvedLaneId = firstWithLane.laneId || firstWithLane.lane?.laneId || null
            item.resolvedLaneName = firstWithLane.lane?.name || item.resolvedLaneName || ''
        }
    } catch {
        laneEvents.value = []
    } finally {
        loadingLaneEvents.value = false
    }
}

async function loadEvidence(item = selectedCase.value) {
    if (!item) return
    loadingEvidence.value = true
    try {
        const query = item.plateText || item.subjectName
        const response = await enterpriseApi.getEvidenceItems({ query, pageSize: 10 })
        evidenceItems.value = response.data?.items || []
    } catch {
        evidenceItems.value = []
    } finally {
        loadingEvidence.value = false
    }
}

async function loadBarrierCommands(item = selectedCase.value) {
    if (!item) return
    loadingBarriers.value = true
    barrierMessage.value = ''
    barrierCommands.value = []
    try {
        let laneId = item.resolvedLaneId
        if (!laneId) {
            await loadLaneEvents(item)
            laneId = item.resolvedLaneId
        }

        if (!laneId) {
            barrierMessage.value = 'Case nay chua truy ra duoc lane cu the, khong the doi barrier command mot cach dang tin cay.'
            return
        }

        const barriersResponse = await enterpriseApi.getBarriers({ laneId, active: true })
        const barriers = barriersResponse.data || []
        if (!barriers.length) {
            barrierMessage.value = 'Khong co barrier nao duoc gan voi lane nay.'
            return
        }

        const response = await enterpriseApi.getBarrierCommands(barriers[0].barrierId, { pageSize: 20 })
        barrierCommands.value = response.data?.items || []
    } catch {
        barrierMessage.value = 'Khong the tai lich su barrier command.'
    } finally {
        loadingBarriers.value = false
    }
}

async function loadCorrelations(item = selectedCase.value) {
    if (!item) return
    loadingCorrelations.value = true
    try {
        const query = item.plateText || item.subjectName
        const response = await enterpriseApi.getCorrelations({ query, pageSize: 10 })
        correlations.value = response.data?.items || []
    } catch {
        correlations.value = []
    } finally {
        loadingCorrelations.value = false
    }
}

async function loadInterventions(preferredRequestId = null) {
    loadingInterventions.value = true
    try {
        const response = await enterpriseApi.getInterventionRequests({ pageSize: 100 })
        interventionRequests.value = response.data?.items || []
        if (interventionRequests.value.length === 0) {
            selectedIntervention.value = null
            return
        }

        const targetId = preferredRequestId || selectedIntervention.value?.operationalInterventionRequestId
        const preferredItem = targetId
            ? interventionRequests.value.find((item) => item.operationalInterventionRequestId === targetId)
            : null

        selectIntervention(preferredItem || interventionRequests.value[0])
    } catch {
        interventionRequests.value = []
        selectedIntervention.value = null
    } finally {
        loadingInterventions.value = false
    }
}

async function refreshInterventionSelection(requestId, successMessage) {
    await loadInterventions(requestId)
    interventionError.value = false
    interventionMessage.value = successMessage
    syncCasesWithInterventions()
}

async function acceptIntervention(item) {
    savingIntervention.value = true
    interventionMessage.value = ''
    interventionError.value = false
    try {
        const response = await enterpriseApi.acceptInterventionRequest(item.operationalInterventionRequestId, {
            note: interventionReviewNote.value || 'Chap nhan tu trang ngoai le',
        })
        Object.assign(item, response.data)
        await refreshInterventionSelection(
            item.operationalInterventionRequestId,
            `Yeu cau #${item.operationalInterventionRequestId} da duoc chap nhan.`,
        )
    } catch (error) {
        interventionError.value = true
        interventionMessage.value = error?.response?.data?.message || 'Chap nhan that bai.'
    } finally {
        savingIntervention.value = false
    }
}

async function rejectIntervention(item) {
    savingIntervention.value = true
    interventionMessage.value = ''
    interventionError.value = false
    try {
        const response = await enterpriseApi.rejectInterventionRequest(item.operationalInterventionRequestId, {
            note: interventionReviewNote.value,
        })
        Object.assign(item, response.data)
        await refreshInterventionSelection(
            item.operationalInterventionRequestId,
            `Yeu cau #${item.operationalInterventionRequestId} da bi tu choi.`,
        )
    } catch (error) {
        interventionError.value = true
        interventionMessage.value = error?.response?.data?.message || 'Tu choi that bai.'
    } finally {
        savingIntervention.value = false
    }
}

async function executeIntervention(item) {
    savingIntervention.value = true
    interventionMessage.value = ''
    interventionError.value = false
    try {
        const response = await enterpriseApi.executeInterventionRequest(item.operationalInterventionRequestId, {
            note: interventionReviewNote.value || 'Thuc thi tu trang ngoai le',
        })
        Object.assign(item, response.data?.request || response.data)
        await refreshInterventionSelection(
            item.operationalInterventionRequestId,
            `Yeu cau #${item.operationalInterventionRequestId} da duoc thuc thi.`,
        )
    } catch (error) {
        interventionError.value = true
        interventionMessage.value = error?.response?.data?.message || 'Thuc thi that bai.'
    } finally {
        savingIntervention.value = false
    }
}

function buildInterventionTimeline(item) {
    const timeline = [
        {
            id: `created-${item.operationalInterventionRequestId}`,
            type: 'system',
            title: 'Yeu cau duoc tao',
            description: item.reason,
            timestamp: item.createdAtUtc,
            actor: item.requestedByUserId ? `User #${item.requestedByUserId}` : '',
            reason: item.interventionType,
        },
    ]

    if (item.acceptedAtUtc) {
        timeline.push({
            id: `accepted-${item.operationalInterventionRequestId}`,
            type: 'approve',
            title: 'Yeu cau duoc chap nhan',
            description: item.note || '',
            timestamp: item.acceptedAtUtc,
            actor: item.acceptedByUserId ? `User #${item.acceptedByUserId}` : '',
            reason: 'Accepted',
        })
    }

    if (item.rejectedAtUtc) {
        timeline.push({
            id: `rejected-${item.operationalInterventionRequestId}`,
            type: 'reject',
            title: 'Yeu cau bi tu choi',
            description: item.rejectionReason || '',
            timestamp: item.rejectedAtUtc,
            actor: item.rejectedByUserId ? `User #${item.rejectedByUserId}` : '',
            reason: 'Rejected',
        })
    }

    if (item.executedAtUtc) {
        timeline.push({
            id: `executed-${item.operationalInterventionRequestId}`,
            type: 'success',
            title: 'Yeu cau duoc thuc thi',
            description: item.note || '',
            timestamp: item.executedAtUtc,
            actor: item.executedByUserId ? `User #${item.executedByUserId}` : '',
            reason: 'Executed',
        })
    }

    return timeline
}

function syncCasesWithInterventions() {
    const requestsById = new Map(interventionRequests.value.map((item) => [item.operationalInterventionRequestId, item]))
    for (const item of exceptionCases.value) {
        if (!item.interventionRequestId) continue
        const request = requestsById.get(item.interventionRequestId)
        if (!request) continue
        item.workflowStatus = request.status
    }
}

async function submitInterventionRequest() {
    creating.value = true
    createError.value = false
    createMessage.value = ''
    try {
        await enterpriseApi.createInterventionRequest({
            interventionType: createForm.interventionType,
            reason: createForm.reason,
            laneName: createForm.laneName || undefined,
            subjectName: createForm.subjectName || undefined,
            plateNumber: createForm.plateNumber || undefined,
            note: createForm.note || undefined,
            priority: createForm.priority,
            expiresInMinutes: 240,
        })

        createMessage.value = 'Da gui yeu cau can thiep.'
        createForm.interventionType = 'temporary_grant'
        createForm.subjectName = ''
        createForm.plateNumber = ''
        createForm.laneName = ''
        createForm.priority = 'medium'
        createForm.reason = ''
        createForm.note = ''
        await loadInterventions()
    } catch (error) {
        createError.value = true
        createMessage.value = error?.response?.data?.message || 'Gui yeu cau that bai.'
    } finally {
        creating.value = false
    }
}

async function loadAll() {
    loading.value = true
    try {
        const response = await getExceptions({ pageSize: 100 })
        const rawItems = response.data?.items || []
        exceptionCases.value = rawItems
            .filter((item) => !(String(item.note || '').startsWith('UEBA_DEMO_SCENARIO:')))
            .map(buildCaseFromException)

        if (!selectedCase.value && exceptionCases.value.length > 0) {
            selectCase(exceptionCases.value[0])
        } else if (selectedCase.value) {
            const fresh = exceptionCases.value.find((item) => item.id === selectedCase.value.id)
            selectedCase.value = fresh || exceptionCases.value[0] || null
        }

        await loadInterventions()
        syncCasesWithInterventions()
    } finally {
        loading.value = false
    }
}

onMounted(loadAll)
</script>

<style scoped>
.summary-strip {
    display: grid;
    grid-template-columns: repeat(4, minmax(0, 1fr));
    gap: 12px;
    margin-bottom: 16px;
}

.tab-bar {
    display: inline-flex;
    align-items: center;
    gap: 10px;
    margin: 0 0 16px;
    padding: 8px;
    background: rgba(255, 255, 255, 0.92);
    border: 1px solid #dbe3ea;
    border-radius: 16px;
    box-shadow: 0 10px 24px rgba(15, 23, 42, 0.06);
    backdrop-filter: blur(8px);
}

.tab-bar button {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 8px;
    min-height: 44px;
    padding: 0 16px;
    border: 0;
    border-radius: 12px;
    background: transparent;
    color: #425466;
    font-weight: 700;
    font-size: 0.96rem;
    line-height: 1;
    transition: background 0.15s ease, color 0.15s ease, box-shadow 0.15s ease;
}

.tab-bar button:hover {
    background: #f4f8fb;
    color: #17324d;
}

.tab-bar button.active {
    background: linear-gradient(135deg, #0f7f8e, #2563eb);
    color: #ffffff;
    box-shadow: 0 10px 22px rgba(37, 99, 235, 0.18);
}

.summary-card,
.workspace-pane {
    background: #fff;
    border: 1px solid #dbe3ea;
    border-radius: 14px;
    box-shadow: 0 8px 18px rgba(15, 23, 42, 0.05);
}

.summary-card {
    padding: 14px 16px;
    display: flex;
    flex-direction: column;
    gap: 6px;
}

.summary-label {
    color: #64748b;
    font-size: 0.82rem;
}

.summary-value {
    color: #10233e;
    font-size: 1.6rem;
    line-height: 1;
}

.summary-card small {
    color: #76879a;
}

.workspace-grid {
    display: grid;
    grid-template-columns: 380px 1fr;
    gap: 16px;
    min-height: 620px;
}

.workspace-pane {
    padding: 16px;
}

.queue-pane,
.detail-pane {
    overflow: hidden;
}

.pane-header,
.detail-header {
    display: flex;
    justify-content: space-between;
    gap: 16px;
    align-items: flex-start;
}

.pane-subtitle,
.detail-subtitle {
    margin: 4px 0 0;
    color: #64748b;
}

.detail-title {
    margin: 4px 0 0;
    font-size: 1.35rem;
    color: #10233e;
}

.search-shell {
    min-width: 240px;
}

.filter-input,
.form-control {
    width: 100%;
    border: 1px solid #d7e2ec;
    border-radius: 10px;
    padding: 10px 12px;
    background: #fff;
    color: #142033;
}

.filter-pills,
.detail-tabs {
    display: flex;
    gap: 8px;
    flex-wrap: wrap;
    margin-top: 14px;
}

.pill-btn {
    border: 1px solid #d7e2ec;
    background: #f8fafc;
    color: #34475d;
    border-radius: 999px;
    padding: 8px 12px;
    font-weight: 600;
}

.pill-btn.active,
.detail-tabs button.active {
    background: #e8f1ff;
    color: #1d4ed8;
    border-color: #b9d2ff;
}

.tab-count {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 22px;
    height: 22px;
    padding: 0 7px;
    border-radius: 999px;
    background: #e7eef6;
    color: #31465f;
    font-size: 0.76rem;
    font-weight: 800;
}

.tab-bar button.active .tab-count {
    background: rgba(255, 255, 255, 0.2);
    color: #ffffff;
}

.tab-count.danger {
    background: #fff0f0;
    color: #b42318;
}

.queue-list {
    display: flex;
    flex-direction: column;
    gap: 8px;
    margin-top: 14px;
    max-height: calc(100vh - 340px);
    overflow: auto;
    padding-right: 4px;
}

.queue-item {
    border: 1px solid #dde6ef;
    background: #fff;
    border-radius: 12px;
    padding: 12px;
    text-align: left;
    transition: border-color 0.12s ease, box-shadow 0.12s ease, background 0.12s ease;
}

.queue-item:hover {
    border-color: #9db7d2;
    box-shadow: 0 6px 18px rgba(15, 23, 42, 0.06);
}

.queue-item.selected {
    border-color: #2563eb;
    background: #f6faff;
    box-shadow: 0 0 0 2px rgba(37, 99, 235, 0.1);
}

.queue-head,
.queue-meta,
.queue-footer {
    display: flex;
    justify-content: space-between;
    gap: 10px;
    align-items: center;
}

.queue-title {
    display: block;
    margin: 8px 0 6px;
    color: #10233e;
}

.queue-meta,
.queue-footer,
.time-label {
    color: #64748b;
    font-size: 0.82rem;
}

.case-badge,
.severity-badge,
.status-badge,
.plate-badge {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    border-radius: 999px;
    padding: 4px 10px;
    font-size: 0.78rem;
    font-weight: 700;
}

.badge-data_mismatch,
.badge-pending_approval {
    background: #eef4ff;
    color: #35579d;
}

.badge-manual_override,
.badge-intv-policy_override,
.badge-intv-device_override {
    background: #fff5e8;
    color: #a46008;
}

.badge-device_degraded,
.badge-intv-other {
    background: #f3f4f6;
    color: #475569;
}

.badge-emergency_pass,
.badge-duress,
.badge-intv-emergency_override {
    background: #fff0f0;
    color: #b42318;
}

.badge-intv-temporary_grant {
    background: #edf8f3;
    color: #166534;
}

.severity-critical,
.status-rejected {
    background: #fff0f0;
    color: #b42318;
}

.severity-high,
.status-pending {
    background: #fff7e9;
    color: #a16207;
}

.severity-medium,
.status-accepted {
    background: #eef4ff;
    color: #35579d;
}

.severity-low,
.status-executed,
.status-closed {
    background: #edf8f3;
    color: #166534;
}

.status-expired {
    background: #f3f4f6;
    color: #475569;
}

.plate-badge {
    background: #f1f5f9;
    color: #0f172a;
}

.detail-actions {
    display: flex;
    gap: 8px;
    flex-wrap: wrap;
}

.meta-grid {
    display: grid;
    grid-template-columns: repeat(4, minmax(0, 1fr));
    gap: 12px;
    margin-top: 16px;
}

.meta-item {
    border: 1px solid #e5edf5;
    border-radius: 12px;
    padding: 12px;
    background: #fbfdff;
    display: flex;
    flex-direction: column;
    gap: 6px;
}

.meta-label {
    color: #64748b;
    font-size: 0.78rem;
    font-weight: 700;
}

.detail-body,
.detail-section {
    margin-top: 16px;
}

.detail-section-title {
    margin: 0 0 10px;
    color: #10233e;
}

.compact-list {
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.compact-row {
    border: 1px solid #e6edf4;
    border-radius: 10px;
    background: #fff;
    padding: 10px 12px;
    display: grid;
    grid-template-columns: 180px auto auto 1fr;
    gap: 10px;
    align-items: center;
}

.compact-time {
    color: #64748b;
    font-size: 0.8rem;
}

.text-muted {
    color: #64748b;
}

.alert {
    border-radius: 12px;
    padding: 12px 14px;
    margin-top: 14px;
    border: 1px solid transparent;
}

.alert-success {
    background: #edf8f3;
    border-color: #bfe3cb;
    color: #166534;
}

.alert-danger {
    background: #fff0f0;
    border-color: #f1c5c5;
    color: #b42318;
}

.empty-card.compact,
.empty-card {
    border-radius: 12px;
    background: #f8fafc;
    color: #64748b;
    padding: 24px;
    text-align: center;
    border: 1px dashed #d5e1eb;
}

.modal-backdrop {
    position: fixed;
    inset: 0;
    background: rgba(15, 23, 42, 0.4);
    display: grid;
    place-items: center;
    z-index: 90;
}

.modal-card {
    width: min(720px, calc(100vw - 32px));
    background: #fff;
    border-radius: 18px;
    box-shadow: 0 30px 60px rgba(15, 23, 42, 0.25);
    overflow: hidden;
}

.modal-header,
.modal-footer {
    padding: 16px 18px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 12px;
}

.modal-header {
    border-bottom: 1px solid #e6edf4;
}

.modal-body {
    padding: 18px;
}

.modal-footer {
    border-top: 1px solid #e6edf4;
}

.form-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 14px;
}

.form-grid label {
    display: flex;
    flex-direction: column;
    gap: 6px;
}

.form-span-2 {
    grid-column: 1 / -1;
}

@media (max-width: 1100px) {
    .summary-strip,
    .meta-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }

    .workspace-grid {
        grid-template-columns: 1fr;
    }

    .queue-list {
        max-height: none;
    }
}

@media (max-width: 720px) {
    .summary-strip,
    .meta-grid,
    .form-grid {
        grid-template-columns: 1fr;
    }

    .tab-bar {
        display: flex;
        width: 100%;
        flex-wrap: wrap;
    }

    .tab-bar button {
        flex: 1 1 220px;
    }

    .compact-row {
        grid-template-columns: 1fr;
    }
}
</style>
