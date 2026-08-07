<template>
    <div class="page-container ops-page animate-in dashboard-ops">
        <div class="page-header-bar">
            <div>
                <span class="panel-kicker">Tổng quan vận hành</span>
                <h1 class="page-title">Bảng điều phối cho ca trực</h1>
                <p class="page-subtitle dashboard-subtitle">
                    Gom cảnh báo, hàng chờ xử lý, luồng cổng và mức sẵn sàng hệ thống vào một mặt nhìn
                    để người vận hành quyết định nhanh hơn.
                </p>
            </div>
            <div class="header-actions">
                <router-link
                    v-for="action in dashboardActions"
                    :key="action.label"
                    :to="action.route"
                    :class="action.primary ? 'btn btn-primary' : 'btn btn-secondary'"
                >
                    {{ action.label }}
                </router-link>
            </div>
        </div>

        <section class="command-deck">
            <article class="command-hero">
                <div class="hero-topline">
                    <span class="hero-kicker">{{ statusBanner.kicker }}</span>
                    <span class="hero-updated">Cập nhật {{ generatedAtLabel }}</span>
                </div>

                <div class="hero-copy">
                    <h2>{{ statusBanner.title }}</h2>
                    <p>{{ statusBanner.message }}</p>
                </div>

                <div class="hero-chip-row">
                    <span class="soft-chip" :class="statusBanner.chipClass">{{ statusBanner.chipText }}</span>
                    <span class="soft-chip">{{ snapshot.openAlarms || 0 }} cảnh báo đang mở</span>
                    <span class="soft-chip warn">{{ snapshot.pendingInterventions || 0 }} yêu cầu can thiệp</span>
                </div>

                <div class="hero-actions">
                    <router-link
                        v-for="action in heroActions"
                        :key="action.label"
                        :to="action.route"
                        :class="action.primary ? 'btn btn-primary' : 'btn btn-secondary'"
                    >
                        {{ action.label }}
                    </router-link>
                </div>
            </article>

            <aside class="command-side">
                <div class="side-head">
                    <span class="panel-kicker">Điểm cần chú ý</span>
                    <strong>Bức tranh vận hành tổng thể</strong>
                </div>

                <div class="command-metric-grid">
                    <article
                        v-for="item in commandMetrics"
                        :key="item.label"
                        class="command-metric"
                        :class="item.tone"
                    >
                        <span>{{ item.label }}</span>
                        <strong>{{ item.value }}</strong>
                        <small>{{ item.note }}</small>
                    </article>
                </div>
            </aside>
        </section>

        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">Hàng chờ công việc</span>
                        <h2 class="panel-title">Việc cần xử lý trước</h2>
                    </div>
                </div>

                <div class="queue-list">
                    <router-link
                        v-for="item in priorityQueue"
                        :key="item.label"
                        :to="item.route"
                        class="queue-item"
                        :class="item.tone"
                    >
                        <div class="queue-main">
                            <strong>{{ item.label }}</strong>
                            <p>{{ item.description }}</p>
                        </div>
                        <div class="queue-side">
                            <span class="queue-value">{{ item.value }}</span>
                            <small>{{ item.helper }}</small>
                        </div>
                    </router-link>
                </div>
            </article>

            <article class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">Sự kiện gần đây</span>
                        <h2 class="panel-title">Dòng sự kiện gần nhất</h2>
                    </div>
                    <router-link to="/access-logs" class="btn btn-secondary btn-sm">Nhật ký</router-link>
                </div>

                <div v-if="recentActivities.length" class="surface-list scrollable-panel">
                    <router-link
                        v-for="activity in recentActivities"
                        :key="activity.id"
                        :to="resolveRoute(activity.route, '/access-logs')"
                        class="event-row"
                    >
                        <div class="event-icon" :class="activity.severity || 'info'">{{ activityKindShort(activity.kind) }}</div>
                        <div class="event-copy">
                            <div class="event-topline">
                                <strong>{{ activity.title }}</strong>
                                <span>{{ formatRelativeTime(activity.occurredAt || activity.timestamp) }}</span>
                            </div>
                            <p>{{ activity.subtitle }}</p>
                            <div class="chip-row">
                                <span class="soft-chip">{{ activity.status || 'Đã ghi nhận' }}</span>
                                <span v-if="activity.kind" class="soft-chip">{{ activity.kind }}</span>
                                <span v-if="activity.meta" class="soft-chip warn">{{ activity.meta }}</span>
                            </div>
                        </div>
                    </router-link>
                </div>
                <div v-else class="empty-card">
                    Chưa có sự kiện mới. Hệ thống vẫn sẵn sàng, nhưng chưa có hoạt động đủ gần để đưa lên dòng vận hành.
                </div>
            </article>
        </section>

        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">Lưu lượng</span>
                        <h2 class="panel-title">Nhịp ra vào trong tuần</h2>
                    </div>
                    <div class="chip-row">
                        <span class="soft-chip">Vào {{ snapshot.dailyCheckIn || 0 }}</span>
                        <span class="soft-chip warn">Ra {{ snapshot.dailyCheckOut || 0 }}</span>
                    </div>
                </div>

                <div v-if="hasTrafficData" class="traffic-chart">
                    <div v-for="day in trafficChart" :key="day.label" class="chart-day">
                        <div class="chart-stack">
                            <div class="chart-bar in" :style="{ height: `${day.inPercent}%` }">
                                <span>{{ day.checkIn }}</span>
                            </div>
                            <div class="chart-bar out" :style="{ height: `${day.outPercent}%` }">
                                <span>{{ day.checkOut }}</span>
                            </div>
                        </div>
                        <strong>{{ day.label }}</strong>
                    </div>
                </div>
                <div v-else class="empty-card">
                    Không có lưu lượng trong tuần hiện tại. Trạng thái này được hiển thị như một ca trực yên tĩnh, không phải lỗi tải dữ liệu.
                </div>
            </article>

            <article class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">Toàn cảnh site</span>
                        <h2 class="panel-title">Hiện diện và phạm vi hệ thống</h2>
                    </div>
                </div>

                <div class="surface-list">
                    <article v-for="item in sitePictureItems" :key="item.label" class="surface-item">
                        <div class="dashboard-surface-line">
                            <div class="inline-stat">
                                <strong>{{ item.value }}</strong>
                                <span>{{ item.label }}</span>
                            </div>
                            <span class="surface-hint">{{ item.hint }}</span>
                        </div>
                    </article>
                </div>
            </article>
        </section>

        <section class="ops-grid two">
            <article class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">Hôm nay</span>
                        <h2 class="panel-title">Nhân sự và lịch trong ngày</h2>
                    </div>
                </div>

                <div class="today-grid">
                    <article v-for="item in workforceItems" :key="item.label" class="today-card">
                        <span>{{ item.label }}</span>
                        <strong>{{ item.value }}</strong>
                        <small>{{ item.note }}</small>
                    </article>
                </div>
            </article>

            <article class="ops-panel">
                <div class="panel-head">
                    <div>
                        <span class="panel-kicker">Tóm tắt AI</span>
                        <h2 class="panel-title">Nhắc việc và xu hướng</h2>
                    </div>
                    <span v-if="intelligenceLoading" class="soft-chip">Đang cập nhật</span>
                </div>

                <div v-if="intelligence" class="intel-stack">
                    <p class="intel-summary">{{ intelligence.summary }}</p>
                    <div v-if="topInsights.length" class="intel-insights">
                        <article
                            v-for="insight in topInsights"
                            :key="insight.title"
                            class="intel-insight"
                            :class="insight.type || 'info'"
                        >
                            <strong>{{ insight.title }}</strong>
                            <p>{{ insight.detail }}</p>
                        </article>
                    </div>
                </div>
                <div v-else-if="loadError" class="empty-card">
                    {{ loadError }}
                </div>
                <div v-else class="empty-card">
                    Chưa có lớp tổng hợp AI phù hợp để hiển thị ở thời điểm này.
                </div>
            </article>
        </section>
    </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { getDashboardOverview, getDashboardIntelligence } from '../services/dashboardApi'
import { enterpriseApi } from '../services/enterpriseSecurityApi'
import { authState } from '../stores/auth'

const snapshot = ref({
    generatedAt: null,
    vehiclesInside: 0,
    expectedVisitorsToday: 0,
    pendingRegistrations: 0,
    dailyCheckIn: 0,
    dailyCheckOut: 0,
    dailyExceptions: 0,
    camerasConfigured: 0,
    gatesConfigured: 0,
    guestProfiles: 0,
    employeeCount: 0,
    trainedEmployeeCount: 0,
    recognitionCoverage: 0,
    checkedInVisitors: 0,
    openAlarms: 0,
    criticalOpenAlarms: 0,
    offlineDevices: 0,
    degradedDevices: 0,
    activeEmergencyPasses: 0,
    pendingInterventions: 0,
    oldestPendingInterventionMinutes: 0,
    employeesWorkingToday: 0,
    employeesNotCheckedIn: 0,
    employeesLateToday: 0,
    pendingLeaveApprovals: 0,
    totalShiftsToday: 0,
    totalOvertimeHoursToday: 0,
})

const weeklyTraffic = ref([])
const recentActivities = ref([])
const laneHealth = ref([])
const intelligence = ref(null)
const intelligenceLoading = ref(false)
const isLoading = ref(true)
const loadError = ref('')
const currentRole = computed(() => authState.user?.role || 'Admin')

function hasAccess(route) {
    const role = currentRole.value
    const roleAccess = {
        '/soc-console': ['Admin', 'BaoVe'],
        '/enterprise-security': ['Admin', 'BaoVe'],
        '/gate-transit-monitor': ['Admin', 'BaoVe'],
        '/exceptions': ['Admin', 'BaoVe', 'QuanLy'],
        '/pre-registrations': ['Admin'],
        '/device-health': ['Admin', 'BaoVe'],
        '/attendance/leave-approvals': ['Admin', 'QuanLy'],
        '/access-logs': ['Admin', 'BaoVe', 'QuanLy'],
    }

    const allowed = roleAccess[route]
    return !allowed || allowed.includes(role)
}

function resolveRoute(primaryRoute, fallbackRoute = '/dashboard') {
    if (primaryRoute && hasAccess(primaryRoute)) return primaryRoute
    if (fallbackRoute && hasAccess(fallbackRoute)) return fallbackRoute
    return '/dashboard'
}

const hasTrafficData = computed(() =>
    weeklyTraffic.value.some((day) => Number(day.checkIn || 0) > 0 || Number(day.checkOut || 0) > 0)
)

const trafficChart = computed(() => {
    const maxValue = Math.max(
        ...weeklyTraffic.value.flatMap((day) => [Number(day.checkIn || 0), Number(day.checkOut || 0)]),
        1
    )

    return weeklyTraffic.value.map((day) => ({
        ...day,
        inPercent: Number(day.checkIn || 0) > 0
            ? Math.max(8, Math.round((Number(day.checkIn || 0) / maxValue) * 100))
            : 0,
        outPercent: Number(day.checkOut || 0) > 0
            ? Math.max(8, Math.round((Number(day.checkOut || 0) / maxValue) * 100))
            : 0,
    }))
})

const generatedAtLabel = computed(() => {
    if (!snapshot.value.generatedAt) return 'vừa xong'
    return new Date(snapshot.value.generatedAt).toLocaleString('vi-VN', {
        hour: '2-digit',
        minute: '2-digit',
        day: '2-digit',
        month: '2-digit',
    })
})

const laneHealthSummary = computed(() => {
    const lanes = Array.isArray(laneHealth.value) ? laneHealth.value : []
    const degraded = lanes.filter((lane) => lane?.isDegraded)
    const healthyCount = Math.max(0, lanes.length - degraded.length)

    return {
        total: lanes.length,
        healthyCount,
        degradedCount: degraded.length,
        barrierCount: lanes.reduce((sum, lane) => sum + Number(lane?.barrierCount || 0), 0),
        degradedNames: degraded.map((lane) => lane?.name).filter(Boolean).slice(0, 3),
    }
})

const statusBanner = computed(() => {
    if ((snapshot.value.criticalOpenAlarms || 0) > 0 || (snapshot.value.activeEmergencyPasses || 0) > 0) {
        return {
            kicker: 'Cần ưu tiên',
            title: 'Ca trực đang có hạng mục cần phản ứng ngay',
            message: 'Ưu tiên kiểm tra cảnh báo mức nghiêm trọng, thông hành khẩn cấp và xác nhận không có điểm kiểm soát nào đang bị bỏ ngỏ.',
            chipText: 'Ưu tiên phản ứng',
            chipClass: 'danger',
        }
    }

    if (
        (snapshot.value.offlineDevices || 0) > 0 ||
        (snapshot.value.pendingInterventions || 0) > 0 ||
        (snapshot.value.dailyExceptions || 0) > 0 ||
        laneHealthSummary.value.degradedCount > 0
    ) {
        return {
            kicker: 'Cần theo dõi',
            title: 'Hệ thống ổn nhưng cần theo dõi sát các điểm phát sinh',
            message: 'Có thiết bị, ngoại lệ hoặc yêu cầu can thiệp đang chờ xử lý. Ca trực nên bám sát hàng chờ thay vì chỉ nhìn số liệu tổng hợp.',
            chipText: 'Theo dõi chủ động',
            chipClass: 'warn',
        }
    }

    return {
        kicker: 'Vận hành ổn định',
        title: 'Tình hình đang yên, phù hợp cho giám sát chủ động',
        message: 'Chưa có tín hiệu khẩn cấp nổi bật. Nên dùng thời gian này để rà soát hàng chờ, sức khỏe thiết bị và bảo đảm các luồng vào/ra giữ đúng chuẩn.',
        chipText: 'Ổn định',
        chipClass: 'success',
    }
})

const commandMetrics = computed(() => [
    {
        label: 'Cảnh báo nghiêm trọng',
        value: snapshot.value.criticalOpenAlarms || 0,
        note: `${snapshot.value.openAlarms || 0} cảnh báo đang mở`,
        tone: (snapshot.value.criticalOpenAlarms || 0) > 0 ? 'danger' : 'neutral',
    },
    {
        label: 'Can thiệp chờ duyệt',
        value: snapshot.value.pendingInterventions || 0,
        note: (snapshot.value.oldestPendingInterventionMinutes || 0) > 0
            ? `Cũ nhất ${snapshot.value.oldestPendingInterventionMinutes} phút`
            : 'Không có tồn đọng',
        tone: (snapshot.value.pendingInterventions || 0) > 0 ? 'warn' : 'neutral',
    },
    {
        label: 'Khách đang ở trong khuôn viên',
        value: snapshot.value.checkedInVisitors || 0,
        note: `${snapshot.value.expectedVisitorsToday || 0} khách dự kiến hôm nay`,
        tone: 'neutral',
    },
    {
        label: 'Thiết bị cần chú ý',
        value: (snapshot.value.offlineDevices || 0) + (snapshot.value.degradedDevices || 0),
        note: `${snapshot.value.offlineDevices || 0} mất kết nối / ${snapshot.value.degradedDevices || 0} suy giảm`,
        tone: ((snapshot.value.offlineDevices || 0) + (snapshot.value.degradedDevices || 0)) > 0 ? 'warn' : 'neutral',
    },
    {
        label: 'Làn cần chú ý',
        value: laneHealthSummary.value.degradedCount,
        note: laneHealthSummary.value.degradedCount > 0
            ? laneHealthSummary.value.degradedNames.join(', ')
            : `${laneHealthSummary.value.healthyCount}/${laneHealthSummary.value.total} làn ổn định`,
        tone: laneHealthSummary.value.degradedCount > 0 ? 'warn' : 'neutral',
    },
])

const dashboardActions = computed(() => {
    if (currentRole.value === 'QuanLy') {
        return [
            { label: 'Xử lý ngoại lệ', route: '/exceptions', primary: true },
            { label: 'Tra cứu vào/ra', route: '/access-logs', primary: false },
            { label: 'Báo cáo chấm công', route: '/attendance/reports', primary: false },
        ]
    }

    return [
        { label: 'Mở SOC', route: '/soc-console', primary: true },
        { label: 'Theo dõi cổng', route: '/gate-transit-monitor', primary: false },
        { label: 'Xử lý ngoại lệ', route: '/exceptions', primary: false },
    ]
})

const heroActions = computed(() => {
    if (currentRole.value === 'QuanLy') {
        return [
            { label: 'Mở hàng chờ', route: '/exceptions', primary: true },
            { label: 'Tra cứu nhật ký', route: '/access-logs', primary: false },
            { label: 'Xem báo cáo', route: '/attendance/reports', primary: false },
        ]
    }

    return [
        { label: 'Xem cảnh báo', route: '/soc-console', primary: true },
        { label: 'Duyệt khách hẹn', route: resolveRoute('/pre-registrations', '/exceptions'), primary: false },
        { label: 'Kiểm tra thiết bị', route: resolveRoute('/device-health', '/exceptions'), primary: false },
    ]
})

const priorityQueue = computed(() => [
    {
        label: 'Cảnh báo SOC',
        value: snapshot.value.criticalOpenAlarms || 0,
        helper: `${snapshot.value.openAlarms || 0} mở`,
        description: 'Điểm vào nhanh để xác nhận báo động, phân công người xử lý và đóng vòng phản ứng.',
        route: resolveRoute('/soc-console', '/exceptions'),
        tone: (snapshot.value.criticalOpenAlarms || 0) > 0 ? 'danger' : 'neutral',
    },
    {
        label: 'Yêu cầu can thiệp',
        value: snapshot.value.pendingInterventions || 0,
        helper: (snapshot.value.oldestPendingInterventionMinutes || 0) > 0
            ? `${snapshot.value.oldestPendingInterventionMinutes} phút`
            : 'Sạch hàng chờ',
        description: 'Các tình huống cần quản lý hoặc quản trị chấp nhận trước khi thực thi.',
        route: '/exceptions',
        tone: (snapshot.value.pendingInterventions || 0) > 0 ? 'warn' : 'neutral',
    },
    {
        label: 'Khách hẹn trước chờ duyệt',
        value: snapshot.value.pendingRegistrations || 0,
        helper: `${snapshot.value.expectedVisitorsToday || 0} khách hôm nay`,
        description: 'Những lượt vào site cần được chốt trước giờ cao điểm để tránh ùn ở cổng hoặc lễ tân.',
        route: resolveRoute('/pre-registrations', '/exceptions'),
        tone: (snapshot.value.pendingRegistrations || 0) > 0 ? 'warn' : 'neutral',
    },
    {
        label: 'Thiết bị cần kiểm tra',
        value: (snapshot.value.offlineDevices || 0) + (snapshot.value.degradedDevices || 0),
        helper: `${snapshot.value.camerasConfigured || 0} camera / ${snapshot.value.gatesConfigured || 0} cổng`,
        description: 'Các thiết bị mất kết nối hoặc suy giảm thường là nguồn gây lỗi vận hành khó demo nhất.',
        route: resolveRoute('/device-health', '/exceptions'),
        tone: ((snapshot.value.offlineDevices || 0) + (snapshot.value.degradedDevices || 0)) > 0 ? 'warn' : 'neutral',
    },
    {
        label: 'Sức khỏe làn',
        value: laneHealthSummary.value.degradedCount,
        helper: laneHealthSummary.value.degradedCount > 0
            ? laneHealthSummary.value.degradedNames.join(', ')
            : `${laneHealthSummary.value.healthyCount}/${laneHealthSummary.value.total} ổn định`,
        description: 'Tóm tắt những làn không có event mới hoặc barrier đang ở trạng thái cần theo dõi để quản lý bám sát từ mặt nhìn tổng quan.',
        route: resolveRoute('/enterprise-security', resolveRoute('/gate-transit-monitor', '/exceptions')),
        tone: laneHealthSummary.value.degradedCount > 0 ? 'warn' : 'neutral',
    },
    {
        label: 'Nghỉ phép chờ duyệt',
        value: snapshot.value.pendingLeaveApprovals || 0,
        helper: `${snapshot.value.totalShiftsToday || 0} ca hôm nay`,
        description: 'Tác động gián tiếp đến vận hành vì làm thay đổi nhân lực thật sự còn có mặt trong ca.',
        route: resolveRoute('/attendance/leave-approvals', '/attendance/reports'),
        tone: (snapshot.value.pendingLeaveApprovals || 0) > 0 ? 'neutral' : 'neutral',
    },
])

const sitePictureItems = computed(() => [
    {
        label: 'Xe đang ở trong bãi',
        value: snapshot.value.vehiclesInside || 0,
        hint: 'Tình trạng tồn xe hiện tại',
    },
    {
        label: 'Khách đã check-in',
        value: snapshot.value.checkedInVisitors || 0,
        hint: 'Người ngoài đang có mặt trong site',
    },
    {
        label: 'Hồ sơ nhân sự',
        value: snapshot.value.employeeCount || 0,
        hint: 'Tổng nhân sự trong dữ liệu',
    },
    {
        label: 'Hồ sơ khách',
        value: snapshot.value.guestProfiles || 0,
        hint: 'Tập hồ sơ phục vụ kiểm soát khách',
    },
    {
        label: 'Camera đã cấu hình',
        value: snapshot.value.camerasConfigured || 0,
        hint: 'Nguồn theo dõi hiện trường',
    },
    {
        label: 'Cổng / làn đang quản lý',
        value: snapshot.value.gatesConfigured || 0,
        hint: 'Điểm kiểm soát trong phạm vi hiện tại',
    },
    {
        label: 'Làn ổn định',
        value: laneHealthSummary.value.healthyCount,
        hint: laneHealthSummary.value.degradedCount > 0
            ? `${laneHealthSummary.value.degradedCount} làn cần chú ý`
            : 'Không có làn suy giảm',
    },
])

const workforceItems = computed(() => [
    {
        label: 'Nhân sự có lịch làm',
        value: snapshot.value.employeesWorkingToday || 0,
        note: 'Quy mô lực lượng dự kiến trong ngày',
    },
    {
        label: 'Chưa check-in',
        value: snapshot.value.employeesNotCheckedIn || 0,
        note: 'Cần phân biệt vắng mặt thật và dữ liệu chưa lên',
    },
    {
        label: 'Đi trễ',
        value: snapshot.value.employeesLateToday || 0,
        note: 'Một tín hiệu để rà lại kỷ luật vào ca',
    },
    {
        label: 'Tổng ca hôm nay',
        value: snapshot.value.totalShiftsToday || 0,
        note: 'Mốc nền để giải nghĩa các số liệu hiện diện',
    },
    {
        label: 'Tăng ca ghi nhận',
        value: `${Number(snapshot.value.totalOvertimeHoursToday || 0).toFixed(1)}h`,
        note: 'Dùng để đọc áp lực vận hành kéo dài',
    },
    {
        label: 'QR / AI coverage',
        value: `${snapshot.value.recognitionCoverage || 0}%`,
        note: 'Hiển thị độ đầy đủ dữ liệu nhận diện hiện có',
    },
])

const topInsights = computed(() => (intelligence.value?.insights || []).slice(0, 3))

function activityKindShort(kind) {
    switch (kind) {
        case 'Alarm': return 'AL'
        case 'Lane': return 'LN'
        case 'Intervention': return 'CT'
        default: return 'AC'
    }
}

function formatRelativeTime(value) {
    if (!value) return '--'
    const diffMs = Date.now() - new Date(value).getTime()
    const diffMinutes = Math.max(0, Math.round(diffMs / 60000))

    if (diffMinutes < 1) return 'vừa xong'
    if (diffMinutes < 60) return `${diffMinutes} phút trước`

    const diffHours = Math.round(diffMinutes / 60)
    if (diffHours < 24) return `${diffHours} giờ trước`

    const diffDays = Math.round(diffHours / 24)
    return `${diffDays} ngày trước`
}

async function loadDashboard() {
    isLoading.value = true
    loadError.value = ''
    try {
        const [overviewResult, laneHealthResult] = await Promise.allSettled([
            getDashboardOverview(),
            enterpriseApi.getLaneHealth(),
        ])

        if (overviewResult.status !== 'fulfilled') {
            throw overviewResult.reason
        }

        const { data } = overviewResult.value
        snapshot.value = { ...snapshot.value, ...(data.snapshot || {}) }
        weeklyTraffic.value = data.weeklyTraffic || []
        recentActivities.value = data.recentActivities || []
        laneHealth.value = laneHealthResult.status === 'fulfilled' ? (laneHealthResult.value.data || []) : []
    } catch (error) {
        console.error('Dashboard load error:', error)
        loadError.value = 'Không thể tải bức tranh vận hành tổng quan.'
    } finally {
        isLoading.value = false
    }
}

async function loadIntelligence() {
    intelligenceLoading.value = true
    try {
        const { data } = await getDashboardIntelligence()
        intelligence.value = data
    } catch (error) {
        console.error('Intelligence load error:', error)
    } finally {
        intelligenceLoading.value = false
    }
}

onMounted(async () => {
    await Promise.allSettled([loadDashboard(), loadIntelligence()])
})
</script>

<style scoped>
.dashboard-ops {
    gap: 20px;
}

.dashboard-subtitle {
    margin-top: 12px;
    max-width: 70ch;
}

.command-deck {
    display: grid;
    grid-template-columns: minmax(0, 1.45fr) minmax(320px, 0.9fr);
    gap: 18px;
}

.command-hero,
.command-side {
    border: 1px solid rgba(255, 255, 255, 0.72);
    border-radius: 28px;
    box-shadow: var(--shadow-sm);
    backdrop-filter: var(--glass-blur);
}

.command-hero {
    padding: 28px;
    background:
        radial-gradient(circle at top left, rgba(84, 196, 211, 0.16), transparent 34%),
        linear-gradient(180deg, rgba(255, 255, 255, 0.94), rgba(248, 252, 253, 0.92));
}

.command-side {
    padding: 24px;
    background:
        radial-gradient(circle at top right, rgba(84, 196, 211, 0.18), transparent 35%),
        linear-gradient(180deg, rgba(16, 32, 51, 0.97), rgba(24, 49, 77, 0.95));
    color: var(--text-inverse);
}

.hero-topline,
.side-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    flex-wrap: wrap;
}

.hero-updated {
    color: var(--text-muted);
    font-size: 0.86rem;
}

.hero-copy {
    margin-top: 18px;
}

.hero-copy h2 {
    font-family: var(--font-heading);
    font-size: clamp(1.8rem, 2.6vw, 2.6rem);
    line-height: 1.04;
    max-width: 18ch;
}

.hero-copy p {
    margin-top: 12px;
    max-width: 62ch;
    color: var(--text-secondary);
    font-size: 1rem;
    line-height: 1.7;
}

.hero-chip-row {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
    margin-top: 22px;
}

.side-head strong {
    font-family: var(--font-heading);
    font-size: 1.08rem;
}

.command-metric-grid {
    margin-top: 18px;
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 12px;
}

.command-metric {
    padding: 16px;
    border-radius: 20px;
    border: 1px solid rgba(239, 247, 248, 0.08);
    background: rgba(255, 255, 255, 0.06);
}

.command-metric.warn {
    border-color: rgba(216, 155, 55, 0.22);
    background: rgba(216, 155, 55, 0.12);
}

.command-metric.danger {
    border-color: rgba(195, 81, 70, 0.24);
    background: rgba(195, 81, 70, 0.12);
}

.command-metric span,
.command-metric small {
    display: block;
}

.command-metric span {
    color: rgba(239, 247, 248, 0.78);
    font-size: 0.82rem;
}

.command-metric strong {
    display: block;
    margin-top: 10px;
    font-family: var(--font-heading);
    font-size: 2rem;
    line-height: 1;
}

.command-metric small {
    margin-top: 10px;
    color: rgba(239, 247, 248, 0.68);
    line-height: 1.5;
}

.queue-list,
.intel-stack {
    display: grid;
    gap: 12px;
}

.queue-item {
    display: grid;
    grid-template-columns: minmax(0, 1fr) 112px;
    gap: 16px;
    padding: 16px 18px;
    border-radius: 20px;
    border: 1px solid rgba(24, 49, 77, 0.08);
    background: rgba(236, 244, 246, 0.72);
    transition: transform var(--transition-normal), border-color var(--transition-normal), box-shadow var(--transition-normal);
}

.queue-item:hover,
.event-row:hover {
    transform: translateY(-2px);
    border-color: var(--border-color-hover);
    box-shadow: var(--shadow-md);
}

.queue-item.warn {
    border-color: rgba(216, 155, 55, 0.18);
}

.queue-item.danger {
    border-color: rgba(195, 81, 70, 0.18);
}

.queue-main strong,
.event-copy strong {
    color: var(--text-primary);
    font-size: 0.98rem;
}

.queue-main p,
.event-copy p {
    margin-top: 6px;
    color: var(--text-secondary);
    font-size: 0.88rem;
    line-height: 1.6;
}

.queue-side {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    justify-content: center;
    gap: 4px;
    text-align: right;
}

.queue-value {
    font-family: var(--font-heading);
    font-size: 1.8rem;
    line-height: 1;
    color: var(--text-primary);
}

.queue-side small,
.surface-hint {
    color: var(--text-muted);
    font-size: 0.78rem;
    line-height: 1.5;
}

.event-row {
    display: grid;
    grid-template-columns: 50px minmax(0, 1fr);
    gap: 14px;
    padding: 14px 16px;
    border-radius: 20px;
    border: 1px solid rgba(24, 49, 77, 0.08);
    background: rgba(236, 244, 246, 0.72);
    transition: transform var(--transition-normal), border-color var(--transition-normal), box-shadow var(--transition-normal);
}

.event-icon {
    width: 50px;
    height: 50px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    border-radius: 16px;
    font-weight: 800;
    font-size: 0.84rem;
    color: var(--accent-primary);
    background: rgba(15, 124, 130, 0.1);
}

.event-icon.warn {
    color: var(--accent-warning);
    background: rgba(184, 111, 33, 0.12);
}

.event-icon.danger {
    color: var(--accent-danger);
    background: rgba(195, 81, 70, 0.12);
}

.event-topline,
.dashboard-surface-line {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 12px;
}

.event-topline span {
    color: var(--text-muted);
    font-size: 0.8rem;
    white-space: nowrap;
}

.today-grid {
    display: grid;
    grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 12px;
}

.today-card {
    padding: 16px;
    border-radius: 18px;
    border: 1px solid rgba(24, 49, 77, 0.08);
    background: rgba(236, 244, 246, 0.68);
}

.today-card span,
.today-card small {
    display: block;
}

.today-card span {
    color: var(--text-secondary);
    font-size: 0.84rem;
}

.today-card strong {
    display: block;
    margin-top: 8px;
    font-family: var(--font-heading);
    font-size: 1.7rem;
    line-height: 1;
}

.today-card small {
    margin-top: 8px;
    color: var(--text-muted);
    line-height: 1.55;
}

.intel-summary {
    margin: 0;
}

.intel-insights {
    display: grid;
    gap: 10px;
}

.intel-insight {
    padding: 14px 16px;
    border-radius: 16px;
    border: 1px solid rgba(24, 49, 77, 0.08);
    background: rgba(236, 244, 246, 0.58);
}

.intel-insight.warning {
    border-color: rgba(216, 155, 55, 0.18);
    background: rgba(216, 155, 55, 0.07);
}

.intel-insight.critical {
    border-color: rgba(195, 81, 70, 0.18);
    background: rgba(195, 81, 70, 0.07);
}

.intel-insight p {
    margin-top: 6px;
    color: var(--text-secondary);
    font-size: 0.88rem;
    line-height: 1.6;
}

.traffic-chart {
    display: grid;
    grid-template-columns: repeat(7, minmax(44px, 1fr));
    align-items: end;
    gap: 14px;
    min-height: 220px;
    padding: 26px 14px 8px;
    overflow: hidden;
    border: 1px solid rgba(24, 49, 77, 0.06);
    border-radius: 18px;
    background:
        repeating-linear-gradient(to top, rgba(24, 49, 77, 0.055) 0 1px, transparent 1px 25%),
        linear-gradient(180deg, rgba(236, 244, 246, 0.48), rgba(255, 255, 255, 0));
}

.chart-day {
    min-width: 0;
    display: grid;
    grid-template-rows: 180px auto;
    gap: 10px;
    text-align: center;
}

.chart-day > strong {
    color: var(--text-secondary);
    font-size: 0.78rem;
}

.chart-stack {
    height: 180px;
    display: flex;
    align-items: flex-end;
    justify-content: center;
    gap: 6px;
}

.chart-bar {
    position: relative;
    width: min(24px, 42%);
    min-height: 0;
    border-radius: 9px 9px 4px 4px;
    transition: height 420ms ease, filter 180ms ease, transform 180ms ease;
}

.chart-bar:hover {
    filter: brightness(0.96);
    transform: translateY(-2px);
}

.chart-bar.in {
    background: linear-gradient(180deg, #55c6cb, #0f7c82);
    box-shadow: 0 8px 18px rgba(15, 124, 130, 0.18);
}

.chart-bar.out {
    background: linear-gradient(180deg, #f4c46d, #c47d2d);
    box-shadow: 0 8px 18px rgba(196, 125, 45, 0.16);
}

.chart-bar span {
    position: absolute;
    left: 50%;
    bottom: calc(100% + 6px);
    transform: translateX(-50%);
    color: var(--text-primary);
    font-size: 0.72rem;
    font-weight: 800;
}

.chart-bar[style*="height: 0%"] span {
    bottom: 4px;
    color: var(--text-muted);
}

@media (max-width: 1180px) {
    .command-deck,
    .today-grid {
        grid-template-columns: 1fr;
    }
}

@media (max-width: 900px) {
    .command-metric-grid,
    .today-grid {
        grid-template-columns: 1fr 1fr;
    }

    .queue-item {
        grid-template-columns: 1fr;
    }

    .queue-side {
        align-items: flex-start;
        text-align: left;
    }
}

@media (max-width: 640px) {
    .command-metric-grid,
    .today-grid {
        grid-template-columns: 1fr;
    }

    .event-row {
        grid-template-columns: 1fr;
    }

    .event-icon {
        width: 42px;
        height: 42px;
    }

    .event-topline,
    .dashboard-surface-line {
        flex-direction: column;
    }

    .traffic-chart {
        gap: 8px;
        padding-inline: 5px;
    }

    .chart-stack { gap: 3px; }
    .chart-bar span { font-size: 0.65rem; }
}
</style>
