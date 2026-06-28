<template>
    <div class="page-container reception-page animate-in">
        <section class="reception-hero">
            <div class="hero-copy">
                <span class="hero-kicker">Lễ tân</span>
                <h1 class="hero-title">Bàn điều phối tiếp đón khách</h1>
                <p class="hero-subtitle">
                    Theo dõi khách đến trong ngày, xử lý các tình huống phát sinh và lưu lại toàn bộ trao đổi để
                    minh bạch, dễ truy vết.
                </p>

                <div class="hero-actions">
                    <button class="btn btn-primary" :disabled="loading" @click="loadAll">Làm mới toàn bộ</button>
                    <button class="btn btn-secondary" @click="activeTab = 'lost-found'">Tra cứu đồ thất lạc</button>
                </div>
            </div>

            <div class="hero-spotlight">
                <div class="spotlight-card primary">
                    <span class="spotlight-label">Ưu tiên ngay</span>
                    <strong class="spotlight-value">{{ priorityCount }}</strong>
                    <span class="spotlight-note">Khách quá giờ hoặc chưa đến cần theo dõi</span>
                </div>
                <div class="spotlight-row">
                    <div class="spotlight-card">
                        <span class="spotlight-label">Khách hôm nay</span>
                        <strong class="spotlight-value small">{{ overview.todayVisits || 0 }}</strong>
                    </div>
                    <div class="spotlight-card">
                        <span class="spotlight-label">Đang ở trong khuôn viên</span>
                        <strong class="spotlight-value small">{{ overview.activeVisitors || 0 }}</strong>
                    </div>
                </div>
            </div>
        </section>

        <section class="metric-ribbon">
            <article class="metric-panel">
                <span class="metric-icon slate">01</span>
                <div>
                    <div class="metric-value">{{ overview.pendingArrivals || 0 }}</div>
                    <div class="metric-label">Khách chờ đến</div>
                </div>
            </article>
            <article class="metric-panel">
                <span class="metric-icon amber">02</span>
                <div>
                    <div class="metric-value">{{ overview.lateArrivalsNeedFollowUp || 0 }}</div>
                    <div class="metric-label">Lịch hẹn cần nhắc</div>
                </div>
            </article>
            <article class="metric-panel">
                <span class="metric-icon red">03</span>
                <div>
                    <div class="metric-value">{{ overview.overdueVisitors || 0 }}</div>
                    <div class="metric-label">Khách quá giờ</div>
                </div>
            </article>
            <article class="metric-panel">
                <span class="metric-icon blue">04</span>
                <div>
                    <div class="metric-value">{{ overview.openSecurityRequests || 0 }}</div>
                    <div class="metric-label">Phối hợp đang mở</div>
                </div>
            </article>
            <article class="metric-panel">
                <span class="metric-icon teal">05</span>
                <div>
                    <div class="metric-value">{{ overview.lostFoundCases || 0 }}</div>
                    <div class="metric-label">Vụ việc đồ thất lạc</div>
                </div>
            </article>
        </section>

        <section class="search-deck ops-panel">
            <div class="search-copy">
                <div class="section-kicker">Tra cứu nhanh</div>
                <h2 class="section-title">Tìm khách theo người liên hệ, điện thoại hoặc email</h2>
            </div>
            <div class="search-shell">
                <div class="search-bar glass">
                    <svg class="search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <circle cx="11" cy="11" r="8" />
                        <path d="M21 21l-4.35-4.35" />
                    </svg>
                    <input
                        v-model="searchQuery"
                        type="text"
                        placeholder="Ví dụ: Nguyễn Văn A, 090..., host phụ trách..."
                        @keyup.enter="loadBoard"
                    />
                </div>
                <button class="btn btn-primary" :disabled="loading" @click="loadBoard">Tra cứu khách</button>
            </div>
        </section>

        <div class="workspace-grid">
            <section class="ops-panel workspace-main">
                <div class="tab-bar reception-tabs">
                    <button
                        v-for="tab in tabs"
                        :key="tab.key"
                        :class="{ active: activeTab === tab.key }"
                        @click="activeTab = tab.key"
                    >
                        <span>{{ tab.label }}</span>
                        <small>{{ tabCounts[tab.key] || 0 }}</small>
                    </button>
                </div>

                <div v-if="loading" class="empty-card empty-large">Đang tải dữ liệu lễ tân...</div>

                <template v-else>
                    <div v-if="activeTab === 'arrivals'" class="queue-layout">
                        <div class="queue-header">
                            <div>
                                <div class="section-title">Hàng chờ tiếp đón hôm nay</div>
                                <div class="text-muted">Chọn khách để xem chi tiết và xử lý nhanh.</div>
                            </div>
                        </div>
                        <div class="queue-list">
                            <button
                                v-for="visit in board.arrivals"
                                :key="visit.visitId"
                                class="visit-card"
                                :class="{ selected: selectedVisit?.visitId === visit.visitId }"
                                @click="selectVisit(visit)"
                            >
                                <div class="visit-card-top">
                                    <div>
                                        <strong class="visit-name">{{ visit.visitorName }}</strong>
                                        <div class="visit-contact">{{ visit.visitorPhone || 'Chưa có số điện thoại' }}</div>
                                    </div>
                                    <span class="soft-chip" :class="statusClass(visit.status)">{{ statusLabel(visit.status) }}</span>
                                </div>
                                <div class="visit-card-meta">
                                    <span>{{ visit.hostEmployee?.fullName || 'Chưa gán người liên hệ' }}</span>
                                    <span>{{ formatDateTime(visit.expectedInUtc) }}</span>
                                </div>
                            </button>
                            <div v-if="board.arrivals.length === 0" class="empty-card">Không có lịch tiếp đón nào hôm nay.</div>
                        </div>
                    </div>

                    <div v-else-if="activeTab === 'overdue'" class="queue-layout">
                        <div class="queue-header">
                            <div>
                                <div class="section-title">Danh sách khách quá giờ</div>
                                <div class="text-muted">Ưu tiên kiểm tra vị trí và chủ trì liên hệ trước khi chuyển phối hợp.</div>
                            </div>
                        </div>
                        <div class="queue-list">
                            <button
                                v-for="visit in board.overdue"
                                :key="visit.visitId"
                                class="visit-card danger"
                                :class="{ selected: selectedVisit?.visitId === visit.visitId }"
                                @click="selectVisit(visit)"
                            >
                                <div class="visit-card-top">
                                    <div>
                                        <strong class="visit-name">{{ visit.visitorName }}</strong>
                                        <div class="visit-contact">{{ visit.hostEmployee?.fullName || 'Chưa gán người liên hệ' }}</div>
                                    </div>
                                    <span class="soft-chip danger">Quá giờ</span>
                                </div>
                                <div class="visit-card-meta">
                                    <span>{{ visit.site?.name || 'Chưa gán địa điểm' }}</span>
                                    <span>Dự kiến ra {{ formatDateTime(visit.expectedOutUtc) }}</span>
                                </div>
                            </button>
                            <div v-if="board.overdue.length === 0" class="empty-card">Không có khách nào quá giờ.</div>
                        </div>
                    </div>

                    <div v-else-if="activeTab === 'follow-up'" class="queue-layout">
                        <div class="queue-header">
                            <div>
                                <div class="section-title">Các lịch hẹn cần nhắc</div>
                                <div class="text-muted">Dùng khi khách chưa đến hoặc người liên hệ chưa phản hồi.</div>
                            </div>
                        </div>
                        <div class="queue-list">
                            <button
                                v-for="visit in board.lateArrivals"
                                :key="visit.visitId"
                                class="visit-card warning"
                                :class="{ selected: selectedVisit?.visitId === visit.visitId }"
                                @click="selectVisit(visit)"
                            >
                                <div class="visit-card-top">
                                    <div>
                                        <strong class="visit-name">{{ visit.visitorName }}</strong>
                                        <div class="visit-contact">{{ visit.visitorPhone || 'Chưa có số điện thoại' }}</div>
                                    </div>
                                    <span class="soft-chip warning">Chưa đến</span>
                                </div>
                                <div class="visit-card-meta">
                                    <span>{{ visit.hostEmployee?.fullName || 'Chưa gán người liên hệ' }}</span>
                                    <span>Hẹn lúc {{ formatDateTime(visit.expectedInUtc) }}</span>
                                </div>
                            </button>
                            <div v-if="board.lateArrivals.length === 0" class="empty-card">Không có lịch nào cần nhắc thêm.</div>
                        </div>
                    </div>

                    <div v-else class="lost-found-layout">
                        <div class="lost-found-header">
                            <div>
                                <div class="section-title">Tra cứu đồ thất lạc tại quầy</div>
                                <div class="text-muted">Tìm nhanh theo tên người báo, người nhặt hoặc mô tả đồ vật.</div>
                            </div>
                            <div class="lost-found-toolbar">
                                <input
                                    v-model="lostFoundQuery"
                                    type="text"
                                    class="form-control"
                                    placeholder="Ví dụ: ví da, Nguyễn Văn B, 090..."
                                    @keyup.enter="loadLostFound"
                                />
                                <button class="btn btn-secondary" @click="loadLostFound">Tra cứu</button>
                            </div>
                        </div>

                        <div class="case-columns">
                            <div class="case-column">
                                <div class="case-column-title">Báo mất</div>
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

                            <div class="case-column">
                                <div class="case-column-title">Đồ tìm thấy</div>
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
                    </div>
                </template>
            </section>

            <aside class="ops-panel workspace-side">
                <div class="detail-heading">
                    <div>
                        <span class="section-kicker">Hồ sơ xử lý</span>
                        <h2 class="section-title">Thông tin khách và nhật ký phối hợp</h2>
                    </div>
                    <div class="priority-pill" :class="{ active: selectedVisit }">
                        {{ selectedVisit ? 'Đang mở hồ sơ' : 'Chưa chọn hồ sơ' }}
                    </div>
                </div>

                <div v-if="detailLoading" class="empty-card empty-large">Đang tải hồ sơ...</div>
                <div v-else-if="!detail?.visit" class="empty-card empty-large">Chọn một khách ở danh sách bên trái để bắt đầu hỗ trợ.</div>

                <template v-else>
                    <div class="profile-card">
                        <div class="profile-top">
                            <div>
                                <div class="profile-name">{{ detail.visit.visitorName }}</div>
                                <div class="profile-sub">{{ detail.visit.hostEmployee?.fullName || 'Chưa gán người liên hệ' }}</div>
                            </div>
                            <span class="soft-chip" :class="statusClass(detail.visit.status)">{{ statusLabel(detail.visit.status) }}</span>
                        </div>

                        <div class="profile-grid">
                            <div class="profile-item">
                                <span>Điện thoại</span>
                                <strong>{{ detail.visit.visitorPhone || 'Chưa có' }}</strong>
                            </div>
                            <div class="profile-item">
                                <span>Email</span>
                                <strong>{{ detail.visit.visitorEmail || 'Chưa có' }}</strong>
                            </div>
                            <div class="profile-item">
                                <span>Giờ hẹn vào</span>
                                <strong>{{ formatDateTime(detail.visit.expectedInUtc) }}</strong>
                            </div>
                            <div class="profile-item">
                                <span>Giờ dự kiến ra</span>
                                <strong>{{ formatDateTime(detail.visit.expectedOutUtc) }}</strong>
                            </div>
                            <div class="profile-item">
                                <span>Hiện diện</span>
                                <strong>{{ presenceLabel(detail.receptionContext?.currentPresence) }}</strong>
                            </div>
                            <div class="profile-item">
                                <span>Xe liên quan</span>
                                <strong>{{ parkingLabel(detail.receptionContext?.latestParkingPermit, detail.receptionContext?.latestLaneEvent) }}</strong>
                            </div>
                        </div>
                    </div>

                    <div class="quick-actions">
                        <button
                            v-if="canCheckIn(detail.visit.status)"
                            class="action-tile primary"
                            @click="checkInSelectedVisit"
                        >
                            <span class="action-tile-label">Xác nhận đã đến</span>
                            <small>Ghi nhận khách vào khuôn viên</small>
                        </button>
                        <button
                            v-if="canCheckOut(detail.visit.status)"
                            class="action-tile"
                            @click="checkOutSelectedVisit"
                        >
                            <span class="action-tile-label">Xác nhận đã rời</span>
                            <small>Khép lại lượt thăm</small>
                        </button>
                        <button class="action-tile" @click="openInteraction('HostContact', 'Đã gọi người liên hệ để phối hợp đón khách')">
                            <span class="action-tile-label">Gọi người liên hệ</span>
                            <small>Ghi lại lần phối hợp</small>
                        </button>
                        <button class="action-tile warning" @click="openInteraction('SecurityDispatch', 'Yêu cầu bảo vệ hỗ trợ khách hoặc kiểm tra vị trí', true)">
                            <span class="action-tile-label">Gọi bảo vệ</span>
                            <small>Khi cần hỗ trợ hiện trường</small>
                        </button>
                        <button class="action-tile" @click="openInteraction('ParkingInquiry', 'Xác nhận tình trạng xe của khách trong bãi')">
                            <span class="action-tile-label">Xác nhận xe</span>
                            <small>Kiểm tra xem xe còn trong bãi không</small>
                        </button>
                        <button class="action-tile" @click="openInteraction('Wayfinding', 'Hướng dẫn đường đi hoặc cung cấp thông tin cho khách')">
                            <span class="action-tile-label">Hướng dẫn khách</span>
                            <small>Chỉ đường, giải đáp thông tin</small>
                        </button>
                    </div>

                    <div class="timeline-shell">
                        <div class="timeline-header">
                            <div class="section-title">Nhật ký phối hợp gần nhất</div>
                            <div class="text-muted">{{ interactionItems.length }} mục</div>
                        </div>
                        <div v-if="interactionItems.length === 0" class="empty-card">Chưa có nhật ký xử lý nào cho khách này.</div>
                        <div v-else class="timeline-list">
                            <article v-for="item in interactionItems" :key="item.receptionInteractionId" class="timeline-item">
                                <div class="timeline-title-row">
                                    <strong>{{ item.summary }}</strong>
                                    <span class="timeline-status">{{ item.status }}</span>
                                </div>
                                <div class="timeline-meta">
                                    <span>{{ interactionTypeLabel(item.interactionType) }}</span>
                                    <span>{{ formatDateTime(item.createdAtUtc) }}</span>
                                </div>
                                <p v-if="item.detailNote" class="timeline-note">{{ item.detailNote }}</p>
                                <p v-if="item.resolutionNote" class="timeline-note muted">Kết quả: {{ item.resolutionNote }}</p>
                            </article>
                        </div>
                    </div>
                </template>
            </aside>
        </div>

        <Teleport to="body">
            <div v-if="showInteractionModal" class="modal-overlay" @click.self="showInteractionModal = false">
                <div class="modal-panel reception-modal">
                    <div class="modal-header">
                        <div>
                            <div class="section-kicker">Nhật ký lễ tân</div>
                            <h2>Ghi nhận một lần xử lý</h2>
                        </div>
                        <button class="btn-close" @click="showInteractionModal = false">&times;</button>
                    </div>
                    <div class="modal-body">
                        <div class="form-grid">
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

                        <div class="form-group">
                            <label>Tóm tắt ngắn</label>
                            <input v-model="interactionForm.summary" type="text" class="form-control" />
                        </div>

                        <div class="form-grid">
                            <div class="form-group">
                                <label>Tên liên hệ</label>
                                <input v-model="interactionForm.contactPersonName" type="text" class="form-control" />
                            </div>
                            <div class="form-group">
                                <label>Số điện thoại</label>
                                <input v-model="interactionForm.contactPersonPhone" type="text" class="form-control" />
                            </div>
                        </div>

                        <div class="form-group">
                            <label>Ghi chú chi tiết</label>
                            <textarea
                                v-model="interactionForm.detailNote"
                                rows="4"
                                class="form-control"
                                placeholder="Đã trao đổi với ai, thống nhất ra sao, còn việc gì cần theo dõi tiếp..."
                            ></textarea>
                        </div>

                        <div class="form-grid">
                            <div class="form-group">
                                <label>Biển số liên quan</label>
                                <input v-model="interactionForm.relatedVehiclePlate" type="text" class="form-control" />
                            </div>
                            <div class="form-group checkbox-shell">
                                <label>&nbsp;</label>
                                <label class="checkbox-label emphasis">
                                    <input v-model="interactionForm.securityRequested" type="checkbox" />
                                    Cần bảo vệ phối hợp
                                </label>
                            </div>
                        </div>

                        <div class="form-group">
                            <label>Kết quả hoặc hướng xử lý tiếp</label>
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
const priorityCount = computed(() => (overview.value.overdueVisitors || 0) + (overview.value.lateArrivalsNeedFollowUp || 0))
const tabCounts = computed(() => ({
    arrivals: board.arrivals.length,
    overdue: board.overdue.length,
    'follow-up': board.lateArrivals.length,
    'lost-found': (lostFound.lostItems?.length || 0) + (lostFound.foundItems?.length || 0),
}))

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
            if (replacement)
                selectedVisit.value = replacement
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

function interactionTypeLabel(type) {
    const map = {
        HostContact: 'Liên hệ người phụ trách',
        VisitorSupport: 'Hỗ trợ khách',
        SecurityDispatch: 'Gọi bảo vệ',
        ParkingInquiry: 'Xác nhận xe',
        LostFoundSupport: 'Tra cứu đồ thất lạc',
        Wayfinding: 'Chỉ đường',
        FollowUp: 'Theo dõi bổ sung',
    }
    return map[type] || type
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
    --reception-cream: #fff7e8;
    --reception-sand: #f0d9b1;
    --reception-ink: #23313f;
    --reception-teal: #0f766e;
    --reception-red: #b45309;
    --reception-slate: #5b6b79;
    display: flex;
    flex-direction: column;
    gap: 18px;
}

.reception-hero {
    position: relative;
    display: grid;
    grid-template-columns: minmax(0, 1.3fr) minmax(320px, 0.9fr);
    gap: 18px;
    padding: 28px;
    border-radius: 28px;
    overflow: hidden;
    color: #fff7ef;
    background:
        radial-gradient(circle at top left, rgba(255, 255, 255, 0.18), transparent 34%),
        radial-gradient(circle at bottom right, rgba(252, 211, 77, 0.14), transparent 28%),
        linear-gradient(135deg, #21404a 0%, #13353d 45%, #0d2026 100%);
}

.reception-hero::after {
    content: '';
    position: absolute;
    inset: auto -70px -70px auto;
    width: 220px;
    height: 220px;
    border-radius: 50%;
    background: rgba(255, 255, 255, 0.06);
}

.hero-copy,
.hero-spotlight {
    position: relative;
    z-index: 1;
}

.hero-kicker,
.section-kicker {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    font-size: 0.8rem;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    color: rgba(255, 247, 239, 0.74);
}

.hero-title {
    margin: 10px 0 12px;
    font-size: clamp(2rem, 3vw, 3.2rem);
    line-height: 1.03;
    letter-spacing: -0.03em;
}

.hero-subtitle {
    max-width: 640px;
    margin: 0;
    font-size: 1rem;
    line-height: 1.65;
    color: rgba(255, 247, 239, 0.84);
}

.hero-actions {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
    margin-top: 20px;
}

.hero-spotlight {
    display: flex;
    flex-direction: column;
    gap: 14px;
}

.spotlight-row {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 14px;
}

.spotlight-card {
    padding: 18px;
    border-radius: 22px;
    backdrop-filter: blur(8px);
    background: rgba(255, 255, 255, 0.08);
    border: 1px solid rgba(255, 255, 255, 0.08);
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.spotlight-card.primary {
    min-height: 170px;
    justify-content: center;
    background: linear-gradient(180deg, rgba(252, 211, 77, 0.18), rgba(255, 255, 255, 0.06));
}

.spotlight-label {
    font-size: 0.82rem;
    text-transform: uppercase;
    letter-spacing: 0.12em;
    color: rgba(255, 247, 239, 0.7);
}

.spotlight-value {
    font-size: 4.2rem;
    line-height: 0.95;
    letter-spacing: -0.06em;
}

.spotlight-value.small {
    font-size: 2.1rem;
}

.spotlight-note {
    color: rgba(255, 247, 239, 0.78);
}

.metric-ribbon {
    display: grid;
    grid-template-columns: repeat(5, minmax(0, 1fr));
    gap: 12px;
}

.metric-panel {
    background: linear-gradient(180deg, #fffdfa 0%, #f7efe4 100%);
    border: 1px solid rgba(176, 132, 71, 0.14);
    border-radius: 22px;
    padding: 16px 18px;
    display: flex;
    align-items: center;
    gap: 14px;
    color: var(--reception-ink);
}

.metric-icon {
    width: 44px;
    height: 44px;
    border-radius: 14px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    font-weight: 700;
    font-size: 0.82rem;
}

.metric-icon.slate { background: #e7eef6; color: #31536d; }
.metric-icon.amber { background: #fff0cf; color: #a16207; }
.metric-icon.red { background: #fde2d8; color: #c2410c; }
.metric-icon.blue { background: #dff3f5; color: #0f766e; }
.metric-icon.teal { background: #e3f4ea; color: #0f766e; }

.metric-value {
    font-size: 1.7rem;
    line-height: 1;
    font-weight: 700;
}

.metric-label {
    margin-top: 4px;
    color: var(--text-muted);
    font-size: 0.9rem;
}

.search-deck {
    display: grid;
    grid-template-columns: minmax(0, 0.9fr) minmax(0, 1.1fr);
    gap: 18px;
    align-items: center;
    padding: 20px 22px;
}

.search-shell {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    gap: 12px;
}

.search-bar.glass {
    border-radius: 18px;
    border: 1px solid rgba(35, 49, 63, 0.08);
    background: linear-gradient(180deg, rgba(255, 255, 255, 0.95), rgba(248, 241, 231, 0.92));
}

.workspace-grid {
    display: grid;
    grid-template-columns: minmax(0, 1.18fr) minmax(360px, 0.82fr);
    gap: 18px;
    align-items: start;
}

.workspace-main,
.workspace-side {
    min-height: 760px;
}

.reception-tabs {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
    margin-bottom: 18px;
}

.reception-tabs button {
    min-width: 140px;
    padding: 12px 14px;
    border-radius: 16px;
    border: 1px solid rgba(35, 49, 63, 0.08);
    background: #fff;
    display: flex;
    justify-content: space-between;
    align-items: center;
    gap: 10px;
    color: var(--reception-ink);
}

.reception-tabs button small {
    padding: 4px 8px;
    border-radius: 999px;
    background: #eff4f7;
    color: var(--reception-slate);
    font-size: 0.78rem;
}

.reception-tabs button.active {
    background: linear-gradient(135deg, #173941 0%, #0f766e 100%);
    color: #fff;
    border-color: transparent;
}

.reception-tabs button.active small {
    background: rgba(255, 255, 255, 0.16);
    color: #fff;
}

.queue-layout,
.lost-found-layout {
    display: flex;
    flex-direction: column;
    gap: 16px;
}

.queue-header,
.lost-found-header,
.detail-heading,
.timeline-header {
    display: flex;
    justify-content: space-between;
    gap: 14px;
    align-items: flex-start;
}

.queue-list,
.timeline-list {
    display: flex;
    flex-direction: column;
    gap: 12px;
}

.visit-card,
.case-card {
    width: 100%;
    border-radius: 20px;
    border: 1px solid rgba(35, 49, 63, 0.08);
    background: linear-gradient(180deg, #ffffff 0%, #fbf6ee 100%);
    padding: 16px 18px;
    text-align: left;
    display: flex;
    flex-direction: column;
    gap: 10px;
    transition: transform .18s ease, border-color .18s ease, box-shadow .18s ease;
}

.visit-card:hover,
.visit-card.selected,
.case-card:hover {
    transform: translateY(-1px);
    border-color: rgba(15, 118, 110, 0.28);
    box-shadow: 0 16px 32px rgba(23, 57, 65, 0.08);
}

.visit-card.warning {
    border-color: rgba(245, 158, 11, 0.22);
    background: linear-gradient(180deg, #fffef7 0%, #fff4dd 100%);
}

.visit-card.danger {
    border-color: rgba(234, 88, 12, 0.18);
    background: linear-gradient(180deg, #fff7f3 0%, #fdeee7 100%);
}

.visit-card-top,
.visit-card-meta {
    display: flex;
    justify-content: space-between;
    gap: 12px;
    align-items: flex-start;
    flex-wrap: wrap;
}

.visit-name {
    display: block;
    font-size: 1.03rem;
    color: var(--reception-ink);
}

.visit-contact {
    margin-top: 4px;
    color: var(--text-muted);
    font-size: 0.9rem;
}

.visit-card-meta {
    color: var(--reception-slate);
    font-size: 0.92rem;
}

.detail-heading {
    margin-bottom: 16px;
}

.priority-pill {
    padding: 10px 12px;
    border-radius: 999px;
    background: #f4f1eb;
    color: var(--reception-slate);
    font-size: 0.86rem;
    font-weight: 600;
}

.priority-pill.active {
    background: #e3f4ea;
    color: var(--reception-teal);
}

.profile-card {
    border-radius: 24px;
    padding: 18px;
    background:
        radial-gradient(circle at top right, rgba(15, 118, 110, 0.08), transparent 34%),
        linear-gradient(180deg, #fffdf8 0%, #f7efe1 100%);
    border: 1px solid rgba(35, 49, 63, 0.08);
}

.profile-top {
    display: flex;
    justify-content: space-between;
    gap: 14px;
    align-items: flex-start;
    margin-bottom: 18px;
}

.profile-name {
    font-size: 1.3rem;
    font-weight: 700;
    color: var(--reception-ink);
}

.profile-sub {
    margin-top: 6px;
    color: var(--text-muted);
}

.profile-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 12px;
}

.profile-item {
    padding: 12px 14px;
    border-radius: 16px;
    background: rgba(255, 255, 255, 0.7);
    display: flex;
    flex-direction: column;
    gap: 6px;
}

.profile-item span {
    color: var(--text-muted);
    font-size: 0.84rem;
}

.profile-item strong {
    color: var(--reception-ink);
    line-height: 1.45;
}

.quick-actions {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 12px;
    margin-top: 16px;
}

.action-tile {
    padding: 14px 16px;
    border-radius: 18px;
    border: 1px solid rgba(35, 49, 63, 0.08);
    text-align: left;
    background: #fff;
    color: var(--reception-ink);
    transition: transform .18s ease, box-shadow .18s ease, border-color .18s ease;
}

.action-tile:hover {
    transform: translateY(-1px);
    box-shadow: 0 14px 28px rgba(23, 57, 65, 0.08);
}

.action-tile.primary {
    background: linear-gradient(135deg, #173941 0%, #0f766e 100%);
    border-color: transparent;
    color: #fff;
}

.action-tile.warning {
    background: linear-gradient(180deg, #fffaf1 0%, #fff2da 100%);
    border-color: rgba(245, 158, 11, 0.2);
}

.action-tile-label {
    display: block;
    font-weight: 700;
    margin-bottom: 6px;
}

.action-tile small {
    display: block;
    line-height: 1.45;
    color: inherit;
    opacity: 0.82;
}

.timeline-shell {
    margin-top: 18px;
}

.timeline-item {
    border-radius: 18px;
    border: 1px solid rgba(35, 49, 63, 0.08);
    background: linear-gradient(180deg, #ffffff 0%, #f9f4ec 100%);
    padding: 14px 16px;
}

.timeline-title-row,
.timeline-meta {
    display: flex;
    justify-content: space-between;
    gap: 12px;
    flex-wrap: wrap;
}

.timeline-status {
    padding: 4px 8px;
    border-radius: 999px;
    background: #edf2f7;
    color: var(--reception-slate);
    font-size: 0.8rem;
    font-weight: 600;
}

.timeline-meta {
    margin-top: 8px;
    color: var(--text-muted);
    font-size: 0.9rem;
}

.timeline-note {
    margin: 10px 0 0;
}

.timeline-note.muted {
    color: var(--text-muted);
}

.lost-found-toolbar {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto;
    gap: 10px;
    min-width: min(420px, 100%);
}

.case-columns {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 16px;
}

.case-column {
    display: flex;
    flex-direction: column;
    gap: 12px;
}

.case-column-title {
    font-size: 0.9rem;
    text-transform: uppercase;
    letter-spacing: 0.1em;
    color: var(--reception-slate);
}

.reception-modal {
    width: min(760px, calc(100vw - 32px));
    border-radius: 28px;
    background: linear-gradient(180deg, #fffdf9 0%, #f5ece0 100%);
    border: 1px solid rgba(35, 49, 63, 0.08);
}

.form-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 14px;
}

.checkbox-shell {
    display: flex;
    flex-direction: column;
    justify-content: flex-end;
}

.checkbox-label.emphasis {
    min-height: 46px;
    padding: 0 14px;
    border-radius: 14px;
    border: 1px solid rgba(35, 49, 63, 0.08);
    background: rgba(255, 255, 255, 0.72);
}

.empty-large {
    min-height: 240px;
    display: grid;
    place-items: center;
    text-align: center;
}

@media (max-width: 1200px) {
    .metric-ribbon {
        grid-template-columns: repeat(3, minmax(0, 1fr));
    }

    .workspace-grid {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 960px) {
    .reception-hero,
    .search-deck {
        grid-template-columns: 1fr;
    }

    .profile-grid,
    .quick-actions,
    .case-columns,
    .form-grid {
        grid-template-columns: 1fr;
    }

    .spotlight-row {
        grid-template-columns: 1fr 1fr;
    }

    .metric-ribbon {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }
}

@media (max-width: 720px) {
    .reception-hero {
        padding: 22px;
    }

    .spotlight-row,
    .metric-ribbon,
    .lost-found-toolbar,
    .search-shell {
        grid-template-columns: 1fr;
    }

    .visit-card-top,
    .visit-card-meta,
    .timeline-title-row,
    .timeline-meta,
    .queue-header,
    .lost-found-header,
    .detail-heading,
    .timeline-header {
        flex-direction: column;
    }

    .reception-tabs button {
        width: 100%;
    }
}
</style>
