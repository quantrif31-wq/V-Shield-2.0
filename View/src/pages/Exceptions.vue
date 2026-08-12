<template>
    <div class="page-container ops-page animate-in">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Quản lý ngoại lệ</span>
                <h1 class="page-title">Trung tâm ngoại lệ</h1>
                <p class="page-subtitle">Hàng đợi case cần đối soát, xin can thiệp và khóa sổ thao tác.</p>
            </div>
            <div class="header-actions">
                <span class="soft-chip warn">{{ openCaseCount }} case mở</span>
                <span class="soft-chip danger">{{ pendingInterventionCount }} yêu cầu chờ xử lý</span>
                <button class="btn btn-secondary btn-sm" :disabled="loading" @click="loadAll">Làm mới</button>
            </div>
        </div>

        <section class="summary-strip">
            <article class="summary-card">
                <span class="summary-label">Case đang mở</span>
                <strong class="summary-value">{{ openCaseCount }}</strong>
                <small>{{ categoryCount('pending_approval') }} chờ phê duyệt</small>
            </article>
            <article class="summary-card">
                <span class="summary-label">Ghi đè / bypass</span>
                <strong class="summary-value">{{ categoryCount('manual_override') }}</strong>
                <small>{{ categoryCount('data_mismatch') }} lệch dữ liệu</small>
            </article>
            <article class="summary-card">
                <span class="summary-label">Thiết bị / ép buộc</span>
                <strong class="summary-value">{{ categoryCount('device_degraded') + categoryCount('duress') }}</strong>
                <small>{{ categoryCount('emergency_pass') }} khẩn cấp</small>
            </article>
            <article class="summary-card">
                <span class="summary-label">Workflow can thiệp</span>
                <strong class="summary-value">{{ interventionRequests.length }}</strong>
                <small>{{ executedInterventionCount }} đã thực thi</small>
            </article>
        </section>

        <div class="tab-bar">
            <button :class="{ active: activeTab === 'cases' }" @click="activeTab = 'cases'">
                Case ngoại lệ
                <span v-if="exceptionCases.length" class="tab-count">{{ exceptionCases.length }}</span>
            </button>
            <button :class="{ active: activeTab === 'interventions' }" @click="activeTab = 'interventions'">
                Hàng đợi can thiệp
                <span v-if="pendingInterventionCount" class="tab-count danger">{{ pendingInterventionCount }}</span>
            </button>
        </div>

        <div v-if="activeTab === 'cases'" class="workspace-grid">
            <section class="workspace-pane queue-pane">
                <div class="pane-header">
                    <div>
                        <h2 class="panel-title">Danh sách case</h2>
                        <p class="pane-subtitle">Tập trung vào case mới nhất và có nguy cơ thao tác.</p>
                    </div>
                    <div class="search-shell">
                        <input v-model.trim="searchQuery" type="text" placeholder="Tìm theo tên, biển số, log..." class="filter-input" />
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

                <div v-if="loading" class="empty-card">Đang tải danh sách case...</div>
                <div v-else-if="filteredCases.length === 0" class="empty-card">Không có case phù hợp bộ lọc hiện tại.</div>
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
                        <strong class="queue-title">{{ item.subjectName || item.plateText || `Nhật ký #${item.sourceLogId}` }}</strong>
                        <div class="queue-meta">
                            <span v-if="item.plateText" class="plate-badge">{{ item.plateText }}</span>
                            <span>{{ item.gateName || 'Chưa gắn cổng' }}</span>
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
                <div v-if="!selectedCase" class="empty-card">Chọn một case để xem chi tiết và thao tác.</div>
                <template v-else>
                    <div class="detail-header">
                        <div>
                            <span class="panel-kicker">Case #{{ selectedCase.id }}</span>
                            <h2 class="detail-title">{{ selectedCase.subjectName || 'Đối tượng chưa rõ' }}</h2>
                            <p class="detail-subtitle">{{ selectedCase.reason || 'Chưa mô tả lý do' }}</p>
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
                                Khóa sổ case
                            </button>
                        </div>
                    </div>

                    <div v-if="caseActionMessage" class="alert" :class="caseActionError ? 'alert-danger' : 'alert-success'">
                        {{ caseActionMessage }}
                    </div>

                    <div class="meta-grid">
                        <div class="meta-item">
                            <span class="meta-label">Danh mục</span>
                            <span class="case-badge" :class="`badge-${selectedCase.category}`">{{ categoryLabel(selectedCase.category) }}</span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Mức độ</span>
                            <span class="severity-badge" :class="`severity-${selectedCase.severity}`">{{ severityLabel(selectedCase.severity) }}</span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Trạng thái log</span>
                            <span>{{ selectedCase.resultStatus || '---' }}</span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Biển số</span>
                            <span>{{ selectedCase.plateText || '---' }}</span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Cổng</span>
                            <span>{{ selectedCase.gateName || '---' }}</span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Thời gian</span>
                            <span>{{ formatDateTime(selectedCase.lastEventAt) }}</span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Nguồn</span>
                            <span>{{ methodLabel(selectedCase.method) }}</span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Mã lý do</span>
                            <span>{{ selectedCase.reasonCode || 'CHƯA PHÂN LOẠI' }}</span>
                        </div>
                    </div>

                    <div class="detail-tabs">
                        <button :class="{ active: detailTab === 'timeline' }" @click="detailTab = 'timeline'">Dòng thời gian</button>
                        <button :class="{ active: detailTab === 'events' }" @click="detailTab = 'events'; loadLaneEvents(selectedCase)">Sự kiện làn</button>
                        <button :class="{ active: detailTab === 'evidence' }" @click="detailTab = 'evidence'; loadEvidence(selectedCase)">Chứng cứ</button>
                        <button :class="{ active: detailTab === 'barriers' }" @click="detailTab = 'barriers'; loadBarrierCommands(selectedCase)">Barrier</button>
                        <button :class="{ active: detailTab === 'correlations' }" @click="detailTab = 'correlations'; loadCorrelations(selectedCase)">Tương quan</button>
                    </div>

                    <div v-if="detailTab === 'timeline'" class="detail-body">
                        <ExceptionCaseTimeline :items="selectedCase.timeline" />
                    </div>

                    <div v-else-if="detailTab === 'events'" class="detail-body">
                        <div v-if="loadingLaneEvents" class="empty-card compact">Đang tải sự kiện làn...</div>
                        <div v-else-if="laneEvents.length === 0" class="empty-card compact">Không có sự kiện làn liên quan từ biển số hiện tại.</div>
                        <div v-else class="compact-list">
                            <div v-for="event in laneEvents" :key="event.laneEventId" class="compact-row">
                                <span class="compact-time">{{ formatDateTime(event.occurredAtUtc) }}</span>
                                <span class="soft-chip">{{ event.eventType }}</span>
                                <span>{{ event.lane?.name || selectedCase.resolvedLaneName || 'Làn chưa rõ' }}</span>
                                <span class="text-muted">{{ event.note || '' }}</span>
                            </div>
                        </div>
                    </div>

                    <div v-else-if="detailTab === 'evidence'" class="detail-body">
                        <div v-if="loadingEvidence" class="empty-card compact">Đang tải chứng cứ...</div>
                        <div v-else-if="evidenceItems.length === 0" class="empty-card compact">Chưa có chứng cứ phù hợp.</div>
                        <div v-else class="compact-list">
                            <div v-for="item in evidenceItems" :key="item.evidenceItemId" class="compact-row">
                                <span class="compact-time">{{ formatDateTime(item.createdAtUtc) }}</span>
                                <strong>{{ item.fileName || `Chứng cứ #${item.evidenceItemId}` }}</strong>
                                <span class="text-muted">{{ item.classification || '' }}</span>
                            </div>
                        </div>
                    </div>

                    <div v-else-if="detailTab === 'barriers'" class="detail-body">
                        <div v-if="loadingBarriers" class="empty-card compact">Đang tải lệnh barrier...</div>
                        <div v-else-if="barrierMessage" class="empty-card compact">{{ barrierMessage }}</div>
                        <div v-else-if="barrierCommands.length === 0" class="empty-card compact">Chưa có lệnh barrier liên quan.</div>
                        <div v-else class="compact-list">
                            <div v-for="command in barrierCommands" :key="command.barrierCommandAuditId" class="compact-row">
                                <span class="compact-time">{{ formatDateTime(command.requestedAtUtc) }}</span>
                                <span class="soft-chip">{{ command.command }}</span>
                                <span>{{ command.reason || '' }}</span>
                            </div>
                        </div>
                    </div>

                    <div v-else class="detail-body">
                        <div v-if="loadingCorrelations" class="empty-card compact">Đang tải tương quan...</div>
                        <div v-else-if="correlations.length === 0" class="empty-card compact">Chưa có tương quan phù hợp.</div>
                        <div v-else class="compact-list">
                            <div v-for="correlation in correlations" :key="correlation.correlationId" class="compact-row">
                                <span class="compact-time">{{ formatDateTime(correlation.createdAtUtc) }}</span>
                                <strong>{{ correlation.correlationType || 'Tương quan' }}</strong>
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
                        <h2 class="panel-title">Hàng đợi can thiệp</h2>
                        <p class="pane-subtitle">Một nơi cho cả bảo vệ, quản lý và admin thao tác theo workflow thật.</p>
                    </div>
                    <button v-if="canManuallyCreateIntervention" class="btn btn-primary btn-sm" @click="showCreateModal = true">+ Tạo yêu cầu</button>
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

                <div v-if="loadingInterventions" class="empty-card">Đang tải yêu cầu can thiệp...</div>
                <div v-else-if="filteredInterventions.length === 0" class="empty-card">Không có yêu cầu phù hợp bộ lọc hiện tại.</div>
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
                        <strong class="queue-title">{{ item.subjectName || item.plateNumber || `Yêu cầu #${item.operationalInterventionRequestId}` }}</strong>
                        <div class="queue-meta">
                            <span>{{ priorityLabel(item.priority) }}</span>
                            <span>{{ item.laneName || item.laneId || 'Không gắn lane' }}</span>
                        </div>
                        <div class="queue-footer">
                            <span class="status-badge" :class="`status-${(item.status || 'Pending').toLowerCase()}`">{{ statusLabel(item.status) }}</span>
                        </div>
                    </button>
                </div>
            </section>

            <section class="workspace-pane detail-pane">
                <div v-if="!selectedIntervention" class="empty-card">Chọn một yêu cầu để xử lý.</div>
                <template v-else>
                    <div class="detail-header">
                        <div>
                            <span class="panel-kicker">Yêu cầu #{{ selectedIntervention.operationalInterventionRequestId }}</span>
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
                                Phê duyệt
                            </button>
                            <button
                                v-if="canRejectIntervention(selectedIntervention)"
                                class="btn btn-warning btn-sm"
                                :disabled="savingIntervention || !interventionReviewNote.trim()"
                                @click="rejectIntervention(selectedIntervention)"
                            >
                                Từ chối
                            </button>
                            <button
                                v-if="canExecuteIntervention(selectedIntervention)"
                                class="btn btn-primary btn-sm"
                                :disabled="savingIntervention"
                                @click="executeIntervention(selectedIntervention)"
                            >
                                Thực thi cấp quyền
                            </button>
                        </div>
                    </div>

                    <div v-if="interventionMessage" class="alert" :class="interventionError ? 'alert-danger' : 'alert-success'">
                        {{ interventionMessage }}
                    </div>

                    <div class="meta-grid">
                        <div class="meta-item">
                            <span class="meta-label">Trạng thái</span>
                            <span class="status-badge" :class="`status-${(selectedIntervention.status || 'Pending').toLowerCase()}`">
                                {{ statusLabel(selectedIntervention.status) }}
                            </span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Ưu tiên</span>
                            <span class="severity-badge" :class="`severity-${prioritySeverity(selectedIntervention.priority)}`">
                                {{ priorityLabel(selectedIntervention.priority) }}
                            </span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Đối tượng</span>
                            <span>{{ selectedIntervention.subjectName || '---' }}</span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Biển số</span>
                            <span>{{ selectedIntervention.plateNumber || '---' }}</span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Lane</span>
                            <span>{{ selectedIntervention.laneName || selectedIntervention.laneId || '---' }}</span>
                        </div>
                        <div class="meta-item">
                            <span class="meta-label">Tạo lúc</span>
                            <span>{{ formatDateTime(selectedIntervention.createdAtUtc) }}</span>
                        </div>
                    </div>

                    <div class="detail-section">
                        <label class="meta-label" for="review-note">Ghi chú duyệt / lý do từ chối</label>
                        <textarea
                            id="review-note"
                            v-model="interventionReviewNote"
                            class="form-control"
                            rows="3"
                            placeholder="Ghi rõ căn cứ, phạm vi và trách nhiệm..."
                        />
                    </div>

                    <div class="detail-section">
                        <h3 class="detail-section-title">Dòng thời gian workflow</h3>
                        <ExceptionCaseTimeline :items="buildInterventionTimeline(selectedIntervention)" />
                    </div>
                </template>
            </section>
        </div>

        <Teleport to="body">
            <div v-if="showCreateModal" class="modal-backdrop" @click.self="showCreateModal = false">
                <div class="modal-card">
                    <div class="modal-header">
                        <h2>Tạo yêu cầu can thiệp</h2>
                        <button class="btn btn-ghost btn-sm" @click="showCreateModal = false">Đóng</button>
                    </div>
                    <div class="modal-body">
                        <div v-if="createMessage" class="alert" :class="createError ? 'alert-danger' : 'alert-success'">
                            {{ createMessage }}
                        </div>
                        <div class="form-grid">
                            <label>
                                <span>Loại can thiệp</span>
                                <select v-model="createForm.interventionType" class="form-control">
                                    <option value="temporary_grant">Cấp quyền tạm thời</option>
                                    <option value="policy_override">Ghi đè chính sách</option>
                                    <option value="device_override">Ghi đè thiết bị</option>
                                    <option value="emergency_override">Ghi đè khẩn cấp</option>
                                    <option value="other">Khác</option>
                                </select>
                            </label>
                            <label>
                                <span>Ưu tiên</span>
                                <select v-model="createForm.priority" class="form-control">
                                    <option value="low">Thấp</option>
                                    <option value="medium">Trung bình</option>
                                    <option value="high">Cao</option>
                                    <option value="critical">Nghiêm trọng</option>
                                </select>
                            </label>
                            <label>
                                <span>Đối tượng</span>
                                <input v-model.trim="createForm.subjectName" class="form-control" />
                            </label>
                            <label>
                                <span>Biển số</span>
                                <input v-model.trim="createForm.plateNumber" class="form-control" />
                            </label>
                            <label>
                                <span>Làn / khu vực</span>
                                <input v-model.trim="createForm.laneName" class="form-control" />
                            </label>
                            <label class="form-span-2">
                                <span>Lý do</span>
                                <textarea v-model.trim="createForm.reason" class="form-control" rows="3" />
                            </label>
                            <label class="form-span-2">
                                <span>Ghi chú</span>
                                <textarea v-model.trim="createForm.note" class="form-control" rows="2" />
                            </label>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="showCreateModal = false">Hủy</button>
                        <button class="btn btn-primary" :disabled="creating || !createForm.reason.trim()" @click="submitInterventionRequest">
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
        { id: 'all', label: 'Tất cả', count: exceptionCases.value.length },
        { id: 'pending_approval', label: 'Chờ phê duyệt', count: categoryCount('pending_approval') },
        { id: 'manual_override', label: 'Override / bypass', count: categoryCount('manual_override') },
        { id: 'data_mismatch', label: 'Lệch dữ liệu', count: categoryCount('data_mismatch') },
        { id: 'device_degraded', label: 'Device lỗi', count: categoryCount('device_degraded') },
        { id: 'emergency_pass', label: 'Khẩn cấp', count: categoryCount('emergency_pass') },
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
        { value: 'all', label: 'Tất cả', count: interventionRequests.value.length },
        { value: 'Pending', label: 'Chờ xử lý', count: counts.Pending || 0 },
        { value: 'Accepted', label: 'Đã chấp nhận', count: counts.Accepted || 0 },
        { value: 'Executed', label: 'Đã thực thi', count: counts.Executed || 0 },
        { value: 'Rejected', label: 'Đã từ chối', count: counts.Rejected || 0 },
        { value: 'Expired', label: 'Hết hạn', count: counts.Expired || 0 },
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
        data_mismatch: 'Lệch dữ liệu',
        manual_override: 'Override',
        device_degraded: 'Device lỗi',
        emergency_pass: 'Khẩn cấp',
        duress: 'Duress',
        pending_approval: 'Chờ phê duyệt',
    }[value] || value
}

function severityLabel(value) {
    return {
        critical: 'Nghiêm trọng',
        high: 'Cao',
        medium: 'Trung bình',
        low: 'Thấp',
    }[value] || value
}

function priorityLabel(value) {
    return {
        critical: 'Nghiêm trọng',
        high: 'Cao',
        medium: 'Trung bình',
        low: 'Thấp',
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
        Pending: 'Chờ xử lý',
        Accepted: 'Đã chấp nhận',
        Executed: 'Đã thực thi',
        Rejected: 'Đã từ chối',
        Escalated: 'Đã chuyển hàng đợi',
        Closed: 'Đã khóa sổ',
    }[value] || value
}

function statusLabel(value) {
    return {
        Pending: 'Chờ xử lý',
        Accepted: 'Đã chấp nhận',
        Rejected: 'Đã từ chối',
        Executed: 'Đã thực thi',
        Expired: 'Hết hạn',
    }[value] || value
}

function interventionTypeLabel(value) {
    return {
        temporary_grant: 'Cấp quyền tạm thời',
        policy_override: 'Ghi đè chính sách',
        device_override: 'Ghi đè thiết bị',
        emergency_override: 'Ghi đè khẩn cấp',
        other: 'Khác',
    }[value] || value
}

function methodLabel(value) {
    return {
        manual: 'Thủ công / bypass',
        plate: 'QR + biển số',
        system: 'Hệ thống',
        'face-and-plate': 'Đa nguồn',
        face: 'Khuôn mặt',
    }[value] || value || 'Hệ thống'
}

function formatDateTime(value) {
    if (!value) return '---'
    return new Date(value).toLocaleString('vi-VN')
}

function formatRelativeTime(value) {
    if (!value) return '---'
    const diffMs = Date.now() - new Date(value).getTime()
    const diffMinutes = Math.floor(diffMs / 60000)
    if (diffMinutes < 1) return 'Vừa xong'
    if (diffMinutes < 60) return `${diffMinutes} phút`
    const diffHours = Math.floor(diffMinutes / 60)
    if (diffHours < 24) return `${diffHours} giờ`
    return `${Math.floor(diffHours / 24)} ngày`
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
        subjectName: item.actorName || 'Chưa rõ',
        actorType: item.actorType || 'Chưa rõ',
        employeeId: item.employeeId || null,
        plateText: item.capturedLicensePlate || '',
        gateId: item.gateId || null,
        gateName: item.gateName || '',
        laneName: '',
        method: item.method || 'system',
        resultStatus: item.resultStatus || '',
        reasonCode,
        reason: item.exceptionReasonDescription || 'Ngoại lệ cần đối soát',
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
                title: item.exceptionReasonDescription || 'Sự kiện ngoại lệ',
                description: note || item.resultStatus || 'Không có mô tả bổ sung',
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
    if (isBaoVe.value) return 'Tạo yêu cầu can thiệp'
    if (isQuanLy.value) return 'Tạo và duyệt'
    if (shouldExecuteImmediately(item)) return 'Tạo và thực thi'
    return 'Tạo và duyệt'
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
        reason: item.reason || item.note || 'Case ngoại lệ cần can thiệp',
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
            title: 'Yêu cầu can thiệp đã được tạo',
            description: payload.reason,
            timestamp: new Date().toISOString(),
            actor: currentRole.value,
            reason: payload.interventionType,
        })

        if ((isAdmin.value || isQuanLy.value) && requestId) {
            await enterpriseApi.acceptInterventionRequest(requestId, { note: `Duyệt từ case #${item.sourceLogId}` })
            item.workflowStatus = 'Accepted'
            item.timeline.push({
                id: `accepted-${requestId}`,
                type: 'approve',
                title: 'Yêu cầu đã được chấp nhận',
                description: 'Chấp nhận ngay từ trang ngoại lệ',
                timestamp: new Date().toISOString(),
                actor: currentRole.value,
                reason: payload.interventionType,
            })
        }

        if (shouldExecuteImmediately(item) && requestId) {
            await enterpriseApi.executeInterventionRequest(requestId, { note: `Thực thi nhanh từ case #${item.sourceLogId}` })
            item.workflowStatus = 'Executed'
            item.timeline.push({
                id: `executed-${requestId}`,
                type: 'success',
                title: 'Yêu cầu đã được thực thi',
                description: 'Đã tạo hiệu lực vận hành từ workflow can thiệp',
                timestamp: new Date().toISOString(),
                actor: currentRole.value,
                reason: payload.interventionType,
            })
        }

        await loadInterventions(requestId)
        caseActionMessage.value = isBaoVe.value
            ? `Đã tạo yêu cầu can thiệp #${requestId}.`
            : item.workflowStatus === 'Executed'
                ? `Đã tạo và thực thi yêu cầu #${requestId}.`
                : `Đã tạo và duyệt yêu cầu #${requestId}.`
    } catch (error) {
        caseActionError.value = true
        caseActionMessage.value = error?.response?.data?.message || 'Không thể tạo workflow can thiệp cho case này.'
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
            note: `Case #${item.sourceLogId} được khóa sổ bởi ${currentRole.value}`,
        })
        item.workflowStatus = 'Closed'
        item.timeline.push({
            id: `closed-${Date.now()}`,
            type: 'close',
            title: 'Case đã được khóa sổ',
            description: 'Không còn yêu cầu xử lý tiếp trên hàng đợi.',
            timestamp: new Date().toISOString(),
            actor: currentRole.value,
            reason: 'Khóa sổ từ bàn ngoại lệ',
        })
        caseActionMessage.value = `Case #${item.sourceLogId} đã được khóa sổ.`
    } catch (error) {
        caseActionError.value = true
        caseActionMessage.value = error?.response?.data?.message || 'Không thể khóa sổ case.'
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
            barrierMessage.value = 'Case này chưa truy ra được lane cụ thể, không thể đối chiếu barrier command một cách đáng tin cậy.'
            return
        }

        const barriersResponse = await enterpriseApi.getBarriers({ laneId, active: true })
        const barriers = barriersResponse.data || []
        if (!barriers.length) {
            barrierMessage.value = 'Không có barrier nào được gắn với lane này.'
            return
        }

        const response = await enterpriseApi.getBarrierCommands(barriers[0].barrierId, { pageSize: 20 })
        barrierCommands.value = response.data?.items || []
    } catch {
        barrierMessage.value = 'Không thể tải lịch sử barrier command.'
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
            note: interventionReviewNote.value || 'Chấp nhận từ trang ngoại lệ',
        })
        Object.assign(item, response.data)
        await refreshInterventionSelection(
            item.operationalInterventionRequestId,
            `Yêu cầu #${item.operationalInterventionRequestId} đã được chấp nhận.`,
        )
    } catch (error) {
        interventionError.value = true
        interventionMessage.value = error?.response?.data?.message || 'Chấp nhận thất bại.'
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
            `Yêu cầu #${item.operationalInterventionRequestId} đã bị từ chối.`,
        )
    } catch (error) {
        interventionError.value = true
        interventionMessage.value = error?.response?.data?.message || 'Từ chối thất bại.'
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
            note: interventionReviewNote.value || 'Thực thi từ trang ngoại lệ',
        })
        Object.assign(item, response.data?.request || response.data)
        await refreshInterventionSelection(
            item.operationalInterventionRequestId,
            `Yêu cầu #${item.operationalInterventionRequestId} đã được thực thi.`,
        )
    } catch (error) {
        interventionError.value = true
        interventionMessage.value = error?.response?.data?.message || 'Thực thi thất bại.'
    } finally {
        savingIntervention.value = false
    }
}

function buildInterventionTimeline(item) {
    const timeline = [
        {
            id: `created-${item.operationalInterventionRequestId}`,
            type: 'system',
            title: 'Yêu cầu được tạo',
            description: item.reason,
            timestamp: item.createdAtUtc,
            actor: item.requestedByUserId ? `Người dùng #${item.requestedByUserId}` : '',
            reason: item.interventionType,
        },
    ]

    if (item.acceptedAtUtc) {
        timeline.push({
            id: `accepted-${item.operationalInterventionRequestId}`,
            type: 'approve',
            title: 'Yêu cầu được chấp nhận',
            description: item.note || '',
            timestamp: item.acceptedAtUtc,
            actor: item.acceptedByUserId ? `Người dùng #${item.acceptedByUserId}` : '',
            reason: 'Đã chấp nhận',
        })
    }

    if (item.rejectedAtUtc) {
        timeline.push({
            id: `rejected-${item.operationalInterventionRequestId}`,
            type: 'reject',
            title: 'Yêu cầu bị từ chối',
            description: item.rejectionReason || '',
            timestamp: item.rejectedAtUtc,
            actor: item.rejectedByUserId ? `Người dùng #${item.rejectedByUserId}` : '',
            reason: 'Đã từ chối',
        })
    }

    if (item.executedAtUtc) {
        timeline.push({
            id: `executed-${item.operationalInterventionRequestId}`,
            type: 'success',
            title: 'Yêu cầu được thực thi',
            description: item.note || '',
            timestamp: item.executedAtUtc,
            actor: item.executedByUserId ? `Người dùng #${item.executedByUserId}` : '',
            reason: 'Đã thực thi',
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

        createMessage.value = 'Đã gửi yêu cầu can thiệp.'
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
        createMessage.value = error?.response?.data?.message || 'Gửi yêu cầu thất bại.'
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
    background: var(--bg-card);
    border: 1px solid var(--border-subtle);
    border-radius: 16px;
    box-shadow: var(--shadow-sm);
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
    color: var(--text-secondary);
    font-weight: 700;
    font-size: 0.96rem;
    line-height: 1;
    transition: background 0.15s ease, color 0.15s ease, box-shadow 0.15s ease;
}

.tab-bar button:hover {
    background: var(--surface-subtle);
    color: var(--text-primary);
}

.tab-bar button.active {
    background: linear-gradient(135deg, #0f7f8e, #2563eb);
    color: var(--text-on-interactive);
    box-shadow: 0 10px 22px rgba(37, 99, 235, 0.18);
}

.summary-card,
.workspace-pane {
    background: var(--surface-default);
    border: 1px solid var(--border-subtle);
    border-radius: 14px;
    box-shadow: var(--shadow-sm);
}

.summary-card {
    padding: 14px 16px;
    display: flex;
    flex-direction: column;
    gap: 6px;
}

.summary-label {
    color: var(--text-muted);
    font-size: 0.82rem;
}

.summary-value {
    color: var(--text-primary);
    font-size: 1.6rem;
    line-height: 1;
}

.summary-card small {
    color: var(--text-muted);
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
    color: var(--text-muted);
}

.detail-title {
    margin: 4px 0 0;
    font-size: 1.35rem;
    color: var(--text-primary);
}

.search-shell {
    min-width: 240px;
}

.filter-input,
.form-control {
    width: 100%;
    border: 1px solid var(--border-subtle);
    border-radius: 10px;
    padding: 10px 12px;
    background: var(--surface-default);
    color: var(--text-primary);
}

.filter-pills,
.detail-tabs {
    display: flex;
    gap: 8px;
    flex-wrap: wrap;
    margin-top: 14px;
}

.pill-btn {
    border: 1px solid var(--border-subtle);
    background: var(--surface-subtle);
    color: var(--text-secondary);
    border-radius: 999px;
    padding: 8px 12px;
    font-weight: 600;
    transition: transform 0.15s ease, box-shadow 0.15s ease, background 0.15s ease, color 0.15s ease, border-color 0.15s ease;
}

.pill-btn:hover {
    transform: translateY(-1px);
    box-shadow: var(--shadow-sm);
}

.detail-tabs button {
    border-radius: 10px;
    padding: 8px 14px;
    font-weight: 600;
    color: var(--text-secondary);
    transition: transform 0.15s ease, box-shadow 0.15s ease, background 0.15s ease, color 0.15s ease, border-color 0.15s ease;
}

.detail-tabs button:hover {
    transform: translateY(-1px);
    box-shadow: var(--shadow-sm);
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
    background: var(--status-neutral-bg);
    color: var(--text-secondary);
    font-size: 0.76rem;
    font-weight: 800;
}

.tab-bar button.active .tab-count {
    background: rgba(255, 255, 255, 0.2);
    color: var(--text-on-interactive);
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
    border: 1px solid var(--border-subtle);
    background: var(--surface-default);
    border-radius: 12px;
    padding: 12px;
    text-align: left;
    transition: border-color 0.12s ease, box-shadow 0.12s ease, background 0.12s ease;
}

.queue-item:hover {
    border-color: var(--border-strong);
    box-shadow: var(--shadow-sm);
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
    color: var(--text-primary);
}

.queue-meta,
.queue-footer,
.time-label {
    color: var(--text-muted);
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
    border: 1px solid var(--border-subtle);
    border-radius: 12px;
    padding: 12px;
    background: var(--surface-raised);
    display: flex;
    flex-direction: column;
    gap: 6px;
}

.meta-label {
    color: var(--text-muted);
    font-size: 0.78rem;
    font-weight: 700;
}

.detail-body,
.detail-section {
    margin-top: 16px;
}

.detail-section-title {
    margin: 0 0 10px;
    color: var(--text-primary);
}

.compact-list {
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.compact-row {
    border: 1px solid var(--border-subtle);
    border-radius: 10px;
    background: var(--surface-default);
    padding: 10px 12px;
    display: grid;
    grid-template-columns: 180px auto auto 1fr;
    gap: 10px;
    align-items: center;
}

.compact-time {
    color: var(--text-muted);
    font-size: 0.8rem;
}

.text-muted {
    color: var(--text-muted);
}

.alert {
    border-radius: 12px;
    padding: 12px 14px;
    margin-top: 14px;
    border: 1px solid transparent;
}

.alert-success {
    background: var(--status-success-bg);
    border-color: var(--status-success-border);
    color: var(--status-success-text);
}

.alert-danger {
    background: var(--status-danger-bg);
    border-color: var(--status-danger-border);
    color: var(--status-danger-text);
}

.empty-card.compact,
.empty-card {
    border-radius: 12px;
    background: var(--surface-subtle);
    color: var(--text-muted);
    padding: 24px;
    text-align: center;
    border: 1px dashed var(--border-subtle);
}

.modal-backdrop {
    position: fixed;
    inset: 0;
    background: var(--surface-overlay);
    display: grid;
    place-items: center;
    z-index: 90;
}

.modal-card {
    width: min(720px, calc(100vw - 32px));
    background: var(--surface-default);
    border-radius: 18px;
    box-shadow: var(--shadow-overlay);
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
    border-bottom: 1px solid var(--border-subtle);
}

.modal-body {
    padding: 18px;
}

.modal-footer {
    border-top: 1px solid var(--border-subtle);
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

