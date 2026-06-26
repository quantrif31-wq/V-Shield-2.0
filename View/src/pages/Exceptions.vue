<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Exception Management</span>
                <h1 class="page-title">Trung tâm ngoại lệ</h1>
                <p class="page-subtitle">Hàng đợi case cần hậu kiểm và xử lý</p>
            </div>
            <div class="header-actions">
                <span v-if="isUsingDemoData" class="soft-chip danger">DEMO DATA</span>
                <span class="soft-chip warn">{{ totalPending }} chờ xử lý</span>
                <button v-if="interventionPendingCount > 0" class="soft-chip danger" style="cursor:pointer" @click="switchToEscalations">
                    {{ interventionPendingCount }} Yêu cầu can thiệp
                </button>
                <button class="btn btn-secondary btn-sm" :disabled="loading" @click="loadAll">Refresh</button>
            </div>
        </div>

        <!-- Tabs: Exception Cases / Escalation Queue -->
        <div class="tab-bar">
            <button :class="{ active: activeTab === 'cases' }" @click="activeTab = 'cases'">
                Case ngoại lệ
                <span v-if="allCases.length" class="tab-count">{{ allCases.length }}</span>
            </button>
            <button :class="{ active: activeTab === 'escalation' }" @click="activeTab = 'escalation'">
                Yêu cầu can thiệp
                <span v-if="interventionPendingCount" class="tab-count danger">{{ interventionPendingCount }}</span>
            </button>
        </div>

        <!-- Case Classification Sub-Tabs (only for cases tab) -->
        <div v-if="activeTab === 'cases'" class="tab-bar sub-tabs">
            <button
                v-for="cat in caseCategories"
                :key="cat.id"
                :class="{ active: activeCategory === cat.id }"
                @click="activeCategory = cat.id"
            >
                {{ cat.label }}
                <span v-if="cat.count" class="tab-count">{{ cat.count }}</span>
            </button>
        </div>

        <!-- Queue + Detail Layout -->
        <div class="exception-layout">
            <!-- Left: Case Queue -->
            <section class="exception-queue">
                <div class="panel-head">
                    <h2 class="panel-title">Danh sách case</h2>
                    <div class="toolbar-shell compact">
                        <div class="search-bar">
                            <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <circle cx="11" cy="11" r="8" /><path d="M21 21l-4.35-4.35" />
                            </svg>
                            <input v-model="searchQuery" type="text" placeholder="Tìm case..." @input="onSearchInput" />
                        </div>
                    </div>
                </div>

                <div v-if="loading" class="empty-card">Đang tải danh sách case...</div>
                <div v-else-if="filteredCases.length === 0" class="empty-card">
                    Không có case nào thuộc danh mục này.
                </div>
                <div v-else class="case-list">
                    <div
                        v-for="c in filteredCases"
                        :key="c.id"
                        class="case-row"
                        :class="{ 'case-row--selected': selectedCase?.id === c.id, [`case-row--${c.severity}`]: true }"
                        @click="selectCase(c)"
                        role="button"
                        :tabindex="0"
                        @keydown.enter="selectCase(c)"
                    >
                        <div class="case-row-head">
                            <span class="case-category-badge" :class="`badge--${c.category}`">{{ categoryLabel(c.category) }}</span>
                            <span class="case-time">{{ formatRelativeTime(c.lastEventAt) }}</span>
                        </div>
                        <div class="case-row-body">
                            <span class="case-subject">{{ c.subjectName || c.plateText || 'Unknown' }}</span>
                            <span v-if="c.plateText" class="case-plate">{{ c.plateText }}</span>
                        </div>
                        <div class="case-row-footer">
                            <span class="case-severity" :class="`severity--${c.severity}`">{{ severityLabel(c.severity) }}</span>
                            <span v-if="c.pendingDuration" class="case-duration">{{ c.pendingDuration }}</span>
                        </div>
                    </div>
                </div>
            </section>

            <!-- Center/Right: Case Detail -->
            <section v-if="!selectedCase" class="exception-detail">
                <div class="empty-card">Chọn một case để xem chi tiết</div>
            </section>

            <section v-else class="exception-detail">
                <div class="detail-header">
                    <div>
                        <span class="panel-kicker">Case #{{ selectedCase.id }}</span>
                        <h2 class="detail-title">{{ selectedCase.subjectName || 'Unknown' }}</h2>
                    </div>
                    <div class="detail-actions">
                        <button
                            v-if="canApprove(selectedCase)"
                            class="btn btn-sm btn-success"
                            :disabled="saving"
                            @click="approveCase(selectedCase)"
                        >
                            {{ saving ? 'Đang xử lý...' : 'Phê duyệt' }}
                        </button>
                        <button
                            v-if="canClose(selectedCase)"
                            class="btn btn-sm btn-secondary"
                            :disabled="saving"
                            @click="closeCase(selectedCase)"
                        >
                            Đóng case
                        </button>
                        <button
                            v-if="canEscalate(selectedCase)"
                            class="btn btn-sm btn-warning"
                            :disabled="saving"
                            @click="escalateCase(selectedCase)"
                        >
                            Chuyển Admin
                        </button>
                    </div>
                </div>

                <div class="detail-meta-grid">
                    <div class="detail-meta-item">
                        <span class="meta-label">Danh mục</span>
                        <span class="case-category-badge" :class="`badge--${selectedCase.category}`">{{ categoryLabel(selectedCase.category) }}</span>
                    </div>
                    <div class="detail-meta-item">
                        <span class="meta-label">Mức độ</span>
                        <span class="case-severity" :class="`severity--${selectedCase.severity}`">{{ severityLabel(selectedCase.severity) }}</span>
                    </div>
                    <div class="detail-meta-item">
                        <span class="meta-label">Biển số</span>
                        <span v-if="selectedCase.plateText" class="plate-badge">{{ selectedCase.plateText }}</span>
                        <span v-else class="text-muted">---</span>
                    </div>
                    <div class="detail-meta-item">
                        <span class="meta-label">Thời gian</span>
                        <span>{{ formatDateTime(selectedCase.lastEventAt) }}</span>
                    </div>
                    <div class="detail-meta-item" v-if="selectedCase.laneName">
                        <span class="meta-label">Làn</span>
                        <span>{{ selectedCase.laneName }}</span>
                    </div>
                    <div class="detail-meta-item" v-if="selectedCase.gateName">
                        <span class="meta-label">Cổng</span>
                        <span>{{ selectedCase.gateName }}</span>
                    </div>
                </div>

                <!-- Case Timeline -->
                <div class="detail-section">
                    <h3 class="detail-section-title">Diễn biến case</h3>
                    <ExceptionCaseTimeline :items="caseTimeline" />
                </div>

                <!-- Related Data Tabs -->
                <div class="detail-section">
                    <div class="detail-tabs">
                        <button :class="{ active: detailTab === 'events' }" @click="detailTab = 'events'">Lane Events</button>
                        <button :class="{ active: detailTab === 'evidence' }" @click="detailTab = 'evidence'; loadEvidence()">Evidence</button>
                        <button :class="{ active: detailTab === 'barriers' }" @click="detailTab = 'barriers'; loadBarrierCommands()">Barriers</button>
                        <button :class="{ active: detailTab === 'correlations' }" @click="detailTab = 'correlations'; loadCorrelations()">Correlations</button>
                    </div>

                    <div v-if="detailTab === 'events'" class="detail-tab-content">
                        <div v-if="laneEvents.length === 0" class="text-muted">No lane events.</div>
                        <div v-else class="compact-list">
                            <div v-for="e in laneEvents" :key="e.laneEventId" class="compact-row">
                                <span class="compact-time">{{ formatTime(e.occurredAtUtc) }}</span>
                                <span class="soft-chip" :class="eventChipClass(e.eventType)">{{ e.eventType }}</span>
                                <span>{{ e.note || '' }}</span>
                            </div>
                        </div>
                    </div>

                    <div v-if="detailTab === 'evidence'" class="detail-tab-content">
                        <div v-if="evidenceItems.length === 0 && !loadingEvidence" class="text-muted">No evidence items.</div>
                        <div v-else-if="loadingEvidence" class="text-muted">Loading...</div>
                        <div v-else class="compact-list">
                            <div v-for="ev in evidenceItems" :key="ev.evidenceItemId" class="compact-row">
                                <span class="compact-time">{{ formatTime(ev.createdAtUtc) }}</span>
                                <span>{{ ev.fileName || 'Evidence' }}</span>
                                <button class="btn btn-xs btn-ghost" @click="viewEvidence(ev)">View</button>
                            </div>
                        </div>
                    </div>

                    <div v-if="detailTab === 'barriers'" class="detail-tab-content">
                        <div v-if="barrierCommands.length === 0 && !loadingBarriers" class="text-muted">No barrier commands.</div>
                        <div v-else-if="loadingBarriers" class="text-muted">Loading...</div>
                        <div v-else class="compact-list">
                            <div v-for="b in barrierCommands" :key="b.barrierCommandAuditId" class="compact-row">
                                <span class="compact-time">{{ formatTime(b.requestedAtUtc) }}</span>
                                <span class="soft-chip">{{ b.command }}</span>
                                <span>{{ b.reason || '' }}</span>
                            </div>
                        </div>
                    </div>

                    <div v-if="detailTab === 'correlations'" class="detail-tab-content">
                        <div v-if="correlations.length === 0 && !loadingCorrelations" class="text-muted">No correlations.</div>
                        <div v-else-if="loadingCorrelations" class="text-muted">Loading...</div>
                        <div v-else class="compact-list">
                            <div v-for="cor in correlations" :key="cor.correlationId" class="compact-row">
                                <span class="compact-time">{{ formatTime(cor.createdAtUtc) }}</span>
                                <span>{{ cor.correlationType || 'Correlation' }}</span>
                            </div>
                        </div>
                    </div>
                </div>
            </section>
        </div>

        <!-- ==================== ESCALATION QUEUE LAYOUT ==================== -->
        <div v-if="activeTab === 'escalation'" class="exception-layout">
            <!-- Left: Intervention Request Queue -->
            <section class="exception-queue">
                <div class="panel-head">
                    <h2 class="panel-title">Yêu cầu can thiệp</h2>
                    <div class="toolbar-shell compact">
                        <button
                            v-if="isAdmin || currentRole === 'BaoVe'"
                            class="btn btn-sm btn-primary"
                            @click="showCreateModal = true"
                        >
                            + Tạo yêu cầu
                        </button>
                    </div>
                </div>

                <!-- Status filter tabs -->
                <div class="filter-pills">
                    <button
                        v-for="s in statusFilters"
                        :key="s.value"
                        :class="{ active: interventionStatusFilter === s.value }"
                        @click="interventionStatusFilter = s.value"
                        class="pill-btn"
                    >
                        {{ s.label }}
                        <span v-if="s.count" class="tab-count">{{ s.count }}</span>
                    </button>
                </div>

                <EnterpriseDataTable
                    :columns="interventionColumns"
                    :rows="filteredInterventions"
                    :loading="loadingInterventions"
                    row-key="operationalInterventionRequestId"
                    density="compact"
                    empty-title="Không có yêu cầu"
                    empty-message="Hàng chờ hiện không có yêu cầu phù hợp bộ lọc."
                >
                    <template #cell:subjectName="{ row }">
                        <strong>{{ row.subjectName || row.plateNumber || 'Chưa xác định' }}</strong>
                        <small class="table-subline">{{ row.plateNumber || row.laneName || '' }}</small>
                    </template>
                    <template #cell:priority="{ row }"><span class="case-severity" :class="`severity--${severityForPriority(row.priority)}`">{{ priorityLabel(row.priority) }}</span></template>
                    <template #cell:status="{ row }"><span class="soft-chip" :class="`intv-status--${row.status.toLowerCase()}`">{{ statusLabel(row.status) }}</span></template>
                    <template #rowActions="{ row }"><button class="btn btn-sm btn-secondary" @click="selectIntervention(row)">Xem</button></template>
                </EnterpriseDataTable>
            </section>

            <!-- Center/Right: Intervention Detail -->
            <section v-if="!selectedIntervention" class="exception-detail">
                <div class="empty-card">Chọn một yêu cầu để xem chi tiết</div>
            </section>

            <section v-else class="exception-detail">
                <div class="detail-header">
                    <div>
                        <span class="panel-kicker">Request #{{ selectedIntervention.operationalInterventionRequestId }}</span>
                        <h2 class="detail-title">{{ interventionTypeLabel(selectedIntervention.interventionType) }}</h2>
                    </div>
                    <div class="detail-actions">
                        <!-- Approval actions -->
                        <button
                            v-if="canReviewInterventions && selectedIntervention.status === 'Pending'"
                            class="btn btn-sm btn-success"
                            :disabled="savingIntervention"
                            @click="acceptIntervention(selectedIntervention)"
                        >
                            {{ savingIntervention ? '...' : 'Chấp nhận' }}
                        </button>
                        <button
                            v-if="canReviewInterventions && selectedIntervention.status === 'Pending'"
                            class="btn btn-sm btn-danger"
                            :disabled="savingIntervention"
                            @click="rejectIntervention(selectedIntervention)"
                        >
                            Từ chối
                        </button>
                        <button
                            v-if="isAdmin && selectedIntervention.status === 'Accepted'"
                            class="btn btn-sm btn-primary"
                            :disabled="savingIntervention"
                            @click="executeIntervention(selectedIntervention)"
                        >
                            Thực thi
                        </button>
                    </div>
                </div>

                <div v-if="selectedIntervention.status === 'Pending' && canReviewInterventions" class="detail-section">
                    <label class="meta-label" for="intervention-review-note">Ghi chú duyệt / lý do từ chối</label>
                    <textarea id="intervention-review-note" v-model.trim="interventionReviewNote" class="form-control" rows="2" placeholder="Bắt buộc khi từ chối; nên ghi rõ căn cứ khi chấp nhận"></textarea>
                </div>
                <div v-if="interventionActionMessage" class="alert" :class="interventionActionError ? 'alert-danger' : 'alert-success'">
                    {{ interventionActionMessage }}
                </div>

                <div class="detail-meta-grid">
                    <div class="detail-meta-item">
                        <span class="meta-label">Loại can thiệp</span>
                        <span class="case-category-badge" :class="`badge--intv-${selectedIntervention.interventionType}`">
                            {{ interventionTypeLabel(selectedIntervention.interventionType) }}
                        </span>
                    </div>
                    <div class="detail-meta-item">
                        <span class="meta-label">Trạng thái</span>
                        <span class="soft-chip" :class="`intv-status--${selectedIntervention.status.toLowerCase()}`">
                            {{ statusLabel(selectedIntervention.status) }}
                        </span>
                    </div>
                    <div class="detail-meta-item">
                        <span class="meta-label">Mức độ</span>
                        <span class="case-severity" :class="`severity--${severityForPriority(selectedIntervention.priority)}`">
                            {{ priorityLabel(selectedIntervention.priority) }}
                        </span>
                    </div>
                    <div class="detail-meta-item">
                        <span class="meta-label">Biển số</span>
                        <span v-if="selectedIntervention.plateNumber" class="plate-badge">{{ selectedIntervention.plateNumber }}</span>
                        <span v-else class="text-muted">---</span>
                    </div>
                    <div class="detail-meta-item">
                        <span class="meta-label">Đối tượng</span>
                        <span>{{ selectedIntervention.subjectName || '---' }}</span>
                    </div>
                    <div class="detail-meta-item">
                        <span class="meta-label">Làn</span>
                        <span>{{ selectedIntervention.laneName || '---' }}</span>
                    </div>
                    <div class="detail-meta-item" v-if="selectedIntervention.requestedByUserId">
                        <span class="meta-label">Người yêu cầu</span>
                        <span>User #{{ selectedIntervention.requestedByUserId }}</span>
                    </div>
                    <div class="detail-meta-item">
                        <span class="meta-label">Thời gian tạo</span>
                        <span>{{ formatDateTime(selectedIntervention.createdAtUtc) }}</span>
                    </div>
                </div>

                <!-- Reason -->
                <div class="detail-section">
                    <h3 class="detail-section-title">Lý do</h3>
                    <div class="reason-box">{{ selectedIntervention.reason }}</div>
                    <div v-if="selectedIntervention.note" class="note-box">
                        <strong>Ghi chú:</strong> {{ selectedIntervention.note }}
                    </div>
                </div>

                <!-- Status Timeline -->
                <div class="detail-section">
                    <h3 class="detail-section-title">Lịch sử xử lý</h3>
                    <ExceptionCaseTimeline :items="buildInterventionTimeline(selectedIntervention)" />
                </div>
            </section>
        </div>

        <!-- ==================== CREATE REQUEST MODAL ==================== -->
        <Teleport to="body">
            <div v-if="showCreateModal" class="modal-overlay" @click.self="showCreateModal = false">
                <div class="modal-panel" style="max-width:520px">
                    <div class="modal-header">
                        <h2>Yêu cầu can thiệp</h2>
                        <button class="btn-close" @click="showCreateModal = false">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="form-group">
                            <label>Loại can thiệp *</label>
                            <select v-model="createForm.interventionType" class="form-control">
                                <option value="temporary_grant">Temporary Grant</option>
                                <option value="anti_passback_reset">Anti-passback Reset</option>
                                <option value="emergency_override">Emergency Override</option>
                                <option value="policy_override">Policy Override</option>
                                <option value="device_override">Device Override</option>
                                <option value="other">Khác</option>
                            </select>
                        </div>
                        <div class="form-group">
                            <label>Tên đối tượng</label>
                            <input v-model="createForm.subjectName" class="form-control" placeholder="Tên người/xe" />
                        </div>
                        <div class="form-row two">
                            <div class="form-group">
                                <label>Biển số</label>
                                <input v-model="createForm.plateNumber" class="form-control" placeholder="VD: 51F-888.88" />
                            </div>
                            <div class="form-group">
                                <label>Làn</label>
                                <input v-model="createForm.laneName" class="form-control" placeholder="VD: Làn 1" />
                            </div>
                        </div>
                        <div class="form-group">
                            <label>Mức độ</label>
                            <select v-model="createForm.priority" class="form-control">
                                <option value="low">Thấp</option>
                                <option value="medium" selected>Trung bình</option>
                                <option value="high">Cao</option>
                                <option value="critical">Nghiêm trọng</option>
                            </select>
                        </div>
                        <div class="form-group">
                            <label>Lý do *</label>
                            <textarea v-model="createForm.reason" class="form-control" rows="3" placeholder="Mô tả lý do cần can thiệp..."></textarea>
                        </div>
                        <div class="form-group">
                            <label>Ghi chú thêm</label>
                            <textarea v-model="createForm.note" class="form-control" rows="2" placeholder="Ghi chú bổ sung..."></textarea>
                        </div>
                        <div v-if="createError" class="alert alert-danger">{{ createError }}</div>
                        <div v-if="createSuccess" class="alert alert-success">{{ createSuccess }}</div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="showCreateModal = false">Hủy</button>
                        <button class="btn btn-primary" :disabled="creating || !createForm.reason || !createForm.interventionType" @click="submitInterventionRequest">
                            {{ creating ? 'Đang gửi...' : 'Gửi yêu cầu' }}
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
import EnterpriseDataTable from '../components/shared/EnterpriseDataTable.vue'

const loading = ref(false)
const saving = ref(false)
const loadingEvidence = ref(false)
const loadingBarriers = ref(false)
const loadingCorrelations = ref(false)
const searchQuery = ref('')
const activeTab = ref('cases')
const activeCategory = ref('all')
const selectedCase = ref(null)
const detailTab = ref('events')
const isUsingDemoData = ref(false)

// Intervention (Phase G) state
const loadingInterventions = ref(false)
const savingIntervention = ref(false)
const interventionRequests = ref([])
const selectedIntervention = ref(null)
const interventionStatusFilter = ref('all')
const showCreateModal = ref(false)
const creating = ref(false)
const createError = ref('')
const createSuccess = ref('')
const interventionReviewNote = ref('')
const interventionActionMessage = ref('')
const interventionActionError = ref(false)
const createForm = reactive({
    interventionType: 'temporary_grant',
    subjectName: '',
    plateNumber: '',
    laneName: '',
    priority: 'medium',
    reason: '',
    note: '',
})
const interventionColumns = [
    { key: 'subjectName', label: 'Đối tượng' },
    { key: 'priority', label: 'Ưu tiên', width: '92px' },
    { key: 'status', label: 'Trạng thái', width: '118px' },
]

// All cases from API
const allCases = ref([])
// Related data
const laneEvents = ref([])
const evidenceItems = ref([])
const barrierCommands = ref([])
const correlations = ref([])

const currentRole = computed(() => authState.user?.role || 'BaoVe')
const isQuanLy = computed(() => currentRole.value === 'QuanLy')
const isAdmin = computed(() => currentRole.value === 'Admin')
const canReviewInterventions = computed(() => isAdmin.value || isQuanLy.value)

const caseCategories = computed(() => {
    const categories = [
        { id: 'all', label: 'Tất cả', count: allCases.value.length },
        { id: 'data_mismatch', label: 'Lệch dữ liệu', count: countByCategory('data_mismatch') },
        { id: 'manual_override', label: 'Override thủ công', count: countByCategory('manual_override') },
        { id: 'device_degraded', label: 'Thiết bị lỗi', count: countByCategory('device_degraded') },
        { id: 'emergency_pass', label: 'Khẩn cấp', count: countByCategory('emergency_pass') },
        { id: 'duress', label: 'Duress', count: countByCategory('duress') },
        { id: 'pending_approval', label: 'Chờ duyệt', count: countByCategory('pending_approval') },
    ]
    // Role-based filtering: QuanLy only sees cases they can act on
    if (!isAdmin.value && !isQuanLy.value) {
        return categories.filter(c => ['all', 'data_mismatch', 'manual_override', 'duress'].includes(c.id))
    }
    return categories
})

const filteredCases = computed(() => {
    let items = allCases.value
    if (activeCategory.value !== 'all') {
        items = items.filter(c => c.category === activeCategory.value)
    }
    if (searchQuery.value) {
        const q = searchQuery.value.toLowerCase()
        items = items.filter(c =>
            (c.subjectName || '').toLowerCase().includes(q) ||
            (c.plateText || '').toLowerCase().includes(q) ||
            String(c.id).includes(q)
        )
    }
    // Sort by severity (critical first) then by time (newest first)
    const severityOrder = { critical: 0, high: 1, medium: 2, low: 3 }
    return [...items].sort((a, b) => {
        const sA = severityOrder[a.severity] || 99
        const sB = severityOrder[b.severity] || 99
        if (sA !== sB) return sA - sB
        return new Date(b.lastEventAt || 0) - new Date(a.lastEventAt || 0)
    })
})

const totalPending = computed(() =>
    allCases.value.filter(c => ['pending_approval', 'duress', 'emergency_pass'].includes(c.category)).length
)

const caseTimeline = computed(() => {
    if (!selectedCase.value?.events) return []
    return selectedCase.value.events.map(e => ({
        id: e.id,
        type: e.type || 'system',
        title: e.title || '',
        description: e.description || '',
        timestamp: e.timestamp || e.occurredAtUtc,
        actor: e.actor || '',
        reason: e.reason || '',
    }))
})

function countByCategory(cat) {
    return allCases.value.filter(c => c.category === cat).length
}

function categoryLabel(cat) {
    const map = {
        data_mismatch: 'Lệch dữ liệu',
        manual_override: 'Override thủ công',
        device_degraded: 'Thiết bị lỗi',
        emergency_pass: 'Khẩn cấp',
        duress: 'Duress',
        pending_approval: 'Chờ duyệt',
    }
    return map[cat] || cat
}

function severityLabel(sev) {
    const map = { critical: 'Nghiêm trọng', high: 'Cao', medium: 'Trung bình', low: 'Thấp' }
    return map[sev] || sev
}

// ===================== ESCALATION COMPUTED =====================

const interventionPendingCount = computed(() =>
    interventionRequests.value.filter(r => r.status === 'Pending').length
)

const statusFilters = computed(() => {
    const all = interventionRequests.value
    const counts = {}
    all.forEach(r => {
        const s = r.status || 'Pending'
        counts[s] = (counts[s] || 0) + 1
    })
    return [
        { value: 'all', label: 'Tất cả', count: all.length },
        { value: 'Pending', label: 'Chờ xử lý', count: counts['Pending'] || 0 },
        { value: 'Accepted', label: 'Đã chấp nhận', count: counts['Accepted'] || 0 },
        { value: 'Rejected', label: 'Đã từ chối', count: counts['Rejected'] || 0 },
        { value: 'Executed', label: 'Đã thực thi', count: counts['Executed'] || 0 },
        { value: 'Expired', label: 'Hết hạn', count: counts['Expired'] || 0 },
    ]
})

const filteredInterventions = computed(() => {
    let items = interventionRequests.value
    if (interventionStatusFilter.value !== 'all') {
        items = items.filter(r => r.status === interventionStatusFilter.value)
    }
    // Sort by priority then by creation time (oldest first for pending)
    const priorityOrder = { critical: 0, high: 1, medium: 2, low: 3 }
    return [...items].sort((a, b) => {
        const pA = priorityOrder[a.priority] || 99
        const pB = priorityOrder[b.priority] || 99
        if (pA !== pB) return pA - pB
        return new Date(a.createdAtUtc || 0) - new Date(b.createdAtUtc || 0)
    })
})

function severityForPriority(priority) {
    const map = { critical: 'critical', high: 'high', medium: 'medium', low: 'low' }
    return map[priority] || 'medium'
}

function interventionTypeLabel(type) {
    const map = {
        temporary_grant: 'Temporary Grant',
        anti_passback_reset: 'Anti-passback Reset',
        emergency_override: 'Emergency Override',
        policy_override: 'Policy Override',
        device_override: 'Device Override',
        other: 'Khác',
    }
    return map[type] || type
}

function priorityLabel(priority) {
    const map = { critical: 'Nghiêm trọng', high: 'Cao', medium: 'Trung bình', low: 'Thấp' }
    return map[priority] || priority
}

function statusLabel(status) {
    const map = { Pending: 'Chờ xử lý', Accepted: 'Đã chấp nhận', Rejected: 'Đã từ chối', Executed: 'Đã thực thi', Expired: 'Hết hạn' }
    return map[status] || status
}

function buildInterventionTimeline(r) {
    const items = []
    items.push({
        id: 'created',
        type: 'system',
        title: 'Yêu cầu được tạo',
        description: r.reason,
        timestamp: r.createdAtUtc,
        actor: `User #${r.requestedByUserId}`,
    })
    if (r.acceptedAtUtc) {
        items.push({
            id: 'accepted',
            type: 'approve',
            title: 'Yêu cầu được chấp nhận',
            timestamp: r.acceptedAtUtc,
            actor: `User #${r.acceptedByUserId}`,
            description: 'Admin xác nhận yêu cầu',
        })
    }
    if (r.rejectedAtUtc) {
        items.push({
            id: 'rejected',
            type: 'reject',
            title: 'Yêu cầu bị từ chối',
            timestamp: r.rejectedAtUtc,
            actor: `User #${r.rejectedByUserId}`,
            description: r.rejectionReason || '',
        })
    }
    if (r.executedAtUtc) {
        items.push({
            id: 'executed',
            type: 'success',
            title: 'Yêu cầu được thực thi',
            timestamp: r.executedAtUtc,
            actor: `User #${r.executedByUserId}`,
        })
    }
    return items
}

function switchToEscalations() {
    activeTab.value = 'escalation'
    loadInterventions()
}

function eventChipClass(type) {
    if (!type) return ''
    const t = String(type).toUpperCase()
    if (t.includes('GRANTED') || t.includes('ALLOW') || t.includes('OPEN')) return 'success'
    if (t.includes('DENIED') || t.includes('DENY') || t.includes('LOCK')) return 'danger'
    if (t.includes('DURESS')) return 'danger'
    if (t.includes('MANUAL') || t.includes('OVERRIDE') || t.includes('ESCALATION')) return 'warn'
    return 'muted'
}

function canApprove(c) {
    if (isAdmin.value) return c.category === 'pending_approval' || c.category === 'emergency_pass'
    if (isQuanLy.value) return c.category === 'pending_approval'
    return false
}

function canClose(c) {
    return isAdmin.value || isQuanLy.value
}

function canEscalate(c) {
    return isQuanLy.value || (currentRole.value === 'BaoVe' && c.category !== 'pending_approval')
}

function formatDateTime(value) {
    if (!value) return '---'
    return new Date(value).toLocaleString('vi-VN')
}

function formatTime(value) {
    if (!value) return ''
    return new Date(value).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
}

function formatRelativeTime(value) {
    if (!value) return ''
    const diff = Date.now() - new Date(value).getTime()
    const mins = Math.floor(diff / 60000)
    if (mins < 1) return 'Vừa xong'
    if (mins < 60) return `${mins} phút`
    const hours = Math.floor(mins / 60)
    if (hours < 24) return `${hours} giờ`
    return `${Math.floor(hours / 24)} ngày`
}

function buildCaseFromException(ex) {
    // Classify the exception into a category
    let category = 'data_mismatch'
    let severity = 'medium'

    const reason = String(ex.exceptionReasonCode || '').toUpperCase()
    const status = String(ex.resultStatus || '').toUpperCase()
    const note = String(ex.note || '').toLowerCase()

    if (reason.includes('DURESS') || status.includes('DURESS')) {
        category = 'duress'
        severity = 'critical'
    } else if (reason.includes('EMERGENCY') || status.includes('EMERGENCY') || note.includes('khẩn cấp')) {
        category = 'emergency_pass'
        severity = 'critical'
    } else if (reason.includes('OVERRIDE') || status.includes('OVERRIDE') || note.includes('override') || note.includes('chịu trách nhiệm')) {
        category = 'manual_override'
        severity = 'high'
    } else if (reason.includes('DEVICE') || reason.includes('DEGRADED') || note.includes('lỗi') || note.includes('degraded')) {
        category = 'device_degraded'
        severity = 'high'
    } else if (status.includes('PENDING') || note.includes('chờ') || note.includes('chưa')) {
        category = 'pending_approval'
        severity = 'medium'
    }

    return {
        id: ex.logId || `case-${Date.now()}-${Math.random().toString(36).slice(2, 6)}`,
        category,
        severity,
        subjectName: ex.actorName || 'Unknown',
        plateText: ex.capturedLicensePlate || '',
        lastEventAt: ex.timestamp,
        laneName: ex.gateName || '',
        gateName: ex.gateName || '',
        actorName: ex.actorName || '',
        reason: ex.exceptionReasonDescription || '',
        note: ex.note || '',
        status: ex.resultStatus || '',
        pendingDuration: '',
        events: [
            {
                id: `${ex.logId}-event`,
                type: ex.isBypass ? 'override' : 'system',
                title: ex.exceptionReasonDescription || 'Sự kiện ngoại lệ',
                description: ex.note || '',
                timestamp: ex.timestamp,
                actor: ex.actorName || '',
                reason: ex.exceptionReasonDescription || '',
            }
        ],
    }
}

async function loadInterventions() {
    loadingInterventions.value = true
    try {
        const res = await enterpriseApi.getInterventionRequests({ pageSize: 100 })
        interventionRequests.value = res.data?.items || []
    } catch (e) {
        console.error('Failed to load interventions:', e)
        interventionRequests.value = []
    } finally {
        loadingInterventions.value = false
    }
}

async function selectIntervention(r) {
    selectedIntervention.value = r
    interventionReviewNote.value = ''
    interventionActionMessage.value = ''
}

async function submitInterventionRequest() {
    if (!createForm.reason || !createForm.interventionType) return
    creating.value = true
    createError.value = ''
    createSuccess.value = ''
    try {
        const payload = {
            interventionType: createForm.interventionType,
            reason: createForm.reason,
            subjectName: createForm.subjectName || undefined,
            plateNumber: createForm.plateNumber || undefined,
            laneName: createForm.laneName || undefined,
            priority: createForm.priority,
            note: createForm.note || undefined,
            expiresInMinutes: 240,
        }
        const res = await enterpriseApi.createInterventionRequest(payload)
        createSuccess.value = `Yêu cầu #${res.data?.operationalInterventionRequestId} đã được gửi!`
        // Reset form
        createForm.reason = ''
        createForm.note = ''
        createForm.subjectName = ''
        createForm.plateNumber = ''
        createForm.laneName = ''
        createForm.interventionType = 'temporary_grant'
        createForm.priority = 'medium'
        // Reload list
        await loadInterventions()
    } catch (e) {
        createError.value = e?.response?.data?.message || e?.message || 'Gửi yêu cầu thất bại.'
    } finally {
        creating.value = false
    }
}

async function acceptIntervention(r) {
    savingIntervention.value = true
    interventionActionMessage.value = ''
    try {
        await enterpriseApi.acceptInterventionRequest(r.operationalInterventionRequestId, { note: interventionReviewNote.value || undefined })
        r.status = 'Accepted'
        r.acceptedAtUtc = new Date().toISOString()
        r.acceptedByUserId = authState.user?.userId
        interventionActionError.value = false
        interventionActionMessage.value = `Yêu cầu #${r.operationalInterventionRequestId} đã được chấp nhận.`
    } catch (e) {
        interventionActionError.value = true
        interventionActionMessage.value = e?.response?.data?.message || 'Chấp nhận thất bại.'
    } finally {
        savingIntervention.value = false
    }
}

async function rejectIntervention(r) {
    const reason = interventionReviewNote.value
    if (!reason) return
    savingIntervention.value = true
    interventionActionMessage.value = ''
    try {
        const res = await enterpriseApi.rejectInterventionRequest(r.operationalInterventionRequestId, { note: reason })
        r.status = 'Rejected'
        r.rejectedAtUtc = new Date().toISOString()
        r.rejectedByUserId = authState.user?.userId
        r.rejectionReason = reason
        interventionActionError.value = false
        interventionActionMessage.value = `Yêu cầu #${r.operationalInterventionRequestId} đã bị từ chối.`
    } catch (e) {
        interventionActionError.value = true
        interventionActionMessage.value = e?.response?.data?.message || 'Từ chối thất bại.'
    } finally {
        savingIntervention.value = false
    }
}

async function executeIntervention(r) {
    savingIntervention.value = true
    interventionActionMessage.value = ''
    try {
        await enterpriseApi.executeInterventionRequest(r.operationalInterventionRequestId, { note: interventionReviewNote.value || 'Admin thực thi yêu cầu đã duyệt' })
        r.status = 'Executed'
        r.executedAtUtc = new Date().toISOString()
        r.executedByUserId = authState.user?.userId
        interventionActionError.value = false
        interventionActionMessage.value = `Yêu cầu #${r.operationalInterventionRequestId} đã được thực thi và tạo hiệu lực thực tế.`
    } catch (e) {
        interventionActionError.value = true
        interventionActionMessage.value = e?.response?.data?.message || 'Thực thi thất bại.'
    } finally {
        savingIntervention.value = false
    }
}

async function loadAll() {
    loading.value = true
    try {
        // Load exceptions from existing API
        const exRes = await getExceptions({ pageSize: 100 })
        const exceptions = exRes.data?.items || []

        // Transform into cases
        allCases.value = exceptions.map(buildCaseFromException)

        // If no real data, create demo cases for UI testing
        if (allCases.value.length === 0) {
            allCases.value = generateDemoCases()
            isUsingDemoData.value = true
        } else {
            isUsingDemoData.value = false
        }

        // Auto-select first case if none selected
        if (!selectedCase.value && allCases.value.length > 0) {
            selectCase(allCases.value[0])
        }

        // Also load intervention requests in background
        loadInterventions()
    } catch (e) {
        console.error('Failed to load cases:', e)
        // Fallback to demo data
        allCases.value = generateDemoCases()
    } finally {
        loading.value = false
    }
}

function generateDemoCases() {
    const now = Date.now()
    return [
        { id: 'DEMO-001', category: 'data_mismatch', severity: 'high', subjectName: 'Nguyễn Văn An', plateText: '51F-888.88', lastEventAt: new Date(now - 60000).toISOString(), laneName: 'Làn 1', gateName: 'Cổng A', actorName: 'Bảo vệ Tuấn', reason: 'Database không khớp', status: 'Pending', note: 'QR hợp lệ nhưng database không có thông tin xe', events: [{ id: 'e1', type: 'scan', title: 'Quét QR thành công', timestamp: new Date(now - 120000).toISOString() }, { id: 'e2', type: 'system', title: 'Database mismatch detected', timestamp: new Date(now - 60000).toISOString() }] },
        { id: 'DEMO-002', category: 'manual_override', severity: 'high', subjectName: 'Trần Thị Bình', plateText: '51G-123.45', lastEventAt: new Date(now - 300000).toISOString(), laneName: 'Làn 2', gateName: 'Cổng B', actorName: 'Bảo vệ Minh', reason: 'Override - khách quên QR', status: 'Closed', note: 'Đã cho qua có chịu trách nhiệm', events: [{ id: 'e3', type: 'override', title: 'Override có trách nhiệm', timestamp: new Date(now - 300000).toISOString(), actor: 'Bảo vệ Minh', reason: 'Khách quên QR' }] },
        { id: 'DEMO-003', category: 'duress', severity: 'critical', subjectName: 'Lê Văn Cường', plateText: '51H-456.78', lastEventAt: new Date(now - 900000).toISOString(), laneName: 'Làn 1', gateName: 'Cổng A', actorName: 'Bảo vệ Tuấn', reason: 'Tín hiệu duress', status: 'Unacknowledged', note: 'Bảo vệ đã kích hoạt duress', events: [{ id: 'e4', type: 'duress', title: 'Duress activated', timestamp: new Date(now - 900000).toISOString(), actor: 'Bảo vệ Tuấn', reason: 'Cảm thấy bị đe dọa' }] },
        { id: 'DEMO-004', category: 'pending_approval', severity: 'medium', subjectName: 'Phạm Thị Dung', plateText: '51K-789.01', lastEventAt: new Date(now - 1800000).toISOString(), laneName: 'Làn 2', gateName: 'Cổng B', actorName: 'Bảo vệ Minh', reason: 'Xe không đăng ký trước', status: 'Pending', note: 'Cần quản lý phê duyệt', events: [{ id: 'e5', type: 'escalate', title: 'Yêu cầu phê duyệt', timestamp: new Date(now - 1800000).toISOString(), actor: 'Bảo vệ Minh', reason: 'Xe không đăng ký trước' }] },
        { id: 'DEMO-005', category: 'device_degraded', severity: 'critical', subjectName: 'Hệ thống', plateText: '', lastEventAt: new Date(now - 3600000).toISOString(), laneName: 'Làn 1', gateName: 'Cổng A', actorName: 'Hệ thống', reason: 'Camera QR offline', status: 'Degraded', note: 'Camera QR không phản hồi trong 5 phút', events: [{ id: 'e6', type: 'system', title: 'Device degraded: QR Camera offline', timestamp: new Date(now - 3600000).toISOString() }] },
        { id: 'DEMO-006', category: 'emergency_pass', severity: 'critical', subjectName: 'Nguyễn Văn Khẩn', plateText: '51L-234.56', lastEventAt: new Date(now - 7200000).toISOString(), laneName: 'Làn 1', gateName: 'Cổng A', actorName: 'Admin', reason: 'Cấp quyền khẩn cấp', status: 'Resolved', note: 'Đã cấp temporary grant 24h', events: [{ id: 'e7', type: 'approve', title: 'Emergency grant issued', timestamp: new Date(now - 7200000).toISOString(), actor: 'Admin' }] },
    ]
}

function selectCase(c) {
    selectedCase.value = c
    detailTab.value = 'events'
    loadLaneEvents(c)
}

async function loadLaneEvents(c) {
    if (!c || !c.plateText) { laneEvents.value = []; return }
    try {
        const res = await enterpriseApi.getLaneEvents({ plateText: c.plateText, pageSize: 20 })
        laneEvents.value = res.data?.items || []
    } catch {
        laneEvents.value = []
    }
}

async function loadEvidence() {
    if (!selectedCase.value) return
    loadingEvidence.value = true
    try {
        const res = await enterpriseApi.getEvidenceItems({ query: selectedCase.value.plateText || selectedCase.value.subjectName, pageSize: 10 })
        evidenceItems.value = res.data?.items || []
    } catch {
        evidenceItems.value = []
    } finally {
        loadingEvidence.value = false
    }
}

async function loadBarrierCommands() {
    if (!selectedCase.value) return
    loadingBarriers.value = true
    try {
        const res = await enterpriseApi.getBarrierCommands(null, { pageSize: 20 })
        barrierCommands.value = res.data?.items || []
    } catch {
        barrierCommands.value = []
    } finally {
        loadingBarriers.value = false
    }
}

async function loadCorrelations() {
    if (!selectedCase.value) return
    loadingCorrelations.value = true
    try {
        const res = await enterpriseApi.getCorrelations({ query: selectedCase.value.plateText || selectedCase.value.subjectName, pageSize: 10 })
        correlations.value = res.data?.items || []
    } catch {
        correlations.value = []
    } finally {
        loadingCorrelations.value = false
    }
}

async function approveCase(c) {
    saving.value = true
    try {
        await enterpriseApi.recordLaneEvent({
            laneId: c.laneName || 'unknown',
            eventType: 'CASE_APPROVED',
            note: `Case #${c.id} approved by ${currentRole.value}`,
        })
        c.status = 'Approved'
        c.events.push({
            id: `approve-${Date.now()}`,
            type: 'approve',
            title: 'Case approved',
            timestamp: new Date().toISOString(),
            actor: currentRole.value,
            reason: 'Approved from exception queue',
        })
        alert(`Case #${c.id} đã được phê duyệt.`)
    } catch (e) {
        alert(e?.response?.data?.message || 'Phê duyệt thất bại.')
    } finally {
        saving.value = false
    }
}

async function closeCase(c) {
    saving.value = true
    try {
        await enterpriseApi.recordLaneEvent({
            laneId: c.laneName || 'unknown',
            eventType: 'CASE_CLOSED',
            note: `Case #${c.id} closed by ${currentRole.value}`,
        })
        c.status = 'Closed'
        c.events.push({
            id: `close-${Date.now()}`,
            type: 'close',
            title: 'Case closed',
            timestamp: new Date().toISOString(),
            actor: currentRole.value,
            reason: 'Closed from exception queue',
        })
        alert(`Case #${c.id} đã được đóng.`)
    } catch (e) {
        alert(e?.response?.data?.message || 'Đóng case thất bại.')
    } finally {
        saving.value = false
    }
}

async function escalateCase(c) {
    saving.value = true
    try {
        await enterpriseApi.recordLaneEvent({
            laneId: c.laneName || 'unknown',
            eventType: 'ESCALATION_TO_ADMIN',
            note: `Case #${c.id} escalated to Admin by ${currentRole.value}`,
        })
        c.status = 'Escalated'
        c.events.push({
            id: `esc-${Date.now()}`,
            type: 'escalate',
            title: 'Escalated to Admin',
            timestamp: new Date().toISOString(),
            actor: currentRole.value,
            reason: 'Escalated from exception queue',
        })
        alert(`Case #${c.id} đã được chuyển lên Admin.`)
    } catch (e) {
        alert(e?.response?.data?.message || 'Chuyển case thất bại.')
    } finally {
        saving.value = false
    }
}

function viewEvidence(ev) {
    alert(`View evidence: ${ev.fileName || ev.evidenceItemId}`)
}

let searchTimer = null
function onSearchInput() {
    clearTimeout(searchTimer)
    searchTimer = setTimeout(() => {}, 300)
}

onMounted(loadAll)
</script>

<style scoped>
.table-subline { display: block; margin-top: 3px; color: #64748b; font-weight: 500; }
.exception-layout {
    display: grid;
    grid-template-columns: 380px 1fr;
    gap: 16px;
    min-height: 500px;
}
.exception-queue {
    background: var(--surface, #ffffff);
    border: 1px solid var(--border-soft, #e9eef5);
    border-radius: 14px;
    padding: 14px;
    overflow-y: auto;
    max-height: calc(100vh - 240px);
}
.exception-detail {
    background: var(--surface, #ffffff);
    border: 1px solid var(--border-soft, #e9eef5);
    border-radius: 14px;
    padding: 16px;
    overflow-y: auto;
    max-height: calc(100vh - 240px);
}
.case-list {
    display: flex;
    flex-direction: column;
    gap: 6px;
    margin-top: 8px;
}
.case-row {
    padding: 10px 12px;
    border-radius: 10px;
    border: 1px solid var(--border-soft, #e9eef5);
    background: var(--surface, #fff);
    cursor: pointer;
    transition: all 0.12s ease;
}
.case-row:hover {
    border-color: #94a3b8;
    box-shadow: 0 2px 8px rgba(15,23,42,0.04);
}
.case-row--selected {
    border-color: #3b82f6;
    background: #eff6ff;
    box-shadow: 0 0 0 2px rgba(59,130,246,0.12);
}
.case-row--critical { border-left: 3px solid #ef4444; }
.case-row--high { border-left: 3px solid #f97316; }
.case-row--medium { border-left: 3px solid #eab308; }
.case-row--low { border-left: 3px solid #94a3b8; }
.case-row-head {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 4px;
}
.case-row-body {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 4px;
}
.case-subject {
    font-weight: 700;
    font-size: 14px;
    color: #0f172a;
}
.case-plate {
    font-family: monospace;
    font-size: 12px;
    color: #15803d;
    background: #dcfce7;
    padding: 1px 6px;
    border-radius: 4px;
}
.case-row-footer {
    display: flex;
    justify-content: space-between;
    align-items: center;
}
.case-category-badge {
    padding: 2px 8px;
    border-radius: 999px;
    font-size: 11px;
    font-weight: 700;
}
.badge--data_mismatch { background: #fef3c7; color: #92400e; }
.badge--manual_override { background: #fff7ed; color: #c2410c; }
.badge--device_degraded { background: #fee2e2; color: #991b1b; }
.badge--emergency_pass { background: #fce7f3; color: #9d174d; }
.badge--duress { background: #fce7f3; color: #9d174d; }
.badge--pending_approval { background: #dbeafe; color: #1e40af; }
.case-severity {
    font-size: 11px;
    font-weight: 700;
}
.severity--critical { color: #ef4444; }
.severity--high { color: #f97316; }
.severity--medium { color: #eab308; }
.severity--low { color: #94a3b8; }
.case-time { font-size: 11px; color: #94a3b8; }
.case-duration { font-size: 11px; color: #94a3b8; }
.detail-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: 12px;
    margin-bottom: 14px;
}
.detail-title {
    margin: 4px 0 0;
    font-size: 22px;
    font-weight: 800;
}
.detail-actions {
    display: flex;
    gap: 8px;
    flex-shrink: 0;
}
.detail-meta-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 8px;
    margin-bottom: 16px;
    padding: 12px;
    background: #f8fafc;
    border-radius: 10px;
    border: 1px solid #e9eef5;
}
.detail-meta-item {
    display: flex;
    flex-direction: column;
    gap: 2px;
}
.meta-label {
    font-size: 11px;
    font-weight: 600;
    color: #64748b;
    text-transform: uppercase;
}
.detail-section {
    margin-bottom: 16px;
}
.detail-section-title {
    font-size: 15px;
    font-weight: 800;
    margin: 0 0 8px;
    color: #0f172a;
}
.detail-tabs {
    display: flex;
    gap: 4px;
    margin-bottom: 8px;
    border-bottom: 1px solid #e2e8f0;
}
.detail-tabs button {
    padding: 6px 14px;
    border: none;
    background: none;
    font-size: 13px;
    font-weight: 600;
    color: #64748b;
    cursor: pointer;
    border-bottom: 2px solid transparent;
    margin-bottom: -1px;
}
.detail-tabs button.active {
    color: #2563eb;
    border-bottom-color: #2563eb;
}
.detail-tab-content {
    min-height: 60px;
}
.compact-list {
    display: flex;
    flex-direction: column;
    gap: 4px;
}
.compact-row {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 6px 8px;
    border-radius: 6px;
    font-size: 13px;
    background: #f8fafc;
}
.compact-time {
    font-size: 11px;
    color: #94a3b8;
    min-width: 60px;
    font-family: monospace;
}
.tab-count {
    margin-left: 6px;
    background: rgba(100,116,139,0.15);
    padding: 1px 7px;
    border-radius: 999px;
    font-size: 11px;
    font-weight: 600;
}
.plate-badge {
    font-family: monospace;
    font-size: 13px;
    color: #15803d;
    background: #dcfce7;
    padding: 2px 8px;
    border-radius: 4px;
}
.text-muted { color: #94a3b8; font-size: 13px; }
.panel-kicker {
    display: inline-flex;
    padding: 3px 10px;
    border-radius: 999px;
    background: rgba(15,124,130,0.08);
    color: #0f7c82;
    font-size: 11px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.06em;
}
.page-subtitle {
    margin: 4px 0 0;
    font-size: 14px;
    color: #64748b;
}

/* ===================== ESCALATION CSS ===================== */
.sub-tabs {
    margin-top: -8px;
}

.filter-pills {
    display: flex;
    flex-wrap: wrap;
    gap: 4px;
    margin: 8px 0;
}
.pill-btn {
    padding: 4px 10px;
    border-radius: 999px;
    border: 1px solid #e2e8f0;
    background: #fff;
    font-size: 11px;
    font-weight: 600;
    color: #64748b;
    cursor: pointer;
    transition: all 0.12s ease;
}
.pill-btn:hover {
    border-color: #94a3b8;
    background: #f8fafc;
}
.pill-btn.active {
    border-color: #3b82f6;
    background: #eff6ff;
    color: #2563eb;
}

.badge--intv-temporary_grant { background: #dbeafe; color: #1e40af; }
.badge--intv-anti_passback_reset { background: #ede9fe; color: #5b21b6; }
.badge--intv-emergency_override { background: #fce7f3; color: #9d174d; }
.badge--intv-policy_override { background: #fff7ed; color: #c2410c; }
.badge--intv-device_override { background: #fef3c7; color: #92400e; }
.badge--intv-other { background: #f1f5f9; color: #475569; }

.intv-status--pending { background: #fef3c7; color: #92400e; }
.intv-status--accepted { background: #dbeafe; color: #1e40af; }
.intv-status--rejected { background: #fee2e2; color: #991b1b; }
.intv-status--executed { background: #dcfce7; color: #15803d; }
.intv-status--expired { background: #f1f5f9; color: #64748b; }

.reason-box {
    padding: 12px;
    background: #f8fafc;
    border-radius: 8px;
    border: 1px solid #e2e8f0;
    font-size: 14px;
    line-height: 1.6;
    color: #334155;
}
.note-box {
    margin-top: 8px;
    padding: 8px 12px;
    background: #fffbeb;
    border-radius: 8px;
    border: 1px solid #fde68a;
    font-size: 13px;
    color: #92400e;
}

.tab-count.danger {
    background: #fee2e2;
    color: #dc2626;
}

@media (max-width: 1024px) {
    .exception-layout {
        grid-template-columns: 1fr;
    }
    .exception-queue {
        max-height: none;
    }
    .exception-detail {
        max-height: none;
    }
    .detail-meta-grid {
        grid-template-columns: repeat(2, 1fr);
    }
}
</style>
