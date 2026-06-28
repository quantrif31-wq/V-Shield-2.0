<template>
    <div class="page-container reception-page animate-in">
        <header class="page-header-bar">
            <div>
                <span class="panel-kicker">Lễ tân</span>
                <h1 class="page-title">Bàn điều phối lễ tân</h1>
                <p class="page-subtitle">Hỗ trợ khách, tra cứu thông tin và ghi nhận mọi phối hợp để dễ truy vết về sau.</p>
            </div>
            <div class="header-actions">
                <button class="btn btn-secondary" :disabled="loading" @click="loadAll">Làm mới</button>
            </div>
        </header>

        <section class="metric-grid four">
            <article class="metric-tile">
                <span class="metric-label">Khách hôm nay</span>
                <strong class="metric-value">{{ overview.todayVisits || 0 }}</strong>
            </article>
            <article class="metric-tile">
                <span class="metric-label">Khách đang ở trong khuôn viên</span>
                <strong class="metric-value">{{ overview.activeVisitors || 0 }}</strong>
            </article>
            <article class="metric-tile warning">
                <span class="metric-label">Quá giờ cần theo dõi</span>
                <strong class="metric-value">{{ overview.overdueVisitors || 0 }}</strong>
            </article>
            <article class="metric-tile accent">
                <span class="metric-label">Yêu cầu đang mở</span>
                <strong class="metric-value">{{ overview.openSecurityRequests || 0 }}</strong>
            </article>
        </section>

        <section class="toolbar-shell">
            <div class="search-bar">
                <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <circle cx="11" cy="11" r="8" />
                    <path d="M21 21l-4.35-4.35" />
                </svg>
                <input
                    v-model="searchQuery"
                    type="text"
                    placeholder="Tìm theo tên khách, số điện thoại, email hoặc người liên hệ..."
                    @keyup.enter="loadBoard"
                />
            </div>
            <button class="btn btn-primary" :disabled="loading" @click="loadBoard">Tra cứu khách</button>
        </section>

        <div class="layout-grid">
            <section class="ops-panel">
                <div class="tab-bar">
                    <button
                        v-for="tab in tabs"
                        :key="tab.key"
                        :class="{ active: activeTab === tab.key }"
                        @click="activeTab = tab.key"
                    >
                        {{ tab.label }}
                    </button>
                </div>

                <div v-if="loading" class="empty-card">Đang tải dữ liệu lễ tân...</div>

                <template v-else>
                    <div v-if="activeTab === 'arrivals'" class="list-stack">
                        <button
                            v-for="visit in board.arrivals"
                            :key="visit.visitId"
                            class="visit-card"
                            :class="{ selected: selectedVisit?.visitId === visit.visitId }"
                            @click="selectVisit(visit)"
                        >
                            <div class="visit-main">
                                <strong>{{ visit.visitorName }}</strong>
                                <span class="text-muted">{{ visit.visitorPhone || 'Chưa có số điện thoại' }}</span>
                            </div>
                            <div class="visit-meta">
                                <span>{{ visit.hostEmployee?.fullName || 'Chưa gán người liên hệ' }}</span>
                                <span>{{ formatDateTime(visit.expectedInUtc) }}</span>
                            </div>
                            <span class="soft-chip" :class="statusClass(visit.status)">{{ statusLabel(visit.status) }}</span>
                        </button>
                        <div v-if="board.arrivals.length === 0" class="empty-card">Chưa có khách nào trong danh sách hôm nay.</div>
                    </div>

                    <div v-else-if="activeTab === 'overdue'" class="list-stack">
                        <button
                            v-for="visit in board.overdue"
                            :key="visit.visitId"
                            class="visit-card danger"
                            :class="{ selected: selectedVisit?.visitId === visit.visitId }"
                            @click="selectVisit(visit)"
                        >
                            <div class="visit-main">
                                <strong>{{ visit.visitorName }}</strong>
                                <span class="text-muted">{{ visit.hostEmployee?.fullName || 'Chưa gán người liên hệ' }}</span>
                            </div>
                            <div class="visit-meta">
                                <span>Dự kiến ra: {{ formatDateTime(visit.expectedOutUtc) }}</span>
                                <span>{{ visit.site?.name || 'Chưa gán địa điểm' }}</span>
                            </div>
                            <span class="soft-chip danger">Cần theo dõi</span>
                        </button>
                        <div v-if="board.overdue.length === 0" class="empty-card">Không có khách quá giờ.</div>
                    </div>

                    <div v-else-if="activeTab === 'follow-up'" class="list-stack">
                        <button
                            v-for="visit in board.lateArrivals"
                            :key="visit.visitId"
                            class="visit-card warning"
                            :class="{ selected: selectedVisit?.visitId === visit.visitId }"
                            @click="selectVisit(visit)"
                        >
                            <div class="visit-main">
                                <strong>{{ visit.visitorName }}</strong>
                                <span class="text-muted">{{ visit.visitorPhone || 'Chưa có số điện thoại' }}</span>
                            </div>
                            <div class="visit-meta">
                                <span>Hẹn đến: {{ formatDateTime(visit.expectedInUtc) }}</span>
                                <span>{{ visit.hostEmployee?.fullName || 'Chưa gán người liên hệ' }}</span>
                            </div>
                            <span class="soft-chip warning">Chưa đến</span>
                        </button>
                        <div v-if="board.lateArrivals.length === 0" class="empty-card">Không có lịch hẹn nào cần nhắc.</div>
                    </div>

                    <div v-else class="list-stack">
                        <div class="lost-found-toolbar">
                            <input
                                v-model="lostFoundQuery"
                                type="text"
                                class="form-control"
                                placeholder="Tìm theo tên người báo, người nhặt, số điện thoại, mô tả đồ..."
                                @keyup.enter="loadLostFound"
                            />
                            <button class="btn btn-secondary" @click="loadLostFound">Tra cứu đồ thất lạc</button>
                        </div>

                        <div class="result-block">
                            <div class="result-title">Báo mất</div>
                            <div v-if="lostFoundLoading" class="empty-card">Đang tra cứu...</div>
                            <div v-else-if="lostFound.lostItems.length === 0" class="empty-card">Chưa có kết quả báo mất phù hợp.</div>
                            <button
                                v-for="item in lostFound.lostItems"
                                :key="`lost-${item.lostItemReportId}`"
                                class="case-card"
                                @click="openInteractionForLostFound('LostFoundSupport', `Hỗ trợ tra cứu báo mất #${item.lostItemReportId}`, item)"
                            >
                                <strong>{{ item.itemDescription }}</strong>
                                <span>{{ item.reporterName }} - {{ item.reporterPhone }}</span>
                                <span class="text-muted">{{ item.lastSeenLocation || 'Chưa ghi vị trí cuối' }}</span>
                            </button>
                        </div>

                        <div class="result-block">
                            <div class="result-title">Đồ tìm thấy</div>
                            <div v-if="lostFoundLoading" class="empty-card">Đang tra cứu...</div>
                            <div v-else-if="lostFound.foundItems.length === 0" class="empty-card">Chưa có kết quả đồ tìm thấy phù hợp.</div>
                            <button
                                v-for="item in lostFound.foundItems"
                                :key="`found-${item.foundItemReportId}`"
                                class="case-card"
                                @click="openInteractionForLostFound('LostFoundSupport', `Hỗ trợ tra cứu đồ nhặt được #${item.foundItemReportId}`, item)"
                            >
                                <strong>{{ item.itemDescription }}</strong>
                                <span>{{ item.foundByName }} - {{ item.foundByPhone }}</span>
                                <span class="text-muted">{{ item.foundLocation || 'Chưa ghi vị trí nhặt được' }}</span>
                            </button>
                        </div>
                    </div>
                </template>
            </section>

            <aside class="ops-panel detail-panel">
                <div class="detail-header">
                    <div>
                        <div class="detail-title">Chi tiết hỗ trợ</div>
                        <div class="text-muted">Mỗi lần xử lý đều nên ghi lại để minh bạch và dễ truy vết.</div>
                    </div>
                </div>

                <div v-if="detailLoading" class="empty-card">Đang tải chi tiết...</div>
                <div v-else-if="!detail?.visit" class="empty-card">Chọn một khách ở danh sách bên trái để xem và hỗ trợ.</div>
                <template v-else>
                    <div class="detail-grid">
                        <div class="detail-row"><span class="detail-label">Khách</span><span>{{ detail.visit.visitorName }}</span></div>
                        <div class="detail-row"><span class="detail-label">Điện thoại</span><span>{{ detail.visit.visitorPhone || 'Chưa có' }}</span></div>
                        <div class="detail-row"><span class="detail-label">Email</span><span>{{ detail.visit.visitorEmail || 'Chưa có' }}</span></div>
                        <div class="detail-row"><span class="detail-label">Người liên hệ</span><span>{{ detail.visit.hostEmployee?.fullName || 'Chưa gán' }}</span></div>
                        <div class="detail-row"><span class="detail-label">Địa điểm</span><span>{{ detail.visit.site?.name || 'Chưa gán' }}</span></div>
                        <div class="detail-row"><span class="detail-label">Trạng thái</span><span class="soft-chip" :class="statusClass(detail.visit.status)">{{ statusLabel(detail.visit.status) }}</span></div>
                        <div class="detail-row"><span class="detail-label">Giờ hẹn vào</span><span>{{ formatDateTime(detail.visit.expectedInUtc) }}</span></div>
                        <div class="detail-row"><span class="detail-label">Giờ dự kiến ra</span><span>{{ formatDateTime(detail.visit.expectedOutUtc) }}</span></div>
                        <div class="detail-row"><span class="detail-label">Hiện diện</span><span>{{ presenceLabel(detail.receptionContext?.currentPresence) }}</span></div>
                        <div class="detail-row"><span class="detail-label">Xe trong bãi</span><span>{{ parkingLabel(detail.receptionContext?.latestParkingPermit, detail.receptionContext?.latestLaneEvent) }}</span></div>
                        <div class="detail-row"><span class="detail-label">Lần ghi nhận xe gần nhất</span><span>{{ laneEventLabel(detail.receptionContext?.latestLaneEvent) }}</span></div>
                    </div>

                    <div class="action-row">
                        <button
                            v-if="canCheckIn(detail.visit.status)"
                            class="btn btn-primary btn-sm"
                            @click="checkInSelectedVisit"
                        >
                            Xác nhận khách đã đến
                        </button>
                        <button
                            v-if="canCheckOut(detail.visit.status)"
                            class="btn btn-secondary btn-sm"
                            @click="checkOutSelectedVisit"
                        >
                            Ghi nhận khách đã rời
                        </button>
                        <button class="btn btn-secondary btn-sm" @click="openInteraction('HostContact', 'Đã gọi người liên hệ để phối hợp đón khách')">Gọi người liên hệ</button>
                        <button class="btn btn-secondary btn-sm" @click="openInteraction('SecurityDispatch', 'Yêu cầu bảo vệ hỗ trợ khách hoặc kiểm tra vị trí', true)">Gọi bảo vệ</button>
                        <button class="btn btn-secondary btn-sm" @click="openInteraction('ParkingInquiry', 'Xác nhận tình trạng xe của khách trong bãi')">Xác nhận xe</button>
                        <button class="btn btn-secondary btn-sm" @click="openInteraction('Wayfinding', 'Hướng dẫn đường đi hoặc cung cấp thông tin cho khách')">Hướng dẫn khách</button>
                    </div>

                    <div class="section-title">Nhật ký phối hợp gần nhất</div>
                    <div v-if="interactionItems.length === 0" class="empty-card">Chưa có nhật ký xử lý nào cho khách này.</div>
                    <div v-else class="timeline-list">
                        <article v-for="item in interactionItems" :key="item.receptionInteractionId" class="timeline-item">
                            <div class="timeline-title">{{ item.summary }}</div>
                            <div class="timeline-meta">
                                <span>{{ item.interactionType }}</span>
                                <span>{{ formatDateTime(item.createdAtUtc) }}</span>
                                <span>{{ item.status }}</span>
                            </div>
                            <p v-if="item.detailNote" class="timeline-note">{{ item.detailNote }}</p>
                            <p v-if="item.resolutionNote" class="timeline-note muted">Kết quả: {{ item.resolutionNote }}</p>
                        </article>
                    </div>
                </template>
            </aside>
        </div>

        <Teleport to="body">
            <div v-if="showInteractionModal" class="modal-overlay" @click.self="showInteractionModal = false">
                <div class="modal-panel">
                    <div class="modal-header">
                        <h2>Ghi nhận xử lý lễ tân</h2>
                        <button class="btn-close" @click="showInteractionModal = false">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="form-group">
                            <label>Loại xử lý</label>
                            <select v-model="interactionForm.interactionType" class="form-control">
                                <option value="HostContact">Liên hệ người phụ trách</option>
                                <option value="VisitorSupport">Hỗ trợ khách</option>
                                <option value="SecurityDispatch">Gọi bảo vệ</option>
                                <option value="ParkingInquiry">Xác nhận xe</option>
                                <option value="LostFoundSupport">Tra cứu đồ thất lạc</option>
                                <option value="Wayfinding">Chỉ đường</option>
                                <option value="FollowUp">Theo dõi bổ sung</option>
                            </select>
                        </div>
                        <div class="form-group">
                            <label>Tóm tắt</label>
                            <input v-model="interactionForm.summary" type="text" class="form-control" />
                        </div>
                        <div class="form-group">
                            <label>Ghi chú chi tiết</label>
                            <textarea v-model="interactionForm.detailNote" rows="4" class="form-control" placeholder="Đã trao đổi với ai, nội dung gì, cần theo dõi gì tiếp..."></textarea>
                        </div>
                        <div class="form-row two">
                            <div class="form-group">
                                <label>Tên liên hệ</label>
                                <input v-model="interactionForm.contactPersonName" type="text" class="form-control" />
                            </div>
                            <div class="form-group">
                                <label>Số điện thoại</label>
                                <input v-model="interactionForm.contactPersonPhone" type="text" class="form-control" />
                            </div>
                        </div>
                        <div class="form-row two">
                            <div class="form-group">
                                <label>Biển số liên quan</label>
                                <input v-model="interactionForm.relatedVehiclePlate" type="text" class="form-control" />
                            </div>
                            <div class="form-group">
                                <label>Trạng thái</label>
                                <select v-model="interactionForm.status" class="form-control">
                                    <option value="Open">Mới mở</option>
                                    <option value="InProgress">Đang xử lý</option>
                                    <option value="Resolved">Đã xử lý xong</option>
                                    <option value="Escalated">Đã chuyển tiếp</option>
                                    <option value="Cancelled">Hủy</option>
                                </select>
                            </div>
                        </div>
                        <label class="checkbox-label">
                            <input v-model="interactionForm.securityRequested" type="checkbox" />
                            Cần bảo vệ phối hợp
                        </label>
                        <div class="form-group">
                            <label>Kết quả / hướng xử lý tiếp</label>
                            <textarea v-model="interactionForm.resolutionNote" rows="3" class="form-control"></textarea>
                        </div>
                        <div v-if="saveError" class="alert alert-danger">{{ saveError }}</div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn btn-secondary" @click="showInteractionModal = false">Đóng</button>
                        <button class="btn btn-primary" :disabled="saving" @click="submitInteraction">
                            {{ saving ? 'Đang lưu...' : 'Lưu nhật ký' }}
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

const loading = ref(false)
const detailLoading = ref(false)
const lostFoundLoading = ref(false)
const saving = ref(false)
const saveError = ref('')

const searchQuery = ref('')
const lostFoundQuery = ref('')
const activeTab = ref('arrivals')
const showInteractionModal = ref(false)

const overview = ref({})
const board = reactive({
    arrivals: [],
    overdue: [],
    lateArrivals: [],
    activeVisitors: [],
    recentInteractions: [],
})
const lostFound = reactive({
    lostItems: [],
    foundItems: [],
})

const selectedVisit = ref(null)
const detail = ref(null)

const interactionForm = reactive({
    visitId: null,
    lostItemReportId: null,
    foundItemReportId: null,
    interactionType: 'VisitorSupport',
    summary: '',
    detailNote: '',
    contactPersonName: '',
    contactPersonPhone: '',
    relatedVehiclePlate: '',
    status: 'Open',
    securityRequested: false,
    resolutionNote: '',
})

const tabs = [
    { key: 'arrivals', label: 'Khách hôm nay' },
    { key: 'overdue', label: 'Khách quá giờ' },
    { key: 'follow-up', label: 'Khách chưa đến' },
    { key: 'lost-found', label: 'Đồ thất lạc' },
]

const interactionItems = computed(() => detail.value?.receptionContext?.interactions || [])

async function loadAll() {
    await Promise.all([loadOverview(), loadBoard()])
}

async function loadOverview() {
    const res = await enterpriseApi.getReceptionOverview()
    overview.value = res.data || {}
}

async function loadBoard() {
    loading.value = true
    try {
        const res = await enterpriseApi.getReceptionBoard({ search: searchQuery.value || undefined })
        Object.assign(board, {
            arrivals: res.data?.arrivals || [],
            overdue: res.data?.overdue || [],
            lateArrivals: res.data?.lateArrivals || [],
            activeVisitors: res.data?.activeVisitors || [],
            recentInteractions: res.data?.recentInteractions || [],
        })

        if (selectedVisit.value?.visitId) {
            const replacement = [...board.arrivals, ...board.overdue, ...board.lateArrivals, ...board.activeVisitors]
                .find((item) => item.visitId === selectedVisit.value.visitId)
            if (replacement) {
                selectedVisit.value = replacement
            }
        }

        if (!selectedVisit.value) {
            const firstVisit = board.overdue[0] || board.lateArrivals[0] || board.arrivals[0] || board.activeVisitors[0] || null
            if (firstVisit)
                await selectVisit(firstVisit)
        }
    } finally {
        loading.value = false
    }
}

async function loadVisitDetail(visitId) {
    detailLoading.value = true
    try {
        const res = await enterpriseApi.getVisitDetail(visitId)
        detail.value = res.data
    } finally {
        detailLoading.value = false
    }
}

async function selectVisit(visit) {
    selectedVisit.value = visit
    await loadVisitDetail(visit.visitId)
}

async function loadLostFound() {
    if (!lostFoundQuery.value.trim()) {
        lostFound.lostItems = []
        lostFound.foundItems = []
        return
    }

    lostFoundLoading.value = true
    try {
        const res = await enterpriseApi.getReceptionLostFound({ search: lostFoundQuery.value.trim() })
        lostFound.lostItems = res.data?.lostItems || []
        lostFound.foundItems = res.data?.foundItems || []
    } finally {
        lostFoundLoading.value = false
    }
}

function resetInteractionForm() {
    Object.assign(interactionForm, {
        visitId: selectedVisit.value?.visitId || null,
        lostItemReportId: null,
        foundItemReportId: null,
        interactionType: 'VisitorSupport',
        summary: '',
        detailNote: '',
        contactPersonName: detail.value?.visit?.hostEmployee?.fullName || '',
        contactPersonPhone: '',
        relatedVehiclePlate: '',
        status: 'Open',
        securityRequested: false,
        resolutionNote: '',
    })
    saveError.value = ''
}

function openInteraction(type, summary, securityRequested = false) {
    resetInteractionForm()
    interactionForm.interactionType = type
    interactionForm.summary = summary
    interactionForm.securityRequested = securityRequested
    showInteractionModal.value = true
}

function openInteractionForLostFound(type, summary, item) {
    resetInteractionForm()
    interactionForm.interactionType = type
    interactionForm.summary = summary
    interactionForm.lostItemReportId = item.lostItemReportId || null
    interactionForm.foundItemReportId = item.foundItemReportId || null
    interactionForm.contactPersonName = item.reporterName || item.foundByName || ''
    interactionForm.contactPersonPhone = item.reporterPhone || item.foundByPhone || ''
    interactionForm.detailNote = item.itemDescription || ''
    showInteractionModal.value = true
}

async function submitInteraction() {
    saving.value = true
    saveError.value = ''
    try {
        await enterpriseApi.createReceptionInteraction({ ...interactionForm })
        showInteractionModal.value = false
        if (selectedVisit.value?.visitId)
            await loadVisitDetail(selectedVisit.value.visitId)
        await loadOverview()
        await loadBoard()
    } catch (error) {
        saveError.value = error.response?.data?.message || 'Không thể lưu nhật ký lễ tân.'
    } finally {
        saving.value = false
    }
}

async function checkInSelectedVisit() {
    if (!detail.value?.visit?.visitId)
        return
    await enterpriseApi.checkInVisit(detail.value.visit.visitId, { verificationStatus: 'Verified' })
    await loadAll()
    await loadVisitDetail(detail.value.visit.visitId)
}

async function checkOutSelectedVisit() {
    if (!detail.value?.visit?.visitId)
        return
    await enterpriseApi.checkOutVisit(detail.value.visit.visitId)
    await loadAll()
    await loadVisitDetail(detail.value.visit.visitId)
}

function canCheckIn(status) {
    return status === 'Approved' || status === 'Invited'
}

function canCheckOut(status) {
    return status === 'CheckedIn' || status === 'Overstay'
}

function statusLabel(status) {
    const map = {
        Invited: 'Đã mời',
        Approved: 'Đã duyệt',
        CheckedIn: 'Đã vào',
        Overstay: 'Quá giờ',
        CheckedOut: 'Đã ra',
        Denied: 'Từ chối',
    }
    return map[status] || status
}

function statusClass(status) {
    if (status === 'CheckedIn') return 'success'
    if (status === 'Overstay') return 'danger'
    if (status === 'Denied') return 'danger'
    if (status === 'Approved') return 'info'
    return 'warning'
}

function presenceLabel(value) {
    return value === 'OnSite' ? 'Đang ở trong khuôn viên' : 'Không còn trong khuôn viên'
}

function parkingLabel(permit, laneEvent) {
    if (!permit)
        return 'Chưa có dữ liệu xe liên kết'
    if (!laneEvent)
        return `Có vé tại ${permit.parkingArea?.name || 'bãi xe'}`
    return `${laneEvent.direction === 'Exit' ? 'Xe có thể đã ra' : 'Xe vẫn còn ghi nhận trong bãi'} - ${laneEvent.plateText || 'chưa rõ biển số'}`
}

function laneEventLabel(event) {
    if (!event)
        return 'Chưa có sự kiện xe gần đây'
    return `${formatDateTime(event.occurredAtUtc)} - ${event.direction || 'Không rõ hướng'} tại ${event.lane?.name || 'làn không xác định'}`
}

function formatDateTime(value) {
    if (!value)
        return 'Chưa có'
    return new Date(value).toLocaleString('vi-VN', {
        hour: '2-digit',
        minute: '2-digit',
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
    })
}

onMounted(loadAll)
</script>

<style scoped>
.reception-page {
    display: flex;
    flex-direction: column;
    gap: 18px;
}

.page-subtitle {
    margin-top: 6px;
    color: var(--text-muted);
}

.layout-grid {
    display: grid;
    grid-template-columns: minmax(0, 1.2fr) minmax(320px, 0.8fr);
    gap: 18px;
}

.list-stack,
.timeline-list {
    display: flex;
    flex-direction: column;
    gap: 12px;
}

.visit-card,
.case-card {
    width: 100%;
    border: 1px solid var(--border-color);
    border-radius: 16px;
    background: var(--bg-card);
    padding: 16px;
    text-align: left;
    display: flex;
    flex-direction: column;
    gap: 8px;
    transition: transform .18s ease, border-color .18s ease, box-shadow .18s ease;
}

.visit-card:hover,
.case-card:hover,
.visit-card.selected {
    transform: translateY(-1px);
    border-color: var(--accent-primary);
    box-shadow: var(--shadow-md);
}

.visit-card.warning {
    border-color: rgba(245, 158, 11, .35);
}

.visit-card.danger {
    border-color: rgba(239, 68, 68, .35);
}

.visit-main,
.visit-meta {
    display: flex;
    justify-content: space-between;
    gap: 12px;
    flex-wrap: wrap;
}

.detail-panel {
    min-height: 560px;
}

.detail-header,
.section-title {
    margin-bottom: 14px;
}

.detail-title,
.result-title,
.section-title {
    font-weight: 700;
    color: var(--text-primary);
}

.detail-grid {
    display: grid;
    gap: 10px;
    margin-bottom: 18px;
}

.detail-row {
    display: flex;
    justify-content: space-between;
    gap: 12px;
    padding: 10px 12px;
    border-radius: 12px;
    background: rgba(148, 163, 184, 0.08);
}

.detail-label {
    color: var(--text-muted);
}

.action-row {
    display: flex;
    gap: 10px;
    flex-wrap: wrap;
    margin-bottom: 18px;
}

.timeline-item {
    border-left: 3px solid var(--accent-primary);
    padding: 12px 14px;
    background: rgba(59, 130, 246, 0.06);
    border-radius: 0 14px 14px 0;
}

.timeline-title {
    font-weight: 600;
}

.timeline-meta {
    display: flex;
    gap: 12px;
    flex-wrap: wrap;
    color: var(--text-muted);
    font-size: .92rem;
    margin-top: 6px;
}

.timeline-note {
    margin: 8px 0 0;
}

.timeline-note.muted {
    color: var(--text-muted);
}

.lost-found-toolbar {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    gap: 10px;
}

.result-block {
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.metric-tile.warning {
    border-color: rgba(245, 158, 11, .3);
}

.metric-tile.accent {
    border-color: rgba(59, 130, 246, .3);
}

@media (max-width: 1080px) {
    .layout-grid {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 720px) {
    .lost-found-toolbar {
        grid-template-columns: 1fr;
    }

    .visit-main,
    .visit-meta,
    .detail-row {
        flex-direction: column;
    }
}
</style>
