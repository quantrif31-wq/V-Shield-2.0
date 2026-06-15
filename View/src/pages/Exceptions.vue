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
                <button class="btn btn-secondary btn-sm" :disabled="loading" @click="loadAll">Refresh</button>
            </div>
        </div>

        <!-- Case Classification Tabs -->
        <div class="tab-bar">
            <button
                v-for="cat in caseCategories"
                :key="cat.id"
                :class="{ active: activeCategory === cat.id }"
                @click="activeCategory = cat.id; loadCases()"
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
    </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { enterpriseApi } from '../services/enterpriseSecurityApi'
import { getExceptions } from '../services/accessLogApi'
import { authState } from '../stores/auth'
import ExceptionCaseTimeline from '../components/shared/ExceptionCaseTimeline.vue'

const loading = ref(false)
const saving = ref(false)
const loadingEvidence = ref(false)
const loadingBarriers = ref(false)
const loadingCorrelations = ref(false)
const searchQuery = ref('')
const activeCategory = ref('all')
const selectedCase = ref(null)
const detailTab = ref('events')
const isUsingDemoData = ref(false)

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
